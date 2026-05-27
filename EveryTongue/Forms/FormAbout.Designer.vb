<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAbout
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub InitializeComponent()
        Me.lblMotto = New System.Windows.Forms.Label()
        Me.lblVersion = New System.Windows.Forms.Label()
        Me.lblFree = New System.Windows.Forms.Label()
        Me.lblCopy = New System.Windows.Forms.Label()
        Me.lnkGithub = New System.Windows.Forms.LinkLabel()
        Me.lnkBoth = New System.Windows.Forms.LinkLabel()
        Me.lblGlory = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.picLogo = New System.Windows.Forms.PictureBox()
        CType(Me.picLogo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()

        ' picLogo
        Me.picLogo.BackColor = System.Drawing.Color.Transparent
        Me.picLogo.Location = New System.Drawing.Point(52, 20)
        Me.picLogo.Name = "picLogo"
        Me.picLogo.Size = New System.Drawing.Size(340, 100)
        Me.picLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picLogo.TabStop = False

        ' lblMotto
        Me.lblMotto.AutoSize = False
        Me.lblMotto.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Italic)
        Me.lblMotto.ForeColor = System.Drawing.Color.FromArgb(170, 170, 170)
        Me.lblMotto.Location = New System.Drawing.Point(0, 130)
        Me.lblMotto.Name = "lblMotto"
        Me.lblMotto.Size = New System.Drawing.Size(444, 24)
        Me.lblMotto.Text = """We hear them in our own tongues."" — Acts 2:11"
        Me.lblMotto.TextAlign = System.Drawing.ContentAlignment.MiddleCenter

        ' lblVersion
        Me.lblVersion.AutoSize = False
        Me.lblVersion.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lblVersion.ForeColor = System.Drawing.Color.FromArgb(140, 140, 140)
        Me.lblVersion.Location = New System.Drawing.Point(0, 162)
        Me.lblVersion.Name = "lblVersion"
        Me.lblVersion.Size = New System.Drawing.Size(444, 22)
        Me.lblVersion.Text = "Version"
        Me.lblVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter

        ' lblFree
        Me.lblFree.AutoSize = False
        Me.lblFree.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblFree.ForeColor = System.Drawing.Color.FromArgb(180, 180, 180)
        Me.lblFree.Location = New System.Drawing.Point(32, 200)
        Me.lblFree.Name = "lblFree"
        Me.lblFree.Size = New System.Drawing.Size(380, 46)
        Me.lblFree.Text = "This is free software, licensed under the GNU General Public License v3.0." &
            Microsoft.VisualBasic.vbCrLf & "You are free to use, modify, and share it."
        Me.lblFree.TextAlign = System.Drawing.ContentAlignment.MiddleCenter

        ' lblCopy
        Me.lblCopy.AutoSize = False
        Me.lblCopy.BackColor = System.Drawing.Color.Black
        Me.lblCopy.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lblCopy.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120)
        Me.lblCopy.Location = New System.Drawing.Point(0, 254)
        Me.lblCopy.Name = "lblCopy"
        Me.lblCopy.Size = New System.Drawing.Size(444, 20)
        Me.lblCopy.Text = "Copyright © 2024 Jeremy Smit"
        Me.lblCopy.TextAlign = System.Drawing.ContentAlignment.MiddleCenter

        ' lnkGithub
        Me.lnkGithub.ActiveLinkColor = System.Drawing.Color.FromArgb(100, 150, 255)
        Me.lnkGithub.AutoSize = True
        Me.lnkGithub.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.lnkGithub.LinkColor = System.Drawing.Color.FromArgb(68, 119, 255)
        Me.lnkGithub.Location = New System.Drawing.Point(0, 286)
        Me.lnkGithub.Name = "lnkGithub"
        Me.lnkGithub.Size = New System.Drawing.Size(200, 19)
        Me.lnkGithub.Text = "github.com/Leapfreak/EveryTongue"

        ' lnkBoth
        Me.lnkBoth.ActiveLinkColor = System.Drawing.Color.FromArgb(150, 150, 150)
        Me.lnkBoth.AutoSize = True
        Me.lnkBoth.Font = New System.Drawing.Font("Segoe UI", 9.0!)
        Me.lnkBoth.LinkColor = System.Drawing.Color.FromArgb(100, 100, 100)
        Me.lnkBoth.Location = New System.Drawing.Point(0, 316)
        Me.lnkBoth.Name = "lnkBoth"
        Me.lnkBoth.Size = New System.Drawing.Size(150, 17)
        Me.lnkBoth.Text = "License  |  Third-Party Notices"

        ' lblGlory
        Me.lblGlory.AutoSize = False
        Me.lblGlory.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Italic)
        Me.lblGlory.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90)
        Me.lblGlory.Location = New System.Drawing.Point(0, 350)
        Me.lblGlory.Name = "lblGlory"
        Me.lblGlory.Size = New System.Drawing.Size(444, 20)
        Me.lblGlory.Text = "For the Glory of God."
        Me.lblGlory.TextAlign = System.Drawing.ContentAlignment.MiddleCenter

        ' btnClose
        Me.btnClose.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
        Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(80, 80, 80)
        Me.btnClose.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.btnClose.ForeColor = System.Drawing.Color.White
        Me.btnClose.Location = New System.Drawing.Point(182, 389)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(80, 30)
        Me.btnClose.Text = "OK"

        ' FormAbout
        Me.AcceptButton = Me.btnClose
        Me.BackColor = System.Drawing.Color.Black
        Me.CancelButton = Me.btnClose
        Me.ClientSize = New System.Drawing.Size(444, 433)
        Me.Controls.Add(Me.picLogo)
        Me.Controls.Add(Me.lblMotto)
        Me.Controls.Add(Me.lblVersion)
        Me.Controls.Add(Me.lblFree)
        Me.Controls.Add(Me.lblCopy)
        Me.Controls.Add(Me.lnkGithub)
        Me.Controls.Add(Me.lnkBoth)
        Me.Controls.Add(Me.lblGlory)
        Me.Controls.Add(Me.btnClose)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormAbout"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "About Every Tongue"

        CType(Me.picLogo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents picLogo As System.Windows.Forms.PictureBox
    Friend WithEvents lblMotto As System.Windows.Forms.Label
    Friend WithEvents lblVersion As System.Windows.Forms.Label
    Friend WithEvents lblFree As System.Windows.Forms.Label
    Friend WithEvents lblCopy As System.Windows.Forms.Label
    Friend WithEvents lnkGithub As System.Windows.Forms.LinkLabel
    Friend WithEvents lnkBoth As System.Windows.Forms.LinkLabel
    Friend WithEvents lblGlory As System.Windows.Forms.Label
    Friend WithEvents btnClose As System.Windows.Forms.Button

End Class
