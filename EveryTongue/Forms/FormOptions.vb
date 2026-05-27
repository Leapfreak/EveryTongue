' FormOptions.vb — VS-style Options dialog with tree categories
' Phase 2 of the UI redesign — consolidates Settings, Paths, and Server tabs.

Imports EveryTongue.Models

Public Class FormOptions
    Inherits Form

    ' ── Result ────────────────────────────────────────────────────
    ''' <summary>True if user clicked OK and config was modified.</summary>
    Public Property ConfigChanged As Boolean = False

    ' ── Config reference ──────────────────────────────────────────
    Private ReadOnly _config As AppConfig
    Private ReadOnly _uiLocales As (Code As String, Name As String)()

    ' ── Layout controls ───────────────────────────────────────────
    Private treeNav As TreeView
    Private pnlPages As Panel
    Private btnOk As Button
    Private btnCancel As Button
    Private btnApply As Button
    Private splitter As SplitContainer

    ' ── Category panels ───────────────────────────────────────────
    Private pnlGeneral As Panel
    Private pnlPaths As Panel
    Private pnlServer As Panel

    ' ── General controls ──────────────────────────────────────────
    Private cboUiLang As ComboBox
    Private cboTheme As ComboBox
    Private chkStartWindows As CheckBox

    ' ── Paths controls ────────────────────────────────────────────
    Private txtWhisper As TextBox
    Private txtYtdlp As TextBox
    Private txtFfmpeg As TextBox
    Private txtFfprobe As TextBox
    Private txtFasterWhisper As TextBox
    Private txtNllbModel As TextBox
    Private txtModel As TextBox
    Private txtModelAudio As TextBox
    Private txtOutputRoot As TextBox
    Private txtYtdlpFormat As TextBox
    Private txtSubtitleEdit As TextBox
    Private txtGlossary As TextBox
    Private txtBibles As TextBox

    ' ── Server controls ───────────────────────────────────────────
    Private nudPort As NumericUpDown
    Private btnBgColor As Button
    Private btnFgColor As Button
    Private cboFont As ComboBox
    Private nudFontSize As NumericUpDown
    Private chkBold As CheckBox
    Private txtPin As TextBox
    Private nudLivePort As NumericUpDown
    Private nudTransPort As NumericUpDown
    Private cboDevice As ComboBox
    Private nudUnload As NumericUpDown
    Private chkTransEnabled As CheckBox
    Private chkFirewall As CheckBox
    Private txtTts As TextBox

    ' ═══════════════════════════════════════════════════════════════
    ' Constructor
    ' ═══════════════════════════════════════════════════════════════
    Public Sub New(config As AppConfig, uiLocales As (Code As String, Name As String)())
        _config = config
        _uiLocales = uiLocales
        InitializeOptions()
        LoadFromConfig()
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Layout
    ' ═══════════════════════════════════════════════════════════════
    Private Sub InitializeOptions()
        Me.Text = "Options"
        Me.Size = New Drawing.Size(780, 560)
        Me.MinimumSize = New Drawing.Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.KeyPreview = True

        ' ── Buttons ───────────────────────────────────────────────
        btnOk = New Button() With {
            .Text = "OK", .Size = New Drawing.Size(80, 28),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
            .DialogResult = DialogResult.OK}
        btnCancel = New Button() With {
            .Text = "Cancel", .Size = New Drawing.Size(80, 28),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
            .DialogResult = DialogResult.Cancel}
        btnApply = New Button() With {
            .Text = "Apply", .Size = New Drawing.Size(80, 28),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right}

        btnOk.Location = New Drawing.Point(Me.ClientSize.Width - 260, Me.ClientSize.Height - 38)
        btnCancel.Location = New Drawing.Point(Me.ClientSize.Width - 172, Me.ClientSize.Height - 38)
        btnApply.Location = New Drawing.Point(Me.ClientSize.Width - 84, Me.ClientSize.Height - 38)

        AddHandler btnOk.Click, Sub(s, e) ApplyToConfig()
        AddHandler btnApply.Click, Sub(s, e) ApplyToConfig()

        Me.AcceptButton = btnOk
        Me.CancelButton = btnCancel

        ' ── Splitter ──────────────────────────────────────────────
        splitter = New SplitContainer() With {
            .Dock = DockStyle.None,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
            .Location = New Drawing.Point(8, 8),
            .Size = New Drawing.Size(Me.ClientSize.Width - 16, Me.ClientSize.Height - 52),
            .SplitterDistance = 180,
            .FixedPanel = FixedPanel.Panel1}

        ' ── Tree nav ──────────────────────────────────────────────
        treeNav = New TreeView() With {
            .Dock = DockStyle.Fill,
            .HideSelection = False,
            .ShowLines = False,
            .ShowPlusMinus = False,
            .ShowRootLines = False,
            .FullRowSelect = True,
            .ItemHeight = 28,
            .Font = New Drawing.Font("Segoe UI", 10)}

        treeNav.Nodes.Add("general", "General")
        treeNav.Nodes.Add("paths", "Tool Paths")
        treeNav.Nodes.Add("server", "Server && Subtitles")
        treeNav.ExpandAll()

        AddHandler treeNav.AfterSelect, AddressOf TreeNav_AfterSelect

        splitter.Panel1.Controls.Add(treeNav)

        ' ── Pages container ───────────────────────────────────────
        pnlPages = New Panel() With {.Dock = DockStyle.Fill}
        splitter.Panel2.Controls.Add(pnlPages)

        ' ── Build category panels ─────────────────────────────────
        BuildGeneralPanel()
        BuildPathsPanel()
        BuildServerPanel()

        pnlPages.Controls.AddRange({pnlGeneral, pnlPaths, pnlServer})

        Me.Controls.AddRange({splitter, btnOk, btnCancel, btnApply})

        ' Default selection
        treeNav.SelectedNode = treeNav.Nodes("general")
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' General Panel
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildGeneralPanel()
        pnlGeneral = New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Visible = True}

        Dim y = 12
        Dim lw = 520  ' control width

        AddSectionHeader(pnlGeneral, "Appearance", y) : y += 30

        AddLabel(pnlGeneral, "UI Language:", 12, y)
        cboUiLang = New ComboBox() With {
            .Location = New Drawing.Point(12, y + 18), .Size = New Drawing.Size(220, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        For Each locale In _uiLocales
            cboUiLang.Items.Add(locale.Name)
        Next
        pnlGeneral.Controls.Add(cboUiLang)
        y += 50

        AddLabel(pnlGeneral, "Theme:", 12, y)
        cboTheme = New ComboBox() With {
            .Location = New Drawing.Point(12, y + 18), .Size = New Drawing.Size(160, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        cboTheme.Items.AddRange({"System", "Light", "Dark"})
        pnlGeneral.Controls.Add(cboTheme)
        y += 58

        AddSectionHeader(pnlGeneral, "Startup", y) : y += 30

        chkStartWindows = New CheckBox() With {
            .Text = "Start with Windows",
            .Location = New Drawing.Point(12, y), .AutoSize = True}
        pnlGeneral.Controls.Add(chkStartWindows)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Paths Panel
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildPathsPanel()
        pnlPaths = New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Visible = False}

        Dim y = 12
        AddSectionHeader(pnlPaths, "Tool Paths", y) : y += 30

        txtWhisper = AddPathRow(pnlPaths, "Whisper CLI:", y, True) : y += 52
        txtYtdlp = AddPathRow(pnlPaths, "yt-dlp:", y, True) : y += 52
        txtFfmpeg = AddPathRow(pnlPaths, "FFmpeg:", y, True) : y += 52
        txtFfprobe = AddPathRow(pnlPaths, "FFprobe:", y, True) : y += 52
        txtSubtitleEdit = AddPathRow(pnlPaths, "SubtitleEdit:", y, True) : y += 52

        AddSectionHeader(pnlPaths, "Model Paths", y) : y += 30

        txtFasterWhisper = AddPathRow(pnlPaths, "Faster Whisper model:", y, False) : y += 52
        txtNllbModel = AddPathRow(pnlPaths, "NLLB model:", y, False) : y += 52
        txtModel = AddPathRow(pnlPaths, "Whisper model (job):", y, True) : y += 52
        txtModelAudio = AddPathRow(pnlPaths, "Whisper model (audio):", y, True) : y += 52

        AddSectionHeader(pnlPaths, "Directories", y) : y += 30

        txtOutputRoot = AddPathRow(pnlPaths, "Output root:", y, False) : y += 52
        txtGlossary = AddPathRow(pnlPaths, "Glossary file:", y, True) : y += 52
        txtBibles = AddPathRow(pnlPaths, "Bibles directory:", y, False) : y += 52

        AddSectionHeader(pnlPaths, "Advanced", y) : y += 30

        AddLabel(pnlPaths, "yt-dlp format:", 12, y)
        txtYtdlpFormat = New TextBox() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(520, 23),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
        pnlPaths.Controls.Add(txtYtdlpFormat)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Server Panel
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildServerPanel()
        pnlServer = New Panel() With {.Dock = DockStyle.Fill, .AutoScroll = True, .Visible = False}

        Dim y = 12
        AddSectionHeader(pnlServer, "Network", y) : y += 30

        AddLabel(pnlServer, "Subtitle server port:", 12, y)
        nudPort = AddNumericUpDown(pnlServer, 12, y + 18, 80, 1024, 65535, 5080)
        y += 52

        AddLabel(pnlServer, "Live server port:", 12, y)
        nudLivePort = AddNumericUpDown(pnlServer, 12, y + 18, 80, 1024, 65535, 5091)

        AddLabel(pnlServer, "Translation port:", 200, y)
        nudTransPort = AddNumericUpDown(pnlServer, 200, y + 18, 80, 1024, 65535, 5090)

        chkFirewall = New CheckBox() With {
            .Text = "Allow remote access (firewall)",
            .Location = New Drawing.Point(400, y + 20), .AutoSize = True}
        pnlServer.Controls.Add(chkFirewall)
        y += 52

        AddLabel(pnlServer, "Admin PIN:", 12, y)
        txtPin = New TextBox() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(100, 23), .MaxLength = 8}
        pnlServer.Controls.Add(txtPin)
        y += 58

        AddSectionHeader(pnlServer, "Subtitle Appearance", y) : y += 30

        AddLabel(pnlServer, "Background:", 12, y)
        btnBgColor = New Button() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(80, 23),
            .FlatStyle = FlatStyle.Flat, .BackColor = Drawing.Color.Black}
        btnBgColor.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnBgColor.Click, Sub(s, e) PickColor(btnBgColor)
        pnlServer.Controls.Add(btnBgColor)

        AddLabel(pnlServer, "Text color:", 110, y)
        btnFgColor = New Button() With {
            .Location = New Drawing.Point(110, y + 18),
            .Size = New Drawing.Size(80, 23),
            .FlatStyle = FlatStyle.Flat, .BackColor = Drawing.Color.White}
        btnFgColor.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnFgColor.Click, Sub(s, e) PickColor(btnFgColor)
        pnlServer.Controls.Add(btnFgColor)

        AddLabel(pnlServer, "Font:", 210, y)
        cboFont = New ComboBox() With {
            .Location = New Drawing.Point(210, y + 18),
            .Size = New Drawing.Size(180, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        For Each fam In Drawing.FontFamily.Families
            cboFont.Items.Add(fam.Name)
        Next
        pnlServer.Controls.Add(cboFont)

        AddLabel(pnlServer, "Size:", 410, y)
        nudFontSize = AddNumericUpDown(pnlServer, 410, y + 18, 55, 8, 72, 14)

        chkBold = New CheckBox() With {
            .Text = "Bold",
            .Location = New Drawing.Point(480, y + 20), .AutoSize = True}
        pnlServer.Controls.Add(chkBold)
        y += 58

        AddSectionHeader(pnlServer, "Translation", y) : y += 30

        chkTransEnabled = New CheckBox() With {
            .Text = "Translation enabled",
            .Location = New Drawing.Point(12, y), .AutoSize = True}
        pnlServer.Controls.Add(chkTransEnabled)
        y += 28

        AddLabel(pnlServer, "Device:", 12, y)
        cboDevice = New ComboBox() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(90, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        cboDevice.Items.AddRange({"cuda", "cpu"})
        pnlServer.Controls.Add(cboDevice)

        AddLabel(pnlServer, "Unload (min):", 120, y)
        nudUnload = AddNumericUpDown(pnlServer, 120, y + 18, 60, 0, 1440, 10)
        y += 58

        AddSectionHeader(pnlServer, "Text-to-Speech", y) : y += 30

        AddLabel(pnlServer, "TTS backends (comma-separated, empty = all):", 12, y)
        txtTts = New TextBox() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(300, 23)}
        pnlServer.Controls.Add(txtTts)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Config load / save
    ' ═══════════════════════════════════════════════════════════════
    Private Sub LoadFromConfig()
        ' General
        For i = 0 To _uiLocales.Length - 1
            If _uiLocales(i).Code.Equals(_config.UiLanguage, StringComparison.OrdinalIgnoreCase) Then
                cboUiLang.SelectedIndex = i
                Exit For
            End If
        Next
        SelectItem(cboTheme, _config.Theme)
        chkStartWindows.Checked = _config.StartWithWindows

        ' Paths
        txtWhisper.Text = _config.PathWhisper
        txtYtdlp.Text = _config.PathYtdlp
        txtFfmpeg.Text = _config.PathFfmpeg
        txtFfprobe.Text = _config.PathFfprobe
        txtFasterWhisper.Text = _config.PathFasterWhisperModel
        txtNllbModel.Text = _config.TranslationModelPath
        txtModel.Text = _config.PathModel
        txtModelAudio.Text = _config.PathModelAudio
        txtOutputRoot.Text = _config.PathOutputRoot
        txtYtdlpFormat.Text = _config.YtdlpFormat
        txtSubtitleEdit.Text = _config.PathSubtitleEdit
        txtGlossary.Text = _config.TranslationGlossaryPath
        txtBibles.Text = _config.BiblesDirectory

        ' Server
        nudPort.Value = _config.SubtitleServerPort
        nudLivePort.Value = _config.LiveServerPort
        nudTransPort.Value = _config.TranslationPort
        chkFirewall.Checked = _config.AllowFirewall
        txtPin.Text = _config.AdminPin

        btnBgColor.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleBgColor, "#000000"))
        btnFgColor.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleFgColor, "#FFFFFF"))
        SelectItem(cboFont, _config.SubtitleFontFamily)
        nudFontSize.Value = CDec(_config.SubtitleFontSize)
        chkBold.Checked = _config.SubtitleFontBold

        chkTransEnabled.Checked = _config.TranslationEnabled
        SelectItem(cboDevice, _config.TranslationDevice)
        nudUnload.Value = _config.TranslationUnloadMinutes
        txtTts.Text = _config.TtsBackends
    End Sub

    Private Sub ApplyToConfig()
        ' General
        If cboUiLang.SelectedIndex >= 0 AndAlso cboUiLang.SelectedIndex < _uiLocales.Length Then
            _config.UiLanguage = _uiLocales(cboUiLang.SelectedIndex).Code
        End If
        If cboTheme.SelectedItem IsNot Nothing Then _config.Theme = cboTheme.SelectedItem.ToString()
        _config.StartWithWindows = chkStartWindows.Checked

        ' Paths
        _config.PathWhisper = txtWhisper.Text
        _config.PathYtdlp = txtYtdlp.Text
        _config.PathFfmpeg = txtFfmpeg.Text
        _config.PathFfprobe = txtFfprobe.Text
        _config.PathFasterWhisperModel = txtFasterWhisper.Text
        _config.TranslationModelPath = txtNllbModel.Text
        _config.PathModel = txtModel.Text
        _config.PathModelAudio = txtModelAudio.Text
        _config.PathOutputRoot = txtOutputRoot.Text
        _config.YtdlpFormat = txtYtdlpFormat.Text
        _config.PathSubtitleEdit = txtSubtitleEdit.Text
        _config.TranslationGlossaryPath = txtGlossary.Text
        _config.BiblesDirectory = txtBibles.Text

        ' Server
        _config.SubtitleServerPort = CInt(nudPort.Value)
        _config.LiveServerPort = CInt(nudLivePort.Value)
        _config.TranslationPort = CInt(nudTransPort.Value)
        _config.AllowFirewall = chkFirewall.Checked
        _config.AdminPin = txtPin.Text.Trim()
        _config.SubtitleBgColor = Drawing.ColorTranslator.ToHtml(btnBgColor.BackColor)
        _config.SubtitleFgColor = Drawing.ColorTranslator.ToHtml(btnFgColor.BackColor)
        _config.SubtitleFontFamily = If(cboFont.SelectedItem?.ToString(), "Segoe UI")
        _config.SubtitleFontSize = CSng(nudFontSize.Value)
        _config.SubtitleFontBold = chkBold.Checked
        _config.TranslationEnabled = chkTransEnabled.Checked
        If cboDevice.SelectedItem IsNot Nothing Then _config.TranslationDevice = cboDevice.SelectedItem.ToString()
        _config.TranslationUnloadMinutes = CInt(nudUnload.Value)
        _config.TtsBackends = txtTts.Text.Trim()

        ConfigManager.Save(_config)
        ConfigChanged = True
    End Sub

    ''' <summary>Opens the dialog pre-selecting the given category.</summary>
    Public Sub SelectCategory(key As String)
        Dim node = treeNav.Nodes(key)
        If node IsNot Nothing Then treeNav.SelectedNode = node
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Navigation
    ' ═══════════════════════════════════════════════════════════════
    Private Sub TreeNav_AfterSelect(sender As Object, e As TreeViewEventArgs)
        pnlGeneral.Visible = (e.Node.Name = "general")
        pnlPaths.Visible = (e.Node.Name = "paths")
        pnlServer.Visible = (e.Node.Name = "server")
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Helpers
    ' ═══════════════════════════════════════════════════════════════
    Private Shared Sub AddLabel(parent As Panel, text As String, x As Integer, y As Integer)
        Dim lbl As New Label() With {
            .Text = text, .Location = New Drawing.Point(x, y), .AutoSize = True}
        parent.Controls.Add(lbl)
    End Sub

    Private Shared Sub AddSectionHeader(parent As Panel, text As String, y As Integer)
        Dim lbl As New Label() With {
            .Text = text,
            .Location = New Drawing.Point(8, y),
            .AutoSize = True,
            .Font = New Drawing.Font("Segoe UI", 11, Drawing.FontStyle.Bold)}
        parent.Controls.Add(lbl)

        Dim sep As New Label() With {
            .Location = New Drawing.Point(8, y + 22),
            .Size = New Drawing.Size(520, 1),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .BorderStyle = BorderStyle.Fixed3D}
        parent.Controls.Add(sep)
    End Sub

    Private Function AddPathRow(parent As Panel, labelText As String, y As Integer, isFile As Boolean) As TextBox
        AddLabel(parent, labelText, 12, y)

        Dim txt As New TextBox() With {
            .Location = New Drawing.Point(12, y + 18),
            .Size = New Drawing.Size(470, 23),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right}
        parent.Controls.Add(txt)

        Dim btn As New Button() With {
            .Text = "...",
            .Location = New Drawing.Point(488, y + 17),
            .Size = New Drawing.Size(34, 25),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right}

        If isFile Then
            AddHandler btn.Click, Sub(s, e)
                                       Using dlg As New OpenFileDialog()
                                           dlg.FileName = txt.Text
                                           If dlg.ShowDialog() = DialogResult.OK Then
                                               txt.Text = dlg.FileName
                                           End If
                                       End Using
                                   End Sub
        Else
            AddHandler btn.Click, Sub(s, e)
                                       Using dlg As New FolderBrowserDialog()
                                           dlg.SelectedPath = AppConfig.ResolvePath(txt.Text)
                                           If dlg.ShowDialog() = DialogResult.OK Then
                                               txt.Text = dlg.SelectedPath
                                           End If
                                       End Using
                                   End Sub
        End If
        parent.Controls.Add(btn)

        Return txt
    End Function

    Private Shared Function AddNumericUpDown(parent As Panel, x As Integer, y As Integer, w As Integer, min As Integer, max As Integer, val As Integer) As NumericUpDown
        Dim nud As New NumericUpDown() With {
            .Location = New Drawing.Point(x, y),
            .Size = New Drawing.Size(w, 23),
            .Minimum = min, .Maximum = max, .Value = val}
        parent.Controls.Add(nud)
        Return nud
    End Function

    Private Shared Sub PickColor(btn As Button)
        Using dlg As New ColorDialog()
            dlg.Color = btn.BackColor
            If dlg.ShowDialog() = DialogResult.OK Then
                btn.BackColor = dlg.Color
            End If
        End Using
    End Sub

    Private Shared Sub SelectItem(cbo As ComboBox, value As String)
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().Equals(value, StringComparison.OrdinalIgnoreCase) Then
                cbo.SelectedIndex = i
                Return
            End If
        Next
        If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
    End Sub
End Class
