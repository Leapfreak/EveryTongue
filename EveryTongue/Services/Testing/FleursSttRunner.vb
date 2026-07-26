Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Namespace Services.Testing

    ''' <summary>
    ''' Word/character error rate against a reference transcript — the STT
    ''' analogue of ChrFScorer. Corpus-level: edit counts accumulate across
    ''' clips. Both sides normalized (lowercase, punctuation stripped).
    ''' </summary>
    Public Class WerScorer
        Private _wordEdits As Long, _refWords As Long
        Private _charEdits As Long, _refChars As Long

        Public Sub AddClip(hypothesis As String, reference As String)
            Dim hypW = Tokens(hypothesis)
            Dim refW = Tokens(reference)
            _wordEdits += Levenshtein(hypW, refW)
            _refWords += refW.Length
            Dim hypC = String.Concat(hypW).ToCharArray().Select(Function(c) c.ToString()).ToArray()
            Dim refC = String.Concat(refW).ToCharArray().Select(Function(c) c.ToString()).ToArray()
            _charEdits += Levenshtein(hypC, refC)
            _refChars += refC.Length
        End Sub

        ''' <summary>Word error rate in percent (lower = better).</summary>
        Public Function Wer() As Double
            Return If(_refWords > 0, 100.0 * _wordEdits / _refWords, 0)
        End Function

        ''' <summary>Character error rate in percent — steadier across languages.</summary>
        Public Function Cer() As Double
            Return If(_refChars > 0, 100.0 * _charEdits / _refChars, 0)
        End Function

        Public Shared Function Tokens(text As String) As String()
            Dim cleaned = New String(If(text, "").ToLowerInvariant().
                Select(Function(c) If(Char.IsLetterOrDigit(c) OrElse Char.IsWhiteSpace(c) OrElse c = "'"c, c, " "c)).ToArray())
            Return cleaned.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
        End Function

        Private Shared Function Levenshtein(a As String(), b As String()) As Long
            Dim prev(b.Length) As Long, cur(b.Length) As Long
            For j = 0 To b.Length : prev(j) = j : Next
            For i = 1 To a.Length
                cur(0) = i
                For j = 1 To b.Length
                    Dim cost = If(a(i - 1) = b(j - 1), 0, 1)
                    cur(j) = Math.Min(Math.Min(cur(j - 1) + 1, prev(j) + 1), prev(j - 1) + cost)
                Next
                Dim tmp = prev : prev = cur : cur = tmp
            Next
            Return prev(b.Length)
        End Function
    End Class

    ''' <summary>
    ''' FLEURS — the speech edition of FLORES (same sentences, read by native
    ''' speakers, ~100 languages, with reference transcripts). Hosted ungated on
    ''' HuggingFace; per-language dev split downloaded on demand (~250 MB).
    ''' Configs are lang_REGION codes (ca_es, es_419, en_us, sv_se, ...).
    ''' </summary>
    Public Class FleursDataset
        Private Const HfBase As String = "https://huggingface.co/datasets/google/fleurs/resolve/main/data"

        Public Shared ReadOnly Property RootDir As String
            Get
                Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", "fleurs")
            End Get
        End Property

        Public Shared Function InstalledConfigs() As List(Of String)
            If Not Directory.Exists(RootDir) Then Return New List(Of String)
            Return Directory.GetDirectories(RootDir).
                Where(Function(d) File.Exists(Path.Combine(d, "dev.tsv"))).
                Select(Function(d) Path.GetFileName(d)).OrderBy(Function(s) s).ToList()
        End Function

        ''' <summary>All FLEURS configs, discovered live from the HF repo tree (never a static list).</summary>
        Public Shared Async Function ListRemoteConfigsAsync(ct As CancellationToken) As Task(Of List(Of String))
            Using http As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}
                Dim json = Await http.GetStringAsync("https://huggingface.co/api/datasets/google/fleurs/tree/main/data", ct)
                Using doc = JsonDocument.Parse(json)
                    Return doc.RootElement.EnumerateArray().
                        Where(Function(e) e.GetProperty("type").GetString() = "directory").
                        Select(Function(e) Path.GetFileName(e.GetProperty("path").GetString())).
                        OrderBy(Function(s) s).ToList()
                End Using
            End Using
        End Function

        ''' <summary>(wavPath, referenceText) clips for an installed config, deduped by file.</summary>
        Public Shared Function Clips(config As String) As List(Of (Wav As String, Ref As String))
            Dim dir = Path.Combine(RootDir, config)
            Dim result As New List(Of (Wav As String, Ref As String))
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each line In File.ReadLines(Path.Combine(dir, "dev.tsv"))
                Dim cols = line.Split(ChrW(9))
                If cols.Length < 4 OrElse Not seen.Add(cols(1)) Then Continue For
                Dim wavs = Directory.GetFiles(dir, cols(1), SearchOption.AllDirectories)
                If wavs.Length > 0 Then result.Add((wavs(0), cols(3)))
            Next
            Return result
        End Function

        Public Shared Async Function DownloadAsync(config As String, status As Action(Of String), ct As CancellationToken) As Task
            Dim dir = Path.Combine(RootDir, config)
            Directory.CreateDirectory(dir)
            Using http As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(30)}
                status($"Downloading {config} transcripts...")
                Dim tsv = Await http.GetByteArrayAsync($"{HfBase}/{config}/dev.tsv", ct)
                File.WriteAllBytes(Path.Combine(dir, "dev.tsv"), tsv)

                status($"Downloading {config} audio (~250 MB)...")
                Dim tmpTar = Path.Combine(dir, "dev.tar.gz.tmp")
                Using resp = Await http.GetAsync($"{HfBase}/{config}/audio/dev.tar.gz",
                                                 HttpCompletionOption.ResponseHeadersRead, ct)
                    resp.EnsureSuccessStatusCode()
                    Using fs As New FileStream(tmpTar, FileMode.Create, FileAccess.Write)
                        Await resp.Content.CopyToAsync(fs, ct)
                    End Using
                End Using
                status("Extracting audio...")
                ' Windows bsdtar — same choice as FLORES (proven robust there).
                Dim tarExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe")
                Await Task.Run(Sub()
                                   Dim psi As New Diagnostics.ProcessStartInfo(tarExe, $"-xzf ""{tmpTar}"" -C ""{dir}""") With {
                                       .UseShellExecute = False, .CreateNoWindow = True,
                                       .RedirectStandardOutput = True, .RedirectStandardError = True}
                                   Using proc = Diagnostics.Process.Start(psi)
                                       Dim stderrTask = proc.StandardError.ReadToEndAsync()
                                       proc.StandardOutput.ReadToEnd()
                                       stderrTask.Wait()
                                       proc.WaitForExit(600000)
                                       If proc.ExitCode <> 0 Then Throw New Exception($"tar extraction failed: {stderrTask.Result}")
                                   End Using
                               End Sub, ct)
                File.Delete(tmpTar)
            End Using
            AppLogger.Log(LogEvents.BENCH_START, $"FLEURS {config} installed: {Clips(config).Count} clips")
        End Function
    End Class

    Public Class FleursResult
        Public Property Config As String
        Public Property EngineKey As String
        Public Property ClipCount As Integer
        Public Property Wer As Double
        Public Property Cer As Double
        Public Property AvgMs As Double
        Public Property FailedClips As Integer
        ''' <summary>First per-clip error body — the actual reason, not just a count.</summary>
        Public Property FirstError As String = ""
        Public Property RunAt As DateTime
        Public Property Examples As New List(Of (Ref As String, Hyp As String))
    End Class

    ''' <summary>
    ''' STT quality sweep: spins up a TEMPORARY live-server in the chosen engine
    ''' mode, one-shot /transcribe per FLEURS clip, scores WER/CER vs references.
    ''' Coverage: faster-whisper (local, /load-model) + online engines with a
    ''' one-shot transcribe fn (speechmatics, google, ...). whisper-cpp variants
    ''' need the managed whisper-server — not wired here (v1).
    ''' </summary>
    Public Class FleursSttRunner

        Public Async Function RunAsync(config As AppConfig,
                                       fleursConfig As String,
                                       engineKey As String,
                                       count As Integer,
                                       progress As Action(Of String, Integer, Integer),
                                       ct As CancellationToken) As Task(Of FleursResult)
            Dim clips = FleursDataset.Clips(fleursConfig)
            Dim n = Math.Min(count, clips.Count)
            Dim langIso1 = fleursConfig.Split("_"c)(0)
            Dim result As New FleursResult With {.Config = fleursConfig, .EngineKey = engineKey, .ClipCount = n}

            Dim port = 5099
            Dim host As New Pipeline.PythonSidecarHost() With {
                .Label = "STT quality live-server", .AddWhisperToPath = True,
                .GracefulShutdownPath = "/shutdown", .LogFileName = "bench-stt-quality.log",
                .BaseEventId = LogEvents.PYLOG_LIVE, .Port = port}
            Using http As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(5)}
                Try
                    progress($"Starting {engineKey} engine...", 0, n)
                    Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live-server", "server.py")
                    host.Start(serverScript, $"--backend {engineKey}")
                    Dim ready = False
                    For i = 0 To 120
                        ct.ThrowIfCancellationRequested()
                        Try
                            If (Await http.GetAsync($"http://127.0.0.1:{port}/health", ct)).IsSuccessStatusCode Then
                                ready = True : Exit For
                            End If
                        Catch ex As OperationCanceledException : Throw
                        Catch : End Try
                        Await Task.Delay(500, ct)
                    Next
                    If Not ready Then Throw New Exception("live-server startup timeout")

                    Dim entry = Stt.SttBackendRegistry.Find(engineKey)
                    If entry IsNot Nothing AndAlso entry.RequiresInternet Then
                        Dim key = config.GetSttApiKey(engineKey)
                        If String.IsNullOrEmpty(key) Then Throw New Exception($"no API key configured for {engineKey}")
                        Dim cfgResp = Await http.PostAsync($"http://127.0.0.1:{port}/config",
                            New StringContent(JsonSerializer.Serialize(New With {.stt_api_key = key}),
                                              Text.Encoding.UTF8, "application/json"), ct)
                        ' The stt_api_key /config support is newer than v2.9.0 —
                        ' an older live-server silently ignores it and every
                        ' /transcribe then 503s. Verify it was ACCEPTED.
                        Dim cfgBody = Await cfgResp.Content.ReadAsStringAsync()
                        If Not cfgBody.Contains("stt_api_key") Then
                            Throw New Exception("this machine's live-server/server.py is too old to accept the API key " &
                                                "(needs the post-2.9.0 /config stt_api_key support) — update live-server\server.py from the current publish")
                        End If
                    Else
                        progress("Loading local model...", 0, n)
                        Dim modelPath = AppConfig.ResolvePath(If(entry?.ModelPathFromConfig?.Invoke(config), ""))
                        Dim loadJson = JsonSerializer.Serialize(New With {.model_path = modelPath})
                        Dim loadResp = Await http.PostAsync($"http://127.0.0.1:{port}/load-model",
                            New StringContent(loadJson, Text.Encoding.UTF8, "application/json"), ct)
                        If Not loadResp.IsSuccessStatusCode Then Throw New Exception($"model load failed (HTTP {CInt(loadResp.StatusCode)})")
                    End If

                    Dim scorer As New WerScorer()
                    Dim totalMs As Double = 0
                    For i = 0 To n - 1
                        ct.ThrowIfCancellationRequested()
                        Dim audio = File.ReadAllBytes(clips(i).Wav)
                        ' Same lesson as the DeepL 429 fix: cloud services throttle
                        ' rapid session churn (Speechmatics caps concurrent RT
                        ' sessions; teardown lags a few seconds). Pace between
                        ' clips and retry a failed clip once after a cooldown —
                        ' otherwise failures corrupt WER with 100%-wrong scores.
                        Dim isOnline = entry IsNot Nothing AndAlso entry.RequiresInternet
                        If isOnline AndAlso i > 0 Then Await Task.Delay(1500, ct)
                        Dim hyp = ""
                        Dim ok = False
                        Dim transportFail = False
                        Dim sw = Diagnostics.Stopwatch.StartNew()
                        For attempt = 0 To 1
                            Try
                                Dim resp = Await http.PostAsync(
                                    $"http://127.0.0.1:{port}/transcribe?lang={langIso1}",
                                    New ByteArrayContent(audio), ct)
                                If resp.IsSuccessStatusCode Then
                                    Using doc = JsonDocument.Parse(Await resp.Content.ReadAsStringAsync())
                                        Dim t As JsonElement = Nothing
                                        If doc.RootElement.TryGetProperty("text", t) Then hyp = If(t.GetString(), "")
                                    End Using
                                    ok = True
                                    Exit For
                                End If
                                If String.IsNullOrEmpty(result.FirstError) Then
                                    Dim errBody = Await resp.Content.ReadAsStringAsync()
                                    result.FirstError = $"HTTP {CInt(resp.StatusCode)}: {errBody.Substring(0, Math.Min(200, errBody.Length))}"
                                End If
                            Catch ex As OperationCanceledException
                                Throw
                            Catch ex As Exception
                                ' Transport failure — the sidecar may have died mid-run.
                                ' One dead clip must not kill the whole benchmark.
                                ' (VB: no Await inside Catch — recovery happens below.)
                                If String.IsNullOrEmpty(result.FirstError) Then result.FirstError = $"transport: {ex.Message}"
                                transportFail = True
                            End Try
                            If transportFail Then
                                transportFail = False
                                Dim alive = False
                                Try
                                    alive = (Await http.GetAsync($"http://127.0.0.1:{port}/health", ct)).IsSuccessStatusCode
                                Catch ex As OperationCanceledException
                                    Throw
                                Catch : End Try
                                If Not alive Then
                                    progress($"Engine process died at clip {i + 1} — restarting...", i, n)
                                    AppLogger.Log(LogEvents.BENCH_ERROR, $"FLEURS: sidecar died at clip {i + 1} — restarting (see live-server.log for its crash)")
                                    Try : host.Stop(2000) : Catch : End Try
                                    host.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live-server", "server.py"),
                                               $"--backend {engineKey}")
                                    For w = 0 To 120
                                        Dim up = False
                                        Try
                                            up = (Await http.GetAsync($"http://127.0.0.1:{port}/health", ct)).IsSuccessStatusCode
                                        Catch ex As OperationCanceledException
                                            Throw
                                        Catch : End Try
                                        If up Then Exit For
                                        Await Task.Delay(500, ct)
                                    Next
                                    If isOnline Then
                                        Await http.PostAsync($"http://127.0.0.1:{port}/config",
                                            New StringContent(JsonSerializer.Serialize(New With {.stt_api_key = config.GetSttApiKey(engineKey)}),
                                                              Text.Encoding.UTF8, "application/json"), ct)
                                    End If
                                End If
                            End If
                            If attempt = 0 Then Await Task.Delay(5000, ct) ' session-slot / recovery cooldown
                        Next
                        sw.Stop() : totalMs += sw.ElapsedMilliseconds
                        If Not ok Then result.FailedClips += 1
                        ' Failed clips are reported, never scored — scoring an empty
                        ' hypothesis as 100% WER conflates availability with accuracy.
                        If ok Then
                            scorer.AddClip(hyp, clips(i).Ref)
                            If result.Examples.Count < 3 Then result.Examples.Add((clips(i).Ref, hyp))
                        End If
                        progress($"{i + 1}/{n} clips", i + 1, n)
                    Next
                    result.Wer = scorer.Wer()
                    result.Cer = scorer.Cer()
                    result.AvgMs = If(n > 0, totalMs / n, 0)
                    result.RunAt = DateTime.Now
                    AppLogger.Log(LogEvents.BENCH_COMPLETE,
                        $"FLEURS {fleursConfig} on {engineKey}, n={n}: WER {result.Wer:F1}% CER {result.Cer:F1}% avg {result.AvgMs:F0}ms failed={result.FailedClips}")
                    Return result
                Finally
                    Try : host.Stop() : host.Dispose() : Catch : End Try
                End Try
            End Using
        End Function
    End Class

End Namespace
