Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports EveryTongue.Models
Imports EveryTongue.Services.Bible
Imports EveryTongue.Services.Infrastructure
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
            ApplyLocale()
        End Sub

        Private Sub ApplyLocale()
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Me.Text = lp.GetString("DM_Title")
            tabComponents.Text = lp.GetString("DM_TabComponents")
            colToolName.Text = lp.GetString("DM_ColComponent")
            colToolCategory.Text = lp.GetString("DM_ColCategory")
            colToolStatus.Text = lp.GetString("DM_ColStatus")
            colToolVersion.Text = lp.GetString("DM_ColVersion")
            btnDownloadAll.Text = lp.GetString("DM_DownloadSelected")
            btnRefresh.Text = lp.GetString("DM_Refresh")
            tabPiper.Text = lp.GetString("DM_TabPiperVoices")
            colVoiceLang.Text = lp.GetString("DM_ColLanguage")
            colVoiceModel.Text = lp.GetString("DM_ColVoiceModel")
            colVoiceStatus.Text = lp.GetString("DM_ColStatus")
            btnDownloadVoices.Text = lp.GetString("DM_DownloadSelected")
            btnRemoveVoices.Text = lp.GetString("DM_RemoveSelected")
            tabMmsTts.Text = lp.GetString("DM_TabMmsTts")
            lblMmsTtsInfo.Text = lp.GetString("DM_MmsTtsInfo")
            btnInstallMmsTts.Text = lp.GetString("DM_Install")
            tabBiblicalVocab.Text = lp.GetString("DM_TabBiblicalVocab")
            lblVocabInfo.Text = lp.GetString("DM_VocabInfo")
            btnGenerateVocab.Text = lp.GetString("DM_VocabGenerate")
            tabBibles.Text = lp.GetString("DM_TabBibles")
            btnFetchCatalog.Text = lp.GetString("DM_FetchCatalog")
            btnDownloadBibles.Text = lp.GetString("DM_DownloadSelected")
            btnOpenBiblesFolder.Text = lp.GetString("DM_OpenFolder")
            tabLangPacks.Text = lp.GetString("DM_TabLangPacks")
            colLangName.Text = lp.GetString("DM_ColLanguage")
            colLangNative.Text = lp.GetString("DM_ColNativeName")
            colLangCode.Text = lp.GetString("DM_ColCode")
            colLangStatus.Text = lp.GetString("DM_ColStatus")
            btnDownloadLangPacks.Text = lp.GetString("DM_DownloadSelected")
            btnDeleteLangPacks.Text = lp.GetString("DM_UninstallSelected")
            lblProgress.Text = lp.GetString("DM_Ready")
            btnOk.Text = lp.GetString("Opt_OK")
            btnCancel.Text = lp.GetString("Opt_Cancel")
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
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            lblProgress.Text = lp.GetString("DM_CheckingComponents")
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
                            item.SubItems.Add(lp.GetString("DM_StatusInstalled"))
                            item.SubItems.Add(If(st.InstalledVersion, ""))
                            item.ForeColor = Drawing.Color.DarkGreen
                        Case ToolStatus.Installed
                            item.SubItems.Add(lp.GetString("DM_StatusInstalled"))
                            item.SubItems.Add(If(st.InstalledVersion, ""))
                            item.ForeColor = Drawing.Color.DarkGreen
                        Case ToolStatus.UpdateAvailable
                            item.SubItems.Add(lp.GetString("DM_StatusUpdateAvailable"))
                            item.SubItems.Add($"{st.InstalledVersion} -> {st.LatestVersion}")
                            item.ForeColor = Drawing.Color.DarkOrange
                        Case ToolStatus.Missing
                            item.SubItems.Add(lp.GetString("DM_StatusNotInstalled"))
                            item.SubItems.Add("")
                            item.ForeColor = Drawing.Color.Red
                        Case ToolStatus.CheckFailed
                            item.SubItems.Add(lp.GetString("DM_StatusCheckFailed"))
                            item.SubItems.Add("")
                            item.ForeColor = Drawing.Color.Gray
                    End Select
                    item.Tag = st
                    lvTools.Items.Add(item)
                Next

                ' Add Python Embedded
                Dim pyItem As New ListViewItem(lp.GetString("DM_PythonEmbedded"))
                pyItem.SubItems.Add(lp.GetString("DM_Runtime"))
                If pythonState.Status = ToolStatus.UpToDate Then
                    pyItem.SubItems.Add(lp.GetString("DM_StatusInstalled"))
                    pyItem.SubItems.Add("3.12")
                    pyItem.ForeColor = Drawing.Color.DarkGreen
                Else
                    pyItem.SubItems.Add(lp.GetString("DM_StatusNotInstalled"))
                    pyItem.SubItems.Add("")
                    pyItem.ForeColor = Drawing.Color.Red
                End If
                pyItem.Tag = pythonState
                lvTools.Items.Add(pyItem)

                ' Add Python Packages
                Dim pkgItem As New ListViewItem(lp.GetString("DM_PythonPackages"))
                pkgItem.SubItems.Add(lp.GetString("DM_Runtime"))
                Dim missingPkgs = _mgr.GetMissingPythonPackages()
                If missingPkgs.Count = 0 Then
                    pkgItem.SubItems.Add(lp.GetString("DM_StatusInstalled"))
                    pkgItem.SubItems.Add("ctranslate2, sentencepiece, fastapi, uvicorn, silero-vad, sounddevice, edge-tts, faster-whisper")
                    pkgItem.ForeColor = Drawing.Color.DarkGreen
                Else
                    pkgItem.SubItems.Add(String.Format(lp.GetString("DM_PackagesMissing"), missingPkgs.Count))
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

            ' Load biblical vocabulary status
            LoadVocabStatus()

            ' Load language packs
            LoadLangPacks()

            ' Load installed Bibles and cached catalog if available
            LoadInstalledBibles()
            LoadCachedCatalog()
            PopulateBibleTree()

            If lblProgress.Text = lp.GetString("DM_CheckingComponents") Then
                lblProgress.Text = lp.GetString("DM_Ready")
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
                Dim lp = Services.Infrastructure.LanguagePackService.Instance
                item.SubItems.Add(If(installed, lp.GetString("DM_StatusInstalled"), lp.GetString("DM_StatusNotInstalled")))
                item.Tag = kvp.Key
                If installed Then item.ForeColor = Drawing.Color.DarkGreen
                lvVoices.Items.Add(item)
            Next
        End Sub

        Private Sub LoadMmsTtsStatus()
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim mmsTtsInstalled = MmsTtsBackend.CheckDepsInstalled()
            If mmsTtsInstalled Then
                lblMmsTtsStatus.Text = lp.GetString("DM_MmsTtsInstalledStatus")
                btnInstallMmsTts.Text = lp.GetString("DM_StatusInstalled")
                btnInstallMmsTts.Enabled = False
            Else
                lblMmsTtsStatus.Text = lp.GetString("DM_MmsTtsNotInstalledStatus")
                btnInstallMmsTts.Text = lp.GetString("DM_MmsTtsInstallSize")
                btnInstallMmsTts.Enabled = True
            End If
        End Sub

        Private Shared Function VocabLangName(code As String) As String
            ' Resolve a display name from the app's language database (no hardcoding).
            Return Services.Infrastructure.LanguageCodeService.Instance.GetDisplayNameForCode(code)
        End Function

        Private Sub LoadVocabStatus()
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim installed = _mgr.BiblicalVocabInstalledCodes()
            If installed.Count > 0 Then
                lblVocabStatus.Text = String.Format(lp.GetString("DM_VocabGenerated"),
                    String.Join(", ", installed.Select(AddressOf VocabLangName)))
            ElseIf _mgr.AnyBibleInstalled() Then
                lblVocabStatus.Text = lp.GetString("DM_VocabNotGenerated")
            Else
                lblVocabStatus.Text = lp.GetString("DM_VocabNoBibles")
            End If
            btnGenerateVocab.Enabled = _mgr.AnyBibleInstalled()
        End Sub

        Private Async Sub btnGenerateVocab_Click(sender As Object, e As EventArgs) Handles btnGenerateVocab.Click
            If _downloading Then Return
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            _downloading = True
            SetAllButtonsEnabled(False)
            btnGenerateVocab.Enabled = False
            Try
                lblProgress.Text = lp.GetString("DM_VocabGenerating")
                pbProgress.Value = 0
                pbProgress.Style = ProgressBarStyle.Marquee
                Await _mgr.GenerateBiblicalVocabAsync()
                pbProgress.Style = ProgressBarStyle.Continuous
                pbProgress.Value = 100
                LoadVocabStatus()
                lblProgress.Text = lp.GetString("DM_VocabDone")
            Catch ex As Exception
                pbProgress.Style = ProgressBarStyle.Continuous
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                btnGenerateVocab.Enabled = _mgr.AnyBibleInstalled()
            End Try
        End Sub

        ' ── Bible Catalog ──
        ' Catalog fetch/parse + download/convert orchestration lives in Core
        ' (Services.Bible.BibleDownloadService) — shared with the web settings
        ' Bible installer. This form only owns the TreeView UI around it.

        Private _catalog As List(Of Services.Bible.BibleCatalogEntry)
        Private _installedBibles As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Private Sub LoadCachedCatalog()
            _catalog = Services.Bible.BibleDownloadService.LoadCachedCatalog(_biblesDir)
        End Sub

        Private Sub LoadInstalledBibles()
            _installedBibles.Clear()
            _installedBibles.UnionWith(Services.Bible.BibleDownloadService.GetInstalledIds(_biblesDir))
        End Sub

        Private Sub PopulateBibleTree(Optional filter As String = Nothing)
            tvBibles.BeginUpdate()
            tvBibles.Nodes.Clear()

            If _catalog Is Nothing OrElse _catalog.Count = 0 Then
                tvBibles.Nodes.Add(Services.Infrastructure.LanguagePackService.Instance.GetString("DM_BiblePlaceholder"))
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
                    Dim blp = Services.Infrastructure.LanguagePackService.Instance
                    Dim status = If(installed, blp.GetString("DM_BibleInstalled"), "")
                    Dim books = ""
                    If entry.OTBooks > 0 AndAlso entry.NTBooks > 0 Then
                        books = blp.GetString("DM_BibleOtNt")
                    ElseIf entry.NTBooks > 0 Then
                        books = blp.GetString("DM_BibleNtOnly")
                    ElseIf entry.OTBooks > 0 Then
                        books = blp.GetString("DM_BibleOtOnly")
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

        Private Async Sub btnFetchCatalog_Click(sender As Object, e As EventArgs) Handles btnFetchCatalog.Click
            If _downloading Then Return
            btnFetchCatalog.Enabled = False
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            lblProgress.Text = lp.GetString("DM_FetchingCatalog")
            pbProgress.Style = ProgressBarStyle.Marquee

            Try
                _catalog = Await Services.Bible.BibleDownloadService.GetCatalogAsync(_biblesDir)
                LoadInstalledBibles()
                PopulateBibleTree()
                lblProgress.Text = String.Format(lp.GetString("DM_FoundTranslations"), _catalog.Count)

            Catch ex As Exception
                lblProgress.Text = String.Format(lp.GetString("DM_ErrorFetchingCatalog"), ex.Message)
            Finally
                pbProgress.Style = ProgressBarStyle.Continuous
                pbProgress.Value = 0
                btnFetchCatalog.Enabled = True
            End Try
        End Sub

        Private Async Sub btnDownloadBibles_Click(sender As Object, e As EventArgs) Handles btnDownloadBibles.Click
            If _downloading Then Return

            ' Collect checked translation nodes
            Dim toDownload As New List(Of Services.Bible.BibleCatalogEntry)
            For Each langNode As TreeNode In tvBibles.Nodes
                For Each childNode As TreeNode In langNode.Nodes
                    If childNode.Checked Then
                        Dim entry = TryCast(childNode.Tag, Services.Bible.BibleCatalogEntry)
                        If entry IsNot Nothing AndAlso Not _installedBibles.Contains(entry.TranslationId) Then
                            toDownload.Add(entry)
                        End If
                    End If
                Next
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = Services.Infrastructure.LanguagePackService.Instance.GetString("DM_SelectBibles")
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)

            Dim allIssues As New List(Of String)()

            Try
                For i = 0 To toDownload.Count - 1
                    Dim entry = toDownload(i)
                    Dim itemNo = i + 1
                    lblProgress.Text = $"Downloading {entry.Title} ({itemNo}/{toDownload.Count})..."
                    pbProgress.Value = 0

                    Try
                        ' Shared Core orchestration: download zip → convert → verify.
                        ' The stage callback runs on the UI thread (awaited from here).
                        Dim issues = Await Services.Bible.BibleDownloadService.DownloadAndConvertAsync(
                            entry, _biblesDir,
                            Sub(stage)
                                If stage = "converting" Then
                                    lblProgress.Text = $"Converting {entry.Title} to SQLite3 ({itemNo}/{toDownload.Count})..."
                                    pbProgress.Style = ProgressBarStyle.Marquee
                                ElseIf stage = "verifying" Then
                                    pbProgress.Style = ProgressBarStyle.Continuous
                                    lblProgress.Text = $"Verifying {entry.Title}..."
                                End If
                            End Sub)

                        If issues.Count > 0 Then
                            allIssues.Add($"{entry.Title}:")
                            For Each issue In issues
                                allIssues.Add($"  - {issue}")
                            Next
                        End If

                        Dim dbPath = Path.Combine(_biblesDir, entry.LanguageCode, entry.TranslationId & ".sqlite3")
                        Dim sizeMb = New FileInfo(dbPath).Length / 1024.0 / 1024.0
                        lblProgress.Text = $"Converted {entry.Title} ({sizeMb:F1} MB) ({itemNo}/{toDownload.Count})"
                    Catch ex As Exception
                        ' One failed Bible shouldn't abort the rest of the batch.
                        Dim dlp = Services.Infrastructure.LanguagePackService.Instance
                        allIssues.Add(String.Format(dlp.GetString("DM_BibleConversionFailed"), entry.Title))
                        lblProgress.Text = String.Format(dlp.GetString("DM_BibleConversionWarning"), entry.Title)
                        AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"Bible download {entry.TranslationId}: {ex.Message}")
                    Finally
                        pbProgress.Style = ProgressBarStyle.Continuous
                    End Try

                    pbProgress.Value = CInt(itemNo * 100 \ toDownload.Count)
                Next

                Dim blp2 = Services.Infrastructure.LanguagePackService.Instance
                lblProgress.Text = String.Format(blp2.GetString("DM_DownloadedBibles"), toDownload.Count)
                pbProgress.Value = 100
                PathsUpdated = True

                ' Show integrity report if any issues were found
                If allIssues.Count > 0 Then
                    Dim report = String.Join(vbCrLf, allIssues)
                    MessageBox.Show(
                        String.Format(blp2.GetString("DM_BibleDownloadedWithIssues"), toDownload.Count, report),
                        blp2.GetString("DM_BibleIntegrityReport"),
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
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Select Case name
                Case "yt-dlp", "FFmpeg", "Subtitle Edit"
                    Return lp.GetString("DM_CategoryTool")
                Case "Whisper Model (ggml-large-v3)", "GGML Whisper Model", "faster-whisper Model", "Silero VAD Model"
                    Return lp.GetString("DM_CategoryAiModel")
                Case "NLLB Translation Model", "NLLB 3.3B Translation Model"
                    Return lp.GetString("DM_CategoryAiModel")
                Case "whisper-server (Vulkan)", "whisper-server (CUDA)"
                    Return lp.GetString("DM_CategoryTool")
                Case "AWS SDK (Amazon Translate)"
                    Return lp.GetString("DM_CategoryTool")
                Case "Whisper CLI + CUDA runtime"
                    Return lp.GetString("DM_CategoryTool")
                Case "Piper TTS"
                    Return lp.GetString("DM_CategoryTts")
                Case Else
                    Return lp.GetString("DM_CategoryOther")
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

            AppLogger.Log(LogEvents.DL_DOWNLOAD_START, $"Download button: {toDownload.Count} tools, needPython={needPython}, needPythonDeps={needPythonDeps}")
            For Each tool In toDownload
                AppLogger.Log(LogEvents.DL_DOWNLOAD_START, $"queued: '{tool.Name}' status={tool.Status}")
            Next

            If toDownload.Count = 0 AndAlso Not needPython AndAlso Not needPythonDeps Then
                lblProgress.Text = Services.Infrastructure.LanguagePackService.Instance.GetString("DM_SelectComponents")
                Return
            End If

            _downloading = True
            SetAllButtonsEnabled(False)

            Try
                Dim total = toDownload.Count + If(needPython, 1, 0) + If(needPythonDeps, 1, 0)
                Dim current = 0

                ' Python FIRST, then packages — some model downloads (e.g.
                ' faster-whisper) run the embedded Python to fetch the model, so
                ' Python + its packages must be installed before the tools loop.
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

                ' Then download tools/models (faster-whisper model now has Python).
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

                pbProgress.Value = 100
                lblProgress.Text = String.Format(Services.Infrastructure.LanguagePackService.Instance.GetString("DM_DownloadedComponents"), total)
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

            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim installedVoices = _mgr.GetInstalledPiperVoices()
            Dim toDownload As New List(Of String)()
            For Each item As ListViewItem In lvVoices.CheckedItems
                Dim lang = DirectCast(item.Tag, String)
                If Not installedVoices.Contains(lang) Then
                    toDownload.Add(lang)
                End If
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = lp.GetString("DM_SelectVoicesToDownload")
                Return
            End If

            ' Check Piper engine first
            Dim piperState = Await _mgr.CheckPiperAsync()
            If piperState.Status <> ToolStatus.UpToDate Then
                lblProgress.Text = lp.GetString("DM_PiperRequired")
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

                lblProgress.Text = String.Format(lp.GetString("DM_DownloadedVoices"), toDownload.Count)
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
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim installedVoices = _mgr.GetInstalledPiperVoices()
            Dim toRemove As New List(Of (lang As String, display As String))()
            For Each item As ListViewItem In lvVoices.CheckedItems
                Dim lang = DirectCast(item.Tag, String)
                If installedVoices.Contains(lang) Then
                    toRemove.Add((lang, item.Text))
                End If
            Next

            If toRemove.Count = 0 Then
                lblProgress.Text = lp.GetString("DM_SelectVoicesToRemove")
                Return
            End If

            Dim names = String.Join(", ", toRemove.Select(Function(r) r.display))
            Dim result = MessageBox.Show(
                String.Format(lp.GetString("DM_RemoveVoicesConfirm"), toRemove.Count) & vbCrLf & names,
                lp.GetString("DM_RemoveVoicesTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
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
                        AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"RemoveVoices (delete model files): {ex.Message}")
                    End Try
                End If
            Next

            lblProgress.Text = String.Format(lp.GetString("DM_RemovedVoices"), removed)
            LoadVoices()
        End Sub

        ' ── MMS-TTS Install ──

        Private Async Sub btnInstallMmsTts_Click(sender As Object, e As EventArgs) Handles btnInstallMmsTts.Click
            If _downloading Then Return

            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim result = MessageBox.Show(
                lp.GetString("DM_MmsTtsInstallConfirm"),
                lp.GetString("DM_MmsTtsInstallTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            _downloading = True
            SetAllButtonsEnabled(False)
            btnInstallMmsTts.Enabled = False

            Try
                lblProgress.Text = lp.GetString("DM_MmsTtsInstalling")
                pbProgress.Value = 0
                pbProgress.Style = ProgressBarStyle.Marquee

                Await Task.Run(Function() _mgr.InstallMmsTtsDepsAsync(Nothing))

                pbProgress.Style = ProgressBarStyle.Continuous
                pbProgress.Value = 100

                If MmsTtsBackend.CheckDepsInstalled() Then
                    lblMmsTtsStatus.Text = lp.GetString("DM_MmsTtsInstalledStatus")
                    btnInstallMmsTts.Text = lp.GetString("DM_StatusInstalled")
                    lblProgress.Text = lp.GetString("DM_MmsTtsInstallSuccess")
                Else
                    lblProgress.Text = lp.GetString("DM_MmsTtsInstallFailed")
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
                Dim lp = Services.Infrastructure.LanguagePackService.Instance
                Dim isInstalled = installed.Any(Function(c) c.Equals(l.Iso1, StringComparison.OrdinalIgnoreCase))
                If isInstalled Then
                    item.SubItems.Add(lp.GetString("DM_StatusInstalled"))
                    item.ForeColor = Drawing.Color.DarkGreen
                Else
                    item.SubItems.Add(lp.GetString("DM_StatusNotInstalled"))
                End If
                item.Tag = l.Iso1
                lvLangPacks.Items.Add(item)
            Next
        End Sub

        Private Async Sub btnDownloadLangPacks_Click(sender As Object, e As EventArgs) Handles btnDownloadLangPacks.Click
            If _downloading Then Return

            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim installedLangs = lp.GetAvailableLanguages()
            Dim toDownload As New List(Of String)()
            For Each item As ListViewItem In lvLangPacks.CheckedItems
                Dim code = DirectCast(item.Tag, String)
                If Not installedLangs.Any(Function(c) c.Equals(code, StringComparison.OrdinalIgnoreCase)) Then
                    toDownload.Add(code)
                End If
            Next

            If toDownload.Count = 0 Then
                lblProgress.Text = lp.GetString("DM_NoLangPacksSelected")
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
                    lblProgress.Text = String.Format(lp.GetString("DM_DownloadingLangPack"), code)
                    Dim ok = Await langPack.DownloadLanguageAsync(code)
                    If ok Then
                        downloaded += 1
                    Else
                        lblProgress.Text = String.Format(lp.GetString("DM_LangPackNotAvailable"), code)
                    End If
                    pbProgress.Value = Math.Min(pbProgress.Value + 1, pbProgress.Maximum)
                Next

                pbProgress.Value = pbProgress.Maximum
                lblProgress.Text = String.Format(lp.GetString("DM_DownloadedLangPacks"), downloaded, toDownload.Count)
            Catch ex As Exception
                lblProgress.Text = $"Error: {ex.Message}"
            Finally
                _downloading = False
                SetAllButtonsEnabled(True)
                LoadLangPacks()
            End Try
        End Sub

        Private Sub btnDeleteLangPacks_Click(sender As Object, e As EventArgs) Handles btnDeleteLangPacks.Click
            Dim lp = Services.Infrastructure.LanguagePackService.Instance
            Dim installedLangs = lp.GetAvailableLanguages()
            Dim toDelete As New List(Of String)()
            For Each item As ListViewItem In lvLangPacks.CheckedItems
                Dim code = DirectCast(item.Tag, String)
                If installedLangs.Any(Function(c) c.Equals(code, StringComparison.OrdinalIgnoreCase)) Then
                    toDelete.Add(code)
                End If
            Next

            If toDelete.Count = 0 Then
                lblProgress.Text = lp.GetString("DM_NoInstalledSelected")
                Return
            End If

            Dim deleted = 0
            For Each code In toDelete
                Try
                    Dim path = IO.Path.Combine(lp.LocalesDirectory, $"{code}.json")
                    If IO.File.Exists(path) Then
                        IO.File.Delete(path)
                        deleted += 1
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.LOCALE_LOADED, $"Failed to uninstall {code}: {ex.Message}")
                End Try
            Next

            lblProgress.Text = String.Format(lp.GetString("DM_UninstalledLangPacks"), deleted)
            LoadLangPacks()
        End Sub

        Private Shared Function GetLanguageDisplayName(iso3Code As String) As String
            If String.IsNullOrEmpty(iso3Code) Then Return iso3Code
            Dim langSvc = Services.Infrastructure.LanguageCodeService.Instance
            Dim flores = langSvc.Iso3ToFlores(iso3Code)
            If Not String.IsNullOrEmpty(flores) Then
                Dim name = langSvc.GetDisplayName(flores)
                If Not String.IsNullOrEmpty(name) Then Return name
            End If
            Return iso3Code
        End Function

    End Class

End Namespace
