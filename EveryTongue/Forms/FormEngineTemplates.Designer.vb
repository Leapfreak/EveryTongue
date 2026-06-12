<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormEngineTemplates
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
        Me.colEngine = New System.Windows.Forms.ColumnHeader()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnEdit = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.grpDetail = New System.Windows.Forms.GroupBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.lblEngine = New System.Windows.Forms.Label()
        Me.cboEngine = New System.Windows.Forms.ComboBox()
        Me.chkIncludeAdvanced = New System.Windows.Forms.CheckBox()
        Me.pnlFields = New System.Windows.Forms.Panel()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        Me.SuspendLayout()
        '
        ' lvTemplates
        '
        Me.lvTemplates.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colName, Me.colEngine})
        Me.lvTemplates.FullRowSelect = True
        Me.lvTemplates.GridLines = True
        Me.lvTemplates.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvTemplates.HideSelection = False
        Me.lvTemplates.Location = New System.Drawing.Point(12, 12)
        Me.lvTemplates.MultiSelect = False
        Me.lvTemplates.Name = "lvTemplates"
        Me.lvTemplates.Size = New System.Drawing.Size(460, 150)
        Me.lvTemplates.TabIndex = 0
        Me.lvTemplates.UseCompatibleStateImageBehavior = False
        Me.lvTemplates.View = System.Windows.Forms.View.Details
        '
        ' colName
        '
        Me.colName.Text = "Name"
        Me.colName.Width = 240
        '
        ' colEngine
        '
        Me.colEngine.Text = "Engine"
        Me.colEngine.Width = 190
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
        Me.btnClose.Location = New System.Drawing.Point(478, 132)
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
        Me.grpDetail.Controls.Add(Me.chkIncludeAdvanced)
        Me.grpDetail.Controls.Add(Me.pnlFields)
        Me.grpDetail.Controls.Add(Me.cboEngine)
        Me.grpDetail.Controls.Add(Me.lblEngine)
        Me.grpDetail.Controls.Add(Me.txtName)
        Me.grpDetail.Controls.Add(Me.lblName)
        Me.grpDetail.Location = New System.Drawing.Point(12, 170)
        Me.grpDetail.Name = "grpDetail"
        Me.grpDetail.Size = New System.Drawing.Size(556, 360)
        Me.grpDetail.TabIndex = 5
        Me.grpDetail.TabStop = False
        Me.grpDetail.Text = "Template Details"
        Me.grpDetail.Visible = False
        '
        ' lblName
        '
        Me.lblName.AutoSize = True
        Me.lblName.Location = New System.Drawing.Point(12, 24)
        Me.lblName.Name = "lblName"
        Me.lblName.Size = New System.Drawing.Size(39, 15)
        Me.lblName.TabIndex = 0
        Me.lblName.Text = "Name"
        '
        ' txtName
        '
        Me.txtName.Location = New System.Drawing.Point(130, 21)
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(280, 23)
        Me.txtName.TabIndex = 1
        '
        ' lblEngine
        '
        Me.lblEngine.AutoSize = True
        Me.lblEngine.Location = New System.Drawing.Point(12, 52)
        Me.lblEngine.Name = "lblEngine"
        Me.lblEngine.Size = New System.Drawing.Size(43, 15)
        Me.lblEngine.TabIndex = 2
        Me.lblEngine.Text = "Engine"
        '
        ' cboEngine
        '
        Me.cboEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboEngine.Location = New System.Drawing.Point(130, 49)
        Me.cboEngine.Name = "cboEngine"
        Me.cboEngine.Size = New System.Drawing.Size(280, 23)
        Me.cboEngine.TabIndex = 3
        '
        ' chkIncludeAdvanced — whether advanced fields are stored in (pinned by)
        ' this template; unchecked, they keep following the app-global settings.
        '
        Me.chkIncludeAdvanced.AutoSize = True
        Me.chkIncludeAdvanced.Location = New System.Drawing.Point(130, 78)
        Me.chkIncludeAdvanced.Name = "chkIncludeAdvanced"
        Me.chkIncludeAdvanced.Size = New System.Drawing.Size(200, 19)
        Me.chkIncludeAdvanced.TabIndex = 7
        Me.chkIncludeAdvanced.Text = "Include advanced settings"
        Me.chkIncludeAdvanced.UseVisualStyleBackColor = True
        '
        ' pnlFields — rows are generated at runtime from the selected engine's
        ' config descriptor (the field set depends on the engine, so this is
        ' genuinely runtime-generated content).
        '
        Me.pnlFields.AutoScroll = True
        Me.pnlFields.Location = New System.Drawing.Point(12, 104)
        Me.pnlFields.Name = "pnlFields"
        Me.pnlFields.Size = New System.Drawing.Size(532, 208)
        Me.pnlFields.TabIndex = 4
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 320)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 5
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 320)
        Me.btnCancelEdit.Name = "btnCancelEdit"
        Me.btnCancelEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnCancelEdit.TabIndex = 6
        Me.btnCancelEdit.Text = "Cancel"
        Me.btnCancelEdit.UseVisualStyleBackColor = True
        '
        ' FormEngineTemplates
        '
        Me.AcceptButton = Me.btnClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 546)
        Me.Controls.Add(Me.grpDetail)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnEdit)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.lvTemplates)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormEngineTemplates"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "STT Templates"
        Me.grpDetail.ResumeLayout(False)
        Me.grpDetail.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lvTemplates As System.Windows.Forms.ListView
    Friend WithEvents colName As System.Windows.Forms.ColumnHeader
    Friend WithEvents colEngine As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnAdd As System.Windows.Forms.Button
    Friend WithEvents btnEdit As System.Windows.Forms.Button
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents lblEngine As System.Windows.Forms.Label
    Friend WithEvents cboEngine As System.Windows.Forms.ComboBox
    Friend WithEvents chkIncludeAdvanced As System.Windows.Forms.CheckBox
    Friend WithEvents pnlFields As System.Windows.Forms.Panel
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
