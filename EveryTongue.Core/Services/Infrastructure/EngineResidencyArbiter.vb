' EngineResidencyArbiter.vb — the "one at a time" system for local model engines
' (ENGINE_CONCURRENCY_PLAN, agreed 2026-09-03).
'
' Every LOCAL model process (registry-flagged offline engines: NLLB sidecars,
' llama-server, whisper live-servers) must ask here before loading. Config sets
' how many may be resident per category (default 1). A resident with zero leases
' is a WARM SPARE: it stays loaded for instant reuse and is evicted on demand
' when a different engine needs the slot. At the limit with every resident
' leased, the request is Denied — the caller re-routes (translation) or refuses
' the room (STT). Cloud/inline engines never call this.

Imports System.Threading

Namespace Services.Infrastructure

    Public Enum EngineCategory
        Stt
        Translation
        Tts
    End Enum

    Public Enum LoadDecisionKind
        ''' <summary>Slot available (a spare may have been evicted) — load, then RegisterResident.</summary>
        Granted
        ''' <summary>The same engine is already resident — share it; any lease was recorded.</summary>
        ShareExisting
        ''' <summary>At limit, all residents leased — do NOT load. Translation re-routes
        ''' to a ResidentBackendNames entry; STT refuses the room.</summary>
        Denied
    End Enum

    Public Class LoadDecision
        Public Kind As LoadDecisionKind
        ''' <summary>For Denied: orchestrator backend names of the leased residents (re-route candidates).</summary>
        Public ResidentBackendNames As New List(Of String)
        ''' <summary>For Denied: display names of the leased residents (operator messages).</summary>
        Public ResidentDisplays As New List(Of String)
        ''' <summary>For Granted after an eviction: what was evicted (log/UI text), else "".</summary>
        Public EvictedDisplay As String = ""
    End Class

    Public Class EngineResidencyArbiter

        Public Shared ReadOnly Instance As New EngineResidencyArbiter()

        Private Class Resident
            Public Category As EngineCategory
            Public Key As String           ' engine key ("salamandra") or model signature (pool sig)
            Public BackendName As String   ' orchestrator name for re-route ("" when n/a)
            Public Display As String
            Public ReadOnly Leases As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Public Evict As Action         ' stops the process + unregisters; owner-supplied
        End Class

        Private ReadOnly _lock As New Object()
        Private ReadOnly _residents As New List(Of Resident)

        ''' <summary>Per-category limit — wired to AppConfig by ConfigManager on every
        ''' load; defaults to 1 when unwired. Floor 1.</summary>
        Public Property LimitProvider As Func(Of EngineCategory, Integer)

        ''' <summary>Raised (outside the lock) when a category transitions to ZERO
        ''' leases — heads subscribe to re-warm the GLOBAL engine (decided 2026-09-03:
        ''' predictable idle state, last-used spare gives way to the Options engine).</summary>
        Public Event CategoryIdle As Action(Of EngineCategory)

        Private Function LimitFor(category As EngineCategory) As Integer
            Try
                Dim p = LimitProvider
                If p IsNot Nothing Then Return Math.Max(1, p(category))
            Catch
                ' A faulty limit provider must never break engine loads — the safe
                ' floor (1) below is the answer either way.
            End Try
            Return 1
        End Function

        ''' <summary>
        ''' Ask permission to load engine <paramref name="key"/>. When a spare must be
        ''' evicted, its evict action runs SYNCHRONOUSLY here (outside the lock) so a
        ''' Granted answer means the slot is genuinely free before the caller loads.
        ''' ownerId "" = warm-spare load (no lease).
        ''' </summary>
        Public Function RequestLoad(category As EngineCategory, key As String,
                                    backendName As String, display As String,
                                    Optional ownerId As String = "") As LoadDecision
            Dim toEvict As New List(Of Resident)
            Dim decision As New LoadDecision With {.Kind = LoadDecisionKind.Granted}

            SyncLock _lock
                Dim same = FindResident(category, key)
                If same IsNot Nothing Then
                    If Not String.IsNullOrEmpty(ownerId) Then same.Leases.Add(ownerId)
                    decision.Kind = LoadDecisionKind.ShareExisting
                    Return decision
                End If

                Dim limit = LimitFor(category)
                Dim inCategory = _residents.Where(Function(r) r.Category = category).ToList()
                Dim overBy = inCategory.Count - limit + 1
                If overBy > 0 Then
                    Dim spares = inCategory.Where(Function(r) r.Leases.Count = 0).Take(overBy).ToList()
                    If spares.Count < overBy Then
                        Dim leased = inCategory.Where(Function(r) r.Leases.Count > 0).ToList()
                        decision.Kind = LoadDecisionKind.Denied
                        decision.ResidentBackendNames = leased.Select(Function(r) r.BackendName).
                            Where(Function(n) Not String.IsNullOrEmpty(n)).ToList()
                        decision.ResidentDisplays = leased.Select(Function(r) r.Display).ToList()
                        Return decision
                    End If
                    For Each s In spares
                        _residents.Remove(s)
                        toEvict.Add(s)
                    Next
                    decision.EvictedDisplay = String.Join(", ", spares.Select(Function(s) s.Display))
                End If
            End SyncLock

            ' Evictions run OUTSIDE the lock — they stop processes (seconds) and may
            ' re-enter owner locks. Synchronous by design: Granted = slot truly free.
            For Each victim In toEvict
                AppLogger.Log(LogEvents.ENGINE_RESIDENCY,
                    $"{category}: evicting warm spare '{victim.Display}' to make room for '{display}' (swap cost = one model load)")
                Try
                    victim.Evict?.Invoke()
                Catch ex As Exception
                    AppLogger.Log(LogEvents.ENGINE_RESIDENCY_CONFLICT,
                        $"{category}: evicting '{victim.Display}' failed: {ex.Message}")
                End Try
            Next
            Return decision
        End Function

        ''' <summary>Record a successfully started resident (idempotent by category+key)
        ''' and log the guard-rail resident inventory. ownerId "" = warm spare.</summary>
        Public Sub RegisterResident(category As EngineCategory, key As String,
                                    backendName As String, display As String,
                                    evict As Action, Optional ownerId As String = "")
            Dim inventory As String
            SyncLock _lock
                Dim r = FindResident(category, key)
                If r Is Nothing Then
                    r = New Resident With {.Category = category, .Key = key,
                                           .BackendName = If(backendName, ""), .Display = display}
                    _residents.Add(r)
                End If
                r.Evict = evict
                If Not String.IsNullOrEmpty(ownerId) Then r.Leases.Add(ownerId)
                inventory = DescribeLocked(category)
            End SyncLock
            AppLogger.Log(LogEvents.ENGINE_RESIDENCY, $"{category} residents: {inventory} (limit {LimitFor(category)})")
        End Sub

        ''' <summary>Lease an already-resident engine for an owner (no-op if absent).</summary>
        Public Sub Lease(category As EngineCategory, key As String, ownerId As String)
            If String.IsNullOrEmpty(ownerId) Then Return
            SyncLock _lock
                FindResident(category, key)?.Leases.Add(ownerId)
            End SyncLock
        End Sub

        ''' <summary>Release every lease this owner holds in the category. The engine
        ''' STAYS resident as a warm spare. Raises CategoryIdle on the last lease.</summary>
        Public Sub ReleaseOwner(category As EngineCategory, ownerId As String)
            If String.IsNullOrEmpty(ownerId) Then Return
            Dim becameIdle = False
            SyncLock _lock
                Dim hadLeases = _residents.Any(Function(r) r.Category = category AndAlso r.Leases.Count > 0)
                For Each r In _residents
                    If r.Category = category Then r.Leases.Remove(ownerId)
                Next
                Dim hasLeases = _residents.Any(Function(r) r.Category = category AndAlso r.Leases.Count > 0)
                becameIdle = hadLeases AndAlso Not hasLeases
            End SyncLock
            If becameIdle Then
                AppLogger.Log(LogEvents.ENGINE_RESIDENCY, $"{category}: last lease released — residents stay warm as spares")
                RaiseEvent CategoryIdle(category)
            End If
        End Sub

        ''' <summary>The owner stopped this engine itself (options restart, shutdown) —
        ''' forget it without invoking its evict action.</summary>
        Public Sub DropResident(category As EngineCategory, key As String)
            SyncLock _lock
                _residents.RemoveAll(Function(r) r.Category = category AndAlso
                                                 r.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
            End SyncLock
        End Sub

        ''' <summary>True when this engine key is currently resident in the category.</summary>
        Public Function IsResident(category As EngineCategory, key As String) As Boolean
            SyncLock _lock
                Return FindResident(category, key) IsNot Nothing
            End SyncLock
        End Function

        ''' <summary>Human summary of a category's residents (for logs/UI).</summary>
        Public Function DescribeResidents(category As EngineCategory) As String
            SyncLock _lock
                Return DescribeLocked(category)
            End SyncLock
        End Function

        Private Function FindResident(category As EngineCategory, key As String) As Resident
            Return _residents.FirstOrDefault(Function(r) r.Category = category AndAlso
                                                         r.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
        End Function

        Private Function DescribeLocked(category As EngineCategory) As String
            Dim parts = _residents.Where(Function(r) r.Category = category).
                Select(Function(r) $"{r.Display} ({If(r.Leases.Count = 0, "spare", r.Leases.Count & " lease(s)")})").ToList()
            Return If(parts.Count = 0, "none", String.Join(", ", parts))
        End Function

    End Class

End Namespace
