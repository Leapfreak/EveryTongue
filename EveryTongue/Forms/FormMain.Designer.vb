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
        mnuToolsCheckDeps = New ToolStripMenuItem()
        mnuToolsVerifyPaths = New ToolStripMenuItem()
        mnuToolsVerifyIntegrity = New ToolStripMenuItem()
        mnuToolsSep3 = New ToolStripSeparator()
        mnuToolsPaths = New ToolStripMenuItem()
        mnuToolsServer = New ToolStripMenuItem()
        mnuToolsSep4 = New ToolStripSeparator()
        mnuToolsOptions = New ToolStripMenuItem()
        mnuSession = New ToolStripMenuItem()
        mnuSessionStart = New ToolStripMenuItem()
        mnuSessionStop = New ToolStripMenuItem()
        mnuSessionSep1 = New ToolStripSeparator()
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
        btnNavLive = New ToolStripButton()
        btnNavTranscribe = New ToolStripButton()
        btnNavTranslate = New ToolStripButton()
        btnNavBible = New ToolStripButton()
        statusMain = New StatusStrip()
        tslServerStatus = New ToolStripStatusLabel()
        tslClients = New ToolStripStatusLabel()
        tslSpring = New ToolStripStatusLabel()
        tslLiveStatus = New ToolStripStatusLabel()
        tslElapsed = New ToolStripStatusLabel()
        tslLogToggle = New ToolStripStatusLabel()
        liveElapsedTimer = New Timer(components)
        pnlContent = New Panel()
        tabMain = New TabControl()
        tabPageLive = New TabPage()
        grpLiveInput = New GroupBox()
        lblLiveDevice = New Label()
        cboLiveDevice = New ComboBox()
        btnRefreshDevices = New Button()
        lblLiveInputLang = New Label()
        cboLiveInputLang = New ComboBox()
        btnEditFilters = New Button()
        pnlLiveButtons = New Panel()
        btnLiveStart = New Button()
        btnLiveStop = New Button()
        lblMaxSegment = New Label()
        trkMaxSegment = New TrackBar()
        lblMaxSegmentValue = New Label()
        lblVadSilence = New Label()
        trkVadSilence = New TrackBar()
        lblVadSilenceValue = New Label()
        btnTuneStats = New Button()
        pnlLiveOutput = New Panel()
        wvLiveClients = New Microsoft.Web.WebView2.WinForms.WebView2()

        tabPageServer = New TabPage()
        grpServerSettings = New GroupBox()
        lblServerPort = New Label()
        nudServerPort = New NumericUpDown()
        btnServerStart = New Button()
        btnServerStop = New Button()
        btnServerRestart = New Button()
        lblSubtitleBg = New Label()
        btnSubtitleBg = New Button()
        lblSubtitleFg = New Label()
        btnSubtitleFg = New Button()
        lblSubtitleFont = New Label()
        cboSubtitleFont = New ComboBox()
        lblSubtitleSize = New Label()
        nudSubtitleSize = New NumericUpDown()
        chkSubtitleBold = New CheckBox()
        lblAdminPin = New Label()
        txtAdminPin = New TextBox()
        lblLiveServerPort = New Label()
        nudLiveServerPort = New NumericUpDown()
        lblTranslationPort = New Label()
        nudTranslationPort = New NumericUpDown()
        lblTransDevice = New Label()
        cboTransDevice = New ComboBox()
        lblTransUnload = New Label()
        nudTransUnload = New NumericUpDown()
        chkTransEnabled = New CheckBox()
        chkAllowFirewall = New CheckBox()
        lblTtsBackends = New Label()
        txtTtsBackends = New TextBox()
        lblTtsHint = New Label()
        btnSetupTranslation = New Button()
        grpServerInfo = New GroupBox()
        lblServerStatus = New Label()
        lblServerUrl = New Label()
        lblServerClients = New Label()
        btnCopyUrl = New Button()
        rtbServerLog = New RichTextBox()
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
        wvBible = New Microsoft.Web.WebView2.WinForms.WebView2()
        lblBibleStatus = New Label()
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
        trayMenu = New ContextMenuStrip(components)
        trayMenuAbout = New ToolStripMenuItem()
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
        tabPageLive.SuspendLayout()
        grpLiveInput.SuspendLayout()
        pnlLiveButtons.SuspendLayout()
        CType(trkMaxSegment, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkVadSilence, ComponentModel.ISupportInitialize).BeginInit()
        pnlLiveOutput.SuspendLayout()
        CType(wvLiveClients, ComponentModel.ISupportInitialize).BeginInit()
        tabPageServer.SuspendLayout()
        grpServerSettings.SuspendLayout()
        CType(nudServerPort, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudSubtitleSize, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudLiveServerPort, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudTranslationPort, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudTransUnload, ComponentModel.ISupportInitialize).BeginInit()
        grpServerInfo.SuspendLayout()
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
        CType(wvBible, ComponentModel.ISupportInitialize).BeginInit()
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
        mnuTools.DropDownItems.AddRange(New ToolStripItem() {mnuToolsTranscribe, mnuToolsTranslate, mnuToolsBible, mnuToolsSep1, mnuToolsGlossary, mnuToolsLocalization, mnuToolsSep2, mnuToolsDownloadMgr, mnuToolsCheckDeps, mnuToolsVerifyPaths, mnuToolsVerifyIntegrity, mnuToolsSep3, mnuToolsPaths, mnuToolsServer, mnuToolsSep4, mnuToolsOptions})
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
        mnuToolsGlossary.Text = "Glossary Editor..."
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
        ' mnuToolsCheckDeps
        ' 
        mnuToolsCheckDeps.Name = "mnuToolsCheckDeps"
        mnuToolsCheckDeps.Size = New Size(184, 22)
        mnuToolsCheckDeps.Text = "Check Dependencies"
        ' 
        ' mnuToolsVerifyPaths
        ' 
        mnuToolsVerifyPaths.Name = "mnuToolsVerifyPaths"
        mnuToolsVerifyPaths.Size = New Size(184, 22)
        mnuToolsVerifyPaths.Text = "Verify Paths"
        ' 
        ' mnuToolsVerifyIntegrity
        ' 
        mnuToolsVerifyIntegrity.Enabled = False
        mnuToolsVerifyIntegrity.Name = "mnuToolsVerifyIntegrity"
        mnuToolsVerifyIntegrity.Size = New Size(184, 22)
        mnuToolsVerifyIntegrity.Text = "Verify File Integrity"
        ' 
        ' mnuToolsSep3
        ' 
        mnuToolsSep3.Name = "mnuToolsSep3"
        mnuToolsSep3.Size = New Size(181, 6)
        ' 
        ' mnuToolsPaths
        ' 
        mnuToolsPaths.Name = "mnuToolsPaths"
        mnuToolsPaths.Size = New Size(184, 22)
        mnuToolsPaths.Text = "Tool Paths..."
        ' 
        ' mnuToolsServer
        ' 
        mnuToolsServer.Name = "mnuToolsServer"
        mnuToolsServer.Size = New Size(184, 22)
        mnuToolsServer.Text = "Server Settings..."
        ' 
        ' mnuToolsSep4
        ' 
        mnuToolsSep4.Name = "mnuToolsSep4"
        mnuToolsSep4.Size = New Size(181, 6)
        ' 
        ' mnuToolsOptions
        ' 
        mnuToolsOptions.Name = "mnuToolsOptions"
        mnuToolsOptions.ShortcutKeyDisplayString = "Ctrl+,"
        mnuToolsOptions.ShortcutKeys = Keys.F10
        mnuToolsOptions.Size = New Size(184, 22)
        mnuToolsOptions.Text = "&Options..."
        ' 
        ' mnuSession
        ' 
        mnuSession.DropDownItems.AddRange(New ToolStripItem() {mnuSessionStart, mnuSessionStop, mnuSessionSep1, mnuSessionQR, mnuSessionCopyUrl})
        mnuSession.Name = "mnuSession"
        mnuSession.Size = New Size(58, 20)
        mnuSession.Text = "&Session"
        ' 
        ' mnuSessionStart
        ' 
        mnuSessionStart.Name = "mnuSessionStart"
        mnuSessionStart.ShortcutKeys = Keys.F5
        mnuSessionStart.Size = New Size(173, 22)
        mnuSessionStart.Text = "Start Live"
        ' 
        ' mnuSessionStop
        ' 
        mnuSessionStop.Name = "mnuSessionStop"
        mnuSessionStop.ShortcutKeys = Keys.Shift Or Keys.F5
        mnuSessionStop.Size = New Size(173, 22)
        mnuSessionStop.Text = "Stop Live"
        ' 
        ' mnuSessionSep1
        ' 
        mnuSessionSep1.Name = "mnuSessionSep1"
        mnuSessionSep1.Size = New Size(170, 6)
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
        mnuView.DropDownItems.AddRange(New ToolStripItem() {mnuViewLogPanel, mnuViewSep1, mnuViewTheme, mnuViewSep2, mnuViewFullScreen})
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
        mnuHelpHardware.Enabled = False
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
        tsNavBar.BackColor = Color.FromArgb(CByte(37), CByte(37), CByte(38))
        tsNavBar.Dock = DockStyle.Top
        tsNavBar.GripStyle = ToolStripGripStyle.Hidden
        tsNavBar.ImageScalingSize = New Size(28, 28)
        tsNavBar.Items.AddRange(New ToolStripItem() {btnNavLive, btnNavTranscribe, btnNavTranslate, btnNavBible})
        tsNavBar.Location = New Point(0, 0)
        tsNavBar.Name = "tsNavBar"
        tsNavBar.Padding = New Padding(4, 2, 4, 2)
        tsNavBar.RenderMode = ToolStripRenderMode.ManagerRenderMode
        tsNavBar.Size = New Size(880, 40)
        tsNavBar.TabIndex = 1
        '
        ' btnNavLive
        '
        btnNavLive.AutoSize = True
        btnNavLive.ForeColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
        btnNavLive.ImageScaling = ToolStripItemImageScaling.None
        btnNavLive.Margin = New Padding(0, 0, 2, 0)
        btnNavLive.Name = "btnNavLive"
        btnNavLive.Padding = New Padding(8, 2, 8, 2)
        btnNavLive.Text = "Live"
        btnNavLive.TextImageRelation = TextImageRelation.ImageBeforeText
        '
        ' btnNavTranscribe
        '
        btnNavTranscribe.AutoSize = True
        btnNavTranscribe.ForeColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
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
        btnNavTranslate.ForeColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
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
        btnNavBible.ForeColor = Color.FromArgb(CByte(200), CByte(200), CByte(200))
        btnNavBible.ImageScaling = ToolStripItemImageScaling.None
        btnNavBible.Margin = New Padding(0, 0, 2, 0)
        btnNavBible.Name = "btnNavBible"
        btnNavBible.Padding = New Padding(8, 2, 8, 2)
        btnNavBible.Text = "Bible"
        btnNavBible.TextImageRelation = TextImageRelation.ImageBeforeText
        ' 
        ' statusMain
        ' 
        statusMain.Items.AddRange(New ToolStripItem() {tslServerStatus, tslClients, tslSpring, tslLiveStatus, tslElapsed, tslLogToggle})
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
        ' tslLiveStatus
        ' 
        tslLiveStatus.BorderSides = ToolStripStatusLabelBorderSides.Right
        tslLiveStatus.Name = "tslLiveStatus"
        tslLiveStatus.Size = New Size(43, 19)
        tslLiveStatus.Text = "Ready"
        ' 
        ' tslElapsed
        ' 
        tslElapsed.BorderSides = ToolStripStatusLabelBorderSides.Right
        tslElapsed.Name = "tslElapsed"
        tslElapsed.Size = New Size(4, 19)
        ' 
        ' tslLogToggle
        ' 
        tslLogToggle.IsLink = True
        tslLogToggle.LinkBehavior = LinkBehavior.HoverUnderline
        tslLogToggle.Name = "tslLogToggle"
        tslLogToggle.Size = New Size(27, 19)
        tslLogToggle.Text = "Log"
        ' 
        ' liveElapsedTimer
        ' 
        liveElapsedTimer.Interval = 1000
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
        tabMain.Controls.Add(tabPageLive)
        tabMain.Controls.Add(tabPageServer)
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
        ' tabPageLive
        ' 
        tabPageLive.Controls.Add(grpLiveInput)
        tabPageLive.Controls.Add(pnlLiveButtons)
        tabPageLive.Controls.Add(pnlLiveOutput)
        tabPageLive.Location = New Point(4, 5)
        tabPageLive.Name = "tabPageLive"
        tabPageLive.Padding = New Padding(8)
        tabPageLive.Size = New Size(792, 593)
        tabPageLive.TabIndex = 0
        tabPageLive.Text = "Live Translation"
        ' 
        ' grpLiveInput
        ' 
        grpLiveInput.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpLiveInput.Controls.Add(lblLiveDevice)
        grpLiveInput.Controls.Add(cboLiveDevice)
        grpLiveInput.Controls.Add(btnRefreshDevices)
        grpLiveInput.Controls.Add(lblLiveInputLang)
        grpLiveInput.Controls.Add(cboLiveInputLang)
        grpLiveInput.Controls.Add(btnEditFilters)
        grpLiveInput.Location = New Point(8, 6)
        grpLiveInput.Name = "grpLiveInput"
        grpLiveInput.Size = New Size(1440, 130)
        grpLiveInput.TabIndex = 0
        grpLiveInput.TabStop = False
        grpLiveInput.Text = "Live Translation Settings"
        ' 
        ' lblLiveDevice
        ' 
        lblLiveDevice.AutoSize = True
        lblLiveDevice.Location = New Point(10, 22)
        lblLiveDevice.Name = "lblLiveDevice"
        lblLiveDevice.Size = New Size(80, 15)
        lblLiveDevice.TabIndex = 0
        lblLiveDevice.Text = "Audio Device:"
        ' 
        ' cboLiveDevice
        ' 
        cboLiveDevice.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        cboLiveDevice.DropDownStyle = ComboBoxStyle.DropDownList
        cboLiveDevice.Location = New Point(10, 38)
        cboLiveDevice.Name = "cboLiveDevice"
        cboLiveDevice.Size = New Size(1280, 23)
        cboLiveDevice.TabIndex = 1
        ' 
        ' btnRefreshDevices
        ' 
        btnRefreshDevices.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnRefreshDevices.Location = New Point(1300, 37)
        btnRefreshDevices.Name = "btnRefreshDevices"
        btnRefreshDevices.Size = New Size(85, 25)
        btnRefreshDevices.TabIndex = 2
        btnRefreshDevices.Text = "Refresh"
        ' 
        ' lblLiveInputLang
        ' 
        lblLiveInputLang.AutoSize = True
        lblLiveInputLang.Location = New Point(10, 70)
        lblLiveInputLang.Name = "lblLiveInputLang"
        lblLiveInputLang.Size = New Size(93, 15)
        lblLiveInputLang.TabIndex = 3
        lblLiveInputLang.Text = "Input Language:"
        ' 
        ' cboLiveInputLang
        ' 
        cboLiveInputLang.Location = New Point(10, 86)
        cboLiveInputLang.Name = "cboLiveInputLang"
        cboLiveInputLang.Size = New Size(150, 23)
        cboLiveInputLang.TabIndex = 4
        ' 
        ' btnEditFilters
        ' 
        btnEditFilters.Location = New Point(170, 85)
        btnEditFilters.Name = "btnEditFilters"
        btnEditFilters.Size = New Size(80, 25)
        btnEditFilters.TabIndex = 5
        btnEditFilters.Text = "Filters..."
        ' 
        ' pnlLiveButtons
        ' 
        pnlLiveButtons.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        pnlLiveButtons.Controls.Add(btnLiveStart)
        pnlLiveButtons.Controls.Add(btnLiveStop)
        pnlLiveButtons.Controls.Add(lblMaxSegment)
        pnlLiveButtons.Controls.Add(trkMaxSegment)
        pnlLiveButtons.Controls.Add(lblMaxSegmentValue)
        pnlLiveButtons.Controls.Add(lblVadSilence)
        pnlLiveButtons.Controls.Add(trkVadSilence)
        pnlLiveButtons.Controls.Add(lblVadSilenceValue)
        pnlLiveButtons.Controls.Add(btnTuneStats)
        pnlLiveButtons.Location = New Point(8, 142)
        pnlLiveButtons.Name = "pnlLiveButtons"
        pnlLiveButtons.Size = New Size(1440, 35)
        pnlLiveButtons.TabIndex = 1
        ' 
        ' btnLiveStart
        ' 
        btnLiveStart.Location = New Point(0, 0)
        btnLiveStart.Name = "btnLiveStart"
        btnLiveStart.Size = New Size(100, 30)
        btnLiveStart.TabIndex = 0
        btnLiveStart.Text = "Start"
        ' 
        ' btnLiveStop
        ' 
        btnLiveStop.Enabled = False
        btnLiveStop.Location = New Point(110, 0)
        btnLiveStop.Name = "btnLiveStop"
        btnLiveStop.Size = New Size(100, 30)
        btnLiveStop.TabIndex = 1
        btnLiveStop.Text = "Stop"
        ' 
        ' lblMaxSegment
        ' 
        lblMaxSegment.AutoSize = True
        lblMaxSegment.Location = New Point(230, 7)
        lblMaxSegment.Name = "lblMaxSegment"
        lblMaxSegment.Size = New Size(82, 15)
        lblMaxSegment.TabIndex = 2
        lblMaxSegment.Text = "Max Segment:"
        ' 
        ' trkMaxSegment
        ' 
        trkMaxSegment.LargeChange = 10
        trkMaxSegment.Location = New Point(315, 0)
        trkMaxSegment.Maximum = 60
        trkMaxSegment.Minimum = 5
        trkMaxSegment.Name = "trkMaxSegment"
        trkMaxSegment.Size = New Size(180, 45)
        trkMaxSegment.SmallChange = 5
        trkMaxSegment.TabIndex = 3
        trkMaxSegment.TickFrequency = 5
        trkMaxSegment.Value = 15
        ' 
        ' lblMaxSegmentValue
        ' 
        lblMaxSegmentValue.AutoSize = True
        lblMaxSegmentValue.Location = New Point(500, 7)
        lblMaxSegmentValue.Name = "lblMaxSegmentValue"
        lblMaxSegmentValue.Size = New Size(24, 15)
        lblMaxSegmentValue.TabIndex = 4
        lblMaxSegmentValue.Text = "15s"
        ' 
        ' lblVadSilence
        ' 
        lblVadSilence.AutoSize = True
        lblVadSilence.Location = New Point(540, 7)
        lblVadSilence.Name = "lblVadSilence"
        lblVadSilence.Size = New Size(72, 15)
        lblVadSilence.TabIndex = 5
        lblVadSilence.Text = "VAD Silence:"
        ' 
        ' trkVadSilence
        ' 
        trkVadSilence.LargeChange = 200
        trkVadSilence.Location = New Point(625, 0)
        trkVadSilence.Maximum = 1500
        trkVadSilence.Minimum = 200
        trkVadSilence.Name = "trkVadSilence"
        trkVadSilence.Size = New Size(150, 45)
        trkVadSilence.SmallChange = 100
        trkVadSilence.TabIndex = 6
        trkVadSilence.TickFrequency = 100
        trkVadSilence.Value = 800
        ' 
        ' lblVadSilenceValue
        ' 
        lblVadSilenceValue.AutoSize = True
        lblVadSilenceValue.Location = New Point(780, 7)
        lblVadSilenceValue.Name = "lblVadSilenceValue"
        lblVadSilenceValue.Size = New Size(41, 15)
        lblVadSilenceValue.TabIndex = 7
        lblVadSilenceValue.Text = "800ms"
        ' 
        ' btnTuneStats
        ' 
        btnTuneStats.Enabled = False
        btnTuneStats.Location = New Point(830, 0)
        btnTuneStats.Name = "btnTuneStats"
        btnTuneStats.Size = New Size(70, 30)
        btnTuneStats.TabIndex = 8
        btnTuneStats.Text = "Tune"
        ' 
        ' pnlLiveOutput
        ' 
        pnlLiveOutput.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        pnlLiveOutput.Controls.Add(wvLiveClients)

        pnlLiveOutput.Location = New Point(8, 183)
        pnlLiveOutput.Name = "pnlLiveOutput"
        pnlLiveOutput.Size = New Size(776, 402)
        pnlLiveOutput.TabIndex = 2
        ' 
        ' wvLiveClients
        ' 
        wvLiveClients.AllowExternalDrop = True
        wvLiveClients.CreationProperties = Nothing
        wvLiveClients.DefaultBackgroundColor = Color.Black
        wvLiveClients.Dock = DockStyle.Fill
        wvLiveClients.Location = New Point(0, 0)
        wvLiveClients.Name = "wvLiveClients"
        wvLiveClients.Size = New Size(776, 402)
        wvLiveClients.TabIndex = 0
        wvLiveClients.ZoomFactor = 1R
        '
        ' tabPageServer
        ' 
        tabPageServer.Controls.Add(grpServerSettings)
        tabPageServer.Controls.Add(grpServerInfo)
        tabPageServer.Controls.Add(rtbServerLog)
        tabPageServer.Location = New Point(4, 5)
        tabPageServer.Name = "tabPageServer"
        tabPageServer.Padding = New Padding(8)
        tabPageServer.Size = New Size(792, 593)
        tabPageServer.TabIndex = 1
        tabPageServer.Text = "Subtitle Server"
        ' 
        ' grpServerSettings
        ' 
        grpServerSettings.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpServerSettings.Controls.Add(lblServerPort)
        grpServerSettings.Controls.Add(nudServerPort)
        grpServerSettings.Controls.Add(btnServerStart)
        grpServerSettings.Controls.Add(btnServerStop)
        grpServerSettings.Controls.Add(btnServerRestart)
        grpServerSettings.Controls.Add(lblSubtitleBg)
        grpServerSettings.Controls.Add(btnSubtitleBg)
        grpServerSettings.Controls.Add(lblSubtitleFg)
        grpServerSettings.Controls.Add(btnSubtitleFg)
        grpServerSettings.Controls.Add(lblSubtitleFont)
        grpServerSettings.Controls.Add(cboSubtitleFont)
        grpServerSettings.Controls.Add(lblSubtitleSize)
        grpServerSettings.Controls.Add(nudSubtitleSize)
        grpServerSettings.Controls.Add(chkSubtitleBold)
        grpServerSettings.Controls.Add(lblAdminPin)
        grpServerSettings.Controls.Add(txtAdminPin)
        grpServerSettings.Controls.Add(lblLiveServerPort)
        grpServerSettings.Controls.Add(nudLiveServerPort)
        grpServerSettings.Controls.Add(lblTranslationPort)
        grpServerSettings.Controls.Add(nudTranslationPort)
        grpServerSettings.Controls.Add(lblTransDevice)
        grpServerSettings.Controls.Add(cboTransDevice)
        grpServerSettings.Controls.Add(lblTransUnload)
        grpServerSettings.Controls.Add(nudTransUnload)
        grpServerSettings.Controls.Add(chkTransEnabled)
        grpServerSettings.Controls.Add(chkAllowFirewall)
        grpServerSettings.Controls.Add(lblTtsBackends)
        grpServerSettings.Controls.Add(txtTtsBackends)
        grpServerSettings.Controls.Add(lblTtsHint)
        grpServerSettings.Controls.Add(btnSetupTranslation)
        grpServerSettings.Location = New Point(8, 6)
        grpServerSettings.Name = "grpServerSettings"
        grpServerSettings.Size = New Size(760, 254)
        grpServerSettings.TabIndex = 0
        grpServerSettings.TabStop = False
        grpServerSettings.Text = "Server Settings"
        ' 
        ' lblServerPort
        ' 
        lblServerPort.AutoSize = True
        lblServerPort.Location = New Point(10, 22)
        lblServerPort.Name = "lblServerPort"
        lblServerPort.Size = New Size(32, 15)
        lblServerPort.TabIndex = 0
        lblServerPort.Text = "Port:"
        ' 
        ' nudServerPort
        ' 
        nudServerPort.Location = New Point(10, 38)
        nudServerPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        nudServerPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        nudServerPort.Name = "nudServerPort"
        nudServerPort.Size = New Size(80, 23)
        nudServerPort.TabIndex = 1
        nudServerPort.Value = New Decimal(New Integer() {5080, 0, 0, 0})
        ' 
        ' btnServerStart
        ' 
        btnServerStart.Location = New Point(110, 37)
        btnServerStart.Name = "btnServerStart"
        btnServerStart.Size = New Size(110, 25)
        btnServerStart.TabIndex = 2
        btnServerStart.Text = "Start Server"
        ' 
        ' btnServerStop
        ' 
        btnServerStop.Enabled = False
        btnServerStop.Location = New Point(230, 37)
        btnServerStop.Name = "btnServerStop"
        btnServerStop.Size = New Size(110, 25)
        btnServerStop.TabIndex = 3
        btnServerStop.Text = "Stop Server"
        ' 
        ' btnServerRestart
        ' 
        btnServerRestart.Enabled = False
        btnServerRestart.Location = New Point(350, 37)
        btnServerRestart.Name = "btnServerRestart"
        btnServerRestart.Size = New Size(110, 25)
        btnServerRestart.TabIndex = 4
        btnServerRestart.Text = "Restart Server"
        ' 
        ' lblSubtitleBg
        ' 
        lblSubtitleBg.AutoSize = True
        lblSubtitleBg.Location = New Point(10, 70)
        lblSubtitleBg.Name = "lblSubtitleBg"
        lblSubtitleBg.Size = New Size(74, 15)
        lblSubtitleBg.TabIndex = 5
        lblSubtitleBg.Text = "Background:"
        ' 
        ' btnSubtitleBg
        ' 
        btnSubtitleBg.BackColor = Color.Black
        btnSubtitleBg.FlatAppearance.BorderColor = Color.Gray
        btnSubtitleBg.FlatStyle = FlatStyle.Flat
        btnSubtitleBg.Location = New Point(10, 86)
        btnSubtitleBg.Name = "btnSubtitleBg"
        btnSubtitleBg.Size = New Size(80, 23)
        btnSubtitleBg.TabIndex = 6
        btnSubtitleBg.UseVisualStyleBackColor = False
        ' 
        ' lblSubtitleFg
        ' 
        lblSubtitleFg.AutoSize = True
        lblSubtitleFg.Location = New Point(110, 70)
        lblSubtitleFg.Name = "lblSubtitleFg"
        lblSubtitleFg.Size = New Size(61, 15)
        lblSubtitleFg.TabIndex = 7
        lblSubtitleFg.Text = "Text color:"
        ' 
        ' btnSubtitleFg
        ' 
        btnSubtitleFg.BackColor = Color.White
        btnSubtitleFg.FlatAppearance.BorderColor = Color.Gray
        btnSubtitleFg.FlatStyle = FlatStyle.Flat
        btnSubtitleFg.Location = New Point(110, 86)
        btnSubtitleFg.Name = "btnSubtitleFg"
        btnSubtitleFg.Size = New Size(80, 23)
        btnSubtitleFg.TabIndex = 8
        btnSubtitleFg.UseVisualStyleBackColor = False
        ' 
        ' lblSubtitleFont
        ' 
        lblSubtitleFont.AutoSize = True
        lblSubtitleFont.Location = New Point(210, 70)
        lblSubtitleFont.Name = "lblSubtitleFont"
        lblSubtitleFont.Size = New Size(34, 15)
        lblSubtitleFont.TabIndex = 9
        lblSubtitleFont.Text = "Font:"
        ' 
        ' cboSubtitleFont
        ' 
        cboSubtitleFont.DropDownStyle = ComboBoxStyle.DropDownList
        cboSubtitleFont.Location = New Point(210, 86)
        cboSubtitleFont.Name = "cboSubtitleFont"
        cboSubtitleFont.Size = New Size(200, 23)
        cboSubtitleFont.TabIndex = 10
        ' 
        ' lblSubtitleSize
        ' 
        lblSubtitleSize.AutoSize = True
        lblSubtitleSize.Location = New Point(430, 70)
        lblSubtitleSize.Name = "lblSubtitleSize"
        lblSubtitleSize.Size = New Size(30, 15)
        lblSubtitleSize.TabIndex = 11
        lblSubtitleSize.Text = "Size:"
        ' 
        ' nudSubtitleSize
        ' 
        nudSubtitleSize.Location = New Point(430, 86)
        nudSubtitleSize.Maximum = New Decimal(New Integer() {72, 0, 0, 0})
        nudSubtitleSize.Minimum = New Decimal(New Integer() {8, 0, 0, 0})
        nudSubtitleSize.Name = "nudSubtitleSize"
        nudSubtitleSize.Size = New Size(55, 23)
        nudSubtitleSize.TabIndex = 12
        nudSubtitleSize.Value = New Decimal(New Integer() {12, 0, 0, 0})
        ' 
        ' chkSubtitleBold
        ' 
        chkSubtitleBold.AutoSize = True
        chkSubtitleBold.Location = New Point(500, 88)
        chkSubtitleBold.Name = "chkSubtitleBold"
        chkSubtitleBold.Size = New Size(50, 19)
        chkSubtitleBold.TabIndex = 13
        chkSubtitleBold.Text = "Bold"
        ' 
        ' lblAdminPin
        ' 
        lblAdminPin.AutoSize = True
        lblAdminPin.Location = New Point(10, 118)
        lblAdminPin.Name = "lblAdminPin"
        lblAdminPin.Size = New Size(68, 15)
        lblAdminPin.TabIndex = 14
        lblAdminPin.Text = "Admin PIN:"
        ' 
        ' txtAdminPin
        ' 
        txtAdminPin.Location = New Point(10, 134)
        txtAdminPin.MaxLength = 8
        txtAdminPin.Name = "txtAdminPin"
        txtAdminPin.Size = New Size(100, 23)
        txtAdminPin.TabIndex = 15
        ' 
        ' lblLiveServerPort
        ' 
        lblLiveServerPort.AutoSize = True
        lblLiveServerPort.Location = New Point(130, 118)
        lblLiveServerPort.Name = "lblLiveServerPort"
        lblLiveServerPort.Size = New Size(56, 15)
        lblLiveServerPort.TabIndex = 16
        lblLiveServerPort.Text = "Live Port:"
        ' 
        ' nudLiveServerPort
        ' 
        nudLiveServerPort.Location = New Point(130, 134)
        nudLiveServerPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        nudLiveServerPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        nudLiveServerPort.Name = "nudLiveServerPort"
        nudLiveServerPort.Size = New Size(80, 23)
        nudLiveServerPort.TabIndex = 17
        nudLiveServerPort.Value = New Decimal(New Integer() {5091, 0, 0, 0})
        ' 
        ' lblTranslationPort
        ' 
        lblTranslationPort.AutoSize = True
        lblTranslationPort.Location = New Point(230, 118)
        lblTranslationPort.Name = "lblTranslationPort"
        lblTranslationPort.Size = New Size(93, 15)
        lblTranslationPort.TabIndex = 18
        lblTranslationPort.Text = "Translation Port:"
        ' 
        ' nudTranslationPort
        ' 
        nudTranslationPort.Location = New Point(230, 134)
        nudTranslationPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        nudTranslationPort.Minimum = New Decimal(New Integer() {1024, 0, 0, 0})
        nudTranslationPort.Name = "nudTranslationPort"
        nudTranslationPort.Size = New Size(80, 23)
        nudTranslationPort.TabIndex = 19
        nudTranslationPort.Value = New Decimal(New Integer() {5090, 0, 0, 0})
        ' 
        ' lblTransDevice
        ' 
        lblTransDevice.AutoSize = True
        lblTransDevice.Location = New Point(330, 118)
        lblTransDevice.Name = "lblTransDevice"
        lblTransDevice.Size = New Size(45, 15)
        lblTransDevice.TabIndex = 20
        lblTransDevice.Text = "Device:"
        ' 
        ' cboTransDevice
        ' 
        cboTransDevice.DropDownStyle = ComboBoxStyle.DropDownList
        cboTransDevice.Items.AddRange(New Object() {"cuda", "cpu"})
        cboTransDevice.Location = New Point(330, 134)
        cboTransDevice.Name = "cboTransDevice"
        cboTransDevice.Size = New Size(90, 23)
        cboTransDevice.TabIndex = 21
        ' 
        ' lblTransUnload
        ' 
        lblTransUnload.AutoSize = True
        lblTransUnload.Location = New Point(440, 118)
        lblTransUnload.Name = "lblTransUnload"
        lblTransUnload.Size = New Size(80, 15)
        lblTransUnload.TabIndex = 22
        lblTransUnload.Text = "Unload (min):"
        ' 
        ' nudTransUnload
        ' 
        nudTransUnload.Location = New Point(440, 134)
        nudTransUnload.Maximum = New Decimal(New Integer() {1440, 0, 0, 0})
        nudTransUnload.Name = "nudTransUnload"
        nudTransUnload.Size = New Size(60, 23)
        nudTransUnload.TabIndex = 23
        nudTransUnload.Value = New Decimal(New Integer() {10, 0, 0, 0})
        ' 
        ' chkTransEnabled
        ' 
        chkTransEnabled.AutoSize = True
        chkTransEnabled.Location = New Point(520, 136)
        chkTransEnabled.Name = "chkTransEnabled"
        chkTransEnabled.Size = New Size(129, 19)
        chkTransEnabled.TabIndex = 24
        chkTransEnabled.Text = "Translation Enabled"
        ' 
        ' chkAllowFirewall
        ' 
        chkAllowFirewall.AutoSize = True
        chkAllowFirewall.Location = New Point(680, 136)
        chkAllowFirewall.Name = "chkAllowFirewall"
        chkAllowFirewall.Size = New Size(99, 19)
        chkAllowFirewall.TabIndex = 25
        chkAllowFirewall.Text = "Allow Firewall"
        ' 
        ' lblTtsBackends
        ' 
        lblTtsBackends.AutoSize = True
        lblTtsBackends.Location = New Point(10, 166)
        lblTtsBackends.Name = "lblTtsBackends"
        lblTtsBackends.Size = New Size(80, 15)
        lblTtsBackends.TabIndex = 26
        lblTtsBackends.Text = "TTS Backends"
        ' 
        ' txtTtsBackends
        ' 
        txtTtsBackends.Location = New Point(10, 182)
        txtTtsBackends.Name = "txtTtsBackends"
        txtTtsBackends.Size = New Size(300, 23)
        txtTtsBackends.TabIndex = 27
        ' 
        ' lblTtsHint
        ' 
        lblTtsHint.AutoSize = True
        lblTtsHint.ForeColor = Color.Gray
        lblTtsHint.Location = New Point(320, 184)
        lblTtsHint.Name = "lblTtsHint"
        lblTtsHint.Size = New Size(319, 15)
        lblTtsHint.TabIndex = 28
        lblTtsHint.Text = "(piper, mms-tts, edgetts — comma-separated, empty = all)"
        ' 
        ' btnSetupTranslation
        ' 
        btnSetupTranslation.Location = New Point(10, 216)
        btnSetupTranslation.Name = "btnSetupTranslation"
        btnSetupTranslation.Size = New Size(240, 28)
        btnSetupTranslation.TabIndex = 29
        btnSetupTranslation.Text = "Check Dependencies"
        ' 
        ' grpServerInfo
        ' 
        grpServerInfo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpServerInfo.Controls.Add(lblServerStatus)
        grpServerInfo.Controls.Add(lblServerUrl)
        grpServerInfo.Controls.Add(lblServerClients)
        grpServerInfo.Controls.Add(btnCopyUrl)
        grpServerInfo.Location = New Point(8, 256)
        grpServerInfo.Name = "grpServerInfo"
        grpServerInfo.Size = New Size(760, 100)
        grpServerInfo.TabIndex = 1
        grpServerInfo.TabStop = False
        grpServerInfo.Text = "Connection Info"
        ' 
        ' lblServerStatus
        ' 
        lblServerStatus.AutoSize = True
        lblServerStatus.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblServerStatus.Location = New Point(10, 25)
        lblServerStatus.Name = "lblServerStatus"
        lblServerStatus.Size = New Size(114, 19)
        lblServerStatus.TabIndex = 0
        lblServerStatus.Text = "Status: Stopped"
        ' 
        ' lblServerUrl
        ' 
        lblServerUrl.AutoSize = True
        lblServerUrl.Font = New Font("Consolas", 11F)
        lblServerUrl.Location = New Point(10, 50)
        lblServerUrl.Name = "lblServerUrl"
        lblServerUrl.Size = New Size(152, 18)
        lblServerUrl.TabIndex = 1
        lblServerUrl.Text = "URL: (not running)"
        ' 
        ' lblServerClients
        ' 
        lblServerClients.AutoSize = True
        lblServerClients.Location = New Point(10, 75)
        lblServerClients.Name = "lblServerClients"
        lblServerClients.Size = New Size(114, 15)
        lblServerClients.TabIndex = 2
        lblServerClients.Text = "Connected clients: 0"
        ' 
        ' btnCopyUrl
        ' 
        btnCopyUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnCopyUrl.Enabled = False
        btnCopyUrl.Location = New Point(700, 47)
        btnCopyUrl.Name = "btnCopyUrl"
        btnCopyUrl.Size = New Size(110, 25)
        btnCopyUrl.TabIndex = 3
        btnCopyUrl.Text = "Copy URL"
        ' 
        ' rtbServerLog
        ' 
        rtbServerLog.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        rtbServerLog.BackColor = Color.Black
        rtbServerLog.Font = New Font("Consolas", 10F)
        rtbServerLog.ForeColor = Color.FromArgb(CByte(0), CByte(200), CByte(255))
        rtbServerLog.Location = New Point(8, 362)
        rtbServerLog.Name = "rtbServerLog"
        rtbServerLog.ReadOnly = True
        rtbServerLog.ScrollBars = RichTextBoxScrollBars.Vertical
        rtbServerLog.Size = New Size(760, 248)
        rtbServerLog.TabIndex = 2
        rtbServerLog.Text = ""
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
        cboMode.Items.AddRange(New Object() {"Audio File -> Subtitles", "YouTube -> Audio Only", "YouTube -> Full Video", "YouTube -> Subtitles"})
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
        cboInputLanguage.DropDownStyle = ComboBoxStyle.DropDownList
        cboInputLanguage.Location = New Point(10, 86)
        cboInputLanguage.Name = "cboInputLanguage"
        cboInputLanguage.Size = New Size(150, 23)
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
        cboOutputLanguage.DropDownStyle = ComboBoxStyle.DropDownList
        cboOutputLanguage.Location = New Point(250, 86)
        cboOutputLanguage.Name = "cboOutputLanguage"
        cboOutputLanguage.Size = New Size(150, 23)
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
        pnlTransBottom.Size = New Size(776, 20)
        pnlTransBottom.TabIndex = 1
        '
        ' lblTransStatus
        '
        lblTransStatus.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        lblTransStatus.AutoSize = True
        lblTransStatus.ForeColor = Color.Gray
        lblTransStatus.Location = New Point(560, 10)
        lblTransStatus.Name = "lblTransStatus"
        lblTransStatus.Size = New Size(0, 15)
        lblTransStatus.TabIndex = 2
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
        ' 
        ' tabPageBibleWs
        ' 
        tabPageBibleWs.Controls.Add(wvBible)
        tabPageBibleWs.Controls.Add(lblBibleStatus)
        tabPageBibleWs.Location = New Point(4, 5)
        tabPageBibleWs.Name = "tabPageBibleWs"
        tabPageBibleWs.Size = New Size(792, 593)
        tabPageBibleWs.TabIndex = 5
        tabPageBibleWs.Text = "Bible"
        ' 
        ' wvBible
        ' 
        wvBible.AllowExternalDrop = True
        wvBible.CreationProperties = Nothing
        wvBible.DefaultBackgroundColor = Color.White
        wvBible.Dock = DockStyle.Fill
        wvBible.Location = New Point(0, 0)
        wvBible.Name = "wvBible"
        wvBible.Size = New Size(792, 593)
        wvBible.TabIndex = 0
        wvBible.Visible = False
        wvBible.ZoomFactor = 1R
        ' 
        ' lblBibleStatus
        ' 
        lblBibleStatus.Dock = DockStyle.Fill
        lblBibleStatus.Font = New Font("Segoe UI", 14F)
        lblBibleStatus.ForeColor = Color.Gray
        lblBibleStatus.Location = New Point(0, 0)
        lblBibleStatus.Name = "lblBibleStatus"
        lblBibleStatus.Size = New Size(112, 0)
        lblBibleStatus.TabIndex = 1
        lblBibleStatus.Text = "Waiting for server to start..."
        lblBibleStatus.TextAlign = ContentAlignment.MiddleCenter
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
        ' rtbUnifiedLog
        ' 
        rtbUnifiedLog.BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        rtbUnifiedLog.Dock = DockStyle.Fill
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
        ' pnlLogToolbar
        ' 
        pnlLogToolbar.Controls.Add(lblLogTitle)
        pnlLogToolbar.Controls.Add(cboLogFilter)
        pnlLogToolbar.Controls.Add(btnLogClear)
        pnlLogToolbar.Controls.Add(btnLogCopy)
        pnlLogToolbar.Controls.Add(txtLogSearch)
        pnlLogToolbar.Controls.Add(btnLogSearchNext)
        pnlLogToolbar.Dock = DockStyle.Top
        pnlLogToolbar.Location = New Point(0, 0)
        pnlLogToolbar.Name = "pnlLogToolbar"
        pnlLogToolbar.Size = New Size(880, 28)
        pnlLogToolbar.TabIndex = 1
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
        ' trayMenu
        ' 
        trayMenu.Items.AddRange(New ToolStripItem() {trayMenuAbout, traySep1, trayMenuShow, traySep2, trayMenuExit})
        trayMenu.Name = "trayMenu"
        trayMenu.Size = New Size(117, 82)
        ' 
        ' trayMenuAbout
        ' 
        trayMenuAbout.Name = "trayMenuAbout"
        trayMenuAbout.Size = New Size(116, 22)
        trayMenuAbout.Text = "About..."
        ' 
        ' traySep1
        ' 
        traySep1.Name = "traySep1"
        traySep1.Size = New Size(113, 6)
        ' 
        ' trayMenuShow
        ' 
        trayMenuShow.Name = "trayMenuShow"
        trayMenuShow.Size = New Size(116, 22)
        trayMenuShow.Text = "Show"
        ' 
        ' traySep2
        ' 
        traySep2.Name = "traySep2"
        traySep2.Size = New Size(113, 6)
        ' 
        ' trayMenuExit
        ' 
        trayMenuExit.Name = "trayMenuExit"
        trayMenuExit.Size = New Size(116, 22)
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
        tabPageLive.ResumeLayout(False)
        grpLiveInput.ResumeLayout(False)
        grpLiveInput.PerformLayout()
        pnlLiveButtons.ResumeLayout(False)
        pnlLiveButtons.PerformLayout()
        CType(trkMaxSegment, ComponentModel.ISupportInitialize).EndInit()
        CType(trkVadSilence, ComponentModel.ISupportInitialize).EndInit()
        pnlLiveOutput.ResumeLayout(False)
        CType(wvLiveClients, ComponentModel.ISupportInitialize).EndInit()
        tabPageServer.ResumeLayout(False)
        grpServerSettings.ResumeLayout(False)
        grpServerSettings.PerformLayout()
        CType(nudServerPort, ComponentModel.ISupportInitialize).EndInit()
        CType(nudSubtitleSize, ComponentModel.ISupportInitialize).EndInit()
        CType(nudLiveServerPort, ComponentModel.ISupportInitialize).EndInit()
        CType(nudTranslationPort, ComponentModel.ISupportInitialize).EndInit()
        CType(nudTransUnload, ComponentModel.ISupportInitialize).EndInit()
        grpServerInfo.ResumeLayout(False)
        grpServerInfo.PerformLayout()
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
        CType(wvBible, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents mnuToolsCheckDeps As ToolStripMenuItem
    Friend WithEvents mnuToolsVerifyPaths As ToolStripMenuItem
    Friend WithEvents mnuToolsVerifyIntegrity As ToolStripMenuItem
    Friend WithEvents mnuToolsSep3 As ToolStripSeparator
    Friend WithEvents mnuToolsPaths As ToolStripMenuItem
    Friend WithEvents mnuToolsServer As ToolStripMenuItem
    Friend WithEvents mnuToolsSep4 As ToolStripSeparator
    Friend WithEvents mnuToolsOptions As ToolStripMenuItem
    Friend WithEvents mnuSession As ToolStripMenuItem
    Friend WithEvents mnuSessionStart As ToolStripMenuItem
    Friend WithEvents mnuSessionStop As ToolStripMenuItem
    Friend WithEvents mnuSessionSep1 As ToolStripSeparator
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
    Friend WithEvents btnNavLive As ToolStripButton
    Friend WithEvents btnNavTranscribe As ToolStripButton
    Friend WithEvents btnNavTranslate As ToolStripButton
    Friend WithEvents btnNavBible As ToolStripButton

    ' Shell: Status bar
    Friend WithEvents statusMain As StatusStrip
    Friend WithEvents tslServerStatus As ToolStripStatusLabel
    Friend WithEvents tslClients As ToolStripStatusLabel
    Friend WithEvents tslSpring As ToolStripStatusLabel
    Friend WithEvents tslLiveStatus As ToolStripStatusLabel
    Friend WithEvents tslElapsed As ToolStripStatusLabel
    Friend WithEvents tslLogToggle As ToolStripStatusLabel
    Friend WithEvents liveElapsedTimer As Timer

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
    Friend WithEvents tabPageLive As TabPage
    Friend WithEvents tabPageHelp As TabPage
    Friend WithEvents tabPageServer As TabPage
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

    ' Tab: Live
    Friend WithEvents grpLiveInput As GroupBox
    Friend WithEvents lblLiveDevice As Label
    Friend WithEvents cboLiveDevice As ComboBox
    Friend WithEvents btnRefreshDevices As Button
    Friend WithEvents lblLiveInputLang As Label
    Friend WithEvents cboLiveInputLang As ComboBox
    Friend WithEvents btnEditFilters As Button
    Friend WithEvents pnlLiveButtons As Panel
    Friend WithEvents btnLiveStart As Button
    Friend WithEvents btnLiveStop As Button

    Friend WithEvents lblMaxSegment As Label
    Friend WithEvents trkMaxSegment As TrackBar
    Friend WithEvents lblMaxSegmentValue As Label
    Friend WithEvents lblVadSilence As Label
    Friend WithEvents trkVadSilence As TrackBar
    Friend WithEvents lblVadSilenceValue As Label
    Friend WithEvents btnTuneStats As Button
    Friend WithEvents pnlLiveOutput As Panel
    Friend WithEvents wvLiveClients As Microsoft.Web.WebView2.WinForms.WebView2

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
    Friend WithEvents wvBible As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents lblBibleStatus As Label

    ' Tab: Server
    Friend WithEvents grpServerSettings As GroupBox
    Friend WithEvents lblServerPort As Label
    Friend WithEvents nudServerPort As NumericUpDown
    Friend WithEvents btnServerStart As Button
    Friend WithEvents btnServerStop As Button
    Friend WithEvents btnServerRestart As Button
    Friend WithEvents grpServerInfo As GroupBox
    Friend WithEvents lblServerStatus As Label
    Friend WithEvents lblServerUrl As Label
    Friend WithEvents lblServerClients As Label
    Friend WithEvents btnCopyUrl As Button
    Friend WithEvents rtbServerLog As RichTextBox
    Friend WithEvents lblSubtitleBg As Label
    Friend WithEvents btnSubtitleBg As Button
    Friend WithEvents lblSubtitleFg As Label
    Friend WithEvents btnSubtitleFg As Button
    Friend WithEvents lblSubtitleFont As Label
    Friend WithEvents cboSubtitleFont As ComboBox
    Friend WithEvents lblSubtitleSize As Label
    Friend WithEvents nudSubtitleSize As NumericUpDown
    Friend WithEvents chkSubtitleBold As CheckBox
    Friend WithEvents lblAdminPin As Label
    Friend WithEvents txtAdminPin As TextBox
    Friend WithEvents btnSetupTranslation As Button
    Friend WithEvents lblLiveServerPort As Label
    Friend WithEvents nudLiveServerPort As NumericUpDown
    Friend WithEvents lblTranslationPort As Label
    Friend WithEvents nudTranslationPort As NumericUpDown
    Friend WithEvents lblTransDevice As Label
    Friend WithEvents cboTransDevice As ComboBox
    Friend WithEvents lblTransUnload As Label
    Friend WithEvents nudTransUnload As NumericUpDown
    Friend WithEvents chkTransEnabled As CheckBox
    Friend WithEvents chkAllowFirewall As CheckBox
    Friend WithEvents lblTtsBackends As Label
    Friend WithEvents txtTtsBackends As TextBox
    Friend WithEvents lblTtsHint As Label

    ' System Tray
    Friend WithEvents trayIcon As NotifyIcon
    Friend WithEvents trayMenu As ContextMenuStrip
    Friend WithEvents trayMenuShow As ToolStripMenuItem
    Friend WithEvents trayMenuAbout As ToolStripMenuItem
    Friend WithEvents trayMenuExit As ToolStripMenuItem
    Friend WithEvents traySep1 As ToolStripSeparator
    Friend WithEvents traySep2 As ToolStripSeparator
End Class
