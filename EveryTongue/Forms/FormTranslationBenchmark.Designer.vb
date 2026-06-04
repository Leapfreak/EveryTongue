<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTranslationBenchmark
    Inherits System.Windows.Forms.Form

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.tabMain = New TabControl()
        Me.tabTranslation = New TabPage()
        Me.tabStt = New TabPage()
        Me.tabTts = New TabPage()

        ' ── Translation tab controls ──
        Me.lblTitle = New Label()
        Me.lblQueueStats = New Label()
        Me.grpConfig = New GroupBox()
        Me.lblDomain = New Label()
        Me.cboDomain = New ComboBox()
        Me.lblTargets = New Label()
        Me.clbTargets = New CheckedListBox()
        Me.lblConcurrency = New Label()
        Me.nudConcurrency = New NumericUpDown()
        Me.lblIterations = New Label()
        Me.nudIterations = New NumericUpDown()
        Me.lblCorpusInfo = New Label()
        Me.grpStages = New GroupBox()
        Me.chkTranslation = New CheckBox()
        Me.chkTts = New CheckBox()
        Me.chkStt = New CheckBox()
        Me.btnRun = New Button()
        Me.btnCancel = New Button()
        Me.btnExport = New Button()
        Me.progressBar = New ProgressBar()
        Me.lblProgress = New Label()
        Me.lblResources = New Label()
        Me.grpResults = New GroupBox()
        Me.dgvResults = New DataGridView()
        Me.colStage = New DataGridViewTextBoxColumn()
        Me.colPair = New DataGridViewTextBoxColumn()
        Me.colCount = New DataGridViewTextBoxColumn()
        Me.colAvgLatency = New DataGridViewTextBoxColumn()
        Me.colAvgQuality = New DataGridViewTextBoxColumn()
        Me.colMinQuality = New DataGridViewTextBoxColumn()
        Me.colMaxQuality = New DataGridViewTextBoxColumn()
        Me.lblSummary = New Label()

        ' ── STT tab controls ──
        Me.lblSttTitle = New Label()
        Me.lblSttAudioFile = New Label()
        Me.txtSttAudioFile = New TextBox()
        Me.btnSttBrowse = New Button()
        Me.lblSttIterations = New Label()
        Me.nudSttIterations = New NumericUpDown()
        Me.chkSttFasterWhisper = New CheckBox()
        Me.chkSttCuda = New CheckBox()
        Me.chkSttVulkan = New CheckBox()
        Me.chkSttCpu = New CheckBox()
        Me.btnSttCompare = New Button()
        Me.btnSttCancel = New Button()
        Me.btnSttExport = New Button()
        Me.lblSttProgress = New Label()
        Me.dgvSttCompare = New DataGridView()
        Me.colSttEngine = New DataGridViewTextBoxColumn()
        Me.colSttLoadTime = New DataGridViewTextBoxColumn()
        Me.colSttAvgMs = New DataGridViewTextBoxColumn()
        Me.colSttMinMs = New DataGridViewTextBoxColumn()
        Me.colSttMaxMs = New DataGridViewTextBoxColumn()
        Me.colSttSpeedup = New DataGridViewTextBoxColumn()
        Me.colSttText = New DataGridViewTextBoxColumn()

        ' ── TTS tab controls ──
        Me.lblTtsTitle = New Label()
        Me.lblTtsPlaceholder = New Label()

        Me.tabMain.SuspendLayout()
        Me.tabTranslation.SuspendLayout()
        Me.tabStt.SuspendLayout()
        Me.tabTts.SuspendLayout()
        Me.grpConfig.SuspendLayout()
        Me.grpStages.SuspendLayout()
        Me.grpResults.SuspendLayout()
        CType(Me.nudConcurrency, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudSttIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvResults, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvSttCompare, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()

        ' ══════════════════════════════════════════════════
        '  tabMain
        ' ══════════════════════════════════════════════════
        Me.tabMain.Controls.Add(Me.tabTranslation)
        Me.tabMain.Controls.Add(Me.tabStt)
        Me.tabMain.Controls.Add(Me.tabTts)
        Me.tabMain.Dock = DockStyle.Fill
        Me.tabMain.Location = New Point(0, 0)
        Me.tabMain.Name = "tabMain"
        Me.tabMain.SelectedIndex = 0
        Me.tabMain.TabIndex = 0

        ' tabTranslation
        Me.tabTranslation.Controls.Add(Me.lblTitle)
        Me.tabTranslation.Controls.Add(Me.grpConfig)
        Me.tabTranslation.Controls.Add(Me.grpStages)
        Me.tabTranslation.Controls.Add(Me.btnRun)
        Me.tabTranslation.Controls.Add(Me.btnCancel)
        Me.tabTranslation.Controls.Add(Me.btnExport)
        Me.tabTranslation.Controls.Add(Me.progressBar)
        Me.tabTranslation.Controls.Add(Me.lblProgress)
        Me.tabTranslation.Controls.Add(Me.lblQueueStats)
        Me.tabTranslation.Controls.Add(Me.lblResources)
        Me.tabTranslation.Controls.Add(Me.grpResults)
        Me.tabTranslation.Location = New Point(4, 26)
        Me.tabTranslation.Name = "tabTranslation"
        Me.tabTranslation.Padding = New Padding(8)
        Me.tabTranslation.Size = New Size(566, 680)
        Me.tabTranslation.TabIndex = 0
        Me.tabTranslation.Text = "Translation Pipeline"
        Me.tabTranslation.UseVisualStyleBackColor = True

        ' tabStt
        Me.tabStt.Controls.Add(Me.lblSttTitle)
        Me.tabStt.Controls.Add(Me.lblSttAudioFile)
        Me.tabStt.Controls.Add(Me.txtSttAudioFile)
        Me.tabStt.Controls.Add(Me.btnSttBrowse)
        Me.tabStt.Controls.Add(Me.lblSttIterations)
        Me.tabStt.Controls.Add(Me.nudSttIterations)
        Me.tabStt.Controls.Add(Me.chkSttFasterWhisper)
        Me.tabStt.Controls.Add(Me.chkSttCuda)
        Me.tabStt.Controls.Add(Me.chkSttVulkan)
        Me.tabStt.Controls.Add(Me.chkSttCpu)
        Me.tabStt.Controls.Add(Me.btnSttCompare)
        Me.tabStt.Controls.Add(Me.btnSttCancel)
        Me.tabStt.Controls.Add(Me.btnSttExport)
        Me.tabStt.Controls.Add(Me.lblSttProgress)
        Me.tabStt.Controls.Add(Me.dgvSttCompare)
        Me.tabStt.Location = New Point(4, 26)
        Me.tabStt.Name = "tabStt"
        Me.tabStt.Padding = New Padding(8)
        Me.tabStt.Size = New Size(566, 680)
        Me.tabStt.TabIndex = 1
        Me.tabStt.Text = "STT Engines"
        Me.tabStt.UseVisualStyleBackColor = True

        ' tabTts
        Me.tabTts.Controls.Add(Me.lblTtsTitle)
        Me.tabTts.Controls.Add(Me.lblTtsPlaceholder)
        Me.tabTts.Location = New Point(4, 26)
        Me.tabTts.Name = "tabTts"
        Me.tabTts.Padding = New Padding(8)
        Me.tabTts.Size = New Size(566, 680)
        Me.tabTts.TabIndex = 2
        Me.tabTts.Text = "TTS Engines"
        Me.tabTts.UseVisualStyleBackColor = True

        ' ══════════════════════════════════════════════════
        '  Translation Pipeline tab content
        ' ══════════════════════════════════════════════════

        ' lblTitle
        Me.lblTitle.AutoSize = True
        Me.lblTitle.Font = New Font("Segoe UI", 14.0!, FontStyle.Bold)
        Me.lblTitle.Location = New Point(16, 12)
        Me.lblTitle.Name = "lblTitle"
        Me.lblTitle.Size = New Size(230, 25)
        Me.lblTitle.Text = "Pipeline Benchmark"

        ' grpConfig
        Me.grpConfig.Controls.Add(Me.lblDomain)
        Me.grpConfig.Controls.Add(Me.cboDomain)
        Me.grpConfig.Controls.Add(Me.lblTargets)
        Me.grpConfig.Controls.Add(Me.clbTargets)
        Me.grpConfig.Controls.Add(Me.lblConcurrency)
        Me.grpConfig.Controls.Add(Me.nudConcurrency)
        Me.grpConfig.Controls.Add(Me.lblIterations)
        Me.grpConfig.Controls.Add(Me.nudIterations)
        Me.grpConfig.Controls.Add(Me.lblCorpusInfo)
        Me.grpConfig.Location = New Point(16, 46)
        Me.grpConfig.Name = "grpConfig"
        Me.grpConfig.Size = New Size(530, 180)
        Me.grpConfig.TabIndex = 0
        Me.grpConfig.Text = "Configuration"

        ' lblDomain
        Me.lblDomain.AutoSize = True
        Me.lblDomain.Location = New Point(12, 24)
        Me.lblDomain.Name = "lblDomain"
        Me.lblDomain.Text = "Domain:"

        ' cboDomain
        Me.cboDomain.DropDownStyle = ComboBoxStyle.DropDownList
        Me.cboDomain.Location = New Point(100, 21)
        Me.cboDomain.Name = "cboDomain"
        Me.cboDomain.Size = New Size(150, 23)

        ' lblTargets
        Me.lblTargets.AutoSize = True
        Me.lblTargets.Location = New Point(12, 55)
        Me.lblTargets.Name = "lblTargets"
        Me.lblTargets.Text = "Targets:"

        ' clbTargets
        Me.clbTargets.CheckOnClick = True
        Me.clbTargets.Location = New Point(100, 52)
        Me.clbTargets.Name = "clbTargets"
        Me.clbTargets.Size = New Size(150, 94)

        ' lblConcurrency
        Me.lblConcurrency.AutoSize = True
        Me.lblConcurrency.Location = New Point(280, 24)
        Me.lblConcurrency.Name = "lblConcurrency"
        Me.lblConcurrency.Text = "Concurrency:"

        ' nudConcurrency
        Me.nudConcurrency.Location = New Point(380, 22)
        Me.nudConcurrency.Minimum = 1
        Me.nudConcurrency.Maximum = 20
        Me.nudConcurrency.Value = 1
        Me.nudConcurrency.Name = "nudConcurrency"
        Me.nudConcurrency.Size = New Size(60, 23)

        ' lblIterations
        Me.lblIterations.AutoSize = True
        Me.lblIterations.Location = New Point(280, 55)
        Me.lblIterations.Name = "lblIterations"
        Me.lblIterations.Text = "Iterations:"

        ' nudIterations
        Me.nudIterations.Location = New Point(380, 53)
        Me.nudIterations.Minimum = 1
        Me.nudIterations.Maximum = 50
        Me.nudIterations.Value = 1
        Me.nudIterations.Name = "nudIterations"
        Me.nudIterations.Size = New Size(60, 23)

        ' lblCorpusInfo
        Me.lblCorpusInfo.AutoSize = True
        Me.lblCorpusInfo.Location = New Point(280, 90)
        Me.lblCorpusInfo.Name = "lblCorpusInfo"
        Me.lblCorpusInfo.ForeColor = Color.Gray
        Me.lblCorpusInfo.Text = ""

        ' grpStages
        Me.grpStages.Controls.Add(Me.chkTranslation)
        Me.grpStages.Controls.Add(Me.chkTts)
        Me.grpStages.Controls.Add(Me.chkStt)
        Me.grpStages.Location = New Point(16, 232)
        Me.grpStages.Name = "grpStages"
        Me.grpStages.Size = New Size(340, 50)
        Me.grpStages.TabIndex = 2
        Me.grpStages.Text = "Pipeline Stages"

        ' chkTranslation
        Me.chkTranslation.AutoSize = True
        Me.chkTranslation.Checked = True
        Me.chkTranslation.CheckState = CheckState.Checked
        Me.chkTranslation.Location = New Point(12, 22)
        Me.chkTranslation.Name = "chkTranslation"
        Me.chkTranslation.Text = "Translation"

        ' chkTts
        Me.chkTts.AutoSize = True
        Me.chkTts.Location = New Point(120, 22)
        Me.chkTts.Name = "chkTts"
        Me.chkTts.Text = "TTS"

        ' chkStt
        Me.chkStt.AutoSize = True
        Me.chkStt.Enabled = False
        Me.chkStt.Location = New Point(200, 22)
        Me.chkStt.Name = "chkStt"
        Me.chkStt.Text = "STT (requires TTS)"

        ' btnRun
        Me.btnRun.Location = New Point(16, 290)
        Me.btnRun.Name = "btnRun"
        Me.btnRun.Size = New Size(100, 30)
        Me.btnRun.Text = "Run"
        Me.btnRun.UseVisualStyleBackColor = True

        ' btnCancel
        Me.btnCancel.Enabled = False
        Me.btnCancel.Location = New Point(124, 290)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New Size(100, 30)
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True

        ' btnExport
        Me.btnExport.Enabled = False
        Me.btnExport.Location = New Point(446, 290)
        Me.btnExport.Name = "btnExport"
        Me.btnExport.Size = New Size(100, 30)
        Me.btnExport.Text = "Export CSV"
        Me.btnExport.UseVisualStyleBackColor = True

        ' progressBar
        Me.progressBar.Location = New Point(16, 328)
        Me.progressBar.Name = "progressBar"
        Me.progressBar.Size = New Size(530, 20)

        ' lblProgress
        Me.lblProgress.AutoSize = True
        Me.lblProgress.Location = New Point(16, 352)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Text = ""

        ' lblQueueStats
        Me.lblQueueStats.AutoSize = True
        Me.lblQueueStats.Location = New Point(16, 372)
        Me.lblQueueStats.Name = "lblQueueStats"
        Me.lblQueueStats.ForeColor = Color.Gray
        Me.lblQueueStats.Text = ""

        ' lblResources
        Me.lblResources.AutoSize = True
        Me.lblResources.Location = New Point(16, 392)
        Me.lblResources.Name = "lblResources"
        Me.lblResources.ForeColor = Color.Gray
        Me.lblResources.MaximumSize = New Size(530, 60)
        Me.lblResources.Text = ""

        ' grpResults
        Me.grpResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.grpResults.Controls.Add(Me.dgvResults)
        Me.grpResults.Controls.Add(Me.lblSummary)
        Me.grpResults.Location = New Point(16, 460)
        Me.grpResults.Name = "grpResults"
        Me.grpResults.Size = New Size(530, 210)
        Me.grpResults.TabIndex = 1
        Me.grpResults.Text = "Results"

        ' lblSummary
        Me.lblSummary.AutoSize = True
        Me.lblSummary.Location = New Point(12, 22)
        Me.lblSummary.Name = "lblSummary"
        Me.lblSummary.Text = "Run a benchmark to see results."

        ' dgvResults
        Me.dgvResults.AllowUserToAddRows = False
        Me.dgvResults.AllowUserToDeleteRows = False
        Me.dgvResults.AllowUserToResizeRows = False
        Me.dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvResults.Columns.AddRange(New DataGridViewColumn() {Me.colStage, Me.colPair, Me.colCount, Me.colAvgLatency, Me.colAvgQuality, Me.colMinQuality, Me.colMaxQuality})
        Me.dgvResults.Location = New Point(12, 72)
        Me.dgvResults.Name = "dgvResults"
        Me.dgvResults.ReadOnly = True
        Me.dgvResults.RowHeadersVisible = False
        Me.dgvResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvResults.Size = New Size(506, 130)
        Me.dgvResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

        ' colStage
        Me.colStage.HeaderText = "Stage"
        Me.colStage.Name = "colStage"
        Me.colStage.FillWeight = 16

        ' colPair
        Me.colPair.HeaderText = "Language"
        Me.colPair.Name = "colPair"
        Me.colPair.FillWeight = 24

        ' colCount
        Me.colCount.HeaderText = "Samples"
        Me.colCount.Name = "colCount"
        Me.colCount.FillWeight = 12

        ' colAvgLatency
        Me.colAvgLatency.HeaderText = "Avg Latency (ms)"
        Me.colAvgLatency.Name = "colAvgLatency"
        Me.colAvgLatency.FillWeight = 18

        ' colAvgQuality
        Me.colAvgQuality.HeaderText = "Quality/Success %"
        Me.colAvgQuality.Name = "colAvgQuality"
        Me.colAvgQuality.FillWeight = 16

        ' colMinQuality
        Me.colMinQuality.HeaderText = "Min %"
        Me.colMinQuality.Name = "colMinQuality"
        Me.colMinQuality.FillWeight = 8

        ' colMaxQuality
        Me.colMaxQuality.HeaderText = "Max %"
        Me.colMaxQuality.Name = "colMaxQuality"
        Me.colMaxQuality.FillWeight = 8

        ' ══════════════════════════════════════════════════
        '  STT Engines tab content
        ' ══════════════════════════════════════════════════

        ' lblSttTitle
        Me.lblSttTitle.AutoSize = True
        Me.lblSttTitle.Font = New Font("Segoe UI", 14.0!, FontStyle.Bold)
        Me.lblSttTitle.Location = New Point(16, 12)
        Me.lblSttTitle.Name = "lblSttTitle"
        Me.lblSttTitle.Text = "STT Engine Comparison"

        ' lblSttAudioFile
        Me.lblSttAudioFile.AutoSize = True
        Me.lblSttAudioFile.Location = New Point(16, 52)
        Me.lblSttAudioFile.Name = "lblSttAudioFile"
        Me.lblSttAudioFile.Text = "Test audio (WAV):"

        ' txtSttAudioFile
        Me.txtSttAudioFile.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtSttAudioFile.Location = New Point(140, 49)
        Me.txtSttAudioFile.Name = "txtSttAudioFile"
        Me.txtSttAudioFile.Size = New Size(350, 23)

        ' btnSttBrowse
        Me.btnSttBrowse.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnSttBrowse.Location = New Point(496, 48)
        Me.btnSttBrowse.Name = "btnSttBrowse"
        Me.btnSttBrowse.Size = New Size(52, 25)
        Me.btnSttBrowse.Text = "..."
        Me.btnSttBrowse.UseVisualStyleBackColor = True

        ' lblSttIterations
        Me.lblSttIterations.AutoSize = True
        Me.lblSttIterations.Location = New Point(16, 84)
        Me.lblSttIterations.Name = "lblSttIterations"
        Me.lblSttIterations.Text = "Iterations:"

        ' nudSttIterations
        Me.nudSttIterations.Location = New Point(140, 82)
        Me.nudSttIterations.Minimum = 1
        Me.nudSttIterations.Maximum = 20
        Me.nudSttIterations.Value = 3
        Me.nudSttIterations.Name = "nudSttIterations"
        Me.nudSttIterations.Size = New Size(60, 23)

        ' chkSttFasterWhisper
        Me.chkSttFasterWhisper.AutoSize = True
        Me.chkSttFasterWhisper.Checked = True
        Me.chkSttFasterWhisper.CheckState = CheckState.Checked
        Me.chkSttFasterWhisper.Location = New Point(16, 114)
        Me.chkSttFasterWhisper.Name = "chkSttFasterWhisper"
        Me.chkSttFasterWhisper.Text = "Faster Whisper"

        ' chkSttCuda
        Me.chkSttCuda.AutoSize = True
        Me.chkSttCuda.Checked = True
        Me.chkSttCuda.CheckState = CheckState.Checked
        Me.chkSttCuda.Location = New Point(150, 114)
        Me.chkSttCuda.Name = "chkSttCuda"
        Me.chkSttCuda.Text = "CUDA"

        ' chkSttVulkan
        Me.chkSttVulkan.AutoSize = True
        Me.chkSttVulkan.Checked = True
        Me.chkSttVulkan.CheckState = CheckState.Checked
        Me.chkSttVulkan.Location = New Point(240, 114)
        Me.chkSttVulkan.Name = "chkSttVulkan"
        Me.chkSttVulkan.Text = "Vulkan"

        ' chkSttCpu
        Me.chkSttCpu.AutoSize = True
        Me.chkSttCpu.Checked = True
        Me.chkSttCpu.CheckState = CheckState.Checked
        Me.chkSttCpu.Location = New Point(330, 114)
        Me.chkSttCpu.Name = "chkSttCpu"
        Me.chkSttCpu.Text = "CPU"

        ' btnSttCompare
        Me.btnSttCompare.Location = New Point(16, 144)
        Me.btnSttCompare.Name = "btnSttCompare"
        Me.btnSttCompare.Size = New Size(130, 28)
        Me.btnSttCompare.Text = "Compare Engines"
        Me.btnSttCompare.UseVisualStyleBackColor = True

        ' btnSttCancel
        Me.btnSttCancel.Enabled = False
        Me.btnSttCancel.Location = New Point(152, 144)
        Me.btnSttCancel.Name = "btnSttCancel"
        Me.btnSttCancel.Size = New Size(80, 28)
        Me.btnSttCancel.Text = "Cancel"
        Me.btnSttCancel.UseVisualStyleBackColor = True

        ' btnSttExport
        Me.btnSttExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnSttExport.Enabled = False
        Me.btnSttExport.Location = New Point(448, 144)
        Me.btnSttExport.Name = "btnSttExport"
        Me.btnSttExport.Size = New Size(100, 28)
        Me.btnSttExport.Text = "Export CSV"
        Me.btnSttExport.UseVisualStyleBackColor = True

        ' lblSttProgress
        Me.lblSttProgress.AutoSize = True
        Me.lblSttProgress.Location = New Point(16, 180)
        Me.lblSttProgress.Name = "lblSttProgress"
        Me.lblSttProgress.ForeColor = Color.Gray
        Me.lblSttProgress.MaximumSize = New Size(530, 40)
        Me.lblSttProgress.Text = "Select a WAV file and click Compare."

        ' dgvSttCompare
        Me.dgvSttCompare.AllowUserToAddRows = False
        Me.dgvSttCompare.AllowUserToDeleteRows = False
        Me.dgvSttCompare.AllowUserToResizeRows = False
        Me.dgvSttCompare.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.dgvSttCompare.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvSttCompare.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvSttCompare.Columns.AddRange(New DataGridViewColumn() {Me.colSttEngine, Me.colSttLoadTime, Me.colSttAvgMs, Me.colSttMinMs, Me.colSttMaxMs, Me.colSttSpeedup, Me.colSttText})
        Me.dgvSttCompare.Location = New Point(16, 206)
        Me.dgvSttCompare.Name = "dgvSttCompare"
        Me.dgvSttCompare.ReadOnly = True
        Me.dgvSttCompare.RowHeadersVisible = False
        Me.dgvSttCompare.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvSttCompare.Size = New Size(530, 456)

        ' colSttEngine
        Me.colSttEngine.HeaderText = "Engine"
        Me.colSttEngine.Name = "colSttEngine"
        Me.colSttEngine.FillWeight = 22

        ' colSttLoadTime
        Me.colSttLoadTime.HeaderText = "Model Load"
        Me.colSttLoadTime.Name = "colSttLoadTime"
        Me.colSttLoadTime.FillWeight = 12

        ' colSttAvgMs
        Me.colSttAvgMs.HeaderText = "Avg (ms)"
        Me.colSttAvgMs.Name = "colSttAvgMs"
        Me.colSttAvgMs.FillWeight = 10

        ' colSttMinMs
        Me.colSttMinMs.HeaderText = "Min (ms)"
        Me.colSttMinMs.Name = "colSttMinMs"
        Me.colSttMinMs.FillWeight = 10

        ' colSttMaxMs
        Me.colSttMaxMs.HeaderText = "Max (ms)"
        Me.colSttMaxMs.Name = "colSttMaxMs"
        Me.colSttMaxMs.FillWeight = 10

        ' colSttSpeedup
        Me.colSttSpeedup.HeaderText = "Speedup"
        Me.colSttSpeedup.Name = "colSttSpeedup"
        Me.colSttSpeedup.FillWeight = 10

        ' colSttText
        Me.colSttText.HeaderText = "Transcription"
        Me.colSttText.Name = "colSttText"
        Me.colSttText.FillWeight = 26

        ' ══════════════════════════════════════════════════
        '  TTS Engines tab content (placeholder)
        ' ══════════════════════════════════════════════════

        ' lblTtsTitle
        Me.lblTtsTitle.AutoSize = True
        Me.lblTtsTitle.Font = New Font("Segoe UI", 14.0!, FontStyle.Bold)
        Me.lblTtsTitle.Location = New Point(16, 12)
        Me.lblTtsTitle.Name = "lblTtsTitle"
        Me.lblTtsTitle.Text = "TTS Engine Comparison"

        ' lblTtsPlaceholder
        Me.lblTtsPlaceholder.AutoSize = True
        Me.lblTtsPlaceholder.ForeColor = Color.Gray
        Me.lblTtsPlaceholder.Location = New Point(16, 52)
        Me.lblTtsPlaceholder.Name = "lblTtsPlaceholder"
        Me.lblTtsPlaceholder.Text = "TTS engine comparison coming soon. Use the Translation Pipeline tab to benchmark TTS as part of the full pipeline."

        ' ══════════════════════════════════════════════════
        '  FormTranslationBenchmark
        ' ══════════════════════════════════════════════════
        Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(574, 710)
        Me.Controls.Add(Me.tabMain)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimizeBox = False
        Me.MaximizeBox = True
        Me.MinimumSize = New Size(590, 600)
        Me.Name = "FormTranslationBenchmark"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Text = "Engine Benchmark"

        Me.tabMain.ResumeLayout(False)
        Me.tabTranslation.ResumeLayout(False)
        Me.tabTranslation.PerformLayout()
        Me.tabStt.ResumeLayout(False)
        Me.tabStt.PerformLayout()
        Me.tabTts.ResumeLayout(False)
        Me.tabTts.PerformLayout()
        Me.grpConfig.ResumeLayout(False)
        Me.grpConfig.PerformLayout()
        Me.grpStages.ResumeLayout(False)
        Me.grpStages.PerformLayout()
        CType(Me.nudConcurrency, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudIterations, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpResults.ResumeLayout(False)
        Me.grpResults.PerformLayout()
        CType(Me.dgvResults, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudSttIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvSttCompare, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabTranslation As TabPage
    Friend WithEvents tabStt As TabPage
    Friend WithEvents tabTts As TabPage

    Friend WithEvents lblTitle As Label
    Friend WithEvents grpConfig As GroupBox
    Friend WithEvents lblDomain As Label
    Friend WithEvents cboDomain As ComboBox
    Friend WithEvents lblTargets As Label
    Friend WithEvents clbTargets As CheckedListBox
    Friend WithEvents lblConcurrency As Label
    Friend WithEvents nudConcurrency As NumericUpDown
    Friend WithEvents lblIterations As Label
    Friend WithEvents nudIterations As NumericUpDown
    Friend WithEvents lblCorpusInfo As Label
    Friend WithEvents grpStages As GroupBox
    Friend WithEvents chkTranslation As CheckBox
    Friend WithEvents chkTts As CheckBox
    Friend WithEvents chkStt As CheckBox
    Friend WithEvents btnRun As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents btnExport As Button
    Friend WithEvents progressBar As ProgressBar
    Friend WithEvents lblProgress As Label
    Friend WithEvents grpResults As GroupBox
    Friend WithEvents dgvResults As DataGridView
    Friend WithEvents colStage As DataGridViewTextBoxColumn
    Friend WithEvents colPair As DataGridViewTextBoxColumn
    Friend WithEvents colCount As DataGridViewTextBoxColumn
    Friend WithEvents colAvgLatency As DataGridViewTextBoxColumn
    Friend WithEvents colAvgQuality As DataGridViewTextBoxColumn
    Friend WithEvents colMinQuality As DataGridViewTextBoxColumn
    Friend WithEvents colMaxQuality As DataGridViewTextBoxColumn
    Friend WithEvents lblSummary As Label
    Friend WithEvents lblQueueStats As Label
    Friend WithEvents lblResources As Label

    Friend WithEvents lblSttTitle As Label
    Friend WithEvents lblSttAudioFile As Label
    Friend WithEvents txtSttAudioFile As TextBox
    Friend WithEvents btnSttBrowse As Button
    Friend WithEvents lblSttIterations As Label
    Friend WithEvents nudSttIterations As NumericUpDown
    Friend WithEvents chkSttFasterWhisper As CheckBox
    Friend WithEvents chkSttCuda As CheckBox
    Friend WithEvents chkSttVulkan As CheckBox
    Friend WithEvents chkSttCpu As CheckBox
    Friend WithEvents btnSttCompare As Button
    Friend WithEvents btnSttCancel As Button
    Friend WithEvents btnSttExport As Button
    Friend WithEvents lblSttProgress As Label
    Friend WithEvents dgvSttCompare As DataGridView
    Friend WithEvents colSttEngine As DataGridViewTextBoxColumn
    Friend WithEvents colSttLoadTime As DataGridViewTextBoxColumn
    Friend WithEvents colSttAvgMs As DataGridViewTextBoxColumn
    Friend WithEvents colSttMinMs As DataGridViewTextBoxColumn
    Friend WithEvents colSttMaxMs As DataGridViewTextBoxColumn
    Friend WithEvents colSttSpeedup As DataGridViewTextBoxColumn
    Friend WithEvents colSttText As DataGridViewTextBoxColumn

    Friend WithEvents lblTtsTitle As Label
    Friend WithEvents lblTtsPlaceholder As Label

End Class
