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

        ' ── Translation nested tab controls ──
        Me.tabTransInner = New TabControl()
        Me.tabTransPipeline = New TabPage()
        Me.tabTransConcurrency = New TabPage()
        Me.tabTransResources = New TabPage()

        ' ── Translation Pipeline controls ──
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
        Me.btnRun = New Button()
        Me.btnCancel = New Button()
        Me.btnExport = New Button()
        Me.progressBar = New ProgressBar()
        Me.lblProgress = New Label()
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

        ' ── Translation Concurrency controls ──
        Me.lblTransConcTargets = New Label()
        Me.clbTransConcTargets = New CheckedListBox()
        Me.lblTransConcIterations = New Label()
        Me.nudTransConcIterations = New NumericUpDown()
        Me.lblTransConcLevels = New Label()
        Me.txtTransConcLevels = New TextBox()
        Me.btnTransConcRun = New Button()
        Me.btnTransConcCancel = New Button()
        Me.btnTransConcExport = New Button()
        Me.lblTransConcProgress = New Label()
        Me.dgvTransConcurrent = New DataGridView()
        Me.colTransConcLevel = New DataGridViewTextBoxColumn()
        Me.colTransConcRequests = New DataGridViewTextBoxColumn()
        Me.colTransConcWall = New DataGridViewTextBoxColumn()
        Me.colTransConcAvg = New DataGridViewTextBoxColumn()
        Me.colTransConcP50 = New DataGridViewTextBoxColumn()
        Me.colTransConcP95 = New DataGridViewTextBoxColumn()
        Me.colTransConcMax = New DataGridViewTextBoxColumn()
        Me.colTransConcThroughput = New DataGridViewTextBoxColumn()
        Me.colTransConcErrors = New DataGridViewTextBoxColumn()
        Me.lblTransConcSummary = New Label()

        ' ── Translation Resources controls ──
        Me.lblResources = New Label()

        ' ── STT nested tab controls ──
        Me.tabSttInner = New TabControl()
        Me.tabSttComparison = New TabPage()
        Me.tabSttConcurrency = New TabPage()
        Me.tabSttResources = New TabPage()

        ' ── STT Comparison controls ──
        Me.lblSttAudioFile = New Label()
        Me.txtSttAudioFile = New TextBox()
        Me.btnSttBrowse = New Button()
        Me.lblSttIterations = New Label()
        Me.nudSttIterations = New NumericUpDown()
        Me.chkSttCuda = New CheckBox()
        Me.chkSttVulkan = New CheckBox()
        Me.chkSttCpu = New CheckBox()
        Me.chkSttFasterWhisper = New CheckBox()
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

        ' ── STT Concurrency controls ──
        Me.lblConcAudioFile = New Label()
        Me.txtConcAudioFile = New TextBox()
        Me.btnConcBrowse = New Button()
        Me.lblConcIterations = New Label()
        Me.nudConcIterations = New NumericUpDown()
        Me.lblConcLevels = New Label()
        Me.txtConcLevels = New TextBox()
        Me.btnConcRun = New Button()
        Me.btnConcCancel = New Button()
        Me.btnConcExport = New Button()
        Me.lblConcProgress = New Label()
        Me.dgvConcurrent = New DataGridView()
        Me.colConcLevel = New DataGridViewTextBoxColumn()
        Me.colConcRequests = New DataGridViewTextBoxColumn()
        Me.colConcWall = New DataGridViewTextBoxColumn()
        Me.colConcAvg = New DataGridViewTextBoxColumn()
        Me.colConcP50 = New DataGridViewTextBoxColumn()
        Me.colConcP95 = New DataGridViewTextBoxColumn()
        Me.colConcMax = New DataGridViewTextBoxColumn()
        Me.colConcThroughput = New DataGridViewTextBoxColumn()
        Me.colConcErrors = New DataGridViewTextBoxColumn()
        Me.lblConcSummary = New Label()

        ' ── STT Resources controls ──
        Me.lblSttResources = New Label()

        ' ── TTS nested tab controls ──
        Me.tabTtsInner = New TabControl()
        Me.tabTtsComparison = New TabPage()
        Me.tabTtsConcurrency = New TabPage()
        Me.tabTtsResources = New TabPage()

        ' ── TTS Comparison controls ──
        Me.lblTtsText = New Label()
        Me.txtTtsText = New TextBox()
        Me.lblTtsLanguage = New Label()
        Me.cboTtsLanguage = New ComboBox()
        Me.lblTtsBackends = New Label()
        Me.clbTtsBackends = New CheckedListBox()
        Me.lblTtsIterations = New Label()
        Me.nudTtsIterations = New NumericUpDown()
        Me.btnTtsCompare = New Button()
        Me.btnTtsCancel = New Button()
        Me.btnTtsExport = New Button()
        Me.lblTtsProgress = New Label()
        Me.dgvTtsCompare = New DataGridView()
        Me.colTtsEngine = New DataGridViewTextBoxColumn()
        Me.colTtsAvgMs = New DataGridViewTextBoxColumn()
        Me.colTtsMinMs = New DataGridViewTextBoxColumn()
        Me.colTtsMaxMs = New DataGridViewTextBoxColumn()
        Me.colTtsP95Ms = New DataGridViewTextBoxColumn()
        Me.colTtsSpeedup = New DataGridViewTextBoxColumn()
        Me.colTtsAudioSize = New DataGridViewTextBoxColumn()
        Me.colTtsCodec = New DataGridViewTextBoxColumn()

        ' ── TTS Concurrency controls ──
        Me.lblTtsConcText = New Label()
        Me.txtTtsConcText = New TextBox()
        Me.lblTtsConcLanguage = New Label()
        Me.cboTtsConcLanguage = New ComboBox()
        Me.lblTtsConcBackend = New Label()
        Me.cboTtsConcBackend = New ComboBox()
        Me.lblTtsConcIterations = New Label()
        Me.nudTtsConcIterations = New NumericUpDown()
        Me.lblTtsConcLevels = New Label()
        Me.txtTtsConcLevels = New TextBox()
        Me.btnTtsConcRun = New Button()
        Me.btnTtsConcCancel = New Button()
        Me.btnTtsConcExport = New Button()
        Me.lblTtsConcProgress = New Label()
        Me.dgvTtsConcurrent = New DataGridView()
        Me.lblTtsConcSummary = New Label()

        ' ── TTS Resources controls ──
        Me.lblTtsResources = New Label()

        ' ── Bottom panel ──
        Me.pnlBottom = New Panel()
        Me.btnExportAll = New Button()
        Me.lblAutoSaveStatus = New Label()

        Me.pnlBottom.SuspendLayout()
        Me.tabMain.SuspendLayout()
        Me.tabTranslation.SuspendLayout()
        Me.tabTransInner.SuspendLayout()
        Me.tabTransPipeline.SuspendLayout()
        Me.tabTransConcurrency.SuspendLayout()
        Me.tabTransResources.SuspendLayout()
        Me.tabStt.SuspendLayout()
        Me.tabSttInner.SuspendLayout()
        Me.tabSttComparison.SuspendLayout()
        Me.tabSttConcurrency.SuspendLayout()
        Me.tabSttResources.SuspendLayout()
        Me.tabTts.SuspendLayout()
        Me.tabTtsInner.SuspendLayout()
        Me.tabTtsComparison.SuspendLayout()
        Me.tabTtsConcurrency.SuspendLayout()
        Me.tabTtsResources.SuspendLayout()
        CType(Me.nudTtsIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTtsConcIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvTtsCompare, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvTtsConcurrent, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.grpConfig.SuspendLayout()
        Me.grpResults.SuspendLayout()
        CType(Me.nudConcurrency, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudSttIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudConcIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTransConcIterations, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvResults, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvSttCompare, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvConcurrent, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.dgvTransConcurrent, System.ComponentModel.ISupportInitialize).BeginInit()
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

        ' tabTranslation — contains nested tabTransInner
        Me.tabTranslation.Controls.Add(Me.tabTransInner)
        Me.tabTranslation.Location = New Point(4, 26)
        Me.tabTranslation.Name = "tabTranslation"
        Me.tabTranslation.Size = New Size(566, 680)
        Me.tabTranslation.TabIndex = 0
        Me.tabTranslation.Text = "Translation"
        Me.tabTranslation.UseVisualStyleBackColor = True

        ' tabTransInner — nested TabControl inside Translation tab
        Me.tabTransInner.Controls.Add(Me.tabTransPipeline)
        Me.tabTransInner.Controls.Add(Me.tabTransConcurrency)
        Me.tabTransInner.Controls.Add(Me.tabTransResources)
        Me.tabTransInner.Dock = DockStyle.Fill
        Me.tabTransInner.Name = "tabTransInner"
        Me.tabTransInner.SelectedIndex = 0
        Me.tabTransInner.TabIndex = 0

        ' tabTransPipeline
        Me.tabTransPipeline.AutoScroll = True
        Me.tabTransPipeline.Controls.Add(Me.grpConfig)
        Me.tabTransPipeline.Controls.Add(Me.btnRun)
        Me.tabTransPipeline.Controls.Add(Me.btnCancel)
        Me.tabTransPipeline.Controls.Add(Me.btnExport)
        Me.tabTransPipeline.Controls.Add(Me.progressBar)
        Me.tabTransPipeline.Controls.Add(Me.lblProgress)
        Me.tabTransPipeline.Controls.Add(Me.lblQueueStats)
        Me.tabTransPipeline.Controls.Add(Me.grpResults)
        Me.tabTransPipeline.Location = New Point(4, 26)
        Me.tabTransPipeline.Name = "tabTransPipeline"
        Me.tabTransPipeline.Padding = New Padding(8)
        Me.tabTransPipeline.Size = New Size(558, 650)
        Me.tabTransPipeline.TabIndex = 0
        Me.tabTransPipeline.Text = "Pipeline"
        Me.tabTransPipeline.UseVisualStyleBackColor = True

        ' tabTransConcurrency
        Me.tabTransConcurrency.Controls.Add(Me.lblTransConcTargets)
        Me.tabTransConcurrency.Controls.Add(Me.clbTransConcTargets)
        Me.tabTransConcurrency.Controls.Add(Me.lblTransConcIterations)
        Me.tabTransConcurrency.Controls.Add(Me.nudTransConcIterations)
        Me.tabTransConcurrency.Controls.Add(Me.lblTransConcLevels)
        Me.tabTransConcurrency.Controls.Add(Me.txtTransConcLevels)
        Me.tabTransConcurrency.Controls.Add(Me.btnTransConcRun)
        Me.tabTransConcurrency.Controls.Add(Me.btnTransConcCancel)
        Me.tabTransConcurrency.Controls.Add(Me.btnTransConcExport)
        Me.tabTransConcurrency.Controls.Add(Me.lblTransConcProgress)
        Me.tabTransConcurrency.Controls.Add(Me.dgvTransConcurrent)
        Me.tabTransConcurrency.Controls.Add(Me.lblTransConcSummary)
        Me.tabTransConcurrency.Location = New Point(4, 26)
        Me.tabTransConcurrency.Name = "tabTransConcurrency"
        Me.tabTransConcurrency.Padding = New Padding(8)
        Me.tabTransConcurrency.Size = New Size(558, 650)
        Me.tabTransConcurrency.TabIndex = 1
        Me.tabTransConcurrency.Text = "Concurrency"
        Me.tabTransConcurrency.UseVisualStyleBackColor = True

        ' tabTransResources
        Me.tabTransResources.Controls.Add(Me.lblResources)
        Me.tabTransResources.Location = New Point(4, 26)
        Me.tabTransResources.Name = "tabTransResources"
        Me.tabTransResources.Padding = New Padding(8)
        Me.tabTransResources.Size = New Size(558, 650)
        Me.tabTransResources.TabIndex = 2
        Me.tabTransResources.Text = "Resources"
        Me.tabTransResources.UseVisualStyleBackColor = True

        ' tabStt — contains nested tabSttInner
        Me.tabStt.Controls.Add(Me.tabSttInner)
        Me.tabStt.Location = New Point(4, 26)
        Me.tabStt.Name = "tabStt"
        Me.tabStt.Size = New Size(566, 680)
        Me.tabStt.TabIndex = 1
        Me.tabStt.Text = "STT Engines"
        Me.tabStt.UseVisualStyleBackColor = True

        ' tabSttInner — nested TabControl inside STT tab
        Me.tabSttInner.Controls.Add(Me.tabSttComparison)
        Me.tabSttInner.Controls.Add(Me.tabSttConcurrency)
        Me.tabSttInner.Controls.Add(Me.tabSttResources)
        Me.tabSttInner.Dock = DockStyle.Fill
        Me.tabSttInner.Name = "tabSttInner"
        Me.tabSttInner.SelectedIndex = 0
        Me.tabSttInner.TabIndex = 0

        ' tabSttComparison
        Me.tabSttComparison.Controls.Add(Me.lblSttAudioFile)
        Me.tabSttComparison.Controls.Add(Me.txtSttAudioFile)
        Me.tabSttComparison.Controls.Add(Me.btnSttBrowse)
        Me.tabSttComparison.Controls.Add(Me.lblSttIterations)
        Me.tabSttComparison.Controls.Add(Me.nudSttIterations)
        Me.tabSttComparison.Controls.Add(Me.chkSttCuda)
        Me.tabSttComparison.Controls.Add(Me.chkSttVulkan)
        Me.tabSttComparison.Controls.Add(Me.chkSttCpu)
        Me.tabSttComparison.Controls.Add(Me.chkSttFasterWhisper)
        Me.tabSttComparison.Controls.Add(Me.btnSttCompare)
        Me.tabSttComparison.Controls.Add(Me.btnSttCancel)
        Me.tabSttComparison.Controls.Add(Me.btnSttExport)
        Me.tabSttComparison.Controls.Add(Me.lblSttProgress)
        Me.tabSttComparison.Controls.Add(Me.dgvSttCompare)
        Me.tabSttComparison.Location = New Point(4, 26)
        Me.tabSttComparison.Name = "tabSttComparison"
        Me.tabSttComparison.Padding = New Padding(8)
        Me.tabSttComparison.Size = New Size(558, 650)
        Me.tabSttComparison.TabIndex = 0
        Me.tabSttComparison.Text = "Comparison"
        Me.tabSttComparison.UseVisualStyleBackColor = True

        ' tabSttConcurrency
        Me.tabSttConcurrency.Controls.Add(Me.lblConcAudioFile)
        Me.tabSttConcurrency.Controls.Add(Me.txtConcAudioFile)
        Me.tabSttConcurrency.Controls.Add(Me.btnConcBrowse)
        Me.tabSttConcurrency.Controls.Add(Me.lblConcIterations)
        Me.tabSttConcurrency.Controls.Add(Me.nudConcIterations)
        Me.tabSttConcurrency.Controls.Add(Me.lblConcLevels)
        Me.tabSttConcurrency.Controls.Add(Me.txtConcLevels)
        Me.tabSttConcurrency.Controls.Add(Me.btnConcRun)
        Me.tabSttConcurrency.Controls.Add(Me.btnConcCancel)
        Me.tabSttConcurrency.Controls.Add(Me.btnConcExport)
        Me.tabSttConcurrency.Controls.Add(Me.lblConcProgress)
        Me.tabSttConcurrency.Controls.Add(Me.dgvConcurrent)
        Me.tabSttConcurrency.Controls.Add(Me.lblConcSummary)
        Me.tabSttConcurrency.Location = New Point(4, 26)
        Me.tabSttConcurrency.Name = "tabSttConcurrency"
        Me.tabSttConcurrency.Padding = New Padding(8)
        Me.tabSttConcurrency.Size = New Size(558, 650)
        Me.tabSttConcurrency.TabIndex = 1
        Me.tabSttConcurrency.Text = "Concurrency"
        Me.tabSttConcurrency.UseVisualStyleBackColor = True

        ' tabSttResources
        Me.tabSttResources.Controls.Add(Me.lblSttResources)
        Me.tabSttResources.Location = New Point(4, 26)
        Me.tabSttResources.Name = "tabSttResources"
        Me.tabSttResources.Padding = New Padding(8)
        Me.tabSttResources.Size = New Size(558, 650)
        Me.tabSttResources.TabIndex = 2
        Me.tabSttResources.Text = "Resources"
        Me.tabSttResources.UseVisualStyleBackColor = True

        ' tabTts — contains nested tabTtsInner
        Me.tabTts.Controls.Add(Me.tabTtsInner)
        Me.tabTts.Location = New Point(4, 26)
        Me.tabTts.Name = "tabTts"
        Me.tabTts.Size = New Size(566, 680)
        Me.tabTts.TabIndex = 2
        Me.tabTts.Text = "TTS Engines"
        Me.tabTts.UseVisualStyleBackColor = True

        ' tabTtsInner — nested TabControl inside TTS tab
        Me.tabTtsInner.Controls.Add(Me.tabTtsComparison)
        Me.tabTtsInner.Controls.Add(Me.tabTtsConcurrency)
        Me.tabTtsInner.Controls.Add(Me.tabTtsResources)
        Me.tabTtsInner.Dock = DockStyle.Fill
        Me.tabTtsInner.Name = "tabTtsInner"

        ' tabTtsComparison
        Me.tabTtsComparison.Controls.Add(Me.lblTtsText)
        Me.tabTtsComparison.Controls.Add(Me.txtTtsText)
        Me.tabTtsComparison.Controls.Add(Me.lblTtsLanguage)
        Me.tabTtsComparison.Controls.Add(Me.cboTtsLanguage)
        Me.tabTtsComparison.Controls.Add(Me.lblTtsBackends)
        Me.tabTtsComparison.Controls.Add(Me.clbTtsBackends)
        Me.tabTtsComparison.Controls.Add(Me.lblTtsIterations)
        Me.tabTtsComparison.Controls.Add(Me.nudTtsIterations)
        Me.tabTtsComparison.Controls.Add(Me.btnTtsCompare)
        Me.tabTtsComparison.Controls.Add(Me.btnTtsCancel)
        Me.tabTtsComparison.Controls.Add(Me.btnTtsExport)
        Me.tabTtsComparison.Controls.Add(Me.lblTtsProgress)
        Me.tabTtsComparison.Controls.Add(Me.dgvTtsCompare)
        Me.tabTtsComparison.Location = New Point(4, 26)
        Me.tabTtsComparison.Name = "tabTtsComparison"
        Me.tabTtsComparison.Padding = New Padding(4)
        Me.tabTtsComparison.Size = New Size(558, 650)
        Me.tabTtsComparison.TabIndex = 0
        Me.tabTtsComparison.Text = "Comparison"
        Me.tabTtsComparison.UseVisualStyleBackColor = True

        ' tabTtsConcurrency
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcText)
        Me.tabTtsConcurrency.Controls.Add(Me.txtTtsConcText)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcLanguage)
        Me.tabTtsConcurrency.Controls.Add(Me.cboTtsConcLanguage)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcBackend)
        Me.tabTtsConcurrency.Controls.Add(Me.cboTtsConcBackend)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcIterations)
        Me.tabTtsConcurrency.Controls.Add(Me.nudTtsConcIterations)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcLevels)
        Me.tabTtsConcurrency.Controls.Add(Me.txtTtsConcLevels)
        Me.tabTtsConcurrency.Controls.Add(Me.btnTtsConcRun)
        Me.tabTtsConcurrency.Controls.Add(Me.btnTtsConcCancel)
        Me.tabTtsConcurrency.Controls.Add(Me.btnTtsConcExport)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcProgress)
        Me.tabTtsConcurrency.Controls.Add(Me.dgvTtsConcurrent)
        Me.tabTtsConcurrency.Controls.Add(Me.lblTtsConcSummary)
        Me.tabTtsConcurrency.Location = New Point(4, 26)
        Me.tabTtsConcurrency.Name = "tabTtsConcurrency"
        Me.tabTtsConcurrency.Padding = New Padding(4)
        Me.tabTtsConcurrency.Size = New Size(558, 650)
        Me.tabTtsConcurrency.TabIndex = 1
        Me.tabTtsConcurrency.Text = "Concurrency"
        Me.tabTtsConcurrency.UseVisualStyleBackColor = True

        ' tabTtsResources
        Me.tabTtsResources.Controls.Add(Me.lblTtsResources)
        Me.tabTtsResources.Location = New Point(4, 26)
        Me.tabTtsResources.Name = "tabTtsResources"
        Me.tabTtsResources.Padding = New Padding(8)
        Me.tabTtsResources.Size = New Size(558, 650)
        Me.tabTtsResources.TabIndex = 2
        Me.tabTtsResources.Text = "Resources"
        Me.tabTtsResources.UseVisualStyleBackColor = True

        ' ══════════════════════════════════════════════════
        '  Translation Pipeline sub-tab content
        ' ══════════════════════════════════════════════════

        ' grpConfig
        Me.grpConfig.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.grpConfig.Controls.Add(Me.lblDomain)
        Me.grpConfig.Controls.Add(Me.cboDomain)
        Me.grpConfig.Controls.Add(Me.lblTargets)
        Me.grpConfig.Controls.Add(Me.clbTargets)
        Me.grpConfig.Controls.Add(Me.lblConcurrency)
        Me.grpConfig.Controls.Add(Me.nudConcurrency)
        Me.grpConfig.Controls.Add(Me.lblIterations)
        Me.grpConfig.Controls.Add(Me.nudIterations)
        Me.grpConfig.Controls.Add(Me.lblCorpusInfo)
        Me.grpConfig.Location = New Point(12, 8)
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

        ' btnRun
        Me.btnRun.Location = New Point(12, 194)
        Me.btnRun.Name = "btnRun"
        Me.btnRun.Size = New Size(100, 30)
        Me.btnRun.Text = "Run"
        Me.btnRun.UseVisualStyleBackColor = True

        ' btnCancel
        Me.btnCancel.Enabled = False
        Me.btnCancel.Location = New Point(120, 194)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New Size(100, 30)
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True

        ' btnExport
        Me.btnExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnExport.Enabled = False
        Me.btnExport.Location = New Point(442, 194)
        Me.btnExport.Name = "btnExport"
        Me.btnExport.Size = New Size(100, 30)
        Me.btnExport.Text = "Export CSV"
        Me.btnExport.UseVisualStyleBackColor = True

        ' progressBar
        Me.progressBar.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.progressBar.Location = New Point(12, 232)
        Me.progressBar.Name = "progressBar"
        Me.progressBar.Size = New Size(530, 20)

        ' lblProgress
        Me.lblProgress.AutoSize = True
        Me.lblProgress.Location = New Point(12, 258)
        Me.lblProgress.Name = "lblProgress"
        Me.lblProgress.Text = ""

        ' lblQueueStats
        Me.lblQueueStats.AutoSize = True
        Me.lblQueueStats.Location = New Point(12, 278)
        Me.lblQueueStats.Name = "lblQueueStats"
        Me.lblQueueStats.ForeColor = Color.Gray
        Me.lblQueueStats.Text = ""

        ' grpResults
        Me.grpResults.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.grpResults.Controls.Add(Me.dgvResults)
        Me.grpResults.Controls.Add(Me.lblSummary)
        Me.grpResults.Location = New Point(12, 302)
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
        '  Translation Concurrency sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblTransConcTargets
        Me.lblTransConcTargets.AutoSize = True
        Me.lblTransConcTargets.Location = New Point(12, 12)
        Me.lblTransConcTargets.Name = "lblTransConcTargets"
        Me.lblTransConcTargets.Text = "Target languages:"

        ' clbTransConcTargets
        Me.clbTransConcTargets.CheckOnClick = True
        Me.clbTransConcTargets.Location = New Point(130, 9)
        Me.clbTransConcTargets.Name = "clbTransConcTargets"
        Me.clbTransConcTargets.Size = New Size(150, 58)

        ' lblTransConcIterations
        Me.lblTransConcIterations.AutoSize = True
        Me.lblTransConcIterations.Location = New Point(300, 12)
        Me.lblTransConcIterations.Name = "lblTransConcIterations"
        Me.lblTransConcIterations.Text = "Rounds per level:"

        ' nudTransConcIterations
        Me.nudTransConcIterations.Location = New Point(420, 10)
        Me.nudTransConcIterations.Minimum = 1
        Me.nudTransConcIterations.Maximum = 20
        Me.nudTransConcIterations.Value = 3
        Me.nudTransConcIterations.Name = "nudTransConcIterations"
        Me.nudTransConcIterations.Size = New Size(60, 23)

        ' lblTransConcLevels
        Me.lblTransConcLevels.AutoSize = True
        Me.lblTransConcLevels.Location = New Point(300, 44)
        Me.lblTransConcLevels.Name = "lblTransConcLevels"
        Me.lblTransConcLevels.Text = "Concurrency levels:"

        ' txtTransConcLevels
        Me.txtTransConcLevels.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtTransConcLevels.Location = New Point(420, 41)
        Me.txtTransConcLevels.Name = "txtTransConcLevels"
        Me.txtTransConcLevels.Size = New Size(130, 23)
        Me.txtTransConcLevels.Text = "1, 2, 5, 10, 20"

        ' btnTransConcRun
        Me.btnTransConcRun.Location = New Point(12, 78)
        Me.btnTransConcRun.Name = "btnTransConcRun"
        Me.btnTransConcRun.Size = New Size(130, 28)
        Me.btnTransConcRun.Text = "Run Throughput"
        Me.btnTransConcRun.UseVisualStyleBackColor = True

        ' btnTransConcCancel
        Me.btnTransConcCancel.Enabled = False
        Me.btnTransConcCancel.Location = New Point(148, 78)
        Me.btnTransConcCancel.Name = "btnTransConcCancel"
        Me.btnTransConcCancel.Size = New Size(80, 28)
        Me.btnTransConcCancel.Text = "Cancel"
        Me.btnTransConcCancel.UseVisualStyleBackColor = True

        ' btnTransConcExport
        Me.btnTransConcExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnTransConcExport.Enabled = False
        Me.btnTransConcExport.Location = New Point(450, 78)
        Me.btnTransConcExport.Name = "btnTransConcExport"
        Me.btnTransConcExport.Size = New Size(100, 28)
        Me.btnTransConcExport.Text = "Export CSV"
        Me.btnTransConcExport.UseVisualStyleBackColor = True

        ' lblTransConcProgress
        Me.lblTransConcProgress.AutoSize = True
        Me.lblTransConcProgress.ForeColor = Color.Gray
        Me.lblTransConcProgress.Location = New Point(12, 114)
        Me.lblTransConcProgress.MaximumSize = New Size(530, 40)
        Me.lblTransConcProgress.Name = "lblTransConcProgress"
        Me.lblTransConcProgress.Text = "Tests N concurrent translation requests to measure throughput."

        ' dgvTransConcurrent
        Me.dgvTransConcurrent.AllowUserToAddRows = False
        Me.dgvTransConcurrent.AllowUserToDeleteRows = False
        Me.dgvTransConcurrent.AllowUserToResizeRows = False
        Me.dgvTransConcurrent.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.dgvTransConcurrent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvTransConcurrent.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTransConcurrent.Columns.AddRange(New DataGridViewColumn() {Me.colTransConcLevel, Me.colTransConcRequests, Me.colTransConcWall, Me.colTransConcAvg, Me.colTransConcP50, Me.colTransConcP95, Me.colTransConcMax, Me.colTransConcThroughput, Me.colTransConcErrors})
        Me.dgvTransConcurrent.Location = New Point(12, 140)
        Me.dgvTransConcurrent.Name = "dgvTransConcurrent"
        Me.dgvTransConcurrent.ReadOnly = True
        Me.dgvTransConcurrent.RowHeadersVisible = False
        Me.dgvTransConcurrent.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvTransConcurrent.Size = New Size(534, 436)

        ' lblTransConcSummary
        Me.lblTransConcSummary.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.lblTransConcSummary.AutoSize = True
        Me.lblTransConcSummary.Location = New Point(12, 582)
        Me.lblTransConcSummary.MaximumSize = New Size(530, 60)
        Me.lblTransConcSummary.Name = "lblTransConcSummary"
        Me.lblTransConcSummary.Text = ""

        ' colTransConcLevel
        Me.colTransConcLevel.HeaderText = "Concurrency"
        Me.colTransConcLevel.Name = "colTransConcLevel"
        Me.colTransConcLevel.FillWeight = 13

        ' colTransConcRequests
        Me.colTransConcRequests.HeaderText = "Requests"
        Me.colTransConcRequests.Name = "colTransConcRequests"
        Me.colTransConcRequests.FillWeight = 11

        ' colTransConcWall
        Me.colTransConcWall.HeaderText = "Wall (ms)"
        Me.colTransConcWall.Name = "colTransConcWall"
        Me.colTransConcWall.FillWeight = 12

        ' colTransConcAvg
        Me.colTransConcAvg.HeaderText = "Avg (ms)"
        Me.colTransConcAvg.Name = "colTransConcAvg"
        Me.colTransConcAvg.FillWeight = 11

        ' colTransConcP50
        Me.colTransConcP50.HeaderText = "P50 (ms)"
        Me.colTransConcP50.Name = "colTransConcP50"
        Me.colTransConcP50.FillWeight = 11

        ' colTransConcP95
        Me.colTransConcP95.HeaderText = "P95 (ms)"
        Me.colTransConcP95.Name = "colTransConcP95"
        Me.colTransConcP95.FillWeight = 11

        ' colTransConcMax
        Me.colTransConcMax.HeaderText = "Max (ms)"
        Me.colTransConcMax.Name = "colTransConcMax"
        Me.colTransConcMax.FillWeight = 11

        ' colTransConcThroughput
        Me.colTransConcThroughput.HeaderText = "Trans/sec"
        Me.colTransConcThroughput.Name = "colTransConcThroughput"
        Me.colTransConcThroughput.FillWeight = 11

        ' colTransConcErrors
        Me.colTransConcErrors.HeaderText = "Errors"
        Me.colTransConcErrors.Name = "colTransConcErrors"
        Me.colTransConcErrors.FillWeight = 9

        ' ══════════════════════════════════════════════════
        '  Translation Resources sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblResources
        Me.lblResources.AutoSize = True
        Me.lblResources.ForeColor = Color.Gray
        Me.lblResources.Location = New Point(12, 12)
        Me.lblResources.MaximumSize = New Size(530, 0)
        Me.lblResources.Name = "lblResources"
        Me.lblResources.Text = "Run a Pipeline or Concurrency test to see resource utilisation (CPU, RAM, GPU, VRAM, temperature)."

        ' ══════════════════════════════════════════════════
        '  STT Comparison sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblSttAudioFile
        Me.lblSttAudioFile.AutoSize = True
        Me.lblSttAudioFile.Location = New Point(12, 12)
        Me.lblSttAudioFile.Name = "lblSttAudioFile"
        Me.lblSttAudioFile.Text = "Test audio (WAV):"

        ' txtSttAudioFile
        Me.txtSttAudioFile.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtSttAudioFile.Location = New Point(130, 9)
        Me.txtSttAudioFile.Name = "txtSttAudioFile"
        Me.txtSttAudioFile.Size = New Size(362, 23)

        ' btnSttBrowse
        Me.btnSttBrowse.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnSttBrowse.Location = New Point(498, 8)
        Me.btnSttBrowse.Name = "btnSttBrowse"
        Me.btnSttBrowse.Size = New Size(52, 25)
        Me.btnSttBrowse.Text = "..."
        Me.btnSttBrowse.UseVisualStyleBackColor = True

        ' lblSttIterations
        Me.lblSttIterations.AutoSize = True
        Me.lblSttIterations.Location = New Point(12, 44)
        Me.lblSttIterations.Name = "lblSttIterations"
        Me.lblSttIterations.Text = "Iterations:"

        ' nudSttIterations
        Me.nudSttIterations.Location = New Point(130, 42)
        Me.nudSttIterations.Minimum = 1
        Me.nudSttIterations.Maximum = 20
        Me.nudSttIterations.Value = 3
        Me.nudSttIterations.Name = "nudSttIterations"
        Me.nudSttIterations.Size = New Size(60, 23)

        ' chkSttCuda
        Me.chkSttCuda.AutoSize = True
        Me.chkSttCuda.Checked = True
        Me.chkSttCuda.CheckState = CheckState.Checked
        Me.chkSttCuda.Location = New Point(12, 74)
        Me.chkSttCuda.Name = "chkSttCuda"
        Me.chkSttCuda.Text = "CUDA"

        ' chkSttVulkan
        Me.chkSttVulkan.AutoSize = True
        Me.chkSttVulkan.Checked = True
        Me.chkSttVulkan.CheckState = CheckState.Checked
        Me.chkSttVulkan.Location = New Point(120, 74)
        Me.chkSttVulkan.Name = "chkSttVulkan"
        Me.chkSttVulkan.Text = "Vulkan"

        ' chkSttCpu
        Me.chkSttCpu.AutoSize = True
        Me.chkSttCpu.Checked = True
        Me.chkSttCpu.CheckState = CheckState.Checked
        Me.chkSttCpu.Location = New Point(220, 74)
        Me.chkSttCpu.Name = "chkSttCpu"
        Me.chkSttCpu.Text = "CPU"

        ' chkSttFasterWhisper
        Me.chkSttFasterWhisper.AutoSize = True
        Me.chkSttFasterWhisper.Checked = True
        Me.chkSttFasterWhisper.CheckState = CheckState.Checked
        Me.chkSttFasterWhisper.Location = New Point(300, 74)
        Me.chkSttFasterWhisper.Name = "chkSttFasterWhisper"
        Me.chkSttFasterWhisper.Text = "faster-whisper"

        ' btnSttCompare
        Me.btnSttCompare.Location = New Point(12, 104)
        Me.btnSttCompare.Name = "btnSttCompare"
        Me.btnSttCompare.Size = New Size(130, 28)
        Me.btnSttCompare.Text = "Compare Engines"
        Me.btnSttCompare.UseVisualStyleBackColor = True

        ' btnSttCancel
        Me.btnSttCancel.Enabled = False
        Me.btnSttCancel.Location = New Point(148, 104)
        Me.btnSttCancel.Name = "btnSttCancel"
        Me.btnSttCancel.Size = New Size(80, 28)
        Me.btnSttCancel.Text = "Cancel"
        Me.btnSttCancel.UseVisualStyleBackColor = True

        ' btnSttExport
        Me.btnSttExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnSttExport.Enabled = False
        Me.btnSttExport.Location = New Point(450, 104)
        Me.btnSttExport.Name = "btnSttExport"
        Me.btnSttExport.Size = New Size(100, 28)
        Me.btnSttExport.Text = "Export CSV"
        Me.btnSttExport.UseVisualStyleBackColor = True

        ' lblSttProgress
        Me.lblSttProgress.AutoSize = True
        Me.lblSttProgress.Location = New Point(12, 140)
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
        Me.dgvSttCompare.Location = New Point(12, 166)
        Me.dgvSttCompare.Name = "dgvSttCompare"
        Me.dgvSttCompare.ReadOnly = True
        Me.dgvSttCompare.RowHeadersVisible = False
        Me.dgvSttCompare.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvSttCompare.Size = New Size(534, 470)

        ' ══════════════════════════════════════════════════
        '  STT Concurrency sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblConcAudioFile
        Me.lblConcAudioFile.AutoSize = True
        Me.lblConcAudioFile.Location = New Point(12, 12)
        Me.lblConcAudioFile.Name = "lblConcAudioFile"
        Me.lblConcAudioFile.Text = "Test audio (WAV):"

        ' txtConcAudioFile
        Me.txtConcAudioFile.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtConcAudioFile.Location = New Point(130, 9)
        Me.txtConcAudioFile.Name = "txtConcAudioFile"
        Me.txtConcAudioFile.Size = New Size(362, 23)

        ' btnConcBrowse
        Me.btnConcBrowse.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnConcBrowse.Location = New Point(498, 8)
        Me.btnConcBrowse.Name = "btnConcBrowse"
        Me.btnConcBrowse.Size = New Size(52, 25)
        Me.btnConcBrowse.Text = "..."
        Me.btnConcBrowse.UseVisualStyleBackColor = True

        ' lblConcIterations
        Me.lblConcIterations.AutoSize = True
        Me.lblConcIterations.Location = New Point(12, 44)
        Me.lblConcIterations.Name = "lblConcIterations"
        Me.lblConcIterations.Text = "Rounds per level:"

        ' nudConcIterations
        Me.nudConcIterations.Location = New Point(130, 42)
        Me.nudConcIterations.Minimum = 1
        Me.nudConcIterations.Maximum = 20
        Me.nudConcIterations.Value = 3
        Me.nudConcIterations.Name = "nudConcIterations"
        Me.nudConcIterations.Size = New Size(60, 23)

        ' lblConcLevels
        Me.lblConcLevels.AutoSize = True
        Me.lblConcLevels.Location = New Point(220, 44)
        Me.lblConcLevels.Name = "lblConcLevels"
        Me.lblConcLevels.Text = "Concurrency levels:"

        ' txtConcLevels
        Me.txtConcLevels.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtConcLevels.Location = New Point(350, 41)
        Me.txtConcLevels.Name = "txtConcLevels"
        Me.txtConcLevels.Size = New Size(200, 23)
        Me.txtConcLevels.Text = "1, 2, 5, 10, 15, 20"

        ' btnConcRun
        Me.btnConcRun.Location = New Point(12, 74)
        Me.btnConcRun.Name = "btnConcRun"
        Me.btnConcRun.Size = New Size(130, 28)
        Me.btnConcRun.Text = "Run Throughput"
        Me.btnConcRun.UseVisualStyleBackColor = True

        ' btnConcCancel
        Me.btnConcCancel.Enabled = False
        Me.btnConcCancel.Location = New Point(148, 74)
        Me.btnConcCancel.Name = "btnConcCancel"
        Me.btnConcCancel.Size = New Size(80, 28)
        Me.btnConcCancel.Text = "Cancel"
        Me.btnConcCancel.UseVisualStyleBackColor = True

        ' btnConcExport
        Me.btnConcExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnConcExport.Enabled = False
        Me.btnConcExport.Location = New Point(450, 74)
        Me.btnConcExport.Name = "btnConcExport"
        Me.btnConcExport.Size = New Size(100, 28)
        Me.btnConcExport.Text = "Export CSV"
        Me.btnConcExport.UseVisualStyleBackColor = True

        ' lblConcProgress
        Me.lblConcProgress.AutoSize = True
        Me.lblConcProgress.ForeColor = Color.Gray
        Me.lblConcProgress.Location = New Point(12, 110)
        Me.lblConcProgress.MaximumSize = New Size(530, 40)
        Me.lblConcProgress.Name = "lblConcProgress"
        Me.lblConcProgress.Text = "Tests N concurrent inference requests to measure real-world throughput."

        ' dgvConcurrent
        Me.dgvConcurrent.AllowUserToAddRows = False
        Me.dgvConcurrent.AllowUserToDeleteRows = False
        Me.dgvConcurrent.AllowUserToResizeRows = False
        Me.dgvConcurrent.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.dgvConcurrent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvConcurrent.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvConcurrent.Columns.AddRange(New DataGridViewColumn() {Me.colConcLevel, Me.colConcRequests, Me.colConcWall, Me.colConcAvg, Me.colConcP50, Me.colConcP95, Me.colConcMax, Me.colConcThroughput, Me.colConcErrors})
        Me.dgvConcurrent.Location = New Point(12, 136)
        Me.dgvConcurrent.Name = "dgvConcurrent"
        Me.dgvConcurrent.ReadOnly = True
        Me.dgvConcurrent.RowHeadersVisible = False
        Me.dgvConcurrent.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvConcurrent.Size = New Size(534, 440)

        ' lblConcSummary
        Me.lblConcSummary.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.lblConcSummary.AutoSize = True
        Me.lblConcSummary.Location = New Point(12, 582)
        Me.lblConcSummary.MaximumSize = New Size(530, 60)
        Me.lblConcSummary.Name = "lblConcSummary"
        Me.lblConcSummary.Text = ""

        ' ══════════════════════════════════════════════════
        '  STT Resources sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblSttResources
        Me.lblSttResources.AutoSize = True
        Me.lblSttResources.ForeColor = Color.Gray
        Me.lblSttResources.Location = New Point(12, 12)
        Me.lblSttResources.MaximumSize = New Size(530, 0)
        Me.lblSttResources.Name = "lblSttResources"
        Me.lblSttResources.Text = "Run a Comparison or Concurrency test to see resource utilisation (CPU, RAM, GPU, VRAM, temperature)."

        ' colConcLevel
        Me.colConcLevel.HeaderText = "Speakers"
        Me.colConcLevel.Name = "colConcLevel"
        Me.colConcLevel.FillWeight = 11

        ' colConcRequests
        Me.colConcRequests.HeaderText = "Requests"
        Me.colConcRequests.Name = "colConcRequests"
        Me.colConcRequests.FillWeight = 11

        ' colConcWall
        Me.colConcWall.HeaderText = "Wall (ms)"
        Me.colConcWall.Name = "colConcWall"
        Me.colConcWall.FillWeight = 12

        ' colConcAvg
        Me.colConcAvg.HeaderText = "Avg (ms)"
        Me.colConcAvg.Name = "colConcAvg"
        Me.colConcAvg.FillWeight = 11

        ' colConcP50
        Me.colConcP50.HeaderText = "P50 (ms)"
        Me.colConcP50.Name = "colConcP50"
        Me.colConcP50.FillWeight = 11

        ' colConcP95
        Me.colConcP95.HeaderText = "P95 (ms)"
        Me.colConcP95.Name = "colConcP95"
        Me.colConcP95.FillWeight = 11

        ' colConcMax
        Me.colConcMax.HeaderText = "Max (ms)"
        Me.colConcMax.Name = "colConcMax"
        Me.colConcMax.FillWeight = 11

        ' colConcThroughput
        Me.colConcThroughput.HeaderText = "Inf/sec"
        Me.colConcThroughput.Name = "colConcThroughput"
        Me.colConcThroughput.FillWeight = 11

        ' colConcErrors
        Me.colConcErrors.HeaderText = "Errors"
        Me.colConcErrors.Name = "colConcErrors"
        Me.colConcErrors.FillWeight = 11

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
        '  TTS Comparison sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblTtsText
        Me.lblTtsText.AutoSize = True
        Me.lblTtsText.Location = New Point(12, 12)
        Me.lblTtsText.Name = "lblTtsText"
        Me.lblTtsText.Text = "Test text:"

        ' txtTtsText
        Me.txtTtsText.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtTtsText.Location = New Point(80, 9)
        Me.txtTtsText.Name = "txtTtsText"
        Me.txtTtsText.Size = New Size(470, 23)
        Me.txtTtsText.Text = "The quick brown fox jumps over the lazy dog."

        ' lblTtsLanguage
        Me.lblTtsLanguage.AutoSize = True
        Me.lblTtsLanguage.Location = New Point(12, 44)
        Me.lblTtsLanguage.Name = "lblTtsLanguage"
        Me.lblTtsLanguage.Text = "Language:"

        ' cboTtsLanguage
        Me.cboTtsLanguage.DropDownStyle = ComboBoxStyle.DropDownList
        Me.cboTtsLanguage.Items.AddRange(New Object() {"eng", "spa", "fra", "deu", "por", "ita", "zho", "rus", "hin", "arb", "jpn", "kor"})
        Me.cboTtsLanguage.Location = New Point(80, 41)
        Me.cboTtsLanguage.Name = "cboTtsLanguage"
        Me.cboTtsLanguage.Size = New Size(100, 23)
        Me.cboTtsLanguage.SelectedIndex = 0

        ' lblTtsBackends
        Me.lblTtsBackends.AutoSize = True
        Me.lblTtsBackends.Location = New Point(200, 44)
        Me.lblTtsBackends.Name = "lblTtsBackends"
        Me.lblTtsBackends.Text = "Backends:"

        ' clbTtsBackends
        Me.clbTtsBackends.CheckOnClick = True
        Me.clbTtsBackends.Location = New Point(270, 41)
        Me.clbTtsBackends.Name = "clbTtsBackends"
        Me.clbTtsBackends.Size = New Size(130, 58)

        ' lblTtsIterations
        Me.lblTtsIterations.AutoSize = True
        Me.lblTtsIterations.Location = New Point(420, 44)
        Me.lblTtsIterations.Name = "lblTtsIterations"
        Me.lblTtsIterations.Text = "Iterations:"

        ' nudTtsIterations
        Me.nudTtsIterations.Location = New Point(490, 42)
        Me.nudTtsIterations.Minimum = 1
        Me.nudTtsIterations.Maximum = 20
        Me.nudTtsIterations.Value = 3
        Me.nudTtsIterations.Name = "nudTtsIterations"
        Me.nudTtsIterations.Size = New Size(60, 23)

        ' btnTtsCompare
        Me.btnTtsCompare.Location = New Point(12, 106)
        Me.btnTtsCompare.Name = "btnTtsCompare"
        Me.btnTtsCompare.Size = New Size(130, 28)
        Me.btnTtsCompare.Text = "Compare Engines"
        Me.btnTtsCompare.UseVisualStyleBackColor = True

        ' btnTtsCancel
        Me.btnTtsCancel.Enabled = False
        Me.btnTtsCancel.Location = New Point(148, 106)
        Me.btnTtsCancel.Name = "btnTtsCancel"
        Me.btnTtsCancel.Size = New Size(80, 28)
        Me.btnTtsCancel.Text = "Cancel"
        Me.btnTtsCancel.UseVisualStyleBackColor = True

        ' btnTtsExport
        Me.btnTtsExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnTtsExport.Enabled = False
        Me.btnTtsExport.Location = New Point(450, 106)
        Me.btnTtsExport.Name = "btnTtsExport"
        Me.btnTtsExport.Size = New Size(100, 28)
        Me.btnTtsExport.Text = "Export CSV"
        Me.btnTtsExport.UseVisualStyleBackColor = True

        ' lblTtsProgress
        Me.lblTtsProgress.AutoSize = True
        Me.lblTtsProgress.ForeColor = Color.Gray
        Me.lblTtsProgress.Location = New Point(12, 142)
        Me.lblTtsProgress.MaximumSize = New Size(530, 40)
        Me.lblTtsProgress.Name = "lblTtsProgress"
        Me.lblTtsProgress.Text = "Select backends and click Compare."

        ' dgvTtsCompare
        Me.dgvTtsCompare.AllowUserToAddRows = False
        Me.dgvTtsCompare.AllowUserToDeleteRows = False
        Me.dgvTtsCompare.AllowUserToResizeRows = False
        Me.dgvTtsCompare.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.dgvTtsCompare.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvTtsCompare.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTtsCompare.Columns.AddRange(New DataGridViewColumn() {Me.colTtsEngine, Me.colTtsAvgMs, Me.colTtsMinMs, Me.colTtsMaxMs, Me.colTtsP95Ms, Me.colTtsSpeedup, Me.colTtsAudioSize, Me.colTtsCodec})
        Me.dgvTtsCompare.Location = New Point(12, 168)
        Me.dgvTtsCompare.Name = "dgvTtsCompare"
        Me.dgvTtsCompare.ReadOnly = True
        Me.dgvTtsCompare.RowHeadersVisible = False
        Me.dgvTtsCompare.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvTtsCompare.Size = New Size(534, 470)

        ' colTtsEngine
        Me.colTtsEngine.HeaderText = "Engine"
        Me.colTtsEngine.Name = "colTtsEngine"
        Me.colTtsEngine.FillWeight = 18

        ' colTtsAvgMs
        Me.colTtsAvgMs.HeaderText = "Avg (ms)"
        Me.colTtsAvgMs.Name = "colTtsAvgMs"
        Me.colTtsAvgMs.FillWeight = 11

        ' colTtsMinMs
        Me.colTtsMinMs.HeaderText = "Min (ms)"
        Me.colTtsMinMs.Name = "colTtsMinMs"
        Me.colTtsMinMs.FillWeight = 11

        ' colTtsMaxMs
        Me.colTtsMaxMs.HeaderText = "Max (ms)"
        Me.colTtsMaxMs.Name = "colTtsMaxMs"
        Me.colTtsMaxMs.FillWeight = 11

        ' colTtsP95Ms
        Me.colTtsP95Ms.HeaderText = "P95 (ms)"
        Me.colTtsP95Ms.Name = "colTtsP95Ms"
        Me.colTtsP95Ms.FillWeight = 11

        ' colTtsSpeedup
        Me.colTtsSpeedup.HeaderText = "Speedup"
        Me.colTtsSpeedup.Name = "colTtsSpeedup"
        Me.colTtsSpeedup.FillWeight = 10

        ' colTtsAudioSize
        Me.colTtsAudioSize.HeaderText = "Audio Size"
        Me.colTtsAudioSize.Name = "colTtsAudioSize"
        Me.colTtsAudioSize.FillWeight = 12

        ' colTtsCodec
        Me.colTtsCodec.HeaderText = "Format"
        Me.colTtsCodec.Name = "colTtsCodec"
        Me.colTtsCodec.FillWeight = 16

        ' ══════════════════════════════════════════════════
        '  TTS Concurrency sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblTtsConcText
        Me.lblTtsConcText.AutoSize = True
        Me.lblTtsConcText.Location = New Point(12, 12)
        Me.lblTtsConcText.Name = "lblTtsConcText"
        Me.lblTtsConcText.Text = "Test text:"

        ' txtTtsConcText
        Me.txtTtsConcText.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtTtsConcText.Location = New Point(80, 9)
        Me.txtTtsConcText.Name = "txtTtsConcText"
        Me.txtTtsConcText.Size = New Size(470, 23)
        Me.txtTtsConcText.Text = "The quick brown fox jumps over the lazy dog."

        ' lblTtsConcLanguage
        Me.lblTtsConcLanguage.AutoSize = True
        Me.lblTtsConcLanguage.Location = New Point(12, 44)
        Me.lblTtsConcLanguage.Name = "lblTtsConcLanguage"
        Me.lblTtsConcLanguage.Text = "Language:"

        ' cboTtsConcLanguage
        Me.cboTtsConcLanguage.DropDownStyle = ComboBoxStyle.DropDownList
        Me.cboTtsConcLanguage.Items.AddRange(New Object() {"eng", "spa", "fra", "deu", "por", "ita", "zho", "rus", "hin", "arb", "jpn", "kor"})
        Me.cboTtsConcLanguage.Location = New Point(80, 41)
        Me.cboTtsConcLanguage.Name = "cboTtsConcLanguage"
        Me.cboTtsConcLanguage.Size = New Size(100, 23)
        Me.cboTtsConcLanguage.SelectedIndex = 0

        ' lblTtsConcBackend
        Me.lblTtsConcBackend.AutoSize = True
        Me.lblTtsConcBackend.Location = New Point(200, 44)
        Me.lblTtsConcBackend.Name = "lblTtsConcBackend"
        Me.lblTtsConcBackend.Text = "Backend:"

        ' cboTtsConcBackend
        Me.cboTtsConcBackend.DropDownStyle = ComboBoxStyle.DropDownList
        Me.cboTtsConcBackend.Location = New Point(270, 41)
        Me.cboTtsConcBackend.Name = "cboTtsConcBackend"
        Me.cboTtsConcBackend.Size = New Size(130, 23)

        ' lblTtsConcIterations
        Me.lblTtsConcIterations.AutoSize = True
        Me.lblTtsConcIterations.Location = New Point(12, 76)
        Me.lblTtsConcIterations.Name = "lblTtsConcIterations"
        Me.lblTtsConcIterations.Text = "Rounds per level:"

        ' nudTtsConcIterations
        Me.nudTtsConcIterations.Location = New Point(130, 74)
        Me.nudTtsConcIterations.Minimum = 1
        Me.nudTtsConcIterations.Maximum = 20
        Me.nudTtsConcIterations.Value = 3
        Me.nudTtsConcIterations.Name = "nudTtsConcIterations"
        Me.nudTtsConcIterations.Size = New Size(60, 23)

        ' lblTtsConcLevels
        Me.lblTtsConcLevels.AutoSize = True
        Me.lblTtsConcLevels.Location = New Point(220, 76)
        Me.lblTtsConcLevels.Name = "lblTtsConcLevels"
        Me.lblTtsConcLevels.Text = "Concurrency levels:"

        ' txtTtsConcLevels
        Me.txtTtsConcLevels.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Me.txtTtsConcLevels.Location = New Point(350, 73)
        Me.txtTtsConcLevels.Name = "txtTtsConcLevels"
        Me.txtTtsConcLevels.Size = New Size(200, 23)
        Me.txtTtsConcLevels.Text = "1, 2, 5, 10"

        ' btnTtsConcRun
        Me.btnTtsConcRun.Location = New Point(12, 106)
        Me.btnTtsConcRun.Name = "btnTtsConcRun"
        Me.btnTtsConcRun.Size = New Size(130, 28)
        Me.btnTtsConcRun.Text = "Run Throughput"
        Me.btnTtsConcRun.UseVisualStyleBackColor = True

        ' btnTtsConcCancel
        Me.btnTtsConcCancel.Enabled = False
        Me.btnTtsConcCancel.Location = New Point(148, 106)
        Me.btnTtsConcCancel.Name = "btnTtsConcCancel"
        Me.btnTtsConcCancel.Size = New Size(80, 28)
        Me.btnTtsConcCancel.Text = "Cancel"
        Me.btnTtsConcCancel.UseVisualStyleBackColor = True

        ' btnTtsConcExport
        Me.btnTtsConcExport.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnTtsConcExport.Enabled = False
        Me.btnTtsConcExport.Location = New Point(450, 106)
        Me.btnTtsConcExport.Name = "btnTtsConcExport"
        Me.btnTtsConcExport.Size = New Size(100, 28)
        Me.btnTtsConcExport.Text = "Export CSV"
        Me.btnTtsConcExport.UseVisualStyleBackColor = True

        ' lblTtsConcProgress
        Me.lblTtsConcProgress.AutoSize = True
        Me.lblTtsConcProgress.ForeColor = Color.Gray
        Me.lblTtsConcProgress.Location = New Point(12, 142)
        Me.lblTtsConcProgress.MaximumSize = New Size(530, 40)
        Me.lblTtsConcProgress.Name = "lblTtsConcProgress"
        Me.lblTtsConcProgress.Text = "Tests N concurrent synthesis requests to measure real-world throughput."

        ' dgvTtsConcurrent
        Me.dgvTtsConcurrent.AllowUserToAddRows = False
        Me.dgvTtsConcurrent.AllowUserToDeleteRows = False
        Me.dgvTtsConcurrent.AllowUserToResizeRows = False
        Me.dgvTtsConcurrent.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.dgvTtsConcurrent.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Me.dgvTtsConcurrent.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.dgvTtsConcurrent.Location = New Point(12, 168)
        Me.dgvTtsConcurrent.Name = "dgvTtsConcurrent"
        Me.dgvTtsConcurrent.ReadOnly = True
        Me.dgvTtsConcurrent.RowHeadersVisible = False
        Me.dgvTtsConcurrent.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Me.dgvTtsConcurrent.Size = New Size(534, 408)

        ' lblTtsConcSummary
        Me.lblTtsConcSummary.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Me.lblTtsConcSummary.AutoSize = True
        Me.lblTtsConcSummary.Location = New Point(12, 582)
        Me.lblTtsConcSummary.MaximumSize = New Size(530, 60)
        Me.lblTtsConcSummary.Name = "lblTtsConcSummary"
        Me.lblTtsConcSummary.Text = ""

        ' ══════════════════════════════════════════════════
        '  TTS Resources sub-tab content
        ' ══════════════════════════════════════════════════

        ' lblTtsResources
        Me.lblTtsResources.AutoSize = True
        Me.lblTtsResources.ForeColor = Color.Gray
        Me.lblTtsResources.Location = New Point(12, 12)
        Me.lblTtsResources.MaximumSize = New Size(530, 0)
        Me.lblTtsResources.Name = "lblTtsResources"
        Me.lblTtsResources.Text = "Run a Comparison or Concurrency test to see resource utilisation (CPU, RAM, GPU, VRAM, temperature)."

        ' ══════════════════════════════════════════════════
        '  Bottom panel (Export All + auto-save status)
        ' ══════════════════════════════════════════════════

        ' pnlBottom
        Me.pnlBottom.Controls.Add(Me.btnExportAll)
        Me.pnlBottom.Controls.Add(Me.lblAutoSaveStatus)
        Me.pnlBottom.Dock = DockStyle.Bottom
        Me.pnlBottom.Size = New Size(574, 36)
        Me.pnlBottom.Name = "pnlBottom"

        ' btnExportAll
        Me.btnExportAll.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Me.btnExportAll.Location = New Point(448, 5)
        Me.btnExportAll.Name = "btnExportAll"
        Me.btnExportAll.Size = New Size(120, 26)
        Me.btnExportAll.Text = "Export All..."
        Me.btnExportAll.UseVisualStyleBackColor = True

        ' lblAutoSaveStatus
        Me.lblAutoSaveStatus.AutoSize = True
        Me.lblAutoSaveStatus.ForeColor = Color.Gray
        Me.lblAutoSaveStatus.Location = New Point(8, 10)
        Me.lblAutoSaveStatus.Name = "lblAutoSaveStatus"
        Me.lblAutoSaveStatus.Text = ""

        ' ══════════════════════════════════════════════════
        '  FormTranslationBenchmark
        ' ══════════════════════════════════════════════════
        Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(574, 746)
        Me.Controls.Add(Me.tabMain)
        Me.Controls.Add(Me.pnlBottom)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimizeBox = False
        Me.MaximizeBox = True
        Me.MinimumSize = New Size(590, 600)
        Me.Name = "FormTranslationBenchmark"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Text = "Engine Benchmark"

        Me.pnlBottom.ResumeLayout(False)
        Me.pnlBottom.PerformLayout()
        Me.tabMain.ResumeLayout(False)
        Me.tabTranslation.ResumeLayout(False)
        Me.tabTransInner.ResumeLayout(False)
        Me.tabTransPipeline.ResumeLayout(False)
        Me.tabTransPipeline.PerformLayout()
        Me.tabTransConcurrency.ResumeLayout(False)
        Me.tabTransConcurrency.PerformLayout()
        Me.tabTransResources.ResumeLayout(False)
        Me.tabTransResources.PerformLayout()
        Me.tabStt.ResumeLayout(False)
        Me.tabSttInner.ResumeLayout(False)
        Me.tabSttComparison.ResumeLayout(False)
        Me.tabSttComparison.PerformLayout()
        Me.tabSttConcurrency.ResumeLayout(False)
        Me.tabSttConcurrency.PerformLayout()
        Me.tabSttResources.ResumeLayout(False)
        Me.tabSttResources.PerformLayout()
        Me.tabTts.ResumeLayout(False)
        Me.tabTtsInner.ResumeLayout(False)
        Me.tabTtsComparison.ResumeLayout(False)
        Me.tabTtsComparison.PerformLayout()
        Me.tabTtsConcurrency.ResumeLayout(False)
        Me.tabTtsConcurrency.PerformLayout()
        Me.tabTtsResources.ResumeLayout(False)
        Me.tabTtsResources.PerformLayout()
        CType(Me.nudTtsIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTtsConcIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvTtsCompare, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvTtsConcurrent, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpConfig.ResumeLayout(False)
        Me.grpConfig.PerformLayout()
        CType(Me.nudConcurrency, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudIterations, System.ComponentModel.ISupportInitialize).EndInit()
        Me.grpResults.ResumeLayout(False)
        Me.grpResults.PerformLayout()
        CType(Me.dgvResults, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudSttIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudConcIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvSttCompare, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvConcurrent, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTransConcIterations, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.dgvTransConcurrent, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabTranslation As TabPage
    Friend WithEvents tabStt As TabPage
    Friend WithEvents tabTts As TabPage

    Friend WithEvents tabTransInner As TabControl
    Friend WithEvents tabTransPipeline As TabPage
    Friend WithEvents tabTransConcurrency As TabPage
    Friend WithEvents tabTransResources As TabPage
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

    Friend WithEvents lblTransConcTargets As Label
    Friend WithEvents clbTransConcTargets As CheckedListBox
    Friend WithEvents lblTransConcIterations As Label
    Friend WithEvents nudTransConcIterations As NumericUpDown
    Friend WithEvents lblTransConcLevels As Label
    Friend WithEvents txtTransConcLevels As TextBox
    Friend WithEvents btnTransConcRun As Button
    Friend WithEvents btnTransConcCancel As Button
    Friend WithEvents btnTransConcExport As Button
    Friend WithEvents lblTransConcProgress As Label
    Friend WithEvents dgvTransConcurrent As DataGridView
    Friend WithEvents colTransConcLevel As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcRequests As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcWall As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcAvg As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcP50 As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcP95 As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcMax As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcThroughput As DataGridViewTextBoxColumn
    Friend WithEvents colTransConcErrors As DataGridViewTextBoxColumn
    Friend WithEvents lblTransConcSummary As Label

    Friend WithEvents tabSttInner As TabControl
    Friend WithEvents tabSttComparison As TabPage
    Friend WithEvents tabSttConcurrency As TabPage
    Friend WithEvents tabSttResources As TabPage
    Friend WithEvents lblSttAudioFile As Label
    Friend WithEvents txtSttAudioFile As TextBox
    Friend WithEvents btnSttBrowse As Button
    Friend WithEvents lblSttIterations As Label
    Friend WithEvents nudSttIterations As NumericUpDown
    Friend WithEvents chkSttCuda As CheckBox
    Friend WithEvents chkSttVulkan As CheckBox
    Friend WithEvents chkSttCpu As CheckBox
    Friend WithEvents chkSttFasterWhisper As CheckBox
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

    Friend WithEvents lblSttResources As Label
    Friend WithEvents lblConcAudioFile As Label
    Friend WithEvents txtConcAudioFile As TextBox
    Friend WithEvents btnConcBrowse As Button
    Friend WithEvents lblConcIterations As Label
    Friend WithEvents nudConcIterations As NumericUpDown
    Friend WithEvents lblConcLevels As Label
    Friend WithEvents txtConcLevels As TextBox
    Friend WithEvents btnConcRun As Button
    Friend WithEvents btnConcCancel As Button
    Friend WithEvents btnConcExport As Button
    Friend WithEvents lblConcProgress As Label
    Friend WithEvents dgvConcurrent As DataGridView
    Friend WithEvents colConcLevel As DataGridViewTextBoxColumn
    Friend WithEvents colConcRequests As DataGridViewTextBoxColumn
    Friend WithEvents colConcWall As DataGridViewTextBoxColumn
    Friend WithEvents colConcAvg As DataGridViewTextBoxColumn
    Friend WithEvents colConcP50 As DataGridViewTextBoxColumn
    Friend WithEvents colConcP95 As DataGridViewTextBoxColumn
    Friend WithEvents colConcMax As DataGridViewTextBoxColumn
    Friend WithEvents colConcThroughput As DataGridViewTextBoxColumn
    Friend WithEvents colConcErrors As DataGridViewTextBoxColumn
    Friend WithEvents lblConcSummary As Label

    Friend WithEvents tabTtsInner As TabControl
    Friend WithEvents tabTtsComparison As TabPage
    Friend WithEvents tabTtsConcurrency As TabPage
    Friend WithEvents tabTtsResources As TabPage

    Friend WithEvents lblTtsText As Label
    Friend WithEvents txtTtsText As TextBox
    Friend WithEvents lblTtsLanguage As Label
    Friend WithEvents cboTtsLanguage As ComboBox
    Friend WithEvents lblTtsBackends As Label
    Friend WithEvents clbTtsBackends As CheckedListBox
    Friend WithEvents lblTtsIterations As Label
    Friend WithEvents nudTtsIterations As NumericUpDown
    Friend WithEvents btnTtsCompare As Button
    Friend WithEvents btnTtsCancel As Button
    Friend WithEvents btnTtsExport As Button
    Friend WithEvents lblTtsProgress As Label
    Friend WithEvents dgvTtsCompare As DataGridView
    Friend WithEvents colTtsEngine As DataGridViewTextBoxColumn
    Friend WithEvents colTtsAvgMs As DataGridViewTextBoxColumn
    Friend WithEvents colTtsMinMs As DataGridViewTextBoxColumn
    Friend WithEvents colTtsMaxMs As DataGridViewTextBoxColumn
    Friend WithEvents colTtsP95Ms As DataGridViewTextBoxColumn
    Friend WithEvents colTtsSpeedup As DataGridViewTextBoxColumn
    Friend WithEvents colTtsAudioSize As DataGridViewTextBoxColumn
    Friend WithEvents colTtsCodec As DataGridViewTextBoxColumn

    Friend WithEvents lblTtsConcText As Label
    Friend WithEvents txtTtsConcText As TextBox
    Friend WithEvents lblTtsConcLanguage As Label
    Friend WithEvents cboTtsConcLanguage As ComboBox
    Friend WithEvents lblTtsConcBackend As Label
    Friend WithEvents cboTtsConcBackend As ComboBox
    Friend WithEvents lblTtsConcIterations As Label
    Friend WithEvents nudTtsConcIterations As NumericUpDown
    Friend WithEvents lblTtsConcLevels As Label
    Friend WithEvents txtTtsConcLevels As TextBox
    Friend WithEvents btnTtsConcRun As Button
    Friend WithEvents btnTtsConcCancel As Button
    Friend WithEvents btnTtsConcExport As Button
    Friend WithEvents lblTtsConcProgress As Label
    Friend WithEvents dgvTtsConcurrent As DataGridView
    Friend WithEvents lblTtsConcSummary As Label

    Friend WithEvents lblTtsResources As Label

    Friend WithEvents pnlBottom As Panel
    Friend WithEvents btnExportAll As Button
    Friend WithEvents lblAutoSaveStatus As Label

End Class
