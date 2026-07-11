<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTemplateManager
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
        Me.colHostingCode = New System.Windows.Forms.ColumnHeader()
        Me.colLanguage = New System.Windows.Forms.ColumnHeader()
        Me.colSttEngine = New System.Windows.Forms.ColumnHeader()
        Me.btnAdd = New System.Windows.Forms.Button()
        Me.btnEdit = New System.Windows.Forms.Button()
        Me.btnDelete = New System.Windows.Forms.Button()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.grpDetail = New System.Windows.Forms.GroupBox()
        Me.lblName = New System.Windows.Forms.Label()
        Me.txtName = New System.Windows.Forms.TextBox()
        Me.lblHostingCode = New System.Windows.Forms.Label()
        Me.txtHostingCode = New System.Windows.Forms.TextBox()
        Me.lblSourceLang = New System.Windows.Forms.Label()
        Me.cboSourceLang = New System.Windows.Forms.ComboBox()
        Me.lblSttEngine = New System.Windows.Forms.Label()
        Me.cboSttEngine = New System.Windows.Forms.ComboBox()
        Me.lblTransEngine = New System.Windows.Forms.Label()
        Me.cboTransEngine = New System.Windows.Forms.ComboBox()
        Me.lblVisibility = New System.Windows.Forms.Label()
        Me.cboVisibility = New System.Windows.Forms.ComboBox()
        Me.lblAudioDevice = New System.Windows.Forms.Label()
        Me.cboAudioDevice = New System.Windows.Forms.ComboBox()
        Me.btnRefreshDevices = New System.Windows.Forms.Button()
        Me.lblModelPath = New System.Windows.Forms.Label()
        Me.cboModel = New System.Windows.Forms.ComboBox()
        Me.lblSttTemplate = New System.Windows.Forms.Label()
        Me.cboSttTemplate = New System.Windows.Forms.ComboBox()
        Me.btnManageSttTemplates = New System.Windows.Forms.Button()
        Me.lblMode = New System.Windows.Forms.Label()
        Me.cboMode = New System.Windows.Forms.ComboBox()
        Me.lblSpeakers = New System.Windows.Forms.Label()
        Me.clbSpeakers = New System.Windows.Forms.CheckedListBox()
        Me.btnManageSpeakers = New System.Windows.Forms.Button()
        Me.lblDefaultSpeaker = New System.Windows.Forms.Label()
        Me.cboDefaultSpeaker = New System.Windows.Forms.ComboBox()
        Me.lblDisplayTpl = New System.Windows.Forms.Label()
        Me.cboDisplayTpl = New System.Windows.Forms.ComboBox()
        Me.btnManageDisplay = New System.Windows.Forms.Button()
        Me.lblFilterSet = New System.Windows.Forms.Label()
        Me.cboFilterSet = New System.Windows.Forms.ComboBox()
        Me.btnManageFilterSets = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        Me.SuspendLayout()
        '
        ' lvTemplates
        '
        Me.lvTemplates.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colName, Me.colHostingCode, Me.colLanguage, Me.colSttEngine})
        Me.lvTemplates.FullRowSelect = True
        Me.lvTemplates.GridLines = True
        Me.lvTemplates.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvTemplates.HideSelection = False
        Me.lvTemplates.Location = New System.Drawing.Point(12, 12)
        Me.lvTemplates.MultiSelect = False
        Me.lvTemplates.Name = "lvTemplates"
        Me.lvTemplates.Size = New System.Drawing.Size(460, 160)
        Me.lvTemplates.TabIndex = 0
        Me.lvTemplates.UseCompatibleStateImageBehavior = False
        Me.lvTemplates.View = System.Windows.Forms.View.Details
        '
        ' colName
        '
        Me.colName.Text = "Name"
        Me.colName.Width = 150
        '
        ' colHostingCode
        '
        Me.colHostingCode.Text = "Hosting Code"
        Me.colHostingCode.Width = 110
        '
        ' colLanguage
        '
        Me.colLanguage.Text = "Language"
        Me.colLanguage.Width = 90
        '
        ' colSttEngine
        '
        Me.colSttEngine.Text = "STT Engine"
        Me.colSttEngine.Width = 100
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
        Me.btnClose.Location = New System.Drawing.Point(478, 142)
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
        Me.grpDetail.Controls.Add(Me.btnManageSttTemplates)
        Me.grpDetail.Controls.Add(Me.cboSttTemplate)
        Me.grpDetail.Controls.Add(Me.lblSttTemplate)
        Me.grpDetail.Controls.Add(Me.lblMode)
        Me.grpDetail.Controls.Add(Me.cboMode)
        Me.grpDetail.Controls.Add(Me.lblSpeakers)
        Me.grpDetail.Controls.Add(Me.clbSpeakers)
        Me.grpDetail.Controls.Add(Me.btnManageSpeakers)
        Me.grpDetail.Controls.Add(Me.lblDefaultSpeaker)
        Me.grpDetail.Controls.Add(Me.cboDefaultSpeaker)
        Me.grpDetail.Controls.Add(Me.lblDisplayTpl)
        Me.grpDetail.Controls.Add(Me.cboDisplayTpl)
        Me.grpDetail.Controls.Add(Me.btnManageDisplay)
        Me.grpDetail.Controls.Add(Me.lblFilterSet)
        Me.grpDetail.Controls.Add(Me.cboFilterSet)
        Me.grpDetail.Controls.Add(Me.btnManageFilterSets)
        Me.grpDetail.Controls.Add(Me.cboModel)
        Me.grpDetail.Controls.Add(Me.lblModelPath)
        Me.grpDetail.Controls.Add(Me.btnRefreshDevices)
        Me.grpDetail.Controls.Add(Me.cboAudioDevice)
        Me.grpDetail.Controls.Add(Me.lblAudioDevice)
        Me.grpDetail.Controls.Add(Me.cboVisibility)
        Me.grpDetail.Controls.Add(Me.lblVisibility)
        Me.grpDetail.Controls.Add(Me.cboTransEngine)
        Me.grpDetail.Controls.Add(Me.lblTransEngine)
        Me.grpDetail.Controls.Add(Me.cboSttEngine)
        Me.grpDetail.Controls.Add(Me.lblSttEngine)
        Me.grpDetail.Controls.Add(Me.cboSourceLang)
        Me.grpDetail.Controls.Add(Me.lblSourceLang)
        Me.grpDetail.Controls.Add(Me.txtHostingCode)
        Me.grpDetail.Controls.Add(Me.lblHostingCode)
        Me.grpDetail.Controls.Add(Me.txtName)
        Me.grpDetail.Controls.Add(Me.lblName)
        Me.grpDetail.Location = New System.Drawing.Point(12, 180)
        Me.grpDetail.Name = "grpDetail"
        Me.grpDetail.Size = New System.Drawing.Size(556, 494)
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
        Me.txtName.Size = New System.Drawing.Size(200, 23)
        Me.txtName.TabIndex = 1
        '
        ' lblHostingCode
        '
        Me.lblHostingCode.AutoSize = True
        Me.lblHostingCode.Location = New System.Drawing.Point(12, 52)
        Me.lblHostingCode.Name = "lblHostingCode"
        Me.lblHostingCode.Size = New System.Drawing.Size(78, 15)
        Me.lblHostingCode.TabIndex = 2
        Me.lblHostingCode.Text = "Hosting Code"
        '
        ' txtHostingCode
        '
        Me.txtHostingCode.Location = New System.Drawing.Point(130, 49)
        Me.txtHostingCode.Name = "txtHostingCode"
        Me.txtHostingCode.Size = New System.Drawing.Size(200, 23)
        Me.txtHostingCode.TabIndex = 3
        '
        ' lblSourceLang
        '
        Me.lblSourceLang.AutoSize = True
        Me.lblSourceLang.Location = New System.Drawing.Point(12, 80)
        Me.lblSourceLang.Name = "lblSourceLang"
        Me.lblSourceLang.Size = New System.Drawing.Size(101, 15)
        Me.lblSourceLang.TabIndex = 4
        Me.lblSourceLang.Text = "Source Language"
        '
        ' cboSourceLang
        '
        Me.cboSourceLang.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSourceLang.Location = New System.Drawing.Point(130, 77)
        Me.cboSourceLang.Name = "cboSourceLang"
        Me.cboSourceLang.Size = New System.Drawing.Size(200, 23)
        Me.cboSourceLang.TabIndex = 5
        '
        ' lblSttEngine
        '
        Me.lblSttEngine.AutoSize = True
        Me.lblSttEngine.Location = New System.Drawing.Point(12, 108)
        Me.lblSttEngine.Name = "lblSttEngine"
        Me.lblSttEngine.Size = New System.Drawing.Size(68, 15)
        Me.lblSttEngine.TabIndex = 6
        Me.lblSttEngine.Text = "STT Engine"
        '
        ' cboSttEngine
        '
        Me.cboSttEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSttEngine.Location = New System.Drawing.Point(130, 105)
        Me.cboSttEngine.Name = "cboSttEngine"
        Me.cboSttEngine.Size = New System.Drawing.Size(200, 23)
        Me.cboSttEngine.TabIndex = 7
        '
        ' lblTransEngine
        '
        Me.lblTransEngine.AutoSize = True
        Me.lblTransEngine.Location = New System.Drawing.Point(12, 136)
        Me.lblTransEngine.Name = "lblTransEngine"
        Me.lblTransEngine.Size = New System.Drawing.Size(110, 15)
        Me.lblTransEngine.TabIndex = 8
        Me.lblTransEngine.Text = "Translation Engine"
        '
        ' cboTransEngine
        '
        Me.cboTransEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboTransEngine.Location = New System.Drawing.Point(130, 133)
        Me.cboTransEngine.Name = "cboTransEngine"
        Me.cboTransEngine.Size = New System.Drawing.Size(200, 23)
        ' Open list wider than the closed box — VRAM-annotated NLLB names need ~300px.
        Me.cboTransEngine.DropDownWidth = 320
        Me.cboTransEngine.TabIndex = 9
        '
        ' lblVisibility
        '
        Me.lblVisibility.AutoSize = True
        Me.lblVisibility.Location = New System.Drawing.Point(350, 108)
        Me.lblVisibility.Name = "lblVisibility"
        Me.lblVisibility.Size = New System.Drawing.Size(54, 15)
        Me.lblVisibility.TabIndex = 18
        Me.lblVisibility.Text = "Visibility"
        '
        ' cboVisibility
        '
        Me.cboVisibility.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboVisibility.Items.AddRange(New Object() {"public", "private"})
        Me.cboVisibility.Location = New System.Drawing.Point(460, 105)
        Me.cboVisibility.Name = "cboVisibility"
        Me.cboVisibility.Size = New System.Drawing.Size(70, 23)
        Me.cboVisibility.TabIndex = 19
        '
        ' lblAudioDevice
        '
        Me.lblAudioDevice.AutoSize = True
        Me.lblAudioDevice.Location = New System.Drawing.Point(12, 164)
        Me.lblAudioDevice.Name = "lblAudioDevice"
        Me.lblAudioDevice.Size = New System.Drawing.Size(79, 15)
        Me.lblAudioDevice.TabIndex = 20
        Me.lblAudioDevice.Text = "Audio Device"
        '
        ' cboAudioDevice
        '
        Me.cboAudioDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboAudioDevice.Location = New System.Drawing.Point(130, 161)
        Me.cboAudioDevice.Name = "cboAudioDevice"
        Me.cboAudioDevice.Size = New System.Drawing.Size(330, 23)
        Me.cboAudioDevice.TabIndex = 21
        '
        ' btnRefreshDevices
        '
        Me.btnRefreshDevices.Location = New System.Drawing.Point(466, 160)
        Me.btnRefreshDevices.Name = "btnRefreshDevices"
        Me.btnRefreshDevices.Size = New System.Drawing.Size(64, 25)
        Me.btnRefreshDevices.TabIndex = 26
        Me.btnRefreshDevices.Text = "Refresh"
        Me.btnRefreshDevices.UseVisualStyleBackColor = True
        '
        ' lblModelPath
        '
        Me.lblModelPath.AutoSize = True
        Me.lblModelPath.Location = New System.Drawing.Point(12, 192)
        Me.lblModelPath.Name = "lblModelPath"
        Me.lblModelPath.Size = New System.Drawing.Size(69, 15)
        Me.lblModelPath.TabIndex = 22
        Me.lblModelPath.Text = "Model Path"
        '
        ' cboModel
        '
        Me.cboModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboModel.Location = New System.Drawing.Point(130, 189)
        Me.cboModel.Name = "cboModel"
        Me.cboModel.Size = New System.Drawing.Size(400, 23)
        Me.cboModel.TabIndex = 23
        '
        ' lblSttTemplate — reference into the STT template library
        '
        Me.lblSttTemplate.AutoSize = True
        Me.lblSttTemplate.Location = New System.Drawing.Point(12, 223)
        Me.lblSttTemplate.Name = "lblSttTemplate"
        Me.lblSttTemplate.Size = New System.Drawing.Size(76, 15)
        Me.lblSttTemplate.TabIndex = 27
        Me.lblSttTemplate.Text = "STT Template"
        '
        ' cboSttTemplate
        '
        Me.cboSttTemplate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSttTemplate.Location = New System.Drawing.Point(130, 220)
        Me.cboSttTemplate.Name = "cboSttTemplate"
        Me.cboSttTemplate.Size = New System.Drawing.Size(304, 23)
        Me.cboSttTemplate.TabIndex = 28
        '
        ' btnManageSttTemplates
        '
        Me.btnManageSttTemplates.Location = New System.Drawing.Point(440, 219)
        Me.btnManageSttTemplates.Name = "btnManageSttTemplates"
        Me.btnManageSttTemplates.Size = New System.Drawing.Size(90, 25)
        Me.btnManageSttTemplates.TabIndex = 29
        Me.btnManageSttTemplates.Text = "Manage..."
        Me.btnManageSttTemplates.UseVisualStyleBackColor = True
        '
        ' lblMode — Online/Offline gate for sessions from this template
        '
        Me.lblMode.AutoSize = True
        Me.lblMode.Location = New System.Drawing.Point(350, 136)
        Me.lblMode.Name = "lblMode"
        Me.lblMode.Text = "Mode"
        '
        ' cboMode
        '
        Me.cboMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboMode.Location = New System.Drawing.Point(440, 133)
        Me.cboMode.Name = "cboMode"
        Me.cboMode.Size = New System.Drawing.Size(90, 23)
        Me.cboMode.TabIndex = 30
        '
        ' lblSpeakers
        '
        Me.lblSpeakers.AutoSize = True
        Me.lblSpeakers.Location = New System.Drawing.Point(12, 252)
        Me.lblSpeakers.Name = "lblSpeakers"
        Me.lblSpeakers.Text = "Speakers"
        '
        ' clbSpeakers — checked = participates in sessions from this template
        '
        Me.clbSpeakers.CheckOnClick = True
        Me.clbSpeakers.IntegralHeight = False
        Me.clbSpeakers.Location = New System.Drawing.Point(130, 250)
        Me.clbSpeakers.Name = "clbSpeakers"
        Me.clbSpeakers.Size = New System.Drawing.Size(300, 105)
        Me.clbSpeakers.TabIndex = 31
        '
        ' btnManageSpeakers
        '
        Me.btnManageSpeakers.Location = New System.Drawing.Point(440, 250)
        Me.btnManageSpeakers.Name = "btnManageSpeakers"
        Me.btnManageSpeakers.Size = New System.Drawing.Size(90, 25)
        Me.btnManageSpeakers.TabIndex = 32
        Me.btnManageSpeakers.Text = "Manage..."
        Me.btnManageSpeakers.UseVisualStyleBackColor = True
        '
        ' lblDefaultSpeaker — speaker auto-selected when a room starts from this template
        '
        Me.lblDefaultSpeaker.AutoSize = True
        Me.lblDefaultSpeaker.Location = New System.Drawing.Point(437, 285)
        Me.lblDefaultSpeaker.Name = "lblDefaultSpeaker"
        Me.lblDefaultSpeaker.Text = "Default speaker"
        '
        ' cboDefaultSpeaker
        '
        Me.cboDefaultSpeaker.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboDefaultSpeaker.Location = New System.Drawing.Point(440, 303)
        Me.cboDefaultSpeaker.Name = "cboDefaultSpeaker"
        Me.cboDefaultSpeaker.Size = New System.Drawing.Size(110, 23)
        Me.cboDefaultSpeaker.TabIndex = 37
        '
        ' lblDisplayTpl — Display template reference (Phase 7)
        '
        Me.lblDisplayTpl.AutoSize = True
        Me.lblDisplayTpl.Location = New System.Drawing.Point(12, 368)
        Me.lblDisplayTpl.Name = "lblDisplayTpl"
        Me.lblDisplayTpl.Text = "Display"
        '
        ' cboDisplayTpl
        '
        Me.cboDisplayTpl.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboDisplayTpl.Location = New System.Drawing.Point(130, 365)
        Me.cboDisplayTpl.Name = "cboDisplayTpl"
        Me.cboDisplayTpl.Size = New System.Drawing.Size(300, 23)
        Me.cboDisplayTpl.TabIndex = 33
        '
        ' btnManageDisplay
        '
        Me.btnManageDisplay.Location = New System.Drawing.Point(440, 364)
        Me.btnManageDisplay.Name = "btnManageDisplay"
        Me.btnManageDisplay.Size = New System.Drawing.Size(90, 25)
        Me.btnManageDisplay.TabIndex = 34
        Me.btnManageDisplay.Text = "Manage..."
        Me.btnManageDisplay.UseVisualStyleBackColor = True
        '
        ' lblFilterSet — named filter set reference (Phase 8)
        '
        Me.lblFilterSet.AutoSize = True
        Me.lblFilterSet.Location = New System.Drawing.Point(12, 399)
        Me.lblFilterSet.Name = "lblFilterSet"
        Me.lblFilterSet.Text = "Filter set"
        '
        ' cboFilterSet
        '
        Me.cboFilterSet.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboFilterSet.Location = New System.Drawing.Point(130, 396)
        Me.cboFilterSet.Name = "cboFilterSet"
        Me.cboFilterSet.Size = New System.Drawing.Size(300, 23)
        Me.cboFilterSet.TabIndex = 35
        '
        ' btnManageFilterSets
        '
        Me.btnManageFilterSets.Location = New System.Drawing.Point(440, 395)
        Me.btnManageFilterSets.Name = "btnManageFilterSets"
        Me.btnManageFilterSets.Size = New System.Drawing.Size(90, 25)
        Me.btnManageFilterSets.TabIndex = 36
        Me.btnManageFilterSets.Text = "Manage..."
        Me.btnManageFilterSets.UseVisualStyleBackColor = True
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 444)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 24
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 444)
        Me.btnCancelEdit.Name = "btnCancelEdit"
        Me.btnCancelEdit.Size = New System.Drawing.Size(90, 30)
        Me.btnCancelEdit.TabIndex = 25
        Me.btnCancelEdit.Text = "Cancel"
        Me.btnCancelEdit.UseVisualStyleBackColor = True
        '
        ' FormTemplateManager
        '
        Me.AcceptButton = Me.btnClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(580, 710)
        Me.Controls.Add(Me.grpDetail)
        Me.Controls.Add(Me.btnClose)
        Me.Controls.Add(Me.btnDelete)
        Me.Controls.Add(Me.btnEdit)
        Me.Controls.Add(Me.btnAdd)
        Me.Controls.Add(Me.lvTemplates)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormTemplateManager"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Conference Templates"
        Me.grpDetail.ResumeLayout(False)
        Me.grpDetail.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents lvTemplates As System.Windows.Forms.ListView
    Friend WithEvents colName As System.Windows.Forms.ColumnHeader
    Friend WithEvents colHostingCode As System.Windows.Forms.ColumnHeader
    Friend WithEvents colLanguage As System.Windows.Forms.ColumnHeader
    Friend WithEvents colSttEngine As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnAdd As System.Windows.Forms.Button
    Friend WithEvents btnEdit As System.Windows.Forms.Button
    Friend WithEvents btnDelete As System.Windows.Forms.Button
    Friend WithEvents btnClose As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblName As System.Windows.Forms.Label
    Friend WithEvents txtName As System.Windows.Forms.TextBox
    Friend WithEvents lblHostingCode As System.Windows.Forms.Label
    Friend WithEvents txtHostingCode As System.Windows.Forms.TextBox
    Friend WithEvents lblSourceLang As System.Windows.Forms.Label
    Friend WithEvents cboSourceLang As System.Windows.Forms.ComboBox
    Friend WithEvents lblSttEngine As System.Windows.Forms.Label
    Friend WithEvents cboSttEngine As System.Windows.Forms.ComboBox
    Friend WithEvents lblTransEngine As System.Windows.Forms.Label
    Friend WithEvents cboTransEngine As System.Windows.Forms.ComboBox
    Friend WithEvents lblVisibility As System.Windows.Forms.Label
    Friend WithEvents cboVisibility As System.Windows.Forms.ComboBox
    Friend WithEvents lblAudioDevice As System.Windows.Forms.Label
    Friend WithEvents cboAudioDevice As System.Windows.Forms.ComboBox
    Friend WithEvents btnRefreshDevices As System.Windows.Forms.Button
    Friend WithEvents lblModelPath As System.Windows.Forms.Label
    Friend WithEvents cboModel As System.Windows.Forms.ComboBox
    Friend WithEvents lblSttTemplate As System.Windows.Forms.Label
    Friend WithEvents cboSttTemplate As System.Windows.Forms.ComboBox
    Friend WithEvents btnManageSttTemplates As System.Windows.Forms.Button
    Friend WithEvents lblMode As System.Windows.Forms.Label
    Friend WithEvents cboMode As System.Windows.Forms.ComboBox
    Friend WithEvents lblSpeakers As System.Windows.Forms.Label
    Friend WithEvents clbSpeakers As System.Windows.Forms.CheckedListBox
    Friend WithEvents btnManageSpeakers As System.Windows.Forms.Button
    Friend WithEvents lblDefaultSpeaker As System.Windows.Forms.Label
    Friend WithEvents cboDefaultSpeaker As System.Windows.Forms.ComboBox
    Friend WithEvents lblDisplayTpl As System.Windows.Forms.Label
    Friend WithEvents cboDisplayTpl As System.Windows.Forms.ComboBox
    Friend WithEvents btnManageDisplay As System.Windows.Forms.Button
    Friend WithEvents lblFilterSet As System.Windows.Forms.Label
    Friend WithEvents cboFilterSet As System.Windows.Forms.ComboBox
    Friend WithEvents btnManageFilterSets As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
