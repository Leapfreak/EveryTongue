<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormServiceNames
    Inherits System.Windows.Forms.Form

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
        Me.lblInfo = New System.Windows.Forms.Label()
        Me.txtNames = New System.Windows.Forms.TextBox()
        Me.btnImport = New System.Windows.Forms.Button()
        Me.lblStatus = New System.Windows.Forms.Label()
        Me.btnOK = New System.Windows.Forms.Button()
        Me.btnCancel = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        ' lblInfo
        '
        Me.lblInfo.Location = New System.Drawing.Point(12, 12)
        Me.lblInfo.Name = "lblInfo"
        Me.lblInfo.Size = New System.Drawing.Size(536, 76)
        Me.lblInfo.TabIndex = 0
        Me.lblInfo.Anchor = CType(System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right, System.Windows.Forms.AnchorStyles)
        '
        ' txtNames
        '
        Me.txtNames.Location = New System.Drawing.Point(12, 92)
        Me.txtNames.Multiline = True
        Me.txtNames.Name = "txtNames"
        Me.txtNames.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtNames.Size = New System.Drawing.Size(536, 300)
        Me.txtNames.TabIndex = 1
        Me.txtNames.AcceptsReturn = True
        Me.txtNames.Anchor = CType(System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right, System.Windows.Forms.AnchorStyles)
        '
        ' btnImport
        '
        Me.btnImport.Location = New System.Drawing.Point(12, 402)
        Me.btnImport.Name = "btnImport"
        Me.btnImport.Size = New System.Drawing.Size(160, 28)
        Me.btnImport.TabIndex = 2
        Me.btnImport.Text = "Import from notes..."
        Me.btnImport.UseVisualStyleBackColor = True
        Me.btnImport.Anchor = CType(System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left, System.Windows.Forms.AnchorStyles)
        '
        ' lblStatus
        '
        Me.lblStatus.Location = New System.Drawing.Point(180, 402)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New System.Drawing.Size(190, 28)
        Me.lblStatus.TabIndex = 3
        Me.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.lblStatus.Anchor = CType(System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right, System.Windows.Forms.AnchorStyles)
        '
        ' btnOK
        '
        Me.btnOK.Location = New System.Drawing.Point(376, 402)
        Me.btnOK.Name = "btnOK"
        Me.btnOK.Size = New System.Drawing.Size(82, 28)
        Me.btnOK.TabIndex = 4
        Me.btnOK.Text = "OK"
        Me.btnOK.UseVisualStyleBackColor = True
        Me.btnOK.Anchor = CType(System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right, System.Windows.Forms.AnchorStyles)
        '
        ' btnCancel
        '
        Me.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnCancel.Location = New System.Drawing.Point(466, 402)
        Me.btnCancel.Name = "btnCancel"
        Me.btnCancel.Size = New System.Drawing.Size(82, 28)
        Me.btnCancel.TabIndex = 5
        Me.btnCancel.Text = "Cancel"
        Me.btnCancel.UseVisualStyleBackColor = True
        Me.btnCancel.Anchor = CType(System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right, System.Windows.Forms.AnchorStyles)
        '
        ' FormServiceNames
        '
        Me.AcceptButton = Me.btnOK
        Me.CancelButton = Me.btnCancel
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(560, 442)
        Me.Controls.Add(Me.lblInfo)
        Me.Controls.Add(Me.txtNames)
        Me.Controls.Add(Me.btnImport)
        Me.Controls.Add(Me.lblStatus)
        Me.Controls.Add(Me.btnOK)
        Me.Controls.Add(Me.btnCancel)
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New System.Drawing.Size(420, 360)
        Me.Name = "FormServiceNames"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Service Names"
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents lblInfo As System.Windows.Forms.Label
    Friend WithEvents txtNames As System.Windows.Forms.TextBox
    Friend WithEvents btnImport As System.Windows.Forms.Button
    Friend WithEvents lblStatus As System.Windows.Forms.Label
    Friend WithEvents btnOK As System.Windows.Forms.Button
    Friend WithEvents btnCancel As System.Windows.Forms.Button
End Class
