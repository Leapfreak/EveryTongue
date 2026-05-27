' FormQrCode.vb — Floating QR code window for phone connection
' Phase 3 of the UI redesign — Feature #1

Imports System.Diagnostics
Imports QRCoder

Public Class FormQrCode
    Inherits Form

    Private _url As String
    Private picQr As PictureBox
    Private lblUrl As Label
    Private btnCopyUrl As Button
    Private btnSaveImage As Button
    Private btnClose As Button
    Private _isDragging As Boolean
    Private _dragOffset As Drawing.Point

    Public Sub New(url As String)
        _url = url
        BuildUi()
        GenerateQr()
    End Sub

    ''' <summary>Updates the QR code with a new URL (e.g. after server restart).</summary>
    Public Sub UpdateUrl(url As String)
        _url = url
        lblUrl.Text = url
        GenerateQr()
    End Sub

    Private Sub BuildUi()
        Me.Text = "QR Code — Every Tongue"
        Me.FormBorderStyle = FormBorderStyle.FixedToolWindow
        Me.StartPosition = FormStartPosition.CenterParent
        Me.TopMost = True
        Me.ShowInTaskbar = False
        Me.ClientSize = New Drawing.Size(320, 420)
        Me.BackColor = Drawing.Color.White

        ' Make draggable
        AddHandler Me.MouseDown, AddressOf Form_MouseDown
        AddHandler Me.MouseMove, AddressOf Form_MouseMove
        AddHandler Me.MouseUp, AddressOf Form_MouseUp

        picQr = New PictureBox() With {
            .Location = New Drawing.Point(10, 10),
            .Size = New Drawing.Size(300, 300),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Drawing.Color.White}
        AddHandler picQr.MouseDown, AddressOf Form_MouseDown
        AddHandler picQr.MouseMove, AddressOf Form_MouseMove
        AddHandler picQr.MouseUp, AddressOf Form_MouseUp

        lblUrl = New Label() With {
            .Text = _url,
            .Location = New Drawing.Point(10, 318),
            .Size = New Drawing.Size(300, 22),
            .TextAlign = Drawing.ContentAlignment.MiddleCenter,
            .Font = New Drawing.Font("Consolas", 9),
            .ForeColor = Drawing.Color.FromArgb(80, 80, 80)}

        btnCopyUrl = New Button() With {
            .Text = "Copy URL",
            .Location = New Drawing.Point(10, 348),
            .Size = New Drawing.Size(92, 28)}
        AddHandler btnCopyUrl.Click, Sub(s, e)
                                          Clipboard.SetText(_url)
                                          btnCopyUrl.Text = "Copied!"
                                          Dim t As New Timer() With {.Interval = 1500}
                                          AddHandler t.Tick, Sub(s2, e2)
                                                                  btnCopyUrl.Text = "Copy URL"
                                                                  t.Stop()
                                                                  t.Dispose()
                                                              End Sub
                                          t.Start()
                                      End Sub

        btnSaveImage = New Button() With {
            .Text = "Save Image",
            .Location = New Drawing.Point(114, 348),
            .Size = New Drawing.Size(92, 28)}
        AddHandler btnSaveImage.Click, Sub(s, e)
                                            Using dlg As New SaveFileDialog()
                                                dlg.Filter = "PNG Image|*.png"
                                                dlg.FileName = "everytongue-qr.png"
                                                If dlg.ShowDialog() = DialogResult.OK Then
                                                    picQr.Image?.Save(dlg.FileName, Drawing.Imaging.ImageFormat.Png)
                                                End If
                                            End Using
                                        End Sub

        btnClose = New Button() With {
            .Text = "Close",
            .Location = New Drawing.Point(218, 348),
            .Size = New Drawing.Size(92, 28),
            .DialogResult = DialogResult.Cancel}

        Me.CancelButton = btnClose
        Me.Controls.AddRange({picQr, lblUrl, btnCopyUrl, btnSaveImage, btnClose})
    End Sub

    Private Sub GenerateQr()
        If String.IsNullOrEmpty(_url) Then Return
        Try
            Using qrGen As New QRCodeGenerator()
                Dim qrData = qrGen.CreateQrCode(_url, QRCodeGenerator.ECCLevel.M)
                Using qrCode As New PngByteQRCode(qrData)
                    Dim pngBytes = qrCode.GetGraphic(10)
                    Using ms As New IO.MemoryStream(pngBytes)
                        Dim oldImage = picQr.Image
                        picQr.Image = Drawing.Image.FromStream(ms)
                        oldImage?.Dispose()
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Debug.WriteLine($"[QR] GenerateQr failed: {ex.Message}")
        End Try
    End Sub

    ' ── Draggable ─────────────────────────────────────────────────
    Private Sub Form_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            _isDragging = True
            _dragOffset = e.Location
            If sender IsNot Me Then
                ' Offset relative to the child control
                Dim ctrl = DirectCast(sender, Control)
                _dragOffset = New Drawing.Point(e.X + ctrl.Left, e.Y + ctrl.Top)
            End If
        End If
    End Sub

    Private Sub Form_MouseMove(sender As Object, e As MouseEventArgs)
        If _isDragging Then
            Dim screenPos = If(sender Is Me, Me.PointToScreen(e.Location), DirectCast(sender, Control).PointToScreen(e.Location))
            Me.Location = New Drawing.Point(screenPos.X - _dragOffset.X, screenPos.Y - _dragOffset.Y)
        End If
    End Sub

    Private Sub Form_MouseUp(sender As Object, e As MouseEventArgs)
        _isDragging = False
    End Sub
End Class
