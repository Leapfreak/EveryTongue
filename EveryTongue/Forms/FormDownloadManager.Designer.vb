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
            Me.grpTools = New System.Windows.Forms.GroupBox()
            Me.lvTools = New System.Windows.Forms.ListView()
            Me.colToolName = New System.Windows.Forms.ColumnHeader()
            Me.colToolCategory = New System.Windows.Forms.ColumnHeader()
            Me.colToolStatus = New System.Windows.Forms.ColumnHeader()
            Me.colToolVersion = New System.Windows.Forms.ColumnHeader()
            Me.btnDownloadAll = New System.Windows.Forms.Button()
            Me.btnRefresh = New System.Windows.Forms.Button()
            Me.grpVoices = New System.Windows.Forms.GroupBox()
            Me.lvVoices = New System.Windows.Forms.ListView()
            Me.colVoiceLang = New System.Windows.Forms.ColumnHeader()
            Me.colVoiceModel = New System.Windows.Forms.ColumnHeader()
            Me.colVoiceStatus = New System.Windows.Forms.ColumnHeader()
            Me.btnDownloadVoices = New System.Windows.Forms.Button()
            Me.btnRemoveVoices = New System.Windows.Forms.Button()
            Me.grpMmsTts = New System.Windows.Forms.GroupBox()
            Me.lblMmsTtsStatus = New System.Windows.Forms.Label()
            Me.btnInstallMmsTts = New System.Windows.Forms.Button()
            Me.grpBibles = New System.Windows.Forms.GroupBox()
            Me.lvBibles = New System.Windows.Forms.ListView()
            Me.colBibleTranslation = New System.Windows.Forms.ColumnHeader()
            Me.colBibleLanguage = New System.Windows.Forms.ColumnHeader()
            Me.colBibleFile = New System.Windows.Forms.ColumnHeader()
            Me.btnOpenBiblesFolder = New System.Windows.Forms.Button()
            Me.lblBibleHint = New System.Windows.Forms.Label()
            Me.pbProgress = New System.Windows.Forms.ProgressBar()
            Me.lblProgress = New System.Windows.Forms.Label()
            Me.btnClose = New System.Windows.Forms.Button()
            Me.grpTools.SuspendLayout()
            Me.grpVoices.SuspendLayout()
            Me.grpMmsTts.SuspendLayout()
            Me.grpBibles.SuspendLayout()
            Me.SuspendLayout()

            ' ── grpTools (y=12, height=235) ──
            Me.grpTools.Text = "Components"
            Me.grpTools.Location = New System.Drawing.Point(12, 12)
            Me.grpTools.Size = New System.Drawing.Size(676, 235)
            Me.grpTools.Controls.Add(Me.lvTools)
            Me.grpTools.Controls.Add(Me.btnDownloadAll)
            Me.grpTools.Controls.Add(Me.btnRefresh)

            ' lvTools
            Me.lvTools.Location = New System.Drawing.Point(10, 20)
            Me.lvTools.Size = New System.Drawing.Size(656, 170)
            Me.lvTools.View = System.Windows.Forms.View.Details
            Me.lvTools.CheckBoxes = True
            Me.lvTools.FullRowSelect = True
            Me.lvTools.GridLines = True
            Me.lvTools.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {
                Me.colToolName, Me.colToolCategory, Me.colToolStatus, Me.colToolVersion})
            Me.colToolName.Text = "Component"
            Me.colToolName.Width = 230
            Me.colToolCategory.Text = "Category"
            Me.colToolCategory.Width = 100
            Me.colToolStatus.Text = "Status"
            Me.colToolStatus.Width = 120
            Me.colToolVersion.Text = "Version"
            Me.colToolVersion.Width = 180

            ' btnDownloadAll
            Me.btnDownloadAll.Text = "Download Selected"
            Me.btnDownloadAll.Location = New System.Drawing.Point(10, 197)
            Me.btnDownloadAll.Size = New System.Drawing.Size(140, 28)

            ' btnRefresh
            Me.btnRefresh.Text = "Refresh"
            Me.btnRefresh.Location = New System.Drawing.Point(180, 197)
            Me.btnRefresh.Size = New System.Drawing.Size(90, 28)

            ' ── grpVoices (y=255, height=185) ──
            Me.grpVoices.Text = "Piper Voice Models (select languages to download)"
            Me.grpVoices.Location = New System.Drawing.Point(12, 255)
            Me.grpVoices.Size = New System.Drawing.Size(676, 185)
            Me.grpVoices.Controls.Add(Me.lvVoices)
            Me.grpVoices.Controls.Add(Me.btnDownloadVoices)
            Me.grpVoices.Controls.Add(Me.btnRemoveVoices)

            ' lvVoices
            Me.lvVoices.Location = New System.Drawing.Point(10, 20)
            Me.lvVoices.Size = New System.Drawing.Size(656, 120)
            Me.lvVoices.View = System.Windows.Forms.View.Details
            Me.lvVoices.CheckBoxes = True
            Me.lvVoices.FullRowSelect = True
            Me.lvVoices.GridLines = True
            Me.lvVoices.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {
                Me.colVoiceLang, Me.colVoiceModel, Me.colVoiceStatus})
            Me.colVoiceLang.Text = "Language"
            Me.colVoiceLang.Width = 200
            Me.colVoiceModel.Text = "Voice Model"
            Me.colVoiceModel.Width = 300
            Me.colVoiceStatus.Text = "Status"
            Me.colVoiceStatus.Width = 130

            ' btnDownloadVoices
            Me.btnDownloadVoices.Text = "Download Selected"
            Me.btnDownloadVoices.Location = New System.Drawing.Point(10, 147)
            Me.btnDownloadVoices.Size = New System.Drawing.Size(140, 28)

            ' btnRemoveVoices
            Me.btnRemoveVoices.Text = "Remove Selected"
            Me.btnRemoveVoices.Location = New System.Drawing.Point(160, 147)
            Me.btnRemoveVoices.Size = New System.Drawing.Size(140, 28)

            ' ── grpMmsTts (y=448, height=55) ──
            Me.grpMmsTts.Text = "MMS-TTS — 1100+ languages via PyTorch (optional)"
            Me.grpMmsTts.Location = New System.Drawing.Point(12, 448)
            Me.grpMmsTts.Size = New System.Drawing.Size(676, 55)
            Me.grpMmsTts.Controls.Add(Me.lblMmsTtsStatus)
            Me.grpMmsTts.Controls.Add(Me.btnInstallMmsTts)

            ' lblMmsTtsStatus
            Me.lblMmsTtsStatus.Text = "Checking..."
            Me.lblMmsTtsStatus.Location = New System.Drawing.Point(10, 24)
            Me.lblMmsTtsStatus.Size = New System.Drawing.Size(440, 18)

            ' btnInstallMmsTts
            Me.btnInstallMmsTts.Text = "Install"
            Me.btnInstallMmsTts.Location = New System.Drawing.Point(550, 19)
            Me.btnInstallMmsTts.Size = New System.Drawing.Size(115, 28)

            ' ── grpBibles (y=510, height=90) ──
            Me.grpBibles.Text = "Bible Translations (installed)"
            Me.grpBibles.Location = New System.Drawing.Point(12, 510)
            Me.grpBibles.Size = New System.Drawing.Size(676, 90)
            Me.grpBibles.Controls.Add(Me.lvBibles)
            Me.grpBibles.Controls.Add(Me.btnOpenBiblesFolder)
            Me.grpBibles.Controls.Add(Me.lblBibleHint)

            ' lvBibles
            Me.lvBibles.Location = New System.Drawing.Point(10, 20)
            Me.lvBibles.Size = New System.Drawing.Size(656, 30)
            Me.lvBibles.View = System.Windows.Forms.View.Details
            Me.lvBibles.FullRowSelect = True
            Me.lvBibles.GridLines = True
            Me.lvBibles.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {
                Me.colBibleTranslation, Me.colBibleLanguage, Me.colBibleFile})
            Me.colBibleTranslation.Text = "Translation"
            Me.colBibleTranslation.Width = 180
            Me.colBibleLanguage.Text = "Language"
            Me.colBibleLanguage.Width = 120
            Me.colBibleFile.Text = "File"
            Me.colBibleFile.Width = 330

            ' btnOpenBiblesFolder
            Me.btnOpenBiblesFolder.Text = "Open Bibles Folder"
            Me.btnOpenBiblesFolder.Location = New System.Drawing.Point(10, 56)
            Me.btnOpenBiblesFolder.Size = New System.Drawing.Size(150, 28)

            ' lblBibleHint
            Me.lblBibleHint.Text = "Add .SQLite3 Bible files to language subfolders (e.g. Bibles\en\KJV+.SQLite3)"
            Me.lblBibleHint.Location = New System.Drawing.Point(170, 61)
            Me.lblBibleHint.Size = New System.Drawing.Size(490, 18)
            Me.lblBibleHint.ForeColor = System.Drawing.SystemColors.GrayText

            ' ── pbProgress (y=608) ──
            Me.pbProgress.Location = New System.Drawing.Point(12, 608)
            Me.pbProgress.Size = New System.Drawing.Size(676, 20)

            ' ── lblProgress (y=634) ──
            Me.lblProgress.Text = "Ready"
            Me.lblProgress.Location = New System.Drawing.Point(12, 634)
            Me.lblProgress.Size = New System.Drawing.Size(560, 18)

            ' ── btnClose (y=630) ──
            Me.btnClose.Text = "Close"
            Me.btnClose.Location = New System.Drawing.Point(598, 630)
            Me.btnClose.Size = New System.Drawing.Size(90, 28)
            Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK

            ' ── Form ──
            Me.Text = "Download Manager"
            Me.ClientSize = New System.Drawing.Size(700, 720)
            Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
            Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.AcceptButton = Me.btnClose
            Me.Controls.Add(Me.grpTools)
            Me.Controls.Add(Me.grpVoices)
            Me.Controls.Add(Me.grpMmsTts)
            Me.Controls.Add(Me.grpBibles)
            Me.Controls.Add(Me.pbProgress)
            Me.Controls.Add(Me.lblProgress)
            Me.Controls.Add(Me.btnClose)

            Me.grpTools.ResumeLayout(False)
            Me.grpVoices.ResumeLayout(False)
            Me.grpMmsTts.ResumeLayout(False)
            Me.grpBibles.ResumeLayout(False)
            Me.ResumeLayout(False)
        End Sub

        Friend WithEvents grpTools As System.Windows.Forms.GroupBox
        Friend WithEvents lvTools As System.Windows.Forms.ListView
        Friend WithEvents colToolName As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolCategory As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolStatus As System.Windows.Forms.ColumnHeader
        Friend WithEvents colToolVersion As System.Windows.Forms.ColumnHeader
        Friend WithEvents btnDownloadAll As System.Windows.Forms.Button
        Friend WithEvents btnRefresh As System.Windows.Forms.Button
        Friend WithEvents grpVoices As System.Windows.Forms.GroupBox
        Friend WithEvents lvVoices As System.Windows.Forms.ListView
        Friend WithEvents colVoiceLang As System.Windows.Forms.ColumnHeader
        Friend WithEvents colVoiceModel As System.Windows.Forms.ColumnHeader
        Friend WithEvents colVoiceStatus As System.Windows.Forms.ColumnHeader
        Friend WithEvents btnDownloadVoices As System.Windows.Forms.Button
        Friend WithEvents btnRemoveVoices As System.Windows.Forms.Button
        Friend WithEvents grpMmsTts As System.Windows.Forms.GroupBox
        Friend WithEvents lblMmsTtsStatus As System.Windows.Forms.Label
        Friend WithEvents btnInstallMmsTts As System.Windows.Forms.Button
        Friend WithEvents grpBibles As System.Windows.Forms.GroupBox
        Friend WithEvents lvBibles As System.Windows.Forms.ListView
        Friend WithEvents colBibleTranslation As System.Windows.Forms.ColumnHeader
        Friend WithEvents colBibleLanguage As System.Windows.Forms.ColumnHeader
        Friend WithEvents colBibleFile As System.Windows.Forms.ColumnHeader
        Friend WithEvents btnOpenBiblesFolder As System.Windows.Forms.Button
        Friend WithEvents lblBibleHint As System.Windows.Forms.Label
        Friend WithEvents pbProgress As System.Windows.Forms.ProgressBar
        Friend WithEvents lblProgress As System.Windows.Forms.Label
        Friend WithEvents btnClose As System.Windows.Forms.Button

    End Class

End Namespace
