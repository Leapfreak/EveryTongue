' SidecarReadiness.vb — the ONE way to wait for a sidecar/engine to become ready.
'
' Replaces the seven hand-rolled wall-clock deadline loops ("give up after 30s")
' that abandoned slow machines mid-load (ENGINE_CONCURRENCY_PLAN, field incident
' 2026-09-02). Semantics decided 2026-09-03:
'   - Keep waiting while there is PROGRESS: the process is alive AND has shown
'     activity (log output / a reachable-but-not-ready probe) within the idle
'     window. A loading engine that is visibly working keeps its slot however
'     slow the machine is.
'   - Give up ONLY after EngineLoadIdleTimeoutSeconds of NO progress, or
'     immediately when the process dies. There is NO absolute hard cap.
'   - Every probe HTTP request is individually capped so one hung request can
'     never eat the window (the 2026-09-02 hang class).

Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline

    Public Enum ReadinessOutcome
        Ready
        NoProgress      ' idle window elapsed with no sign of life
        ProcessExited   ' the process died — fail fast, no waiting
        Cancelled
    End Enum

    Public Structure ReadinessResult
        Public Outcome As ReadinessOutcome
        Public ElapsedMs As Long
        Public LastProbeError As String
    End Structure

    Public NotInheritable Class SidecarReadiness

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Ambient default for the idle window, refreshed from AppConfig at startup
        ''' and whenever Options are applied. Callers without direct config access
        ''' use this; floor 5s so a mis-edited config can't make every load fail.
        ''' </summary>
        Private Shared _defaultIdleTimeoutSeconds As Integer = 15

        Public Shared Property DefaultIdleTimeoutSeconds As Integer
            Get
                Return _defaultIdleTimeoutSeconds
            End Get
            Set(value As Integer)
                _defaultIdleTimeoutSeconds = Math.Max(5, value)
            End Set
        End Property

        ''' <summary>
        ''' Wait until <paramref name="probe"/> reports ready. Progress-aware:
        '''   probe True                      → Ready.
        '''   probe False / probe throws      → not ready; keep waiting while the
        '''                                     HOST activity signal shows life.
        '''   process dead                    → ProcessExited immediately.
        '''   no activity for the idle window → NoProgress.
        ''' Progress comes ONLY from the host signal (log tail advancing / process
        ''' start) — deliberately NOT from probe responses, because several probes
        ''' (e.g. CheckHealthAsync wrappers) return False without contacting the
        ''' server, which would fake permanent progress.
        ''' Each probe call is capped at <paramref name="perProbeCapMs"/> via a linked
        ''' token so a hung request reads as a failed poll, not a stuck wait.
        ''' </summary>
        ''' <param name="processAlive">Snapshot: is the sidecar process running.</param>
        ''' <param name="msSinceActivity">Snapshot: ms since the sidecar last showed
        ''' life (e.g. PythonSidecarHost.MillisecondsSinceLastActivity).</param>
        Public Shared Async Function WaitAsync(label As String,
                                               probe As Func(Of CancellationToken, Task(Of Boolean)),
                                               processAlive As Func(Of Boolean),
                                               msSinceActivity As Func(Of Long),
                                               ct As CancellationToken,
                                               Optional idleTimeoutSeconds As Integer = 0,
                                               Optional pollIntervalMs As Integer = 500,
                                               Optional perProbeCapMs As Integer = 3000) As Task(Of ReadinessResult)
            Dim idleSeconds = If(idleTimeoutSeconds > 0, idleTimeoutSeconds, DefaultIdleTimeoutSeconds)
            Dim idleMs As Long = CLng(idleSeconds) * 1000L
            Dim startTick = Environment.TickCount64
            Dim lastProbeError As String = ""

            While Not ct.IsCancellationRequested
                If Not processAlive() Then
                    Return Finish(label, ReadinessOutcome.ProcessExited, startTick, lastProbeError, idleSeconds)
                End If

                Try
                    Using probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                        probeCts.CancelAfter(perProbeCapMs)
                        If Await probe(probeCts.Token).ConfigureAwait(False) Then
                            Return Finish(label, ReadinessOutcome.Ready, startTick, "", idleSeconds)
                        End If
                    End Using
                Catch ex As Exception When ct.IsCancellationRequested
                    ' Caller cancelled mid-probe — logged as Cancelled by Finish.
                    Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, idleSeconds)
                Catch ex As Exception
                    ' Probe failure = not ready yet; the message is carried into the
                    ' final outcome log (Finish) rather than spamming per poll.
                    lastProbeError = If(ex.InnerException?.Message, ex.Message)
                End Try

                ' Idle check: host activity only (see summary for why not the probe).
                If msSinceActivity() > idleMs Then
                    Return Finish(label, ReadinessOutcome.NoProgress, startTick, lastProbeError, idleSeconds)
                End If

                Try
                    Await Task.Delay(pollIntervalMs, ct).ConfigureAwait(False)
                Catch ex As OperationCanceledException
                    ' Cancellation during the poll pause — logged as Cancelled by Finish.
                    Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, idleSeconds)
                End Try
            End While
            Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, idleSeconds)
        End Function

        Private Shared Function Finish(label As String, outcome As ReadinessOutcome,
                                       startTick As Long, lastProbeError As String,
                                       idleSeconds As Integer) As ReadinessResult
            Dim elapsed = Environment.TickCount64 - startTick
            Select Case outcome
                Case ReadinessOutcome.Ready
                    AppLogger.Log(LogEvents.STT_CAPTURE_LIFECYCLE,
                        $"{label}: ready after {elapsed}ms")
                Case ReadinessOutcome.NoProgress
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR,
                        $"{label}: NO PROGRESS for {idleSeconds}s (waited {elapsed}ms total) — giving up; last probe error: {If(String.IsNullOrEmpty(lastProbeError), "none", lastProbeError)}")
                Case ReadinessOutcome.ProcessExited
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR,
                        $"{label}: process exited after {elapsed}ms while waiting for ready — giving up immediately")
                Case ReadinessOutcome.Cancelled
                    AppLogger.Log(LogEvents.STT_CAPTURE_LIFECYCLE,
                        $"{label}: readiness wait cancelled after {elapsed}ms")
            End Select
            Return New ReadinessResult With {
                .Outcome = outcome, .ElapsedMs = elapsed, .LastProbeError = lastProbeError}
        End Function

    End Class

End Namespace
