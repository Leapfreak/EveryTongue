Imports System.IO
Imports System.Threading
Imports SttBenchmark.Models
Imports SttBenchmark.Services

Namespace Global.SttBenchmark

    Public Class FormMain

        Private ReadOnly _runner As New PipelineBenchmarkRunner()
        Private ReadOnly _werCalc As New WerCalculator()
        Private ReadOnly _store As New ResultStore()
        Private _cts As CancellationTokenSource
        Private _referenceText As String = ""

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            SetupGrid()
            LoadHistory()

            AddHandler _runner.StatusChanged, Sub(msg)
                                                  If InvokeRequired Then
                                                      BeginInvoke(Sub() lblStatus.Text = msg)
                                                  Else
                                                      lblStatus.Text = msg
                                                  End If
                                              End Sub
            AddHandler _runner.ProgressChanged, Sub(pct)
                                                    If InvokeRequired Then
                                                        BeginInvoke(Sub() progressBar.Value = Math.Min(pct, 100))
                                                    Else
                                                        progressBar.Value = Math.Min(pct, 100)
                                                    End If
                                                End Sub
        End Sub

        Private Sub SetupGrid()
            dgvResults.Columns.Clear()
            dgvResults.Columns.Add("ColId", "#")
            dgvResults.Columns.Add("ColTime", "Time")
            dgvResults.Columns.Add("ColBackend", "Backend")
            dgvResults.Columns.Add("ColPipeline", "Pipeline")
            dgvResults.Columns.Add("ColWer", "WER %")
            dgvResults.Columns.Add("ColSubs", "S")
            dgvResults.Columns.Add("ColDels", "D")
            dgvResults.Columns.Add("ColIns", "I")
            dgvResults.Columns.Add("ColRefW", "Ref")
            dgvResults.Columns.Add("ColCommits", "Cmts")
            dgvResults.Columns.Add("ColHal", "Hal")
            dgvResults.Columns.Add("ColElapsed", "Time(s)")
            dgvResults.Columns.Add("ColNotes", "Notes")

            dgvResults.Columns("ColId").Width = 30
            dgvResults.Columns("ColTime").Width = 55
            dgvResults.Columns("ColWer").Width = 55
            dgvResults.Columns("ColSubs").Width = 35
            dgvResults.Columns("ColDels").Width = 35
            dgvResults.Columns("ColIns").Width = 35
            dgvResults.Columns("ColRefW").Width = 40
            dgvResults.Columns("ColCommits").Width = 40
            dgvResults.Columns("ColHal").Width = 35
            dgvResults.Columns("ColElapsed").Width = 55
        End Sub

        Private Sub LoadHistory()
            dgvResults.Rows.Clear()
            For i = 0 To _store.Runs.Count - 1
                AddRunToGrid(_store.Runs(i), i + 1)
            Next
        End Sub

        Private Sub AddRunToGrid(run As BenchmarkRun, index As Integer)
            dgvResults.Rows.Add(
                index,
                run.Timestamp.ToString("HH:mm"),
                run.Backend,
                run.Pipeline,
                $"{run.Wer:F1}",
                run.Substitutions,
                run.Deletions,
                run.Insertions,
                run.RefWords,
                run.CommitCount,
                run.HallucinationCount,
                $"{run.ElapsedS:F0}",
                run.Notes
            )
        End Sub

        Private Sub btnBrowseAudio_Click(sender As Object, e As EventArgs) Handles btnBrowseAudio.Click
            Using dlg As New OpenFileDialog()
                dlg.Filter = "Audio Files|*.wav;*.mp3;*.flac;*.ogg;*.m4a|All Files|*.*"
                If dlg.ShowDialog() = DialogResult.OK Then
                    txtAudioFile.Text = dlg.FileName
                End If
            End Using
        End Sub

        Private Sub btnBrowseRef_Click(sender As Object, e As EventArgs) Handles btnBrowseRef.Click
            Using dlg As New OpenFileDialog()
                dlg.Filter = "Text Files|*.txt|All Files|*.*"
                If dlg.ShowDialog() = DialogResult.OK Then
                    txtReference.Text = dlg.FileName
                    _referenceText = File.ReadAllText(dlg.FileName)
                End If
            End Using
        End Sub

        Private Async Sub btnRun_Click(sender As Object, e As EventArgs) Handles btnRun.Click
            If String.IsNullOrWhiteSpace(txtAudioFile.Text) OrElse Not File.Exists(txtAudioFile.Text) Then
                MessageBox.Show("Please select a valid audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            If String.IsNullOrWhiteSpace(txtReference.Text) OrElse Not File.Exists(txtReference.Text) Then
                MessageBox.Show("Please select a valid reference file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Load reference if not yet loaded
            If String.IsNullOrEmpty(_referenceText) Then
                _referenceText = File.ReadAllText(txtReference.Text)
            End If

            _runner.ServerBaseUrl = txtServerUrl.Text.TrimEnd("/"c)

            ' Check server health
            lblStatus.Text = "Checking server..."
            Dim healthy = Await _runner.CheckHealthAsync()
            If Not healthy Then
                MessageBox.Show(
                    "Cannot reach the live-server. Make sure EveryTongue is running with a live session, " &
                    "or start the live-server manually.",
                    "Server Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                lblStatus.Text = "Server not reachable"
                Return
            End If

            ' Parse speed
            Dim rtFactor As Double = 0.0
            Select Case cboSpeed.SelectedIndex
                Case 0 : rtFactor = 0.0
                Case 1 : rtFactor = 1.0
                Case 2 : rtFactor = 0.5
            End Select

            ' Set up cancellation
            _cts = New CancellationTokenSource()
            btnRun.Enabled = False
            btnCancel.Enabled = True
            progressBar.Value = 0

            Try
                Dim result = Await _runner.RunAsync(
                    txtAudioFile.Text,
                    txtLanguage.Text,
                    cboPipeline.SelectedItem.ToString(),
                    CInt(nudBeamSize.Value),
                    CInt(nudBestOf.Value),
                    txtInitialPrompt.Text,
                    CInt(nudVadSilence.Value),
                    CInt(nudVadMaxSeg.Value),
                    CInt(nudInterimMs.Value),
                    rtFactor,
                    _cts.Token
                )

                ' Compute WER
                _werCalc.IgnoreCase = chkIgnoreCase.Checked
                _werCalc.StripPunctuation = chkStripPunct.Checked
                Dim werResult = _werCalc.Compute(_referenceText, result.OutputText)

                ' Build run record
                Dim run As New BenchmarkRun With {
                    .Id = DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    .Timestamp = DateTime.Now,
                    .Backend = "(server)",
                    .AudioFile = Path.GetFileName(txtAudioFile.Text),
                    .ReferenceFile = Path.GetFileName(txtReference.Text),
                    .Language = txtLanguage.Text,
                    .Pipeline = cboPipeline.SelectedItem.ToString(),
                    .Notes = txtNotes.Text,
                    .BeamSize = CInt(nudBeamSize.Value),
                    .BestOf = CInt(nudBestOf.Value),
                    .VadMinSilenceMs = CInt(nudVadSilence.Value),
                    .VadMaxSegmentS = CInt(nudVadMaxSeg.Value),
                    .InterimIntervalMs = CInt(nudInterimMs.Value),
                    .InitialPrompt = txtInitialPrompt.Text,
                    .RealtimeFactor = rtFactor,
                    .Wer = werResult.Wer,
                    .Substitutions = werResult.Substitutions,
                    .Deletions = werResult.Deletions,
                    .Insertions = werResult.Insertions,
                    .RefWords = werResult.RefWords,
                    .HypWords = werResult.HypWords,
                    .CommitCount = result.CommitCount,
                    .HallucinationCount = result.HallucinationCount,
                    .AudioDurationS = result.AudioDurationS,
                    .ElapsedS = result.ElapsedS,
                    .OutputText = result.OutputText,
                    .StatsSummary = result.StatsSummary
                }

                _store.Add(run)
                AddRunToGrid(run, _store.Runs.Count)

                lblStatus.Text = $"Done. WER = {werResult.Wer:F1}% ({werResult.Substitutions}S {werResult.Deletions}D {werResult.Insertions}I / {werResult.RefWords} ref words)"

                ' Show transcript panels
                txtRefTranscript.Text = _referenceText
                txtOutTranscript.Text = result.OutputText

            Catch ex As OperationCanceledException
                lblStatus.Text = "Cancelled."
            Catch ex As Exception
                lblStatus.Text = $"Error: {ex.Message}"
                MessageBox.Show(ex.Message, "Benchmark Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnRun.Enabled = True
                btnCancel.Enabled = False
                progressBar.Value = 0
            End Try
        End Sub

        Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
            _cts?.Cancel()
        End Sub

        Private Sub btnExportCsv_Click(sender As Object, e As EventArgs) Handles btnExportCsv.Click
            Using dlg As New SaveFileDialog()
                dlg.Filter = "CSV Files|*.csv"
                dlg.FileName = $"benchmark_{DateTime.Now:yyyyMMdd}.csv"
                If dlg.ShowDialog() = DialogResult.OK Then
                    _store.ExportCsv(dlg.FileName)
                    lblStatus.Text = $"Exported to {dlg.FileName}"
                End If
            End Using
        End Sub

        Private Sub btnClearHistory_Click(sender As Object, e As EventArgs) Handles btnClearHistory.Click
            If MessageBox.Show("Clear all benchmark history?", "Confirm",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                _store.Clear()
                dgvResults.Rows.Clear()
                txtRunParams.Text = ""
                txtRefTranscript.Text = ""
                txtOutTranscript.Text = ""
                lblStatus.Text = "History cleared."
            End If
        End Sub

        Private Sub dgvResults_SelectionChanged(sender As Object, e As EventArgs) Handles dgvResults.SelectionChanged
            If dgvResults.SelectedRows.Count = 0 Then Return
            Dim idx = dgvResults.SelectedRows(0).Index
            If idx < 0 OrElse idx >= _store.Runs.Count Then Return

            Dim run = _store.Runs(idx)

            txtRunParams.Text =
                $"Backend: {run.Backend}" & vbCrLf &
                $"Pipeline: {run.Pipeline}" & vbCrLf &
                $"Language: {run.Language}" & vbCrLf &
                $"beam_size: {run.BeamSize}" & vbCrLf &
                $"best_of: {run.BestOf}" & vbCrLf &
                $"vad_silence: {run.VadMinSilenceMs}ms" & vbCrLf &
                $"vad_max_seg: {run.VadMaxSegmentS}s" & vbCrLf &
                $"interim_ms: {run.InterimIntervalMs}" & vbCrLf &
                $"init_prompt: {run.InitialPrompt}" & vbCrLf &
                $"WER: {run.Wer:F1}%" & vbCrLf &
                $"S={run.Substitutions} D={run.Deletions} I={run.Insertions}" & vbCrLf &
                $"Audio: {run.AudioDurationS:F0}s" & vbCrLf &
                $"Elapsed: {run.ElapsedS:F0}s"

            txtOutTranscript.Text = run.OutputText

            ' Try to load reference text
            If Not String.IsNullOrEmpty(_referenceText) Then
                txtRefTranscript.Text = _referenceText
            End If
        End Sub

        Private Sub dgvResults_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles dgvResults.CellDoubleClick
            If e.RowIndex < 0 OrElse e.RowIndex >= _store.Runs.Count Then Return

            ' Load parameters from clicked run back into controls
            Dim run = _store.Runs(e.RowIndex)
            cboPipeline.SelectedItem = run.Pipeline
            nudBeamSize.Value = run.BeamSize
            nudBestOf.Value = run.BestOf
            nudVadSilence.Value = run.VadMinSilenceMs
            nudVadMaxSeg.Value = run.VadMaxSegmentS
            nudInterimMs.Value = run.InterimIntervalMs
            txtInitialPrompt.Text = run.InitialPrompt
            txtLanguage.Text = run.Language
            txtNotes.Text = run.Notes

            lblStatus.Text = "Loaded parameters from run #" & (e.RowIndex + 1)
        End Sub

    End Class

End Namespace
