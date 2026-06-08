<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLogViewer
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
        lblSession = New Label()
        cboSession = New ComboBox()
        lblSessionInfo = New Label()
        lblCategory = New Label()
        cboFilterCategory = New ComboBox()
        lblLevel = New Label()
        cboFilterLevel = New ComboBox()
        txtSearch = New TextBox()
        btnSearchNext = New Button()
        btnCopy = New Button()
        btnClose = New Button()
        dgvLogHistory = New DataGridView()
        colTime = New DataGridViewTextBoxColumn()
        colCategory = New DataGridViewTextBoxColumn()
        colLevel = New DataGridViewTextBoxColumn()
        colMessage = New DataGridViewTextBoxColumn()
        CType(dgvLogHistory, System.ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        '
        ' lblSession
        '
        lblSession.AutoSize = True
        lblSession.Location = New Drawing.Point(12, 15)
        lblSession.Name = "lblSession"
        lblSession.Size = New Drawing.Size(48, 15)
        lblSession.Text = "Session:"
        '
        ' cboSession
        '
        cboSession.DropDownStyle = ComboBoxStyle.DropDownList
        cboSession.Location = New Drawing.Point(66, 12)
        cboSession.Name = "cboSession"
        cboSession.Size = New Drawing.Size(350, 23)
        cboSession.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        '
        ' lblSessionInfo
        '
        lblSessionInfo.AutoSize = True
        lblSessionInfo.Location = New Drawing.Point(422, 15)
        lblSessionInfo.Name = "lblSessionInfo"
        lblSessionInfo.Size = New Drawing.Size(10, 15)
        lblSessionInfo.Text = ""
        lblSessionInfo.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        '
        ' lblCategory
        '
        lblCategory.AutoSize = True
        lblCategory.Location = New Drawing.Point(12, 47)
        lblCategory.Name = "lblCategory"
        lblCategory.Size = New Drawing.Size(55, 15)
        lblCategory.Text = "Category:"
        '
        ' cboFilterCategory
        '
        cboFilterCategory.DropDownStyle = ComboBoxStyle.DropDownList
        cboFilterCategory.Location = New Drawing.Point(73, 44)
        cboFilterCategory.Name = "cboFilterCategory"
        cboFilterCategory.Size = New Drawing.Size(130, 23)
        '
        ' lblLevel
        '
        lblLevel.AutoSize = True
        lblLevel.Location = New Drawing.Point(213, 47)
        lblLevel.Name = "lblLevel"
        lblLevel.Size = New Drawing.Size(37, 15)
        lblLevel.Text = "Level:"
        '
        ' cboFilterLevel
        '
        cboFilterLevel.DropDownStyle = ComboBoxStyle.DropDownList
        cboFilterLevel.Location = New Drawing.Point(256, 44)
        cboFilterLevel.Name = "cboFilterLevel"
        cboFilterLevel.Size = New Drawing.Size(100, 23)
        '
        ' txtSearch
        '
        txtSearch.Location = New Drawing.Point(366, 44)
        txtSearch.Name = "txtSearch"
        txtSearch.Size = New Drawing.Size(200, 23)
        txtSearch.PlaceholderText = "Search..."
        txtSearch.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        '
        ' btnSearchNext
        '
        btnSearchNext.Location = New Drawing.Point(572, 43)
        btnSearchNext.Name = "btnSearchNext"
        btnSearchNext.Size = New Drawing.Size(60, 25)
        btnSearchNext.Text = "Next"
        btnSearchNext.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        '
        ' btnCopy
        '
        btnCopy.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnCopy.Location = New Drawing.Point(768, 43)
        btnCopy.Name = "btnCopy"
        btnCopy.Size = New Drawing.Size(75, 25)
        btnCopy.Text = "Copy"
        '
        ' btnClose
        '
        btnClose.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnClose.Location = New Drawing.Point(768, 527)
        btnClose.Name = "btnClose"
        btnClose.Size = New Drawing.Size(75, 28)
        btnClose.Text = "Close"
        btnClose.DialogResult = DialogResult.Cancel
        '
        ' dgvLogHistory
        '
        dgvLogHistory.AllowUserToAddRows = False
        dgvLogHistory.AllowUserToDeleteRows = False
        dgvLogHistory.AllowUserToResizeRows = False
        dgvLogHistory.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        dgvLogHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvLogHistory.Columns.AddRange(New DataGridViewColumn() {colTime, colCategory, colLevel, colMessage})
        dgvLogHistory.Location = New Drawing.Point(12, 75)
        dgvLogHistory.Name = "dgvLogHistory"
        dgvLogHistory.ReadOnly = True
        dgvLogHistory.RowHeadersVisible = False
        dgvLogHistory.RowTemplate.Height = 22
        dgvLogHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvLogHistory.Size = New Drawing.Size(831, 445)
        '
        ' colTime
        '
        colTime.HeaderText = "Time"
        colTime.Name = "colTime"
        colTime.ReadOnly = True
        colTime.Width = 90
        '
        ' colCategory
        '
        colCategory.HeaderText = "Category"
        colCategory.Name = "colCategory"
        colCategory.ReadOnly = True
        colCategory.Width = 90
        '
        ' colLevel
        '
        colLevel.HeaderText = "Level"
        colLevel.Name = "colLevel"
        colLevel.ReadOnly = True
        colLevel.Width = 70
        '
        ' colMessage
        '
        colMessage.HeaderText = "Message"
        colMessage.Name = "colMessage"
        colMessage.ReadOnly = True
        colMessage.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        '
        ' FormLogViewer
        '
        CancelButton = btnClose
        ClientSize = New Drawing.Size(855, 565)
        Controls.Add(lblSession)
        Controls.Add(cboSession)
        Controls.Add(lblSessionInfo)
        Controls.Add(lblCategory)
        Controls.Add(cboFilterCategory)
        Controls.Add(lblLevel)
        Controls.Add(cboFilterLevel)
        Controls.Add(txtSearch)
        Controls.Add(btnSearchNext)
        Controls.Add(btnCopy)
        Controls.Add(btnClose)
        Controls.Add(dgvLogHistory)
        MinimumSize = New Drawing.Size(700, 400)
        Name = "FormLogViewer"
        StartPosition = FormStartPosition.CenterParent
        Text = "Session Logs"
        CType(dgvLogHistory, System.ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lblSession As Label
    Friend WithEvents cboSession As ComboBox
    Friend WithEvents lblSessionInfo As Label
    Friend WithEvents lblCategory As Label
    Friend WithEvents cboFilterCategory As ComboBox
    Friend WithEvents lblLevel As Label
    Friend WithEvents cboFilterLevel As ComboBox
    Friend WithEvents txtSearch As TextBox
    Friend WithEvents btnSearchNext As Button
    Friend WithEvents btnCopy As Button
    Friend WithEvents btnClose As Button
    Friend WithEvents dgvLogHistory As DataGridView
    Friend WithEvents colTime As DataGridViewTextBoxColumn
    Friend WithEvents colCategory As DataGridViewTextBoxColumn
    Friend WithEvents colLevel As DataGridViewTextBoxColumn
    Friend WithEvents colMessage As DataGridViewTextBoxColumn

End Class
