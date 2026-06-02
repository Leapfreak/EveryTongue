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
        Me.lblBeamSize = New System.Windows.Forms.Label()
        Me.nudBeamSize = New System.Windows.Forms.NumericUpDown()
        Me.lblMaxSegment = New System.Windows.Forms.Label()
        Me.nudMaxSegment = New System.Windows.Forms.NumericUpDown()
        Me.lblVadSilence = New System.Windows.Forms.Label()
        Me.nudVadSilence = New System.Windows.Forms.NumericUpDown()
        Me.lblInitialPrompt = New System.Windows.Forms.Label()
        Me.txtInitialPrompt = New System.Windows.Forms.TextBox()
        Me.lblVisibility = New System.Windows.Forms.Label()
        Me.cboVisibility = New System.Windows.Forms.ComboBox()
        Me.lblAudioDevice = New System.Windows.Forms.Label()
        Me.cboAudioDevice = New System.Windows.Forms.ComboBox()
        Me.btnRefreshDevices = New System.Windows.Forms.Button()
        Me.lblModelPath = New System.Windows.Forms.Label()
        Me.txtModelPath = New System.Windows.Forms.TextBox()
        Me.btnBrowseModel = New System.Windows.Forms.Button()
        Me.btnSave = New System.Windows.Forms.Button()
        Me.btnCancelEdit = New System.Windows.Forms.Button()
        Me.grpDetail.SuspendLayout()
        CType(Me.nudBeamSize, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudMaxSegment, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.nudVadSilence, System.ComponentModel.ISupportInitialize).BeginInit()
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
        Me.grpDetail.Controls.Add(Me.btnBrowseModel)
        Me.grpDetail.Controls.Add(Me.txtModelPath)
        Me.grpDetail.Controls.Add(Me.lblModelPath)
        Me.grpDetail.Controls.Add(Me.btnRefreshDevices)
        Me.grpDetail.Controls.Add(Me.cboAudioDevice)
        Me.grpDetail.Controls.Add(Me.lblAudioDevice)
        Me.grpDetail.Controls.Add(Me.cboVisibility)
        Me.grpDetail.Controls.Add(Me.lblVisibility)
        Me.grpDetail.Controls.Add(Me.txtInitialPrompt)
        Me.grpDetail.Controls.Add(Me.lblInitialPrompt)
        Me.grpDetail.Controls.Add(Me.nudVadSilence)
        Me.grpDetail.Controls.Add(Me.lblVadSilence)
        Me.grpDetail.Controls.Add(Me.nudMaxSegment)
        Me.grpDetail.Controls.Add(Me.lblMaxSegment)
        Me.grpDetail.Controls.Add(Me.nudBeamSize)
        Me.grpDetail.Controls.Add(Me.lblBeamSize)
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
        Me.grpDetail.Size = New System.Drawing.Size(556, 310)
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
        Me.cboTransEngine.TabIndex = 9
        '
        ' lblBeamSize
        '
        Me.lblBeamSize.AutoSize = True
        Me.lblBeamSize.Location = New System.Drawing.Point(350, 24)
        Me.lblBeamSize.Name = "lblBeamSize"
        Me.lblBeamSize.Size = New System.Drawing.Size(61, 15)
        Me.lblBeamSize.TabIndex = 10
        Me.lblBeamSize.Text = "Beam Size"
        '
        ' nudBeamSize
        '
        Me.nudBeamSize.Location = New System.Drawing.Point(460, 21)
        Me.nudBeamSize.Maximum = New Decimal(New Integer() {15, 0, 0, 0})
        Me.nudBeamSize.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.nudBeamSize.Name = "nudBeamSize"
        Me.nudBeamSize.Size = New System.Drawing.Size(70, 23)
        Me.nudBeamSize.TabIndex = 11
        Me.nudBeamSize.Value = New Decimal(New Integer() {7, 0, 0, 0})
        '
        ' lblMaxSegment
        '
        Me.lblMaxSegment.AutoSize = True
        Me.lblMaxSegment.Location = New System.Drawing.Point(350, 52)
        Me.lblMaxSegment.Name = "lblMaxSegment"
        Me.lblMaxSegment.Size = New System.Drawing.Size(103, 15)
        Me.lblMaxSegment.TabIndex = 12
        Me.lblMaxSegment.Text = "Max Segment (sec)"
        '
        ' nudMaxSegment
        '
        Me.nudMaxSegment.Location = New System.Drawing.Point(460, 49)
        Me.nudMaxSegment.Maximum = New Decimal(New Integer() {60, 0, 0, 0})
        Me.nudMaxSegment.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.nudMaxSegment.Name = "nudMaxSegment"
        Me.nudMaxSegment.Size = New System.Drawing.Size(70, 23)
        Me.nudMaxSegment.TabIndex = 13
        Me.nudMaxSegment.Value = New Decimal(New Integer() {15, 0, 0, 0})
        '
        ' lblVadSilence
        '
        Me.lblVadSilence.AutoSize = True
        Me.lblVadSilence.Location = New System.Drawing.Point(350, 80)
        Me.lblVadSilence.Name = "lblVadSilence"
        Me.lblVadSilence.Size = New System.Drawing.Size(98, 15)
        Me.lblVadSilence.TabIndex = 14
        Me.lblVadSilence.Text = "VAD Silence (ms)"
        '
        ' nudVadSilence
        '
        Me.nudVadSilence.Increment = New Decimal(New Integer() {100, 0, 0, 0})
        Me.nudVadSilence.Location = New System.Drawing.Point(460, 77)
        Me.nudVadSilence.Maximum = New Decimal(New Integer() {2000, 0, 0, 0})
        Me.nudVadSilence.Minimum = New Decimal(New Integer() {200, 0, 0, 0})
        Me.nudVadSilence.Name = "nudVadSilence"
        Me.nudVadSilence.Size = New System.Drawing.Size(70, 23)
        Me.nudVadSilence.TabIndex = 15
        Me.nudVadSilence.Value = New Decimal(New Integer() {800, 0, 0, 0})
        '
        ' lblInitialPrompt
        '
        Me.lblInitialPrompt.AutoSize = True
        Me.lblInitialPrompt.Location = New System.Drawing.Point(12, 164)
        Me.lblInitialPrompt.Name = "lblInitialPrompt"
        Me.lblInitialPrompt.Size = New System.Drawing.Size(81, 15)
        Me.lblInitialPrompt.TabIndex = 16
        Me.lblInitialPrompt.Text = "Initial Prompt"
        '
        ' txtInitialPrompt
        '
        Me.txtInitialPrompt.Location = New System.Drawing.Point(130, 161)
        Me.txtInitialPrompt.Name = "txtInitialPrompt"
        Me.txtInitialPrompt.Size = New System.Drawing.Size(400, 23)
        Me.txtInitialPrompt.TabIndex = 17
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
        Me.lblAudioDevice.Location = New System.Drawing.Point(12, 192)
        Me.lblAudioDevice.Name = "lblAudioDevice"
        Me.lblAudioDevice.Size = New System.Drawing.Size(79, 15)
        Me.lblAudioDevice.TabIndex = 20
        Me.lblAudioDevice.Text = "Audio Device"
        '
        ' cboAudioDevice
        '
        Me.cboAudioDevice.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboAudioDevice.Location = New System.Drawing.Point(130, 189)
        Me.cboAudioDevice.Name = "cboAudioDevice"
        Me.cboAudioDevice.Size = New System.Drawing.Size(330, 23)
        Me.cboAudioDevice.TabIndex = 21
        '
        ' btnRefreshDevices
        '
        Me.btnRefreshDevices.Location = New System.Drawing.Point(466, 188)
        Me.btnRefreshDevices.Name = "btnRefreshDevices"
        Me.btnRefreshDevices.Size = New System.Drawing.Size(64, 25)
        Me.btnRefreshDevices.TabIndex = 26
        Me.btnRefreshDevices.Text = "Refresh"
        Me.btnRefreshDevices.UseVisualStyleBackColor = True
        '
        ' lblModelPath
        '
        Me.lblModelPath.AutoSize = True
        Me.lblModelPath.Location = New System.Drawing.Point(12, 220)
        Me.lblModelPath.Name = "lblModelPath"
        Me.lblModelPath.Size = New System.Drawing.Size(69, 15)
        Me.lblModelPath.TabIndex = 22
        Me.lblModelPath.Text = "Model Path"
        '
        ' txtModelPath
        '
        Me.txtModelPath.Location = New System.Drawing.Point(130, 217)
        Me.txtModelPath.Name = "txtModelPath"
        Me.txtModelPath.Size = New System.Drawing.Size(330, 23)
        Me.txtModelPath.TabIndex = 23
        '
        ' btnBrowseModel
        '
        Me.btnBrowseModel.Location = New System.Drawing.Point(466, 216)
        Me.btnBrowseModel.Name = "btnBrowseModel"
        Me.btnBrowseModel.Size = New System.Drawing.Size(64, 25)
        Me.btnBrowseModel.TabIndex = 27
        Me.btnBrowseModel.Text = "Browse..."
        Me.btnBrowseModel.UseVisualStyleBackColor = True
        '
        ' btnSave
        '
        Me.btnSave.Location = New System.Drawing.Point(350, 260)
        Me.btnSave.Name = "btnSave"
        Me.btnSave.Size = New System.Drawing.Size(90, 30)
        Me.btnSave.TabIndex = 24
        Me.btnSave.Text = "Save"
        Me.btnSave.UseVisualStyleBackColor = True
        '
        ' btnCancelEdit
        '
        Me.btnCancelEdit.Location = New System.Drawing.Point(446, 260)
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
        Me.ClientSize = New System.Drawing.Size(580, 500)
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
        CType(Me.nudBeamSize, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudMaxSegment, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.nudVadSilence, System.ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents lblBeamSize As System.Windows.Forms.Label
    Friend WithEvents nudBeamSize As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblMaxSegment As System.Windows.Forms.Label
    Friend WithEvents nudMaxSegment As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblVadSilence As System.Windows.Forms.Label
    Friend WithEvents nudVadSilence As System.Windows.Forms.NumericUpDown
    Friend WithEvents lblInitialPrompt As System.Windows.Forms.Label
    Friend WithEvents txtInitialPrompt As System.Windows.Forms.TextBox
    Friend WithEvents lblVisibility As System.Windows.Forms.Label
    Friend WithEvents cboVisibility As System.Windows.Forms.ComboBox
    Friend WithEvents lblAudioDevice As System.Windows.Forms.Label
    Friend WithEvents cboAudioDevice As System.Windows.Forms.ComboBox
    Friend WithEvents btnRefreshDevices As System.Windows.Forms.Button
    Friend WithEvents lblModelPath As System.Windows.Forms.Label
    Friend WithEvents txtModelPath As System.Windows.Forms.TextBox
    Friend WithEvents btnBrowseModel As System.Windows.Forms.Button
    Friend WithEvents btnSave As System.Windows.Forms.Button
    Friend WithEvents btnCancelEdit As System.Windows.Forms.Button

End Class
