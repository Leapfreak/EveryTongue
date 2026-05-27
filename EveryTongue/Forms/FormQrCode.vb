' FormQrCode.vb — Floating QR code window for phone connection
' Phase 3 of the UI redesign — Feature #1

Imports System.Diagnostics
Imports QRCoder

Public Class FormQrCode
    Inherits Form

    Private _url As String
    Private _isDragging As Boolean
    Private _dragOffset As Drawing.Point

    Public Sub New(url As String)
        _url = url
        InitializeComponent()
        lblUrl.Text = url
        AddHandler Me.MouseDown, AddressOf Form_MouseDown
        AddHandler Me.MouseMove, AddressOf Form_MouseMove
        AddHandler Me.MouseUp, AddressOf Form_MouseUp
        AddHandler picQr.MouseDown, AddressOf Form_MouseDown
        AddHandler picQr.MouseMove, AddressOf Form_MouseMove
        AddHandler picQr.MouseUp, AddressOf Form_MouseUp
        GenerateQr()
    End Sub

    ''' <summary>Updates the QR code with a new URL (e.g. after server restart).</summary>
    Public Sub UpdateUrl(url As String)
        _url = url
        lblUrl.Text = url
        GenerateQr()
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

    Private Sub btnCopyUrl_Click(sender As Object, e As EventArgs) Handles btnCopyUrl.Click
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

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.Close()
    End Sub

    Private Sub btnSaveImage_Click(sender As Object, e As EventArgs) Handles btnSaveImage.Click
        Using dlg As New SaveFileDialog()
            dlg.Filter = "PNG Image|*.png"
            dlg.FileName = "everytongue-qr.png"
            If dlg.ShowDialog() = DialogResult.OK Then
                picQr.Image?.Save(dlg.FileName, Drawing.Imaging.ImageFormat.Png)
            End If
        End Using
    End Sub

End Class
