Imports System.Diagnostics
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Testing

    ''' <summary>
    ''' Compares TTS backends (Piper, MMS-TTS, Edge TTS) by synthesising the same
    ''' text on each backend and measuring latency, audio size, and health status.
    ''' </summary>
    Public Class TtsBenchmarkRunner

        Public Event ProgressChanged As EventHandler(Of String)

        Private _cts As CancellationTokenSource

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        Public Async Function RunComparisonAsync(
            backends As IEnumerable(Of ITtsBackend),
            testText As String,
            language As String,
            iterations As Integer,
            ct As CancellationToken
        ) As Task(Of TtsBenchmarkResult)

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            Dim token = _cts.Token
            Dim result As New TtsBenchmarkResult()

            If String.IsNullOrWhiteSpace(testText) Then
                result.ErrorMessage = "No test text provided"
                Return result
            End If

            AppLogger.Log(LogEvents.BENCH_START, $"═══════════════════════════════════════════════")
            AppLogger.Log(LogEvents.BENCH_START, $"Text: {testText.Substring(0, Math.Min(80, testText.Length))}...")
            AppLogger.Log(LogEvents.BENCH_START, $"Language: {language}, Iterations: {iterations}")

            ' Start resource monitoring
            Dim monitor As New ResourceMonitor(500)
            monitor.Start()

            Try
                For Each backend In backends
                    token.ThrowIfCancellationRequested()
                    Dim backendResult As New TtsBackendResult() With {
                        .BackendName = backend.Name,
                        .RequiresInternet = backend.RequiresInternet
                    }

                    RaiseProgress($"Testing {backend.Name}...")
                    AppLogger.Log(LogEvents.BENCH_PROGRESS, $"── {backend.Name} ──")

                    ' Health check
                    Dim healthy = Await backend.CheckHealthAsync(token)
                    If Not healthy Then
                        backendResult.Skipped = True
                        backendResult.SkipReason = "Not available (health check failed)"
                        AppLogger.Log(LogEvents.BENCH_PROGRESS, $"  Skipped: health check failed")
                        result.Backends.Add(backendResult)
                        Continue For
                    End If

                    ' Language support check
                    Dim langSupported = Await backend.IsLanguageSupportedAsync(language, token)
                    If Not langSupported Then
                        backendResult.Skipped = True
                        backendResult.SkipReason = $"Language '{language}' not supported"
                        AppLogger.Log(LogEvents.BENCH_PROGRESS, $"  Skipped: language not supported")
                        result.Backends.Add(backendResult)
                        Continue For
                    End If

                    ' Warm up with 1 synthesis
                    RaiseProgress($"Warming up {backend.Name}...")
                    Try
                        Await backend.SynthesiseAsync(testText, language, token)
                    Catch ex As OperationCanceledException When token.IsCancellationRequested
                        Throw
                    Catch ex As Exception
                        backendResult.Failed = True
                        backendResult.ErrorMessage = $"Warm-up failed: {ex.Message}"
                        AppLogger.Log(LogEvents.BENCH_ERROR, $"  Failed warm-up: {ex.Message}")
                        result.Backends.Add(backendResult)
                        Continue For
                    End Try

                    ' Run iterations
                    RaiseProgress($"Running {backend.Name} ({iterations} iterations)...")
                    Dim latencies As New List(Of Long)()
                    Dim totalAudioBytes As Long = 0
                    Dim errors As Integer = 0

                    For i = 1 To iterations
                        token.ThrowIfCancellationRequested()
                        Dim sw = Stopwatch.StartNew()
                        Try
                            Dim ttsResult = Await backend.SynthesiseAsync(testText, language, token)
                            sw.Stop()
                            latencies.Add(sw.ElapsedMilliseconds)
                            If ttsResult IsNot Nothing AndAlso ttsResult.AudioData IsNot Nothing Then
                                totalAudioBytes += ttsResult.AudioData.Length
                                If backendResult.Codec Is Nothing Then
                                    backendResult.Codec = ttsResult.Codec
                                    backendResult.SampleRate = ttsResult.SampleRate
                                End If
                            Else
                                errors += 1
                            End If
                        Catch ex As OperationCanceledException When token.IsCancellationRequested
                            Throw
                        Catch ex As Exception
                            sw.Stop()
                            latencies.Add(sw.ElapsedMilliseconds)
                            errors += 1
                            Services.Infrastructure.AppLogger.Log(LogEvents.BENCH_ERROR, $"Synthesis error: {ex.Message}")
                        End Try
                    Next

                    latencies.Sort()
                    backendResult.Iterations = iterations
                    backendResult.Errors = errors
                    backendResult.AvgAudioBytes = If(latencies.Count - errors > 0,
                        CLng(totalAudioBytes / (latencies.Count - errors)), 0)

                    If latencies.Count > 0 Then
                        backendResult.AvgLatencyMs = CLng(latencies.Average())
                        backendResult.MinLatencyMs = latencies.First()
                        backendResult.MaxLatencyMs = latencies.Last()
                        backendResult.P50LatencyMs = Percentile(latencies, 50)
                        backendResult.P95LatencyMs = Percentile(latencies, 95)
                    End If

                    AppLogger.Log(LogEvents.BENCH_RESULT, $"  Avg: {backendResult.AvgLatencyMs}ms, " &
                                  $"Min: {backendResult.MinLatencyMs}ms, Max: {backendResult.MaxLatencyMs}ms, " &
                                  $"Audio: {backendResult.AvgAudioBytes \ 1024}KB, Errors: {errors}")

                    result.Backends.Add(backendResult)
                Next

                ' Calculate speedup relative to fastest
                Dim completed = result.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed AndAlso b.AvgLatencyMs > 0).ToList()
                If completed.Count > 0 Then
                    Dim fastestAvg = completed.Min(Function(b) b.AvgLatencyMs)
                    For Each b In completed
                        b.SpeedupVsFastest = Math.Round(fastestAvg / b.AvgLatencyMs, 2)
                    Next
                End If

                result.Resources = monitor.Stop()
                AppLogger.Log(LogEvents.BENCH_COMPLETE, $"Resources: {result.Resources.ToSummaryText()}")
                RaiseProgress("TTS comparison complete.")

            Catch ex As OperationCanceledException When token.IsCancellationRequested
                Throw
            Catch ex As Exception
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        Private Shared Function Percentile(sorted As List(Of Long), p As Integer) As Long
            If sorted.Count = 0 Then Return 0
            Dim idx = CInt(Math.Ceiling(p / 100.0 * sorted.Count)) - 1
            Return sorted(Math.Max(0, Math.Min(idx, sorted.Count - 1)))
        End Function

        Private Sub RaiseProgress(msg As String)
            AppLogger.Log(LogEvents.BENCH_PROGRESS, msg)
            RaiseEvent ProgressChanged(Me, msg)
        End Sub

    End Class

    ' ── Result models ──

    Public Class TtsBenchmarkResult
        Public Property ErrorMessage As String = ""
        Public Property Backends As New List(Of TtsBackendResult)()
        Public Property Resources As ResourceReport
    End Class

    Public Class TtsBackendResult
        Public Property BackendName As String = ""
        Public Property RequiresInternet As Boolean
        Public Property Skipped As Boolean
        Public Property SkipReason As String = ""
        Public Property Failed As Boolean
        Public Property ErrorMessage As String = ""
        Public Property Iterations As Integer
        Public Property Errors As Integer
        Public Property AvgLatencyMs As Long
        Public Property MinLatencyMs As Long
        Public Property MaxLatencyMs As Long
        Public Property P50LatencyMs As Long
        Public Property P95LatencyMs As Long
        Public Property SpeedupVsFastest As Double
        Public Property AvgAudioBytes As Long
        Public Property Codec As String
        Public Property SampleRate As Integer
    End Class

End Namespace
