Imports System.IO
Imports EveryTongue.Models
Imports EveryTongue.Services.Tts

Namespace Forms

    Public Class FormDownloadManager
        Inherits Form

        Private ReadOnly _mgr As DependencyManager
        Private ReadOnly _biblesDir As String
        Private _downloading As Boolean = False

        ' Controls — Piper engine
        Private WithEvents grpPiper As GroupBox
        Private lblPiperStatus As Label
        Private WithEvents btnDownloadPiper As Button

        ' Controls — Voice models
        Private grpVoices As GroupBox
        Private lvVoices As ListView
        Private WithEvents btnDownloadVoices As Button
        Private WithEvents btnRemoveVoices As Button

        ' Controls — MMS-TTS
        Private grpMmsTts As GroupBox
        Private lblMmsTtsStatus As Label
        Private WithEvents btnInstallMmsTts As Button

        ' Controls — Bibles
        Private grpBibles As GroupBox
        Private lvBibles As ListView
        Private WithEvents btnOpenBiblesFolder As Button

        ' Controls — Progress
        Private pbProgress As ProgressBar
        Private lblProgress As Label
        Private WithEvents btnClose As Button

        Public Sub New(config As AppConfig, biblesDir As String)
            _biblesDir = If(String.IsNullOrEmpty(biblesDir),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles"), biblesDir)
            Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
            _mgr = New DependencyManager(config, toolsDir)
            InitializeForm()
            LoadState()
        End Sub

        Private Sub InitializeForm()
            Me.Text = "Download Manager"
            Me.ClientSize = New Drawing.Size(630, 620)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False

            Dim y = 12

            ' ── Piper TTS Engine ──
            grpPiper = New GroupBox() With {
                .Text = "Piper TTS Engine (offline speech synthesis — Tier 1)",
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(606, 55)
            }

            lblPiperStatus = New Label() With {
                .Text = "Checking...",
                .Location = New Drawing.Point(10, 24),
                .Size = New Drawing.Size(380, 18)
            }

            btnDownloadPiper = New Button() With {
                .Text = "Download",
                .Location = New Drawing.Point(480, 19),
                .Size = New Drawing.Size(115, 28)
            }

            grpPiper.Controls.AddRange({lblPiperStatus, btnDownloadPiper})
            y += 62

            ' ── TTS Voice Models ──
            grpVoices = New GroupBox() With {
                .Text = "Piper Voice Models (select languages to download)",
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(606, 220)
            }

            lvVoices = New ListView() With {
                .Location = New Drawing.Point(10, 20),
                .Size = New Drawing.Size(586, 155),
                .View = View.Details,
                .CheckBoxes = True,
                .FullRowSelect = True,
                .GridLines = True
            }
            lvVoices.Columns.Add("Language", 160)
            lvVoices.Columns.Add("Voice Model", 270)
            lvVoices.Columns.Add("Status", 130)

            btnDownloadVoices = New Button() With {
                .Text = "Download Selected",
                .Location = New Drawing.Point(10, 182),
                .Size = New Drawing.Size(140, 28)
            }

            btnRemoveVoices = New Button() With {
                .Text = "Remove Selected",
                .Location = New Drawing.Point(160, 182),
                .Size = New Drawing.Size(140, 28)
            }

            grpVoices.Controls.AddRange({lvVoices, btnDownloadVoices, btnRemoveVoices})
            y += 228

            ' ── MMS-TTS (optional tier 2) ──
            grpMmsTts = New GroupBox() With {
                .Text = "MMS-TTS — 1100+ languages via PyTorch (optional — Tier 2)",
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(606, 55)
            }

            lblMmsTtsStatus = New Label() With {
                .Text = "Checking...",
                .Location = New Drawing.Point(10, 24),
                .Size = New Drawing.Size(380, 18)
            }

            btnInstallMmsTts = New Button() With {
                .Text = "Install",
                .Location = New Drawing.Point(480, 19),
                .Size = New Drawing.Size(115, 28)
            }

            grpMmsTts.Controls.AddRange({lblMmsTtsStatus, btnInstallMmsTts})
            y += 62

            ' ── Bible Translations ──
            grpBibles = New GroupBox() With {
                .Text = "Bible Translations (installed)",
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(606, 120)
            }

            lvBibles = New ListView() With {
                .Location = New Drawing.Point(10, 20),
                .Size = New Drawing.Size(586, 55),
                .View = View.Details,
                .FullRowSelect = True,
                .GridLines = True
            }
            lvBibles.Columns.Add("Translation", 160)
            lvBibles.Columns.Add("Language", 100)
            lvBibles.Columns.Add("File", 300)

            btnOpenBiblesFolder = New Button() With {
                .Text = "Open Bibles Folder",
                .Location = New Drawing.Point(10, 82),
                .Size = New Drawing.Size(150, 28)
            }

            Dim lblBibleHint As New Label() With {
                .Text = "Add .SQLite3 Bible files to language subfolders (e.g. Bibles\en\KJV+.SQLite3)",
                .Location = New Drawing.Point(170, 87),
                .Size = New Drawing.Size(420, 18),
                .ForeColor = Drawing.SystemColors.GrayText
            }

            grpBibles.Controls.AddRange({lvBibles, btnOpenBiblesFolder, lblBibleHint})
            y += 128

            ' ── Progress ──
            pbProgress = New ProgressBar() With {
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(606, 20)
            }
            y += 26

            lblProgress = New Label() With {
                .Text = "Ready",
                .Location = New Drawing.Point(12, y),
                .Size = New Drawing.Size(490, 18)
            }

            btnClose = New Button() With {
                .Text = "Close",
                .Location = New Drawing.Point(528, y - 4),
                .Size = New Drawing.Size(90, 28),
                .DialogResult = DialogResult.OK
            }

            Me.Controls.AddRange({grpPiper, grpVoices, grpMmsTts, grpBibles,
                pbProgress, lblProgress, btnClose})
            Me.AcceptButton = btnClose
        End Sub

        Private Sub LoadState()
            ' ── Piper engine status ──
            Dim piperState = _mgr.CheckPiperAsync().Result
            If piperState.Status = ToolStatus.UpToDate Then
                lblPiperStatus.Text = $"Installed (v{piperState.InstalledVersion})"
                btnDownloadPiper.Text = "Installed"
                btnDownloadPiper.Enabled = False
            Else
                lblPiperStatus.Text = "Not installed — required for offline TTS"
                btnDownloadPiper.Text = "Download (~15 MB)"
                btnDownloadPiper.Enabled = True
            End If

            ' ── Voice models ──
            Dim installedVoices = _mgr.GetInstalledPiperVoices()
            lvVoices.Items.Clear()
            For Each kvp In PiperBackend.VoiceMap.OrderBy(Function(k) GetLanguageDisplayName(k.Key))
                Dim modelFile = PiperBackend.GetModelFileName(kvp.Key)
                Dim installed = installedVoices.Contains(kvp.Key)

                Dim item As New ListViewItem(GetLanguageDisplayName(kvp.Key))
                item.SubItems.Add(If(modelFile, ""))
                item.SubItems.Add(If(installed, "Installed", "Not installed"))
                item.Tag = kvp.Key
                If installed Then
                    item.ForeColor = Drawing.Color.DarkGreen
                End If
                lvVoices.Items.Add(item)
            Next

            ' ── MMS-TTS status ──
            Dim mmsTtsInstalled = MmsTtsBackend.CheckDepsInstalled()
            If mmsTtsInstalled Then
                lblMmsTtsStatus.Text = "Installed — covers languages not in Piper"
                btnInstallMmsTts.Text = "Installed"
                btnInstallMmsTts.Enabled = False
            Else
                lblMmsTtsStatus.Text = "Not installed — CPU-only PyTorch (~200 MB)"
                btnInstallMmsTts.Text = "Install (~200 MB)"
                btnInstallMmsTts.Enabled = True
            End If

            ' ── Bibles ──
            LoadBibles()
        End Sub

        Private Sub LoadBibles()
            lvBibles.Items.Clear()
            If Not Directory.Exists(_biblesDir) Then Return

            Dim dbExtensions = {".db", ".sqlite", ".sqlite3"}
            For Each langDir As String In Directory.GetDirectories(_biblesDir)
                Dim langCode = Path.GetFileName(langDir)
                For Each dbFile As String In Directory.GetFiles(langDir)
                    If dbExtensions.Contains(Path.GetExtension(dbFile).ToLower()) Then
                        Dim item As New ListViewItem(Path.GetFileNameWithoutExtension(dbFile))
                        item.SubItems.Add(langCode)
                        item.SubItems.Add(Path.GetFileName(dbFile))
                        lvBibles.Items.Add(item)
                    End If
                Next
            Next
        End Sub

        ' ── Event handlers ──

        Private Async Sub btnDownloadPiper_Click(sender As Object, e As EventArgs) Handles btnDownloadPiper.Click
            If _downloading Then Return
            _downloading = True
            btnDownloadPiper.Enabled = False
            SetButtonsEnabled(False)

            Try
                lblProgress.Text = "Downloading Piper TTS engine..."
                pbProgress.Value = 0

                Dim progress As New Progress(Of (downloaded As Long, total As Long))(
                    Sub(p)
                        If p.total > 0 Then
                            pbProgress.Value = CInt(Math.Min(p.downloaded * 100 \ p.total, 100))
                            lblProgress.Text = $"Downloading Piper... {(p.downloaded / 1048576.0):F1} / {(p.total / 1048576.0):F1} MB"
                        End If
                    End Sub)

                Await _mgr.DownloadPiperAsync(progress)

                lblPiperStatus.Text = "Installed (v2023.11.14-2)"
                btnDownloadPiper.Text = "Installed"
                lblProgress.Text = "Piper TTS engine installed successfully"
                pbProgress.Value = 100
            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
                btnDownloadPiper.Enabled = True
            Finally
                _downloading = False
                SetButtonsEnabled(True)
            End Try
        End Sub

        Private Async Sub btnDownloadVoices_Click(sender As Object, e As EventArgs) Handles btnDownloadVoices.Click
            If _downloading Then Return

            ' Collect uninstalled checked items
            Dim toDownload As New List(Of String)()
            For Each item As ListViewItem In lvVoices.CheckedItems
                If item.SubItems(2).Text <> "Installed" Then
                    toDownload.Add(DirectCast(item.Tag, String))
                End If
            Next

            If toDownload.Count = 0 Then
                MessageBox.Show("Select one or more uninstalled voices to download.",
                    "Download Voices", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Check engine first
            Dim piperState = Await _mgr.CheckPiperAsync()
            If piperState.Status <> ToolStatus.UpToDate Then
                Dim result = MessageBox.Show(
                    "The Piper TTS engine is not installed. Download it first?",
                    "Download Voices", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.Yes Then
                    btnDownloadPiper_Click(Nothing, Nothing)
                End If
                Return
            End If

            _downloading = True
            SetButtonsEnabled(False)

            Try
                For i = 0 To toDownload.Count - 1
                    Dim lang = toDownload(i)
                    Dim displayName = GetLanguageDisplayName(lang)
                    lblProgress.Text = $"Downloading voice: {displayName} ({i + 1}/{toDownload.Count})..."
                    pbProgress.Value = 0

                    Dim progress As New Progress(Of (downloaded As Long, total As Long))(
                        Sub(p)
                            If p.total > 0 Then
                                pbProgress.Value = CInt(Math.Min(p.downloaded * 100 \ p.total, 100))
                            End If
                        End Sub)

                    Await _mgr.DownloadPiperVoiceAsync(lang, progress)
                Next

                lblProgress.Text = $"Downloaded {toDownload.Count} voice(s) successfully"
                pbProgress.Value = 100
                LoadState()
            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetButtonsEnabled(True)
            End Try
        End Sub

        Private Sub btnRemoveVoices_Click(sender As Object, e As EventArgs) Handles btnRemoveVoices.Click
            Dim toRemove As New List(Of (lang As String, display As String))()
            For Each item As ListViewItem In lvVoices.CheckedItems
                If item.SubItems(2).Text = "Installed" Then
                    toRemove.Add((DirectCast(item.Tag, String), item.Text))
                End If
            Next

            If toRemove.Count = 0 Then
                MessageBox.Show("Select one or more installed voices to remove.",
                    "Remove Voices", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim names = String.Join(", ", toRemove.Select(Function(r) r.display))
            Dim result = MessageBox.Show(
                $"Remove {toRemove.Count} voice model(s)?" & vbCrLf & names,
                "Remove Voices", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            Dim voicesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "tts-models", "piper", "voices")
            Dim removed = 0
            For Each entry In toRemove
                Dim modelFile = PiperBackend.GetModelFileName(entry.lang)
                If modelFile IsNot Nothing Then
                    Dim onnxPath = Path.Combine(voicesDir, modelFile)
                    Dim jsonPath = onnxPath & ".json"
                    Try
                        If File.Exists(onnxPath) Then File.Delete(onnxPath)
                        If File.Exists(jsonPath) Then File.Delete(jsonPath)
                        removed += 1
                    Catch
                    End Try
                End If
            Next

            lblProgress.Text = $"Removed {removed} voice(s)"
            LoadState()
        End Sub

        Private Async Sub btnInstallMmsTts_Click(sender As Object, e As EventArgs) Handles btnInstallMmsTts.Click
            If _downloading Then Return

            Dim result = MessageBox.Show(
                "Install MMS-TTS (Meta's offline speech synthesis)?" & vbCrLf & vbCrLf &
                "This will download CPU-only PyTorch (~200 MB) and the transformers library." & vbCrLf &
                "MMS-TTS covers 1100+ languages for when Piper doesn't have a voice." & vbCrLf & vbCrLf &
                "Requires Python Embedded to be installed (from tool updates).",
                "Install MMS-TTS", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            _downloading = True
            SetButtonsEnabled(False)
            btnInstallMmsTts.Enabled = False

            Try
                lblProgress.Text = "Installing MMS-TTS dependencies (this may take several minutes)..."
                pbProgress.Value = 0
                pbProgress.Style = ProgressBarStyle.Marquee

                Await Task.Run(Function() _mgr.InstallMmsTtsDepsAsync(Nothing))

                pbProgress.Style = ProgressBarStyle.Continuous
                pbProgress.Value = 100

                If MmsTtsBackend.CheckDepsInstalled() Then
                    lblMmsTtsStatus.Text = "Installed — covers languages not in Piper"
                    btnInstallMmsTts.Text = "Installed"
                    lblProgress.Text = "MMS-TTS installed successfully. Restart app to activate."
                Else
                    lblProgress.Text = "Installation may have failed — check Python is installed"
                    btnInstallMmsTts.Enabled = True
                End If
            Catch ex As Exception
                pbProgress.Style = ProgressBarStyle.Continuous
                lblProgress.Text = $"Error: {ex.Message}"
                btnInstallMmsTts.Enabled = True
            Finally
                _downloading = False
                SetButtonsEnabled(True)
            End Try
        End Sub

        Private Sub btnOpenBiblesFolder_Click(sender As Object, e As EventArgs) Handles btnOpenBiblesFolder.Click
            If Not Directory.Exists(_biblesDir) Then
                Directory.CreateDirectory(_biblesDir)
            End If
            Process.Start(New ProcessStartInfo() With {
                .FileName = "explorer.exe",
                .Arguments = $"""{_biblesDir}""",
                .UseShellExecute = True
            })
        End Sub

        Private Sub SetButtonsEnabled(enabled As Boolean)
            btnDownloadVoices.Enabled = enabled
            btnRemoveVoices.Enabled = enabled
            btnClose.Enabled = enabled
        End Sub

        Private Shared Function GetLanguageDisplayName(nllbCode As String) As String
            Select Case nllbCode
                Case "eng" : Return "English"
                Case "spa" : Return "Spanish"
                Case "fra" : Return "French"
                Case "deu" : Return "German"
                Case "cat" : Return "Catalan"
                Case "por" : Return "Portuguese"
                Case "ita" : Return "Italian"
                Case "zho" : Return "Chinese"
                Case "nld" : Return "Dutch"
                Case "pol" : Return "Polish"
                Case "rus" : Return "Russian"
                Case "ukr" : Return "Ukrainian"
                Case "ces" : Return "Czech"
                Case "dan" : Return "Danish"
                Case "fin" : Return "Finnish"
                Case "ell" : Return "Greek"
                Case "hun" : Return "Hungarian"
                Case "isl" : Return "Icelandic"
                Case "nor" : Return "Norwegian"
                Case "ron" : Return "Romanian"
                Case "slk" : Return "Slovak"
                Case "slv" : Return "Slovenian"
                Case "srp" : Return "Serbian"
                Case "swe" : Return "Swedish"
                Case "swh" : Return "Swahili"
                Case "tur" : Return "Turkish"
                Case "vie" : Return "Vietnamese"
                Case "kat" : Return "Georgian"
                Case "kaz" : Return "Kazakh"
                Case "nep" : Return "Nepali"
                Case Else : Return nllbCode
            End Select
        End Function
    End Class

End Namespace
