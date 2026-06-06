<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLanguageChooser
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
        lblPrompt = New Label()
        txtSearch = New TextBox()
        lstLanguages = New ListBox()
        btnOK = New Button()
        btnCancel = New Button()
        SuspendLayout()
        '
        ' lblPrompt
        '
        lblPrompt.AutoSize = True
        lblPrompt.Location = New Point(12, 12)
        lblPrompt.Name = "lblPrompt"
        lblPrompt.Size = New Size(130, 15)
        lblPrompt.TabIndex = 0
        lblPrompt.Text = "Search for a language:"
        '
        ' txtSearch
        '
        txtSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtSearch.Location = New Point(12, 30)
        txtSearch.Name = "txtSearch"
        txtSearch.Size = New Size(310, 23)
        txtSearch.TabIndex = 1
        '
        ' lstLanguages
        '
        lstLanguages.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lstLanguages.FormattingEnabled = True
        lstLanguages.ItemHeight = 15
        lstLanguages.Location = New Point(12, 59)
        lstLanguages.Name = "lstLanguages"
        lstLanguages.Size = New Size(310, 244)
        lstLanguages.TabIndex = 2
        '
        ' btnOK
        '
        btnOK.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnOK.Location = New Point(166, 315)
        btnOK.Name = "btnOK"
        btnOK.Size = New Size(75, 28)
        btnOK.TabIndex = 3
        btnOK.Text = "OK"
        btnOK.UseVisualStyleBackColor = True
        '
        ' btnCancel
        '
        btnCancel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Location = New Point(247, 315)
        btnCancel.Name = "btnCancel"
        btnCancel.Size = New Size(75, 28)
        btnCancel.TabIndex = 4
        btnCancel.Text = "Cancel"
        btnCancel.UseVisualStyleBackColor = True
        '
        ' FormLanguageChooser
        '
        AcceptButton = btnOK
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        CancelButton = btnCancel
        ClientSize = New Size(334, 351)
        Controls.Add(lblPrompt)
        Controls.Add(txtSearch)
        Controls.Add(lstLanguages)
        Controls.Add(btnOK)
        Controls.Add(btnCancel)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormLanguageChooser"
        StartPosition = FormStartPosition.CenterParent
        Text = "Select Language"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lblPrompt As Label
    Friend WithEvents txtSearch As TextBox
    Friend WithEvents lstLanguages As ListBox
    Friend WithEvents btnOK As Button
    Friend WithEvents btnCancel As Button

End Class
