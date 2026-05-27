Imports System.IO
Imports EveryTongue.Models
Imports EveryTongue.Services.Tts

Namespace Forms

    Partial Public Class FormDownloadManager

        Private ReadOnly _mgr As DependencyManager
        Private ReadOnly _biblesDir As String
        Private ReadOnly _config As AppConfig
        Private _downloading As Boolean = False

        Public Property PathsUpdated As Boolean = False

        Public Sub New(config As AppConfig, biblesDir As String)
            _config = config
            _biblesDir = If(String.IsNullOrEmpty(biblesDir),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles"), biblesDir)
            Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
            _mgr = New DependencyManager(config, toolsDir)
            InitializeComponent()
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            LoadStateAsync()
        End Sub

        Private Async Sub LoadStateAsync()
            lblProgress.Text = "Checking components..."
            btnDownloadAll.Enabled = False
            btnRefresh.Enabled = False

            Try
                ' Check all core tools asynchronously
                Dim states = Await _mgr.CheckAllToolsAsync()

                ' Also check Python and its deps
                Dim pythonState = Await _mgr.CheckPythonEmbedAsync()
                Dim pythonDepsState = Await _mgr.CheckPythonDepsStateAsync()

                lvTools.Items.Clear()

                ' Add core tools
                For Each st In states
                    Dim cat = GetCategory(st.Name)
                    Dim item As New ListViewItem(st.Name)
                    item.SubItems.Add(cat)
                    Select Case st.Status
                        Case ToolStatus.UpToDate
                            item.SubItems.Add("Installed")
                            item.SubItems.Add(If(st.InstalledVersion, ""))
                            item.ForeColor = Drawing.Color.DarkGreen
                        Case ToolStatus.Installed
                            item.SubItems.Add("Installed")
                            item.SubItems.Add(If(st.InstalledVersion, ""))
                            item.ForeColor = Drawing.Color.DarkGreen
                        Case ToolStatus.UpdateAvailable
                            item.SubItems.Add("Update Available")
                            item.SubItems.Add($"{st.InstalledVersion} -> {st.LatestVersion}")
                            item.ForeColor = Drawing.Color.DarkOrange
                        Case ToolStatus.Missing
                            item.SubItems.Add("Not Installed")
                            item.SubItems.Add("")
                            item.ForeColor = Drawing.Color.Red
                        Case ToolStatus.CheckFailed
                            item.SubItems.Add("Check Failed")
                            item.SubItems.Add("")
                            item.ForeColor = Drawing.Color.Gray
                    End Select
                    item.Tag = st
                    lvTools.Items.Add(item)
                Next

                ' Add Python Embedded
                Dim pyItem As New ListViewItem("Python Embedded")
                pyItem.SubItems.Add("Runtime")
                If pythonState.Status = ToolStatus.UpToDate Then
                    pyItem.SubItems.Add("Installed")
                    pyItem.SubItems.Add("3.12")
                    pyItem.ForeColor = Drawing.Color.DarkGreen
                Else
                    pyItem.SubItems.Add("Not Installed")
                    pyItem.SubItems.Add("")
                    pyItem.ForeColor = Drawing.Color.Red
                End If
                pyItem.Tag = pythonState
                lvTools.Items.Add(pyItem)

                ' Add Python Packages
                Dim pkgItem As New ListViewItem("Python Packages")
                pkgItem.SubItems.Add("Runtime")
                If pythonDepsState.Status = ToolStatus.UpToDate Then
                    pkgItem.SubItems.Add("Installed")
                    pkgItem.SubItems.Add("")
                    pkgItem.ForeColor = Drawing.Color.DarkGreen
                Else
                    pkgItem.SubItems.Add("Not Installed")
                    pkgItem.SubItems.Add("")
                    pkgItem.ForeColor = Drawing.Color.Red
                End If
                pkgItem.Tag = pythonDepsState
                lvTools.Items.Add(pkgItem)

                ' Pre-check missing items and enable button if any are actionable
                Dim hasActionable = False
                For Each item As ListViewItem In lvTools.Items
                    Dim s = TryCast(item.Tag, ToolState)
                    If s IsNot Nothing AndAlso (s.Status = ToolStatus.Missing OrElse s.Status = ToolStatus.UpdateAvailable) Then
                        item.Checked = True
                        hasActionable = True
                    End If
                Next
                btnDownloadAll.Enabled = hasActionable

            Catch ex As Exception
                lblProgress.Text = $"Error checking: {ex.Message}"
            End Try

            btnRefresh.Enabled = True

            ' Load TTS voices
            LoadVoices()

            ' Load MMS-TTS status
            LoadMmsTtsStatus()

            ' Load Bibles
            LoadBibles()

            If lblProgress.Text = "Checking components..." Then
                lblProgress.Text = "Ready"
            End If
        End Sub

        Private Sub LoadVoices()
            Dim installedVoices = _mgr.GetInstalledPiperVoices()
            lvVoices.Items.Clear()
            For Each kvp In PiperBackend.VoiceMap.OrderBy(Function(k) GetLanguageDisplayName(k.Key))
                Dim modelFile = PiperBackend.GetModelFileName(kvp.Key)
                Dim installed = installedVoices.Contains(kvp.Key)
                Dim item As New ListViewItem(GetLanguageDisplayName(kvp.Key))
                item.SubItems.Add(If(modelFile, ""))
                item.SubItems.Add(If(installed, "Installed", "Not installed"))
                item.Tag = kvp.Key
                If installed Then item.ForeColor = Drawing.Color.DarkGreen
                lvVoices.Items.Add(item)
            Next
        End Sub

        Private Sub LoadMmsTtsStatus()
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

        Private Shared Function GetCategory(name As String) As String
            Select Case name
                Case "yt-dlp", "FFmpeg", "Subtitle Edit"
                    Return "Tool"
                Case "Whisper Model (ggml-large-v3)", "faster-whisper Model (large-v3)"
                    Return "AI Model"
                Case "NLLB Translation Model"
                    Return "AI Model"
                Case "Piper TTS"
                    Return "TTS"
                Case Else
                    Return "Other"
            End Select
        End Function

        ' ── Download All Missing ──

        Private Async Sub btnDownloadAll_Click(sender As Object, e As EventArgs) Handles btnDownloadAll.Click
            If _downloading Then Return

            ' Collect checked items that need downloading
            Dim toDownload As New List(Of ToolState)()
            Dim needPython = False
            Dim needPythonDeps = False

            For Each item As ListViewItem In lvTools.CheckedItems
                Dim st = TryCast(item.Tag, ToolState)
                If st Is Nothing Then Continue For
                If st.Status = ToolStatus.UpToDate OrElse st.Status = ToolStatus.Installed Then Continue For

                If st.Name = "Python Embedded" Then
                    needPython = True
                ElseIf st.Name = "Python Packages" Then
                    needPythonDeps = True
                Else
                    toDownload.Add(st)
                End If
            Next

            If toDownload.Count = 0 AndAlso Not needPython AndAlso Not needPythonDeps Then
                lblProgress.Text = "Select one or more components to download."
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)

            Try
                Dim total = toDownload.Count + If(needPython, 1, 0) + If(needPythonDeps, 1, 0)
                Dim current = 0

                ' Download tools/models
                For Each tool In toDownload
                    current += 1
                    lblProgress.Text = $"Downloading {tool.Name} ({current}/{total})..."
                    pbProgress.Value = 0

                    Dim progress As New Progress(Of (downloaded As Long, total As Long))(
                        Sub(p)
                            If p.total > 0 Then
                                pbProgress.Value = CInt(Math.Min(p.downloaded * 100 \ p.total, 100))
                            End If
                        End Sub)

                    Await _mgr.DownloadToolAsync(tool, progress)
                Next

                ' Python
                If needPython Then
                    current += 1
                    lblProgress.Text = $"Installing Python Embedded ({current}/{total})..."
                    pbProgress.Value = 0
                    pbProgress.Style = ProgressBarStyle.Marquee
                    Await _mgr.DownloadPythonEmbedAsync(Nothing)
                    pbProgress.Style = ProgressBarStyle.Continuous
                End If

                If needPythonDeps OrElse needPython Then
                    current += 1
                    lblProgress.Text = $"Installing Python packages ({current}/{total})..."
                    pbProgress.Style = ProgressBarStyle.Marquee
                    Await _mgr.InstallPythonDepsAsync(Nothing)
                    pbProgress.Style = ProgressBarStyle.Continuous
                End If

                pbProgress.Value = 100
                lblProgress.Text = $"Downloaded {total} component(s) successfully"
                PathsUpdated = True

            Catch ex As Exception
                pbProgress.Style = ProgressBarStyle.Continuous
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                LoadStateAsync()
            End Try
        End Sub

        Private Sub btnRefresh_Click(sender As Object, e As EventArgs) Handles btnRefresh.Click
            LoadStateAsync()
        End Sub

        ' ── Voice Downloads ──

        Private Async Sub btnDownloadVoices_Click(sender As Object, e As EventArgs) Handles btnDownloadVoices.Click
            If _downloading Then Return

            Dim toDownload As New List(Of String)()
            For Each item As ListViewItem In lvVoices.CheckedItems
                If item.SubItems(2).Text <> "Installed" Then
                    toDownload.Add(DirectCast(item.Tag, String))
                End If
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = "Select one or more uninstalled voices to download."
                Return
            End If

            ' Check Piper engine first
            Dim piperState = Await _mgr.CheckPiperAsync()
            If piperState.Status <> ToolStatus.UpToDate Then
                lblProgress.Text = "Piper TTS engine must be installed first. Use 'Download All Missing'."
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)

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
            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                LoadVoices()
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
                lblProgress.Text = "Select one or more installed voices to remove."
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
            LoadVoices()
        End Sub

        ' ── MMS-TTS Install ──

        Private Async Sub btnInstallMmsTts_Click(sender As Object, e As EventArgs) Handles btnInstallMmsTts.Click
            If _downloading Then Return

            Dim result = MessageBox.Show(
                "Install MMS-TTS (Meta's offline speech synthesis)?" & vbCrLf & vbCrLf &
                "This will download CPU-only PyTorch (~200 MB) and the transformers library." & vbCrLf &
                "MMS-TTS covers 1100+ languages for when Piper doesn't have a voice.",
                "Install MMS-TTS", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            _downloading = True
            SetAllButtonsEnabled(False)
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
                    lblProgress.Text = "MMS-TTS installed successfully"
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
                SetAllButtonsEnabled(True)
            End Try
        End Sub

        ' ── Bibles ──

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

        ' ── Helpers ──

        Private Sub SetAllButtonsEnabled(enabled As Boolean)
            btnDownloadAll.Enabled = enabled
            btnRefresh.Enabled = enabled
            btnDownloadVoices.Enabled = enabled
            btnRemoveVoices.Enabled = enabled
            btnInstallMmsTts.Enabled = enabled
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
