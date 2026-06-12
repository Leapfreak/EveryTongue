<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormOptions
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.btnOk = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.splitter = New System.Windows.Forms.SplitContainer()
        Me.treeNav = New System.Windows.Forms.TreeView()
        Me.pnlPages = New System.Windows.Forms.Panel()
        Me.pnlGeneral = New System.Windows.Forms.Panel()
        Me.chkStartWindows = New System.Windows.Forms.CheckBox()
        Me.chkStartMinimized = New System.Windows.Forms.CheckBox()
        Me.chkMinimizeToTray = New System.Windows.Forms.CheckBox()
        Me.chkResetFirstRun = New System.Windows.Forms.CheckBox()
        Me.lblStartupSep = New System.Windows.Forms.Label()
        Me.lblStartupHeader = New System.Windows.Forms.Label()
        Me.cboTheme = New System.Windows.Forms.ComboBox()
        Me.lblTheme = New System.Windows.Forms.Label()
        Me.cboLogLevel = New System.Windows.Forms.ComboBox()
        Me.lblLogLevel = New System.Windows.Forms.Label()
        Me.cboUiLang = New System.Windows.Forms.ComboBox()
        Me.lblUiLang = New System.Windows.Forms.Label()
        Me.lblAppearanceSep = New System.Windows.Forms.Label()
        Me.lblAppearanceHeader = New System.Windows.Forms.Label()
        Me.pnlPaths = New System.Windows.Forms.Panel()
        Me.txtYtdlpFormat = New System.Windows.Forms.TextBox()
        Me.lblYtdlpFormat = New System.Windows.Forms.Label()
        Me.lblAdvancedSep = New System.Windows.Forms.Label()
        Me.lblAdvancedHeader = New System.Windows.Forms.Label()
        Me.btnBrowseBibles = New System.Windows.Forms.Button()
        Me.txtBibles = New System.Windows.Forms.TextBox()
        Me.lblBiblesPath = New System.Windows.Forms.Label()
        Me.btnBrowseLogs = New System.Windows.Forms.Button()
        Me.txtLogs = New System.Windows.Forms.TextBox()
        Me.lblLogsPath = New System.Windows.Forms.Label()
        Me.btnBrowseGlossary = New System.Windows.Forms.Button()
        Me.txtGlossary = New System.Windows.Forms.TextBox()
        Me.lblGlossaryPath = New System.Windows.Forms.Label()
        Me.btnBrowseOutputRoot = New System.Windows.Forms.Button()
        Me.txtOutputRoot = New System.Windows.Forms.TextBox()
        Me.lblOutputRootPath = New System.Windows.Forms.Label()
        Me.lblDirectoriesSep = New System.Windows.Forms.Label()
        Me.lblDirectoriesHeader = New System.Windows.Forms.Label()
        Me.btnBrowseModelAudio = New System.Windows.Forms.Button()
        Me.txtModelAudio = New System.Windows.Forms.TextBox()
        Me.lblModelAudioPath = New System.Windows.Forms.Label()
        Me.btnBrowseModel = New System.Windows.Forms.Button()
        Me.txtModel = New System.Windows.Forms.TextBox()
        Me.lblModelPath = New System.Windows.Forms.Label()
        Me.btnBrowseTransModel = New System.Windows.Forms.Button()
        Me.txtTransModel = New System.Windows.Forms.TextBox()
        Me.lblTransModelPath = New System.Windows.Forms.Label()
        Me.btnBrowseWhisperServer = New System.Windows.Forms.Button()
        Me.txtWhisperServer = New System.Windows.Forms.TextBox()
        Me.lblWhisperServerPath = New System.Windows.Forms.Label()
        Me.btnBrowseGgmlModel = New System.Windows.Forms.Button()
        Me.txtGgmlModel = New System.Windows.Forms.TextBox()
        Me.lblGgmlModelPath = New System.Windows.Forms.Label()
        Me.btnBrowseFwModel = New System.Windows.Forms.Button()
        Me.txtFwModel = New System.Windows.Forms.TextBox()
        Me.lblFwModelPath = New System.Windows.Forms.Label()
        Me.lblModelPathsSep = New System.Windows.Forms.Label()
        Me.lblModelPathsHeader = New System.Windows.Forms.Label()
        Me.btnBrowseSubtitleEdit = New System.Windows.Forms.Button()
        Me.txtSubtitleEdit = New System.Windows.Forms.TextBox()
        Me.lblSubtitleEditPath = New System.Windows.Forms.Label()
        Me.btnBrowseFfprobe = New System.Windows.Forms.Button()
        Me.txtFfprobe = New System.Windows.Forms.TextBox()
        Me.lblFfprobePath = New System.Windows.Forms.Label()
        Me.btnBrowseFfmpeg = New System.Windows.Forms.Button()
        Me.txtFfmpeg = New System.Windows.Forms.TextBox()
        Me.lblFfmpegPath = New System.Windows.Forms.Label()
        Me.btnBrowseYtdlp = New System.Windows.Forms.Button()
        Me.txtYtdlp = New System.Windows.Forms.TextBox()
        Me.lblYtdlpPath = New System.Windows.Forms.Label()
        Me.btnBrowseWhisper = New System.Windows.Forms.Button()
        Me.txtWhisper = New System.Windows.Forms.TextBox()
        Me.lblWhisperPath = New System.Windows.Forms.Label()
        Me.lblToolPathsSep = New System.Windows.Forms.Label()
        Me.lblToolPathsHeader = New System.Windows.Forms.Label()
        Me.pnlHardware = New System.Windows.Forms.Panel()
        Me.cboSttBackend = New System.Windows.Forms.ComboBox()
        Me.txtSttApiKey = New System.Windows.Forms.TextBox()
        Me.lblSttApiKey = New System.Windows.Forms.Label()
        Me.lblSttBackend = New System.Windows.Forms.Label()
        Me.lblSttOperatingPoint = New System.Windows.Forms.Label()
        Me.cboSttOperatingPoint = New System.Windows.Forms.ComboBox()
        Me.lblSttRegion = New System.Windows.Forms.Label()
        Me.cboSttRegion = New System.Windows.Forms.ComboBox()
        Me.lblSttEouSilence = New System.Windows.Forms.Label()
        Me.nudSttEouSilence = New System.Windows.Forms.NumericUpDown()
        Me.lblSttEngineSep = New System.Windows.Forms.Label()
        Me.lblSttEngineHeader = New System.Windows.Forms.Label()
        Me.lblHwHeader = New System.Windows.Forms.Label()
        Me.lblHwSep = New System.Windows.Forms.Label()
        Me.lblHwOverallCaption = New System.Windows.Forms.Label()
        Me.lblHwOverallScore = New System.Windows.Forms.Label()
        Me.pnlHwIndicator = New System.Windows.Forms.Panel()
        Me.lblHwVerdict = New System.Windows.Forms.Label()
        Me.lblHwBreakdownHeader = New System.Windows.Forms.Label()
        Me.lblHwBreakdownSep = New System.Windows.Forms.Label()
        Me.lblHwGpu = New System.Windows.Forms.Label()
        Me.lblHwCpu = New System.Windows.Forms.Label()
        Me.lblHwRam = New System.Windows.Forms.Label()
        Me.lblHwDisk = New System.Windows.Forms.Label()
        Me.lblHwOs = New System.Windows.Forms.Label()
        Me.lblHwRecsHeader = New System.Windows.Forms.Label()
        Me.lblHwRecsSep = New System.Windows.Forms.Label()
        Me.txtHwRecs = New System.Windows.Forms.TextBox()
        Me.btnHwRescan = New System.Windows.Forms.Button()
        Me.pnlServer = New System.Windows.Forms.Panel()
        Me.pnlTranslation = New System.Windows.Forms.Panel()
        Me.pnlTts = New System.Windows.Forms.Panel()
        Me.lblTtsSep = New System.Windows.Forms.Label()
        Me.lblTtsHeader = New System.Windows.Forms.Label()
        Me.lblTtsPref1 = New System.Windows.Forms.Label()
        Me.cboTtsPref1 = New System.Windows.Forms.ComboBox()
        Me.lblTtsPref2 = New System.Windows.Forms.Label()
        Me.cboTtsPref2 = New System.Windows.Forms.ComboBox()
        Me.lblTtsPref3 = New System.Windows.Forms.Label()
        Me.cboTtsPref3 = New System.Windows.Forms.ComboBox()
        Me.lblTtsNote = New System.Windows.Forms.Label()
        Me.cboDevice = New System.Windows.Forms.ComboBox()
        Me.lblDevice = New System.Windows.Forms.Label()
        Me.cboTransBackend = New System.Windows.Forms.ComboBox()
        Me.lblTransBackend = New System.Windows.Forms.Label()
        Me.chkTransEnabled = New System.Windows.Forms.CheckBox()
        Me.chkUseSpeechmaticsTranslation = New System.Windows.Forms.CheckBox()
        Me.lblClauseHeader = New System.Windows.Forms.Label()
        Me.chkSpeechmaticsHoldClauses = New System.Windows.Forms.CheckBox()
        Me.chkClauseLockOnPunctuation = New System.Windows.Forms.CheckBox()
        Me.lblClauseGraceMs = New System.Windows.Forms.Label()
        Me.nudClauseGraceMs = New System.Windows.Forms.NumericUpDown()
        Me.lblClauseMaxMs = New System.Windows.Forms.Label()
        Me.nudClauseMaxMs = New System.Windows.Forms.NumericUpDown()
        Me.lblClauseMaxChars = New System.Windows.Forms.Label()
        Me.nudClauseMaxChars = New System.Windows.Forms.NumericUpDown()
        Me.lblClauseMinLockChars = New System.Windows.Forms.Label()
        Me.nudClauseMinLockChars = New System.Windows.Forms.NumericUpDown()
        Me.lblClauseTimerMs = New System.Windows.Forms.Label()
        Me.nudClauseTimerMs = New System.Windows.Forms.NumericUpDown()
        Me.lblClauseSentenceEnders = New System.Windows.Forms.Label()
        Me.txtClauseSentenceEnders = New System.Windows.Forms.TextBox()
        Me.lblTranslationSep = New System.Windows.Forms.Label()
        Me.lblTranslationHeader = New System.Windows.Forms.Label()
        Me.chkBold = New System.Windows.Forms.CheckBox()
        Me.nudFontSize = New System.Windows.Forms.NumericUpDown()
        Me.lblFontSize = New System.Windows.Forms.Label()
        Me.cboFont = New System.Windows.Forms.ComboBox()
        Me.lblFont = New System.Windows.Forms.Label()
        Me.btnFgColor = New System.Windows.Forms.Button()
        Me.lblFgColor = New System.Windows.Forms.Label()
        Me.btnBgColor = New System.Windows.Forms.Button()
        Me.lblBgColor = New System.Windows.Forms.Label()
        Me.lblSubtitleAppearanceSep = New System.Windows.Forms.Label()
        Me.lblSubtitleAppearanceHeader = New System.Windows.Forms.Label()
        Me.txtPin = New System.Windows.Forms.TextBox()
        Me.lblPin = New System.Windows.Forms.Label()
        Me.chkFirewall = New System.Windows.Forms.CheckBox()
        Me.nudTransPort = New System.Windows.Forms.NumericUpDown()
        Me.lblTransPort = New System.Windows.Forms.Label()
        Me.nudLivePort = New System.Windows.Forms.NumericUpDown()
        Me.lblLivePort = New System.Windows.Forms.Label()
        Me.nudPort = New System.Windows.Forms.NumericUpDown()
        Me.lblPort = New System.Windows.Forms.Label()
        Me.lblNetworkSep = New System.Windows.Forms.Label()
        Me.lblNetworkHeader = New System.Windows.Forms.Label()
        Me.lblTemplatesHeader = New System.Windows.Forms.Label()
        Me.lblTemplatesSep = New System.Windows.Forms.Label()
        Me.btnManageTemplates = New System.Windows.Forms.Button()
        ' ── Speech-to-Text / Display pages ──
        Me.pnlStt = New System.Windows.Forms.Panel()
        Me.pnlDisplay = New System.Windows.Forms.Panel()
        Me.btnManageSttTemplatesOpt = New System.Windows.Forms.Button()
        ' ── Advanced panel controls ──
        Me.pnlAdvanced = New System.Windows.Forms.Panel()
        Me.lblAdvPipelineHeader = New System.Windows.Forms.Label()
        Me.lblAdvPipelineSep = New System.Windows.Forms.Label()
        Me.nudParallelJobs = New System.Windows.Forms.NumericUpDown()
        Me.lblParallelJobs = New System.Windows.Forms.Label()
        Me.nudChunkSize = New System.Windows.Forms.NumericUpDown()
        Me.lblChunkSize = New System.Windows.Forms.Label()
        Me.nudPollInterval = New System.Windows.Forms.NumericUpDown()
        Me.lblPollInterval = New System.Windows.Forms.Label()
        Me.nudChunkTimeout = New System.Windows.Forms.NumericUpDown()
        Me.lblChunkTimeout = New System.Windows.Forms.Label()
        Me.chkKeepChunks = New System.Windows.Forms.CheckBox()
        Me.chkKeepPreview = New System.Windows.Forms.CheckBox()
        Me.lblAdvLivePipelineHeader = New System.Windows.Forms.Label()
        Me.lblAdvLivePipelineSep = New System.Windows.Forms.Label()
        Me.lblTranslationConcurrency = New System.Windows.Forms.Label()
        Me.nudTranslationConcurrency = New System.Windows.Forms.NumericUpDown()
        Me.lblTtsConcurrency = New System.Windows.Forms.Label()
        Me.nudTtsConcurrency = New System.Windows.Forms.NumericUpDown()
        Me.lblAdvOutputHeader = New System.Windows.Forms.Label()
        Me.lblAdvOutputSep = New System.Windows.Forms.Label()
        Me.chkOutSrt = New System.Windows.Forms.CheckBox()
        Me.chkOutVtt = New System.Windows.Forms.CheckBox()
        Me.chkOutTxt = New System.Windows.Forms.CheckBox()
        Me.chkOutJson = New System.Windows.Forms.CheckBox()
        Me.chkOutCsv = New System.Windows.Forms.CheckBox()
        Me.chkOutLrc = New System.Windows.Forms.CheckBox()
        Me.lblAdvWhisperHeader = New System.Windows.Forms.Label()
        Me.lblAdvWhisperSep = New System.Windows.Forms.Label()
        Me.nudThreads = New System.Windows.Forms.NumericUpDown()
        Me.lblThreads = New System.Windows.Forms.Label()
        Me.nudProcessors = New System.Windows.Forms.NumericUpDown()
        Me.lblProcessors = New System.Windows.Forms.Label()
        Me.nudBeamSize = New System.Windows.Forms.NumericUpDown()
        Me.lblBeamSize = New System.Windows.Forms.Label()
        Me.nudBestOf = New System.Windows.Forms.NumericUpDown()
        Me.lblBestOf = New System.Windows.Forms.Label()
        Me.nudTemperature = New System.Windows.Forms.NumericUpDown()
        Me.lblTemperature = New System.Windows.Forms.Label()
        Me.nudTempInc = New System.Windows.Forms.NumericUpDown()
        Me.lblTempInc = New System.Windows.Forms.Label()
        Me.nudMaxContext = New System.Windows.Forms.NumericUpDown()
        Me.lblMaxContext = New System.Windows.Forms.Label()
        Me.nudMaxSegLen = New System.Windows.Forms.NumericUpDown()
        Me.lblMaxSegLen = New System.Windows.Forms.Label()
        Me.nudMaxTokens = New System.Windows.Forms.NumericUpDown()
        Me.lblMaxTokens = New System.Windows.Forms.Label()
        Me.nudAudioContext = New System.Windows.Forms.NumericUpDown()
        Me.lblAudioContext = New System.Windows.Forms.Label()
        Me.nudWordThresh = New System.Windows.Forms.NumericUpDown()
        Me.lblWordThresh = New System.Windows.Forms.Label()
        Me.nudEntropyThresh = New System.Windows.Forms.NumericUpDown()
        Me.lblEntropyThresh = New System.Windows.Forms.Label()
        Me.nudLogProbThresh = New System.Windows.Forms.NumericUpDown()
        Me.lblLogProbThresh = New System.Windows.Forms.Label()
        Me.nudNoSpeechThresh = New System.Windows.Forms.NumericUpDown()
        Me.lblNoSpeechThresh = New System.Windows.Forms.Label()
        Me.nudVadThresh = New System.Windows.Forms.NumericUpDown()
        Me.lblVadThresh = New System.Windows.Forms.Label()
        Me.txtInitialPrompt = New System.Windows.Forms.TextBox()
        Me.lblInitialPrompt = New System.Windows.Forms.Label()
        Me.lblAdvFlagsHeader = New System.Windows.Forms.Label()
        Me.lblAdvFlagsSep = New System.Windows.Forms.Label()
        Me.chkSplitOnWord = New System.Windows.Forms.CheckBox()
        Me.chkNoGpu = New System.Windows.Forms.CheckBox()
        Me.chkFlashAttn = New System.Windows.Forms.CheckBox()
        Me.chkDiarize = New System.Windows.Forms.CheckBox()
        Me.chkTinyDiarize = New System.Windows.Forms.CheckBox()
        Me.chkTranslateEn = New System.Windows.Forms.CheckBox()
        Me.chkNoTimestamps = New System.Windows.Forms.CheckBox()
        Me.chkPrintProgress = New System.Windows.Forms.CheckBox()
        Me.chkPrintColours = New System.Windows.Forms.CheckBox()
        Me.lblAdvLiveHeader = New System.Windows.Forms.Label()
        Me.lblAdvLiveSep = New System.Windows.Forms.Label()
        Me.cboComputeType = New System.Windows.Forms.ComboBox()
        Me.lblComputeType = New System.Windows.Forms.Label()
        Me.nudLiveVadSilence = New System.Windows.Forms.NumericUpDown()
        Me.lblLiveVadSilence = New System.Windows.Forms.Label()
        Me.nudLiveMaxSeg = New System.Windows.Forms.NumericUpDown()
        Me.lblLiveMaxSeg = New System.Windows.Forms.Label()
        Me.nudLiveInterim = New System.Windows.Forms.NumericUpDown()
        Me.lblLiveInterim = New System.Windows.Forms.Label()
        Me.lblAdvBibleHeader = New System.Windows.Forms.Label()
        Me.lblAdvBibleSep = New System.Windows.Forms.Label()
        Me.chkShowBibleCopyright = New System.Windows.Forms.CheckBox()
        CType(Me.splitter, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.splitter.Panel1.SuspendLayout()
        Me.splitter.Panel2.SuspendLayout()
        Me.splitter.SuspendLayout()
        Me.pnlGeneral.SuspendLayout()
        Me.pnlPaths.SuspendLayout()
        Me.pnlServer.SuspendLayout()
        Me.pnlTranslation.SuspendLayout()
        Me.pnlTts.SuspendLayout()
        Me.pnlHardware.SuspendLayout()
        Me.pnlAdvanced.SuspendLayout()
        CType(Me.nudParallelJobs, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudChunkSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudPollInterval, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudChunkTimeout, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTranslationConcurrency, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTtsConcurrency, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudThreads, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudProcessors, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudBeamSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudBestOf, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTemperature, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTempInc, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudMaxContext, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudMaxSegLen, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudMaxTokens, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudAudioContext, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudWordThresh, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudEntropyThresh, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLogProbThresh, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudNoSpeechThresh, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudVadThresh, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLiveVadSilence, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLiveMaxSeg, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLiveInterim, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudPort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLivePort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTransPort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudFontSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudClauseGraceMs, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudClauseMaxMs, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudClauseMaxChars, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudClauseMinLockChars, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudClauseTimerMs, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudSttEouSilence, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        ' btnOk
        '
        Me.btnOk.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnOk.Location = New System.Drawing.Point(592, 483)
        Me.btnOk.Name = "btnOk"
        Me.btnOk.Size = New System.Drawing.Size(80, 28)
        Me.btnOk.TabIndex = 1
        Me.btnOk.Text = "OK"
        Me.btnOk.UseVisualStyleBackColor = True
        '
        ' btnCancel
        '
        Me.btnCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(680, 483)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(80, 28)
        Me.btnCancel.TabIndex = 2
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        '
        ' splitter
        '
        Me.splitter.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.splitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1
        Me.splitter.Location = New System.Drawing.Point(8, 8)
        Me.splitter.Name = "splitter"
        '
        ' splitter.Panel1
        '
        Me.splitter.Panel1.Controls.Add(Me.treeNav)
        '
        ' splitter.Panel2
        '
        Me.splitter.Panel2.Controls.Add(Me.pnlPages)
        Me.splitter.Size = New System.Drawing.Size(748, 469)
        Me.splitter.SplitterDistance = 180
        Me.splitter.TabIndex = 0
        '
        ' treeNav
        '
        Me.treeNav.Dock = System.Windows.Forms.DockStyle.Fill
        Me.treeNav.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.treeNav.FullRowSelect = True
        Me.treeNav.HideSelection = False
        Me.treeNav.ItemHeight = 28
        Me.treeNav.Location = New System.Drawing.Point(0, 0)
        Me.treeNav.Name = "treeNav"
        Me.treeNav.Nodes.AddRange(New System.Windows.Forms.TreeNode() {New System.Windows.Forms.TreeNode("General") With {.Name = "general"}, New System.Windows.Forms.TreeNode("Tool Paths") With {.Name = "paths"}, New System.Windows.Forms.TreeNode("Speech-to-Text") With {.Name = "stt"}, New System.Windows.Forms.TreeNode("Translation") With {.Name = "translation"}, New System.Windows.Forms.TreeNode("Text-to-Speech") With {.Name = "tts"}, New System.Windows.Forms.TreeNode("Display") With {.Name = "display"}, New System.Windows.Forms.TreeNode("Server") With {.Name = "server"}, New System.Windows.Forms.TreeNode("Hardware") With {.Name = "hardware"}, New System.Windows.Forms.TreeNode("Advanced") With {.Name = "advanced"}})
        Me.treeNav.ShowLines = False
        Me.treeNav.ShowPlusMinus = False
        Me.treeNav.ShowRootLines = False
        Me.treeNav.Size = New System.Drawing.Size(180, 469)
        Me.treeNav.TabIndex = 0
        '
        ' pnlPages
        '
        Me.pnlPages.Controls.Add(Me.pnlGeneral)
        Me.pnlPages.Controls.Add(Me.pnlPaths)
        Me.pnlPages.Controls.Add(Me.pnlStt)
        Me.pnlPages.Controls.Add(Me.pnlServer)
        Me.pnlPages.Controls.Add(Me.pnlDisplay)
        Me.pnlPages.Controls.Add(Me.pnlTranslation)
        Me.pnlPages.Controls.Add(Me.pnlTts)
        Me.pnlPages.Controls.Add(Me.pnlHardware)
        Me.pnlPages.Controls.Add(Me.pnlAdvanced)
        Me.pnlPages.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlPages.Location = New System.Drawing.Point(0, 0)
        Me.pnlPages.Name = "pnlPages"
        Me.pnlPages.Size = New System.Drawing.Size(564, 469)
        Me.pnlPages.TabIndex = 0
        '
        ' ══════════════════════════════════════════════════════════════
        ' GENERAL PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlGeneral
        '
        Me.pnlGeneral.AutoScroll = True
        Me.pnlGeneral.Controls.Add(Me.chkResetFirstRun)
        Me.pnlGeneral.Controls.Add(Me.chkMinimizeToTray)
        Me.pnlGeneral.Controls.Add(Me.chkStartMinimized)
        Me.pnlGeneral.Controls.Add(Me.chkStartWindows)
        Me.pnlGeneral.Controls.Add(Me.lblStartupSep)
        Me.pnlGeneral.Controls.Add(Me.lblStartupHeader)
        Me.pnlGeneral.Controls.Add(Me.cboLogLevel)
        Me.pnlGeneral.Controls.Add(Me.lblLogLevel)
        Me.pnlGeneral.Controls.Add(Me.cboTheme)
        Me.pnlGeneral.Controls.Add(Me.lblTheme)
        Me.pnlGeneral.Controls.Add(Me.cboUiLang)
        Me.pnlGeneral.Controls.Add(Me.lblUiLang)
        Me.pnlGeneral.Controls.Add(Me.lblAppearanceSep)
        Me.pnlGeneral.Controls.Add(Me.lblAppearanceHeader)
        Me.pnlGeneral.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlGeneral.Location = New System.Drawing.Point(0, 0)
        Me.pnlGeneral.Name = "pnlGeneral"
        Me.pnlGeneral.Size = New System.Drawing.Size(564, 469)
        Me.pnlGeneral.TabIndex = 0
        '
        ' lblAppearanceHeader
        '
        Me.lblAppearanceHeader.AutoSize = True
        Me.lblAppearanceHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAppearanceHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblAppearanceHeader.Name = "lblAppearanceHeader"
        Me.lblAppearanceHeader.Size = New System.Drawing.Size(93, 20)
        Me.lblAppearanceHeader.TabIndex = 0
        Me.lblAppearanceHeader.Text = "Appearance"
        '
        ' lblAppearanceSep
        '
        Me.lblAppearanceSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAppearanceSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAppearanceSep.Location = New System.Drawing.Point(8, 34)
        Me.lblAppearanceSep.Name = "lblAppearanceSep"
        Me.lblAppearanceSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAppearanceSep.TabIndex = 1
        '
        ' lblUiLang
        '
        Me.lblUiLang.AutoSize = True
        Me.lblUiLang.Location = New System.Drawing.Point(12, 42)
        Me.lblUiLang.Name = "lblUiLang"
        Me.lblUiLang.Size = New System.Drawing.Size(80, 15)
        Me.lblUiLang.TabIndex = 2
        Me.lblUiLang.Text = "UI Language:"
        '
        ' cboUiLang
        '
        Me.cboUiLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboUiLang.FormattingEnabled = True
        Me.cboUiLang.Location = New System.Drawing.Point(12, 60)
        Me.cboUiLang.Name = "cboUiLang"
        Me.cboUiLang.Size = New System.Drawing.Size(220, 23)
        Me.cboUiLang.TabIndex = 3
        '
        ' lblTheme
        '
        Me.lblTheme.AutoSize = True
        Me.lblTheme.Location = New System.Drawing.Point(12, 92)
        Me.lblTheme.Name = "lblTheme"
        Me.lblTheme.Size = New System.Drawing.Size(46, 15)
        Me.lblTheme.TabIndex = 4
        Me.lblTheme.Text = "Theme:"
        '
        ' cboTheme
        '
        Me.cboTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTheme.FormattingEnabled = True
        Me.cboTheme.Items.AddRange(New Object() {"System", "Light", "Dark"})
        Me.cboTheme.Location = New System.Drawing.Point(12, 110)
        Me.cboTheme.Name = "cboTheme"
        Me.cboTheme.Size = New System.Drawing.Size(160, 23)
        Me.cboTheme.TabIndex = 5
        '
        ' lblLogLevel
        '
        Me.lblLogLevel.AutoSize = True
        Me.lblLogLevel.Location = New System.Drawing.Point(200, 92)
        Me.lblLogLevel.Name = "lblLogLevel"
        Me.lblLogLevel.Size = New System.Drawing.Size(60, 15)
        Me.lblLogLevel.TabIndex = 60
        Me.lblLogLevel.Text = "Log Level:"
        '
        ' cboLogLevel
        '
        Me.cboLogLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboLogLevel.FormattingEnabled = True
        Me.cboLogLevel.Items.AddRange(New Object() {"Minimal", "Normal", "Verbose"})
        Me.cboLogLevel.Location = New System.Drawing.Point(200, 110)
        Me.cboLogLevel.Name = "cboLogLevel"
        Me.cboLogLevel.Size = New System.Drawing.Size(160, 23)
        Me.cboLogLevel.TabIndex = 61
        '
        ' lblStartupHeader
        '
        Me.lblStartupHeader.AutoSize = True
        Me.lblStartupHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblStartupHeader.Location = New System.Drawing.Point(8, 150)
        Me.lblStartupHeader.Name = "lblStartupHeader"
        Me.lblStartupHeader.Size = New System.Drawing.Size(62, 20)
        Me.lblStartupHeader.TabIndex = 6
        Me.lblStartupHeader.Text = "Startup"
        '
        ' lblStartupSep
        '
        Me.lblStartupSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblStartupSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblStartupSep.Location = New System.Drawing.Point(8, 172)
        Me.lblStartupSep.Name = "lblStartupSep"
        Me.lblStartupSep.Size = New System.Drawing.Size(520, 1)
        Me.lblStartupSep.TabIndex = 7
        '
        ' chkStartWindows
        '
        Me.chkStartWindows.AutoSize = True
        Me.chkStartWindows.Location = New System.Drawing.Point(12, 180)
        Me.chkStartWindows.Name = "chkStartWindows"
        Me.chkStartWindows.Size = New System.Drawing.Size(134, 19)
        Me.chkStartWindows.TabIndex = 8
        Me.chkStartWindows.Text = "Start with Windows"
        Me.chkStartWindows.UseVisualStyleBackColor = True
        '
        ' chkMinimizeToTray
        '
        Me.chkStartMinimized.AutoSize = True
        Me.chkStartMinimized.Location = New System.Drawing.Point(12, 205)
        Me.chkStartMinimized.Name = "chkStartMinimized"
        Me.chkStartMinimized.Size = New System.Drawing.Size(180, 19)
        Me.chkStartMinimized.TabIndex = 9
        Me.chkStartMinimized.Text = "Start minimized to tray"
        Me.chkStartMinimized.UseVisualStyleBackColor = True
        '
        ' chkMinimizeToTray
        '
        Me.chkMinimizeToTray.AutoSize = True
        Me.chkMinimizeToTray.Location = New System.Drawing.Point(12, 230)
        Me.chkMinimizeToTray.Name = "chkMinimizeToTray"
        Me.chkMinimizeToTray.Size = New System.Drawing.Size(180, 19)
        Me.chkMinimizeToTray.TabIndex = 10
        Me.chkMinimizeToTray.Text = "Minimize to tray on close"
        Me.chkMinimizeToTray.UseVisualStyleBackColor = True
        '
        ' chkResetFirstRun
        '
        Me.chkResetFirstRun.AutoSize = True
        Me.chkResetFirstRun.Location = New System.Drawing.Point(12, 255)
        Me.chkResetFirstRun.Name = "chkResetFirstRun"
        Me.chkResetFirstRun.Size = New System.Drawing.Size(200, 19)
        Me.chkResetFirstRun.TabIndex = 11
        Me.chkResetFirstRun.Text = "Re-run first-time setup on next start"
        Me.chkResetFirstRun.UseVisualStyleBackColor = True
        '
        ' ══════════════════════════════════════════════════════════════
        ' PATHS PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlPaths
        '
        Me.pnlPaths.AutoScroll = True
        Me.pnlPaths.Controls.Add(Me.txtYtdlpFormat)
        Me.pnlPaths.Controls.Add(Me.lblYtdlpFormat)
        Me.pnlPaths.Controls.Add(Me.lblAdvancedSep)
        Me.pnlPaths.Controls.Add(Me.lblAdvancedHeader)
        Me.pnlPaths.Controls.Add(Me.btnBrowseLogs)
        Me.pnlPaths.Controls.Add(Me.txtLogs)
        Me.pnlPaths.Controls.Add(Me.lblLogsPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseBibles)
        Me.pnlPaths.Controls.Add(Me.txtBibles)
        Me.pnlPaths.Controls.Add(Me.lblBiblesPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseGlossary)
        Me.pnlPaths.Controls.Add(Me.txtGlossary)
        Me.pnlPaths.Controls.Add(Me.lblGlossaryPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseOutputRoot)
        Me.pnlPaths.Controls.Add(Me.txtOutputRoot)
        Me.pnlPaths.Controls.Add(Me.lblOutputRootPath)
        Me.pnlPaths.Controls.Add(Me.lblDirectoriesSep)
        Me.pnlPaths.Controls.Add(Me.lblDirectoriesHeader)
        Me.pnlPaths.Controls.Add(Me.btnBrowseModelAudio)
        Me.pnlPaths.Controls.Add(Me.txtModelAudio)
        Me.pnlPaths.Controls.Add(Me.lblModelAudioPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseModel)
        Me.pnlPaths.Controls.Add(Me.txtModel)
        Me.pnlPaths.Controls.Add(Me.lblModelPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseTransModel)
        Me.pnlPaths.Controls.Add(Me.txtTransModel)
        Me.pnlPaths.Controls.Add(Me.lblTransModelPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseFwModel)
        Me.pnlPaths.Controls.Add(Me.txtFwModel)
        Me.pnlPaths.Controls.Add(Me.lblFwModelPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseGgmlModel)
        Me.pnlPaths.Controls.Add(Me.txtGgmlModel)
        Me.pnlPaths.Controls.Add(Me.lblGgmlModelPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseWhisperServer)
        Me.pnlPaths.Controls.Add(Me.txtWhisperServer)
        Me.pnlPaths.Controls.Add(Me.lblWhisperServerPath)
        Me.pnlPaths.Controls.Add(Me.lblModelPathsSep)
        Me.pnlPaths.Controls.Add(Me.lblModelPathsHeader)
        Me.pnlPaths.Controls.Add(Me.btnBrowseSubtitleEdit)
        Me.pnlPaths.Controls.Add(Me.txtSubtitleEdit)
        Me.pnlPaths.Controls.Add(Me.lblSubtitleEditPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseFfprobe)
        Me.pnlPaths.Controls.Add(Me.txtFfprobe)
        Me.pnlPaths.Controls.Add(Me.lblFfprobePath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseFfmpeg)
        Me.pnlPaths.Controls.Add(Me.txtFfmpeg)
        Me.pnlPaths.Controls.Add(Me.lblFfmpegPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseYtdlp)
        Me.pnlPaths.Controls.Add(Me.txtYtdlp)
        Me.pnlPaths.Controls.Add(Me.lblYtdlpPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseWhisper)
        Me.pnlPaths.Controls.Add(Me.txtWhisper)
        Me.pnlPaths.Controls.Add(Me.lblWhisperPath)
        Me.pnlPaths.Controls.Add(Me.lblToolPathsSep)
        Me.pnlPaths.Controls.Add(Me.lblToolPathsHeader)
        Me.pnlPaths.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlPaths.Location = New System.Drawing.Point(0, 0)
        Me.pnlPaths.Name = "pnlPaths"
        Me.pnlPaths.Size = New System.Drawing.Size(564, 469)
        Me.pnlPaths.TabIndex = 1
        Me.pnlPaths.Visible = False
        '
        ' lblToolPathsHeader
        '
        Me.lblToolPathsHeader.AutoSize = True
        Me.lblToolPathsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblToolPathsHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblToolPathsHeader.Name = "lblToolPathsHeader"
        Me.lblToolPathsHeader.Size = New System.Drawing.Size(84, 20)
        Me.lblToolPathsHeader.TabIndex = 0
        Me.lblToolPathsHeader.Text = "Tool Paths"
        '
        ' lblToolPathsSep
        '
        Me.lblToolPathsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblToolPathsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblToolPathsSep.Location = New System.Drawing.Point(8, 34)
        Me.lblToolPathsSep.Name = "lblToolPathsSep"
        Me.lblToolPathsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblToolPathsSep.TabIndex = 1
        '
        ' lblWhisperPath
        '
        Me.lblWhisperPath.AutoSize = True
        Me.lblWhisperPath.Location = New System.Drawing.Point(12, 42)
        Me.lblWhisperPath.Name = "lblWhisperPath"
        Me.lblWhisperPath.Size = New System.Drawing.Size(75, 15)
        Me.lblWhisperPath.TabIndex = 2
        Me.lblWhisperPath.Text = "Whisper CLI (job):"
        '
        ' txtWhisper
        '
        Me.txtWhisper.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtWhisper.Location = New System.Drawing.Point(12, 60)
        Me.txtWhisper.Name = "txtWhisper"
        Me.txtWhisper.Size = New System.Drawing.Size(510, 23)
        Me.txtWhisper.TabIndex = 3
        '
        ' btnBrowseWhisper
        '
        Me.btnBrowseWhisper.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseWhisper.Location = New System.Drawing.Point(528, 59)
        Me.btnBrowseWhisper.Name = "btnBrowseWhisper"
        Me.btnBrowseWhisper.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseWhisper.TabIndex = 4
        Me.btnBrowseWhisper.Text = "..."
        Me.btnBrowseWhisper.UseVisualStyleBackColor = True
        '
        ' lblYtdlpPath
        '
        Me.lblYtdlpPath.AutoSize = True
        Me.lblYtdlpPath.Location = New System.Drawing.Point(12, 94)
        Me.lblYtdlpPath.Name = "lblYtdlpPath"
        Me.lblYtdlpPath.Size = New System.Drawing.Size(42, 15)
        Me.lblYtdlpPath.TabIndex = 5
        Me.lblYtdlpPath.Text = "yt-dlp:"
        '
        ' txtYtdlp
        '
        Me.txtYtdlp.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtYtdlp.Location = New System.Drawing.Point(12, 112)
        Me.txtYtdlp.Name = "txtYtdlp"
        Me.txtYtdlp.Size = New System.Drawing.Size(510, 23)
        Me.txtYtdlp.TabIndex = 6
        '
        ' btnBrowseYtdlp
        '
        Me.btnBrowseYtdlp.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseYtdlp.Location = New System.Drawing.Point(528, 111)
        Me.btnBrowseYtdlp.Name = "btnBrowseYtdlp"
        Me.btnBrowseYtdlp.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseYtdlp.TabIndex = 7
        Me.btnBrowseYtdlp.Text = "..."
        Me.btnBrowseYtdlp.UseVisualStyleBackColor = True
        '
        ' lblFfmpegPath
        '
        Me.lblFfmpegPath.AutoSize = True
        Me.lblFfmpegPath.Location = New System.Drawing.Point(12, 146)
        Me.lblFfmpegPath.Name = "lblFfmpegPath"
        Me.lblFfmpegPath.Size = New System.Drawing.Size(52, 15)
        Me.lblFfmpegPath.TabIndex = 8
        Me.lblFfmpegPath.Text = "FFmpeg:"
        '
        ' txtFfmpeg
        '
        Me.txtFfmpeg.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtFfmpeg.Location = New System.Drawing.Point(12, 164)
        Me.txtFfmpeg.Name = "txtFfmpeg"
        Me.txtFfmpeg.Size = New System.Drawing.Size(510, 23)
        Me.txtFfmpeg.TabIndex = 9
        '
        ' btnBrowseFfmpeg
        '
        Me.btnBrowseFfmpeg.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseFfmpeg.Location = New System.Drawing.Point(528, 163)
        Me.btnBrowseFfmpeg.Name = "btnBrowseFfmpeg"
        Me.btnBrowseFfmpeg.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseFfmpeg.TabIndex = 10
        Me.btnBrowseFfmpeg.Text = "..."
        Me.btnBrowseFfmpeg.UseVisualStyleBackColor = True
        '
        ' lblFfprobePath
        '
        Me.lblFfprobePath.AutoSize = True
        Me.lblFfprobePath.Location = New System.Drawing.Point(12, 198)
        Me.lblFfprobePath.Name = "lblFfprobePath"
        Me.lblFfprobePath.Size = New System.Drawing.Size(52, 15)
        Me.lblFfprobePath.TabIndex = 11
        Me.lblFfprobePath.Text = "FFprobe:"
        '
        ' txtFfprobe
        '
        Me.txtFfprobe.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtFfprobe.Location = New System.Drawing.Point(12, 216)
        Me.txtFfprobe.Name = "txtFfprobe"
        Me.txtFfprobe.Size = New System.Drawing.Size(510, 23)
        Me.txtFfprobe.TabIndex = 12
        '
        ' btnBrowseFfprobe
        '
        Me.btnBrowseFfprobe.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseFfprobe.Location = New System.Drawing.Point(528, 215)
        Me.btnBrowseFfprobe.Name = "btnBrowseFfprobe"
        Me.btnBrowseFfprobe.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseFfprobe.TabIndex = 13
        Me.btnBrowseFfprobe.Text = "..."
        Me.btnBrowseFfprobe.UseVisualStyleBackColor = True
        '
        ' lblSubtitleEditPath
        '
        Me.lblSubtitleEditPath.AutoSize = True
        Me.lblSubtitleEditPath.Location = New System.Drawing.Point(12, 250)
        Me.lblSubtitleEditPath.Name = "lblSubtitleEditPath"
        Me.lblSubtitleEditPath.Size = New System.Drawing.Size(72, 15)
        Me.lblSubtitleEditPath.TabIndex = 14
        Me.lblSubtitleEditPath.Text = "SubtitleEdit:"
        '
        ' txtSubtitleEdit
        '
        Me.txtSubtitleEdit.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtSubtitleEdit.Location = New System.Drawing.Point(12, 268)
        Me.txtSubtitleEdit.Name = "txtSubtitleEdit"
        Me.txtSubtitleEdit.Size = New System.Drawing.Size(510, 23)
        Me.txtSubtitleEdit.TabIndex = 15
        '
        ' btnBrowseSubtitleEdit
        '
        Me.btnBrowseSubtitleEdit.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseSubtitleEdit.Location = New System.Drawing.Point(528, 267)
        Me.btnBrowseSubtitleEdit.Name = "btnBrowseSubtitleEdit"
        Me.btnBrowseSubtitleEdit.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseSubtitleEdit.TabIndex = 16
        Me.btnBrowseSubtitleEdit.Text = "..."
        Me.btnBrowseSubtitleEdit.UseVisualStyleBackColor = True
        '
        ' lblModelPathsHeader
        '
        Me.lblModelPathsHeader.AutoSize = True
        Me.lblModelPathsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblModelPathsHeader.Location = New System.Drawing.Point(8, 302)
        Me.lblModelPathsHeader.Name = "lblModelPathsHeader"
        Me.lblModelPathsHeader.Size = New System.Drawing.Size(102, 20)
        Me.lblModelPathsHeader.TabIndex = 17
        Me.lblModelPathsHeader.Text = "Model Paths"
        '
        ' lblModelPathsSep
        '
        Me.lblModelPathsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblModelPathsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblModelPathsSep.Location = New System.Drawing.Point(8, 324)
        Me.lblModelPathsSep.Name = "lblModelPathsSep"
        Me.lblModelPathsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblModelPathsSep.TabIndex = 18
        '
        ' lblWhisperServerPath
        '
        Me.lblWhisperServerPath.AutoSize = True
        Me.lblWhisperServerPath.Location = New System.Drawing.Point(12, 332)
        Me.lblWhisperServerPath.Name = "lblWhisperServerPath"
        Me.lblWhisperServerPath.Size = New System.Drawing.Size(95, 15)
        Me.lblWhisperServerPath.TabIndex = 50
        Me.lblWhisperServerPath.Text = "whisper-server:"
        '
        ' txtWhisperServer
        '
        Me.txtWhisperServer.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtWhisperServer.Location = New System.Drawing.Point(12, 350)
        Me.txtWhisperServer.Name = "txtWhisperServer"
        Me.txtWhisperServer.Size = New System.Drawing.Size(510, 23)
        Me.txtWhisperServer.TabIndex = 51
        '
        ' btnBrowseWhisperServer
        '
        Me.btnBrowseWhisperServer.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseWhisperServer.Location = New System.Drawing.Point(528, 349)
        Me.btnBrowseWhisperServer.Name = "btnBrowseWhisperServer"
        Me.btnBrowseWhisperServer.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseWhisperServer.TabIndex = 52
        Me.btnBrowseWhisperServer.Text = "..."
        Me.btnBrowseWhisperServer.UseVisualStyleBackColor = True
        '
        ' lblGgmlModelPath
        '
        Me.lblGgmlModelPath.AutoSize = True
        Me.lblGgmlModelPath.Location = New System.Drawing.Point(12, 384)
        Me.lblGgmlModelPath.Name = "lblGgmlModelPath"
        Me.lblGgmlModelPath.Size = New System.Drawing.Size(150, 15)
        Me.lblGgmlModelPath.TabIndex = 53
        Me.lblGgmlModelPath.Text = "GGML model (Vulkan/CPU):"
        '
        ' txtGgmlModel
        '
        Me.txtGgmlModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtGgmlModel.Location = New System.Drawing.Point(12, 402)
        Me.txtGgmlModel.Name = "txtGgmlModel"
        Me.txtGgmlModel.Size = New System.Drawing.Size(510, 23)
        Me.txtGgmlModel.TabIndex = 54
        '
        ' btnBrowseGgmlModel
        '
        Me.btnBrowseGgmlModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseGgmlModel.Location = New System.Drawing.Point(528, 401)
        Me.btnBrowseGgmlModel.Name = "btnBrowseGgmlModel"
        Me.btnBrowseGgmlModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseGgmlModel.TabIndex = 55
        Me.btnBrowseGgmlModel.Text = "..."
        Me.btnBrowseGgmlModel.UseVisualStyleBackColor = True
        '
        ' lblFwModelPath
        '
        Me.lblFwModelPath.AutoSize = True
        Me.lblFwModelPath.Location = New System.Drawing.Point(12, 436)
        Me.lblFwModelPath.Name = "lblFwModelPath"
        Me.lblFwModelPath.Size = New System.Drawing.Size(170, 15)
        Me.lblFwModelPath.TabIndex = 56
        Me.lblFwModelPath.Text = "faster-whisper model (CUDA):"
        '
        ' txtFwModel
        '
        Me.txtFwModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtFwModel.Location = New System.Drawing.Point(12, 454)
        Me.txtFwModel.Name = "txtFwModel"
        Me.txtFwModel.Size = New System.Drawing.Size(510, 23)
        Me.txtFwModel.TabIndex = 57
        '
        ' btnBrowseFwModel
        '
        Me.btnBrowseFwModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseFwModel.Location = New System.Drawing.Point(528, 453)
        Me.btnBrowseFwModel.Name = "btnBrowseFwModel"
        Me.btnBrowseFwModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseFwModel.TabIndex = 58
        Me.btnBrowseFwModel.Text = "..."
        Me.btnBrowseFwModel.UseVisualStyleBackColor = True
        '
        ' lblTransModelPath
        '
        Me.lblTransModelPath.AutoSize = True
        Me.lblTransModelPath.Location = New System.Drawing.Point(12, 488)
        Me.lblTransModelPath.Name = "lblTransModelPath"
        Me.lblTransModelPath.Size = New System.Drawing.Size(77, 15)
        Me.lblTransModelPath.TabIndex = 22
        Me.lblTransModelPath.Text = "Translation model:"
        '
        ' txtTransModel
        '
        Me.txtTransModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtTransModel.Location = New System.Drawing.Point(12, 506)
        Me.txtTransModel.Name = "txtTransModel"
        Me.txtTransModel.Size = New System.Drawing.Size(510, 23)
        Me.txtTransModel.TabIndex = 23
        '
        ' btnBrowseTransModel
        '
        Me.btnBrowseTransModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseTransModel.Location = New System.Drawing.Point(528, 505)
        Me.btnBrowseTransModel.Name = "btnBrowseTransModel"
        Me.btnBrowseTransModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseTransModel.TabIndex = 24
        Me.btnBrowseTransModel.Text = "..."
        Me.btnBrowseTransModel.UseVisualStyleBackColor = True
        '
        ' lblModelPath
        '
        Me.lblModelPath.AutoSize = True
        Me.lblModelPath.Location = New System.Drawing.Point(12, 540)
        Me.lblModelPath.Name = "lblModelPath"
        Me.lblModelPath.Size = New System.Drawing.Size(119, 15)
        Me.lblModelPath.TabIndex = 25
        Me.lblModelPath.Text = "STT model (job):"
        '
        ' txtModel
        '
        Me.txtModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtModel.Location = New System.Drawing.Point(12, 558)
        Me.txtModel.Name = "txtModel"
        Me.txtModel.Size = New System.Drawing.Size(510, 23)
        Me.txtModel.TabIndex = 26
        '
        ' btnBrowseModel
        '
        Me.btnBrowseModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseModel.Location = New System.Drawing.Point(528, 557)
        Me.btnBrowseModel.Name = "btnBrowseModel"
        Me.btnBrowseModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseModel.TabIndex = 27
        Me.btnBrowseModel.Text = "..."
        Me.btnBrowseModel.UseVisualStyleBackColor = True
        '
        ' lblModelAudioPath
        '
        Me.lblModelAudioPath.AutoSize = True
        Me.lblModelAudioPath.Location = New System.Drawing.Point(12, 592)
        Me.lblModelAudioPath.Name = "lblModelAudioPath"
        Me.lblModelAudioPath.Size = New System.Drawing.Size(136, 15)
        Me.lblModelAudioPath.TabIndex = 28
        Me.lblModelAudioPath.Text = "STT model (audio):"
        '
        ' txtModelAudio
        '
        Me.txtModelAudio.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtModelAudio.Location = New System.Drawing.Point(12, 610)
        Me.txtModelAudio.Name = "txtModelAudio"
        Me.txtModelAudio.Size = New System.Drawing.Size(510, 23)
        Me.txtModelAudio.TabIndex = 29
        '
        ' btnBrowseModelAudio
        '
        Me.btnBrowseModelAudio.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseModelAudio.Location = New System.Drawing.Point(528, 609)
        Me.btnBrowseModelAudio.Name = "btnBrowseModelAudio"
        Me.btnBrowseModelAudio.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseModelAudio.TabIndex = 30
        Me.btnBrowseModelAudio.Text = "..."
        Me.btnBrowseModelAudio.UseVisualStyleBackColor = True
        '
        ' lblDirectoriesHeader
        '
        Me.lblDirectoriesHeader.AutoSize = True
        Me.lblDirectoriesHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblDirectoriesHeader.Location = New System.Drawing.Point(8, 644)
        Me.lblDirectoriesHeader.Name = "lblDirectoriesHeader"
        Me.lblDirectoriesHeader.Size = New System.Drawing.Size(89, 20)
        Me.lblDirectoriesHeader.TabIndex = 31
        Me.lblDirectoriesHeader.Text = "Directories"
        '
        ' lblDirectoriesSep
        '
        Me.lblDirectoriesSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblDirectoriesSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblDirectoriesSep.Location = New System.Drawing.Point(8, 666)
        Me.lblDirectoriesSep.Name = "lblDirectoriesSep"
        Me.lblDirectoriesSep.Size = New System.Drawing.Size(520, 1)
        Me.lblDirectoriesSep.TabIndex = 32
        '
        ' lblOutputRootPath
        '
        Me.lblOutputRootPath.AutoSize = True
        Me.lblOutputRootPath.Location = New System.Drawing.Point(12, 674)
        Me.lblOutputRootPath.Name = "lblOutputRootPath"
        Me.lblOutputRootPath.Size = New System.Drawing.Size(73, 15)
        Me.lblOutputRootPath.TabIndex = 33
        Me.lblOutputRootPath.Text = "Output root:"
        '
        ' txtOutputRoot
        '
        Me.txtOutputRoot.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOutputRoot.Location = New System.Drawing.Point(12, 692)
        Me.txtOutputRoot.Name = "txtOutputRoot"
        Me.txtOutputRoot.Size = New System.Drawing.Size(510, 23)
        Me.txtOutputRoot.TabIndex = 34
        '
        ' btnBrowseOutputRoot
        '
        Me.btnBrowseOutputRoot.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseOutputRoot.Location = New System.Drawing.Point(528, 691)
        Me.btnBrowseOutputRoot.Name = "btnBrowseOutputRoot"
        Me.btnBrowseOutputRoot.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseOutputRoot.TabIndex = 35
        Me.btnBrowseOutputRoot.Text = "..."
        Me.btnBrowseOutputRoot.UseVisualStyleBackColor = True
        '
        ' lblGlossaryPath
        '
        Me.lblGlossaryPath.AutoSize = True
        Me.lblGlossaryPath.Location = New System.Drawing.Point(12, 726)
        Me.lblGlossaryPath.Name = "lblGlossaryPath"
        Me.lblGlossaryPath.Size = New System.Drawing.Size(76, 15)
        Me.lblGlossaryPath.TabIndex = 36
        Me.lblGlossaryPath.Text = "Glossary file:"
        '
        ' txtGlossary
        '
        Me.txtGlossary.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtGlossary.Location = New System.Drawing.Point(12, 744)
        Me.txtGlossary.Name = "txtGlossary"
        Me.txtGlossary.Size = New System.Drawing.Size(510, 23)
        Me.txtGlossary.TabIndex = 37
        '
        ' btnBrowseGlossary
        '
        Me.btnBrowseGlossary.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseGlossary.Location = New System.Drawing.Point(528, 743)
        Me.btnBrowseGlossary.Name = "btnBrowseGlossary"
        Me.btnBrowseGlossary.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseGlossary.TabIndex = 38
        Me.btnBrowseGlossary.Text = "..."
        Me.btnBrowseGlossary.UseVisualStyleBackColor = True
        '
        ' lblBiblesPath
        '
        Me.lblBiblesPath.AutoSize = True
        Me.lblBiblesPath.Location = New System.Drawing.Point(12, 778)
        Me.lblBiblesPath.Name = "lblBiblesPath"
        Me.lblBiblesPath.Size = New System.Drawing.Size(95, 15)
        Me.lblBiblesPath.TabIndex = 39
        Me.lblBiblesPath.Text = "Bibles directory:"
        '
        ' txtBibles
        '
        Me.txtBibles.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtBibles.Location = New System.Drawing.Point(12, 796)
        Me.txtBibles.Name = "txtBibles"
        Me.txtBibles.Size = New System.Drawing.Size(510, 23)
        Me.txtBibles.TabIndex = 40
        '
        ' btnBrowseBibles
        '
        Me.btnBrowseBibles.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseBibles.Location = New System.Drawing.Point(528, 795)
        Me.btnBrowseBibles.Name = "btnBrowseBibles"
        Me.btnBrowseBibles.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseBibles.TabIndex = 41
        Me.btnBrowseBibles.Text = "..."
        Me.btnBrowseBibles.UseVisualStyleBackColor = True
        '
        ' lblLogsPath
        '
        Me.lblLogsPath.AutoSize = True
        Me.lblLogsPath.Location = New System.Drawing.Point(12, 828)
        Me.lblLogsPath.Name = "lblLogsPath"
        Me.lblLogsPath.Size = New System.Drawing.Size(85, 15)
        Me.lblLogsPath.TabIndex = 42
        Me.lblLogsPath.Text = "Logs directory:"
        '
        ' txtLogs
        '
        Me.txtLogs.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtLogs.Location = New System.Drawing.Point(12, 846)
        Me.txtLogs.Name = "txtLogs"
        Me.txtLogs.Size = New System.Drawing.Size(510, 23)
        Me.txtLogs.TabIndex = 43
        '
        ' btnBrowseLogs
        '
        Me.btnBrowseLogs.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseLogs.Location = New System.Drawing.Point(528, 845)
        Me.btnBrowseLogs.Name = "btnBrowseLogs"
        Me.btnBrowseLogs.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseLogs.TabIndex = 44
        Me.btnBrowseLogs.Text = "..."
        Me.btnBrowseLogs.UseVisualStyleBackColor = True
        '
        ' lblAdvancedHeader
        '
        Me.lblAdvancedHeader.AutoSize = True
        Me.lblAdvancedHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvancedHeader.Location = New System.Drawing.Point(8, 880)
        Me.lblAdvancedHeader.Name = "lblAdvancedHeader"
        Me.lblAdvancedHeader.Size = New System.Drawing.Size(82, 20)
        Me.lblAdvancedHeader.TabIndex = 42
        Me.lblAdvancedHeader.Text = "Advanced"
        '
        ' lblAdvancedSep
        '
        Me.lblAdvancedSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvancedSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvancedSep.Location = New System.Drawing.Point(8, 902)
        Me.lblAdvancedSep.Name = "lblAdvancedSep"
        Me.lblAdvancedSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvancedSep.TabIndex = 43
        '
        ' lblYtdlpFormat
        '
        Me.lblYtdlpFormat.AutoSize = True
        Me.lblYtdlpFormat.Location = New System.Drawing.Point(12, 910)
        Me.lblYtdlpFormat.Name = "lblYtdlpFormat"
        Me.lblYtdlpFormat.Size = New System.Drawing.Size(81, 15)
        Me.lblYtdlpFormat.TabIndex = 44
        Me.lblYtdlpFormat.Text = "yt-dlp format:"
        '
        ' txtYtdlpFormat
        '
        Me.txtYtdlpFormat.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtYtdlpFormat.Location = New System.Drawing.Point(12, 928)
        Me.txtYtdlpFormat.Name = "txtYtdlpFormat"
        Me.txtYtdlpFormat.Size = New System.Drawing.Size(550, 23)
        Me.txtYtdlpFormat.TabIndex = 45
        '
        ' ══════════════════════════════════════════════════════════════
        ' SERVER PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlServer
        '
        Me.pnlServer.AutoScroll = True
        Me.pnlServer.Controls.Add(Me.btnManageTemplates)
        Me.pnlServer.Controls.Add(Me.lblTemplatesSep)
        Me.pnlServer.Controls.Add(Me.lblTemplatesHeader)
        Me.pnlDisplay.Controls.Add(Me.chkBold)
        Me.pnlDisplay.Controls.Add(Me.nudFontSize)
        Me.pnlDisplay.Controls.Add(Me.lblFontSize)
        Me.pnlDisplay.Controls.Add(Me.cboFont)
        Me.pnlDisplay.Controls.Add(Me.lblFont)
        Me.pnlDisplay.Controls.Add(Me.btnFgColor)
        Me.pnlDisplay.Controls.Add(Me.lblFgColor)
        Me.pnlDisplay.Controls.Add(Me.btnBgColor)
        Me.pnlDisplay.Controls.Add(Me.lblBgColor)
        Me.pnlDisplay.Controls.Add(Me.lblSubtitleAppearanceSep)
        Me.pnlDisplay.Controls.Add(Me.lblSubtitleAppearanceHeader)
        Me.pnlServer.Controls.Add(Me.txtPin)
        Me.pnlServer.Controls.Add(Me.lblPin)
        Me.pnlServer.Controls.Add(Me.chkFirewall)
        Me.pnlServer.Controls.Add(Me.nudLivePort)
        Me.pnlServer.Controls.Add(Me.lblLivePort)
        Me.pnlServer.Controls.Add(Me.nudPort)
        Me.pnlServer.Controls.Add(Me.lblPort)
        Me.pnlServer.Controls.Add(Me.lblNetworkSep)
        Me.pnlServer.Controls.Add(Me.lblNetworkHeader)
        Me.pnlServer.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlServer.Location = New System.Drawing.Point(0, 0)
        Me.pnlServer.Name = "pnlServer"
        Me.pnlServer.Size = New System.Drawing.Size(564, 469)
        Me.pnlServer.TabIndex = 2
        Me.pnlServer.Visible = False
        '
        ' lblNetworkHeader
        '
        Me.lblNetworkHeader.AutoSize = True
        Me.lblNetworkHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblNetworkHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblNetworkHeader.Name = "lblNetworkHeader"
        Me.lblNetworkHeader.Size = New System.Drawing.Size(72, 20)
        Me.lblNetworkHeader.TabIndex = 0
        Me.lblNetworkHeader.Text = "Network"
        '
        ' lblNetworkSep
        '
        Me.lblNetworkSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblNetworkSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblNetworkSep.Location = New System.Drawing.Point(8, 34)
        Me.lblNetworkSep.Name = "lblNetworkSep"
        Me.lblNetworkSep.Size = New System.Drawing.Size(520, 1)
        Me.lblNetworkSep.TabIndex = 1
        '
        ' lblPort
        '
        Me.lblPort.AutoSize = True
        Me.lblPort.Location = New System.Drawing.Point(12, 42)
        Me.lblPort.Name = "lblPort"
        Me.lblPort.Size = New System.Drawing.Size(115, 15)
        Me.lblPort.TabIndex = 2
        Me.lblPort.Text = "Subtitle server port:"
        '
        ' nudPort
        '
        Me.nudPort.Location = New System.Drawing.Point(12, 60)
        Me.nudPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.nudPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudPort.Name = "nudPort"
        Me.nudPort.Size = New System.Drawing.Size(80, 23)
        Me.nudPort.TabIndex = 3
        Me.nudPort.Value = New Decimal(New Integer() {5080, 0, 0, 0})
        '
        ' lblLivePort
        '
        Me.lblLivePort.AutoSize = True
        Me.lblLivePort.Location = New System.Drawing.Point(12, 94)
        Me.lblLivePort.Name = "lblLivePort"
        Me.lblLivePort.Size = New System.Drawing.Size(93, 15)
        Me.lblLivePort.TabIndex = 4
        Me.lblLivePort.Text = "Live server port:"
        '
        ' nudLivePort
        '
        Me.nudLivePort.Location = New System.Drawing.Point(12, 112)
        Me.nudLivePort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.nudLivePort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudLivePort.Name = "nudLivePort"
        Me.nudLivePort.Size = New System.Drawing.Size(80, 23)
        Me.nudLivePort.TabIndex = 5
        Me.nudLivePort.Value = New Decimal(New Integer() {5091, 0, 0, 0})
        '
        ' lblTransPort
        '
        Me.lblTransPort.AutoSize = True
        Me.lblTransPort.Location = New System.Drawing.Point(12, 170)
        Me.lblTransPort.Name = "lblTransPort"
        Me.lblTransPort.Size = New System.Drawing.Size(97, 15)
        Me.lblTransPort.TabIndex = 9
        Me.lblTransPort.Text = "Translation port:"
        '
        ' nudTransPort
        '
        Me.nudTransPort.Location = New System.Drawing.Point(12, 188)
        Me.nudTransPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.nudTransPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudTransPort.Name = "nudTransPort"
        Me.nudTransPort.Size = New System.Drawing.Size(80, 23)
        Me.nudTransPort.TabIndex = 10
        Me.nudTransPort.Value = New Decimal(New Integer() {5090, 0, 0, 0})
        '
        ' chkUseSpeechmaticsTranslation
        '
        Me.chkUseSpeechmaticsTranslation.AutoSize = True
        Me.chkUseSpeechmaticsTranslation.Location = New System.Drawing.Point(12, 224)
        Me.chkUseSpeechmaticsTranslation.Name = "chkUseSpeechmaticsTranslation"
        Me.chkUseSpeechmaticsTranslation.Size = New System.Drawing.Size(330, 19)
        Me.chkUseSpeechmaticsTranslation.TabIndex = 11
        Me.chkUseSpeechmaticsTranslation.Text = "Use Speechmatics translation (when Speechmatics is the STT engine)"
        Me.chkUseSpeechmaticsTranslation.UseVisualStyleBackColor = True
        '
        ' lblClauseHeader
        '
        Me.lblClauseHeader.AutoSize = True
        Me.lblClauseHeader.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lblClauseHeader.Location = New System.Drawing.Point(12, 262)
        Me.lblClauseHeader.Name = "lblClauseHeader"
        Me.lblClauseHeader.Size = New System.Drawing.Size(220, 15)
        Me.lblClauseHeader.TabIndex = 12
        Me.lblClauseHeader.Text = "Speechmatics clause buffering"
        '
        ' chkSpeechmaticsHoldClauses
        '
        Me.chkSpeechmaticsHoldClauses.AutoSize = True
        Me.chkSpeechmaticsHoldClauses.Location = New System.Drawing.Point(12, 286)
        Me.chkSpeechmaticsHoldClauses.Name = "chkSpeechmaticsHoldClauses"
        Me.chkSpeechmaticsHoldClauses.Size = New System.Drawing.Size(400, 19)
        Me.chkSpeechmaticsHoldClauses.TabIndex = 13
        Me.chkSpeechmaticsHoldClauses.Text = "Hold & merge fragments into whole clauses before translating"
        Me.chkSpeechmaticsHoldClauses.UseVisualStyleBackColor = True
        '
        ' chkClauseLockOnPunctuation
        '
        Me.chkClauseLockOnPunctuation.AutoSize = True
        Me.chkClauseLockOnPunctuation.Location = New System.Drawing.Point(12, 310)
        Me.chkClauseLockOnPunctuation.Name = "chkClauseLockOnPunctuation"
        Me.chkClauseLockOnPunctuation.Size = New System.Drawing.Size(400, 19)
        Me.chkClauseLockOnPunctuation.TabIndex = 14
        Me.chkClauseLockOnPunctuation.Text = "Lock immediately on sentence-final punctuation"
        Me.chkClauseLockOnPunctuation.UseVisualStyleBackColor = True
        '
        ' lblClauseGraceMs
        '
        Me.lblClauseGraceMs.AutoSize = True
        Me.lblClauseGraceMs.Location = New System.Drawing.Point(12, 338)
        Me.lblClauseGraceMs.Name = "lblClauseGraceMs"
        Me.lblClauseGraceMs.Size = New System.Drawing.Size(60, 15)
        Me.lblClauseGraceMs.TabIndex = 15
        Me.lblClauseGraceMs.Text = "Grace (ms)"
        '
        ' nudClauseGraceMs
        '
        Me.nudClauseGraceMs.Location = New System.Drawing.Point(12, 356)
        Me.nudClauseGraceMs.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
        Me.nudClauseGraceMs.Increment = New Decimal(New Integer() {100, 0, 0, 0})
        Me.nudClauseGraceMs.Name = "nudClauseGraceMs"
        Me.nudClauseGraceMs.Size = New System.Drawing.Size(72, 23)
        Me.nudClauseGraceMs.TabIndex = 16
        Me.nudClauseGraceMs.Value = New Decimal(New Integer() {1200, 0, 0, 0})
        '
        ' lblClauseMaxMs
        '
        Me.lblClauseMaxMs.AutoSize = True
        Me.lblClauseMaxMs.Location = New System.Drawing.Point(102, 338)
        Me.lblClauseMaxMs.Name = "lblClauseMaxMs"
        Me.lblClauseMaxMs.Size = New System.Drawing.Size(60, 15)
        Me.lblClauseMaxMs.TabIndex = 17
        Me.lblClauseMaxMs.Text = "Max (ms)"
        '
        ' nudClauseMaxMs
        '
        Me.nudClauseMaxMs.Location = New System.Drawing.Point(102, 356)
        Me.nudClauseMaxMs.Maximum = New Decimal(New Integer() {60000, 0, 0, 0})
        Me.nudClauseMaxMs.Increment = New Decimal(New Integer() {500, 0, 0, 0})
        Me.nudClauseMaxMs.Name = "nudClauseMaxMs"
        Me.nudClauseMaxMs.Size = New System.Drawing.Size(72, 23)
        Me.nudClauseMaxMs.TabIndex = 18
        Me.nudClauseMaxMs.Value = New Decimal(New Integer() {8000, 0, 0, 0})
        '
        ' lblClauseMaxChars
        '
        Me.lblClauseMaxChars.AutoSize = True
        Me.lblClauseMaxChars.Location = New System.Drawing.Point(192, 338)
        Me.lblClauseMaxChars.Name = "lblClauseMaxChars"
        Me.lblClauseMaxChars.Size = New System.Drawing.Size(70, 15)
        Me.lblClauseMaxChars.TabIndex = 19
        Me.lblClauseMaxChars.Text = "Max (chars)"
        '
        ' nudClauseMaxChars
        '
        Me.nudClauseMaxChars.Location = New System.Drawing.Point(192, 356)
        Me.nudClauseMaxChars.Maximum = New Decimal(New Integer() {2000, 0, 0, 0})
        Me.nudClauseMaxChars.Minimum = New Decimal(New Integer() {20, 0, 0, 0})
        Me.nudClauseMaxChars.Name = "nudClauseMaxChars"
        Me.nudClauseMaxChars.Size = New System.Drawing.Size(72, 23)
        Me.nudClauseMaxChars.TabIndex = 20
        Me.nudClauseMaxChars.Value = New Decimal(New Integer() {300, 0, 0, 0})
        '
        ' lblClauseMinLockChars
        '
        Me.lblClauseMinLockChars.AutoSize = True
        Me.lblClauseMinLockChars.Location = New System.Drawing.Point(282, 338)
        Me.lblClauseMinLockChars.Name = "lblClauseMinLockChars"
        Me.lblClauseMinLockChars.Size = New System.Drawing.Size(80, 15)
        Me.lblClauseMinLockChars.TabIndex = 21
        Me.lblClauseMinLockChars.Text = "Min lock (ch)"
        '
        ' nudClauseMinLockChars
        '
        Me.nudClauseMinLockChars.Location = New System.Drawing.Point(282, 356)
        Me.nudClauseMinLockChars.Maximum = New Decimal(New Integer() {200, 0, 0, 0})
        Me.nudClauseMinLockChars.Name = "nudClauseMinLockChars"
        Me.nudClauseMinLockChars.Size = New System.Drawing.Size(72, 23)
        Me.nudClauseMinLockChars.TabIndex = 22
        Me.nudClauseMinLockChars.Value = New Decimal(New Integer() {12, 0, 0, 0})
        '
        ' lblClauseTimerMs
        '
        Me.lblClauseTimerMs.AutoSize = True
        Me.lblClauseTimerMs.Location = New System.Drawing.Point(372, 338)
        Me.lblClauseTimerMs.Name = "lblClauseTimerMs"
        Me.lblClauseTimerMs.Size = New System.Drawing.Size(60, 15)
        Me.lblClauseTimerMs.TabIndex = 23
        Me.lblClauseTimerMs.Text = "Poll (ms)"
        '
        ' nudClauseTimerMs
        '
        Me.nudClauseTimerMs.Location = New System.Drawing.Point(372, 356)
        Me.nudClauseTimerMs.Maximum = New Decimal(New Integer() {5000, 0, 0, 0})
        Me.nudClauseTimerMs.Minimum = New Decimal(New Integer() {50, 0, 0, 0})
        Me.nudClauseTimerMs.Increment = New Decimal(New Integer() {50, 0, 0, 0})
        Me.nudClauseTimerMs.Name = "nudClauseTimerMs"
        Me.nudClauseTimerMs.Size = New System.Drawing.Size(72, 23)
        Me.nudClauseTimerMs.TabIndex = 24
        Me.nudClauseTimerMs.Value = New Decimal(New Integer() {300, 0, 0, 0})
        '
        ' lblClauseSentenceEnders
        '
        Me.lblClauseSentenceEnders.AutoSize = True
        Me.lblClauseSentenceEnders.Location = New System.Drawing.Point(12, 392)
        Me.lblClauseSentenceEnders.Name = "lblClauseSentenceEnders"
        Me.lblClauseSentenceEnders.Size = New System.Drawing.Size(110, 15)
        Me.lblClauseSentenceEnders.TabIndex = 25
        Me.lblClauseSentenceEnders.Text = "Sentence enders"
        '
        ' txtClauseSentenceEnders
        '
        Me.txtClauseSentenceEnders.Location = New System.Drawing.Point(12, 410)
        Me.txtClauseSentenceEnders.Name = "txtClauseSentenceEnders"
        Me.txtClauseSentenceEnders.Size = New System.Drawing.Size(200, 23)
        Me.txtClauseSentenceEnders.TabIndex = 26
        '
        ' chkFirewall
        '
        Me.chkFirewall.AutoSize = True
        Me.chkFirewall.Location = New System.Drawing.Point(200, 114)
        Me.chkFirewall.Name = "chkFirewall"
        Me.chkFirewall.Size = New System.Drawing.Size(196, 19)
        Me.chkFirewall.TabIndex = 8
        Me.chkFirewall.Text = "Allow remote access (firewall)"
        Me.chkFirewall.UseVisualStyleBackColor = True
        '
        ' lblPin
        '
        Me.lblPin.AutoSize = True
        Me.lblPin.Location = New System.Drawing.Point(12, 146)
        Me.lblPin.Name = "lblPin"
        Me.lblPin.Size = New System.Drawing.Size(69, 15)
        Me.lblPin.TabIndex = 9
        Me.lblPin.Text = "Admin PIN:"
        '
        ' txtPin
        '
        Me.txtPin.Location = New System.Drawing.Point(12, 164)
        Me.txtPin.MaxLength = 8
        Me.txtPin.Name = "txtPin"
        Me.txtPin.Size = New System.Drawing.Size(100, 23)
        Me.txtPin.TabIndex = 10
        '
        ' lblSubtitleAppearanceHeader
        '
        Me.lblSubtitleAppearanceHeader.AutoSize = True
        Me.lblSubtitleAppearanceHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblSubtitleAppearanceHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblSubtitleAppearanceHeader.Name = "lblSubtitleAppearanceHeader"
        Me.lblSubtitleAppearanceHeader.Size = New System.Drawing.Size(163, 20)
        Me.lblSubtitleAppearanceHeader.TabIndex = 11
        Me.lblSubtitleAppearanceHeader.Text = "Subtitle Appearance"
        '
        ' lblSubtitleAppearanceSep
        '
        Me.lblSubtitleAppearanceSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblSubtitleAppearanceSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblSubtitleAppearanceSep.Location = New System.Drawing.Point(8, 34)
        Me.lblSubtitleAppearanceSep.Name = "lblSubtitleAppearanceSep"
        Me.lblSubtitleAppearanceSep.Size = New System.Drawing.Size(520, 1)
        Me.lblSubtitleAppearanceSep.TabIndex = 12
        '
        ' lblBgColor
        '
        Me.lblBgColor.AutoSize = True
        Me.lblBgColor.Location = New System.Drawing.Point(12, 42)
        Me.lblBgColor.Name = "lblBgColor"
        Me.lblBgColor.Size = New System.Drawing.Size(76, 15)
        Me.lblBgColor.TabIndex = 13
        Me.lblBgColor.Text = "Background:"
        '
        ' btnBgColor
        '
        Me.btnBgColor.BackColor = System.Drawing.Color.Black
        Me.btnBgColor.FlatAppearance.BorderColor = System.Drawing.Color.Gray
        Me.btnBgColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnBgColor.Location = New System.Drawing.Point(12, 60)
        Me.btnBgColor.Name = "btnBgColor"
        Me.btnBgColor.Size = New System.Drawing.Size(80, 23)
        Me.btnBgColor.TabIndex = 14
        Me.btnBgColor.UseVisualStyleBackColor = False
        '
        ' lblFgColor
        '
        Me.lblFgColor.AutoSize = True
        Me.lblFgColor.Location = New System.Drawing.Point(110, 42)
        Me.lblFgColor.Name = "lblFgColor"
        Me.lblFgColor.Size = New System.Drawing.Size(62, 15)
        Me.lblFgColor.TabIndex = 15
        Me.lblFgColor.Text = "Text color:"
        '
        ' btnFgColor
        '
        Me.btnFgColor.BackColor = System.Drawing.Color.White
        Me.btnFgColor.FlatAppearance.BorderColor = System.Drawing.Color.Gray
        Me.btnFgColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnFgColor.Location = New System.Drawing.Point(110, 60)
        Me.btnFgColor.Name = "btnFgColor"
        Me.btnFgColor.Size = New System.Drawing.Size(80, 23)
        Me.btnFgColor.TabIndex = 16
        Me.btnFgColor.UseVisualStyleBackColor = False
        '
        ' lblFont
        '
        Me.lblFont.AutoSize = True
        Me.lblFont.Location = New System.Drawing.Point(210, 42)
        Me.lblFont.Name = "lblFont"
        Me.lblFont.Size = New System.Drawing.Size(34, 15)
        Me.lblFont.TabIndex = 17
        Me.lblFont.Text = "Font:"
        '
        ' cboFont
        '
        Me.cboFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboFont.FormattingEnabled = True
        Me.cboFont.Location = New System.Drawing.Point(210, 60)
        Me.cboFont.Name = "cboFont"
        Me.cboFont.Size = New System.Drawing.Size(180, 23)
        Me.cboFont.TabIndex = 18
        '
        ' lblFontSize
        '
        Me.lblFontSize.AutoSize = True
        Me.lblFontSize.Location = New System.Drawing.Point(410, 42)
        Me.lblFontSize.Name = "lblFontSize"
        Me.lblFontSize.Size = New System.Drawing.Size(30, 15)
        Me.lblFontSize.TabIndex = 19
        Me.lblFontSize.Text = "Size:"
        '
        ' nudFontSize
        '
        Me.nudFontSize.Location = New System.Drawing.Point(410, 60)
        Me.nudFontSize.Maximum = New Decimal(New Integer() {72, 0, 0, 0})
        Me.nudFontSize.Minimum = New Decimal(New Integer() {8, 0, 0, 0})
        Me.nudFontSize.Name = "nudFontSize"
        Me.nudFontSize.Size = New System.Drawing.Size(55, 23)
        Me.nudFontSize.TabIndex = 20
        Me.nudFontSize.Value = New Decimal(New Integer() {14, 0, 0, 0})
        '
        ' chkBold
        '
        Me.chkBold.AutoSize = True
        Me.chkBold.Location = New System.Drawing.Point(480, 62)
        Me.chkBold.Name = "chkBold"
        Me.chkBold.Size = New System.Drawing.Size(50, 19)
        Me.chkBold.TabIndex = 21
        Me.chkBold.Text = "Bold"
        Me.chkBold.UseVisualStyleBackColor = True
        '
        ' lblTemplatesHeader
        '
        Me.lblTemplatesHeader.AutoSize = True
        Me.lblTemplatesHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblTemplatesHeader.Location = New System.Drawing.Point(8, 204)
        Me.lblTemplatesHeader.Name = "lblTemplatesHeader"
        Me.lblTemplatesHeader.Size = New System.Drawing.Size(170, 20)
        Me.lblTemplatesHeader.TabIndex = 22
        Me.lblTemplatesHeader.Text = "Conference Templates"
        '
        ' lblTemplatesSep
        '
        Me.lblTemplatesSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblTemplatesSep.Location = New System.Drawing.Point(8, 228)
        Me.lblTemplatesSep.Name = "lblTemplatesSep"
        Me.lblTemplatesSep.Size = New System.Drawing.Size(540, 2)
        Me.lblTemplatesSep.TabIndex = 23
        '
        ' btnManageTemplates
        '
        Me.btnManageTemplates.Location = New System.Drawing.Point(12, 238)
        Me.btnManageTemplates.Name = "btnManageTemplates"
        Me.btnManageTemplates.Size = New System.Drawing.Size(180, 30)
        Me.btnManageTemplates.TabIndex = 24
        Me.btnManageTemplates.Text = "Manage Templates..."
        Me.btnManageTemplates.UseVisualStyleBackColor = True
        '
        ' lblTranslationHeader
        '
        Me.lblTranslationHeader.AutoSize = True
        Me.lblTranslationHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblTranslationHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblTranslationHeader.Name = "lblTranslationHeader"
        Me.lblTranslationHeader.Size = New System.Drawing.Size(96, 20)
        Me.lblTranslationHeader.TabIndex = 0
        Me.lblTranslationHeader.Text = "Translation"
        '
        ' lblTranslationSep
        '
        Me.lblTranslationSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTranslationSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblTranslationSep.Location = New System.Drawing.Point(8, 34)
        Me.lblTranslationSep.Name = "lblTranslationSep"
        Me.lblTranslationSep.Size = New System.Drawing.Size(520, 1)
        Me.lblTranslationSep.TabIndex = 1
        '
        ' chkTransEnabled
        '
        Me.chkTransEnabled.AutoSize = True
        Me.chkTransEnabled.Location = New System.Drawing.Point(12, 42)
        Me.chkTransEnabled.Name = "chkTransEnabled"
        Me.chkTransEnabled.Size = New System.Drawing.Size(137, 19)
        Me.chkTransEnabled.TabIndex = 2
        Me.chkTransEnabled.Text = "Translation enabled"
        Me.chkTransEnabled.UseVisualStyleBackColor = True
        '
        ' lblTransBackend
        '
        Me.lblTransBackend.AutoSize = True
        Me.lblTransBackend.Location = New System.Drawing.Point(12, 68)
        Me.lblTransBackend.Name = "lblTransBackend"
        Me.lblTransBackend.Size = New System.Drawing.Size(47, 15)
        Me.lblTransBackend.TabIndex = 3
        Me.lblTransBackend.Text = "Engine:"
        '
        ' cboTransBackend
        '
        Me.cboTransBackend.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTransBackend.FormattingEnabled = True
        Me.cboTransBackend.Location = New System.Drawing.Point(12, 86)
        Me.cboTransBackend.Name = "cboTransBackend"
        Me.cboTransBackend.Size = New System.Drawing.Size(200, 23)
        Me.cboTransBackend.TabIndex = 4
        '
        ' lblDevice
        '
        Me.lblDevice.AutoSize = True
        Me.lblDevice.Location = New System.Drawing.Point(12, 118)
        Me.lblDevice.Name = "lblDevice"
        Me.lblDevice.Size = New System.Drawing.Size(45, 15)
        Me.lblDevice.TabIndex = 5
        Me.lblDevice.Text = "Device:"
        '
        ' cboDevice
        '
        Me.cboDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboDevice.FormattingEnabled = True
        Me.cboDevice.Items.AddRange(New Object() {"cuda", "cpu"})
        Me.cboDevice.Location = New System.Drawing.Point(12, 136)
        Me.cboDevice.Name = "cboDevice"
        Me.cboDevice.Size = New System.Drawing.Size(90, 23)
        Me.cboDevice.TabIndex = 6
        '
        '
        '
        '
        '
        ' lblTtsHeader
        '
        Me.lblTtsHeader.AutoSize = True
        Me.lblTtsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblTtsHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblTtsHeader.Name = "lblTtsHeader"
        Me.lblTtsHeader.Size = New System.Drawing.Size(122, 20)
        Me.lblTtsHeader.TabIndex = 0
        Me.lblTtsHeader.Text = "Text-to-Speech"
        '
        ' lblTtsSep
        '
        Me.lblTtsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTtsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblTtsSep.Location = New System.Drawing.Point(8, 34)
        Me.lblTtsSep.Name = "lblTtsSep"
        Me.lblTtsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblTtsSep.TabIndex = 1
        '
        ' lblTtsPref1
        '
        Me.lblTtsPref1.AutoSize = True
        Me.lblTtsPref1.Location = New System.Drawing.Point(12, 44)
        Me.lblTtsPref1.Name = "lblTtsPref1"
        Me.lblTtsPref1.Size = New System.Drawing.Size(90, 15)
        Me.lblTtsPref1.TabIndex = 2
        Me.lblTtsPref1.Text = "1st preference:"
        '
        ' cboTtsPref1
        '
        Me.cboTtsPref1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTtsPref1.Location = New System.Drawing.Point(130, 41)
        Me.cboTtsPref1.Name = "cboTtsPref1"
        Me.cboTtsPref1.Size = New System.Drawing.Size(200, 23)
        Me.cboTtsPref1.TabIndex = 3
        '
        ' lblTtsPref2
        '
        Me.lblTtsPref2.AutoSize = True
        Me.lblTtsPref2.Location = New System.Drawing.Point(12, 74)
        Me.lblTtsPref2.Name = "lblTtsPref2"
        Me.lblTtsPref2.Size = New System.Drawing.Size(93, 15)
        Me.lblTtsPref2.TabIndex = 4
        Me.lblTtsPref2.Text = "2nd preference:"
        '
        ' cboTtsPref2
        '
        Me.cboTtsPref2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTtsPref2.Location = New System.Drawing.Point(130, 71)
        Me.cboTtsPref2.Name = "cboTtsPref2"
        Me.cboTtsPref2.Size = New System.Drawing.Size(200, 23)
        Me.cboTtsPref2.TabIndex = 5
        '
        ' lblTtsPref3
        '
        Me.lblTtsPref3.AutoSize = True
        Me.lblTtsPref3.Location = New System.Drawing.Point(12, 104)
        Me.lblTtsPref3.Name = "lblTtsPref3"
        Me.lblTtsPref3.Size = New System.Drawing.Size(90, 15)
        Me.lblTtsPref3.TabIndex = 6
        Me.lblTtsPref3.Text = "3rd preference:"
        '
        ' cboTtsPref3
        '
        Me.cboTtsPref3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTtsPref3.Location = New System.Drawing.Point(130, 101)
        Me.cboTtsPref3.Name = "cboTtsPref3"
        Me.cboTtsPref3.Size = New System.Drawing.Size(200, 23)
        Me.cboTtsPref3.TabIndex = 7
        '
        ' lblTtsNote
        '
        Me.lblTtsNote.AutoSize = True
        Me.lblTtsNote.ForeColor = System.Drawing.SystemColors.GrayText
        Me.lblTtsNote.Location = New System.Drawing.Point(12, 134)
        Me.lblTtsNote.Name = "lblTtsNote"
        Me.lblTtsNote.Size = New System.Drawing.Size(350, 15)
        Me.lblTtsNote.TabIndex = 8
        Me.lblTtsNote.Text = "Set to (none) to disable. Falls back to next preference if unavailable."
        '
        ' ══════════════════════════════════════════════════════════════
        ' TRANSLATION PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlTranslation
        '
        Me.pnlTranslation.AutoScroll = True
        Me.pnlTranslation.Controls.Add(Me.lblClauseHeader)
        Me.pnlTranslation.Controls.Add(Me.chkSpeechmaticsHoldClauses)
        Me.pnlTranslation.Controls.Add(Me.chkClauseLockOnPunctuation)
        Me.pnlTranslation.Controls.Add(Me.lblClauseGraceMs)
        Me.pnlTranslation.Controls.Add(Me.nudClauseGraceMs)
        Me.pnlTranslation.Controls.Add(Me.lblClauseMaxMs)
        Me.pnlTranslation.Controls.Add(Me.nudClauseMaxMs)
        Me.pnlTranslation.Controls.Add(Me.lblClauseMaxChars)
        Me.pnlTranslation.Controls.Add(Me.nudClauseMaxChars)
        Me.pnlTranslation.Controls.Add(Me.lblClauseMinLockChars)
        Me.pnlTranslation.Controls.Add(Me.nudClauseMinLockChars)
        Me.pnlTranslation.Controls.Add(Me.lblClauseTimerMs)
        Me.pnlTranslation.Controls.Add(Me.nudClauseTimerMs)
        Me.pnlTranslation.Controls.Add(Me.lblClauseSentenceEnders)
        Me.pnlTranslation.Controls.Add(Me.txtClauseSentenceEnders)
        Me.pnlTranslation.Controls.Add(Me.chkUseSpeechmaticsTranslation)
        Me.pnlTranslation.Controls.Add(Me.nudTransPort)
        Me.pnlTranslation.Controls.Add(Me.lblTransPort)
        Me.pnlTranslation.Controls.Add(Me.cboDevice)
        Me.pnlTranslation.Controls.Add(Me.lblDevice)
        Me.pnlTranslation.Controls.Add(Me.cboTransBackend)
        Me.pnlTranslation.Controls.Add(Me.lblTransBackend)
        Me.pnlTranslation.Controls.Add(Me.chkTransEnabled)
        Me.pnlTranslation.Controls.Add(Me.lblTranslationSep)
        Me.pnlTranslation.Controls.Add(Me.lblTranslationHeader)
        Me.pnlTranslation.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlTranslation.Location = New System.Drawing.Point(0, 0)
        Me.pnlTranslation.Name = "pnlTranslation"
        Me.pnlTranslation.Size = New System.Drawing.Size(564, 469)
        Me.pnlTranslation.TabIndex = 4
        Me.pnlTranslation.Visible = False
        '
        ' ══════════════════════════════════════════════════════════════
        ' TTS PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlTts
        '
        Me.pnlTts.AutoScroll = True
        Me.pnlTts.Controls.Add(Me.lblTtsNote)
        Me.pnlTts.Controls.Add(Me.cboTtsPref3)
        Me.pnlTts.Controls.Add(Me.lblTtsPref3)
        Me.pnlTts.Controls.Add(Me.cboTtsPref2)
        Me.pnlTts.Controls.Add(Me.lblTtsPref2)
        Me.pnlTts.Controls.Add(Me.cboTtsPref1)
        Me.pnlTts.Controls.Add(Me.lblTtsPref1)
        Me.pnlTts.Controls.Add(Me.lblTtsSep)
        Me.pnlTts.Controls.Add(Me.lblTtsHeader)
        Me.pnlTts.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlTts.Location = New System.Drawing.Point(0, 0)
        Me.pnlTts.Name = "pnlTts"
        Me.pnlTts.Size = New System.Drawing.Size(564, 469)
        Me.pnlTts.TabIndex = 5
        Me.pnlTts.Visible = False
        '
        ' ══════════════════════════════════════════════════════════════
        ' HARDWARE PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlHardware
        '
        Me.pnlHardware.AutoScroll = True
        Me.pnlStt.Controls.Add(Me.nudSttEouSilence)
        Me.pnlStt.Controls.Add(Me.lblSttEouSilence)
        Me.pnlStt.Controls.Add(Me.cboSttRegion)
        Me.pnlStt.Controls.Add(Me.lblSttRegion)
        Me.pnlStt.Controls.Add(Me.cboSttOperatingPoint)
        Me.pnlStt.Controls.Add(Me.lblSttOperatingPoint)
        Me.pnlStt.Controls.Add(Me.txtSttApiKey)
        Me.pnlStt.Controls.Add(Me.lblSttApiKey)
        Me.pnlStt.Controls.Add(Me.cboSttBackend)
        Me.pnlStt.Controls.Add(Me.lblSttBackend)
        Me.pnlStt.Controls.Add(Me.lblSttEngineSep)
        Me.pnlStt.Controls.Add(Me.lblSttEngineHeader)
        Me.pnlHardware.Controls.Add(Me.btnHwRescan)
        Me.pnlHardware.Controls.Add(Me.txtHwRecs)
        Me.pnlHardware.Controls.Add(Me.lblHwRecsSep)
        Me.pnlHardware.Controls.Add(Me.lblHwRecsHeader)
        Me.pnlHardware.Controls.Add(Me.lblHwOs)
        Me.pnlHardware.Controls.Add(Me.lblHwDisk)
        Me.pnlHardware.Controls.Add(Me.lblHwRam)
        Me.pnlHardware.Controls.Add(Me.lblHwCpu)
        Me.pnlHardware.Controls.Add(Me.lblHwGpu)
        Me.pnlHardware.Controls.Add(Me.lblHwBreakdownSep)
        Me.pnlHardware.Controls.Add(Me.lblHwBreakdownHeader)
        Me.pnlHardware.Controls.Add(Me.lblHwVerdict)
        Me.pnlHardware.Controls.Add(Me.pnlHwIndicator)
        Me.pnlHardware.Controls.Add(Me.lblHwOverallScore)
        Me.pnlHardware.Controls.Add(Me.lblHwOverallCaption)
        Me.pnlHardware.Controls.Add(Me.lblHwSep)
        Me.pnlHardware.Controls.Add(Me.lblHwHeader)
        Me.pnlHardware.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlHardware.Location = New System.Drawing.Point(0, 0)
        Me.pnlHardware.Name = "pnlHardware"
        Me.pnlHardware.Size = New System.Drawing.Size(564, 469)
        Me.pnlHardware.TabIndex = 3
        Me.pnlHardware.Visible = False
        '
        ' lblHwHeader
        '
        Me.lblHwHeader.AutoSize = True
        Me.lblHwHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblHwHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblHwHeader.Name = "lblHwHeader"
        Me.lblHwHeader.Size = New System.Drawing.Size(165, 20)
        Me.lblHwHeader.TabIndex = 0
        Me.lblHwHeader.Text = "Hardware Readiness"
        '
        ' lblHwSep
        '
        Me.lblHwSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblHwSep.Location = New System.Drawing.Point(8, 34)
        Me.lblHwSep.Name = "lblHwSep"
        Me.lblHwSep.Size = New System.Drawing.Size(520, 1)
        Me.lblHwSep.TabIndex = 1
        '
        ' lblHwOverallCaption
        '
        Me.lblHwOverallCaption.AutoSize = True
        Me.lblHwOverallCaption.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblHwOverallCaption.Location = New System.Drawing.Point(12, 46)
        Me.lblHwOverallCaption.Name = "lblHwOverallCaption"
        Me.lblHwOverallCaption.Size = New System.Drawing.Size(93, 19)
        Me.lblHwOverallCaption.TabIndex = 2
        Me.lblHwOverallCaption.Text = "Overall Score:"
        '
        ' lblHwOverallScore
        '
        Me.lblHwOverallScore.AutoSize = True
        Me.lblHwOverallScore.Font = New System.Drawing.Font("Segoe UI", 18.0!, System.Drawing.FontStyle.Bold)
        Me.lblHwOverallScore.Location = New System.Drawing.Point(40, 68)
        Me.lblHwOverallScore.Name = "lblHwOverallScore"
        Me.lblHwOverallScore.Size = New System.Drawing.Size(50, 32)
        Me.lblHwOverallScore.TabIndex = 3
        Me.lblHwOverallScore.Text = "—"
        '
        ' pnlHwIndicator
        '
        Me.pnlHwIndicator.BackColor = System.Drawing.Color.Gray
        Me.pnlHwIndicator.Location = New System.Drawing.Point(12, 74)
        Me.pnlHwIndicator.Name = "pnlHwIndicator"
        Me.pnlHwIndicator.Size = New System.Drawing.Size(20, 20)
        Me.pnlHwIndicator.TabIndex = 4
        '
        ' lblHwVerdict
        '
        Me.lblHwVerdict.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwVerdict.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwVerdict.Location = New System.Drawing.Point(12, 106)
        Me.lblHwVerdict.Name = "lblHwVerdict"
        Me.lblHwVerdict.Size = New System.Drawing.Size(520, 20)
        Me.lblHwVerdict.TabIndex = 5
        Me.lblHwVerdict.Text = "Click Re-scan to check hardware."
        '
        ' lblHwBreakdownHeader
        '
        Me.lblHwBreakdownHeader.AutoSize = True
        Me.lblHwBreakdownHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblHwBreakdownHeader.Location = New System.Drawing.Point(8, 140)
        Me.lblHwBreakdownHeader.Name = "lblHwBreakdownHeader"
        Me.lblHwBreakdownHeader.Size = New System.Drawing.Size(170, 20)
        Me.lblHwBreakdownHeader.TabIndex = 6
        Me.lblHwBreakdownHeader.Text = "Component Scores"
        '
        ' lblHwBreakdownSep
        '
        Me.lblHwBreakdownSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwBreakdownSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblHwBreakdownSep.Location = New System.Drawing.Point(8, 162)
        Me.lblHwBreakdownSep.Name = "lblHwBreakdownSep"
        Me.lblHwBreakdownSep.Size = New System.Drawing.Size(520, 1)
        Me.lblHwBreakdownSep.TabIndex = 7
        '
        ' lblHwGpu
        '
        Me.lblHwGpu.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwGpu.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwGpu.Location = New System.Drawing.Point(12, 170)
        Me.lblHwGpu.Name = "lblHwGpu"
        Me.lblHwGpu.Size = New System.Drawing.Size(520, 18)
        Me.lblHwGpu.TabIndex = 8
        Me.lblHwGpu.Text = "GPU:  —"
        '
        ' lblHwCpu
        '
        Me.lblHwCpu.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwCpu.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwCpu.Location = New System.Drawing.Point(12, 192)
        Me.lblHwCpu.Name = "lblHwCpu"
        Me.lblHwCpu.Size = New System.Drawing.Size(520, 18)
        Me.lblHwCpu.TabIndex = 9
        Me.lblHwCpu.Text = "CPU:  —"
        '
        ' lblHwRam
        '
        Me.lblHwRam.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwRam.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwRam.Location = New System.Drawing.Point(12, 214)
        Me.lblHwRam.Name = "lblHwRam"
        Me.lblHwRam.Size = New System.Drawing.Size(520, 18)
        Me.lblHwRam.TabIndex = 10
        Me.lblHwRam.Text = "RAM:  —"
        '
        ' lblHwDisk
        '
        Me.lblHwDisk.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwDisk.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwDisk.Location = New System.Drawing.Point(12, 236)
        Me.lblHwDisk.Name = "lblHwDisk"
        Me.lblHwDisk.Size = New System.Drawing.Size(520, 18)
        Me.lblHwDisk.TabIndex = 11
        Me.lblHwDisk.Text = "Disk:  —"
        '
        ' lblHwOs
        '
        Me.lblHwOs.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwOs.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblHwOs.Location = New System.Drawing.Point(12, 258)
        Me.lblHwOs.Name = "lblHwOs"
        Me.lblHwOs.Size = New System.Drawing.Size(520, 18)
        Me.lblHwOs.TabIndex = 12
        Me.lblHwOs.Text = "OS:  —"
        '
        ' lblHwRecsHeader
        '
        Me.lblHwRecsHeader.AutoSize = True
        Me.lblHwRecsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblHwRecsHeader.Location = New System.Drawing.Point(8, 290)
        Me.lblHwRecsHeader.Name = "lblHwRecsHeader"
        Me.lblHwRecsHeader.Size = New System.Drawing.Size(145, 20)
        Me.lblHwRecsHeader.TabIndex = 13
        Me.lblHwRecsHeader.Text = "Recommendations"
        '
        ' lblHwRecsSep
        '
        Me.lblHwRecsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblHwRecsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblHwRecsSep.Location = New System.Drawing.Point(8, 312)
        Me.lblHwRecsSep.Name = "lblHwRecsSep"
        Me.lblHwRecsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblHwRecsSep.TabIndex = 14
        '
        ' txtHwRecs
        '
        Me.txtHwRecs.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtHwRecs.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.txtHwRecs.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.txtHwRecs.Location = New System.Drawing.Point(12, 320)
        Me.txtHwRecs.Multiline = True
        Me.txtHwRecs.Name = "txtHwRecs"
        Me.txtHwRecs.ReadOnly = True
        Me.txtHwRecs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtHwRecs.Size = New System.Drawing.Size(520, 80)
        Me.txtHwRecs.TabIndex = 15
        Me.txtHwRecs.Text = ""
        '
        ' btnHwRescan
        '
        Me.btnHwRescan.Location = New System.Drawing.Point(12, 410)
        Me.btnHwRescan.Name = "btnHwRescan"
        Me.btnHwRescan.Size = New System.Drawing.Size(100, 28)
        Me.btnHwRescan.TabIndex = 16
        Me.btnHwRescan.Text = "Re-scan"
        Me.btnHwRescan.UseVisualStyleBackColor = True
        '
        ' lblSttEngineHeader
        '
        Me.lblSttEngineHeader.AutoSize = True
        Me.lblSttEngineHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblSttEngineHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblSttEngineHeader.Name = "lblSttEngineHeader"
        Me.lblSttEngineHeader.Size = New System.Drawing.Size(102, 20)
        Me.lblSttEngineHeader.TabIndex = 17
        Me.lblSttEngineHeader.Text = "STT Engine"
        '
        ' lblSttEngineSep
        '
        Me.lblSttEngineSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblSttEngineSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblSttEngineSep.Location = New System.Drawing.Point(8, 34)
        Me.lblSttEngineSep.Name = "lblSttEngineSep"
        Me.lblSttEngineSep.Size = New System.Drawing.Size(520, 1)
        Me.lblSttEngineSep.TabIndex = 18
        '
        ' lblSttBackend
        '
        Me.lblSttBackend.AutoSize = True
        Me.lblSttBackend.Location = New System.Drawing.Point(12, 44)
        Me.lblSttBackend.Name = "lblSttBackend"
        Me.lblSttBackend.Size = New System.Drawing.Size(77, 15)
        Me.lblSttBackend.TabIndex = 19
        Me.lblSttBackend.Text = "Engine:"
        '
        ' cboSttBackend
        '
        Me.cboSttBackend.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboSttBackend.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSttBackend.FormattingEnabled = True
        Me.cboSttBackend.Location = New System.Drawing.Point(12, 62)
        Me.cboSttBackend.Name = "cboSttBackend"
        Me.cboSttBackend.Size = New System.Drawing.Size(350, 23)
        Me.cboSttBackend.TabIndex = 20
        '
        ' lblSttApiKey
        '
        Me.lblSttApiKey.AutoSize = True
        Me.lblSttApiKey.Location = New System.Drawing.Point(12, 92)
        Me.lblSttApiKey.Name = "lblSttApiKey"
        Me.lblSttApiKey.Size = New System.Drawing.Size(77, 15)
        Me.lblSttApiKey.TabIndex = 21
        Me.lblSttApiKey.Text = "API Key:"
        Me.lblSttApiKey.Visible = False
        '
        ' txtSttApiKey
        '
        Me.txtSttApiKey.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtSttApiKey.Location = New System.Drawing.Point(12, 110)
        Me.txtSttApiKey.Name = "txtSttApiKey"
        Me.txtSttApiKey.Size = New System.Drawing.Size(350, 23)
        Me.txtSttApiKey.TabIndex = 22
        Me.txtSttApiKey.UseSystemPasswordChar = True
        Me.txtSttApiKey.Visible = False
        '
        ' lblSttOperatingPoint
        '
        Me.lblSttOperatingPoint.AutoSize = True
        Me.lblSttOperatingPoint.Location = New System.Drawing.Point(12, 142)
        Me.lblSttOperatingPoint.Name = "lblSttOperatingPoint"
        Me.lblSttOperatingPoint.Size = New System.Drawing.Size(95, 15)
        Me.lblSttOperatingPoint.TabIndex = 23
        Me.lblSttOperatingPoint.Text = "Operating point:"
        Me.lblSttOperatingPoint.Visible = False
        '
        ' cboSttOperatingPoint
        '
        Me.cboSttOperatingPoint.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboSttOperatingPoint.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSttOperatingPoint.FormattingEnabled = True
        Me.cboSttOperatingPoint.Location = New System.Drawing.Point(12, 160)
        Me.cboSttOperatingPoint.Name = "cboSttOperatingPoint"
        Me.cboSttOperatingPoint.Size = New System.Drawing.Size(350, 23)
        Me.cboSttOperatingPoint.TabIndex = 24
        Me.cboSttOperatingPoint.Visible = False
        '
        ' lblSttRegion
        '
        Me.lblSttRegion.AutoSize = True
        Me.lblSttRegion.Location = New System.Drawing.Point(12, 192)
        Me.lblSttRegion.Name = "lblSttRegion"
        Me.lblSttRegion.Size = New System.Drawing.Size(50, 15)
        Me.lblSttRegion.TabIndex = 25
        Me.lblSttRegion.Text = "Region:"
        Me.lblSttRegion.Visible = False
        '
        ' cboSttRegion
        '
        Me.cboSttRegion.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.cboSttRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSttRegion.FormattingEnabled = True
        Me.cboSttRegion.Location = New System.Drawing.Point(12, 210)
        Me.cboSttRegion.Name = "cboSttRegion"
        Me.cboSttRegion.Size = New System.Drawing.Size(350, 23)
        Me.cboSttRegion.TabIndex = 26
        Me.cboSttRegion.Visible = False
        '
        ' lblSttEouSilence
        '
        Me.lblSttEouSilence.AutoSize = True
        Me.lblSttEouSilence.Location = New System.Drawing.Point(12, 242)
        Me.lblSttEouSilence.Name = "lblSttEouSilence"
        Me.lblSttEouSilence.Size = New System.Drawing.Size(220, 15)
        Me.lblSttEouSilence.TabIndex = 27
        Me.lblSttEouSilence.Text = "End-of-utterance silence (ms)"
        Me.lblSttEouSilence.Visible = False
        '
        ' nudSttEouSilence
        '
        Me.nudSttEouSilence.Location = New System.Drawing.Point(240, 240)
        Me.nudSttEouSilence.Minimum = New Decimal(New Integer() {300, 0, 0, 0})
        Me.nudSttEouSilence.Maximum = New Decimal(New Integer() {4000, 0, 0, 0})
        Me.nudSttEouSilence.Increment = New Decimal(New Integer() {100, 0, 0, 0})
        Me.nudSttEouSilence.Name = "nudSttEouSilence"
        Me.nudSttEouSilence.Size = New System.Drawing.Size(80, 23)
        Me.nudSttEouSilence.TabIndex = 28
        Me.nudSttEouSilence.Value = New Decimal(New Integer() {800, 0, 0, 0})
        Me.nudSttEouSilence.Visible = False
        '
        ' ══════════════════════════════════════════════════════════════
        ' SPEECH-TO-TEXT PANEL (engine baseline; moved out of Hardware)
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlStt
        '
        Me.pnlStt.AutoScroll = True
        Me.pnlStt.Controls.Add(Me.btnManageSttTemplatesOpt)
        Me.pnlStt.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlStt.Location = New System.Drawing.Point(0, 0)
        Me.pnlStt.Name = "pnlStt"
        Me.pnlStt.Size = New System.Drawing.Size(564, 469)
        Me.pnlStt.TabIndex = 0
        Me.pnlStt.Visible = False
        '
        ' btnManageSttTemplatesOpt
        '
        Me.btnManageSttTemplatesOpt.Location = New System.Drawing.Point(12, 282)
        Me.btnManageSttTemplatesOpt.Name = "btnManageSttTemplatesOpt"
        Me.btnManageSttTemplatesOpt.Size = New System.Drawing.Size(190, 28)
        Me.btnManageSttTemplatesOpt.TabIndex = 30
        Me.btnManageSttTemplatesOpt.Text = "Manage STT Templates..."
        Me.btnManageSttTemplatesOpt.UseVisualStyleBackColor = True
        '
        ' ══════════════════════════════════════════════════════════════
        ' DISPLAY PANEL (subtitle appearance; moved out of Server)
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlDisplay
        '
        Me.pnlDisplay.AutoScroll = True
        Me.pnlDisplay.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlDisplay.Location = New System.Drawing.Point(0, 0)
        Me.pnlDisplay.Name = "pnlDisplay"
        Me.pnlDisplay.Size = New System.Drawing.Size(564, 469)
        Me.pnlDisplay.TabIndex = 0
        Me.pnlDisplay.Visible = False
        '
        ' ══════════════════════════════════════════════════════════════
        ' ADVANCED PANEL
        ' ══════════════════════════════════════════════════════════════
        '
        ' pnlAdvanced
        '
        Me.pnlAdvanced.AutoScroll = True
        Me.pnlAdvanced.Controls.Add(Me.lblAdvPipelineHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvPipelineSep)
        Me.pnlAdvanced.Controls.Add(Me.lblParallelJobs)
        Me.pnlAdvanced.Controls.Add(Me.nudParallelJobs)
        Me.pnlAdvanced.Controls.Add(Me.lblChunkSize)
        Me.pnlAdvanced.Controls.Add(Me.nudChunkSize)
        Me.pnlAdvanced.Controls.Add(Me.lblPollInterval)
        Me.pnlAdvanced.Controls.Add(Me.nudPollInterval)
        Me.pnlAdvanced.Controls.Add(Me.lblChunkTimeout)
        Me.pnlAdvanced.Controls.Add(Me.nudChunkTimeout)
        Me.pnlAdvanced.Controls.Add(Me.chkKeepChunks)
        Me.pnlAdvanced.Controls.Add(Me.chkKeepPreview)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvLivePipelineHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvLivePipelineSep)
        Me.pnlAdvanced.Controls.Add(Me.lblTranslationConcurrency)
        Me.pnlAdvanced.Controls.Add(Me.nudTranslationConcurrency)
        Me.pnlAdvanced.Controls.Add(Me.lblTtsConcurrency)
        Me.pnlAdvanced.Controls.Add(Me.nudTtsConcurrency)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvOutputHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvOutputSep)
        Me.pnlAdvanced.Controls.Add(Me.chkOutSrt)
        Me.pnlAdvanced.Controls.Add(Me.chkOutVtt)
        Me.pnlAdvanced.Controls.Add(Me.chkOutTxt)
        Me.pnlAdvanced.Controls.Add(Me.chkOutJson)
        Me.pnlAdvanced.Controls.Add(Me.chkOutCsv)
        Me.pnlAdvanced.Controls.Add(Me.chkOutLrc)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvWhisperHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvWhisperSep)
        Me.pnlAdvanced.Controls.Add(Me.lblThreads)
        Me.pnlAdvanced.Controls.Add(Me.nudThreads)
        Me.pnlAdvanced.Controls.Add(Me.lblProcessors)
        Me.pnlAdvanced.Controls.Add(Me.nudProcessors)
        Me.pnlAdvanced.Controls.Add(Me.lblBeamSize)
        Me.pnlAdvanced.Controls.Add(Me.nudBeamSize)
        Me.pnlAdvanced.Controls.Add(Me.lblBestOf)
        Me.pnlAdvanced.Controls.Add(Me.nudBestOf)
        Me.pnlAdvanced.Controls.Add(Me.lblTemperature)
        Me.pnlAdvanced.Controls.Add(Me.nudTemperature)
        Me.pnlAdvanced.Controls.Add(Me.lblTempInc)
        Me.pnlAdvanced.Controls.Add(Me.nudTempInc)
        Me.pnlAdvanced.Controls.Add(Me.lblMaxContext)
        Me.pnlAdvanced.Controls.Add(Me.nudMaxContext)
        Me.pnlAdvanced.Controls.Add(Me.lblMaxSegLen)
        Me.pnlAdvanced.Controls.Add(Me.nudMaxSegLen)
        Me.pnlAdvanced.Controls.Add(Me.lblMaxTokens)
        Me.pnlAdvanced.Controls.Add(Me.nudMaxTokens)
        Me.pnlAdvanced.Controls.Add(Me.lblAudioContext)
        Me.pnlAdvanced.Controls.Add(Me.nudAudioContext)
        Me.pnlAdvanced.Controls.Add(Me.lblWordThresh)
        Me.pnlAdvanced.Controls.Add(Me.nudWordThresh)
        Me.pnlAdvanced.Controls.Add(Me.lblEntropyThresh)
        Me.pnlAdvanced.Controls.Add(Me.nudEntropyThresh)
        Me.pnlAdvanced.Controls.Add(Me.lblLogProbThresh)
        Me.pnlAdvanced.Controls.Add(Me.nudLogProbThresh)
        Me.pnlAdvanced.Controls.Add(Me.lblNoSpeechThresh)
        Me.pnlAdvanced.Controls.Add(Me.nudNoSpeechThresh)
        Me.pnlAdvanced.Controls.Add(Me.lblVadThresh)
        Me.pnlAdvanced.Controls.Add(Me.nudVadThresh)
        Me.pnlAdvanced.Controls.Add(Me.lblInitialPrompt)
        Me.pnlAdvanced.Controls.Add(Me.txtInitialPrompt)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvFlagsHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvFlagsSep)
        Me.pnlAdvanced.Controls.Add(Me.chkSplitOnWord)
        Me.pnlAdvanced.Controls.Add(Me.chkNoGpu)
        Me.pnlAdvanced.Controls.Add(Me.chkFlashAttn)
        Me.pnlAdvanced.Controls.Add(Me.chkDiarize)
        Me.pnlAdvanced.Controls.Add(Me.chkTinyDiarize)
        Me.pnlAdvanced.Controls.Add(Me.chkTranslateEn)
        Me.pnlAdvanced.Controls.Add(Me.chkNoTimestamps)
        Me.pnlAdvanced.Controls.Add(Me.chkPrintProgress)
        Me.pnlAdvanced.Controls.Add(Me.chkPrintColours)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvLiveHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvLiveSep)
        Me.pnlAdvanced.Controls.Add(Me.lblComputeType)
        Me.pnlAdvanced.Controls.Add(Me.cboComputeType)
        Me.pnlAdvanced.Controls.Add(Me.lblLiveVadSilence)
        Me.pnlAdvanced.Controls.Add(Me.nudLiveVadSilence)
        Me.pnlAdvanced.Controls.Add(Me.lblLiveMaxSeg)
        Me.pnlAdvanced.Controls.Add(Me.nudLiveMaxSeg)
        Me.pnlAdvanced.Controls.Add(Me.lblLiveInterim)
        Me.pnlAdvanced.Controls.Add(Me.nudLiveInterim)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvBibleHeader)
        Me.pnlAdvanced.Controls.Add(Me.lblAdvBibleSep)
        Me.pnlAdvanced.Controls.Add(Me.chkShowBibleCopyright)
        Me.pnlAdvanced.Dock = System.Windows.Forms.DockStyle.Fill
        Me.pnlAdvanced.Location = New System.Drawing.Point(0, 0)
        Me.pnlAdvanced.Name = "pnlAdvanced"
        Me.pnlAdvanced.Size = New System.Drawing.Size(564, 469)
        Me.pnlAdvanced.TabIndex = 7
        Me.pnlAdvanced.Visible = False
        '
        ' ── Section 1: Transcription Pipeline ──
        '
        ' lblAdvPipelineHeader
        '
        Me.lblAdvPipelineHeader.AutoSize = True
        Me.lblAdvPipelineHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvPipelineHeader.Location = New System.Drawing.Point(8, 12)
        Me.lblAdvPipelineHeader.Name = "lblAdvPipelineHeader"
        Me.lblAdvPipelineHeader.Size = New System.Drawing.Size(176, 20)
        Me.lblAdvPipelineHeader.TabIndex = 0
        Me.lblAdvPipelineHeader.Text = "Transcription Pipeline"
        '
        ' lblAdvPipelineSep
        '
        Me.lblAdvPipelineSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvPipelineSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvPipelineSep.Location = New System.Drawing.Point(8, 34)
        Me.lblAdvPipelineSep.Name = "lblAdvPipelineSep"
        Me.lblAdvPipelineSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvPipelineSep.TabIndex = 1
        '
        ' lblParallelJobs
        '
        Me.lblParallelJobs.AutoSize = True
        Me.lblParallelJobs.Location = New System.Drawing.Point(12, 42)
        Me.lblParallelJobs.Name = "lblParallelJobs"
        Me.lblParallelJobs.Size = New System.Drawing.Size(74, 15)
        Me.lblParallelJobs.TabIndex = 2
        Me.lblParallelJobs.Text = "Parallel jobs:"
        '
        ' nudParallelJobs
        '
        Me.nudParallelJobs.Location = New System.Drawing.Point(130, 40)
        Me.nudParallelJobs.Maximum = New Decimal(New Integer() {16, 0, 0, 0})
        Me.nudParallelJobs.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudParallelJobs.Name = "nudParallelJobs"
        Me.nudParallelJobs.Size = New System.Drawing.Size(60, 23)
        Me.nudParallelJobs.TabIndex = 3
        Me.nudParallelJobs.Value = New Decimal(New Integer() {4, 0, 0, 0})
        '
        ' lblChunkSize
        '
        Me.lblChunkSize.AutoSize = True
        Me.lblChunkSize.Location = New System.Drawing.Point(220, 42)
        Me.lblChunkSize.Name = "lblChunkSize"
        Me.lblChunkSize.Size = New System.Drawing.Size(110, 15)
        Me.lblChunkSize.TabIndex = 4
        Me.lblChunkSize.Text = "Chunk size (seconds):"
        '
        ' nudChunkSize
        '
        Me.nudChunkSize.Location = New System.Drawing.Point(370, 40)
        Me.nudChunkSize.Maximum = New Decimal(New Integer() {1800, 0, 0, 0})
        Me.nudChunkSize.Minimum = New Decimal(New Integer() {30, 0, 0, 0})
        Me.nudChunkSize.Name = "nudChunkSize"
        Me.nudChunkSize.Size = New System.Drawing.Size(70, 23)
        Me.nudChunkSize.TabIndex = 5
        Me.nudChunkSize.Value = New Decimal(New Integer() {300, 0, 0, 0})
        '
        ' lblPollInterval
        '
        Me.lblPollInterval.AutoSize = True
        Me.lblPollInterval.Location = New System.Drawing.Point(12, 70)
        Me.lblPollInterval.Name = "lblPollInterval"
        Me.lblPollInterval.Size = New System.Drawing.Size(94, 15)
        Me.lblPollInterval.TabIndex = 6
        Me.lblPollInterval.Text = "Poll interval (ms):"
        '
        ' nudPollInterval
        '
        Me.nudPollInterval.Location = New System.Drawing.Point(130, 68)
        Me.nudPollInterval.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
        Me.nudPollInterval.Minimum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.nudPollInterval.Name = "nudPollInterval"
        Me.nudPollInterval.Size = New System.Drawing.Size(70, 23)
        Me.nudPollInterval.TabIndex = 7
        Me.nudPollInterval.Value = New Decimal(New Integer() {2000, 0, 0, 0})
        '
        ' lblChunkTimeout
        '
        Me.lblChunkTimeout.AutoSize = True
        Me.lblChunkTimeout.Location = New System.Drawing.Point(220, 70)
        Me.lblChunkTimeout.Name = "lblChunkTimeout"
        Me.lblChunkTimeout.Size = New System.Drawing.Size(130, 15)
        Me.lblChunkTimeout.TabIndex = 8
        Me.lblChunkTimeout.Text = "Chunk timeout (minutes):"
        '
        ' nudChunkTimeout
        '
        Me.nudChunkTimeout.Location = New System.Drawing.Point(370, 68)
        Me.nudChunkTimeout.Maximum = New Decimal(New Integer() {240, 0, 0, 0})
        Me.nudChunkTimeout.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudChunkTimeout.Name = "nudChunkTimeout"
        Me.nudChunkTimeout.Size = New System.Drawing.Size(70, 23)
        Me.nudChunkTimeout.TabIndex = 9
        Me.nudChunkTimeout.Value = New Decimal(New Integer() {60, 0, 0, 0})
        '
        ' chkKeepChunks
        '
        Me.chkKeepChunks.AutoSize = True
        Me.chkKeepChunks.Location = New System.Drawing.Point(12, 98)
        Me.chkKeepChunks.Name = "chkKeepChunks"
        Me.chkKeepChunks.Size = New System.Drawing.Size(113, 19)
        Me.chkKeepChunks.TabIndex = 10
        Me.chkKeepChunks.Text = "Keep chunk files"
        Me.chkKeepChunks.UseVisualStyleBackColor = True
        '
        ' chkKeepPreview
        '
        Me.chkKeepPreview.AutoSize = True
        Me.chkKeepPreview.Location = New System.Drawing.Point(170, 98)
        Me.chkKeepPreview.Name = "chkKeepPreview"
        Me.chkKeepPreview.Size = New System.Drawing.Size(120, 19)
        Me.chkKeepPreview.TabIndex = 11
        Me.chkKeepPreview.Text = "Keep preview files"
        Me.chkKeepPreview.UseVisualStyleBackColor = True
        '
        '
        '
        ' ── Section 2: Live Pipeline ──
        '
        ' lblAdvLivePipelineHeader
        '
        Me.lblAdvLivePipelineHeader.AutoSize = True
        Me.lblAdvLivePipelineHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvLivePipelineHeader.Location = New System.Drawing.Point(8, 125)
        Me.lblAdvLivePipelineHeader.Name = "lblAdvLivePipelineHeader"
        Me.lblAdvLivePipelineHeader.Size = New System.Drawing.Size(96, 20)
        Me.lblAdvLivePipelineHeader.TabIndex = 13
        Me.lblAdvLivePipelineHeader.Text = "Live Pipeline"
        '
        ' lblAdvLivePipelineSep
        '
        Me.lblAdvLivePipelineSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvLivePipelineSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvLivePipelineSep.Location = New System.Drawing.Point(8, 147)
        Me.lblAdvLivePipelineSep.Name = "lblAdvLivePipelineSep"
        Me.lblAdvLivePipelineSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvLivePipelineSep.TabIndex = 14
        '
        ' lblTranslationConcurrency
        '
        Me.lblTranslationConcurrency.AutoSize = True
        Me.lblTranslationConcurrency.Location = New System.Drawing.Point(12, 152)
        Me.lblTranslationConcurrency.Name = "lblTranslationConcurrency"
        Me.lblTranslationConcurrency.Size = New System.Drawing.Size(155, 15)
        Me.lblTranslationConcurrency.TabIndex = 15
        Me.lblTranslationConcurrency.Text = "Translation concurrency:"
        '
        ' nudTranslationConcurrency
        '
        Me.nudTranslationConcurrency.Location = New System.Drawing.Point(180, 150)
        Me.nudTranslationConcurrency.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudTranslationConcurrency.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nudTranslationConcurrency.Name = "nudTranslationConcurrency"
        Me.nudTranslationConcurrency.Size = New System.Drawing.Size(60, 23)
        Me.nudTranslationConcurrency.TabIndex = 16
        Me.nudTranslationConcurrency.Value = New Decimal(New Integer() {3, 0, 0, 0})
        '
        ' lblTtsConcurrency
        '
        Me.lblTtsConcurrency.AutoSize = True
        Me.lblTtsConcurrency.Location = New System.Drawing.Point(260, 152)
        Me.lblTtsConcurrency.Name = "lblTtsConcurrency"
        Me.lblTtsConcurrency.Size = New System.Drawing.Size(115, 15)
        Me.lblTtsConcurrency.TabIndex = 17
        Me.lblTtsConcurrency.Text = "TTS concurrency:"
        '
        ' nudTtsConcurrency
        '
        Me.nudTtsConcurrency.Location = New System.Drawing.Point(390, 150)
        Me.nudTtsConcurrency.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudTtsConcurrency.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nudTtsConcurrency.Name = "nudTtsConcurrency"
        Me.nudTtsConcurrency.Size = New System.Drawing.Size(60, 23)
        Me.nudTtsConcurrency.TabIndex = 18
        Me.nudTtsConcurrency.Value = New Decimal(New Integer() {3, 0, 0, 0})
        '
        ' ── Section 3: Output Formats ──
        '
        ' lblAdvOutputHeader
        '
        Me.lblAdvOutputHeader.AutoSize = True
        Me.lblAdvOutputHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvOutputHeader.Location = New System.Drawing.Point(8, 184)
        Me.lblAdvOutputHeader.Name = "lblAdvOutputHeader"
        Me.lblAdvOutputHeader.Size = New System.Drawing.Size(120, 20)
        Me.lblAdvOutputHeader.TabIndex = 19
        Me.lblAdvOutputHeader.Text = "Output Formats"
        '
        ' lblAdvOutputSep
        '
        Me.lblAdvOutputSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvOutputSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvOutputSep.Location = New System.Drawing.Point(8, 206)
        Me.lblAdvOutputSep.Name = "lblAdvOutputSep"
        Me.lblAdvOutputSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvOutputSep.TabIndex = 20
        '
        ' chkOutSrt
        '
        Me.chkOutSrt.AutoSize = True
        Me.chkOutSrt.Location = New System.Drawing.Point(12, 214)
        Me.chkOutSrt.Name = "chkOutSrt"
        Me.chkOutSrt.Size = New System.Drawing.Size(47, 19)
        Me.chkOutSrt.TabIndex = 21
        Me.chkOutSrt.Text = "SRT"
        Me.chkOutSrt.UseVisualStyleBackColor = True
        '
        ' chkOutVtt
        '
        Me.chkOutVtt.AutoSize = True
        Me.chkOutVtt.Location = New System.Drawing.Point(170, 214)
        Me.chkOutVtt.Name = "chkOutVtt"
        Me.chkOutVtt.Size = New System.Drawing.Size(47, 19)
        Me.chkOutVtt.TabIndex = 22
        Me.chkOutVtt.Text = "VTT"
        Me.chkOutVtt.UseVisualStyleBackColor = True
        '
        ' chkOutTxt
        '
        Me.chkOutTxt.AutoSize = True
        Me.chkOutTxt.Location = New System.Drawing.Point(330, 214)
        Me.chkOutTxt.Name = "chkOutTxt"
        Me.chkOutTxt.Size = New System.Drawing.Size(75, 19)
        Me.chkOutTxt.TabIndex = 23
        Me.chkOutTxt.Text = "Plain text"
        Me.chkOutTxt.UseVisualStyleBackColor = True
        '
        ' chkOutJson
        '
        Me.chkOutJson.AutoSize = True
        Me.chkOutJson.Location = New System.Drawing.Point(12, 239)
        Me.chkOutJson.Name = "chkOutJson"
        Me.chkOutJson.Size = New System.Drawing.Size(54, 19)
        Me.chkOutJson.TabIndex = 24
        Me.chkOutJson.Text = "JSON"
        Me.chkOutJson.UseVisualStyleBackColor = True
        '
        ' chkOutCsv
        '
        Me.chkOutCsv.AutoSize = True
        Me.chkOutCsv.Location = New System.Drawing.Point(170, 239)
        Me.chkOutCsv.Name = "chkOutCsv"
        Me.chkOutCsv.Size = New System.Drawing.Size(48, 19)
        Me.chkOutCsv.TabIndex = 25
        Me.chkOutCsv.Text = "CSV"
        Me.chkOutCsv.UseVisualStyleBackColor = True
        '
        ' chkOutLrc
        '
        Me.chkOutLrc.AutoSize = True
        Me.chkOutLrc.Location = New System.Drawing.Point(330, 239)
        Me.chkOutLrc.Name = "chkOutLrc"
        Me.chkOutLrc.Size = New System.Drawing.Size(46, 19)
        Me.chkOutLrc.TabIndex = 26
        Me.chkOutLrc.Text = "LRC"
        Me.chkOutLrc.UseVisualStyleBackColor = True
        '
        ' ── Section 4: STT Parameters ──
        '
        ' lblAdvWhisperHeader
        '
        Me.lblAdvWhisperHeader.AutoSize = True
        Me.lblAdvWhisperHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvWhisperHeader.Location = New System.Drawing.Point(8, 269)
        Me.lblAdvWhisperHeader.Name = "lblAdvWhisperHeader"
        Me.lblAdvWhisperHeader.Size = New System.Drawing.Size(156, 20)
        Me.lblAdvWhisperHeader.TabIndex = 27
        Me.lblAdvWhisperHeader.Text = "STT Parameters"
        '
        ' lblAdvWhisperSep
        '
        Me.lblAdvWhisperSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvWhisperSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvWhisperSep.Location = New System.Drawing.Point(8, 291)
        Me.lblAdvWhisperSep.Name = "lblAdvWhisperSep"
        Me.lblAdvWhisperSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvWhisperSep.TabIndex = 28
        '
        ' Row 1: Threads (left), Processors (right) — Y=299
        '
        ' lblThreads
        '
        Me.lblThreads.AutoSize = True
        Me.lblThreads.Location = New System.Drawing.Point(12, 301)
        Me.lblThreads.Name = "lblThreads"
        Me.lblThreads.Size = New System.Drawing.Size(52, 15)
        Me.lblThreads.TabIndex = 29
        Me.lblThreads.Text = "Threads:"
        '
        ' nudThreads
        '
        Me.nudThreads.Location = New System.Drawing.Point(130, 299)
        Me.nudThreads.Maximum = New Decimal(New Integer() {32, 0, 0, 0})
        Me.nudThreads.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudThreads.Name = "nudThreads"
        Me.nudThreads.Size = New System.Drawing.Size(60, 23)
        Me.nudThreads.TabIndex = 30
        Me.nudThreads.Value = New Decimal(New Integer() {4, 0, 0, 0})
        '
        ' lblProcessors
        '
        Me.lblProcessors.AutoSize = True
        Me.lblProcessors.Location = New System.Drawing.Point(280, 301)
        Me.lblProcessors.Name = "lblProcessors"
        Me.lblProcessors.Size = New System.Drawing.Size(67, 15)
        Me.lblProcessors.TabIndex = 31
        Me.lblProcessors.Text = "Processors:"
        '
        ' nudProcessors
        '
        Me.nudProcessors.Location = New System.Drawing.Point(400, 299)
        Me.nudProcessors.Maximum = New Decimal(New Integer() {8, 0, 0, 0})
        Me.nudProcessors.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudProcessors.Name = "nudProcessors"
        Me.nudProcessors.Size = New System.Drawing.Size(60, 23)
        Me.nudProcessors.TabIndex = 32
        Me.nudProcessors.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        ' Row 2: Beam size (left), Best of (right) — Y=327
        '
        ' lblBeamSize
        '
        Me.lblBeamSize.AutoSize = True
        Me.lblBeamSize.Location = New System.Drawing.Point(12, 329)
        Me.lblBeamSize.Name = "lblBeamSize"
        Me.lblBeamSize.Size = New System.Drawing.Size(61, 15)
        Me.lblBeamSize.TabIndex = 33
        Me.lblBeamSize.Text = "Beam size:"
        '
        ' nudBeamSize
        '
        Me.nudBeamSize.Location = New System.Drawing.Point(130, 327)
        Me.nudBeamSize.Maximum = New Decimal(New Integer() {20, 0, 0, 0})
        Me.nudBeamSize.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudBeamSize.Name = "nudBeamSize"
        Me.nudBeamSize.Size = New System.Drawing.Size(60, 23)
        Me.nudBeamSize.TabIndex = 34
        Me.nudBeamSize.Value = New Decimal(New Integer() {7, 0, 0, 0})
        '
        ' lblBestOf
        '
        Me.lblBestOf.AutoSize = True
        Me.lblBestOf.Location = New System.Drawing.Point(280, 329)
        Me.lblBestOf.Name = "lblBestOf"
        Me.lblBestOf.Size = New System.Drawing.Size(46, 15)
        Me.lblBestOf.TabIndex = 35
        Me.lblBestOf.Text = "Best of:"
        '
        ' nudBestOf
        '
        Me.nudBestOf.Location = New System.Drawing.Point(400, 327)
        Me.nudBestOf.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nudBestOf.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudBestOf.Name = "nudBestOf"
        Me.nudBestOf.Size = New System.Drawing.Size(60, 23)
        Me.nudBestOf.TabIndex = 36
        Me.nudBestOf.Value = New Decimal(New Integer() {5, 0, 0, 0})
        '
        ' Row 3: Temperature (left), Temperature increment (right) — Y=355
        '
        ' lblTemperature
        '
        Me.lblTemperature.AutoSize = True
        Me.lblTemperature.Location = New System.Drawing.Point(12, 357)
        Me.lblTemperature.Name = "lblTemperature"
        Me.lblTemperature.Size = New System.Drawing.Size(76, 15)
        Me.lblTemperature.TabIndex = 37
        Me.lblTemperature.Text = "Temperature:"
        '
        ' nudTemperature
        '
        Me.nudTemperature.DecimalPlaces = 2
        Me.nudTemperature.Increment = New Decimal(New Integer() {5, 0, 0, 131072})
        Me.nudTemperature.Location = New System.Drawing.Point(130, 355)
        Me.nudTemperature.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudTemperature.Name = "nudTemperature"
        Me.nudTemperature.Size = New System.Drawing.Size(60, 23)
        Me.nudTemperature.TabIndex = 38
        '
        ' lblTempInc
        '
        Me.lblTempInc.AutoSize = True
        Me.lblTempInc.Location = New System.Drawing.Point(280, 357)
        Me.lblTempInc.Name = "lblTempInc"
        Me.lblTempInc.Size = New System.Drawing.Size(120, 15)
        Me.lblTempInc.TabIndex = 39
        Me.lblTempInc.Text = "Temperature increment:"
        '
        ' nudTempInc
        '
        Me.nudTempInc.DecimalPlaces = 2
        Me.nudTempInc.Increment = New Decimal(New Integer() {5, 0, 0, 131072})
        Me.nudTempInc.Location = New System.Drawing.Point(400, 355)
        Me.nudTempInc.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudTempInc.Name = "nudTempInc"
        Me.nudTempInc.Size = New System.Drawing.Size(60, 23)
        Me.nudTempInc.TabIndex = 40
        '
        ' Row 4: Max context (left), Max segment length (right) — Y=327
        '
        ' lblMaxContext
        '
        Me.lblMaxContext.AutoSize = True
        Me.lblMaxContext.Location = New System.Drawing.Point(12, 329)
        Me.lblMaxContext.Name = "lblMaxContext"
        Me.lblMaxContext.Size = New System.Drawing.Size(74, 15)
        Me.lblMaxContext.TabIndex = 35
        Me.lblMaxContext.Text = "Max context:"
        '
        ' nudMaxContext
        '
        Me.nudMaxContext.Location = New System.Drawing.Point(130, 327)
        Me.nudMaxContext.Maximum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudMaxContext.Name = "nudMaxContext"
        Me.nudMaxContext.Size = New System.Drawing.Size(60, 23)
        Me.nudMaxContext.TabIndex = 36
        '
        ' lblMaxSegLen
        '
        Me.lblMaxSegLen.AutoSize = True
        Me.lblMaxSegLen.Location = New System.Drawing.Point(280, 329)
        Me.lblMaxSegLen.Name = "lblMaxSegLen"
        Me.lblMaxSegLen.Size = New System.Drawing.Size(108, 15)
        Me.lblMaxSegLen.TabIndex = 37
        Me.lblMaxSegLen.Text = "Max segment length:"
        '
        ' nudMaxSegLen
        '
        Me.nudMaxSegLen.Location = New System.Drawing.Point(400, 327)
        Me.nudMaxSegLen.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
        Me.nudMaxSegLen.Name = "nudMaxSegLen"
        Me.nudMaxSegLen.Size = New System.Drawing.Size(60, 23)
        Me.nudMaxSegLen.TabIndex = 38
        '
        ' Row 5: Max tokens (left), Audio context (right) — Y=355
        '
        ' lblMaxTokens
        '
        Me.lblMaxTokens.AutoSize = True
        Me.lblMaxTokens.Location = New System.Drawing.Point(12, 357)
        Me.lblMaxTokens.Name = "lblMaxTokens"
        Me.lblMaxTokens.Size = New System.Drawing.Size(72, 15)
        Me.lblMaxTokens.TabIndex = 39
        Me.lblMaxTokens.Text = "Max tokens:"
        '
        ' nudMaxTokens
        '
        Me.nudMaxTokens.Location = New System.Drawing.Point(130, 355)
        Me.nudMaxTokens.Maximum = New Decimal(New Integer() {1000, 0, 0, 0})
        Me.nudMaxTokens.Name = "nudMaxTokens"
        Me.nudMaxTokens.Size = New System.Drawing.Size(60, 23)
        Me.nudMaxTokens.TabIndex = 40
        '
        ' lblAudioContext
        '
        Me.lblAudioContext.AutoSize = True
        Me.lblAudioContext.Location = New System.Drawing.Point(280, 357)
        Me.lblAudioContext.Name = "lblAudioContext"
        Me.lblAudioContext.Size = New System.Drawing.Size(83, 15)
        Me.lblAudioContext.TabIndex = 41
        Me.lblAudioContext.Text = "Audio context:"
        '
        ' nudAudioContext
        '
        Me.nudAudioContext.Location = New System.Drawing.Point(400, 355)
        Me.nudAudioContext.Maximum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudAudioContext.Name = "nudAudioContext"
        Me.nudAudioContext.Size = New System.Drawing.Size(60, 23)
        Me.nudAudioContext.TabIndex = 42
        '
        ' Row 6: Word threshold (left), Entropy threshold (right) — Y=383
        '
        ' lblWordThresh
        '
        Me.lblWordThresh.AutoSize = True
        Me.lblWordThresh.Location = New System.Drawing.Point(12, 385)
        Me.lblWordThresh.Name = "lblWordThresh"
        Me.lblWordThresh.Size = New System.Drawing.Size(91, 15)
        Me.lblWordThresh.TabIndex = 43
        Me.lblWordThresh.Text = "Word threshold:"
        '
        ' nudWordThresh
        '
        Me.nudWordThresh.DecimalPlaces = 2
        Me.nudWordThresh.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        Me.nudWordThresh.Location = New System.Drawing.Point(130, 383)
        Me.nudWordThresh.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudWordThresh.Name = "nudWordThresh"
        Me.nudWordThresh.Size = New System.Drawing.Size(60, 23)
        Me.nudWordThresh.TabIndex = 44
        '
        ' lblEntropyThresh
        '
        Me.lblEntropyThresh.AutoSize = True
        Me.lblEntropyThresh.Location = New System.Drawing.Point(280, 385)
        Me.lblEntropyThresh.Name = "lblEntropyThresh"
        Me.lblEntropyThresh.Size = New System.Drawing.Size(103, 15)
        Me.lblEntropyThresh.TabIndex = 45
        Me.lblEntropyThresh.Text = "Entropy threshold:"
        '
        ' nudEntropyThresh
        '
        Me.nudEntropyThresh.DecimalPlaces = 1
        Me.nudEntropyThresh.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.nudEntropyThresh.Location = New System.Drawing.Point(400, 383)
        Me.nudEntropyThresh.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.nudEntropyThresh.Name = "nudEntropyThresh"
        Me.nudEntropyThresh.Size = New System.Drawing.Size(60, 23)
        Me.nudEntropyThresh.TabIndex = 46
        '
        ' Row 7: Log prob threshold (left), No speech threshold (right) — Y=411
        '
        ' lblLogProbThresh
        '
        Me.lblLogProbThresh.AutoSize = True
        Me.lblLogProbThresh.Location = New System.Drawing.Point(12, 413)
        Me.lblLogProbThresh.Name = "lblLogProbThresh"
        Me.lblLogProbThresh.Size = New System.Drawing.Size(106, 15)
        Me.lblLogProbThresh.TabIndex = 47
        Me.lblLogProbThresh.Text = "Log prob threshold:"
        '
        ' nudLogProbThresh
        '
        Me.nudLogProbThresh.DecimalPlaces = 1
        Me.nudLogProbThresh.Increment = New Decimal(New Integer() {1, 0, 0, 65536})
        Me.nudLogProbThresh.Location = New System.Drawing.Point(130, 411)
        Me.nudLogProbThresh.Maximum = New Decimal(New Integer() {0, 0, 0, 0})
        Me.nudLogProbThresh.Minimum = New Decimal(New Integer() {5, 0, 0, -2147483648})
        Me.nudLogProbThresh.Name = "nudLogProbThresh"
        Me.nudLogProbThresh.Size = New System.Drawing.Size(60, 23)
        Me.nudLogProbThresh.TabIndex = 48
        Me.nudLogProbThresh.Value = New Decimal(New Integer() {10, 0, 0, -2147418112})
        '
        ' lblNoSpeechThresh
        '
        Me.lblNoSpeechThresh.AutoSize = True
        Me.lblNoSpeechThresh.Location = New System.Drawing.Point(280, 413)
        Me.lblNoSpeechThresh.Name = "lblNoSpeechThresh"
        Me.lblNoSpeechThresh.Size = New System.Drawing.Size(116, 15)
        Me.lblNoSpeechThresh.TabIndex = 49
        Me.lblNoSpeechThresh.Text = "No speech threshold:"
        '
        ' nudNoSpeechThresh
        '
        Me.nudNoSpeechThresh.DecimalPlaces = 2
        Me.nudNoSpeechThresh.Increment = New Decimal(New Integer() {5, 0, 0, 131072})
        Me.nudNoSpeechThresh.Location = New System.Drawing.Point(400, 411)
        Me.nudNoSpeechThresh.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudNoSpeechThresh.Name = "nudNoSpeechThresh"
        Me.nudNoSpeechThresh.Size = New System.Drawing.Size(60, 23)
        Me.nudNoSpeechThresh.TabIndex = 50
        '
        ' Row 8: VAD threshold (left), Frequency threshold (right) — Y=439
        '
        ' lblVadThresh
        '
        Me.lblVadThresh.AutoSize = True
        Me.lblVadThresh.Location = New System.Drawing.Point(12, 441)
        Me.lblVadThresh.Name = "lblVadThresh"
        Me.lblVadThresh.Size = New System.Drawing.Size(85, 15)
        Me.lblVadThresh.TabIndex = 51
        Me.lblVadThresh.Text = "VAD threshold:"
        '
        ' nudVadThresh
        '
        Me.nudVadThresh.DecimalPlaces = 2
        Me.nudVadThresh.Increment = New Decimal(New Integer() {5, 0, 0, 131072})
        Me.nudVadThresh.Location = New System.Drawing.Point(130, 439)
        Me.nudVadThresh.Maximum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudVadThresh.Name = "nudVadThresh"
        Me.nudVadThresh.Size = New System.Drawing.Size(60, 23)
        Me.nudVadThresh.TabIndex = 52
        '
        '
        '
        '
        '
        ' Row 9: Initial prompt — Y=467
        '
        ' lblInitialPrompt
        '
        Me.lblInitialPrompt.AutoSize = True
        Me.lblInitialPrompt.Location = New System.Drawing.Point(12, 469)
        Me.lblInitialPrompt.Name = "lblInitialPrompt"
        Me.lblInitialPrompt.Size = New System.Drawing.Size(82, 15)
        Me.lblInitialPrompt.TabIndex = 55
        Me.lblInitialPrompt.Text = "Initial prompt:"
        '
        ' txtInitialPrompt
        '
        Me.txtInitialPrompt.Location = New System.Drawing.Point(130, 467)
        Me.txtInitialPrompt.Name = "txtInitialPrompt"
        Me.txtInitialPrompt.Size = New System.Drawing.Size(400, 23)
        Me.txtInitialPrompt.TabIndex = 56
        '
        '
        '
        '
        '
        '
        ' ── Section 4: STT Flags ──
        '
        ' lblAdvFlagsHeader
        '
        Me.lblAdvFlagsHeader.AutoSize = True
        Me.lblAdvFlagsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvFlagsHeader.Location = New System.Drawing.Point(8, 528)
        Me.lblAdvFlagsHeader.Name = "lblAdvFlagsHeader"
        Me.lblAdvFlagsHeader.Size = New System.Drawing.Size(112, 20)
        Me.lblAdvFlagsHeader.TabIndex = 59
        Me.lblAdvFlagsHeader.Text = "STT Flags"
        '
        ' lblAdvFlagsSep
        '
        Me.lblAdvFlagsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvFlagsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvFlagsSep.Location = New System.Drawing.Point(8, 550)
        Me.lblAdvFlagsSep.Name = "lblAdvFlagsSep"
        Me.lblAdvFlagsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvFlagsSep.TabIndex = 60
        '
        ' chkSplitOnWord — row 1
        '
        Me.chkSplitOnWord.AutoSize = True
        Me.chkSplitOnWord.Location = New System.Drawing.Point(12, 506)
        Me.chkSplitOnWord.Name = "chkSplitOnWord"
        Me.chkSplitOnWord.Size = New System.Drawing.Size(98, 19)
        Me.chkSplitOnWord.TabIndex = 61
        Me.chkSplitOnWord.Text = "Split on word"
        Me.chkSplitOnWord.UseVisualStyleBackColor = True
        '
        ' chkNoGpu
        '
        Me.chkNoGpu.AutoSize = True
        Me.chkNoGpu.Location = New System.Drawing.Point(170, 558)
        Me.chkNoGpu.Name = "chkNoGpu"
        Me.chkNoGpu.Size = New System.Drawing.Size(92, 19)
        Me.chkNoGpu.TabIndex = 62
        Me.chkNoGpu.Text = "Disable GPU"
        Me.chkNoGpu.UseVisualStyleBackColor = True
        '
        ' chkFlashAttn
        '
        Me.chkFlashAttn.AutoSize = True
        Me.chkFlashAttn.Location = New System.Drawing.Point(330, 558)
        Me.chkFlashAttn.Name = "chkFlashAttn"
        Me.chkFlashAttn.Size = New System.Drawing.Size(107, 19)
        Me.chkFlashAttn.TabIndex = 63
        Me.chkFlashAttn.Text = "Flash attention"
        Me.chkFlashAttn.UseVisualStyleBackColor = True
        '
        ' chkDiarize — row 2
        '
        Me.chkDiarize.AutoSize = True
        Me.chkDiarize.Location = New System.Drawing.Point(12, 583)
        Me.chkDiarize.Name = "chkDiarize"
        Me.chkDiarize.Size = New System.Drawing.Size(68, 19)
        Me.chkDiarize.TabIndex = 64
        Me.chkDiarize.Text = "Diarize"
        Me.chkDiarize.UseVisualStyleBackColor = True
        '
        ' chkTinyDiarize
        '
        Me.chkTinyDiarize.AutoSize = True
        Me.chkTinyDiarize.Location = New System.Drawing.Point(170, 583)
        Me.chkTinyDiarize.Name = "chkTinyDiarize"
        Me.chkTinyDiarize.Size = New System.Drawing.Size(94, 19)
        Me.chkTinyDiarize.TabIndex = 65
        Me.chkTinyDiarize.Text = "Tiny diarize"
        Me.chkTinyDiarize.UseVisualStyleBackColor = True
        '
        ' chkTranslateEn
        '
        Me.chkTranslateEn.AutoSize = True
        Me.chkTranslateEn.Location = New System.Drawing.Point(330, 583)
        Me.chkTranslateEn.Name = "chkTranslateEn"
        Me.chkTranslateEn.Size = New System.Drawing.Size(130, 19)
        Me.chkTranslateEn.TabIndex = 66
        Me.chkTranslateEn.Text = "Translate to English"
        Me.chkTranslateEn.UseVisualStyleBackColor = True
        '
        ' chkNoTimestamps — row 3
        '
        Me.chkNoTimestamps.AutoSize = True
        Me.chkNoTimestamps.Location = New System.Drawing.Point(12, 608)
        Me.chkNoTimestamps.Name = "chkNoTimestamps"
        Me.chkNoTimestamps.Size = New System.Drawing.Size(104, 19)
        Me.chkNoTimestamps.TabIndex = 67
        Me.chkNoTimestamps.Text = "No timestamps"
        Me.chkNoTimestamps.UseVisualStyleBackColor = True
        '
        ' chkPrintProgress
        '
        Me.chkPrintProgress.AutoSize = True
        Me.chkPrintProgress.Location = New System.Drawing.Point(170, 608)
        Me.chkPrintProgress.Name = "chkPrintProgress"
        Me.chkPrintProgress.Size = New System.Drawing.Size(101, 19)
        Me.chkPrintProgress.TabIndex = 68
        Me.chkPrintProgress.Text = "Print progress"
        Me.chkPrintProgress.UseVisualStyleBackColor = True
        '
        ' chkPrintColours
        '
        Me.chkPrintColours.AutoSize = True
        Me.chkPrintColours.Location = New System.Drawing.Point(330, 608)
        Me.chkPrintColours.Name = "chkPrintColours"
        Me.chkPrintColours.Size = New System.Drawing.Size(97, 19)
        Me.chkPrintColours.TabIndex = 69
        Me.chkPrintColours.Text = "Print colours"
        Me.chkPrintColours.UseVisualStyleBackColor = True
        '
        '
        '
        ' ── Section 5: Live Transcription ──
        '
        ' lblAdvLiveHeader
        '
        Me.lblAdvLiveHeader.AutoSize = True
        Me.lblAdvLiveHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvLiveHeader.Location = New System.Drawing.Point(8, 663)
        Me.lblAdvLiveHeader.Name = "lblAdvLiveHeader"
        Me.lblAdvLiveHeader.Size = New System.Drawing.Size(145, 20)
        Me.lblAdvLiveHeader.TabIndex = 71
        Me.lblAdvLiveHeader.Text = "Live Transcription"
        '
        ' lblAdvLiveSep
        '
        Me.lblAdvLiveSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvLiveSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvLiveSep.Location = New System.Drawing.Point(8, 685)
        Me.lblAdvLiveSep.Name = "lblAdvLiveSep"
        Me.lblAdvLiveSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvLiveSep.TabIndex = 72
        '
        ' lblComputeType
        '
        Me.lblComputeType.AutoSize = True
        Me.lblComputeType.Location = New System.Drawing.Point(12, 693)
        Me.lblComputeType.Name = "lblComputeType"
        Me.lblComputeType.Size = New System.Drawing.Size(82, 15)
        Me.lblComputeType.TabIndex = 73
        Me.lblComputeType.Text = "Compute type:"
        '
        ' cboComputeType
        '
        Me.cboComputeType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboComputeType.FormattingEnabled = True
        Me.cboComputeType.Items.AddRange(New Object() {"int8", "int8_float16", "float16", "float32"})
        Me.cboComputeType.Location = New System.Drawing.Point(130, 690)
        Me.cboComputeType.Name = "cboComputeType"
        Me.cboComputeType.Size = New System.Drawing.Size(120, 23)
        Me.cboComputeType.TabIndex = 74
        '
        ' lblLiveVadSilence
        '
        Me.lblLiveVadSilence.AutoSize = True
        Me.lblLiveVadSilence.Location = New System.Drawing.Point(280, 693)
        Me.lblLiveVadSilence.Name = "lblLiveVadSilence"
        Me.lblLiveVadSilence.Size = New System.Drawing.Size(92, 15)
        Me.lblLiveVadSilence.TabIndex = 75
        Me.lblLiveVadSilence.Text = "VAD silence (ms):"
        '
        ' nudLiveVadSilence
        '
        Me.nudLiveVadSilence.Location = New System.Drawing.Point(400, 691)
        Me.nudLiveVadSilence.Maximum = New Decimal(New Integer() {5000, 0, 0, 0})
        Me.nudLiveVadSilence.Minimum = New Decimal(New Integer() {100, 0, 0, 0})
        Me.nudLiveVadSilence.Name = "nudLiveVadSilence"
        Me.nudLiveVadSilence.Size = New System.Drawing.Size(70, 23)
        Me.nudLiveVadSilence.TabIndex = 76
        Me.nudLiveVadSilence.Value = New Decimal(New Integer() {800, 0, 0, 0})
        '
        ' lblLiveMaxSeg
        '
        Me.lblLiveMaxSeg.AutoSize = True
        Me.lblLiveMaxSeg.Location = New System.Drawing.Point(12, 721)
        Me.lblLiveMaxSeg.Name = "lblLiveMaxSeg"
        Me.lblLiveMaxSeg.Size = New System.Drawing.Size(120, 15)
        Me.lblLiveMaxSeg.TabIndex = 77
        Me.lblLiveMaxSeg.Text = "Max segment (seconds):"
        '
        ' nudLiveMaxSeg
        '
        Me.nudLiveMaxSeg.Location = New System.Drawing.Point(160, 719)
        Me.nudLiveMaxSeg.Maximum = New Decimal(New Integer() {60, 0, 0, 0})
        Me.nudLiveMaxSeg.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.nudLiveMaxSeg.Name = "nudLiveMaxSeg"
        Me.nudLiveMaxSeg.Size = New System.Drawing.Size(60, 23)
        Me.nudLiveMaxSeg.TabIndex = 78
        Me.nudLiveMaxSeg.Value = New Decimal(New Integer() {15, 0, 0, 0})
        '
        ' lblLiveInterim
        '
        Me.lblLiveInterim.AutoSize = True
        Me.lblLiveInterim.Location = New System.Drawing.Point(280, 721)
        Me.lblLiveInterim.Name = "lblLiveInterim"
        Me.lblLiveInterim.Size = New System.Drawing.Size(112, 15)
        Me.lblLiveInterim.TabIndex = 79
        Me.lblLiveInterim.Text = "Interim interval (ms):"
        '
        ' nudLiveInterim
        '
        Me.nudLiveInterim.Location = New System.Drawing.Point(400, 719)
        Me.nudLiveInterim.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
        Me.nudLiveInterim.Minimum = New Decimal(New Integer() {500, 0, 0, 0})
        Me.nudLiveInterim.Name = "nudLiveInterim"
        Me.nudLiveInterim.Size = New System.Drawing.Size(70, 23)
        Me.nudLiveInterim.TabIndex = 80
        Me.nudLiveInterim.Value = New Decimal(New Integer() {1500, 0, 0, 0})
        '
        ' ── Section 6: Bible ──
        '
        ' lblAdvBibleHeader
        '
        Me.lblAdvBibleHeader.AutoSize = True
        Me.lblAdvBibleHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvBibleHeader.Location = New System.Drawing.Point(8, 760)
        Me.lblAdvBibleHeader.Name = "lblAdvBibleHeader"
        Me.lblAdvBibleHeader.Size = New System.Drawing.Size(45, 20)
        Me.lblAdvBibleHeader.TabIndex = 81
        Me.lblAdvBibleHeader.Text = "Bible"
        '
        ' lblAdvBibleSep
        '
        Me.lblAdvBibleSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblAdvBibleSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblAdvBibleSep.Location = New System.Drawing.Point(8, 782)
        Me.lblAdvBibleSep.Name = "lblAdvBibleSep"
        Me.lblAdvBibleSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvBibleSep.TabIndex = 82
        '
        ' chkShowBibleCopyright
        '
        Me.chkShowBibleCopyright.AutoSize = True
        Me.chkShowBibleCopyright.Location = New System.Drawing.Point(12, 790)
        Me.chkShowBibleCopyright.Name = "chkShowBibleCopyright"
        Me.chkShowBibleCopyright.Size = New System.Drawing.Size(201, 19)
        Me.chkShowBibleCopyright.TabIndex = 83
        Me.chkShowBibleCopyright.Text = "Show Bible copyright notices"
        Me.chkShowBibleCopyright.UseVisualStyleBackColor = True
        '
        ' FormOptions
        '
        Me.AcceptButton = Me.btnOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnCancel
        Me.ClientSize = New System.Drawing.Size(764, 521)
        Me.Controls.Add(Me.splitter)
        Me.Controls.Add(Me.btnOk)
        Me.Controls.Add(Me.btnCancel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(600, 400)
        Me.Name = "FormOptions"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Options"
        CType(Me.splitter, System.ComponentModel.ISupportInitialize).EndInit()
        Me.splitter.Panel1.ResumeLayout(False)
        Me.splitter.Panel2.ResumeLayout(False)
        Me.splitter.ResumeLayout(False)
        Me.pnlGeneral.ResumeLayout(False)
        Me.pnlGeneral.PerformLayout()
        Me.pnlPaths.ResumeLayout(False)
        Me.pnlPaths.PerformLayout()
        Me.pnlServer.ResumeLayout(False)
        Me.pnlServer.PerformLayout()
        Me.pnlTranslation.ResumeLayout(False)
        Me.pnlTranslation.PerformLayout()
        Me.pnlTts.ResumeLayout(False)
        Me.pnlTts.PerformLayout()
        Me.pnlHardware.ResumeLayout(False)
        Me.pnlHardware.PerformLayout()
        Me.pnlAdvanced.ResumeLayout(False)
        Me.pnlAdvanced.PerformLayout()
        CType(Me.nudParallelJobs, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudChunkSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudPollInterval, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudChunkTimeout, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudThreads, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudProcessors, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudBeamSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudBestOf, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTemperature, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTempInc, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudMaxContext, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudMaxSegLen, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudMaxTokens, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudAudioContext, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudWordThresh, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudEntropyThresh, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLogProbThresh, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudNoSpeechThresh, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudVadThresh, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLiveVadSilence, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLiveMaxSeg, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLiveInterim, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudPort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLivePort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTransPort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudFontSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudClauseGraceMs, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudClauseMaxMs, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudClauseMaxChars, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudClauseMinLockChars, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudClauseTimerMs, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudSttEouSilence, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTranslationConcurrency, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTtsConcurrency, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    ' ── Field declarations ─────────────────────────────────────────────

    ' Layout
    Friend WithEvents treeNav As System.Windows.Forms.TreeView
    Friend WithEvents pnlPages As System.Windows.Forms.Panel
    Friend WithEvents btnOk As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
    Friend WithEvents splitter As System.Windows.Forms.SplitContainer

    ' Category panels
    Friend WithEvents pnlGeneral As System.Windows.Forms.Panel
    Friend WithEvents pnlPaths As System.Windows.Forms.Panel
    Friend WithEvents pnlServer As System.Windows.Forms.Panel
    Friend WithEvents pnlTranslation As System.Windows.Forms.Panel
    Friend WithEvents pnlTts As System.Windows.Forms.Panel

    ' General panel
    Friend WithEvents lblAppearanceHeader As System.Windows.Forms.Label
    Friend WithEvents lblAppearanceSep As System.Windows.Forms.Label
    Friend WithEvents lblUiLang As System.Windows.Forms.Label
    Friend WithEvents cboUiLang As System.Windows.Forms.ComboBox
    Friend WithEvents lblTheme As System.Windows.Forms.Label
    Friend WithEvents cboTheme As System.Windows.Forms.ComboBox
    Friend WithEvents lblLogLevel As System.Windows.Forms.Label
    Friend WithEvents cboLogLevel As System.Windows.Forms.ComboBox
    Friend WithEvents lblStartupHeader As System.Windows.Forms.Label
    Friend WithEvents lblStartupSep As System.Windows.Forms.Label
    Friend WithEvents chkStartWindows As System.Windows.Forms.CheckBox
    Friend WithEvents chkStartMinimized As System.Windows.Forms.CheckBox
    Friend WithEvents chkMinimizeToTray As System.Windows.Forms.CheckBox
    Friend WithEvents chkResetFirstRun As System.Windows.Forms.CheckBox

    ' Paths panel — section headers
    Friend WithEvents lblToolPathsHeader As System.Windows.Forms.Label
    Friend WithEvents lblToolPathsSep As System.Windows.Forms.Label
    Friend WithEvents lblModelPathsHeader As System.Windows.Forms.Label
    Friend WithEvents lblModelPathsSep As System.Windows.Forms.Label
    Friend WithEvents lblDirectoriesHeader As System.Windows.Forms.Label
    Friend WithEvents lblDirectoriesSep As System.Windows.Forms.Label
    Friend WithEvents lblAdvancedHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvancedSep As System.Windows.Forms.Label

    ' Paths panel — path rows
    Friend WithEvents lblWhisperPath As System.Windows.Forms.Label
    Friend WithEvents txtWhisper As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseWhisper As System.Windows.Forms.Button
    Friend WithEvents lblYtdlpPath As System.Windows.Forms.Label
    Friend WithEvents txtYtdlp As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseYtdlp As System.Windows.Forms.Button
    Friend WithEvents lblFfmpegPath As System.Windows.Forms.Label
    Friend WithEvents txtFfmpeg As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseFfmpeg As System.Windows.Forms.Button
    Friend WithEvents lblFfprobePath As System.Windows.Forms.Label
    Friend WithEvents txtFfprobe As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseFfprobe As System.Windows.Forms.Button
    Friend WithEvents lblSubtitleEditPath As System.Windows.Forms.Label
    Friend WithEvents txtSubtitleEdit As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseSubtitleEdit As System.Windows.Forms.Button
    Friend WithEvents lblWhisperServerPath As System.Windows.Forms.Label
    Friend WithEvents txtWhisperServer As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseWhisperServer As System.Windows.Forms.Button
    Friend WithEvents lblGgmlModelPath As System.Windows.Forms.Label
    Friend WithEvents txtGgmlModel As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseGgmlModel As System.Windows.Forms.Button
    Friend WithEvents lblFwModelPath As System.Windows.Forms.Label
    Friend WithEvents txtFwModel As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseFwModel As System.Windows.Forms.Button
    Friend WithEvents lblTransModelPath As System.Windows.Forms.Label
    Friend WithEvents txtTransModel As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseTransModel As System.Windows.Forms.Button
    Friend WithEvents lblModelPath As System.Windows.Forms.Label
    Friend WithEvents txtModel As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseModel As System.Windows.Forms.Button
    Friend WithEvents lblModelAudioPath As System.Windows.Forms.Label
    Friend WithEvents txtModelAudio As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseModelAudio As System.Windows.Forms.Button
    Friend WithEvents lblOutputRootPath As System.Windows.Forms.Label
    Friend WithEvents txtOutputRoot As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseOutputRoot As System.Windows.Forms.Button
    Friend WithEvents lblGlossaryPath As System.Windows.Forms.Label
    Friend WithEvents txtGlossary As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseGlossary As System.Windows.Forms.Button
    Friend WithEvents lblBiblesPath As System.Windows.Forms.Label
    Friend WithEvents txtBibles As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseBibles As System.Windows.Forms.Button
    Friend WithEvents lblLogsPath As System.Windows.Forms.Label
    Friend WithEvents txtLogs As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseLogs As System.Windows.Forms.Button
    Friend WithEvents lblYtdlpFormat As System.Windows.Forms.Label
    Friend WithEvents txtYtdlpFormat As System.Windows.Forms.TextBox

    ' Server panel — section headers
    Friend WithEvents lblNetworkHeader As System.Windows.Forms.Label
    Friend WithEvents lblNetworkSep As System.Windows.Forms.Label
    Friend WithEvents lblSubtitleAppearanceHeader As System.Windows.Forms.Label
    Friend WithEvents lblSubtitleAppearanceSep As System.Windows.Forms.Label

    ' Server panel — network
    Friend WithEvents lblPort As System.Windows.Forms.Label
    Friend WithEvents nudPort As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLivePort As System.Windows.Forms.Label
    Friend WithEvents nudLivePort As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkFirewall As System.Windows.Forms.CheckBox
    Friend WithEvents lblPin As System.Windows.Forms.Label
    Friend WithEvents txtPin As System.Windows.Forms.TextBox

    ' Server panel — subtitle appearance
    Friend WithEvents lblBgColor As System.Windows.Forms.Label
    Friend WithEvents btnBgColor As System.Windows.Forms.Button
    Friend WithEvents lblFgColor As System.Windows.Forms.Label
    Friend WithEvents btnFgColor As System.Windows.Forms.Button
    Friend WithEvents lblFont As System.Windows.Forms.Label
    Friend WithEvents cboFont As System.Windows.Forms.ComboBox
    Friend WithEvents lblFontSize As System.Windows.Forms.Label
    Friend WithEvents nudFontSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkBold As System.Windows.Forms.CheckBox
    Friend WithEvents lblTemplatesHeader As System.Windows.Forms.Label
    Friend WithEvents lblTemplatesSep As System.Windows.Forms.Label
    Friend WithEvents btnManageTemplates As System.Windows.Forms.Button

    ' Translation panel
    Friend WithEvents lblTranslationHeader As System.Windows.Forms.Label
    Friend WithEvents lblTranslationSep As System.Windows.Forms.Label
    Friend WithEvents chkTransEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents chkUseSpeechmaticsTranslation As System.Windows.Forms.CheckBox
    Friend WithEvents lblClauseHeader As System.Windows.Forms.Label
    Friend WithEvents chkSpeechmaticsHoldClauses As System.Windows.Forms.CheckBox
    Friend WithEvents chkClauseLockOnPunctuation As System.Windows.Forms.CheckBox
    Friend WithEvents lblClauseGraceMs As System.Windows.Forms.Label
    Friend WithEvents nudClauseGraceMs As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblClauseMaxMs As System.Windows.Forms.Label
    Friend WithEvents nudClauseMaxMs As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblClauseMaxChars As System.Windows.Forms.Label
    Friend WithEvents nudClauseMaxChars As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblClauseMinLockChars As System.Windows.Forms.Label
    Friend WithEvents nudClauseMinLockChars As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblClauseTimerMs As System.Windows.Forms.Label
    Friend WithEvents nudClauseTimerMs As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblClauseSentenceEnders As System.Windows.Forms.Label
    Friend WithEvents txtClauseSentenceEnders As System.Windows.Forms.TextBox
    Friend WithEvents lblSttEouSilence As System.Windows.Forms.Label
    Friend WithEvents nudSttEouSilence As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTransBackend As System.Windows.Forms.Label
    Friend WithEvents cboTransBackend As System.Windows.Forms.ComboBox
    Friend WithEvents lblDevice As System.Windows.Forms.Label
    Friend WithEvents cboDevice As System.Windows.Forms.ComboBox
    Friend WithEvents lblTransPort As System.Windows.Forms.Label
    Friend WithEvents nudTransPort As System.Windows.Forms.NumericUpDown

    ' TTS panel
    Friend WithEvents lblTtsHeader As System.Windows.Forms.Label
    Friend WithEvents lblTtsSep As System.Windows.Forms.Label
    Friend WithEvents lblTtsPref1 As System.Windows.Forms.Label
    Friend WithEvents cboTtsPref1 As System.Windows.Forms.ComboBox
    Friend WithEvents lblTtsPref2 As System.Windows.Forms.Label
    Friend WithEvents cboTtsPref2 As System.Windows.Forms.ComboBox
    Friend WithEvents lblTtsPref3 As System.Windows.Forms.Label
    Friend WithEvents cboTtsPref3 As System.Windows.Forms.ComboBox
    Friend WithEvents lblTtsNote As System.Windows.Forms.Label

    ' Hardware panel
    Friend WithEvents pnlHardware As System.Windows.Forms.Panel
    Friend WithEvents lblHwHeader As System.Windows.Forms.Label
    Friend WithEvents lblHwSep As System.Windows.Forms.Label
    Friend WithEvents lblHwOverallCaption As System.Windows.Forms.Label
    Friend WithEvents lblHwOverallScore As System.Windows.Forms.Label
    Friend WithEvents pnlHwIndicator As System.Windows.Forms.Panel
    Friend WithEvents lblHwVerdict As System.Windows.Forms.Label
    Friend WithEvents lblHwBreakdownHeader As System.Windows.Forms.Label
    Friend WithEvents lblHwBreakdownSep As System.Windows.Forms.Label
    Friend WithEvents lblHwGpu As System.Windows.Forms.Label
    Friend WithEvents lblHwCpu As System.Windows.Forms.Label
    Friend WithEvents lblHwRam As System.Windows.Forms.Label
    Friend WithEvents lblHwDisk As System.Windows.Forms.Label
    Friend WithEvents lblHwOs As System.Windows.Forms.Label
    Friend WithEvents lblHwRecsHeader As System.Windows.Forms.Label
    Friend WithEvents lblHwRecsSep As System.Windows.Forms.Label
    Friend WithEvents txtHwRecs As System.Windows.Forms.TextBox
    Friend WithEvents btnHwRescan As System.Windows.Forms.Button
    Friend WithEvents lblSttEngineHeader As System.Windows.Forms.Label
    Friend WithEvents lblSttEngineSep As System.Windows.Forms.Label
    Friend WithEvents lblSttBackend As System.Windows.Forms.Label
    Friend WithEvents cboSttBackend As System.Windows.Forms.ComboBox
    Friend WithEvents lblSttApiKey As System.Windows.Forms.Label
    Friend WithEvents txtSttApiKey As System.Windows.Forms.TextBox
    Friend WithEvents lblSttOperatingPoint As System.Windows.Forms.Label
    Friend WithEvents cboSttOperatingPoint As System.Windows.Forms.ComboBox
    Friend WithEvents lblSttRegion As System.Windows.Forms.Label
    Friend WithEvents cboSttRegion As System.Windows.Forms.ComboBox

    ' Advanced panel
    Friend WithEvents pnlAdvanced As System.Windows.Forms.Panel
    Friend WithEvents pnlStt As System.Windows.Forms.Panel
    Friend WithEvents pnlDisplay As System.Windows.Forms.Panel
    Friend WithEvents btnManageSttTemplatesOpt As System.Windows.Forms.Button
    Friend WithEvents lblAdvPipelineHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvPipelineSep As System.Windows.Forms.Label
    Friend WithEvents nudParallelJobs As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblParallelJobs As System.Windows.Forms.Label
    Friend WithEvents nudChunkSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblChunkSize As System.Windows.Forms.Label
    Friend WithEvents nudPollInterval As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblPollInterval As System.Windows.Forms.Label
    Friend WithEvents nudChunkTimeout As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblChunkTimeout As System.Windows.Forms.Label
    Friend WithEvents chkKeepChunks As System.Windows.Forms.CheckBox
    Friend WithEvents chkKeepPreview As System.Windows.Forms.CheckBox
    Friend WithEvents lblAdvOutputHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvOutputSep As System.Windows.Forms.Label
    Friend WithEvents chkOutSrt As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutVtt As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutTxt As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutJson As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutCsv As System.Windows.Forms.CheckBox
    Friend WithEvents chkOutLrc As System.Windows.Forms.CheckBox
    Friend WithEvents lblAdvWhisperHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvWhisperSep As System.Windows.Forms.Label
    Friend WithEvents nudThreads As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblThreads As System.Windows.Forms.Label
    Friend WithEvents nudProcessors As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblProcessors As System.Windows.Forms.Label
    Friend WithEvents nudBeamSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblBeamSize As System.Windows.Forms.Label
    Friend WithEvents nudBestOf As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblBestOf As System.Windows.Forms.Label
    Friend WithEvents nudTemperature As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTemperature As System.Windows.Forms.Label
    Friend WithEvents nudTempInc As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTempInc As System.Windows.Forms.Label
    Friend WithEvents nudMaxContext As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblMaxContext As System.Windows.Forms.Label
    Friend WithEvents nudMaxSegLen As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblMaxSegLen As System.Windows.Forms.Label
    Friend WithEvents nudMaxTokens As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblMaxTokens As System.Windows.Forms.Label
    Friend WithEvents nudAudioContext As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblAudioContext As System.Windows.Forms.Label
    Friend WithEvents nudWordThresh As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblWordThresh As System.Windows.Forms.Label
    Friend WithEvents nudEntropyThresh As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblEntropyThresh As System.Windows.Forms.Label
    Friend WithEvents nudLogProbThresh As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLogProbThresh As System.Windows.Forms.Label
    Friend WithEvents nudNoSpeechThresh As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblNoSpeechThresh As System.Windows.Forms.Label
    Friend WithEvents nudVadThresh As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblVadThresh As System.Windows.Forms.Label
    Friend WithEvents txtInitialPrompt As System.Windows.Forms.TextBox
    Friend WithEvents lblInitialPrompt As System.Windows.Forms.Label
    Friend WithEvents lblAdvFlagsHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvFlagsSep As System.Windows.Forms.Label
    Friend WithEvents chkSplitOnWord As System.Windows.Forms.CheckBox
    Friend WithEvents chkNoGpu As System.Windows.Forms.CheckBox
    Friend WithEvents chkFlashAttn As System.Windows.Forms.CheckBox
    Friend WithEvents chkDiarize As System.Windows.Forms.CheckBox
    Friend WithEvents chkTinyDiarize As System.Windows.Forms.CheckBox
    Friend WithEvents chkTranslateEn As System.Windows.Forms.CheckBox
    Friend WithEvents chkNoTimestamps As System.Windows.Forms.CheckBox
    Friend WithEvents chkPrintProgress As System.Windows.Forms.CheckBox
    Friend WithEvents chkPrintColours As System.Windows.Forms.CheckBox
    Friend WithEvents lblAdvLiveHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvLiveSep As System.Windows.Forms.Label
    Friend WithEvents cboComputeType As System.Windows.Forms.ComboBox
    Friend WithEvents lblComputeType As System.Windows.Forms.Label
    Friend WithEvents nudLiveVadSilence As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLiveVadSilence As System.Windows.Forms.Label
    Friend WithEvents nudLiveMaxSeg As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLiveMaxSeg As System.Windows.Forms.Label
    Friend WithEvents nudLiveInterim As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLiveInterim As System.Windows.Forms.Label
    Friend WithEvents lblAdvBibleHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvBibleSep As System.Windows.Forms.Label
    Friend WithEvents chkShowBibleCopyright As System.Windows.Forms.CheckBox
    Friend WithEvents lblAdvLivePipelineHeader As System.Windows.Forms.Label
    Friend WithEvents lblAdvLivePipelineSep As System.Windows.Forms.Label
    Friend WithEvents lblTranslationConcurrency As System.Windows.Forms.Label
    Friend WithEvents nudTranslationConcurrency As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTtsConcurrency As System.Windows.Forms.Label
    Friend WithEvents nudTtsConcurrency As System.Windows.Forms.NumericUpDown

End Class
