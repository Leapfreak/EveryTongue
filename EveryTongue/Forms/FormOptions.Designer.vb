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
        Me.lblStartupSep = New System.Windows.Forms.Label()
        Me.lblStartupHeader = New System.Windows.Forms.Label()
        Me.cboTheme = New System.Windows.Forms.ComboBox()
        Me.lblTheme = New System.Windows.Forms.Label()
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
        Me.btnBrowseNllbModel = New System.Windows.Forms.Button()
        Me.txtNllbModel = New System.Windows.Forms.TextBox()
        Me.lblNllbModelPath = New System.Windows.Forms.Label()
        Me.btnBrowseFasterWhisper = New System.Windows.Forms.Button()
        Me.txtFasterWhisper = New System.Windows.Forms.TextBox()
        Me.lblFasterWhisperPath = New System.Windows.Forms.Label()
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
        Me.pnlServer = New System.Windows.Forms.Panel()
        Me.txtTts = New System.Windows.Forms.TextBox()
        Me.lblTts = New System.Windows.Forms.Label()
        Me.lblTtsSep = New System.Windows.Forms.Label()
        Me.lblTtsHeader = New System.Windows.Forms.Label()
        Me.nudUnload = New System.Windows.Forms.NumericUpDown()
        Me.lblUnload = New System.Windows.Forms.Label()
        Me.cboDevice = New System.Windows.Forms.ComboBox()
        Me.lblDevice = New System.Windows.Forms.Label()
        Me.chkTransEnabled = New System.Windows.Forms.CheckBox()
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
        CType(Me.splitter, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.splitter.Panel1.SuspendLayout()
        Me.splitter.Panel2.SuspendLayout()
        Me.splitter.SuspendLayout()
        Me.pnlGeneral.SuspendLayout()
        Me.pnlPaths.SuspendLayout()
        Me.pnlServer.SuspendLayout()
        CType(Me.nudPort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudLivePort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudTransPort, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudFontSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudUnload, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.treeNav.Nodes.AddRange(New System.Windows.Forms.TreeNode() {New System.Windows.Forms.TreeNode("General") With {.Name = "general"}, New System.Windows.Forms.TreeNode("Tool Paths") With {.Name = "paths"}, New System.Windows.Forms.TreeNode("Server & Subtitles") With {.Name = "server"}})
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
        Me.pnlPages.Controls.Add(Me.pnlServer)
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
        Me.pnlGeneral.Controls.Add(Me.chkStartWindows)
        Me.pnlGeneral.Controls.Add(Me.lblStartupSep)
        Me.pnlGeneral.Controls.Add(Me.lblStartupHeader)
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
        Me.pnlPaths.Controls.Add(Me.btnBrowseNllbModel)
        Me.pnlPaths.Controls.Add(Me.txtNllbModel)
        Me.pnlPaths.Controls.Add(Me.lblNllbModelPath)
        Me.pnlPaths.Controls.Add(Me.btnBrowseFasterWhisper)
        Me.pnlPaths.Controls.Add(Me.txtFasterWhisper)
        Me.pnlPaths.Controls.Add(Me.lblFasterWhisperPath)
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
        Me.lblWhisperPath.Text = "Whisper CLI:"
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
        ' lblFasterWhisperPath
        '
        Me.lblFasterWhisperPath.AutoSize = True
        Me.lblFasterWhisperPath.Location = New System.Drawing.Point(12, 332)
        Me.lblFasterWhisperPath.Name = "lblFasterWhisperPath"
        Me.lblFasterWhisperPath.Size = New System.Drawing.Size(126, 15)
        Me.lblFasterWhisperPath.TabIndex = 19
        Me.lblFasterWhisperPath.Text = "Faster Whisper model:"
        '
        ' txtFasterWhisper
        '
        Me.txtFasterWhisper.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtFasterWhisper.Location = New System.Drawing.Point(12, 350)
        Me.txtFasterWhisper.Name = "txtFasterWhisper"
        Me.txtFasterWhisper.Size = New System.Drawing.Size(510, 23)
        Me.txtFasterWhisper.TabIndex = 20
        '
        ' btnBrowseFasterWhisper
        '
        Me.btnBrowseFasterWhisper.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseFasterWhisper.Location = New System.Drawing.Point(528, 349)
        Me.btnBrowseFasterWhisper.Name = "btnBrowseFasterWhisper"
        Me.btnBrowseFasterWhisper.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseFasterWhisper.TabIndex = 21
        Me.btnBrowseFasterWhisper.Text = "..."
        Me.btnBrowseFasterWhisper.UseVisualStyleBackColor = True
        '
        ' lblNllbModelPath
        '
        Me.lblNllbModelPath.AutoSize = True
        Me.lblNllbModelPath.Location = New System.Drawing.Point(12, 384)
        Me.lblNllbModelPath.Name = "lblNllbModelPath"
        Me.lblNllbModelPath.Size = New System.Drawing.Size(77, 15)
        Me.lblNllbModelPath.TabIndex = 22
        Me.lblNllbModelPath.Text = "NLLB model:"
        '
        ' txtNllbModel
        '
        Me.txtNllbModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtNllbModel.Location = New System.Drawing.Point(12, 402)
        Me.txtNllbModel.Name = "txtNllbModel"
        Me.txtNllbModel.Size = New System.Drawing.Size(510, 23)
        Me.txtNllbModel.TabIndex = 23
        '
        ' btnBrowseNllbModel
        '
        Me.btnBrowseNllbModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseNllbModel.Location = New System.Drawing.Point(528, 401)
        Me.btnBrowseNllbModel.Name = "btnBrowseNllbModel"
        Me.btnBrowseNllbModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseNllbModel.TabIndex = 24
        Me.btnBrowseNllbModel.Text = "..."
        Me.btnBrowseNllbModel.UseVisualStyleBackColor = True
        '
        ' lblModelPath
        '
        Me.lblModelPath.AutoSize = True
        Me.lblModelPath.Location = New System.Drawing.Point(12, 436)
        Me.lblModelPath.Name = "lblModelPath"
        Me.lblModelPath.Size = New System.Drawing.Size(119, 15)
        Me.lblModelPath.TabIndex = 25
        Me.lblModelPath.Text = "Whisper model (job):"
        '
        ' txtModel
        '
        Me.txtModel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtModel.Location = New System.Drawing.Point(12, 454)
        Me.txtModel.Name = "txtModel"
        Me.txtModel.Size = New System.Drawing.Size(510, 23)
        Me.txtModel.TabIndex = 26
        '
        ' btnBrowseModel
        '
        Me.btnBrowseModel.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseModel.Location = New System.Drawing.Point(528, 453)
        Me.btnBrowseModel.Name = "btnBrowseModel"
        Me.btnBrowseModel.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseModel.TabIndex = 27
        Me.btnBrowseModel.Text = "..."
        Me.btnBrowseModel.UseVisualStyleBackColor = True
        '
        ' lblModelAudioPath
        '
        Me.lblModelAudioPath.AutoSize = True
        Me.lblModelAudioPath.Location = New System.Drawing.Point(12, 488)
        Me.lblModelAudioPath.Name = "lblModelAudioPath"
        Me.lblModelAudioPath.Size = New System.Drawing.Size(136, 15)
        Me.lblModelAudioPath.TabIndex = 28
        Me.lblModelAudioPath.Text = "Whisper model (audio):"
        '
        ' txtModelAudio
        '
        Me.txtModelAudio.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtModelAudio.Location = New System.Drawing.Point(12, 506)
        Me.txtModelAudio.Name = "txtModelAudio"
        Me.txtModelAudio.Size = New System.Drawing.Size(510, 23)
        Me.txtModelAudio.TabIndex = 29
        '
        ' btnBrowseModelAudio
        '
        Me.btnBrowseModelAudio.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseModelAudio.Location = New System.Drawing.Point(528, 505)
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
        Me.lblDirectoriesHeader.Location = New System.Drawing.Point(8, 540)
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
        Me.lblDirectoriesSep.Location = New System.Drawing.Point(8, 562)
        Me.lblDirectoriesSep.Name = "lblDirectoriesSep"
        Me.lblDirectoriesSep.Size = New System.Drawing.Size(520, 1)
        Me.lblDirectoriesSep.TabIndex = 32
        '
        ' lblOutputRootPath
        '
        Me.lblOutputRootPath.AutoSize = True
        Me.lblOutputRootPath.Location = New System.Drawing.Point(12, 570)
        Me.lblOutputRootPath.Name = "lblOutputRootPath"
        Me.lblOutputRootPath.Size = New System.Drawing.Size(73, 15)
        Me.lblOutputRootPath.TabIndex = 33
        Me.lblOutputRootPath.Text = "Output root:"
        '
        ' txtOutputRoot
        '
        Me.txtOutputRoot.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtOutputRoot.Location = New System.Drawing.Point(12, 588)
        Me.txtOutputRoot.Name = "txtOutputRoot"
        Me.txtOutputRoot.Size = New System.Drawing.Size(510, 23)
        Me.txtOutputRoot.TabIndex = 34
        '
        ' btnBrowseOutputRoot
        '
        Me.btnBrowseOutputRoot.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseOutputRoot.Location = New System.Drawing.Point(528, 587)
        Me.btnBrowseOutputRoot.Name = "btnBrowseOutputRoot"
        Me.btnBrowseOutputRoot.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseOutputRoot.TabIndex = 35
        Me.btnBrowseOutputRoot.Text = "..."
        Me.btnBrowseOutputRoot.UseVisualStyleBackColor = True
        '
        ' lblGlossaryPath
        '
        Me.lblGlossaryPath.AutoSize = True
        Me.lblGlossaryPath.Location = New System.Drawing.Point(12, 622)
        Me.lblGlossaryPath.Name = "lblGlossaryPath"
        Me.lblGlossaryPath.Size = New System.Drawing.Size(76, 15)
        Me.lblGlossaryPath.TabIndex = 36
        Me.lblGlossaryPath.Text = "Glossary file:"
        '
        ' txtGlossary
        '
        Me.txtGlossary.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtGlossary.Location = New System.Drawing.Point(12, 640)
        Me.txtGlossary.Name = "txtGlossary"
        Me.txtGlossary.Size = New System.Drawing.Size(510, 23)
        Me.txtGlossary.TabIndex = 37
        '
        ' btnBrowseGlossary
        '
        Me.btnBrowseGlossary.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseGlossary.Location = New System.Drawing.Point(528, 639)
        Me.btnBrowseGlossary.Name = "btnBrowseGlossary"
        Me.btnBrowseGlossary.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseGlossary.TabIndex = 38
        Me.btnBrowseGlossary.Text = "..."
        Me.btnBrowseGlossary.UseVisualStyleBackColor = True
        '
        ' lblBiblesPath
        '
        Me.lblBiblesPath.AutoSize = True
        Me.lblBiblesPath.Location = New System.Drawing.Point(12, 674)
        Me.lblBiblesPath.Name = "lblBiblesPath"
        Me.lblBiblesPath.Size = New System.Drawing.Size(95, 15)
        Me.lblBiblesPath.TabIndex = 39
        Me.lblBiblesPath.Text = "Bibles directory:"
        '
        ' txtBibles
        '
        Me.txtBibles.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtBibles.Location = New System.Drawing.Point(12, 692)
        Me.txtBibles.Name = "txtBibles"
        Me.txtBibles.Size = New System.Drawing.Size(510, 23)
        Me.txtBibles.TabIndex = 40
        '
        ' btnBrowseBibles
        '
        Me.btnBrowseBibles.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnBrowseBibles.Location = New System.Drawing.Point(528, 691)
        Me.btnBrowseBibles.Name = "btnBrowseBibles"
        Me.btnBrowseBibles.Size = New System.Drawing.Size(36, 25)
        Me.btnBrowseBibles.TabIndex = 41
        Me.btnBrowseBibles.Text = "..."
        Me.btnBrowseBibles.UseVisualStyleBackColor = True
        '
        ' lblAdvancedHeader
        '
        Me.lblAdvancedHeader.AutoSize = True
        Me.lblAdvancedHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblAdvancedHeader.Location = New System.Drawing.Point(8, 726)
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
        Me.lblAdvancedSep.Location = New System.Drawing.Point(8, 748)
        Me.lblAdvancedSep.Name = "lblAdvancedSep"
        Me.lblAdvancedSep.Size = New System.Drawing.Size(520, 1)
        Me.lblAdvancedSep.TabIndex = 43
        '
        ' lblYtdlpFormat
        '
        Me.lblYtdlpFormat.AutoSize = True
        Me.lblYtdlpFormat.Location = New System.Drawing.Point(12, 756)
        Me.lblYtdlpFormat.Name = "lblYtdlpFormat"
        Me.lblYtdlpFormat.Size = New System.Drawing.Size(81, 15)
        Me.lblYtdlpFormat.TabIndex = 44
        Me.lblYtdlpFormat.Text = "yt-dlp format:"
        '
        ' txtYtdlpFormat
        '
        Me.txtYtdlpFormat.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.txtYtdlpFormat.Location = New System.Drawing.Point(12, 774)
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
        Me.pnlServer.Controls.Add(Me.txtTts)
        Me.pnlServer.Controls.Add(Me.lblTts)
        Me.pnlServer.Controls.Add(Me.lblTtsSep)
        Me.pnlServer.Controls.Add(Me.lblTtsHeader)
        Me.pnlServer.Controls.Add(Me.nudUnload)
        Me.pnlServer.Controls.Add(Me.lblUnload)
        Me.pnlServer.Controls.Add(Me.cboDevice)
        Me.pnlServer.Controls.Add(Me.lblDevice)
        Me.pnlServer.Controls.Add(Me.chkTransEnabled)
        Me.pnlServer.Controls.Add(Me.lblTranslationSep)
        Me.pnlServer.Controls.Add(Me.lblTranslationHeader)
        Me.pnlServer.Controls.Add(Me.chkBold)
        Me.pnlServer.Controls.Add(Me.nudFontSize)
        Me.pnlServer.Controls.Add(Me.lblFontSize)
        Me.pnlServer.Controls.Add(Me.cboFont)
        Me.pnlServer.Controls.Add(Me.lblFont)
        Me.pnlServer.Controls.Add(Me.btnFgColor)
        Me.pnlServer.Controls.Add(Me.lblFgColor)
        Me.pnlServer.Controls.Add(Me.btnBgColor)
        Me.pnlServer.Controls.Add(Me.lblBgColor)
        Me.pnlServer.Controls.Add(Me.lblSubtitleAppearanceSep)
        Me.pnlServer.Controls.Add(Me.lblSubtitleAppearanceHeader)
        Me.pnlServer.Controls.Add(Me.txtPin)
        Me.pnlServer.Controls.Add(Me.lblPin)
        Me.pnlServer.Controls.Add(Me.chkFirewall)
        Me.pnlServer.Controls.Add(Me.nudTransPort)
        Me.pnlServer.Controls.Add(Me.lblTransPort)
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
        Me.lblTransPort.Location = New System.Drawing.Point(200, 94)
        Me.lblTransPort.Name = "lblTransPort"
        Me.lblTransPort.Size = New System.Drawing.Size(97, 15)
        Me.lblTransPort.TabIndex = 6
        Me.lblTransPort.Text = "Translation port:"
        '
        ' nudTransPort
        '
        Me.nudTransPort.Location = New System.Drawing.Point(200, 112)
        Me.nudTransPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.nudTransPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        Me.nudTransPort.Name = "nudTransPort"
        Me.nudTransPort.Size = New System.Drawing.Size(80, 23)
        Me.nudTransPort.TabIndex = 7
        Me.nudTransPort.Value = New Decimal(New Integer() {5090, 0, 0, 0})
        '
        ' chkFirewall
        '
        Me.chkFirewall.AutoSize = True
        Me.chkFirewall.Location = New System.Drawing.Point(400, 114)
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
        Me.lblSubtitleAppearanceHeader.Location = New System.Drawing.Point(8, 204)
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
        Me.lblSubtitleAppearanceSep.Location = New System.Drawing.Point(8, 226)
        Me.lblSubtitleAppearanceSep.Name = "lblSubtitleAppearanceSep"
        Me.lblSubtitleAppearanceSep.Size = New System.Drawing.Size(520, 1)
        Me.lblSubtitleAppearanceSep.TabIndex = 12
        '
        ' lblBgColor
        '
        Me.lblBgColor.AutoSize = True
        Me.lblBgColor.Location = New System.Drawing.Point(12, 234)
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
        Me.btnBgColor.Location = New System.Drawing.Point(12, 252)
        Me.btnBgColor.Name = "btnBgColor"
        Me.btnBgColor.Size = New System.Drawing.Size(80, 23)
        Me.btnBgColor.TabIndex = 14
        Me.btnBgColor.UseVisualStyleBackColor = False
        '
        ' lblFgColor
        '
        Me.lblFgColor.AutoSize = True
        Me.lblFgColor.Location = New System.Drawing.Point(110, 234)
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
        Me.btnFgColor.Location = New System.Drawing.Point(110, 252)
        Me.btnFgColor.Name = "btnFgColor"
        Me.btnFgColor.Size = New System.Drawing.Size(80, 23)
        Me.btnFgColor.TabIndex = 16
        Me.btnFgColor.UseVisualStyleBackColor = False
        '
        ' lblFont
        '
        Me.lblFont.AutoSize = True
        Me.lblFont.Location = New System.Drawing.Point(210, 234)
        Me.lblFont.Name = "lblFont"
        Me.lblFont.Size = New System.Drawing.Size(34, 15)
        Me.lblFont.TabIndex = 17
        Me.lblFont.Text = "Font:"
        '
        ' cboFont
        '
        Me.cboFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboFont.FormattingEnabled = True
        Me.cboFont.Location = New System.Drawing.Point(210, 252)
        Me.cboFont.Name = "cboFont"
        Me.cboFont.Size = New System.Drawing.Size(180, 23)
        Me.cboFont.TabIndex = 18
        '
        ' lblFontSize
        '
        Me.lblFontSize.AutoSize = True
        Me.lblFontSize.Location = New System.Drawing.Point(410, 234)
        Me.lblFontSize.Name = "lblFontSize"
        Me.lblFontSize.Size = New System.Drawing.Size(30, 15)
        Me.lblFontSize.TabIndex = 19
        Me.lblFontSize.Text = "Size:"
        '
        ' nudFontSize
        '
        Me.nudFontSize.Location = New System.Drawing.Point(410, 252)
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
        Me.chkBold.Location = New System.Drawing.Point(480, 254)
        Me.chkBold.Name = "chkBold"
        Me.chkBold.Size = New System.Drawing.Size(50, 19)
        Me.chkBold.TabIndex = 21
        Me.chkBold.Text = "Bold"
        Me.chkBold.UseVisualStyleBackColor = True
        '
        ' lblTranslationHeader
        '
        Me.lblTranslationHeader.AutoSize = True
        Me.lblTranslationHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblTranslationHeader.Location = New System.Drawing.Point(8, 292)
        Me.lblTranslationHeader.Name = "lblTranslationHeader"
        Me.lblTranslationHeader.Size = New System.Drawing.Size(96, 20)
        Me.lblTranslationHeader.TabIndex = 22
        Me.lblTranslationHeader.Text = "Translation"
        '
        ' lblTranslationSep
        '
        Me.lblTranslationSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTranslationSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblTranslationSep.Location = New System.Drawing.Point(8, 314)
        Me.lblTranslationSep.Name = "lblTranslationSep"
        Me.lblTranslationSep.Size = New System.Drawing.Size(520, 1)
        Me.lblTranslationSep.TabIndex = 23
        '
        ' chkTransEnabled
        '
        Me.chkTransEnabled.AutoSize = True
        Me.chkTransEnabled.Location = New System.Drawing.Point(12, 322)
        Me.chkTransEnabled.Name = "chkTransEnabled"
        Me.chkTransEnabled.Size = New System.Drawing.Size(137, 19)
        Me.chkTransEnabled.TabIndex = 24
        Me.chkTransEnabled.Text = "Translation enabled"
        Me.chkTransEnabled.UseVisualStyleBackColor = True
        '
        ' lblDevice
        '
        Me.lblDevice.AutoSize = True
        Me.lblDevice.Location = New System.Drawing.Point(12, 350)
        Me.lblDevice.Name = "lblDevice"
        Me.lblDevice.Size = New System.Drawing.Size(45, 15)
        Me.lblDevice.TabIndex = 25
        Me.lblDevice.Text = "Device:"
        '
        ' cboDevice
        '
        Me.cboDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboDevice.FormattingEnabled = True
        Me.cboDevice.Items.AddRange(New Object() {"cuda", "cpu"})
        Me.cboDevice.Location = New System.Drawing.Point(12, 368)
        Me.cboDevice.Name = "cboDevice"
        Me.cboDevice.Size = New System.Drawing.Size(90, 23)
        Me.cboDevice.TabIndex = 26
        '
        ' lblUnload
        '
        Me.lblUnload.AutoSize = True
        Me.lblUnload.Location = New System.Drawing.Point(120, 350)
        Me.lblUnload.Name = "lblUnload"
        Me.lblUnload.Size = New System.Drawing.Size(81, 15)
        Me.lblUnload.TabIndex = 27
        Me.lblUnload.Text = "Unload (min):"
        '
        ' nudUnload
        '
        Me.nudUnload.Location = New System.Drawing.Point(120, 368)
        Me.nudUnload.Maximum = New Decimal(New Integer() {1440, 0, 0, 0})
        Me.nudUnload.Name = "nudUnload"
        Me.nudUnload.Size = New System.Drawing.Size(60, 23)
        Me.nudUnload.TabIndex = 28
        Me.nudUnload.Value = New Decimal(New Integer() {10, 0, 0, 0})
        '
        ' lblTtsHeader
        '
        Me.lblTtsHeader.AutoSize = True
        Me.lblTtsHeader.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.lblTtsHeader.Location = New System.Drawing.Point(8, 408)
        Me.lblTtsHeader.Name = "lblTtsHeader"
        Me.lblTtsHeader.Size = New System.Drawing.Size(122, 20)
        Me.lblTtsHeader.TabIndex = 29
        Me.lblTtsHeader.Text = "Text-to-Speech"
        '
        ' lblTtsSep
        '
        Me.lblTtsSep.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lblTtsSep.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.lblTtsSep.Location = New System.Drawing.Point(8, 430)
        Me.lblTtsSep.Name = "lblTtsSep"
        Me.lblTtsSep.Size = New System.Drawing.Size(520, 1)
        Me.lblTtsSep.TabIndex = 30
        '
        ' lblTts
        '
        Me.lblTts.AutoSize = True
        Me.lblTts.Location = New System.Drawing.Point(12, 438)
        Me.lblTts.Name = "lblTts"
        Me.lblTts.Size = New System.Drawing.Size(267, 15)
        Me.lblTts.TabIndex = 31
        Me.lblTts.Text = "TTS backends (comma-separated, empty = all):"
        '
        ' txtTts
        '
        Me.txtTts.Location = New System.Drawing.Point(12, 456)
        Me.txtTts.Name = "txtTts"
        Me.txtTts.Size = New System.Drawing.Size(300, 23)
        Me.txtTts.TabIndex = 32
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
        CType(Me.nudPort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudLivePort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudTransPort, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudFontSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudUnload, System.ComponentModel.ISupportInitialize).EndInit()
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

    ' General panel
    Friend WithEvents lblAppearanceHeader As System.Windows.Forms.Label
    Friend WithEvents lblAppearanceSep As System.Windows.Forms.Label
    Friend WithEvents lblUiLang As System.Windows.Forms.Label
    Friend WithEvents cboUiLang As System.Windows.Forms.ComboBox
    Friend WithEvents lblTheme As System.Windows.Forms.Label
    Friend WithEvents cboTheme As System.Windows.Forms.ComboBox
    Friend WithEvents lblStartupHeader As System.Windows.Forms.Label
    Friend WithEvents lblStartupSep As System.Windows.Forms.Label
    Friend WithEvents chkStartWindows As System.Windows.Forms.CheckBox

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
    Friend WithEvents lblFasterWhisperPath As System.Windows.Forms.Label
    Friend WithEvents txtFasterWhisper As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseFasterWhisper As System.Windows.Forms.Button
    Friend WithEvents lblNllbModelPath As System.Windows.Forms.Label
    Friend WithEvents txtNllbModel As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseNllbModel As System.Windows.Forms.Button
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
    Friend WithEvents lblYtdlpFormat As System.Windows.Forms.Label
    Friend WithEvents txtYtdlpFormat As System.Windows.Forms.TextBox

    ' Server panel — section headers
    Friend WithEvents lblNetworkHeader As System.Windows.Forms.Label
    Friend WithEvents lblNetworkSep As System.Windows.Forms.Label
    Friend WithEvents lblSubtitleAppearanceHeader As System.Windows.Forms.Label
    Friend WithEvents lblSubtitleAppearanceSep As System.Windows.Forms.Label
    Friend WithEvents lblTranslationHeader As System.Windows.Forms.Label
    Friend WithEvents lblTranslationSep As System.Windows.Forms.Label
    Friend WithEvents lblTtsHeader As System.Windows.Forms.Label
    Friend WithEvents lblTtsSep As System.Windows.Forms.Label

    ' Server panel — network
    Friend WithEvents lblPort As System.Windows.Forms.Label
    Friend WithEvents nudPort As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblLivePort As System.Windows.Forms.Label
    Friend WithEvents nudLivePort As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblTransPort As System.Windows.Forms.Label
    Friend WithEvents nudTransPort As System.Windows.Forms.NumericUpDown
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

    ' Server panel — translation
    Friend WithEvents chkTransEnabled As System.Windows.Forms.CheckBox
    Friend WithEvents lblDevice As System.Windows.Forms.Label
    Friend WithEvents cboDevice As System.Windows.Forms.ComboBox
    Friend WithEvents lblUnload As System.Windows.Forms.Label
    Friend WithEvents nudUnload As System.Windows.Forms.NumericUpDown

    ' Server panel — TTS
    Friend WithEvents lblTts As System.Windows.Forms.Label
    Friend WithEvents txtTts As System.Windows.Forms.TextBox

End Class
