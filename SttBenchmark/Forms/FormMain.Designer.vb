Namespace Global.SttBenchmark

    Partial Class FormMain
        Inherits System.Windows.Forms.Form

        Private components As System.ComponentModel.IContainer

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container()

            ' === Controls ===
            Me.lblAudioFile = New System.Windows.Forms.Label()
            Me.txtAudioFile = New System.Windows.Forms.TextBox()
            Me.btnBrowseAudio = New System.Windows.Forms.Button()
            Me.lblReference = New System.Windows.Forms.Label()
            Me.txtReference = New System.Windows.Forms.TextBox()
            Me.btnBrowseRef = New System.Windows.Forms.Button()
            Me.lblLanguage = New System.Windows.Forms.Label()
            Me.txtLanguage = New System.Windows.Forms.TextBox()
            Me.lblSpeed = New System.Windows.Forms.Label()
            Me.cboSpeed = New System.Windows.Forms.ComboBox()
            Me.lblPipeline = New System.Windows.Forms.Label()
            Me.cboPipeline = New System.Windows.Forms.ComboBox()
            Me.lblServerUrl = New System.Windows.Forms.Label()
            Me.txtServerUrl = New System.Windows.Forms.TextBox()

            ' Engine parameters
            Me.grpParams = New System.Windows.Forms.GroupBox()
            Me.lblBeamSize = New System.Windows.Forms.Label()
            Me.nudBeamSize = New System.Windows.Forms.NumericUpDown()
            Me.lblBestOf = New System.Windows.Forms.Label()
            Me.nudBestOf = New System.Windows.Forms.NumericUpDown()
            Me.lblVadSilence = New System.Windows.Forms.Label()
            Me.nudVadSilence = New System.Windows.Forms.NumericUpDown()
            Me.lblVadMaxSeg = New System.Windows.Forms.Label()
            Me.nudVadMaxSeg = New System.Windows.Forms.NumericUpDown()
            Me.lblInterimMs = New System.Windows.Forms.Label()
            Me.nudInterimMs = New System.Windows.Forms.NumericUpDown()
            Me.lblInitialPrompt = New System.Windows.Forms.Label()
            Me.txtInitialPrompt = New System.Windows.Forms.TextBox()
            Me.lblNotes = New System.Windows.Forms.Label()
            Me.txtNotes = New System.Windows.Forms.TextBox()

            ' Normalization
            Me.chkIgnoreCase = New System.Windows.Forms.CheckBox()
            Me.chkStripPunct = New System.Windows.Forms.CheckBox()

            ' Buttons
            Me.btnRun = New System.Windows.Forms.Button()
            Me.btnCancel = New System.Windows.Forms.Button()
            Me.btnExportCsv = New System.Windows.Forms.Button()
            Me.btnClearHistory = New System.Windows.Forms.Button()

            ' Status
            Me.lblStatus = New System.Windows.Forms.Label()
            Me.progressBar = New System.Windows.Forms.ProgressBar()

            ' Results grid
            Me.dgvResults = New System.Windows.Forms.DataGridView()

            ' Detail panels
            Me.splitMain = New System.Windows.Forms.SplitContainer()
            Me.panelControls = New System.Windows.Forms.Panel()
            Me.panelResults = New System.Windows.Forms.Panel()

            ' Transcript panels
            Me.lblRefTranscript = New System.Windows.Forms.Label()
            Me.txtRefTranscript = New System.Windows.Forms.TextBox()
            Me.lblOutTranscript = New System.Windows.Forms.Label()
            Me.txtOutTranscript = New System.Windows.Forms.TextBox()
            Me.lblRunParams = New System.Windows.Forms.Label()
            Me.txtRunParams = New System.Windows.Forms.TextBox()

            Me.SuspendLayout()

            ' === splitMain ===
            Me.splitMain.Dock = System.Windows.Forms.DockStyle.Fill
            Me.splitMain.Orientation = System.Windows.Forms.Orientation.Vertical
            Me.splitMain.SplitterDistance = 340
            Me.splitMain.Panel1.Controls.Add(Me.panelControls)
            Me.splitMain.Panel2.Controls.Add(Me.panelResults)

            ' === panelControls ===
            Me.panelControls.Dock = System.Windows.Forms.DockStyle.Fill
            Me.panelControls.AutoScroll = True
            Dim cy = 8 ' current Y position

            ' Server URL
            Me.lblServerUrl.SetBounds(8, cy + 3, 75, 20)
            Me.lblServerUrl.Text = "Server URL:"
            Me.txtServerUrl.SetBounds(88, cy, 220, 23)
            Me.txtServerUrl.Text = "http://127.0.0.1:5091"
            cy += 30

            ' Audio file
            Me.lblAudioFile.SetBounds(8, cy + 3, 75, 20)
            Me.lblAudioFile.Text = "Audio File:"
            Me.txtAudioFile.SetBounds(88, cy, 180, 23)
            Me.btnBrowseAudio.SetBounds(272, cy, 36, 23)
            Me.btnBrowseAudio.Text = "..."
            cy += 30

            ' Reference file
            Me.lblReference.SetBounds(8, cy + 3, 75, 20)
            Me.lblReference.Text = "Reference:"
            Me.txtReference.SetBounds(88, cy, 180, 23)
            Me.btnBrowseRef.SetBounds(272, cy, 36, 23)
            Me.btnBrowseRef.Text = "..."
            cy += 30

            ' Language
            Me.lblLanguage.SetBounds(8, cy + 3, 75, 20)
            Me.lblLanguage.Text = "Language:"
            Me.txtLanguage.SetBounds(88, cy, 60, 23)
            Me.txtLanguage.Text = "ca"
            cy += 30

            ' Speed
            Me.lblSpeed.SetBounds(8, cy + 3, 75, 20)
            Me.lblSpeed.Text = "Speed:"
            Me.cboSpeed.SetBounds(88, cy, 120, 23)
            Me.cboSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboSpeed.Items.AddRange(New Object() {"Max speed (0x)", "Real-time (1x)", "Half speed (0.5x)"})
            Me.cboSpeed.SelectedIndex = 0
            cy += 30

            ' Pipeline
            Me.lblPipeline.SetBounds(8, cy + 3, 75, 20)
            Me.lblPipeline.Text = "Pipeline:"
            Me.cboPipeline.SetBounds(88, cy, 160, 23)
            Me.cboPipeline.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.cboPipeline.Items.AddRange(New Object() {"VadPipeline", "AccumulatingPipeline"})
            Me.cboPipeline.SelectedIndex = 0
            cy += 30

            ' === Engine Parameters Group ===
            Me.grpParams.SetBounds(8, cy, 300, 220)
            Me.grpParams.Text = "Engine Parameters"
            Dim py = 20

            Me.lblBeamSize.SetBounds(10, py + 3, 95, 20)
            Me.lblBeamSize.Text = "beam_size:"
            Me.nudBeamSize.SetBounds(110, py, 60, 23)
            Me.nudBeamSize.Minimum = 1
            Me.nudBeamSize.Maximum = 20
            Me.nudBeamSize.Value = 5
            py += 28

            Me.lblBestOf.SetBounds(10, py + 3, 95, 20)
            Me.lblBestOf.Text = "best_of:"
            Me.nudBestOf.SetBounds(110, py, 60, 23)
            Me.nudBestOf.Minimum = 1
            Me.nudBestOf.Maximum = 10
            Me.nudBestOf.Value = 1
            py += 28

            Me.lblVadSilence.SetBounds(10, py + 3, 95, 20)
            Me.lblVadSilence.Text = "vad_silence_ms:"
            Me.nudVadSilence.SetBounds(110, py, 80, 23)
            Me.nudVadSilence.Minimum = 100
            Me.nudVadSilence.Maximum = 5000
            Me.nudVadSilence.Value = 750
            Me.nudVadSilence.Increment = 50
            py += 28

            Me.lblVadMaxSeg.SetBounds(10, py + 3, 95, 20)
            Me.lblVadMaxSeg.Text = "vad_max_seg_s:"
            Me.nudVadMaxSeg.SetBounds(110, py, 80, 23)
            Me.nudVadMaxSeg.Minimum = 5
            Me.nudVadMaxSeg.Maximum = 120
            Me.nudVadMaxSeg.Value = 25
            py += 28

            Me.lblInterimMs.SetBounds(10, py + 3, 95, 20)
            Me.lblInterimMs.Text = "interim_ms:"
            Me.nudInterimMs.SetBounds(110, py, 80, 23)
            Me.nudInterimMs.Minimum = 100
            Me.nudInterimMs.Maximum = 10000
            Me.nudInterimMs.Value = 1000
            Me.nudInterimMs.Increment = 100
            py += 28

            Me.lblInitialPrompt.SetBounds(10, py + 3, 95, 20)
            Me.lblInitialPrompt.Text = "init_prompt:"
            Me.txtInitialPrompt.SetBounds(110, py, 180, 23)
            py += 28

            Me.grpParams.Controls.AddRange(New System.Windows.Forms.Control() {
                Me.lblBeamSize, Me.nudBeamSize,
                Me.lblBestOf, Me.nudBestOf,
                Me.lblVadSilence, Me.nudVadSilence,
                Me.lblVadMaxSeg, Me.nudVadMaxSeg,
                Me.lblInterimMs, Me.nudInterimMs,
                Me.lblInitialPrompt, Me.txtInitialPrompt
            })
            cy += 228

            ' Notes
            Me.lblNotes.SetBounds(8, cy + 3, 75, 20)
            Me.lblNotes.Text = "Notes:"
            Me.txtNotes.SetBounds(88, cy, 220, 23)
            cy += 30

            ' Normalization checkboxes
            Me.chkIgnoreCase.SetBounds(8, cy, 130, 20)
            Me.chkIgnoreCase.Text = "Ignore case"
            Me.chkIgnoreCase.Checked = True
            Me.chkStripPunct.SetBounds(140, cy, 150, 20)
            Me.chkStripPunct.Text = "Strip punctuation"
            Me.chkStripPunct.Checked = True
            cy += 28

            ' Buttons
            Me.btnRun.SetBounds(8, cy, 80, 30)
            Me.btnRun.Text = "Run"
            Me.btnCancel.SetBounds(96, cy, 80, 30)
            Me.btnCancel.Text = "Cancel"
            Me.btnCancel.Enabled = False
            Me.btnExportCsv.SetBounds(184, cy, 80, 30)
            Me.btnExportCsv.Text = "Export CSV"
            Me.btnClearHistory.SetBounds(8, cy + 36, 80, 26)
            Me.btnClearHistory.Text = "Clear History"
            cy += 68

            ' Status
            Me.lblStatus.SetBounds(8, cy, 300, 20)
            Me.lblStatus.Text = "Ready"
            cy += 24
            Me.progressBar.SetBounds(8, cy, 300, 18)
            cy += 26

            Me.panelControls.Controls.AddRange(New System.Windows.Forms.Control() {
                Me.lblServerUrl, Me.txtServerUrl,
                Me.lblAudioFile, Me.txtAudioFile, Me.btnBrowseAudio,
                Me.lblReference, Me.txtReference, Me.btnBrowseRef,
                Me.lblLanguage, Me.txtLanguage,
                Me.lblSpeed, Me.cboSpeed,
                Me.lblPipeline, Me.cboPipeline,
                Me.grpParams,
                Me.lblNotes, Me.txtNotes,
                Me.chkIgnoreCase, Me.chkStripPunct,
                Me.btnRun, Me.btnCancel, Me.btnExportCsv, Me.btnClearHistory,
                Me.lblStatus, Me.progressBar
            })

            ' === panelResults ===
            Me.panelResults.Dock = System.Windows.Forms.DockStyle.Fill

            ' Results grid
            Me.dgvResults.SetBounds(0, 0, 600, 250)
            Me.dgvResults.Anchor = System.Windows.Forms.AnchorStyles.Top Or
                System.Windows.Forms.AnchorStyles.Left Or
                System.Windows.Forms.AnchorStyles.Right Or
                System.Windows.Forms.AnchorStyles.Bottom
            Me.dgvResults.AllowUserToAddRows = False
            Me.dgvResults.AllowUserToDeleteRows = False
            Me.dgvResults.ReadOnly = True
            Me.dgvResults.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
            Me.dgvResults.MultiSelect = False
            Me.dgvResults.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill

            ' Detail panels below grid
            Dim detailY = 258
            Me.lblRunParams.SetBounds(4, detailY, 180, 16)
            Me.lblRunParams.Text = "Parameters (click row):"
            Me.lblRunParams.Font = New System.Drawing.Font(Me.Font, System.Drawing.FontStyle.Bold)
            Me.txtRunParams.SetBounds(4, detailY + 18, 180, 200)
            Me.txtRunParams.Multiline = True
            Me.txtRunParams.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
            Me.txtRunParams.ReadOnly = True
            Me.txtRunParams.Anchor = System.Windows.Forms.AnchorStyles.Left Or
                System.Windows.Forms.AnchorStyles.Bottom

            Me.lblRefTranscript.SetBounds(190, detailY, 200, 16)
            Me.lblRefTranscript.Text = "Reference:"
            Me.lblRefTranscript.Font = New System.Drawing.Font(Me.Font, System.Drawing.FontStyle.Bold)
            Me.txtRefTranscript.SetBounds(190, detailY + 18, 200, 200)
            Me.txtRefTranscript.Multiline = True
            Me.txtRefTranscript.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
            Me.txtRefTranscript.ReadOnly = True
            Me.txtRefTranscript.Anchor = System.Windows.Forms.AnchorStyles.Left Or
                System.Windows.Forms.AnchorStyles.Bottom Or
                System.Windows.Forms.AnchorStyles.Right

            Me.lblOutTranscript.SetBounds(396, detailY, 200, 16)
            Me.lblOutTranscript.Text = "Output:"
            Me.lblOutTranscript.Font = New System.Drawing.Font(Me.Font, System.Drawing.FontStyle.Bold)
            Me.txtOutTranscript.SetBounds(396, detailY + 18, 200, 200)
            Me.txtOutTranscript.Multiline = True
            Me.txtOutTranscript.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
            Me.txtOutTranscript.ReadOnly = True
            Me.txtOutTranscript.Anchor = System.Windows.Forms.AnchorStyles.Right Or
                System.Windows.Forms.AnchorStyles.Bottom

            Me.panelResults.Controls.AddRange(New System.Windows.Forms.Control() {
                Me.dgvResults,
                Me.lblRunParams, Me.txtRunParams,
                Me.lblRefTranscript, Me.txtRefTranscript,
                Me.lblOutTranscript, Me.txtOutTranscript
            })

            ' === Form ===
            Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0F, 15.0F)
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.ClientSize = New System.Drawing.Size(960, 600)
            Me.Controls.Add(Me.splitMain)
            Me.MinimumSize = New System.Drawing.Size(800, 500)
            Me.Text = "STT Pipeline Benchmark"

            Me.ResumeLayout(False)
        End Sub

        ' File pickers
        Friend WithEvents lblAudioFile As System.Windows.Forms.Label
        Friend WithEvents txtAudioFile As System.Windows.Forms.TextBox
        Friend WithEvents btnBrowseAudio As System.Windows.Forms.Button
        Friend WithEvents lblReference As System.Windows.Forms.Label
        Friend WithEvents txtReference As System.Windows.Forms.TextBox
        Friend WithEvents btnBrowseRef As System.Windows.Forms.Button

        ' Config
        Friend WithEvents lblLanguage As System.Windows.Forms.Label
        Friend WithEvents txtLanguage As System.Windows.Forms.TextBox
        Friend WithEvents lblSpeed As System.Windows.Forms.Label
        Friend WithEvents cboSpeed As System.Windows.Forms.ComboBox
        Friend WithEvents lblPipeline As System.Windows.Forms.Label
        Friend WithEvents cboPipeline As System.Windows.Forms.ComboBox
        Friend WithEvents lblServerUrl As System.Windows.Forms.Label
        Friend WithEvents txtServerUrl As System.Windows.Forms.TextBox

        ' Parameters
        Friend WithEvents grpParams As System.Windows.Forms.GroupBox
        Friend WithEvents lblBeamSize As System.Windows.Forms.Label
        Friend WithEvents nudBeamSize As System.Windows.Forms.NumericUpDown
        Friend WithEvents lblBestOf As System.Windows.Forms.Label
        Friend WithEvents nudBestOf As System.Windows.Forms.NumericUpDown
        Friend WithEvents lblVadSilence As System.Windows.Forms.Label
        Friend WithEvents nudVadSilence As System.Windows.Forms.NumericUpDown
        Friend WithEvents lblVadMaxSeg As System.Windows.Forms.Label
        Friend WithEvents nudVadMaxSeg As System.Windows.Forms.NumericUpDown
        Friend WithEvents lblInterimMs As System.Windows.Forms.Label
        Friend WithEvents nudInterimMs As System.Windows.Forms.NumericUpDown
        Friend WithEvents lblInitialPrompt As System.Windows.Forms.Label
        Friend WithEvents txtInitialPrompt As System.Windows.Forms.TextBox
        Friend WithEvents lblNotes As System.Windows.Forms.Label
        Friend WithEvents txtNotes As System.Windows.Forms.TextBox

        ' Normalization
        Friend WithEvents chkIgnoreCase As System.Windows.Forms.CheckBox
        Friend WithEvents chkStripPunct As System.Windows.Forms.CheckBox

        ' Buttons
        Friend WithEvents btnRun As System.Windows.Forms.Button
        Friend WithEvents btnCancel As System.Windows.Forms.Button
        Friend WithEvents btnExportCsv As System.Windows.Forms.Button
        Friend WithEvents btnClearHistory As System.Windows.Forms.Button

        ' Status
        Friend WithEvents lblStatus As System.Windows.Forms.Label
        Friend WithEvents progressBar As System.Windows.Forms.ProgressBar

        ' Results
        Friend WithEvents dgvResults As System.Windows.Forms.DataGridView
        Friend WithEvents splitMain As System.Windows.Forms.SplitContainer
        Friend WithEvents panelControls As System.Windows.Forms.Panel
        Friend WithEvents panelResults As System.Windows.Forms.Panel

        ' Detail panels
        Friend WithEvents lblRunParams As System.Windows.Forms.Label
        Friend WithEvents txtRunParams As System.Windows.Forms.TextBox
        Friend WithEvents lblRefTranscript As System.Windows.Forms.Label
        Friend WithEvents txtRefTranscript As System.Windows.Forms.TextBox
        Friend WithEvents lblOutTranscript As System.Windows.Forms.Label
        Friend WithEvents txtOutTranscript As System.Windows.Forms.TextBox

    End Class

End Namespace
