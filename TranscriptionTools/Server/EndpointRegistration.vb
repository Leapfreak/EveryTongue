Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Options
Imports TranscriptionTools.Services.Infrastructure

Namespace Server
    ''' <summary>
    ''' Registers all Minimal API endpoints on the Kestrel WebApplication.
    ''' Each route defined once — served on both HTTP and HTTPS automatically.
    ''' </summary>
    Public Module EndpointRegistration

        Public Sub MapAllEndpoints(app As IEndpointRouteBuilder)
            MapCoreEndpoints(app)
            ' Future phases will add:
            ' MapBibleEndpoints(app)
            ' MapTtsEndpoints(app)
            ' MapAudioEndpoints(app)
            ' MapFeedbackEndpoints(app)
        End Sub

        Private Sub MapCoreEndpoints(app As IEndpointRouteBuilder)

            ' Health check — lightweight, returns server status
            app.MapGet("/api/health", Function(context As HttpContext) As IResult
                                          Dim ver = GetType(EndpointRegistration).Assembly.
                                              GetName().Version
                                          Dim version = If(ver?.ToString(), "unknown")
                                          Return Results.Ok(New With {
                                              .status = "healthy",
                                              .version = version,
                                              .timestamp = DateTime.UtcNow
                                          })
                                      End Function).
                ExcludeFromDescription()

            ' Client configuration — replaces {{BG_COLOR}}/{{FG_COLOR}} template injection
            ' Client fetches this on load to configure itself
            app.MapGet("/api/config", Function(context As HttpContext) As IResult
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
                                              .version = If(GetType(EndpointRegistration).Assembly.
                                                  GetName().Version?.ToString(), "unknown")
                                          })
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
        ''' Generates a minimal silent WAV file (2 seconds, 8kHz, 8-bit mono).
        ''' Same as the existing SubtitleServer implementation.
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
