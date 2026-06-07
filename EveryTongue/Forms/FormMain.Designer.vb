<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMain
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        tipMain = New ToolTip(components)
        menuMain = New MenuStrip()
        mnuFile = New ToolStripMenuItem()
        mnuFileNewSession = New ToolStripMenuItem()
        mnuFileSep1 = New ToolStripSeparator()
        mnuFileExportDiag = New ToolStripMenuItem()
        mnuFileSep2 = New ToolStripSeparator()
        mnuFileExit = New ToolStripMenuItem()
        mnuTools = New ToolStripMenuItem()
        mnuToolsTranscribe = New ToolStripMenuItem()
        mnuToolsTranslate = New ToolStripMenuItem()
        mnuToolsBible = New ToolStripMenuItem()
        mnuToolsSep1 = New ToolStripSeparator()
        mnuToolsGlossary = New ToolStripMenuItem()
        mnuToolsLocalization = New ToolStripMenuItem()
        mnuToolsSep2 = New ToolStripSeparator()
        mnuToolsDownloadMgr = New ToolStripMenuItem()
        mnuToolsVerifyPaths = New ToolStripMenuItem()
        mnuToolsVerifyIntegrity = New ToolStripMenuItem()
        mnuToolsBenchmark = New ToolStripMenuItem()
        mnuToolsSep3 = New ToolStripSeparator()
        mnuToolsOptions = New ToolStripMenuItem()
        mnuSession = New ToolStripMenuItem()
        mnuSessionQR = New ToolStripMenuItem()
        mnuSessionCopyUrl = New ToolStripMenuItem()
        mnuView = New ToolStripMenuItem()
        mnuViewLogPanel = New ToolStripMenuItem()
        mnuViewSep1 = New ToolStripSeparator()
        mnuViewTheme = New ToolStripMenuItem()
        mnuViewThemeSystem = New ToolStripMenuItem()
        mnuViewThemeLight = New ToolStripMenuItem()
        mnuViewThemeDark = New ToolStripMenuItem()
        mnuViewSep2 = New ToolStripSeparator()
        mnuViewClients = New ToolStripMenuItem()
        mnuViewSep3 = New ToolStripSeparator()
        mnuViewFullScreen = New ToolStripMenuItem()
        mnuHelpMenu = New ToolStripMenuItem()
        mnuHelpQuickStart = New ToolStripMenuItem()
        mnuHelpShortcuts = New ToolStripMenuItem()
        mnuHelpSep1 = New ToolStripSeparator()
        mnuHelpHardware = New ToolStripMenuItem()
        mnuHelpSpecSheet = New ToolStripMenuItem()
        mnuHelpSep2 = New ToolStripSeparator()
        mnuHelpUpdates = New ToolStripMenuItem()
        mnuHelpAbout = New ToolStripMenuItem()
        tsNavBar = New ToolStrip()
        btnNavLog = New ToolStripButton()
        btnNavTranscribe = New ToolStripButton()
        btnNavTranslate = New ToolStripButton()
        btnNavBible = New ToolStripButton()
        statusMain = New StatusStrip()
        tslServerStatus = New ToolStripStatusLabel()
        tslClients = New ToolStripStatusLabel()
        tslSpring = New ToolStripStatusLabel()
        tslLogToggle = New ToolStripStatusLabel()
        pnlContent = New Panel()
        tabMain = New TabControl()

        tabPageJob = New TabPage()
        lblMode = New Label()
        cboMode = New ComboBox()
        grpInput = New GroupBox()
        lblUrl = New Label()
        txtUrl = New TextBox()
        btnBrowseFile = New Button()
        lblInputLanguage = New Label()
        cboInputLanguage = New ComboBox()
        lblOutputLanguage = New Label()
        cboOutputLanguage = New ComboBox()
        lblModel = New Label()
        cboModel = New ComboBox()
        lblStartTime = New Label()
        txtStartHH = New TextBox()
        lblStartColon1 = New Label()
        txtStartMM = New TextBox()
        lblStartColon2 = New Label()
        txtStartSS = New TextBox()
        lblEndTime = New Label()
        txtEndHH = New TextBox()
        lblEndColon1 = New Label()
        txtEndMM = New TextBox()
        lblEndColon2 = New Label()
        txtEndSS = New TextBox()
        lblOutputDir = New Label()
        txtOutputDir = New TextBox()
        btnBrowseOutput = New Button()
        grpOutputFormats = New GroupBox()
        chkSrt = New CheckBox()
        chkVtt = New CheckBox()
        chkTxt = New CheckBox()
        chkJson = New CheckBox()
        chkCsv = New CheckBox()
        chkLrc = New CheckBox()
        grpProgress = New GroupBox()
        lblStepStatus = New Label()
        pbOverall = New ProgressBar()
        pbChunk = New ProgressBar()
        btnStart = New Button()
        btnResume = New Button()
        btnCancel = New Button()
        btnOpenOutput = New Button()
        btnOpenSubtitleEdit = New Button()
        lnkPreviewSrt = New LinkLabel()

        tabPageHelp = New TabPage()
        rtbHelp = New RichTextBox()
        tabPageTranslate = New TabPage()
        splitTrans = New SplitContainer()
        txtTransInput = New TextBox()
        txtTransOutput = New TextBox()

        pnlTransInButtons = New Panel()
        btnTransCopy = New Button()
        btnTransClear = New Button()
        pnlTransOutButtons = New Panel()
        btnTransOutCopy = New Button()
        btnTransOutClear = New Button()
        pnlTransBottom = New Panel()
        lblTransStatus = New Label()
        pnlTransTop = New Panel()
        lblTransFrom = New Label()
        cboTransSource = New ComboBox()
        btnTransSwap = New Button()
        lblTransTo = New Label()
        cboTransTarget = New ComboBox()
        btnTranslate = New Button()
        tabPageBibleWs = New TabPage()
        pnlBibleTop = New Panel()
        lblBibleLang = New Label()
        cboBibleLang = New ComboBox()
        lblBibleTrans = New Label()
        cboBibleTrans = New ComboBox()
        txtBibleRef = New ComboBox()
        btnBibleGo = New Button()
        splitBible = New SplitContainer()
        pnlBibleNavHeader = New Panel()
        btnBibleBack = New Button()
        lblBibleNavTitle = New Label()
        flpBibleNav = New FlowLayoutPanel()
        rtbBibleText = New RichTextBox()
        lblBibleCopyright = New Label()
        splitterLog = New Splitter()
        pnlLogPanel = New Panel()
        rtbUnifiedLog = New RichTextBox()
        txtLogSearch = New TextBox()
        btnLogSearchNext = New Button()
        pnlLogToolbar = New Panel()
        lblLogTitle = New Label()
        cboLogFilter = New ComboBox()
        btnLogClear = New Button()
        btnLogCopy = New Button()
        ctxBible = New ContextMenuStrip(components)
        ctxBibleCopySelection = New ToolStripMenuItem()
        ctxBibleCopyVerse = New ToolStripMenuItem()
        ctxBibleCopyChapter = New ToolStripMenuItem()
        ctxTransInput = New ContextMenuStrip(components)
        ctxTransInputCut = New ToolStripMenuItem()
        ctxTransInputCopy = New ToolStripMenuItem()
        ctxTransInputPaste = New ToolStripMenuItem()
        ctxTransInputSelectAll = New ToolStripMenuItem()
        ctxTransOutput = New ContextMenuStrip(components)
        ctxTransOutputCopy = New ToolStripMenuItem()
        ctxTransOutputSelectAll = New ToolStripMenuItem()
        trayMenu = New ContextMenuStrip(components)
        trayMenuAbout = New ToolStripMenuItem()
        traySep0 = New ToolStripSeparator()
        trayMenuQR = New ToolStripMenuItem()
        trayMenuBrowser = New ToolStripMenuItem()
        traySep1 = New ToolStripSeparator()
        trayMenuShow = New ToolStripMenuItem()
        traySep2 = New ToolStripSeparator()
        trayMenuExit = New ToolStripMenuItem()
        trayIcon = New NotifyIcon(components)
        menuMain.SuspendLayout()
        tsNavBar.SuspendLayout()
        statusMain.SuspendLayout()
        pnlContent.SuspendLayout()
        tabMain.SuspendLayout()
        tabPageJob.SuspendLayout()
        grpInput.SuspendLayout()
        grpOutputFormats.SuspendLayout()
        grpProgress.SuspendLayout()

        tabPageHelp.SuspendLayout()
        tabPageTranslate.SuspendLayout()
        CType(splitTrans, ComponentModel.ISupportInitialize).BeginInit()
        splitTrans.Panel1.SuspendLayout()
        splitTrans.Panel2.SuspendLayout()
        splitTrans.SuspendLayout()

        pnlTransInButtons.SuspendLayout()
        pnlTransOutButtons.SuspendLayout()
        pnlTransBottom.SuspendLayout()
        pnlTransTop.SuspendLayout()
        tabPageBibleWs.SuspendLayout()
        pnlBibleTop.SuspendLayout()
        CType(splitBible, ComponentModel.ISupportInitialize).BeginInit()
        splitBible.Panel1.SuspendLayout()
        splitBible.Panel2.SuspendLayout()
        splitBible.SuspendLayout()
        pnlBibleNavHeader.SuspendLayout()
        pnlLogPanel.SuspendLayout()
        pnlLogToolbar.SuspendLayout()
        trayMenu.SuspendLayout()
        SuspendLayout()
        ' 
        ' menuMain
        ' 
        menuMain.Items.AddRange(New ToolStripItem() {mnuFile, mnuTools, mnuSession, mnuView, mnuHelpMenu})
        menuMain.Location = New Point(0, 0)
        menuMain.Name = "menuMain"
        menuMain.RenderMode = ToolStripRenderMode.Professional
        menuMain.Size = New Size(880, 24)
        menuMain.TabIndex = 3
        ' 
        ' mnuFile
        ' 
        mnuFile.DropDownItems.AddRange(New ToolStripItem() {mnuFileNewSession, mnuFileSep1, mnuFileExportDiag, mnuFileSep2, mnuFileExit})
        mnuFile.Name = "mnuFile"
        mnuFile.Size = New Size(37, 20)
        mnuFile.Text = "&File"
        ' 
        ' mnuFileNewSession
        ' 
        mnuFileNewSession.Name = "mnuFileNewSession"
        mnuFileNewSession.ShortcutKeys = Keys.Control Or Keys.N
        mnuFileNewSession.Size = New Size(192, 22)
        mnuFileNewSession.Text = "New Session..."
        ' 
        ' mnuFileSep1
        ' 
        mnuFileSep1.Name = "mnuFileSep1"
        mnuFileSep1.Size = New Size(189, 6)
        ' 
        ' mnuFileExportDiag
        ' 
        mnuFileExportDiag.Name = "mnuFileExportDiag"
        mnuFileExportDiag.Size = New Size(192, 22)
        mnuFileExportDiag.Text = "Export Diagnostics..."
        ' 
        ' mnuFileSep2
        ' 
        mnuFileSep2.Name = "mnuFileSep2"
        mnuFileSep2.Size = New Size(189, 6)
        ' 
        ' mnuFileExit
        ' 
        mnuFileExit.Name = "mnuFileExit"
        mnuFileExit.ShortcutKeys = Keys.Alt Or Keys.F4
        mnuFileExit.Size = New Size(192, 22)
        mnuFileExit.Text = "E&xit"
        ' 
        ' mnuTools
        ' 
        mnuTools.DropDownItems.AddRange(New ToolStripItem() {mnuToolsTranscribe, mnuToolsTranslate, mnuToolsBible, mnuToolsSep1, mnuToolsGlossary, mnuToolsLocalization, mnuToolsSep2, mnuToolsDownloadMgr, mnuToolsVerifyPaths, mnuToolsVerifyIntegrity, mnuToolsBenchmark, mnuToolsSep3, mnuToolsOptions})
        mnuTools.Name = "mnuTools"
        mnuTools.Size = New Size(47, 20)
        mnuTools.Text = "&Tools"
        ' 
        ' mnuToolsTranscribe
        ' 
        mnuToolsTranscribe.Name = "mnuToolsTranscribe"
        mnuToolsTranscribe.Size = New Size(184, 22)
        mnuToolsTranscribe.Text = "Transcribe File/URL..."
        ' 
        ' mnuToolsTranslate
        ' 
        mnuToolsTranslate.Name = "mnuToolsTranslate"
        mnuToolsTranslate.Size = New Size(184, 22)
        mnuToolsTranslate.Text = "Translate Text..."
        ' 
        ' mnuToolsBible
        ' 
        mnuToolsBible.Name = "mnuToolsBible"
        mnuToolsBible.Size = New Size(184, 22)
        mnuToolsBible.Text = "Bible Lookup..."
        ' 
        ' mnuToolsSep1
        ' 
        mnuToolsSep1.Name = "mnuToolsSep1"
        mnuToolsSep1.Size = New Size(181, 6)
        ' 
        ' mnuToolsGlossary
        ' 
        mnuToolsGlossary.Name = "mnuToolsGlossary"
        mnuToolsGlossary.Size = New Size(184, 22)
        mnuToolsGlossary.Text = "Filter Editor..."
        ' 
        ' mnuToolsLocalization
        ' 
        mnuToolsLocalization.Enabled = False
        mnuToolsLocalization.Name = "mnuToolsLocalization"
        mnuToolsLocalization.Size = New Size(184, 22)
        mnuToolsLocalization.Text = "Localization Editor..."
        ' 
        ' mnuToolsSep2
        ' 
        mnuToolsSep2.Name = "mnuToolsSep2"
        mnuToolsSep2.Size = New Size(181, 6)
        ' 
        ' mnuToolsDownloadMgr
        ' 
        mnuToolsDownloadMgr.Name = "mnuToolsDownloadMgr"
        mnuToolsDownloadMgr.Size = New Size(184, 22)
        mnuToolsDownloadMgr.Text = "Download Manager"
        ' 
        ' 
        ' mnuToolsVerifyPaths
        ' 
        mnuToolsVerifyPaths.Name = "mnuToolsVerifyPaths"
        mnuToolsVerifyPaths.Size = New Size(184, 22)
        mnuToolsVerifyPaths.Text = "Verify Paths"
        ' 
        ' mnuToolsVerifyIntegrity
        ' 
        mnuToolsVerifyIntegrity.Name = "mnuToolsVerifyIntegrity"
        mnuToolsVerifyIntegrity.Size = New Size(184, 22)
        mnuToolsVerifyIntegrity.Text = "Verify File Integrity"
        '
        ' mnuToolsBenchmark
        '
        mnuToolsBenchmark.Name = "mnuToolsBenchmark"
        mnuToolsBenchmark.Size = New Size(184, 22)
        mnuToolsBenchmark.Text = "Translation Benchmark..."
        '
        ' mnuToolsSep3
        ' 
        mnuToolsSep3.Name = "mnuToolsSep3"
        mnuToolsSep3.Size = New Size(181, 6)
        ' 
        ' 
        ' mnuToolsOptions
        ' 
        mnuToolsOptions.Name = "mnuToolsOptions"
        mnuToolsOptions.ShortcutKeys = Keys.F12
        mnuToolsOptions.ShortcutKeyDisplayString = "F12"
        mnuToolsOptions.Size = New Size(184, 22)
        mnuToolsOptions.Text = "&Options..."
        ' 
        ' mnuSession
        ' 
        mnuSession.DropDownItems.AddRange(New ToolStripItem() {mnuSessionQR, mnuSessionCopyUrl})
        mnuSession.Name = "mnuSession"
        mnuSession.Size = New Size(58, 20)
        mnuSession.Text = "&Server"
        '
        ' mnuSessionQR
        ' 
        mnuSessionQR.Name = "mnuSessionQR"
        mnuSessionQR.Size = New Size(173, 22)
        mnuSessionQR.Text = "Show QR Code"
        ' 
        ' mnuSessionCopyUrl
        ' 
        mnuSessionCopyUrl.Name = "mnuSessionCopyUrl"
        mnuSessionCopyUrl.Size = New Size(173, 22)
        mnuSessionCopyUrl.Text = "Copy Phone URL"
        ' 
        ' mnuView
        ' 
        mnuView.DropDownItems.AddRange(New ToolStripItem() {mnuViewLogPanel, mnuViewSep1, mnuViewTheme, mnuViewSep2, mnuViewClients, mnuViewSep3, mnuViewFullScreen})
        mnuView.Name = "mnuView"
        mnuView.Size = New Size(44, 20)
        mnuView.Text = "&View"
        ' 
        ' mnuViewLogPanel
        ' 
        mnuViewLogPanel.Name = "mnuViewLogPanel"
        mnuViewLogPanel.ShortcutKeys = Keys.Control Or Keys.L
        mnuViewLogPanel.Size = New Size(198, 22)
        mnuViewLogPanel.Text = "Show Log Panel"
        ' 
        ' mnuViewSep1
        ' 
        mnuViewSep1.Name = "mnuViewSep1"
        mnuViewSep1.Size = New Size(195, 6)
        ' 
        ' mnuViewTheme
        ' 
        mnuViewTheme.DropDownItems.AddRange(New ToolStripItem() {mnuViewThemeSystem, mnuViewThemeLight, mnuViewThemeDark})
        mnuViewTheme.Name = "mnuViewTheme"
        mnuViewTheme.Size = New Size(198, 22)
        mnuViewTheme.Text = "Theme"
        ' 
        ' mnuViewThemeSystem
        ' 
        mnuViewThemeSystem.Name = "mnuViewThemeSystem"
        mnuViewThemeSystem.Size = New Size(112, 22)
        mnuViewThemeSystem.Text = "System"
        ' 
        ' mnuViewThemeLight
        ' 
        mnuViewThemeLight.Name = "mnuViewThemeLight"
        mnuViewThemeLight.Size = New Size(112, 22)
        mnuViewThemeLight.Text = "Light"
        ' 
        ' mnuViewThemeDark
        ' 
        mnuViewThemeDark.Name = "mnuViewThemeDark"
        mnuViewThemeDark.Size = New Size(112, 22)
        mnuViewThemeDark.Text = "Dark"
        ' 
        ' mnuViewSep2
        ' 
        mnuViewSep2.Name = "mnuViewSep2"
        mnuViewSep2.Size = New Size(195, 6)
        '
        ' mnuViewClients
        '
        mnuViewClients.Name = "mnuViewClients"
        mnuViewClients.Size = New Size(198, 22)
        mnuViewClients.Text = "Connected Clients..."
        '
        ' mnuViewSep3
        '
        mnuViewSep3.Name = "mnuViewSep3"
        mnuViewSep3.Size = New Size(195, 6)
        '
        ' mnuViewFullScreen
        ' 
        mnuViewFullScreen.Name = "mnuViewFullScreen"
        mnuViewFullScreen.ShortcutKeys = Keys.F11
        mnuViewFullScreen.Size = New Size(198, 22)
        mnuViewFullScreen.Text = "Full Screen"
        ' 
        ' mnuHelpMenu
        ' 
        mnuHelpMenu.DropDownItems.AddRange(New ToolStripItem() {mnuHelpQuickStart, mnuHelpShortcuts, mnuHelpSep1, mnuHelpHardware, mnuHelpSpecSheet, mnuHelpSep2, mnuHelpUpdates, mnuHelpAbout})
        mnuHelpMenu.Name = "mnuHelpMenu"
        mnuHelpMenu.Size = New Size(44, 20)
        mnuHelpMenu.Text = "&Help"
        ' 
        ' mnuHelpQuickStart
        ' 
        mnuHelpQuickStart.Name = "mnuHelpQuickStart"
        mnuHelpQuickStart.ShortcutKeys = Keys.F1
        mnuHelpQuickStart.Size = New Size(190, 22)
        mnuHelpQuickStart.Text = "Quick Start Guide"
        ' 
        ' mnuHelpShortcuts
        ' 
        mnuHelpShortcuts.Name = "mnuHelpShortcuts"
        mnuHelpShortcuts.Size = New Size(190, 22)
        mnuHelpShortcuts.Text = "Keyboard Shortcuts"
        ' 
        ' mnuHelpSep1
        ' 
        mnuHelpSep1.Name = "mnuHelpSep1"
        mnuHelpSep1.Size = New Size(187, 6)
        ' 
        ' mnuHelpHardware
        ' 
        mnuHelpHardware.Name = "mnuHelpHardware"
        mnuHelpHardware.Size = New Size(190, 22)
        mnuHelpHardware.Text = "Hardware Report"
        ' 
        ' mnuHelpSpecSheet
        ' 
        mnuHelpSpecSheet.Enabled = False
        mnuHelpSpecSheet.Name = "mnuHelpSpecSheet"
        mnuHelpSpecSheet.Size = New Size(190, 22)
        mnuHelpSpecSheet.Text = "Generate Spec Sheet..."
        ' 
        ' mnuHelpSep2
        ' 
        mnuHelpSep2.Name = "mnuHelpSep2"
        mnuHelpSep2.Size = New Size(187, 6)
        ' 
        ' mnuHelpUpdates
        ' 
        mnuHelpUpdates.Name = "mnuHelpUpdates"
        mnuHelpUpdates.Size = New Size(190, 22)
        mnuHelpUpdates.Text = "Check for Updates"
        ' 
        ' mnuHelpAbout
        ' 
        mnuHelpAbout.Name = "mnuHelpAbout"
        mnuHelpAbout.Size = New Size(190, 22)
        mnuHelpAbout.Text = "About Every Tongue"
        ' 
        ' tsNavBar
        '
        tsNavBar.BackColor = Color.FromArgb(CByte(240), CByte(240), CByte(240))
        tsNavBar.Dock = DockStyle.Top
        tsNavBar.GripStyle = ToolStripGripStyle.Hidden
        tsNavBar.ImageScalingSize = New Size(28, 28)
        tsNavBar.Items.AddRange(New ToolStripItem() {btnNavLog, btnNavTranscribe, btnNavTranslate, btnNavBible})
        tsNavBar.Location = New Point(0, 0)
        tsNavBar.Name = "tsNavBar"
        tsNavBar.Padding = New Padding(4, 2, 4, 2)
        tsNavBar.RenderMode = ToolStripRenderMode.ManagerRenderMode
        tsNavBar.Size = New Size(880, 40)
        tsNavBar.TabIndex = 1
        '
        ' btnNavLog
        '
        btnNavLog.AutoSize = True
        btnNavLog.ForeColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
        btnNavLog.ImageScaling = ToolStripItemImageScaling.None
        btnNavLog.Margin = New Padding(0, 0, 2, 0)
        btnNavLog.Name = "btnNavLog"
        btnNavLog.Padding = New Padding(8, 2, 8, 2)
        btnNavLog.Text = "Log"
        btnNavLog.TextImageRelation = TextImageRelation.ImageBeforeText
        '
        ' btnNavTranscribe
        '
        btnNavTranscribe.AutoSize = True
        btnNavTranscribe.ForeColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
        btnNavTranscribe.ImageScaling = ToolStripItemImageScaling.None
        btnNavTranscribe.Margin = New Padding(0, 0, 2, 0)
        btnNavTranscribe.Name = "btnNavTranscribe"
        btnNavTranscribe.Padding = New Padding(8, 2, 8, 2)
        btnNavTranscribe.Text = "Transcribe"
        btnNavTranscribe.TextImageRelation = TextImageRelation.ImageBeforeText
        '
        ' btnNavTranslate
        '
        btnNavTranslate.AutoSize = True
        btnNavTranslate.ForeColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
        btnNavTranslate.ImageScaling = ToolStripItemImageScaling.None
        btnNavTranslate.Margin = New Padding(0, 0, 2, 0)
        btnNavTranslate.Name = "btnNavTranslate"
        btnNavTranslate.Padding = New Padding(8, 2, 8, 2)
        btnNavTranslate.Text = "Translate"
        btnNavTranslate.TextImageRelation = TextImageRelation.ImageBeforeText
        '
        ' btnNavBible
        '
        btnNavBible.AutoSize = True
        btnNavBible.ForeColor = Color.FromArgb(CByte(60), CByte(60), CByte(60))
        btnNavBible.ImageScaling = ToolStripItemImageScaling.None
        btnNavBible.Margin = New Padding(0, 0, 2, 0)
        btnNavBible.Name = "btnNavBible"
        btnNavBible.Padding = New Padding(8, 2, 8, 2)
        btnNavBible.Text = "Bible"
        btnNavBible.TextImageRelation = TextImageRelation.ImageBeforeText
        ' 
        ' statusMain
        ' 
        statusMain.Items.AddRange(New ToolStripItem() {tslServerStatus, tslClients, tslSpring, tslLogToggle})
        statusMain.Location = New Point(0, 626)
        statusMain.Name = "statusMain"
        statusMain.Size = New Size(880, 24)
        statusMain.TabIndex = 2
        ' 
        ' tslServerStatus
        ' 
        tslServerStatus.BorderSides = ToolStripStatusLabelBorderSides.Right
        tslServerStatus.Name = "tslServerStatus"
        tslServerStatus.Size = New Size(93, 19)
        tslServerStatus.Text = "Server: Stopped"
        ' 
        ' tslClients
        ' 
        tslClients.BorderSides = ToolStripStatusLabelBorderSides.Right
        tslClients.Name = "tslClients"
        tslClients.Size = New Size(59, 19)
        tslClients.Text = "Clients: 0"
        ' 
        ' tslSpring
        ' 
        tslSpring.Name = "tslSpring"
        tslSpring.Size = New Size(639, 19)
        tslSpring.Spring = True
        '
        ' tslLogToggle
        ' 
        tslLogToggle.IsLink = True
        tslLogToggle.LinkBehavior = LinkBehavior.HoverUnderline
        tslLogToggle.Name = "tslLogToggle"
        tslLogToggle.Size = New Size(27, 19)
        tslLogToggle.Text = "Log"
        '
        ' pnlContent
        ' 
        pnlContent.Controls.Add(tabMain)
        pnlContent.Controls.Add(tsNavBar)
        pnlContent.Controls.Add(splitterLog)
        pnlContent.Controls.Add(pnlLogPanel)
        pnlContent.Dock = DockStyle.Fill
        pnlContent.Location = New Point(0, 24)
        pnlContent.Name = "pnlContent"
        pnlContent.Size = New Size(880, 602)
        pnlContent.TabIndex = 1
        ' 
        ' tabMain
        ' 
        tabMain.Appearance = TabAppearance.FlatButtons
        tabMain.Controls.Add(tabPageJob)
        tabMain.Controls.Add(tabPageHelp)
        tabMain.Controls.Add(tabPageTranslate)
        tabMain.Controls.Add(tabPageBibleWs)
        tabMain.Dock = DockStyle.Fill
        tabMain.ItemSize = New Size(0, 1)
        tabMain.Location = New Point(80, 0)
        tabMain.Name = "tabMain"
        tabMain.SelectedIndex = 0
        tabMain.Size = New Size(800, 602)
        tabMain.SizeMode = TabSizeMode.Fixed
        tabMain.TabIndex = 0
        '
        ' tabPageJob
        ' 
        tabPageJob.AutoScroll = True
        tabPageJob.Controls.Add(lblMode)
        tabPageJob.Controls.Add(cboMode)
        tabPageJob.Controls.Add(grpInput)
        tabPageJob.Controls.Add(grpOutputFormats)
        tabPageJob.Controls.Add(grpProgress)

        tabPageJob.Location = New Point(4, 5)
        tabPageJob.Name = "tabPageJob"
        tabPageJob.Padding = New Padding(8)
        tabPageJob.Size = New Size(792, 593)
        tabPageJob.TabIndex = 2
        tabPageJob.Text = "Main / Job"
        ' 
        ' lblMode
        ' 
        lblMode.AutoSize = True
        lblMode.Location = New Point(12, 6)
        lblMode.Name = "lblMode"
        lblMode.Size = New Size(41, 15)
        lblMode.TabIndex = 0
        lblMode.Text = "Mode:"
        ' 
        ' cboMode
        ' 
        cboMode.DropDownStyle = ComboBoxStyle.DropDownList
        cboMode.Items.AddRange(New Object() {"Audio/Video File -> Subtitles", "YouTube -> Audio Only", "YouTube -> Full Video", "YouTube -> Subtitles"})
        cboMode.Location = New Point(12, 22)
        cboMode.Name = "cboMode"
        cboMode.Size = New Size(200, 23)
        cboMode.TabIndex = 1
        ' 
        ' grpInput
        ' 
        grpInput.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpInput.Controls.Add(lblUrl)
        grpInput.Controls.Add(txtUrl)
        grpInput.Controls.Add(btnBrowseFile)
        grpInput.Controls.Add(lblInputLanguage)
        grpInput.Controls.Add(cboInputLanguage)
        grpInput.Controls.Add(lblOutputLanguage)
        grpInput.Controls.Add(cboOutputLanguage)
        grpInput.Controls.Add(lblModel)
        grpInput.Controls.Add(cboModel)
        grpInput.Controls.Add(lblStartTime)
        grpInput.Controls.Add(txtStartHH)
        grpInput.Controls.Add(lblStartColon1)
        grpInput.Controls.Add(txtStartMM)
        grpInput.Controls.Add(lblStartColon2)
        grpInput.Controls.Add(txtStartSS)
        grpInput.Controls.Add(lblEndTime)
        grpInput.Controls.Add(txtEndHH)
        grpInput.Controls.Add(lblEndColon1)
        grpInput.Controls.Add(txtEndMM)
        grpInput.Controls.Add(lblEndColon2)
        grpInput.Controls.Add(txtEndSS)
        grpInput.Controls.Add(lblOutputDir)
        grpInput.Controls.Add(txtOutputDir)
        grpInput.Controls.Add(btnBrowseOutput)
        grpInput.Location = New Point(8, 54)
        grpInput.Name = "grpInput"
        grpInput.Size = New Size(772, 310)
        grpInput.TabIndex = 2
        grpInput.TabStop = False
        grpInput.Text = "Input"
        ' 
        ' lblUrl
        ' 
        lblUrl.AutoSize = True
        lblUrl.Location = New Point(10, 22)
        lblUrl.Name = "lblUrl"
        lblUrl.Size = New Size(142, 15)
        lblUrl.TabIndex = 0
        lblUrl.Text = "YouTube URL or local file:"
        ' 
        ' txtUrl
        ' 
        txtUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtUrl.Location = New Point(10, 38)
        txtUrl.Name = "txtUrl"
        txtUrl.Size = New Size(667, 23)
        txtUrl.TabIndex = 1
        ' 
        ' btnBrowseFile
        ' 
        btnBrowseFile.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnBrowseFile.Location = New Point(687, 37)
        btnBrowseFile.Name = "btnBrowseFile"
        btnBrowseFile.Size = New Size(75, 25)
        btnBrowseFile.TabIndex = 2
        btnBrowseFile.Text = "..."
        ' 
        ' lblInputLanguage
        ' 
        lblInputLanguage.AutoSize = True
        lblInputLanguage.Location = New Point(10, 70)
        lblInputLanguage.Name = "lblInputLanguage"
        lblInputLanguage.Size = New Size(93, 15)
        lblInputLanguage.TabIndex = 3
        lblInputLanguage.Text = "Input Language:"
        ' 
        ' cboInputLanguage
        ' 
        cboInputLanguage.DropDownStyle = ComboBoxStyle.DropDown
        cboInputLanguage.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboInputLanguage.AutoCompleteSource = AutoCompleteSource.ListItems
        cboInputLanguage.Location = New Point(10, 86)
        cboInputLanguage.Name = "cboInputLanguage"
        cboInputLanguage.Size = New Size(190, 23)
        cboInputLanguage.TabIndex = 4
        ' 
        ' lblOutputLanguage
        ' 
        lblOutputLanguage.AutoSize = True
        lblOutputLanguage.Location = New Point(250, 70)
        lblOutputLanguage.Name = "lblOutputLanguage"
        lblOutputLanguage.Size = New Size(103, 15)
        lblOutputLanguage.TabIndex = 5
        lblOutputLanguage.Text = "Output Language:"
        ' 
        ' cboOutputLanguage
        ' 
        cboOutputLanguage.DropDownStyle = ComboBoxStyle.DropDown
        cboOutputLanguage.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboOutputLanguage.AutoCompleteSource = AutoCompleteSource.ListItems
        cboOutputLanguage.Location = New Point(250, 86)
        cboOutputLanguage.Name = "cboOutputLanguage"
        cboOutputLanguage.Size = New Size(190, 23)
        cboOutputLanguage.TabIndex = 6
        ' 
        ' lblModel
        ' 
        lblModel.AutoSize = True
        lblModel.Location = New Point(10, 118)
        lblModel.Name = "lblModel"
        lblModel.Size = New Size(44, 15)
        lblModel.TabIndex = 7
        lblModel.Text = "Model:"
        ' 
        ' cboModel
        ' 
        cboModel.DropDownStyle = ComboBoxStyle.DropDownList
        cboModel.Location = New Point(10, 134)
        cboModel.Name = "cboModel"
        cboModel.Size = New Size(400, 23)
        cboModel.TabIndex = 8
        ' 
        ' lblStartTime
        ' 
        lblStartTime.AutoSize = True
        lblStartTime.Location = New Point(10, 166)
        lblStartTime.Name = "lblStartTime"
        lblStartTime.Size = New Size(61, 15)
        lblStartTime.TabIndex = 9
        lblStartTime.Text = "Start time:"
        ' 
        ' txtStartHH
        ' 
        txtStartHH.Location = New Point(10, 182)
        txtStartHH.MaxLength = 2
        txtStartHH.Name = "txtStartHH"
        txtStartHH.Size = New Size(35, 23)
        txtStartHH.TabIndex = 10
        txtStartHH.Text = "00"
        txtStartHH.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblStartColon1
        ' 
        lblStartColon1.AutoSize = True
        lblStartColon1.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblStartColon1.Location = New Point(46, 185)
        lblStartColon1.Name = "lblStartColon1"
        lblStartColon1.Size = New Size(13, 19)
        lblStartColon1.TabIndex = 11
        lblStartColon1.Text = ":"
        ' 
        ' txtStartMM
        ' 
        txtStartMM.Location = New Point(58, 182)
        txtStartMM.MaxLength = 2
        txtStartMM.Name = "txtStartMM"
        txtStartMM.Size = New Size(35, 23)
        txtStartMM.TabIndex = 12
        txtStartMM.Text = "00"
        txtStartMM.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblStartColon2
        ' 
        lblStartColon2.AutoSize = True
        lblStartColon2.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblStartColon2.Location = New Point(94, 185)
        lblStartColon2.Name = "lblStartColon2"
        lblStartColon2.Size = New Size(13, 19)
        lblStartColon2.TabIndex = 13
        lblStartColon2.Text = ":"
        ' 
        ' txtStartSS
        ' 
        txtStartSS.Location = New Point(106, 182)
        txtStartSS.MaxLength = 2
        txtStartSS.Name = "txtStartSS"
        txtStartSS.Size = New Size(35, 23)
        txtStartSS.TabIndex = 14
        txtStartSS.Text = "00"
        txtStartSS.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblEndTime
        ' 
        lblEndTime.AutoSize = True
        lblEndTime.Location = New Point(250, 166)
        lblEndTime.Name = "lblEndTime"
        lblEndTime.Size = New Size(57, 15)
        lblEndTime.TabIndex = 15
        lblEndTime.Text = "End time:"
        ' 
        ' txtEndHH
        ' 
        txtEndHH.Location = New Point(250, 182)
        txtEndHH.MaxLength = 2
        txtEndHH.Name = "txtEndHH"
        txtEndHH.Size = New Size(35, 23)
        txtEndHH.TabIndex = 16
        txtEndHH.Text = "00"
        txtEndHH.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblEndColon1
        ' 
        lblEndColon1.AutoSize = True
        lblEndColon1.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblEndColon1.Location = New Point(286, 185)
        lblEndColon1.Name = "lblEndColon1"
        lblEndColon1.Size = New Size(13, 19)
        lblEndColon1.TabIndex = 17
        lblEndColon1.Text = ":"
        ' 
        ' txtEndMM
        ' 
        txtEndMM.Location = New Point(298, 182)
        txtEndMM.MaxLength = 2
        txtEndMM.Name = "txtEndMM"
        txtEndMM.Size = New Size(35, 23)
        txtEndMM.TabIndex = 18
        txtEndMM.Text = "00"
        txtEndMM.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblEndColon2
        ' 
        lblEndColon2.AutoSize = True
        lblEndColon2.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblEndColon2.Location = New Point(334, 185)
        lblEndColon2.Name = "lblEndColon2"
        lblEndColon2.Size = New Size(13, 19)
        lblEndColon2.TabIndex = 19
        lblEndColon2.Text = ":"
        ' 
        ' txtEndSS
        ' 
        txtEndSS.Location = New Point(346, 182)
        txtEndSS.MaxLength = 2
        txtEndSS.Name = "txtEndSS"
        txtEndSS.Size = New Size(35, 23)
        txtEndSS.TabIndex = 20
        txtEndSS.Text = "00"
        txtEndSS.TextAlign = HorizontalAlignment.Center
        ' 
        ' lblOutputDir
        ' 
        lblOutputDir.AutoSize = True
        lblOutputDir.Location = New Point(10, 214)
        lblOutputDir.Name = "lblOutputDir"
        lblOutputDir.Size = New Size(82, 15)
        lblOutputDir.TabIndex = 21
        lblOutputDir.Text = "Output folder:"
        ' 
        ' txtOutputDir
        ' 
        txtOutputDir.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtOutputDir.Location = New Point(10, 230)
        txtOutputDir.Name = "txtOutputDir"
        txtOutputDir.Size = New Size(667, 23)
        txtOutputDir.TabIndex = 22
        ' 
        ' btnBrowseOutput
        ' 
        btnBrowseOutput.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnBrowseOutput.Location = New Point(687, 229)
        btnBrowseOutput.Name = "btnBrowseOutput"
        btnBrowseOutput.Size = New Size(75, 25)
        btnBrowseOutput.TabIndex = 23
        btnBrowseOutput.Text = "..."
        ' 
        ' grpOutputFormats
        ' 
        grpOutputFormats.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpOutputFormats.Controls.Add(chkSrt)
        grpOutputFormats.Controls.Add(chkVtt)
        grpOutputFormats.Controls.Add(chkTxt)
        grpOutputFormats.Controls.Add(chkJson)
        grpOutputFormats.Controls.Add(chkCsv)
        grpOutputFormats.Controls.Add(chkLrc)
        grpOutputFormats.Location = New Point(8, 374)
        grpOutputFormats.Name = "grpOutputFormats"
        grpOutputFormats.Size = New Size(772, 55)
        grpOutputFormats.TabIndex = 3
        grpOutputFormats.TabStop = False
        grpOutputFormats.Text = "Output Formats"
        ' 
        ' chkSrt
        ' 
        chkSrt.AutoSize = True
        chkSrt.Checked = True
        chkSrt.CheckState = CheckState.Checked
        chkSrt.Location = New Point(15, 22)
        chkSrt.Name = "chkSrt"
        chkSrt.Size = New Size(45, 19)
        chkSrt.TabIndex = 0
        chkSrt.Text = "SRT"
        ' 
        ' chkVtt
        ' 
        chkVtt.AutoSize = True
        chkVtt.Location = New Point(85, 22)
        chkVtt.Name = "chkVtt"
        chkVtt.Size = New Size(47, 19)
        chkVtt.TabIndex = 1
        chkVtt.Text = "VTT"
        ' 
        ' chkTxt
        ' 
        chkTxt.AutoSize = True
        chkTxt.Location = New Point(155, 22)
        chkTxt.Name = "chkTxt"
        chkTxt.Size = New Size(47, 19)
        chkTxt.TabIndex = 2
        chkTxt.Text = "TXT"
        ' 
        ' chkJson
        ' 
        chkJson.AutoSize = True
        chkJson.Location = New Point(225, 22)
        chkJson.Name = "chkJson"
        chkJson.Size = New Size(54, 19)
        chkJson.TabIndex = 3
        chkJson.Text = "JSON"
        ' 
        ' chkCsv
        ' 
        chkCsv.AutoSize = True
        chkCsv.Location = New Point(305, 22)
        chkCsv.Name = "chkCsv"
        chkCsv.Size = New Size(47, 19)
        chkCsv.TabIndex = 4
        chkCsv.Text = "CSV"
        ' 
        ' chkLrc
        ' 
        chkLrc.AutoSize = True
        chkLrc.Location = New Point(375, 22)
        chkLrc.Name = "chkLrc"
        chkLrc.Size = New Size(47, 19)
        chkLrc.TabIndex = 5
        chkLrc.Text = "LRC"
        ' 
        ' grpProgress
        ' 
        grpProgress.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpProgress.Controls.Add(lblStepStatus)
        grpProgress.Controls.Add(pbOverall)
        grpProgress.Controls.Add(pbChunk)
        grpProgress.Controls.Add(btnStart)
        grpProgress.Controls.Add(btnResume)
        grpProgress.Controls.Add(btnCancel)
        grpProgress.Controls.Add(btnOpenOutput)
        grpProgress.Controls.Add(btnOpenSubtitleEdit)
        grpProgress.Controls.Add(lnkPreviewSrt)
        grpProgress.Location = New Point(8, 439)
        grpProgress.Name = "grpProgress"
        grpProgress.Size = New Size(772, 160)
        grpProgress.TabIndex = 4
        grpProgress.TabStop = False
        grpProgress.Text = "Progress"
        ' 
        ' lblStepStatus
        ' 
        lblStepStatus.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        lblStepStatus.Location = New Point(10, 22)
        lblStepStatus.Name = "lblStepStatus"
        lblStepStatus.Size = New Size(742, 20)
        lblStepStatus.TabIndex = 0
        lblStepStatus.Text = "Ready"
        ' 
        ' pbOverall
        ' 
        pbOverall.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        pbOverall.Location = New Point(10, 45)
        pbOverall.Name = "pbOverall"
        pbOverall.Size = New Size(742, 23)
        pbOverall.TabIndex = 1
        ' 
        ' pbChunk
        ' 
        pbChunk.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        pbChunk.Location = New Point(10, 73)
        pbChunk.Name = "pbChunk"
        pbChunk.Size = New Size(742, 23)
        pbChunk.TabIndex = 2
        pbChunk.Visible = False
        ' 
        ' btnStart
        ' 
        btnStart.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        btnStart.Location = New Point(10, 105)
        btnStart.Name = "btnStart"
        btnStart.Size = New Size(100, 35)
        btnStart.TabIndex = 3
        btnStart.Text = "Start"
        ' 
        ' btnResume
        ' 
        btnResume.Location = New Point(120, 105)
        btnResume.Name = "btnResume"
        btnResume.Size = New Size(100, 35)
        btnResume.TabIndex = 4
        btnResume.Text = "Resume"
        ' 
        ' btnCancel
        ' 
        btnCancel.Enabled = False
        btnCancel.Location = New Point(230, 105)
        btnCancel.Name = "btnCancel"
        btnCancel.Size = New Size(100, 35)
        btnCancel.TabIndex = 5
        btnCancel.Text = "Cancel"
        ' 
        ' btnOpenOutput
        ' 
        btnOpenOutput.Enabled = False
        btnOpenOutput.Location = New Point(350, 105)
        btnOpenOutput.Name = "btnOpenOutput"
        btnOpenOutput.Size = New Size(150, 35)
        btnOpenOutput.TabIndex = 6
        btnOpenOutput.Text = "Open Output Folder"
        ' 
        ' btnOpenSubtitleEdit
        ' 
        btnOpenSubtitleEdit.Enabled = False
        btnOpenSubtitleEdit.Location = New Point(510, 105)
        btnOpenSubtitleEdit.Name = "btnOpenSubtitleEdit"
        btnOpenSubtitleEdit.Size = New Size(130, 35)
        btnOpenSubtitleEdit.TabIndex = 7
        btnOpenSubtitleEdit.Text = "Subtitle Edit"
        ' 
        ' lnkPreviewSrt
        ' 
        lnkPreviewSrt.AutoSize = True
        lnkPreviewSrt.Location = New Point(660, 115)
        lnkPreviewSrt.Name = "lnkPreviewSrt"
        lnkPreviewSrt.Size = New Size(96, 15)
        lnkPreviewSrt.TabIndex = 8
        lnkPreviewSrt.TabStop = True
        lnkPreviewSrt.Text = "Open preview.srt"
        lnkPreviewSrt.Visible = False
        ' 
        '
        ' tabPageHelp
        ' 
        tabPageHelp.Controls.Add(rtbHelp)
        tabPageHelp.Location = New Point(4, 5)
        tabPageHelp.Name = "tabPageHelp"
        tabPageHelp.Padding = New Padding(8)
        tabPageHelp.Size = New Size(792, 593)
        tabPageHelp.TabIndex = 3
        tabPageHelp.Text = "Help"
        ' 
        ' rtbHelp
        ' 
        rtbHelp.BackColor = Color.White
        rtbHelp.BorderStyle = BorderStyle.None
        rtbHelp.Dock = DockStyle.Fill
        rtbHelp.Font = New Font("Segoe UI", 10F)
        rtbHelp.Location = New Point(8, 8)
        rtbHelp.Name = "rtbHelp"
        rtbHelp.ReadOnly = True
        rtbHelp.Size = New Size(96, 0)
        rtbHelp.TabIndex = 0
        rtbHelp.Text = ""
        ' 
        ' tabPageTranslate
        ' 
        tabPageTranslate.Controls.Add(splitTrans)
        tabPageTranslate.Controls.Add(pnlTransBottom)
        tabPageTranslate.Controls.Add(pnlTransTop)
        tabPageTranslate.Location = New Point(4, 5)
        tabPageTranslate.Name = "tabPageTranslate"
        tabPageTranslate.Padding = New Padding(8)
        tabPageTranslate.Size = New Size(792, 593)
        tabPageTranslate.TabIndex = 4
        tabPageTranslate.Text = "Translate"
        '
        ' pnlTransTop — header bar with From/To combos and Translate button
        '
        pnlTransTop.Controls.Add(lblTransFrom)
        pnlTransTop.Controls.Add(cboTransSource)
        pnlTransTop.Controls.Add(btnTransSwap)
        pnlTransTop.Controls.Add(lblTransTo)
        pnlTransTop.Controls.Add(cboTransTarget)
        pnlTransTop.Controls.Add(btnTranslate)
        pnlTransTop.Dock = DockStyle.Top
        pnlTransTop.Location = New Point(8, 8)
        pnlTransTop.Name = "pnlTransTop"
        pnlTransTop.Size = New Size(776, 40)
        pnlTransTop.TabIndex = 2
        '
        ' lblTransFrom
        '
        lblTransFrom.AutoSize = True
        lblTransFrom.Font = New Font("Segoe UI", 10.0!)
        lblTransFrom.Location = New Point(0, 10)
        lblTransFrom.Name = "lblTransFrom"
        lblTransFrom.Size = New Size(44, 19)
        lblTransFrom.TabIndex = 0
        lblTransFrom.Text = "From:"
        '
        ' cboTransSource
        '
        cboTransSource.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboTransSource.AutoCompleteSource = AutoCompleteSource.ListItems
        cboTransSource.Location = New Point(50, 8)
        cboTransSource.Name = "cboTransSource"
        cboTransSource.Size = New Size(200, 23)
        cboTransSource.TabIndex = 1
        '
        ' btnTransSwap
        '
        btnTransSwap.FlatAppearance.BorderSize = 0
        btnTransSwap.FlatStyle = FlatStyle.Flat
        btnTransSwap.Font = New Font("Segoe UI", 12.0!)
        btnTransSwap.Location = New Point(260, 6)
        btnTransSwap.Name = "btnTransSwap"
        btnTransSwap.Size = New Size(34, 26)
        btnTransSwap.TabIndex = 2
        btnTransSwap.Text = "⇄"
        '
        ' lblTransTo
        '
        lblTransTo.AutoSize = True
        lblTransTo.Font = New Font("Segoe UI", 10.0!)
        lblTransTo.Location = New Point(304, 10)
        lblTransTo.Name = "lblTransTo"
        lblTransTo.Size = New Size(26, 19)
        lblTransTo.TabIndex = 3
        lblTransTo.Text = "To:"
        '
        ' cboTransTarget
        '
        cboTransTarget.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboTransTarget.AutoCompleteSource = AutoCompleteSource.ListItems
        cboTransTarget.Location = New Point(334, 8)
        cboTransTarget.Name = "cboTransTarget"
        cboTransTarget.Size = New Size(200, 23)
        cboTransTarget.TabIndex = 4
        '
        ' btnTranslate
        '
        btnTranslate.Font = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        btnTranslate.Location = New Point(544, 7)
        btnTranslate.Name = "btnTranslate"
        btnTranslate.Size = New Size(90, 26)
        btnTranslate.TabIndex = 5
        btnTranslate.Text = "Translate"
        '
        ' pnlTransInButtons — Copy/Clear for input text box
        '
        pnlTransInButtons.Controls.Add(btnTransCopy)
        pnlTransInButtons.Controls.Add(btnTransClear)
        pnlTransInButtons.Dock = DockStyle.Bottom
        pnlTransInButtons.Name = "pnlTransInButtons"
        pnlTransInButtons.Size = New Size(385, 30)
        pnlTransInButtons.TabIndex = 1
        '
        ' btnTransCopy — copies input text
        '
        btnTransCopy.Location = New Point(0, 2)
        btnTransCopy.Name = "btnTransCopy"
        btnTransCopy.Size = New Size(75, 26)
        btnTransCopy.TabIndex = 0
        btnTransCopy.Text = "Copy"
        '
        ' btnTransClear — clears input text
        '
        btnTransClear.Location = New Point(80, 2)
        btnTransClear.Name = "btnTransClear"
        btnTransClear.Size = New Size(75, 26)
        btnTransClear.TabIndex = 1
        btnTransClear.Text = "Clear"
        '
        ' pnlTransOutButtons — Copy/Clear for output text box
        '
        pnlTransOutButtons.Controls.Add(btnTransOutCopy)
        pnlTransOutButtons.Controls.Add(btnTransOutClear)
        pnlTransOutButtons.Dock = DockStyle.Bottom
        pnlTransOutButtons.Name = "pnlTransOutButtons"
        pnlTransOutButtons.Size = New Size(385, 30)
        pnlTransOutButtons.TabIndex = 1
        '
        ' btnTransOutCopy — copies output text
        '
        btnTransOutCopy.Location = New Point(0, 2)
        btnTransOutCopy.Name = "btnTransOutCopy"
        btnTransOutCopy.Size = New Size(75, 26)
        btnTransOutCopy.TabIndex = 0
        btnTransOutCopy.Text = "Copy"
        '
        ' btnTransOutClear — clears output text
        '
        btnTransOutClear.Location = New Point(80, 2)
        btnTransOutClear.Name = "btnTransOutClear"
        btnTransOutClear.Size = New Size(75, 26)
        btnTransOutClear.TabIndex = 1
        btnTransOutClear.Text = "Clear"
        '
        ' pnlTransBottom — status bar
        '
        pnlTransBottom.Controls.Add(lblTransStatus)
        pnlTransBottom.Dock = DockStyle.Bottom
        pnlTransBottom.Location = New Point(8, 543)
        pnlTransBottom.Name = "pnlTransBottom"
        pnlTransBottom.Size = New Size(776, 24)
        pnlTransBottom.TabIndex = 1
        '
        ' lblTransStatus
        '
        lblTransStatus.Dock = DockStyle.Fill
        lblTransStatus.ForeColor = Color.Gray
        lblTransStatus.Name = "lblTransStatus"
        lblTransStatus.TabIndex = 2
        lblTransStatus.TextAlign = ContentAlignment.MiddleRight
        '
        ' splitTrans — 50/50 horizontal split for input/output
        '
        splitTrans.Dock = DockStyle.Fill
        splitTrans.Location = New Point(8, 48)
        splitTrans.Name = "splitTrans"
        '
        ' splitTrans.Panel1
        '
        splitTrans.Panel1.Controls.Add(txtTransInput)
        splitTrans.Panel1.Controls.Add(pnlTransInButtons)
        '
        ' splitTrans.Panel2
        '
        splitTrans.Panel2.Controls.Add(txtTransOutput)
        splitTrans.Panel2.Controls.Add(pnlTransOutButtons)
        splitTrans.Size = New Size(776, 495)
        splitTrans.SplitterDistance = 385
        splitTrans.SplitterWidth = 6
        splitTrans.TabIndex = 0
        '
        ' txtTransInput
        '
        txtTransInput.AcceptsReturn = True
        txtTransInput.Dock = DockStyle.Fill
        txtTransInput.Font = New Font("Segoe UI", 11.0!)
        txtTransInput.Location = New Point(0, 0)
        txtTransInput.Multiline = True
        txtTransInput.Name = "txtTransInput"
        txtTransInput.ScrollBars = ScrollBars.Vertical
        txtTransInput.Size = New Size(385, 495)
        txtTransInput.TabIndex = 0
        txtTransInput.ContextMenuStrip = ctxTransInput
        '
        ' txtTransOutput
        '
        txtTransOutput.Dock = DockStyle.Fill
        txtTransOutput.Font = New Font("Segoe UI", 11.0!)
        txtTransOutput.Location = New Point(0, 0)
        txtTransOutput.Multiline = True
        txtTransOutput.Name = "txtTransOutput"
        txtTransOutput.ReadOnly = True
        txtTransOutput.ScrollBars = ScrollBars.Vertical
        txtTransOutput.Size = New Size(385, 495)
        txtTransOutput.TabIndex = 0
        txtTransOutput.ContextMenuStrip = ctxTransOutput
        ' 
        ' tabPageBibleWs
        '
        tabPageBibleWs.Controls.Add(splitBible)
        tabPageBibleWs.Controls.Add(pnlBibleTop)
        tabPageBibleWs.Location = New Point(4, 5)
        tabPageBibleWs.Name = "tabPageBibleWs"
        tabPageBibleWs.Size = New Size(792, 593)
        tabPageBibleWs.TabIndex = 5
        tabPageBibleWs.Text = "Bible"
        '
        ' pnlBibleTop
        '
        pnlBibleTop.Controls.Add(btnBibleGo)
        pnlBibleTop.Controls.Add(txtBibleRef)
        pnlBibleTop.Controls.Add(cboBibleTrans)
        pnlBibleTop.Controls.Add(lblBibleTrans)
        pnlBibleTop.Controls.Add(cboBibleLang)
        pnlBibleTop.Controls.Add(lblBibleLang)
        pnlBibleTop.Dock = DockStyle.Top
        pnlBibleTop.Location = New Point(0, 0)
        pnlBibleTop.Name = "pnlBibleTop"
        pnlBibleTop.Padding = New Padding(8, 6, 8, 6)
        pnlBibleTop.Size = New Size(792, 38)
        pnlBibleTop.TabIndex = 0
        '
        ' lblBibleLang
        '
        lblBibleLang.AutoSize = True
        lblBibleLang.Location = New Point(11, 10)
        lblBibleLang.Name = "lblBibleLang"
        lblBibleLang.Text = "Language:"
        '
        ' cboBibleLang
        '
        cboBibleLang.DropDownStyle = ComboBoxStyle.DropDownList
        cboBibleLang.Location = New Point(80, 7)
        cboBibleLang.Name = "cboBibleLang"
        cboBibleLang.Size = New Size(140, 23)
        cboBibleLang.TabIndex = 1
        '
        ' lblBibleTrans
        '
        lblBibleTrans.AutoSize = True
        lblBibleTrans.Location = New Point(230, 10)
        lblBibleTrans.Name = "lblBibleTrans"
        lblBibleTrans.Text = "Translation:"
        '
        ' cboBibleTrans
        '
        cboBibleTrans.DropDownStyle = ComboBoxStyle.DropDownList
        cboBibleTrans.Location = New Point(310, 7)
        cboBibleTrans.Name = "cboBibleTrans"
        cboBibleTrans.Size = New Size(200, 23)
        cboBibleTrans.TabIndex = 2
        '
        ' txtBibleRef
        '
        txtBibleRef.DropDownStyle = ComboBoxStyle.DropDown
        txtBibleRef.Location = New Point(530, 7)
        txtBibleRef.Name = "txtBibleRef"
        txtBibleRef.Size = New Size(140, 23)
        txtBibleRef.TabIndex = 3
        txtBibleRef.MaxDropDownItems = 15
        '
        ' btnBibleGo
        '
        btnBibleGo.Location = New Point(676, 6)
        btnBibleGo.Name = "btnBibleGo"
        btnBibleGo.Size = New Size(50, 25)
        btnBibleGo.TabIndex = 4
        btnBibleGo.Text = "Go"
        btnBibleGo.UseVisualStyleBackColor = True
        '
        ' splitBible
        '
        splitBible.Dock = DockStyle.Fill
        splitBible.Location = New Point(0, 38)
        splitBible.Name = "splitBible"
        splitBible.FixedPanel = FixedPanel.Panel1
        splitBible.SplitterDistance = 240
        splitBible.Size = New Size(792, 555)
        splitBible.TabIndex = 1
        '
        ' pnlBibleNavHeader (Panel1 top)
        '
        splitBible.Panel1.Controls.Add(flpBibleNav)
        splitBible.Panel1.Controls.Add(pnlBibleNavHeader)
        pnlBibleNavHeader.Controls.Add(lblBibleNavTitle)
        pnlBibleNavHeader.Controls.Add(btnBibleBack)
        pnlBibleNavHeader.Dock = DockStyle.Top
        pnlBibleNavHeader.Size = New Size(220, 30)
        pnlBibleNavHeader.Name = "pnlBibleNavHeader"
        '
        ' btnBibleBack
        '
        btnBibleBack.Dock = DockStyle.Left
        btnBibleBack.FlatStyle = FlatStyle.Flat
        btnBibleBack.FlatAppearance.BorderSize = 0
        btnBibleBack.Font = New Font("Segoe MDL2 Assets", 12F)
        btnBibleBack.Text = ChrW(&HE72B)
        btnBibleBack.Size = New Size(30, 30)
        btnBibleBack.Name = "btnBibleBack"
        btnBibleBack.Visible = False
        '
        ' lblBibleNavTitle
        '
        lblBibleNavTitle.Dock = DockStyle.Fill
        lblBibleNavTitle.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblBibleNavTitle.TextAlign = ContentAlignment.MiddleCenter
        lblBibleNavTitle.Text = "Books"
        lblBibleNavTitle.Name = "lblBibleNavTitle"
        '
        ' flpBibleNav (Panel1 fill)
        '
        flpBibleNav.AutoScroll = True
        flpBibleNav.Dock = DockStyle.Fill
        flpBibleNav.FlowDirection = FlowDirection.LeftToRight
        flpBibleNav.Location = New Point(0, 30)
        flpBibleNav.Name = "flpBibleNav"
        flpBibleNav.Padding = New Padding(4)
        flpBibleNav.Size = New Size(220, 525)
        flpBibleNav.TabIndex = 0
        flpBibleNav.WrapContents = True
        '
        ' lblBibleCopyright
        '
        lblBibleCopyright.Dock = DockStyle.Bottom
        lblBibleCopyright.Font = New Font("Segoe UI", 8F)
        lblBibleCopyright.ForeColor = Color.Gray
        lblBibleCopyright.Name = "lblBibleCopyright"
        lblBibleCopyright.Padding = New Padding(4, 2, 4, 2)
        lblBibleCopyright.Size = New Size(568, 0)
        lblBibleCopyright.AutoSize = True
        lblBibleCopyright.MaximumSize = New Size(568, 0)
        lblBibleCopyright.Text = ""
        '
        ' rtbBibleText (Panel2)
        '
        splitBible.Panel2.Controls.Add(rtbBibleText)
        splitBible.Panel2.Controls.Add(lblBibleCopyright)
        rtbBibleText.BackColor = Color.White
        rtbBibleText.BorderStyle = BorderStyle.None
        rtbBibleText.Dock = DockStyle.Fill
        rtbBibleText.Font = New Font("Segoe UI", 12F)
        rtbBibleText.Location = New Point(0, 0)
        rtbBibleText.Name = "rtbBibleText"
        rtbBibleText.ReadOnly = True
        rtbBibleText.Size = New Size(568, 555)
        rtbBibleText.TabIndex = 0
        rtbBibleText.ContextMenuStrip = ctxBible
        ' 
        ' splitterLog
        ' 
        splitterLog.BackColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
        splitterLog.Dock = DockStyle.Bottom
        splitterLog.Location = New Point(0, 418)
        splitterLog.Name = "splitterLog"
        splitterLog.Size = New Size(880, 4)
        splitterLog.TabIndex = 2
        splitterLog.TabStop = False
        splitterLog.Visible = False
        ' 
        ' pnlLogPanel
        ' 
        pnlLogPanel.Controls.Add(rtbUnifiedLog)
        pnlLogPanel.Controls.Add(pnlLogToolbar)
        pnlLogPanel.Dock = DockStyle.Bottom
        pnlLogPanel.Location = New Point(0, 422)
        pnlLogPanel.Name = "pnlLogPanel"
        pnlLogPanel.Size = New Size(880, 180)
        pnlLogPanel.TabIndex = 3
        pnlLogPanel.Visible = False
        '
        ' pnlLogToolbar
        '
        pnlLogToolbar.Controls.Add(lblLogTitle)
        pnlLogToolbar.Controls.Add(cboLogFilter)
        pnlLogToolbar.Controls.Add(btnLogClear)
        pnlLogToolbar.Controls.Add(btnLogCopy)
        pnlLogToolbar.Controls.Add(txtLogSearch)
        pnlLogToolbar.Controls.Add(btnLogSearchNext)
        pnlLogToolbar.Location = New Point(0, 0)
        pnlLogToolbar.Name = "pnlLogToolbar"
        pnlLogToolbar.Size = New Size(880, 28)
        pnlLogToolbar.TabIndex = 1
        '
        ' rtbUnifiedLog
        '
        rtbUnifiedLog.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        rtbUnifiedLog.Font = New Font("Consolas", 9.5F)
        rtbUnifiedLog.ForeColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
        rtbUnifiedLog.Location = New Point(0, 28)
        rtbUnifiedLog.Name = "rtbUnifiedLog"
        rtbUnifiedLog.ReadOnly = True
        rtbUnifiedLog.ScrollBars = RichTextBoxScrollBars.Vertical
        rtbUnifiedLog.Size = New Size(880, 152)
        rtbUnifiedLog.TabIndex = 0
        rtbUnifiedLog.Text = ""
        rtbUnifiedLog.WordWrap = False
        ' 
        ' lblLogTitle
        ' 
        lblLogTitle.AutoSize = True
        lblLogTitle.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        lblLogTitle.Location = New Point(4, 5)
        lblLogTitle.Name = "lblLogTitle"
        lblLogTitle.Size = New Size(47, 15)
        lblLogTitle.TabIndex = 0
        lblLogTitle.Text = "Output"
        ' 
        ' cboLogFilter
        ' 
        cboLogFilter.DropDownStyle = ComboBoxStyle.DropDownList
        cboLogFilter.Items.AddRange(New Object() {"All", "Pipeline", "Server", "Live", "Debug"})
        cboLogFilter.Location = New Point(70, 2)
        cboLogFilter.Name = "cboLogFilter"
        cboLogFilter.Size = New Size(100, 23)
        cboLogFilter.TabIndex = 1
        ' 
        ' btnLogClear
        ' 
        btnLogClear.FlatStyle = FlatStyle.Flat
        btnLogClear.Location = New Point(180, 1)
        btnLogClear.Name = "btnLogClear"
        btnLogClear.Size = New Size(55, 24)
        btnLogClear.TabIndex = 2
        btnLogClear.Text = "Clear"
        ' 
        ' btnLogCopy
        ' 
        btnLogCopy.FlatStyle = FlatStyle.Flat
        btnLogCopy.Location = New Point(240, 1)
        btnLogCopy.Name = "btnLogCopy"
        btnLogCopy.Size = New Size(55, 24)
        btnLogCopy.TabIndex = 3
        btnLogCopy.Text = "Copy"
        '
        ' txtLogSearch
        '
        txtLogSearch.Location = New Point(310, 2)
        txtLogSearch.Name = "txtLogSearch"
        txtLogSearch.Size = New Size(150, 23)
        txtLogSearch.TabIndex = 4
        txtLogSearch.PlaceholderText = "Search log..."
        '
        ' btnLogSearchNext
        '
        btnLogSearchNext.FlatStyle = FlatStyle.Flat
        btnLogSearchNext.Location = New Point(465, 1)
        btnLogSearchNext.Name = "btnLogSearchNext"
        btnLogSearchNext.Size = New Size(55, 24)
        btnLogSearchNext.TabIndex = 5
        btnLogSearchNext.Text = "Find"
        '
        ' ctxBible
        ctxBible.Items.AddRange(New ToolStripItem() {ctxBibleCopySelection, ctxBibleCopyVerse, ctxBibleCopyChapter})
        ctxBible.Name = "ctxBible"

        ctxBibleCopySelection.Name = "ctxBibleCopySelection"
        ctxBibleCopySelection.Text = "Copy Selection"
        ctxBibleCopySelection.ShortcutKeys = Keys.Control Or Keys.C

        ctxBibleCopyVerse.Name = "ctxBibleCopyVerse"
        ctxBibleCopyVerse.Text = "Copy Verse with Reference"

        ctxBibleCopyChapter.Name = "ctxBibleCopyChapter"
        ctxBibleCopyChapter.Text = "Copy Chapter"

        ' ctxTransInput
        ctxTransInput.Items.AddRange(New ToolStripItem() {ctxTransInputCut, ctxTransInputCopy, ctxTransInputPaste, ctxTransInputSelectAll})
        ctxTransInput.Name = "ctxTransInput"

        ctxTransInputCut.Name = "ctxTransInputCut"
        ctxTransInputCut.Text = "Cut"
        ctxTransInputCut.ShortcutKeys = Keys.Control Or Keys.X

        ctxTransInputCopy.Name = "ctxTransInputCopy"
        ctxTransInputCopy.Text = "Copy"
        ctxTransInputCopy.ShortcutKeys = Keys.Control Or Keys.C

        ctxTransInputPaste.Name = "ctxTransInputPaste"
        ctxTransInputPaste.Text = "Paste"
        ctxTransInputPaste.ShortcutKeys = Keys.Control Or Keys.V

        ctxTransInputSelectAll.Name = "ctxTransInputSelectAll"
        ctxTransInputSelectAll.Text = "Select All"
        ctxTransInputSelectAll.ShortcutKeys = Keys.Control Or Keys.A

        ' ctxTransOutput
        ctxTransOutput.Items.AddRange(New ToolStripItem() {ctxTransOutputCopy, ctxTransOutputSelectAll})
        ctxTransOutput.Name = "ctxTransOutput"

        ctxTransOutputCopy.Name = "ctxTransOutputCopy"
        ctxTransOutputCopy.Text = "Copy"
        ctxTransOutputCopy.ShortcutKeys = Keys.Control Or Keys.C

        ctxTransOutputSelectAll.Name = "ctxTransOutputSelectAll"
        ctxTransOutputSelectAll.Text = "Select All"
        ctxTransOutputSelectAll.ShortcutKeys = Keys.Control Or Keys.A

        ' trayMenu
        '
        trayMenu.Items.AddRange(New ToolStripItem() {trayMenuAbout, traySep0, trayMenuQR, trayMenuBrowser, traySep1, trayMenuShow, traySep2, trayMenuExit})
        trayMenu.Name = "trayMenu"
        trayMenu.Size = New Size(181, 148)
        '
        ' trayMenuAbout
        '
        trayMenuAbout.Name = "trayMenuAbout"
        trayMenuAbout.Size = New Size(180, 22)
        trayMenuAbout.Text = "About..."
        '
        ' traySep0
        '
        traySep0.Name = "traySep0"
        traySep0.Size = New Size(177, 6)
        '
        ' trayMenuQR
        '
        trayMenuQR.Name = "trayMenuQR"
        trayMenuQR.Size = New Size(180, 22)
        trayMenuQR.Text = "Show QR Code"
        '
        ' trayMenuBrowser
        '
        trayMenuBrowser.Name = "trayMenuBrowser"
        trayMenuBrowser.Size = New Size(180, 22)
        trayMenuBrowser.Text = "Open in Browser"
        '
        ' traySep1
        '
        traySep1.Name = "traySep1"
        traySep1.Size = New Size(177, 6)
        '
        ' trayMenuShow
        '
        trayMenuShow.Name = "trayMenuShow"
        trayMenuShow.Size = New Size(180, 22)
        trayMenuShow.Text = "Show"
        '
        ' traySep2
        '
        traySep2.Name = "traySep2"
        traySep2.Size = New Size(177, 6)
        '
        ' trayMenuExit
        '
        trayMenuExit.Name = "trayMenuExit"
        trayMenuExit.Size = New Size(180, 22)
        trayMenuExit.Text = "Exit"
        ' 
        ' trayIcon
        ' 
        trayIcon.ContextMenuStrip = trayMenu
        trayIcon.Text = "Every Tongue"
        trayIcon.Visible = True
        ' 
        ' FormMain
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        KeyPreview = True
        ClientSize = New Size(880, 650)
        Controls.Add(pnlContent)
        Controls.Add(statusMain)
        Controls.Add(menuMain)
        MainMenuStrip = menuMain
        MinimumSize = New Size(880, 650)
        Name = "FormMain"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Every Tongue"
        WindowState = FormWindowState.Maximized
        menuMain.ResumeLayout(False)
        menuMain.PerformLayout()
        tsNavBar.ResumeLayout(False)
        tsNavBar.PerformLayout()
        statusMain.ResumeLayout(False)
        statusMain.PerformLayout()
        pnlContent.ResumeLayout(False)
        tabMain.ResumeLayout(False)
        tabPageJob.ResumeLayout(False)
        tabPageJob.PerformLayout()
        grpInput.ResumeLayout(False)
        grpInput.PerformLayout()
        grpOutputFormats.ResumeLayout(False)
        grpOutputFormats.PerformLayout()
        grpProgress.ResumeLayout(False)
        grpProgress.PerformLayout()


        tabPageHelp.ResumeLayout(False)
        tabPageTranslate.ResumeLayout(False)
        splitTrans.Panel1.ResumeLayout(False)
        splitTrans.Panel1.PerformLayout()
        splitTrans.Panel2.ResumeLayout(False)
        splitTrans.Panel2.PerformLayout()
        CType(splitTrans, ComponentModel.ISupportInitialize).EndInit()
        splitTrans.ResumeLayout(False)

        pnlTransInButtons.ResumeLayout(False)
        pnlTransOutButtons.ResumeLayout(False)
        pnlTransBottom.ResumeLayout(False)
        pnlTransBottom.PerformLayout()
        pnlTransTop.ResumeLayout(False)
        pnlTransTop.PerformLayout()
        tabPageBibleWs.ResumeLayout(False)
        pnlBibleTop.ResumeLayout(False)
        pnlBibleTop.PerformLayout()
        pnlBibleNavHeader.ResumeLayout(False)
        splitBible.Panel1.ResumeLayout(False)
        splitBible.Panel2.ResumeLayout(False)
        CType(splitBible, ComponentModel.ISupportInitialize).EndInit()
        splitBible.ResumeLayout(False)
        pnlLogPanel.ResumeLayout(False)
        pnlLogToolbar.ResumeLayout(False)
        pnlLogToolbar.PerformLayout()
        trayMenu.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    ' ============================================================
    '  FIELD DECLARATIONS
    ' ============================================================

    ' Shell: Menu bar
    Friend WithEvents menuMain As MenuStrip
    Friend WithEvents mnuFile As ToolStripMenuItem
    Friend WithEvents mnuFileNewSession As ToolStripMenuItem
    Friend WithEvents mnuFileSep1 As ToolStripSeparator
    Friend WithEvents mnuFileExportDiag As ToolStripMenuItem
    Friend WithEvents mnuFileSep2 As ToolStripSeparator
    Friend WithEvents mnuFileExit As ToolStripMenuItem
    Friend WithEvents mnuTools As ToolStripMenuItem
    Friend WithEvents mnuToolsTranscribe As ToolStripMenuItem
    Friend WithEvents mnuToolsTranslate As ToolStripMenuItem
    Friend WithEvents mnuToolsBible As ToolStripMenuItem
    Friend WithEvents mnuToolsSep1 As ToolStripSeparator
    Friend WithEvents mnuToolsGlossary As ToolStripMenuItem
    Friend WithEvents mnuToolsLocalization As ToolStripMenuItem
    Friend WithEvents mnuToolsSep2 As ToolStripSeparator
    Friend WithEvents mnuToolsDownloadMgr As ToolStripMenuItem
    Friend WithEvents mnuToolsVerifyPaths As ToolStripMenuItem
    Friend WithEvents mnuToolsVerifyIntegrity As ToolStripMenuItem
    Friend WithEvents mnuToolsBenchmark As ToolStripMenuItem
    Friend WithEvents mnuToolsSep3 As ToolStripSeparator
    Friend WithEvents mnuToolsOptions As ToolStripMenuItem
    Friend WithEvents mnuSession As ToolStripMenuItem
    Friend WithEvents mnuSessionQR As ToolStripMenuItem
    Friend WithEvents mnuSessionCopyUrl As ToolStripMenuItem
    Friend WithEvents mnuView As ToolStripMenuItem
    Friend WithEvents mnuViewLogPanel As ToolStripMenuItem
    Friend WithEvents mnuViewSep1 As ToolStripSeparator
    Friend WithEvents mnuViewTheme As ToolStripMenuItem
    Friend WithEvents mnuViewThemeSystem As ToolStripMenuItem
    Friend WithEvents mnuViewThemeLight As ToolStripMenuItem
    Friend WithEvents mnuViewThemeDark As ToolStripMenuItem
    Friend WithEvents mnuViewSep2 As ToolStripSeparator
    Friend WithEvents mnuViewClients As ToolStripMenuItem
    Friend WithEvents mnuViewSep3 As ToolStripSeparator
    Friend WithEvents mnuViewFullScreen As ToolStripMenuItem
    Friend WithEvents mnuHelpMenu As ToolStripMenuItem
    Friend WithEvents mnuHelpQuickStart As ToolStripMenuItem
    Friend WithEvents mnuHelpShortcuts As ToolStripMenuItem
    Friend WithEvents mnuHelpSep1 As ToolStripSeparator
    Friend WithEvents mnuHelpHardware As ToolStripMenuItem
    Friend WithEvents mnuHelpSpecSheet As ToolStripMenuItem
    Friend WithEvents mnuHelpSep2 As ToolStripSeparator
    Friend WithEvents mnuHelpUpdates As ToolStripMenuItem
    Friend WithEvents mnuHelpAbout As ToolStripMenuItem

    ' Shell: Nav rail
    Friend WithEvents tsNavBar As ToolStrip
    Friend WithEvents btnNavLog As ToolStripButton
    Friend WithEvents btnNavTranscribe As ToolStripButton
    Friend WithEvents btnNavTranslate As ToolStripButton
    Friend WithEvents btnNavBible As ToolStripButton

    ' Shell: Status bar
    Friend WithEvents statusMain As StatusStrip
    Friend WithEvents tslServerStatus As ToolStripStatusLabel
    Friend WithEvents tslClients As ToolStripStatusLabel
    Friend WithEvents tslSpring As ToolStripStatusLabel
    Friend WithEvents tslLogToggle As ToolStripStatusLabel

    ' Shell: Content panel
    Friend WithEvents pnlContent As Panel

    ' Shell: Log panel
    Friend WithEvents pnlLogPanel As Panel
    Friend WithEvents splitterLog As Splitter
    Friend WithEvents pnlLogToolbar As Panel
    Friend WithEvents lblLogTitle As Label
    Friend WithEvents cboLogFilter As ComboBox
    Friend WithEvents btnLogClear As Button
    Friend WithEvents btnLogCopy As Button
    Friend WithEvents rtbUnifiedLog As RichTextBox
    Friend WithEvents txtLogSearch As TextBox
    Friend WithEvents btnLogSearchNext As Button

    ' TabControl
    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabPageJob As TabPage
    Friend WithEvents tabPageHelp As TabPage
    Friend WithEvents tabPageTranslate As TabPage
    Friend WithEvents tabPageBibleWs As TabPage

    ' Tooltip
    Friend WithEvents tipMain As ToolTip

    ' Tab: Job
    Friend WithEvents lblMode As Label
    Friend WithEvents cboMode As ComboBox
    Friend WithEvents grpInput As GroupBox
    Friend WithEvents lblUrl As Label
    Friend WithEvents txtUrl As TextBox
    Friend WithEvents btnBrowseFile As Button
    Friend WithEvents lblStartTime As Label
    Friend WithEvents txtStartHH As TextBox
    Friend WithEvents lblStartColon1 As Label
    Friend WithEvents txtStartMM As TextBox
    Friend WithEvents lblStartColon2 As Label
    Friend WithEvents txtStartSS As TextBox
    Friend WithEvents lblEndTime As Label
    Friend WithEvents txtEndHH As TextBox
    Friend WithEvents lblEndColon1 As Label
    Friend WithEvents txtEndMM As TextBox
    Friend WithEvents lblEndColon2 As Label
    Friend WithEvents txtEndSS As TextBox
    Friend WithEvents lblOutputDir As Label
    Friend WithEvents txtOutputDir As TextBox
    Friend WithEvents btnBrowseOutput As Button
    Friend WithEvents lblInputLanguage As Label
    Friend WithEvents cboInputLanguage As ComboBox
    Friend WithEvents lblOutputLanguage As Label
    Friend WithEvents cboOutputLanguage As ComboBox
    Friend WithEvents lblModel As Label
    Friend WithEvents cboModel As ComboBox
    Friend WithEvents grpOutputFormats As GroupBox
    Friend WithEvents chkSrt As CheckBox
    Friend WithEvents chkVtt As CheckBox
    Friend WithEvents chkTxt As CheckBox
    Friend WithEvents chkJson As CheckBox
    Friend WithEvents chkCsv As CheckBox
    Friend WithEvents chkLrc As CheckBox
    Friend WithEvents grpProgress As GroupBox
    Friend WithEvents pbOverall As ProgressBar
    Friend WithEvents lblStepStatus As Label
    Friend WithEvents pbChunk As ProgressBar
    Friend WithEvents btnStart As Button
    Friend WithEvents btnResume As Button
    Friend WithEvents btnCancel As Button
    Friend WithEvents btnOpenOutput As Button
    Friend WithEvents btnOpenSubtitleEdit As Button
    Friend WithEvents lnkPreviewSrt As LinkLabel

    Friend WithEvents rtbHelp As RichTextBox

    ' Tab: Translate
    Friend WithEvents pnlTransTop As Panel
    Friend WithEvents lblTransFrom As Label
    Friend WithEvents cboTransSource As ComboBox
    Friend WithEvents btnTransSwap As Button
    Friend WithEvents lblTransTo As Label
    Friend WithEvents cboTransTarget As ComboBox
    Friend WithEvents btnTranslate As Button
    Friend WithEvents splitTrans As SplitContainer
    Friend WithEvents txtTransInput As TextBox
    Friend WithEvents txtTransOutput As TextBox
    Friend WithEvents pnlTransInButtons As Panel
    Friend WithEvents pnlTransOutButtons As Panel
    Friend WithEvents btnTransOutCopy As Button
    Friend WithEvents btnTransOutClear As Button
    Friend WithEvents pnlTransBottom As Panel
    Friend WithEvents btnTransCopy As Button
    Friend WithEvents btnTransClear As Button
    Friend WithEvents lblTransStatus As Label

    ' Tab: Bible
    Friend WithEvents pnlBibleTop As Panel
    Friend WithEvents lblBibleLang As Label
    Friend WithEvents cboBibleLang As ComboBox
    Friend WithEvents lblBibleTrans As Label
    Friend WithEvents cboBibleTrans As ComboBox
    Friend WithEvents txtBibleRef As ComboBox
    Friend WithEvents btnBibleGo As Button
    Friend WithEvents splitBible As SplitContainer
    Friend WithEvents pnlBibleNavHeader As Panel
    Friend WithEvents btnBibleBack As Button
    Friend WithEvents lblBibleNavTitle As Label
    Friend WithEvents flpBibleNav As FlowLayoutPanel
    Friend WithEvents rtbBibleText As RichTextBox
    Friend WithEvents lblBibleCopyright As Label

    ' Context menus
    Friend WithEvents ctxBible As ContextMenuStrip
    Friend WithEvents ctxBibleCopySelection As ToolStripMenuItem
    Friend WithEvents ctxBibleCopyVerse As ToolStripMenuItem
    Friend WithEvents ctxBibleCopyChapter As ToolStripMenuItem
    Friend WithEvents ctxTransInput As ContextMenuStrip
    Friend WithEvents ctxTransInputCut As ToolStripMenuItem
    Friend WithEvents ctxTransInputCopy As ToolStripMenuItem
    Friend WithEvents ctxTransInputPaste As ToolStripMenuItem
    Friend WithEvents ctxTransInputSelectAll As ToolStripMenuItem
    Friend WithEvents ctxTransOutput As ContextMenuStrip
    Friend WithEvents ctxTransOutputCopy As ToolStripMenuItem
    Friend WithEvents ctxTransOutputSelectAll As ToolStripMenuItem

    ' Tab: Server

    ' System Tray
    Friend WithEvents trayIcon As NotifyIcon
    Friend WithEvents trayMenu As ContextMenuStrip
    Friend WithEvents trayMenuShow As ToolStripMenuItem
    Friend WithEvents trayMenuAbout As ToolStripMenuItem
    Friend WithEvents trayMenuQR As ToolStripMenuItem
    Friend WithEvents trayMenuBrowser As ToolStripMenuItem
    Friend WithEvents trayMenuExit As ToolStripMenuItem
    Friend WithEvents traySep0 As ToolStripSeparator
    Friend WithEvents traySep1 As ToolStripSeparator
    Friend WithEvents traySep2 As ToolStripSeparator
End Class
