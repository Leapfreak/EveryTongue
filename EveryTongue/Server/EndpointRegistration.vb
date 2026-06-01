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

        Public Sub MapAllEndpoints(app As IEndpointRouteBuilder)
            MapWebSocketEndpoint(app)
            MapCoreEndpoints(app)
            MapLocaleEndpoints(app)
            MapControlEndpoint(app)
            MapBibleEndpoints(app)
            MapAudioEndpoints(app)
            MapTtsEndpoints(app)
            MapRoomEndpoints(app)
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
                                              .version = If(GetType(EndpointRegistration).Assembly.
                                                  GetName().Version?.ToString(), "unknown")
                                          })
                                      End Function)

            ' Admin PIN verification — returns {ok:true} if PIN matches
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
            ' Returns array of [nllbCode, nativeName, englishName, iso1Code]
            app.MapGet("/api/languages", Function(context As HttpContext) As Task
                                              context.Response.Headers.CacheControl = "public, max-age=3600"
                                              Dim langSvc = LanguageCodeService.Instance
                                              Dim langs = langSvc.GetAllLanguagesForWeb()
                                              Dim result = langs.Select(Function(l) New Object() {l.Nllb, l.Native, l.Name, l.Iso1}).ToArray()
                                              Return context.Response.WriteAsJsonAsync(result)
                                          End Function)

        End Sub

        ''' <summary>
        ''' Callback for remote control commands from /api/control.
        ''' Set by LiveController to handle start/stop/restart/clear/setSliders.
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

        Private Sub MapControlEndpoint(app As IEndpointRouteBuilder)

            ' Admin remote control — /api/control?action=start|stop|restart|clear|status|tune|setsliders
            app.MapGet("/api/control", Function(context As HttpContext) As IResult
                                           Dim subtitleService = context.RequestServices.
                                               GetService(Of ISubtitleService)
                                           If subtitleService Is Nothing Then
                                               Return Results.Json(New With {.error = "service unavailable"})
                                           End If

                                           Dim action = context.Request.Query("action").FirstOrDefault()

                                           ' No action = status (same as legacy)
                                           If String.IsNullOrEmpty(action) OrElse
                                              action.Equals("status", StringComparison.OrdinalIgnoreCase) Then
                                               Return Results.Json(New With {
                                                   .live = subtitleService.IsLiveRunning,
                                                   .inputLang = If(subtitleService.InputLanguage, "")
                                               })
                                           End If

                                           Select Case action.ToLower()
                                               Case "start", "stop", "restart", "clear"
                                                   RemoteCommandHandler?.Invoke(action.ToLower())
                                                   Return Results.Json(New With {
                                                       .ok = True,
                                                       .action = action.ToLower()
                                                   })

                                               Case "tune"
                                                   If subtitleService.TuneCallback IsNot Nothing Then
                                                       Dim json = subtitleService.TuneCallback.Invoke()
                                                       If json IsNot Nothing Then
                                                           Return Results.Text(json, "application/json")
                                                       End If
                                                   End If
                                                   Return Results.Json(New With {.error = "not available"})

                                               Case "setsliders"
                                                   Dim maxSeg = context.Request.Query("maxSeg").FirstOrDefault()
                                                   Dim vadSilence = context.Request.Query("vadSilence").FirstOrDefault()
                                                   If maxSeg IsNot Nothing AndAlso vadSilence IsNot Nothing Then
                                                       RemoteCommandHandler?.Invoke($"setSliders:{maxSeg},{vadSilence}")
                                                       Return Results.Json(New With {.ok = True})
                                                   End If
                                                   Return Results.Json(New With {.error = "missing params"})

                                               Case Else
                                                   Return Results.BadRequest(New With {.error = "unknown action"})
                                           End Select
                                       End Function)
        End Sub

        Private Sub MapBibleEndpoints(app As IEndpointRouteBuilder)

            ' List available translations
            app.MapGet("/bible/translations", Async Function(context As HttpContext) As Task
                                                  Dim logger = context.RequestServices.
                                                      GetService(Of ILoggerFactory)?.CreateLogger("BibleEndpoint")
                                                  Dim bibleService = context.RequestServices.
                                                      GetService(Of IBibleService)
                                                  If bibleService Is Nothing Then
                                                      logger?.LogWarning("Bible service not available")
                                                      Await context.Response.WriteAsJsonAsync(
                                                          New With {.error = "Bible service not available"})
                                                      Return
                                                  End If
                                                  Dim lang = context.Request.Query("lang").FirstOrDefault()
                                                  Dim translations = Await bibleService.GetTranslationsAsync(
                                                      If(lang, ""), context.RequestAborted)
                                                  logger?.LogInformation("Bible translations: lang={Lang}, count={Count}",
                                                      If(lang, "(all)"), translations.Count)
                                                  Await context.Response.WriteAsJsonAsync(translations)
                                              End Function)

            ' List books for a translation
            app.MapGet("/bible/{id}/books",
                Async Function(id As String, context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(Array.Empty(Of Object)())
                        Return
                    End If
                    Dim books = Await bibleService.GetBooksAsync(id, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(books)
                End Function)

            ' Get chapter
            app.MapGet("/bible/{id}/{book}/{chapter:int}",
                Async Function(id As String, book As String, chapter As Integer,
                               context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(
                            New With {.error = "Bible service not available"})
                        Return
                    End If
                    Dim result = Await bibleService.GetChapterAsync(
                        id, book, chapter, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(result)
                End Function)

            ' Get verse or verse range
            app.MapGet("/bible/{id}/{book}/{chapter:int}/{verses}",
                Async Function(id As String, book As String, chapter As Integer,
                               verses As String, context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(
                            New With {.error = "Bible service not available"})
                        Return
                    End If
                    Dim parts = verses.Split("-"c)
                    Dim vStart = Integer.Parse(parts(0))
                    Dim vEnd = If(parts.Length > 1, Integer.Parse(parts(1)), -1)
                    Dim result = Await bibleService.GetVersesAsync(
                        id, book, chapter, vStart, vEnd, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(result)
                End Function)

            ' Full-text search
            app.MapGet("/bible/search", Async Function(context As HttpContext) As Task
                                            Dim bibleService = context.RequestServices.
                                                GetService(Of IBibleService)
                                            If bibleService Is Nothing Then
                                                Await context.Response.WriteAsJsonAsync(
                                                    New With {.error = "Bible service not available"})
                                                Return
                                            End If
                                            Dim q = context.Request.Query("q").FirstOrDefault()
                                            Dim transId = context.Request.Query("translation").FirstOrDefault()
                                            If String.IsNullOrEmpty(q) OrElse String.IsNullOrEmpty(transId) Then
                                                context.Response.StatusCode = 400
                                                Await context.Response.WriteAsJsonAsync(
                                                    New With {.error = "q and translation required"})
                                                Return
                                            End If
                                            Dim maxResults = 50
                                            Dim maxStr = context.Request.Query("max").FirstOrDefault()
                                            If maxStr IsNot Nothing Then Integer.TryParse(maxStr, maxResults)
                                            Dim searchResults = Await bibleService.SearchAsync(
                                                q, transId, maxResults, context.RequestAborted)
                                            Await context.Response.WriteAsJsonAsync(searchResults)
                                        End Function)

            ' Parse human-readable reference
            app.MapGet("/bible/parse", Async Function(context As HttpContext) As Task
                                           Dim bibleService = context.RequestServices.
                                               GetService(Of IBibleService)
                                           If bibleService Is Nothing Then
                                               Await context.Response.WriteAsJsonAsync(
                                                   New With {.error = "Bible service not available"})
                                               Return
                                           End If
                                           Dim ref = context.Request.Query("ref").FirstOrDefault()
                                           If String.IsNullOrEmpty(ref) Then
                                               context.Response.StatusCode = 400
                                               Await context.Response.WriteAsJsonAsync(
                                                   New With {.error = "ref parameter required"})
                                               Return
                                           End If
                                           Dim lang = If(context.Request.Query("lang").FirstOrDefault(), "en")
                                           Dim transId = context.Request.Query("translation").FirstOrDefault()
                                           Dim result = Await bibleService.ParseReferenceAsync(ref, lang, transId)
                                           Await context.Response.WriteAsJsonAsync(result)
                                       End Function)
        End Sub

        Private Sub MapAudioEndpoints(app As IEndpointRouteBuilder)

            ' List NDI sources
            app.MapGet("/audio/ndi/sources",
                Async Function(context As HttpContext) As Task
                    Dim audioService = context.RequestServices.
                        GetService(Of IAudioStreamService)
                    If audioService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(New With {.error = "Audio service not available"})
                        Return
                    End If
                    Dim sources = Await audioService.GetNdiSourcesAsync(context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(sources)
                End Function)

            ' Stream audio file (with range support)
            app.MapGet("/audio/file/{**path}",
                Async Function(path As String, context As HttpContext) As Task
                    Dim audioService = context.RequestServices.
                        GetService(Of IAudioStreamService)
                    If audioService Is Nothing Then
                        context.Response.StatusCode = 503
                        Return
                    End If

                    ' Parse Range header
                    Dim rangeStart As Long = 0
                    Dim rangeEnd As Long = -1
                    Dim rangeHeader = context.Request.Headers("Range").FirstOrDefault()
                    If rangeHeader IsNot Nothing AndAlso rangeHeader.StartsWith("bytes=") Then
                        Dim rangeParts = rangeHeader.Substring(6).Split("-"c)
                        If rangeParts.Length >= 1 Then Long.TryParse(rangeParts(0), rangeStart)
                        If rangeParts.Length >= 2 AndAlso rangeParts(1).Length > 0 Then
                            Long.TryParse(rangeParts(1), rangeEnd)
                        End If
                    End If

                    Dim result = Await audioService.GetFileStreamAsync(
                        path, rangeStart, rangeEnd, context.RequestAborted)
                    If result Is Nothing Then
                        context.Response.StatusCode = 404
                        Return
                    End If

                    ' Set headers
                    context.Response.ContentType = result.ContentType
                    context.Response.Headers("Accept-Ranges") = "bytes"

                    If rangeHeader IsNot Nothing Then
                        context.Response.StatusCode = 206
                        Dim length = result.RangeEnd - result.RangeStart + 1
                        context.Response.Headers("Content-Range") =
                            $"bytes {result.RangeStart}-{result.RangeEnd}/{result.TotalLength}"
                        context.Response.ContentLength = length
                    Else
                        context.Response.ContentLength = result.TotalLength
                    End If

                    Await result.Stream.CopyToAsync(context.Response.Body, context.RequestAborted)
                    result.Stream.Dispose()
                End Function)
        End Sub

        Private Sub MapTtsEndpoints(app As IEndpointRouteBuilder)

            ' List available audio output devices
            app.MapGet("/tts/devices",
                Function(context As HttpContext) As Task
                    Dim devices = TtsAudioOutput.GetOutputDevices()
                    Return context.Response.WriteAsJsonAsync(devices)
                End Function)

            ' Serve cached TTS audio files — /tts/cache/{filename}
            ' Streams from disk (no full-file memory copy) with cache headers.
            app.MapGet("/tts/cache/{filename}",
                Function(filename As String, context As HttpContext) As IResult
                    ' Validate filename — no path traversal
                    If filename.Contains("..") OrElse filename.Contains("/") OrElse
                       filename.Contains("\") Then
                        Return Results.BadRequest(New With {.error = "invalid filename"})
                    End If

                    Dim cache = context.RequestServices.GetService(Of TtsCache)()
                    If cache Is Nothing Then
                        Return Results.StatusCode(503)
                    End If

                    Dim filePath = IO.Path.Combine(cache.CacheDirectory, filename)
                    If Not IO.File.Exists(filePath) Then
                        Return Results.NotFound(New With {.error = "not found"})
                    End If

                    Dim contentType = "audio/mpeg"
                    If filename.EndsWith(".wav") Then contentType = "audio/wav"
                    If filename.EndsWith(".opus") Then contentType = "audio/ogg"

                    ' Cache TTS audio for 1 hour — same commit/language always produces same audio
                    context.Response.Headers.CacheControl = "public, max-age=3600"

                    ' Stream from disk instead of File.ReadAllBytes
                    Return Results.File(
                        New IO.FileStream(filePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read),
                        contentType,
                        filename)
                End Function)
        End Sub

        Private Sub MapRoomEndpoints(app As IEndpointRouteBuilder)

            ' List public rooms (for lobby)
            app.MapGet("/api/rooms", Function(context As HttpContext) As Task
                                         Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                         Dim rooms = mgr.GetPublicRooms()
                                         Dim result = rooms.Select(Function(r) New With {
                                             .id = r.Id,
                                             .name = r.Name,
                                             .type = r.Type.ToString().ToLower(),
                                             .clients = r.ClientCount,
                                             .createdAt = r.CreatedAt
                                         }).ToArray()
                                         Return context.Response.WriteAsJsonAsync(result)
                                     End Function)

            ' Get a specific room (works for both public and private — you need the ID)
            ' Optional ?clientId= param tells the client if they are the host.
            app.MapGet("/api/rooms/{id}", Function(id As String, context As HttpContext) As Task
                                               Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                               Dim room = mgr.GetRoom(id)
                                               If room Is Nothing Then
                                                   context.Response.StatusCode = 404
                                                   Return context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                               End If
                                               Dim clientId = If(context.Request.Query("clientId").FirstOrDefault(), "")
                                               Dim isHost = Not String.IsNullOrEmpty(clientId) AndAlso room.HostClientId = clientId
                                               Return context.Response.WriteAsJsonAsync(New With {
                                                   .id = room.Id,
                                                   .name = room.Name,
                                                   .type = room.Type.ToString().ToLower(),
                                                   .visibility = room.Visibility.ToString().ToLower(),
                                                   .clients = room.ClientCount,
                                                   .createdAt = room.CreatedAt,
                                                   .isHost = isHost,
                                                   .isLocked = room.IsLocked,
                                                   .pttMode = room.Config.PttMode
                                               })
                                           End Function)

            ' Create a room — returns hostToken for reconnection
            app.MapPost("/api/rooms", Async Function(context As HttpContext) As Task
                                           Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                           Dim doc As JsonDocument = Nothing
                                           Try
                                               doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                               Dim root = doc.RootElement
                                               Dim name = ""
                                               Dim nameProp As JsonElement = Nothing
                                               Dim typeProp As JsonElement = Nothing
                                               Dim visProp As JsonElement = Nothing
                                               Dim hostId = ""

                                               If root.TryGetProperty("name", nameProp) Then name = If(nameProp.GetString(), "")
                                               Dim roomType As RoomType = RoomType.Conference
                                               If root.TryGetProperty("type", typeProp) Then
                                                   Dim typeStr = If(typeProp.GetString(), "")
                                                   Select Case typeStr.ToLower()
                                                       Case "conversation" : roomType = RoomType.Conversation
                                                       Case "workroom" : roomType = RoomType.Workroom
                                                       Case Else : roomType = RoomType.Conference
                                                   End Select
                                               End If
                                               Dim visibility As RoomVisibility = If(roomType = RoomType.Conversation,
                                                   RoomVisibility.Private, RoomVisibility.Public)
                                               If root.TryGetProperty("visibility", visProp) Then
                                                   Dim visStr = If(visProp.GetString(), "")
                                                   If visStr.ToLower() = "private" Then visibility = RoomVisibility.Private
                                                   If visStr.ToLower() = "public" Then visibility = RoomVisibility.Public
                                               End If
                                               Dim hostProp As JsonElement = Nothing
                                               If root.TryGetProperty("hostClientId", hostProp) Then hostId = If(hostProp.GetString(), "")

                                               Dim room = mgr.CreateRoom(name, roomType, visibility, hostId)
                                               context.Response.StatusCode = 201
                                               Await context.Response.WriteAsJsonAsync(New With {
                                                   .id = room.Id,
                                                   .name = room.Name,
                                                   .type = room.Type.ToString().ToLower(),
                                                   .visibility = room.Visibility.ToString().ToLower(),
                                                   .hostToken = room.HostToken
                                               })
                                           Catch ex As Exception
                                               context.Response.StatusCode = 400
                                               context.Response.WriteAsync("{""error"":""Invalid request""}").Wait()
                                           Finally
                                               doc?.Dispose()
                                           End Try
                                       End Function)

            ' Close a room — broadcasts roomClosed to all clients
            app.MapDelete("/api/rooms/{id}", Function(id As String, context As HttpContext) As Task
                                                  Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                  Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                  Dim clientId = context.Request.Query("clientId").FirstOrDefault()
                                                  Dim ok = mgr.CloseRoom(id, If(clientId, ""))
                                                  If Not ok Then
                                                      context.Response.StatusCode = 403
                                                      Return context.Response.WriteAsJsonAsync(New With {.error = "Not authorized or room not found"})
                                                  End If
                                                  ' Broadcast roomClosed to all room members
                                                  hub.BroadcastToRoom(id, "{""type"":""roomClosed""}", "")
                                                  Return context.Response.WriteAsJsonAsync(New With {.ok = True})
                                              End Function)

            ' QR code PNG for a room join URL
            app.MapGet("/api/rooms/{id}/qr", Function(id As String, context As HttpContext) As IResult
                                                  Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                  Dim room = mgr.GetRoom(id)
                                                  If room Is Nothing Then
                                                      Return Results.NotFound(New With {.error = "Room not found"})
                                                  End If
                                                  Dim scheme = If(context.Request.IsHttps, "https", "http")
                                                  Dim host = context.Request.Host.ToString()
                                                  Dim joinUrl = $"{scheme}://{host}/index.html?room={id}"
                                                  Dim pngBytes = GenerateQrPng(joinUrl)
                                                  Return Results.File(pngBytes, "image/png", $"room-{id}.png")
                                              End Function)

            ' Claim host via stored token
            app.MapPost("/api/rooms/{id}/claim-host", Async Function(id As String, context As HttpContext) As Task
                                                            Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                            Dim doc As JsonDocument = Nothing
                                                            Dim ok = False
                                                            Dim failed = False
                                                            Try
                                                                doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                                Dim root = doc.RootElement
                                                                Dim tokenProp As JsonElement = Nothing
                                                                Dim cidProp As JsonElement = Nothing
                                                                Dim hostToken = ""
                                                                Dim clientId = ""
                                                                If root.TryGetProperty("hostToken", tokenProp) Then hostToken = If(tokenProp.GetString(), "")
                                                                If root.TryGetProperty("clientId", cidProp) Then clientId = If(cidProp.GetString(), "")
                                                                ok = mgr.ClaimHost(id, hostToken, clientId)
                                                            Catch
                                                                failed = True
                                                            Finally
                                                                doc?.Dispose()
                                                            End Try
                                                            If failed Then context.Response.StatusCode = 400
                                                            Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                        End Function)

            ' Kick a client from a room (host only)
            app.MapPost("/api/rooms/{id}/kick", Async Function(id As String, context As HttpContext) As Task
                                                      Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                      Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                      Dim doc As JsonDocument = Nothing
                                                      Dim ok = False
                                                      Dim failed = False
                                                      Try
                                                          doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                          Dim root = doc.RootElement
                                                          Dim targetProp As JsonElement = Nothing
                                                          Dim reqProp As JsonElement = Nothing
                                                          Dim targetClientId = ""
                                                          Dim requestingClientId = ""
                                                          If root.TryGetProperty("clientId", targetProp) Then targetClientId = If(targetProp.GetString(), "")
                                                          If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                          ok = mgr.KickClient(id, targetClientId, requestingClientId)
                                                          If ok Then
                                                              hub.SendToClient(targetClientId, "{""type"":""kicked""}")
                                                              hub.BroadcastToRoom(id, "{""type"":""memberLeft"",""clientId"":""" & targetClientId & """}", "")
                                                          End If
                                                      Catch
                                                          failed = True
                                                      Finally
                                                          doc?.Dispose()
                                                      End Try
                                                      If failed Then context.Response.StatusCode = 400
                                                      Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                  End Function)

            ' Lock/unlock a room (host only)
            app.MapPost("/api/rooms/{id}/lock", Async Function(id As String, context As HttpContext) As Task
                                                      Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                      Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                      Dim doc As JsonDocument = Nothing
                                                      Dim ok = False
                                                      Dim failed = False
                                                      Try
                                                          doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                          Dim root = doc.RootElement
                                                          Dim lockedProp As JsonElement = Nothing
                                                          Dim reqProp As JsonElement = Nothing
                                                          Dim locked = False
                                                          Dim requestingClientId = ""
                                                          If root.TryGetProperty("locked", lockedProp) Then locked = lockedProp.GetBoolean()
                                                          If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                          ok = mgr.SetLocked(id, locked, requestingClientId)
                                                          If ok Then
                                                              hub.BroadcastToRoom(id, "{""type"":""roomLocked"",""locked"":" & If(locked, "true", "false") & "}", "")
                                                          End If
                                                      Catch
                                                          failed = True
                                                      Finally
                                                          doc?.Dispose()
                                                      End Try
                                                      If failed Then context.Response.StatusCode = 400
                                                      Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                  End Function)

            ' Set PTT mode for a room (host only)
            app.MapPost("/api/rooms/{id}/ptt-mode", Async Function(id As String, context As HttpContext) As Task
                                                          Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                          Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                          Dim doc As JsonDocument = Nothing
                                                          Dim ok = False
                                                          Dim failed = False
                                                          Try
                                                              doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                              Dim root = doc.RootElement
                                                              Dim modeProp As JsonElement = Nothing
                                                              Dim reqProp As JsonElement = Nothing
                                                              Dim mode = "hold"
                                                              Dim requestingClientId = ""
                                                              If root.TryGetProperty("mode", modeProp) Then mode = If(modeProp.GetString(), "hold")
                                                              If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                              ok = mgr.SetPttMode(id, mode, requestingClientId)
                                                              If ok Then
                                                                  hub.BroadcastToRoom(id, "{""type"":""pttModeChanged"",""mode"":""" & mode & """}", "")
                                                              End If
                                                          Catch
                                                              failed = True
                                                          Finally
                                                              doc?.Dispose()
                                                          End Try
                                                          If failed Then context.Response.StatusCode = 400
                                                          Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                      End Function)

            ' Get room members (clients + virtual members)
            app.MapGet("/api/rooms/{id}/members", Function(id As String, context As HttpContext) As Task
                                                        Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                        Dim subtitleSvc = TryCast(context.RequestServices.GetRequiredService(Of ISubtitleService)(), SubtitleService)
                                                        Dim room = mgr.GetRoom(id)
                                                        If room Is Nothing Then
                                                            context.Response.StatusCode = 404
                                                            Return context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                                        End If
                                                        Dim members As New List(Of Object)()
                                                        ' Real clients
                                                        For Each cid In room.ClientIds.Keys
                                                            Dim client = subtitleSvc?.GetClient(cid)
                                                            If client IsNot Nothing Then
                                                                members.Add(New With {
                                                                    .clientId = cid,
                                                                    .displayName = If(String.IsNullOrEmpty(client.DisplayName), "Guest", client.DisplayName),
                                                                    .language = If(client.Language, ""),
                                                                    .virtual = False
                                                                })
                                                            End If
                                                        Next
                                                        ' Virtual members
                                                        For Each vm In room.VirtualMembers.Values
                                                            members.Add(New With {
                                                                .clientId = vm.Id,
                                                                .displayName = vm.Name,
                                                                .language = If(vm.Language, ""),
                                                                .virtual = True
                                                            })
                                                        Next
                                                        Return context.Response.WriteAsJsonAsync(members)
                                                    End Function)

            ' Add a virtual member (host only)
            app.MapPost("/api/rooms/{id}/virtual-members", Async Function(id As String, context As HttpContext) As Task
                                                                 Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                                 Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                                 Dim doc As JsonDocument = Nothing
                                                                 Dim vm As VirtualMember = Nothing
                                                                 Dim failed = False
                                                                 Dim notAuth = False
                                                                 Try
                                                                     doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                                     Dim root = doc.RootElement
                                                                     Dim nameProp As JsonElement = Nothing
                                                                     Dim langProp As JsonElement = Nothing
                                                                     Dim reqProp As JsonElement = Nothing
                                                                     Dim vmName = ""
                                                                     Dim vmLang = ""
                                                                     Dim requestingClientId = ""
                                                                     If root.TryGetProperty("name", nameProp) Then vmName = If(nameProp.GetString(), "")
                                                                     If root.TryGetProperty("language", langProp) Then vmLang = If(langProp.GetString(), "")
                                                                     If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                                     vm = mgr.AddVirtualMember(id, vmName, vmLang, requestingClientId)
                                                                     If vm Is Nothing Then notAuth = True
                                                                     If vm IsNot Nothing Then
                                                                         hub.BroadcastToRoom(id, "{""type"":""virtualMemberAdded"",""id"":""" & vm.Id & """,""name"":" & SubtitleService.EscapeJson(vm.Name) & ",""language"":""" & If(vm.Language, "") & """}", "")
                                                                     End If
                                                                 Catch
                                                                     failed = True
                                                                 Finally
                                                                     doc?.Dispose()
                                                                 End Try
                                                                 If failed Then
                                                                     context.Response.StatusCode = 400
                                                                     Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid request"})
                                                                 ElseIf notAuth Then
                                                                     context.Response.StatusCode = 403
                                                                     Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized"})
                                                                 Else
                                                                     context.Response.StatusCode = 201
                                                                     Await context.Response.WriteAsJsonAsync(New With {
                                                                         .id = vm.Id,
                                                                         .name = vm.Name,
                                                                         .language = vm.Language
                                                                     })
                                                                 End If
                                                             End Function)

            ' Remove a virtual member (host only)
            app.MapDelete("/api/rooms/{id}/virtual-members/{vmId}", Function(id As String, vmId As String, context As HttpContext) As Task
                                                                          Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                                          Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                                          Dim requestingClientId = If(context.Request.Query("requestingClientId").FirstOrDefault(), "")
                                                                          Dim ok = mgr.RemoveVirtualMember(id, vmId, requestingClientId)
                                                                          If ok Then
                                                                              hub.BroadcastToRoom(id, "{""type"":""virtualMemberRemoved"",""id"":""" & vmId & """}", "")
                                                                          End If
                                                                          If Not ok Then
                                                                              context.Response.StatusCode = 403
                                                                          End If
                                                                          Return context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                                      End Function)

            ' List all rooms (server dashboard — includes private rooms)
            app.MapGet("/api/rooms/all", Function(context As HttpContext) As Task
                                              Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                              Dim rooms = mgr.GetAllRooms()
                                              Dim result = rooms.Select(Function(r) New With {
                                                  .id = r.Id,
                                                  .name = r.Name,
                                                  .type = r.Type.ToString().ToLower(),
                                                  .visibility = r.Visibility.ToString().ToLower(),
                                                  .clients = r.ClientCount,
                                                  .createdAt = r.CreatedAt
                                              }).ToArray()
                                              Return context.Response.WriteAsJsonAsync(result)
                                          End Function)

            ' Translate text on demand (shared-device clients translate cached messages on identity switch)
            app.MapPost("/api/translate", Async Function(context As HttpContext) As Task
                                              Dim translationSvc = context.RequestServices.GetService(Of ITranslationService)()
                                              If translationSvc Is Nothing Then
                                                  context.Response.StatusCode = 503
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Translation not available"})
                                                  Return
                                              End If

                                              Dim body As JsonElement = Nothing
                                              Dim parseOk = True
                                              Try
                                                  body = Await JsonSerializer.DeserializeAsync(Of JsonElement)(context.Request.Body)
                                              Catch
                                                  parseOk = False
                                              End Try
                                              If Not parseOk Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid JSON"})
                                                  Return
                                              End If

                                              Dim textProp As JsonElement = Nothing
                                              Dim srcProp As JsonElement = Nothing
                                              Dim tgtProp As JsonElement = Nothing
                                              If Not body.TryGetProperty("text", textProp) OrElse
                                                 Not body.TryGetProperty("sourceLang", srcProp) OrElse
                                                 Not body.TryGetProperty("targetLang", tgtProp) Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Missing text, sourceLang, or targetLang"})
                                                  Return
                                              End If

                                              Dim text = If(textProp.GetString(), "")
                                              Dim sourceLang = If(srcProp.GetString(), "")
                                              Dim targetLang = If(tgtProp.GetString(), "")

                                              If String.IsNullOrEmpty(text) OrElse String.IsNullOrEmpty(sourceLang) OrElse String.IsNullOrEmpty(targetLang) Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Empty text, sourceLang, or targetLang"})
                                                  Return
                                              End If

                                              If sourceLang = targetLang Then
                                                  Await context.Response.WriteAsJsonAsync(New With {.text = text})
                                                  Return
                                              End If

                                              Dim translated = text
                                              Dim translateOk = True
                                              Try
                                                  Dim results = Await translationSvc.TranslateAsync(
                                                      text, sourceLang, New List(Of String) From {targetLang}, context.RequestAborted)
                                                  If results IsNot Nothing AndAlso results.ContainsKey(targetLang) Then
                                                      translated = results(targetLang)
                                                  End If
                                              Catch ex As Exception
                                                  translateOk = False
                                              End Try
                                              If Not translateOk Then
                                                  context.Response.StatusCode = 500
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Translation failed"})
                                                  Return
                                              End If
                                              Await context.Response.WriteAsJsonAsync(New With {.text = translated})
                                          End Function)

        End Sub

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
