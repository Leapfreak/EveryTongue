<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormFilterEditor
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
        lblLang = New Label()
        cboLang = New ComboBox()
        btnAddLang = New Button()
        TabControl1 = New TabControl()
        tabHal = New TabPage()
        lblHalDesc = New Label()
        clbHalPhrases = New CheckedListBox()
        txtHalPhrase = New TextBox()
        btnHalAdd = New Button()
        btnHalRemove = New Button()
        btnHalSave = New Button()
        tabProf = New TabPage()
        lblProfDesc = New Label()
        clbProfWords = New CheckedListBox()
        txtProfWord = New TextBox()
        btnProfAdd = New Button()
        btnProfRemove = New Button()
        btnProfSave = New Button()
        tabGlos = New TabPage()
        dgvGlossary = New DataGridView()
        colGlosEnabled = New DataGridViewCheckBoxColumn()
        colTrigger = New DataGridViewTextBoxColumn()
        colComment = New DataGridViewTextBoxColumn()
        btnGlosAdd = New Button()
        btnGlosRemove = New Button()
        grpDetail = New GroupBox()
        lblTrigger = New Label()
        txtGlosTrigger = New TextBox()
        lblComment = New Label()
        txtGlosComment = New TextBox()
        lblFixes = New Label()
        dgvFixes = New DataGridView()
        colTargetLang = New DataGridViewComboBoxColumn()
        colWrong = New DataGridViewTextBoxColumn()
        colRight = New DataGridViewTextBoxColumn()
        btnFixAdd = New Button()
        btnFixRemove = New Button()
        btnGlosSave = New Button()
        lblGlosDesc = New Label()
        TabControl1.SuspendLayout()
        tabHal.SuspendLayout()
        tabProf.SuspendLayout()
        tabGlos.SuspendLayout()
        CType(dgvGlossary, ComponentModel.ISupportInitialize).BeginInit()
        grpDetail.SuspendLayout()
        CType(dgvFixes, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        '
        ' lblLang — shared language selector above tabs
        '
        lblLang.AutoSize = True
        lblLang.Location = New Point(12, 11)
        lblLang.Name = "lblLang"
        lblLang.Size = New Size(62, 15)
        lblLang.TabIndex = 0
        lblLang.Text = "Language:"
        '
        ' cboLang
        '
        cboLang.DropDownStyle = ComboBoxStyle.DropDownList
        cboLang.Location = New Point(84, 8)
        cboLang.Name = "cboLang"
        cboLang.Size = New Size(200, 23)
        cboLang.TabIndex = 1
        '
        ' btnAddLang
        '
        btnAddLang.Location = New Point(294, 7)
        btnAddLang.Name = "btnAddLang"
        btnAddLang.Size = New Size(110, 25)
        btnAddLang.TabIndex = 2
        btnAddLang.Text = "Add Language..."
        btnAddLang.UseVisualStyleBackColor = True
        '
        ' TabControl1
        '
        TabControl1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        TabControl1.Controls.Add(tabHal)
        TabControl1.Controls.Add(tabProf)
        TabControl1.Controls.Add(tabGlos)
        TabControl1.Location = New Point(0, 38)
        TabControl1.Name = "TabControl1"
        TabControl1.SelectedIndex = 0
        TabControl1.Size = New Size(734, 523)
        TabControl1.TabIndex = 3
        '
        ' tabHal
        '
        tabHal.Controls.Add(lblHalDesc)
        tabHal.Controls.Add(clbHalPhrases)
        tabHal.Controls.Add(txtHalPhrase)
        tabHal.Controls.Add(btnHalAdd)
        tabHal.Controls.Add(btnHalRemove)
        tabHal.Controls.Add(btnHalSave)
        tabHal.Location = New Point(4, 24)
        tabHal.Name = "tabHal"
        tabHal.Padding = New Padding(8)
        tabHal.Size = New Size(726, 495)
        tabHal.TabIndex = 0
        tabHal.Text = "Hallucinations"
        tabHal.UseVisualStyleBackColor = True
        '
        ' lblHalDesc
        '
        lblHalDesc.AutoSize = True
        lblHalDesc.Location = New Point(8, 8)
        lblHalDesc.MaximumSize = New Size(700, 0)
        lblHalDesc.Name = "lblHalDesc"
        lblHalDesc.Size = New Size(479, 15)
        lblHalDesc.TabIndex = 0
        lblHalDesc.Text = "Phrases that whisper hallucinates. Uncheck to disable without removing."
        '
        ' clbHalPhrases
        '
        clbHalPhrases.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        clbHalPhrases.CheckOnClick = True
        clbHalPhrases.Font = New Font("Consolas", 10F)
        clbHalPhrases.FormattingEnabled = True
        clbHalPhrases.Location = New Point(8, 28)
        clbHalPhrases.Name = "clbHalPhrases"
        clbHalPhrases.Size = New Size(710, 430)
        clbHalPhrases.TabIndex = 1
        '
        ' txtHalPhrase
        '
        txtHalPhrase.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtHalPhrase.Location = New Point(8, 462)
        txtHalPhrase.Name = "txtHalPhrase"
        txtHalPhrase.Size = New Size(400, 23)
        txtHalPhrase.TabIndex = 2
        '
        ' btnHalAdd
        '
        btnHalAdd.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalAdd.Location = New Point(416, 461)
        btnHalAdd.Name = "btnHalAdd"
        btnHalAdd.Size = New Size(60, 25)
        btnHalAdd.TabIndex = 3
        btnHalAdd.Text = "Add"
        btnHalAdd.UseVisualStyleBackColor = True
        '
        ' btnHalRemove
        '
        btnHalRemove.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalRemove.Location = New Point(484, 461)
        btnHalRemove.Name = "btnHalRemove"
        btnHalRemove.Size = New Size(110, 25)
        btnHalRemove.TabIndex = 4
        btnHalRemove.Text = "Remove Selected"
        btnHalRemove.UseVisualStyleBackColor = True
        '
        ' btnHalSave
        '
        btnHalSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalSave.Location = New Point(602, 461)
        btnHalSave.Name = "btnHalSave"
        btnHalSave.Size = New Size(120, 25)
        btnHalSave.TabIndex = 5
        btnHalSave.Text = "Save && Reload"
        btnHalSave.UseVisualStyleBackColor = True
        '
        ' tabProf
        '
        tabProf.Controls.Add(lblProfDesc)
        tabProf.Controls.Add(clbProfWords)
        tabProf.Controls.Add(txtProfWord)
        tabProf.Controls.Add(btnProfAdd)
        tabProf.Controls.Add(btnProfRemove)
        tabProf.Controls.Add(btnProfSave)
        tabProf.Location = New Point(4, 24)
        tabProf.Name = "tabProf"
        tabProf.Padding = New Padding(8)
        tabProf.Size = New Size(726, 495)
        tabProf.TabIndex = 1
        tabProf.Text = "Profanity Filter"
        tabProf.UseVisualStyleBackColor = True
        '
        ' lblProfDesc
        '
        lblProfDesc.AutoSize = True
        lblProfDesc.Location = New Point(8, 8)
        lblProfDesc.MaximumSize = New Size(700, 0)
        lblProfDesc.Name = "lblProfDesc"
        lblProfDesc.Size = New Size(367, 15)
        lblProfDesc.TabIndex = 0
        lblProfDesc.Text = "Words to censor in translated output. Uncheck to disable without removing."
        '
        ' clbProfWords
        '
        clbProfWords.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        clbProfWords.CheckOnClick = True
        clbProfWords.Font = New Font("Consolas", 10F)
        clbProfWords.FormattingEnabled = True
        clbProfWords.Location = New Point(8, 28)
        clbProfWords.Name = "clbProfWords"
        clbProfWords.Size = New Size(710, 430)
        clbProfWords.TabIndex = 1
        '
        ' txtProfWord
        '
        txtProfWord.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtProfWord.Location = New Point(8, 462)
        txtProfWord.Name = "txtProfWord"
        txtProfWord.Size = New Size(400, 23)
        txtProfWord.TabIndex = 2
        '
        ' btnProfAdd
        '
        btnProfAdd.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfAdd.Location = New Point(416, 461)
        btnProfAdd.Name = "btnProfAdd"
        btnProfAdd.Size = New Size(60, 25)
        btnProfAdd.TabIndex = 3
        btnProfAdd.Text = "Add"
        btnProfAdd.UseVisualStyleBackColor = True
        '
        ' btnProfRemove
        '
        btnProfRemove.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfRemove.Location = New Point(484, 461)
        btnProfRemove.Name = "btnProfRemove"
        btnProfRemove.Size = New Size(110, 25)
        btnProfRemove.TabIndex = 4
        btnProfRemove.Text = "Remove Selected"
        btnProfRemove.UseVisualStyleBackColor = True
        '
        ' btnProfSave
        '
        btnProfSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfSave.Location = New Point(602, 461)
        btnProfSave.Name = "btnProfSave"
        btnProfSave.Size = New Size(120, 25)
        btnProfSave.TabIndex = 5
        btnProfSave.Text = "Save && Reload"
        btnProfSave.UseVisualStyleBackColor = True
        '
        ' tabGlos
        '
        tabGlos.Controls.Add(dgvGlossary)
        tabGlos.Controls.Add(btnGlosAdd)
        tabGlos.Controls.Add(btnGlosRemove)
        tabGlos.Controls.Add(grpDetail)
        tabGlos.Controls.Add(btnGlosSave)
        tabGlos.Controls.Add(lblGlosDesc)
        tabGlos.Location = New Point(4, 24)
        tabGlos.Name = "tabGlos"
        tabGlos.Padding = New Padding(8)
        tabGlos.Size = New Size(726, 495)
        tabGlos.TabIndex = 2
        tabGlos.Text = "Glossary"
        tabGlos.UseVisualStyleBackColor = True
        '
        ' dgvGlossary
        '
        dgvGlossary.AllowUserToAddRows = False
        dgvGlossary.AllowUserToDeleteRows = False
        dgvGlossary.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        dgvGlossary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvGlossary.Columns.AddRange(New DataGridViewColumn() {colGlosEnabled, colTrigger, colComment})
        dgvGlossary.Location = New Point(8, 28)
        dgvGlossary.MultiSelect = False
        dgvGlossary.Name = "dgvGlossary"
        dgvGlossary.RowHeadersVisible = False
        dgvGlossary.ScrollBars = ScrollBars.Vertical
        dgvGlossary.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvGlossary.Size = New Size(710, 130)
        dgvGlossary.TabIndex = 1
        '
        ' colGlosEnabled
        '
        colGlosEnabled.FillWeight = 8F
        colGlosEnabled.HeaderText = ""
        colGlosEnabled.Name = "colGlosEnabled"
        colGlosEnabled.Width = 30
        '
        ' colTrigger
        '
        colTrigger.FillWeight = 30F
        colTrigger.HeaderText = "Trigger"
        colTrigger.Name = "colTrigger"
        colTrigger.ReadOnly = True
        '
        ' colComment
        '
        colComment.FillWeight = 62F
        colComment.HeaderText = "Comment"
        colComment.Name = "colComment"
        colComment.ReadOnly = True
        '
        ' btnGlosAdd
        '
        btnGlosAdd.Location = New Point(8, 164)
        btnGlosAdd.Name = "btnGlosAdd"
        btnGlosAdd.Size = New Size(90, 25)
        btnGlosAdd.TabIndex = 2
        btnGlosAdd.Text = "Add Entry"
        btnGlosAdd.UseVisualStyleBackColor = True
        '
        ' btnGlosRemove
        '
        btnGlosRemove.Location = New Point(105, 164)
        btnGlosRemove.Name = "btnGlosRemove"
        btnGlosRemove.Size = New Size(100, 25)
        btnGlosRemove.TabIndex = 3
        btnGlosRemove.Text = "Remove Entry"
        btnGlosRemove.UseVisualStyleBackColor = True
        '
        ' grpDetail — Trigger | Comment | Fixes
        '
        grpDetail.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        grpDetail.Controls.Add(lblTrigger)
        grpDetail.Controls.Add(txtGlosTrigger)
        grpDetail.Controls.Add(lblComment)
        grpDetail.Controls.Add(txtGlosComment)
        grpDetail.Controls.Add(lblFixes)
        grpDetail.Controls.Add(dgvFixes)
        grpDetail.Controls.Add(btnFixAdd)
        grpDetail.Controls.Add(btnFixRemove)
        grpDetail.Location = New Point(8, 195)
        grpDetail.Name = "grpDetail"
        grpDetail.Size = New Size(710, 262)
        grpDetail.TabIndex = 4
        grpDetail.TabStop = False
        grpDetail.Text = "Selected Entry"
        '
        ' lblTrigger
        '
        lblTrigger.AutoSize = True
        lblTrigger.Location = New Point(10, 22)
        lblTrigger.Name = "lblTrigger"
        lblTrigger.Size = New Size(47, 15)
        lblTrigger.TabIndex = 0
        lblTrigger.Text = "Trigger:"
        '
        ' txtGlosTrigger
        '
        txtGlosTrigger.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtGlosTrigger.Location = New Point(100, 19)
        txtGlosTrigger.Name = "txtGlosTrigger"
        txtGlosTrigger.Size = New Size(600, 23)
        txtGlosTrigger.TabIndex = 1
        '
        ' lblComment
        '
        lblComment.AutoSize = True
        lblComment.Location = New Point(10, 50)
        lblComment.Name = "lblComment"
        lblComment.Size = New Size(64, 15)
        lblComment.TabIndex = 2
        lblComment.Text = "Comment:"
        '
        ' txtGlosComment
        '
        txtGlosComment.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtGlosComment.Location = New Point(100, 47)
        txtGlosComment.Name = "txtGlosComment"
        txtGlosComment.Size = New Size(600, 23)
        txtGlosComment.TabIndex = 3
        '
        ' lblFixes
        '
        lblFixes.AutoSize = True
        lblFixes.Location = New Point(10, 78)
        lblFixes.Name = "lblFixes"
        lblFixes.Size = New Size(35, 15)
        lblFixes.TabIndex = 4
        lblFixes.Text = "Fixes:"
        '
        ' dgvFixes
        '
        dgvFixes.AllowUserToAddRows = False
        dgvFixes.AllowUserToDeleteRows = False
        dgvFixes.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        dgvFixes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvFixes.Columns.AddRange(New DataGridViewColumn() {colTargetLang, colWrong, colRight})
        dgvFixes.Location = New Point(10, 96)
        dgvFixes.MultiSelect = False
        dgvFixes.Name = "dgvFixes"
        dgvFixes.RowHeadersVisible = False
        dgvFixes.ScrollBars = ScrollBars.Vertical
        dgvFixes.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvFixes.Size = New Size(590, 154)
        dgvFixes.TabIndex = 5
        '
        ' colTargetLang — ComboBox column with language display names
        '
        colTargetLang.FillWeight = 30F
        colTargetLang.HeaderText = "Target Lang"
        colTargetLang.Name = "colTargetLang"
        colTargetLang.DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        '
        ' colWrong
        '
        colWrong.FillWeight = 35F
        colWrong.HeaderText = "Wrong"
        colWrong.Name = "colWrong"
        '
        ' colRight
        '
        colRight.FillWeight = 35F
        colRight.HeaderText = "Right"
        colRight.Name = "colRight"
        '
        ' btnFixAdd
        '
        btnFixAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnFixAdd.Location = New Point(610, 96)
        btnFixAdd.Name = "btnFixAdd"
        btnFixAdd.Size = New Size(90, 25)
        btnFixAdd.TabIndex = 6
        btnFixAdd.Text = "Add Fix"
        btnFixAdd.UseVisualStyleBackColor = True
        '
        ' btnFixRemove
        '
        btnFixRemove.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnFixRemove.Location = New Point(610, 126)
        btnFixRemove.Name = "btnFixRemove"
        btnFixRemove.Size = New Size(90, 25)
        btnFixRemove.TabIndex = 7
        btnFixRemove.Text = "Remove Fix"
        btnFixRemove.UseVisualStyleBackColor = True
        '
        ' btnGlosSave
        '
        btnGlosSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnGlosSave.Location = New Point(601, 463)
        btnGlosSave.Name = "btnGlosSave"
        btnGlosSave.Size = New Size(122, 25)
        btnGlosSave.TabIndex = 5
        btnGlosSave.Text = "Save && Reload"
        btnGlosSave.UseVisualStyleBackColor = True
        '
        ' lblGlosDesc
        '
        lblGlosDesc.AutoSize = True
        lblGlosDesc.Location = New Point(8, 8)
        lblGlosDesc.Name = "lblGlosDesc"
        lblGlosDesc.Size = New Size(152, 15)
        lblGlosDesc.TabIndex = 0
        lblGlosDesc.Text = "Translation glossary entries:"
        '
        ' FormFilterEditor
        '
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(734, 561)
        Controls.Add(lblLang)
        Controls.Add(cboLang)
        Controls.Add(btnAddLang)
        Controls.Add(TabControl1)
        MinimumSize = New Size(650, 500)
        Name = "FormFilterEditor"
        StartPosition = FormStartPosition.CenterParent
        Text = "Filter Editor"
        TabControl1.ResumeLayout(False)
        tabHal.ResumeLayout(False)
        tabHal.PerformLayout()
        tabProf.ResumeLayout(False)
        tabProf.PerformLayout()
        tabGlos.ResumeLayout(False)
        tabGlos.PerformLayout()
        CType(dgvGlossary, ComponentModel.ISupportInitialize).EndInit()
        grpDetail.ResumeLayout(False)
        grpDetail.PerformLayout()
        CType(dgvFixes, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    ' Shared language selector
    Friend WithEvents lblLang As System.Windows.Forms.Label
    Friend WithEvents cboLang As System.Windows.Forms.ComboBox
    Friend WithEvents btnAddLang As System.Windows.Forms.Button

    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents tabHal As System.Windows.Forms.TabPage
    Friend WithEvents tabProf As System.Windows.Forms.TabPage
    Friend WithEvents tabGlos As System.Windows.Forms.TabPage

    ' Hallucinations tab
    Friend WithEvents lblHalDesc As System.Windows.Forms.Label
    Friend WithEvents clbHalPhrases As System.Windows.Forms.CheckedListBox
    Friend WithEvents txtHalPhrase As System.Windows.Forms.TextBox
    Friend WithEvents btnHalAdd As System.Windows.Forms.Button
    Friend WithEvents btnHalRemove As System.Windows.Forms.Button
    Friend WithEvents btnHalSave As System.Windows.Forms.Button

    ' Profanity tab
    Friend WithEvents lblProfDesc As System.Windows.Forms.Label
    Friend WithEvents clbProfWords As System.Windows.Forms.CheckedListBox
    Friend WithEvents txtProfWord As System.Windows.Forms.TextBox
    Friend WithEvents btnProfAdd As System.Windows.Forms.Button
    Friend WithEvents btnProfRemove As System.Windows.Forms.Button
    Friend WithEvents btnProfSave As System.Windows.Forms.Button

    ' Glossary tab
    Friend WithEvents lblGlosDesc As System.Windows.Forms.Label
    Friend WithEvents dgvGlossary As System.Windows.Forms.DataGridView
    Friend WithEvents colGlosEnabled As System.Windows.Forms.DataGridViewCheckBoxColumn
    Friend WithEvents colTrigger As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colComment As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnGlosAdd As System.Windows.Forms.Button
    Friend WithEvents btnGlosRemove As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblTrigger As System.Windows.Forms.Label
    Friend WithEvents txtGlosTrigger As System.Windows.Forms.TextBox
    Friend WithEvents lblComment As System.Windows.Forms.Label
    Friend WithEvents txtGlosComment As System.Windows.Forms.TextBox
    Friend WithEvents lblFixes As System.Windows.Forms.Label
    Friend WithEvents dgvFixes As System.Windows.Forms.DataGridView
    Friend WithEvents colTargetLang As System.Windows.Forms.DataGridViewComboBoxColumn
    Friend WithEvents colWrong As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colRight As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnFixAdd As System.Windows.Forms.Button
    Friend WithEvents btnFixRemove As System.Windows.Forms.Button
    Friend WithEvents btnGlosSave As System.Windows.Forms.Button

End Class
