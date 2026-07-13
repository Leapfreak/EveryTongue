Imports System.Net.Http
Imports System.Security
Imports System.Text
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Azure AI Speech TTS backend — REST synthesis against
    ''' https://{region}.tts.speech.microsoft.com/cognitiveservices/v1.
    ''' The Endpoint value holds the Azure region (default "westeurope",
    ''' pushed from the registry entry). Uses the same neural voice catalogue
    ''' as Edge TTS (shared via NeuralVoiceCatalog) and returns 24kHz MP3 to
    ''' match the rest of the TTS pipeline.
    ''' </summary>
    Public Class AzureTtsBackend
        Inherits CloudTtsBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Azure-TTS"   ' lowercase = registry key "azure-tts"
            End Get
        End Property

        Public Overrides ReadOnly Property Priority As Integer
            Get
                Return 4
            End Get
        End Property

        Private ReadOnly Property Region As String
            Get
                Return If(String.IsNullOrEmpty(Endpoint), "westeurope", Endpoint)
            End Get
        End Property

        Public Overrides Async Function SynthesiseAsync(text As String,
                                                        language As String,
                                                        ct As CancellationToken
        ) As Task(Of TtsResult)
            If Not IsAvailable Then Return Nothing
            Try
                Dim voice = NeuralVoiceCatalog.GetVoiceForLanguage(language)
                Dim locale = NeuralVoiceCatalog.GetLocaleForLanguage(language)
                Dim ssml = $"<speak version='1.0' xml:lang='{locale}'><voice name='{voice}'>{SecurityElement.Escape(If(text, ""))}</voice></speak>"

                Dim request As New HttpRequestMessage(HttpMethod.Post,
                    $"https://{Region}.tts.speech.microsoft.com/cognitiveservices/v1")
                request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey)
                ' MP3 to match what the TTS pipeline/cache expects (EdgeTTS also returns 24kHz MP3)
                request.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3")
                request.Headers.TryAddWithoutValidation("User-Agent", "EveryTongue")
                request.Content = New StringContent(ssml, Encoding.UTF8, "application/ssml+xml")

                Dim response = Await HttpClient.SendAsync(request, ct)
                If response.IsSuccessStatusCode Then
                    Dim audioBytes = Await response.Content.ReadAsByteArrayAsync(ct)
                    If audioBytes IsNot Nothing AndAlso audioBytes.Length > 0 Then
                        Return New TtsResult With {
                            .AudioData = audioBytes,
                            .Codec = "mp3",
                            .SampleRate = 24000
                        }
                    End If
                Else
                    AppLogger.Log(LogEvents.TTS_ENGINE_ERROR,
                        $"AzureTtsBackend: HTTP {CInt(response.StatusCode)} for lang={language} voice={voice} region={Region}")
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"AzureTtsBackend.SynthesiseAsync: {ex.Message}")
            End Try
            Return Nothing
        End Function

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Try
                ' Cheap voices/list call validates both key and region
                Dim request As New HttpRequestMessage(HttpMethod.Get,
                    $"https://{Region}.tts.speech.microsoft.com/cognitiveservices/voices/list")
                request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey)
                Dim response = Await HttpClient.SendAsync(request, ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"AzureTtsBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class
End Namespace
