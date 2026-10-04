Imports System.Threading

Namespace Services.Translation

    ''' <summary>
    ''' Where the time of ONE room translation went, so a slow or timed-out caption can
    ''' be explained from the log (field 2026-09-27 11:04: Salamandra went from ~0.6s to
    ''' 6-10s for half a minute and two sentences passed the room's 10s limit - the log
    ''' could not say whether they waited in a queue or the engine itself was slow).
    '''
    ''' The caller sets <see cref="Current"/> for the duration of one translation call;
    ''' it flows down the async call chain (the orchestrator's priority queue runs the
    ''' work in the caller's own flow), and each layer records its part:
    '''   QueueMs       - waiting for a slot in the orchestrator's priority queue
    '''   EngineWaitMs  - waiting for the engine's one-at-a-time gate (Salamandra)
    '''   EngineMs      - inside the engine's requests (llama-server /completion)
    '''   Prompt*/Gen*  - llama-server's own timings: prompt evaluation and generation
    ''' Stage says where the call was when it ended - for a timeout, where the time ran out.
    ''' Measurement only: nothing reads it to change behaviour.
    ''' </summary>
    Public NotInheritable Class TranslationTiming

        Private Shared ReadOnly _current As New AsyncLocal(Of TranslationTiming)

        ''' <summary>The timing record of the translation call in progress on this async flow, or Nothing.</summary>
        Public Shared Property Current As TranslationTiming
            Get
                Return _current.Value
            End Get
            Set(value As TranslationTiming)
                _current.Value = value
            End Set
        End Property

        Public Property QueueMs As Long = -1
        Public Property EngineWaitMs As Long
        Public Property EngineMs As Long
        Public Property Requests As Integer
        Public Property PromptTokens As Integer
        Public Property PromptMs As Double
        Public Property GenTokens As Integer
        Public Property GenMs As Double
        Public Property CachedTokens As Integer
        ''' <summary>"queue", "engine-wait", "engine" or "done".</summary>
        Public Property Stage As String = "queue"

        ''' <summary>One log fragment, e.g. "queue=0ms engineWait=3400ms engine=6600ms
        ''' (2 req; prompt 204 tok/120ms, gen 18 tok/430ms, cached 150 tok)".</summary>
        Public Function Describe() As String
            Dim parts As New List(Of String)
            parts.Add($"stage={Stage}")
            parts.Add(If(QueueMs < 0, "queue=never got a slot", $"queue={QueueMs}ms"))
            If EngineWaitMs > 0 Then parts.Add($"engineWait={EngineWaitMs}ms")
            If PromptTokens + GenTokens > 0 Then
                parts.Add($"engine={EngineMs}ms ({Requests} req; prompt {PromptTokens} tok/{PromptMs:F0}ms, gen {GenTokens} tok/{GenMs:F0}ms, cached {CachedTokens} tok)")
            ElseIf Requests > 0 OrElse EngineMs > 0 Then
                parts.Add($"engine={EngineMs}ms ({Requests} req)")
            End If
            Return String.Join(" ", parts)
        End Function

    End Class

End Namespace
