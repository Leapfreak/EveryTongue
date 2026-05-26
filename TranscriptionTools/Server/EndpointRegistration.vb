Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Options
Imports TranscriptionTools.Server.Hubs
Imports TranscriptionTools.Services.Infrastructure
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Audio
Imports TranscriptionTools.Services.Tts

Namespace Server
    ''' <summary>
    ''' Registers all Minimal API endpoints on the Kestrel WebApplication.
    ''' Each route defined once — served on both HTTP and HTTPS automatically.
    ''' </summary>
    Public Module EndpointRegistration

        Public Sub MapAllEndpoints(app As IEndpointRouteBuilder)
            MapWebSocketEndpoint(app)
            MapCoreEndpoints(app)
            MapControlEndpoint(app)
            MapBibleEndpoints(app)
            MapAudioEndpoints(app)
            MapTtsEndpoints(app)
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
            app.MapGet("/api/health", Function(context As HttpContext) As IResult
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

                                          Return Results.Ok(New With {
                                              .status = overall,
                                              .checks = checks,
                                              .uptime = uptime,
                                              .version = version,
                                              .timestamp = DateTime.UtcNow
                                          })
                                      End Function).
                ExcludeFromDescription()

            ' Metrics — detailed runtime statistics
            app.MapGet("/api/metrics", Function(context As HttpContext) As IResult
                                           Dim metricsSvc = context.RequestServices.
                                               GetService(Of IMetricsService)
                                           If metricsSvc Is Nothing Then
                                               Return Results.Json(New With {.error = "Metrics not available"})
                                           End If
                                           Return Results.Ok(metricsSvc.GetSnapshot())
                                       End Function)

            ' Client configuration — replaces {{BG_COLOR}}/{{FG_COLOR}} template injection
            ' Client fetches this on load to configure itself.
            ' Cached 60s — colors/config rarely change mid-service.
            app.MapGet("/api/config", Function(context As HttpContext) As IResult
                                          context.Response.Headers.CacheControl = "public, max-age=60"

                                          Dim opts = context.RequestServices.
                                              GetService(Of IOptions(Of ServerOptions))
                                          Dim serverOpts = If(opts?.Value, New ServerOptions())

                                          ' Determine WebSocket URL from the incoming request
                                          Dim scheme = If(context.Request.IsHttps, "wss", "ws")
                                          Dim host = context.Request.Host.ToString()
                                          Dim wsUrl = $"{scheme}://{host}/ws"

                                          Return Results.Ok(New With {
                                              .bgColor = serverOpts.BgColor,
                                              .fgColor = serverOpts.FgColor,
                                              .wsUrl = wsUrl,
                                              .httpsEnabled = True,
                                              .hasAdminPin = Not String.IsNullOrEmpty(serverOpts.AdminPin),
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

        ''' <summary>
        ''' Callback for remote control commands from /api/control.
        ''' Set by FormMain to handle start/stop/restart/simulate/clear/setSliders.
        ''' </summary>
        Public RemoteCommandHandler As Action(Of String)

        Private Sub MapControlEndpoint(app As IEndpointRouteBuilder)

            ' Admin remote control — /api/control?action=start|stop|restart|simulate|clear|status|tune|setsliders
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
                                                   .sim = subtitleService.IsSimulating,
                                                   .inputLang = If(subtitleService.InputLanguage, "")
                                               })
                                           End If

                                           Select Case action.ToLower()
                                               Case "start", "stop", "restart", "simulate", "clear"
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
                                                  Dim bibleService = context.RequestServices.
                                                      GetService(Of IBibleService)
                                                  If bibleService Is Nothing Then
                                                      Await context.Response.WriteAsJsonAsync(
                                                          New With {.error = "Bible service not available"})
                                                      Return
                                                  End If
                                                  Dim lang = context.Request.Query("lang").FirstOrDefault()
                                                  Dim translations = Await bibleService.GetTranslationsAsync(
                                                      If(lang, ""), context.RequestAborted)
                                                  Await context.Response.WriteAsJsonAsync(translations)
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
                                           Dim result = Await bibleService.ParseReferenceAsync(ref, lang)
                                           Await context.Response.WriteAsJsonAsync(result)
                                       End Function)
        End Sub

        Private Sub MapAudioEndpoints(app As IEndpointRouteBuilder)

            ' List NDI sources
            app.MapGet("/audio/ndi/sources",
                Async Function(context As HttpContext) As Task(Of IResult)
                    Dim audioService = context.RequestServices.
                        GetService(Of IAudioStreamService)
                    If audioService Is Nothing Then
                        Return Results.Json(New With {.error = "Audio service not available"})
                    End If
                    Dim sources = Await audioService.GetNdiSourcesAsync(context.RequestAborted)
                    Return Results.Ok(sources)
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
                Function(context As HttpContext) As IResult
                    Dim devices = TtsAudioOutput.GetOutputDevices()
                    Return Results.Ok(devices)
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
