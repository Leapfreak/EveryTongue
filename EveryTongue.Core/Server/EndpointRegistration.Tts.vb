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
    Partial Public Module EndpointRegistration

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

            ' List available audio output devices — enumeration is supplied by the head
            ' (NAudio on Windows); headless hosts have no local audio → empty list.
            app.MapGet("/tts/devices",
                Function(context As HttpContext) As Task
                    Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                    Dim provider = opts?.Value?.AudioDeviceProvider
                    Dim devices = If(provider IsNot Nothing, provider(), New List(Of Services.Audio.AudioOutputDevice)())
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

    End Module
End Namespace
