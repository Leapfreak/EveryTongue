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
' WaitForStateAsync (2026-10-05) goes further for engines that REPORT their state
' (starting / ready / failed - room STT): no idle window at all and no fail-open;
' it ends only on the engine's own answer or process death. WaitAsync's idle
' window remains for sidecars that can only answer yes/no.

Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline

    Public Enum ReadinessOutcome
        Ready
        NoProgress      ' idle window elapsed with no sign of life
        ProcessExited   ' the process died — fail fast, no waiting
        Cancelled
        Failed          ' the engine itself reported failure (reason in LastProbeError)
    End Enum

    Public Structure ReadinessResult
        Public Outcome As ReadinessOutcome
        Public ElapsedMs As Long
        Public LastProbeError As String
    End Structure

    ''' <summary>What an engine says about itself, as opposed to a yes/no "capturing?".
    ''' "Not yet" and "failed" are different answers: only the engine can tell them
    ''' apart, so a waiter that gets the engine's state needs no idle timer to guess.</summary>
    Public Enum EngineState
        Starting
        Ready
        Failed
    End Enum

    Public Structure EngineStateResult
        Public State As EngineState
        ''' <summary>Failed: the engine's own reason (e.g. "thread dead (authentication failed)").</summary>
        Public Detail As String

        Public Shared Function Starting() As EngineStateResult
            Return New EngineStateResult With {.State = EngineState.Starting, .Detail = ""}
        End Function

        Public Shared Function Ready() As EngineStateResult
            Return New EngineStateResult With {.State = EngineState.Ready, .Detail = ""}
        End Function

        Public Shared Function Failed(reason As String) As EngineStateResult
            Return New EngineStateResult With {.State = EngineState.Failed, .Detail = If(reason, "")}
        End Function
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
        '''   no activity for the idle window → NoProgress (the window starts no earlier
        '''                                     than this call: quiet before it doesn't count).
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

                ' Idle check: host activity only (see summary for why not the probe),
                ' counted from no earlier than the START of this wait - a process that
                ' was quiet before we began (an idle parked spare) has not failed to
                ' progress yet (field 2026-09-27: "NO PROGRESS for 15s (waited 0ms total)").
                Dim idleSoFar = Math.Min(msSinceActivity(), Environment.TickCount64 - startTick)
                If idleSoFar > idleMs Then
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

        ''' <summary>
        ''' Wait until an engine that REPORTS ITS STATE is ready - no idle timer, no
        ''' fail-open. Ends only on a definite answer:
        '''   probe Ready            -> Ready.
        '''   probe Failed           -> Failed, with the engine's own reason.
        '''   process dead           -> ProcessExited, immediately.
        '''   caller cancels         -> Cancelled.
        '''   probe Starting / throws -> keep waiting (a probe error is "no answer yet").
        ''' A hang therefore stays visibly "preparing" (the host can Reset) instead of
        ''' being declared ready on a guess. Replaces WaitAsync's idle window for engines
        ''' with a state probe (field 2026-09-27: a parked live-server that was merely
        ''' idle for 24s read as "NO PROGRESS for 15s (waited 0ms total)").
        ''' </summary>
        Public Shared Async Function WaitForStateAsync(label As String,
                                                       probe As Func(Of CancellationToken, Task(Of EngineStateResult)),
                                                       processAlive As Func(Of Boolean),
                                                       ct As CancellationToken,
                                                       Optional pollIntervalMs As Integer = 500,
                                                       Optional perProbeCapMs As Integer = 3000) As Task(Of ReadinessResult)
            Dim startTick = Environment.TickCount64
            Dim lastProbeError As String = ""

            While Not ct.IsCancellationRequested
                If processAlive IsNot Nothing AndAlso Not processAlive() Then
                    Return Finish(label, ReadinessOutcome.ProcessExited, startTick, lastProbeError, 0)
                End If

                Try
                    Using probeCts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                        probeCts.CancelAfter(perProbeCapMs)
                        Dim answer = Await probe(probeCts.Token).ConfigureAwait(False)
                        If answer.State = EngineState.Ready Then
                            Return Finish(label, ReadinessOutcome.Ready, startTick, "", 0)
                        End If
                        If answer.State = EngineState.Failed Then
                            Return Finish(label, ReadinessOutcome.Failed, startTick, answer.Detail, 0)
                        End If
                    End Using
                Catch ex As Exception When ct.IsCancellationRequested
                    ' Caller cancelled mid-probe — logged as Cancelled by Finish.
                    Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, 0)
                Catch ex As Exception
                    ' No answer yet (server still booting / one request capped) - keep
                    ' waiting; the message is carried into the final outcome log.
                    lastProbeError = If(ex.InnerException?.Message, ex.Message)
                End Try

                Try
                    Await Task.Delay(pollIntervalMs, ct).ConfigureAwait(False)
                Catch ex As OperationCanceledException
                    ' Cancellation during the poll pause — logged as Cancelled by Finish.
                    Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, 0)
                End Try
            End While
            Return Finish(label, ReadinessOutcome.Cancelled, startTick, lastProbeError, 0)
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
                Case ReadinessOutcome.Failed
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR,
                        $"{label}: engine reported FAILURE after {elapsed}ms - {If(String.IsNullOrEmpty(lastProbeError), "no reason given", lastProbeError)}")
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
