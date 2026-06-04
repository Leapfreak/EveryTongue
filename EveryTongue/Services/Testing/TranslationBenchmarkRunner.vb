Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Scheduling

Namespace Services.Testing

    ' ── Pipeline stage enum ──

    Public Enum BenchmarkStage
        Translation
        Tts
        Stt
    End Enum

    ''' <summary>
    ''' Runs load tests against the three pipeline stages (STT, Translation, TTS)
    ''' through their abstract interfaces — engine-agnostic.
    ''' After each run, generates a LatencyProfile that the system uses as a baseline.
    ''' </summary>
    Public Class TranslationBenchmarkRunner

        Public Event ProgressChanged As EventHandler(Of BenchmarkProgress)
        Public Event Completed As EventHandler(Of BenchmarkResult)

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(60)
        }

        Private _cts As CancellationTokenSource

        ''' <summary>Raw TTS results from the last TTS benchmark run, used for STT round-trip.</summary>
        Public Property LastTtsResults As List(Of TtsRequestResult)

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested
            End Get
        End Property

        ' ── Corpus loading ──

        Public Shared Function LoadCorpus() As List(Of CorpusEntry)
            Dim path = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", "translation-corpus.json")
            If Not File.Exists(path) Then Return New List(Of CorpusEntry)()

            Dim json = File.ReadAllText(path, Encoding.UTF8)
            Dim entries As New List(Of CorpusEntry)()

            Using doc = JsonDocument.Parse(json)
                For Each el In doc.RootElement.EnumerateArray()
                    Dim entry As New CorpusEntry() With {
                        .Id = el.GetProperty("id").GetInt32(),
                        .Domain = el.GetProperty("domain").GetString(),
                        .Source = el.GetProperty("source").GetString(),
                        .SourceLang = el.GetProperty("sourceLang").GetString(),
                        .Targets = New List(Of CorpusTarget)()
                    }
                    For Each t In el.GetProperty("targets").EnumerateArray()
                        entry.Targets.Add(New CorpusTarget() With {
                            .Lang = t.GetProperty("lang").GetString(),
                            .Reference = t.GetProperty("reference").GetString()
                        })
                    Next
                    entries.Add(entry)
                Next
            End Using

            Return entries
        End Function

        Public Shared Function GetDomains(corpus As List(Of CorpusEntry)) As List(Of String)
            Return corpus.Select(Function(e) e.Domain).Distinct().OrderBy(Function(d) d).ToList()
        End Function

        Public Shared Function GetSourceLanguages(corpus As List(Of CorpusEntry)) As List(Of String)
            Return corpus.Select(Function(e) e.SourceLang).Distinct().OrderBy(Function(l) l).ToList()
        End Function

        Public Shared Function GetTargetLanguages(corpus As List(Of CorpusEntry)) As List(Of String)
            Return corpus.SelectMany(Function(e) e.Targets).Select(Function(t) t.Lang).Distinct().OrderBy(Function(l) l).ToList()
        End Function

        ' ══════════════════════════════════════════════
        '  Translation benchmark
        ' ══════════════════════════════════════════════

        Public Async Function RunTranslationBenchmarkAsync(
            translationService As ITranslationService,
            corpus As List(Of CorpusEntry),
            targetLangs As List(Of String),
            concurrency As Integer,
            iterations As Integer
        ) As Task(Of StageBenchmarkResult)

            _cts = New CancellationTokenSource()
            Dim ct = _cts.Token

            ' Wait for translation backend to be available (model may still be loading)
            Dim backends = translationService.GetAllBackends()
            If Not backends.Any(Function(b) b.IsAvailable) Then
                Services.Infrastructure.AppLogger.Log("[BENCHMARK] Waiting for translation backend to become available...")
                Dim deadline = DateTime.UtcNow.AddSeconds(60)
                While DateTime.UtcNow < deadline AndAlso Not ct.IsCancellationRequested
                    Await Task.Delay(1000, ct)
                    backends = translationService.GetAllBackends()
                    If backends.Any(Function(b) b.IsAvailable) Then Exit While
                End While
                If Not backends.Any(Function(b) b.IsAvailable) Then
                    Services.Infrastructure.AppLogger.Log("[BENCHMARK] Translation backend not available after 60s, aborting")
                    Return New StageBenchmarkResult()
                End If
                Services.Infrastructure.AppLogger.Log("[BENCHMARK] Translation backend ready")
            End If

            Dim workItems As New List(Of WorkItem)()
            For iter = 1 To iterations
                For Each entry In corpus
                    Dim targets = entry.Targets.Where(Function(t) targetLangs.Contains(t.Lang)).ToList()
                    If targets.Count > 0 Then
                        workItems.Add(New WorkItem() With {
                            .Entry = entry, .Targets = targets, .Iteration = iter})
                    End If
                Next
            Next

            Dim totalItems = workItems.Count
            Dim completedCount As Integer = 0
            Dim results As New Collections.Concurrent.ConcurrentBag(Of RequestResult)()
            Dim overallSw = Stopwatch.StartNew()

            Using sem As New SemaphoreSlim(concurrency, concurrency)
                Dim tasks As New List(Of Task)()

                For Each wi In workItems
                    If ct.IsCancellationRequested Then Exit For
                    Await sem.WaitAsync(ct)

                    Dim capturedWi = wi
                    Dim svc = translationService
                    tasks.Add(Task.Run(Async Function()
                                           Try
                                               Dim result = Await ExecuteTranslation(svc, capturedWi, ct)
                                               results.Add(result)
                                           Catch ex As OperationCanceledException
                                           Catch ex As Exception
                                               results.Add(New RequestResult() With {
                                                   .EntryId = capturedWi.Entry.Id,
                                                   .SourceLang = capturedWi.Entry.SourceLang,
                                                   .ErrorMessage = ex.Message,
                                                   .PairResults = New List(Of PairResult)()})
                                           Finally
                                               Dim done = Interlocked.Increment(completedCount)
                                               Dim qm = svc.TranslationQueueMetrics
                                               RaiseEvent ProgressChanged(Me, New BenchmarkProgress() With {
                                                   .Stage = BenchmarkStage.Translation,
                                                   .Completed = done, .Total = totalItems,
                                                   .ElapsedMs = overallSw.ElapsedMilliseconds,
                                                   .QueueDepth = qm.CurrentDepth,
                                                   .QueueActive = qm.CurrentActive,
                                                   .QueueAvgWaitMs = qm.AvgWaitMs})
                                               Try
                                                   sem.Release()
                                               Catch ex As ObjectDisposedException
                                               End Try
                                           End Try
                                       End Function))
                Next

                Await Task.WhenAll(tasks)
            End Using

            overallSw.Stop()
            Return BuildTranslationResult(results.ToList(), overallSw.ElapsedMilliseconds,
                                           concurrency, iterations,
                                           translationService.TranslationQueueMetrics)
        End Function

        Private Async Function ExecuteTranslation(svc As ITranslationService,
                                                   wi As WorkItem,
                                                   ct As CancellationToken) As Task(Of RequestResult)
            Dim result As New RequestResult() With {
                .EntryId = wi.Entry.Id,
                .SourceLang = wi.Entry.SourceLang,
                .PairResults = New List(Of PairResult)()}

            Dim targetLangs = wi.Targets.Select(Function(t) t.Lang).ToList()
            Dim sw = Stopwatch.StartNew()
            Try
                Dim translations = Await svc.TranslateAsync(
                    wi.Entry.Source, wi.Entry.SourceLang,
                    targetLangs, ct, TranslationPriority.Benchmark,
                    noCache:=True)
                sw.Stop()
                result.LatencyMs = sw.ElapsedMilliseconds

                For Each target In wi.Targets
                    Dim pr As New PairResult() With {
                        .SourceLang = wi.Entry.SourceLang,
                        .TargetLang = target.Lang,
                        .Reference = target.Reference}
                    If translations IsNot Nothing AndAlso translations.ContainsKey(target.Lang) Then
                        pr.Output = translations(target.Lang)
                        pr.QualityScore = ComputeSimilarity(pr.Reference, pr.Output)
                    End If
                    result.PairResults.Add(pr)
                Next
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                sw.Stop()
                result.LatencyMs = sw.ElapsedMilliseconds
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        ' ══════════════════════════════════════════════
        '  TTS benchmark
        ' ══════════════════════════════════════════════

        ''' <summary>
        ''' Benchmark TTS through ITtsService — engine-agnostic.
        ''' Uses corpus reference texts in each target language.
        ''' Returns per-language latency and success metrics.
        ''' </summary>
        Public Async Function RunTtsBenchmarkAsync(
            ttsService As ITtsService,
            corpus As List(Of CorpusEntry),
            targetLangs As List(Of String),
            concurrency As Integer,
            iterations As Integer
        ) As Task(Of StageBenchmarkResult)

            _cts = New CancellationTokenSource()
            Dim ct = _cts.Token

            ' Build TTS work items: each corpus entry x each target language x iterations
            ' Use reference text in the target language (so we test TTS in multiple languages)
            Dim ttsItems As New List(Of TtsWorkItem)()
            ' Use timestamp-based offset so each benchmark run gets unique commit IDs (avoids TTS cache hits)
            Dim commitIdBase = -CInt(DateTime.UtcNow.Ticks Mod 900000000L) - 100000
            Dim idCounter As Integer = 0

            For iter = 1 To iterations
                For Each entry In corpus
                    For Each target In entry.Targets
                        If targetLangs.Contains(target.Lang) Then
                            idCounter += 1
                            ttsItems.Add(New TtsWorkItem() With {
                                .Text = target.Reference,
                                .Language = target.Lang,
                                .CommitId = commitIdBase - idCounter,
                                .SourceText = entry.Source,
                                .SourceLang = entry.SourceLang})
                        End If
                    Next
                Next
            Next

            Dim totalItems = ttsItems.Count
            Dim completedCount As Integer = 0
            Dim results As New Collections.Concurrent.ConcurrentBag(Of TtsRequestResult)()
            Dim overallSw = Stopwatch.StartNew()

            Using sem As New SemaphoreSlim(concurrency, concurrency)
                Dim tasks As New List(Of Task)()

                For Each item In ttsItems
                    If ct.IsCancellationRequested Then Exit For
                    Await sem.WaitAsync(ct)

                    Dim captured = item
                    tasks.Add(Task.Run(Async Function()
                                           Try
                                               Dim sw = Stopwatch.StartNew()
                                               Dim url = Await ttsService.SynthesiseAsync(
                                                   captured.Text, captured.Language,
                                                   captured.CommitId, ct,
                                                   TranslationPriority.Benchmark)
                                               sw.Stop()

                                               results.Add(New TtsRequestResult() With {
                                                   .Language = captured.Language,
                                                   .LatencyMs = sw.ElapsedMilliseconds,
                                                   .Success = url IsNot Nothing,
                                                   .AudioUrl = url,
                                                   .SourceText = captured.SourceText,
                                                   .SpokenText = captured.Text})
                                           Catch ex As OperationCanceledException
                                           Catch ex As Exception
                                               results.Add(New TtsRequestResult() With {
                                                   .Language = captured.Language,
                                                   .LatencyMs = 0,
                                                   .Success = False,
                                                   .ErrorMessage = ex.Message})
                                           Finally
                                               Dim done = Interlocked.Increment(completedCount)
                                               Dim qm = ttsService.TtsQueueMetrics
                                               RaiseEvent ProgressChanged(Me, New BenchmarkProgress() With {
                                                   .Stage = BenchmarkStage.Tts,
                                                   .Completed = done, .Total = totalItems,
                                                   .ElapsedMs = overallSw.ElapsedMilliseconds,
                                                   .QueueDepth = qm.CurrentDepth,
                                                   .QueueActive = qm.CurrentActive,
                                                   .QueueAvgWaitMs = qm.AvgWaitMs})
                                               sem.Release()
                                           End Try
                                       End Function))
                Next

                Await Task.WhenAll(tasks)
            End Using

            overallSw.Stop()
            Dim resultsList = results.ToList()
            LastTtsResults = resultsList
            Return BuildTtsResult(resultsList, overallSw.ElapsedMilliseconds,
                                  concurrency, iterations,
                                  ttsService.TtsQueueMetrics)
        End Function

        ' ══════════════════════════════════════════════
        '  STT benchmark (round-trip: TTS audio → transcribe → compare)
        ' ══════════════════════════════════════════════

        ''' <summary>
        ''' Benchmark STT by feeding TTS-generated audio through the transcription endpoint.
        ''' Measures transcription latency and accuracy (comparing STT output to original text).
        ''' Engine-agnostic — tests whatever STT backend the live-server is running.
        ''' </summary>
        Public Async Function RunSttBenchmarkAsync(
            liveServerPort As Integer,
            ttsAudioSamples As List(Of SttSample),
            concurrency As Integer
        ) As Task(Of StageBenchmarkResult)

            _cts = New CancellationTokenSource()
            Dim ct = _cts.Token

            Dim totalItems = ttsAudioSamples.Count
            Dim completedCount As Integer = 0
            Dim results As New Collections.Concurrent.ConcurrentBag(Of SttRequestResult)()
            Dim overallSw = Stopwatch.StartNew()

            Using sem As New SemaphoreSlim(concurrency, concurrency)
                Dim tasks As New List(Of Task)()

                For Each sample In ttsAudioSamples
                    If ct.IsCancellationRequested Then Exit For
                    Await sem.WaitAsync(ct)

                    Dim captured = sample
                    tasks.Add(Task.Run(Async Function()
                                           Try
                                               Dim result = Await ExecuteStt(liveServerPort, captured, ct)
                                               results.Add(result)
                                           Catch ex As OperationCanceledException
                                           Catch ex As Exception
                                               results.Add(New SttRequestResult() With {
                                                   .Language = captured.Language,
                                                   .ExpectedText = captured.ExpectedText,
                                                   .ErrorMessage = ex.Message})
                                           Finally
                                               Dim done = Interlocked.Increment(completedCount)
                                               RaiseEvent ProgressChanged(Me, New BenchmarkProgress() With {
                                                   .Stage = BenchmarkStage.Stt,
                                                   .Completed = done, .Total = totalItems,
                                                   .ElapsedMs = overallSw.ElapsedMilliseconds})
                                               sem.Release()
                                           End Try
                                       End Function))
                Next

                Await Task.WhenAll(tasks)
            End Using

            overallSw.Stop()
            Return BuildSttResult(results.ToList(), overallSw.ElapsedMilliseconds, concurrency)
        End Function

        ''' <summary>Read a TTS audio file from disk and POST to the transcription endpoint.</summary>
        Private Async Function ExecuteStt(port As Integer, sample As SttSample,
                                           ct As CancellationToken) As Task(Of SttRequestResult)
            Dim result As New SttRequestResult() With {
                .Language = sample.Language,
                .ExpectedText = sample.ExpectedText}

            Dim sw = Stopwatch.StartNew()
            Try
                ' Read audio file from TTS cache
                If Not File.Exists(sample.AudioFilePath) Then
                    result.ErrorMessage = "Audio file not found"
                    Return result
                End If

                Dim audioBytes = File.ReadAllBytes(sample.AudioFilePath)
                Dim content As New ByteArrayContent(audioBytes)
                Dim ext = Path.GetExtension(sample.AudioFilePath).ToLower()
                Dim mimeType = If(ext = ".mp3", "audio/mpeg", If(ext = ".opus", "audio/opus", "audio/wav"))
                content.Headers.ContentType = New Headers.MediaTypeHeaderValue(mimeType)

                ' Use language hint for better accuracy
                Dim whisperLang = Pipeline.TranslationService.FloresToShortCode(sample.Language).ToLowerInvariant()
                Dim url = $"http://127.0.0.1:{port}/transcribe?lang={Uri.EscapeDataString(whisperLang)}"

                Dim response = Await _httpClient.PostAsync(url, content, ct)
                sw.Stop()
                result.LatencyMs = sw.ElapsedMilliseconds

                If response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync()
                    Using doc = JsonDocument.Parse(body)
                        result.TranscribedText = doc.RootElement.GetProperty("text").GetString()
                    End Using
                    result.Accuracy = ComputeSimilarity(sample.ExpectedText, result.TranscribedText)
                Else
                    Dim errBody = Await response.Content.ReadAsStringAsync()
                    Dim detail = $"HTTP {CInt(response.StatusCode)}"
                    Try
                        Using doc = JsonDocument.Parse(errBody)
                            Dim detailProp As JsonElement
                            If doc.RootElement.TryGetProperty("detail", detailProp) Then
                                detail &= $": {detailProp.GetString()}"
                            End If
                        End Using
                    Catch
                    End Try
                    result.ErrorMessage = detail
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                sw.Stop()
                result.LatencyMs = sw.ElapsedMilliseconds
                result.ErrorMessage = ex.Message
            End Try

            Return result
        End Function

        ''' <summary>
        ''' Collect TTS audio files from a TTS benchmark run for use in STT round-trip testing.
        ''' Reads the cached audio files that TTS generated.
        ''' </summary>
        Public Shared Function CollectSttSamples(ttsResults As List(Of TtsRequestResult),
                                                  ttsCacheDir As String) As List(Of SttSample)
            Dim samples As New List(Of SttSample)()
            For Each r In ttsResults
                If Not r.Success OrElse String.IsNullOrEmpty(r.AudioUrl) Then Continue For

                ' URL format: /tts/cache/{filename}
                Dim fileName = r.AudioUrl.Replace("/tts/cache/", "")
                Dim filePath = Path.Combine(ttsCacheDir, fileName)
                If File.Exists(filePath) Then
                    samples.Add(New SttSample() With {
                        .AudioFilePath = filePath,
                        .Language = r.Language,
                        .ExpectedText = If(r.SpokenText, r.SourceText)})
                End If
            Next
            Return samples
        End Function

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        ' ══════════════════════════════════════════════
        '  Quality scoring
        ' ══════════════════════════════════════════════

        Public Shared Function ComputeSimilarity(reference As String, output As String) As Double
            If String.IsNullOrWhiteSpace(reference) OrElse String.IsNullOrWhiteSpace(output) Then Return 0

            Dim refTokens = Tokenize(reference.ToLowerInvariant())
            Dim outTokens = Tokenize(output.ToLowerInvariant())
            If refTokens.Count = 0 OrElse outTokens.Count = 0 Then Return 0

            Dim refSet As New HashSet(Of String)(refTokens)
            Dim outSet As New HashSet(Of String)(outTokens)
            Dim overlap = refSet.Intersect(outSet).Count()
            If overlap = 0 Then Return 0

            Dim precision = overlap / CDbl(outSet.Count)
            Dim recall = overlap / CDbl(refSet.Count)
            Dim f1 = 2 * precision * recall / (precision + recall)
            Return Math.Round(f1 * 100, 1)
        End Function

        Private Shared Function Tokenize(text As String) As List(Of String)
            Return text.Split({" "c, ","c, "."c, "!"c, "?"c, ";"c, ":"c, "'"c, """"c, "("c, ")"c},
                              StringSplitOptions.RemoveEmptyEntries).ToList()
        End Function

        ' ══════════════════════════════════════════════
        '  Result building
        ' ══════════════════════════════════════════════

        Private Function BuildTranslationResult(results As List(Of RequestResult), totalMs As Long,
                                                 concurrency As Integer, iterations As Integer,
                                                 queueMetrics As QueueMetrics) As StageBenchmarkResult
            Dim sr As New StageBenchmarkResult() With {
                .Stage = BenchmarkStage.Translation,
                .TotalMs = totalMs, .Concurrency = concurrency,
                .TotalRequests = results.Count,
                .Errors = results.Where(Function(r) r.ErrorMessage IsNot Nothing).Count(),
                .PairSummaries = New List(Of PairSummary)(),
                .FinalQueueMetrics = queueMetrics}

            Dim latencies = results.Where(Function(r) r.ErrorMessage Is Nothing).
                                    Select(Function(r) r.LatencyMs).OrderBy(Function(l) l).ToList()
            ComputeLatencyStats(sr, latencies, totalMs)

            Dim allPairs = results.Where(Function(r) r.ErrorMessage Is Nothing).
                                   SelectMany(Function(r) r.PairResults).ToList()
            For Each grp In allPairs.GroupBy(Function(p) $"{p.SourceLang}>{p.TargetLang}")
                Dim scores = grp.Select(Function(p) p.QualityScore).ToList()
                Dim parts = grp.Key.Split(">"c)
                sr.PairSummaries.Add(New PairSummary() With {
                    .Stage = BenchmarkStage.Translation,
                    .SourceLang = parts(0), .TargetLang = parts(1),
                    .Count = grp.Count(),
                    .AvgQuality = Math.Round(scores.Average(), 1),
                    .MinQuality = Math.Round(scores.Min(), 1),
                    .MaxQuality = Math.Round(scores.Max(), 1),
                    .AvgLatencyMs = Math.Round(
                        results.Where(Function(r) r.ErrorMessage Is Nothing AndAlso r.SourceLang = parts(0)).
                                Select(Function(r) CDbl(r.LatencyMs)).DefaultIfEmpty(0).Average(), 0)})
            Next

            Return sr
        End Function

        Private Function BuildTtsResult(results As List(Of TtsRequestResult), totalMs As Long,
                                         concurrency As Integer, iterations As Integer,
                                         queueMetrics As QueueMetrics) As StageBenchmarkResult
            Dim sr As New StageBenchmarkResult() With {
                .Stage = BenchmarkStage.Tts,
                .TotalMs = totalMs, .Concurrency = concurrency,
                .TotalRequests = results.Count,
                .Errors = results.Where(Function(r) Not r.Success).Count(),
                .PairSummaries = New List(Of PairSummary)(),
                .FinalQueueMetrics = queueMetrics}

            Dim latencies = results.Where(Function(r) r.Success).
                                    Select(Function(r) r.LatencyMs).OrderBy(Function(l) l).ToList()
            ComputeLatencyStats(sr, latencies, totalMs)

            For Each grp In results.GroupBy(Function(r) r.Language)
                Dim successes = grp.Where(Function(r) r.Success).ToList()
                Dim successRate = If(grp.Count() > 0, Math.Round(successes.Count / CDbl(grp.Count()) * 100, 1), 0)
                sr.PairSummaries.Add(New PairSummary() With {
                    .Stage = BenchmarkStage.Tts,
                    .SourceLang = grp.Key, .TargetLang = "",
                    .Count = grp.Count(),
                    .AvgLatencyMs = If(successes.Count > 0,
                        Math.Round(successes.Select(Function(r) CDbl(r.LatencyMs)).Average(), 0), 0),
                    .AvgQuality = successRate,
                    .MinQuality = successRate,
                    .MaxQuality = successRate})
            Next

            Return sr
        End Function

        Private Function BuildSttResult(results As List(Of SttRequestResult), totalMs As Long,
                                         concurrency As Integer) As StageBenchmarkResult
            ' Log error details for debugging
            For Each errResult In results.Where(Function(r) r.ErrorMessage IsNot Nothing)
                Services.Infrastructure.AppLogger.Log(
                    $"[BENCHMARK] STT error for {errResult.Language}: {errResult.ErrorMessage}")
            Next

            Dim sr As New StageBenchmarkResult() With {
                .Stage = BenchmarkStage.Stt,
                .TotalMs = totalMs, .Concurrency = concurrency,
                .TotalRequests = results.Count,
                .Errors = results.Where(Function(r) r.ErrorMessage IsNot Nothing).Count(),
                .PairSummaries = New List(Of PairSummary)()}

            Dim latencies = results.Where(Function(r) r.ErrorMessage Is Nothing).
                                    Select(Function(r) r.LatencyMs).OrderBy(Function(l) l).ToList()
            ComputeLatencyStats(sr, latencies, totalMs)

            For Each grp In results.Where(Function(r) r.ErrorMessage Is Nothing).GroupBy(Function(r) r.Language)
                Dim accuracies = grp.Select(Function(r) r.Accuracy).ToList()
                sr.PairSummaries.Add(New PairSummary() With {
                    .Stage = BenchmarkStage.Stt,
                    .SourceLang = grp.Key, .TargetLang = "",
                    .Count = grp.Count(),
                    .AvgLatencyMs = Math.Round(grp.Select(Function(r) CDbl(r.LatencyMs)).Average(), 0),
                    .AvgQuality = Math.Round(accuracies.Average(), 1),
                    .MinQuality = Math.Round(accuracies.Min(), 1),
                    .MaxQuality = Math.Round(accuracies.Max(), 1)})
            Next

            Return sr
        End Function

        Private Shared Sub ComputeLatencyStats(sr As StageBenchmarkResult,
                                                latencies As List(Of Long), totalMs As Long)
            If latencies.Count > 0 Then
                sr.AvgLatencyMs = latencies.Average()
                sr.P50LatencyMs = Percentile(latencies, 50)
                sr.P95LatencyMs = Percentile(latencies, 95)
                sr.P99LatencyMs = Percentile(latencies, 99)
                sr.RequestsPerSec = Math.Round(latencies.Count / (totalMs / 1000.0), 2)
            End If
        End Sub

        Private Shared Function Percentile(sortedList As List(Of Long), pct As Integer) As Long
            If sortedList.Count = 0 Then Return 0
            Dim idx = CInt(Math.Ceiling(pct / 100.0 * sortedList.Count)) - 1
            Return sortedList(Math.Max(0, Math.Min(idx, sortedList.Count - 1)))
        End Function

        ' ══════════════════════════════════════════════
        '  Latency profile generation
        ' ══════════════════════════════════════════════

        Public Shared Sub SaveLatencyProfile(result As BenchmarkResult)
            Try
                Dim profile As New LatencyProfile() With {
                    .Timestamp = result.Timestamp,
                    .TotalRequests = 0}

                For Each stage In result.Stages
                    profile.TotalRequests += stage.TotalRequests
                    If stage.Stage = BenchmarkStage.Translation Then
                        profile.Concurrency = stage.Concurrency
                        profile.OverallAvgLatencyMs = stage.AvgLatencyMs
                        profile.OverallP95LatencyMs = stage.P95LatencyMs
                        profile.OverallReqPerSec = stage.RequestsPerSec
                        For Each ps In stage.PairSummaries
                            Dim key = $"{ps.SourceLang}>{ps.TargetLang}"
                            profile.Pairs(key) = New PairLatency() With {
                                .AvgLatencyMs = ps.AvgLatencyMs,
                                .AvgQuality = ps.AvgQuality,
                                .SampleCount = ps.Count}
                        Next
                    End If
                Next

                profile.Save()
                result.ProfileSaved = True
            Catch ex As Exception
                Infrastructure.AppLogger.Log($"[WARN] Failed to save latency profile: {ex.Message}")
            End Try
        End Sub

    End Class

    ' ══════════════════════════════════════════════════
    '  Data models
    ' ══════════════════════════════════════════════════

    Public Class CorpusEntry
        Public Property Id As Integer
        Public Property Domain As String
        Public Property Source As String
        Public Property SourceLang As String
        Public Property Targets As List(Of CorpusTarget)
    End Class

    Public Class CorpusTarget
        Public Property Lang As String
        Public Property Reference As String
    End Class

    Public Class WorkItem
        Public Property Entry As CorpusEntry
        Public Property Targets As List(Of CorpusTarget)
        Public Property Iteration As Integer
    End Class

    Public Class TtsWorkItem
        Public Property Text As String
        Public Property Language As String
        Public Property CommitId As Integer
        Public Property SourceText As String
        Public Property SourceLang As String
    End Class

    Public Class SttSample
        Public Property AudioFilePath As String
        Public Property Language As String
        Public Property ExpectedText As String
    End Class

    ' ── Per-request results ──

    Public Class RequestResult
        Public Property EntryId As Integer
        Public Property SourceLang As String
        Public Property LatencyMs As Long
        Public Property ErrorMessage As String
        Public Property PairResults As List(Of PairResult)
    End Class

    Public Class PairResult
        Public Property SourceLang As String
        Public Property TargetLang As String
        Public Property Reference As String
        Public Property Output As String
        Public Property QualityScore As Double
    End Class

    Public Class TtsRequestResult
        Public Property Language As String
        Public Property LatencyMs As Long
        Public Property Success As Boolean
        Public Property AudioUrl As String
        Public Property SourceText As String
        ''' <summary>The actual text that was synthesised (in the target language).</summary>
        Public Property SpokenText As String
        Public Property ErrorMessage As String
    End Class

    Public Class SttRequestResult
        Public Property Language As String
        Public Property ExpectedText As String
        Public Property TranscribedText As String
        Public Property LatencyMs As Long
        Public Property Accuracy As Double
        Public Property ErrorMessage As String
    End Class

    ' ── Aggregated results ──

    Public Class BenchmarkProgress
        Public Property Stage As BenchmarkStage
        Public Property Completed As Integer
        Public Property Total As Integer
        Public Property ElapsedMs As Long
        Public Property QueueDepth As Integer
        Public Property QueueActive As Integer
        Public Property QueueAvgWaitMs As Double
    End Class

    Public Class StageBenchmarkResult
        Public Property Stage As BenchmarkStage
        Public Property TotalMs As Long
        Public Property Concurrency As Integer
        Public Property TotalRequests As Integer
        Public Property Errors As Integer
        Public Property AvgLatencyMs As Double
        Public Property P50LatencyMs As Long
        Public Property P95LatencyMs As Long
        Public Property P99LatencyMs As Long
        Public Property RequestsPerSec As Double
        Public Property PairSummaries As List(Of PairSummary)
        Public Property FinalQueueMetrics As QueueMetrics
    End Class

    Public Class BenchmarkResult
        Public Property Timestamp As DateTime
        Public Property Stages As New List(Of StageBenchmarkResult)()
        Public Property ProfileSaved As Boolean
        Public Property Resources As ResourceReport
    End Class

    Public Class PairSummary
        Public Property Stage As BenchmarkStage
        Public Property SourceLang As String
        Public Property TargetLang As String
        Public Property Count As Integer
        Public Property AvgQuality As Double
        Public Property MinQuality As Double
        Public Property MaxQuality As Double
        Public Property AvgLatencyMs As Double
    End Class

End Namespace
