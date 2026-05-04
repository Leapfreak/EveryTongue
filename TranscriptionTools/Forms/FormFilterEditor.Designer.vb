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
        TabControl1 = New TabControl()
        tabHal = New TabPage()
        lblHalDesc = New Label()
        lblHalLang = New Label()
        cboHalLang = New ComboBox()
        btnHalAddLang = New Button()
        lstHalPhrases = New ListBox()
        txtHalPhrase = New TextBox()
        btnHalAdd = New Button()
        btnHalRemove = New Button()
        btnHalSave = New Button()
        tabProf = New TabPage()
        lblProfDesc = New Label()
        lblProfLang = New Label()
        cboProfLang = New ComboBox()
        btnProfAddLang = New Button()
        lstProfWords = New ListBox()
        txtProfWord = New TextBox()
        btnProfAdd = New Button()
        btnProfRemove = New Button()
        btnProfSave = New Button()
        tabGlos = New TabPage()
        dgvGlossary = New DataGridView()
        colTrigger = New DataGridViewTextBoxColumn()
        colSourceLangs = New DataGridViewTextBoxColumn()
        colComment = New DataGridViewTextBoxColumn()
        btnGlosAdd = New Button()
        btnGlosRemove = New Button()
        grpDetail = New GroupBox()
        lblTrigger = New Label()
        txtGlosTrigger = New TextBox()
        lblSrcLangs = New Label()
        txtGlosSourceLangs = New TextBox()
        lblComment = New Label()
        txtGlosComment = New TextBox()
        dgvFixes = New DataGridView()
        colTargetLang = New DataGridViewTextBoxColumn()
        colWrong = New DataGridViewTextBoxColumn()
        colRight = New DataGridViewTextBoxColumn()
        btnFixAdd = New Button()
        btnFixRemove = New Button()
        lblFixes = New Label()
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
        ' TabControl1
        ' 
        TabControl1.Controls.Add(tabHal)
        TabControl1.Controls.Add(tabProf)
        TabControl1.Controls.Add(tabGlos)
        TabControl1.Dock = DockStyle.Fill
        TabControl1.Location = New Point(0, 0)
        TabControl1.Name = "TabControl1"
        TabControl1.SelectedIndex = 0
        TabControl1.Size = New Size(734, 511)
        TabControl1.TabIndex = 0
        ' 
        ' tabHal
        ' 
        tabHal.Controls.Add(lblHalDesc)
        tabHal.Controls.Add(lblHalLang)
        tabHal.Controls.Add(cboHalLang)
        tabHal.Controls.Add(btnHalAddLang)
        tabHal.Controls.Add(lstHalPhrases)
        tabHal.Controls.Add(txtHalPhrase)
        tabHal.Controls.Add(btnHalAdd)
        tabHal.Controls.Add(btnHalRemove)
        tabHal.Controls.Add(btnHalSave)
        tabHal.Location = New Point(4, 24)
        tabHal.Name = "tabHal"
        tabHal.Padding = New Padding(8)
        tabHal.Size = New Size(726, 483)
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
        lblHalDesc.Text = "Phrases that whisper hallucinates (e.g. 'Thanks for watching'). Matched case-insensitively."
        ' 
        ' lblHalLang
        ' 
        lblHalLang.AutoSize = True
        lblHalLang.Location = New Point(8, 38)
        lblHalLang.Name = "lblHalLang"
        lblHalLang.Size = New Size(62, 15)
        lblHalLang.TabIndex = 1
        lblHalLang.Text = "Language:"
        ' 
        ' cboHalLang
        ' 
        cboHalLang.DropDownStyle = ComboBoxStyle.DropDownList
        cboHalLang.Location = New Point(80, 35)
        cboHalLang.Name = "cboHalLang"
        cboHalLang.Size = New Size(150, 23)
        cboHalLang.TabIndex = 2
        ' 
        ' btnHalAddLang
        ' 
        btnHalAddLang.Location = New Point(240, 34)
        btnHalAddLang.Name = "btnHalAddLang"
        btnHalAddLang.Size = New Size(110, 25)
        btnHalAddLang.TabIndex = 3
        btnHalAddLang.Text = "Add Language..."
        btnHalAddLang.UseVisualStyleBackColor = True
        ' 
        ' lstHalPhrases
        ' 
        lstHalPhrases.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lstHalPhrases.Font = New Font("Consolas", 10F)
        lstHalPhrases.FormattingEnabled = True
        lstHalPhrases.ItemHeight = 15
        lstHalPhrases.Location = New Point(8, 65)
        lstHalPhrases.Name = "lstHalPhrases"
        lstHalPhrases.Size = New Size(710, 379)
        lstHalPhrases.TabIndex = 4
        ' 
        ' txtHalPhrase
        ' 
        txtHalPhrase.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtHalPhrase.Location = New Point(8, 449)
        txtHalPhrase.Name = "txtHalPhrase"
        txtHalPhrase.Size = New Size(400, 23)
        txtHalPhrase.TabIndex = 5
        ' 
        ' btnHalAdd
        ' 
        btnHalAdd.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalAdd.Location = New Point(416, 448)
        btnHalAdd.Name = "btnHalAdd"
        btnHalAdd.Size = New Size(60, 25)
        btnHalAdd.TabIndex = 6
        btnHalAdd.Text = "Add"
        btnHalAdd.UseVisualStyleBackColor = True
        ' 
        ' btnHalRemove
        ' 
        btnHalRemove.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalRemove.Location = New Point(484, 448)
        btnHalRemove.Name = "btnHalRemove"
        btnHalRemove.Size = New Size(110, 25)
        btnHalRemove.TabIndex = 7
        btnHalRemove.Text = "Remove Selected"
        btnHalRemove.UseVisualStyleBackColor = True
        ' 
        ' btnHalSave
        ' 
        btnHalSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnHalSave.Location = New Point(602, 448)
        btnHalSave.Name = "btnHalSave"
        btnHalSave.Size = New Size(120, 25)
        btnHalSave.TabIndex = 8
        btnHalSave.Text = "Save && Reload"
        btnHalSave.UseVisualStyleBackColor = True
        ' 
        ' tabProf
        ' 
        tabProf.Controls.Add(lblProfDesc)
        tabProf.Controls.Add(lblProfLang)
        tabProf.Controls.Add(cboProfLang)
        tabProf.Controls.Add(btnProfAddLang)
        tabProf.Controls.Add(lstProfWords)
        tabProf.Controls.Add(txtProfWord)
        tabProf.Controls.Add(btnProfAdd)
        tabProf.Controls.Add(btnProfRemove)
        tabProf.Controls.Add(btnProfSave)
        tabProf.Location = New Point(4, 24)
        tabProf.Name = "tabProf"
        tabProf.Padding = New Padding(8)
        tabProf.Size = New Size(726, 483)
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
        lblProfDesc.Text = "Words to censor in translated output. Matches are replaced with [...]."
        ' 
        ' lblProfLang
        ' 
        lblProfLang.AutoSize = True
        lblProfLang.Location = New Point(8, 38)
        lblProfLang.Name = "lblProfLang"
        lblProfLang.Size = New Size(62, 15)
        lblProfLang.TabIndex = 1
        lblProfLang.Text = "Language:"
        ' 
        ' cboProfLang
        ' 
        cboProfLang.DropDownStyle = ComboBoxStyle.DropDownList
        cboProfLang.Location = New Point(80, 35)
        cboProfLang.Name = "cboProfLang"
        cboProfLang.Size = New Size(150, 23)
        cboProfLang.TabIndex = 2
        ' 
        ' btnProfAddLang
        ' 
        btnProfAddLang.Location = New Point(240, 34)
        btnProfAddLang.Name = "btnProfAddLang"
        btnProfAddLang.Size = New Size(110, 25)
        btnProfAddLang.TabIndex = 3
        btnProfAddLang.Text = "Add Language..."
        btnProfAddLang.UseVisualStyleBackColor = True
        ' 
        ' lstProfWords
        ' 
        lstProfWords.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lstProfWords.Font = New Font("Consolas", 10F)
        lstProfWords.FormattingEnabled = True
        lstProfWords.ItemHeight = 15
        lstProfWords.Location = New Point(8, 65)
        lstProfWords.Name = "lstProfWords"
        lstProfWords.Size = New Size(710, 379)
        lstProfWords.TabIndex = 4
        ' 
        ' txtProfWord
        ' 
        txtProfWord.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtProfWord.Location = New Point(8, 449)
        txtProfWord.Name = "txtProfWord"
        txtProfWord.Size = New Size(400, 23)
        txtProfWord.TabIndex = 5
        ' 
        ' btnProfAdd
        ' 
        btnProfAdd.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfAdd.Location = New Point(416, 448)
        btnProfAdd.Name = "btnProfAdd"
        btnProfAdd.Size = New Size(60, 28)
        btnProfAdd.TabIndex = 6
        btnProfAdd.Text = "Add"
        btnProfAdd.UseVisualStyleBackColor = True
        ' 
        ' btnProfRemove
        ' 
        btnProfRemove.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfRemove.Location = New Point(484, 448)
        btnProfRemove.Name = "btnProfRemove"
        btnProfRemove.Size = New Size(110, 28)
        btnProfRemove.TabIndex = 7
        btnProfRemove.Text = "Remove Selected"
        btnProfRemove.UseVisualStyleBackColor = True
        ' 
        ' btnProfSave
        ' 
        btnProfSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnProfSave.Location = New Point(602, 448)
        btnProfSave.Name = "btnProfSave"
        btnProfSave.Size = New Size(120, 28)
        btnProfSave.TabIndex = 8
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
        tabGlos.Size = New Size(726, 483)
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
        dgvGlossary.Columns.AddRange(New DataGridViewColumn() {colTrigger, colSourceLangs, colComment})
        dgvGlossary.Location = New Point(8, 28)
        dgvGlossary.MultiSelect = False
        dgvGlossary.Name = "dgvGlossary"
        dgvGlossary.ReadOnly = True
        dgvGlossary.RowHeadersVisible = False
        dgvGlossary.ScrollBars = ScrollBars.Vertical
        dgvGlossary.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvGlossary.Size = New Size(710, 150)
        dgvGlossary.TabIndex = 1
        ' 
        ' colTrigger
        ' 
        colTrigger.FillWeight = 20F
        colTrigger.HeaderText = "Trigger"
        colTrigger.Name = "colTrigger"
        colTrigger.ReadOnly = True
        ' 
        ' colSourceLangs
        ' 
        colSourceLangs.FillWeight = 25F
        colSourceLangs.HeaderText = "Source Langs"
        colSourceLangs.Name = "colSourceLangs"
        colSourceLangs.ReadOnly = True
        ' 
        ' colComment
        ' 
        colComment.FillWeight = 55F
        colComment.HeaderText = "Comment"
        colComment.Name = "colComment"
        colComment.ReadOnly = True
        ' 
        ' btnGlosAdd
        ' 
        btnGlosAdd.Location = New Point(8, 184)
        btnGlosAdd.Name = "btnGlosAdd"
        btnGlosAdd.Size = New Size(90, 25)
        btnGlosAdd.TabIndex = 2
        btnGlosAdd.Text = "Add Entry"
        btnGlosAdd.UseVisualStyleBackColor = True
        ' 
        ' btnGlosRemove
        ' 
        btnGlosRemove.Location = New Point(105, 184)
        btnGlosRemove.Name = "btnGlosRemove"
        btnGlosRemove.Size = New Size(100, 25)
        btnGlosRemove.TabIndex = 3
        btnGlosRemove.Text = "Remove Entry"
        btnGlosRemove.UseVisualStyleBackColor = True
        ' 
        ' grpDetail
        ' 
        grpDetail.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        grpDetail.Controls.Add(lblTrigger)
        grpDetail.Controls.Add(txtGlosTrigger)
        grpDetail.Controls.Add(lblSrcLangs)
        grpDetail.Controls.Add(txtGlosSourceLangs)
        grpDetail.Controls.Add(lblComment)
        grpDetail.Controls.Add(txtGlosComment)
        grpDetail.Controls.Add(dgvFixes)
        grpDetail.Controls.Add(btnFixAdd)
        grpDetail.Controls.Add(btnFixRemove)
        grpDetail.Controls.Add(lblFixes)
        grpDetail.Location = New Point(8, 215)
        grpDetail.Name = "grpDetail"
        grpDetail.Size = New Size(710, 230)
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
        txtGlosTrigger.Location = New Point(100, 19)
        txtGlosTrigger.Name = "txtGlosTrigger"
        txtGlosTrigger.Size = New Size(200, 23)
        txtGlosTrigger.TabIndex = 1
        ' 
        ' lblSrcLangs
        ' 
        lblSrcLangs.AutoSize = True
        lblSrcLangs.Location = New Point(310, 22)
        lblSrcLangs.Name = "lblSrcLangs"
        lblSrcLangs.Size = New Size(80, 15)
        lblSrcLangs.TabIndex = 2
        lblSrcLangs.Text = "Source Langs:"
        ' 
        ' txtGlosSourceLangs
        ' 
        txtGlosSourceLangs.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtGlosSourceLangs.Location = New Point(410, 19)
        txtGlosSourceLangs.Name = "txtGlosSourceLangs"
        txtGlosSourceLangs.Size = New Size(290, 23)
        txtGlosSourceLangs.TabIndex = 3
        ' 
        ' lblComment
        ' 
        lblComment.AutoSize = True
        lblComment.Location = New Point(10, 50)
        lblComment.Name = "lblComment"
        lblComment.Size = New Size(64, 15)
        lblComment.TabIndex = 4
        lblComment.Text = "Comment:"
        ' 
        ' txtGlosComment
        ' 
        txtGlosComment.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtGlosComment.Location = New Point(100, 47)
        txtGlosComment.Name = "txtGlosComment"
        txtGlosComment.Size = New Size(600, 23)
        txtGlosComment.TabIndex = 5
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
        dgvFixes.Size = New Size(590, 120)
        dgvFixes.TabIndex = 7
        ' 
        ' colTargetLang
        ' 
        colTargetLang.FillWeight = 30F
        colTargetLang.HeaderText = "Target Lang"
        colTargetLang.Name = "colTargetLang"
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
        btnFixAdd.TabIndex = 8
        btnFixAdd.Text = "Add Fix"
        btnFixAdd.UseVisualStyleBackColor = True
        ' 
        ' btnFixRemove
        ' 
        btnFixRemove.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnFixRemove.Location = New Point(610, 126)
        btnFixRemove.Name = "btnFixRemove"
        btnFixRemove.Size = New Size(90, 25)
        btnFixRemove.TabIndex = 9
        btnFixRemove.Text = "Remove Fix"
        btnFixRemove.UseVisualStyleBackColor = True
        ' 
        ' lblFixes
        ' 
        lblFixes.AutoSize = True
        lblFixes.Location = New Point(10, 78)
        lblFixes.Name = "lblFixes"
        lblFixes.Size = New Size(35, 15)
        lblFixes.TabIndex = 6
        lblFixes.Text = "Fixes:"
        ' 
        ' btnGlosSave
        ' 
        btnGlosSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnGlosSave.Location = New Point(601, 451)
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
        ClientSize = New Size(734, 511)
        Controls.Add(TabControl1)
        MinimumSize = New Size(650, 400)
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
    End Sub

    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents tabHal As System.Windows.Forms.TabPage
    Friend WithEvents tabProf As System.Windows.Forms.TabPage
    Friend WithEvents tabGlos As System.Windows.Forms.TabPage

    ' Hallucinations tab
    Friend WithEvents lblHalDesc As System.Windows.Forms.Label
    Friend WithEvents lblHalLang As System.Windows.Forms.Label
    Friend WithEvents cboHalLang As System.Windows.Forms.ComboBox
    Friend WithEvents btnHalAddLang As System.Windows.Forms.Button
    Friend WithEvents lstHalPhrases As System.Windows.Forms.ListBox
    Friend WithEvents txtHalPhrase As System.Windows.Forms.TextBox
    Friend WithEvents btnHalAdd As System.Windows.Forms.Button
    Friend WithEvents btnHalRemove As System.Windows.Forms.Button
    Friend WithEvents btnHalSave As System.Windows.Forms.Button

    ' Profanity tab
    Friend WithEvents lblProfDesc As System.Windows.Forms.Label
    Friend WithEvents lblProfLang As System.Windows.Forms.Label
    Friend WithEvents cboProfLang As System.Windows.Forms.ComboBox
    Friend WithEvents btnProfAddLang As System.Windows.Forms.Button
    Friend WithEvents lstProfWords As System.Windows.Forms.ListBox
    Friend WithEvents txtProfWord As System.Windows.Forms.TextBox
    Friend WithEvents btnProfAdd As System.Windows.Forms.Button
    Friend WithEvents btnProfRemove As System.Windows.Forms.Button
    Friend WithEvents btnProfSave As System.Windows.Forms.Button

    ' Glossary tab
    Friend WithEvents lblGlosDesc As System.Windows.Forms.Label
    Friend WithEvents dgvGlossary As System.Windows.Forms.DataGridView
    Friend WithEvents colTrigger As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colSourceLangs As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colComment As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnGlosAdd As System.Windows.Forms.Button
    Friend WithEvents btnGlosRemove As System.Windows.Forms.Button
    Friend WithEvents grpDetail As System.Windows.Forms.GroupBox
    Friend WithEvents lblTrigger As System.Windows.Forms.Label
    Friend WithEvents txtGlosTrigger As System.Windows.Forms.TextBox
    Friend WithEvents lblSrcLangs As System.Windows.Forms.Label
    Friend WithEvents txtGlosSourceLangs As System.Windows.Forms.TextBox
    Friend WithEvents lblComment As System.Windows.Forms.Label
    Friend WithEvents txtGlosComment As System.Windows.Forms.TextBox
    Friend WithEvents lblFixes As System.Windows.Forms.Label
    Friend WithEvents dgvFixes As System.Windows.Forms.DataGridView
    Friend WithEvents colTargetLang As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colWrong As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents colRight As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents btnFixAdd As System.Windows.Forms.Button
    Friend WithEvents btnFixRemove As System.Windows.Forms.Button
    Friend WithEvents btnGlosSave As System.Windows.Forms.Button

End Class
