Namespace Services.Stt

    ''' <summary>
    ''' Accumulates Speechmatics END_OF_UTTERANCE fragments into a complete clause
    ''' before it is translated and broadcast ("hold-until-pause"). This combats
    ''' fragmented speakers whose mid-clause pauses make Speechmatics fire an
    ''' utterance boundary on a partial thought — instead of translating "We need"
    ''' on its own, we wait and merge "a bit more time" into one clause.
    '''
    ''' Pure buffer-to-pause: fragments accumulate until the grace window elapses
    ''' (a real pause) or a runaway cap fires. Sentence SEGMENTATION of the merged
    ''' clause is done downstream by SaT (engine-agnostic), which replaced the old
    ''' lockPunct + per-language function-word lists.
    '''
    ''' Holds NO thresholds of its own — every dial is passed in via
    ''' <see cref="Thresholds"/> on each call, read fresh from AppConfig by the
    ''' caller, so Options changes apply live without restarting a room.
    ''' </summary>
    Friend Class SpeechmaticsClauseAccumulator

        ''' <summary>Why a clause was locked — drives the diagnostics that inform dial tuning.</summary>
        Friend Enum LockReason
            MaxChars        ' runaway length cap hit
            MaxMs           ' runaway age cap hit
            GraceTimeout    ' silence after last fragment exceeded GraceMs (the common case)
            LanguageChange  ' detected language switched mid-clause
            Force           ' room close / restart / shutdown
        End Enum

        ''' <summary>Live snapshot of the tunable dials, supplied by the caller each call.</summary>
        Friend Structure Thresholds
            Public GraceMs As Integer
            Public MaxMs As Integer
            Public MaxChars As Integer
        End Structure

        Private ReadOnly _buffer As New Text.StringBuilder()
        Private _lang As String = ""
        Private _lastFragmentTick As Long = 0   ' Environment.TickCount64
        Private _startTick As Long = 0
        Private _fragmentCount As Integer = 0
        Private ReadOnly _gaps As New List(Of Integer)()   ' inter-fragment silences (ms) swallowed into the clause

        ' Trace of the just-added fragment (NOT cleared by Flush, so the caller can
        ' log it even on the fragment that triggered the lock).
        Private _lastGapMs As Integer = 0
        Private _lastFragmentIndex As Integer = 0

        ''' <summary>Inter-fragment gap (ms) of the fragment most recently passed to <see cref="Add"/> (0 for the first in a clause).</summary>
        Public ReadOnly Property LastGapMs As Integer
            Get
                Return _lastGapMs
            End Get
        End Property

        ''' <summary>1-based index of the fragment most recently passed to <see cref="Add"/> within its clause.</summary>
        Public ReadOnly Property LastFragmentIndex As Integer
            Get
                Return _lastFragmentIndex
            End Get
        End Property

        ''' <summary>True if nothing is currently accumulated.</summary>
        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return _buffer.Length = 0
            End Get
        End Property

        ''' <summary>Detected language of the clause currently accumulating.</summary>
        Public ReadOnly Property CurrentLanguage As String
            Get
                Return _lang
            End Get
        End Property

        ''' <summary>
        ''' Add a fragment. Returns a <see cref="LockResult"/> only when a runaway cap
        ''' was hit (length/age); otherwise Nothing — keep accumulating, the grace
        ''' timer locks the clause once the speaker pauses.
        ''' </summary>
        Public Function Add(fragment As String, detectedLang As String, t As Thresholds) As LockResult
            If String.IsNullOrWhiteSpace(fragment) Then Return Nothing
            Dim now = Environment.TickCount64

            ' Language changed mid-clause — lock the old clause first, then start fresh.
            If _buffer.Length > 0 AndAlso Not String.IsNullOrEmpty(detectedLang) AndAlso
               Not String.IsNullOrEmpty(_lang) AndAlso detectedLang <> _lang Then
                Dim prev = Flush(LockReason.LanguageChange)
                AppendFragment(fragment, detectedLang, now)
                Return prev
            End If

            AppendFragment(fragment, detectedLang, now)

            ' Runaway guards — never let a clause grow without bound.
            If _buffer.Length >= t.MaxChars Then Return Flush(LockReason.MaxChars)
            If t.MaxMs > 0 AndAlso (now - _startTick) >= t.MaxMs Then Return Flush(LockReason.MaxMs)

            Return Nothing
        End Function

        ''' <summary>
        ''' Lock the clause if the grace window has elapsed since the last fragment
        ''' (the speaker paused). Call periodically from the controller's timer.
        ''' </summary>
        Public Function TryLockExpired(t As Thresholds) As LockResult
            If _buffer.Length = 0 Then Return Nothing
            Dim now = Environment.TickCount64
            If (now - _lastFragmentTick) >= t.GraceMs Then Return Flush(LockReason.GraceTimeout)
            ' Also honour the absolute age cap here in case fragments stopped arriving.
            If t.MaxMs > 0 AndAlso (now - _startTick) >= t.MaxMs Then Return Flush(LockReason.MaxMs)
            Return Nothing
        End Function

        ''' <summary>Force-lock any pending clause (room close / reset / shutdown).</summary>
        Public Function ForceLock() As LockResult
            If _buffer.Length = 0 Then Return Nothing
            Return Flush(LockReason.Force)
        End Function

        Private Sub AppendFragment(fragment As String, detectedLang As String, now As Long)
            Dim piece = fragment.Trim()
            If _buffer.Length = 0 Then
                _startTick = now
                _lastGapMs = 0
            Else
                _lastGapMs = CInt(Math.Min(Integer.MaxValue, now - _lastFragmentTick))
                _gaps.Add(_lastGapMs)
                If piece.Length > 0 AndAlso Not piece.StartsWith(" ") Then _buffer.Append(" ")
            End If
            _buffer.Append(piece)
            _fragmentCount += 1
            _lastFragmentIndex = _fragmentCount
            If Not String.IsNullOrEmpty(detectedLang) Then _lang = detectedLang
            _lastFragmentTick = now
        End Sub

        Private Function Flush(reason As LockReason) As LockResult
            Dim now = Environment.TickCount64
            Dim result As New LockResult With {
                .Text = _buffer.ToString().Trim(),
                .DetectedLanguage = _lang,
                .Reason = reason,
                .FragmentCount = _fragmentCount,
                .DurationMs = If(_startTick > 0, CInt(Math.Min(Integer.MaxValue, now - _startTick)), 0),
                .HoldMs = If(_lastFragmentTick > 0, CInt(Math.Min(Integer.MaxValue, now - _lastFragmentTick)), 0),
                .Gaps = New List(Of Integer)(_gaps)
            }
            _buffer.Clear()
            _lang = ""
            _lastFragmentTick = 0
            _startTick = 0
            _fragmentCount = 0
            _gaps.Clear()
            Return result
        End Function

    End Class

    ''' <summary>Result of locking a clause: the merged text plus diagnostics for dial tuning.</summary>
    Friend Class LockResult
        Public Property Text As String
        Public Property DetectedLanguage As String
        Public Property Reason As SpeechmaticsClauseAccumulator.LockReason
        Public Property FragmentCount As Integer
        Public Property DurationMs As Integer
        ''' <summary>Time (ms) from the LAST fragment's arrival to the lock — the latency the grace/hold actually spent before this clause could broadcast.</summary>
        Public Property HoldMs As Integer
        ''' <summary>Inter-fragment silences (ms) that were merged into this clause — the key signal for tuning GraceMs.</summary>
        Public Property Gaps As List(Of Integer)
    End Class

End Namespace
