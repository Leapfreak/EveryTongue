Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Google Cloud Text-to-Speech backend — REST synthesis against
    ''' https://texttospeech.googleapis.com/v1/text:synthesize. Voice selection
    ''' passes only the BCP-47 languageCode (derived from the shared
    ''' NeuralVoiceCatalog locale) and lets Google pick its default voice for
    ''' that language — guessing vendor voice names risks API errors. Returns
    ''' MP3 (base64 audioContent) to match the TTS pipeline.
    ''' </summary>
    Public Class GoogleTtsBackend
        Inherits CloudTtsBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Google-TTS"   ' lowercase = registry key "google-tts"
            End Get
        End Property

        Public Overrides ReadOnly Property Priority As Integer
            Get
                Return 5
            End Get
        End Property

        Public Overrides Async Function SynthesiseAsync(text As String,
                                                        language As String,
                                                        ct As CancellationToken
        ) As Task(Of TtsResult)
            If Not IsAvailable Then Return Nothing
            Try
                Dim languageCode = NeuralVoiceCatalog.GetLocaleForLanguage(language)
                Dim body As String
                Using stream As New IO.MemoryStream()
                    Using writer As New Utf8JsonWriter(stream)
                        writer.WriteStartObject()
                        writer.WriteStartObject("input")
                        writer.WriteString("text", If(text, ""))
                        writer.WriteEndObject()
                        writer.WriteStartObject("voice")
                        writer.WriteString("languageCode", languageCode)
                        writer.WriteEndObject()
                        writer.WriteStartObject("audioConfig")
                        writer.WriteString("audioEncoding", "MP3")
                        writer.WriteNumber("sampleRateHertz", 24000)
                        writer.WriteEndObject()
                        writer.WriteEndObject()
                    End Using
                    body = Encoding.UTF8.GetString(stream.ToArray())
                End Using

                Dim content As New StringContent(body, Encoding.UTF8, "application/json")
                ' NOTE: URL carries the key — never log the URL
                Dim response = Await HttpClient.PostAsync(
                    $"https://texttospeech.googleapis.com/v1/text:synthesize?key={ApiKey}", content, ct)
                If response.IsSuccessStatusCode Then
                    Dim responseBody = Await response.Content.ReadAsStringAsync(ct)
                    Using doc = JsonDocument.Parse(responseBody)
                        Dim audioContent As JsonElement
                        If doc.RootElement.TryGetProperty("audioContent", audioContent) Then
                            Dim audioBytes = Convert.FromBase64String(audioContent.GetString())
                            If audioBytes.Length > 0 Then
                                Return New TtsResult With {
                                    .AudioData = audioBytes,
                                    .Codec = "mp3",
                                    .SampleRate = 24000
                                }
                            End If
                        End If
                    End Using
                Else
                    AppLogger.Log(LogEvents.TTS_ENGINE_ERROR,
                        $"GoogleTtsBackend: HTTP {CInt(response.StatusCode)} for lang={language} ({languageCode})")
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"GoogleTtsBackend.SynthesiseAsync: {ex.Message}")
            End Try
            Return Nothing
        End Function

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Try
                ' Cheap voices listing validates the key (URL carries the key — never log it)
                Dim response = Await HttpClient.GetAsync(
                    $"https://texttospeech.googleapis.com/v1/voices?key={ApiKey}", ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"GoogleTtsBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class
End Namespace
