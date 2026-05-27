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
        Me.BackColor = Color.Black
        Me.Size = New Size(460, 460)
        Me.Icon = Me.Owner?.Icon

        Dim cw = Me.ClientSize.Width

        ' --- Logo ---
        Dim logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EveryTongue.Assets.Logo.png")
        If logoStream IsNot Nothing Then
            Dim logoPic As New PictureBox()
            logoPic.Image = Image.FromStream(logoStream)
            logoPic.SizeMode = PictureBoxSizeMode.Zoom
            logoPic.Size = New Size(340, 100)
            logoPic.Location = New Point((cw - 340) \ 2, 20)
            logoPic.BackColor = Color.Transparent
            Me.Controls.Add(logoPic)
        End If

        ' --- Motto ---
        Dim lblMotto As New Label()
        lblMotto.Text = """We hear them in our own tongues."" — Acts 2:11"
        lblMotto.Font = New Font("Segoe UI", 11, FontStyle.Italic)
        lblMotto.ForeColor = Color.FromArgb(170, 170, 170)
        lblMotto.AutoSize = False
        lblMotto.Size = New Size(cw, 24)
        lblMotto.Location = New Point(0, 130)
        lblMotto.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblMotto)

        ' --- Version ---
        Dim lblVersion As New Label()
        lblVersion.Text = $"Version {ver.Major}.{ver.Minor}.{ver.Build}"
        lblVersion.Font = New Font("Segoe UI", 10)
        lblVersion.ForeColor = Color.FromArgb(140, 140, 140)
        lblVersion.AutoSize = False
        lblVersion.Size = New Size(cw, 22)
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
        lblFree.Location = New Point((cw - 380) \ 2, 200)
        lblFree.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblFree)

        ' --- Copyright ---
        Dim lblCopy As New Label()
        lblCopy.Text = $"Copyright © 2024-{DateTime.Now.Year} Jeremy Smit"
        lblCopy.Font = New Font("Segoe UI", 9)
        lblCopy.ForeColor = Color.FromArgb(120, 120, 120)
        lblCopy.BackColor = Color.Black
        lblCopy.AutoSize = False
        lblCopy.Size = New Size(cw, 20)
        lblCopy.Location = New Point(0, 254)
        lblCopy.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblCopy)

        ' --- GitHub link ---
        Dim lnkGithub As New LinkLabel()
        lnkGithub.Text = "github.com/Leapfreak/EveryTongue"
        lnkGithub.Font = New Font("Segoe UI", 10)
        lnkGithub.LinkColor = Color.FromArgb(68, 119, 255)
        lnkGithub.ActiveLinkColor = Color.FromArgb(100, 150, 255)
        lnkGithub.AutoSize = True
        lnkGithub.Location = New Point(0, 286)
        AddHandler lnkGithub.LinkClicked, Sub(s, ev)
            Process.Start(New ProcessStartInfo("https://github.com/Leapfreak/EveryTongue") With {.UseShellExecute = True})
        End Sub
        Me.Controls.Add(lnkGithub)
        ' Center after AutoSize calculates width
        AddHandler Me.Layout, Sub(s, ev)
            lnkGithub.Location = New Point((cw - lnkGithub.Width) \ 2, 286)
        End Sub

        ' --- License and notices links ---
        Dim lnkBoth As New LinkLabel()
        lnkBoth.Text = "License  |  Third-Party Notices"
        lnkBoth.Font = New Font("Segoe UI", 9)
        lnkBoth.LinkColor = Color.FromArgb(100, 100, 100)
        lnkBoth.ActiveLinkColor = Color.FromArgb(150, 150, 150)
        lnkBoth.AutoSize = True
        lnkBoth.Location = New Point(0, 316)
        ' Set up two link regions
        lnkBoth.Links.Clear()
        lnkBoth.Links.Add(0, 7, "license")       ' "License"
        lnkBoth.Links.Add(12, 20, "notices")      ' "Third-Party Notices"
        AddHandler lnkBoth.LinkClicked, Sub(s, ev)
            Dim tag = CStr(ev.Link.LinkData)
            Dim filePath As String
            If tag = "license" Then
                filePath = IO.Path.Combine(Application.StartupPath, "LICENSE")
            Else
                filePath = IO.Path.Combine(Application.StartupPath, "THIRD_PARTY_NOTICES.md")
            End If
            If IO.File.Exists(filePath) Then
                Process.Start(New ProcessStartInfo(filePath) With {.UseShellExecute = True})
            End If
        End Sub
        Me.Controls.Add(lnkBoth)
        AddHandler Me.Layout, Sub(s, ev)
            lnkBoth.Location = New Point((cw - lnkBoth.Width) \ 2, 316)
        End Sub

        ' --- Soli Deo Gloria ---
        Dim lblGlory As New Label()
        lblGlory.Text = "For the Glory of God."
        lblGlory.Font = New Font("Segoe UI", 9, FontStyle.Italic)
        lblGlory.ForeColor = Color.FromArgb(90, 90, 90)
        lblGlory.AutoSize = False
        lblGlory.Size = New Size(cw, 20)
        lblGlory.Location = New Point(0, 350)
        lblGlory.TextAlign = ContentAlignment.MiddleCenter
        Me.Controls.Add(lblGlory)

        ' --- Close button ---
        Dim btnClose As New Button()
        btnClose.Text = "OK"
        btnClose.Size = New Size(80, 30)
        btnClose.Location = New Point((cw - 80) \ 2, Me.ClientSize.Height - 44)
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
