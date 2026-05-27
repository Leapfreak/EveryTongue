Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Resources
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
    Private _cts As CancellationTokenSource
    Private _isRunning As Boolean = False
    Private _currentOutputDir As String = ""
    Private _resMgr As ResourceManager
    Private _liveRunner As LiveStreamRunner
    Private _liveTranscript As New System.Text.StringBuilder()
    Private _kestrelHost As KestrelHost
    Private _serverPort As Integer = 0
    Private _translationService As TranslationService
    Private _translationUnloadTimer As System.Threading.Timer
    Private _pendingCommits As New List(Of String)()
    Private _isInitializing As Boolean = True
    Private _isSyncingUi As Boolean = False
    Private _exitForReal As Boolean = False

    ''' <summary>Resolves ISubtitleService from Kestrel DI. Returns Nothing if Kestrel isn't running.</summary>
    Private ReadOnly Property SubtitleSvc As Services.Interfaces.ISubtitleService
        Get
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ISubtitleService)), Services.Interfaces.ISubtitleService)
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

    Private ReadOnly _langNames As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
        {"auto", "Auto Detect"}, {"en", "English"}, {"es", "Spanish"}, {"fr", "French"},
        {"de", "German"}, {"it", "Italian"}, {"pt", "Portuguese"}, {"nl", "Dutch"},
        {"pl", "Polish"}, {"ru", "Russian"}, {"zh", "Chinese"}, {"ja", "Japanese"},
        {"ko", "Korean"}, {"ar", "Arabic"}, {"hi", "Hindi"}, {"tr", "Turkish"},
        {"vi", "Vietnamese"}, {"th", "Thai"}, {"cs", "Czech"}, {"el", "Greek"},
        {"hu", "Hungarian"}, {"ro", "Romanian"}, {"da", "Danish"}, {"fi", "Finnish"},
        {"no", "Norwegian"}, {"sv", "Swedish"}, {"sk", "Slovak"}, {"uk", "Ukrainian"},
        {"bg", "Bulgarian"}, {"hr", "Croatian"}, {"ca", "Catalan"}, {"cy", "Welsh"},
        {"et", "Estonian"}, {"ga", "Irish"}, {"lv", "Latvian"}, {"lt", "Lithuanian"},
        {"mt", "Maltese"}, {"sl", "Slovenian"}, {"sq", "Albanian"}, {"mk", "Macedonian"},
        {"sr", "Serbian"}, {"bs", "Bosnian"}, {"is", "Icelandic"}, {"ms", "Malay"},
        {"sw", "Swahili"}, {"tl", "Tagalog"}, {"ta", "Tamil"}, {"te", "Telugu"},
        {"ml", "Malayalam"}, {"si", "Sinhala"}, {"bn", "Bengali"}, {"gu", "Gujarati"},
        {"kn", "Kannada"}, {"mr", "Marathi"}, {"ne", "Nepali"}, {"pa", "Punjabi"},
        {"ur", "Urdu"}, {"my", "Myanmar"}, {"lo", "Lao"}, {"km", "Khmer"},
        {"he", "Hebrew"}, {"fa", "Persian"}, {"id", "Indonesian"}, {"jw", "Javanese"},
        {"la", "Latin"}, {"mn", "Mongolian"}, {"ps", "Pashto"}, {"sd", "Sindhi"},
        {"sn", "Shona"}, {"so", "Somali"}, {"su", "Sundanese"}, {"tg", "Tajik"},
        {"tt", "Tatar"}, {"uz", "Uzbek"}, {"yo", "Yoruba"}, {"af", "Afrikaans"},
        {"am", "Amharic"}, {"as", "Assamese"}, {"az", "Azerbaijani"}, {"ba", "Bashkir"},
        {"be", "Belarusian"}, {"br", "Breton"}, {"fo", "Faroese"}, {"gl", "Galician"},
        {"ha", "Hausa"}, {"ht", "Haitian Creole"}, {"hy", "Armenian"}, {"ka", "Georgian"},
        {"kk", "Kazakh"}, {"lb", "Luxembourgish"}, {"ln", "Lingala"}, {"mg", "Malagasy"},
        {"mi", "Maori"}, {"nn", "Nynorsk"}, {"oc", "Occitan"}, {"sa", "Sanskrit"},
        {"tk", "Turkmen"}, {"wo", "Wolof"}, {"yi", "Yiddish"}, {"yue", "Cantonese"}
    }

    Private Function LangDisplayName(code As String) As String
        Dim name As String = Nothing
        If _langNames.TryGetValue(code, name) Then Return $"{name} ({code})"
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

    ' Available UI locales with native names
    Private ReadOnly _uiLocales As (Code As String, Name As String)() = {
        ("en", "English"),
        ("es", "Espanol"),
        ("fr", "Francais"),
        ("de", "Deutsch"),
        ("ca", "Catala"),
        ("pt", "Portugues"),
        ("zh-Hans", "中文(简体)"),
        ("ja", "日本語")
    }

    Private Sub FormMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        _resMgr = New ResourceManager("EveryTongue.Strings", GetType(FormMain).Assembly)

        ' Load config
        _config = ConfigManager.Load()
        WriteDebugLog($"[STARTUP] Config loaded: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}")

        ' Populate dropdowns
        PopulateLanguageDropdowns()

        ' Bind config to UI
        LoadConfigToUi()
        _isInitializing = False

        ' Set default output directory
        If String.IsNullOrWhiteSpace(txtOutputDir.Text) Then
            Dim stamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")
            txtOutputDir.Text = Path.Combine(AppConfig.ResolvePath(_config.PathOutputRoot), stamp)
        End If

        ' Apply locale
        Dim culture = New CultureInfo(_config.UiLanguage)
        Thread.CurrentThread.CurrentUICulture = culture
        ApplyLocale()

        ' Populate model dropdown
        PopulateModelDropdown()

        ' Set browse button text for YouTube mode on startup
        btnBrowseFile.Text = GetString("Btn_BrowseFile")

        ' Restrict time boxes to digits only
        For Each tb In {txtStartHH, txtStartMM, txtStartSS, txtEndHH, txtEndMM, txtEndSS}
            AddHandler tb.KeyPress, AddressOf TimeBox_KeyPress
        Next

        ' Apply tooltips
        ApplyToolTips()

        ' Load help content
        LoadHelpContent(_config.UiLanguage)

        ' Initialize live translation tab
        PopulateLiveLanguageDropdowns()
        cboLiveDevice.Items.Add("Detecting devices...")
        cboLiveDevice.SelectedIndex = 0
        btnLiveStart.Enabled = False

        ' Enumerate audio devices in the background
        Dim pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
        Task.Run(Sub()
                     Try
                         Dim runner As New LiveStreamRunner()
                         Dim devices = runner.EnumerateDevicesAsync(pythonPath)
                         cboLiveDevice.BeginInvoke(Sub()
                                                       UpdateDeviceCombo(devices)
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

        ' Apply theme
        ApplyTheme(_config.Theme)

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

        ' Update translation setup button
        UpdateTranslationButtonAsync()

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

        ' Auto-start subtitle server after form is fully shown
        AddHandler Me.Shown, Sub(s, ev) StartSubtitleServer()
    End Sub

    Private Sub RunFirstTimeSetup()
        ' Ask about starting with Windows
        Dim bootResult = MessageBox.Show(
            "Would you like Every Tongue to start automatically when Windows starts?",
            "Startup Preference",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        _config.StartWithWindows = (bootResult = DialogResult.Yes)
        If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()

        ' Ask about firewall access (needed for phones to connect to subtitle server)
        Dim fwResult = MessageBox.Show(
            "Allow Every Tongue to accept connections from other devices on your network?" & vbCrLf & vbCrLf &
            "This is needed for phones to display live subtitles. " &
            "Windows may ask for administrator permission.",
            "Network Access",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)
        _config.AllowFirewall = (fwResult = DialogResult.Yes)

        _config.FirstRunComplete = True
        ConfigManager.Save(_config)
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
        Dim update = Await Models.UpdateChecker.CheckForUpdateAsync()
        If update Is Nothing Then Return

        ' Try modular (app-only) update first, fall back to full installer
        Dim canModularUpdate = Not String.IsNullOrWhiteSpace(update.AppZipUrl)
        Dim needsWhisper = update.NeedsWhisperUpdate AndAlso Not String.IsNullOrWhiteSpace(update.WhisperZipUrl)

        Dim updateDesc = If(canModularUpdate AndAlso Not needsWhisper,
            "A small app update is available.",
            "An update is available.")

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
                    MessageBox.Show($"Update failed: {ex.Message}" & vbCrLf & vbCrLf &
                                   "Opening release page instead.",
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
            MessageBox.Show($"Update download failed: {ex.Message}" & vbCrLf & vbCrLf &
                           "Opening release page instead.",
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
        Try
            Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
            Dim mgr As New Models.DependencyManager(_config, toolsDir)

            Dim states = Await mgr.CheckAllToolsAsync()
            Dim missing = mgr.GetMissingTools(states)
            Dim updatable = mgr.GetUpdatableTools(states)

            If missing.Count > 0 OrElse updatable.Count > 0 OrElse manualCheck Then
                ' Open the Download Manager instead of nagging with MessageBoxes
                OpenDownloadManager()
            End If

        Catch ex As Exception
            If manualCheck Then
                WriteDebugLog($"[ERROR] CheckDependenciesAsync: {ex.Message}")
            End If
        End Try

        ' Run first-time setup after dependencies are ready
        If Not manualCheck AndAlso Not _config.FirstRunComplete Then
            RunFirstTimeSetup()
        End If
    End Sub

    Private Async Function DownloadToolsAsync(mgr As Models.DependencyManager, tools As List(Of Models.ToolState)) As Task
        Dim progressForm As New Form() With {
            .Text = "Downloading Tools",
            .Size = New Drawing.Size(450, 160),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False
        }
        Dim lblStatus As New Label() With {
            .Text = "Preparing...",
            .Location = New Drawing.Point(15, 15),
            .Size = New Drawing.Size(400, 20)
        }
        Dim pbDownload As New ProgressBar() With {
            .Location = New Drawing.Point(15, 45),
            .Size = New Drawing.Size(400, 25),
            .Style = ProgressBarStyle.Continuous
        }
        Dim lblProgress As New Label() With {
            .Text = "",
            .Location = New Drawing.Point(15, 78),
            .Size = New Drawing.Size(400, 20)
        }
        progressForm.Controls.AddRange({lblStatus, pbDownload, lblProgress})
        progressForm.Show(Me)

        Try
            For i = 0 To tools.Count - 1
                Dim tool = tools(i)
                lblStatus.Text = $"Downloading {tool.Name} ({i + 1}/{tools.Count})..."
                pbDownload.Value = 0

                Dim progress As New Progress(Of (downloaded As Long, total As Long))(
                    Sub(p)
                        If p.total > 0 Then
                            Dim pct = CInt(p.downloaded * 100 \ p.total)
                            pbDownload.Value = Math.Min(pct, 100)
                            Dim dlMB = (p.downloaded / 1048576.0).ToString("F1")
                            Dim totalMB = (p.total / 1048576.0).ToString("F1")
                            lblProgress.Text = $"{dlMB} MB / {totalMB} MB"
                        Else
                            Dim dlMB = (p.downloaded / 1048576.0).ToString("F1")
                            lblProgress.Text = $"{dlMB} MB downloaded"
                        End If
                    End Sub)

                Await mgr.DownloadToolAsync(tool, progress)
            Next

            ' Silently ensure Python + pip packages are installed (needed by live-server and nllb-server)
            lblStatus.Text = "Installing Python runtime and packages..."
            pbDownload.Value = 0
            pbDownload.Style = ProgressBarStyle.Marquee
            lblProgress.Text = "This may take a few minutes..."
            Await mgr.EnsurePythonReadyAsync(Nothing)
            pbDownload.Style = ProgressBarStyle.Continuous
            pbDownload.Value = 100

            MessageBox.Show(GetString("Msg_DownloadComplete"), GetString("Msg_DownloadCompleteTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Update config paths to point to the tools directory
            UpdateConfigPaths(mgr.ToolsDirectory)

        Catch ex As Exception
            MessageBox.Show($"{GetString("Msg_DownloadFailed")} {ex.Message}", GetString("Msg_DownloadError"),
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            progressForm.Close()
            progressForm.Dispose()
        End Try
    End Function

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
        PopulateModelDropdown()

        ' Re-enumerate audio devices
        Dim pythonPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
        btnLiveStart.Enabled = False
        Task.Run(Sub()
                     Try
                         Dim runner As New LiveStreamRunner()
                         Dim devices = runner.EnumerateDevicesAsync(pythonPath2)
                         cboLiveDevice.BeginInvoke(Sub()
                                                       UpdateDeviceCombo(devices)
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

    Private Sub OpenDownloadManager()
        Try
            Dim biblesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles")
            Using dlg As New Forms.FormDownloadManager(_config, biblesDir)
                dlg.ShowDialog(Me)
                If dlg.PathsUpdated Then
                    UpdateConfigPaths(AppDomain.CurrentDomain.BaseDirectory)
                End If
            End Using
        Catch ex As Exception
            WriteDebugLog($"[ERROR] OpenDownloadManager: {ex}")
            MessageBox.Show($"Error opening Download Manager: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PopulateLanguageDropdowns()
        cboInputLanguage.Items.Clear()
        cboOutputLanguage.Items.Clear()
        For Each lang In _whisperLanguages
            cboInputLanguage.Items.Add(lang)
            cboOutputLanguage.Items.Add(lang)
        Next
    End Sub

    Private Sub PopulateModelDropdown()
        cboModel.Items.Clear()
        Try
            Dim modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModel))
            If Not String.IsNullOrWhiteSpace(modelDir) AndAlso Directory.Exists(modelDir) Then
                For Each binFile In Directory.GetFiles(modelDir, "ggml-*.bin")
                    cboModel.Items.Add(Path.GetFileName(binFile))
                Next
            End If
        Catch ex As Exception
            WriteDebugLog($"[ERROR] PopulateModelDropdown: {ex.Message}")
        End Try

        ' Select the model for the current mode
        Dim currentModel = If(cboMode.SelectedIndex = 0, _config.PathModelAudio, _config.PathModel) ' Audio File mode uses separate model
        Dim currentName = Path.GetFileName(currentModel)
        SelectComboItem(cboModel, currentName)

        ' If model not in list, add it
        If cboModel.SelectedIndex < 0 AndAlso Not String.IsNullOrWhiteSpace(currentName) Then
            cboModel.Items.Add(currentName)
            SelectComboItem(cboModel, currentName)
        End If
    End Sub

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
            Catch
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
            Catch
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

            btnClearLog.Text = GetString("Btn_ClearLog")
            btnCopyLog.Text = GetString("Btn_CopyLog")

            ' Live tab
            grpLiveInput.Text = GetString("Grp_LiveInput")
            lblLiveDevice.Text = GetString("Lbl_LiveDevice")
            btnRefreshDevices.Text = GetString("Btn_RefreshDevices")
            lblLiveInputLang.Text = GetString("Lbl_LiveInputLang")
            btnEditFilters.Text = GetString("Btn_Filters")
            btnLiveStart.Text = GetString("Btn_LiveStart")
            btnLiveStop.Text = GetString("Btn_LiveStop")
            btnLiveSave.Text = GetString("Btn_LiveSave")

            ' Subtitle Server tab
            tabPageServer.Text = GetString("Tab_Server")
            grpServerSettings.Text = GetString("Grp_ServerSettings")
            lblServerPort.Text = GetString("Lbl_ServerPort")
            btnServerStart.Text = GetString("Btn_ServerStart")
            btnServerStop.Text = GetString("Btn_ServerStop")
            btnServerRestart.Text = GetString("Btn_ServerRestart")
            lblSubtitleBg.Text = GetString("Lbl_SubtitleBg")
            lblSubtitleFg.Text = GetString("Lbl_SubtitleFg")
            grpServerInfo.Text = GetString("Grp_ServerInfo")
            btnCopyUrl.Text = GetString("Btn_CopyUrl")

            If Not _isRunning Then
                lblStepStatus.Text = GetString("Msg_Ready")
            End If
        Catch
            ' Fallback silently if resource not found
        End Try
    End Sub

    Private Sub ApplyToolTips()
        tipMain.SetToolTip(txtUrl, GetString("Tip_Url"))
        tipMain.SetToolTip(cboInputLanguage, GetString("Tip_InputLanguage"))
        tipMain.SetToolTip(cboOutputLanguage, GetString("Tip_OutputLanguage"))
    End Sub

    Private Function GetString(key As String) As String
        Try
            Dim val = _resMgr.GetString(key)
            Return If(val, key)
        Catch
            Return key
        End Try
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
                rtbHelp.Text = "Help file not found."
            End If
        Catch ex As Exception
            rtbHelp.Text = "Error loading help: " & ex.Message
        End Try
    End Sub

#End Region

#Region "Mode Switching"

    Private Sub cboMode_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboMode.SelectedIndexChanged
        If _config Is Nothing Then Return
        Dim isYouTubeLike = (cboMode.SelectedIndex <> 0) ' 1, 2, 3 are YouTube-like
        Dim isAudioFile = (cboMode.SelectedIndex = 0)

        ' Show/hide YouTube-specific controls (time controls for YouTube-like modes)
        lblStartTime.Visible = isYouTubeLike
        txtStartHH.Visible = isYouTubeLike
        lblStartColon1.Visible = isYouTubeLike
        txtStartMM.Visible = isYouTubeLike
        lblStartColon2.Visible = isYouTubeLike
        txtStartSS.Visible = isYouTubeLike
        lblEndTime.Visible = isYouTubeLike
        txtEndHH.Visible = isYouTubeLike
        lblEndColon1.Visible = isYouTubeLike
        txtEndMM.Visible = isYouTubeLike
        lblEndColon2.Visible = isYouTubeLike
        txtEndSS.Visible = isYouTubeLike
        btnResume.Visible = isYouTubeLike

        ' Show/hide output formats (only for modes that produce subtitles)
        Dim hasSubtitles = (cboMode.SelectedIndex = 0 OrElse cboMode.SelectedIndex = 3)
        grpOutputFormats.Visible = hasSubtitles

        ' Update label and button text
        lblUrl.Text = If(isAudioFile, GetString("Lbl_AudioFile"), GetString("Lbl_Url"))
        btnBrowseFile.Text = If(isAudioFile, GetString("Btn_BrowseAudio"), GetString("Btn_BrowseFile"))

        ' Switch model to the mode's default
        Dim modelName = Path.GetFileName(If(isAudioFile, _config.PathModelAudio, _config.PathModel))
        SelectComboItem(cboModel, modelName)
    End Sub

#End Region

#Region "Pipeline Execution"

    Private Async Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If _isRunning Then Return

        ' Save current UI to config and set active model for current mode
        SaveUiToConfig()
        If cboMode.SelectedIndex = 0 Then ' Audio File mode
            _config.PathModel = _config.PathModelAudio
        End If

        _isRunning = True
        _cts = New CancellationTokenSource()
        _currentOutputDir = txtOutputDir.Text

        ' UI state
        btnStart.Enabled = False
        btnResume.Enabled = False
        btnCancel.Enabled = True
        btnOpenOutput.Enabled = False
        btnOpenSubtitleEdit.Enabled = False
        lnkPreviewSrt.Visible = False
        pbOverall.Value = 0
        pbChunk.Value = 0
        pbChunk.Visible = False

        ' Switch to Job tab (log is visible there)
        tabMain.SelectedTab = tabPageJob
        Application.DoEvents()

        Dim progress As New Progress(Of PipelineProgress)(
            Sub(p)
                lblStepStatus.Text = p.StatusMessage
                pbOverall.Value = Math.Min(100, p.OverallPercent)
                If p.ChunkTotal > 0 Then
                    pbChunk.Visible = True
                    pbChunk.Maximum = p.ChunkTotal
                    pbChunk.Value = Math.Min(p.ChunkTotal, p.ChunkDone)
                End If
            End Sub)

        Dim runner As New PipelineRunner(_config, progress, _cts.Token)
        AddHandler runner.LogMessage, Sub(s, entry) LogToRtb(entry.Message, entry.Level)

        Try
            Dim url = txtUrl.Text.Trim()
            Dim startTime = BuildTimeString(txtStartHH.Text, txtStartMM.Text, txtStartSS.Text)
            Dim endTime = BuildTimeString(txtEndHH.Text, txtEndMM.Text, txtEndSS.Text)

            Select Case cboMode.SelectedIndex
                Case 0 ' Audio File mode
                    Await runner.RunAudioFileAsync(url, _currentOutputDir)
                Case 1 ' YouTube / Audio Only mode
                    Await runner.RunExtractAudioAsync(url, startTime, endTime, _currentOutputDir)
                Case 2 ' YouTube / Download Only mode
                    Await runner.RunDownloadOnlyAsync(url, startTime, endTime, _currentOutputDir)
                Case Else ' YouTube / Subtitles mode
                    Await runner.RunAsync(url, startTime, endTime, _currentOutputDir)
            End Select

            lblStepStatus.Text = GetString("Msg_Done")
            pbOverall.Value = 100
            btnOpenOutput.Enabled = True
            btnOpenSubtitleEdit.Enabled = (cboMode.SelectedIndex = 0 OrElse cboMode.SelectedIndex = 3)
            lnkPreviewSrt.Visible = (cboMode.SelectedIndex = 0 OrElse cboMode.SelectedIndex = 3)
        Catch ex As OperationCanceledException
            lblStepStatus.Text = GetString("Msg_Cancelled")
            LogToRtb("Pipeline cancelled by user.", PipelineRunner.LogLevel.Err)
        Catch ex As PipelineException
            lblStepStatus.Text = GetString(ex.MessageKey)
            LogToRtb($"ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
            MessageBox.Show(ex.Message, GetString("Msg_PipelineError"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
            lblStepStatus.Text = "Error"
            LogToRtb($"UNEXPECTED ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
            MessageBox.Show(ex.Message, GetString("Msg_UnexpectedError"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _isRunning = False
            btnStart.Enabled = True
            btnResume.Enabled = True
            btnCancel.Enabled = False
            pbChunk.Visible = False
        End Try
    End Sub

    Private Async Sub btnResume_Click(sender As Object, e As EventArgs) Handles btnResume.Click
        If _isRunning Then Return

        ' Let user pick an existing output folder
        Using dlg As New FolderBrowserDialog()
            dlg.Description = "Select an existing output folder to resume"
            If Not String.IsNullOrWhiteSpace(_config.PathOutputRoot) Then
                dlg.SelectedPath = AppConfig.ResolvePath(_config.PathOutputRoot)
            End If
            If dlg.ShowDialog() <> DialogResult.OK Then Return
            txtOutputDir.Text = dlg.SelectedPath
        End Using

        SaveUiToConfig()

        _isRunning = True
        _cts = New CancellationTokenSource()
        _currentOutputDir = txtOutputDir.Text

        ' UI state
        btnStart.Enabled = False
        btnResume.Enabled = False
        btnCancel.Enabled = True
        btnOpenOutput.Enabled = False
        lnkPreviewSrt.Visible = False
        pbOverall.Value = 0
        pbChunk.Value = 0
        pbChunk.Visible = False

        ' Switch to Job tab (log is visible there)
        tabMain.SelectedTab = tabPageJob
        Application.DoEvents()

        Dim progress As New Progress(Of PipelineProgress)(
            Sub(p)
                lblStepStatus.Text = p.StatusMessage
                pbOverall.Value = Math.Min(100, p.OverallPercent)
                If p.ChunkTotal > 0 Then
                    pbChunk.Visible = True
                    pbChunk.Maximum = p.ChunkTotal
                    pbChunk.Value = Math.Min(p.ChunkTotal, p.ChunkDone)
                End If
            End Sub)

        Dim runner As New PipelineRunner(_config, progress, _cts.Token)
        AddHandler runner.LogMessage, Sub(s, entry) LogToRtb(entry.Message, entry.Level)

        Try
            Dim startTime = BuildTimeString(txtStartHH.Text, txtStartMM.Text, txtStartSS.Text)
            Dim endTime = BuildTimeString(txtEndHH.Text, txtEndMM.Text, txtEndSS.Text)

            Select Case cboMode.SelectedIndex
                Case 1 ' YouTube / Audio Only mode
                    Await runner.RunExtractAudioAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
                Case 2 ' YouTube / Download Only mode
                    Await runner.RunDownloadOnlyAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
                Case Else ' YouTube / Subtitles mode
                    Await runner.RunAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
            End Select

            lblStepStatus.Text = GetString("Msg_Done")
            pbOverall.Value = 100
            btnOpenOutput.Enabled = True
            lnkPreviewSrt.Visible = (cboMode.SelectedIndex = 3)
        Catch ex As OperationCanceledException
            lblStepStatus.Text = GetString("Msg_Cancelled")
            LogToRtb("Pipeline cancelled by user.", PipelineRunner.LogLevel.Err)
        Catch ex As PipelineException
            lblStepStatus.Text = GetString(ex.MessageKey)
            LogToRtb($"ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
            MessageBox.Show(ex.Message, GetString("Msg_PipelineError"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
            lblStepStatus.Text = "Error"
            LogToRtb($"UNEXPECTED ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
            MessageBox.Show(ex.Message, GetString("Msg_UnexpectedError"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _isRunning = False
            btnStart.Enabled = True
            btnResume.Enabled = True
            btnCancel.Enabled = False
            pbChunk.Visible = False
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        _cts?.Cancel()
    End Sub

#End Region

#Region "Logging"

    Private _logAutoScroll As Boolean = True
    Private Const PipelineLogMaxLines As Integer = 2000

    Private Sub LogToRtb(message As String, level As PipelineRunner.LogLevel)
        If rtbLog.InvokeRequired Then
            rtbLog.BeginInvoke(Sub() LogToRtb(message, level))
            Return
        End If

        ' Skip verbose messages from the UI log to avoid flooding the RTB
        If level = PipelineRunner.LogLevel.Verbose Then Return

        Dim color As Drawing.Color
        Select Case level
            Case PipelineRunner.LogLevel.Success : color = Drawing.Color.DarkGreen
            Case PipelineRunner.LogLevel.Err : color = Drawing.Color.Red
            Case Else : color = Drawing.Color.Black
        End Select

        ' Feed unified log panel
        AppendUnifiedLog("Pipeline", message, color)

        SendMessage(rtbLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
        Try
            rtbLog.SelectionStart = rtbLog.TextLength
            rtbLog.SelectionLength = 0
            rtbLog.SelectionColor = color
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}")

            ' Trim excess lines
            If rtbLog.Lines.Length > PipelineLogMaxLines Then
                Dim removeUpTo = rtbLog.GetFirstCharIndexFromLine(rtbLog.Lines.Length - PipelineLogMaxLines)
                rtbLog.Select(0, removeUpTo)
                rtbLog.SelectedText = ""
            End If
        Finally
            SendMessage(rtbLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            rtbLog.Invalidate()
        End Try

        If _logAutoScroll Then
            SendMessage(rtbLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
        End If
    End Sub

    Private Sub rtbLog_VScroll(sender As Object, e As EventArgs) Handles rtbLog.VScroll
        ' Detect if user scrolled away from bottom
        Dim pos = rtbLog.GetPositionFromCharIndex(rtbLog.TextLength - 1)
        _logAutoScroll = pos.Y <= rtbLog.Height + 50
    End Sub

    Private Sub btnClearLog_Click(sender As Object, e As EventArgs) Handles btnClearLog.Click
        rtbLog.Clear()
    End Sub

    Private Sub btnCopyLog_Click(sender As Object, e As EventArgs) Handles btnCopyLog.Click
        If rtbLog.TextLength > 0 Then
            Clipboard.SetText(rtbLog.Text)
        End If
    End Sub

#End Region

#Region "Browse Buttons"

    Private Sub btnBrowseFile_Click(sender As Object, e As EventArgs) Handles btnBrowseFile.Click
        Using dlg As New OpenFileDialog()
            If cboMode.SelectedIndex = 0 Then
                ' Audio File mode
                dlg.Filter = "Audio files|*.wav;*.mp3;*.ogg;*.flac;*.m4a;*.wma;*.aac;*.opus|All files|*.*"
                Dim resolvedRoot = AppConfig.ResolvePath(_config.PathOutputRoot)
                If Not String.IsNullOrWhiteSpace(resolvedRoot) AndAlso Directory.Exists(resolvedRoot) Then
                    dlg.InitialDirectory = resolvedRoot
                End If
            Else
                ' YouTube / Download Only / Extract Audio modes
                dlg.Filter = "Video/Audio files|*.mp4;*.mkv;*.avi;*.webm;*.wav;*.mp3;*.m4a;*.flac|All files|*.*"
            End If
            If dlg.ShowDialog() = DialogResult.OK Then
                txtUrl.Text = dlg.FileName
            End If
        End Using
    End Sub

    Private Sub btnBrowseOutput_Click(sender As Object, e As EventArgs) Handles btnBrowseOutput.Click
        Using dlg As New FolderBrowserDialog()
            dlg.SelectedPath = txtOutputDir.Text
            If dlg.ShowDialog() = DialogResult.OK Then
                txtOutputDir.Text = dlg.SelectedPath
            End If
        End Using
    End Sub

    Private Sub btnOpenOutput_Click(sender As Object, e As EventArgs) Handles btnOpenOutput.Click
        If Directory.Exists(_currentOutputDir) Then
            Process.Start("explorer.exe", _currentOutputDir)
        End If
    End Sub

    Private Sub lnkPreviewSrt_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles lnkPreviewSrt.LinkClicked
        Dim srtPath = FindOutputSrt()
        If srtPath IsNot Nothing Then
            Process.Start(New ProcessStartInfo(srtPath) With {.UseShellExecute = True})
        End If
    End Sub

    Private Sub btnOpenSubtitleEdit_Click(sender As Object, e As EventArgs) Handles btnOpenSubtitleEdit.Click
        Dim srtPath = FindOutputSrt()
        If srtPath Is Nothing Then
            MessageBox.Show(GetString("Msg_NoSrtFound"), "Subtitle Edit", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim subtitleEditPath = AppConfig.ResolvePath(_config.PathSubtitleEdit)
        If Not File.Exists(subtitleEditPath) Then
            MessageBox.Show($"{GetString("Msg_SubtitleEditNotFound")} {subtitleEditPath}", "Subtitle Edit", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Process.Start(subtitleEditPath, $"""{srtPath}""")
    End Sub

    Private Function FindOutputSrt() As String
        If String.IsNullOrWhiteSpace(_currentOutputDir) OrElse Not Directory.Exists(_currentOutputDir) Then Return Nothing

        ' YouTube mode: preview.srt
        Dim previewSrt = Path.Combine(_currentOutputDir, "preview.srt")
        If File.Exists(previewSrt) Then Return previewSrt

        ' Audio File mode: first .srt in output dir
        Dim srtFiles = Directory.GetFiles(_currentOutputDir, "*.srt")
        If srtFiles.Length > 0 Then Return srtFiles(0)

        Return Nothing
    End Function

    Private Sub BrowseForExe(textBox As TextBox)
        Using dlg As New OpenFileDialog()
            dlg.Filter = "Executable files|*.exe|All files|*.*"
            If Not String.IsNullOrWhiteSpace(textBox.Text) Then
                Try
                    dlg.InitialDirectory = Path.GetDirectoryName(AppConfig.ResolvePath(textBox.Text))
                Catch
                End Try
            End If
            If dlg.ShowDialog() = DialogResult.OK Then
                textBox.Text = ToRelativePath(dlg.FileName)
            End If
        End Using
    End Sub

    Private Sub BrowseForFile(textBox As TextBox, filter As String)
        Using dlg As New OpenFileDialog()
            dlg.Filter = filter
            If Not String.IsNullOrWhiteSpace(textBox.Text) Then
                Try
                    dlg.InitialDirectory = Path.GetDirectoryName(AppConfig.ResolvePath(textBox.Text))
                Catch
                End Try
            End If
            If dlg.ShowDialog() = DialogResult.OK Then
                textBox.Text = ToRelativePath(dlg.FileName)
            End If
        End Using
    End Sub

    Private Sub BrowseForFolder(textBox As TextBox)
        Using dlg As New FolderBrowserDialog()
            If Not String.IsNullOrWhiteSpace(textBox.Text) Then
                Try
                    Dim resolved = Path.GetFullPath(AppConfig.ResolvePath(textBox.Text))
                    If Directory.Exists(resolved) Then dlg.SelectedPath = resolved
                Catch
                End Try
            End If
            If dlg.ShowDialog() = DialogResult.OK Then
                textBox.Text = ToRelativePath(dlg.SelectedPath)
            End If
        End Using
    End Sub

    Private Shared Function ToRelativePath(fullPath As String) As String
        Dim baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) & Path.DirectorySeparatorChar
        If fullPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase) Then
            Return ".\" & fullPath.Substring(baseDir.Length)
        End If
        Return fullPath
    End Function

    ' (Browse, Verify Paths, Settings event handlers removed — managed by FormOptions)

    Private Sub cboModel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboModel.SelectedIndexChanged
        If _config Is Nothing OrElse cboModel.SelectedItem Is Nothing Then Return
        Dim modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModel))
        If String.IsNullOrWhiteSpace(modelDir) Then modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModelAudio))
        Dim fullPath = Path.Combine(If(modelDir, ""), cboModel.SelectedItem.ToString())

        Select Case cboMode.SelectedIndex
            Case 0 : _config.PathModelAudio = fullPath
            Case 3 : _config.PathModel = fullPath
            Case Else : Return
        End Select
        ConfigManager.Save(_config)
    End Sub

#End Region

#Region "Theme"

    Private Sub ApplyTheme(theme As String)
        WriteDebugLog($"[THEME] ApplyTheme called with theme=""{theme}""")
        Dim backColor, foreColor, controlBack As Drawing.Color

        Select Case theme.ToLower()
            Case "dark"
                backColor = Drawing.Color.FromArgb(30, 30, 30)
                foreColor = Drawing.Color.FromArgb(220, 220, 220)
                controlBack = Drawing.Color.FromArgb(45, 45, 48)
            Case "light"
                backColor = Drawing.Color.White
                foreColor = Drawing.Color.Black
                controlBack = Drawing.Color.White
            Case Else ' System
                backColor = Drawing.SystemColors.Control
                foreColor = Drawing.SystemColors.ControlText
                controlBack = Drawing.SystemColors.Window
        End Select

        Me.BackColor = backColor
        Me.ForeColor = foreColor
        ApplyThemeToControls(Me, backColor, foreColor, controlBack)
        ApplyShellTheme(theme)
    End Sub

    Private Sub ApplyThemeToControls(parent As Control, backColor As Drawing.Color, foreColor As Drawing.Color, controlBack As Drawing.Color)
        For Each ctrl As Control In parent.Controls
            ' Skip nav rail — themed by ApplyShellTheme
            If ctrl Is pnlNavRail Then Continue For

            ctrl.ForeColor = foreColor

            If TypeOf ctrl Is TextBox OrElse TypeOf ctrl Is MaskedTextBox OrElse
               TypeOf ctrl Is RichTextBox OrElse TypeOf ctrl Is ComboBox OrElse
               TypeOf ctrl Is NumericUpDown Then
                ctrl.BackColor = controlBack
            ElseIf TypeOf ctrl Is TabControl Then
                ' Don't change tab control background
            ElseIf TypeOf ctrl Is Button Then
                ' Skip color picker buttons
                If ctrl Is btnSubtitleBg OrElse ctrl Is btnSubtitleFg Then
                    Continue For
                End If
                ' Keep buttons readable
                If backColor = Drawing.SystemColors.Control Then
                    ctrl.BackColor = Drawing.SystemColors.Control
                Else
                    ctrl.BackColor = Drawing.Color.FromArgb(
                        Math.Min(255, backColor.R + 30),
                        Math.Min(255, backColor.G + 30),
                        Math.Min(255, backColor.B + 30))
                End If
            Else
                ctrl.BackColor = backColor
            End If

            If ctrl.HasChildren Then
                ApplyThemeToControls(ctrl, backColor, foreColor, controlBack)
            End If
        Next
    End Sub

#End Region

#Region "Live Translation"

    Private Sub PopulateLiveLanguageDropdowns()
        cboLiveInputLang.Items.Clear()
        For Each lang In _whisperLanguages
            cboLiveInputLang.Items.Add(LangDisplayName(lang))
        Next

        cboLiveInputLang.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboLiveInputLang.AutoCompleteSource = AutoCompleteSource.ListItems

        SelectLiveInputLang(_config.Language)
    End Sub

    Private Sub SelectLiveInputLang(code As String)
        Dim display = LangDisplayName(code)
        For i = 0 To cboLiveInputLang.Items.Count - 1
            If cboLiveInputLang.Items(i).ToString().Equals(display, StringComparison.OrdinalIgnoreCase) Then
                cboLiveInputLang.SelectedIndex = i
                Return
            End If
        Next
        ' Fallback: try matching by code in parentheses
        For i = 0 To cboLiveInputLang.Items.Count - 1
            If cboLiveInputLang.Items(i).ToString().Contains($"({code})") Then
                cboLiveInputLang.SelectedIndex = i
                Return
            End If
        Next
        If cboLiveInputLang.Items.Count > 0 Then cboLiveInputLang.SelectedIndex = 0
    End Sub

    Private Sub cboLiveDevice_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLiveDevice.SelectedIndexChanged
        If cboLiveDevice.SelectedItem IsNot Nothing Then
            Dim txt = cboLiveDevice.SelectedItem.ToString()
            Dim colonIdx = txt.IndexOf(":"c)
            If colonIdx > 0 Then
                _config.LastLiveDeviceId = txt.Substring(0, colonIdx).Trim()
                ConfigManager.Save(_config)
            End If
        End If
    End Sub

    Private Sub cboLiveInputLang_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLiveInputLang.SelectedIndexChanged
        If _isInitializing OrElse cboLiveInputLang.SelectedItem Is Nothing Then Return
        _config.Language = LangCodeFromDisplay(cboLiveInputLang.SelectedItem.ToString())
        SelectComboItem(cboInputLanguage, _config.Language)
        ConfigManager.Save(_config)
    End Sub

    Private Sub cboLiveInputLang_Leave(sender As Object, e As EventArgs) Handles cboLiveInputLang.Leave
        ' Snap to valid item if user typed something that didn't match
        If cboLiveInputLang.SelectedIndex < 0 Then
            SelectLiveInputLang(_config.Language)
        End If
    End Sub

    Private Sub btnRefreshDevices_Click(sender As Object, e As EventArgs) Handles btnRefreshDevices.Click
        SaveUiToConfig()
        cboLiveDevice.Items.Clear()
        cboLiveDevice.Items.Add("Detecting devices...")
        cboLiveDevice.SelectedIndex = 0
        cboLiveDevice.Enabled = False
        btnRefreshDevices.Enabled = False

        Dim pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")

        Task.Run(Sub()
                     Try
                         Dim runner As New LiveStreamRunner()
                         Dim devices = runner.EnumerateDevicesAsync(pythonPath)
                         cboLiveDevice.BeginInvoke(Sub()
                                                       UpdateDeviceCombo(devices)
                                                       cboLiveDevice.Enabled = True
                                                       btnRefreshDevices.Enabled = True
                                                   End Sub)
                     Catch ex As Exception
                         WriteDebugLog($"[ERROR] Refresh devices failed: {ex.Message}")
                         cboLiveDevice.BeginInvoke(Sub()
                                                       cboLiveDevice.Items.Clear()
                                                       cboLiveDevice.Items.Add("0: Default Device")
                                                       cboLiveDevice.SelectedIndex = 0
                                                       cboLiveDevice.Enabled = True
                                                       btnRefreshDevices.Enabled = True
                                                   End Sub)
                     End Try
                 End Sub)
    End Sub

    Private Sub btnEditFilters_Click(sender As Object, e As EventArgs) Handles btnEditFilters.Click
        Using frm As New FormFilterEditor(AppDomain.CurrentDomain.BaseDirectory, _config.LiveServerPort, _config.TranslationPort, _resMgr)
            frm.Icon = Me.Icon
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Async Sub btnLiveStart_Click(sender As Object, e As EventArgs) Handles btnLiveStart.Click
        SaveUiToConfig()

        ' Check live dependencies (Python, packages, model)
        Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
        Dim mgr As New Models.DependencyManager(_config, toolsDir)
        Dim liveDeps = Await mgr.CheckLiveDepsAsync()

        If Not liveDeps.pythonOk OrElse Not liveDeps.depsOk OrElse Not liveDeps.modelOk Then
            If _isRemoteCommand Then
                AppendServerLog("ERROR: Live transcription dependencies not installed.")
                Return
            End If

            MessageBox.Show(
                GetString("Msg_LiveDepsMissing"),
                GetString("Msg_DepsMissing"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        ' Get device ID from combo selection
        Dim deviceId = 0
        If cboLiveDevice.SelectedItem IsNot Nothing Then
            Dim deviceText = cboLiveDevice.SelectedItem.ToString()
            Dim colonIdx = deviceText.IndexOf(":"c)
            If colonIdx > 0 Then
                Integer.TryParse(deviceText.Substring(0, colonIdx).Trim(), deviceId)
            End If
        End If

        ' Get input language
        Dim inputLang = "auto"
        If cboLiveInputLang.SelectedItem IsNot Nothing Then inputLang = LangCodeFromDisplay(cboLiveInputLang.SelectedItem.ToString())
        Dim svc1 = SubtitleSvc
        If svc1 IsNot Nothing Then svc1.InputLanguage = inputLang

        Dim translateToEn = False

        _liveRunner = New LiveStreamRunner()

        ' In-progress line refinement (log only, not broadcast to subtitle clients)
        AddHandler _liveRunner.OutputLineUpdated, Sub(s, line)
                                                      WriteDebugLog($"[Live] >>> UPDATE: {line}")
                                                  End Sub

        ' Committed final line
        AddHandler _liveRunner.OutputLineCommitted, Sub(s, line)
                                                        ' Accumulate transcript text (strip lang prefix)
                                                        Dim textOnly = line
                                                        Dim ti = line.IndexOf(vbTab)
                                                        If ti > 0 Then textOnly = line.Substring(ti + 1)
                                                        _liveTranscript.AppendLine(textOnly)

                                                        WriteDebugLog($"[Live] >>> COMMIT: {line}")
                                                        TranslateAndBroadcastAsync(line)
                                                    End Sub

        AddHandler _liveRunner.ErrorReceived, Sub(s, line)
                                                  ' Skip lines already handled by OutputLineUpdated/Committed
                                                  If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                     Not line.StartsWith(">>> COMMIT") AndAlso
                                                     Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                     Not line.Contains("ASGI callable returned without completing response") Then
                                                      WriteDebugLog($"[Live] {line}")
                                                  End If
                                              End Sub

        AppendServerLog($"Live transcription starting (device {deviceId}, lang={inputLang})...")
        WriteDebugLog($"[Live] faster-whisper + Silero VAD (port {_config.LiveServerPort})")

        _liveRunner.Start(_config, deviceId, inputLang, translateToEn)

        If _liveRunner.IsRunning Then
            SubtitleSvc?.BroadcastSystemMessage("[Transcription Started]")
        End If

        If _liveRunner.IsRunning Then
            btnLiveStart.Enabled = False
            btnLiveStop.Enabled = True
            btnTuneStats.Enabled = True
            grpLiveInput.Enabled = False
            UpdateLiveRunningStatus()
            ShowLogPanel()
        End If
    End Sub

    Private Sub btnLiveStop_Click(sender As Object, e As EventArgs) Handles btnLiveStop.Click
        If _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning Then
            SubtitleSvc?.BroadcastSystemMessage("[Transcription Stopped]")
            _liveRunner.Stop()
            AppendServerLog("Live transcription stopped.")
        End If

        btnLiveStart.Enabled = True
        btnLiveStop.Enabled = False
        grpLiveInput.Enabled = True
        UpdateLiveRunningStatus()
    End Sub

    Private Sub trkMaxSegment_Scroll(sender As Object, e As EventArgs) Handles trkMaxSegment.Scroll
        lblMaxSegmentValue.Text = $"{trkMaxSegment.Value}s"
        SaveUiToConfig()
        PushLiveConfig()
    End Sub

    Private Sub trkVadSilence_Scroll(sender As Object, e As EventArgs) Handles trkVadSilence.Scroll
        lblVadSilenceValue.Text = $"{trkVadSilence.Value}ms"
        SaveUiToConfig()
        PushLiveConfig()
    End Sub

    Private Async Sub PushLiveConfig()
        If _liveRunner Is Nothing OrElse Not _liveRunner.IsRunning Then Return
        Dim cfg As New Dictionary(Of String, Object) From {
            {"vad_max_segment_s", trkMaxSegment.Value},
            {"vad_min_silence_ms", trkVadSilence.Value}
        }
        Await _liveRunner.UpdateConfigAsync(cfg)
    End Sub

    Private _isRemoteCommand As Boolean = False

    Private Sub HandleRemoteCommand(command As String)
        Dim isLiveActive = _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning
        _isRemoteCommand = True
        Try
            Select Case command
                Case "start"
                    If Not isLiveActive Then
                        AppendServerLog("Remote command: START")
                        btnLiveStart_Click(Nothing, EventArgs.Empty)
                    End If
                Case "stop"
                    If isLiveActive Then
                        AppendServerLog("Remote command: STOP")
                        btnLiveStop_Click(Nothing, EventArgs.Empty)
                    End If
                Case "restart"
                    AppendServerLog("Remote command: RESTART")
                    If isLiveActive Then
                        btnLiveStop_Click(Nothing, EventArgs.Empty)
                    End If
                    btnLiveStart_Click(Nothing, EventArgs.Empty)
                Case "clear"
                    AppendServerLog("Remote command: CLEAR")
                    SubtitleSvc?.BroadcastClear()
                Case Else
                    If command.StartsWith("setSliders:") Then
                        Dim parts = command.Substring(11).Split(","c)
                        If parts.Length = 2 Then
                            Dim maxSeg As Integer
                            Dim vadSilence As Integer
                            If Integer.TryParse(parts(0), maxSeg) AndAlso Integer.TryParse(parts(1), vadSilence) Then
                                AppendServerLog($"Remote command: SET SLIDERS maxSeg={maxSeg}s vadSilence={vadSilence}ms")
                                trkMaxSegment.Value = Math.Max(trkMaxSegment.Minimum, Math.Min(trkMaxSegment.Maximum, maxSeg))
                                lblMaxSegmentValue.Text = $"{trkMaxSegment.Value}s"
                                trkVadSilence.Value = Math.Max(trkVadSilence.Minimum, Math.Min(trkVadSilence.Maximum, vadSilence))
                                lblVadSilenceValue.Text = $"{trkVadSilence.Value}ms"
                                SaveUiToConfig()
                                PushLiveConfig()
                            End If
                        End If
                    End If
            End Select
        Finally
            _isRemoteCommand = False
        End Try
        UpdateLiveRunningStatus()
    End Sub

    Private Sub UpdateLiveRunningStatus()
        Dim svc = SubtitleSvc
        If svc IsNot Nothing Then
            svc.IsLiveRunning = _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning
        End If
        UpdateShellStatus()
    End Sub

    Private Sub btnLiveSave_Click(sender As Object, e As EventArgs) Handles btnLiveSave.Click
        Dim transcript = _liveTranscript.ToString()
        If String.IsNullOrWhiteSpace(transcript) Then
            MessageBox.Show("No transcript to save.", "Save Transcript", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Using dlg As New SaveFileDialog()
            dlg.Filter = "Text files|*.txt|All files|*.*"
            dlg.DefaultExt = "txt"
            dlg.FileName = $"live_transcript_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt"
            Dim resolvedOutput = AppConfig.ResolvePath(_config.PathOutputRoot)
            If Not String.IsNullOrWhiteSpace(resolvedOutput) Then
                dlg.InitialDirectory = resolvedOutput
            End If
            If dlg.ShowDialog() = DialogResult.OK Then
                File.WriteAllText(dlg.FileName, transcript, System.Text.Encoding.UTF8)
                MessageBox.Show($"{GetString("Msg_TranscriptSaved")}{Environment.NewLine}{dlg.FileName}", GetString("Msg_Saved"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Private Async Sub btnTuneStats_Click(sender As Object, e As EventArgs) Handles btnTuneStats.Click
        If _liveRunner Is Nothing Then
            MessageBox.Show("No session data available.", "Tune", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim json = Await _liveRunner.GetStatsAsync()
        If String.IsNullOrEmpty(json) Then
            MessageBox.Show("No statistics available yet. Run the live transcription for a while first.", "Tune", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Try
            Using doc = System.Text.Json.JsonDocument.Parse(json)
                Dim root = doc.RootElement

                Dim commitsProp As System.Text.Json.JsonElement = Nothing
                Dim commits As Integer = 0
                If root.TryGetProperty("commits", commitsProp) Then commits = commitsProp.GetInt32()
                If commits = 0 Then
                    MessageBox.Show("No commits recorded yet. Let it run for a bit longer.", "Tune", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                ' Read current slider values
                Dim currentMaxSeg = trkMaxSegment.Value
                Dim currentVadSilence = trkVadSilence.Value

                ' Extract stats
                Dim durAvg = root.GetProperty("duration").GetProperty("avg").GetDouble()
                Dim durMedian = root.GetProperty("duration").GetProperty("median").GetDouble()
                Dim durMax = root.GetProperty("duration").GetProperty("max").GetDouble()
                Dim forceRatio = root.GetProperty("force_commit_ratio").GetDouble()
                Dim shortRatio = root.GetProperty("short_segment_ratio").GetDouble()
                Dim hallucinations = root.GetProperty("hallucinations").GetInt32()

                Dim gapAvg As Double = 0
                Dim gapMedian As Double = 0
                Dim hasGaps = False
                Dim gapsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("silence_gaps", gapsProp) Then
                    gapAvg = gapsProp.GetProperty("avg").GetDouble()
                    gapMedian = gapsProp.GetProperty("median").GetDouble()
                    hasGaps = True
                End If

                Dim wpsAvg As Double = 0
                Dim wpsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("wps", wpsProp) Then
                    wpsAvg = wpsProp.GetProperty("avg").GetDouble()
                End If

                ' Language breakdown
                Dim langInfo = ""
                Dim langsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("languages", langsProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In langsProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    langInfo = String.Join(", ", parts)
                End If

                ' Commit type breakdown
                Dim typeInfo = ""
                Dim typesProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("commit_types", typesProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In typesProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    typeInfo = String.Join(", ", parts)
                End If

                ' Build recommendations
                Dim tips As New List(Of String)
                Dim suggestedMaxSeg = currentMaxSeg
                Dim suggestedVadSilence = currentVadSilence

                ' --- Max Segment analysis ---
                If forceRatio > 0.15 Then
                    ' Too many force-commits: speaker talks in long stretches
                    Dim suggested = CInt(Math.Min(60, Math.Ceiling(durMax * 1.3 / 5) * 5))
                    tips.Add($"• Max Segment too low — {forceRatio.ToString("P0")} of commits are force-cut. Speaker needs longer segments.")
                    tips.Add($"  Suggest: {suggested}s (currently {currentMaxSeg}s)")
                    suggestedMaxSeg = suggested
                ElseIf forceRatio = 0 AndAlso durMax < currentMaxSeg * 0.5 Then
                    ' Max segment never reached and is way higher than needed
                    Dim suggested = CInt(Math.Max(10, Math.Ceiling(durMax * 1.5 / 5) * 5))
                    tips.Add($"• Max Segment could be tighter — longest commit was {durMax.ToString("F1")}s, limit is {currentMaxSeg}s.")
                    tips.Add($"  Suggest: {suggested}s")
                    suggestedMaxSeg = suggested
                Else
                    tips.Add($"• Max Segment ({currentMaxSeg}s) looks good for this speaker.")
                End If

                ' --- VAD Silence analysis ---
                If shortRatio > 0.4 Then
                    ' Too many short segments: VAD is cutting too aggressively
                    Dim suggested = CInt(Math.Min(1500, Math.Ceiling((currentVadSilence + 200) / 100) * 100))
                    tips.Add($"• VAD Silence too low — {shortRatio.ToString("P0")} of commits are under 3s (fragmented speech).")
                    tips.Add($"  Suggest: {suggested}ms (currently {currentVadSilence}ms)")
                    suggestedVadSilence = suggested
                ElseIf hasGaps AndAlso gapMedian > 3.0 AndAlso shortRatio < 0.1 Then
                    ' Long gaps, few short segments: could tighten silence
                    Dim suggested = CInt(Math.Max(200, Math.Floor((currentVadSilence - 100) / 100) * 100))
                    tips.Add($"• VAD Silence could be lower — median gap is {gapMedian.ToString("F1")}s, few short segments.")
                    tips.Add($"  Suggest: {suggested}ms (currently {currentVadSilence}ms)")
                    suggestedVadSilence = suggested
                Else
                    tips.Add($"• VAD Silence ({currentVadSilence}ms) looks good for this speaker.")
                End If

                ' --- Language detection ---
                If langInfo.Contains(",") Then
                    tips.Add($"• Multiple languages detected: {langInfo}")
                    tips.Add("  If the speaker uses one language, consider forcing it via Input Language.")
                ElseIf langInfo <> "" Then
                    tips.Add($"• Language consistent: {langInfo}")
                End If

                ' --- General stats ---
                tips.Add("")
                tips.Add($"Session: {commits} commits, {hallucinations} hallucinations filtered")
                tips.Add($"Commit types: {typeInfo}")
                If wpsAvg > 0 Then tips.Add($"Speaking rate: {wpsAvg.ToString("F1")} words/sec")
                tips.Add($"Segment duration: avg {durAvg.ToString("F1")}s, median {durMedian.ToString("F1")}s, max {durMax.ToString("F1")}s")
                If hasGaps Then tips.Add($"Silence gaps: avg {gapAvg.ToString("F1")}s, median {gapMedian.ToString("F1")}s")

                Dim msg = String.Join(Environment.NewLine, tips)
                Dim result = MessageBox.Show(
                    msg & Environment.NewLine & Environment.NewLine &
                    "Apply suggested slider values?",
                    "Tuning Recommendations",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information)

                If result = DialogResult.Yes Then
                    If suggestedMaxSeg <> currentMaxSeg Then
                        trkMaxSegment.Value = Math.Max(trkMaxSegment.Minimum, Math.Min(trkMaxSegment.Maximum, suggestedMaxSeg))
                        lblMaxSegmentValue.Text = $"{trkMaxSegment.Value}s"
                    End If
                    If suggestedVadSilence <> currentVadSilence Then
                        trkVadSilence.Value = Math.Max(trkVadSilence.Minimum, Math.Min(trkVadSilence.Maximum, suggestedVadSilence))
                        lblVadSilenceValue.Text = $"{trkVadSilence.Value}ms"
                    End If
                    SaveUiToConfig()
                    PushLiveConfig()
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Failed to parse stats: {ex.Message}", "Tune", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Function GetTuneJson() As String
        If _liveRunner Is Nothing Then Return Nothing

        Dim json As String = Nothing
        ' GetStatsAsync must be awaited — use .Result on background thread (TuneCallback is called from server thread)
        Try
            json = _liveRunner.GetStatsAsync().Result
        Catch ex As Exception
            WriteDebugLog($"[ERROR] GetTuneJson stats: {ex.Message}")
        End Try
        If String.IsNullOrEmpty(json) Then Return Nothing

        Try
            Using doc = System.Text.Json.JsonDocument.Parse(json)
                Dim root = doc.RootElement

                Dim commitsProp As System.Text.Json.JsonElement = Nothing
                Dim commits As Integer = 0
                If root.TryGetProperty("commits", commitsProp) Then commits = commitsProp.GetInt32()
                If commits = 0 Then Return Nothing

                Dim currentMaxSeg = trkMaxSegment.Value
                Dim currentVadSilence = trkVadSilence.Value

                Dim durAvg = root.GetProperty("duration").GetProperty("avg").GetDouble()
                Dim durMedian = root.GetProperty("duration").GetProperty("median").GetDouble()
                Dim durMax = root.GetProperty("duration").GetProperty("max").GetDouble()
                Dim forceRatio = root.GetProperty("force_commit_ratio").GetDouble()
                Dim shortRatio = root.GetProperty("short_segment_ratio").GetDouble()
                Dim hallucinations = root.GetProperty("hallucinations").GetInt32()

                Dim gapAvg As Double = 0
                Dim gapMedian As Double = 0
                Dim hasGaps = False
                Dim gapsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("silence_gaps", gapsProp) Then
                    gapAvg = gapsProp.GetProperty("avg").GetDouble()
                    gapMedian = gapsProp.GetProperty("median").GetDouble()
                    hasGaps = True
                End If

                Dim wpsAvg As Double = 0
                Dim wpsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("wps", wpsProp) Then
                    wpsAvg = wpsProp.GetProperty("avg").GetDouble()
                End If

                Dim langInfo = ""
                Dim langsProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("languages", langsProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In langsProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    langInfo = String.Join(", ", parts)
                End If

                Dim typeInfo = ""
                Dim typesProp As Text.Json.JsonElement = Nothing
                If root.TryGetProperty("commit_types", typesProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In typesProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    typeInfo = String.Join(", ", parts)
                End If

                Dim tips As New List(Of String)
                Dim suggestedMaxSeg = currentMaxSeg
                Dim suggestedVadSilence = currentVadSilence

                If forceRatio > 0.15 Then
                    Dim suggested = CInt(Math.Min(60, Math.Ceiling(durMax * 1.3 / 5) * 5))
                    tips.Add($"Max Segment too low - {forceRatio.ToString("P0")} force-cut. Suggest: {suggested}s (current {currentMaxSeg}s)")
                    suggestedMaxSeg = suggested
                ElseIf forceRatio = 0 AndAlso durMax < currentMaxSeg * 0.5 Then
                    Dim suggested = CInt(Math.Max(10, Math.Ceiling(durMax * 1.5 / 5) * 5))
                    tips.Add($"Max Segment could be tighter - longest {durMax.ToString("F1")}s, limit {currentMaxSeg}s. Suggest: {suggested}s")
                    suggestedMaxSeg = suggested
                Else
                    tips.Add($"Max Segment ({currentMaxSeg}s) looks good.")
                End If

                If shortRatio > 0.4 Then
                    Dim suggested = CInt(Math.Min(1500, Math.Ceiling((currentVadSilence + 200) / 100) * 100))
                    tips.Add($"VAD Silence too low - {shortRatio.ToString("P0")} under 3s. Suggest: {suggested}ms (current {currentVadSilence}ms)")
                    suggestedVadSilence = suggested
                ElseIf hasGaps AndAlso gapMedian > 3.0 AndAlso shortRatio < 0.1 Then
                    Dim suggested = CInt(Math.Max(200, Math.Floor((currentVadSilence - 100) / 100) * 100))
                    tips.Add($"VAD Silence could be lower - median gap {gapMedian.ToString("F1")}s. Suggest: {suggested}ms (current {currentVadSilence}ms)")
                    suggestedVadSilence = suggested
                Else
                    tips.Add($"VAD Silence ({currentVadSilence}ms) looks good.")
                End If

                If langInfo.Contains(",") Then
                    tips.Add($"Multiple languages detected: {langInfo}")
                End If

                ' Build JSON response
                Dim tipsJson = String.Join(",", tips.Select(Function(t) $"""{t.Replace("""", "\""")}"""))
                Return $"{{""currentMaxSeg"":{currentMaxSeg},""currentVadSilence"":{currentVadSilence}," &
                       $"""suggestedMaxSeg"":{suggestedMaxSeg},""suggestedVadSilence"":{suggestedVadSilence}," &
                       $"""commits"":{commits},""hallucinations"":{hallucinations}," &
                       $"""durAvg"":{durAvg.ToString("F1", Globalization.CultureInfo.InvariantCulture)}," &
                       $"""durMax"":{durMax.ToString("F1", Globalization.CultureInfo.InvariantCulture)}," &
                       $"""wpsAvg"":{wpsAvg.ToString("F1", Globalization.CultureInfo.InvariantCulture)}," &
                       $"""tips"":[{tipsJson}]}}"
            End Using
        Catch ex As Exception
            WriteDebugLog($"[ERROR] GetTuneJson parse: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Private Sub HandleInputLanguageChanged(lang As String)
        ' Update the UI dropdown
        SelectLiveInputLang(lang)
        ' Update subtitle server's tracked state (for status polling)
        Dim svc2 = SubtitleSvc
        If svc2 IsNot Nothing Then svc2.InputLanguage = lang
        ' Forward to live-server if running
        If _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning Then
            Dim config As New Dictionary(Of String, Object) From {{"language", lang}}
            Task.Run(Function() _liveRunner.UpdateConfigAsync(config))
        End If
        WriteDebugLog($"[Live] Input language changed to '{lang}' via client")
    End Sub


    Private Async Sub TranslateAndBroadcastAsync(commitData As String)
        ' Parse detected language from tab-separated format: "lang\ttext"
        Dim detectedLang = ""
        Dim line = commitData
        Dim tabIdx = commitData.IndexOf(vbTab)
        If tabIdx > 0 Then
            detectedLang = commitData.Substring(0, tabIdx)
            line = commitData.Substring(tabIdx + 1)
        End If

        WriteDebugLog($"[WHISPER COMMIT] [{detectedLang}] {line}")

        ' Use detected language for NLLB source (convert whisper ISO code to NLLB code)
        Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), GetCurrentSourceNllbLang())
        Dim sourceShort = NllbToShortCode(sourceLang)

        ' Check if translation is available
        Dim targets = SubtitleSvc?.GetActiveTranslationLanguages()
        Dim activeTargets = If(targets IsNot Nothing, String.Join(",", targets), "none")
        targets?.Remove(sourceLang)

        Dim hasTargets = targets IsNot Nothing AndAlso targets.Count > 0
        Dim serviceRunning = _translationService IsNot Nothing AndAlso _translationService.IsRunning
        Dim modelLoaded = serviceRunning AndAlso _translationService.IsModelLoaded
        Dim translationReady = hasTargets AndAlso modelLoaded

        WriteDebugLog($"[BROADCAST] targets=[{activeTargets}] source={sourceLang} ready={translationReady} modelLoaded={modelLoaded}")

        ' Model still loading — buffer the commit for later translation
        If hasTargets AndAlso serviceRunning AndAlso Not modelLoaded Then
            SyncLock _pendingCommits
                _pendingCommits.Add(commitData)
            End SyncLock
            ' Send original text to source-language and non-translation clients
            SubtitleSvc?.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang)
            WriteDebugLog($"[BUFFERED] commit queued ({_pendingCommits.Count} pending)")
            Return
        End If

        ' No translation clients or service not running — send to everyone immediately
        If Not translationReady Then
            SubtitleSvc?.BroadcastCommit(line, skipTranslationClients:=False, lang:=sourceShort)
            Return
        End If

        ' Filter garbage commits — send to non-translation clients only
        If IsGarbageCommit(line) Then
            WriteDebugLog($"[FILTERED] garbage commit skipped for translation")
            SubtitleSvc?.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang)
            Return
        End If

        ' Translate for all target languages
        Dim sw = Diagnostics.Stopwatch.StartNew()
        Dim translations As New Dictionary(Of String, String)()
        Try
            Dim result = Await _translationService.TranslateAsync(line, sourceLang, targets)
            If result IsNot Nothing Then
                For Each kvp In result
                    translations(kvp.Key) = kvp.Value
                    WriteDebugLog($"[TRANSLATION {kvp.Key}] {kvp.Value}")
                Next
            End If
        Catch ex As Exception
            WriteDebugLog($"[TRANSLATE ERROR] {ex.Message}")
        End Try

        ' Include original text for source-language clients
        translations(sourceLang) = line

        ' Re-check: translate any new languages that appeared during the await
        Dim currentTargets = SubtitleSvc?.GetActiveTranslationLanguages()
        If currentTargets IsNot Nothing Then
            Dim missing As New List(Of String)
            For Each t In currentTargets
                If Not translations.ContainsKey(t) Then missing.Add(t)
            Next
            If missing.Count > 0 Then
                WriteDebugLog($"[CATCH-UP] new targets: {String.Join(",", missing)}")
                Try
                    Dim extra = Await _translationService.TranslateAsync(line, sourceLang, missing)
                    If extra IsNot Nothing Then
                        For Each kvp In extra
                            translations(kvp.Key) = kvp.Value
                        Next
                    End If
                Catch ex As Exception
                    WriteDebugLog($"[CATCH-UP ERROR] {ex.Message}")
                End Try
            End If
        End If

        sw.Stop()
        WriteDebugLog($"[ROUND TRIP] {sw.ElapsedMilliseconds}ms")

        ' Build lang tags for display
        Dim langTags As New Dictionary(Of String, String)
        For Each kvp In translations
            langTags(kvp.Key) = NllbToShortCode(kvp.Key)
        Next

        ' Single atomic broadcast — sends the right text to each client based on their
        ' current language. No two-phase race condition.
        SubtitleSvc?.BroadcastCommitTranslated(line, sourceShort, translations, langTags)
    End Sub

    Private Sub FlushPendingCommits()
        ' Verify model is actually loaded before flushing
        If _translationService Is Nothing OrElse Not _translationService.IsModelLoaded Then Return

        Dim commits As List(Of String)
        SyncLock _pendingCommits
            If _pendingCommits.Count = 0 Then Return
            commits = New List(Of String)(_pendingCommits)
            _pendingCommits.Clear()
        End SyncLock

        WriteDebugLog($"[FLUSH] translating {commits.Count} buffered commits")
        For Each c In commits
            TranslateAndBroadcastAsync(c)
        Next
    End Sub

    Private Shared Function NllbToShortCode(nllbCode As String) As String
        If String.IsNullOrEmpty(nllbCode) Then Return "??"
        ' Extract first part before underscore and uppercase it (e.g. "eng_Latn" → "EN", "spa_Latn" → "ES")
        Dim prefix = nllbCode.Split("_"c)(0).ToUpperInvariant()
        Select Case prefix
            Case "ENG" : Return "EN"
            Case "SPA" : Return "ES"
            Case "FRA" : Return "FR"
            Case "DEU" : Return "DE"
            Case "POR" : Return "PT"
            Case "ITA" : Return "IT"
            Case "CAT" : Return "CA"
            Case "RON" : Return "RO"
            Case "NLD" : Return "NL"
            Case "POL" : Return "PL"
            Case "RUS" : Return "RU"
            Case "UKR" : Return "UK"
            Case "ZHO" : Return "ZH"
            Case "JPN" : Return "JA"
            Case "KOR" : Return "KO"
            Case "ARB" : Return "AR"
            Case "SWE" : Return "SV"
            Case "NOB" : Return "NO"
            Case "DAN" : Return "DA"
            Case "FIN" : Return "FI"
            Case "HUN" : Return "HU"
            Case "CES" : Return "CS"
            Case "SLK" : Return "SK"
            Case "SLV" : Return "SL"
            Case "HRV" : Return "HR"
            Case "SRP" : Return "SR"
            Case "BUL" : Return "BG"
            Case "ELL" : Return "EL"
            Case "TUR" : Return "TR"
            Case "LIT" : Return "LT"
            Case "LVS" : Return "LV"
            Case "EST" : Return "ET"
            Case "AFR" : Return "AF"
            Case "AMH" : Return "AM"
            Case "HYE" : Return "HY"
            Case "AZJ" : Return "AZ"
            Case "EUS" : Return "EU"
            Case "BEL" : Return "BE"
            Case "BEN" : Return "BN"
            Case "BOS" : Return "BS"
            Case "CYM" : Return "CY"
            Case "PES" : Return "FA"
            Case "GLG" : Return "GL"
            Case "KAT" : Return "KA"
            Case "GUJ" : Return "GU"
            Case "HAT" : Return "HT"
            Case "HAU" : Return "HA"
            Case "HEB" : Return "HE"
            Case "HIN" : Return "HI"
            Case "ISL" : Return "IS"
            Case "IND" : Return "ID"
            Case "JAV" : Return "JW"
            Case "KAN" : Return "KN"
            Case "KAZ" : Return "KK"
            Case "KHM" : Return "KM"
            Case "LAO" : Return "LO"
            Case "LTZ" : Return "LB"
            Case "MKD" : Return "MK"
            Case "ZSM" : Return "MS"
            Case "MAL" : Return "ML"
            Case "MLT" : Return "MT"
            Case "MRI" : Return "MI"
            Case "MAR" : Return "MR"
            Case "KHK" : Return "MN"
            Case "MYA" : Return "MY"
            Case "NPI" : Return "NE"
            Case "PBT" : Return "PS"
            Case "PAN" : Return "PA"
            Case "SND" : Return "SD"
            Case "SIN" : Return "SI"
            Case "SNA" : Return "SN"
            Case "SOM" : Return "SO"
            Case "SUN" : Return "SU"
            Case "SWH" : Return "SW"
            Case "TGL" : Return "TL"
            Case "TGK" : Return "TG"
            Case "TAM" : Return "TA"
            Case "TAT" : Return "TT"
            Case "TEL" : Return "TE"
            Case "THA" : Return "TH"
            Case "TUK" : Return "TK"
            Case "URD" : Return "UR"
            Case "UZN" : Return "UZ"
            Case "VIE" : Return "VI"
            Case "YOR" : Return "YO"
            Case "ZUL" : Return "ZU"
            Case Else : Return prefix.Substring(0, Math.Min(2, prefix.Length))
        End Select
    End Function

    Private Shared Function WhisperToNllbCode(whisperLang As String) As String
        ' Map whisper ISO 639-1 codes to NLLB codes
        Select Case whisperLang.ToLowerInvariant()
            Case "en" : Return "eng_Latn"
            Case "es" : Return "spa_Latn"
            Case "fr" : Return "fra_Latn"
            Case "de" : Return "deu_Latn"
            Case "pt" : Return "por_Latn"
            Case "it" : Return "ita_Latn"
            Case "ca" : Return "cat_Latn"
            Case "ro" : Return "ron_Latn"
            Case "nl" : Return "nld_Latn"
            Case "pl" : Return "pol_Latn"
            Case "ru" : Return "rus_Cyrl"
            Case "uk" : Return "ukr_Cyrl"
            Case "zh" : Return "zho_Hans"
            Case "ja" : Return "jpn_Jpan"
            Case "ko" : Return "kor_Hang"
            Case "ar" : Return "arb_Arab"
            Case "sv" : Return "swe_Latn"
            Case "no" : Return "nob_Latn"
            Case "da" : Return "dan_Latn"
            Case "fi" : Return "fin_Latn"
            Case "hu" : Return "hun_Latn"
            Case "cs" : Return "ces_Latn"
            Case "sk" : Return "slk_Latn"
            Case "sl" : Return "slv_Latn"
            Case "hr" : Return "hrv_Latn"
            Case "sr" : Return "srp_Cyrl"
            Case "bg" : Return "bul_Cyrl"
            Case "el" : Return "ell_Grek"
            Case "tr" : Return "tur_Latn"
            Case "lt" : Return "lit_Latn"
            Case "lv" : Return "lvs_Latn"
            Case "et" : Return "est_Latn"
            Case "af" : Return "afr_Latn"
            Case "am" : Return "amh_Ethi"
            Case "hy" : Return "hye_Armn"
            Case "az" : Return "azj_Latn"
            Case "eu" : Return "eus_Latn"
            Case "be" : Return "bel_Cyrl"
            Case "bn" : Return "ben_Beng"
            Case "bs" : Return "bos_Latn"
            Case "cy" : Return "cym_Latn"
            Case "fa" : Return "pes_Arab"
            Case "gl" : Return "glg_Latn"
            Case "ka" : Return "kat_Geor"
            Case "gu" : Return "guj_Gujr"
            Case "ht" : Return "hat_Latn"
            Case "ha" : Return "hau_Latn"
            Case "he" : Return "heb_Hebr"
            Case "hi" : Return "hin_Deva"
            Case "is" : Return "isl_Latn"
            Case "id" : Return "ind_Latn"
            Case "jw" : Return "jav_Latn"
            Case "kn" : Return "kan_Knda"
            Case "kk" : Return "kaz_Cyrl"
            Case "km" : Return "khm_Khmr"
            Case "lo" : Return "lao_Laoo"
            Case "lb" : Return "ltz_Latn"
            Case "mk" : Return "mkd_Cyrl"
            Case "ms" : Return "zsm_Latn"
            Case "ml" : Return "mal_Mlym"
            Case "mt" : Return "mlt_Latn"
            Case "mi" : Return "mri_Latn"
            Case "mr" : Return "mar_Deva"
            Case "mn" : Return "khk_Cyrl"
            Case "my" : Return "mya_Mymr"
            Case "ne" : Return "npi_Deva"
            Case "ps" : Return "pbt_Arab"
            Case "pa" : Return "pan_Guru"
            Case "sd" : Return "snd_Arab"
            Case "si" : Return "sin_Sinh"
            Case "sn" : Return "sna_Latn"
            Case "so" : Return "som_Latn"
            Case "su" : Return "sun_Latn"
            Case "sw" : Return "swh_Latn"
            Case "tl" : Return "tgl_Latn"
            Case "tg" : Return "tgk_Cyrl"
            Case "ta" : Return "tam_Taml"
            Case "tt" : Return "tat_Cyrl"
            Case "te" : Return "tel_Telu"
            Case "th" : Return "tha_Thai"
            Case "tk" : Return "tuk_Latn"
            Case "ur" : Return "urd_Arab"
            Case "uz" : Return "uzn_Latn"
            Case "vi" : Return "vie_Latn"
            Case "yo" : Return "yor_Latn"
            Case "zu" : Return "zul_Latn"
            Case Else : Return "eng_Latn"
        End Select
    End Function

    Private Shared Function IsGarbageCommit(line As String) As Boolean
        Dim trimmed = line.Trim().TrimEnd("."c).Trim()
        ' Empty or just punctuation
        If String.IsNullOrWhiteSpace(trimmed) Then Return True
        ' Single short word (whisper hallucination during silence)
        If trimmed.Length <= 3 AndAlso Not trimmed.Any(Function(c) Char.IsDigit(c)) Then Return True
        ' Known whisper artifacts
        If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then Return True
        Return False
    End Function

    Friend Shared Function GetPipelineLogPath() As String
        Return IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMdd}_pipeline-debug.log")
    End Function

    Friend Shared Sub WriteDebugLog(msg As String)
        Try
            Dim logPath = GetPipelineLogPath()
            IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}{Environment.NewLine}")
        Catch
        End Try

        ' Also send to the unified log panel in the UI
        Try
            Dim frm = TryCast(Application.OpenForms.OfType(Of FormMain)().FirstOrDefault(), FormMain)
            If frm IsNot Nothing Then
                ' Route to appropriate source category for log filtering
                Dim source = "Debug"
                If msg.StartsWith("[Server]") Then
                    source = "Server"
                ElseIf msg.StartsWith("[Live]") Then
                    source = "Live"
                End If

                ' Source-aware colors (visible on both light and dark backgrounds)
                Dim color As Drawing.Color
                If msg.Contains("[ERROR]") Then
                    color = Drawing.Color.Red
                ElseIf msg.Contains("[WARN]") Then
                    color = Drawing.Color.Orange
                Else
                    Select Case source
                        Case "Server" : color = Drawing.Color.FromArgb(0, 120, 180)   ' blue
                        Case "Live" : color = Drawing.Color.FromArgb(0, 140, 80)       ' green
                        Case Else : color = Drawing.Color.FromArgb(100, 100, 100)      ' grey
                    End Select
                End If
                frm.AppendUnifiedLog(source, msg, color)
            End If
        Catch
        End Try
    End Sub

    Private Function GetCurrentSourceNllbLang() As String
        ' Fallback when faster-whisper doesn't provide a detected language
        Dim inputLang = ""
        If cboLiveInputLang.InvokeRequired Then
            inputLang = CStr(cboLiveInputLang.Invoke(Function() LangCodeFromDisplay(If(cboLiveInputLang.SelectedItem, "auto").ToString())))
        Else
            If cboLiveInputLang.SelectedItem IsNot Nothing Then inputLang = LangCodeFromDisplay(cboLiveInputLang.SelectedItem.ToString())
        End If

        If inputLang = "auto" Then inputLang = "es"
        Return TranslationService.WhisperToNllbLang(inputLang)
    End Function

    Private _translationSetupPrompted As Boolean = False

    Private Sub HandleActiveLanguagesChanged(sender As Object, e As EventArgs)
        Dim targets = SubtitleSvc?.GetActiveTranslationLanguages()
        If targets Is Nothing OrElse targets.Count = 0 Then
            ' No translation clients — reset unload timer
            ResetTranslationUnloadTimer()
            Return
        End If

        ' Cancel unload timer if active
        _translationUnloadTimer?.Change(Timeout.Infinite, Timeout.Infinite)

        ' If translation service is already running, nothing to do — language routing is handled per-client
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then Return

        ' Check if deps are installed first
        Dim deps = TranslationService.CheckDependenciesInstalled()
        If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
            AppendServerLog("Translation dependencies not installed. Run dependency check from Settings tab.")
            Return
        End If
        StartTranslationService()
    End Sub

    Private _translationStarting As Boolean = False

    Private Sub StartTranslationService()
        If Not _config.TranslationEnabled Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: TranslationEnabled=False, skipping")
            Return
        End If

        ' Already running or in the process of starting — nothing to do
        If _translationService IsNot Nothing AndAlso _translationService.IsRunning Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: already running, skipping")
            Return
        End If
        If _translationStarting Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: already starting, skipping")
            Return
        End If
        _translationStarting = True

        ' Stop any existing (non-running) service first to clean up
        If _translationService IsNot Nothing Then
            WriteDebugLog("[TRANSLATE] StartTranslationService: stopping existing service before creating new one")
            _translationService.Stop()
            _translationService = Nothing
        End If

        ' Append session header (keep previous logs)
        Try
            Dim logPath = GetPipelineLogPath()
            Dim ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            IO.File.AppendAllText(logPath, $"{Environment.NewLine}=== Session started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} v{ver.Major}.{ver.Minor}.{ver.Build} ==={Environment.NewLine}")
        Catch
        End Try

        _translationService = New TranslationService()
        AddHandler _translationService.StatusChanged, Sub(s, msg)
                                                          AppendServerLog(msg)
                                                          If msg.Contains("model loaded") Then FlushPendingCommits()
                                                      End Sub

        Dim modelPath = _config.TranslationModelPath
        Dim port = _config.TranslationPort
        Dim device = _config.TranslationDevice
        Dim glossaryPath = _config.TranslationGlossaryPath

        WriteDebugLog($"[TRANSLATE] StartTranslationService: port={port}, device={device}, modelPath={modelPath}")
        _translationService.Start(port, modelPath, device, glossaryPath)
        _translationStarting = False
        AppendServerLog("Translation service starting...")
    End Sub

    Private Sub StopTranslationService()
        _translationStarting = False
        _translationUnloadTimer?.Dispose()
        _translationUnloadTimer = Nothing
        _translationService?.Stop()
        _translationService = Nothing
        SyncLock _pendingCommits
            _pendingCommits.Clear()
        End SyncLock
    End Sub

    Private Sub ResetTranslationUnloadTimer()
        If _translationService Is Nothing OrElse Not _translationService.IsRunning Then Return

        Dim minutes = _config.TranslationUnloadMinutes
        If minutes <= 0 Then Return

        _translationUnloadTimer?.Dispose()
        _translationUnloadTimer = New System.Threading.Timer(
            Sub(state)
                Dim tgts = SubtitleSvc?.GetActiveTranslationLanguages()
                If tgts Is Nothing OrElse tgts.Count = 0 Then
                    AppendServerLog($"No translation clients for {minutes} min, unloading model...")
                    _translationService?.UnloadModelAsync().Wait()
                End If
            End Sub, Nothing, TimeSpan.FromMinutes(minutes), Timeout.InfiniteTimeSpan)
    End Sub

    Private Sub btnSetupTranslation_Click(sender As Object, e As EventArgs) Handles btnSetupTranslation.Click
        CheckDependenciesAsync(manualCheck:=True)
    End Sub

    Private Async Sub UpdateTranslationButtonAsync()
        Dim deps = Await Task.Run(Function() TranslationService.CheckDependenciesInstalled())
        UpdateTranslationButton(deps)
    End Sub

    Private Sub UpdateTranslationButton(Optional deps As (pythonOk As Boolean, depsOk As Boolean, modelOk As Boolean)? = Nothing)
        If deps Is Nothing Then
            deps = TranslationService.CheckDependenciesInstalled()
        End If
        Dim d = deps.Value
        If d.pythonOk AndAlso d.depsOk AndAlso d.modelOk Then
            btnSetupTranslation.Text = "Translation Ready"
            btnSetupTranslation.Enabled = True
        Else
            btnSetupTranslation.Text = GetString("Btn_CheckToolUpdates")
            btnSetupTranslation.Enabled = True
        End If
    End Sub


    Private Sub UpdateDeviceCombo(devices As List(Of String))
        ' Prefer saved device from config, fall back to current selection
        Dim previousId = _config.LastLiveDeviceId
        If String.IsNullOrEmpty(previousId) AndAlso cboLiveDevice.SelectedItem IsNot Nothing Then
            Dim txt = cboLiveDevice.SelectedItem.ToString()
            Dim colonIdx = txt.IndexOf(":"c)
            If colonIdx > 0 Then previousId = txt.Substring(0, colonIdx).Trim()
        End If

        cboLiveDevice.Items.Clear()
        For Each d In devices
            cboLiveDevice.Items.Add(d)
        Next

        ' Try to re-select the previously selected device
        Dim found = False
        If previousId IsNot Nothing AndAlso previousId.Length > 0 Then
            For i = 0 To cboLiveDevice.Items.Count - 1
                If cboLiveDevice.Items(i).ToString().StartsWith(previousId & ":") Then
                    cboLiveDevice.SelectedIndex = i
                    found = True
                    Exit For
                End If
            Next
        End If
        If Not found AndAlso cboLiveDevice.Items.Count > 0 Then cboLiveDevice.SelectedIndex = 0
    End Sub

#End Region

#Region "Subtitle Server"

    Private Function GetLocalIpAddress() As String
        Try
            For Each addr In System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                If addr.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                    Dim ip = addr.ToString()
                    If Not ip.StartsWith("127.") Then Return ip
                End If
            Next
        Catch ex As Exception
            WriteDebugLog($"[WARN] GetLocalIpAddress: {ex.Message}")
        End Try
        Return "127.0.0.1"
    End Function

    Private Sub UpdateServerUi(running As Boolean)
        btnServerStart.Enabled = Not running
        btnServerStop.Enabled = running
        btnServerRestart.Enabled = running
        nudServerPort.Enabled = Not running
        btnCopyUrl.Enabled = running

        If running Then
            Dim ip = GetLocalIpAddress()
            Dim url = $"http://{ip}:{_serverPort}"
            lblServerStatus.Text = "Status: Running"
            lblServerStatus.ForeColor = Drawing.Color.Green
            lblServerUrl.Text = $"URL: {url}"
        Else
            lblServerStatus.Text = "Status: Stopped"
            lblServerStatus.ForeColor = Drawing.SystemColors.ControlText
            lblServerUrl.Text = "URL: (not running)"
            lblServerClients.Text = "Connected clients: 0"
        End If

        UpdateShellStatus()
    End Sub

    Private ReadOnly _serverLogBuffer As New System.Collections.Concurrent.ConcurrentQueue(Of String)
    Private _serverLogPending As Integer = 0
    Private Const ServerLogMaxLines As Integer = 2000

    Private Sub AppendServerLog(text As String)
        WriteDebugLog($"[Server] {text}")

        _serverLogBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss}] {text}")

        ' Coalesce rapid calls — only schedule one flush
        If Interlocked.CompareExchange(_serverLogPending, 1, 0) = 0 Then
            rtbServerLog.BeginInvoke(Sub() FlushServerLog())
        End If
    End Sub

    Private Sub FlushServerLog()
        Interlocked.Exchange(_serverLogPending, 0)

        Dim lines As New System.Text.StringBuilder()
        Dim line As String = Nothing
        While _serverLogBuffer.TryDequeue(line)
            lines.AppendLine(line)
        End While
        If lines.Length = 0 Then Return

        ' Suspend drawing to prevent flicker
        SendMessage(rtbServerLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
        Try
            rtbServerLog.AppendText(lines.ToString())

            ' Trim excess lines to prevent memory bloat
            If rtbServerLog.Lines.Length > ServerLogMaxLines Then
                Dim removeUpTo = rtbServerLog.GetFirstCharIndexFromLine(rtbServerLog.Lines.Length - ServerLogMaxLines)
                rtbServerLog.Select(0, removeUpTo)
                rtbServerLog.SelectedText = ""
            End If
        Finally
            SendMessage(rtbServerLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            rtbServerLog.Invalidate()
        End Try

        ' Reliable scroll to bottom
        SendMessage(rtbServerLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
    End Sub

    Private Sub btnServerStart_Click(sender As Object, e As EventArgs) Handles btnServerStart.Click
        SaveUiToConfig()
        StartSubtitleServer()
    End Sub

    Private Sub StartSubtitleServer()
        Dim port = CInt(nudServerPort.Value)
        _serverPort = port
        If _config.AllowFirewall Then EnsureFirewallRule(port)

        Try
            _kestrelHost = New KestrelHost()
            AddHandler _kestrelHost.StatusChanged, Sub(s, msg)
                                                       AppendServerLog(msg)
                                                   End Sub

            Dim resolvedBiblesDir = AppConfig.ResolvePath(If(_config.BiblesDirectory, ".\Bibles"))
            WriteDebugLog($"[BIBLE] Server startup: config.BiblesDirectory={_config.BiblesDirectory}, resolved={resolvedBiblesDir}, exists={IO.Directory.Exists(resolvedBiblesDir)}")
            If IO.Directory.Exists(resolvedBiblesDir) Then
                Try
                    Dim dbFiles = IO.Directory.GetFiles(resolvedBiblesDir, "*.db", IO.SearchOption.AllDirectories)
                    Dim sqliteFiles = IO.Directory.GetFiles(resolvedBiblesDir, "*.sqlite", IO.SearchOption.AllDirectories)
                    WriteDebugLog($"[BIBLE] Found {dbFiles.Length} .db files and {sqliteFiles.Length} .sqlite files in {resolvedBiblesDir}")
                    For Each f In dbFiles
                        WriteDebugLog($"[BIBLE]   DB: {f}")
                    Next
                    For Each f In sqliteFiles
                        WriteDebugLog($"[BIBLE]   SQLite: {f}")
                    Next
                Catch ex As Exception
                    WriteDebugLog($"[BIBLE] Error listing files: {ex.Message}")
                End Try
            End If

            Dim kestrelOptions As New ServerOptions() With {
                .HttpPort = port,
                .AllowRemote = _config.AllowFirewall,
                .BgColor = _config.SubtitleBgColor,
                .FgColor = _config.SubtitleFgColor,
                .AdminPin = If(_config.AdminPin, ""),
                .BiblesDirectory = resolvedBiblesDir,
                .TtsBackends = If(_config.TtsBackends, "")
            }

            _kestrelHost.Start(kestrelOptions,
                Sub(msg) AppendServerLog(msg))

            ' Wire up remote command handler so /api/control routes to FormMain
            EndpointRegistration.RemoteCommandHandler = Sub(cmd)
                                                            Me.BeginInvoke(Sub() HandleRemoteCommand(cmd))
                                                        End Sub

            ' Configure Kestrel's SubtitleService with events and callbacks
            Dim svc = SubtitleSvc
            If svc IsNot Nothing Then
                svc.BgColor = _config.SubtitleBgColor
                svc.FgColor = _config.SubtitleFgColor
                svc.TuneCallback = AddressOf GetTuneJson
                svc.IsLiveRunning = (_liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning)
                svc.InputLanguage = "auto"

                AddHandler svc.StatusChanged, Sub(s, msg)
                                                  AppendServerLog(msg)
                                                  If Me.InvokeRequired Then
                                                      Me.BeginInvoke(Sub()
                                                                         Dim sv = SubtitleSvc
                                                                         If sv IsNot Nothing Then
                                                                             lblServerClients.Text = $"Connected clients: {sv.ConnectedClients}"
                                                                         End If
                                                                         UpdateShellStatus()
                                                                     End Sub)
                                                  Else
                                                      lblServerClients.Text = $"Connected clients: {svc.ConnectedClients}"
                                                      UpdateShellStatus()
                                                  End If
                                              End Sub

                AddHandler svc.RemoteCommand, Sub(s, cmd)
                                                  Me.BeginInvoke(Sub() HandleRemoteCommand(cmd))
                                              End Sub

                AddHandler svc.ActiveLanguagesChanged, AddressOf HandleActiveLanguagesChanged
                AddHandler svc.InputLanguageChanged, Sub(s, lang)
                                                         Me.BeginInvoke(Sub() HandleInputLanguageChanged(lang))
                                                     End Sub
                AddHandler svc.LogMessage, Sub(s, msg)
                                               WriteDebugLog(msg)
                                           End Sub
            End If

            UpdateServerUi(True)
            AppendServerLog($"Server started on HTTP:{port} HTTPS:{port + 1}")
            NavigateLivePreview(port)
            NavigateBibleView()
            Dim localIp = GetLocalIpAddress()
            AppendServerLog($"Phones should open: https://{localIp}:{port + 1}")
            AppendServerLog($"(Accept the certificate warning on first visit)")
        Catch ex As Exception
            AppendServerLog($"ERROR: {ex.Message}")
            AppendServerLog("Tip: Try running as Administrator, or use a different port.")
            _kestrelHost = Nothing
        End Try
    End Sub

    Private Sub NavigateLivePreview(port As Integer)
        Try
            Dim bust = DateTime.Now.Ticks
            wvLiveClients.Source = New Uri($"http://127.0.0.1:{port}/?preview=1&_cb={bust}")
        Catch ex As Exception
            AppendServerLog($"Live preview: {ex.Message}")
        End Try
    End Sub

    Private Sub btnServerStop_Click(sender As Object, e As EventArgs) Handles btnServerStop.Click
        EndpointRegistration.RemoteCommandHandler = Nothing
        Try : _kestrelHost?.Stop() : Catch : End Try
        _kestrelHost = Nothing
        _serverPort = 0
        UpdateServerUi(False)
        AppendServerLog("Server stopped.")
    End Sub

    Private Sub btnServerRestart_Click(sender As Object, e As EventArgs) Handles btnServerRestart.Click
        EndpointRegistration.RemoteCommandHandler = Nothing
        Try : _kestrelHost?.Stop() : Catch : End Try
        _kestrelHost = Nothing
        _serverPort = 0
        AppendServerLog("Restarting server...")
        btnServerStart_Click(sender, e)
    End Sub

    Private Sub btnSubtitleBg_Click(sender As Object, e As EventArgs) Handles btnSubtitleBg.Click
        Using dlg As New ColorDialog()
            dlg.Color = btnSubtitleBg.BackColor
            dlg.FullOpen = True
            If dlg.ShowDialog() = DialogResult.OK Then
                btnSubtitleBg.BackColor = dlg.Color
                _config.SubtitleBgColor = ColorToHex(dlg.Color)
                ConfigManager.Save(_config)
                Dim bgSvc = SubtitleSvc
                If bgSvc IsNot Nothing Then bgSvc.BgColor = _config.SubtitleBgColor
            End If
        End Using
    End Sub

    Private Sub btnSubtitleFg_Click(sender As Object, e As EventArgs) Handles btnSubtitleFg.Click
        Using dlg As New ColorDialog()
            dlg.Color = btnSubtitleFg.BackColor
            dlg.FullOpen = True
            If dlg.ShowDialog() = DialogResult.OK Then
                btnSubtitleFg.BackColor = dlg.Color
                _config.SubtitleFgColor = ColorToHex(dlg.Color)
                ConfigManager.Save(_config)
                Dim fgSvc = SubtitleSvc
                If fgSvc IsNot Nothing Then fgSvc.FgColor = _config.SubtitleFgColor
            End If
        End Using
    End Sub

    Private Sub cboSubtitleFont_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSubtitleFont.SelectedIndexChanged
        ApplyLiveOutputFont()
    End Sub

    Private Sub nudSubtitleSize_ValueChanged(sender As Object, e As EventArgs) Handles nudSubtitleSize.ValueChanged
        ApplyLiveOutputFont()
    End Sub

    Private Sub chkSubtitleBold_CheckedChanged(sender As Object, e As EventArgs) Handles chkSubtitleBold.CheckedChanged
        ApplyLiveOutputFont()
    End Sub

    Private Sub ApplyLiveOutputFont()
        If nudSubtitleSize Is Nothing OrElse cboSubtitleFont Is Nothing OrElse chkSubtitleBold Is Nothing Then Return
        If Not _isInitializing Then
            _config.SubtitleFontFamily = If(cboSubtitleFont.SelectedItem?.ToString(), "Segoe UI")
            _config.SubtitleFontSize = CSng(nudSubtitleSize.Value)
            _config.SubtitleFontBold = chkSubtitleBold.Checked
            ConfigManager.Save(_config)
        End If
    End Sub

    Private Sub btnCopyUrl_Click(sender As Object, e As EventArgs) Handles btnCopyUrl.Click
        CopyPhoneUrl()
    End Sub

    Private Sub CopyPhoneUrl()
        If _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning Then
            Dim url = $"https://{GetLocalIpAddress()}:{_serverPort + 1}"
            Clipboard.SetText(url)
            AppendServerLog("URL copied to clipboard.")
        End If
    End Sub

    Private Sub VerifyAllPaths()
        Dim sb As New Text.StringBuilder()
        Dim allOk = True

        Dim checks As (Label As String, Path As String, IsDir As Boolean)() = {
            ("Whisper", Models.AppConfig.ResolvePath(_config.PathWhisper), False),
            ("Whisper Model", Models.AppConfig.ResolvePath(_config.PathModel), False),
            ("Audio Model", Models.AppConfig.ResolvePath(_config.PathModelAudio), False),
            ("FFmpeg", Models.AppConfig.ResolvePath(_config.PathFfmpeg), False),
            ("FFprobe", Models.AppConfig.ResolvePath(_config.PathFfprobe), False),
            ("yt-dlp", Models.AppConfig.ResolvePath(_config.PathYtdlp), False),
            ("Bibles", Models.AppConfig.ResolvePath(_config.BiblesDirectory), True)
        }

        For Each item In checks
            Dim resolved = item.Path
            Dim exists = If(item.IsDir, IO.Directory.Exists(resolved), IO.File.Exists(resolved))
            If String.IsNullOrWhiteSpace(resolved) Then
                sb.AppendLine($"  NOT SET: {item.Label}")
                allOk = False
            ElseIf Not exists Then
                sb.AppendLine($"  MISSING: {item.Label} → {resolved}")
                allOk = False
            Else
                sb.AppendLine($"  OK: {item.Label}")
            End If
        Next

        If allOk Then
            MessageBox.Show("All paths verified successfully." & Environment.NewLine & Environment.NewLine & sb.ToString(),
                "Verify Paths", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            MessageBox.Show("Some paths are missing or not set:" & Environment.NewLine & Environment.NewLine & sb.ToString(),
                "Verify Paths", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

#End Region

    Private Shared Function BuildTimeString(hh As String, mm As String, ss As String) As String
        hh = hh.Trim()
        mm = mm.Trim()
        ss = ss.Trim()
        If hh.Length = 0 AndAlso mm.Length = 0 AndAlso ss.Length = 0 Then Return ""
        Return $"{hh.PadLeft(2, "0"c)}:{mm.PadLeft(2, "0"c)}:{ss.PadLeft(2, "0"c)}"
    End Function

    Private Sub TimeBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
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
                Catch
                    ' Access denied or already exited — ignore
                End Try
            Next
        Catch
        End Try
    End Sub

    Private Sub FormMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Always save settings
        SaveUiToConfig()

        If Not _exitForReal Then
            ' Minimize to system tray instead of closing
            e.Cancel = True
            Me.Hide()
            Return
        End If

        ' Guarantee process exit even if cleanup hangs
        Dim exitTimer As New System.Threading.Timer(
            Sub(state) Environment.Exit(0), Nothing, 15000, Timeout.Infinite)

        ' Real exit — clean up everything
        _cts?.Cancel()

        ' Shut down servers in parallel with a combined timeout
        Dim shutdownTask = Task.Run(
            Sub()
                Try : _liveRunner?.ShutdownServer() : Catch : End Try
                Try : StopTranslationService() : Catch : End Try
                Try : _kestrelHost?.Stop() : Catch : End Try
            End Sub)
        shutdownTask.Wait(10000)

        trayIcon.Visible = False
        trayIcon.Dispose()

        ' Kill any orphaned python-embed processes that survived graceful shutdown
        KillOrphanedPythonProcesses()

        ' Offer to clean up today's working folders — show form so MessageBox is visible
        Try
            Dim outputRoot = AppConfig.ResolvePath(_config.PathOutputRoot)
            If Not String.IsNullOrWhiteSpace(outputRoot) AndAlso Directory.Exists(outputRoot) Then
                Dim todayPrefix = DateTime.Now.ToString("yyyy-MM-dd")
                Dim todayFolders = Directory.GetDirectories(outputRoot).
                    Where(Function(d) Path.GetFileName(d).StartsWith(todayPrefix)).
                    ToArray()

                If todayFolders.Length > 0 Then
                    Me.Show()
                    Me.BringToFront()
                    Dim folderNames = String.Join(Environment.NewLine, todayFolders.Select(Function(d) "  " & Path.GetFileName(d)))
                    Dim msg = $"Delete {todayFolders.Length} working folder(s) from today?" & Environment.NewLine & Environment.NewLine & folderNames
                    Dim result = MessageBox.Show(msg, GetString("Msg_CleanUp"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                    If result = DialogResult.Yes Then
                        For Each folder In todayFolders
                            Try
                                Directory.Delete(folder, True)
                            Catch
                            End Try
                        Next
                    End If
                End If
            End If
        Catch
        End Try

        ' Force process exit to ensure no background tasks keep the process alive
        exitTimer.Dispose()
        Environment.Exit(0)
    End Sub

    Private Shared Function ColorToHex(c As Drawing.Color) As String
        Return $"#{c.R:X2}{c.G:X2}{c.B:X2}"
    End Function

    Private Shared Sub EnsureFirewallRule(port As Integer)
        Const ruleName As String = "EveryTongue Subtitle Server"
        Dim httpsPort = port + 1

        ' Build a single command that deletes old rules then adds new ones (HTTP + HTTPS)
        Dim cmd = $"advfirewall firewall delete rule name=""{ruleName}"" & " &
                  $"netsh advfirewall firewall add rule name=""{ruleName}"" dir=in action=allow protocol=TCP localport={port},{httpsPort}"

        ' First try without elevation
        Try
            Dim psi As New ProcessStartInfo() With {
                .FileName = "netsh",
                .Arguments = cmd,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }
            Dim p = Process.Start(psi)
            p.WaitForExit(5000)
            If p.ExitCode = 0 Then Return
        Catch
        End Try

        ' Non-elevated failed — try with UAC elevation via cmd /c
        Try
            Dim fullCmd = $"/c netsh advfirewall firewall delete rule name=""{ruleName}"" & " &
                          $"netsh advfirewall firewall add rule name=""{ruleName}"" dir=in action=allow protocol=TCP localport={port},{httpsPort}"
            Dim psi As New ProcessStartInfo() With {
                .FileName = "cmd.exe",
                .Arguments = fullCmd,
                .Verb = "runas",
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .WindowStyle = ProcessWindowStyle.Hidden
            }
            Dim p = Process.Start(psi)
            p?.WaitForExit(10000)
        Catch
            ' User declined UAC or elevation not available — server still works on localhost
        End Try
    End Sub
End Class
