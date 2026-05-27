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

    ' ── Workspace tab pages ──────────────────────────────────────────
    Private tabPageTranslate As TabPage
    Private tabPageBibleWs As TabPage

    ' ── Translate workspace controls ──────────────────────────────
    Private cboTransSource As ComboBox
    Private cboTransTarget As ComboBox
    Private txtTransInput As TextBox
    Private txtTransOutput As TextBox
    Private btnTranslate As Button
    Private btnTransSwap As Button
    Private btnTransCopy As Button
    Private btnTransClear As Button
    Private lblTransStatus As Label

    ' ── Bible workspace controls ──────────────────────────────────
    Private wvBible As Microsoft.Web.WebView2.WinForms.WebView2
    Private lblBibleStatus As Label

    ' ── QR Code window ────────────────────────────────────────────
    Private _formQr As FormQrCode

    ' ── Log panel ─────────────────────────────────────────────────
    Private pnlLogPanel As Panel
    Private splitterLog As Splitter
    Private rtbUnifiedLog As RichTextBox
    Private cboLogFilter As ComboBox
    Private btnLogClear As Button
    Private btnLogCopy As Button
    Private _logPanelVisible As Boolean = False
    Private ReadOnly _unifiedLogBuffer As New System.Collections.Concurrent.ConcurrentQueue(Of (Source As String, Text As String, Color As Drawing.Color))
    Private _unifiedLogPending As Integer = 0
    Private Const UnifiedLogMaxLines As Integer = 3000

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

        ' ── Create workspace tab pages ────────────────────────────
        BuildTranslateWorkspace()
        BuildBibleWorkspace()
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
        BuildLogPanel()

        ' ── Restructure form layout ────────────────────────────────
        Me.Controls.Remove(tabMain)

        pnlContent = New Panel()
        pnlContent.Dock = DockStyle.Fill
        pnlContent.Controls.Add(tabMain)      ' Fill — innermost
        pnlContent.Controls.Add(pnlNavRail)   ' Left — beside tabMain
        pnlContent.Controls.Add(splitterLog)   ' Bottom splitter (hidden)
        pnlContent.Controls.Add(pnlLogPanel)   ' Bottom log panel (hidden)

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
        AddHandler mnuFileNewSession.Click, Sub(s, e) LaunchSessionWizard()

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
        AddHandler mnuSessionQR.Click, Sub(s, e) ShowQrCode()

        mnuSessionCopyUrl = New ToolStripMenuItem("Copy Phone URL")
        AddHandler mnuSessionCopyUrl.Click, Sub(s, e) btnCopyUrl.PerformClick()

        mnuSession.DropDownItems.AddRange({
            mnuSessionStart, mnuSessionStop,
            New ToolStripSeparator(),
            mnuSessionQR, mnuSessionCopyUrl})

        ' ── View ───────────────────────────────────────────────────
        mnuView = New ToolStripMenuItem("&View")

        mnuViewLogPanel = New ToolStripMenuItem("Show Log Panel")
        mnuViewLogPanel.ShortcutKeys = Keys.Control Or Keys.L
        AddHandler mnuViewLogPanel.Click, Sub(s, e) ToggleLogPanel()

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
            .ToolTipText = "Show QR Code"}
        AddHandler tsbQR.Click, Sub(s, e) ShowQrCode()

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
    ' Translate Workspace
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildTranslateWorkspace()
        tabPageTranslate = New TabPage() With {.Text = "Translate", .Padding = New Padding(8)}

        ' ── Top bar: language selectors ───────────────────────────
        Dim pnlTop As New Panel() With {
            .Dock = DockStyle.Top, .Height = 45}

        Dim lblFrom As New Label() With {
            .Text = "From:", .Location = New Drawing.Point(0, 4), .AutoSize = True,
            .Font = New Font("Segoe UI", 10)}

        cboTransSource = New ComboBox() With {
            .Location = New Drawing.Point(50, 1), .Size = New Drawing.Size(200, 24),
            .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            .AutoCompleteSource = AutoCompleteSource.ListItems}

        btnTransSwap = New Button() With {
            .Text = ChrW(&H21C4), .Location = New Drawing.Point(260, 0),
            .Size = New Drawing.Size(34, 26), .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Segoe UI", 12)}
        btnTransSwap.FlatAppearance.BorderSize = 0
        AddHandler btnTransSwap.Click, Sub(s, e) SwapTranslateLanguages()

        Dim lblTo As New Label() With {
            .Text = "To:", .Location = New Drawing.Point(304, 4), .AutoSize = True,
            .Font = New Font("Segoe UI", 10)}

        cboTransTarget = New ComboBox() With {
            .Location = New Drawing.Point(334, 1), .Size = New Drawing.Size(200, 24),
            .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            .AutoCompleteSource = AutoCompleteSource.ListItems}

        btnTranslate = New Button() With {
            .Text = "Translate", .Location = New Drawing.Point(548, 0),
            .Size = New Drawing.Size(90, 26),
            .Font = New Font("Segoe UI", 9, Drawing.FontStyle.Bold)}
        AddHandler btnTranslate.Click, Sub(s, e) RunTranslateAsync()

        pnlTop.Controls.AddRange({lblFrom, cboTransSource, btnTransSwap, lblTo, cboTransTarget, btnTranslate})

        ' ── Middle: split input/output ────────────────────────────
        Dim split As New SplitContainer() With {
            .Dock = DockStyle.Fill,
            .Orientation = Orientation.Vertical,
            .SplitterWidth = 6}

        txtTransInput = New TextBox() With {
            .Dock = DockStyle.Fill, .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Font = New Font("Segoe UI", 11),
            .AcceptsReturn = True}

        txtTransOutput = New TextBox() With {
            .Dock = DockStyle.Fill, .Multiline = True,
            .ScrollBars = ScrollBars.Vertical,
            .Font = New Font("Segoe UI", 11),
            .[ReadOnly] = True}

        split.Panel1.Controls.Add(txtTransInput)
        split.Panel2.Controls.Add(txtTransOutput)

        ' ── Bottom bar: copy / clear / status ─────────────────────
        Dim pnlBottom As New Panel() With {
            .Dock = DockStyle.Bottom, .Height = 34}

        btnTransCopy = New Button() With {
            .Text = "Copy", .Location = New Drawing.Point(0, 4),
            .Size = New Drawing.Size(70, 26)}
        AddHandler btnTransCopy.Click, Sub(s, e)
                                            If Not String.IsNullOrEmpty(txtTransOutput.Text) Then
                                                Clipboard.SetText(txtTransOutput.Text)
                                            End If
                                        End Sub

        btnTransClear = New Button() With {
            .Text = "Clear", .Location = New Drawing.Point(78, 4),
            .Size = New Drawing.Size(70, 26)}
        AddHandler btnTransClear.Click, Sub(s, e)
                                             txtTransInput.Clear()
                                             txtTransOutput.Clear()
                                         End Sub

        lblTransStatus = New Label() With {
            .Text = "", .Location = New Drawing.Point(160, 8),
            .AutoSize = True, .ForeColor = Color.Gray}

        pnlBottom.Controls.AddRange({btnTransCopy, btnTransClear, lblTransStatus})

        ' ── Populate language dropdowns ───────────────────────────
        For Each lang In _whisperLanguages
            If lang = "auto" Then Continue For
            Dim display = LangDisplayName(lang)
            cboTransSource.Items.Add(display)
            cboTransTarget.Items.Add(display)
        Next
        ' Default: Auto Detect source, English target
        cboTransSource.Items.Insert(0, LangDisplayName("auto"))
        cboTransSource.SelectedIndex = 0
        For i = 0 To cboTransTarget.Items.Count - 1
            If cboTransTarget.Items(i).ToString().StartsWith("English") Then
                cboTransTarget.SelectedIndex = i
                Exit For
            End If
        Next

        ' ── Assemble ──────────────────────────────────────────────
        tabPageTranslate.Controls.Add(split)      ' Fill
        tabPageTranslate.Controls.Add(pnlBottom)  ' Bottom
        tabPageTranslate.Controls.Add(pnlTop)     ' Top
    End Sub

    Private Sub SwapTranslateLanguages()
        If cboTransSource.SelectedIndex < 0 OrElse cboTransTarget.SelectedIndex < 0 Then Return
        Dim srcText = cboTransSource.Text
        Dim tgtText = cboTransTarget.Text
        ' Don't swap if source is "Auto Detect"
        If srcText.StartsWith("Auto") Then Return
        cboTransSource.Text = tgtText
        cboTransTarget.Text = srcText
        ' Also swap the text content
        Dim tmp = txtTransInput.Text
        txtTransInput.Text = txtTransOutput.Text
        txtTransOutput.Text = tmp
    End Sub

    Private Async Sub RunTranslateAsync()
        Dim inputText = txtTransInput.Text.Trim()
        If String.IsNullOrEmpty(inputText) Then Return

        Dim sourceLang = LangCodeFromDisplay(cboTransSource.Text)
        Dim targetLang = LangCodeFromDisplay(cboTransTarget.Text)

        btnTranslate.Enabled = False
        lblTransStatus.Text = "Translating..."
        lblTransStatus.ForeColor = Color.FromArgb(0, 122, 204)
        txtTransOutput.Text = ""

        Try
            Dim port = _config.TranslationPort
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(30)
                Dim url = $"http://127.0.0.1:{port}/translate"

                Dim bodyObj As New Dictionary(Of String, Object) From {
                    {"text", inputText},
                    {"source_lang", sourceLang},
                    {"target_langs", New String() {targetLang}}
                }
                Dim bodyJson = System.Text.Json.JsonSerializer.Serialize(bodyObj)
                Dim content As New System.Net.Http.StringContent(
                    bodyJson, System.Text.Encoding.UTF8, "application/json")

                Dim response = Await client.PostAsync(url, content)
                If response.IsSuccessStatusCode Then
                    Dim json = Await response.Content.ReadAsStringAsync()
                    Dim doc = System.Text.Json.JsonDocument.Parse(json)
                    Dim root = doc.RootElement

                    Dim translationsEl As System.Text.Json.JsonElement
                    Dim resultEl As System.Text.Json.JsonElement
                    If root.TryGetProperty("translations", translationsEl) Then
                        If translationsEl.TryGetProperty(targetLang, resultEl) Then
                            txtTransOutput.Text = resultEl.GetString()
                        End If
                    End If
                    lblTransStatus.Text = "Done"
                    lblTransStatus.ForeColor = Color.Green
                Else
                    lblTransStatus.Text = $"Error: {response.StatusCode}"
                    lblTransStatus.ForeColor = Color.Red
                End If
            End Using
        Catch ex As System.Net.Http.HttpRequestException
            lblTransStatus.Text = "Translation server not running"
            lblTransStatus.ForeColor = Color.Red
        Catch ex As TaskCanceledException
            lblTransStatus.Text = "Request timed out"
            lblTransStatus.ForeColor = Color.Red
        Catch ex As Exception
            lblTransStatus.Text = $"Error: {ex.Message}"
            lblTransStatus.ForeColor = Color.Red
        Finally
            btnTranslate.Enabled = True
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Bible Workspace
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildBibleWorkspace()
        tabPageBibleWs = New TabPage() With {.Text = "Bible", .Padding = New Padding(0)}

        ' Status label shown when server isn't running
        lblBibleStatus = New Label() With {
            .Text = "Waiting for server to start...",
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI", 14),
            .ForeColor = Color.Gray}

        ' WebView2 for the Bible UI
        wvBible = New Microsoft.Web.WebView2.WinForms.WebView2()
        CType(wvBible, ComponentModel.ISupportInitialize).BeginInit()
        wvBible.Dock = DockStyle.Fill
        wvBible.Visible = False
        CType(wvBible, ComponentModel.ISupportInitialize).EndInit()

        tabPageBibleWs.Controls.Add(wvBible)
        tabPageBibleWs.Controls.Add(lblBibleStatus)
    End Sub

    ''' <summary>
    ''' Navigates the Bible WebView2 to the server's Bible tab.
    ''' Called after the server starts.
    ''' </summary>
    Private Sub NavigateBibleView()
        If wvBible Is Nothing OrElse _serverPort = 0 Then Return
        Try
            wvBible.Source = New Uri($"http://127.0.0.1:{_serverPort}/#bible")
            wvBible.Visible = True
            lblBibleStatus.Visible = False
        Catch
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Unified Log Panel
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildLogPanel()
        pnlLogPanel = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 180,
            .Visible = False}

        splitterLog = New Splitter() With {
            .Dock = DockStyle.Bottom,
            .Height = 4,
            .BackColor = Color.FromArgb(200, 200, 200),
            .Visible = False}

        ' Toolbar row
        Dim pnlLogToolbar As New Panel() With {
            .Dock = DockStyle.Top, .Height = 28}

        Dim lblLogTitle As New Label() With {
            .Text = "Output", .Location = New Drawing.Point(4, 5),
            .AutoSize = True, .Font = New Font("Segoe UI", 9, Drawing.FontStyle.Bold)}

        cboLogFilter = New ComboBox() With {
            .Location = New Drawing.Point(70, 2), .Size = New Drawing.Size(100, 22),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        cboLogFilter.Items.AddRange({"All", "Pipeline", "Server", "Debug"})
        cboLogFilter.SelectedIndex = 0

        btnLogClear = New Button() With {
            .Text = "Clear", .Location = New Drawing.Point(180, 1),
            .Size = New Drawing.Size(55, 24), .FlatStyle = FlatStyle.Flat}
        btnLogClear.FlatAppearance.BorderSize = 1
        AddHandler btnLogClear.Click, Sub(s, e) rtbUnifiedLog.Clear()

        btnLogCopy = New Button() With {
            .Text = "Copy", .Location = New Drawing.Point(240, 1),
            .Size = New Drawing.Size(55, 24), .FlatStyle = FlatStyle.Flat}
        btnLogCopy.FlatAppearance.BorderSize = 1
        AddHandler btnLogCopy.Click, Sub(s, e)
                                          If rtbUnifiedLog.TextLength > 0 Then
                                              Clipboard.SetText(rtbUnifiedLog.Text)
                                          End If
                                      End Sub

        pnlLogToolbar.Controls.AddRange({lblLogTitle, cboLogFilter, btnLogClear, btnLogCopy})

        ' Log RTB
        rtbUnifiedLog = New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .[ReadOnly] = True,
            .BackColor = Color.FromArgb(30, 30, 30),
            .ForeColor = Color.FromArgb(200, 200, 200),
            .Font = New Font("Consolas", 9.5F),
            .ScrollBars = RichTextBoxScrollBars.Vertical,
            .WordWrap = False}

        pnlLogPanel.Controls.Add(rtbUnifiedLog)
        pnlLogPanel.Controls.Add(pnlLogToolbar)
    End Sub

    Private Sub ToggleLogPanel()
        _logPanelVisible = Not _logPanelVisible
        pnlLogPanel.Visible = _logPanelVisible
        splitterLog.Visible = _logPanelVisible
        mnuViewLogPanel.Checked = _logPanelVisible
        If _logPanelVisible Then
            mnuViewLogPanel.Text = "Hide Log Panel"
        Else
            mnuViewLogPanel.Text = "Show Log Panel"
        End If
    End Sub

    ''' <summary>
    ''' Appends a message to the unified log panel. Thread-safe.
    ''' </summary>
    Private Sub AppendUnifiedLog(source As String, text As String, color As Drawing.Color)
        If rtbUnifiedLog Is Nothing Then Return

        _unifiedLogBuffer.Enqueue((source, text, color))

        If Threading.Interlocked.CompareExchange(_unifiedLogPending, 1, 0) = 0 Then
            If rtbUnifiedLog.IsHandleCreated Then
                rtbUnifiedLog.BeginInvoke(Sub() FlushUnifiedLog())
            End If
        End If
    End Sub

    Private Sub FlushUnifiedLog()
        Threading.Interlocked.Exchange(_unifiedLogPending, 0)

        Dim filter = ""
        If cboLogFilter.InvokeRequired Then
            filter = CStr(cboLogFilter.Invoke(Function() If(cboLogFilter.SelectedItem, "All").ToString()))
        Else
            filter = If(cboLogFilter.SelectedItem, "All").ToString()
        End If

        SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
        Try
            Dim entry As (Source As String, Text As String, Color As Drawing.Color) = ("", "", Color.White)
            While _unifiedLogBuffer.TryDequeue(entry)
                ' Apply filter
                If filter <> "All" AndAlso Not entry.Source.Equals(filter, StringComparison.OrdinalIgnoreCase) Then
                    Continue While
                End If

                rtbUnifiedLog.SelectionStart = rtbUnifiedLog.TextLength
                rtbUnifiedLog.SelectionLength = 0
                rtbUnifiedLog.SelectionColor = Color.FromArgb(100, 100, 100)
                rtbUnifiedLog.AppendText($"[{entry.Source}] ")
                rtbUnifiedLog.SelectionStart = rtbUnifiedLog.TextLength
                rtbUnifiedLog.SelectionColor = entry.Color
                rtbUnifiedLog.AppendText($"{entry.Text}{Environment.NewLine}")
            End While

            ' Trim excess
            If rtbUnifiedLog.Lines.Length > UnifiedLogMaxLines Then
                Dim removeUpTo = rtbUnifiedLog.GetFirstCharIndexFromLine(rtbUnifiedLog.Lines.Length - UnifiedLogMaxLines)
                rtbUnifiedLog.Select(0, removeUpTo)
                rtbUnifiedLog.SelectedText = ""
            End If
        Finally
            SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            rtbUnifiedLog.Invalidate()
        End Try

        SendMessage(rtbUnifiedLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
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

    Private Sub LaunchSessionWizard()
        ' Gather current audio devices from the live device combo
        Dim devices As New List(Of String)
        For Each item In cboLiveDevice.Items
            Dim text = item.ToString()
            If text <> "Detecting devices..." Then devices.Add(text)
        Next

        Using dlg As New FormSessionWizard(_config, devices.ToArray(), _whisperLanguages, _langNames)
            If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.StartSession Then
                ' Reload config into UI, switch to Live, start session
                LoadConfigToUi()
                SwitchWorkspace(tabPageLive, btnNavLive)
                btnLiveStart.PerformClick()
            End If
        End Using
    End Sub

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

    Private Sub ShowQrCode()
        If _serverPort = 0 Then
            MessageBox.Show("The server is not running. Start a live session first.",
                "QR Code", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim localIp = GetLocalIpAddress()
        Dim url = $"https://{localIp}:{_serverPort + 1}"

        If _formQr Is Nothing OrElse _formQr.IsDisposed Then
            _formQr = New FormQrCode(url)
            _formQr.Show(Me)
        Else
            _formQr.UpdateUrl(url)
            _formQr.BringToFront()
        End If
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
