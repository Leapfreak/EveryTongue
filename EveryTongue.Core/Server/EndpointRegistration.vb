Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server.Hubs
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Audio
Imports EveryTongue.Services.Rooms
Imports EveryTongue.Services.Subtitle
Imports EveryTongue.Services.Tts
Imports QRCoder

Namespace Server
    ''' <summary>
    ''' Registers all Minimal API endpoints on the Kestrel WebApplication.
    ''' Each route defined once — served on both HTTP and HTTPS automatically.
    ''' </summary>
    Public Module EndpointRegistration

        ''' <summary>
        ''' The host[:port] PHONES should use to reach this server. Priority:
        ''' EVERYTONGUE_PUBLIC_HOST env (containers/hosted); else the request's own
        ''' Host header — except when the operator browsed via loopback on a native
        ''' host, where the LAN IP is substituted (keeping the request's port) so
        ''' QR codes and share links never encode "localhost".
        ''' </summary>
        Friend Function PublicHostFor(context As HttpContext) As String
            Dim pub = Environment.GetEnvironmentVariable("EVERYTONGUE_PUBLIC_HOST")
            If Not String.IsNullOrEmpty(pub) Then Return pub
            Dim reqHost = context.Request.Host
            Dim name = If(reqHost.Host, "")
            Dim isLoopback = name.Equals("localhost", StringComparison.OrdinalIgnoreCase) OrElse
                             name = "127.0.0.1" OrElse name = "::1" OrElse name = "[::1]"
            If isLoopback AndAlso Not IO.File.Exists("/.dockerenv") Then
                Dim ip = Controllers.ServerController.GetLocalIpAddress()
                Return If(reqHost.Port.HasValue, $"{ip}:{reqHost.Port.Value}", ip)
            End If
            Return reqHost.ToString()
        End Function

        Public Sub MapAllEndpoints(app As IEndpointRouteBuilder)
            MapWebSocketEndpoint(app)
            MapCoreEndpoints(app)
            MapLocaleEndpoints(app)
            MapControlEndpoint(app)
            MapBibleEndpoints(app)
            MapAudioEndpoints(app)
            MapTtsEndpoints(app)
            MapRoomEndpoints(app)
            MapTemplateEndpoints(app)
            MapSettingsEndpoints(app)
        End Sub

        Private Sub MapWebSocketEndpoint(app As IEndpointRouteBuilder)
            ' WebSocket subtitle hub — /ws
            app.Map("/ws", Async Function(context As HttpContext) As Task
                               Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                               Await hub.HandleAsync(context)
                           End Function)
        End Sub

        Private Sub MapCoreEndpoints(app As IEndpointRouteBuilder)

            ' Health check — returns status of all subsystems
            app.MapGet("/api/health", Function(context As HttpContext) As Task
                                          Dim ver = GetType(EndpointRegistration).Assembly.
                                              GetName().Version
                                          Dim version = If(ver?.ToString(), "unknown")

                                          Dim subtitleSvc = context.RequestServices.GetService(Of ISubtitleService)
                                          Dim translationSvc = context.RequestServices.GetService(Of ITranslationService)
                                          Dim bibleSvc = context.RequestServices.GetService(Of IBibleService)
                                          Dim metricsSvc = context.RequestServices.GetService(Of IMetricsService)

                                          Dim checks As New Dictionary(Of String, String)()
                                          checks("kestrel") = "healthy"
                                          checks("subtitles") = If(subtitleSvc IsNot Nothing, "healthy", "unavailable")
                                          checks("translation") = If(translationSvc IsNot Nothing, "healthy", "unavailable")
                                          checks("bible") = If(bibleSvc IsNot Nothing, "healthy", "unavailable")

                                          Dim overall = If(checks.Values.All(Function(v) v = "healthy"),
                                              "healthy", "degraded")

                                          Dim uptime = If(metricsSvc IsNot Nothing,
                                              TimeSpan.FromSeconds(metricsSvc.UptimeSeconds).ToString("hh\:mm\:ss"),
                                              "00:00:00")

                                          Return context.Response.WriteAsJsonAsync(New With {
                                              .status = overall,
                                              .checks = checks,
                                              .uptime = uptime,
                                              .version = version,
                                              .timestamp = DateTime.UtcNow
                                          })
                                      End Function).
                ExcludeFromDescription()

            ' Metrics — detailed runtime statistics
            app.MapGet("/api/metrics", Function(context As HttpContext) As Task
                                           Dim metricsSvc = context.RequestServices.
                                               GetService(Of IMetricsService)
                                           If metricsSvc Is Nothing Then
                                               Return context.Response.WriteAsJsonAsync(New With {.error = "Metrics not available"})
                                           End If
                                           Return context.Response.WriteAsJsonAsync(metricsSvc.GetSnapshot())
                                       End Function)

            ' Queue metrics — priority queue stats for translation and TTS pipelines
            app.MapGet("/api/queue-metrics", Function(context As HttpContext) As Task
                                                 Dim translationSvc = context.RequestServices.
                                                     GetService(Of ITranslationService)
                                                 Dim ttsSvc = context.RequestServices.
                                                     GetService(Of ITtsService)
                                                 Return context.Response.WriteAsJsonAsync(New With {
                                                     .translation = translationSvc?.TranslationQueueMetrics,
                                                     .tts = ttsSvc?.TtsQueueMetrics,
                                                     .timestamp = DateTime.UtcNow
                                                 })
                                             End Function)

            ' Client configuration — replaces {{BG_COLOR}}/{{FG_COLOR}} template injection
            ' Client fetches this on load to configure itself.
            ' Cached 60s — colors/config rarely change mid-service.
            app.MapGet("/api/config", Function(context As HttpContext) As Task
                                          context.Response.Headers.CacheControl = "public, max-age=60"

                                          Dim opts = context.RequestServices.
                                              GetService(Of IOptions(Of ServerOptions))
                                          Dim serverOpts = If(opts?.Value, New ServerOptions())

                                          ' Determine WebSocket URL from the incoming request
                                          Dim scheme = If(context.Request.IsHttps, "wss", "ws")
                                          Dim host = context.Request.Host.ToString()
                                          Dim wsUrl = $"{scheme}://{host}/ws"

                                          Return context.Response.WriteAsJsonAsync(New With {
                                              .bgColor = serverOpts.BgColor,
                                              .fgColor = serverOpts.FgColor,
                                              .wsUrl = wsUrl,
                                              .httpsEnabled = True,
                                              .hasAdminPin = Not String.IsNullOrEmpty(serverOpts.AdminPin),
                                              .showBibleCopyright = serverOpts.ShowBibleCopyright,
                                              .publicHost = PublicHostFor(context),
                                              .version = If(GetType(EndpointRegistration).Assembly.
                                                  GetName().Version?.ToString(), "unknown")
                                          })
                                      End Function)

            ' Admin PIN verification — returns {ok:true} if PIN matches
            ' (PublicHostFor lives below MapAllEndpoints — shared by QR + config.)
            app.MapGet("/api/admin/verify",
                Function(context As HttpContext) As IResult
                    Dim opts = context.RequestServices.
                        GetService(Of IOptions(Of ServerOptions))
                    Dim serverOpts = If(opts?.Value, New ServerOptions())
                    Dim pin = context.Request.Query("pin").ToString()

                    If String.IsNullOrEmpty(serverOpts.AdminPin) Then
                        Return Results.Json(New With {.ok = False, .error = "Admin not configured"})
                    End If

                    If String.Equals(pin, serverOpts.AdminPin, StringComparison.Ordinal) Then
                        Return Results.Json(New With {.ok = True})
                    Else
                        Return Results.Json(New With {.ok = False, .error = "Invalid PIN"})
                    End If
                End Function)

            ' Certificate download (DER format) — for manual trust on phones
            app.MapGet("/cert", Function(context As HttpContext) As IResult
                                    Dim certService = context.RequestServices.
                                        GetService(Of ICertificateService)
                                    Dim certBytes = certService?.GetCertificateBytes()
                                    If certBytes Is Nothing Then
                                        Return Results.NotFound(New With {
                                            .error = "No certificate available"})
                                    End If
                                    Return Results.File(
                                        certBytes,
                                        "application/x-x509-ca-cert",
                                        "transcription-tools.cer")
                                End Function)

            ' Silent WAV for keepalive (prevents phone from killing the WebSocket)
            app.MapGet("/nosleep.wav", Function(context As HttpContext) As IResult
                                           Dim wavBytes = GenerateSilentWav()
                                           Return Results.File(wavBytes, "audio/wav", "nosleep.wav")
                                       End Function)

        End Sub

        Private Sub MapLocaleEndpoints(app As IEndpointRouteBuilder)

            ' Serve web client translations as JSON — /api/locale
            ' Returns only web.* keys (with prefix stripped).
            ' Accepts optional ?lang= param for per-client locale (detected from browser).
            ' Falls back to server's UI language if the requested locale is unavailable.
            app.MapGet("/api/locale", Function(context As HttpContext) As Task
                                          context.Response.Headers.CacheControl = "no-cache"
                                          Dim langPack = LanguagePackService.Instance
                                          Dim clientLang = context.Request.Query("lang").FirstOrDefault()
                                          Dim strings = If(Not String.IsNullOrEmpty(clientLang),
                                              langPack.GetWebStringsForLanguage(clientLang),
                                              langPack.GetWebStrings())
                                          Return context.Response.WriteAsJsonAsync(strings)
                                      End Function)

            ' Serve language list from language-codes.json — /api/languages
            ' Returns array of [floresCode, nativeName, englishName, iso1Code]
            app.MapGet("/api/languages", Function(context As HttpContext) As Task
                                              context.Response.Headers.CacheControl = "public, max-age=3600"
                                              Dim langSvc = LanguageCodeService.Instance
                                              Dim langs = langSvc.GetAllLanguagesForWeb()
                                              Dim result = langs.Select(Function(l) New Object() {l.Flores, l.Native, l.Name, l.Iso1}).ToArray()
                                              Return context.Response.WriteAsJsonAsync(result)
                                          End Function)

        End Sub

        ''' <summary>
        ''' Callback for remote control commands from /api/control.
        ''' Uses Volatile read/write for thread safety (set from UI thread, read from HTTP threads).
        ''' </summary>
        Private _remoteCommandHandler As Action(Of String)
        Public Property RemoteCommandHandler As Action(Of String)
            Get
                Return Threading.Volatile.Read(_remoteCommandHandler)
            End Get
            Set(value As Action(Of String))
                Threading.Volatile.Write(_remoteCommandHandler, value)
            End Set
        End Property

        ''' <summary>
        ''' Callback fired when a conference room is created from a template.
        ''' Args: (roomId, templateId). Set by FormMain to spin up the pipeline.
        ''' </summary>
        Private _conferenceRoomCreatedHandler As Action(Of String, String)
        Public Property ConferenceRoomCreatedHandler As Action(Of String, String)
            Get
                Return Threading.Volatile.Read(_conferenceRoomCreatedHandler)
            End Get
            Set(value As Action(Of String, String))
                Threading.Volatile.Write(_conferenceRoomCreatedHandler, value)
            End Set
        End Property

        ''' <summary>
        ''' Callback for pipeline config changes from web host controls.
        ''' Args: (roomId, Dictionary of param name → value).
        ''' </summary>
        Private _pipelineConfigHandler As Action(Of String, Dictionary(Of String, Object))
        Public Property PipelineConfigHandler As Action(Of String, Dictionary(Of String, Object))
            Get
                Return Threading.Volatile.Read(_pipelineConfigHandler)
            End Get
            Set(value As Action(Of String, Dictionary(Of String, Object)))
                Threading.Volatile.Write(_pipelineConfigHandler, value)
            End Set
        End Property

        ''' <summary>
        ''' Callback for pipeline reset commands from /api/control/pipeline/reset.
        ''' Arg: roomId.
        ''' </summary>
        Private _pipelineResetHandler As Action(Of String)
        Public Property PipelineResetHandler As Action(Of String)
            Get
                Return Threading.Volatile.Read(_pipelineResetHandler)
            End Get
            Set(value As Action(Of String))
                Threading.Volatile.Write(_pipelineResetHandler, value)
            End Set
        End Property

        ''' <summary>
        ''' Callback fired when a conference room is closed. Arg: roomId.
        ''' </summary>
        Private _roomClosedHandler As Action(Of String)
        Public Property RoomClosedHandler As Action(Of String)
            Get
                Return Threading.Volatile.Read(_roomClosedHandler)
            End Get
            Set(value As Action(Of String))
                Threading.Volatile.Write(_roomClosedHandler, value)
            End Set
        End Property

        ''' <summary>(roomId, paused) — fired when the host pauses/resumes a room, so the
        ''' conference controller can reset the STT engine's per-speaker pace auto-tune.</summary>
        Private _roomPausedHandler As Action(Of String, Boolean)
        Public Property RoomPausedHandler As Action(Of String, Boolean)
            Get
                Return Threading.Volatile.Read(_roomPausedHandler)
            End Get
            Set(value As Action(Of String, Boolean))
                Threading.Volatile.Write(_roomPausedHandler, value)
            End Set
        End Property

        ''' <summary>
        ''' Generates a QR code PNG for the given text.
        ''' </summary>
        Private Function GenerateQrPng(text As String) As Byte()
            Using qrGen As New QRCodeGenerator()
                Dim qrData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.M)
                Using qrCode As New PngByteQRCode(qrData)
                    Return qrCode.GetGraphic(10)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Generates a minimal silent WAV file (2 seconds, 8kHz, 8-bit mono).
        ''' </summary>
        Private Function GenerateSilentWav() As Byte()
            Dim sampleRate As Integer = 8000
            Dim durationSec As Integer = 2
            Dim bitsPerSample As Integer = 8
            Dim channels As Integer = 1
            Dim dataSize As Integer = sampleRate * durationSec * channels * (bitsPerSample \ 8)
            Dim fileSize As Integer = 36 + dataSize

            Using ms As New IO.MemoryStream(44 + dataSize)
                Using bw As New IO.BinaryWriter(ms)
                    ' RIFF header
                    bw.Write(Text.Encoding.ASCII.GetBytes("RIFF"))
                    bw.Write(fileSize)
                    bw.Write(Text.Encoding.ASCII.GetBytes("WAVE"))
                    ' fmt chunk
                    bw.Write(Text.Encoding.ASCII.GetBytes("fmt "))
                    bw.Write(16)                        ' chunk size
                    bw.Write(CShort(1))                 ' PCM
                    bw.Write(CShort(channels))
                    bw.Write(sampleRate)
                    bw.Write(sampleRate * channels * (bitsPerSample \ 8)) ' byte rate
                    bw.Write(CShort(channels * (bitsPerSample \ 8)))      ' block align
                    bw.Write(CShort(bitsPerSample))
                    ' data chunk
                    bw.Write(Text.Encoding.ASCII.GetBytes("data"))
                    bw.Write(dataSize)
                    ' Silent samples (128 = silence for 8-bit unsigned PCM)
                    Dim silence(dataSize - 1) As Byte
                    Array.Fill(silence, CByte(128))
                    bw.Write(silence)
                End Using
                Return ms.ToArray()
            End Using
        End Function

    End Module
End Namespace
