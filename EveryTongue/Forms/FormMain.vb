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
    Private _conferenceController As Controllers.ConferenceController
    Private _dictationService As Services.Input.DictationService
    Private _globalHotkeys As Services.Input.GlobalHotkeys
    Private _dictationController As Controllers.DictationController
    Private _langPack As LanguagePackService
    Private _translationService As TranslationService
    ''' <summary>
    ''' Pool of ADDITIONAL offline translation sidecars (one per distinct model+precision)
    ''' so rooms can run different offline models concurrently. The global-default model
    ''' stays the "Local" sidecar above; the pool only holds non-default models.
    ''' </summary>
    Private _translationPool As Services.Translation.TranslationSidecarPool
    ''' <summary>
    ''' The CTranslate2 compute_type the NLLB sidecar is currently loaded with.
    ''' Tracked alongside _config.TranslationModelPath because int8 and float16
    ''' variants of the same model size share a DefaultModelPath and differ only
    ''' by compute_type — a model-path-only check can't detect a precision switch.
    ''' </summary>
    Private _loadedTranslationComputeType As String = "auto"
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
    Private ReadOnly _sttLanguages As String() = {
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
        Dim flores = _langCodeService.ToFlores(code)
        If Not String.IsNullOrEmpty(flores) Then
            Dim name = _langCodeService.GetDisplayName(flores)
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
            AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"DiscoverUiLocales: {ex.Message}")
        End Try

        Return result.ToArray()
    End Function

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Services.Infrastructure.AppLogger.UiCallback = Sub(entry) AppendUnifiedLog(entry)
        Services.Infrastructure.AppLogger.OpenDownloadManager = AddressOf OpenDownloadManager
        _langPack = LanguagePackService.Instance

        ' Load config
        _config = ConfigManager.Load()

        ' Apply logs directory from config
        Services.Infrastructure.AppLogger.LogDirectory = If(_config.LogsDirectory, ".\logs")

        ' Apply log routing from config (or default to Normal preset)
        If _config.LogRouting IsNot Nothing AndAlso _config.LogRouting.Routes.Count > 0 Then
            Services.Infrastructure.AppLogger.Routing = _config.LogRouting
        Else
            _config.LogRouting = Services.Infrastructure.LogRoutingConfig.CreateNormal()
            Services.Infrastructure.AppLogger.Routing = _config.LogRouting
        End If

        AppLogger.Log(LogEvents.STARTUP_APP_STARTED, $"EveryTongue v{Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} starting (logs: {Services.Infrastructure.AppLogger.GetLogDir()})")
        AppLogger.Log(LogEvents.CONFIG_LOADED, $"Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}")
        AppLogger.Log(LogEvents.CONFIG_LOADED, $"SttBackend={_config.SttBackend}, WhisperModel={AppConfig.ResolvePath(_config.PathWhisperCppModel)}, TranslationBackend={_config.TranslationBackend}, TtsBackends={_config.TtsBackends}")

        ' First-run language picker — before anything else
        If Not _config.FirstRunComplete Then
            Using picker As New FormLanguagePicker()
                If picker.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrEmpty(picker.SelectedLanguage) Then
                    _config.UiLanguage = picker.SelectedLanguage
                    ConfigManager.Save(_config)

                    ' Download language pack if not English, then load it
                    If Not picker.SelectedLanguage.Equals("en", StringComparison.OrdinalIgnoreCase) Then
                        AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Downloading language pack: {picker.SelectedLanguage}")
                        Try
                            ' Use Task.Run to avoid deadlock on UI thread
                            Dim ok = Task.Run(Function() _langPack.DownloadLanguageAsync(picker.SelectedLanguage)).GetAwaiter().GetResult()
                            If ok Then
                                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Language pack downloaded: {picker.SelectedLanguage}")
                                _langPack.LoadLanguage(picker.SelectedLanguage)
                            Else
                                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Language pack not available: {picker.SelectedLanguage}")
                            End If
                        Catch ex As Exception
                            AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Language pack download failed: {ex.Message}")
                        End Try
                    End If

                    ' Refresh available locales after download
                    _uiLocales = DiscoverUiLocales()
                End If
            End Using
        End If

        ' Clean up old log files (keep last 30 days)
        Services.Infrastructure.AppLogger.CleanupOldLogFiles(30)

        ' Run file integrity check at startup (log only, non-blocking).
        ' Log the summary + failures ONLY: the full per-file report (~40 lines on
        ' one event id) trips the rate limiter and buries the [FAIL] line that
        ' matters. The full report stays available via Tools → Verify File
        ' Integrity and the diagnostics export.
        Task.Run(Sub()
                     Try
                         Dim result = Services.Infrastructure.IntegrityChecker.Check()
                         If Not result.ManifestFound Then
                             AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK,
                                 "Integrity: checksums.json not found — cannot verify files")
                             Return
                         End If
                         AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK,
                             $"Integrity: manifest {result.ManifestVersion} ({result.ManifestGenerated}) — {result.PassCount} pass, {result.FailCount} fail, {result.MissingCount} missing")
                         For Each f In result.Files
                             Select Case f.Status
                                 Case Services.Infrastructure.IntegrityChecker.FileStatus.Fail
                                     AppLogger.Log(LogEvents.STARTUP_INTEGRITY_FAIL,
                                         $"[FAIL] {f.RelativePath} (expected {f.ExpectedHash?.Substring(0, 12)}…, actual {f.ActualHash?.Substring(0, 12)}…, size {f.ExpectedSize}→{f.ActualSize})")
                                 Case Services.Infrastructure.IntegrityChecker.FileStatus.Missing
                                     AppLogger.Log(LogEvents.STARTUP_INTEGRITY_FAIL,
                                         $"[MISSING] {f.RelativePath}")
                             End Select
                         Next
                     Catch ex As Exception
                         AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK, $"Check failed: {ex.Message}")
                     End Try
                 End Sub)

        ' Create transcribe controller
        _transcribeController = New Controllers.TranscribeController(
            _config, cboMode, cboModel, cboInputLanguage, cboOutputLanguage,
            txtUrl, txtOutputDir, btnStart, btnResume, btnCancel,
            btnBrowseFile, btnBrowseOutput, btnOpenOutput, btnOpenSubtitleEdit,
            lnkPreviewSrt, lblStepStatus, lblUrl,
            lblInputLanguage, lblOutputLanguage, lblModel,
            lblStartTime, lblEndTime, lblStartColon1, lblStartColon2, lblEndColon1, lblEndColon2,
            txtStartHH, txtStartMM, txtStartSS, txtEndHH, txtEndMM, txtEndSS,
            pbOverall, pbChunk, grpOutputFormats, tabMain, tabPageJob,
            _sttLanguages,
            AddressOf LangDisplayName,
            AddressOf LangCodeFromDisplay,
            AddressOf SaveUiToConfig,
            AddressOf ShowLogPanel,
            Sub(source, msg, clr) AppendUnifiedLog(New Services.Infrastructure.LogEntry With {
                .Time = DateTime.Now, .Category = Services.Infrastructure.LogCategory.Pipeline,
                .Level = Services.Infrastructure.LogSeverity.Info, .Source = source, .Message = msg, .Color = clr
            }),
            AddressOf GetString,
            Sub(m) Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogCategory.Pipeline, Services.Infrastructure.LogSeverity.Info, m),
            Sub(msg, title, icon) MessageBox.Show(Me, msg, title, MessageBoxButtons.OK, icon),
            Function() _serverController?.GetTranslationOrchestrator())
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

        ' Create server controller
        _serverController = New Controllers.ServerController(
            _config,
            AddressOf UpdateShellStatus,
            Sub(m) Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogCategory.Server, Services.Infrastructure.LogSeverity.Info, m),
            Sub(msg, title, icon) MessageBox.Show(Me, msg, title, MessageBoxButtons.OK, icon))

        ' Dictation: system-wide voice typing. We're in Form Load, so the window handle
        ' exists (RegisterHotKey needs a valid HWND).
        _dictationService = New Services.Input.DictationService(
            _config,
            Function() _serverController?.GetTranslationOrchestrator(),
            AddressOf StartTranslationService,
            Sub(id, msg) AppLogger.Log(id, msg))
        _globalHotkeys = New Services.Input.GlobalHotkeys(Me.Handle)
        _dictationController = New Controllers.DictationController(
            _config, Me, _dictationService, _globalHotkeys, trayMenuDictation,
            AddressOf GetString,
            Function(flores) _langCodeService.GetDisplayName(flores),
            Sub() Models.ConfigManager.Save(_config),
            Sub(title, text) trayIcon.ShowBalloonTip(3000, title, text, ToolTipIcon.Info))
        _dictationController.WireEvents()
        _dictationController.ApplyHotkeys()

        ' Run path verification at startup (log only, non-blocking)
        Dim sc = _serverController
        Task.Run(Sub()
                     Try
                         sc.VerifyAllPathsCore()
                     Catch ex As Exception
                         AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK, $"VerifyPaths check failed: {ex.Message}")
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
        AddHandler trayMenuQR.Click, Sub(s, ev) ShowQrCode()
        AddHandler trayMenuBrowser.Click, Sub(s, ev)
            If _serverController Is Nothing OrElse _serverController.Port = 0 Then Return
            Dim localIp = Controllers.ServerController.GetLocalIpAddress()
            Dim url = $"https://{localIp}:{_serverController.Port + 1}"
            Try
                Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
            Catch ex As Exception
                AppLogger.Log(LogEvents.UI_ERROR, $"Failed to open browser: {ex.Message}")
            End Try
        End Sub
        AddHandler trayMenuExit.Click, Sub(s, ev) ExitApplication()

        ' Apply saved startup preference (first-run setup happens after dependency download)
        If _config.FirstRunComplete Then
            If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()
        End If

        ' Start minimized to tray if configured and not first-run
        If _config.FirstRunComplete AndAlso _config.StartMinimized Then
            Me.WindowState = FormWindowState.Minimized
            Me.ShowInTaskbar = False
        End If

        ' Build shell chrome (menu, toolbar, nav rail, status bar)
        InitializeShell()

        ' Apply theme (must be after InitializeShell so nav bar gets correct colours)
        ApplyTheme(_config.Theme)

        ' Auto-start subtitle server after form is fully shown
        AddHandler Me.Shown, Sub(s, ev)
                                  ' Clean up orphaned sidecars (python / whisper-server) left by a prior
                                  ' crash BEFORE starting the server or warming engines — they may still
                                  ' hold the GPU/mic. The single-instance lock guarantees these are orphans.
                                  Dim orphansKilled = KillOrphanedSidecars()
                                  AppLogger.Log(LogEvents.STARTUP_APP_STARTED, $"Orphaned-sidecar cleanup on startup: killed {orphansKilled} process(es)")

                                  _serverController.StartServer()

                                  ' Create and wire conference controller for template-based rooms
                                  _conferenceController = New Controllers.ConferenceController(
                                      _config,
                                      Function() SubtitleSvc,
                                      Function() _translationService,
                                      Function() TryCast(_serverController?.KestrelHost?.Services?.GetService(
                                          GetType(Services.Interfaces.ITranslationService)), Services.Interfaces.ITranslationService),
                                      Function() _serverController.GetRoomManager(),
                                      Function(roomId, engineKey) AcquireRoomTranslationBackend(roomId, engineKey),
                                      Sub(roomId) ReleaseRoomTranslationBackend(roomId),
                                      TryCast(_serverController?.KestrelHost?.Services?.GetService(
                                          GetType(Services.Rooms.RoomReadinessNotifier)), Services.Rooms.RoomReadinessNotifier),
                                      Sub(m) Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogCategory.Conference, Services.Infrastructure.LogSeverity.Info, m),
                                      Me)
                                  _conferenceController.WireEndpointHandlers()

                                  ' Wire conversation room translation callback
                                  Dim convAudioHandler = TryCast(_serverController.KestrelHost?.Services?.GetService(
                                      GetType(Services.Rooms.ConversationAudioHandler)), Services.Rooms.ConversationAudioHandler)
                                  If convAudioHandler IsNot Nothing Then
                                      convAudioHandler.EnsureTranslationAvailable = Sub() Me.BeginInvoke(Sub() EnsureTranslationForRooms())
                                      ' Per-room translation engine for conversation rooms (same pool as conference;
                                      ' release happens via the room-closed handler → StopConferenceBackend).
                                      convAudioHandler.AcquireTranslationBackend = Function(roomId, engineKey) AcquireRoomTranslationBackend(roomId, engineKey)
                                  End If

                                  ' Lazy engine start: conversation/conference rooms start STT and
                                  ' translation on first use (ConversationAudioHandler.EnsureLiveServerAsync
                                  ' is called in the audio path, and EnsureTranslationAvailable is invoked
                                  ' when a room needs translation but no backend is loaded). A
                                  ' Speechmatics-only session must NOT auto-start whisper-cpp at launch,
                                  ' which wastes VRAM and can crash on small GPUs.
                                  AppLogger.Log(LogEvents.STARTUP_APP_STARTED, "Conversation-rooms STT and translation will start on first use (no eager warm-up)")
                                  trayIcon.Text = GetString("Tray_Ready")

                                  InitBibleTab()
                              End Sub
    End Sub


    Private Sub RegisterStartup()
        Try
            Using key = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
                key?.SetValue("EveryTongue", $"""{Application.ExecutablePath}""")
            End Using
        Catch ex As Exception
            AppLogger.Log(LogEvents.CONFIG_SAVE_FAILED, $"RegisterStartup failed: {ex.Message}")
        End Try
    End Sub

    Private Sub UnregisterStartup()
        Try
            Using key = Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
                key?.DeleteValue("EveryTongue", False)
            End Using
        Catch ex As Exception
            AppLogger.Log(LogEvents.CONFIG_SAVE_FAILED, $"UnregisterStartup failed: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowFromTray()
        Me.Show()
        Me.ShowInTaskbar = True
        Me.WindowState = FormWindowState.Maximized
        Me.Activate()
    End Sub

    Private Sub ExitApplication()
        _exitForReal = True
        Me.Close()
    End Sub

    Private Async Sub CheckForUpdatesAsync()
        AppLogger.Log(LogEvents.UPDATE_CHECK, "CheckForUpdatesAsync starting...")
        Dim update = Await Models.UpdateChecker.CheckForUpdateAsync()
        If update Is Nothing Then
            AppLogger.Log(LogEvents.UPDATE_CHECK, "No update available")
            Return
        End If
        AppLogger.Log(LogEvents.UPDATE_AVAILABLE, $"Update found: {update.TagName} — showing MessageBox")

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
                    AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"Missing tool: {tool.Name}")
                Next
            End If

        Catch ex As Exception
            If manualCheck Then
                AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK, $"CheckDependenciesAsync: {ex.Message}")
            End If
        End Try

        ' First run: Download Manager (Language Packs tab) → Options Hardware
        If isFirstRun Then
            AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, "Setting FirstRunComplete=True")
            _config.FirstRunComplete = True

            ' Auto-detect best STT backend based on hardware (CUDA → Vulkan → CPU)
            Try
                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, "Running hardware scan for STT auto-detection...")
                Dim hwInfo = Services.Infrastructure.HardwareScanner.Scan()
                Dim suggestedBackend = Services.Infrastructure.HardwareScanner.SuggestSttBackend(hwInfo)
                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Hardware: GPU={hwInfo.GpuName}, CUDA={hwInfo.HasCuda}, Vulkan={hwInfo.HasVulkan}, Suggested STT={suggestedBackend}")
                If String.IsNullOrEmpty(_config.SttBackend) OrElse _config.SttBackend = Services.Stt.SttBackendRegistry.GetAll()(0).Key Then
                    _config.SttBackend = suggestedBackend
                    AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Set SttBackend={suggestedBackend}")
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Hardware auto-detection failed: {ex.Message}")
            End Try

            ConfigManager.Save(_config)

            ' Show Download Manager opened to Language Packs tab
            AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, "Opening Download Manager → Language Packs")
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
                AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, $"Download Manager error: {ex.Message}")
            End Try
            AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, "Download Manager closed")

            ' Refresh locales after user may have downloaded more packs
            _uiLocales = DiscoverUiLocales()

            ' Show Options dialog opened to Hardware panel
            AppLogger.Log(LogEvents.STARTUP_FIRST_RUN, "Opening Options → Hardware")
            Using opts As New FormOptions(_config, _uiLocales)
                opts.SelectCategory("hardware")
                opts.ShowDialog(Me)
                If opts.ConfigChanged Then
                    _langPack.LoadLanguage(_config.UiLanguage)
                    ApplyLocale()
                    ApplyTheme(_config.Theme)
                    _dictationController?.ReapplySettings()
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
    End Sub

    Private Sub OpenDownloadManager()
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
                End If
            End Using
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"OpenDownloadManager: {ex}")
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
        AppLogger.Log(LogEvents.CONFIG_LOADED, $"LoadConfigToUiCore: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}")

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
                AppLogger.Log(LogEvents.CONFIG_SAVE_FAILED, $"SaveUiToConfig (snapshot old): {ex.Message}")
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

        ' Language (sync both dropdowns — extract code from display name)
        If cboInputLanguage.SelectedItem IsNot Nothing Then
            _config.Language = LangCodeFromDisplay(cboInputLanguage.SelectedItem.ToString())
        End If
        If cboOutputLanguage.SelectedItem IsNot Nothing Then
            _config.OutputLanguage = LangCodeFromDisplay(cboOutputLanguage.SelectedItem.ToString())
        End If
        AppLogger.Log(LogEvents.CONFIG_SAVED, $"SaveUiToConfig: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}")

        ' Log any changes before saving
        For Each prop In GetType(AppConfig).GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
            If Not prop.CanRead Then Continue For
            Try
                Dim newVal = If(prop.GetValue(_config)?.ToString(), "")
                Dim oldVal As String = ""
                oldValues.TryGetValue(prop.Name, oldVal)
                If Not String.Equals(oldVal, newVal, StringComparison.Ordinal) Then
                    Dim displayNew = If(prop.Name.IndexOf("Pin", StringComparison.OrdinalIgnoreCase) >= 0, "****", newVal)
                    AppLogger.Log(LogEvents.CONFIG_SAVED, $"{prop.Name}: {oldVal} -> {displayNew}")
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_SAVE_FAILED, $"SaveUiToConfig (change detection): {ex.Message}")
            End Try
        Next

        ConfigManager.Save(_config)
    End Sub

    Private Sub SelectComboItem(cbo As ComboBox, value As String)
        If String.IsNullOrEmpty(value) Then
            If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
            Return
        End If
        ' Try exact match first (handles display names stored directly)
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().Equals(value, StringComparison.OrdinalIgnoreCase) Then
                cbo.SelectedIndex = i
                Return
            End If
        Next
        ' Try matching by code suffix, e.g. "English (en)" contains "(en)"
        Dim suffix = $"({value})"
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().EndsWith(suffix, StringComparison.OrdinalIgnoreCase) Then
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
            AppLogger.Log(LogEvents.LOCALE_LOADED, $"ApplyLocale starting, lang={_langPack.CurrentLanguage}")
            tabPageJob.Text = GetString("Tab_Main")
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


            ' Status
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
            mnuToolsLogConfig.Text = GetString("Menu_ToolsLogConfig")
            mnuToolsLogViewer.Text = GetString("Menu_ToolsLogViewer")
            mnuToolsOptions.Text = GetString("Menu_ToolsOptions")
            mnuSession.Text = GetString("Menu_Session")
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
            btnNavLog.Text = GetString("Nav_Log")
            btnNavTranscribe.Text = GetString("Nav_Transcribe")
            btnNavTranslate.Text = GetString("Nav_Translate")
            btnNavBible.Text = GetString("Nav_Bible")

            ' Translate workspace TTS + Bible verse translation controls
            btnTransSpeak.Text = GetString("Trans_Speak")
            lblBibleTransTo.Text = GetString("Bible_TranslateTo")
            btnBibleSpeak.Text = GetString("Bible_ReadAloud")
            AppLogger.Log(LogEvents.LOCALE_LOADED, $"ApplyLocale complete: Nav_Transcribe={btnNavTranscribe.Text}, Menu_File={mnuFile.Text}")
        Catch ex As Exception
            AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"ApplyLocale: {ex.Message}")
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
        AppLogger.Log(LogEvents.UI_THEME_CHANGED, $"ApplyTheme called with theme=""{theme}""")
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

    Private _translationStarting As Boolean = False

    Private Function GetTranslationPool() As Services.Translation.TranslationSidecarPool
        If _translationPool IsNot Nothing Then Return _translationPool
        Dim orch = TryCast(_serverController?.KestrelHost?.Services?.GetService(
            GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
        If orch Is Nothing Then Return Nothing
        _translationPool = New Services.Translation.TranslationSidecarPool(orch, _config)
        Return _translationPool
    End Function

    ''' <summary>
    ''' Resolve + ensure the translation backend for a room's engine key and return the
    ''' orchestrator backend NAME to use as backendOverride. Cloud → its backend name;
    ''' inline/unknown → "" (global default); offline matching the global default →
    ''' "Local" (shared default sidecar); any other offline model → a pooled per-model
    ''' sidecar. Reference-counted by roomId; pair with ReleaseRoomTranslationBackend.
    ''' </summary>
    Private Function AcquireRoomTranslationBackend(roomId As String, engineKey As String) As String
        Dim entry = Services.Translation.TranslationBackendRegistry.Find(engineKey)
        If entry Is Nothing OrElse Not String.IsNullOrEmpty(entry.InlineWithStt) Then Return ""
        If String.IsNullOrEmpty(entry.ModelType) Then Return entry.BackendName   ' cloud — configured at server start

        Dim compute = Services.Translation.TranslationBackendRegistry.ComputeTypeForKey(engineKey)
        Dim modelPath = entry.DefaultModelPath

        ' Matches the configured GLOBAL DEFAULT offline model? Share the "Local" sidecar.
        Dim defKey = Services.Translation.TranslationBackendRegistry.ResolveEffectiveBackendKey(_config)
        Dim defEntry = Services.Translation.TranslationBackendRegistry.Find(defKey)
        If defEntry IsNot Nothing AndAlso Not String.IsNullOrEmpty(defEntry.ModelType) Then
            Dim defModel = If(String.IsNullOrEmpty(_config.TranslationModelPath), defEntry.DefaultModelPath, _config.TranslationModelPath)
            Dim defCompute = Services.Translation.TranslationBackendRegistry.ComputeTypeForKey(defKey)
            If Services.Translation.TranslationSidecarPool.Signature(modelPath, compute).Equals(
                   Services.Translation.TranslationSidecarPool.Signature(defModel, defCompute), StringComparison.OrdinalIgnoreCase) Then
                EnsureDefaultTranslationRunning()
                Return "Local"
            End If
        End If

        ' Non-default offline model → its own pooled sidecar.
        Dim resolved = AppConfig.ResolvePath(modelPath)
        If Not IO.Directory.Exists(resolved) Then
            AppLogger.Log(LogEvents.TRANS_ERROR, $"Room engine '{engineKey}' model folder not found ({resolved}) — using the default translation engine instead")
            EnsureDefaultTranslationRunning()
            Return "Local"
        End If
        Dim pool = GetTranslationPool()
        If pool Is Nothing Then
            EnsureDefaultTranslationRunning()
            Return "Local"
        End If
        Return pool.Acquire(roomId, engineKey, modelPath, entry.ModelType, compute)
    End Function

    Private Sub ReleaseRoomTranslationBackend(roomId As String)
        _translationPool?.Release(roomId)
    End Sub

    ''' <summary>Start the global-default ("Local") translation sidecar if it isn't already running.</summary>
    Private Sub EnsureDefaultTranslationRunning()
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            EnsureSidecarBackendRegistered()
            Return
        End If
        Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
        If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then Return
        Dim wasEnabled = _config.TranslationEnabled
        _config.TranslationEnabled = True
        StartTranslationService()
        If Not wasEnabled Then _config.TranslationEnabled = wasEnabled
    End Sub

    ''' <summary>
    ''' Ensure the NLLB sidecar is running for a specific translation engine KEY
    ''' (a conference room's own engine), independent of the global Options engine.
    ''' Only meaningful for local/NLLB engines (registry ModelType non-empty);
    ''' cloud engines are configured at server start and ignored here.
    '''
    ''' CONSTRAINT: the sidecar holds ONE NLLB model at a time. If it is already
    ''' running with a DIFFERENT model than this key requests, we log a single
    ''' warning and KEEP the running model (no reload) — two rooms on different
    ''' NLLB models share the first-loaded one. Cloud engines have no such limit.
    ''' </summary>
    Private Sub EnsureTranslationSidecarForKey(engineKey As String, mayReload As Boolean)
        Dim entry = Services.Translation.TranslationBackendRegistry.Find(engineKey)
        ' Cloud engine (or unknown key): no sidecar needed. Cloud engines are
        ' configured via ConfigureCloudApiKeys at server start; if unkeyed they
        ' fail at call time and fall back, which is acceptable.
        If entry Is Nothing OrElse String.IsNullOrEmpty(entry.ModelType) Then Return

        ' Already running: ensure the sidecar backend is registered, then handle a
        ' model mismatch. A conference room overrides the global default, so when
        ' it's safe (no other local room active) RELOAD the sidecar to this room's
        ' model; otherwise share the running model (a concurrent local room is using it).
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            EnsureSidecarBackendRegistered()
            Dim requested = AppConfig.ResolvePath(entry.DefaultModelPath)
            Dim loaded = AppConfig.ResolvePath(_config.TranslationModelPath)
            Dim modelDiffers = Not String.IsNullOrEmpty(requested) AndAlso
                               Not requested.Equals(loaded, StringComparison.OrdinalIgnoreCase)
            ' int8 and float16 variants of the same size share a DefaultModelPath,
            ' so a model-path-only check can't catch a needed precision switch.
            ' Compare the requested engine's compute_type to the loaded one too.
            Dim requestedCompute = Services.Translation.TranslationBackendRegistry.ComputeTypeForKey(engineKey)
            Dim computeDiffers = Not requestedCompute.Equals(_loadedTranslationComputeType, StringComparison.OrdinalIgnoreCase)
            If modelDiffers OrElse computeDiffers Then
                If mayReload Then
                    ' Room wins: reload the sidecar to this room's model/precision.
                    ' Costs a model-load (~seconds); the reloaded model then also
                    ' serves non-conference paths until the global engine changes.
                    AppLogger.Log(LogEvents.TRANS_SERVER_STARTING,
                        $"Reloading NLLB sidecar to room engine '{engineKey}' (model {entry.DefaultModelPath}, compute {requestedCompute}; was model '{_config.TranslationModelPath}', compute '{_loadedTranslationComputeType}')")
                    _translationService.Stop()
                    _translationService = Nothing
                    Dim wasEnabledR = _config.TranslationEnabled
                    _config.TranslationEnabled = True
                    StartTranslationService(engineKey)
                    If Not wasEnabledR Then _config.TranslationEnabled = wasEnabledR
                ElseIf modelDiffers Then
                    AppLogger.Log(LogEvents.TRANS_BACKEND_FALLBACK,
                        $"Room requested NLLB engine '{engineKey}' (model {entry.DefaultModelPath}), but another active room is using model '{_config.TranslationModelPath}'. Simultaneous different NLLB models aren't supported — using the running model.")
                Else
                    AppLogger.Log(LogEvents.TRANS_BACKEND_FALLBACK,
                        $"Room requested NLLB engine '{engineKey}' (compute {requestedCompute}), but another active room is using compute '{_loadedTranslationComputeType}' on the same model. Simultaneous different precisions aren't supported — using the running model.")
                End If
            End If
            Return
        End If

        If _translationStarting Then
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, $"EnsureTranslationSidecarForKey('{engineKey}'): already starting, skipping")
            Return
        End If

        ' Not running: verify deps, then start with the REQUESTED key's model.
        Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
        If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
            AppLogger.Log(LogEvents.TRANS_ERROR, $"Room requested NLLB engine '{engineKey}' but translation dependencies aren't installed — translation unavailable for this room")
            Return
        End If

        ' Force TranslationEnabled so StartTranslationService proceeds, and start
        ' the sidecar with the room engine's model (overriding the global config).
        Dim wasEnabled = _config.TranslationEnabled
        _config.TranslationEnabled = True
        StartTranslationService(engineKey)
        If Not wasEnabled Then _config.TranslationEnabled = wasEnabled
    End Sub

    ''' <summary>
    ''' Register SidecarTranslationBackend with the orchestrator if not already
    ''' present (mirrors the re-register logic in EnsureTranslationForRooms).
    ''' </summary>
    Private Sub EnsureSidecarBackendRegistered()
        Try
            Dim orchestrator = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
            If orchestrator Is Nothing OrElse _translationService Is Nothing Then Return
            Dim sidecarRegistered = orchestrator.GetAllBackends().Any(
                Function(b) b.Name.Equals("Local", StringComparison.OrdinalIgnoreCase))
            If Not sidecarRegistered Then
                orchestrator.RegisterBackend(New Services.Translation.SidecarTranslationBackend(_translationService))
                AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "Re-registered SidecarTranslationBackend with orchestrator")
            End If
        Catch ex As Exception
            AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"EnsureSidecarBackendRegistered: {ex.Message}")
        End Try
    End Sub

    ''' <param name="keyOverride">
    ''' When set, the sidecar starts with this translation engine KEY's model
    ''' (registry DefaultModelPath/ModelType) instead of the global config model —
    ''' used to start NLLB on demand for a conference room whose template selects a
    ''' local engine even when the GLOBAL engine is cloud. Nothing = use global config.
    ''' </param>
    Private Sub StartTranslationService(Optional keyOverride As String = Nothing)
        If Not _config.TranslationEnabled Then
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "StartTranslationService: TranslationEnabled=False, skipping")
            Return
        End If

        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "StartTranslationService: already running, skipping")
            Return
        End If
        If _translationStarting Then
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "StartTranslationService: already starting, skipping")
            Return
        End If
        _translationStarting = True

        If _translationService IsNot Nothing Then
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "StartTranslationService: stopping existing service before creating new one")
            _translationService.Stop()
            _translationService = Nothing
        End If

        ' Route the session header through AppLogger (which serialises writes under
        ' _fileLock). Writing to session.log directly here raced AppLogger's concurrent
        ' writes from other threads → "the process cannot access the file ... because it
        ' is being used by another process".
        Dim ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
        AppLogger.Log(LogEvents.TRANS_SERVER_STARTING,
            $"=== Translation session started v{ver.Major}.{ver.Minor}.{ver.Build} ===")

        _translationService = New TranslationService()
        AddHandler _translationService.StatusChanged, Sub(s, msg)
                                                          AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, msg)
                                                      End Sub

        ' Register SidecarTranslationBackend with the TranslationOrchestrator so conversation rooms
        ' (and any other ITranslationService consumer) can use the translation sidecar
        Try
            Dim orchestrator = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
            If orchestrator IsNot Nothing Then
                Dim sidecarBackend As New Services.Translation.SidecarTranslationBackend(_translationService)
                orchestrator.RegisterBackend(sidecarBackend)

                ' Apply the user's selected translation backend to the orchestrator
                ' When the STT engine declares a companion translation backend (shares
                ' the same API key) and the user has a key, auto-use that backend
                ' (much faster than waiting for the local NLLB model to load and translate)
                Dim configKey = Services.Translation.TranslationBackendRegistry.ResolveEffectiveBackendKey(_config)
                If Not configKey.Equals(If(_config.TranslationBackend, "nllb"), StringComparison.OrdinalIgnoreCase) Then
                    AppLogger.Log(LogEvents.TRANS_SERVER_READY, $"Auto-selecting companion translation backend '{configKey}' (STT backend is {If(_config.SttBackend, "")})")
                End If
                Dim entry = Services.Translation.TranslationBackendRegistry.Find(configKey)
                Dim orchestratorName = If(entry?.BackendName, "Local")
                orchestrator.SetActiveBackend(orchestratorName)
                AppLogger.Log(LogEvents.TRANS_SERVER_READY, $"SidecarTranslationBackend registered, active backend set to {orchestratorName} (config={configKey})")
            Else
                AppLogger.Log(LogEvents.TRANS_ERROR, "TranslationOrchestrator not available to register SidecarTranslationBackend")
            End If
        Catch ex As Exception
            AppLogger.Log(LogEvents.TRANS_ERROR, $"Failed to register SidecarTranslationBackend: {ex.Message}")
        End Try

        ' A room may request a SPECIFIC local engine (keyOverride) even when the
        ' global engine is cloud — in that case start the sidecar with the room
        ' engine's model. Otherwise: skip the NLLB sidecar when the global
        ' effective backend is cloud (no point loading a multi-GB model for an API).
        Dim overrideEntry = If(String.IsNullOrWhiteSpace(keyOverride), Nothing,
                               Services.Translation.TranslationBackendRegistry.Find(keyOverride))
        Dim overrideIsLocal = overrideEntry IsNot Nothing AndAlso Not String.IsNullOrEmpty(overrideEntry.ModelType)

        Dim effectiveKey = Services.Translation.TranslationBackendRegistry.ResolveEffectiveBackendKey(_config)
        Dim effectiveEntry = Services.Translation.TranslationBackendRegistry.Find(effectiveKey)
        Dim usingCloudTranslation = effectiveEntry IsNot Nothing AndAlso
                                    String.IsNullOrEmpty(effectiveEntry.ModelType)

        If usingCloudTranslation AndAlso Not overrideIsLocal Then
            AppLogger.Log(LogEvents.TRANS_SERVER_READY, "Skipping NLLB sidecar — using cloud translation backend")
            _translationStarting = False
        Else
            ' When a room override is set, use ITS model/modelType (registry metadata);
            ' otherwise use the global config model.
            Dim modelPath = If(overrideIsLocal, AppConfig.ResolvePath(overrideEntry.DefaultModelPath), _config.TranslationModelPath)
            Dim modelType = If(overrideIsLocal, overrideEntry.ModelType, If(_config.TranslationModelType, "nllb"))
            ' Derive the compute_type from the registry: a room override uses its own
            ' engine key; the global path uses the effective configured engine. int8
            ' variants carry "int8_float16" so the float16 model is quantized at load.
            Dim computeType = If(overrideIsLocal,
                Services.Translation.TranslationBackendRegistry.ComputeTypeForKey(keyOverride),
                Services.Translation.TranslationBackendRegistry.ComputeTypeForKey(effectiveKey))
            Dim port = _config.TranslationPort
            Dim device = _config.TranslationDevice
            Dim glossaryPath = _config.TranslationGlossaryPath

            ' Keep the loaded model + compute_type in sync so the single-model
            ' constraint check (EnsureTranslationSidecarForKey) reports the
            ' actually-loaded model and precision.
            If overrideIsLocal Then _config.TranslationModelPath = modelPath
            _loadedTranslationComputeType = computeType

            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, $"StartTranslationService: port={port}, device={device}, modelPath={modelPath}, modelType={modelType}, computeType={computeType}{If(overrideIsLocal, $" (room engine '{keyOverride}')", "")}")
            _translationService.Start(port, modelPath, device, glossaryPath, modelType, computeType)
            _translationStarting = False
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "Translation service starting...")
        End If
    End Sub

    ''' <summary>
    ''' Called by ConversationAudioHandler when a conversation room needs translation
    ''' but no backend is available. Checks translation deps and starts the service.
    ''' </summary>
    Private Sub EnsureTranslationForRooms()
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            ' Already running — just make sure SidecarTranslationBackend is registered
            Try
                Dim orchestrator = TryCast(_serverController?.KestrelHost?.Services?.GetService(
                    GetType(Services.Interfaces.ITranslationService)), Services.Translation.TranslationOrchestrator)
                If orchestrator IsNot Nothing Then
                    Dim backends = orchestrator.GetAllBackends()
                    Dim sidecarRegistered = backends.Any(Function(b) b.Name.Equals("Local", StringComparison.OrdinalIgnoreCase))
                    If Not sidecarRegistered Then
                        Dim sidecarBackend As New Services.Translation.SidecarTranslationBackend(_translationService)
                        orchestrator.RegisterBackend(sidecarBackend)
                        AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "Re-registered SidecarTranslationBackend with orchestrator")
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"EnsureTranslationForRooms re-register: {ex.Message}")
            End Try
            Return
        End If

        AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, "Conversation room needs translation — starting translation sidecar")

        Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
        If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
            AppLogger.Log(LogEvents.TRANS_ERROR, "Translation dependencies not installed — translation unavailable for rooms")
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

    ' Subtitle Server — delegated to ServerController

    Private Sub VerifyAllPaths()
        _serverController?.VerifyAllPaths()
    End Sub


    ''' <summary>
    ''' Kill orphaned sidecar processes left over from a prior crash: embedded python
    ''' (under python-embed) and whisper-server binaries (whisper-server / whisper-server-cuda
    ''' under the app base dir). All matching is path-scoped to our install so we never kill
    ''' an unrelated python or whisper-server from another application.
    ''' Safe to call on startup: the single-instance lock in Program.vb guarantees no other
    ''' EveryTongue GUI is running, so any such process under our dir is a genuine orphan.
    ''' Returns the number of processes killed.
    ''' </summary>
    Private Function KillOrphanedSidecars() As Integer
        Dim killed As Integer = 0
        Dim baseDir = AppDomain.CurrentDomain.BaseDirectory.ToLowerInvariant()
        Dim pythonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed").ToLowerInvariant()

        ' (process name, required path prefix) — name has no .exe per GetProcessesByName
        Dim targets = New(Name As String, PathPrefix As String)() {
            ("python", pythonDir),
            ("whisper-server", baseDir),
            ("whisper-server-cuda", baseDir)
        }

        For Each target In targets
            Try
                For Each proc In Process.GetProcessesByName(target.Name)
                    Try
                        Dim exePath = proc.MainModule?.FileName
                        If exePath IsNot Nothing AndAlso exePath.ToLowerInvariant().StartsWith(target.PathPrefix) Then
                            proc.Kill(True)
                            proc.WaitForExit(3000)
                            killed += 1
                        End If
                    Catch ex As Exception
                        ' Access denied or already exited — ignore
                        AppLogger.Log(LogEvents.PIPELINE_SIDECAR_ERROR, $"KillOrphanedSidecars (per-process {target.Name}): {ex.Message}")
                    End Try
                Next
            Catch ex As Exception
                AppLogger.Log(LogEvents.PIPELINE_SIDECAR_ERROR, $"KillOrphanedSidecars ({target.Name}): {ex.Message}")
            End Try
        Next

        Return killed
    End Function

    ''' <summary>Forward global-hotkey messages (WM_HOTKEY) to the dictation hotkey handler.</summary>
    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = &H312 Then   ' WM_HOTKEY
            Try : _globalHotkeys?.OnWmHotkey(m.WParam.ToInt32()) : Catch : End Try
        End If
        MyBase.WndProc(m)
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

        ' Stop dictation (disarm STT session, release hotkeys)
        Try : _dictationController?.Shutdown() : Catch : End Try

        ' Stop conference backends
        _conferenceController?.StopAllConferenceBackends()

        ' Persist any pending cloud translation usage before shutdown
        Try : Services.Translation.TranslationUsageTracker.Flush() : Catch : End Try

        ' Emit session summary before shutdown
        AppLogger.EmitSessionSummary()

        ' Clean exit
        KillOrphanedSidecars()
        Try : IO.File.Delete(Program.CrashSentinelPath) : Catch : End Try
        Environment.Exit(0)
    End Sub

    ' ColorToHex, EnsureFirewallRule, VerifyAllPaths — delegated to ServerController
End Class
