Imports System.Diagnostics
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Testing

    ''' <summary>
    ''' Tests concurrent TTS throughput by firing N parallel synthesis requests
    ''' at a specific TTS backend and measuring latency/throughput at each concurrency level.
    ''' </summary>
    Public Class TtsConcurrencyRunner

        Public Event ProgressChanged As EventHandler(Of String)

        Private _cts As CancellationTokenSource

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        ''' <summary>
        ''' Run the concurrent throughput test against a TTS backend.
        ''' </summary>
        Public Async Function RunAsync(
            backend As ITtsBackend,
            testText As String,
            language As String,
            concurrencyLevels As Integer(),
            iterationsPerLevel As Integer,
            ct As CancellationToken
        ) As Task(Of ConcurrencyTestResult)

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            Dim token = _cts.Token
            Dim result As New ConcurrencyTestResult()

            If String.IsNullOrWhiteSpace(testText) Then
                result.ErrorMessage = "No test text provided"
                Return result
            End If

            result.BackendName = backend.Name
            AppLogger.Log(LogEvents.BENCH_START, $"═══════════════════════════════════════════════")
            AppLogger.Log(LogEvents.BENCH_START, $"Backend: {backend.Name}")
            AppLogger.Log(LogEvents.BENCH_START, $"Text: {testText.Substring(0, Math.Min(80, testText.Length))}...")
            AppLogger.Log(LogEvents.BENCH_START, $"Language: {language}")
            AppLogger.Log(LogEvents.BENCH_START, $"Levels: {String.Join(", ", concurrencyLevels)}, iterations/level: {iterationsPerLevel}")

            ' Health check
            RaiseProgress($"Checking {backend.Name} health...")
            Dim healthy = Await backend.CheckHealthAsync(token)
            If Not healthy Then
                result.ErrorMessage = $"{backend.Name} not available (health check failed)"
                Return result
            End If

            ' Language support check
            Dim langSupported = Await backend.IsLanguageSupportedAsync(language, token)
            If Not langSupported Then
                result.ErrorMessage = $"{backend.Name} does not support language '{language}'"
                Return result
            End If

            ' Start resource monitoring
            Dim monitor As New ResourceMonitor(500)
            monitor.Start()

            Try
                ' Warm up with 2 syntheses
                RaiseProgress($"Warming up {backend.Name} (2 syntheses)...")
                For i = 1 To 2
                    token.ThrowIfCancellationRequested()
                    Await backend.SynthesiseAsync(testText, language, token)
                Next
                AppLogger.Log(LogEvents.BENCH_PROGRESS, $"Warm-up complete")

                ' Test each concurrency level
                For Each level In concurrencyLevels
                    token.ThrowIfCancellationRequested()
                    RaiseProgress($"Testing {level} concurrent syntheses ({iterationsPerLevel} iterations)...")
                    AppLogger.Log(LogEvents.BENCH_PROGRESS, $"── Level {level} ──")

                    Dim levelResult = Await TestConcurrencyLevel(
                        backend, testText, language, level, iterationsPerLevel, token)
                    result.Levels.Add(levelResult)

                    AppLogger.Log(LogEvents.BENCH_RESULT, $"  Wall: {levelResult.WallTimeMs}ms, Avg: {levelResult.AvgLatencyMs}ms, " &
                                  $"Max: {levelResult.MaxLatencyMs}ms, Throughput: {levelResult.InferencesPerSec:F1}/s, " &
                                  $"Errors: {levelResult.Errors}")
                Next

                result.Resources = monitor.Stop()
                AppLogger.Log(LogEvents.BENCH_COMPLETE, $"Resources: {result.Resources.ToSummaryText()}")
                RaiseProgress("Concurrent TTS test complete.")

            Catch ex As OperationCanceledException When token.IsCancellationRequested
                Throw
            Catch ex As Exception
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        Private Async Function TestConcurrencyLevel(
            backend As ITtsBackend,
            testText As String,
            language As String,
            concurrency As Integer,
            iterations As Integer,
            ct As CancellationToken
        ) As Task(Of ConcurrencyLevelResult)

            Dim levelResult As New ConcurrencyLevelResult() With {
                .Concurrency = concurrency
            }
            Dim allLatencies As New Collections.Concurrent.ConcurrentBag(Of Long)()
            Dim errorCount As Integer = 0
            Dim totalRequests = concurrency * iterations

            Dim wallSw = Stopwatch.StartNew()

            ' Run `iterations` rounds, each round fires `concurrency` parallel requests
            For round = 1 To iterations
                ct.ThrowIfCancellationRequested()

                Dim tasks As New List(Of Task)()
                For i = 1 To concurrency
                    tasks.Add(Task.Run(Async Function()
                        Dim sw = Stopwatch.StartNew()
                        Try
                            Dim ttsResult = Await backend.SynthesiseAsync(testText, language, ct)
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                            If ttsResult Is Nothing OrElse ttsResult.AudioData Is Nothing Then
                                Interlocked.Increment(errorCount)
                            End If
                        Catch ex As OperationCanceledException When ct.IsCancellationRequested
                            Throw
                        Catch ex As Exception
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                            Interlocked.Increment(errorCount)
                            Services.Infrastructure.AppLogger.Log(LogEvents.BENCH_ERROR, $"Synthesis error: {ex.Message}")
                        End Try
                    End Function))
                Next

                Await Task.WhenAll(tasks)
            Next

            wallSw.Stop()

            Dim latencyList = allLatencies.ToList()
            latencyList.Sort()

            levelResult.WallTimeMs = wallSw.ElapsedMilliseconds
            levelResult.TotalRequests = totalRequests
            levelResult.Errors = errorCount

            If latencyList.Count > 0 Then
                levelResult.AvgLatencyMs = CLng(latencyList.Average())
                levelResult.MinLatencyMs = latencyList.First()
                levelResult.MaxLatencyMs = latencyList.Last()
                levelResult.P50LatencyMs = Percentile(latencyList, 50)
                levelResult.P95LatencyMs = Percentile(latencyList, 95)
                levelResult.InferencesPerSec = Math.Round(latencyList.Count / (wallSw.ElapsedMilliseconds / 1000.0), 1)
            End If

            Return levelResult
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

End Namespace
