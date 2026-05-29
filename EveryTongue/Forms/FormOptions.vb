' FormOptions.vb — VS-style Options dialog with tree categories
' Phase 2 of the UI redesign — consolidates Settings, Paths, and Server tabs.

Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Public Class FormOptions
    Inherits Form

    ' ── Result ────────────────────────────────────────────────────
    ''' <summary>True if user clicked OK and config was modified.</summary>
    Public Property ConfigChanged As Boolean = False

    ' ── Config reference ──────────────────────────────────────────
    Private ReadOnly _config As AppConfig
    Private ReadOnly _uiLocales As (Code As String, Name As String)()
    Private _hwInfo As HardwareInfo

    ' ═══════════════════════════════════════════════════════════════
    ' Constructor
    ' ═══════════════════════════════════════════════════════════════
    Public Sub New(config As AppConfig, uiLocales As (Code As String, Name As String)())
        _config = config
        _uiLocales = uiLocales
        InitializeComponent()
        treeNav.SelectedNode = treeNav.Nodes("general")
        WireEvents()
        PopulateFontCombo()
        LoadFromConfig()
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Post-designer wiring (event handlers that reference runtime state)
    ' ═══════════════════════════════════════════════════════════════
    Private Sub WireEvents()
        ' OK button
        AddHandler btnOk.Click, Sub(s, e) ApplyToConfig()

        ' Tree navigation
        AddHandler treeNav.AfterSelect, AddressOf TreeNav_AfterSelect

        ' Color pickers
        AddHandler btnBgColor.Click, Sub(s, e) PickColor(btnBgColor)
        AddHandler btnFgColor.Click, Sub(s, e) PickColor(btnFgColor)

        ' Browse buttons — file pickers
        AddHandler btnBrowseWhisper.Click, Sub(s, e) BrowseFile(txtWhisper)
        AddHandler btnBrowseYtdlp.Click, Sub(s, e) BrowseFile(txtYtdlp)
        AddHandler btnBrowseFfmpeg.Click, Sub(s, e) BrowseFile(txtFfmpeg)
        AddHandler btnBrowseFfprobe.Click, Sub(s, e) BrowseFile(txtFfprobe)
        AddHandler btnBrowseSubtitleEdit.Click, Sub(s, e) BrowseFile(txtSubtitleEdit)
        AddHandler btnBrowseModel.Click, Sub(s, e) BrowseFile(txtModel)
        AddHandler btnBrowseModelAudio.Click, Sub(s, e) BrowseFile(txtModelAudio)
        AddHandler btnBrowseGlossary.Click, Sub(s, e) BrowseFile(txtGlossary)

        ' Hardware re-scan
        AddHandler btnHwRescan.Click, Sub(s, e) RunHardwareScan()

        ' Browse buttons — folder pickers
        AddHandler btnBrowseFasterWhisper.Click, Sub(s, e) BrowseFolder(txtFasterWhisper)
        AddHandler btnBrowseNllbModel.Click, Sub(s, e) BrowseFolder(txtNllbModel)
        AddHandler btnBrowseOutputRoot.Click, Sub(s, e) BrowseFolder(txtOutputRoot)
        AddHandler btnBrowseBibles.Click, Sub(s, e) BrowseFolder(txtBibles)
    End Sub

    Private Sub PopulateFontCombo()
        For Each fam In Drawing.FontFamily.Families
            cboFont.Items.Add(fam.Name)
        Next
        For Each locale In _uiLocales
            cboUiLang.Items.Add(locale.Name)
        Next
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Navigation
    ' ═══════════════════════════════════════════════════════════════
    Private Sub TreeNav_AfterSelect(sender As Object, e As TreeViewEventArgs)
        pnlGeneral.Visible = (e.Node.Name = "general")
        pnlPaths.Visible = (e.Node.Name = "paths")
        pnlServer.Visible = (e.Node.Name = "server")
        pnlHardware.Visible = (e.Node.Name = "hardware")

        ' Auto-scan on first visit to hardware panel
        If e.Node.Name = "hardware" AndAlso _hwInfo Is Nothing Then
            RunHardwareScan()
        End If
    End Sub

    ''' <summary>Opens the dialog pre-selecting the given category.</summary>
    Public Sub SelectCategory(key As String)
        Dim node = treeNav.Nodes(key)
        If node IsNot Nothing Then treeNav.SelectedNode = node
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
        SelectItem(cboTheme, _config.Theme.ToString())
        chkStartWindows.Checked = _config.StartWithWindows
        chkMinimizeToTray.Checked = _config.MinimizeToTray

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
        If cboTheme.SelectedItem IsNot Nothing Then
            Dim parsed As Models.ThemeMode
            If [Enum].TryParse(cboTheme.SelectedItem.ToString(), True, parsed) Then
                _config.Theme = parsed
            End If
        End If
        _config.StartWithWindows = chkStartWindows.Checked
        _config.MinimizeToTray = chkMinimizeToTray.Checked
        If chkResetFirstRun.Checked Then _config.FirstRunComplete = False

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

        FormMain.WriteDebugLog($"[OPTIONS] ApplyToConfig: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}, TranslationEnabled={_config.TranslationEnabled}")
        ConfigManager.Save(_config)
        ConfigChanged = True
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Hardware scan
    ' ═══════════════════════════════════════════════════════════════
    Private Sub RunHardwareScan()
        btnHwRescan.Enabled = False
        lblHwVerdict.Text = "Scanning hardware..."
        Me.Cursor = Cursors.WaitCursor

        Threading.ThreadPool.QueueUserWorkItem(
            Sub()
                Dim info = HardwareScanner.Scan()
                Me.BeginInvoke(Sub() DisplayHardwareInfo(info))
            End Sub)
    End Sub

    Private Sub DisplayHardwareInfo(info As HardwareInfo)
        _hwInfo = info
        Me.Cursor = Cursors.Default
        btnHwRescan.Enabled = True

        ' Overall score
        lblHwOverallScore.Text = $"{info.OverallScore}/100"

        ' Traffic light indicator
        Select Case info.Rating
            Case "Green"
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(0, 180, 0)
            Case "Amber"
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(255, 180, 0)
            Case Else
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(220, 40, 40)
        End Select

        lblHwVerdict.Text = info.RatingDescription

        ' Component breakdown
        Dim gpuVram = If(info.GpuMemoryMB > 0, $"{info.GpuMemoryMB \ 1024}GB VRAM", "N/A")
        lblHwGpu.Text = $"GPU:  [{info.GpuScore}/100]  {info.GpuName} ({gpuVram})"
        Dim clockGHz = (info.CpuClockMHz / 1000.0).ToString("F1")
        lblHwCpu.Text = $"CPU:  [{info.CpuScore}/100]  {info.CpuName} ({info.CpuCores} cores, {clockGHz} GHz)"
        lblHwRam.Text = $"RAM:  [{info.RamScore}/100]  {info.RamTotalMB \ 1024}GB"
        Dim diskGB = info.DiskFreeMB / 1024.0
        lblHwDisk.Text = $"Disk:  [{info.DiskScore}/100]  {diskGB:F1}GB free"
        lblHwOs.Text = $"OS:  [{info.OsScore}/100]  {info.OsDescription}"

        ' Recommendations
        Dim recs = info.GetRecommendations()
        If recs.Count = 0 Then
            txtHwRecs.Text = "No recommendations — your hardware looks great!"
        Else
            txtHwRecs.Text = String.Join(Environment.NewLine & Environment.NewLine, recs)
        End If

        FormMain.WriteDebugLog($"[HW] Scan complete: Overall={info.OverallScore}, GPU={info.GpuScore}, CPU={info.CpuScore}, RAM={info.RamScore}, Disk={info.DiskScore}, OS={info.OsScore}, Rating={info.Rating}")
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Helpers
    ' ═══════════════════════════════════════════════════════════════
    Private Shared Sub BrowseFile(txt As TextBox)
        Using dlg As New OpenFileDialog()
            dlg.FileName = txt.Text
            If dlg.ShowDialog() = DialogResult.OK Then
                txt.Text = dlg.FileName
            End If
        End Using
    End Sub

    Private Shared Sub BrowseFolder(txt As TextBox)
        Using dlg As New FolderBrowserDialog()
            dlg.SelectedPath = AppConfig.ResolvePath(txt.Text)
            If dlg.ShowDialog() = DialogResult.OK Then
                txt.Text = dlg.SelectedPath
            End If
        End Using
    End Sub

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
