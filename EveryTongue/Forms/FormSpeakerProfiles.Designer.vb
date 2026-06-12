<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSpeakerProfiles
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
        Me.lvSpeakers = New System.Windows.Forms.ListView()
        Me.colName = New System.Windows.Forms.ColumnHeader()
        Me.colOnline = New System.Windows.Forms.ColumnHeader()
        Me.colOffline = New System.Windows.Forms.ColumnHeader()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnEdit = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.grpDetail = New System.Windows.Forms.GroupBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.lblOnlineStt = New System.Windows.Forms.Label()
        Me.cboOnlineStt = New System.Windows.Forms.ComboBox()
        Me.lblOfflineStt = New System.Windows.Forms.Label()
        Me.cboOfflineStt = New System.Windows.Forms.ComboBox()
        Me.lblTranslate = New System.Windows.Forms.Label()
        Me.cboTranslate = New System.Windows.Forms.ComboBox()
        Me.lblTts = New System.Windows.Forms.Label()
        Me.cboTts = New System.Windows.Forms.ComboBox()
        Me.lblGlossary = New System.Windows.Forms.Label()
        Me.cboGlossary = New System.Windows.Forms.ComboBox()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        Me.SuspendLayout()
        '
        ' lvSpeakers
        '
        Me.lvSpeakers.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colName, Me.colOnline, Me.colOffline})
        Me.lvSpeakers.FullRowSelect = True
        Me.lvSpeakers.GridLines = True
        Me.lvSpeakers.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvSpeakers.HideSelection = False
        Me.lvSpeakers.Location = New System.Drawing.Point(12, 12)
        Me.lvSpeakers.MultiSelect = False
        Me.lvSpeakers.Name = "lvSpeakers"
        Me.lvSpeakers.Size = New System.Drawing.Size(460, 150)
        Me.lvSpeakers.TabIndex = 0
        Me.lvSpeakers.UseCompatibleStateImageBehavior = False
        Me.lvSpeakers.View = System.Windows.Forms.View.Details
        '
        ' colName
        '
        Me.colName.Text = "Name"
        Me.colName.Width = 160
        '
        ' colOnline
        '
        Me.colOnline.Text = "Online STT"
        Me.colOnline.Width = 140
        '
        ' colOffline
        '
        Me.colOffline.Text = "Offline STT"
        Me.colOffline.Width = 140
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
        Me.grpDetail.Controls.Add(Me.cboGlossary)
        Me.grpDetail.Controls.Add(Me.lblGlossary)
        Me.grpDetail.Controls.Add(Me.cboTts)
        Me.grpDetail.Controls.Add(Me.lblTts)
        Me.grpDetail.Controls.Add(Me.cboTranslate)
        Me.grpDetail.Controls.Add(Me.lblTranslate)
        Me.grpDetail.Controls.Add(Me.cboOfflineStt)
        Me.grpDetail.Controls.Add(Me.lblOfflineStt)
        Me.grpDetail.Controls.Add(Me.cboOnlineStt)
        Me.grpDetail.Controls.Add(Me.lblOnlineStt)
        Me.grpDetail.Controls.Add(Me.txtName)
        Me.grpDetail.Controls.Add(Me.lblName)
        Me.grpDetail.Location = New System.Drawing.Point(12, 170)
        Me.grpDetail.Name = "grpDetail"
        Me.grpDetail.Size = New System.Drawing.Size(556, 268)
        Me.grpDetail.TabIndex = 5
        Me.grpDetail.TabStop = False
        Me.grpDetail.Text = "Speaker Details"
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
        Me.txtName.Location = New System.Drawing.Point(170, 21)
        Me.txtName.Name = "txtName"
        Me.txtName.Size = New System.Drawing.Size(280, 23)
        Me.txtName.TabIndex = 1
        '
        ' lblOnlineStt
        '
        Me.lblOnlineStt.AutoSize = True
        Me.lblOnlineStt.Location = New System.Drawing.Point(12, 56)
        Me.lblOnlineStt.Name = "lblOnlineStt"
        Me.lblOnlineStt.Text = "Online STT template"
        '
        ' cboOnlineStt
        '
        Me.cboOnlineStt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboOnlineStt.Location = New System.Drawing.Point(170, 53)
        Me.cboOnlineStt.Name = "cboOnlineStt"
        Me.cboOnlineStt.Size = New System.Drawing.Size(280, 23)
        Me.cboOnlineStt.TabIndex = 2
        '
        ' lblOfflineStt
        '
        Me.lblOfflineStt.AutoSize = True
        Me.lblOfflineStt.Location = New System.Drawing.Point(12, 88)
        Me.lblOfflineStt.Name = "lblOfflineStt"
        Me.lblOfflineStt.Text = "Offline STT template"
        '
        ' cboOfflineStt
        '
        Me.cboOfflineStt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboOfflineStt.Location = New System.Drawing.Point(170, 85)
        Me.cboOfflineStt.Name = "cboOfflineStt"
        Me.cboOfflineStt.Size = New System.Drawing.Size(280, 23)
        Me.cboOfflineStt.TabIndex = 3
        '
        ' lblTranslate
        '
        Me.lblTranslate.AutoSize = True
        Me.lblTranslate.Location = New System.Drawing.Point(12, 120)
        Me.lblTranslate.Name = "lblTranslate"
        Me.lblTranslate.Text = "Translation template"
        '
        ' cboTranslate
        '
        Me.cboTranslate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTranslate.Location = New System.Drawing.Point(170, 117)
        Me.cboTranslate.Name = "cboTranslate"
        Me.cboTranslate.Size = New System.Drawing.Size(280, 23)
        Me.cboTranslate.TabIndex = 4
        '
        ' lblTts
        '
        Me.lblTts.AutoSize = True
        Me.lblTts.Location = New System.Drawing.Point(12, 152)
        Me.lblTts.Name = "lblTts"
        Me.lblTts.Text = "TTS template"
        '
        ' cboTts
        '
        Me.cboTts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTts.Location = New System.Drawing.Point(170, 149)
        Me.cboTts.Name = "cboTts"
        Me.cboTts.Size = New System.Drawing.Size(280, 23)
        Me.cboTts.TabIndex = 5
        '
        ' lblGlossary — activates with per-session filter sets (Phase 8)
        '
        Me.lblGlossary.AutoSize = True
        Me.lblGlossary.Location = New System.Drawing.Point(12, 184)
        Me.lblGlossary.Name = "lblGlossary"
        Me.lblGlossary.Text = "Glossary set"
        '
        ' cboGlossary
        '
        Me.cboGlossary.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboGlossary.Enabled = False
        Me.cboGlossary.Location = New System.Drawing.Point(170, 181)
        Me.cboGlossary.Name = "cboGlossary"
        Me.cboGlossary.Size = New System.Drawing.Size(280, 23)
        Me.cboGlossary.TabIndex = 6
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 222)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 7
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 222)
        Me.btnCancelEdit.Name = "btnCancelEdit"
        Me.btnCancelEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnCancelEdit.TabIndex = 8
        Me.btnCancelEdit.Text = "Cancel"
        Me.btnCancelEdit.UseVisualStyleBackColor = True
        '
        ' FormSpeakerProfiles
        '
        Me.AcceptButton = Me.btnClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 452)
        Me.Controls.Add(Me.grpDetail)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnEdit)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.lvSpeakers)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormSpeakerProfiles"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Speakers"
        Me.grpDetail.ResumeLayout(False)
        Me.grpDetail.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lvSpeakers As System.Windows.Forms.ListView
    Friend WithEvents colName As System.Windows.Forms.ColumnHeader
    Friend WithEvents colOnline As System.Windows.Forms.ColumnHeader
    Friend WithEvents colOffline As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnAdd As System.Windows.Forms.Button
    Friend WithEvents btnEdit As System.Windows.Forms.Button
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents lblOnlineStt As System.Windows.Forms.Label
    Friend WithEvents cboOnlineStt As System.Windows.Forms.ComboBox
    Friend WithEvents lblOfflineStt As System.Windows.Forms.Label
    Friend WithEvents cboOfflineStt As System.Windows.Forms.ComboBox
    Friend WithEvents lblTranslate As System.Windows.Forms.Label
    Friend WithEvents cboTranslate As System.Windows.Forms.ComboBox
    Friend WithEvents lblTts As System.Windows.Forms.Label
    Friend WithEvents cboTts As System.Windows.Forms.ComboBox
    Friend WithEvents lblGlossary As System.Windows.Forms.Label
    Friend WithEvents cboGlossary As System.Windows.Forms.ComboBox
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
