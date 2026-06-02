Imports System.Net
Imports System.Security.Cryptography.X509Certificates
Imports System.Threading
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Hosting
Imports Microsoft.AspNetCore.ResponseCompression
Imports Microsoft.AspNetCore.Server.Kestrel.Core
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server.Hubs
Imports EveryTongue.Server.Middleware
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports Microsoft.AspNetCore.StaticFiles
Imports Microsoft.Extensions.FileProviders
Imports EveryTongue.Services.Audio
Imports EveryTongue.Services.Bible
Imports EveryTongue.Services.Subtitle
Imports EveryTongue.Services.Translation
Imports EveryTongue.Services.Rooms
Imports EveryTongue.Services.Stt
Imports EveryTongue.Services.Tts

Namespace Server
    ''' <summary>
    ''' Hosts ASP.NET Core Kestrel in-process within the WinForms application.
    ''' Runs on a background thread; FormMain remains on the UI thread.
    '''
    ''' Runs on production ports 5080/5081.
    ''' </summary>
    Public Class KestrelHost
        Implements IDisposable

        Private _app As WebApplication
        Private _cts As CancellationTokenSource
        Private _hostTask As Task
        Private _isRunning As Boolean = False

        ''' <summary>Raised when the host starts, stops, or encounters errors.</summary>
        Public Event StatusChanged As EventHandler(Of String)

        ''' <summary>True when Kestrel is accepting connections.</summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Provides access to the DI container for resolving services from FormMain.
        ''' Available after Start() completes.
        ''' </summary>
        Public ReadOnly Property Services As IServiceProvider
            Get
                Return _app?.Services
            End Get
        End Property

        ''' <summary>
        ''' Builds and starts the Kestrel host on a background thread.
        ''' </summary>
        ''' <param name="options">Server configuration (ports, colours, etc.)</param>
        ''' <param name="logCallback">Optional callback to route Kestrel logs to the UI.</param>
        Public Sub Start(options As ServerOptions, Optional logCallback As Action(Of String) = Nothing)
            If _isRunning Then Return

            _cts = New CancellationTokenSource()

            Try
                _app = BuildApplication(options, logCallback)

                ' Start on background thread so WinForms UI thread is never blocked
                _hostTask = Task.Run(
                    Async Function()
                        Try
                            Await _app.RunAsync(_cts.Token)
                        Catch ex As OperationCanceledException
                            ' Normal shutdown
                        Catch ex As Exception
                            RaiseEvent StatusChanged(Me, $"Kestrel error: {ex.Message}")
                        End Try
                    End Function, _cts.Token)

                _isRunning = True
                RaiseEvent StatusChanged(Me,
                    $"Kestrel started on HTTP:{options.HttpPort} HTTPS:{options.HttpsPort}")

            Catch ex As Exception
                _isRunning = False
                RaiseEvent StatusChanged(Me, $"Kestrel failed to start: {ex.Message}")
                Throw
            End Try
        End Sub

        ''' <summary>
        ''' Gracefully shuts down the Kestrel host.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isRunning Then Return

            Try
                _cts?.Cancel()
                ' Give the host up to 5 seconds to shut down gracefully
                _hostTask?.Wait(TimeSpan.FromSeconds(5))
            Catch ex As AggregateException
                ' Expected — contains OperationCanceledException
            Catch ex As Exception
                RaiseEvent StatusChanged(Me, $"Kestrel stop error: {ex.Message}")
            Finally
                _isRunning = False
                RaiseEvent StatusChanged(Me, "Kestrel stopped")
            End Try
        End Sub

        Private Function BuildApplication(options As ServerOptions,
                                           logCallback As Action(Of String)) As WebApplication

            Dim builder = WebApplication.CreateBuilder(New WebApplicationOptions With {
                .ApplicationName = "EveryTongue"
            })

            ' Create cert service early — shared between Kestrel config and DI
            Dim certService As New CertificateService(
                LoggerFactory.Create(Sub(b) b.SetMinimumLevel(LogLevel.Information).
                    AddProvider(If(logCallback IsNot Nothing,
                        DirectCast(New CallbackLoggerProvider(logCallback), ILoggerProvider),
                        DirectCast(Microsoft.Extensions.Logging.Abstractions.NullLoggerProvider.Instance, ILoggerProvider)))).
                    CreateLogger(Of CertificateService)())

            ' ── Kestrel configuration ──
            ConfigureKestrel(builder, options, certService)

            ' ── Logging ──
            ConfigureLogging(builder, logCallback)

            ' ── JSON serialization — camelCase for all endpoints ──
            builder.Services.Configure(Of Microsoft.AspNetCore.Http.Json.JsonOptions)(
                Sub(j) j.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase)

            ' ── Services (Dependency Injection) ──
            ConfigureServices(builder.Services, options, certService)

            ' ── Build the app ──
            Dim app = builder.Build()

            ' Expose TemplateStore singleton for UI-thread access and sync from config
            TemplateStore.Instance = app.Services.GetRequiredService(Of TemplateStore)()
            TemplateStore.Instance.SyncFromConfig(options.ConferenceTemplates)

            ' Apply TTS backend preference from config
            Dim ttsSvc = app.Services.GetService(Of ITtsService)()
            Dim ttsOrch = TryCast(ttsSvc, TtsOrchestrator)
            If ttsOrch IsNot Nothing AndAlso Not String.IsNullOrEmpty(options.TtsBackends) Then
                ttsOrch.SetPreferredBackends(options.TtsBackends)
            End If

            ' Wire TTS audio output for local PA/NDI playback (conditional, so stays post-build)
            Dim subtitleSvc = TryCast(app.Services.GetService(GetType(ISubtitleService)), SubtitleService)
            If subtitleSvc IsNot Nothing AndAlso options.TtsOutputDevice >= 0 Then
                Dim ttsOutput As New TtsAudioOutput(
                    LoggerFactory.Create(Sub(b) b.SetMinimumLevel(LogLevel.Information).
                        AddProvider(If(logCallback IsNot Nothing,
                            DirectCast(New CallbackLoggerProvider(logCallback), ILoggerProvider),
                            DirectCast(Microsoft.Extensions.Logging.Abstractions.NullLoggerProvider.Instance, ILoggerProvider)))).
                        CreateLogger(Of TtsAudioOutput)())
                ttsOutput.DeviceNumber = options.TtsOutputDevice
                ttsOutput.Volume = options.TtsOutputVolume
                ttsOutput.Start()
                subtitleSvc.TtsAudioOutput = ttsOutput
            End If

            ' Wire conversation audio handler with live-server port, FFmpeg, and model config
            Dim convAudioHandler = app.Services.GetService(Of ConversationAudioHandler)()
            If convAudioHandler IsNot Nothing Then
                convAudioHandler.LiveServerPort = options.LiveServerPort
                convAudioHandler.FfmpegPath = options.FfmpegPath
                convAudioHandler.WhisperModelPath = options.WhisperModelPath
                convAudioHandler.WhisperComputeType = options.WhisperComputeType
                convAudioHandler.WhisperUseCpu = options.WhisperUseCpu
            End If

            ' Start MMS-TTS sidecar if deps are installed (optional tier-2 offline TTS)
            Dim backends = app.Services.GetServices(Of ITtsBackend)()
            For Each backend In backends
                Dim mmsTts = TryCast(backend, MmsTtsBackend)
                If mmsTts IsNot Nothing Then
                    If MmsTtsBackend.CheckDepsInstalled() Then
                        AddHandler mmsTts.StatusChanged, Sub(s, msg)
                                                             logCallback?.Invoke(msg)
                                                         End Sub
                        mmsTts.Start()
                    End If
                    Exit For
                End If
            Next

            ' ── Middleware pipeline (order matters) ──
            ConfigureMiddleware(app)

            ' ── Endpoints ──
            EndpointRegistration.MapAllEndpoints(app)

            Return app
        End Function

        Private Sub ConfigureKestrel(builder As WebApplicationBuilder,
                                      options As ServerOptions,
                                      certService As CertificateService)
            builder.WebHost.UseKestrel(
                Sub(kestrelOpts)
                    ' Determine bind address
                    Dim bindAddress = If(options.AllowRemote, IPAddress.Any, IPAddress.Loopback)

                    ' HTTP
                    kestrelOpts.Listen(bindAddress, options.HttpPort,
                        Sub(listenOpts)
                            listenOpts.Protocols = HttpProtocols.Http1AndHttp2
                        End Sub)

                    ' HTTPS with self-signed certificate
                    Dim cert = certService.GetOrCreateCertificate()

                    If cert IsNot Nothing Then
                        kestrelOpts.Listen(bindAddress, options.HttpsPort,
                            Sub(listenOpts)
                                listenOpts.Protocols = HttpProtocols.Http1AndHttp2
                                listenOpts.UseHttps(cert)
                            End Sub)
                    End If

                    ' Limits
                    kestrelOpts.Limits.MaxConcurrentConnections = 1000
                    kestrelOpts.Limits.MaxRequestBodySize = 1 * 1024 * 1024 ' 1MB
                    kestrelOpts.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2)
                    kestrelOpts.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30)
                End Sub)

            ' Suppress the default Kestrel startup URLs
            builder.WebHost.UseUrls()
        End Sub

        Private Sub ConfigureLogging(builder As WebApplicationBuilder,
                                      logCallback As Action(Of String))
            builder.Logging.ClearProviders()

            ' Minimum level: Information in production, Debug for development
            builder.Logging.SetMinimumLevel(LogLevel.Information)

            ' Suppress noisy ASP.NET Core internal logs
            builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning)
            builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Warning)

            ' Route logs to the UI callback if provided
            If logCallback IsNot Nothing Then
                builder.Logging.AddProvider(New CallbackLoggerProvider(logCallback))
            End If
        End Sub

        Private Sub ConfigureServices(services As IServiceCollection,
                                      options As ServerOptions,
                                      certService As CertificateService)
            ' Configuration
            services.AddSingleton(Of IOptions(Of ServerOptions))(
                Microsoft.Extensions.Options.Options.Create(options))

            ' Infrastructure services — same instance used for Kestrel HTTPS binding
            services.AddSingleton(Of ICertificateService)(certService)

            ' Response compression
            services.AddResponseCompression(
                Sub(opts)
                    opts.EnableForHttps = True
                    opts.Providers.Add(Of BrotliCompressionProvider)()
                    opts.Providers.Add(Of GzipCompressionProvider)()
                    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                        {"application/json", "text/html", "application/javascript", "text/css"})
                End Sub)

            services.Configure(Of BrotliCompressionProviderOptions)(
                Sub(opts) opts.Level = IO.Compression.CompressionLevel.Fastest)

            services.Configure(Of GzipCompressionProviderOptions)(
                Sub(opts) opts.Level = IO.Compression.CompressionLevel.Fastest)

            ' Core services
            services.AddSingleton(Of TemplateStore)()
            services.AddSingleton(Of RoomManager)()
            services.AddSingleton(Of ISubtitleService, SubtitleService)()
            services.AddSingleton(Of ConversationAudioHandler)()
            services.AddTransient(Of SubtitleHub)()
            services.AddSingleton(Of IBibleService, BibleService)()
            services.AddSingleton(Of IMetricsService, MetricsService)()
            services.AddSingleton(Of IAudioStreamService, AudioStreamService)()

            ' STT backend registry (backends are created per-session by LiveController, not as singletons)
            services.AddSingleton(Of SttBackendRegistry)()

            ' Translation backends (NllbBackend registered dynamically by FormMain when NLLB sidecar starts)
            services.AddSingleton(Of ITranslationBackend, DeepLBackend)()
            services.AddSingleton(Of ITranslationBackend, GoogleBackend)()
            services.AddSingleton(Of ITranslationBackend, AzureBackend)()
            services.AddSingleton(Of ITranslationService, TranslationOrchestrator)()

            ' TTS backends and orchestrator
            services.AddSingleton(Of TtsCache)()
            services.AddSingleton(Of ITtsBackend, PiperBackend)()
            services.AddSingleton(Of ITtsBackend, MmsTtsBackend)()
            services.AddSingleton(Of ITtsBackend, EdgeTtsBackend)()
            services.AddSingleton(Of ITtsService, TtsOrchestrator)()
        End Sub

        Private Sub ConfigureMiddleware(app As WebApplication)
            ' Order matters: outermost middleware wraps all inner middleware

            ' 1. Error handling (catch-all)
            app.UseMiddleware(Of ErrorHandlingMiddleware)()

            ' 2. Request logging
            app.UseMiddleware(Of RequestLoggingMiddleware)()

            ' 3. Response compression (gzip/brotli)
            app.UseResponseCompression()

            ' 4. WebSocket support
            app.UseWebSockets(New WebSocketOptions With {
                .KeepAliveInterval = TimeSpan.FromSeconds(30)
            })

            ' 5. Lobby redirect — phones hitting / without ?room= or ?preview go to lobby
            app.Use(Function(ctx, [next])
                        Dim path = ctx.Request.Path.Value
                        If (path = "/" OrElse String.Equals(path, "/index.html", StringComparison.OrdinalIgnoreCase)) AndAlso
                           Not ctx.Request.Query.ContainsKey("room") AndAlso
                           Not ctx.Request.Query.ContainsKey("preview") AndAlso
                           Not ctx.Request.Query.ContainsKey("bibleLang") Then
                            ctx.Response.Redirect("/lobby.html", False)
                            Return Task.CompletedTask
                        End If
                        Return [next](ctx)
                    End Function)

            ' 6. Static files (wwwroot/) — serves index.html, CSS, JS, lobby.html
            Dim wwwrootPath = IO.Path.Combine(AppContext.BaseDirectory, "wwwroot")
            If IO.Directory.Exists(wwwrootPath) Then
                Dim fileProvider = New PhysicalFileProvider(wwwrootPath)
                app.UseDefaultFiles(New DefaultFilesOptions With {
                    .FileProvider = fileProvider
                })
                app.UseStaticFiles(New StaticFileOptions With {
                    .FileProvider = fileProvider,
                    .OnPrepareResponse = Sub(ctx)
                                             Dim path = ctx.Context.Request.Path.Value
                                             If path IsNot Nothing AndAlso
                                                (path.EndsWith(".js") OrElse path.EndsWith(".css") OrElse path.EndsWith(".html")) Then
                                                 ' JS/CSS/HTML — always revalidate via ETag, no stale cache
                                                 ctx.Context.Response.Headers.CacheControl = "no-cache"
                                             Else
                                                 ' Images, fonts, etc. — cache 1 day
                                                 ctx.Context.Response.Headers.CacheControl = "public, max-age=86400"
                                             End If
                                         End Sub
                })
            End If

            ' 6. Routing + endpoints handled by MapAllEndpoints
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ' Stop TTS audio output and MMS-TTS sidecar before shutting down
            Try
                Dim subtitleSvc = TryCast(_app?.Services?.GetService(GetType(ISubtitleService)), SubtitleService)
                subtitleSvc?.TtsAudioOutput?.Dispose()
            Catch ex As Exception
                AppLogger.Log($"[ERROR] KestrelHost.Dispose (TtsAudioOutput): {ex.Message}")
            End Try
            Try
                Dim backends = _app?.Services?.GetServices(Of ITtsBackend)()
                If backends IsNot Nothing Then
                    For Each backend In backends
                        Dim mmsTts = TryCast(backend, MmsTtsBackend)
                        mmsTts?.Stop()
                    Next
                End If
            Catch ex As Exception
                AppLogger.Log($"[ERROR] KestrelHost.Dispose (MmsTtsBackend): {ex.Message}")
            End Try

            [Stop]()
            _app?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3))
            _cts?.Dispose()
        End Sub
    End Class

    ' ──────────────────────────────────────────────────
    ' Callback logger — routes Kestrel logs to the UI
    ' ──────────────────────────────────────────────────

    Friend Class CallbackLoggerProvider
        Implements ILoggerProvider

        Private ReadOnly _callback As Action(Of String)

        Public Sub New(callback As Action(Of String))
            _callback = callback
        End Sub

        Public Function CreateLogger(categoryName As String) As ILogger Implements ILoggerProvider.CreateLogger
            Return New CallbackLogger(_callback, categoryName)
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class

    Friend Class CallbackLogger
        Implements ILogger

        Private ReadOnly _callback As Action(Of String)
        Private ReadOnly _category As String

        Public Sub New(callback As Action(Of String), category As String)
            _callback = callback
            ' Shorten category: "EveryTongue.Server.KestrelHost" -> "KestrelHost"
            Dim lastDot = category.LastIndexOf("."c)
            _category = If(lastDot >= 0, category.Substring(lastDot + 1), category)
        End Sub

        Public Function BeginScope(Of TState)(state As TState) As IDisposable Implements ILogger.BeginScope
            Return Nothing
        End Function

        Public Function IsEnabled(logLevel As LogLevel) As Boolean Implements ILogger.IsEnabled
            Return logLevel >= LogLevel.Information
        End Function

        Public Sub Log(Of TState)(logLevel As LogLevel,
                                   eventId As EventId,
                                   state As TState,
                                   exception As Exception,
                                   formatter As Func(Of TState, Exception, String)) Implements ILogger.Log
            If Not IsEnabled(logLevel) Then Return
            Dim message = formatter(state, exception)
            If String.IsNullOrEmpty(message) Then Return
            _callback($"[{_category}] {message}")
        End Sub
    End Class
End Namespace
