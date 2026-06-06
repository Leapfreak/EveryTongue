Imports System.Diagnostics
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Testing

    ''' <summary>
    ''' Tests concurrent translation throughput by firing N parallel translate requests
    ''' at the active translation backend and measuring latency/throughput at each concurrency level.
    ''' </summary>
    Public Class TranslationConcurrencyRunner

        Public Event ProgressChanged As EventHandler(Of String)

        Private _cts As CancellationTokenSource

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        ''' <summary>
        ''' Run the concurrent throughput test against the translation service.
        ''' </summary>
        Public Async Function RunAsync(
            translationService As ITranslationService,
            corpus As List(Of CorpusEntry),
            targetLangs As List(Of String),
            concurrencyLevels As Integer(),
            iterationsPerLevel As Integer,
            ct As CancellationToken
        ) As Task(Of ConcurrencyTestResult)

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            Dim token = _cts.Token
            Dim result As New ConcurrencyTestResult()

            If corpus.Count = 0 OrElse targetLangs.Count = 0 Then
                result.ErrorMessage = "No corpus entries or target languages selected"
                Return result
            End If

            ' Detect active backend
            result.BackendName = translationService.ActiveBackend
            AppLogger.Log($"[TRANS-CONCURRENCY] ═══════════════════════════════════════════════")
            AppLogger.Log($"[TRANS-CONCURRENCY] Backend: {result.BackendName}")
            AppLogger.Log($"[TRANS-CONCURRENCY] Corpus: {corpus.Count} sentences, Targets: {String.Join(", ", targetLangs)}")
            AppLogger.Log($"[TRANS-CONCURRENCY] Levels: {String.Join(", ", concurrencyLevels)}, iterations/level: {iterationsPerLevel}")

            ' Wait for backend to be available
            Dim loadSw = Stopwatch.StartNew()
            RaiseProgress("Waiting for translation backend...")
            Dim backends = translationService.GetAllBackends()
            If Not backends.Any(Function(b) b.IsAvailable) Then
                Dim deadline = DateTime.UtcNow.AddSeconds(60)
                While DateTime.UtcNow < deadline AndAlso Not token.IsCancellationRequested
                    Await Task.Delay(1000, token)
                    backends = translationService.GetAllBackends()
                    If backends.Any(Function(b) b.IsAvailable) Then Exit While
                End While
                If Not backends.Any(Function(b) b.IsAvailable) Then
                    result.ErrorMessage = "Translation backend not available after 60s"
                    Return result
                End If
            End If
            loadSw.Stop()
            result.ModelLoadMs = loadSw.ElapsedMilliseconds
            AppLogger.Log($"[TRANS-CONCURRENCY] Backend ready in {result.ModelLoadMs}ms")

            ' Start resource monitoring
            Dim monitor As New ResourceMonitor(500)
            monitor.Start()

            Try
                ' Warm up with 2 translations
                RaiseProgress("Warming up (2 translations)...")
                For i = 1 To 2
                    token.ThrowIfCancellationRequested()
                    Dim entry = corpus(i Mod corpus.Count)
                    Await translationService.TranslateAsync(
                        entry.Source, entry.SourceLang, targetLangs, token,
                        noCache:=True)
                Next
                AppLogger.Log($"[TRANS-CONCURRENCY] Warm-up complete")

                ' Test each concurrency level
                For Each level In concurrencyLevels
                    token.ThrowIfCancellationRequested()
                    RaiseProgress($"Testing {level} concurrent translations ({iterationsPerLevel} iterations)...")
                    AppLogger.Log($"[TRANS-CONCURRENCY] ── Level {level} ──")

                    Dim levelResult = Await TestConcurrencyLevel(
                        translationService, corpus, targetLangs, level, iterationsPerLevel, token)
                    result.Levels.Add(levelResult)

                    AppLogger.Log($"[TRANS-CONCURRENCY]   Wall: {levelResult.WallTimeMs}ms, Avg: {levelResult.AvgLatencyMs}ms, " &
                                  $"Max: {levelResult.MaxLatencyMs}ms, Throughput: {levelResult.InferencesPerSec:F1}/s, " &
                                  $"Errors: {levelResult.Errors}")
                Next

                ' Stop resource monitoring
                result.Resources = monitor.Stop()
                AppLogger.Log($"[TRANS-CONCURRENCY] Resources: {result.Resources.ToSummaryText()}")

                RaiseProgress("Concurrent translation test complete.")

            Catch ex As OperationCanceledException When token.IsCancellationRequested
                Throw
            Catch ex As Exception
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        Private Async Function TestConcurrencyLevel(
            translationService As ITranslationService,
            corpus As List(Of CorpusEntry),
            targetLangs As List(Of String),
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
            Dim entryIndex As Integer = 0

            Dim wallSw = Stopwatch.StartNew()

            ' Run `iterations` rounds, each round fires `concurrency` parallel requests
            For round = 1 To iterations
                ct.ThrowIfCancellationRequested()

                Dim tasks As New List(Of Task)()
                For i = 1 To concurrency
                    Dim entry = corpus(Interlocked.Increment(entryIndex) Mod corpus.Count)
                    tasks.Add(Task.Run(Async Function()
                        Dim sw = Stopwatch.StartNew()
                        Try
                            Await translationService.TranslateAsync(
                                entry.Source, entry.SourceLang, targetLangs, ct,
                                noCache:=True)
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                        Catch ex As OperationCanceledException When ct.IsCancellationRequested
                            Throw
                        Catch ex As Exception
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                            Interlocked.Increment(errorCount)
                            Services.Infrastructure.AppLogger.Log($"[TRANSLATION-CONCURRENCY] Translation error: {ex.Message}")
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
            AppLogger.Log($"[TRANS-CONCURRENCY] {msg}")
            RaiseEvent ProgressChanged(Me, msg)
        End Sub

    End Class

End Namespace
