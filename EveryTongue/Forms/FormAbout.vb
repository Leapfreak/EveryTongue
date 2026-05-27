Imports System.Drawing
Imports System.Reflection
Imports System.Windows.Forms

Public Class FormAbout
    Inherits Form

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim ver = Assembly.GetExecutingAssembly().GetName().Version

        ' --- Form setup ---
        Me.Text = "About Every Tongue"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.ShowInTaskbar = False
        Me.BackColor = Color.FromArgb(24, 24, 24)
        Me.Size = New Size(460, 420)
        Me.Icon = Me.Owner?.Icon

        ' --- Logo ---
        Dim logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EveryTongue.Assets.Logo.png")
        If logoStream IsNot Nothing Then
            Dim logoPic As New PictureBox()
            logoPic.Image = Image.FromStream(logoStream)
            logoPic.SizeMode = PictureBoxSizeMode.Zoom
            logoPic.Size = New Size(340, 100)
            logoPic.Location = New Point((Me.ClientSize.Width - 340) \ 2, 20)
            logoPic.BackColor = Color.Transparent
            Me.Controls.Add(logoPic)
        End If

        ' --- Motto ---
        Dim lblMotto As New Label()
        lblMotto.Text = "Breaking language barriers for the Gospel"
        lblMotto.Font = New Font("Segoe UI", 11, FontStyle.Italic)
        lblMotto.ForeColor = Color.FromArgb(170, 170, 170)
        lblMotto.AutoSize = False
        lblMotto.Size = New Size(Me.ClientSize.Width, 24)
        lblMotto.Location = New Point(0, 130)
        lblMotto.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblMotto)

        ' --- Version ---
        Dim lblVersion As New Label()
        lblVersion.Text = $"Version {ver.Major}.{ver.Minor}.{ver.Build}"
        lblVersion.Font = New Font("Segoe UI", 10)
        lblVersion.ForeColor = Color.FromArgb(140, 140, 140)
        lblVersion.AutoSize = False
        lblVersion.Size = New Size(Me.ClientSize.Width, 22)
        lblVersion.Location = New Point(0, 162)
        lblVersion.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblVersion)

        ' --- Free software notice ---
        Dim lblFree As New Label()
        lblFree.Text = "This is free software, licensed under the GNU General Public License v3.0." &
            vbCrLf & "You are free to use, modify, and share it."
        lblFree.Font = New Font("Segoe UI", 9)
        lblFree.ForeColor = Color.FromArgb(180, 180, 180)
        lblFree.AutoSize = False
        lblFree.Size = New Size(380, 46)
        lblFree.Location = New Point((Me.ClientSize.Width - 380) \ 2, 200)
        lblFree.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblFree)

        ' --- Copyright ---
        Dim lblCopy As New Label()
        lblCopy.Text = $"Copyright © 2024-{DateTime.Now.Year} Jeremy Smit"
        lblCopy.Font = New Font("Segoe UI", 9)
        lblCopy.ForeColor = Color.FromArgb(120, 120, 120)
        lblCopy.AutoSize = False
        lblCopy.Size = New Size(Me.ClientSize.Width, 20)
        lblCopy.Location = New Point(0, 254)
        lblCopy.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblCopy)

        ' --- GitHub link ---
        Dim lnkGithub As New LinkLabel()
        lnkGithub.Text = "github.com/Leapfreak/EveryTongue"
        lnkGithub.Font = New Font("Segoe UI", 10)
        lnkGithub.LinkColor = Color.FromArgb(68, 119, 255)
        lnkGithub.ActiveLinkColor = Color.FromArgb(100, 150, 255)
        lnkGithub.AutoSize = False
        lnkGithub.Size = New Size(Me.ClientSize.Width, 22)
        lnkGithub.Location = New Point(0, 286)
        lnkGithub.TextAlign = ContentAlignment.MiddleCenter
        AddHandler lnkGithub.LinkClicked, Sub(s, ev)
            Process.Start(New ProcessStartInfo("https://github.com/Leapfreak/EveryTongue") With {.UseShellExecute = True})
        End Sub
        Me.Controls.Add(lnkGithub)

        ' --- Third-party notices link ---
        Dim lnkNotices As New LinkLabel()
        lnkNotices.Text = "Third-Party Notices"
        lnkNotices.Font = New Font("Segoe UI", 9)
        lnkNotices.LinkColor = Color.FromArgb(100, 100, 100)
        lnkNotices.ActiveLinkColor = Color.FromArgb(150, 150, 150)
        lnkNotices.AutoSize = False
        lnkNotices.Size = New Size(Me.ClientSize.Width, 20)
        lnkNotices.Location = New Point(0, 314)
        lnkNotices.TextAlign = ContentAlignment.MiddleCenter
        AddHandler lnkNotices.LinkClicked, Sub(s, ev)
            Dim noticesPath = IO.Path.Combine(Application.StartupPath, "THIRD_PARTY_NOTICES.md")
            If IO.File.Exists(noticesPath) Then
                Process.Start(New ProcessStartInfo(noticesPath) With {.UseShellExecute = True})
            End If
        End Sub
        Me.Controls.Add(lnkNotices)

        ' --- Close button ---
        Dim btnClose As New Button()
        btnClose.Text = "OK"
        btnClose.Size = New Size(80, 30)
        btnClose.Location = New Point((Me.ClientSize.Width - 80) \ 2, Me.ClientSize.Height - 48)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80)
        btnClose.BackColor = Color.FromArgb(50, 50, 50)
        btnClose.ForeColor = Color.White
        btnClose.Font = New Font("Segoe UI", 10)
        btnClose.DialogResult = DialogResult.OK
        Me.Controls.Add(btnClose)
        Me.AcceptButton = btnClose
        Me.CancelButton = btnClose
    End Sub
End Class
