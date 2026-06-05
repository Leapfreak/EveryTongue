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

    Public Sub New(translationService As ITranslationService,
                   Optional ttsService As ITtsService = Nothing,
                   Optional liveServerPort As Integer = 0,
                   Optional config As AppConfig = Nothing,
                   Optional ttsBackends As IEnumerable(Of ITtsBackend) = Nothing)
        InitializeComponent()
        _translationService = translationService
        _ttsService = ttsService
        _ttsBackends = If(ttsBackends, Enumerable.Empty(Of ITtsBackend)())
        _liveServerPort = liveServerPort
        _config = If(config, New AppConfig())
    End Sub

    Private Sub FormTranslationBenchmark_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = Owner?.Icon
        _corpus = TranslationBenchmarkRunner.LoadCorpus()
        AppLogger.Log($"[BENCHMARK] Form loaded — corpus: {_corpus.Count} entries, " &
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

        AppLogger.Log($"[BENCHMARK] Translation Pipeline starting — {filtered.Count} sentences, " &
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
            AppLogger.Log($"[BENCHMARK] Translation Pipeline complete — {result.Stages.Count} stages, model: {GetTranslationModelInfo()}")
            ShowResult(result)
            AutoSaveResults()

        Catch ex As OperationCanceledException
            result.Resources = monitor.Stop()
            AppLogger.Log("[BENCHMARK] Translation Pipeline cancelled by user")
            lblSummary.Text = "Benchmark cancelled."
            lblProgress.Text = ""
        Catch ex As Exception
            result.Resources = monitor.Stop()
            AppLogger.Log($"[BENCHMARK] Translation Pipeline error: {ex.Message}")
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

        dgvConcurrent.Columns.AddRange(
            New DataGridViewTextBoxColumn() With {.Name = "colConcLevel", .HeaderText = "Speakers", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcRequests", .HeaderText = "Requests", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcWall", .HeaderText = "Wall (ms)", .FillWeight = 12},
            New DataGridViewTextBoxColumn() With {.Name = "colConcAvg", .HeaderText = "Avg (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcP50", .HeaderText = "P50 (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcP95", .HeaderText = "P95 (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcMax", .HeaderText = "Max (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcThroughput", .HeaderText = "Inf/sec", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colConcErrors", .HeaderText = "Errors", .FillWeight = 11}
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

        AppLogger.Log($"[BENCHMARK] STT Comparison starting — audio: {txtSttAudioFile.Text}, " &
                      $"iterations: {nudSttIterations.Value}, engines: {String.Join(", ", enabled)}, " &
                      $"model: {GetSttModelInfo()}")

        Try
            Dim result = Await _sttComparer.RunComparisonAsync(
                txtSttAudioFile.Text, _config, _liveServerPort,
                CInt(nudSttIterations.Value), Threading.CancellationToken.None, enabled)
            _lastSttResult = result
            StampModelInfo(result.Resources, GetSttModelInfo())
            btnSttExport.Enabled = True
            AppLogger.Log($"[BENCHMARK] STT Comparison complete — {result.Backends.Count} backends tested, model: {GetSttModelInfo()}")
            ShowSttComparisonResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log("[BENCHMARK] STT Comparison cancelled by user")
            lblSttProgress.Text = "Comparison cancelled."
        Catch ex As Exception
            AppLogger.Log($"[BENCHMARK] STT Comparison error: {ex.Message}")
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

        AppLogger.Log($"[BENCHMARK] STT Concurrency starting — audio: {txtConcAudioFile.Text}, " &
                      $"levels: [{String.Join(", ", levels)}], iterations: {nudConcIterations.Value}, " &
                      $"model: {GetSttModelInfo()}")

        Try
            Dim result = Await _concRunner.RunAsync(
                txtConcAudioFile.Text, _config, levels,
                CInt(nudConcIterations.Value), Threading.CancellationToken.None)
            _lastConcResult = result
            StampModelInfo(result.Resources, GetSttModelInfo())
            btnConcExport.Enabled = True
            AppLogger.Log($"[BENCHMARK] STT Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}, model: {GetSttModelInfo()}")
            ShowConcurrentResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log("[BENCHMARK] STT Concurrency cancelled by user")
            lblConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log($"[BENCHMARK] STT Concurrency error: {ex.Message}")
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

        AppLogger.Log($"[BENCHMARK] Translation Concurrency starting — " &
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
            AppLogger.Log($"[BENCHMARK] Translation Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}, model: {GetTranslationModelInfo()}")
            ShowTransConcResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log("[BENCHMARK] Translation Concurrency cancelled by user")
            lblTransConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log($"[BENCHMARK] Translation Concurrency error: {ex.Message}")
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

        dgvTtsConcurrent.Columns.AddRange(
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcLevel", .HeaderText = "Concurrent", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcRequests", .HeaderText = "Requests", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcWall", .HeaderText = "Wall (ms)", .FillWeight = 12},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcAvg", .HeaderText = "Avg (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcP50", .HeaderText = "P50 (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcP95", .HeaderText = "P95 (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcMax", .HeaderText = "Max (ms)", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcThroughput", .HeaderText = "Synth/sec", .FillWeight = 11},
            New DataGridViewTextBoxColumn() With {.Name = "colTtsConcErrors", .HeaderText = "Errors", .FillWeight = 11}
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

        AppLogger.Log($"[BENCHMARK] TTS Comparison starting — " &
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
            AppLogger.Log($"[BENCHMARK] TTS Comparison complete — {result.Backends.Count} backends tested ({ttsModels})")
            ShowTtsComparisonResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log("[BENCHMARK] TTS Comparison cancelled by user")
            lblTtsProgress.Text = "Comparison cancelled."
        Catch ex As Exception
            AppLogger.Log($"[BENCHMARK] TTS Comparison error: {ex.Message}")
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

        AppLogger.Log($"[BENCHMARK] TTS Concurrency starting — backend: {backend.Name}, " &
                      $"language: {language}, levels: [{String.Join(", ", levels)}], " &
                      $"iterations: {nudTtsConcIterations.Value}")

        Try
            Dim result = Await _ttsConcRunner.RunAsync(
                backend, txtTtsConcText.Text, language, levels,
                CInt(nudTtsConcIterations.Value), Threading.CancellationToken.None)
            _lastTtsConcResult = result
            StampModelInfo(result.Resources, $"TTS engine: {result.BackendName}")
            btnTtsConcExport.Enabled = True
            AppLogger.Log($"[BENCHMARK] TTS Concurrency complete — {result.Levels.Count} levels, backend: {result.BackendName}")
            ShowTtsConcResult(result)
            AutoSaveResults()
        Catch ex As OperationCanceledException
            AppLogger.Log("[BENCHMARK] TTS Concurrency cancelled by user")
            lblTtsConcProgress.Text = "Test cancelled."
        Catch ex As Exception
            AppLogger.Log($"[BENCHMARK] TTS Concurrency error: {ex.Message}")
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

    Private Function GetTtsModelInfo(backendName As String) As String
        Return backendName
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

        If Not hasAny Then Return ""
        Return sb.ToString()
    End Function

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
            AppLogger.Log($"[BENCHMARK] Export All saved to {dlg.FileName}")
            MessageBox.Show($"Exported to {dlg.FileName}", "Export All", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

    Private Sub AutoSaveResults()
        Try
            Dim csv = BuildUnifiedCsv()
            If String.IsNullOrEmpty(csv) Then Return

            Dim benchDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "EveryTongue", "benchmarks")
            If Not Directory.Exists(benchDir) Then Directory.CreateDirectory(benchDir)

            Dim filePath = Path.Combine(benchDir, $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv")
            File.WriteAllText(filePath, csv, Encoding.UTF8)

            lblAutoSaveStatus.ForeColor = Color.Gray
            lblAutoSaveStatus.Text = $"Auto-saved: {Path.GetFileName(filePath)}"
            AppLogger.Log($"[BENCHMARK] Auto-saved to {filePath}")
        Catch ex As Exception
            lblAutoSaveStatus.ForeColor = Color.OrangeRed
            lblAutoSaveStatus.Text = $"Auto-save failed: {ex.Message}"
            AppLogger.Log($"[BENCHMARK] Auto-save error: {ex.Message}")
        End Try
    End Sub

End Class
