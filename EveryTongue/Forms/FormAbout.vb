Imports System.Diagnostics
Imports System.IO
Imports System.Reflection
Imports System.Windows.Forms

Public Class FormAbout

    Private Sub FormAbout_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Inherit owner icon
        Me.Icon = Me.Owner?.Icon

        ' Load logo from embedded resource
        Dim logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EveryTongue.Assets.Logo.png")
        If logoStream IsNot Nothing Then
            picLogo.Image = Drawing.Image.FromStream(logoStream)
        End If

        ' Set version text at runtime
        Dim ver = Assembly.GetExecutingAssembly().GetName().Version
        lblVersion.Text = $"Version {ver.Major}.{ver.Minor}.{ver.Build}"

        ' Set copyright year at runtime
        lblCopy.Text = $"Copyright © 2024-{DateTime.Now.Year} Jeremy Smit"

        ' Set up link regions on lnkBoth
        lnkBoth.Links.Clear()
        lnkBoth.Links.Add(0, 7, "license")   ' "License"
        lnkBoth.Links.Add(12, 20, "notices") ' "Third-Party Notices"

        ' Center AutoSize link labels via Layout
        AddHandler Me.Layout, Sub(s As Object, ev As LayoutEventArgs)
            Dim cw = Me.ClientSize.Width
            lnkGithub.Location = New Drawing.Point((cw - lnkGithub.Width) \ 2, 286)
            lnkBoth.Location = New Drawing.Point((cw - lnkBoth.Width) \ 2, 316)
        End Sub
    End Sub

    Private Sub lnkGithub_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkGithub.LinkClicked
        Process.Start(New ProcessStartInfo("https://github.com/Leapfreak/EveryTongue") With {.UseShellExecute = True})
    End Sub

    Private Sub lnkBoth_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkBoth.LinkClicked
        Dim tag = CStr(e.Link.LinkData)
        Dim filePath As String
        If tag = "license" Then
            filePath = Path.Combine(Application.StartupPath, "LICENSE")
        Else
            filePath = Path.Combine(Application.StartupPath, "THIRD_PARTY_NOTICES.md")
        End If
        If File.Exists(filePath) Then
            Process.Start(New ProcessStartInfo(filePath) With {.UseShellExecute = True})
        End If
    End Sub

End Class
