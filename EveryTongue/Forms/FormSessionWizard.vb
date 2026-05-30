' FormSessionWizard.vb — Live Session Wizard (5 steps)
' Phase 3 of the UI redesign — Feature #2 (Setup Wizard)

Imports System.Diagnostics
Imports EveryTongue.Models
Imports QRCoder

Public Class FormSessionWizard

    ' ── Result ────────────────────────────────────────────────────
    ''' <summary>True if user clicked Start on the final step.</summary>
    Public Property StartSession As Boolean = False

    ' ── Config / state ────────────────────────────────────────────
    Private ReadOnly _config As AppConfig
    Private ReadOnly _devices As String()
    Private ReadOnly _whisperLanguages As String()
    Private Shared ReadOnly _langCodeService As Services.Infrastructure.LanguageCodeService = Services.Infrastructure.LanguageCodeService.Instance

    ' ── Step state ────────────────────────────────────────────────
    Private _currentStep As Integer = 0
    Private ReadOnly _stepPanels As New List(Of Panel)
    Private _hwInfo As Services.Infrastructure.HardwareInfo

    ' ═══════════════════════════════════════════════════════════════
    ' Constructor
    ' ═══════════════════════════════════════════════════════════════
    Public Sub New(config As AppConfig, devices As String(),
                   whisperLanguages As String())
        _config = config
        _devices = If(devices, {})
        _whisperLanguages = whisperLanguages

        InitializeComponent()

        Me.CancelButton = btnCancel

        ' Wire button events
        AddHandler btnBack.Click, Sub(s, e) ShowStep(_currentStep - 1)
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

        ' Wire preview-update events (Step 4)
        AddHandler btnWizBg.BackColorChanged, Sub(s, e) UpdatePreview()
        AddHandler btnWizFg.BackColorChanged, Sub(s, e) UpdatePreview()
        AddHandler cboWizFont.SelectedIndexChanged, Sub(s, e) UpdatePreview()
        AddHandler nudWizFontSize.ValueChanged, Sub(s, e) UpdatePreview()
        AddHandler chkWizBold.CheckedChanged, Sub(s, e) UpdatePreview()

        ' Wire color-picker events
        AddHandler btnWizBg.Click, Sub(s, e) PickColor(btnWizBg)
        AddHandler btnWizFg.Click, Sub(s, e) PickColor(btnWizFg)

        ' Collect step panels in order
        _stepPanels.AddRange({pnlStep1, pnlStep2, pnlStep3, pnlStep4, pnlStep5})

        ' Populate runtime data into Designer controls
        PopulateStep2Audio()
        PopulateStep3Translation()
        PopulateStep4Display()

        ' Start hardware scan
        StartHardwareScan()

        ShowStep(0)
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 1: Hardware — runtime scan & population
    ' ═══════════════════════════════════════════════════════════════
    Private Sub StartHardwareScan()
        Threading.Tasks.Task.Run(
            Function() Services.Infrastructure.HardwareScanner.Scan()
        ).ContinueWith(Sub(t)
                           If t.IsFaulted Then
                               Dim msg = Services.Infrastructure.LanguagePackService.Instance.GetString("SW_HwScanFailed")
                               If Me.InvokeRequired Then
                                   Me.BeginInvoke(Sub() lblHwStatus.Text = msg)
                               Else
                                   lblHwStatus.Text = msg
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

        lblHwStatus.Text = $"Overall: {_hwInfo.OverallScore}/100 — {_hwInfo.GetRatingDescription(Services.Infrastructure.LanguagePackService.Instance)}"
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
    ' Step 2: Audio Input — populate runtime data
    ' ═══════════════════════════════════════════════════════════════
    Private Sub PopulateStep2Audio()
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

        For Each lang In _whisperLanguages
            Dim nllb = _langCodeService.ToNllb(lang)
            Dim name = If(Not String.IsNullOrEmpty(nllb), _langCodeService.GetDisplayName(nllb), "")
            If Not String.IsNullOrEmpty(name) Then
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
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 3: Translation — populate runtime data
    ' ═══════════════════════════════════════════════════════════════
    Private Sub PopulateStep3Translation()
        chkEnableTrans.Checked = _config.TranslationEnabled

        For i = 0 To cboTransDevice.Items.Count - 1
            If cboTransDevice.Items(i).ToString().Equals(_config.TranslationDevice, StringComparison.OrdinalIgnoreCase) Then
                cboTransDevice.SelectedIndex = i
                Exit For
            End If
        Next
        If cboTransDevice.SelectedIndex < 0 Then cboTransDevice.SelectedIndex = 0
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 4: Display & Network — populate runtime data
    ' ═══════════════════════════════════════════════════════════════
    Private Sub PopulateStep4Display()
        btnWizBg.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleBgColor, "#000000"))
        btnWizFg.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleFgColor, "#FFFFFF"))

        For Each fam In Drawing.FontFamily.Families
            cboWizFont.Items.Add(fam.Name)
        Next
        SelectItem(cboWizFont, _config.SubtitleFontFamily)

        nudWizFontSize.Value = CDec(_config.SubtitleFontSize)
        chkWizBold.Checked = _config.SubtitleFontBold

        UpdatePreview()
        UpdateNetworkLabel()
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
        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        Try
            Dim ip = GetLocalIp()
            lblNetworkStatus.Text = String.Format(lp.GetString("SW_NetworkConnected"), ip)
            lblNetworkStatus.ForeColor = Drawing.Color.DarkGreen
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ERROR] UpdateNetworkLabel: {ex.Message}")
            lblNetworkStatus.Text = lp.GetString("SW_NetworkNoIp")
            lblNetworkStatus.ForeColor = Drawing.Color.Red
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Step 5: Ready — summary & QR
    ' ═══════════════════════════════════════════════════════════════
    Private Sub PopulateSummary()
        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        Dim deviceText = If(lstDevices.SelectedItem?.ToString(), "(none)")
        Dim langText = If(cboSpeakerLang.SelectedItem?.ToString(), lp.GetString("SW_AutoDetect"))
        Dim transText = If(chkEnableTrans.Checked,
            String.Format(lp.GetString("SW_TransEnabled"), cboTransDevice.SelectedItem),
            lp.GetString("SW_TransDisabled"))
        Dim fontText = $"{If(cboWizFont.SelectedItem, "Segoe UI")} {nudWizFontSize.Value}pt"
        If chkWizBold.Checked Then fontText &= $" {lp.GetString("Opt_Bold")}"

        Dim ip = GetLocalIp()
        Dim port = _config.SubtitleServerPort
        Dim url = $"https://{ip}:{port + 1}"

        lblSummary.Text =
            String.Format(lp.GetString("SW_SummaryAudio"), deviceText) & vbCrLf & vbCrLf &
            String.Format(lp.GetString("SW_SummaryLanguage"), langText) & vbCrLf & vbCrLf &
            String.Format(lp.GetString("SW_SummaryTranslation"), transText) & vbCrLf & vbCrLf &
            String.Format(lp.GetString("SW_SummaryFont"), fontText) & vbCrLf & vbCrLf &
            String.Format(lp.GetString("SW_SummaryServer"), url)

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
            FormMain.WriteDebugLog($"[Wizard] GenerateQr failed: {ex.Message}")
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Navigation
    ' ═══════════════════════════════════════════════════════════════
    Private Function GetStepTitles() As String()
        Dim lp = Services.Infrastructure.LanguagePackService.Instance
        Return {
            lp.GetString("SW_StepHardware"),
            lp.GetString("SW_StepAudio"),
            lp.GetString("SW_StepTranslation"),
            lp.GetString("SW_StepDisplay"),
            lp.GetString("SW_StepReady")
        }
    End Function

    Private Sub ShowStep(index As Integer)
        If index < 0 OrElse index >= _stepPanels.Count Then Return

        _currentStep = index
        Dim lp = Services.Infrastructure.LanguagePackService.Instance

        ' Hide all, show current
        For Each p In _stepPanels
            p.Visible = False
        Next
        _stepPanels(index).Visible = True

        ' Update header
        lblStepNumber.Text = String.Format(lp.GetString("SW_StepNumber"), index + 1, _stepPanels.Count)
        Dim titles = GetStepTitles()
        lblStepTitle.Text = titles(index)

        ' Update buttons
        btnBack.Enabled = (index > 0)
        If index = _stepPanels.Count - 1 Then
            btnNext.Text = lp.GetString("SW_BtnStart")
            PopulateSummary()
        Else
            btnNext.Text = lp.GetString("SW_BtnNext")
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Apply settings to config
    ' ═══════════════════════════════════════════════════════════════
    Private Sub ApplyWizardSettings()
        ' Audio device
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
            FormMain.WriteDebugLog($"[Wizard] GetLocalIp failed: {ex.Message}")
        End Try
        Return "127.0.0.1"
    End Function

End Class
