Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' Speechmatics-only clause hold-and-lock coordinator (one accumulator per
    ''' conference room). Owns the buffering/holding/locking DECISION state
    ''' machine — which fragments merge into a clause and when a clause locks —
    ''' while translation + broadcast EXECUTION stays with the caller, invoked
    ''' through the <c>onClauseLocked</c> callback (an Async Sub on the controller
    ''' that contains its own exceptions; locks fire on sidecar event threads or
    ''' this class's flush timer's threadpool thread, exactly as before extraction).
    '''
    ''' HYBRID dial semantics: rooms whose STT template explicitly stores clause
    ''' fields get their resolved Speechmatics block pinned for the session;
    ''' rooms without an entry read the live app-global dials on EVERY call —
    ''' nothing is cached at construction — so Options changes apply immediately
    ''' without restarting a room.
    ''' </summary>
    Friend Class SpeechmaticsClauseCoordinator

        ''' <summary>The engine this subsystem is gated to — engine knowledge lives here, not in shared controller code.</summary>
        Private Const HoldEngineKey As String = "speechmatics"

        Private ReadOnly _config As AppConfig
        Private ReadOnly _roomBackendKey As Func(Of String, String)
        Private ReadOnly _onClauseLocked As Action(Of String, String, String)
        Private ReadOnly _onTimerTick As Action
        Private ReadOnly _log As Action(Of String)
        ' (roomId, clauseText) → sentences. Splits a held clause into proper
        ' sentences at the pause via live-server's SaT segmenter (list-free,
        ' engine-agnostic). Nothing = SaT segmentation unavailable → single emit.
        Private ReadOnly _segment As Func(Of String, String, List(Of String))

        ' Speechmatics-only clause hold-and-lock accumulators (one per room). Gated
        ' behind AppConfig.SpeechmaticsHoldClauses + backend == "speechmatics".
        Private ReadOnly _clauseAccumulators As New Concurrent.ConcurrentDictionary(Of String, SpeechmaticsClauseAccumulator)()
        ' HYBRID clause dials: rooms whose STT template explicitly stores clause
        ' fields get their resolved block pinned here (fixed for the session);
        ' rooms without an entry keep reading the live app-global dials.
        Private ReadOnly _pinnedClauseDials As New Concurrent.ConcurrentDictionary(Of String, Configs.SpeechmaticsConfig)()
        Private _bufferFlushTimer As System.Threading.Timer

        ' ── Grace auto-tune ─────────────────────────────────────────────────
        ' The EOU auto-tune adapts where SPEECHMATICS cuts; this adapts whether WE
        ' stitch across those cuts. A burst-pause speaker ("three words … pause")
        ' has inter-commit gaps longer than the fixed GraceMs, so every burst locked
        ' alone as a tiny fragment. Track the rolling inter-commit gaps per room and
        ' widen grace toward their p75 (never below the configured value, capped —
        ' grace is pure latency). Over-merged sentences are split downstream by SaT.
        ' Pinned template dials (the manual override) are never adapted.
        Private Const GraceWindowSize As Integer = 40     ' rolling inter-commit gaps kept
        Private Const GraceMinSamples As Integer = 12     ' don't adapt on thin data
        Private Const GraceCapMs As Integer = 3000        ' latency ceiling
        Private Const GraceGapCapMs As Integer = 8000     ' longer = real silence, not pace
        Private Const GraceHeadroomMs As Integer = 250    ' stitch margin above the p75 gap
        Private ReadOnly _interCommitGaps As New Concurrent.ConcurrentDictionary(Of String, Concurrent.ConcurrentQueue(Of Integer))()
        Private ReadOnly _lastFeedTick As New Concurrent.ConcurrentDictionary(Of String, Long)()
        Private ReadOnly _adaptiveGraceMs As New Concurrent.ConcurrentDictionary(Of String, Integer)()

        ' ── Per-room STT quality scoreboard ─────────────────────────────────
        ' One summary line at room close (CONF_ROOM_STT_SUMMARY) so tuning changes
        ' can be A/B'd across services without hand-mining the per-event log.
        Private Class RoomSttStats
            Public ReadOnly Gate As New Object()
            Public Commits As Integer            ' raw Speechmatics commits fed in
            Public DupCommits As Integer         ' consecutive identical commits (repetition signal)
            Public LastCommitText As String = ""
            Public Merged As Integer             ' locks that stitched >1 fragment
            Public SatSplits As Integer          ' locks SaT split into >1 sentence
            Public ReadOnly LockChars As New List(Of Integer)   ' clause sizes
            Public ReadOnly HoldMs As New List(Of Integer)      ' latency spent holding
        End Class
        Private ReadOnly _roomStats As New Concurrent.ConcurrentDictionary(Of String, RoomSttStats)()
        Private Const StatsListCap As Integer = 5000   ' ~8h of locks; plenty, bounded

        ''' <param name="config">Live AppConfig reference — dials are read fresh on every call (never cached) so Options stays live-tunable.</param>
        ''' <param name="roomBackendKey">Resolves a room's STT backend key (room template override, else global config).</param>
        ''' <param name="onClauseLocked">(roomId, text, detectedLang) — broadcast a locked clause; execution stays with the caller.</param>
        ''' <param name="onTimerTick">Caller hook run on each flush-timer tick BEFORE expired clauses are locked (NLLB sentence-buffer flush).</param>
        ''' <param name="segment">(roomId, clauseText) → sentences; splits a held clause via SaT at the pause. Nothing = feature off (single emit).</param>
        Public Sub New(config As AppConfig,
                       roomBackendKey As Func(Of String, String),
                       onClauseLocked As Action(Of String, String, String),
                       onTimerTick As Action,
                       log As Action(Of String),
                       Optional segment As Func(Of String, String, List(Of String)) = Nothing)
            _config = config
            _roomBackendKey = roomBackendKey
            _onClauseLocked = onClauseLocked
            _onTimerTick = onTimerTick
            _log = log
            _segment = segment

            ' Timer to flush expired sentence buffers (NLLB path, via onTimerTick)
            ' and lock expired Speechmatics clause accumulators. Re-arms itself
            ' each tick to the current SpeechmaticsClauseTimerMs dial so poll
            ' resolution is live-tunable.
            _bufferFlushTimer = New System.Threading.Timer(
                AddressOf BufferTimerTick, Nothing, 1000, System.Threading.Timeout.Infinite)
        End Sub

        ''' <summary>ConcurrentDictionary remove without the ByRef-out ceremony.</summary>
        Private Shared Sub DropKey(Of TValue)(dict As Concurrent.ConcurrentDictionary(Of String, TValue), key As String)
            Dim ignored As TValue = Nothing
            dict.TryRemove(key, ignored)
        End Sub

        Private Sub BufferTimerTick(state As Object)
            Try
                _onTimerTick?.Invoke()
                FlushExpiredClauses()
            Catch ex As Exception
                _log($"[Conference] BufferTimerTick error: {ex.Message}")
            Finally
                Try
                    Dim interval = Math.Max(50, _config.SpeechmaticsClauseTimerMs)
                    _bufferFlushTimer?.Change(interval, System.Threading.Timeout.Infinite)
                Catch
                End Try
            End Try
        End Sub

        ''' <summary>Whether clause hold-and-lock applies to this room (pinned template value, else live master switch; Speechmatics only).</summary>
        Public Function IsHoldEnabled(roomId As String) As Boolean
            Dim pinned As Configs.SpeechmaticsConfig = Nothing
            ' SaT segmentation needs the buffering path, so enabling "Split with SaT"
            ' implies Hold & merge — the operator only has to tick one switch.
            If _pinnedClauseDials.TryGetValue(roomId, pinned) Then Return pinned.HoldClauses OrElse pinned.UseSat
            Return (_config.SpeechmaticsHoldClauses OrElse _config.SpeechmaticsUseSat) AndAlso
                   _roomBackendKey(roomId).Equals(HoldEngineKey, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Buffer-to-pause dials for a room: the template-pinned values when the
        ''' room's STT template set them, otherwise a live read of the app-global
        ''' dials (Options changes apply immediately to unpinned rooms). Segmentation
        ''' of the merged clause is done downstream by SaT, not here.
        ''' </summary>
        Private Function CurrentThresholds(roomId As String) As SpeechmaticsClauseAccumulator.Thresholds
            Dim pinned As Configs.SpeechmaticsConfig = Nothing
            If _pinnedClauseDials.TryGetValue(roomId, pinned) Then
                ' Pinned template dials = explicit operator override — never adapted.
                Return New SpeechmaticsClauseAccumulator.Thresholds With {
                    .GraceMs = pinned.ClauseGraceMs,
                    .MaxMs = pinned.ClauseMaxMs,
                    .MaxChars = pinned.ClauseMaxChars
                }
            End If
            Dim graceMs = _config.SpeechmaticsClauseGraceMs
            ' Grace auto-tune (rides the same master switch as the EOU auto-tune):
            ' widen — never shrink — toward the room's observed inter-commit gaps.
            If _config.SpeechmaticsAutoTuneEou Then
                Dim adaptive As Integer
                If _adaptiveGraceMs.TryGetValue(roomId, adaptive) AndAlso adaptive > graceMs Then graceMs = adaptive
            End If
            Return New SpeechmaticsClauseAccumulator.Thresholds With {
                .GraceMs = graceMs,
                .MaxMs = _config.SpeechmaticsClauseMaxMs,
                .MaxChars = _config.SpeechmaticsClauseMaxChars
            }
        End Function

        ''' <summary>Record the gap since the room's previous Speechmatics commit and recompute the adaptive grace.</summary>
        Private Sub TrackInterCommitGap(roomId As String)
            Dim now = Environment.TickCount64
            Dim last As Long
            If _lastFeedTick.TryGetValue(roomId, last) Then
                Dim gap = CInt(Math.Min(Integer.MaxValue, now - last))
                If gap > 0 AndAlso gap <= GraceGapCapMs Then
                    Dim q = _interCommitGaps.GetOrAdd(roomId, Function(k) New Concurrent.ConcurrentQueue(Of Integer)())
                    q.Enqueue(gap)
                    Dim dropped As Integer
                    While q.Count > GraceWindowSize
                        q.TryDequeue(dropped)
                    End While
                    UpdateAdaptiveGrace(roomId, q)
                End If
            End If
            _lastFeedTick(roomId) = now
        End Sub

        Private Sub UpdateAdaptiveGrace(roomId As String, q As Concurrent.ConcurrentQueue(Of Integer))
            Dim gaps = q.ToArray()
            If gaps.Length < GraceMinSamples Then Return
            Array.Sort(gaps)
            Dim p75 = gaps(CInt(Math.Min(gaps.Length - 1, Math.Floor(gaps.Length * 0.75))))
            Dim target = Math.Min(GraceCapMs, p75 + GraceHeadroomMs)
            Dim baseMs = _config.SpeechmaticsClauseGraceMs
            If target < baseMs Then target = baseMs   ' config is the floor
            Dim previous As Integer = 0
            _adaptiveGraceMs.TryGetValue(roomId, previous)
            If previous = 0 Then previous = baseMs
            If Math.Abs(target - previous) >= 250 Then
                _adaptiveGraceMs(roomId) = target
                AppLogger.Log(LogCategory.Conference, LogSeverity.Info,
                    $"[Conference:{roomId}] grace auto-tune {previous}ms -> {target}ms " &
                    $"(p75 inter-commit gap {p75}ms over {gaps.Length} commits)")
            End If
        End Sub

        ''' <summary>Whether SaT sentence-splitting is active for this room (needs the segment delegate, the hold/buffering path, and the toggle — pinned template value else app-global).</summary>
        Private Function IsSatEnabled(roomId As String) As Boolean
            If _segment Is Nothing Then Return False
            Dim pinned As Configs.SpeechmaticsConfig = Nothing
            If _pinnedClauseDials.TryGetValue(roomId, pinned) Then Return pinned.UseSat
            Return _config.SpeechmaticsUseSat AndAlso
                   _roomBackendKey(roomId).Equals(HoldEngineKey, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Pin a room's clause dials when its STT template explicitly stores any
        ''' clause field; otherwise the room keeps live app-global dials.
        ''' </summary>
        Public Sub StorePinnedClauseDials(roomId As String, engineTpl As EveryTongue.Models.Templates.EngineTemplate, sttConfig As SttSessionConfig)
            DropKey(_pinnedClauseDials, roomId)
            Dim sm = sttConfig.Block(Of Configs.SpeechmaticsConfig)()
            If sm Is Nothing OrElse engineTpl Is Nothing OrElse Not engineTpl.Config.HasValue Then Return
            If engineTpl.Config.Value.ValueKind <> System.Text.Json.JsonValueKind.Object Then Return
            For Each prop In engineTpl.Config.Value.EnumerateObject()
                If prop.Name.Equals("HoldClauses", StringComparison.OrdinalIgnoreCase) OrElse
                   prop.Name.StartsWith("Clause", StringComparison.OrdinalIgnoreCase) Then
                    _pinnedClauseDials(roomId) = sm
                    AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                        $"[Conference:{roomId}] clause dials pinned by template '{engineTpl.Name}' (hold={sm.HoldClauses}, grace={sm.ClauseGraceMs}ms)")
                    Return
                End If
            Next
        End Sub

        ''' <summary>Feed a Speechmatics fragment into the room's clause accumulator; broadcast if it locks.</summary>
        Public Sub FeedClause(roomId As String, text As String, detectedLang As String)
            If String.IsNullOrWhiteSpace(text) Then Return
            TrackInterCommitGap(roomId)
            ' Scoreboard: raw commit count + consecutive-duplicate detection (the
            ' "repetitions" the audience sees — STT re-emitting the same clause).
            Dim stats = _roomStats.GetOrAdd(roomId, Function(k) New RoomSttStats())
            SyncLock stats.Gate
                stats.Commits += 1
                Dim norm = text.Trim().ToLowerInvariant()
                If norm.Length > 0 AndAlso norm = stats.LastCommitText Then stats.DupCommits += 1
                stats.LastCommitText = norm
            End SyncLock
            Dim acc = _clauseAccumulators.GetOrAdd(roomId, Function(k) New SpeechmaticsClauseAccumulator())
            Dim locked = acc.Add(text, detectedLang, CurrentThresholds(roomId))

            ' Per-fragment trace: the gap before this fragment is the raw signal for
            ' tuning GraceMs (gaps the speaker took mid-clause vs. between clauses).
            AppLogger.Log(LogEvents.CONF_CLAUSE_FRAGMENT,
                $"room={roomId} frag#{acc.LastFragmentIndex} gapMs={acc.LastGapMs} fragChars={text.Trim().Length} lang={detectedLang}")

            If locked IsNot Nothing Then OnClauseLocked(roomId, locked)
        End Sub

        ''' <summary>Lock-on-timeout for all clause accumulators (grace-window expiry). Called from the timer.</summary>
        Private Sub FlushExpiredClauses()
            If _clauseAccumulators.Count = 0 Then Return
            For Each kvp In _clauseAccumulators.ToList()
                Dim locked = kvp.Value.TryLockExpired(CurrentThresholds(kvp.Key))
                If locked IsNot Nothing Then OnClauseLocked(kvp.Key, locked)
            Next
        End Sub

        ''' <summary>Force-lock and broadcast any pending clause for a room (close/reset/shutdown).</summary>
        Public Sub ForceLockClause(roomId As String)
            Dim acc As SpeechmaticsClauseAccumulator = Nothing
            If _clauseAccumulators.TryGetValue(roomId, acc) Then
                Dim locked = acc.ForceLock()
                If locked IsNot Nothing Then OnClauseLocked(roomId, locked)
            End If
        End Sub

        ''' <summary>Force-lock and broadcast all pending clauses, then drop all accumulators (app shutdown).</summary>
        Public Sub ForceLockAll()
            For Each kvp In _clauseAccumulators.ToList()
                Dim locked = kvp.Value.ForceLock()
                If locked IsNot Nothing Then OnClauseLocked(kvp.Key, locked)
            Next
            _clauseAccumulators.Clear()
            ' App shutdown bypasses ClearRoom — still emit each room's scoreboard.
            For Each roomId In _roomStats.Keys.ToList()
                EmitRoomSummary(roomId)
            Next
            _roomStats.Clear()
        End Sub

        ''' <summary>Drop a room's accumulator + pinned dials (room closed). Does NOT force-lock — call <see cref="ForceLockClause"/> first.</summary>
        Public Sub ClearRoom(roomId As String)
            EmitRoomSummary(roomId)
            DropKey(_clauseAccumulators, roomId)
            DropKey(_pinnedClauseDials, roomId)
            DropKey(_interCommitGaps, roomId)
            DropKey(_lastFeedTick, roomId)
            DropKey(_adaptiveGraceMs, roomId)
            DropKey(_roomStats, roomId)
        End Sub

        ''' <summary>One-line STT quality scoreboard for the room — the A/B line for tuning changes.</summary>
        Private Sub EmitRoomSummary(roomId As String)
            Dim stats As RoomSttStats = Nothing
            If Not _roomStats.TryGetValue(roomId, stats) Then Return
            Dim line As String
            SyncLock stats.Gate
                If stats.LockChars.Count = 0 Then Return
                Dim chars = stats.LockChars.ToArray()
                Array.Sort(chars)
                Dim holds = stats.HoldMs.ToArray()
                Array.Sort(holds)
                Dim n = chars.Length
                Dim tiny = chars.TakeWhile(Function(c) c < 20).Count()
                Dim grace As Integer = 0
                _adaptiveGraceMs.TryGetValue(roomId, grace)
                line = $"room={roomId} locks={n} medianChars={Pct(chars, 50)} tiny(<20ch)={CInt(100 * tiny / n)}% " &
                       $"merged={CInt(100 * stats.Merged / n)}% satSplits={stats.SatSplits} " &
                       $"commits={stats.Commits} dupCommits={stats.DupCommits} " &
                       $"holdMs(p50/p95)={Pct(holds, 50)}/{Pct(holds, 95)} " &
                       $"grace={If(grace > 0, grace, _config.SpeechmaticsClauseGraceMs)}ms"
            End SyncLock
            AppLogger.Log(LogEvents.CONF_ROOM_STT_SUMMARY, line)
        End Sub

        ''' <summary>Percentile of a SORTED array (nearest-rank).</summary>
        Private Shared Function Pct(sorted As Integer(), p As Integer) As Integer
            If sorted Is Nothing OrElse sorted.Length = 0 Then Return 0
            Return sorted(Math.Min(sorted.Length - 1, CInt(Math.Floor(sorted.Length * p / 100.0))))
        End Function

        ''' <summary>
        ''' Emit per-lock tuning diagnostics, then broadcast the clause. The log line is
        ''' the single source for optimising the dials: reason shows which threshold
        ''' fired, gaps[] is the inter-fragment silence distribution (→ GraceMs),
        ''' frags/durMs/chars show whether the runaway caps (MaxMs/MaxChars) are biting.
        ''' </summary>
        Private Sub OnClauseLocked(roomId As String, locked As LockResult)
            If locked Is Nothing Then Return
            Dim t = CurrentThresholds(roomId)
            Dim gaps = If(locked.Gaps IsNot Nothing AndAlso locked.Gaps.Count > 0, String.Join(",", locked.Gaps), "")
            Dim maxGap = If(locked.Gaps IsNot Nothing AndAlso locked.Gaps.Count > 0, locked.Gaps.Max(), 0)
            AppLogger.Log(LogEvents.CONF_CLAUSE_LOCK,
                $"room={roomId} reason={locked.Reason} frags={locked.FragmentCount} durMs={locked.DurationMs} " &
                $"chars={If(locked.Text, "").Length} maxGapMs={maxGap} gaps=[{gaps}] " &
                $"dials(graceMs={t.GraceMs},maxMs={t.MaxMs},maxChars={t.MaxChars}) " &
                $"text=""{Truncate(locked.Text, 120)}""")

            ' Scoreboard: clause size, merge success, hold latency.
            Dim stats = _roomStats.GetOrAdd(roomId, Function(k) New RoomSttStats())
            SyncLock stats.Gate
                If stats.LockChars.Count < StatsListCap Then stats.LockChars.Add(If(locked.Text, "").Length)
                If stats.HoldMs.Count < StatsListCap Then stats.HoldMs.Add(locked.HoldMs)
                If locked.FragmentCount > 1 Then stats.Merged += 1
            End SyncLock

            ' SaT path: split the buffered clause into proper sentences at the pause
            ' (list-free, engine-agnostic) and emit each. Falls back to a single emit
            ' if SaT is off/unavailable (segment returns {text}).
            If IsSatEnabled(roomId) AndAlso Not String.IsNullOrWhiteSpace(locked.Text) Then
                Dim sentences = _segment(roomId, locked.Text)
                If sentences Is Nothing OrElse sentences.Count = 0 Then
                    sentences = New List(Of String) From {locked.Text}
                End If
                If sentences.Count > 1 Then
                    AppLogger.Log(LogEvents.CONF_CLAUSE_SAT_SEGMENT,
                        $"room={roomId} lang={locked.DetectedLanguage} chars={If(locked.Text, "").Length} " &
                        $"split into {sentences.Count} sentences: ""{Truncate(locked.Text, 120)}""")
                    SyncLock stats.Gate
                        stats.SatSplits += 1
                    End SyncLock
                End If
                For Each s In sentences
                    If Not String.IsNullOrWhiteSpace(s) Then _onClauseLocked(roomId, s, locked.DetectedLanguage)
                Next
                Return
            End If

            _onClauseLocked(roomId, locked.Text, locked.DetectedLanguage)
        End Sub

        Private Shared Function Truncate(s As String, n As Integer) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Return If(s.Length > n, s.Substring(0, n) & "…", s)
        End Function

    End Class
End Namespace
