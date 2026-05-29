Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Edge TTS backend — free cloud TTS using Microsoft Edge's read-aloud API.
    ''' Priority 3 (cloud fallback). 400+ voices, 100+ languages, best quality.
    ''' Uses the edge-tts Python package (no API key needed).
    ''' </summary>
    Public Class EdgeTtsBackend
        Implements ITtsBackend

        Public ReadOnly Property Name As String Implements ITtsBackend.Name
            Get
                Return "EdgeTTS"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property Priority As Integer Implements ITtsBackend.Priority
            Get
                Return 3
            End Get
        End Property

        Public Async Function SynthesiseAsync(text As String, language As String,
                                              ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            ' Use edge-tts CLI via Python to generate audio
            Dim pythonPath = FindPython()
            If String.IsNullOrEmpty(pythonPath) Then Return Nothing

            Dim tempFile = Path.Combine(Path.GetTempPath(),
                $"edge_tts_{Guid.NewGuid():N}.mp3")

            Dim textFile = Path.Combine(Path.GetTempPath(),
                $"edge_tts_{Guid.NewGuid():N}.txt")
            Try
                ' Write text to file to avoid CLI escaping issues
                Await File.WriteAllTextAsync(textFile, text, ct)

                Dim voice = GetVoiceForLanguage(language)
                Dim psi As New ProcessStartInfo() With {
                    .FileName = pythonPath,
                    .Arguments = $"-m edge_tts --voice ""{voice}"" --file ""{textFile}"" --write-media ""{tempFile}""",
                    .UseShellExecute = False,
                    .CreateNoWindow = True,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True
                }

                Using proc = Process.Start(psi)
                    Await proc.WaitForExitAsync(ct)
                    If proc.ExitCode = 0 AndAlso File.Exists(tempFile) Then
                        Dim audioBytes = Await File.ReadAllBytesAsync(tempFile, ct)
                        Return New TtsResult With {
                            .AudioData = audioBytes,
                            .Codec = "mp3",
                            .SampleRate = 24000
                        }
                    End If
                End Using
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] EdgeTtsBackend.SynthesiseAsync: {ex.Message}")
            Finally
                Try : File.Delete(tempFile) : Catch : End Try
                Try : File.Delete(textFile) : Catch : End Try
            End Try

            Return Nothing
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            ' Edge TTS supports 100+ languages — broad coverage
            Return Task.FromResult(DirectCast(New List(Of String)(), IReadOnlyList(Of String)))
        End Function

        Public Function IsLanguageSupportedAsync(language As String,
                                                  ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            ' Edge TTS has very broad coverage
            Return Task.FromResult(True)
        End Function

        Public Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            ' Check if edge-tts package is available
            Return Task.FromResult(Not String.IsNullOrEmpty(FindPython()))
        End Function

        Private Shared Function GetVoiceForLanguage(language As String) As String
            ' Map common NLLB codes to Edge TTS voice names
            ' Default to a reasonable voice per language
            Select Case language.Split("_"c)(0).ToLower()
                Case "eng" : Return "en-US-JennyNeural"
                Case "spa" : Return "es-ES-ElviraNeural"
                Case "fra" : Return "fr-FR-DeniseNeural"
                Case "deu" : Return "de-DE-KatjaNeural"
                Case "cat" : Return "ca-ES-JoanaNeural"
                Case "por" : Return "pt-BR-FranciscaNeural"
                Case "ita" : Return "it-IT-ElsaNeural"
                Case "jpn" : Return "ja-JP-NanamiNeural"
                Case "zho" : Return "zh-CN-XiaoxiaoNeural"
                Case "kor" : Return "ko-KR-SunHiNeural"
                Case "arb" : Return "ar-SA-ZariyahNeural"
                Case "hin" : Return "hi-IN-SwaraNeural"
                Case "rus" : Return "ru-RU-SvetlanaNeural"
                Case Else : Return "en-US-JennyNeural"
            End Select
        End Function

        Private Shared Function FindPython() As String
            Return Pipeline.ProcessHelper.FindPython()
        End Function
    End Class
End Namespace
