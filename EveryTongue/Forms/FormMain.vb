Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports EveryTongue.Services.Infrastructure
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Win32
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Server

Public Class FormMain

    ' Win32 helpers for RichTextBox flicker-free scrolling
    Private Const WM_SETREDRAW As Integer = &HB
    Private Const WM_VSCROLL As Integer = &H115
    Private Const SB_BOTTOM As Integer = 7

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    Private _config As AppConfig
    Private _transcribeController As Controllers.TranscribeController
    Private _serverController As Controllers.ServerController
    Private _liveController As Controllers.LiveController
    Private _langPack As LanguagePackService
    Private _translationService As TranslationService
    Private _isInitializing As Boolean = True
    Private _isSyncingUi As Boolean = False
    Private _exitForReal As Boolean = False

    ''' <summary>Resolves ISubtitleService from Kestrel DI. Returns Nothing if Kestrel isn't running.</summary>
    Private ReadOnly Property SubtitleSvc As Services.Interfaces.ISubtitleService
        Get
            Return _serverController?.GetSubtitleService()
        End Get
    End Property

    ' Supported whisper languages
    Private ReadOnly _whisperLanguages As String() = {
        "auto", "en", "es", "fr", "de", "it", "pt", "nl", "pl", "ru",
        "zh", "ja", "ko", "ar", "hi", "tr", "vi", "th", "cs", "el",
        "hu", "ro", "da", "fi", "no", "sv", "sk", "uk", "bg", "hr",
        "ca", "cy", "et", "ga", "lv", "lt", "mt", "sl", "sq", "mk",
        "sr", "bs", "is", "ms", "sw", "tl", "ta", "te", "ml", "si",
        "bn", "gu", "kn", "mr", "ne", "pa", "ur", "my", "lo", "km",
        "he", "fa", "id", "jw", "la", "mn", "ps", "sd", "sn", "so",
        "su", "tg", "tt", "uz", "yo", "af", "am", "as", "az", "ba",
        "be", "br", "fo", "gl", "ha", "ht", "hy", "ka", "kk", "lb",
        "ln", "mg", "mi", "nn", "oc", "sa", "tk", "wo", "yi", "yue"
    }

    Private Shared ReadOnly _langCodeService As LanguageCodeService = LanguageCodeService.Instance

    Private Function LangDisplayName(code As String) As String
        If String.Equals(code, "auto", StringComparison.OrdinalIgnoreCase) Then Return "Auto Detect (auto)"
        Dim nllb = _langCodeService.ToNllb(code)
        If Not String.IsNullOrEmpty(nllb) Then
            Dim name = _langCodeService.GetDisplayName(nllb)
            If Not String.IsNullOrEmpty(name) Then Return $"{name} ({code})"
        End If
        Return code
    End Function

    Private Function LangCodeFromDisplay(display As String) As String
        Dim pIdx = display.LastIndexOf("("c)
        If pIdx > 0 Then
            Dim code = display.Substring(pIdx + 1).TrimEnd(")"c)
            Return code
        End If
        Return display
    End Function

    ' Available UI locales with native names (refreshed after language pack downloads)
    Private _uiLocales As (Code As String, Name As String)() = DiscoverUiLocales()

    Private Shared Function DiscoverUiLocales() As (Code As String, Name As String)()
        Dim result As New List(Of (Code As String, Name As String))()
        ' English is always available
        result.Add(("en", "English"))

        ' Scan for locale JSON files
        Dim localesDir = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales")
        Try
            If IO.Directory.Exists(localesDir) Then
                For Each f In IO.Directory.GetFiles(localesDir, "*.json")
                    Dim code = IO.Path.GetFileNameWithoutExtension(f)
                    If code.Equals("en", StringComparison.OrdinalIgnoreCase) Then Continue For
                    Try
                        Dim ci = Globalization.CultureInfo.GetCultureInfo(code)
                        result.Add((code, ci.NativeName.Substring(0, 1).ToUpper() & ci.NativeName.Substring(1)))
                    Catch ex As Exception
                        result.Add((code, code))
                    End Try
                Next
            End If
        Catch ex As Exception
            WriteDebugLog($"[ERROR] DiscoverUiLocales: {ex.Message}")
        End Try

        Return result.ToArray()
    End Function

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Services.Infrastructure.AppLogger.UiCallback = AddressOf AppendUnifiedLog
        Services.Infrastructure.AppLogger.OpenDownloadManager = AddressOf OpenDownloadManager
        _langPack = LanguagePackService.Instance

        ' Load config
        _config = ConfigManager.Load()
        WriteDebugLog($"[STARTUP] Config loaded: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}")

        ' First-run language picker — before anything else
        If Not _config.FirstRunComplete Then
            Using picker As New FormLanguagePicker()
                If picker.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrEmpty(picker.SelectedLanguage) Then
                    _config.UiLanguage = picker.SelectedLanguage
                    ConfigManager.Save(_config)

                    ' Download language pack if not English, then load it
                    If Not picker.SelectedLanguage.Equals("en", StringComparison.OrdinalIgnoreCase) Then
                        WriteDebugLog($"[FIRSTRUN] Downloading language pack: {picker.SelectedLanguage}")
                        Try
                            ' Use Task.Run to avoid deadlock on UI thread
                            Dim ok = Task.Run(Function() _langPack.DownloadLanguageAsync(picker.SelectedLanguage)).GetAwaiter().GetResult()
                            If ok Then
                                WriteDebugLog($"[FIRSTRUN] Language pack downloaded: {picker.SelectedLanguage}")
                                _langPack.LoadLanguage(picker.SelectedLanguage)
                            Else
                                WriteDebugLog($"[FIRSTRUN] Language pack not available: {picker.SelectedLanguage}")
                            End If
                        Catch ex As Exception
                            WriteDebugLog($"[FIRSTRUN] Language pack download failed: {ex.Message}")
                        End Try
                    End If

                    ' Refresh available locales after download
                    _uiLocales = DiscoverUiLocales()
                End If
            End Using
        End If

        ' Clean up old log files (keep last 30 days)
        Services.Infrastructure.AppLogger.CleanupOldLogFiles(30)

        ' Run file integrity check at startup (log only, non-blocking)
        Task.Run(Sub()
                     Try
                         Dim result = Services.Infrastructure.IntegrityChecker.Check()
                         For Each line In Services.Infrastructure.IntegrityChecker.ToReportLines(result)
                             WriteDebugLog($"[Integrity] {line}")
                         Next
                     Catch ex As Exception
                         WriteDebugLog($"[Integrity] Check failed: {ex.Message}")
                     End Try
                 End Sub)

        ' Create transcribe controller
        _transcribeController = New Controllers.TranscribeController(
            _config, cboMode, cboModel, cboInputLanguage, cboOutputLanguage,
            txtUrl, txtOutputDir, btnStart, btnResume, btnCancel,
            btnBrowseFile, btnBrowseOutput, btnOpenOutput, btnOpenSubtitleEdit,
            lnkPreviewSrt, lblStepStatus, lblUrl,
            lblStartTime, lblEndTime, lblStartColon1, lblStartColon2, lblEndColon1, lblEndColon2,
            txtStartHH, txtStartMM, txtStartSS, txtEndHH, txtEndMM, txtEndSS,
            pbOverall, pbChunk, grpOutputFormats, tabMain, tabPageJob,
            _whisperLanguages,
            AddressOf SaveUiToConfig,
            AddressOf ShowLogPanel,
            Sub(source, msg, clr) AppendUnifiedLog(source, msg, clr),
            AddressOf GetString,
            AddressOf WriteDebugLog)
        _transcribeController.WireEvents()

        ' Populate dropdowns
        _transcribeController.PopulateLanguageDropdowns()

        ' Bind config to UI
        LoadConfigToUi()
        _isInitializing = False

        ' Set default output directory
        If String.IsNullOrWhiteSpace(txtOutputDir.Text) Then
            Dim stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")
            txtOutputDir.Text = Path.Combine(AppConfig.ResolvePath(_config.PathOutputRoot), stamp)
        End If

        ' Apply locale
        _langPack.LoadLanguage(_config.UiLanguage)
        ApplyLocale()

        ' Populate model dropdown
        _transcribeController.PopulateModelDropdown()

        ' Set browse button text for YouTube mode on startup
        btnBrowseFile.Text = GetString("Btn_BrowseFile")

        ' Apply tooltips
        ApplyToolTips()

        ' Load help content
        LoadHelpContent(_config.UiLanguage)

        ' Create live controller
        _liveController = New Controllers.LiveController(
            _config,
            cboLiveDevice, cboLiveInputLang,
            btnLiveStart, btnLiveStop,
            btnRefreshDevices, btnEditFilters,
            btnTuneStats,
            grpLiveInput,
            trkMaxSegment, trkVadSilence,
            lblMaxSegmentValue, lblVadSilenceValue,
            _whisperLanguages,
            AddressOf LangDisplayName,
            AddressOf LangCodeFromDisplay,
            AddressOf SaveUiToConfig,
            Function() SubtitleSvc,
            Function() _translationService,
            AddressOf StartTranslationService,
            AddressOf WriteDebugLog,
            AddressOf ShowLogPanel,
            AddressOf UpdateShellStatus,
            AddressOf GetString,
            Me.Icon,
            Me,
            Sub(lang) SelectComboItem(cboInputLanguage, lang))
        _liveController.WireEvents()
        _liveController.PopulateLanguageDropdown()

        ' Enumerate audio devices in the background
        cboLiveDevice.Items.Add("Detecting devices...")
        cboLiveDevice.SelectedIndex = 0
        btnLiveStart.Enabled = False
        Dim pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
        Task.Run(Sub()
                     Try
                         Dim backend As New Services.Stt.FasterWhisperBackend()
                         Dim deviceInfos = backend.EnumerateDevicesAsync(pythonPath)
                         Dim devices As New List(Of String)
                         For Each d In deviceInfos
                             devices.Add(d.ToString())
                         Next
                         cboLiveDevice.BeginInvoke(Sub()
                                                       _liveController.UpdateDeviceCombo(devices)
                                                       btnLiveStart.Enabled = True
                                                   End Sub)
                     Catch ex As Exception
                         WriteDebugLog($"[ERROR] Device enumeration failed: {ex.Message}")
                         cboLiveDevice.BeginInvoke(Sub()
                                                       cboLiveDevice.Items.Clear()
                                                       cboLiveDevice.Items.Add("(Device detection failed)")
                                                       cboLiveDevice.SelectedIndex = 0
                                                       btnLiveStart.Enabled = True
                                                   End Sub)
                     End Try
                 End Sub)

        ' Create server controller
        _serverController = New Controllers.ServerController(
            _config, wvLiveClients,
            AddressOf UpdateShellStatus,
            AddressOf WriteDebugLog)

        ' Run path verification at startup (log only, non-blocking)
        Dim sc = _serverController
        Task.Run(Sub()
                     Try
                         sc.VerifyAllPathsCore()
                     Catch ex As Exception
                         WriteDebugLog($"[VerifyPaths] Check failed: {ex.Message}")
                     End Try
                 End Sub)

        ' Fix tab switching rendering (progress bars don't repaint correctly)
        AddHandler tabMain.SelectedIndexChanged, Sub(s, ev)
                                                     If tabMain.SelectedTab Is tabPageJob Then
                                                         pbOverall.Refresh()
                                                         pbChunk.Refresh()
                                                         grpProgress.Refresh()
                                                     End If
                                                 End Sub

        ' Check for updates in the background
        CheckForUpdatesAsync()

        ' Check for missing dependencies
        CheckDependenciesAsync()

        ' Wire up system tray
        AddHandler trayIcon.DoubleClick, Sub(s, ev) ShowFromTray()
        AddHandler trayMenuShow.Click, Sub(s, ev) ShowFromTray()
        AddHandler trayMenuAbout.Click, Sub(s, ev)
            Using dlg As New FormAbout()
                dlg.ShowDialog(Me)
            End Using
        End Sub
        AddHandler trayMenuExit.Click, Sub(s, ev) ExitApplication()

        ' Apply saved startup preference (first-run setup happens after dependency download)
        If _config.FirstRunComplete Then
            If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()
        End If

        ' Build shell chrome (menu, toolbar, nav rail, status bar)
        InitializeShell()

        ' Apply theme (must be after InitializeShell so nav bar gets correct colours)
        ApplyTheme(_config.Theme)

        ' Auto-start subtitle server after form is fully shown
        AddHandler Me.Shown, Sub(s, ev)
                                  _serverController.StartServer(Sub(svc) _liveController.ConfigureSubtitleService(svc))

                                  ' Wire conversation room translation callback
                                  Dim convAudioHandler = TryCast(_serverController.KestrelHost?.Services?.GetService(
                                      GetType(Services.Rooms.ConversationAudioHandler)), Services.Rooms.ConversationAudioHandler)
                                  If convAudioHandler IsNot Nothing Then
                                      convAudioHandler.EnsureTranslationAvailable = Sub() Me.BeginInvoke(Sub() EnsureNllbForRooms())
                                  End If

                                  ' Auto-start NLLB translation engine and Whisper live-server
                                  ' so conversation rooms are ready immediately
                                  EnsureNllbForRooms()
                                  If convAudioHandler IsNot Nothing Then
                                      WriteDebugLog("[STARTUP] Auto-starting Whisper live-server for conversation rooms...")
                                      Task.Run(Async Function()
                                                   Try
                                                       Dim ready = Await convAudioHandler.EnsureLiveServerAsync(CancellationToken.None)
                                                       WriteDebugLog($"[STARTUP] Whisper live-server auto-start: ready={ready}")
                                                   Catch ex As Exception
                                                       WriteDebugLog($"[STARTUP] Whisper live-server auto-start failed: {ex.Message}")
                                                   End Try
                                               End Function)
                                  Else
                                      WriteDebugLog("[STARTUP] ConversationAudioHandler not available — skipping Whisper auto-start")
                                  End If

                                  InitBibleTab()
                              End Sub
    End Sub


    Private Sub RegisterStartup()
        Try
            Using key = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
                key?.SetValue("EveryTongue", $"""{Application.ExecutablePath}""")
            End Using
        Catch ex As Exception
            WriteDebugLog($"[WARN] RegisterStartup failed: {ex.Message}")
        End Try
    End Sub

    Private Sub UnregisterStartup()
        Try
            Using key = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
                key?.DeleteValue("EveryTongue", False)
            End Using
        Catch ex As Exception
            WriteDebugLog($"[WARN] UnregisterStartup failed: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowFromTray()
        Me.Show()
        Me.WindowState = FormWindowState.Maximized
        Me.Activate()
    End Sub

    Private Sub ExitApplication()
        _exitForReal = True
        Me.Close()
    End Sub

    Private Async Sub CheckForUpdatesAsync()
        WriteDebugLog("[UPDATE] CheckForUpdatesAsync starting...")
        Dim update = Await Models.UpdateChecker.CheckForUpdateAsync()
        If update Is Nothing Then
            WriteDebugLog("[UPDATE] No update available")
            Return
        End If
        WriteDebugLog($"[UPDATE] Update found: {update.TagName} — showing MessageBox")

        ' Try modular (app-only) update first, fall back to full installer
        Dim canModularUpdate = Not String.IsNullOrWhiteSpace(update.AppZipUrl)
        Dim needsWhisper = update.NeedsWhisperUpdate AndAlso Not String.IsNullOrWhiteSpace(update.WhisperZipUrl)

        Dim updateDesc = If(canModularUpdate AndAlso Not needsWhisper,
            GetString("Main_SmallUpdate"),
            GetString("Main_UpdateAvailable"))

        Dim confirm = MessageBox.Show(
            $"{GetString("Msg_NewVersionAvailable")} {update.TagName}" & vbCrLf & vbCrLf &
            updateDesc & vbCrLf & GetString("Msg_DownloadUpdate"),
            GetString("Msg_UpdateAvailable"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information)
        If confirm <> DialogResult.Yes Then Return

        ' Modular update: download zips and extract in-place
        If canModularUpdate Then
            Try
                Await ApplyModularUpdateAsync(update)
                Return
            Catch ex As Exception
                ' Modular update failed — fall back to installer
                If String.IsNullOrWhiteSpace(update.InstallerUrl) Then
                    MessageBox.Show(String.Format(GetString("Main_UpdateFailed"), ex.Message) & vbCrLf & vbCrLf &
                                   GetString("Main_OpeningReleasePage"),
                                   GetString("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Process.Start(New ProcessStartInfo(update.HtmlUrl) With {.UseShellExecute = True})
                    Return
                End If
            End Try
        End If

        ' Full installer fallback
        If String.IsNullOrWhiteSpace(update.InstallerUrl) Then
            Process.Start(New ProcessStartInfo(update.HtmlUrl) With {.UseShellExecute = True})
            Return
        End If

        Try
            Dim tempPath = Path.Combine(Path.GetTempPath(), $"EveryTongue_Setup_{update.TagName}.exe")
            Using client As New Net.Http.HttpClient()
                client.DefaultRequestHeaders.UserAgent.ParseAdd("EveryTongue-Updater")
                Using response = Await client.GetAsync(update.InstallerUrl)
                    response.EnsureSuccessStatusCode()
                    Using fs As New FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None)
                        Await response.Content.CopyToAsync(fs)
                    End Using
                End Using
            End Using

            Process.Start(New ProcessStartInfo(tempPath) With {.UseShellExecute = True})
            _exitForReal = True
            Application.Exit()
        Catch ex As Exception
            MessageBox.Show(String.Format(GetString("Main_UpdateDownloadFailed"), ex.Message) & vbCrLf & vbCrLf &
                           GetString("Main_OpeningReleasePage"),
                           GetString("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Process.Start(New ProcessStartInfo(update.HtmlUrl) With {.UseShellExecute = True})
        End Try
    End Sub

    Private Async Function ApplyModularUpdateAsync(update As Models.UpdateInfo) As Task
        Dim appDir = AppDomain.CurrentDomain.BaseDirectory
        Dim tempDir = Path.Combine(Path.GetTempPath(), $"EveryTongue_Update_{update.TagName}")

        If Directory.Exists(tempDir) Then Directory.Delete(tempDir, True)
        Directory.CreateDirectory(tempDir)

        Using client As New Net.Http.HttpClient()
            client.DefaultRequestHeaders.UserAgent.ParseAdd("EveryTongue-Updater")
            client.Timeout = TimeSpan.FromMinutes(10)

            ' Download app zip
            Dim appZipPath = Path.Combine(tempDir, "app.zip")
            Using response = Await client.GetAsync(update.AppZipUrl)
                response.EnsureSuccessStatusCode()
                Using fs As New FileStream(appZipPath, FileMode.Create, FileAccess.Write, FileShare.None)
                    Await response.Content.CopyToAsync(fs)
                End Using
            End Using

            ' Download whisper zip if needed
            Dim whisperZipPath = ""
            If update.NeedsWhisperUpdate AndAlso Not String.IsNullOrWhiteSpace(update.WhisperZipUrl) Then
                whisperZipPath = Path.Combine(tempDir, "whisper.zip")
                Using response = Await client.GetAsync(update.WhisperZipUrl)
                    response.EnsureSuccessStatusCode()
                    Using fs As New FileStream(whisperZipPath, FileMode.Create, FileAccess.Write, FileShare.None)
                        Await response.Content.CopyToAsync(fs)
                    End Using
                End Using
            End If

            ' Write a batch script that waits for us to exit, extracts, and relaunches
            Dim batchPath = Path.Combine(tempDir, "apply-update.cmd")
            Dim exeName = Path.GetFileName(Application.ExecutablePath)
            Dim whisperExtract = ""
            If Not String.IsNullOrEmpty(whisperZipPath) Then
                whisperExtract = $"powershell -NoProfile -Command ""Expand-Archive -Path '{whisperZipPath}' -DestinationPath '{appDir}whisper' -Force""" & vbCrLf
            End If

            Dim batchContent = $"@echo off
echo Waiting for Every Tongue to close...
:wait
tasklist /FI ""PID eq %1"" 2>NUL | find ""%1"" >NUL
if not errorlevel 1 (
    timeout /t 1 /nobreak >NUL
    goto wait
)
echo Applying update...
powershell -NoProfile -Command ""Expand-Archive -Path '{appZipPath}' -DestinationPath '{appDir}' -Force""
{whisperExtract}echo Update complete. Launching...
start """" ""{Path.Combine(appDir, exeName)}""
del ""%~f0""
"
            IO.File.WriteAllText(batchPath, batchContent)

            ' Launch the batch script with our PID, then exit
            Dim myPid = Process.GetCurrentProcess().Id
            Process.Start(New ProcessStartInfo("cmd.exe", $"/c ""{batchPath}"" {myPid}") With {
                .CreateNoWindow = True,
                .UseShellExecute = False
            })

            _exitForReal = True
            Application.Exit()
        End Using
    End Function

    Private Async Sub CheckDependenciesAsync(Optional manualCheck As Boolean = False)
        Dim isFirstRun = Not _config.FirstRunComplete

        Try
            Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
            Dim mgr As New Models.DependencyManager(_config, toolsDir)

            Dim states = Await mgr.CheckAllToolsAsync()
            Dim missing = mgr.GetMissingTools(states)
            Dim updatable = mgr.GetUpdatableTools(states)

            If manualCheck Then
                ' User explicitly asked — always show Download Manager
                OpenDownloadManager()
            ElseIf Not isFirstRun AndAlso missing.Count > 0 Then
                ' Subsequent runs — just warn in the log
                For Each tool In missing
                    WriteDebugLog($"[WARN] Missing tool: {tool.Name}")
                Next
            End If

        Catch ex As Exception
            If manualCheck Then
                WriteDebugLog($"[ERROR] CheckDependenciesAsync: {ex.Message}")
            End If
        End Try

        ' First run: Download Manager (Language Packs tab) → Options Hardware
        If isFirstRun Then
            WriteDebugLog("[FIRSTRUN] Setting FirstRunComplete=True")
            _config.FirstRunComplete = True
            ConfigManager.Save(_config)

            ' Show Download Manager opened to Language Packs tab
            WriteDebugLog("[FIRSTRUN] Opening Download Manager → Language Packs")
            Try
                Dim biblesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles")
                Using dlg As New Forms.FormDownloadManager(_config, biblesDir)
                    dlg.SelectTab("tabLangPacks")
                    dlg.ShowDialog(Me)
                    If dlg.PathsUpdated Then
                        UpdateConfigPaths(AppDomain.CurrentDomain.BaseDirectory)
                    End If
                End Using
            Catch ex As Exception
                WriteDebugLog($"[FIRSTRUN] Download Manager error: {ex.Message}")
            End Try
            WriteDebugLog("[FIRSTRUN] Download Manager closed")

            ' Refresh locales after user may have downloaded more packs
            _uiLocales = DiscoverUiLocales()

            ' Show Options dialog opened to Hardware panel
            WriteDebugLog("[FIRSTRUN] Opening Options → Hardware")
            Using opts As New FormOptions(_config, _uiLocales)
                opts.SelectCategory("hardware")
                opts.ShowDialog(Me)
                If opts.ConfigChanged Then
                    _langPack.LoadLanguage(_config.UiLanguage)
                    ApplyLocale()
                    ApplyTheme(_config.Theme)
                End If
            End Using
        End If
    End Sub

    Private Sub UpdateConfigPaths(toolsDir As String)
        Dim whisperDir = IO.Path.Combine(toolsDir, "whisper")
        _config.PathWhisper = IO.Path.Combine(whisperDir, "whisper-cli.exe")
        _config.PathYtdlp = IO.Path.Combine(toolsDir, "yt-dlp.exe")
        _config.PathFfmpeg = IO.Path.Combine(toolsDir, "ffmpeg.exe")
        _config.PathFfprobe = IO.Path.Combine(toolsDir, "ffprobe.exe")
        _config.PathModel = IO.Path.Combine(toolsDir, "ggml-large-v3.bin")
        _config.PathModelAudio = IO.Path.Combine(toolsDir, "ggml-large-v3.bin")
        _config.PathSubtitleEdit = IO.Path.Combine(toolsDir, "SubtitleEdit", "SubtitleEdit.exe")
        _config.PathOutputRoot = toolsDir
        Models.ConfigManager.Save(_config)
        LoadConfigToUi()
        _transcribeController?.PopulateModelDropdown()

        ' Re-enumerate audio devices
        Dim pythonPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
        btnLiveStart.Enabled = False
        Task.Run(Sub()
                     Try
                         Dim backend As New Services.Stt.FasterWhisperBackend()
                         Dim deviceInfos = backend.EnumerateDevicesAsync(pythonPath2)
                         Dim devices As New List(Of String)
                         For Each d In deviceInfos
                             devices.Add(d.ToString())
                         Next
                         cboLiveDevice.BeginInvoke(Sub()
                                                       _liveController.UpdateDeviceCombo(devices)
                                                       btnLiveStart.Enabled = True
                                                   End Sub)
                     Catch ex As Exception
                         WriteDebugLog($"[ERROR] Re-enumerate devices failed: {ex.Message}")
                         cboLiveDevice.BeginInvoke(Sub()
                                                       cboLiveDevice.Items.Clear()
                                                       cboLiveDevice.Items.Add("0: Default Device")
                                                       cboLiveDevice.SelectedIndex = 0
                                                       btnLiveStart.Enabled = True
                                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    Private Async Sub OpenDownloadManager()
        Try
            Dim biblesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles")
            Using dlg As New Forms.FormDownloadManager(_config, biblesDir)
                dlg.ShowDialog(Me)
                If dlg.PathsUpdated Then
                    UpdateConfigPaths(AppDomain.CurrentDomain.BaseDirectory)

                    ' Rescan Bibles so newly downloaded translations appear immediately
                    Dim bibleSvc = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                        GetType(Services.Interfaces.IBibleService)), Services.Interfaces.IBibleService)
                    bibleSvc?.RescanTranslations()

                    ' Refresh native Bible tab
                    RefreshBibleTab()

                    ' Refresh Bible dropdown on Live tab WebView
                    Dim refreshJs = "if(typeof refreshBibleDropdown==='function'){refreshBibleDropdown()}"
                    Try
                        If wvLiveClients?.CoreWebView2 IsNot Nothing Then
                            Await wvLiveClients.CoreWebView2.ExecuteScriptAsync(refreshJs)
                        End If
                    Catch : End Try
                End If
            End Using
        Catch ex As Exception
            WriteDebugLog($"[ERROR] OpenDownloadManager: {ex}")
            MessageBox.Show($"Error opening Download Manager: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' PopulateLanguageDropdowns and PopulateModelDropdown — delegated to TranscribeController

    ' (PopulateUiLanguageDropdown removed — managed by FormOptions)

#Region "Config <-> UI Binding"

    Private Sub LoadConfigToUi()
        _isSyncingUi = True
        Try
            LoadConfigToUiCore()
        Finally
            _isSyncingUi = False
        End Try
    End Sub

    Private Sub LoadConfigToUiCore()
        WriteDebugLog($"[CONFIG] LoadConfigToUiCore: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}")

        ' (Paths and Settings tabs removed — managed by FormOptions)

        ' Output formats
        chkSrt.Checked = _config.OutputSrt
        chkVtt.Checked = _config.OutputVtt
        chkTxt.Checked = _config.OutputTxt
        chkJson.Checked = _config.OutputJson
        chkCsv.Checked = _config.OutputCsv
        chkLrc.Checked = _config.OutputLrc

        ' Language on main tab
        SelectComboItem(cboInputLanguage, _config.Language)
        SelectComboItem(cboOutputLanguage, _config.OutputLanguage)

        ' Ensure subtitle color config defaults
        If String.IsNullOrEmpty(_config.SubtitleBgColor) OrElse Not _config.SubtitleBgColor.StartsWith("#") Then _config.SubtitleBgColor = "#000000"
        If String.IsNullOrEmpty(_config.SubtitleFgColor) OrElse Not _config.SubtitleFgColor.StartsWith("#") Then _config.SubtitleFgColor = "#FFFFFF"

        ' Live sliders
        Dim segVal = Math.Max(trkMaxSegment.Minimum, Math.Min(trkMaxSegment.Maximum, _config.LiveMaxSegmentSec))
        trkMaxSegment.Value = segVal
        lblMaxSegmentValue.Text = $"{segVal}s"

        Dim silVal = Math.Max(trkVadSilence.Minimum, Math.Min(trkVadSilence.Maximum, _config.LiveVadSilenceMs))
        trkVadSilence.Value = silVal
        lblVadSilenceValue.Text = $"{silVal}ms"
    End Sub

    Private Sub SaveUiToConfig()
        If _config Is Nothing OrElse _isInitializing OrElse _isSyncingUi Then Return

        ' Snapshot old values for change detection
        Dim oldValues As New Dictionary(Of String, String)
        For Each prop In GetType(AppConfig).GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
            If Not prop.CanRead Then Continue For
            Try
                Dim val = prop.GetValue(_config)
                oldValues(prop.Name) = If(val?.ToString(), "")
            Catch ex As Exception
                WriteDebugLog($"[ERROR] SaveUiToConfig (snapshot old): {ex.Message}")
            End Try
        Next

        ' (Paths, Settings, and Server config managed by FormOptions — not read from hidden controls)

        ' Output formats
        _config.OutputSrt = chkSrt.Checked
        _config.OutputVtt = chkVtt.Checked
        _config.OutputTxt = chkTxt.Checked
        _config.OutputJson = chkJson.Checked
        _config.OutputCsv = chkCsv.Checked
        _config.OutputLrc = chkLrc.Checked

        ' Language (sync both dropdowns)
        If cboInputLanguage.SelectedItem IsNot Nothing Then
            _config.Language = cboInputLanguage.SelectedItem.ToString()
        End If
        If cboOutputLanguage.SelectedItem IsNot Nothing Then
            _config.OutputLanguage = cboOutputLanguage.SelectedItem.ToString()
        End If
        WriteDebugLog($"[CONFIG] SaveUiToConfig: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}")

        ' Live sliders
        _config.LiveMaxSegmentSec = trkMaxSegment.Value
        _config.LiveVadSilenceMs = trkVadSilence.Value

        ' Log any changes before saving
        For Each prop In GetType(AppConfig).GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
            If Not prop.CanRead Then Continue For
            Try
                Dim newVal = If(prop.GetValue(_config)?.ToString(), "")
                Dim oldVal As String = ""
                oldValues.TryGetValue(prop.Name, oldVal)
                If Not String.Equals(oldVal, newVal, StringComparison.Ordinal) Then
                    Dim displayNew = If(prop.Name.IndexOf("Pin", StringComparison.OrdinalIgnoreCase) >= 0, "****", newVal)
                    WriteDebugLog($"[Config] {prop.Name}: {oldVal} -> {displayNew}")
                End If
            Catch ex As Exception
                WriteDebugLog($"[ERROR] SaveUiToConfig (change detection): {ex.Message}")
            End Try
        Next

        ConfigManager.Save(_config)
    End Sub

    Private Sub SelectComboItem(cbo As ComboBox, value As String)
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().Equals(value, StringComparison.OrdinalIgnoreCase) Then
                cbo.SelectedIndex = i
                Return
            End If
        Next
        If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
    End Sub


#End Region

#Region "Localization"

    Private Sub ApplyLocale()
        Try
            WriteDebugLog($"[LOCALE] ApplyLocale starting, lang={_langPack.CurrentLanguage}")
            tabPageJob.Text = GetString("Tab_Main")
            tabPageLive.Text = GetString("Tab_Live")
            tabPageHelp.Text = GetString("Tab_Help")

            lblMode.Text = GetString("Lbl_Mode")
            grpInput.Text = GetString("Grp_Input")
            lblUrl.Text = If(cboMode.SelectedIndex = 0, GetString("Lbl_AudioFile"), GetString("Lbl_Url"))
            btnBrowseFile.Text = If(cboMode.SelectedIndex = 0, GetString("Btn_BrowseAudio"), GetString("Btn_BrowseFile"))
            lblStartTime.Text = GetString("Lbl_StartTime")
            lblEndTime.Text = GetString("Lbl_EndTime")
            lblOutputDir.Text = GetString("Lbl_OutputDir")
            btnBrowseOutput.Text = GetString("Btn_BrowseOutput")
            lblInputLanguage.Text = GetString("Lbl_InputLanguage")
            lblOutputLanguage.Text = GetString("Lbl_OutputLanguage")
            lblModel.Text = GetString("Lbl_Model")

            grpOutputFormats.Text = GetString("Grp_OutputFormats")
            grpProgress.Text = GetString("Grp_Progress")
            btnStart.Text = GetString("Btn_Start")
            btnCancel.Text = GetString("Btn_Cancel")
            btnOpenOutput.Text = GetString("Btn_OpenOutputFolder")
            btnOpenSubtitleEdit.Text = GetString("Btn_SubtitleEdit")
            lnkPreviewSrt.Text = GetString("Lnk_PreviewSrt")


            ' Live tab
            grpLiveInput.Text = GetString("Grp_LiveInput")
            lblLiveDevice.Text = GetString("Lbl_LiveDevice")
            btnRefreshDevices.Text = GetString("Btn_RefreshDevices")
            lblLiveInputLang.Text = GetString("Lbl_LiveInputLang")
            btnEditFilters.Text = GetString("Btn_Filters")
            btnLiveStart.Text = GetString("Btn_LiveStart")
            btnLiveStop.Text = GetString("Btn_LiveStop")


            ' Subtitle Server tab
            If _transcribeController Is Nothing OrElse Not _transcribeController.IsRunning Then
                lblStepStatus.Text = GetString("Msg_Ready")
            End If

            ' Menus
            mnuFile.Text = GetString("Menu_File")
            mnuFileNewSession.Text = GetString("Menu_FileNewSession")
            mnuFileExportDiag.Text = GetString("Menu_FileExportDiag")
            mnuFileExit.Text = GetString("Menu_FileExit")
            mnuTools.Text = GetString("Menu_Tools")
            mnuToolsTranscribe.Text = GetString("Menu_ToolsTranscribe")
            mnuToolsTranslate.Text = GetString("Menu_ToolsTranslate")
            mnuToolsBible.Text = GetString("Menu_ToolsBible")
            mnuToolsGlossary.Text = GetString("Menu_ToolsGlossary")
            mnuToolsLocalization.Text = GetString("Menu_ToolsLocalization")
            mnuToolsDownloadMgr.Text = GetString("Menu_ToolsDownloadMgr")
            mnuToolsVerifyPaths.Text = GetString("Menu_ToolsVerifyPaths")
            mnuToolsVerifyIntegrity.Text = GetString("Menu_ToolsVerifyIntegrity")
            mnuToolsOptions.Text = GetString("Menu_ToolsOptions")
            mnuSession.Text = GetString("Menu_Session")
            mnuSessionStart.Text = GetString("Menu_SessionStart")
            mnuSessionStop.Text = GetString("Menu_SessionStop")
            mnuSessionQR.Text = GetString("Menu_SessionQR")
            mnuSessionCopyUrl.Text = GetString("Menu_SessionCopyUrl")
            mnuView.Text = GetString("Menu_View")
            mnuViewLogPanel.Text = If(_logPanelVisible, GetString("Menu_ViewHideLogPanel"), GetString("Menu_ViewLogPanel"))
            mnuViewTheme.Text = GetString("Menu_ViewTheme")
            mnuViewThemeSystem.Text = GetString("Menu_ViewThemeSystem")
            mnuViewThemeLight.Text = GetString("Menu_ViewThemeLight")
            mnuViewThemeDark.Text = GetString("Menu_ViewThemeDark")
            mnuViewClients.Text = GetString("Menu_ViewClients")
            mnuViewFullScreen.Text = GetString("Menu_ViewFullScreen")
            mnuHelpMenu.Text = GetString("Menu_Help")
            mnuHelpQuickStart.Text = GetString("Menu_HelpQuickStart")
            mnuHelpShortcuts.Text = GetString("Menu_HelpShortcuts")
            mnuHelpHardware.Text = GetString("Menu_HelpHardware")
            mnuHelpSpecSheet.Text = GetString("Menu_HelpSpecSheet")
            mnuHelpUpdates.Text = GetString("Menu_HelpUpdates")
            mnuHelpAbout.Text = GetString("Menu_HelpAbout")

            ' Nav rail buttons
            btnNavLive.Text = GetString("Nav_Live")
            btnNavTranscribe.Text = GetString("Nav_Transcribe")
            btnNavTranslate.Text = GetString("Nav_Translate")
            btnNavBible.Text = GetString("Nav_Bible")
            WriteDebugLog($"[LOCALE] ApplyLocale complete: Nav_Live={btnNavLive.Text}, Menu_File={mnuFile.Text}")
        Catch ex As Exception
            WriteDebugLog($"[ERROR] ApplyLocale: {ex.Message}")
        End Try
    End Sub

    Private Sub ApplyToolTips()
        tipMain.SetToolTip(txtUrl, GetString("Tip_Url"))
        tipMain.SetToolTip(cboInputLanguage, GetString("Tip_InputLanguage"))
        tipMain.SetToolTip(cboOutputLanguage, GetString("Tip_OutputLanguage"))
    End Sub

    Private Function GetString(key As String) As String
        Return _langPack.GetString(key)
    End Function

#End Region

#Region "Help"

    Private Sub LoadHelpContent(langCode As String)
        Try
            Dim appDir = AppDomain.CurrentDomain.BaseDirectory
            Dim helpDir = Path.Combine(appDir, "Help")
            Dim helpFile = Path.Combine(helpDir, $"help.{langCode}.rtf")

            ' Fall back to English if locale-specific help not found
            If Not File.Exists(helpFile) Then
                helpFile = Path.Combine(helpDir, "help.en.rtf")
            End If

            If File.Exists(helpFile) Then
                rtbHelp.LoadFile(helpFile, RichTextBoxStreamType.RichText)
            Else
                rtbHelp.Text = GetString("Main_HelpNotFound")
            End If
        Catch ex As Exception
            rtbHelp.Text = String.Format(GetString("Main_HelpLoadError"), ex.Message)
        End Try
    End Sub

#End Region

    ' Mode Switching, Pipeline Execution, Logging, Browse Buttons — delegated to TranscribeController

#Region "Theme"

    Private Sub ApplyTheme(theme As Models.ThemeMode)
        WriteDebugLog($"[THEME] ApplyTheme called with theme=""{theme}""")
        Dim backColor, foreColor, controlBack As Drawing.Color

        Dim panelBack, buttonBack, borderColor As Drawing.Color

        Select Case theme
            Case Models.ThemeMode.Dark
                backColor = Drawing.Color.FromArgb(30, 30, 30)       ' main background
                foreColor = Drawing.Color.FromArgb(220, 220, 220)    ' text
                controlBack = Drawing.Color.FromArgb(60, 63, 65)     ' input fields — noticeably lighter
                panelBack = Drawing.Color.FromArgb(37, 37, 38)       ' panels/groups — subtle depth
                buttonBack = Drawing.Color.FromArgb(55, 55, 58)      ' buttons — distinct from panels
                borderColor = Drawing.Color.FromArgb(62, 62, 66)     ' subtle borders
            Case Models.ThemeMode.Light
                backColor = Drawing.Color.White
                foreColor = Drawing.Color.Black
                controlBack = Drawing.Color.White
                panelBack = Drawing.Color.FromArgb(245, 245, 245)
                buttonBack = Drawing.SystemColors.Control
                borderColor = Drawing.Color.FromArgb(204, 204, 204)
            Case Else ' System
                backColor = Drawing.SystemColors.Control
                foreColor = Drawing.SystemColors.ControlText
                controlBack = Drawing.SystemColors.Window
                panelBack = Drawing.SystemColors.Control
                buttonBack = Drawing.SystemColors.Control
                borderColor = Drawing.SystemColors.ControlDark
        End Select

        Me.BackColor = backColor
        Me.ForeColor = foreColor
        ApplyThemeToControls(Me, backColor, foreColor, controlBack, panelBack, buttonBack, borderColor)
        ApplyShellTheme(theme)
    End Sub

    Private Sub ApplyThemeToControls(parent As Control, backColor As Drawing.Color, foreColor As Drawing.Color,
                                       controlBack As Drawing.Color, panelBack As Drawing.Color,
                                       buttonBack As Drawing.Color, borderColor As Drawing.Color)
        For Each ctrl As Control In parent.Controls
            ' Skip nav rail — themed by ApplyShellTheme
            If ctrl Is tsNavBar Then Continue For

            ctrl.ForeColor = foreColor

            If TypeOf ctrl Is TextBox OrElse TypeOf ctrl Is MaskedTextBox OrElse
               TypeOf ctrl Is RichTextBox OrElse TypeOf ctrl Is ComboBox OrElse
               TypeOf ctrl Is NumericUpDown OrElse TypeOf ctrl Is ListBox Then
                ctrl.BackColor = controlBack
            ElseIf TypeOf ctrl Is TabControl Then
                ' Don't change tab control background
            ElseIf TypeOf ctrl Is Button Then
                ctrl.BackColor = buttonBack
                Dim btn = DirectCast(ctrl, Button)
                btn.FlatStyle = FlatStyle.Flat
                btn.FlatAppearance.BorderColor = borderColor
                btn.FlatAppearance.BorderSize = 1
            ElseIf TypeOf ctrl Is GroupBox Then
                ctrl.BackColor = panelBack
            ElseIf TypeOf ctrl Is Panel Then
                ctrl.BackColor = panelBack
            Else
                ctrl.BackColor = backColor
            End If

            If ctrl.HasChildren Then
                ApplyThemeToControls(ctrl, backColor, foreColor, controlBack, panelBack, buttonBack, borderColor)
            End If
        Next
    End Sub

#End Region

    ' Live workspace — delegated to LiveController

    Private _translationStarting As Boolean = False

    Private Sub StartTranslationService()
        If Not _config.TranslationEnabled Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: TranslationEnabled=False, skipping")
            Return
        End If

        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: already running, skipping")
            Return
        End If
        If _translationStarting Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: already starting, skipping")
            Return
        End If
        _translationStarting = True

        If _translationService IsNot Nothing Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: stopping existing service before creating new one")
            _translationService.Stop()
            _translationService = Nothing
        End If

        Try
            Dim logPath = GetPipelineLogPath()
            Dim ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            IO.File.AppendAllText(logPath, $"{Environment.NewLine}=== Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} v{ver.Major}.{ver.Minor}.{ver.Build} ==={Environment.NewLine}")
        Catch ex As Exception
            WriteDebugLog($"[ERROR] StartTranslationService (write session header): {ex.Message}")
        End Try

        _translationService = New TranslationService()
        AddHandler _translationService.StatusChanged, Sub(s, msg)
                                                          WriteDebugLog(msg)
                                                          If msg.Contains("model loaded") Then
                                                              _liveController?.FlushPendingCommits()
                                                          End If
                                                      End Sub

        ' Register NllbBackend with the TranslationOrchestrator so conversation rooms
        ' (and any other ITranslationService consumer) can use NLLB for translation
        Try
            Dim orchestrator = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
            If orchestrator IsNot Nothing Then
                Dim nllbBackend As New Services.Translation.NllbBackend(_translationService)
                orchestrator.RegisterBackend(nllbBackend)
                WriteDebugLog("[TRANSLATE] NllbBackend registered with TranslationOrchestrator")
            Else
                WriteDebugLog("[TRANSLATE] Warning: TranslationOrchestrator not available to register NllbBackend")
            End If
        Catch ex As Exception
            WriteDebugLog($"[ERROR] Failed to register NllbBackend: {ex.Message}")
        End Try

        Dim modelPath = _config.TranslationModelPath
        Dim port = _config.TranslationPort
        Dim device = _config.TranslationDevice
        Dim glossaryPath = _config.TranslationGlossaryPath

        WriteDebugLog($"[TRANSLATE] StartTranslationService: port={port}, device={device}, modelPath={modelPath}")
        _translationService.Start(port, modelPath, device, glossaryPath)
        _translationStarting = False
        WriteDebugLog("Translation service starting...")
    End Sub

    ''' <summary>
    ''' Called by ConversationAudioHandler when a conversation room needs translation
    ''' but no backend is available. Checks NLLB deps and starts the service.
    ''' </summary>
    Private Sub EnsureNllbForRooms()
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            ' Already running — just make sure NllbBackend is registered
            Try
                Dim orchestrator = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                    GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
                If orchestrator IsNot Nothing Then
                    Dim backends = orchestrator.GetAllBackends()
                    Dim nllbRegistered = backends.Any(Function(b) b.Name.Equals("NLLB", StringComparison.OrdinalIgnoreCase))
                    If Not nllbRegistered Then
                        Dim nllbBackend As New Services.Translation.NllbBackend(_translationService)
                        orchestrator.RegisterBackend(nllbBackend)
                        WriteDebugLog("[ROOMS] Re-registered NllbBackend with orchestrator")
                    End If
                End If
            Catch ex As Exception
                WriteDebugLog($"[ERROR] EnsureNllbForRooms re-register: {ex.Message}")
            End Try
            Return
        End If

        WriteDebugLog("[ROOMS] Conversation room needs translation — starting NLLB service")

        Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
        If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
            WriteDebugLog("[ROOMS] NLLB dependencies not installed — translation unavailable for rooms")
            Return
        End If

        ' Force TranslationEnabled so StartTranslationService proceeds
        Dim wasEnabled = _config.TranslationEnabled
        _config.TranslationEnabled = True
        StartTranslationService()
        If Not wasEnabled Then _config.TranslationEnabled = wasEnabled
    End Sub

    Friend Shared Function GetPipelineLogPath() As String
        Return Services.Infrastructure.AppLogger.GetLogPath()
    End Function

    Friend Shared Sub WriteDebugLog(msg As String)
        Services.Infrastructure.AppLogger.Log(msg)
    End Sub

    ' Subtitle Server — delegated to ServerController

    Private Sub VerifyAllPaths()
        _serverController?.VerifyAllPaths()
    End Sub


    Private Sub KillOrphanedPythonProcesses()
        Try
            Dim pythonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed").ToLowerInvariant()
            For Each proc In Process.GetProcessesByName("python")
                Try
                    Dim exePath = proc.MainModule?.FileName
                    If exePath IsNot Nothing AndAlso exePath.ToLowerInvariant().StartsWith(pythonDir) Then
                        proc.Kill(True)
                        proc.WaitForExit(3000)
                    End If
                Catch ex As Exception
                    ' Access denied or already exited — ignore
                    WriteDebugLog($"[ERROR] KillOrphanedPythonProcesses (per-process): {ex.Message}")
                End Try
            Next
        Catch ex As Exception
            WriteDebugLog($"[ERROR] KillOrphanedPythonProcesses: {ex.Message}")
        End Try
    End Sub

    Private Sub FormMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Always save settings
        SaveUiToConfig()

        If Not _exitForReal AndAlso _config.MinimizeToTray Then
            ' Minimize to system tray instead of closing
            e.Cancel = True
            Me.Hide()
            Return
        End If
        _exitForReal = True

        ' Hide tray icon immediately so it looks closed
        Try : trayIcon.Visible = False : Catch : End Try
        Try : trayIcon.Dispose() : Catch : End Try

        ' Clean exit — stop all conference backends
        _liveController?.StopAllConferenceBackends()
        KillOrphanedPythonProcesses()
        Try : IO.File.Delete(Program.CrashSentinelPath) : Catch : End Try
        Environment.Exit(0)
    End Sub

    ' ColorToHex, EnsureFirewallRule, VerifyAllPaths — delegated to ServerController
End Class
