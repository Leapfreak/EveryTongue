Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Meta MMS-TTS backend — 1100+ languages via PyTorch.
    ''' Priority 2 (fallback when Piper doesn't cover the language).
    ''' Runs as a Python FastAPI sidecar (same pattern as NLLB).
    ''' </summary>
    Public Class MmsTtsBackend
        Implements ITtsBackend

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(30)
        }
        Private _port As Integer = 5092
        Private _isRunning As Boolean = False

        Public ReadOnly Property Name As String Implements ITtsBackend.Name
            Get
                Return "MMS-TTS"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Priority As Integer Implements ITtsBackend.Priority
            Get
                Return 2
            End Get
        End Property

        Public Async Function SynthesiseAsync(text As String, language As String,
                                              ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            If Not _isRunning Then Return Nothing

            Try
                Dim json = $"{{""text"":{JsonSerializer.Serialize(text)},""language"":""{language}""}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Dim response = Await _httpClient.PostAsync(
                    $"http://127.0.0.1:{_port}/synthesise", content, ct)

                If response.IsSuccessStatusCode Then
                    Dim audioBytes = Await response.Content.ReadAsByteArrayAsync()
                    Return New TtsResult With {
                        .AudioData = audioBytes,
                        .Codec = "wav",
                        .SampleRate = 16000
                    }
                End If
            Catch
            End Try

            Return Nothing
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            ' MMS-TTS supports 1100+ languages — return empty list until server provides it
            Return Task.FromResult(DirectCast(New List(Of String)(), IReadOnlyList(Of String)))
        End Function

        Public Function IsLanguageSupportedAsync(language As String,
                                                  ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            ' MMS-TTS has very broad coverage — assume supported if server is running
            Return Task.FromResult(_isRunning)
        End Function

        Public Async Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            If Not _isRunning Then Return False
            Try
                Dim response = Await _httpClient.GetAsync(
                    $"http://127.0.0.1:{_port}/health", ct)
                Return response.IsSuccessStatusCode
            Catch
                Return False
            End Try
        End Function
    End Class
End Namespace
