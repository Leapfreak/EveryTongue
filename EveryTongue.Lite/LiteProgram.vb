Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Headless console host: config + Kestrel + conference wiring, nothing else.
''' The online-only deployment (cloud STT + cloud translation) — Windows terminal,
''' Linux/Docker, or macOS. Mirrors FormMain's server bootstrap minus every
''' desktop concern (tray, dictation, workspaces, local NLLB sidecar).
''' </summary>
Public Module LiteProgram

    Private ReadOnly _exitEvent As New ManualResetEventSlim(False)
    Private _serverController As Controllers.ServerController
    Private _conferenceController As Controllers.ConferenceController

    Public Function Main(args As String()) As Integer
        Console.WriteLine($"EveryTongue Lite {Reflection.Assembly.GetEntryAssembly()?.GetName().Version} — headless online-only server")
        Console.WriteLine($"Config: {ConfigManager.ConfigDirectory}")

        Dim config = ConfigManager.Load()

        ' ── Logging: file (same structured pipeline as the desktop) + console ──
        AppLogger.LogDirectory = If(Environment.GetEnvironmentVariable("EVERYTONGUE_CONFIG_DIR") Is Nothing,
                                    If(config.LogsDirectory, ".\logs"),
                                    IO.Path.Combine(ConfigManager.ConfigDirectory, "logs"))
        AppLogger.Routing = If(config.LogRouting, LogRoutingConfig.CreateNormal())
        AppLogger.UiCallback = Sub(entry)
                                   ' Console is this host's "UI sink" — same routing rules as the desktop grid.
                                   Console.WriteLine($"{entry.Time:HH:mm:ss} [{entry.Category}] {entry.Message}")
                               End Sub

        ' ── Bibles: Lite uses <config-dir>\Bibles — drop .sqlite3 files there
        ' (or install from web settings → Bibles) and the phone Bible panel works.
        ' Created up-front so web downloads land on the persistent volume, never
        ' inside the container image (lost on update). A custom ABSOLUTE path from
        ' config that actually exists wins (set via the raw config editor); the
        ' relative default and Windows paths copied from a desktop install don't.
        Dim volumeBibles = IO.Path.Combine(ConfigManager.ConfigDirectory, "Bibles")
        Try
            If Not IO.Directory.Exists(volumeBibles) Then IO.Directory.CreateDirectory(volumeBibles)
        Catch ex As Exception
            AppLogger.Log(LogCategory.Config, LogSeverity.Warning, $"Could not create Bibles folder {volumeBibles}: {ex.Message}")
        End Try
        Dim configuredBibles = If(config.BiblesDirectory, "")
        Dim customBibles = configuredBibles <> "" AndAlso IO.Path.IsPathRooted(configuredBibles) AndAlso
                           IO.Directory.Exists(configuredBibles) AndAlso
                           Not String.Equals(IO.Path.GetFullPath(configuredBibles).TrimEnd("\"c, "/"c),
                                             IO.Path.GetFullPath(volumeBibles).TrimEnd("\"c, "/"c),
                                             StringComparison.OrdinalIgnoreCase)
        If customBibles Then
            Console.WriteLine($"  Bibles: {configuredBibles} (custom path from config)")
        ElseIf IO.Directory.Exists(volumeBibles) Then
            config.BiblesDirectory = volumeBibles
            Console.WriteLine($"  Bibles: {volumeBibles} ({IO.Directory.GetFiles(volumeBibles, "*.sqlite3", IO.SearchOption.AllDirectories).Length} file(s))")
        End If

        ' ── Online-only validation: warn LOUD but always start — a fresh install
        ' fixes all of this from the web settings page (/api/settings), which only
        ' exists once Kestrel is up. Exiting here would brick the bootstrap. ──
        If Not ValidateOnlineConfig(config) Then
            Console.Error.WriteLine()
            Console.Error.WriteLine("  >>> Configuration incomplete — the server is starting anyway so you")
            Console.Error.WriteLine("  >>> can finish setup in the browser: open /admin.html, enter the")
            Console.Error.WriteLine("  >>> PIN, and choose engines + paste API keys.")
            Console.Error.WriteLine()
        End If

        ' ── Server ──
        _serverController = New Controllers.ServerController(
            config,
            Sub() ' no shell status to update
            End Sub,
            Sub(m) AppLogger.Log(LogCategory.Server, LogSeverity.Info, m),
            Sub(msg, title, severity) AppLogger.Log(LogCategory.Server,
                If(severity >= LogSeverity.Warning, LogSeverity.Warning, LogSeverity.Info), $"{title}: {msg}"))
        ' No ClipboardSetter / TtsSinkFactory / AudioDeviceProvider — headless defaults
        ' (URL is logged; local TTS playback absent; /tts/devices returns []).
        _serverController.StartServer()

        If Not _serverController.IsRunning Then
            Console.Error.WriteLine("Server failed to start — see the log above.")
            Return 1
        End If

        ' ── Conference rooms: same controller as the desktop, marshalled inline
        '    (no UI thread to protect in a console host). ──
        _conferenceController = New Controllers.ConferenceController(
            config,
            Function() _serverController.GetSubtitleService(),
            Function() Nothing, ' local NLLB sidecar service — not available in Lite (online-only)
            Function() TryCast(_serverController.KestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ITranslationService)), Services.Interfaces.ITranslationService),
            Function() _serverController.GetRoomManager(),
            Function(roomId, engineKey) AcquireRoomTranslationBackend(roomId, engineKey),
            Sub(roomId) ' nothing to release — no offline sidecar pool in Lite
            End Sub,
            TryCast(_serverController.KestrelHost?.Services?.GetService(
                GetType(Services.Rooms.RoomReadinessNotifier)), Services.Rooms.RoomReadinessNotifier),
            Sub(m) AppLogger.Log(LogCategory.Conference, LogSeverity.Info, m),
            Sub(work) work())
        _conferenceController.WireEndpointHandlers()

        ' Browser settings (/api/settings): the ONLY config surface in Lite.
        Server.EndpointRegistration.SettingsConfigProvider = Function() config
        Server.EndpointRegistration.SettingsSaveHandler = Sub() ConfigManager.Save(config)
        If String.IsNullOrEmpty(config.AdminPin) OrElse config.AdminPin = "1234" Then
            Console.WriteLine("  NOTE: admin PIN is the default (1234) — open /admin.html in the")
            Console.WriteLine("        browser, enter it, then CHANGE it in Server Settings.")
        End If

        ' Conversation rooms: cloud translation needs no sidecar warm-up.
        Dim convAudioHandler = TryCast(_serverController.KestrelHost?.Services?.GetService(
            GetType(Services.Rooms.ConversationAudioHandler)), Services.Rooms.ConversationAudioHandler)
        If convAudioHandler IsNot Nothing Then
            convAudioHandler.EnsureTranslationAvailable = Sub()
                                                          End Sub
            convAudioHandler.AcquireTranslationBackend = Function(roomId, engineKey) AcquireRoomTranslationBackend(roomId, engineKey)
        End If

        ' Inside a container our own IP (172.17.x.x) is unreachable from outside —
        ' only the HOST knows the phone-facing address. EVERYTONGUE_PUBLIC_HOST
        ' (e.g. "192.168.1.20:6081") lets a deployment print the exact URL;
        ' otherwise print an honest placeholder. Served pages are unaffected
        ' (they build URLs from the request's Host header).
        Dim publicHost = Environment.GetEnvironmentVariable("EVERYTONGUE_PUBLIC_HOST")
        Dim inContainer = IO.File.Exists("/.dockerenv")
        Dim shown As String
        If Not String.IsNullOrEmpty(publicHost) Then
            shown = publicHost
        ElseIf inContainer Then
            shown = $"<host-machine-ip>:<mapped {_serverController.Port + 1}>"
        Else
            shown = $"{Controllers.ServerController.GetLocalIpAddress()}:{_serverController.Port + 1}"
        End If
        Console.WriteLine()
        Console.WriteLine($"  Server running.  Phones: https://{shown}")
        Console.WriteLine($"  Lobby (volunteers): https://{shown}/lobby.html")
        Console.WriteLine($"  Admin:              https://{shown}/admin.html")
        If inContainer AndAlso String.IsNullOrEmpty(publicHost) Then
            Console.WriteLine("  (containerized: use the HOST machine's LAN IP with your -p mapped port,")
            Console.WriteLine("   e.g. https://192.168.1.20:6081 — or set EVERYTONGUE_PUBLIC_HOST to show it here)")
        End If
        Console.WriteLine("  Ctrl+C to stop.")
        Console.WriteLine()

        AddHandler Console.CancelKeyPress, Sub(s, e)
                                               e.Cancel = True   ' we exit deliberately after cleanup
                                               Shutdown()
                                           End Sub
        AddHandler AppDomain.CurrentDomain.ProcessExit, Sub(s, e) Shutdown()

        _exitEvent.Wait()
        Return 0
    End Function

    ''' <summary>
    ''' Lite runs cloud engines only. A per-room engine key that isn't cloud logs a
    ''' warning and falls back to the configured default backend ("" = orchestrator default).
    ''' </summary>
    Private Function AcquireRoomTranslationBackend(roomId As String, engineKey As String) As String
        If String.IsNullOrEmpty(engineKey) Then Return ""
        Dim entry = Services.Translation.TranslationBackendRegistry.Find(engineKey)
        If entry IsNot Nothing AndAlso Not entry.RequiresInternet Then
            AppLogger.Log(LogCategory.Translation, LogSeverity.Warning,
                $"room={roomId} requested offline translation engine '{engineKey}' — Lite is online-only, using the default engine")
            Return ""
        End If
        Return ""   ' cloud engines route via backendOverride/orchestrator; no pool name needed
    End Function

    Private Function ValidateOnlineConfig(config As AppConfig) As Boolean
        Dim ok = True
        Dim sttKey = config.GetSttApiKey(If(config.SttBackend, ""))
        Dim sttEntry = Services.Stt.SttBackendRegistry.Find(If(config.SttBackend, ""))
        If sttEntry Is Nothing OrElse Not sttEntry.RequiresApiKey Then
            Console.Error.WriteLine($"WARNING: STT engine '{config.SttBackend}' is not an online engine. " &
                "Lite has no local models — rooms must use an online engine (e.g. speechmatics).")
        ElseIf String.IsNullOrEmpty(sttKey) Then
            Console.Error.WriteLine($"ERROR: no API key configured for STT engine '{config.SttBackend}'. " &
                $"Add it under SttApiKeys in {IO.Path.Combine(ConfigManager.ConfigDirectory, "config.json")}")
            ok = False
        End If

        Dim transKey = If(config.TranslationBackend, "")
        Dim transEntry = Services.Translation.TranslationBackendRegistry.Find(transKey)
        If transEntry Is Nothing OrElse Not transEntry.RequiresInternet Then
            Console.Error.WriteLine($"ERROR: translation engine '{transKey}' is offline/unknown. " &
                "Lite is online-only — set TranslationBackend to a cloud engine (e.g. google-translate, deepl).")
            ok = False
        ElseIf transEntry.RequiresApiKey AndAlso String.IsNullOrEmpty(
                Services.Translation.TranslationBackendRegistry.ResolveTranslationApiKey(config, transKey)) Then
            ' (ResolveTranslationApiKey includes the companion-STT-key fallback —
            ' e.g. one Google key powering both STT and Translate — so this only
            ' fires when the runtime would genuinely have no key.)
            Console.Error.WriteLine($"ERROR: no API key configured for translation engine '{transKey}'. " &
                $"Add it under TranslationApiKeys in {IO.Path.Combine(ConfigManager.ConfigDirectory, "config.json")}")
            ok = False
        End If
        Return ok
    End Function

    Private _shutdownDone As Boolean = False

    Private Sub Shutdown()
        If _shutdownDone Then Return
        _shutdownDone = True
        Console.WriteLine("Shutting down...")
        ' Deliberate sidecar kills must not trigger the watchdog restart.
        PythonSidecarHost.GlobalShutdown = True
        Try : _conferenceController?.StopAllConferenceBackends() : Catch : End Try
        Try : _serverController?.StopServer() : Catch : End Try
        Try : AppLogger.EmitSessionSummary() : Catch : End Try
        _exitEvent.Set()
    End Sub

End Module
