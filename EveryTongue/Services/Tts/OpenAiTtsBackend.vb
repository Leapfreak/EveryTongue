Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' OpenAI TTS backend — POST https://api.openai.com/v1/audio/speech with
    ''' the gpt-4o-mini-tts model. OpenAI voices are multilingual (the model
    ''' speaks the language of the input text), so the session language maps to
    ''' no per-language voice — a single default voice ("alloy") is used for
    ''' all languages. Returns MP3 to match the TTS pipeline.
    ''' </summary>
    Public Class OpenAiTtsBackend
        Inherits CloudTtsBackend

        ''' <summary>Default multilingual voice. OpenAI's fixed catalogue: alloy,
        ''' ash, ballad, coral, echo, fable, nova, onyx, sage, shimmer, verse.</summary>
        Private Const DefaultVoice As String = "alloy"
        Private Const Model As String = "gpt-4o-mini-tts"

        Public Overrides ReadOnly Property Name As String
            Get
                Return "OpenAI-TTS"   ' lowercase = registry key "openai-tts"
            End Get
        End Property

        Public Overrides ReadOnly Property Priority As Integer
            Get
                Return 6
            End Get
        End Property

        Public Overrides Async Function SynthesiseAsync(text As String,
                                                        language As String,
                                                        ct As CancellationToken
        ) As Task(Of TtsResult)
            If Not IsAvailable Then Return Nothing
            Try
                Dim body As String
                Using stream As New IO.MemoryStream()
                    Using writer As New Utf8JsonWriter(stream)
                        writer.WriteStartObject()
                        writer.WriteString("model", Model)
                        writer.WriteString("voice", DefaultVoice)
                        writer.WriteString("input", If(text, ""))
                        writer.WriteString("response_format", "mp3")
                        writer.WriteEndObject()
                    End Using
                    body = Encoding.UTF8.GetString(stream.ToArray())
                End Using

                Dim request As New HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/speech")
                request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", ApiKey)
                request.Content = New StringContent(body, Encoding.UTF8, "application/json")

                Dim response = Await HttpClient.SendAsync(request, ct)
                If response.IsSuccessStatusCode Then
                    ' Response body is the raw audio bytes
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
                        $"OpenAiTtsBackend: HTTP {CInt(response.StatusCode)} for lang={language}")
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"OpenAiTtsBackend.SynthesiseAsync: {ex.Message}")
            End Try
            Return Nothing
        End Function

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Try
                ' Cheap models listing validates the key
                Dim request As New HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models")
                request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", ApiKey)
                Dim response = Await HttpClient.SendAsync(request, ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"OpenAiTtsBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class
End Namespace
