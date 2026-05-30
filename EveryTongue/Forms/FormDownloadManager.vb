Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports EveryTongue.Models
Imports EveryTongue.Services.Bible
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

        Public Sub SelectTab(tabName As String)
            For i = 0 To tabMain.TabCount - 1
                If tabMain.TabPages(i).Name.Equals(tabName, StringComparison.OrdinalIgnoreCase) OrElse
                   tabMain.TabPages(i).Text.Equals(tabName, StringComparison.OrdinalIgnoreCase) Then
                    tabMain.SelectedIndex = i
                    Exit For
                End If
            Next
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
                ' Run all checks in parallel
                Dim statesTask = _mgr.CheckAllToolsAsync()
                Dim pythonTask = _mgr.CheckPythonEmbedAsync()
                Dim pythonDepsTask = _mgr.CheckPythonDepsStateAsync()
                Await Task.WhenAll(statesTask, pythonTask, pythonDepsTask)

                Dim states = statesTask.Result
                Dim pythonState = pythonTask.Result
                Dim pythonDepsState = pythonDepsTask.Result

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
                Dim missingPkgs = _mgr.GetMissingPythonPackages()
                If missingPkgs.Count = 0 Then
                    pkgItem.SubItems.Add("Installed")
                    pkgItem.SubItems.Add("ctranslate2, sentencepiece, fastapi, uvicorn, faster-whisper, sounddevice, edge-tts")
                    pkgItem.ForeColor = Drawing.Color.DarkGreen
                Else
                    pkgItem.SubItems.Add($"Missing ({missingPkgs.Count})")
                    pkgItem.SubItems.Add(String.Join(", ", missingPkgs))
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

            ' Load language packs
            LoadLangPacks()

            ' Load installed Bibles and cached catalog if available
            LoadInstalledBibles()
            LoadCachedCatalog()
            PopulateBibleTree()

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

        ' ── Bible Catalog ──

        Private Shared ReadOnly _httpClient As New HttpClient()
        Private Const CatalogUrl = "https://ebible.org/Scriptures/translations.csv"
        Private Const UsfmUrlTemplate = "https://ebible.org/Scriptures/{0}_usfm.zip"

        ''' <summary>Parsed row from eBible.org translations.csv</summary>
        Private Class CatalogEntry
            Public Property TranslationId As String
            Public Property LanguageCode As String
            Public Property LanguageName As String
            Public Property LanguageNameEnglish As String
            Public Property Title As String
            Public Property Description As String
            Public Property Redistributable As Boolean
            Public Property Copyright As String
            Public Property OTBooks As Integer
            Public Property NTBooks As Integer
        End Class

        Private _catalog As List(Of CatalogEntry)
        Private _installedBibles As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Private Sub LoadCachedCatalog()
            Dim cacheFile = Path.Combine(_biblesDir, "translations.csv")
            If File.Exists(cacheFile) Then
                Try
                    Dim csvText = File.ReadAllText(cacheFile)
                    _catalog = ParseCatalogCsv(csvText)
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[ERROR] LoadCachedCatalog: {ex.Message}")
                End Try
            End If
        End Sub

        Private Sub LoadInstalledBibles()
            _installedBibles.Clear()
            If Not Directory.Exists(_biblesDir) Then Return
            Dim exts = {".db", ".sqlite", ".sqlite3"}
            For Each f In Directory.GetFiles(_biblesDir, "*.*", SearchOption.AllDirectories)
                If exts.Contains(Path.GetExtension(f).ToLower()) Then
                    _installedBibles.Add(Path.GetFileNameWithoutExtension(f))
                End If
            Next
        End Sub

        Private Sub PopulateBibleTree(Optional filter As String = Nothing)
            tvBibles.BeginUpdate()
            tvBibles.Nodes.Clear()

            If _catalog Is Nothing OrElse _catalog.Count = 0 Then
                tvBibles.Nodes.Add("Click 'Fetch Catalog' to load available Bible translations from eBible.org")
                tvBibles.EndUpdate()
                Return
            End If

            Dim hasFilter = Not String.IsNullOrWhiteSpace(filter)
            Dim filterLower = If(hasFilter, filter.ToLower(), "")

            ' Group by language (English name)
            Dim groups = _catalog.
                Where(Function(c)
                          If Not hasFilter Then Return True
                          Return c.LanguageNameEnglish.ToLower().Contains(filterLower) OrElse
                                 c.Title.ToLower().Contains(filterLower) OrElse
                                 c.TranslationId.ToLower().Contains(filterLower) OrElse
                                 c.LanguageCode.ToLower().Contains(filterLower)
                      End Function).
                GroupBy(Function(c) c.LanguageNameEnglish).
                OrderBy(Function(g) g.Key)

            For Each grp In groups
                Dim langNode As New TreeNode($"{grp.Key} ({grp.Count()})")
                langNode.Tag = Nothing
                For Each entry In grp.OrderBy(Function(c) c.Title)
                    Dim installed = _installedBibles.Contains(entry.TranslationId)
                    Dim status = If(installed, " [Installed]", "")
                    Dim books = ""
                    If entry.OTBooks > 0 AndAlso entry.NTBooks > 0 Then
                        books = $" (OT+NT)"
                    ElseIf entry.NTBooks > 0 Then
                        books = $" (NT only)"
                    ElseIf entry.OTBooks > 0 Then
                        books = $" (OT only)"
                    End If
                    Dim nodeText = $"{entry.Title}{books}{status}"
                    Dim childNode As New TreeNode(nodeText)
                    childNode.Tag = entry
                    If installed Then childNode.ForeColor = Drawing.Color.DarkGreen
                    langNode.Nodes.Add(childNode)
                Next
                tvBibles.Nodes.Add(langNode)
            Next

            If hasFilter Then tvBibles.ExpandAll()
            tvBibles.EndUpdate()
        End Sub

        Private Function ParseCatalogCsv(csvText As String) As List(Of CatalogEntry)
            Dim entries As New List(Of CatalogEntry)
            Dim lines = csvText.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If lines.Length < 2 Then Return entries

            ' Parse header to find column indices
            Dim headers = ParseCsvLine(lines(0))
            Dim colMap As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For i = 0 To headers.Length - 1
                colMap(headers(i).Trim()) = i
            Next

            For i = 1 To lines.Length - 1
                Try
                    Dim cols = ParseCsvLine(lines(i))
                    Dim redistributable = GetCol(cols, colMap, "Redistributable")
                    If Not redistributable.Equals("True", StringComparison.OrdinalIgnoreCase) Then Continue For
                    Dim downloadable = GetCol(cols, colMap, "downloadable")
                    If downloadable.Equals("False", StringComparison.OrdinalIgnoreCase) Then Continue For

                    Dim entry As New CatalogEntry With {
                        .TranslationId = GetCol(cols, colMap, "translationId"),
                        .LanguageCode = GetCol(cols, colMap, "languageCode"),
                        .LanguageName = GetCol(cols, colMap, "languageName"),
                        .LanguageNameEnglish = GetCol(cols, colMap, "languageNameInEnglish"),
                        .Title = GetCol(cols, colMap, "title"),
                        .Description = GetCol(cols, colMap, "description"),
                        .Redistributable = True,
                        .Copyright = GetCol(cols, colMap, "Copyright")
                    }
                    Integer.TryParse(GetCol(cols, colMap, "OTbooks"), entry.OTBooks)
                    Integer.TryParse(GetCol(cols, colMap, "NTbooks"), entry.NTBooks)

                    If Not String.IsNullOrEmpty(entry.TranslationId) Then
                        entries.Add(entry)
                    End If
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[ERROR] ParseCatalogCsv (row {i}): {ex.Message}")
                End Try
            Next

            Return entries
        End Function

        Private Shared Function GetCol(cols As String(), colMap As Dictionary(Of String, Integer), name As String) As String
            Dim idx As Integer
            If colMap.TryGetValue(name, idx) AndAlso idx < cols.Length Then Return cols(idx).Trim()
            Return ""
        End Function

        ''' <summary>Simple CSV line parser that handles quoted fields.</summary>
        Private Shared Function ParseCsvLine(line As String) As String()
            Dim fields As New List(Of String)
            Dim current As New Text.StringBuilder()
            Dim inQuotes = False
            For Each ch In line
                If ch = """"c Then
                    inQuotes = Not inQuotes
                ElseIf ch = ","c AndAlso Not inQuotes Then
                    fields.Add(current.ToString())
                    current.Clear()
                Else
                    current.Append(ch)
                End If
            Next
            fields.Add(current.ToString())
            Return fields.ToArray()
        End Function

        Private Async Sub btnFetchCatalog_Click(sender As Object, e As EventArgs) Handles btnFetchCatalog.Click
            If _downloading Then Return
            btnFetchCatalog.Enabled = False
            lblProgress.Text = "Fetching Bible catalog from eBible.org..."
            pbProgress.Style = ProgressBarStyle.Marquee

            Try
                ' Check for cached CSV (less than 24 hours old)
                Dim cacheFile = Path.Combine(_biblesDir, "translations.csv")
                Dim csvText As String

                If File.Exists(cacheFile) AndAlso (DateTime.Now - File.GetLastWriteTime(cacheFile)).TotalHours < 24 Then
                    csvText = File.ReadAllText(cacheFile)
                    lblProgress.Text = "Loaded cached catalog"
                Else
                    csvText = Await _httpClient.GetStringAsync(CatalogUrl)
                    If Not Directory.Exists(_biblesDir) Then Directory.CreateDirectory(_biblesDir)
                    File.WriteAllText(cacheFile, csvText)
                    lblProgress.Text = "Catalog downloaded"
                End If

                _catalog = ParseCatalogCsv(csvText)
                LoadInstalledBibles()
                PopulateBibleTree()
                lblProgress.Text = $"Found {_catalog.Count} freely redistributable translations"

            Catch ex As Exception
                lblProgress.Text = $"Error fetching catalog: {ex.Message}"
            Finally
                pbProgress.Style = ProgressBarStyle.Continuous
                pbProgress.Value = 0
                btnFetchCatalog.Enabled = True
            End Try
        End Sub

        Private Async Sub btnDownloadBibles_Click(sender As Object, e As EventArgs) Handles btnDownloadBibles.Click
            If _downloading Then Return

            ' Collect checked translation nodes
            Dim toDownload As New List(Of CatalogEntry)
            For Each langNode As TreeNode In tvBibles.Nodes
                For Each childNode As TreeNode In langNode.Nodes
                    If childNode.Checked Then
                        Dim entry = TryCast(childNode.Tag, CatalogEntry)
                        If entry IsNot Nothing AndAlso Not _installedBibles.Contains(entry.TranslationId) Then
                            toDownload.Add(entry)
                        End If
                    End If
                Next
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = "Check one or more translations to download."
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)

            Dim allIssues As New List(Of String)()

            Try
                For i = 0 To toDownload.Count - 1
                    Dim entry = toDownload(i)
                    lblProgress.Text = $"Downloading {entry.Title} ({i + 1}/{toDownload.Count})..."
                    pbProgress.Value = 0

                    ' Download USFM zip
                    Dim url = String.Format(UsfmUrlTemplate, entry.TranslationId)
                    Dim zipBytes = Await _httpClient.GetByteArrayAsync(url)

                    ' Extract to temp directory
                    Dim tempDir = Path.Combine(Path.GetTempPath(), "EveryTongue_USFM_" & entry.TranslationId)
                    If Directory.Exists(tempDir) Then Directory.Delete(tempDir, True)
                    Directory.CreateDirectory(tempDir)

                    Dim zipPath = Path.Combine(tempDir, "usfm.zip")
                    File.WriteAllBytes(zipPath, zipBytes)
                    ZipFile.ExtractToDirectory(zipPath, tempDir)
                    File.Delete(zipPath)

                    ' Convert USFM to SQLite3
                    lblProgress.Text = $"Converting {entry.Title} to SQLite3 ({i + 1}/{toDownload.Count})..."
                    pbProgress.Style = ProgressBarStyle.Marquee
                    Application.DoEvents()

                    Dim langDir = Path.Combine(_biblesDir, entry.LanguageCode)
                    If Not Directory.Exists(langDir) Then Directory.CreateDirectory(langDir)
                    Dim dbPath = Path.Combine(langDir, entry.TranslationId & ".sqlite3")

                    Await Task.Run(Sub() UsfmConverter.Convert(tempDir, dbPath, entry.Title, entry.LanguageCode, entry.Copyright))

                    pbProgress.Style = ProgressBarStyle.Continuous

                    If File.Exists(dbPath) Then
                        Dim sizeMb = New FileInfo(dbPath).Length / 1024.0 / 1024.0
                        lblProgress.Text = $"Verifying {entry.Title}..."

                        ' Run integrity check on the converted database
                        Dim issues = Await Task.Run(Function() UsfmConverter.VerifyDatabase(dbPath))
                        If issues.Count > 0 Then
                            allIssues.Add($"{entry.Title}:")
                            For Each issue In issues
                                allIssues.Add($"  - {issue}")
                            Next
                        End If

                        lblProgress.Text = $"Converted {entry.Title} ({sizeMb:F1} MB) ({i + 1}/{toDownload.Count})"
                    Else
                        allIssues.Add($"{entry.Title}: conversion failed — no output file")
                        lblProgress.Text = $"Warning: conversion may have failed for {entry.Title}"
                    End If

                    ' Cleanup temp
                    Try
                        Directory.Delete(tempDir, True)
                    Catch ex As Exception
                        FormMain.WriteDebugLog($"[ERROR] DownloadBibles (cleanup tempDir): {ex.Message}")
                    End Try

                    pbProgress.Value = CInt((i + 1) * 100 \ toDownload.Count)
                Next

                lblProgress.Text = $"Downloaded and converted {toDownload.Count} Bible(s) successfully"
                pbProgress.Value = 100
                PathsUpdated = True

                ' Show integrity report if any issues were found
                If allIssues.Count > 0 Then
                    Dim report = String.Join(vbCrLf, allIssues)
                    MessageBox.Show(
                        $"Downloaded {toDownload.Count} Bible(s). The following issues were found:" &
                        vbCrLf & vbCrLf & report,
                        "Bible Integrity Report",
                        MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If

            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                LoadInstalledBibles()
                PopulateBibleTree(txtBibleSearch.Text)
            End Try
        End Sub

        Private Sub txtBibleSearch_TextChanged(sender As Object, e As EventArgs) Handles txtBibleSearch.TextChanged
            PopulateBibleTree(txtBibleSearch.Text)
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
                    Catch ex As Exception
                        FormMain.WriteDebugLog($"[ERROR] RemoveVoices (delete model files): {ex.Message}")
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
            btnFetchCatalog.Enabled = enabled
            btnDownloadBibles.Enabled = enabled
            btnDownloadLangPacks.Enabled = enabled
            btnDeleteLangPacks.Enabled = enabled
            btnOk.Enabled = enabled
            btnCancel.Enabled = enabled
        End Sub

        ' ── Language Packs ──

        Private Sub LoadLangPacks()
            lvLangPacks.Items.Clear()
            Dim langPack = Services.Infrastructure.LanguagePackService.Instance
            Dim langSvc = Services.Infrastructure.LanguageCodeService.Instance
            Dim all = langSvc.GetAllLanguagesForWeb()
            Dim installed = langPack.GetAvailableLanguages()

            ' Sort installed languages to the top
            Dim sorted = all.Where(Function(l) Not String.IsNullOrEmpty(l.Iso1)).
                OrderByDescending(Function(l) installed.Any(Function(c) c.Equals(l.Iso1, StringComparison.OrdinalIgnoreCase))).
                ThenBy(Function(l) l.Name).ToList()

            For Each l In sorted
                Dim item As New ListViewItem(l.Name)
                item.SubItems.Add(If(l.Native, l.Name))
                item.SubItems.Add(l.Iso1)
                Dim isInstalled = installed.Any(Function(c) c.Equals(l.Iso1, StringComparison.OrdinalIgnoreCase))
                If isInstalled Then
                    item.SubItems.Add("Installed")
                    item.ForeColor = Drawing.Color.DarkGreen
                Else
                    item.SubItems.Add("Not installed")
                End If
                item.Tag = l.Iso1
                lvLangPacks.Items.Add(item)
            Next
        End Sub

        Private Async Sub btnDownloadLangPacks_Click(sender As Object, e As EventArgs) Handles btnDownloadLangPacks.Click
            If _downloading Then Return

            Dim toDownload As New List(Of String)()
            For Each item As ListViewItem In lvLangPacks.CheckedItems
                If item.SubItems(3).Text <> "Installed" Then
                    toDownload.Add(DirectCast(item.Tag, String))
                End If
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = "No language packs selected for download"
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)
            pbProgress.Style = ProgressBarStyle.Continuous
            pbProgress.Value = 0
            pbProgress.Maximum = toDownload.Count

            Try
                Dim langPack = Services.Infrastructure.LanguagePackService.Instance
                Dim downloaded = 0

                For Each code In toDownload
                    lblProgress.Text = $"Downloading {code} language pack..."
                    Dim ok = Await langPack.DownloadLanguageAsync(code)
                    If ok Then
                        downloaded += 1
                    Else
                        lblProgress.Text = $"Failed to download {code} — not available on GitHub"
                    End If
                    pbProgress.Value = Math.Min(pbProgress.Value + 1, pbProgress.Maximum)
                Next

                pbProgress.Value = pbProgress.Maximum
                lblProgress.Text = $"Downloaded {downloaded} of {toDownload.Count} language pack(s)"
            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                LoadLangPacks()
            End Try
        End Sub

        Private Sub btnDeleteLangPacks_Click(sender As Object, e As EventArgs) Handles btnDeleteLangPacks.Click
            Dim toDelete As New List(Of String)()
            For Each item As ListViewItem In lvLangPacks.CheckedItems
                Dim code = DirectCast(item.Tag, String)
                ' Never delete English
                If code.Equals("en", StringComparison.OrdinalIgnoreCase) Then Continue For
                If item.SubItems(3).Text = "Installed" Then
                    toDelete.Add(code)
                End If
            Next

            If toDelete.Count = 0 Then
                lblProgress.Text = "No installed language packs selected for uninstall"
                Return
            End If

            Dim result = MessageBox.Show(
                $"Uninstall {toDelete.Count} language pack(s)?" & vbCrLf & String.Join(", ", toDelete),
                "Uninstall Language Packs", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            Dim langPack = Services.Infrastructure.LanguagePackService.Instance
            Dim deleted = 0
            For Each code In toDelete
                Try
                    Dim path = IO.Path.Combine(langPack.LocalesDirectory, $"{code}.json")
                    If IO.File.Exists(path) Then
                        IO.File.Delete(path)
                        deleted += 1
                    End If
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[LangPack] Failed to delete {code}: {ex.Message}")
                End Try
            Next

            lblProgress.Text = $"Uninstalled {deleted} language pack(s)"
            LoadLangPacks()
        End Sub

        Private Shared Function GetLanguageDisplayName(iso3Code As String) As String
            If String.IsNullOrEmpty(iso3Code) Then Return iso3Code
            Dim langSvc = Services.Infrastructure.LanguageCodeService.Instance
            Dim nllb = langSvc.Iso3ToNllb(iso3Code)
            If Not String.IsNullOrEmpty(nllb) Then
                Dim name = langSvc.GetDisplayName(nllb)
                If Not String.IsNullOrEmpty(name) Then Return name
            End If
            Return iso3Code
        End Function

    End Class

End Namespace
