Imports System.IO
Imports System.Text
Imports EveryTongue.Models
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Scheduling
Imports EveryTongue.Services.Testing
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Translation

Public Class FormTranslationBenchmark

    Private ReadOnly _runner As New TranslationBenchmarkRunner()
    Private ReadOnly _sttComparer As New SttComparisonRunner()
    Private ReadOnly _concRunner As New SttConcurrencyRunner()
    Private ReadOnly _transConcRunner As New TranslationConcurrencyRunner()
    Private ReadOnly _ttsBenchRunner As New TtsBenchmarkRunner()
    Private ReadOnly _ttsConcRunner As New TtsConcurrencyRunner()
    Private _corpus As List(Of CorpusEntry)
    Private _lastResult As BenchmarkResult
    Private _lastSttResult As SttComparisonResult
    Private _lastConcResult As ConcurrencyTestResult
    Private _lastTransConcResult As ConcurrencyTestResult
    Private _lastTtsResult As TtsBenchmarkResult
    Private _lastTtsConcResult As ConcurrencyTestResult
    Private _translationService As ITranslationService
    Private _ttsService As ITtsService
    Private _ttsBackends As IEnumerable(Of ITtsBackend)
    Private _liveServerPort As Integer
    Private _config As AppConfig

    ''' <summary>
    ''' Head-supplied kick-starter for the local translation sidecar (FormMain's
    ''' EnsureDefaultTranslationRunning). Lets the Pair A/B tab start the NLLB
    ''' engine itself when the user selects it, instead of requiring a warm-up
    ''' detour through the Translate workspace.
    ''' </summary>
    Private ReadOnly _startLocalEngine As Action

    Public Sub New(translationService As ITranslationService,
                   Optional ttsService As ITtsService = Nothing,
                   Optional liveServerPort As Integer = 0,
                   Optional config As AppConfig = Nothing,
                   Optional ttsBackends As IEnumerable(Of ITtsBackend) = Nothing,
                   Optional startLocalEngine As Action = Nothing)
        InitializeComponent()
        _translationService = translationService
        _ttsService = ttsService
        _ttsBackends = If(ttsBackends, Enumerable.Empty(Of ITtsBackend)())
        _liveServerPort = liveServerPort
        _config = If(config, New AppConfig())
        _startLocalEngine = startLocalEngine
        ApplyLocale()
    End Sub

    ''' <summary>
    ''' Static chrome only — runtime status/progress messages stay English
    ''' (operator diagnostics, like log contents).
    ''' </summary>
    Private Sub ApplyLocale()
        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        Me.Text = lp.GetString("Bm_Title")
        tabTranslation.Text = lp.GetString("Bm_TabTranslation")
        tabTransPipeline.Text = lp.GetString("Bm_TabPipeline")
        tabTransConcurrency.Text = lp.GetString("Bm_TabConcurrency")
        tabTransPairAb.Text = lp.GetString("Bm_TabPairAb")
        tabTransResources.Text = lp.GetString("Bm_TabResources")
        tabStt.Text = lp.GetString("Bm_TabStt")
        tabSttQuality.Text = lp.GetString("Bm_TabQuality")
        tabSttComparison.Text = lp.GetString("Bm_TabComparison")
        tabSttConcurrency.Text = lp.GetString("Bm_TabConcurrency")
        tabSttResources.Text = lp.GetString("Bm_TabResources")
        tabTts.Text = lp.GetString("Bm_TabTts")
        tabTtsComparison.Text = lp.GetString("Bm_TabComparison")
        tabTtsConcurrency.Text = lp.GetString("Bm_TabConcurrency")
        tabTtsResources.Text = lp.GetString("Bm_TabResources")

        ' Pair A/B (FLORES)
        lblAbInfo.Text = lp.GetString("Bm_AbInfoChecking")
        btnAbDownload.Text = lp.GetString("Bm_AbDownload")
        lblAbEngine.Text = lp.GetString("Bm_Engine")
        lblAbSource.Text = lp.GetString("Bm_AbSource")
        lblAbTarget.Text = lp.GetString("Bm_AbTarget")
        lblAbCount.Text = lp.GetString("Bm_AbSentences")
        btnAbRun.Text = lp.GetString("Bm_AbRun")
        btnAbCancel.Text = lp.GetString("Opt_Cancel")
        btnAbSave.Text = lp.GetString("Bm_AbSave")

        ' Quality (FLEURS)
        lblFlInfo.Text = lp.GetString("Bm_FlInfo")
        btnFlDownload.Text = lp.GetString("Bm_FlDownload")
        lblFlLang.Text = lp.GetString("Bm_FlLang")
        lblFlEngine.Text = lp.GetString("Bm_SttEngine")
        lblFlCount.Text = lp.GetString("Bm_FlClips")
        btnFlRun.Text = lp.GetString("Bm_Run")
        btnFlCancel.Text = lp.GetString("Opt_Cancel")

        ' Pipeline
        grpConfig.Text = lp.GetString("Bm_Configuration")
        lblDomain.Text = lp.GetString("Bm_Domain")
        lblTargets.Text = lp.GetString("Bm_Targets")
        lblConcurrency.Text = lp.GetString("Bm_Concurrency")
        lblIterations.Text = lp.GetString("Bm_Iterations")
        btnRun.Text = lp.GetString("Bm_Run")
        btnCancel.Text = lp.GetString("Opt_Cancel")
        btnExport.Text = lp.GetString("Bm_ExportCsv")
        grpResults.Text = lp.GetString("Bm_Results")
        lblSummary.Text = lp.GetString("Bm_RunHint")
        colStage.HeaderText = lp.GetString("Bm_ColStage")
        colPair.HeaderText = lp.GetString("Bm_ColLanguage")
        colCount.HeaderText = lp.GetString("Bm_ColSamples")
        colAvgLatency.HeaderText = lp.GetString("Bm_ColAvgLatency")
        colAvgQuality.HeaderText = lp.GetString("Bm_ColQuality")
        colMinQuality.HeaderText = lp.GetString("Bm_ColMinPct")
        colMaxQuality.HeaderText = lp.GetString("Bm_ColMaxPct")

        ' Translation concurrency
        lblTransConcTargets.Text = lp.GetString("Bm_TargetLangs")
        lblTransConcIterations.Text = lp.GetString("Bm_RoundsPerLevel")
        lblTransConcLevels.Text = lp.GetString("Bm_ConcLevels")
        btnTransConcRun.Text = lp.GetString("Bm_RunThroughput")
        btnTransConcCancel.Text = lp.GetString("Opt_Cancel")
        btnTransConcExport.Text = lp.GetString("Bm_ExportCsv")
        lblTransConcProgress.Text = lp.GetString("Bm_TransConcHint")
        colTransConcLevel.HeaderText = lp.GetString("Bm_ColConcurrency")
        colTransConcRequests.HeaderText = lp.GetString("Bm_ColRequests")
        colTransConcWall.HeaderText = lp.GetString("Bm_ColWallMs")
        colTransConcAvg.HeaderText = lp.GetString("Bm_ColAvgMs")
        colTransConcP50.HeaderText = lp.GetString("Bm_ColP50")
        colTransConcP95.HeaderText = lp.GetString("Bm_ColP95")
        colTransConcMax.HeaderText = lp.GetString("Bm_ColMaxMs")
        colTransConcThroughput.HeaderText = lp.GetString("Bm_ColTransSec")
        colTransConcErrors.HeaderText = lp.GetString("Bm_ColErrors")
        lblResources.Text = lp.GetString("Bm_ResourcesHintPipeline")

        ' STT comparison + concurrency
        lblSttAudioFile.Text = lp.GetString("Bm_TestAudio")
        lblSttIterations.Text = lp.GetString("Bm_Iterations")
        btnSttCompare.Text = lp.GetString("Bm_CompareEngines")
        btnSttCancel.Text = lp.GetString("Opt_Cancel")
        btnSttExport.Text = lp.GetString("Bm_ExportCsv")
        lblSttProgress.Text = lp.GetString("Bm_SttSelectHint")
        lblConcAudioFile.Text = lp.GetString("Bm_TestAudio")
        lblConcIterations.Text = lp.GetString("Bm_RoundsPerLevel")
        lblConcLevels.Text = lp.GetString("Bm_ConcLevels")
        btnConcRun.Text = lp.GetString("Bm_RunThroughput")
        btnConcCancel.Text = lp.GetString("Opt_Cancel")
        btnConcExport.Text = lp.GetString("Bm_ExportCsv")
        lblConcProgress.Text = lp.GetString("Bm_ConcHint")
        lblSttResources.Text = lp.GetString("Bm_ResourcesHintComparison")
        colConcLevel.HeaderText = lp.GetString("Bm_ColSpeakers")
        colConcRequests.HeaderText = lp.GetString("Bm_ColRequests")
        colConcWall.HeaderText = lp.GetString("Bm_ColWallMs")
        colConcAvg.HeaderText = lp.GetString("Bm_ColAvgMs")
        colConcP50.HeaderText = lp.GetString("Bm_ColP50")
        colConcP95.HeaderText = lp.GetString("Bm_ColP95")
        colConcMax.HeaderText = lp.GetString("Bm_ColMaxMs")
        colConcThroughput.HeaderText = lp.GetString("Bm_ColInfSec")
        colConcErrors.HeaderText = lp.GetString("Bm_ColErrors")
        colSttEngine.HeaderText = lp.GetString("Bm_ColEngine")
        colSttLoadTime.HeaderText = lp.GetString("Bm_ColModelLoad")
        colSttAvgMs.HeaderText = lp.GetString("Bm_ColAvgMs")
        colSttMinMs.HeaderText = lp.GetString("Bm_ColMinMs")
        colSttMaxMs.HeaderText = lp.GetString("Bm_ColMaxMs")
        colSttSpeedup.HeaderText = lp.GetString("Bm_ColSpeedup")
        colSttText.HeaderText = lp.GetString("Bm_ColTranscription")

        ' TTS comparison + concurrency
        lblTtsText.Text = lp.GetString("Bm_TestText")
        lblTtsLanguage.Text = lp.GetString("Bible_Language")
        lblTtsBackends.Text = lp.GetString("Bm_Backends")
        lblTtsIterations.Text = lp.GetString("Bm_Iterations")
        btnTtsCompare.Text = lp.GetString("Bm_CompareEngines")
        btnTtsCancel.Text = lp.GetString("Opt_Cancel")
        btnTtsExport.Text = lp.GetString("Bm_ExportCsv")
        lblTtsProgress.Text = lp.GetString("Bm_TtsSelectHint")
        colTtsEngine.HeaderText = lp.GetString("Bm_ColEngine")
        colTtsAvgMs.HeaderText = lp.GetString("Bm_ColAvgMs")
        colTtsMinMs.HeaderText = lp.GetString("Bm_ColMinMs")
        colTtsMaxMs.HeaderText = lp.GetString("Bm_ColMaxMs")
        colTtsP95Ms.HeaderText = lp.GetString("Bm_ColP95")
        colTtsSpeedup.HeaderText = lp.GetString("Bm_ColSpeedup")
        colTtsAudioSize.HeaderText = lp.GetString("Bm_ColAudioSize")
        colTtsCodec.HeaderText = lp.GetString("Bm_ColFormat")
        lblTtsConcText.Text = lp.GetString("Bm_TestText")
        lblTtsConcLanguage.Text = lp.GetString("Bible_Language")
        lblTtsConcBackend.Text = lp.GetString("Bm_Backend")
        lblTtsConcIterations.Text = lp.GetString("Bm_RoundsPerLevel")
        lblTtsConcLevels.Text = lp.GetString("Bm_ConcLevels")
        btnTtsConcRun.Text = lp.GetString("Bm_RunThroughput")
        btnTtsConcCancel.Text = lp.GetString("Opt_Cancel")
        btnTtsConcExport.Text = lp.GetString("Bm_ExportCsv")
        lblTtsConcProgress.Text = lp.GetString("Bm_TtsConcHint")
        lblTtsResources.Text = lp.GetString("Bm_ResourcesHintComparison")
        btnExportAll.Text = lp.GetString("Bm_ExportAll")
    End Sub

    Private Sub FormTranslationBenchmark_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = Owner?.Icon
        _corpus = TranslationBenchmarkRunner.LoadCorpus()
        AppLogger.Log(LogEvents.BENCH_START, $"Form loaded — corpus: {_corpus.Count} entries, " &
                      $"translation: {_translationService IsNot Nothing}, " &
                      $"TTS: {_ttsService IsNot Nothing} ({_ttsBackends.Count()} backends), " &
                      $"livePort: {_liveServerPort}, " &
                      $"translationModel: {GetTranslationModelInfo()}, " &
                      $"sttModel: {GetSttModelInfo()}, " &
                      $"ttsEngines: [{String.Join(", ", _ttsBackends.Select(Function(b) b.Name))}]")

        ' Populate domain filter
        cboDomain.Items.Add("(All)")
        For Each d In TranslationBenchmarkRunner.GetDomains(_corpus)
            cboDomain.Items.Add(d)
        Next
        cboDomain.SelectedIndex = 0

        ' Populate target languages
        For Each lang In TranslationBenchmarkRunner.GetTargetLanguages(_corpus)
            clbTargets.Items.Add(lang, True)
        Next


        ' Populate translation concurrency targets
        For Each lang In TranslationBenchmarkRunner.GetTargetLanguages(_corpus)
            clbTransConcTargets.Items.Add(lang, True)
        Next

        UpdateCorpusInfo()
        ShowExistingProfile()
        WireSttHandlers()
        WireTransConcHandlers()
        WireTtsHandlers()
        InitPairAb()
        InitFleurs()

        AddHandler btnExportAll.Click, AddressOf ExportAll_Click

        AddHandler cboDomain.SelectedIndexChanged, Sub(s, ev) UpdateCorpusInfo()

        AddHandler _runner.ProgressChanged, Sub(s, prog)
                                                 If InvokeRequired Then
                                                     BeginInvoke(Sub() UpdateProgress(prog))
                                                 Else
                                                     UpdateProgress(prog)
                                                 End If
                                             End Sub
    End Sub

    Private Sub UpdateCorpusInfo()
        Dim filtered = GetFilteredCorpus()
        Dim targetCount = clbTargets.CheckedItems.Count
        lblCorpusInfo.Text = $"{filtered.Count} sentences" & vbCrLf &
                             $"{targetCount} target language(s)" & vbCrLf &
                             $"{filtered.Count * targetCount * nudIterations.Value} total translations"
    End Sub

    Private Sub ShowExistingProfile()
        Dim orch = TryCast(_translationService, TranslationOrchestrator)
        Dim profile = orch?.LatencyProfile
        If profile IsNot Nothing Then
            lblQueueStats.Text = $"Baseline: {profile.Pairs.Count} pairs  |  " &
                                 $"avg {profile.OverallAvgLatencyMs:F0}ms  |  " &
                                 $"{profile.OverallReqPerSec} req/s  |  " &
                                 $"from {profile.Timestamp:yyyy-MM-dd HH:mm}"
        Else
            lblQueueStats.Text = "No baseline profile — run a benchmark to create one."
        End If
    End Sub

    Private Function GetFilteredCorpus() As List(Of CorpusEntry)
        Dim domain = If(cboDomain.SelectedIndex <= 0, "", cboDomain.SelectedItem?.ToString())
        If String.IsNullOrEmpty(domain) Then Return _corpus
        Return _corpus.Where(Function(entry) entry.Domain = domain).ToList()
    End Function

    Private Function GetSelectedTargets() As List(Of String)
        Dim targets As New List(Of String)()
        For Each item In clbTargets.CheckedItems
            targets.Add(item.ToString())
        Next
        Return targets
    End Function

    Private Async Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
        Dim targets = GetSelectedTargets()
        If targets.Count = 0 Then
            MessageBox.Show("Select at least one target language.", "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If _translationService Is Nothing Then
            MessageBox.Show("No translation service available.", "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim filtered = GetFilteredCorpus()
        If filtered.Count = 0 Then
            MessageBox.Show("No corpus entries match the selected domain.", "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnRun.Enabled = False
        btnCancel.Enabled = True
        btnExport.Enabled = False
        progressBar.Value = 0
        dgvResults.Rows.Clear()
        lblSummary.Text = "Running..."
        lblProgress.Text = ""
        lblQueueStats.Text = "Queue: starting..."
        lblResources.Text = "Monitoring resources..."

        Dim concurrency = CInt(nudConcurrency.Value)
        Dim iterations = CInt(nudIterations.Value)
        Dim result As New BenchmarkResult() With {.Timestamp = DateTime.Now}

        AppLogger.Log(LogEvents.BENCH_START, $"Translation Pipeline starting — {filtered.Count} sentences, " &
                      $"{targets.Count} targets, concurrency={concurrency}, iterations={iterations}, " &
                      $"model: {GetTranslationModelInfo()}")

        ' Start resource monitoring (samples every 500ms)
        Dim monitor As New ResourceMonitor(500)
        monitor.Start()

        Try
            lblSummary.Text = "Running Translation benchmark..."
            progressBar.Maximum = filtered.Count * iterations
            Dim translationResult = Await _runner.RunTranslationBenchmarkAsync(
                _translationService, filtered, targets, concurrency, iterations)
            result.Stages.Add(translationResult)

            ' Stop resource monitoring and attach report
            result.Resources = monitor.Stop()
            StampModelInfo(result.Resources, GetTranslationModelInfo())

            ' Save latency profile and show combined results
            TranslationBenchmarkRunner.SaveLatencyProfile(result)
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"Translation Pipeline complete — {result.Stages.Count} stages, model: {GetTranslationModelInfo()}")
            ShowResult(result)
            AutoSaveResults()

        Catch ex As OperationCanceledException
            result.Resources = monitor.Stop()
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "Translation Pipeline cancelled by user")
            lblSummary.Text = "Benchmark cancelled."
            lblProgress.Text = ""
        Catch ex As Exception
            result.Resources = monitor.Stop()
            AppLogger.Log(LogEvents.BENCH_ERROR, $"Translation Pipeline error: {ex.Message}")
            lblSummary.Text = $"Error: {ex.Message}"
        Finally
            btnRun.Enabled = True
            btnCancel.Enabled = False
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        _runner.Cancel()
        btnCancel.Enabled = False
    End Sub

    Private Sub UpdateProgress(prog As BenchmarkProgress)
        If prog.Total > 0 Then
            progressBar.Maximum = prog.Total
            progressBar.Value = Math.Min(prog.Completed, prog.Total)
        End If
        Dim elapsed = TimeSpan.FromMilliseconds(prog.ElapsedMs)
        Dim rate = If(prog.ElapsedMs > 0, Math.Round(prog.Completed / (prog.ElapsedMs / 1000.0), 1), 0)
        Dim stageName = prog.Stage.ToString()
        lblProgress.Text = $"[{stageName}] {prog.Completed}/{prog.Total}  —  {elapsed:mm\:ss}  —  {rate} req/s"

        If prog.QueueDepth > 0 OrElse prog.QueueActive > 0 Then
            lblQueueStats.Text = $"Queue: {prog.QueueActive} active  |  " &
                                 $"{prog.QueueDepth} waiting  |  " &
                                 $"avg wait {prog.QueueAvgWaitMs:F0}ms"
        End If
    End Sub

    Private Sub ShowResult(result As BenchmarkResult)
        _lastResult = result
        btnExport.Enabled = True

        ' Build summary across all stages
        Dim sb As New StringBuilder()
        For Each stage In result.Stages
            Dim elapsed = TimeSpan.FromMilliseconds(stage.TotalMs)
            sb.AppendLine($"{stage.Stage}: {stage.TotalRequests} requests in {elapsed:mm\:ss\.f}  |  " &
                           $"{stage.RequestsPerSec} req/s  |  " &
                           $"avg {stage.AvgLatencyMs:F0}ms  |  p95 {stage.P95LatencyMs}ms  |  " &
                           $"{stage.Errors} errors")
        Next
        lblSummary.Text = sb.ToString().TrimEnd()

        ' Populate grid with per-stage pair summaries
        dgvResults.Rows.Clear()
        For Each stage In result.Stages
            For Each ps In stage.PairSummaries.OrderBy(Function(p) p.SourceLang).ThenBy(Function(p) p.TargetLang)
                Dim langDisplay = If(String.IsNullOrEmpty(ps.TargetLang),
                    ps.SourceLang,
                    $"{ps.SourceLang} > {ps.TargetLang}")
                Dim idx = dgvResults.Rows.Add(
                    ps.Stage.ToString(),
                    langDisplay,
                    ps.Count,
                    $"{ps.AvgLatencyMs:F0}",
                    $"{ps.AvgQuality:F1}",
                    $"{ps.MinQuality:F1}",
                    $"{ps.MaxQuality:F1}")

                Dim row = dgvResults.Rows(idx)
                If ps.AvgQuality >= 70 Then
                    row.Cells("colAvgQuality").Style.ForeColor = Color.DarkGreen
                ElseIf ps.AvgQuality >= 40 Then
                    row.Cells("colAvgQuality").Style.ForeColor = Color.DarkOrange
                Else
                    row.Cells("colAvgQuality").Style.ForeColor = Color.Red
                End If
            Next
        Next

        progressBar.Value = progressBar.Maximum

        ' Show final queue metrics + profile status
        Dim queueText As New StringBuilder()
        For Each stage In result.Stages
            Dim qm = stage.FinalQueueMetrics
            If qm IsNot Nothing Then
                queueText.Append($"{stage.Stage}: {qm.TotalCompleted} done, " &
                                 $"avg wait {qm.AvgWaitMs:F0}ms  |  ")
            End If
        Next
        If result.ProfileSaved Then queueText.Append("Profile saved")
        lblQueueStats.Text = If(queueText.Length > 0, queueText.ToString().TrimEnd(" "c, "|"c, " "c), "Benchmark complete.")

        ' Show resource utilisation report
        ShowResourceReport(result.Resources)

        ' Tell the orchestrator to reload the profile
        Dim orch = TryCast(_translationService, TranslationOrchestrator)
        orch?.ReloadLatencyProfile()
    End Sub

    Private Sub ShowResourceReport(report As ResourceReport)
        If report Is Nothing OrElse report.SampleCount = 0 Then
            lblResources.Text = "No resource data collected."
            Return
        End If

        Dim sb As New StringBuilder()
        sb.Append(report.ToSummaryText())

        ' Show warnings in red if any
        If report.Warnings.Count > 0 Then
            lblResources.ForeColor = Color.OrangeRed
            sb.AppendLine()
            For Each warning In report.Warnings
                sb.AppendLine($"  WARNING: {warning}")
            Next
        Else
            lblResources.ForeColor = Color.Gray
        End If

        lblResources.Text = sb.ToString().TrimEnd()
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' STT Engine Comparison
    ' ═══════════════════════════════════════════════════════════════
    Private Sub WireSttHandlers()
        AddHandler btnSttBrowse.Click, AddressOf SttBrowse_Click
        AddHandler btnSttCompare.Click, AddressOf SttCompare_Click
        AddHandler btnSttCancel.Click, AddressOf SttCancel_Click
        AddHandler btnSttExport.Click, AddressOf SttExport_Click
        AddHandler _sttComparer.ProgressChanged, Sub(s, msg)
                                                       If InvokeRequired Then
                                                           BeginInvoke(Sub() lblSttProgress.Text = msg)
                                                       Else
                                                           lblSttProgress.Text = msg
                                                       End If
                                                   End Sub

        ' Ensure concurrent grid has columns (Designer AddRange may not persist)
        EnsureConcurrentGridColumns()

        ' Concurrent throughput handlers
        AddHandler btnConcBrowse.Click, AddressOf ConcBrowse_Click
        AddHandler btnConcRun.Click, AddressOf ConcRun_Click
        AddHandler btnConcCancel.Click, Sub(s, e) _concRunner.Cancel()
        AddHandler btnConcExport.Click, AddressOf ConcExport_Click
        AddHandler _concRunner.ProgressChanged, Sub(s, msg)
                                                     If InvokeRequired Then
                                                         BeginInvoke(Sub() lblConcProgress.Text = msg)
                                                     Else
                                                         lblConcProgress.Text = msg
                                                     End If
                                                 End Sub
    End Sub

    Private Sub EnsureConcurrentGridColumns()
        If dgvConcurrent.Columns.Count > 0 Then Return

        ' Re-add grid to tab if it lost its parent
        If dgvConcurrent.Parent Is Nothing AndAlso tabSttConcurrency IsNot Nothing Then
            tabSttConcurrency.Controls.Add(dgvConcurrent)
        End If

        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        dgvConcurrent.Columns.AddRange(
            New DataGridViewTextBoxColumn() With {.Name = "colConcLevel", .HeaderText = lp.GetString("Bm_ColSpeakers"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcRequests", .HeaderText = lp.GetString("Bm_ColRequests"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcWall", .HeaderText = lp.GetString("Bm_ColWallMs"), .FillWeight = 12},
            New DataGridViewTextBoxColumn() With {.Name = "colConcAvg", .HeaderText = lp.GetString("Bm_ColAvgMs"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcP50", .HeaderText = lp.GetString("Bm_ColP50Ms"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcP95", .HeaderText = lp.GetString("Bm_ColP95Ms"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcMax", .HeaderText = lp.GetString("Bm_ColMaxMs"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcThroughput", .HeaderText = lp.GetString("Bm_ColInfPerSec"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcErrors", .HeaderText = lp.GetString("Bm_ColErrors"), .FillWeight = 11}
        )
    End Sub

    Private Sub SttBrowse_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Filter = "WAV files|*.wav|All files|*.*"
            dlg.Title = "Select test audio file"
            If dlg.ShowDialog() = DialogResult.OK Then
                txtSttAudioFile.Text = dlg.FileName
            End If
        End Using
    End Sub

    Private Async Sub SttCompare_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtSttAudioFile.Text) OrElse Not File.Exists(txtSttAudioFile.Text) Then
            MessageBox.Show("Select a valid WAV audio file first.", "STT Comparison",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Build set of enabled engines from checkboxes (before disabling UI)
        Dim enabled As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If chkSttCuda.Checked Then enabled.Add("whisper-cpp-cuda")
        If chkSttVulkan.Checked Then enabled.Add("whisper-cpp-vulkan")
        If chkSttCpu.Checked Then enabled.Add("whisper-cpp-cpu")
        If chkSttFasterWhisper.Checked Then enabled.Add("faster-whisper")

        If enabled.Count = 0 Then
            MessageBox.Show("Select at least one engine to test.", "STT Comparison",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnSttCompare.Enabled = False
        btnSttCancel.Enabled = True
        dgvSttCompare.Rows.Clear()
        lblSttProgress.ForeColor = Color.Gray
        lblSttProgress.Text = "Starting comparison..."

        AppLogger.Log(LogEvents.BENCH_START, $"STT Comparison starting — audio: {txtSttAudioFile.Text}, " &
                      $"iterations: {nudSttIterations.Value}, engines: {String.Join(", ", enabled)}, " &
                      $"model: {GetSttModelInfo()}")

        Try
            Dim result = Await _sttComparer.RunComparisonAsync(
                txtSttAudioFile.Text, _config, _liveServerPort,
                CInt(nudSttIterations.Value), Threading.CancellationToken.None, enabled)
            _lastSttResult = result
            StampModelInfo(result.Resources, GetSttModelInfo())
            btnSttExport.Enabled = True
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"STT Comparison complete — {result.Backends.Count} backends tested, model: {GetSttModelInfo()}")
            ShowSttComparisonResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "STT Comparison cancelled by user")
            lblSttProgress.Text = "Comparison cancelled."
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"STT Comparison error: {ex.Message}")
            lblSttProgress.ForeColor = Color.Red
            lblSttProgress.Text = $"Error: {ex.Message}"
        Finally
            btnSttCompare.Enabled = True
            btnSttCancel.Enabled = False
        End Try
    End Sub

    Private Sub SttCancel_Click(sender As Object, e As EventArgs)
        _sttComparer.Cancel()
        btnSttCancel.Enabled = False
    End Sub

    Private Sub SttExport_Click(sender As Object, e As EventArgs)
        If _lastSttResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"stt_benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            sb.AppendLine($"# STT Engine Comparison — {GetSttModelInfo()}")
            sb.AppendLine("Engine,Status,Model Load (ms),Avg (ms),Min (ms),Max (ms),Iterations,Speedup,Transcription")

            For Each b In _lastSttResult.Backends
                Dim status = If(b.Skipped, $"Skipped: {b.SkipReason}",
                              If(b.Failed, $"Failed: {b.ErrorMessage}", "OK"))
                Dim text = If(b.TranscribedText, "").Replace("""", """""")
                sb.AppendLine($"""{b.BackendName}"",""{status}"",{b.ModelLoadMs},{b.AvgInferenceMs},{b.MinInferenceMs},{b.MaxInferenceMs},{b.Iterations},{b.SpeedupVsFastest:F2},""{text}""")
            Next

            ' Resource utilisation section
            If _lastSttResult.Resources IsNot Nothing Then
                sb.AppendLine()
                sb.Append(_lastSttResult.Resources.ToCsvSection())
            End If

            IO.File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    Private Sub ShowSttComparisonResult(result As SttComparisonResult)
        dgvSttCompare.Rows.Clear()

        For Each b In result.Backends
            Dim statusOrText As String
            Dim loadTime As String
            Dim avgMs As String
            Dim minMs As String
            Dim maxMs As String
            Dim speedup As String

            If b.Skipped Then
                statusOrText = b.SkipReason
                loadTime = "—"
                avgMs = "—"
                minMs = "—"
                maxMs = "—"
                speedup = "—"
            ElseIf b.Failed Then
                statusOrText = $"FAILED: {b.ErrorMessage}"
                loadTime = "—"
                avgMs = "—"
                minMs = "—"
                maxMs = "—"
                speedup = "—"
            Else
                Dim text = If(b.TranscribedText, "")
                statusOrText = If(text.Length > 80, text.Substring(0, 80) & "...", text)
                loadTime = If(b.ModelLoadMs > 0, $"{(b.ModelLoadMs / 1000.0):F1}s", "—")
                avgMs = $"{b.AvgInferenceMs}"
                minMs = $"{b.MinInferenceMs}"
                maxMs = $"{b.MaxInferenceMs}"
                speedup = If(b.SpeedupVsFastest > 0, $"{b.SpeedupVsFastest:F2}x", "—")
            End If

            Dim idx = dgvSttCompare.Rows.Add(b.BackendName, loadTime, avgMs, minMs, maxMs, speedup, statusOrText)
            Dim row = dgvSttCompare.Rows(idx)

            ' Color-code the speedup column
            If Not b.Skipped AndAlso Not b.Failed Then
                If b.SpeedupVsFastest >= 1.0 Then
                    row.Cells("colSttSpeedup").Style.ForeColor = Color.DarkGreen
                    row.Cells("colSttSpeedup").Style.Font = New Font(dgvSttCompare.Font, FontStyle.Bold)
                ElseIf b.SpeedupVsFastest >= 0.5 Then
                    row.Cells("colSttSpeedup").Style.ForeColor = Color.DarkOrange
                Else
                    row.Cells("colSttSpeedup").Style.ForeColor = Color.Gray
                End If
            ElseIf b.Skipped Then
                row.DefaultCellStyle.ForeColor = Color.Gray
            ElseIf b.Failed Then
                row.DefaultCellStyle.ForeColor = Color.Red
            End If
        Next

        ' Summary text
        Dim completed = result.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).ToList()
        If completed.Count > 0 Then
            Dim fastest = completed.OrderBy(Function(b) b.AvgInferenceMs).First()
            lblSttProgress.ForeColor = Color.DarkGreen
            lblSttProgress.Text = $"Fastest: {fastest.BackendName} ({fastest.AvgInferenceMs}ms avg)  |  " &
                                   $"{completed.Count}/{result.Backends.Count} engines tested"
        Else
            lblSttProgress.ForeColor = Color.Red
            lblSttProgress.Text = "No engines could be tested. Check dependencies in Download Manager."
        End If

        ' Resource utilisation
        ShowSttResourceReport(result.Resources)
    End Sub

    Private Sub ShowSttResourceReport(report As ResourceReport)
        If report Is Nothing OrElse report.SampleCount = 0 Then
            lblSttResources.Text = ""
            Return
        End If

        Dim sb As New StringBuilder()
        sb.Append(report.ToSummaryText())

        If report.Warnings.Count > 0 Then
            lblSttResources.ForeColor = Color.OrangeRed
            sb.AppendLine()
            For Each warning In report.Warnings
                sb.AppendLine($"  WARNING: {warning}")
            Next
        Else
            lblSttResources.ForeColor = Color.Gray
        End If

        lblSttResources.Text = sb.ToString().TrimEnd()
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        If _lastResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            sb.AppendLine($"# Translation Model: {GetTranslationModelInfo()}")

            For Each stage In _lastResult.Stages
                sb.AppendLine($"# {stage.Stage} Stage")
                sb.AppendLine("total_ms,concurrency,total_requests,errors,avg_latency_ms,p50_ms,p95_ms,p99_ms,req_per_sec")
                sb.AppendLine($"{stage.TotalMs},{stage.Concurrency},{stage.TotalRequests},{stage.Errors}," &
                               $"{stage.AvgLatencyMs:F1},{stage.P50LatencyMs},{stage.P95LatencyMs}," &
                               $"{stage.P99LatencyMs},{stage.RequestsPerSec}")
                sb.AppendLine()

                sb.AppendLine("source_lang,target_lang,samples,avg_latency_ms,avg_quality,min_quality,max_quality")
                For Each ps In stage.PairSummaries
                    sb.AppendLine($"{ps.SourceLang},{ps.TargetLang},{ps.Count},{ps.AvgLatencyMs:F0},{ps.AvgQuality:F1},{ps.MinQuality:F1},{ps.MaxQuality:F1}")
                Next

                If stage.FinalQueueMetrics IsNot Nothing Then
                    sb.AppendLine()
                    sb.AppendLine("queue_metric,value")
                    Dim qm = stage.FinalQueueMetrics
                    sb.AppendLine($"total_enqueued,{qm.TotalEnqueued}")
                    sb.AppendLine($"total_completed,{qm.TotalCompleted}")
                    sb.AppendLine($"total_errors,{qm.TotalErrors}")
                    sb.AppendLine($"avg_wait_ms,{qm.AvgWaitMs:F1}")
                    sb.AppendLine($"max_wait_ms,{qm.MaxWaitMs}")
                End If

                sb.AppendLine()
            Next

            ' Resource utilisation section
            If _lastResult.Resources IsNot Nothing Then
                sb.Append(_lastResult.Resources.ToCsvSection())
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Concurrent Throughput Test
    ' ═══════════════════════════════════════════════════════════════

    Private Sub ConcBrowse_Click(sender As Object, e As EventArgs)
        Using dlg As New OpenFileDialog()
            dlg.Filter = "WAV files|*.wav|All files|*.*"
            dlg.Title = "Select test audio file (short 3-5s clip recommended)"
            If dlg.ShowDialog() = DialogResult.OK Then
                txtConcAudioFile.Text = dlg.FileName
            End If
        End Using
    End Sub

    Private Async Sub ConcRun_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtConcAudioFile.Text) OrElse Not File.Exists(txtConcAudioFile.Text) Then
            MessageBox.Show("Select a valid WAV audio file first.", "Concurrent Test",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Parse concurrency levels from text
        Dim levels As Integer()
        Try
            levels = txtConcLevels.Text.Split(","c).
                Select(Function(s) Integer.Parse(s.Trim())).
                Where(Function(n) n > 0).
                OrderBy(Function(n) n).
                ToArray()
        Catch
            MessageBox.Show("Invalid concurrency levels. Use comma-separated numbers (e.g. 1, 2, 5, 10).",
                            "Concurrent Test", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        If levels.Length = 0 Then
            MessageBox.Show("Enter at least one concurrency level.", "Concurrent Test",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnConcRun.Enabled = False
        btnConcCancel.Enabled = True
        btnConcExport.Enabled = False
        dgvConcurrent.Rows.Clear()
        lblConcSummary.Text = ""
        lblConcProgress.ForeColor = Color.Gray
        lblConcProgress.Text = "Starting..."

        AppLogger.Log(LogEvents.BENCH_START, $"STT Concurrency starting — audio: {txtConcAudioFile.Text}, " &
                      $"levels: [{String.Join(", ", levels)}], iterations: {nudConcIterations.Value}, " &
                      $"model: {GetSttModelInfo()}")

        Try
            Dim result = Await _concRunner.RunAsync(
                txtConcAudioFile.Text, _config, levels,
                CInt(nudConcIterations.Value), Threading.CancellationToken.None)
            _lastConcResult = result
            StampModelInfo(result.Resources, GetSttModelInfo())
            btnConcExport.Enabled = True
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"STT Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}, model: {GetSttModelInfo()}")
            ShowConcurrentResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "STT Concurrency cancelled by user")
            lblConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"STT Concurrency error: {ex.Message}")
            lblConcProgress.ForeColor = Color.Red
            lblConcProgress.Text = $"Error: {ex.Message}"
        Finally
            btnConcRun.Enabled = True
            btnConcCancel.Enabled = False
        End Try
    End Sub

    Private Sub ShowConcurrentResult(result As ConcurrencyTestResult)
        ' Ensure columns exist (safety net)
        EnsureConcurrentGridColumns()

        dgvConcurrent.Rows.Clear()

        If Not String.IsNullOrEmpty(result.ErrorMessage) Then
            lblConcProgress.ForeColor = Color.Red
            lblConcProgress.Text = $"Error: {result.ErrorMessage}"
            Return
        End If

        For Each lv In result.Levels
            Dim idx = dgvConcurrent.Rows.Add(
                lv.Concurrency,
                lv.TotalRequests,
                lv.WallTimeMs,
                lv.AvgLatencyMs,
                lv.P50LatencyMs,
                lv.P95LatencyMs,
                lv.MaxLatencyMs,
                $"{lv.InferencesPerSec:F1}",
                lv.Errors)

            Dim row = dgvConcurrent.Rows(idx)
            ' Color-code: green if avg < 2s, orange if < 5s, red if > 5s
            If lv.AvgLatencyMs < 2000 Then
                row.Cells("colConcAvg").Style.ForeColor = Color.DarkGreen
            ElseIf lv.AvgLatencyMs < 5000 Then
                row.Cells("colConcAvg").Style.ForeColor = Color.DarkOrange
            Else
                row.Cells("colConcAvg").Style.ForeColor = Color.Red
            End If
        Next

        ' Summary
        Dim sb As New StringBuilder()
        sb.Append($"Backend: {result.BackendName}  |  Model load: {(result.ModelLoadMs / 1000.0):F1}s")
        If result.Levels.Count > 0 Then
            Dim best = result.Levels.OrderByDescending(Function(l) l.InferencesPerSec).First()
            sb.Append($"  |  Peak throughput: {best.InferencesPerSec:F1} inf/s at {best.Concurrency} speakers")
            ' Find max usable concurrency (avg < 2s)
            Dim usable = result.Levels.Where(Function(l) l.AvgLatencyMs < 2000).OrderByDescending(Function(l) l.Concurrency).FirstOrDefault()
            If usable IsNot Nothing Then
                sb.Append($"  |  Max usable (<2s): {usable.Concurrency} speakers")
            End If
        End If
        lblConcSummary.Text = sb.ToString()
        lblConcProgress.ForeColor = Color.DarkGreen
        lblConcProgress.Text = "Concurrent throughput test complete."

        ' Resource utilisation
        ShowSttResourceReport(result.Resources)

        dgvConcurrent.Refresh()
    End Sub

    Private Sub ConcExport_Click(sender As Object, e As EventArgs)
        If _lastConcResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"stt_concurrent_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            sb.AppendLine($"# STT Concurrent Throughput — {_lastConcResult.BackendName}")
            sb.AppendLine($"# Model: {GetSttModelInfo()}")
            sb.AppendLine($"# Model load: {_lastConcResult.ModelLoadMs}ms")
            sb.AppendLine("speakers,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,inf_per_sec,errors")
            For Each lv In _lastConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next

            ' Resource utilisation section
            If _lastConcResult.Resources IsNot Nothing Then
                sb.AppendLine()
                sb.Append(_lastConcResult.Resources.ToCsvSection())
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Translation Concurrency Test
    ' ═══════════════════════════════════════════════════════════════

    Private Sub WireTransConcHandlers()
        AddHandler btnTransConcRun.Click, AddressOf TransConcRun_Click
        AddHandler btnTransConcCancel.Click, Sub(s, e) _transConcRunner.Cancel()
        AddHandler btnTransConcExport.Click, AddressOf TransConcExport_Click
        AddHandler _transConcRunner.ProgressChanged, Sub(s, msg)
                                                          If InvokeRequired Then
                                                              BeginInvoke(Sub() lblTransConcProgress.Text = msg)
                                                          Else
                                                              lblTransConcProgress.Text = msg
                                                          End If
                                                      End Sub
    End Sub

    Private Function GetTransConcTargets() As List(Of String)
        Dim targets As New List(Of String)()
        For Each item In clbTransConcTargets.CheckedItems
            targets.Add(item.ToString())
        Next
        Return targets
    End Function

    Private Async Sub TransConcRun_Click(sender As Object, e As EventArgs)
        If _translationService Is Nothing Then
            MessageBox.Show("No translation service available.", "Translation Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim targets = GetTransConcTargets()
        If targets.Count = 0 Then
            MessageBox.Show("Select at least one target language.", "Translation Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Parse concurrency levels
        Dim levels As Integer()
        Try
            levels = txtTransConcLevels.Text.Split(","c).
                Select(Function(s) Integer.Parse(s.Trim())).
                Where(Function(n) n > 0).
                OrderBy(Function(n) n).
                ToArray()
        Catch
            MessageBox.Show("Invalid concurrency levels. Use comma-separated numbers (e.g. 1, 2, 5, 10).",
                            "Translation Concurrency", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        If levels.Length = 0 Then
            MessageBox.Show("Enter at least one concurrency level.", "Translation Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnTransConcRun.Enabled = False
        btnTransConcCancel.Enabled = True
        btnTransConcExport.Enabled = False
        dgvTransConcurrent.Rows.Clear()
        lblTransConcSummary.Text = ""
        lblTransConcProgress.ForeColor = Color.Gray
        lblTransConcProgress.Text = "Starting..."

        AppLogger.Log(LogEvents.BENCH_START, $"Translation Concurrency starting — " &
                      $"{targets.Count} targets ({String.Join(", ", targets)}), " &
                      $"levels: [{String.Join(", ", levels)}], iterations: {nudTransConcIterations.Value}, " &
                      $"model: {GetTranslationModelInfo()}")

        Try
            Dim result = Await _transConcRunner.RunAsync(
                _translationService, _corpus, targets, levels,
                CInt(nudTransConcIterations.Value), Threading.CancellationToken.None)
            _lastTransConcResult = result
            StampModelInfo(result.Resources, GetTranslationModelInfo())
            btnTransConcExport.Enabled = True
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"Translation Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}, model: {GetTranslationModelInfo()}")
            ShowTransConcResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "Translation Concurrency cancelled by user")
            lblTransConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"Translation Concurrency error: {ex.Message}")
            lblTransConcProgress.ForeColor = Color.Red
            lblTransConcProgress.Text = $"Error: {ex.Message}"
        Finally
            btnTransConcRun.Enabled = True
            btnTransConcCancel.Enabled = False
        End Try
    End Sub

    Private Sub ShowTransConcResult(result As ConcurrencyTestResult)
        dgvTransConcurrent.Rows.Clear()

        If Not String.IsNullOrEmpty(result.ErrorMessage) Then
            lblTransConcProgress.ForeColor = Color.Red
            lblTransConcProgress.Text = $"Error: {result.ErrorMessage}"
            Return
        End If

        For Each lv In result.Levels
            dgvTransConcurrent.Rows.Add(
                lv.Concurrency,
                lv.TotalRequests,
                lv.WallTimeMs,
                lv.AvgLatencyMs,
                lv.P50LatencyMs,
                lv.P95LatencyMs,
                lv.MaxLatencyMs,
                $"{lv.InferencesPerSec:F1}",
                lv.Errors)
        Next

        ' Summary
        Dim sb As New StringBuilder()
        sb.Append($"Backend: {result.BackendName}")
        If result.Levels.Count > 0 Then
            Dim best = result.Levels.OrderByDescending(Function(l) l.InferencesPerSec).First()
            sb.Append($"  |  Peak throughput: {best.InferencesPerSec:F1} trans/s at {best.Concurrency} concurrent")
        End If
        lblTransConcSummary.Text = sb.ToString()
        lblTransConcProgress.ForeColor = Color.DarkGreen
        lblTransConcProgress.Text = "Concurrent translation test complete."

        ' Resource utilisation — show on Translation Resources tab
        ShowResourceReport(result.Resources)

        dgvTransConcurrent.Refresh()
    End Sub

    Private Sub TransConcExport_Click(sender As Object, e As EventArgs)
        If _lastTransConcResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"trans_concurrent_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            sb.AppendLine($"# Translation Concurrent Throughput — {_lastTransConcResult.BackendName}")
            sb.AppendLine($"# Model: {GetTranslationModelInfo()}")
            sb.AppendLine("concurrency,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,trans_per_sec,errors")
            For Each lv In _lastTransConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next

            ' Resource utilisation section
            If _lastTransConcResult.Resources IsNot Nothing Then
                sb.AppendLine()
                sb.Append(_lastTransConcResult.Resources.ToCsvSection())
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' TTS Engine Comparison
    ' ═══════════════════════════════════════════════════════════════

    Private Sub WireTtsHandlers()
        ' Populate backend checkboxes and concurrency combo
        For Each backend In _ttsBackends
            clbTtsBackends.Items.Add(backend.Name, True)
            cboTtsConcBackend.Items.Add(backend.Name)
        Next
        If cboTtsConcBackend.Items.Count > 0 Then cboTtsConcBackend.SelectedIndex = 0

        AddHandler btnTtsCompare.Click, AddressOf TtsCompare_Click
        AddHandler btnTtsCancel.Click, Sub(s, e) _ttsBenchRunner.Cancel()
        AddHandler btnTtsExport.Click, AddressOf TtsExport_Click
        AddHandler _ttsBenchRunner.ProgressChanged, Sub(s, msg)
                                                         If InvokeRequired Then
                                                             BeginInvoke(Sub() lblTtsProgress.Text = msg)
                                                         Else
                                                             lblTtsProgress.Text = msg
                                                         End If
                                                     End Sub

        ' Concurrency handlers
        EnsureTtsConcGridColumns()
        AddHandler btnTtsConcRun.Click, AddressOf TtsConcRun_Click
        AddHandler btnTtsConcCancel.Click, Sub(s, e) _ttsConcRunner.Cancel()
        AddHandler btnTtsConcExport.Click, AddressOf TtsConcExport_Click
        AddHandler _ttsConcRunner.ProgressChanged, Sub(s, msg)
                                                        If InvokeRequired Then
                                                            BeginInvoke(Sub() lblTtsConcProgress.Text = msg)
                                                        Else
                                                            lblTtsConcProgress.Text = msg
                                                        End If
                                                    End Sub
    End Sub

    Private Sub EnsureTtsConcGridColumns()
        If dgvTtsConcurrent.Columns.Count > 0 Then Return

        If dgvTtsConcurrent.Parent Is Nothing AndAlso tabTtsConcurrency IsNot Nothing Then
            tabTtsConcurrency.Controls.Add(dgvTtsConcurrent)
        End If

        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        dgvTtsConcurrent.Columns.AddRange(
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcLevel", .HeaderText = lp.GetString("Bm_ColConcurrent"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcRequests", .HeaderText = lp.GetString("Bm_ColRequests"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcWall", .HeaderText = lp.GetString("Bm_ColWallMs"), .FillWeight = 12},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcAvg", .HeaderText = lp.GetString("Bm_ColAvgMs"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcP50", .HeaderText = lp.GetString("Bm_ColP50Ms"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcP95", .HeaderText = lp.GetString("Bm_ColP95Ms"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcMax", .HeaderText = lp.GetString("Bm_ColMaxMs"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcThroughput", .HeaderText = lp.GetString("Bm_ColSynthPerSec"), .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcErrors", .HeaderText = lp.GetString("Bm_ColErrors"), .FillWeight = 11}
        )
    End Sub

    Private Async Sub TtsCompare_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtTtsText.Text) Then
            MessageBox.Show("Enter some test text to synthesise.", "TTS Comparison",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Build list of selected backends
        Dim selectedBackends As New List(Of ITtsBackend)()
        For Each backend In _ttsBackends
            Dim idx = clbTtsBackends.Items.IndexOf(backend.Name)
            If idx >= 0 AndAlso clbTtsBackends.GetItemChecked(idx) Then
                selectedBackends.Add(backend)
            End If
        Next

        If selectedBackends.Count = 0 Then
            MessageBox.Show("Select at least one TTS backend to test.", "TTS Comparison",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim language = cboTtsLanguage.SelectedItem?.ToString()
        If String.IsNullOrEmpty(language) Then language = "eng"

        btnTtsCompare.Enabled = False
        btnTtsCancel.Enabled = True
        btnTtsExport.Enabled = False
        dgvTtsCompare.Rows.Clear()
        lblTtsProgress.ForeColor = Color.Gray
        lblTtsProgress.Text = "Starting comparison..."

        AppLogger.Log(LogEvents.BENCH_START, $"TTS Comparison starting — " &
                      $"backends: [{String.Join(", ", selectedBackends.Select(Function(b) b.Name))}], " &
                      $"language: {language}, iterations: {nudTtsIterations.Value}, " &
                      $"text: ""{txtTtsText.Text.Substring(0, Math.Min(60, txtTtsText.Text.Length))}""")

        Try
            Dim result = Await _ttsBenchRunner.RunComparisonAsync(
                selectedBackends, txtTtsText.Text, language,
                CInt(nudTtsIterations.Value), Threading.CancellationToken.None)
            _lastTtsResult = result
            Dim ttsModels = String.Join(", ", result.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).Select(Function(b) b.BackendName))
            StampModelInfo(result.Resources, $"TTS engines: {ttsModels}")
            btnTtsExport.Enabled = True
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"TTS Comparison complete — {result.Backends.Count} backends tested ({ttsModels})")
            ShowTtsComparisonResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "TTS Comparison cancelled by user")
            lblTtsProgress.Text = "Comparison cancelled."
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"TTS Comparison error: {ex.Message}")
            lblTtsProgress.ForeColor = Color.Red
            lblTtsProgress.Text = $"Error: {ex.Message}"
        Finally
            btnTtsCompare.Enabled = True
            btnTtsCancel.Enabled = False
        End Try
    End Sub

    Private Sub ShowTtsComparisonResult(result As TtsBenchmarkResult)
        dgvTtsCompare.Rows.Clear()

        If Not String.IsNullOrEmpty(result.ErrorMessage) Then
            lblTtsProgress.ForeColor = Color.Red
            lblTtsProgress.Text = $"Error: {result.ErrorMessage}"
            Return
        End If

        For Each b In result.Backends
            Dim statusOrCodec As String
            Dim avgMs As String
            Dim minMs As String
            Dim maxMs As String
            Dim p95Ms As String
            Dim speedup As String
            Dim audioSize As String

            If b.Skipped Then
                statusOrCodec = b.SkipReason
                avgMs = "—" : minMs = "—" : maxMs = "—" : p95Ms = "—" : speedup = "—" : audioSize = "—"
            ElseIf b.Failed Then
                statusOrCodec = $"FAILED: {b.ErrorMessage}"
                avgMs = "—" : minMs = "—" : maxMs = "—" : p95Ms = "—" : speedup = "—" : audioSize = "—"
            Else
                statusOrCodec = $"{If(b.Codec, "?")} @ {b.SampleRate}Hz"
                avgMs = $"{b.AvgLatencyMs}"
                minMs = $"{b.MinLatencyMs}"
                maxMs = $"{b.MaxLatencyMs}"
                p95Ms = $"{b.P95LatencyMs}"
                speedup = If(b.SpeedupVsFastest > 0, $"{b.SpeedupVsFastest:F2}x", "—")
                audioSize = $"{b.AvgAudioBytes \ 1024}KB"
            End If

            Dim idx = dgvTtsCompare.Rows.Add(b.BackendName, avgMs, minMs, maxMs, p95Ms, speedup, audioSize, statusOrCodec)
            Dim row = dgvTtsCompare.Rows(idx)

            If Not b.Skipped AndAlso Not b.Failed Then
                If b.SpeedupVsFastest >= 1.0 Then
                    row.Cells("colTtsSpeedup").Style.ForeColor = Color.DarkGreen
                    row.Cells("colTtsSpeedup").Style.Font = New Font(dgvTtsCompare.Font, FontStyle.Bold)
                ElseIf b.SpeedupVsFastest >= 0.5 Then
                    row.Cells("colTtsSpeedup").Style.ForeColor = Color.DarkOrange
                Else
                    row.Cells("colTtsSpeedup").Style.ForeColor = Color.Gray
                End If
            ElseIf b.Skipped Then
                row.DefaultCellStyle.ForeColor = Color.Gray
            ElseIf b.Failed Then
                row.DefaultCellStyle.ForeColor = Color.Red
            End If
        Next

        ' Summary
        Dim completed = result.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).ToList()
        If completed.Count > 0 Then
            Dim fastest = completed.OrderBy(Function(b) b.AvgLatencyMs).First()
            lblTtsProgress.ForeColor = Color.DarkGreen
            lblTtsProgress.Text = $"Fastest: {fastest.BackendName} ({fastest.AvgLatencyMs}ms avg)  |  " &
                                   $"{completed.Count}/{result.Backends.Count} engines tested"
        Else
            lblTtsProgress.ForeColor = Color.Red
            lblTtsProgress.Text = "No engines could be tested. Check dependencies in Download Manager."
        End If

        ' Resource utilisation
        ShowTtsResourceReport(result.Resources)
    End Sub

    Private Sub ShowTtsResourceReport(report As ResourceReport)
        If report Is Nothing OrElse report.SampleCount = 0 Then
            lblTtsResources.Text = ""
            Return
        End If

        Dim sb As New StringBuilder()
        sb.Append(report.ToSummaryText())

        If report.Warnings.Count > 0 Then
            lblTtsResources.ForeColor = Color.OrangeRed
            sb.AppendLine()
            For Each warning In report.Warnings
                sb.AppendLine($"  WARNING: {warning}")
            Next
        Else
            lblTtsResources.ForeColor = Color.Gray
        End If

        lblTtsResources.Text = sb.ToString().TrimEnd()
    End Sub

    Private Sub TtsExport_Click(sender As Object, e As EventArgs)
        If _lastTtsResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"tts_benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            Dim ttsModels = String.Join(", ", _lastTtsResult.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).Select(Function(b) b.BackendName))
            sb.AppendLine($"# TTS Engine Comparison — engines: {ttsModels}")
            sb.AppendLine("Engine,Status,Avg (ms),Min (ms),Max (ms),P50 (ms),P95 (ms),Speedup,Avg Audio (bytes),Codec,Sample Rate,Iterations,Errors")

            For Each b In _lastTtsResult.Backends
                Dim status = If(b.Skipped, $"Skipped: {b.SkipReason}",
                              If(b.Failed, $"Failed: {b.ErrorMessage}", "OK"))
                sb.AppendLine($"""{b.BackendName}"",""{status}"",{b.AvgLatencyMs},{b.MinLatencyMs},{b.MaxLatencyMs},{b.P50LatencyMs},{b.P95LatencyMs},{b.SpeedupVsFastest:F2},{b.AvgAudioBytes},{If(b.Codec, "")},{b.SampleRate},{b.Iterations},{b.Errors}")
            Next

            If _lastTtsResult.Resources IsNot Nothing Then
                sb.AppendLine()
                sb.Append(_lastTtsResult.Resources.ToCsvSection())
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' TTS Concurrency Test
    ' ═══════════════════════════════════════════════════════════════

    Private Async Sub TtsConcRun_Click(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(txtTtsConcText.Text) Then
            MessageBox.Show("Enter some test text to synthesise.", "TTS Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Get selected backend
        Dim selectedName = cboTtsConcBackend.SelectedItem?.ToString()
        Dim backend = _ttsBackends.FirstOrDefault(Function(b) b.Name = selectedName)
        If backend Is Nothing Then
            MessageBox.Show("Select a TTS backend.", "TTS Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim language = cboTtsConcLanguage.SelectedItem?.ToString()
        If String.IsNullOrEmpty(language) Then language = "eng"

        ' Parse concurrency levels
        Dim levels As Integer()
        Try
            levels = txtTtsConcLevels.Text.Split(","c).
                Select(Function(s) Integer.Parse(s.Trim())).
                Where(Function(n) n > 0).
                OrderBy(Function(n) n).
                ToArray()
        Catch
            MessageBox.Show("Invalid concurrency levels. Use comma-separated numbers (e.g. 1, 2, 5, 10).",
                            "TTS Concurrency", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End Try

        If levels.Length = 0 Then
            MessageBox.Show("Enter at least one concurrency level.", "TTS Concurrency",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        btnTtsConcRun.Enabled = False
        btnTtsConcCancel.Enabled = True
        btnTtsConcExport.Enabled = False
        dgvTtsConcurrent.Rows.Clear()
        lblTtsConcSummary.Text = ""
        lblTtsConcProgress.ForeColor = Color.Gray
        lblTtsConcProgress.Text = "Starting..."

        AppLogger.Log(LogEvents.BENCH_START, $"TTS Concurrency starting — backend: {backend.Name}, " &
                      $"language: {language}, levels: [{String.Join(", ", levels)}], " &
                      $"iterations: {nudTtsConcIterations.Value}")

        Try
            Dim result = Await _ttsConcRunner.RunAsync(
                backend, txtTtsConcText.Text, language, levels,
                CInt(nudTtsConcIterations.Value), Threading.CancellationToken.None)
            _lastTtsConcResult = result
            StampModelInfo(result.Resources, $"TTS engine: {result.BackendName}")
            btnTtsConcExport.Enabled = True
            AppLogger.Log(LogEvents.BENCH_COMPLETE, $"TTS Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}")
            ShowTtsConcResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log(LogEvents.BENCH_COMPLETE, "TTS Concurrency cancelled by user")
            lblTtsConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"TTS Concurrency error: {ex.Message}")
            lblTtsConcProgress.ForeColor = Color.Red
            lblTtsConcProgress.Text = $"Error: {ex.Message}"
        Finally
            btnTtsConcRun.Enabled = True
            btnTtsConcCancel.Enabled = False
        End Try
    End Sub

    Private Sub ShowTtsConcResult(result As ConcurrencyTestResult)
        EnsureTtsConcGridColumns()
        dgvTtsConcurrent.Rows.Clear()

        If Not String.IsNullOrEmpty(result.ErrorMessage) Then
            lblTtsConcProgress.ForeColor = Color.Red
            lblTtsConcProgress.Text = $"Error: {result.ErrorMessage}"
            Return
        End If

        For Each lv In result.Levels
            Dim idx = dgvTtsConcurrent.Rows.Add(
                lv.Concurrency,
                lv.TotalRequests,
                lv.WallTimeMs,
                lv.AvgLatencyMs,
                lv.P50LatencyMs,
                lv.P95LatencyMs,
                lv.MaxLatencyMs,
                $"{lv.InferencesPerSec:F1}",
                lv.Errors)

            Dim row = dgvTtsConcurrent.Rows(idx)
            If lv.AvgLatencyMs < 2000 Then
                row.Cells("colTtsConcAvg").Style.ForeColor = Color.DarkGreen
            ElseIf lv.AvgLatencyMs < 5000 Then
                row.Cells("colTtsConcAvg").Style.ForeColor = Color.DarkOrange
            Else
                row.Cells("colTtsConcAvg").Style.ForeColor = Color.Red
            End If
        Next

        ' Summary
        Dim sb As New StringBuilder()
        sb.Append($"Backend: {result.BackendName}")
        If result.Levels.Count > 0 Then
            Dim best = result.Levels.OrderByDescending(Function(l) l.InferencesPerSec).First()
            sb.Append($"  |  Peak throughput: {best.InferencesPerSec:F1} synth/s at {best.Concurrency} concurrent")
        End If
        lblTtsConcSummary.Text = sb.ToString()
        lblTtsConcProgress.ForeColor = Color.DarkGreen
        lblTtsConcProgress.Text = "Concurrent TTS test complete."

        ' Resource utilisation
        ShowTtsResourceReport(result.Resources)

        dgvTtsConcurrent.Refresh()
    End Sub

    Private Sub TtsConcExport_Click(sender As Object, e As EventArgs)
        If _lastTtsConcResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"tts_concurrent_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()
            sb.AppendLine($"# TTS Concurrent Throughput — {_lastTtsConcResult.BackendName}")
            sb.AppendLine($"# Engine: {_lastTtsConcResult.BackendName}")
            sb.AppendLine("concurrency,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,synth_per_sec,errors")
            For Each lv In _lastTtsConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next

            If _lastTtsConcResult.Resources IsNot Nothing Then
                sb.AppendLine()
                sb.Append(_lastTtsConcResult.Resources.ToCsvSection())
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Model Identification Helpers
    ' ═══════════════════════════════════════════════════════════════

    Private Function GetTranslationModelInfo() As String
        Dim backend = If(_config.TranslationBackend, "nllb")
        Dim modelType = If(_config.TranslationModelType, "nllb")
        Dim modelPath = If(_config.TranslationModelPath, "")
        Dim modelFolder = If(String.IsNullOrEmpty(modelPath), "unknown", Path.GetFileName(modelPath.TrimEnd("\"c, "/"c)))
        Dim device = If(_config.TranslationDevice, "cpu")
        Return $"{backend} ({modelFolder}, {modelType}, {device})"
    End Function

    Private Function GetSttModelInfo() As String
        Dim backend = If(_config.SttBackend, "whisper-cpp-vulkan")
        Dim modelFile = If(String.IsNullOrEmpty(_config.PathWhisperCppModel), "unknown",
                          Path.GetFileName(_config.PathWhisperCppModel))
        Return $"{backend} ({modelFile})"
    End Function

    Private Sub StampModelInfo(report As ResourceReport, modelInfo As String)
        If report IsNot Nothing Then report.ModelInfo = modelInfo
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Unified Export & Auto-Save
    ' ═══════════════════════════════════════════════════════════════

    Private Function BuildUnifiedCsv() As String
        Dim sb As New StringBuilder()
        sb.AppendLine($"# EveryTongue Benchmark Report — {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
        sb.AppendLine()

        Dim hasAny = False

        ' ── Translation Pipeline ──
        If _lastResult IsNot Nothing AndAlso _lastResult.Stages.Count > 0 Then
            hasAny = True
            sb.AppendLine($"# Translation Model: {GetTranslationModelInfo()}")
            For Each stage In _lastResult.Stages
                sb.AppendLine($"## {stage.Stage} Pipeline")
                sb.AppendLine("total_ms,concurrency,total_requests,errors,avg_latency_ms,p50_ms,p95_ms,p99_ms,req_per_sec")
                sb.AppendLine($"{stage.TotalMs},{stage.Concurrency},{stage.TotalRequests},{stage.Errors}," &
                               $"{stage.AvgLatencyMs:F1},{stage.P50LatencyMs},{stage.P95LatencyMs}," &
                               $"{stage.P99LatencyMs},{stage.RequestsPerSec}")
                sb.AppendLine()
                sb.AppendLine("source_lang,target_lang,samples,avg_latency_ms,avg_quality,min_quality,max_quality")
                For Each ps In stage.PairSummaries
                    sb.AppendLine($"{ps.SourceLang},{ps.TargetLang},{ps.Count},{ps.AvgLatencyMs:F0},{ps.AvgQuality:F1},{ps.MinQuality:F1},{ps.MaxQuality:F1}")
                Next
                If stage.FinalQueueMetrics IsNot Nothing Then
                    sb.AppendLine()
                    sb.AppendLine("queue_metric,value")
                    Dim qm = stage.FinalQueueMetrics
                    sb.AppendLine($"total_enqueued,{qm.TotalEnqueued}")
                    sb.AppendLine($"total_completed,{qm.TotalCompleted}")
                    sb.AppendLine($"total_errors,{qm.TotalErrors}")
                    sb.AppendLine($"avg_wait_ms,{qm.AvgWaitMs:F1}")
                    sb.AppendLine($"max_wait_ms,{qm.MaxWaitMs}")
                End If
                sb.AppendLine()
            Next
            If _lastResult.Resources IsNot Nothing Then
                sb.AppendLine("## Translation Pipeline Resources")
                sb.Append(_lastResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── Translation Concurrency ──
        If _lastTransConcResult IsNot Nothing AndAlso _lastTransConcResult.Levels.Count > 0 Then
            hasAny = True
            sb.AppendLine($"## Translation Concurrency — {_lastTransConcResult.BackendName}")
            sb.AppendLine($"# Model: {GetTranslationModelInfo()}")
            sb.AppendLine("concurrency,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,trans_per_sec,errors")
            For Each lv In _lastTransConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next
            sb.AppendLine()
            If _lastTransConcResult.Resources IsNot Nothing Then
                sb.AppendLine("## Translation Concurrency Resources")
                sb.Append(_lastTransConcResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── STT Comparison ──
        If _lastSttResult IsNot Nothing AndAlso _lastSttResult.Backends.Count > 0 Then
            hasAny = True
            sb.AppendLine($"## STT Engine Comparison — {GetSttModelInfo()}")
            sb.AppendLine("engine,status,model_load_ms,avg_ms,min_ms,max_ms,iterations,speedup,transcription")
            For Each b In _lastSttResult.Backends
                Dim status = If(b.Skipped, $"Skipped: {b.SkipReason}",
                              If(b.Failed, $"Failed: {b.ErrorMessage}", "OK"))
                Dim text = If(b.TranscribedText, "").Replace("""", """""")
                sb.AppendLine($"""{b.BackendName}"",""{status}"",{b.ModelLoadMs},{b.AvgInferenceMs},{b.MinInferenceMs},{b.MaxInferenceMs},{b.Iterations},{b.SpeedupVsFastest:F2},""{text}""")
            Next
            sb.AppendLine()
            If _lastSttResult.Resources IsNot Nothing Then
                sb.AppendLine("## STT Comparison Resources")
                sb.Append(_lastSttResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── STT Concurrency ──
        If _lastConcResult IsNot Nothing AndAlso _lastConcResult.Levels.Count > 0 Then
            hasAny = True
            sb.AppendLine($"## STT Concurrency — {_lastConcResult.BackendName}")
            sb.AppendLine($"# Model: {GetSttModelInfo()}")
            sb.AppendLine($"# Model load: {_lastConcResult.ModelLoadMs}ms")
            sb.AppendLine("speakers,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,inf_per_sec,errors")
            For Each lv In _lastConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next
            sb.AppendLine()
            If _lastConcResult.Resources IsNot Nothing Then
                sb.AppendLine("## STT Concurrency Resources")
                sb.Append(_lastConcResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── TTS Comparison ──
        If _lastTtsResult IsNot Nothing AndAlso _lastTtsResult.Backends.Count > 0 Then
            hasAny = True
            Dim ttsEngines = String.Join(", ", _lastTtsResult.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).Select(Function(b) b.BackendName))
            sb.AppendLine($"## TTS Engine Comparison — engines: {ttsEngines}")
            sb.AppendLine("engine,status,avg_ms,min_ms,max_ms,p50_ms,p95_ms,speedup,avg_audio_bytes,codec,sample_rate,iterations,errors")
            For Each b In _lastTtsResult.Backends
                Dim status = If(b.Skipped, $"Skipped: {b.SkipReason}",
                              If(b.Failed, $"Failed: {b.ErrorMessage}", "OK"))
                sb.AppendLine($"""{b.BackendName}"",""{status}"",{b.AvgLatencyMs},{b.MinLatencyMs},{b.MaxLatencyMs},{b.P50LatencyMs},{b.P95LatencyMs},{b.SpeedupVsFastest:F2},{b.AvgAudioBytes},{If(b.Codec, "")},{b.SampleRate},{b.Iterations},{b.Errors}")
            Next
            sb.AppendLine()
            If _lastTtsResult.Resources IsNot Nothing Then
                sb.AppendLine("## TTS Comparison Resources")
                sb.Append(_lastTtsResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── TTS Concurrency ──
        If _lastTtsConcResult IsNot Nothing AndAlso _lastTtsConcResult.Levels.Count > 0 Then
            hasAny = True
            sb.AppendLine($"## TTS Concurrency — {_lastTtsConcResult.BackendName}")
            sb.AppendLine("concurrency,total_requests,wall_ms,avg_ms,p50_ms,p95_ms,max_ms,synth_per_sec,errors")
            For Each lv In _lastTtsConcResult.Levels
                sb.AppendLine($"{lv.Concurrency},{lv.TotalRequests},{lv.WallTimeMs},{lv.AvgLatencyMs},{lv.P50LatencyMs},{lv.P95LatencyMs},{lv.MaxLatencyMs},{lv.InferencesPerSec:F1},{lv.Errors}")
            Next
            sb.AppendLine()
            If _lastTtsConcResult.Resources IsNot Nothing Then
                sb.AppendLine("## TTS Concurrency Resources")
                sb.Append(_lastTtsConcResult.Resources.ToCsvSection())
                sb.AppendLine()
            End If
        End If

        ' ── Translation Pair Quality (FLORES chrF) — all runs this session ──
        If _abHistory.Count > 0 Then
            hasAny = True
            sb.AppendLine("## Translation Pair Quality (FLORES chrF)")
            sb.AppendLine(PairScoreCsvHeader)
            For Each r In _abHistory
                sb.AppendLine(PairScoreCsvLine(r))
            Next
            sb.AppendLine()
        End If

        If Not hasAny Then Return ""
        Return sb.ToString()
    End Function

    Private Const PairScoreCsvHeader As String =
        "timestamp,source,target,engine,sentences,direct_chrf,pivot_chrf,direct_avg_ms,pivot_avg_ms,mode,winner,qe_direct,qe_pivot"

    Private Shared Function PairScoreCsvLine(r As PairAbResult) As String
        Dim mode = If(r.PivotSkipped, "direct-only", "A/B")
        Dim winner = If(r.PivotSkipped, "", If(r.DirectWins, "direct", "pivot"))
        Dim pivotChrf = If(r.PivotSkipped, "", r.PivotChrF.ToString("F1", Globalization.CultureInfo.InvariantCulture))
        Dim pivotMs = If(r.PivotSkipped, "", r.PivotAvgMs.ToString("F0", Globalization.CultureInfo.InvariantCulture))
        Dim qeDirect = If(r.QeDirect >= 0, r.QeDirect.ToString("F3", Globalization.CultureInfo.InvariantCulture), "")
        Dim qePivot = If(r.QePivot >= 0, r.QePivot.ToString("F3", Globalization.CultureInfo.InvariantCulture), "")
        Return $"{r.RunAt:yyyy-MM-dd HH:mm:ss},{r.SourceLang},{r.TargetLang},{r.Engine},{r.SentenceCount}," &
               $"{r.DirectChrF.ToString("F1", Globalization.CultureInfo.InvariantCulture)},{pivotChrf}," &
               $"{r.DirectAvgMs.ToString("F0", Globalization.CultureInfo.InvariantCulture)},{pivotMs},{mode},{winner},{qeDirect},{qePivot}"
    End Function

    ''' <summary>
    ''' Append the run to the CUMULATIVE cross-session scoreboard
    ''' (config-dir\benchmarks\pair-scores.csv) — one growing file holding every
    ''' FLORES-scored run ever made on this machine, for calibration anchors and
    ''' drift tracking (cloud engines change over time; re-running a pair months
    ''' later against the same references catches regressions).
    ''' </summary>
    Private Sub AppendPairScoreHistory(r As PairAbResult)
        Try
            Dim benchDir = Path.Combine(Global.EveryTongue.Models.ConfigManager.ConfigDirectory, "benchmarks")
            If Not Directory.Exists(benchDir) Then Directory.CreateDirectory(benchDir)
            Dim filePath = Path.Combine(benchDir, "pair-scores.csv")
            ' Header changed (QE columns added)? Keep the old file intact under a
            ' versioned name and start fresh — never mix column layouts.
            If File.Exists(filePath) Then
                Dim firstLine = File.ReadLines(filePath).FirstOrDefault()
                If firstLine IsNot Nothing AndAlso firstLine <> PairScoreCsvHeader Then
                    File.Move(filePath, Path.Combine(benchDir, $"pair-scores-old-{DateTime.Now:yyyyMMdd_HHmmss}.csv"))
                End If
            End If
            If Not File.Exists(filePath) Then
                File.WriteAllText(filePath, PairScoreCsvHeader & Environment.NewLine, Encoding.UTF8)
            End If
            File.AppendAllText(filePath, PairScoreCsvLine(r) & Environment.NewLine, Encoding.UTF8)
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"pair-scores.csv append failed: {ex.Message}")
        End Try
    End Sub

    Private Sub ExportAll_Click(sender As Object, e As EventArgs)
        Dim csv = BuildUnifiedCsv()
        If String.IsNullOrEmpty(csv) Then
            MessageBox.Show("No benchmark results to export. Run at least one test first.",
                            "Export All", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"benchmark_all_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            File.WriteAllText(dlg.FileName, csv, Encoding.UTF8)
            AppLogger.Log(LogEvents.BENCH_RESULT, $"Export All saved to {dlg.FileName}")
            MessageBox.Show($"Exported to {dlg.FileName}", "Export All", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    Private Sub AutoSaveResults()
        Try
            Dim csv = BuildUnifiedCsv()
            If String.IsNullOrEmpty(csv) Then Return

            Dim benchDir = Path.Combine(Global.EveryTongue.Models.ConfigManager.ConfigDirectory, "benchmarks")
            If Not Directory.Exists(benchDir) Then Directory.CreateDirectory(benchDir)

            Dim filePath = Path.Combine(benchDir, $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv")
            File.WriteAllText(filePath, csv, Encoding.UTF8)

            lblAutoSaveStatus.ForeColor = Color.Gray
            lblAutoSaveStatus.Text = $"Auto-saved: {Path.GetFileName(filePath)}"
            AppLogger.Log(LogEvents.BENCH_RESULT, $"Auto-saved to {filePath}")
        Catch ex As Exception
            lblAutoSaveStatus.ForeColor = Color.OrangeRed
            lblAutoSaveStatus.Text = $"Auto-save failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"Auto-save error: {ex.Message}")
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Pair A/B (FLORES) — direct vs English-pivot, scored with chrF
    ' ═══════════════════════════════════════════════════════════════
    Private _abCts As Threading.CancellationTokenSource
    Private _abResult As PairAbResult
    Private ReadOnly _abRunner As New PairQualityRunner()
    Private _abLangCodes As New List(Of String)
    ''' <summary>All Pair A/B runs this session — exported by BuildUnifiedCsv.</summary>
    Private ReadOnly _abHistory As New List(Of PairAbResult)

    ' ── CometKiwi QE (optional — scores appear when installed) ──
    Private _qeService As Pipeline.QeService
    Private _qeInstalled As Boolean?

    Private Function QeInstalled() As Boolean
        If Not _qeInstalled.HasValue Then _qeInstalled = Pipeline.QeService.CheckInstalled()
        Return _qeInstalled.Value
    End Function

    ''' <summary>The QE sidecar is benchmark-scoped — stop it with the form.</summary>
    Private Sub FormTranslationBenchmark_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        Try
            _qeService?.Stop()
            _qeService?.Dispose()
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"QE sidecar shutdown: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Score the run's outputs with CometKiwi (reference-free, cross-pair
    ''' comparable). Starts the QE sidecar + loads the model on first use.
    ''' Failures degrade gracefully — the run keeps its chrF numbers.
    ''' </summary>
    Private Async Function ScoreWithQeAsync(r As PairAbResult) As Task
        Try
            If _qeService Is Nothing Then _qeService = New Pipeline.QeService()
            If Not _qeService.IsRunning Then
                lblAbProgress.Text = "Starting CometKiwi QE engine..."
                _qeService.Start(_config.QeServerPort)
            End If
            lblAbProgress.Text = "Loading CometKiwi model (first load ~1 min)..."
            Dim sw = Diagnostics.Stopwatch.StartNew()
            Dim loaded = False
            While sw.Elapsed < TimeSpan.FromMinutes(3)
                If _abCts.IsCancellationRequested Then Return
                loaded = Await _qeService.EnsureModelLoadedAsync(_abCts.Token)
                If loaded Then Exit While
                Await Task.Delay(2000)
            End While
            If Not loaded Then
                lblAbProgress.Text = "CometKiwi model did not load — QE skipped (see qe-server.log)."
                Return
            End If

            lblAbProgress.Text = $"Scoring {r.Sources.Count} sentences with CometKiwi (CPU — can take a few minutes)..."
            Dim directScores = Await _qeService.ScoreAsync(r.Sources, r.DirectOutputs, _abCts.Token)
            If directScores IsNot Nothing AndAlso directScores.Count > 0 Then
                r.QeDirect = directScores.Average()
            End If
            If Not r.PivotSkipped Then
                Dim pivotScores = Await _qeService.ScoreAsync(r.Sources, r.PivotOutputs, _abCts.Token)
                If pivotScores IsNot Nothing AndAlso pivotScores.Count > 0 Then
                    r.QePivot = pivotScores.Average()
                End If
            End If
        Catch ex As OperationCanceledException
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"QE scoring failed: {ex.Message}")
            lblAbProgress.Text = $"QE scoring failed: {ex.Message} (chrF results unaffected)"
        End Try
    End Function

    Private Sub InitPairAb()
        AddHandler btnAbDownload.Click, AddressOf AbDownload_Click
        AddHandler btnAbRun.Click, AddressOf AbRun_Click
        AddHandler btnAbCancel.Click, Sub() _abCts?.Cancel()
        AddHandler btnAbSave.Click, AddressOf AbSave_Click
        ' Availability changes while the form is open (e.g. the NLLB sidecar
        ' finishes starting) — re-check whenever the user opens the list.
        AddHandler cboAbEngine.DropDown, Sub() PopulateAbEngines()
        RefreshAbDatasetState()
    End Sub

    Private Sub RefreshAbDatasetState()
        PopulateAbEngines()
        If FloresDataset.IsInstalled() Then
            _abLangCodes = FloresDataset.AvailableLanguages()
            lblAbInfo.Text = $"FLORES-200: {_abLangCodes.Count} languages installed."
            btnAbDownload.Enabled = False
            PopulateAbCombos()
            btnAbRun.Enabled = _translationService IsNot Nothing
        Else
            lblAbInfo.Text = "FLORES-200 reference set not installed — professional reference translations for scoring pairs."
            btnAbDownload.Enabled = True
            btnAbRun.Enabled = False
        End If
    End Sub

    Private _abEngineNames As New List(Of String)

    ''' <summary>
    ''' Engine combo: registry-declared backends (deduped by orchestrator backend
    ''' name, inline engines skipped) so Local/NLLB is listed even BEFORE its
    ''' sidecar starts — the sidecar backend only registers on the orchestrator
    ''' at sidecar startup, so building this list from GetAllBackends() alone
    ''' hid it. Live availability is overlaid from the orchestrator, and any
    ''' dynamically-registered backend the registry doesn't know is appended.
    ''' Each run is forced through the selection via backendOverride. Unavailable
    ''' backends are listed but block the run with a hint.
    ''' </summary>
    Private Sub PopulateAbEngines()
        Dim keep = If(cboAbEngine.SelectedIndex >= 0 AndAlso cboAbEngine.SelectedIndex < _abEngineNames.Count,
                      _abEngineNames(cboAbEngine.SelectedIndex), Nothing)
        cboAbEngine.BeginUpdate()
        cboAbEngine.Items.Clear()
        _abEngineNames.Clear()

        Dim live = If(_translationService?.GetAllBackends(),
                      DirectCast(New List(Of Services.Models.BackendInfo)(), IReadOnlyList(Of Services.Models.BackendInfo)))
        Dim names As New List(Of String)
        For Each entry In Services.Translation.TranslationBackendRegistry.GetAll()
            If Not String.IsNullOrEmpty(entry.InlineWithStt) OrElse String.IsNullOrEmpty(entry.BackendName) Then Continue For
            If Not names.Contains(entry.BackendName, StringComparer.OrdinalIgnoreCase) Then names.Add(entry.BackendName)
        Next
        For Each b In live
            If Not names.Contains(b.Name, StringComparer.OrdinalIgnoreCase) Then names.Add(b.Name)
        Next

        Dim selectIdx = 0
        For Each backendName In names
            Dim n = backendName
            Dim info = live.FirstOrDefault(Function(x) x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
            If keep IsNot Nothing AndAlso n.Equals(keep, StringComparison.OrdinalIgnoreCase) Then
                selectIdx = _abEngineNames.Count
            ElseIf keep Is Nothing AndAlso info IsNot Nothing AndAlso info.IsActive Then
                selectIdx = _abEngineNames.Count
            End If
            _abEngineNames.Add(n)
            cboAbEngine.Items.Add(n & If(info IsNot Nothing AndAlso info.IsAvailable, "", "  (not available)"))
        Next
        cboAbEngine.EndUpdate()
        If cboAbEngine.Items.Count > 0 Then cboAbEngine.SelectedIndex = selectIdx
    End Sub

    Private Function SelectedAbEngine() As String
        If cboAbEngine.SelectedIndex < 0 OrElse cboAbEngine.SelectedIndex >= _abEngineNames.Count Then Return Nothing
        Return _abEngineNames(cboAbEngine.SelectedIndex)
    End Function

    Private Sub PopulateAbCombos()
        ' Display names come from the canonical table; codes without an entry
        ' show as the raw FLORES code.
        Dim names As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each lang In LanguageCodeService.Instance.GetAllLanguagesSorted()
            If Not String.IsNullOrEmpty(lang.Flores) Then names(lang.Flores) = lang.Name
        Next
        cboAbSource.BeginUpdate() : cboAbTarget.BeginUpdate()
        cboAbSource.Items.Clear() : cboAbTarget.Items.Clear()
        For Each code In _abLangCodes
            Dim label = If(names.ContainsKey(code), $"{names(code)} ({code})", code)
            cboAbSource.Items.Add(label)
            cboAbTarget.Items.Add(label)
        Next
        cboAbSource.EndUpdate() : cboAbTarget.EndUpdate()
        ' Soft default to the field-motivating pair when present (GLS: cat→swe).
        Dim srcIdx = _abLangCodes.FindIndex(Function(c) c.Equals("cat_Latn", StringComparison.OrdinalIgnoreCase))
        Dim tgtIdx = _abLangCodes.FindIndex(Function(c) c.Equals("swe_Latn", StringComparison.OrdinalIgnoreCase))
        If cboAbSource.Items.Count > 0 Then cboAbSource.SelectedIndex = Math.Max(0, srcIdx)
        If cboAbTarget.Items.Count > 0 Then cboAbTarget.SelectedIndex = Math.Max(0, tgtIdx)
    End Sub

    Private Async Sub AbDownload_Click(sender As Object, e As EventArgs)
        btnAbDownload.Enabled = False
        _abCts = New Threading.CancellationTokenSource()
        btnAbCancel.Enabled = True
        Try
            Await FloresDataset.DownloadAsync(
                Sub(msg) Me.BeginInvoke(Sub() lblAbProgress.Text = msg),
                _abCts.Token)
            lblAbProgress.Text = "FLORES-200 installed."
        Catch ex As Exception
            lblAbProgress.Text = $"Download failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"FLORES download failed: {ex.Message}")
        Finally
            btnAbCancel.Enabled = False
            RefreshAbDatasetState()
        End Try
    End Sub

    Private Async Sub AbRun_Click(sender As Object, e As EventArgs)
        If _translationService Is Nothing Then Return
        If cboAbSource.SelectedIndex < 0 OrElse cboAbTarget.SelectedIndex < 0 Then Return
        Dim engineName = SelectedAbEngine()
        If engineName Is Nothing Then Return
        Dim engineInfo = _translationService.GetAllBackends().
            FirstOrDefault(Function(b) b.Name.Equals(engineName, StringComparison.OrdinalIgnoreCase))
        If engineInfo Is Nothing OrElse Not engineInfo.IsAvailable Then
            ' Offline engine (per registry — an unregistered sidecar has no live
            ' BackendInfo): start it ourselves and wait for the model, so picking
            ' it from the list Just Works. Cloud engines can only mean missing key.
            Dim offline = If(engineInfo IsNot Nothing,
                             Not engineInfo.RequiresInternet,
                             Not If(Services.Translation.TranslationBackendRegistry.FindByBackendName(engineName)?.RequiresInternet, True))
            If offline AndAlso _startLocalEngine IsNot Nothing Then
                If Not Await StartLocalEngineAndWait(engineName) Then Return
            Else
                lblAbProgress.Text = $"Engine '{engineName}' is not available — configure its API key in Options."
                Return
            End If
        End If
        Dim src = _abLangCodes(cboAbSource.SelectedIndex)
        Dim tgt = _abLangCodes(cboAbTarget.SelectedIndex)
        Dim pivotLang = If(_config.TranslationPivotLanguage, "eng_Latn")
        If src.Equals(tgt, StringComparison.OrdinalIgnoreCase) Then
            lblAbProgress.Text = "Source and target must differ."
            Return
        End If

        Dim count = CInt(nudAbCount.Value)
        btnAbRun.Enabled = False : btnAbSave.Enabled = False : btnAbCancel.Enabled = True
        progressAb.Value = 0 : progressAb.Maximum = count
        txtAbResults.Text = ""
        _abCts = New Threading.CancellationTokenSource()
        Try
            _abResult = Await _abRunner.RunAsync(
                _translationService, engineName, src, tgt, pivotLang, count,
                Sub(done, total) Me.BeginInvoke(Sub()
                                                    progressAb.Value = Math.Min(done, progressAb.Maximum)
                                                    lblAbProgress.Text = $"{done}/{total} sentences (each = 3 translations)"
                                                End Sub),
                _abCts.Token)
            If QeInstalled() Then Await ScoreWithQeAsync(_abResult)
            ShowAbResult(_abResult)
            btnAbSave.Enabled = _abResult.DirectWins
            _abHistory.Add(_abResult)
            AppendPairScoreHistory(_abResult)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            lblAbProgress.Text = "Cancelled."
        Catch ex As Exception
            lblAbProgress.Text = $"A/B failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"Pair A/B failed: {ex.Message}")
        Finally
            btnAbRun.Enabled = True : btnAbCancel.Enabled = False
        End Try
    End Sub

    Private Sub ShowAbResult(r As PairAbResult)
        Dim sb As New StringBuilder()
        If r.PivotSkipped Then
            ' Direct-only engine-quality score: the pair includes the pivot
            ' language, so no pivot route exists (such pairs never pivot in
            ' production either).
            sb.AppendLine($"{r.SourceLang} → {r.TargetLang}  on {r.Engine}, {r.SentenceCount} sentences (engine quality vs FLORES reference)")
            sb.AppendLine()
            sb.AppendLine($"  chrF {r.DirectChrF,6:F1}   avg {r.DirectAvgMs,6:F0} ms")
            If r.QeDirect >= 0 Then
                sb.AppendLine($"  QE   {r.QeDirect,6:F3}   (CometKiwi, ~0-1 — comparable ACROSS pairs)")
            End If
            sb.AppendLine()
            sb.AppendLine($"  Direct-only run — the pair includes the pivot language ({r.PivotLang}), so there is no pivot route to compare.")
        Else
            sb.AppendLine($"{r.SourceLang} → {r.TargetLang}  (via {r.PivotLang})  on {r.Engine}, {r.SentenceCount} sentences")
            sb.AppendLine()
            sb.AppendLine($"  DIRECT : chrF {r.DirectChrF,6:F1}   avg {r.DirectAvgMs,6:F0} ms" &
                          If(r.QeDirect >= 0, $"   QE {r.QeDirect:F3}", ""))
            sb.AppendLine($"  PIVOT  : chrF {r.PivotChrF,6:F1}   avg {r.PivotAvgMs,6:F0} ms" &
                          If(r.QePivot >= 0, $"   QE {r.QePivot:F3}", ""))
            If r.QeDirect >= 0 Then
                sb.AppendLine($"  (QE = CometKiwi, ~0-1, reference-free — comparable ACROSS pairs)")
            End If
            sb.AppendLine()
            Dim delta = Math.Abs(r.DirectChrF - r.PivotChrF)
            sb.AppendLine(If(r.DirectWins,
                $"  → DIRECT wins by {delta:F1} chrF. 'Save as measured direct pair' records this for the {r.Engine} engine.",
                $"  → PIVOT wins by {delta:F1} chrF. No entry needed — pivoting is the policy's default for this pair."))
        End If
        sb.AppendLine()
        sb.AppendLine("── Examples ──")
        For Each ex In r.Examples
            sb.AppendLine($"src: {ex.Source}")
            sb.AppendLine($"ref: {ex.Reference}")
            sb.AppendLine($"dir: {ex.Direct}")
            If Not r.PivotSkipped Then sb.AppendLine($"piv: {ex.Pivot}")
            sb.AppendLine()
        Next
        txtAbResults.Text = sb.ToString()
        lblAbProgress.Text = "Done."
    End Sub

    ''' <summary>
    ''' Kick the local sidecar via the head callback and poll the orchestrator
    ''' until the backend reports available (model loaded) — up to 3 minutes,
    ''' cancellable. Returns True when the engine is ready to benchmark.
    ''' </summary>
    Private Async Function StartLocalEngineAndWait(engineName As String) As Task(Of Boolean)
        btnAbRun.Enabled = False : btnAbCancel.Enabled = True
        _abCts = New Threading.CancellationTokenSource()
        Try
            lblAbProgress.Text = $"Starting '{engineName}' — launching the translation engine..."
            _startLocalEngine()
            Dim sw = Diagnostics.Stopwatch.StartNew()
            While sw.Elapsed < TimeSpan.FromMinutes(3)
                If _abCts.IsCancellationRequested Then
                    lblAbProgress.Text = "Cancelled."
                    Return False
                End If
                Dim info = _translationService.GetAllBackends().
                    FirstOrDefault(Function(b) b.Name.Equals(engineName, StringComparison.OrdinalIgnoreCase))
                If info IsNot Nothing AndAlso info.IsAvailable Then
                    PopulateAbEngines()
                    lblAbProgress.Text = $"'{engineName}' ready."
                    Return True
                End If
                lblAbProgress.Text = $"Starting '{engineName}' — loading the translation model ({sw.Elapsed.TotalSeconds:F0}s, first load can take a minute or two)..."
                Await Task.Delay(2000)
            End While
            lblAbProgress.Text = $"'{engineName}' did not become ready in 3 minutes — check the log (Translation category) for model-load errors, or install the engine via the Download Manager."
            Return False
        Finally
            btnAbRun.Enabled = True : btnAbCancel.Enabled = False
        End Try
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' STT Quality (FLEURS) — WER/CER per engine vs native-speaker refs
    ' ═══════════════════════════════════════════════════════════════
    Private _flCts As Threading.CancellationTokenSource
    Private ReadOnly _flRunner As New FleursSttRunner()
    Private _flEngineKeys As New List(Of String)

    Private Sub InitFleurs()
        AddHandler btnFlDownload.Click, AddressOf FlDownload_Click
        AddHandler btnFlRun.Click, AddressOf FlRun_Click
        AddHandler btnFlCancel.Click, Sub() _flCts?.Cancel()
        RefreshFleursState()
    End Sub

    Private Sub RefreshFleursState()
        cboFlLang.Items.Clear()
        For Each cfg In FleursDataset.InstalledConfigs()
            cboFlLang.Items.Add(cfg)
        Next
        If cboFlLang.Items.Count > 0 Then cboFlLang.SelectedIndex = 0
        cboFlEngine.Items.Clear()
        _flEngineKeys.Clear()
        For Each entry In Services.Stt.SttBackendRegistry.GetAll()
            _flEngineKeys.Add(entry.Key)
            cboFlEngine.Items.Add(entry.DisplayName)
        Next
        Dim cfgIdx = _flEngineKeys.FindIndex(Function(k) k.Equals(If(_config.SttBackend, ""), StringComparison.OrdinalIgnoreCase))
        If cboFlEngine.Items.Count > 0 Then cboFlEngine.SelectedIndex = Math.Max(0, cfgIdx)
        btnFlRun.Enabled = cboFlLang.Items.Count > 0
    End Sub

    Private Async Sub FlDownload_Click(sender As Object, e As EventArgs)
        Try
            lblFlProgress.Text = "Fetching FLEURS language list..."
            _flCts = New Threading.CancellationTokenSource()
            Dim configs = Await FleursDataset.ListRemoteConfigsAsync(_flCts.Token)
            Dim prompt = $"FLEURS config to download (e.g. ca_es, es_419, en_us, sv_se, sq_al)." & vbCrLf &
                         $"{configs.Count} available: {String.Join(", ", configs.Take(40))}..."
            Dim cfg = InputBox(prompt, "Download FLEURS language", "ca_es").Trim()
            If cfg = "" Then lblFlProgress.Text = "" : Return
            If Not configs.Contains(cfg, StringComparer.OrdinalIgnoreCase) Then
                lblFlProgress.Text = $"'{cfg}' is not a FLEURS config."
                Return
            End If
            btnFlDownload.Enabled = False : btnFlCancel.Enabled = True
            Await FleursDataset.DownloadAsync(cfg,
                Sub(msg) Me.BeginInvoke(Sub() lblFlProgress.Text = msg), _flCts.Token)
            lblFlProgress.Text = $"{cfg} installed."
        Catch ex As Exception
            lblFlProgress.Text = $"Download failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"FLEURS download: {ex.Message}")
        Finally
            btnFlDownload.Enabled = True : btnFlCancel.Enabled = False
            RefreshFleursState()
        End Try
    End Sub

    Private Async Sub FlRun_Click(sender As Object, e As EventArgs)
        If cboFlLang.SelectedIndex < 0 OrElse cboFlEngine.SelectedIndex < 0 Then Return
        Dim cfg = cboFlLang.SelectedItem.ToString()
        Dim engineKey = _flEngineKeys(cboFlEngine.SelectedIndex)
        Dim count = CInt(nudFlCount.Value)
        btnFlRun.Enabled = False : btnFlCancel.Enabled = True
        progressFl.Value = 0 : progressFl.Maximum = count
        txtFlResults.Text = ""
        _flCts = New Threading.CancellationTokenSource()
        Try
            Dim r = Await _flRunner.RunAsync(_config, cfg, engineKey, count,
                Sub(msg, done, total) Me.BeginInvoke(Sub()
                                                         lblFlProgress.Text = msg
                                                         progressFl.Maximum = Math.Max(1, total)
                                                         progressFl.Value = Math.Min(done, progressFl.Maximum)
                                                     End Sub),
                _flCts.Token)
            Dim sb As New StringBuilder()
            sb.AppendLine($"{r.Config}  on {r.EngineKey}, {r.ClipCount} clips (WER/CER vs FLEURS reference)")
            sb.AppendLine()
            sb.AppendLine($"  WER {r.Wer,6:F1}%   CER {r.Cer,6:F1}%   avg {r.AvgMs,6:F0} ms" &
                          If(r.FailedClips > 0, $"   ({r.FailedClips} clips FAILED)", ""))
            If Not String.IsNullOrEmpty(r.FirstError) Then
                sb.AppendLine($"  First failure: {r.FirstError}")
            End If
            sb.AppendLine()
            sb.AppendLine("  Rough guide: <10% WER excellent, 10-20% usable, >25% painful.")
            sb.AppendLine("  Compare engines on the SAME language with WER; across languages prefer CER.")
            sb.AppendLine()
            sb.AppendLine("── Examples ──")
            For Each ex2 In r.Examples
                sb.AppendLine($"ref: {ex2.Ref}")
                sb.AppendLine($"hyp: {ex2.Hyp}")
                sb.AppendLine()
            Next
            txtFlResults.Text = sb.ToString()
            lblFlProgress.Text = "Done."
            AppendSttScoreHistory(r)
        Catch ex As OperationCanceledException
            lblFlProgress.Text = "Cancelled."
        Catch ex As Exception
            lblFlProgress.Text = $"Run failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"FLEURS run: {ex.Message}")
        Finally
            btnFlRun.Enabled = True : btnFlCancel.Enabled = False
        End Try
    End Sub

    Private Const SttScoreCsvHeader As String =
        "timestamp,fleurs_config,engine,clips,wer,cer,avg_ms,failed_clips"

    ''' <summary>Cumulative cross-session STT scoreboard, sibling of pair-scores.csv.</summary>
    Private Sub AppendSttScoreHistory(r As FleursResult)
        Try
            Dim benchDir = Path.Combine(Global.EveryTongue.Models.ConfigManager.ConfigDirectory, "benchmarks")
            If Not Directory.Exists(benchDir) Then Directory.CreateDirectory(benchDir)
            Dim filePath = Path.Combine(benchDir, "stt-scores.csv")
            If Not File.Exists(filePath) Then
                File.WriteAllText(filePath, SttScoreCsvHeader & Environment.NewLine, Encoding.UTF8)
            End If
            Dim inv = Globalization.CultureInfo.InvariantCulture
            File.AppendAllText(filePath,
                $"{r.RunAt:yyyy-MM-dd HH:mm:ss},{r.Config},{r.EngineKey},{r.ClipCount}," &
                $"{r.Wer.ToString("F1", inv)},{r.Cer.ToString("F1", inv)},{r.AvgMs.ToString("F0", inv)},{r.FailedClips}" &
                Environment.NewLine, Encoding.UTF8)
        Catch ex As Exception
            AppLogger.Log(LogEvents.BENCH_ERROR, $"stt-scores.csv append failed: {ex.Message}")
        End Try
    End Sub

    Private Sub AbSave_Click(sender As Object, e As EventArgs)
        If _abResult Is Nothing OrElse Not _abResult.DirectWins Then Return
        Try
            PairQualityRunner.SaveMeasuredEntry(_abResult)
            btnAbSave.Enabled = False
            lblAbProgress.Text = "Measured entry saved to translation-direct-pairs.local.json — applies on next server start."
        Catch ex As Exception
            lblAbProgress.Text = $"Save failed: {ex.Message}"
            AppLogger.Log(LogEvents.BENCH_ERROR, $"Measured-pair save failed: {ex.Message}")
        End Try
    End Sub

End Class
