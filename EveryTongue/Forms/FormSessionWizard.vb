' FormSessionWizard.vb — Live Session Wizard (5 steps)
' Phase 3 of the UI redesign — Feature #2 (Setup Wizard)

Imports System.Diagnostics
Imports EveryTongue.Models
Imports QRCoder

Public Class FormSessionWizard
    Inherits Form

    ' ── Result ────────────────────────────────────────────────────
    ''' <summary>True if user clicked Start on the final step.</summary>
    Public Property StartSession As Boolean = False

    ' ── Config / state ────────────────────────────────────────────
    Private ReadOnly _config As AppConfig
    Private ReadOnly _devices As String()
    Private ReadOnly _whisperLanguages As String()
    Private ReadOnly _langNames As Dictionary(Of String, String)

    ' ── Layout ────────────────────────────────────────────────────
    Private pnlSteps As Panel
    Private lblStepTitle As Label
    Private lblStepNumber As Label
    Private btnBack As Button
    Private btnNext As Button
    Private btnCancel As Button
    Private _currentStep As Integer = 0
    Private ReadOnly _stepPanels As New List(Of Panel)

    ' ── Step 1: Hardware ────────────────────────────────────────────
    Private lblHwStatus As Label
    Private pnlHwBars As Panel
    Private _hwInfo As Services.Infrastructure.HardwareInfo

    ' ── Step 2: Audio Input ───────────────────────────────────────
    Private lstDevices As ListBox
    Private cboSpeakerLang As ComboBox

    ' ── Step 3: Translation ───────────────────────────────────────
    Private chkEnableTrans As CheckBox
    Private cboTransDevice As ComboBox
    Private lblTransStatus As Label

    ' ── Step 4: Display & Network ─────────────────────────────────
    Private btnWizBg As Button
    Private btnWizFg As Button
    Private cboWizFont As ComboBox
    Private nudWizFontSize As NumericUpDown
    Private chkWizBold As CheckBox
    Private pnlPreview As Panel
    Private lblPreview As Label
    Private lblNetworkStatus As Label

    ' ── Step 5: Ready ─────────────────────────────────────────────
    Private lblSummary As Label
    Private picQr As PictureBox
    Private lblQrUrl As Label

    ' ═══════════════════════════════════════════════════════════════
    ' Constructor
    ' ═══════════════════════════════════════════════════════════════
    Public Sub New(config As AppConfig, devices As String(),
                   whisperLanguages As String(),
                   langNames As Dictionary(Of String, String))
        _config = config
        _devices = If(devices, {})
        _whisperLanguages = whisperLanguages
        _langNames = langNames
        BuildUi()
        ShowStep(0)
    End Sub

    Private Sub BuildUi()
        Me.Text = "New Live Session"
        Me.ClientSize = New Drawing.Size(560, 440)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False

        ' ── Header ────────────────────────────────────────────────
        lblStepNumber = New Label() With {
            .Location = New Drawing.Point(16, 12),
            .AutoSize = True, .Font = New Drawing.Font("Segoe UI", 9),
            .ForeColor = Drawing.Color.Gray}

        lblStepTitle = New Label() With {
            .Location = New Drawing.Point(16, 30),
            .AutoSize = True, .Font = New Drawing.Font("Segoe UI", 14, Drawing.FontStyle.Bold)}

        ' ── Steps container ───────────────────────────────────────
        pnlSteps = New Panel() With {
            .Location = New Drawing.Point(16, 62),
            .Size = New Drawing.Size(528, 320)}

        ' ── Buttons ───────────────────────────────────────────────
        btnBack = New Button() With {
            .Text = "< Back", .Size = New Drawing.Size(80, 30),
            .Location = New Drawing.Point(280, 398)}
        AddHandler btnBack.Click, Sub(s, e) ShowStep(_currentStep - 1)

        btnNext = New Button() With {
            .Text = "Next >", .Size = New Drawing.Size(80, 30),
            .Location = New Drawing.Point(368, 398)}
        AddHandler btnNext.Click, Sub(s, e)
                                       If _currentStep = _stepPanels.Count - 1 Then
                                           ApplyWizardSettings()
                                           StartSession = True
                                           Me.DialogResult = DialogResult.OK
                                           Me.Close()
                                       Else
                                           ShowStep(_currentStep + 1)
                                       End If
                                   End Sub

        btnCancel = New Button() With {
            .Text = "Cancel", .Size = New Drawing.Size(80, 30),
            .Location = New Drawing.Point(456, 398),
            .DialogResult = DialogResult.Cancel}
        Me.CancelButton = btnCancel

        Me.Controls.AddRange({lblStepNumber, lblStepTitle, pnlSteps, btnBack, btnNext, btnCancel})

        ' ── Build each step ───────────────────────────────────────
        BuildStep1_Hardware()
        BuildStep2_Audio()
        BuildStep3_Translation()
        BuildStep4_Display()
        BuildStep5_Ready()

        For Each p In _stepPanels
            p.Dock = DockStyle.Fill
            p.Visible = False
            pnlSteps.Controls.Add(p)
        Next
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 1: Hardware Check (stub)
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStep1_Hardware()
        Dim pnl As New Panel()

        lblHwStatus = New Label() With {
            .Text = "Scanning hardware...",
            .Location = New Drawing.Point(0, 0),
            .Size = New Drawing.Size(520, 24),
            .Font = New Drawing.Font("Segoe UI", 11)}

        pnlHwBars = New Panel() With {
            .Location = New Drawing.Point(0, 30),
            .Size = New Drawing.Size(520, 260)}

        pnl.Controls.AddRange({lblHwStatus, pnlHwBars})
        _stepPanels.Add(pnl)

        ' Scan hardware in background
        Threading.Tasks.Task.Run(
            Function() Services.Infrastructure.HardwareScanner.Scan()
        ).ContinueWith(Sub(t)
                            If t.IsFaulted Then
                                If Me.InvokeRequired Then
                                    Me.BeginInvoke(Sub() lblHwStatus.Text = "Hardware scan failed.")
                                Else
                                    lblHwStatus.Text = "Hardware scan failed."
                                End If
                                Return
                            End If
                            _hwInfo = t.Result
                            If Me.InvokeRequired Then
                                Me.BeginInvoke(Sub() PopulateHardwareStep())
                            Else
                                PopulateHardwareStep()
                            End If
                        End Sub)
    End Sub

    Private Sub PopulateHardwareStep()
        If _hwInfo Is Nothing Then Return

        Dim ratingColor As Drawing.Color
        Select Case _hwInfo.Rating
            Case "Green" : ratingColor = Drawing.Color.DarkGreen
            Case "Amber" : ratingColor = Drawing.Color.DarkOrange
            Case Else : ratingColor = Drawing.Color.Red
        End Select

        lblHwStatus.Text = $"Overall: {_hwInfo.OverallScore}/100 — {_hwInfo.RatingDescription}"
        lblHwStatus.ForeColor = ratingColor

        pnlHwBars.Controls.Clear()
        Dim y = 10

        AddScoreBar(pnlHwBars, $"GPU:  {_hwInfo.GpuName}", _hwInfo.GpuScore, _hwInfo.GpuMemoryMB & " MB", y) : y += 55
        AddScoreBar(pnlHwBars, $"CPU:  {_hwInfo.CpuName}", _hwInfo.CpuScore, _hwInfo.CpuCores & " cores", y) : y += 55
        AddScoreBar(pnlHwBars, "RAM:", _hwInfo.RamScore, FormatMB(_hwInfo.RamTotalMB), y) : y += 55
        AddScoreBar(pnlHwBars, "Disk free:", _hwInfo.DiskScore, FormatMB(_hwInfo.DiskFreeMB), y)
    End Sub

    Private Shared Sub AddScoreBar(parent As Panel, label As String, score As Integer, detail As String, y As Integer)
        Dim lblName As New Label() With {
            .Text = label, .Location = New Drawing.Point(0, y),
            .Size = New Drawing.Size(360, 18), .AutoEllipsis = True,
            .Font = New Drawing.Font("Segoe UI", 9)}
        parent.Controls.Add(lblName)

        Dim lblDetail As New Label() With {
            .Text = detail, .Location = New Drawing.Point(370, y),
            .AutoSize = True, .ForeColor = Drawing.Color.Gray,
            .Font = New Drawing.Font("Segoe UI", 8.5F)}
        parent.Controls.Add(lblDetail)

        ' Bar background
        Dim pnlBg As New Panel() With {
            .Location = New Drawing.Point(0, y + 20),
            .Size = New Drawing.Size(360, 16),
            .BackColor = Drawing.Color.FromArgb(230, 230, 230)}
        parent.Controls.Add(pnlBg)

        ' Bar fill
        Dim barWidth = CInt(360 * score / 100)
        Dim barColor As Drawing.Color
        If score >= 70 Then
            barColor = Drawing.Color.FromArgb(76, 175, 80)
        ElseIf score >= 40 Then
            barColor = Drawing.Color.FromArgb(255, 193, 7)
        Else
            barColor = Drawing.Color.FromArgb(244, 67, 54)
        End If

        Dim pnlFill As New Panel() With {
            .Location = New Drawing.Point(0, 0),
            .Size = New Drawing.Size(barWidth, 16),
            .BackColor = barColor}
        pnlBg.Controls.Add(pnlFill)

        ' Score text
        Dim lblScore As New Label() With {
            .Text = $"{score}/100", .Location = New Drawing.Point(370, y + 20),
            .AutoSize = True, .Font = New Drawing.Font("Segoe UI", 8.5F)}
        parent.Controls.Add(lblScore)
    End Sub

    Private Shared Function FormatMB(mb As Long) As String
        If mb >= 1024 Then Return $"{mb / 1024:F1} GB"
        Return $"{mb} MB"
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Step 2: Audio Input
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStep2_Audio()
        Dim pnl As New Panel()

        Dim lblDev As New Label() With {
            .Text = "Select your audio input device:",
            .Location = New Drawing.Point(0, 0), .AutoSize = True,
            .Font = New Drawing.Font("Segoe UI", 10)}

        lstDevices = New ListBox() With {
            .Location = New Drawing.Point(0, 26),
            .Size = New Drawing.Size(520, 140)}
        For Each dev In _devices
            lstDevices.Items.Add(dev)
        Next
        ' Pre-select saved device
        If Not String.IsNullOrEmpty(_config.LastLiveDeviceId) Then
            For i = 0 To lstDevices.Items.Count - 1
                If lstDevices.Items(i).ToString().Contains(_config.LastLiveDeviceId) Then
                    lstDevices.SelectedIndex = i
                    Exit For
                End If
            Next
        End If
        If lstDevices.SelectedIndex < 0 AndAlso lstDevices.Items.Count > 0 Then
            lstDevices.SelectedIndex = 0
        End If

        Dim lblLang As New Label() With {
            .Text = "Speaker language:",
            .Location = New Drawing.Point(0, 178), .AutoSize = True,
            .Font = New Drawing.Font("Segoe UI", 10)}

        cboSpeakerLang = New ComboBox() With {
            .Location = New Drawing.Point(0, 200),
            .Size = New Drawing.Size(200, 24),
            .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            .AutoCompleteSource = AutoCompleteSource.ListItems}
        For Each lang In _whisperLanguages
            Dim name As String = Nothing
            If _langNames.TryGetValue(lang, name) Then
                cboSpeakerLang.Items.Add($"{name} ({lang})")
            Else
                cboSpeakerLang.Items.Add(lang)
            End If
        Next
        ' Default to config language
        For i = 0 To cboSpeakerLang.Items.Count - 1
            If cboSpeakerLang.Items(i).ToString().Contains($"({_config.Language})") Then
                cboSpeakerLang.SelectedIndex = i
                Exit For
            End If
        Next

        pnl.Controls.AddRange({lblDev, lstDevices, lblLang, cboSpeakerLang})
        _stepPanels.Add(pnl)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 3: Translation
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStep3_Translation()
        Dim pnl As New Panel()

        chkEnableTrans = New CheckBox() With {
            .Text = "Enable live translation for phone viewers",
            .Location = New Drawing.Point(0, 0), .AutoSize = True,
            .Font = New Drawing.Font("Segoe UI", 10),
            .Checked = _config.TranslationEnabled}

        Dim lblDevice As New Label() With {
            .Text = "Device:",
            .Location = New Drawing.Point(0, 40), .AutoSize = True}

        cboTransDevice = New ComboBox() With {
            .Location = New Drawing.Point(0, 60),
            .Size = New Drawing.Size(100, 24),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        cboTransDevice.Items.AddRange({"cuda", "cpu"})
        For i = 0 To cboTransDevice.Items.Count - 1
            If cboTransDevice.Items(i).ToString().Equals(_config.TranslationDevice, StringComparison.OrdinalIgnoreCase) Then
                cboTransDevice.SelectedIndex = i
                Exit For
            End If
        Next
        If cboTransDevice.SelectedIndex < 0 Then cboTransDevice.SelectedIndex = 0

        lblTransStatus = New Label() With {
            .Text = "Translation status will be checked when the session starts.",
            .Location = New Drawing.Point(0, 100),
            .Size = New Drawing.Size(500, 40),
            .ForeColor = Drawing.Color.Gray}

        pnl.Controls.AddRange({chkEnableTrans, lblDevice, cboTransDevice, lblTransStatus})
        _stepPanels.Add(pnl)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 4: Display & Network
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStep4_Display()
        Dim pnl As New Panel()

        Dim lblAppear As New Label() With {
            .Text = "How should subtitles look on phones?",
            .Location = New Drawing.Point(0, 0), .AutoSize = True,
            .Font = New Drawing.Font("Segoe UI", 10)}

        Dim lblBg As New Label() With {.Text = "Background:", .Location = New Drawing.Point(0, 32), .AutoSize = True}
        btnWizBg = New Button() With {
            .Location = New Drawing.Point(0, 50), .Size = New Drawing.Size(70, 23),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleBgColor, "#000000"))}
        btnWizBg.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnWizBg.Click, Sub(s, e) PickColor(btnWizBg)

        Dim lblFg As New Label() With {.Text = "Text:", .Location = New Drawing.Point(90, 32), .AutoSize = True}
        btnWizFg = New Button() With {
            .Location = New Drawing.Point(90, 50), .Size = New Drawing.Size(70, 23),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleFgColor, "#FFFFFF"))}
        btnWizFg.FlatAppearance.BorderColor = Drawing.Color.Gray
        AddHandler btnWizFg.Click, Sub(s, e) PickColor(btnWizFg)

        Dim lblFont As New Label() With {.Text = "Font:", .Location = New Drawing.Point(180, 32), .AutoSize = True}
        cboWizFont = New ComboBox() With {
            .Location = New Drawing.Point(180, 50), .Size = New Drawing.Size(160, 23),
            .DropDownStyle = ComboBoxStyle.DropDownList}
        For Each fam In Drawing.FontFamily.Families
            cboWizFont.Items.Add(fam.Name)
        Next
        SelectItem(cboWizFont, _config.SubtitleFontFamily)

        Dim lblSize As New Label() With {.Text = "Size:", .Location = New Drawing.Point(355, 32), .AutoSize = True}
        nudWizFontSize = New NumericUpDown() With {
            .Location = New Drawing.Point(355, 50), .Size = New Drawing.Size(50, 23),
            .Minimum = 8, .Maximum = 72, .Value = CDec(_config.SubtitleFontSize)}

        chkWizBold = New CheckBox() With {
            .Text = "Bold", .Location = New Drawing.Point(415, 52), .AutoSize = True,
            .Checked = _config.SubtitleFontBold}

        ' Preview
        pnlPreview = New Panel() With {
            .Location = New Drawing.Point(0, 90),
            .Size = New Drawing.Size(520, 70),
            .BorderStyle = BorderStyle.FixedSingle}
        lblPreview = New Label() With {
            .Text = "The quick brown fox jumps over the lazy dog",
            .Dock = DockStyle.Fill,
            .TextAlign = Drawing.ContentAlignment.MiddleCenter}
        pnlPreview.Controls.Add(lblPreview)
        UpdatePreview()

        AddHandler btnWizBg.BackColorChanged, Sub(s, e) UpdatePreview()
        AddHandler btnWizFg.BackColorChanged, Sub(s, e) UpdatePreview()
        AddHandler cboWizFont.SelectedIndexChanged, Sub(s, e) UpdatePreview()
        AddHandler nudWizFontSize.ValueChanged, Sub(s, e) UpdatePreview()
        AddHandler chkWizBold.CheckedChanged, Sub(s, e) UpdatePreview()

        ' Network
        lblNetworkStatus = New Label() With {
            .Location = New Drawing.Point(0, 178),
            .Size = New Drawing.Size(520, 40),
            .Font = New Drawing.Font("Segoe UI", 10)}
        UpdateNetworkLabel()

        pnl.Controls.AddRange({lblAppear, lblBg, btnWizBg, lblFg, btnWizFg,
            lblFont, cboWizFont, lblSize, nudWizFontSize, chkWizBold,
            pnlPreview, lblNetworkStatus})
        _stepPanels.Add(pnl)
    End Sub

    Private Sub UpdatePreview()
        If pnlPreview Is Nothing Then Return
        pnlPreview.BackColor = btnWizBg.BackColor
        Dim style = If(chkWizBold.Checked, Drawing.FontStyle.Bold, Drawing.FontStyle.Regular)
        Dim fontName = If(cboWizFont.SelectedItem?.ToString(), "Segoe UI")
        lblPreview.Font = New Drawing.Font(fontName, CSng(nudWizFontSize.Value), style)
        lblPreview.ForeColor = btnWizFg.BackColor
    End Sub

    Private Sub UpdateNetworkLabel()
        Try
            Dim ip = GetLocalIp()
            lblNetworkStatus.Text = $"Network: Connected ({ip})"
            lblNetworkStatus.ForeColor = Drawing.Color.DarkGreen
        Catch
            lblNetworkStatus.Text = "Network: Could not determine IP address"
            lblNetworkStatus.ForeColor = Drawing.Color.Red
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 5: Ready
    ' ═══════════════════════════════════════════════════════════════
    Private Sub BuildStep5_Ready()
        Dim pnl As New Panel()

        lblSummary = New Label() With {
            .Location = New Drawing.Point(0, 0),
            .Size = New Drawing.Size(300, 180),
            .Font = New Drawing.Font("Segoe UI", 10)}

        picQr = New PictureBox() With {
            .Location = New Drawing.Point(310, 0),
            .Size = New Drawing.Size(200, 200),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Drawing.Color.White}

        lblQrUrl = New Label() With {
            .Location = New Drawing.Point(310, 205),
            .Size = New Drawing.Size(200, 20),
            .TextAlign = Drawing.ContentAlignment.MiddleCenter,
            .Font = New Drawing.Font("Consolas", 8.5F),
            .ForeColor = Drawing.Color.Gray}

        pnl.Controls.AddRange({lblSummary, picQr, lblQrUrl})
        _stepPanels.Add(pnl)
    End Sub

    Private Sub PopulateSummary()
        Dim deviceText = If(lstDevices.SelectedItem?.ToString(), "(none)")
        Dim langText = If(cboSpeakerLang.SelectedItem?.ToString(), "Auto Detect")
        Dim transText = If(chkEnableTrans.Checked,
            $"Enabled ({cboTransDevice.SelectedItem})",
            "Disabled")
        Dim fontText = $"{If(cboWizFont.SelectedItem, "Segoe UI")} {nudWizFontSize.Value}pt"
        If chkWizBold.Checked Then fontText &= " Bold"

        Dim ip = GetLocalIp()
        Dim port = _config.SubtitleServerPort
        Dim url = $"https://{ip}:{port + 1}"

        lblSummary.Text =
            $"Audio: {deviceText}" & vbCrLf & vbCrLf &
            $"Language: {langText}" & vbCrLf & vbCrLf &
            $"Translation: {transText}" & vbCrLf & vbCrLf &
            $"Font: {fontText}" & vbCrLf & vbCrLf &
            $"Server: {url}"

        lblQrUrl.Text = url
        GenerateQr(url)
    End Sub

    Private Sub GenerateQr(url As String)
        Try
            Using qrGen As New QRCodeGenerator()
                Dim qrData = qrGen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M)
                Using qrCode As New PngByteQRCode(qrData)
                    Dim pngBytes = qrCode.GetGraphic(8)
                    Using ms As New IO.MemoryStream(pngBytes)
                        Dim oldImage = picQr.Image
                        picQr.Image = Drawing.Image.FromStream(ms)
                        oldImage?.Dispose()
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine($"[Wizard] GenerateQr failed: {ex.Message}")
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Navigation
    ' ═══════════════════════════════════════════════════════════════
    Private ReadOnly _stepTitles As String() = {
        "Hardware Check",
        "Audio Input",
        "Translation",
        "Display && Network",
        "Ready"
    }

    Private Sub ShowStep(index As Integer)
        If index < 0 OrElse index >= _stepPanels.Count Then Return

        _currentStep = index

        ' Hide all, show current
        For Each p In _stepPanels
            p.Visible = False
        Next
        _stepPanels(index).Visible = True

        ' Update header
        lblStepNumber.Text = $"Step {index + 1} of {_stepPanels.Count}"
        lblStepTitle.Text = _stepTitles(index)

        ' Update buttons
        btnBack.Enabled = (index > 0)
        If index = _stepPanels.Count - 1 Then
            btnNext.Text = "Start!"
            PopulateSummary()
        Else
            btnNext.Text = "Next >"
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Apply settings to config
    ' ═══════════════════════════════════════════════════════════════
    Private Sub ApplyWizardSettings()
        ' Audio device — store the selected device text for matching later
        If lstDevices.SelectedItem IsNot Nothing Then
            _config.LastLiveDeviceId = lstDevices.SelectedItem.ToString()
        End If

        ' Speaker language
        If cboSpeakerLang.SelectedItem IsNot Nothing Then
            Dim display = cboSpeakerLang.SelectedItem.ToString()
            Dim pIdx = display.LastIndexOf("("c)
            If pIdx > 0 Then
                _config.Language = display.Substring(pIdx + 1).TrimEnd(")"c)
            End If
        End If

        ' Translation
        _config.TranslationEnabled = chkEnableTrans.Checked
        If cboTransDevice.SelectedItem IsNot Nothing Then
            _config.TranslationDevice = cboTransDevice.SelectedItem.ToString()
        End If

        ' Display
        _config.SubtitleBgColor = Drawing.ColorTranslator.ToHtml(btnWizBg.BackColor)
        _config.SubtitleFgColor = Drawing.ColorTranslator.ToHtml(btnWizFg.BackColor)
        _config.SubtitleFontFamily = If(cboWizFont.SelectedItem?.ToString(), "Segoe UI")
        _config.SubtitleFontSize = CSng(nudWizFontSize.Value)
        _config.SubtitleFontBold = chkWizBold.Checked

        ConfigManager.Save(_config)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Helpers
    ' ═══════════════════════════════════════════════════════════════
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

    Private Shared Function GetLocalIp() As String
        Try
            For Each addr In System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                If addr.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                    Dim ip = addr.ToString()
                    If Not ip.StartsWith("127.") Then Return ip
                End If
            Next
        Catch ex As Exception
            Debug.WriteLine($"[Wizard] GetLocalIp failed: {ex.Message}")
        End Try
        Return "127.0.0.1"
    End Function
End Class
