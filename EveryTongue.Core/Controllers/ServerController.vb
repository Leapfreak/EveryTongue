Imports System.Diagnostics
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Server
Imports EveryTongue.Services.Infrastructure

Namespace Controllers
    ''' <summary>
    ''' Manages the Kestrel subtitle server lifecycle — start/stop/restart,
    ''' subtitle appearance, firewall rules, URL copy.
    ''' Extracted from FormMain.
    ''' </summary>
    Friend Class ServerController

        ' Callbacks — deliberately WinForms-free signatures: the head maps LogSeverity
        ' to MessageBox icons; a headless host maps them to log lines.
        Private ReadOnly _config As AppConfig
        Private ReadOnly _updateShellStatus As Action
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _notify As Action(Of String, String, LogSeverity)

        ''' <summary>Head-supplied clipboard write (WinForms Clipboard on desktop; Nothing headless → URL is logged instead).</summary>
        Public Property ClipboardSetter As Action(Of String)
        ''' <summary>Head-supplied local TTS playback sink factory (NAudio on Windows); flows into ServerOptions.</summary>
        Public Property TtsSinkFactory As Func(Of Integer, Single, Services.Audio.ITtsAudioSink)
        ''' <summary>Head-supplied audio output device enumerator for /tts/devices; flows into ServerOptions.</summary>
        Public Property AudioDeviceProvider As Func(Of List(Of Services.Audio.AudioOutputDevice))

        ' State
        Private _kestrelHost As KestrelHost
        Private _serverPort As Integer = 0

        Public ReadOnly Property KestrelHost As KestrelHost
            Get
                Return _kestrelHost
            End Get
        End Property

        Public ReadOnly Property Port As Integer
            Get
                Return _serverPort
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning
            End Get
        End Property

        Public Sub New(config As AppConfig,
                       updateShellStatus As Action, log As Action(Of String),
                       notify As Action(Of String, String, LogSeverity))
            _config = config
            _updateShellStatus = updateShellStatus
            _log = log
            _notify = notify
        End Sub

        ''' <summary>
        ''' Start the Kestrel server. Caller provides live-specific callbacks
        ''' that wire SubtitleService events to the live session.
        ''' </summary>
        Public Sub StartServer(Optional configureSubtitleSvc As Action(Of Services.Interfaces.ISubtitleService) = Nothing)
            Dim port = _config.SubtitleServerPort
            _serverPort = port
            If _config.AllowFirewall Then EnsureFirewallRule(port)

            Try
                AppLogger.Log(LogEvents.SERVER_STARTING, "StartServer: creating KestrelHost...")
                _kestrelHost = New KestrelHost()
                AddHandler _kestrelHost.StatusChanged, Sub(s, msg) _log($"[Server] {msg}")

                Dim resolvedBiblesDir = AppConfig.ResolvePath(If(_config.BiblesDirectory, ".\Bibles"))

                Dim kestrelOptions As New ServerOptions() With {
                    .HttpPort = port,
                    .AllowRemote = _config.AllowFirewall,
                    .BgColor = _config.SubtitleBgColor,
                    .FgColor = _config.SubtitleFgColor,
                    .AdminPin = If(_config.AdminPin, ""),
                    .BiblesDirectory = resolvedBiblesDir,
                    .TtsBackends = If(_config.TtsBackends, ""),
                    .ShowBibleCopyright = _config.ShowBibleCopyright,
                    .LiveServerPort = _config.LiveServerPort,
                    .FfmpegPath = Models.AppConfig.ResolvePath(_config.PathFfmpeg),
                    .WhisperModelPath = Models.AppConfig.ResolvePath(
                        If(Services.Stt.SttBackendRegistry.Find(If(_config.SttBackend, ""))?.ModelPathFromConfig?.Invoke(_config),
                           _config.PathWhisperCppModel)),
                    .WhisperComputeType = If(_config.LiveComputeType, "int8_float16"),
                    .WhisperUseCpu = _config.NoGpu,
                    .WhisperServerPath = Models.AppConfig.ResolvePath(_config.PathWhisperServer),
                    .WhisperServerPort = _config.WhisperServerPort,
                    .SttBackend = If(_config.SttBackend, "whisper-cpp-vulkan"),
                    .SileroVadModelPath = Models.AppConfig.ResolvePath(_config.PathSileroVadModel),
                    .BeamSize = _config.BeamSize,
                    .BestOf = _config.BestOf,
                    .SttApiKey = _config.GetSttApiKey(If(_config.SttBackend, "")),
                    .TranslationConcurrency = _config.TranslationConcurrency,
                    .GlossaryFilePath = Models.AppConfig.ResolvePath(
                        If(_config.TranslationGlossaryPath, ".\translate-server\glossary.json")),
                    .ProfanityFilePath = Models.AppConfig.ResolvePath(".\translate-server\profanity.json"),
                    .TtsConcurrency = _config.TtsConcurrency,
                    .TtsSinkFactory = TtsSinkFactory,
                    .AudioDeviceProvider = AudioDeviceProvider,
                    .ConferenceTemplates = _config.ConferenceTemplates
                }

                AppLogger.Log(LogEvents.SERVER_STARTING, $"StartServer: calling Start() on port {port}...")
                _kestrelHost.Start(kestrelOptions, Sub(msg) _log($"[Server] {msg}"))
                AppLogger.Log(LogEvents.SERVER_STARTING, $"StartServer: Start() returned, IsRunning={_kestrelHost.IsRunning}, Services IsNot Nothing={_kestrelHost.Services IsNot Nothing}")

                ' Push per-engine API keys into the cloud translation backends
                ' (own translation key first, else the companion STT engine's key)
                If _kestrelHost.Services IsNot Nothing Then
                    Services.Translation.TranslationBackendRegistry.ConfigureCloudApiKeys(_kestrelHost.Services, _config)
                    ' Same pass for cloud TTS backends (own TTS key first, else the
                    ' companion engine's key declared on the registry entry)
                    Services.Tts.TtsBackendRegistry.ConfigureCloudTtsKeys(_kestrelHost.Services, _config)
                End If

                ' Cloud usage tracker reads budgets live from config so Options
                ' changes apply without a server restart.
                Services.Translation.TranslationUsageTracker.BudgetProvider =
                    Function(backendKey) _config.GetTranslationCharBudget(backendKey)

                ' Let caller wire live-specific subtitle service events
                Dim svc = GetSubtitleService()
                If svc IsNot Nothing Then
                    svc.BgColor = _config.SubtitleBgColor
                    svc.FgColor = _config.SubtitleFgColor

                    AddHandler svc.StatusChanged, Sub(s, msg)
                                                      ' Route client lifecycle to structured Subtitle events (not the
                                                      ' event-0 [Legacy] catch-all) so the rate-limiter counts them
                                                      ' independently instead of lumping them with all other [Server] noise.
                                                      Dim evt = If(msg.StartsWith("Client disconnected", StringComparison.OrdinalIgnoreCase),
                                                          Services.Infrastructure.LogEvents.SUB_CLIENT_DISCONNECTED,
                                                          Services.Infrastructure.LogEvents.SUB_CLIENT_CONNECTED)
                                                      Services.Infrastructure.AppLogger.Log(evt, msg)
                                                      _updateShellStatus()
                                                  End Sub

                    AddHandler svc.LogMessage, Sub(s, msg) _log(msg)

                    configureSubtitleSvc?.Invoke(svc)
                End If

                _log($"[Server] Server started on HTTP:{port} HTTPS:{port + 1}")
                ' In a container our own IP is the unreachable bridge address — the
                ' host's LAN IP is unknowable from in here (network namespace isolation).
                If IO.File.Exists("/.dockerenv") Then
                    Dim pub = Environment.GetEnvironmentVariable("EVERYTONGUE_PUBLIC_HOST")
                    _log(If(String.IsNullOrEmpty(pub),
                        $"[Server] Phones should open: https://<host-machine-ip>:<port mapped to {port + 1}>",
                        $"[Server] Phones should open: https://{pub}"))
                Else
                    _log($"[Server] Phones should open: https://{GetLocalIpAddress()}:{port + 1}")
                End If
                _log("[Server] (Accept the certificate warning on first visit)")
                _updateShellStatus()
            Catch ex As Exception
                AppLogger.Log(LogEvents.SERVER_ERROR, $"StartServer FAILED: {ex.GetType().Name}: {ex.Message}")
                AppLogger.Log(LogEvents.SERVER_ERROR, $"StartServer stack: {ex.StackTrace}")
                _log($"[Server] ERROR: {ex.Message}")
                _log("[Server] Tip: Try running as Administrator, or use a different port.")
                _kestrelHost = Nothing
            End Try
        End Sub

        Public Sub StopServer()
            EndpointRegistration.RemoteCommandHandler = Nothing
            Try : _kestrelHost?.Dispose() : Catch : End Try
            _kestrelHost = Nothing
            _serverPort = 0
            _log("[Server] Server stopped.")
            _updateShellStatus()
        End Sub

        Private Sub RestartServer()
            EndpointRegistration.RemoteCommandHandler = Nothing
            Try : _kestrelHost?.Dispose() : Catch : End Try
            _kestrelHost = Nothing
            _serverPort = 0
            _log("[Server] Restarting server...")
            StartServer()
        End Sub

        Public Function GetSubtitleService() As Services.Interfaces.ISubtitleService
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ISubtitleService)), Services.Interfaces.ISubtitleService)
        End Function

        Public Function GetRoomManager() As Services.Rooms.RoomManager
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Rooms.RoomManager)), Services.Rooms.RoomManager)
        End Function

        Public Function GetMetricsService() As Services.Interfaces.IMetricsService
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.IMetricsService)), Services.Interfaces.IMetricsService)
        End Function

        Public Function GetTtsService() As Services.Interfaces.ITtsService
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITtsService)), Services.Interfaces.ITtsService)
        End Function

        Public Function GetTranslationOrchestrator() As Services.Interfaces.ITranslationService
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITranslationService)), Services.Interfaces.ITranslationService)
        End Function

        Public Function GetTtsCacheDirectory() As String
            Dim cache = TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Tts.TtsCache)), Services.Tts.TtsCache)
            Return cache?.CacheDirectory
        End Function

        Public Sub CopyPhoneUrl()
            If _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning Then
                Dim url = $"https://{GetLocalIpAddress()}:{_serverPort + 1}"
                If ClipboardSetter IsNot Nothing Then
                    ClipboardSetter.Invoke(url)
                    _log("[Server] URL copied to clipboard.")
                Else
                    _log($"[Server] Phone URL: {url}")   ' headless: no clipboard, log it
                End If
            End If
        End Sub

        ''' <summary>
        ''' Verifies all configured paths and logs results. Returns True if all OK.
        ''' </summary>
        Public Function VerifyAllPathsCore() As (AllOk As Boolean, Report As String)
            Dim sb As New Text.StringBuilder()
            Dim allOk = True

            Dim checks As (Label As String, Path As String, IsDir As Boolean)() = {
                ("Whisper", AppConfig.ResolvePath(_config.PathWhisper), False),
                ("Whisper Model", AppConfig.ResolvePath(_config.PathModel), False),
                ("Audio Model", AppConfig.ResolvePath(_config.PathModelAudio), False),
                ("FFmpeg", AppConfig.ResolvePath(_config.PathFfmpeg), False),
                ("FFprobe", AppConfig.ResolvePath(_config.PathFfprobe), False),
                ("yt-dlp", AppConfig.ResolvePath(_config.PathYtdlp), False),
                ("Bibles", AppConfig.ResolvePath(_config.BiblesDirectory), True)
            }

            _log("[VerifyPaths] Starting path verification...")
            For Each item In checks
                Dim resolved = item.Path
                Dim exists = If(item.IsDir, IO.Directory.Exists(resolved), IO.File.Exists(resolved))
                If String.IsNullOrWhiteSpace(resolved) Then
                    Dim line = $"  NOT SET: {item.Label}"
                    sb.AppendLine(line)
                    _log($"[VerifyPaths] {line}")
                    allOk = False
                ElseIf Not exists Then
                    Dim line = $"  MISSING: {item.Label} → {resolved}"
                    sb.AppendLine(line)
                    _log($"[VerifyPaths] {line}")
                    allOk = False
                Else
                    Dim line = $"  OK: {item.Label} → {resolved}"
                    sb.AppendLine(line)
                    _log($"[VerifyPaths] {line}")
                End If
            Next
            _log($"[VerifyPaths] Result: {If(allOk, "All paths OK", "Some paths missing or not set")}")

            Return (allOk, sb.ToString())
        End Function

        Public Sub VerifyAllPaths()
            Dim result = VerifyAllPathsCore()

            Dim lps = Services.Infrastructure.LanguagePackService.Instance
            If result.AllOk Then
                _notify?.Invoke(lps.GetString("Server_AllPathsOk") & Environment.NewLine & Environment.NewLine & result.Report,
                    lps.GetString("Msg_PathVerification"), LogSeverity.Info)
            Else
                _notify?.Invoke(lps.GetString("Server_PathsMissing") & Environment.NewLine & Environment.NewLine & result.Report,
                    lps.GetString("Msg_PathVerification"), LogSeverity.Warning)
            End If
        End Sub

        Friend Shared Function GetLocalIpAddress() As String
            Try
                For Each addr In System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                    If addr.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                        Dim ip = addr.ToString()
                        If Not ip.StartsWith("127.") Then Return ip
                    End If
                Next
            Catch ex As Exception
                AppLogger.Log(LogEvents.SERVER_ERROR, $"GetLocalIpAddress: {ex.Message}")
            End Try
            Return "127.0.0.1"
        End Function

        Private Shared Sub EnsureFirewallRule(port As Integer)
            ' netsh is Windows-only. Headless Linux/macOS/Docker hosts manage their own
            ' firewall (container port mapping / ufw) — silently skip.
            If Not OperatingSystem.IsWindows() Then Return
            Const ruleName As String = "EveryTongue Subtitle Server"
            Dim httpsPort = port + 1

            Dim cmd = $"advfirewall firewall delete rule name=""{ruleName}"" & " &
                      $"netsh advfirewall firewall add rule name=""{ruleName}"" dir=in action=allow protocol=TCP localport={port},{httpsPort}"

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
                ' Drain both pipes before WaitForExit to prevent pipe buffer deadlock
                Dim stderrTask = p.StandardError.ReadToEndAsync()
                p.StandardOutput.ReadToEnd()
                stderrTask.Wait()
                p.WaitForExit(5000)
                If p.ExitCode = 0 Then Return
            Catch ex As Exception
                AppLogger.Log(LogEvents.SERVER_ERROR, $"EnsureFirewallRule (netsh direct): {ex.Message}")
            End Try

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
            Catch ex As Exception
                AppLogger.Log(LogEvents.SERVER_ERROR, $"EnsureFirewallRule (elevated cmd): {ex.Message}")
            End Try
        End Sub

    End Class
End Namespace
