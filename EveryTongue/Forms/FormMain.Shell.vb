' FormMain.Shell.vb — UI shell: menu bar, toolbar, nav rail, status bar
' Phase 1 of the UI redesign — wraps existing tab content in new navigation chrome.
' All existing functionality is preserved; tabs become hidden workspaces.

Imports System.Drawing

Partial Class FormMain

    ' ── Shell controls ──────────────────────────────────────────────
    Private menuMain As MenuStrip
    Private toolStripMain As ToolStrip
    Private pnlNavRail As Panel
    Private pnlContent As Panel
    Private statusMain As StatusStrip

    ' ── Nav rail ────────────────────────────────────────────────────
    Private btnNavLive As Button
    Private btnNavTranscribe As Button
    Private btnNavTranslate As Button
    Private btnNavBible As Button
    Private _activeNavButton As Button

    ' ── Toolbar buttons ─────────────────────────────────────────────
    Private tsbLive As ToolStripButton
    Private tsbTranscribe As ToolStripButton
    Private tsbTranslate As ToolStripButton
    Private tsbBible As ToolStripButton
    Private tsbQR As ToolStripButton
    Private tsbOptions As ToolStripButton

    ' ── Status bar labels ───────────────────────────────────────────
    Private tslServerStatus As ToolStripStatusLabel
    Private tslClients As ToolStripStatusLabel
    Private tslSpring As ToolStripStatusLabel
    Private tslLiveStatus As ToolStripStatusLabel

    ' ── Stub tab pages ──────────────────────────────────────────────
    Private tabPageTranslate As TabPage
    Private tabPageBibleWs As TabPage

    ' ── Menu items (kept as fields for localization/enable toggling) ─
    Private mnuFile As ToolStripMenuItem
    Private mnuFileNewSession As ToolStripMenuItem
    Private mnuFileExportDiag As ToolStripMenuItem
    Private mnuFileExit As ToolStripMenuItem

    Private mnuTools As ToolStripMenuItem
    Private mnuToolsTranscribe As ToolStripMenuItem
    Private mnuToolsTranslate As ToolStripMenuItem
    Private mnuToolsBible As ToolStripMenuItem
    Private mnuToolsGlossary As ToolStripMenuItem
    Private mnuToolsLocalization As ToolStripMenuItem
    Private mnuToolsDownloadMgr As ToolStripMenuItem
    Private mnuToolsCheckDeps As ToolStripMenuItem
    Private mnuToolsVerifyPaths As ToolStripMenuItem
    Private mnuToolsVerifyIntegrity As ToolStripMenuItem
    Private mnuToolsOptions As ToolStripMenuItem
    Private mnuToolsPaths As ToolStripMenuItem
    Private mnuToolsServer As ToolStripMenuItem

    Private mnuSession As ToolStripMenuItem
    Private mnuSessionStart As ToolStripMenuItem
    Private mnuSessionStop As ToolStripMenuItem
    Private mnuSessionQR As ToolStripMenuItem
    Private mnuSessionCopyUrl As ToolStripMenuItem

    Private mnuView As ToolStripMenuItem
    Private mnuViewLogPanel As ToolStripMenuItem
    Private mnuViewTheme As ToolStripMenuItem
    Private mnuViewThemeSystem As ToolStripMenuItem
    Private mnuViewThemeLight As ToolStripMenuItem
    Private mnuViewThemeDark As ToolStripMenuItem
    Private mnuViewFullScreen As ToolStripMenuItem

    Private mnuHelpMenu As ToolStripMenuItem
    Private mnuHelpQuickStart As ToolStripMenuItem
    Private mnuHelpShortcuts As ToolStripMenuItem
    Private mnuHelpHardware As ToolStripMenuItem
    Private mnuHelpSpecSheet As ToolStripMenuItem
    Private mnuHelpUpdates As ToolStripMenuItem
    Private mnuHelpAbout As ToolStripMenuItem

    ' ── Constants ───────────────────────────────────────────────────
    Private Const NavRailWidth As Integer = 80
    Private Shared ReadOnly NavBackColor As Color = Color.FromArgb(37, 37, 38)
    Private Shared ReadOnly NavForeColor As Color = Color.FromArgb(200, 200, 200)
    Private Shared ReadOnly NavSelectedColor As Color = Color.FromArgb(0, 122, 204)
    Private Shared ReadOnly NavHoverColor As Color = Color.FromArgb(51, 51, 52)
    Private Shared ReadOnly NavAccentBar As Color = Color.FromArgb(0, 122, 204)

    ' ═══════════════════════════════════════════════════════════════
    ' InitializeShell — called from Form_Load after InitializeComponent
    ' ═══════════════════════════════════════════════════════════════
    Private Sub InitializeShell()
        Me.SuspendLayout()

        ' ── Create stub tab pages ──────────────────────────────────
        tabPageTranslate = New TabPage() With {
            .Text = "Translate", .Padding = New Padding(8)}
        Dim lblTransStub As New Label() With {
            .Text = "Text Translation" & vbCrLf & vbCrLf &
                    "Coming soon",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.Gray}
        tabPageTranslate.Controls.Add(lblTransStub)

        tabPageBibleWs = New TabPage() With {
            .Text = "Bible", .Padding = New Padding(8)}
        Dim lblBibleStub As New Label() With {
            .Text = "Bible" & vbCrLf & vbCrLf &
                    "Coming soon",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.Gray}
        tabPageBibleWs.Controls.Add(lblBibleStub)

        tabMain.TabPages.Add(tabPageTranslate)
        tabMain.TabPages.Add(tabPageBibleWs)

        ' ── Hide tab headers ───────────────────────────────────────
        tabMain.Appearance = TabAppearance.FlatButtons
        tabMain.ItemSize = New Size(0, 1)
        tabMain.SizeMode = TabSizeMode.Fixed
        tabMain.Dock = DockStyle.Fill

        ' ── Build shell components ─────────────────────────────────
        BuildMenuBar()
        BuildToolbar()
        BuildNavRail()
        BuildStatusBar()

        ' ── Restructure form layout ────────────────────────────────
        Me.Controls.Remove(tabMain)

        pnlContent = New Panel()
        pnlContent.Dock = DockStyle.Fill
        pnlContent.Controls.Add(tabMain)      ' Fill — innermost
        pnlContent.Controls.Add(pnlNavRail)   ' Left — beside tabMain

        Me.Controls.Add(pnlContent)
        Me.Controls.Add(statusMain)
        Me.Controls.Add(toolStripMain)
        Me.Controls.Add(menuMain)
        Me.MainMenuStrip = menuMain

        ' ── Default to Live workspace ──────────────────────────────
        SwitchWorkspace(tabPageLive, btnNavLive)

        Me.ResumeLayout(True)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Menu Bar
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildMenuBar()
        menuMain = New MenuStrip()
        menuMain.RenderMode = ToolStripRenderMode.Professional

        ' ── File ───────────────────────────────────────────────────
        mnuFile = New ToolStripMenuItem("&File")

        mnuFileNewSession = New ToolStripMenuItem("New Session...")
        mnuFileNewSession.ShortcutKeys = Keys.Control Or Keys.N
        mnuFileNewSession.Enabled = False
        AddHandler mnuFileNewSession.Click, Sub(s, e)
                                                 MessageBox.Show("Session Wizard coming soon.", "New Session",
                                                     MessageBoxButtons.OK, MessageBoxIcon.Information)
                                             End Sub

        mnuFileExportDiag = New ToolStripMenuItem("Export Diagnostics...")
        mnuFileExportDiag.Enabled = False

        mnuFileExit = New ToolStripMenuItem("E&xit")
        mnuFileExit.ShortcutKeys = Keys.Alt Or Keys.F4
        AddHandler mnuFileExit.Click, Sub(s, e) ExitApplication()

        mnuFile.DropDownItems.AddRange({
            mnuFileNewSession,
            New ToolStripSeparator(),
            mnuFileExportDiag,
            New ToolStripSeparator(),
            mnuFileExit})

        ' ── Tools ──────────────────────────────────────────────────
        mnuTools = New ToolStripMenuItem("&Tools")

        mnuToolsTranscribe = New ToolStripMenuItem("Transcribe File/URL...")
        AddHandler mnuToolsTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)

        mnuToolsTranslate = New ToolStripMenuItem("Translate Text...")
        AddHandler mnuToolsTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)

        mnuToolsBible = New ToolStripMenuItem("Bible Lookup...")
        AddHandler mnuToolsBible.Click, Sub(s, e) SwitchWorkspace(tabPageBibleWs, btnNavBible)

        mnuToolsGlossary = New ToolStripMenuItem("Glossary Editor...")
        AddHandler mnuToolsGlossary.Click, Sub(s, e)
                                                Using dlg As New FormFilterEditor(AppDomain.CurrentDomain.BaseDirectory, _config.LiveServerPort, _config.TranslationPort, _resMgr)
                                                    dlg.ShowDialog(Me)
                                                End Using
                                            End Sub

        mnuToolsLocalization = New ToolStripMenuItem("Localization Editor...")
        mnuToolsLocalization.Enabled = False

        mnuToolsDownloadMgr = New ToolStripMenuItem("Download Manager")
        AddHandler mnuToolsDownloadMgr.Click, Sub(s, e) btnDownloadManager.PerformClick()

        mnuToolsCheckDeps = New ToolStripMenuItem("Check Dependencies")
        AddHandler mnuToolsCheckDeps.Click, Sub(s, e) CheckDependenciesAsync()

        mnuToolsVerifyPaths = New ToolStripMenuItem("Verify Paths")
        AddHandler mnuToolsVerifyPaths.Click, Sub(s, e) btnVerifyPaths.PerformClick()

        mnuToolsVerifyIntegrity = New ToolStripMenuItem("Verify File Integrity")
        mnuToolsVerifyIntegrity.Enabled = False

        mnuToolsPaths = New ToolStripMenuItem("Tool Paths...")
        AddHandler mnuToolsPaths.Click, Sub(s, e) ShowOptionsDialog("paths")

        mnuToolsServer = New ToolStripMenuItem("Server Settings...")
        AddHandler mnuToolsServer.Click, Sub(s, e) ShowOptionsDialog("server")

        mnuToolsOptions = New ToolStripMenuItem("&Options...")
        mnuToolsOptions.ShortcutKeys = Keys.Control Or Keys.Oemcomma
        AddHandler mnuToolsOptions.Click, Sub(s, e) ShowOptionsDialog("general")

        mnuTools.DropDownItems.AddRange({
            mnuToolsTranscribe, mnuToolsTranslate, mnuToolsBible,
            New ToolStripSeparator(),
            mnuToolsGlossary, mnuToolsLocalization,
            New ToolStripSeparator(),
            mnuToolsDownloadMgr, mnuToolsCheckDeps, mnuToolsVerifyPaths,
            mnuToolsVerifyIntegrity,
            New ToolStripSeparator(),
            mnuToolsPaths, mnuToolsServer,
            New ToolStripSeparator(),
            mnuToolsOptions})

        ' ── Session ────────────────────────────────────────────────
        mnuSession = New ToolStripMenuItem("&Session")

        mnuSessionStart = New ToolStripMenuItem("Start Live")
        mnuSessionStart.ShortcutKeys = Keys.F5
        AddHandler mnuSessionStart.Click, Sub(s, e)
                                               SwitchWorkspace(tabPageLive, btnNavLive)
                                               btnLiveStart.PerformClick()
                                           End Sub

        mnuSessionStop = New ToolStripMenuItem("Stop Live")
        mnuSessionStop.ShortcutKeys = Keys.Shift Or Keys.F5
        AddHandler mnuSessionStop.Click, Sub(s, e) btnLiveStop.PerformClick()

        mnuSessionQR = New ToolStripMenuItem("Show QR Code")
        mnuSessionQR.Enabled = False

        mnuSessionCopyUrl = New ToolStripMenuItem("Copy Phone URL")
        AddHandler mnuSessionCopyUrl.Click, Sub(s, e) btnCopyUrl.PerformClick()

        mnuSession.DropDownItems.AddRange({
            mnuSessionStart, mnuSessionStop,
            New ToolStripSeparator(),
            mnuSessionQR, mnuSessionCopyUrl})

        ' ── View ───────────────────────────────────────────────────
        mnuView = New ToolStripMenuItem("&View")

        mnuViewLogPanel = New ToolStripMenuItem("Show Log Panel")
        mnuViewLogPanel.Enabled = False

        mnuViewTheme = New ToolStripMenuItem("Theme")
        mnuViewThemeSystem = New ToolStripMenuItem("System")
        mnuViewThemeLight = New ToolStripMenuItem("Light")
        mnuViewThemeDark = New ToolStripMenuItem("Dark")
        AddHandler mnuViewThemeSystem.Click, Sub(s, e) SetThemeFromMenu("System")
        AddHandler mnuViewThemeLight.Click, Sub(s, e) SetThemeFromMenu("Light")
        AddHandler mnuViewThemeDark.Click, Sub(s, e) SetThemeFromMenu("Dark")
        mnuViewTheme.DropDownItems.AddRange({mnuViewThemeSystem, mnuViewThemeLight, mnuViewThemeDark})

        mnuViewFullScreen = New ToolStripMenuItem("Full Screen")
        mnuViewFullScreen.ShortcutKeys = Keys.F11
        AddHandler mnuViewFullScreen.Click, Sub(s, e) ToggleFullScreen()

        mnuView.DropDownItems.AddRange({
            mnuViewLogPanel,
            New ToolStripSeparator(),
            mnuViewTheme,
            New ToolStripSeparator(),
            mnuViewFullScreen})

        ' ── Help ───────────────────────────────────────────────────
        mnuHelpMenu = New ToolStripMenuItem("&Help")

        mnuHelpQuickStart = New ToolStripMenuItem("Quick Start Guide")
        AddHandler mnuHelpQuickStart.Click, Sub(s, e) ShowLegacyTab(tabPageHelp)

        mnuHelpShortcuts = New ToolStripMenuItem("Keyboard Shortcuts")
        mnuHelpShortcuts.Enabled = False

        mnuHelpHardware = New ToolStripMenuItem("Hardware Report")
        mnuHelpHardware.Enabled = False

        mnuHelpSpecSheet = New ToolStripMenuItem("Generate Spec Sheet...")
        mnuHelpSpecSheet.Enabled = False

        mnuHelpUpdates = New ToolStripMenuItem("Check for Updates")
        AddHandler mnuHelpUpdates.Click, Sub(s, e) CheckForUpdatesAsync()

        mnuHelpAbout = New ToolStripMenuItem("About Every Tongue")
        AddHandler mnuHelpAbout.Click, Sub(s, e)
                                            Using dlg As New FormAbout()
                                                dlg.ShowDialog(Me)
                                            End Using
                                        End Sub

        mnuHelpMenu.DropDownItems.AddRange({
            mnuHelpQuickStart, mnuHelpShortcuts,
            New ToolStripSeparator(),
            mnuHelpHardware, mnuHelpSpecSheet,
            New ToolStripSeparator(),
            mnuHelpUpdates, mnuHelpAbout})

        menuMain.Items.AddRange({mnuFile, mnuTools, mnuSession, mnuView, mnuHelpMenu})
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Toolbar
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildToolbar()
        toolStripMain = New ToolStrip()
        toolStripMain.GripStyle = ToolStripGripStyle.Hidden
        toolStripMain.RenderMode = ToolStripRenderMode.Professional

        tsbLive = New ToolStripButton("Live") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Live Translation"}
        AddHandler tsbLive.Click, Sub(s, e) SwitchWorkspace(tabPageLive, btnNavLive)

        tsbTranscribe = New ToolStripButton("Transcribe") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Transcribe File/URL"}
        AddHandler tsbTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)

        tsbTranslate = New ToolStripButton("Translate") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Translate Text"}
        AddHandler tsbTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)

        tsbBible = New ToolStripButton("Bible") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Bible"}
        AddHandler tsbBible.Click, Sub(s, e) SwitchWorkspace(tabPageBibleWs, btnNavBible)

        tsbQR = New ToolStripButton("QR Code") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Show QR Code (coming soon)",
            .Enabled = False}

        tsbOptions = New ToolStripButton("Options") With {
            .DisplayStyle = ToolStripItemDisplayStyle.Text,
            .ToolTipText = "Options (Ctrl+,)"}
        AddHandler tsbOptions.Click, Sub(s, e) ShowOptionsDialog("general")

        toolStripMain.Items.AddRange({
            tsbLive, tsbTranscribe, tsbTranslate, tsbBible,
            New ToolStripSeparator(),
            tsbQR, tsbOptions})
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Nav Rail
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildNavRail()
        pnlNavRail = New Panel() With {
            .Width = NavRailWidth,
            .Dock = DockStyle.Left,
            .BackColor = NavBackColor}

        ' Add buttons in reverse order (Dock=Top stacks first-added at top)
        btnNavBible = CreateNavButton(ChrW(&HE736), "Bible")
        AddHandler btnNavBible.Click, Sub(s, e) SwitchWorkspace(tabPageBibleWs, btnNavBible)

        btnNavTranslate = CreateNavButton("Aa", "Translate")
        AddHandler btnNavTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)

        btnNavTranscribe = CreateNavButton(ChrW(&H266A), "Transcribe")
        AddHandler btnNavTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)

        btnNavLive = CreateNavButton(ChrW(&H25B6), "Live")
        AddHandler btnNavLive.Click, Sub(s, e) SwitchWorkspace(tabPageLive, btnNavLive)
    End Sub

    Private Function CreateNavButton(icon As String, label As String) As Button
        Dim btn As New Button()
        btn.FlatStyle = FlatStyle.Flat
        btn.FlatAppearance.BorderSize = 0
        btn.FlatAppearance.MouseOverBackColor = NavHoverColor
        btn.FlatAppearance.MouseDownBackColor = NavSelectedColor
        btn.Size = New Size(NavRailWidth, 65)
        btn.Dock = DockStyle.Top
        btn.Text = icon & vbCrLf & label
        btn.TextAlign = ContentAlignment.MiddleCenter
        btn.Font = New Font("Segoe UI", 9)
        btn.ForeColor = NavForeColor
        btn.BackColor = NavBackColor
        btn.Cursor = Cursors.Hand
        btn.Margin = New Padding(0)
        btn.Padding = New Padding(0)
        pnlNavRail.Controls.Add(btn)
        Return btn
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Status Bar
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStatusBar()
        statusMain = New StatusStrip()

        tslServerStatus = New ToolStripStatusLabel("Server: Stopped") With {
            .BorderSides = ToolStripStatusLabelBorderSides.Right}
        tslClients = New ToolStripStatusLabel("Clients: 0") With {
            .BorderSides = ToolStripStatusLabelBorderSides.Right}
        tslSpring = New ToolStripStatusLabel("") With {
            .Spring = True}
        tslLiveStatus = New ToolStripStatusLabel("Ready")

        statusMain.Items.AddRange({tslServerStatus, tslClients, tslSpring, tslLiveStatus})
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Workspace Switching
    ' ═══════════════════════════════════════════════════════════════
    Private Sub SwitchWorkspace(tabPage As TabPage, navButton As Button)
        ' Update tab
        If tabMain.SelectedTab IsNot tabPage Then
            tabMain.SelectedTab = tabPage
        End If

        ' Update nav rail selection
        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = NavBackColor
            _activeNavButton.ForeColor = NavForeColor
        End If

        navButton.BackColor = NavSelectedColor
        navButton.ForeColor = Color.White
        _activeNavButton = navButton

        ' Update toolbar toggle state
        tsbLive.Checked = (navButton Is btnNavLive)
        tsbTranscribe.Checked = (navButton Is btnNavTranscribe)
        tsbTranslate.Checked = (navButton Is btnNavTranslate)
        tsbBible.Checked = (navButton Is btnNavBible)
    End Sub

    ''' <summary>
    ''' Temporarily show a legacy tab (Paths, Settings, Server, Help)
    ''' that doesn't have a nav rail button yet.
    ''' </summary>
    Private Sub ShowLegacyTab(tabPage As TabPage)
        ' Deselect nav rail
        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = NavBackColor
            _activeNavButton.ForeColor = NavForeColor
            _activeNavButton = Nothing
        End If

        ' Clear toolbar toggle
        tsbLive.Checked = False
        tsbTranscribe.Checked = False
        tsbTranslate.Checked = False
        tsbBible.Checked = False

        tabMain.SelectedTab = tabPage
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Shell helpers
    ' ═══════════════════════════════════════════════════════════════

    Private Sub ShowOptionsDialog(Optional category As String = "general")
        Using dlg As New FormOptions(_config, _uiLocales)
            dlg.SelectCategory(category)
            If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.ConfigChanged Then
                ' Re-apply settings that affect the running UI
                LoadConfigToUi()
                ApplyTheme(_config.Theme)
                If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()
            End If
        End Using
    End Sub

    Private Sub SetThemeFromMenu(theme As String)
        cboTheme.SelectedItem = theme
        ' cboTheme change handler already calls ApplyTheme and saves config
    End Sub

    Private _isFullScreen As Boolean = False
    Private _previousWindowState As FormWindowState
    Private _previousBorderStyle As FormBorderStyle

    Private Sub ToggleFullScreen()
        If _isFullScreen Then
            Me.FormBorderStyle = _previousBorderStyle
            Me.WindowState = _previousWindowState
            menuMain.Visible = True
            toolStripMain.Visible = True
            statusMain.Visible = True
            _isFullScreen = False
        Else
            _previousWindowState = Me.WindowState
            _previousBorderStyle = Me.FormBorderStyle
            Me.WindowState = FormWindowState.Normal
            Me.FormBorderStyle = FormBorderStyle.None
            Me.WindowState = FormWindowState.Maximized
            menuMain.Visible = False
            toolStripMain.Visible = False
            statusMain.Visible = False
            _isFullScreen = True
        End If
    End Sub

    ''' <summary>
    ''' Updates the status bar with current server and live session state.
    ''' Called from existing status update points.
    ''' </summary>
    Private Sub UpdateShellStatus()
        If statusMain Is Nothing Then Return

        ' Server status
        If _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning Then
            tslServerStatus.Text = $"Server: Running :{_serverPort}"
            tslServerStatus.ForeColor = Color.Green
        Else
            tslServerStatus.Text = "Server: Stopped"
            tslServerStatus.ForeColor = Color.Gray
        End If

        ' Client count
        Dim svc = SubtitleSvc
        Dim clients = If(svc?.ConnectedClients, 0)
        tslClients.Text = $"Clients: {clients}"

        ' Live status
        If _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning Then
            tslLiveStatus.Text = "Live: Running"
            tslLiveStatus.ForeColor = Color.Green
        Else
            tslLiveStatus.Text = "Ready"
            tslLiveStatus.ForeColor = Color.Gray
        End If
    End Sub

    ''' <summary>
    ''' Applies shell-specific theming to the nav rail and status bar.
    ''' Called after the base ApplyTheme runs.
    ''' </summary>
    Private Sub ApplyShellTheme(theme As String)
        If pnlNavRail Is Nothing Then Return

        Select Case theme.ToLower()
            Case "light"
                pnlNavRail.BackColor = Color.FromArgb(240, 240, 240)
                For Each ctrl As Control In pnlNavRail.Controls
                    If TypeOf ctrl Is Button Then
                        Dim btn = DirectCast(ctrl, Button)
                        btn.ForeColor = Color.FromArgb(60, 60, 60)
                        If btn IsNot _activeNavButton Then
                            btn.BackColor = pnlNavRail.BackColor
                        End If
                        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220)
                    End If
                Next
                If _activeNavButton IsNot Nothing Then
                    _activeNavButton.BackColor = Color.FromArgb(0, 122, 204)
                    _activeNavButton.ForeColor = Color.White
                End If
            Case "dark"
                pnlNavRail.BackColor = NavBackColor
                For Each ctrl As Control In pnlNavRail.Controls
                    If TypeOf ctrl Is Button Then
                        Dim btn = DirectCast(ctrl, Button)
                        btn.ForeColor = NavForeColor
                        If btn IsNot _activeNavButton Then
                            btn.BackColor = NavBackColor
                        End If
                        btn.FlatAppearance.MouseOverBackColor = NavHoverColor
                    End If
                Next
                If _activeNavButton IsNot Nothing Then
                    _activeNavButton.BackColor = NavSelectedColor
                    _activeNavButton.ForeColor = Color.White
                End If
            Case Else ' System
                pnlNavRail.BackColor = SystemColors.Control
                For Each ctrl As Control In pnlNavRail.Controls
                    If TypeOf ctrl Is Button Then
                        Dim btn = DirectCast(ctrl, Button)
                        btn.ForeColor = SystemColors.ControlText
                        If btn IsNot _activeNavButton Then
                            btn.BackColor = SystemColors.Control
                        End If
                        btn.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight
                    End If
                Next
                If _activeNavButton IsNot Nothing Then
                    _activeNavButton.BackColor = Color.FromArgb(0, 122, 204)
                    _activeNavButton.ForeColor = Color.White
                End If
        End Select

        ' Update theme menu checkmarks
        If mnuViewThemeSystem IsNot Nothing Then
            mnuViewThemeSystem.Checked = theme.Equals("System", StringComparison.OrdinalIgnoreCase)
            mnuViewThemeLight.Checked = theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
            mnuViewThemeDark.Checked = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
        End If
    End Sub

End Class
