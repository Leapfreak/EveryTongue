<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormDisplayTemplates
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
        Me.lvTemplates = New System.Windows.Forms.ListView()
        Me.colName = New System.Windows.Forms.ColumnHeader()
        Me.colLangs = New System.Windows.Forms.ColumnHeader()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnEdit = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.grpDetail = New System.Windows.Forms.GroupBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.lblBg = New System.Windows.Forms.Label()
        Me.btnBg = New System.Windows.Forms.Button()
        Me.lblFg = New System.Windows.Forms.Label()
        Me.btnFg = New System.Windows.Forms.Button()
        Me.lblFont = New System.Windows.Forms.Label()
        Me.cboFont = New System.Windows.Forms.ComboBox()
        Me.lblSize = New System.Windows.Forms.Label()
        Me.nudSize = New System.Windows.Forms.NumericUpDown()
        Me.chkBold = New System.Windows.Forms.CheckBox()
        Me.lblLayout = New System.Windows.Forms.Label()
        Me.cboLayout = New System.Windows.Forms.ComboBox()
        Me.lblOffered = New System.Windows.Forms.Label()
        Me.clbOffered = New System.Windows.Forms.CheckedListBox()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        CType(Me.nudSize, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        ' lvTemplates
        '
        Me.lvTemplates.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colName, Me.colLangs})
        Me.lvTemplates.FullRowSelect = True
        Me.lvTemplates.GridLines = True
        Me.lvTemplates.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvTemplates.HideSelection = False
        Me.lvTemplates.Location = New System.Drawing.Point(12, 12)
        Me.lvTemplates.MultiSelect = False
        Me.lvTemplates.Name = "lvTemplates"
        Me.lvTemplates.Size = New System.Drawing.Size(460, 140)
        Me.lvTemplates.TabIndex = 0
        Me.lvTemplates.UseCompatibleStateImageBehavior = False
        Me.lvTemplates.View = System.Windows.Forms.View.Details
        '
        ' colName
        '
        Me.colName.Text = "Name"
        Me.colName.Width = 240
        '
        ' colLangs
        '
        Me.colLangs.Text = "Offered Languages"
        Me.colLangs.Width = 190
        '
        ' btnAdd
        '
        Me.btnAdd.Location = New System.Drawing.Point(478, 12)
        Me.btnAdd.Name = "btnAdd"
        Me.btnAdd.Size = New System.Drawing.Size(90, 30)
        Me.btnAdd.TabIndex = 1
        Me.btnAdd.Text = "Add"
        Me.btnAdd.UseVisualStyleBackColor = True
        '
        ' btnEdit
        '
        Me.btnEdit.Location = New System.Drawing.Point(478, 48)
        Me.btnEdit.Name = "btnEdit"
        Me.btnEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnEdit.TabIndex = 2
        Me.btnEdit.Text = "Edit"
        Me.btnEdit.UseVisualStyleBackColor = True
        '
        ' btnDelete
        '
        Me.btnDelete.Location = New System.Drawing.Point(478, 84)
        Me.btnDelete.Name = "btnDelete"
        Me.btnDelete.Size = New System.Drawing.Size(90, 30)
        Me.btnDelete.TabIndex = 3
        Me.btnDelete.Text = "Delete"
        Me.btnDelete.UseVisualStyleBackColor = True
        '
        ' btnClose
        '
        Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.btnClose.Location = New System.Drawing.Point(478, 122)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(90, 30)
        Me.btnClose.TabIndex = 4
        Me.btnClose.Text = "Close"
        Me.btnClose.UseVisualStyleBackColor = True
        '
        ' grpDetail
        '
        Me.grpDetail.Controls.Add(Me.btnCancelEdit)
        Me.grpDetail.Controls.Add(Me.btnSave)
        Me.grpDetail.Controls.Add(Me.clbOffered)
        Me.grpDetail.Controls.Add(Me.lblOffered)
        Me.grpDetail.Controls.Add(Me.cboLayout)
        Me.grpDetail.Controls.Add(Me.lblLayout)
        Me.grpDetail.Controls.Add(Me.chkBold)
        Me.grpDetail.Controls.Add(Me.nudSize)
        Me.grpDetail.Controls.Add(Me.lblSize)
        Me.grpDetail.Controls.Add(Me.cboFont)
        Me.grpDetail.Controls.Add(Me.lblFont)
        Me.grpDetail.Controls.Add(Me.btnFg)
        Me.grpDetail.Controls.Add(Me.lblFg)
        Me.grpDetail.Controls.Add(Me.btnBg)
        Me.grpDetail.Controls.Add(Me.lblBg)
        Me.grpDetail.Controls.Add(Me.txtName)
        Me.grpDetail.Controls.Add(Me.lblName)
        Me.grpDetail.Location = New System.Drawing.Point(12, 160)
        Me.grpDetail.Name = "grpDetail"
        Me.grpDetail.Size = New System.Drawing.Size(556, 348)
        Me.grpDetail.TabIndex = 5
        Me.grpDetail.TabStop = False
        Me.grpDetail.Text = "Display Details"
        Me.grpDetail.Visible = False
        '
        ' lblName
        '
        Me.lblName.AutoSize = True
        Me.lblName.Location = New System.Drawing.Point(12, 24)
        Me.lblName.Name = "lblName"
        Me.lblName.Text = "Name"
        '
        ' txtName
        '
        Me.txtName.Location = New System.Drawing.Point(130, 21)
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(280, 23)
        Me.txtName.TabIndex = 1
        '
        ' lblBg
        '
        Me.lblBg.AutoSize = True
        Me.lblBg.Location = New System.Drawing.Point(12, 56)
        Me.lblBg.Name = "lblBg"
        Me.lblBg.Text = "Background"
        '
        ' btnBg
        '
        Me.btnBg.BackColor = System.Drawing.Color.Black
        Me.btnBg.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnBg.Location = New System.Drawing.Point(130, 52)
        Me.btnBg.Name = "btnBg"
        Me.btnBg.Size = New System.Drawing.Size(80, 23)
        Me.btnBg.TabIndex = 2
        Me.btnBg.UseVisualStyleBackColor = False
        '
        ' lblFg
        '
        Me.lblFg.AutoSize = True
        Me.lblFg.Location = New System.Drawing.Point(230, 56)
        Me.lblFg.Name = "lblFg"
        Me.lblFg.Text = "Text"
        '
        ' btnFg
        '
        Me.btnFg.BackColor = System.Drawing.Color.White
        Me.btnFg.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btnFg.Location = New System.Drawing.Point(290, 52)
        Me.btnFg.Name = "btnFg"
        Me.btnFg.Size = New System.Drawing.Size(80, 23)
        Me.btnFg.TabIndex = 3
        Me.btnFg.UseVisualStyleBackColor = False
        '
        ' lblFont
        '
        Me.lblFont.AutoSize = True
        Me.lblFont.Location = New System.Drawing.Point(12, 88)
        Me.lblFont.Name = "lblFont"
        Me.lblFont.Text = "Font"
        '
        ' cboFont
        '
        Me.cboFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboFont.Location = New System.Drawing.Point(130, 85)
        Me.cboFont.Name = "cboFont"
        Me.cboFont.Size = New System.Drawing.Size(200, 23)
        Me.cboFont.TabIndex = 4
        '
        ' lblSize
        '
        Me.lblSize.AutoSize = True
        Me.lblSize.Location = New System.Drawing.Point(350, 88)
        Me.lblSize.Name = "lblSize"
        Me.lblSize.Text = "Size"
        '
        ' nudSize
        '
        Me.nudSize.Location = New System.Drawing.Point(390, 85)
        Me.nudSize.Minimum = New Decimal(New Integer() {8, 0, 0, 0})
        Me.nudSize.Maximum = New Decimal(New Integer() {72, 0, 0, 0})
        Me.nudSize.Name = "nudSize"
        Me.nudSize.Size = New System.Drawing.Size(55, 23)
        Me.nudSize.TabIndex = 5
        Me.nudSize.Value = New Decimal(New Integer() {14, 0, 0, 0})
        '
        ' chkBold
        '
        Me.chkBold.AutoSize = True
        Me.chkBold.Checked = True
        Me.chkBold.CheckState = System.Windows.Forms.CheckState.Checked
        Me.chkBold.Location = New System.Drawing.Point(460, 87)
        Me.chkBold.Name = "chkBold"
        Me.chkBold.TabIndex = 6
        Me.chkBold.Text = "Bold"
        Me.chkBold.UseVisualStyleBackColor = True
        '
        ' lblLayout
        '
        Me.lblLayout.AutoSize = True
        Me.lblLayout.Location = New System.Drawing.Point(12, 120)
        Me.lblLayout.Name = "lblLayout"
        Me.lblLayout.Text = "Layout"
        '
        ' cboLayout
        '
        Me.cboLayout.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboLayout.Location = New System.Drawing.Point(130, 117)
        Me.cboLayout.Name = "cboLayout"
        Me.cboLayout.Size = New System.Drawing.Size(200, 23)
        Me.cboLayout.TabIndex = 7
        '
        ' lblOffered
        '
        Me.lblOffered.AutoSize = True
        Me.lblOffered.Location = New System.Drawing.Point(12, 150)
        Me.lblOffered.Name = "lblOffered"
        Me.lblOffered.Text = "Offered languages (none checked = all)"
        '
        ' clbOffered
        '
        Me.clbOffered.CheckOnClick = True
        Me.clbOffered.IntegralHeight = False
        Me.clbOffered.Location = New System.Drawing.Point(12, 170)
        Me.clbOffered.Name = "clbOffered"
        Me.clbOffered.Size = New System.Drawing.Size(530, 130)
        Me.clbOffered.TabIndex = 8
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 308)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 9
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 308)
        Me.btnCancelEdit.Name = "btnCancelEdit"
        Me.btnCancelEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnCancelEdit.TabIndex = 10
        Me.btnCancelEdit.Text = "Cancel"
        Me.btnCancelEdit.UseVisualStyleBackColor = True
        '
        ' FormDisplayTemplates
        '
        Me.AcceptButton = Me.btnClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 522)
        Me.Controls.Add(Me.grpDetail)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnEdit)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.lvTemplates)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormDisplayTemplates"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Display Templates"
        Me.grpDetail.ResumeLayout(False)
        Me.grpDetail.PerformLayout()
        CType(Me.nudSize, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lvTemplates As System.Windows.Forms.ListView
    Friend WithEvents colName As System.Windows.Forms.ColumnHeader
    Friend WithEvents colLangs As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnAdd As System.Windows.Forms.Button
    Friend WithEvents btnEdit As System.Windows.Forms.Button
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents lblBg As System.Windows.Forms.Label
    Friend WithEvents btnBg As System.Windows.Forms.Button
    Friend WithEvents lblFg As System.Windows.Forms.Label
    Friend WithEvents btnFg As System.Windows.Forms.Button
    Friend WithEvents lblFont As System.Windows.Forms.Label
    Friend WithEvents cboFont As System.Windows.Forms.ComboBox
    Friend WithEvents lblSize As System.Windows.Forms.Label
    Friend WithEvents nudSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents chkBold As System.Windows.Forms.CheckBox
    Friend WithEvents lblLayout As System.Windows.Forms.Label
    Friend WithEvents cboLayout As System.Windows.Forms.ComboBox
    Friend WithEvents lblOffered As System.Windows.Forms.Label
    Friend WithEvents clbOffered As System.Windows.Forms.CheckedListBox
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
