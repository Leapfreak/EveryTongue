<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormFilterSets
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
        Me.lvSets = New System.Windows.Forms.ListView()
        Me.colName = New System.Windows.Forms.ColumnHeader()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnEdit = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.grpDetail = New System.Windows.Forms.GroupBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.lblGlossary = New System.Windows.Forms.Label()
        Me.txtGlossary = New System.Windows.Forms.TextBox()
        Me.btnBrowseGlossary = New System.Windows.Forms.Button()
        Me.lblProfanity = New System.Windows.Forms.Label()
        Me.txtProfanity = New System.Windows.Forms.TextBox()
        Me.btnBrowseProfanity = New System.Windows.Forms.Button()
        Me.lblHalluc = New System.Windows.Forms.Label()
        Me.txtHalluc = New System.Windows.Forms.TextBox()
        Me.btnBrowseHalluc = New System.Windows.Forms.Button()
        Me.btnCopyGlobal = New System.Windows.Forms.Button()
        Me.btnOpenFolder = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        Me.SuspendLayout()
        '
        ' lvSets
        '
        Me.lvSets.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colName})
        Me.lvSets.FullRowSelect = True
        Me.lvSets.GridLines = True
        Me.lvSets.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvSets.HideSelection = False
        Me.lvSets.Location = New System.Drawing.Point(12, 12)
        Me.lvSets.MultiSelect = False
        Me.lvSets.Name = "lvSets"
        Me.lvSets.Size = New System.Drawing.Size(460, 140)
        Me.lvSets.TabIndex = 0
        Me.lvSets.UseCompatibleStateImageBehavior = False
        Me.lvSets.View = System.Windows.Forms.View.Details
        '
        ' colName
        '
        Me.colName.Text = "Name"
        Me.colName.Width = 430
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
        Me.grpDetail.Controls.Add(Me.btnOpenFolder)
        Me.grpDetail.Controls.Add(Me.btnCopyGlobal)
        Me.grpDetail.Controls.Add(Me.btnBrowseHalluc)
        Me.grpDetail.Controls.Add(Me.txtHalluc)
        Me.grpDetail.Controls.Add(Me.lblHalluc)
        Me.grpDetail.Controls.Add(Me.btnBrowseProfanity)
        Me.grpDetail.Controls.Add(Me.txtProfanity)
        Me.grpDetail.Controls.Add(Me.lblProfanity)
        Me.grpDetail.Controls.Add(Me.btnBrowseGlossary)
        Me.grpDetail.Controls.Add(Me.txtGlossary)
        Me.grpDetail.Controls.Add(Me.lblGlossary)
        Me.grpDetail.Controls.Add(Me.txtName)
        Me.grpDetail.Controls.Add(Me.lblName)
        Me.grpDetail.Location = New System.Drawing.Point(12, 160)
        Me.grpDetail.Name = "grpDetail"
        Me.grpDetail.Size = New System.Drawing.Size(556, 250)
        Me.grpDetail.TabIndex = 5
        Me.grpDetail.TabStop = False
        Me.grpDetail.Text = "Filter Set Details"
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
        ' lblGlossary
        '
        Me.lblGlossary.AutoSize = True
        Me.lblGlossary.Location = New System.Drawing.Point(12, 56)
        Me.lblGlossary.Name = "lblGlossary"
        Me.lblGlossary.Text = "Glossary file"
        '
        ' txtGlossary
        '
        Me.txtGlossary.Location = New System.Drawing.Point(130, 53)
        Me.txtGlossary.Name = "txtGlossary"
        Me.txtGlossary.Size = New System.Drawing.Size(330, 23)
        Me.txtGlossary.TabIndex = 2
        '
        ' btnBrowseGlossary
        '
        Me.btnBrowseGlossary.Location = New System.Drawing.Point(466, 52)
        Me.btnBrowseGlossary.Name = "btnBrowseGlossary"
        Me.btnBrowseGlossary.Size = New System.Drawing.Size(64, 25)
        Me.btnBrowseGlossary.TabIndex = 3
        Me.btnBrowseGlossary.Text = "..."
        Me.btnBrowseGlossary.UseVisualStyleBackColor = True
        '
        ' lblProfanity
        '
        Me.lblProfanity.AutoSize = True
        Me.lblProfanity.Location = New System.Drawing.Point(12, 88)
        Me.lblProfanity.Name = "lblProfanity"
        Me.lblProfanity.Text = "Profanity file"
        '
        ' txtProfanity
        '
        Me.txtProfanity.Location = New System.Drawing.Point(130, 85)
        Me.txtProfanity.Name = "txtProfanity"
        Me.txtProfanity.Size = New System.Drawing.Size(330, 23)
        Me.txtProfanity.TabIndex = 4
        '
        ' btnBrowseProfanity
        '
        Me.btnBrowseProfanity.Location = New System.Drawing.Point(466, 84)
        Me.btnBrowseProfanity.Name = "btnBrowseProfanity"
        Me.btnBrowseProfanity.Size = New System.Drawing.Size(64, 25)
        Me.btnBrowseProfanity.TabIndex = 5
        Me.btnBrowseProfanity.Text = "..."
        Me.btnBrowseProfanity.UseVisualStyleBackColor = True
        '
        ' lblHalluc
        '
        Me.lblHalluc.AutoSize = True
        Me.lblHalluc.Location = New System.Drawing.Point(12, 120)
        Me.lblHalluc.Name = "lblHalluc"
        Me.lblHalluc.Text = "Hallucinations file"
        '
        ' txtHalluc
        '
        Me.txtHalluc.Location = New System.Drawing.Point(130, 117)
        Me.txtHalluc.Name = "txtHalluc"
        Me.txtHalluc.Size = New System.Drawing.Size(330, 23)
        Me.txtHalluc.TabIndex = 6
        '
        ' btnBrowseHalluc
        '
        Me.btnBrowseHalluc.Location = New System.Drawing.Point(466, 116)
        Me.btnBrowseHalluc.Name = "btnBrowseHalluc"
        Me.btnBrowseHalluc.Size = New System.Drawing.Size(64, 25)
        Me.btnBrowseHalluc.TabIndex = 7
        Me.btnBrowseHalluc.Text = "..."
        Me.btnBrowseHalluc.UseVisualStyleBackColor = True
        '
        ' btnCopyGlobal — copy the global filter files into this set's own folder
        '
        Me.btnCopyGlobal.Location = New System.Drawing.Point(130, 156)
        Me.btnCopyGlobal.Name = "btnCopyGlobal"
        Me.btnCopyGlobal.Size = New System.Drawing.Size(220, 28)
        Me.btnCopyGlobal.TabIndex = 8
        Me.btnCopyGlobal.Text = "Copy global files into this set"
        Me.btnCopyGlobal.UseVisualStyleBackColor = True
        '
        ' btnOpenFolder
        '
        Me.btnOpenFolder.Location = New System.Drawing.Point(360, 156)
        Me.btnOpenFolder.Name = "btnOpenFolder"
        Me.btnOpenFolder.Size = New System.Drawing.Size(170, 28)
        Me.btnOpenFolder.TabIndex = 9
        Me.btnOpenFolder.Text = "Open set folder"
        Me.btnOpenFolder.UseVisualStyleBackColor = True
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 206)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 10
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 206)
        Me.btnCancelEdit.Name = "btnCancelEdit"
        Me.btnCancelEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnCancelEdit.TabIndex = 11
        Me.btnCancelEdit.Text = "Cancel"
        Me.btnCancelEdit.UseVisualStyleBackColor = True
        '
        ' FormFilterSets
        '
        Me.AcceptButton = Me.btnClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 424)
        Me.Controls.Add(Me.grpDetail)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnEdit)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.lvSets)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormFilterSets"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Filter Sets"
        Me.grpDetail.ResumeLayout(False)
        Me.grpDetail.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lvSets As System.Windows.Forms.ListView
    Friend WithEvents colName As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnAdd As System.Windows.Forms.Button
    Friend WithEvents btnEdit As System.Windows.Forms.Button
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents lblGlossary As System.Windows.Forms.Label
    Friend WithEvents txtGlossary As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseGlossary As System.Windows.Forms.Button
    Friend WithEvents lblProfanity As System.Windows.Forms.Label
    Friend WithEvents txtProfanity As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseProfanity As System.Windows.Forms.Button
    Friend WithEvents lblHalluc As System.Windows.Forms.Label
    Friend WithEvents txtHalluc As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseHalluc As System.Windows.Forms.Button
    Friend WithEvents btnCopyGlobal As System.Windows.Forms.Button
    Friend WithEvents btnOpenFolder As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
