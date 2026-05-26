Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Piper TTS backend — high-quality offline speech synthesis.
    ''' Priority 1 (preferred). Runs piper.exe locally, no internet required.
    ''' Voice models (~15-50 MB each) stored in tts-models/piper/voices/.
    ''' </summary>
    Public Class PiperBackend
        Implements ITtsBackend

        Private ReadOnly _piperDir As String
        Private ReadOnly _voicesDir As String

        ''' <summary>
        ''' NLLB 3-letter prefix to Piper voice mapping.
        ''' Each entry: (PiperLang, VoiceName, Quality).
        ''' Model filename: {PiperLang}-{VoiceName}-{Quality}.onnx
        ''' </summary>
        Friend Shared ReadOnly VoiceMap As New Dictionary(Of String, (PiperLang As String, VoiceName As String, Quality As String)) From {
            {"eng", ("en_US", "lessac", "medium")},
            {"spa", ("es_ES", "davefx", "medium")},
            {"fra", ("fr_FR", "siwis", "medium")},
            {"deu", ("de_DE", "thorsten", "medium")},
            {"cat", ("ca_ES", "upc_ona", "x_low")},
            {"por", ("pt_BR", "faber", "medium")},
            {"ita", ("it_IT", "riccardo", "x_low")},
            {"zho", ("zh_CN", "huayan", "medium")},
            {"nld", ("nl_NL", "mls", "medium")},
            {"pol", ("pl_PL", "gosia", "medium")},
            {"rus", ("ru_RU", "ruslan", "medium")},
            {"ukr", ("uk_UA", "lada", "x_low")},
            {"ces", ("cs_CZ", "jirka", "medium")},
            {"dan", ("da_DK", "talesyntese", "medium")},
            {"fin", ("fi_FI", "harri", "medium")},
            {"ell", ("el_GR", "rapunzelina", "low")},
            {"hun", ("hu_HU", "anna", "medium")},
            {"isl", ("is_IS", "bui", "medium")},
            {"nor", ("no_NO", "talesyntese", "medium")},
            {"ron", ("ro_RO", "mihai", "medium")},
            {"slk", ("sk_SK", "lili", "medium")},
            {"slv", ("sl_SI", "artur", "medium")},
            {"srp", ("sr_RS", "serbski_institut", "medium")},
            {"swe", ("sv_SE", "nst", "medium")},
            {"swh", ("sw_CD", "lanfrica", "medium")},
            {"tur", ("tr_TR", "dfki", "medium")},
            {"vie", ("vi_VN", "25hours_single", "low")},
            {"kat", ("ka_GE", "natia", "medium")},
            {"kaz", ("kk_KZ", "iseke", "x_low")},
            {"nep", ("ne_NP", "google", "medium")}
        }

        Public Sub New()
            _piperDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tts-models", "piper")
            _voicesDir = Path.Combine(_piperDir, "voices")
        End Sub

        Public ReadOnly Property Name As String Implements ITtsBackend.Name
            Get
                Return "Piper"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Priority As Integer Implements ITtsBackend.Priority
            Get
                Return 1
            End Get
        End Property

        Public Async Function SynthesiseAsync(text As String, language As String,
                                              ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            Dim piperExe = FindPiperExe()
            If piperExe Is Nothing Then Return Nothing

            Dim voicePath = FindVoiceModel(language)
            If voicePath Is Nothing Then Return Nothing

            Dim tempFile = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid():N}.wav")
            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = piperExe,
                    .Arguments = $"--model ""{voicePath}"" --output_file ""{tempFile}""",
                    .UseShellExecute = False,
                    .CreateNoWindow = True,
                    .RedirectStandardInput = True,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .WorkingDirectory = _piperDir
                }

                Using proc = Process.Start(psi)
                    ' Write text to stdin and close to signal EOF
                    Await proc.StandardInput.WriteLineAsync(text)
                    proc.StandardInput.Close()

                    Using cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                        cts.CancelAfter(TimeSpan.FromSeconds(30))
                        Try
                            Await proc.WaitForExitAsync(cts.Token)
                        Catch ex As OperationCanceledException
                            Try : proc.Kill(True) : Catch : End Try
                            Throw
                        End Try
                    End Using

                    If proc.ExitCode = 0 AndAlso File.Exists(tempFile) Then
                        Dim audioBytes = Await File.ReadAllBytesAsync(tempFile, ct)
                        Return New TtsResult With {
                            .AudioData = audioBytes,
                            .Codec = "wav",
                            .SampleRate = 22050
                        }
                    End If
                End Using
            Finally
                Try : File.Delete(tempFile) : Catch : End Try
            End Try

            Return Nothing
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            Dim supported As New List(Of String)()
            If FindPiperExe() IsNot Nothing AndAlso Directory.Exists(_voicesDir) Then
                For Each kvp In VoiceMap
                    Dim modelName = GetModelFileNameFromVoice(kvp.Value)
                    If File.Exists(Path.Combine(_voicesDir, modelName)) Then
                        supported.Add(kvp.Key)
                    End If
                Next
            End If
            Return Task.FromResult(DirectCast(supported, IReadOnlyList(Of String)))
        End Function

        Public Function IsLanguageSupportedAsync(language As String,
                                                  ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            If FindPiperExe() Is Nothing Then Return Task.FromResult(False)
            Return Task.FromResult(FindVoiceModel(language) IsNot Nothing)
        End Function

        Public Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            Return Task.FromResult(FindPiperExe() IsNot Nothing)
        End Function

        Private Function FindPiperExe() As String
            Dim exePath = Path.Combine(_piperDir, "piper.exe")
            If File.Exists(exePath) Then Return exePath
            Return Nothing
        End Function

        Private Function FindVoiceModel(language As String) As String
            If Not Directory.Exists(_voicesDir) Then Return Nothing
            Dim langPrefix = language.Split("_"c)(0).ToLower()

            Dim voiceInfo As (PiperLang As String, VoiceName As String, Quality As String) = Nothing
            If Not VoiceMap.TryGetValue(langPrefix, voiceInfo) Then Return Nothing

            Dim modelPath = Path.Combine(_voicesDir, GetModelFileNameFromVoice(voiceInfo))
            If File.Exists(modelPath) Then Return modelPath

            Return Nothing
        End Function

        Private Shared Function GetModelFileNameFromVoice(
            voice As (PiperLang As String, VoiceName As String, Quality As String)) As String
            Return $"{voice.PiperLang}-{voice.VoiceName}-{voice.Quality}.onnx"
        End Function

        ''' <summary>
        ''' Returns the model filename for a given NLLB language prefix, or Nothing if unsupported.
        ''' </summary>
        Friend Shared Function GetModelFileName(nllbPrefix As String) As String
            Dim voiceInfo As (PiperLang As String, VoiceName As String, Quality As String) = Nothing
            If Not VoiceMap.TryGetValue(nllbPrefix, voiceInfo) Then Return Nothing
            Return GetModelFileNameFromVoice(voiceInfo)
        End Function

        ''' <summary>
        ''' Returns the HuggingFace base URL for a voice model (without extension).
        ''' Append .onnx or .onnx.json to get the actual download URLs.
        ''' </summary>
        Friend Shared Function GetModelDownloadUrl(nllbPrefix As String) As String
            Dim voiceInfo As (PiperLang As String, VoiceName As String, Quality As String) = Nothing
            If Not VoiceMap.TryGetValue(nllbPrefix, voiceInfo) Then Return Nothing
            Dim shortLang = voiceInfo.PiperLang.Substring(0, 2)
            Dim modelName = $"{voiceInfo.PiperLang}-{voiceInfo.VoiceName}-{voiceInfo.Quality}"
            Return $"https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.0/{shortLang}/{voiceInfo.PiperLang}/{voiceInfo.VoiceName}/{voiceInfo.Quality}/{modelName}"
        End Function
    End Class
End Namespace
