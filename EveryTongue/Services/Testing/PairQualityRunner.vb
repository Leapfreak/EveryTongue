Imports System.Formats.Tar
Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Testing

    ''' <summary>
    ''' FLORES-200 evaluation set (997 professionally-translated dev sentences per
    ''' language, Meta/NLLB) — the reference corpus for the direct-vs-pivot A/B.
    ''' Downloaded once (~26 MB) into test-data\flores200.
    ''' </summary>
    Public Class FloresDataset

        Public Const DownloadUrl As String = "https://dl.fbaipublicfiles.com/nllb/flores200_dataset.tar.gz"

        Public Shared ReadOnly Property RootDir As String
            Get
                Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data", "flores200")
            End Get
        End Property

        Public Shared Function IsInstalled() As Boolean
            Return Directory.Exists(RootDir) AndAlso
                   Directory.GetFiles(RootDir, "*.dev", SearchOption.AllDirectories).Length > 0
        End Function

        ''' <summary>FLORES codes that have a dev file installed, sorted.</summary>
        Public Shared Function AvailableLanguages() As List(Of String)
            If Not IsInstalled() Then Return New List(Of String)
            Return Directory.GetFiles(RootDir, "*.dev", SearchOption.AllDirectories).
                Select(Function(f) Path.GetFileNameWithoutExtension(f)).
                Distinct(StringComparer.OrdinalIgnoreCase).
                OrderBy(Function(c) c, StringComparer.OrdinalIgnoreCase).
                ToList()
        End Function

        ''' <summary>Dev sentences for one FLORES code (Nothing if not installed).</summary>
        Public Shared Function SentencesFor(floresCode As String) As String()
            Dim files = Directory.GetFiles(RootDir, floresCode & ".dev", SearchOption.AllDirectories)
            If files.Length = 0 Then Return Nothing
            Return File.ReadAllLines(files(0))
        End Function

        Public Shared Async Function DownloadAsync(status As Action(Of String), ct As CancellationToken) As Task
            Directory.CreateDirectory(RootDir)
            Dim tmpTarGz = Path.Combine(RootDir, "flores200_dataset.tar.gz.tmp")
            Using http As New HttpClient() With {.Timeout = TimeSpan.FromMinutes(10)}
                status("Downloading FLORES-200 (~26 MB)...")
                Using response = Await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct)
                    response.EnsureSuccessStatusCode()
                    Using fs As New FileStream(tmpTarGz, FileMode.Create, FileAccess.Write)
                        Await response.Content.CopyToAsync(fs, ct)
                    End Using
                End Using
            End Using
            status("Extracting...")
            ' Windows' bundled bsdtar first: .NET's TarReader rejects this archive's
            ' numeric header fields ("Unable to parse number"); bsdtar reads it fine.
            Dim usedSystemTar = Await Task.Run(Function() TryExtractWithSystemTar(tmpTarGz), ct)
            If Not usedSystemTar Then
                Using fs As New FileStream(tmpTarGz, FileMode.Open, FileAccess.Read)
                    Using gz As New GZipStream(fs, CompressionMode.Decompress)
                        Await TarFile.ExtractToDirectoryAsync(gz, RootDir, overwriteFiles:=True, cancellationToken:=ct)
                    End Using
                End Using
            End If
            File.Delete(tmpTarGz)
            AppLogger.Log(LogEvents.BENCH_START,
                $"FLORES-200 installed: {AvailableLanguages().Count} languages under {RootDir}")
        End Function

        ''' <summary>
        ''' Extract with the OS-bundled bsdtar (System32\tar.exe, present since
        ''' Windows 10 1803). Returns False when unavailable or failing so the
        ''' caller can fall back to System.Formats.Tar.
        ''' </summary>
        Private Shared Function TryExtractWithSystemTar(tarGzPath As String) As Boolean
            Dim tarExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "tar.exe")
            If Not File.Exists(tarExe) Then Return False
            Try
                Dim psi As New Diagnostics.ProcessStartInfo(tarExe,
                        $"-xzf ""{tarGzPath}"" -C ""{RootDir}""") With {
                    .UseShellExecute = False, .CreateNoWindow = True,
                    .RedirectStandardOutput = True, .RedirectStandardError = True}
                Using proc = Diagnostics.Process.Start(psi)
                    ' Both pipes MUST be drained before WaitForExit (4KB pipe buffer).
                    Dim stderrTask = proc.StandardError.ReadToEndAsync()
                    Dim stdout = proc.StandardOutput.ReadToEnd()
                    stderrTask.Wait()
                    proc.WaitForExit(600000)
                    If proc.ExitCode = 0 Then Return True
                    AppLogger.Log(LogEvents.BENCH_ERROR,
                        $"tar.exe extraction failed (exit {proc.ExitCode}): {stderrTask.Result} — falling back to .NET tar")
                    Return False
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.BENCH_ERROR,
                    $"tar.exe extraction error: {ex.Message} — falling back to .NET tar")
                Return False
            End Try
        End Function

    End Class

    Public Class PairAbExample
        Public Property Source As String
        Public Property Reference As String
        Public Property Direct As String
        Public Property Pivot As String
    End Class

    Public Class PairAbResult
        Public Property SourceLang As String
        Public Property TargetLang As String
        Public Property PivotLang As String
        Public Property Engine As String
        Public Property SentenceCount As Integer
        Public Property DirectChrF As Double
        Public Property PivotChrF As Double
        Public Property DirectAvgMs As Double
        Public Property PivotAvgMs As Double
        ''' <summary>
        ''' True when the pair includes the pivot language: no pivot route exists,
        ''' so the run is a DIRECT-ONLY engine-quality score (no verdict, no
        ''' measured-pair entry — pairs with English never pivot anyway).
        ''' </summary>
        Public Property PivotSkipped As Boolean
        Public Property RunAt As DateTime
        Public Property Examples As New List(Of PairAbExample)

        ''' <summary>Full per-sentence texts, for reference-free QE scoring after the run.</summary>
        Public Property Sources As New List(Of String)
        Public Property DirectOutputs As New List(Of String)
        Public Property PivotOutputs As New List(Of String)

        ''' <summary>CometKiwi QE system scores (~0..1, cross-pair comparable); -1 = not scored.</summary>
        Public Property QeDirect As Double = -1
        Public Property QePivot As Double = -1

        Public ReadOnly Property DirectWins As Boolean
            Get
                Return Not PivotSkipped AndAlso DirectChrF >= PivotChrF
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Direct-vs-pivot A/B over FLORES references: translates N dev sentences
    ''' src→tgt DIRECT (noPivot bypasses the policy) and src→English→tgt, scores
    ''' both against the professional reference with chrF. The verdict feeds
    ''' measured entries in translation-direct-pairs.local.json.
    ''' </summary>
    Public Class PairQualityRunner

        ''' <param name="backendName">
        ''' Orchestrator backend NAME (e.g. "Local", "Google") every request is
        ''' forced through via backendOverride — the benchmark engine selection is
        ''' independent of the globally active engine. NOTE: the orchestrator's
        ''' fallback chain still applies; if the chosen backend fails mid-run the
        ''' log shows TRANS_BACKEND_FALLBACK and the numbers stop being that
        ''' engine's — check the log if results look implausible.
        ''' </param>
        Public Async Function RunAsync(svc As ITranslationService,
                                       backendName As String,
                                       srcFlores As String,
                                       tgtFlores As String,
                                       pivotFlores As String,
                                       count As Integer,
                                       progress As Action(Of Integer, Integer),
                                       ct As CancellationToken) As Task(Of PairAbResult)

            Dim srcLines = FloresDataset.SentencesFor(srcFlores)
            Dim refLines = FloresDataset.SentencesFor(tgtFlores)
            If srcLines Is Nothing OrElse refLines Is Nothing Then
                Throw New InvalidOperationException($"FLORES dev files missing for {srcFlores} or {tgtFlores}")
            End If
            Dim n = Math.Min(count, Math.Min(srcLines.Length, refLines.Length))

            Dim directScorer As New ChrFScorer()
            Dim pivotScorer As New ChrFScorer()
            Dim directMs As Double = 0
            Dim pivotMs As Double = 0
            Dim skipPivot = srcFlores.Equals(pivotFlores, StringComparison.OrdinalIgnoreCase) OrElse
                            tgtFlores.Equals(pivotFlores, StringComparison.OrdinalIgnoreCase)
            Dim result As New PairAbResult With {
                .SourceLang = srcFlores, .TargetLang = tgtFlores, .PivotLang = pivotFlores,
                .Engine = backendName, .SentenceCount = n, .PivotSkipped = skipPivot
            }

            For i = 0 To n - 1
                ct.ThrowIfCancellationRequested()
                Dim src = srcLines(i)
                Dim ref = refLines(i)

                ' Direct baseline — noPivot so the policy can't reroute it.
                Dim sw = Diagnostics.Stopwatch.StartNew()
                Dim direct = Await svc.TranslateAsync(src, srcFlores, {tgtFlores}, ct,
                                                      noCache:=True, backendOverride:=backendName, noPivot:=True)
                sw.Stop() : directMs += sw.ElapsedMilliseconds
                Dim directOut = ""
                direct.TryGetValue(tgtFlores, directOut)

                ' Pivot path — two explicit hops, both direct by construction
                ' (each leg includes the pivot language). Skipped entirely for
                ' pairs that include the pivot language (direct-only scoring).
                Dim pivotOut = ""
                If Not skipPivot Then
                    sw.Restart()
                    Dim eng = Await svc.TranslateAsync(src, srcFlores, {pivotFlores}, ct,
                                                       noCache:=True, backendOverride:=backendName, noPivot:=True)
                    Dim engOut = ""
                    eng.TryGetValue(pivotFlores, engOut)
                    If Not String.IsNullOrWhiteSpace(engOut) Then
                        Dim second = Await svc.TranslateAsync(engOut, pivotFlores, {tgtFlores}, ct,
                                                              noCache:=True, backendOverride:=backendName, noPivot:=True)
                        second.TryGetValue(tgtFlores, pivotOut)
                    End If
                    sw.Stop() : pivotMs += sw.ElapsedMilliseconds
                End If

                directScorer.AddSentence(If(directOut, ""), ref)
                If Not skipPivot Then pivotScorer.AddSentence(If(pivotOut, ""), ref)

                result.Sources.Add(src)
                result.DirectOutputs.Add(If(directOut, ""))
                result.PivotOutputs.Add(If(pivotOut, ""))

                If result.Examples.Count < 3 Then
                    result.Examples.Add(New PairAbExample With {
                        .Source = src, .Reference = ref,
                        .Direct = If(directOut, ""), .Pivot = If(pivotOut, "")
                    })
                End If
                progress?.Invoke(i + 1, n)
            Next

            result.DirectChrF = directScorer.Score()
            result.PivotChrF = If(skipPivot, 0, pivotScorer.Score())
            result.DirectAvgMs = If(n > 0, directMs / n, 0)
            result.PivotAvgMs = If(n > 0 AndAlso Not skipPivot, pivotMs / n, 0)
            result.RunAt = DateTime.Now

            If skipPivot Then
                AppLogger.Log(LogEvents.BENCH_COMPLETE,
                    $"Engine score {srcFlores}→{tgtFlores} on {result.Engine}, n={n}: " &
                    $"chrF {result.DirectChrF:F1} ({result.DirectAvgMs:F0}ms) — direct-only (pair includes pivot language)")
            Else
                AppLogger.Log(LogEvents.BENCH_COMPLETE,
                    $"Pair A/B {srcFlores}→{tgtFlores} via {pivotFlores} on {result.Engine}, n={n}: " &
                    $"direct chrF {result.DirectChrF:F1} ({result.DirectAvgMs:F0}ms) vs " &
                    $"pivot chrF {result.PivotChrF:F1} ({result.PivotAvgMs:F0}ms) → " &
                    If(result.DirectWins, "DIRECT wins", "PIVOT wins"))
            End If
            Return result
        End Function

        ''' <summary>
        ''' Persist a measured direct-pair entry (engine-scoped) to the overlay file
        ''' the PivotPolicy also loads. Only call when direct won — the file's
        ''' semantic is "direct is trusted". Takes effect on the next server start.
        ''' </summary>
        Public Shared Sub SaveMeasuredEntry(result As PairAbResult)
            Dim engineKey = If(Translation.TranslationBackendRegistry.
                FindByBackendName(result.Engine)?.Key, result.Engine)
            Dim overlayPath = Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory,
                                           "translation-direct-pairs.local.json")

            Dim pairs As New List(Of Dictionary(Of String, Object))
            If File.Exists(overlayPath) Then
                Using doc = JsonDocument.Parse(File.ReadAllText(overlayPath))
                    Dim pairsEl As JsonElement = Nothing
                    If doc.RootElement.TryGetProperty("pairs", pairsEl) Then
                        For Each el In pairsEl.EnumerateArray()
                            ' Drop an older measurement of the same pair — the new run wins.
                            Dim a = GetStrProp(el, "a")
                            Dim b = GetStrProp(el, "b")
                            Dim sameLangs = (a.Equals(result.SourceLang, StringComparison.OrdinalIgnoreCase) AndAlso b.Equals(result.TargetLang, StringComparison.OrdinalIgnoreCase)) OrElse
                                            (a.Equals(result.TargetLang, StringComparison.OrdinalIgnoreCase) AndAlso b.Equals(result.SourceLang, StringComparison.OrdinalIgnoreCase))
                            If Not sameLangs Then
                                pairs.Add(JsonSerializer.Deserialize(Of Dictionary(Of String, Object))(el.GetRawText()))
                            End If
                        Next
                    End If
                End Using
            End If

            pairs.Add(New Dictionary(Of String, Object) From {
                {"a", result.SourceLang},
                {"b", result.TargetLang},
                {"reason", $"benchmark: direct chrF {result.DirectChrF:F1} ≥ pivot {result.PivotChrF:F1} (n={result.SentenceCount})"},
                {"source", "measured"},
                {"engines", New String() {engineKey}},
                {"scoreDirect", Math.Round(result.DirectChrF, 1)},
                {"scorePivot", Math.Round(result.PivotChrF, 1)},
                {"measuredDate", DateTime.Now.ToString("yyyy-MM-dd")}
            })

            Dim payload = New Dictionary(Of String, Object) From {
                {"_about", "Measured direct-pair entries written by the translation benchmark. Loaded by PivotPolicy in ADDITION to the shipped translation-direct-pairs.json. Survives app updates."},
                {"pairs", pairs}
            }
            File.WriteAllText(overlayPath,
                JsonSerializer.Serialize(payload, New JsonSerializerOptions With {
                    .WriteIndented = True,
                    .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }))
            AppLogger.Log(LogEvents.TRANS_PIVOT,
                $"Measured direct pair saved: {result.SourceLang}↔{result.TargetLang} engines=[{engineKey}] → {overlayPath} (applies on next server start)")
        End Sub

        Private Shared Function GetStrProp(el As JsonElement, name As String) As String
            Dim v As JsonElement = Nothing
            If el.TryGetProperty(name, v) AndAlso v.ValueKind = JsonValueKind.String Then Return v.GetString()
            Return ""
        End Function

    End Class

End Namespace
