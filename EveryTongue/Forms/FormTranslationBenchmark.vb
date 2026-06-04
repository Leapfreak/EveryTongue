Imports System.IO
Imports System.Text
Imports EveryTongue.Models
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Scheduling
Imports EveryTongue.Services.Testing
Imports EveryTongue.Services.Translation

Public Class FormTranslationBenchmark

    Private ReadOnly _runner As New TranslationBenchmarkRunner()
    Private ReadOnly _sttComparer As New SttComparisonRunner()
    Private _corpus As List(Of CorpusEntry)
    Private _lastResult As BenchmarkResult
    Private _lastSttResult As SttComparisonResult
    Private _translationService As ITranslationService
    Private _ttsService As ITtsService
    Private _liveServerPort As Integer
    Private _config As AppConfig

    Public Sub New(translationService As ITranslationService,
                   Optional ttsService As ITtsService = Nothing,
                   Optional liveServerPort As Integer = 0,
                   Optional config As AppConfig = Nothing)
        InitializeComponent()
        _translationService = translationService
        _ttsService = ttsService
        _liveServerPort = liveServerPort
        _config = If(config, New AppConfig())
    End Sub

    Private Sub FormTranslationBenchmark_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        _corpus = TranslationBenchmarkRunner.LoadCorpus()

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

        ' Enable/disable stage checkboxes based on available services
        chkTranslation.Enabled = _translationService IsNot Nothing
        chkTranslation.Checked = _translationService IsNot Nothing
        chkTts.Enabled = _ttsService IsNot Nothing
        chkStt.Enabled = False ' Always starts disabled; enabled when TTS is checked

        UpdateCorpusInfo()
        ShowExistingProfile()
        WireSttHandlers()

        AddHandler cboDomain.SelectedIndexChanged, Sub(s, ev) UpdateCorpusInfo()
        AddHandler chkTts.CheckedChanged, Sub(s, ev)
                                               chkStt.Enabled = chkTts.Checked AndAlso _liveServerPort > 0
                                               If Not chkTts.Checked Then chkStt.Checked = False
                                           End Sub

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

        If Not chkTranslation.Checked AndAlso Not chkTts.Checked Then
            MessageBox.Show("Select at least one pipeline stage.", "Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
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

        ' Start resource monitoring (samples every 500ms)
        Dim monitor As New ResourceMonitor(500)
        monitor.Start()

        Try
            ' Stage 1: Translation
            If chkTranslation.Checked AndAlso _translationService IsNot Nothing Then
                lblSummary.Text = "Running Translation benchmark..."
                progressBar.Maximum = filtered.Count * iterations
                Dim translationResult = Await _runner.RunTranslationBenchmarkAsync(
                    _translationService, filtered, targets, concurrency, iterations)
                result.Stages.Add(translationResult)
            End If

            ' Stage 2: TTS
            If chkTts.Checked AndAlso _ttsService IsNot Nothing Then
                lblSummary.Text = "Running TTS benchmark..."
                Dim ttsStageResult = Await _runner.RunTtsBenchmarkAsync(
                    _ttsService, filtered, targets, concurrency, iterations)
                result.Stages.Add(ttsStageResult)
            End If

            ' Stage 3: STT (round-trip from TTS-generated audio)
            If chkStt.Checked AndAlso _liveServerPort > 0 AndAlso _runner.LastTtsResults IsNot Nothing Then
                lblSummary.Text = "Running STT benchmark..."
                Dim ttsCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EveryTongue", "tts-cache")
                Dim samples = TranslationBenchmarkRunner.CollectSttSamples(
                    _runner.LastTtsResults, ttsCacheDir)
                If samples.Count > 0 Then
                    Dim sttResult = Await _runner.RunSttBenchmarkAsync(
                        _liveServerPort, samples, concurrency)
                    result.Stages.Add(sttResult)
                Else
                    lblProgress.Text = "STT: no audio samples found from TTS run."
                End If
            End If

            ' Stop resource monitoring and attach report
            result.Resources = monitor.Stop()

            ' Save latency profile and show combined results
            TranslationBenchmarkRunner.SaveLatencyProfile(result)
            ShowResult(result)

        Catch ex As OperationCanceledException
            result.Resources = monitor.Stop()
            lblSummary.Text = "Benchmark cancelled."
            lblProgress.Text = ""
        Catch ex As Exception
            result.Resources = monitor.Stop()
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
        If chkSttFasterWhisper.Checked Then enabled.Add("faster-whisper")
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

        Try
            Dim result = Await _sttComparer.RunComparisonAsync(
                txtSttAudioFile.Text, _config, _liveServerPort,
                CInt(nudSttIterations.Value), Threading.CancellationToken.None, enabled)
            _lastSttResult = result
            btnSttExport.Enabled = True
            ShowSttComparisonResult(result)
        Catch ex As OperationCanceledException
            lblSttProgress.Text = "Comparison cancelled."
        Catch ex As Exception
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
            sb.AppendLine("Engine,Status,Model Load (ms),Avg (ms),Min (ms),Max (ms),Iterations,Speedup,Transcription")

            For Each b In _lastSttResult.Backends
                Dim status = If(b.Skipped, $"Skipped: {b.SkipReason}",
                              If(b.Failed, $"Failed: {b.ErrorMessage}", "OK"))
                Dim text = If(b.TranscribedText, "").Replace("""", """""")
                sb.AppendLine($"""{b.BackendName}"",""{status}"",{b.ModelLoadMs},{b.AvgInferenceMs},{b.MinInferenceMs},{b.MaxInferenceMs},{b.Iterations},{b.SpeedupVsFastest:F2},""{text}""")
            Next

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
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs) Handles btnExport.Click
        If _lastResult Is Nothing Then Return

        Using dlg As New SaveFileDialog()
            dlg.Filter = "CSV files|*.csv"
            dlg.FileName = $"benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Dim sb As New StringBuilder()

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
            Dim res = _lastResult.Resources
            If res IsNot Nothing AndAlso res.SampleCount > 0 Then
                sb.AppendLine("# Resource Utilisation")
                sb.AppendLine("metric,min,avg,max,unit")
                sb.AppendLine($"process_memory,{res.ProcessMemoryMinMB},{res.ProcessMemoryAvgMB},{res.ProcessMemoryMaxMB},MB")
                If res.HasGpu Then
                    sb.AppendLine($"gpu_util,{res.GpuUtilMinPercent},{res.GpuUtilAvgPercent},{res.GpuUtilMaxPercent},%")
                    sb.AppendLine($"gpu_memory,{res.GpuMemoryMinMB},{res.GpuMemoryAvgMB},{res.GpuMemoryMaxMB},MB")
                    sb.AppendLine($"gpu_memory_total,{res.GpuMemoryTotalMB},{res.GpuMemoryTotalMB},{res.GpuMemoryTotalMB},MB")
                    sb.AppendLine($"gpu_memory_peak_percent,,,{res.GpuMemoryPeakPercent},%")
                    If res.GpuTempMaxC > 0 Then
                        sb.AppendLine($"gpu_temp,{res.GpuTempMinC},,{res.GpuTempMaxC},C")
                    End If
                End If
                If res.Warnings.Count > 0 Then
                    sb.AppendLine()
                    sb.AppendLine("# Warnings")
                    For Each w In res.Warnings
                        sb.AppendLine(w)
                    Next
                End If
            End If

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8)
            MessageBox.Show($"Exported to {dlg.FileName}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
    End Sub

End Class
