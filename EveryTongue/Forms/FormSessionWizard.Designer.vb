<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSessionWizard
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        ' ── Top-level form controls ────────────────────────────────────
        lblStepNumber = New Label()
        lblStepTitle = New Label()
        pnlSteps = New Panel()
        btnBack = New Button()
        btnNext = New Button()
        btnCancel = New Button()

        ' ── Step 1: Hardware ──────────────────────────────────────────
        pnlStep1 = New Panel()
        lblHwStatus = New Label()
        pnlHwBars = New Panel()

        ' ── Step 2: Audio Input ───────────────────────────────────────
        pnlStep2 = New Panel()
        lblDev = New Label()
        lstDevices = New ListBox()
        lblLang = New Label()
        cboSpeakerLang = New ComboBox()

        ' ── Step 3: Translation ───────────────────────────────────────
        pnlStep3 = New Panel()
        chkEnableTrans = New CheckBox()
        lblDevice = New Label()
        cboTransDevice = New ComboBox()
        lblTransStatus = New Label()

        ' ── Step 4: Display & Network ─────────────────────────────────
        pnlStep4 = New Panel()
        lblAppear = New Label()
        lblBg = New Label()
        btnWizBg = New Button()
        lblFg = New Label()
        btnWizFg = New Button()
        lblFont = New Label()
        cboWizFont = New ComboBox()
        lblSize = New Label()
        nudWizFontSize = New NumericUpDown()
        chkWizBold = New CheckBox()
        pnlPreview = New Panel()
        lblPreview = New Label()
        lblNetworkStatus = New Label()

        ' ── Step 5: Ready ─────────────────────────────────────────────
        pnlStep5 = New Panel()
        lblSummary = New Label()
        picQr = New PictureBox()
        lblQrUrl = New Label()
        chkSaveTemplate = New CheckBox()
        txtTemplateName = New TextBox()

        pnlSteps.SuspendLayout()
        pnlStep1.SuspendLayout()
        pnlStep2.SuspendLayout()
        pnlStep3.SuspendLayout()
        pnlStep4.SuspendLayout()
        CType(nudWizFontSize, ComponentModel.ISupportInitialize).BeginInit()
        pnlPreview.SuspendLayout()
        pnlStep5.SuspendLayout()
        CType(picQr, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()

        ' ── lblStepNumber ─────────────────────────────────────────────
        lblStepNumber.Location = New Drawing.Point(16, 12)
        lblStepNumber.AutoSize = True
        lblStepNumber.Font = New Drawing.Font("Segoe UI", 9)
        lblStepNumber.ForeColor = Drawing.Color.Gray
        lblStepNumber.Name = "lblStepNumber"

        ' ── lblStepTitle ──────────────────────────────────────────────
        lblStepTitle.Location = New Drawing.Point(16, 30)
        lblStepTitle.AutoSize = True
        lblStepTitle.Font = New Drawing.Font("Segoe UI", 14, Drawing.FontStyle.Bold)
        lblStepTitle.Name = "lblStepTitle"

        ' ── pnlSteps ─────────────────────────────────────────────────
        pnlSteps.Location = New Drawing.Point(16, 62)
        pnlSteps.Size = New Drawing.Size(528, 320)
        pnlSteps.Name = "pnlSteps"
        pnlSteps.Controls.Add(pnlStep1)
        pnlSteps.Controls.Add(pnlStep2)
        pnlSteps.Controls.Add(pnlStep3)
        pnlSteps.Controls.Add(pnlStep4)
        pnlSteps.Controls.Add(pnlStep5)

        ' ── btnBack ───────────────────────────────────────────────────
        btnBack.Text = "< Back"
        btnBack.Size = New Drawing.Size(80, 30)
        btnBack.Location = New Drawing.Point(280, 398)
        btnBack.Name = "btnBack"

        ' ── btnNext ───────────────────────────────────────────────────
        btnNext.Text = "Next >"
        btnNext.Size = New Drawing.Size(80, 30)
        btnNext.Location = New Drawing.Point(368, 398)
        btnNext.Name = "btnNext"

        ' ── btnCancel ─────────────────────────────────────────────────
        btnCancel.Text = "Cancel"
        btnCancel.Size = New Drawing.Size(80, 30)
        btnCancel.Location = New Drawing.Point(456, 398)
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Name = "btnCancel"

        ' ════════════════════════════════════════════════════════════════
        ' Step 1: Hardware Check
        ' ════════════════════════════════════════════════════════════════
        ' lblHwStatus
        lblHwStatus.Text = "Scanning hardware..."
        lblHwStatus.Location = New Drawing.Point(0, 0)
        lblHwStatus.Size = New Drawing.Size(520, 24)
        lblHwStatus.Font = New Drawing.Font("Segoe UI", 11)
        lblHwStatus.Name = "lblHwStatus"

        ' pnlHwBars
        pnlHwBars.Location = New Drawing.Point(0, 30)
        pnlHwBars.Size = New Drawing.Size(520, 260)
        pnlHwBars.Name = "pnlHwBars"

        ' pnlStep1
        pnlStep1.Dock = DockStyle.Fill
        pnlStep1.Visible = False
        pnlStep1.Name = "pnlStep1"
        pnlStep1.Controls.Add(lblHwStatus)
        pnlStep1.Controls.Add(pnlHwBars)

        ' ════════════════════════════════════════════════════════════════
        ' Step 2: Audio Input
        ' ════════════════════════════════════════════════════════════════
        ' lblDev
        lblDev.Text = "Select your audio input device:"
        lblDev.Location = New Drawing.Point(0, 0)
        lblDev.AutoSize = True
        lblDev.Font = New Drawing.Font("Segoe UI", 10)
        lblDev.Name = "lblDev"

        ' lstDevices
        lstDevices.Location = New Drawing.Point(0, 26)
        lstDevices.Size = New Drawing.Size(520, 140)
        lstDevices.Name = "lstDevices"

        ' lblLang
        lblLang.Text = "Speaker language:"
        lblLang.Location = New Drawing.Point(0, 178)
        lblLang.AutoSize = True
        lblLang.Font = New Drawing.Font("Segoe UI", 10)
        lblLang.Name = "lblLang"

        ' cboSpeakerLang
        cboSpeakerLang.Location = New Drawing.Point(0, 200)
        cboSpeakerLang.Size = New Drawing.Size(200, 24)
        cboSpeakerLang.DropDownStyle = ComboBoxStyle.DropDown
        cboSpeakerLang.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboSpeakerLang.AutoCompleteSource = AutoCompleteSource.ListItems
        cboSpeakerLang.Name = "cboSpeakerLang"

        ' pnlStep2
        pnlStep2.Dock = DockStyle.Fill
        pnlStep2.Visible = False
        pnlStep2.Name = "pnlStep2"
        pnlStep2.Controls.Add(lblDev)
        pnlStep2.Controls.Add(lstDevices)
        pnlStep2.Controls.Add(lblLang)
        pnlStep2.Controls.Add(cboSpeakerLang)

        ' ════════════════════════════════════════════════════════════════
        ' Step 3: Translation
        ' ════════════════════════════════════════════════════════════════
        ' chkEnableTrans
        chkEnableTrans.Text = "Enable live translation for phone viewers"
        chkEnableTrans.Location = New Drawing.Point(0, 0)
        chkEnableTrans.AutoSize = True
        chkEnableTrans.Font = New Drawing.Font("Segoe UI", 10)
        chkEnableTrans.Name = "chkEnableTrans"

        ' lblDevice
        lblDevice.Text = "Device:"
        lblDevice.Location = New Drawing.Point(0, 40)
        lblDevice.AutoSize = True
        lblDevice.Name = "lblDevice"

        ' cboTransDevice
        cboTransDevice.Location = New Drawing.Point(0, 60)
        cboTransDevice.Size = New Drawing.Size(100, 24)
        cboTransDevice.DropDownStyle = ComboBoxStyle.DropDownList
        cboTransDevice.Name = "cboTransDevice"
        cboTransDevice.Items.AddRange({"cuda", "cpu"})

        ' lblTransStatus
        lblTransStatus.Text = "Translation status will be checked when the session starts."
        lblTransStatus.Location = New Drawing.Point(0, 100)
        lblTransStatus.Size = New Drawing.Size(500, 40)
        lblTransStatus.ForeColor = Drawing.Color.Gray
        lblTransStatus.Name = "lblTransStatus"

        ' pnlStep3
        pnlStep3.Dock = DockStyle.Fill
        pnlStep3.Visible = False
        pnlStep3.Name = "pnlStep3"
        pnlStep3.Controls.Add(chkEnableTrans)
        pnlStep3.Controls.Add(lblDevice)
        pnlStep3.Controls.Add(cboTransDevice)
        pnlStep3.Controls.Add(lblTransStatus)

        ' ════════════════════════════════════════════════════════════════
        ' Step 4: Display & Network
        ' ════════════════════════════════════════════════════════════════
        ' lblAppear
        lblAppear.Text = "How should subtitles look on phones?"
        lblAppear.Location = New Drawing.Point(0, 0)
        lblAppear.AutoSize = True
        lblAppear.Font = New Drawing.Font("Segoe UI", 10)
        lblAppear.Name = "lblAppear"

        ' lblBg
        lblBg.Text = "Background:"
        lblBg.Location = New Drawing.Point(0, 32)
        lblBg.AutoSize = True
        lblBg.Name = "lblBg"

        ' btnWizBg
        btnWizBg.Location = New Drawing.Point(0, 50)
        btnWizBg.Size = New Drawing.Size(70, 23)
        btnWizBg.FlatStyle = FlatStyle.Flat
        btnWizBg.FlatAppearance.BorderColor = Drawing.Color.Gray
        btnWizBg.Name = "btnWizBg"

        ' lblFg
        lblFg.Text = "Text:"
        lblFg.Location = New Drawing.Point(90, 32)
        lblFg.AutoSize = True
        lblFg.Name = "lblFg"

        ' btnWizFg
        btnWizFg.Location = New Drawing.Point(90, 50)
        btnWizFg.Size = New Drawing.Size(70, 23)
        btnWizFg.FlatStyle = FlatStyle.Flat
        btnWizFg.FlatAppearance.BorderColor = Drawing.Color.Gray
        btnWizFg.Name = "btnWizFg"

        ' lblFont
        lblFont.Text = "Font:"
        lblFont.Location = New Drawing.Point(180, 32)
        lblFont.AutoSize = True
        lblFont.Name = "lblFont"

        ' cboWizFont
        cboWizFont.Location = New Drawing.Point(180, 50)
        cboWizFont.Size = New Drawing.Size(160, 23)
        cboWizFont.DropDownStyle = ComboBoxStyle.DropDownList
        cboWizFont.Name = "cboWizFont"

        ' lblSize
        lblSize.Text = "Size:"
        lblSize.Location = New Drawing.Point(355, 32)
        lblSize.AutoSize = True
        lblSize.Name = "lblSize"

        ' nudWizFontSize
        nudWizFontSize.Location = New Drawing.Point(355, 50)
        nudWizFontSize.Size = New Drawing.Size(50, 23)
        nudWizFontSize.Minimum = 8
        nudWizFontSize.Maximum = 72
        nudWizFontSize.Name = "nudWizFontSize"

        ' chkWizBold
        chkWizBold.Text = "Bold"
        chkWizBold.Location = New Drawing.Point(415, 52)
        chkWizBold.AutoSize = True
        chkWizBold.Name = "chkWizBold"

        ' lblPreview
        lblPreview.Text = "The quick brown fox jumps over the lazy dog"
        lblPreview.Dock = DockStyle.Fill
        lblPreview.TextAlign = Drawing.ContentAlignment.MiddleCenter
        lblPreview.Name = "lblPreview"

        ' pnlPreview
        pnlPreview.Location = New Drawing.Point(0, 90)
        pnlPreview.Size = New Drawing.Size(520, 70)
        pnlPreview.BorderStyle = BorderStyle.FixedSingle
        pnlPreview.Name = "pnlPreview"
        pnlPreview.Controls.Add(lblPreview)

        ' lblNetworkStatus
        lblNetworkStatus.Location = New Drawing.Point(0, 178)
        lblNetworkStatus.Size = New Drawing.Size(520, 40)
        lblNetworkStatus.Font = New Drawing.Font("Segoe UI", 10)
        lblNetworkStatus.Name = "lblNetworkStatus"

        ' pnlStep4
        pnlStep4.Dock = DockStyle.Fill
        pnlStep4.Visible = False
        pnlStep4.Name = "pnlStep4"
        pnlStep4.Controls.Add(lblAppear)
        pnlStep4.Controls.Add(lblBg)
        pnlStep4.Controls.Add(btnWizBg)
        pnlStep4.Controls.Add(lblFg)
        pnlStep4.Controls.Add(btnWizFg)
        pnlStep4.Controls.Add(lblFont)
        pnlStep4.Controls.Add(cboWizFont)
        pnlStep4.Controls.Add(lblSize)
        pnlStep4.Controls.Add(nudWizFontSize)
        pnlStep4.Controls.Add(chkWizBold)
        pnlStep4.Controls.Add(pnlPreview)
        pnlStep4.Controls.Add(lblNetworkStatus)

        ' ════════════════════════════════════════════════════════════════
        ' Step 5: Ready
        ' ════════════════════════════════════════════════════════════════
        ' lblSummary
        lblSummary.Location = New Drawing.Point(0, 0)
        lblSummary.Size = New Drawing.Size(300, 180)
        lblSummary.Font = New Drawing.Font("Segoe UI", 10)
        lblSummary.Name = "lblSummary"

        ' picQr
        picQr.Location = New Drawing.Point(310, 0)
        picQr.Size = New Drawing.Size(200, 200)
        picQr.SizeMode = PictureBoxSizeMode.Zoom
        picQr.BackColor = Drawing.Color.White
        picQr.Name = "picQr"

        ' lblQrUrl
        lblQrUrl.Location = New Drawing.Point(310, 205)
        lblQrUrl.Size = New Drawing.Size(200, 20)
        lblQrUrl.TextAlign = Drawing.ContentAlignment.MiddleCenter
        lblQrUrl.Font = New Drawing.Font("Consolas", 8.5F)
        lblQrUrl.ForeColor = Drawing.Color.Gray
        lblQrUrl.Name = "lblQrUrl"

        ' chkSaveTemplate — save the wizard's choices as a reusable session template
        chkSaveTemplate.AutoSize = True
        chkSaveTemplate.Location = New Drawing.Point(0, 230)
        chkSaveTemplate.Name = "chkSaveTemplate"
        chkSaveTemplate.Text = "Save as session template"

        ' txtTemplateName
        txtTemplateName.Location = New Drawing.Point(0, 255)
        txtTemplateName.Size = New Drawing.Size(300, 23)
        txtTemplateName.Name = "txtTemplateName"
        txtTemplateName.Enabled = False

        ' pnlStep5
        pnlStep5.Dock = DockStyle.Fill
        pnlStep5.Visible = False
        pnlStep5.Name = "pnlStep5"
        pnlStep5.Controls.Add(lblSummary)
        pnlStep5.Controls.Add(picQr)
        pnlStep5.Controls.Add(lblQrUrl)
        pnlStep5.Controls.Add(chkSaveTemplate)
        pnlStep5.Controls.Add(txtTemplateName)

        ' ════════════════════════════════════════════════════════════════
        ' Form
        ' ════════════════════════════════════════════════════════════════
        Me.Text = "New Live Session"
        Me.ClientSize = New Drawing.Size(560, 440)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.Name = "FormSessionWizard"
        Me.Controls.AddRange({lblStepNumber, lblStepTitle, pnlSteps, btnBack, btnNext, btnCancel})

        pnlStep5.ResumeLayout(False)
        CType(picQr, ComponentModel.ISupportInitialize).EndInit()
        pnlPreview.ResumeLayout(False)
        CType(nudWizFontSize, ComponentModel.ISupportInitialize).EndInit()
        pnlStep4.ResumeLayout(False)
        pnlStep4.PerformLayout()
        pnlStep3.ResumeLayout(False)
        pnlStep3.PerformLayout()
        pnlStep2.ResumeLayout(False)
        pnlStep2.PerformLayout()
        pnlStep1.ResumeLayout(False)
        pnlSteps.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    ' ── Field declarations ────────────────────────────────────────────
    Friend WithEvents lblStepNumber As Label
    Friend WithEvents lblStepTitle As Label
    Friend WithEvents pnlSteps As Panel

    Friend WithEvents btnBack As Button
    Friend WithEvents btnNext As Button
    Friend WithEvents btnCancel As Button

    ' Step panels
    Friend WithEvents pnlStep1 As Panel
    Friend WithEvents pnlStep2 As Panel
    Friend WithEvents pnlStep3 As Panel
    Friend WithEvents pnlStep4 As Panel
    Friend WithEvents pnlStep5 As Panel

    ' Step 1: Hardware
    Friend WithEvents lblHwStatus As Label
    Friend WithEvents pnlHwBars As Panel

    ' Step 2: Audio Input
    Friend WithEvents lblDev As Label
    Friend WithEvents lstDevices As ListBox
    Friend WithEvents lblLang As Label
    Friend WithEvents cboSpeakerLang As ComboBox

    ' Step 3: Translation
    Friend WithEvents chkEnableTrans As CheckBox
    Friend WithEvents lblDevice As Label
    Friend WithEvents cboTransDevice As ComboBox
    Friend WithEvents lblTransStatus As Label

    ' Step 4: Display & Network
    Friend WithEvents lblAppear As Label
    Friend WithEvents lblBg As Label
    Friend WithEvents btnWizBg As Button
    Friend WithEvents lblFg As Label
    Friend WithEvents btnWizFg As Button
    Friend WithEvents lblFont As Label
    Friend WithEvents cboWizFont As ComboBox
    Friend WithEvents lblSize As Label
    Friend WithEvents nudWizFontSize As NumericUpDown
    Friend WithEvents chkWizBold As CheckBox
    Friend WithEvents pnlPreview As Panel
    Friend WithEvents lblPreview As Label
    Friend WithEvents lblNetworkStatus As Label

    ' Step 5: Ready
    Friend WithEvents lblSummary As Label
    Friend WithEvents picQr As PictureBox
    Friend WithEvents lblQrUrl As Label
    Friend WithEvents chkSaveTemplate As CheckBox
    Friend WithEvents txtTemplateName As TextBox

End Class
