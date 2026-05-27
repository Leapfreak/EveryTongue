<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormQrCode
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    Private Sub InitializeComponent()
        Me.picQr = New System.Windows.Forms.PictureBox()
        Me.lblUrl = New System.Windows.Forms.Label()
        Me.btnCopyUrl = New System.Windows.Forms.Button()
        Me.btnSaveImage = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        CType(Me.picQr, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        ' picQr
        '
        Me.picQr.BackColor = System.Drawing.Color.White
        Me.picQr.Location = New System.Drawing.Point(10, 10)
        Me.picQr.Name = "picQr"
        Me.picQr.Size = New System.Drawing.Size(300, 300)
        Me.picQr.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.picQr.TabIndex = 0
        Me.picQr.TabStop = False
        '
        ' lblUrl
        '
        Me.lblUrl.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.lblUrl.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80)
        Me.lblUrl.Location = New System.Drawing.Point(10, 318)
        Me.lblUrl.Name = "lblUrl"
        Me.lblUrl.Size = New System.Drawing.Size(300, 22)
        Me.lblUrl.TabIndex = 1
        Me.lblUrl.Text = "http://placeholder"
        Me.lblUrl.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        ' btnCopyUrl
        '
        Me.btnCopyUrl.Location = New System.Drawing.Point(10, 348)
        Me.btnCopyUrl.Name = "btnCopyUrl"
        Me.btnCopyUrl.Size = New System.Drawing.Size(92, 28)
        Me.btnCopyUrl.TabIndex = 2
        Me.btnCopyUrl.Text = "Copy URL"
        Me.btnCopyUrl.UseVisualStyleBackColor = True
        '
        ' btnSaveImage
        '
        Me.btnSaveImage.Location = New System.Drawing.Point(114, 348)
        Me.btnSaveImage.Name = "btnSaveImage"
        Me.btnSaveImage.Size = New System.Drawing.Size(92, 28)
        Me.btnSaveImage.TabIndex = 3
        Me.btnSaveImage.Text = "Save Image"
        Me.btnSaveImage.UseVisualStyleBackColor = True
        '
        ' btnClose
        '
        Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnClose.Location = New System.Drawing.Point(218, 348)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(92, 28)
        Me.btnClose.TabIndex = 4
        Me.btnClose.Text = "Close"
        Me.btnClose.UseVisualStyleBackColor = True
        '
        ' FormQrCode
        '
        Me.BackColor = System.Drawing.Color.White
        Me.CancelButton = Me.btnClose
        Me.ClientSize = New System.Drawing.Size(320, 420)
        Me.Controls.Add(Me.picQr)
        Me.Controls.Add(Me.lblUrl)
        Me.Controls.Add(Me.btnCopyUrl)
        Me.Controls.Add(Me.btnSaveImage)
        Me.Controls.Add(Me.btnClose)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.Name = "FormQrCode"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "QR Code — Every Tongue"
        Me.TopMost = True
        CType(Me.picQr, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents picQr As System.Windows.Forms.PictureBox
    Friend WithEvents lblUrl As System.Windows.Forms.Label
    Friend WithEvents btnCopyUrl As System.Windows.Forms.Button
    Friend WithEvents btnSaveImage As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button

End Class
