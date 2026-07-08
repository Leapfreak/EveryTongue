Namespace Forms

    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
    Partial Class FormDownloadManager
        Inherits System.Windows.Forms.Form

        Private components As System.ComponentModel.IContainer

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso (components IsNot Nothing) Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        Private Sub InitializeComponent()
            tabMain = New TabControl()
            tabComponents = New TabPage()
            lvTools = New ListView()
            colToolName = New ColumnHeader()
            colToolCategory = New ColumnHeader()
            colToolStatus = New ColumnHeader()
            colToolVersion = New ColumnHeader()
            pnlToolsButtons = New Panel()
            btnDownloadAll = New Button()
            btnRefresh = New Button()
            tabPiper = New TabPage()
            lvVoices = New ListView()
            colVoiceLang = New ColumnHeader()
            colVoiceModel = New ColumnHeader()
            colVoiceStatus = New ColumnHeader()
            pnlVoicesButtons = New Panel()
            btnDownloadVoices = New Button()
            btnRemoveVoices = New Button()
            tabMmsTts = New TabPage()
            lblMmsTtsInfo = New Label()
            lblMmsTtsStatus = New Label()
            btnInstallMmsTts = New Button()
            tabBiblicalVocab = New TabPage()
            lblVocabInfo = New Label()
            lblVocabStatus = New Label()
            btnGenerateVocab = New Button()
            tabBibles = New TabPage()
            pnlBibleSearch = New Panel()
            txtBibleSearch = New TextBox()
            tvBibles = New TreeView()
            pnlBiblesButtons = New Panel()
            btnFetchCatalog = New Button()
            btnDownloadBibles = New Button()
            btnOpenBiblesFolder = New Button()
            tabLangPacks = New TabPage()
            lvLangPacks = New ListView()
            colLangName = New ColumnHeader()
            colLangNative = New ColumnHeader()
            colLangCode = New ColumnHeader()
            colLangStatus = New ColumnHeader()
            pnlLangPacksButtons = New Panel()
            btnDownloadLangPacks = New Button()
            btnDeleteLangPacks = New Button()
            pnlBottom = New Panel()
            pbProgress = New ProgressBar()
            lblProgress = New Label()
            btnOk = New Button()
            btnCancel = New Button()
            tabMain.SuspendLayout()
            tabComponents.SuspendLayout()
            pnlToolsButtons.SuspendLayout()
            tabPiper.SuspendLayout()
            pnlVoicesButtons.SuspendLayout()
            tabMmsTts.SuspendLayout()
            tabBiblicalVocab.SuspendLayout()
            tabBibles.SuspendLayout()
            pnlBibleSearch.SuspendLayout()
            pnlBiblesButtons.SuspendLayout()
            tabLangPacks.SuspendLayout()
            pnlLangPacksButtons.SuspendLayout()
            pnlBottom.SuspendLayout()
            SuspendLayout()
            ' 
            ' tabMain
            ' 
            tabMain.Controls.Add(tabLangPacks)
            tabMain.Controls.Add(tabComponents)
            tabMain.Controls.Add(tabPiper)
            tabMain.Controls.Add(tabMmsTts)
            tabMain.Controls.Add(tabBiblicalVocab)
            tabMain.Controls.Add(tabBibles)
            tabMain.Dock = DockStyle.Fill
            tabMain.Location = New Point(0, 0)
            tabMain.Name = "tabMain"
            tabMain.Padding = New Point(12, 4)
            tabMain.SelectedIndex = 0
            tabMain.Size = New Size(700, 448)
            tabMain.TabIndex = 0
            ' 
            ' tabComponents
            ' 
            tabComponents.Controls.Add(lvTools)
            tabComponents.Controls.Add(pnlToolsButtons)
            tabComponents.Location = New Point(4, 26)
            tabComponents.Name = "tabComponents"
            tabComponents.Padding = New Padding(8)
            tabComponents.Size = New Size(692, 418)
            tabComponents.TabIndex = 0
            tabComponents.Text = "Components"
            ' 
            ' lvTools
            ' 
            lvTools.CheckBoxes = True
            lvTools.Columns.AddRange(New ColumnHeader() {colToolName, colToolCategory, colToolStatus, colToolVersion})
            lvTools.Dock = DockStyle.Fill
            lvTools.FullRowSelect = True
            lvTools.GridLines = True
            lvTools.Location = New Point(8, 8)
            lvTools.Name = "lvTools"
            lvTools.Size = New Size(676, 364)
            lvTools.TabIndex = 0
            lvTools.UseCompatibleStateImageBehavior = False
            lvTools.View = View.Details
            ' 
            ' colToolName
            ' 
            colToolName.Text = "Component"
            colToolName.Width = 230
            ' 
            ' colToolCategory
            ' 
            colToolCategory.Text = "Category"
            colToolCategory.Width = 100
            ' 
            ' colToolStatus
            ' 
            colToolStatus.Text = "Status"
            colToolStatus.Width = 120
            ' 
            ' colToolVersion
            ' 
            colToolVersion.Text = "Version / Details"
            colToolVersion.Width = 200
            ' 
            ' pnlToolsButtons
            ' 
            pnlToolsButtons.Controls.Add(btnDownloadAll)
            pnlToolsButtons.Controls.Add(btnRefresh)
            pnlToolsButtons.Dock = DockStyle.Bottom
            pnlToolsButtons.Location = New Point(8, 372)
            pnlToolsButtons.Name = "pnlToolsButtons"
            pnlToolsButtons.Size = New Size(676, 38)
            pnlToolsButtons.TabIndex = 1
            ' 
            ' btnDownloadAll
            ' 
            btnDownloadAll.Location = New Point(0, 4)
            btnDownloadAll.Name = "btnDownloadAll"
            btnDownloadAll.Size = New Size(140, 30)
            btnDownloadAll.TabIndex = 0
            btnDownloadAll.Text = "Download Selected"
            ' 
            ' btnRefresh
            ' 
            btnRefresh.Location = New Point(148, 4)
            btnRefresh.Name = "btnRefresh"
            btnRefresh.Size = New Size(90, 30)
            btnRefresh.TabIndex = 1
            btnRefresh.Text = "Refresh"
            ' 
            ' tabPiper
            ' 
            tabPiper.Controls.Add(lvVoices)
            tabPiper.Controls.Add(pnlVoicesButtons)
            tabPiper.Location = New Point(4, 26)
            tabPiper.Name = "tabPiper"
            tabPiper.Padding = New Padding(8)
            tabPiper.Size = New Size(692, 418)
            tabPiper.TabIndex = 1
            tabPiper.Text = "Piper Voices"
            ' 
            ' lvVoices
            ' 
            lvVoices.CheckBoxes = True
            lvVoices.Columns.AddRange(New ColumnHeader() {colVoiceLang, colVoiceModel, colVoiceStatus})
            lvVoices.Dock = DockStyle.Fill
            lvVoices.FullRowSelect = True
            lvVoices.GridLines = True
            lvVoices.Location = New Point(8, 8)
            lvVoices.Name = "lvVoices"
            lvVoices.Size = New Size(676, 364)
            lvVoices.TabIndex = 0
            lvVoices.UseCompatibleStateImageBehavior = False
            lvVoices.View = View.Details
            ' 
            ' colVoiceLang
            ' 
            colVoiceLang.Text = "Language"
            colVoiceLang.Width = 200
            ' 
            ' colVoiceModel
            ' 
            colVoiceModel.Text = "Voice Model"
            colVoiceModel.Width = 300
            ' 
            ' colVoiceStatus
            ' 
            colVoiceStatus.Text = "Status"
            colVoiceStatus.Width = 130
            ' 
            ' pnlVoicesButtons
            ' 
            pnlVoicesButtons.Controls.Add(btnDownloadVoices)
            pnlVoicesButtons.Controls.Add(btnRemoveVoices)
            pnlVoicesButtons.Dock = DockStyle.Bottom
            pnlVoicesButtons.Location = New Point(8, 372)
            pnlVoicesButtons.Name = "pnlVoicesButtons"
            pnlVoicesButtons.Size = New Size(676, 38)
            pnlVoicesButtons.TabIndex = 1
            ' 
            ' btnDownloadVoices
            ' 
            btnDownloadVoices.Location = New Point(0, 4)
            btnDownloadVoices.Name = "btnDownloadVoices"
            btnDownloadVoices.Size = New Size(140, 30)
            btnDownloadVoices.TabIndex = 0
            btnDownloadVoices.Text = "Download Selected"
            ' 
            ' btnRemoveVoices
            ' 
            btnRemoveVoices.Location = New Point(148, 4)
            btnRemoveVoices.Name = "btnRemoveVoices"
            btnRemoveVoices.Size = New Size(140, 30)
            btnRemoveVoices.TabIndex = 1
            btnRemoveVoices.Text = "Remove Selected"
            ' 
            ' tabMmsTts
            ' 
            tabMmsTts.Controls.Add(lblMmsTtsInfo)
            tabMmsTts.Controls.Add(lblMmsTtsStatus)
            tabMmsTts.Controls.Add(btnInstallMmsTts)
            tabMmsTts.Location = New Point(4, 26)
            tabMmsTts.Name = "tabMmsTts"
            tabMmsTts.Padding = New Padding(16)
            tabMmsTts.Size = New Size(692, 418)
            tabMmsTts.TabIndex = 2
            tabMmsTts.Text = "MMS-TTS"
            ' 
            ' lblMmsTtsInfo
            ' 
            lblMmsTtsInfo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            lblMmsTtsInfo.Location = New Point(16, 16)
            lblMmsTtsInfo.Name = "lblMmsTtsInfo"
            lblMmsTtsInfo.Size = New Size(1112, 80)
            lblMmsTtsInfo.TabIndex = 0
            lblMmsTtsInfo.Text = "MMS-TTS is Meta's Massively Multilingual Speech synthesis engine." & vbCrLf & vbCrLf & "It covers 1100+ languages for when Piper doesn't have a voice model." & vbCrLf & "Requires CPU-only PyTorch (~200 MB download)."
            ' 
            ' lblMmsTtsStatus
            ' 
            lblMmsTtsStatus.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
            lblMmsTtsStatus.Location = New Point(16, 110)
            lblMmsTtsStatus.Name = "lblMmsTtsStatus"
            lblMmsTtsStatus.Size = New Size(460, 20)
            lblMmsTtsStatus.TabIndex = 1
            lblMmsTtsStatus.Text = "Checking..."
            ' 
            ' btnInstallMmsTts
            ' 
            btnInstallMmsTts.Location = New Point(16, 140)
            btnInstallMmsTts.Name = "btnInstallMmsTts"
            btnInstallMmsTts.Size = New Size(160, 30)
            btnInstallMmsTts.TabIndex = 2
            btnInstallMmsTts.Text = "Install"
            '
            ' tabBiblicalVocab
            '
            tabBiblicalVocab.Controls.Add(lblVocabInfo)
            tabBiblicalVocab.Controls.Add(lblVocabStatus)
            tabBiblicalVocab.Controls.Add(btnGenerateVocab)
            tabBiblicalVocab.Location = New Point(4, 26)
            tabBiblicalVocab.Name = "tabBiblicalVocab"
            tabBiblicalVocab.Padding = New Padding(16)
            tabBiblicalVocab.Size = New Size(692, 418)
            tabBiblicalVocab.TabIndex = 5
            tabBiblicalVocab.Text = "Biblical Vocabulary"
            '
            ' lblVocabInfo
            '
            lblVocabInfo.Anchor = AnchorStyles.Top Or AnchorStyles.Left
            lblVocabInfo.Location = New Point(16, 16)
            lblVocabInfo.Name = "lblVocabInfo"
            lblVocabInfo.Size = New Size(640, 120)
            lblVocabInfo.TabIndex = 0
            lblVocabInfo.Text = "Biblical name dictionaries help Speechmatics recognise proper nouns (Elies, Nabucodonosor, Getsemaní) instead of garbling them." & vbCrLf & vbCrLf & "The lists are generated on-device from the Bibles you have installed — no download. Install a language's Bible first, then generate."
            '
            ' lblVocabStatus
            '
            lblVocabStatus.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
            lblVocabStatus.Location = New Point(16, 146)
            lblVocabStatus.Name = "lblVocabStatus"
            lblVocabStatus.Size = New Size(640, 40)
            lblVocabStatus.TabIndex = 1
            lblVocabStatus.Text = "Checking..."
            '
            ' btnGenerateVocab
            '
            btnGenerateVocab.Location = New Point(16, 194)
            btnGenerateVocab.Name = "btnGenerateVocab"
            btnGenerateVocab.Size = New Size(240, 30)
            btnGenerateVocab.TabIndex = 2
            btnGenerateVocab.Text = "Generate from installed Bibles"
            '
            ' tabBibles
            '
            tabBibles.Controls.Add(tvBibles)
            tabBibles.Controls.Add(pnlBibleSearch)
            tabBibles.Controls.Add(pnlBiblesButtons)
            tabBibles.Location = New Point(4, 26)
            tabBibles.Name = "tabBibles"
            tabBibles.Padding = New Padding(8)
            tabBibles.Size = New Size(692, 418)
            tabBibles.TabIndex = 3
            tabBibles.Text = "Bibles"
            '
            ' pnlBibleSearch — top search bar
            '
            pnlBibleSearch.Controls.Add(txtBibleSearch)
            pnlBibleSearch.Dock = DockStyle.Top
            pnlBibleSearch.Height = 30
            pnlBibleSearch.Name = "pnlBibleSearch"
            '
            ' txtBibleSearch
            '
            txtBibleSearch.Dock = DockStyle.Fill
            txtBibleSearch.Name = "txtBibleSearch"
            txtBibleSearch.PlaceholderText = "Search translations..."
            '
            ' tvBibles — fills remaining space
            '
            tvBibles.CheckBoxes = True
            tvBibles.Dock = DockStyle.Fill
            tvBibles.Name = "tvBibles"
            '
            ' pnlBiblesButtons — bottom button bar
            '
            pnlBiblesButtons.Controls.Add(btnFetchCatalog)
            pnlBiblesButtons.Controls.Add(btnDownloadBibles)
            pnlBiblesButtons.Controls.Add(btnOpenBiblesFolder)
            pnlBiblesButtons.Dock = DockStyle.Bottom
            pnlBiblesButtons.Height = 38
            pnlBiblesButtons.Name = "pnlBiblesButtons"
            '
            ' btnFetchCatalog
            '
            btnFetchCatalog.Location = New Point(0, 4)
            btnFetchCatalog.Name = "btnFetchCatalog"
            btnFetchCatalog.Size = New Size(120, 30)
            btnFetchCatalog.TabIndex = 0
            btnFetchCatalog.Text = "Fetch Catalog"
            '
            ' btnDownloadBibles
            '
            btnDownloadBibles.Location = New Point(128, 4)
            btnDownloadBibles.Name = "btnDownloadBibles"
            btnDownloadBibles.Size = New Size(140, 30)
            btnDownloadBibles.TabIndex = 1
            btnDownloadBibles.Text = "Download Selected"
            '
            ' btnOpenBiblesFolder
            '
            btnOpenBiblesFolder.Location = New Point(276, 4)
            btnOpenBiblesFolder.Name = "btnOpenBiblesFolder"
            btnOpenBiblesFolder.Size = New Size(130, 30)
            btnOpenBiblesFolder.TabIndex = 2
            btnOpenBiblesFolder.Text = "Open Folder"
            '
            ' tabLangPacks
            '
            tabLangPacks.Controls.Add(lvLangPacks)
            tabLangPacks.Controls.Add(pnlLangPacksButtons)
            tabLangPacks.Location = New Point(4, 26)
            tabLangPacks.Name = "tabLangPacks"
            tabLangPacks.Padding = New Padding(8)
            tabLangPacks.Size = New Size(692, 418)
            tabLangPacks.TabIndex = 4
            tabLangPacks.Text = "Language Packs"
            '
            ' lvLangPacks
            '
            lvLangPacks.CheckBoxes = True
            lvLangPacks.Columns.AddRange(New ColumnHeader() {colLangName, colLangNative, colLangCode, colLangStatus})
            lvLangPacks.Dock = DockStyle.Fill
            lvLangPacks.FullRowSelect = True
            lvLangPacks.GridLines = True
            lvLangPacks.Location = New Point(8, 8)
            lvLangPacks.Name = "lvLangPacks"
            lvLangPacks.Size = New Size(676, 364)
            lvLangPacks.TabIndex = 0
            lvLangPacks.UseCompatibleStateImageBehavior = False
            lvLangPacks.View = View.Details
            '
            ' colLangName
            '
            colLangName.Text = "Language"
            colLangName.Width = 180
            '
            ' colLangNative
            '
            colLangNative.Text = "Native Name"
            colLangNative.Width = 180
            '
            ' colLangCode
            '
            colLangCode.Text = "Code"
            colLangCode.Width = 60
            '
            ' colLangStatus
            '
            colLangStatus.Text = "Status"
            colLangStatus.Width = 220
            '
            ' pnlLangPacksButtons
            '
            pnlLangPacksButtons.Controls.Add(btnDownloadLangPacks)
            pnlLangPacksButtons.Controls.Add(btnDeleteLangPacks)
            pnlLangPacksButtons.Dock = DockStyle.Bottom
            pnlLangPacksButtons.Height = 38
            pnlLangPacksButtons.Name = "pnlLangPacksButtons"
            '
            ' btnDownloadLangPacks
            '
            btnDownloadLangPacks.Location = New Point(0, 4)
            btnDownloadLangPacks.Name = "btnDownloadLangPacks"
            btnDownloadLangPacks.Size = New Size(140, 30)
            btnDownloadLangPacks.TabIndex = 0
            btnDownloadLangPacks.Text = "Download Selected"
            '
            ' btnDeleteLangPacks
            '
            btnDeleteLangPacks.Location = New Point(148, 4)
            btnDeleteLangPacks.Name = "btnDeleteLangPacks"
            btnDeleteLangPacks.Size = New Size(120, 30)
            btnDeleteLangPacks.TabIndex = 1
            btnDeleteLangPacks.Text = "Uninstall Selected"
            '
            ' pnlBottom
            '
            pnlBottom.Controls.Add(pbProgress)
            pnlBottom.Controls.Add(lblProgress)
            pnlBottom.Controls.Add(btnOk)
            pnlBottom.Controls.Add(btnCancel)
            pnlBottom.Dock = DockStyle.Bottom
            pnlBottom.Location = New Point(0, 448)
            pnlBottom.Name = "pnlBottom"
            pnlBottom.Size = New Size(700, 72)
            pnlBottom.TabIndex = 1
            ' 
            ' pbProgress
            ' 
            pbProgress.Location = New Point(12, 6)
            pbProgress.Name = "pbProgress"
            pbProgress.Size = New Size(676, 22)
            pbProgress.TabIndex = 0
            ' 
            ' lblProgress
            ' 
            lblProgress.Location = New Point(12, 34)
            lblProgress.Name = "lblProgress"
            lblProgress.Size = New Size(480, 20)
            lblProgress.TabIndex = 1
            lblProgress.Text = "Ready"
            ' 
            ' btnOk
            ' 
            btnOk.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            btnOk.DialogResult = DialogResult.OK
            btnOk.Location = New Point(502, 34)
            btnOk.Name = "btnOk"
            btnOk.Size = New Size(90, 28)
            btnOk.TabIndex = 2
            btnOk.Text = "OK"
            ' 
            ' btnCancel
            ' 
            btnCancel.Anchor = AnchorStyles.Top Or AnchorStyles.Right
            btnCancel.DialogResult = DialogResult.Cancel
            btnCancel.Location = New Point(598, 34)
            btnCancel.Name = "btnCancel"
            btnCancel.Size = New Size(90, 28)
            btnCancel.TabIndex = 3
            btnCancel.Text = "Cancel"
            ' 
            ' FormDownloadManager
            ' 
            AcceptButton = btnOk
            CancelButton = btnCancel
            ClientSize = New Size(700, 520)
            Controls.Add(tabMain)
            Controls.Add(pnlBottom)
            MinimizeBox = False
            MinimumSize = New Size(600, 400)
            Name = "FormDownloadManager"
            StartPosition = FormStartPosition.CenterParent
            Text = "Download Manager"
            tabMain.ResumeLayout(False)
            tabComponents.ResumeLayout(False)
            pnlToolsButtons.ResumeLayout(False)
            tabPiper.ResumeLayout(False)
            pnlVoicesButtons.ResumeLayout(False)
            tabMmsTts.ResumeLayout(False)
            tabBiblicalVocab.ResumeLayout(False)
            tabBibles.ResumeLayout(False)
            pnlBibleSearch.ResumeLayout(False)
            pnlBiblesButtons.ResumeLayout(False)
            tabLangPacks.ResumeLayout(False)
            pnlLangPacksButtons.ResumeLayout(False)
            pnlBottom.ResumeLayout(False)
            ResumeLayout(False)
        End Sub

        Friend WithEvents tabMain As System.Windows.Forms.TabControl
        Friend WithEvents tabComponents As System.Windows.Forms.TabPage
        Friend WithEvents tabPiper As System.Windows.Forms.TabPage
        Friend WithEvents tabMmsTts As System.Windows.Forms.TabPage
        Friend WithEvents tabBibles As System.Windows.Forms.TabPage
        Friend WithEvents lvTools As System.Windows.Forms.ListView
        Friend WithEvents colToolName As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolCategory As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolStatus As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolVersion As System.Windows.Forms.ColumnHeader
        Friend WithEvents pnlToolsButtons As System.Windows.Forms.Panel
        Friend WithEvents btnDownloadAll As System.Windows.Forms.Button
        Friend WithEvents btnRefresh As System.Windows.Forms.Button
        Friend WithEvents lvVoices As System.Windows.Forms.ListView
        Friend WithEvents colVoiceLang As System.Windows.Forms.ColumnHeader
        Friend WithEvents colVoiceModel As System.Windows.Forms.ColumnHeader
        Friend WithEvents colVoiceStatus As System.Windows.Forms.ColumnHeader
        Friend WithEvents pnlVoicesButtons As System.Windows.Forms.Panel
        Friend WithEvents btnDownloadVoices As System.Windows.Forms.Button
        Friend WithEvents btnRemoveVoices As System.Windows.Forms.Button
        Friend WithEvents lblMmsTtsStatus As System.Windows.Forms.Label
        Friend WithEvents lblMmsTtsInfo As System.Windows.Forms.Label
        Friend WithEvents btnInstallMmsTts As System.Windows.Forms.Button
        Friend WithEvents tabBiblicalVocab As System.Windows.Forms.TabPage
        Friend WithEvents lblVocabInfo As System.Windows.Forms.Label
        Friend WithEvents lblVocabStatus As System.Windows.Forms.Label
        Friend WithEvents btnGenerateVocab As System.Windows.Forms.Button
        Friend WithEvents pnlBibleSearch As System.Windows.Forms.Panel
        Friend WithEvents txtBibleSearch As System.Windows.Forms.TextBox
        Friend WithEvents tvBibles As System.Windows.Forms.TreeView
        Friend WithEvents pnlBiblesButtons As System.Windows.Forms.Panel
        Friend WithEvents btnFetchCatalog As System.Windows.Forms.Button
        Friend WithEvents btnDownloadBibles As System.Windows.Forms.Button
        Friend WithEvents btnOpenBiblesFolder As System.Windows.Forms.Button
        Friend WithEvents tabLangPacks As System.Windows.Forms.TabPage
        Friend WithEvents lvLangPacks As System.Windows.Forms.ListView
        Friend WithEvents colLangName As System.Windows.Forms.ColumnHeader
        Friend WithEvents colLangNative As System.Windows.Forms.ColumnHeader
        Friend WithEvents colLangCode As System.Windows.Forms.ColumnHeader
        Friend WithEvents colLangStatus As System.Windows.Forms.ColumnHeader
        Friend WithEvents pnlLangPacksButtons As System.Windows.Forms.Panel
        Friend WithEvents btnDownloadLangPacks As System.Windows.Forms.Button
        Friend WithEvents btnDeleteLangPacks As System.Windows.Forms.Button
        Friend WithEvents pnlBottom As System.Windows.Forms.Panel
        Friend WithEvents pbProgress As System.Windows.Forms.ProgressBar
        Friend WithEvents lblProgress As System.Windows.Forms.Label
        Friend WithEvents btnOk As System.Windows.Forms.Button
        Friend WithEvents btnCancel As System.Windows.Forms.Button

    End Class

End Namespace
