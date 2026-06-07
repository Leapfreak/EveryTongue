<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLogConfig
    Inherits System.Windows.Forms.Form

    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Private Sub InitializeComponent()
        lblPreset = New Label()
        cboPreset = New ComboBox()
        dgvRouting = New DataGridView()
        colCategory = New DataGridViewTextBoxColumn()
        colEnabled = New DataGridViewCheckBoxColumn()
        colFileLevel = New DataGridViewComboBoxColumn()
        colUiLevel = New DataGridViewComboBoxColumn()
        btnOK = New Button()
        btnCancel = New Button()
        CType(dgvRouting, System.ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        '
        ' lblPreset
        '
        lblPreset.AutoSize = True
        lblPreset.Location = New Drawing.Point(12, 15)
        lblPreset.Name = "lblPreset"
        lblPreset.Size = New Drawing.Size(42, 15)
        lblPreset.Text = "Preset:"
        '
        ' cboPreset
        '
        cboPreset.DropDownStyle = ComboBoxStyle.DropDownList
        cboPreset.Items.AddRange(New Object() {"Minimal", "Normal", "Verbose", "Custom"})
        cboPreset.Location = New Drawing.Point(60, 12)
        cboPreset.Name = "cboPreset"
        cboPreset.Size = New Drawing.Size(120, 23)
        '
        ' dgvRouting
        '
        dgvRouting.AllowUserToAddRows = False
        dgvRouting.AllowUserToDeleteRows = False
        dgvRouting.AllowUserToResizeRows = False
        dgvRouting.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        dgvRouting.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvRouting.Columns.AddRange(New DataGridViewColumn() {colCategory, colEnabled, colFileLevel, colUiLevel})
        dgvRouting.Location = New Drawing.Point(12, 44)
        dgvRouting.Name = "dgvRouting"
        dgvRouting.RowHeadersVisible = False
        dgvRouting.RowTemplate.Height = 22
        dgvRouting.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvRouting.Size = New Drawing.Size(460, 340)
        '
        ' colCategory
        '
        colCategory.HeaderText = "Category"
        colCategory.Name = "colCategory"
        colCategory.ReadOnly = True
        colCategory.Width = 120
        '
        ' colEnabled
        '
        colEnabled.HeaderText = "Enabled"
        colEnabled.Name = "colEnabled"
        colEnabled.Width = 60
        '
        ' colFileLevel
        '
        colFileLevel.HeaderText = "File Level"
        colFileLevel.Name = "colFileLevel"
        colFileLevel.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        colFileLevel.Width = 100
        '
        ' colUiLevel
        '
        colUiLevel.HeaderText = "UI Level"
        colUiLevel.Name = "colUiLevel"
        colUiLevel.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        colUiLevel.Width = 100
        '
        ' btnOK
        '
        btnOK.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnOK.Location = New Drawing.Point(316, 394)
        btnOK.Name = "btnOK"
        btnOK.Size = New Drawing.Size(75, 28)
        btnOK.Text = "OK"
        btnOK.DialogResult = DialogResult.OK
        '
        ' btnCancel
        '
        btnCancel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnCancel.Location = New Drawing.Point(397, 394)
        btnCancel.Name = "btnCancel"
        btnCancel.Size = New Drawing.Size(75, 28)
        btnCancel.Text = "Cancel"
        btnCancel.DialogResult = DialogResult.Cancel
        '
        ' FormLogConfig
        '
        AcceptButton = btnOK
        CancelButton = btnCancel
        ClientSize = New Drawing.Size(484, 432)
        Controls.Add(lblPreset)
        Controls.Add(cboPreset)
        Controls.Add(dgvRouting)
        Controls.Add(btnOK)
        Controls.Add(btnCancel)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormLogConfig"
        StartPosition = FormStartPosition.CenterParent
        Text = "Log Configuration"
        CType(dgvRouting, System.ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lblPreset As Label
    Friend WithEvents cboPreset As ComboBox
    Friend WithEvents dgvRouting As DataGridView
    Friend WithEvents colCategory As DataGridViewTextBoxColumn
    Friend WithEvents colEnabled As DataGridViewCheckBoxColumn
    Friend WithEvents colFileLevel As DataGridViewComboBoxColumn
    Friend WithEvents colUiLevel As DataGridViewComboBoxColumn
    Friend WithEvents btnOK As Button
    Friend WithEvents btnCancel As Button

End Class
