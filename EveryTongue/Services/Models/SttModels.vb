Namespace Services.Models
    ''' <summary>
    ''' Event args for STT output (interim updates and final commits).
    ''' </summary>
    Public Class SttOutputEventArgs
        Inherits EventArgs
        Public Property Text As String
        Public Property DetectedLanguage As String

        Public Sub New()
            Text = ""
            DetectedLanguage = ""
        End Sub

        Public Sub New(text As String, Optional detectedLanguage As String = "")
            Me.Text = If(text, "")
            Me.DetectedLanguage = If(detectedLanguage, "")
        End Sub
    End Class

    ''' <summary>
    ''' Engine-agnostic configuration for an STT session.
    ''' </summary>
    Public Class SttConfig
        Public Property DeviceIndex As Integer = 0
        Public Property Language As String = "auto"
        Public Property ModelPath As String = ""
        Public Property ComputeType As String = "int8_float16"
        Public Property UseGpu As Boolean = True
        Public Property BeamSize As Integer = 7
        Public Property BestOf As Integer = 5
        Public Property VadSilenceMs As Integer = 800
        Public Property MaxSegmentSec As Integer = 15
        Public Property InterimIntervalMs As Integer = 1000
        Public Property InitialPrompt As String = ""
        Public Property TranslateToEnglish As Boolean = False
        Public Property ServerPort As Integer = 5100

        ''' <summary>Backend key: "whisper-cpp-vulkan", "whisper-cpp-cuda", "whisper-cpp-cpu".</summary>
        Public Property Backend As String = "whisper-cpp-vulkan"

        ''' <summary>Path to whisper-server.exe (whisper-cpp backends only).</summary>
        Public Property WhisperServerPath As String = ""

        ''' <summary>Port for whisper-server.exe inference endpoint.</summary>
        Public Property WhisperServerPort As Integer = 8178

        ''' <summary>Path to Silero VAD GGML model for whisper-server built-in VAD.</summary>
        Public Property SileroVadModelPath As String = ""

        ''' <summary>API key for cloud STT backends (e.g. Google Cloud STT, Speechmatics).</summary>
        Public Property ApiKey As String = ""

        ''' <summary>Region for cloud STT backends that are region-scoped (e.g. Speechmatics "eu2"/"us").</summary>
        Public Property Region As String = ""

        ''' <summary>Operating point / quality tier for cloud STT backends (e.g. Speechmatics "enhanced"/"standard").</summary>
        Public Property OperatingPoint As String = ""

        ''' <summary>End-of-utterance silence trigger (ms) for streaming engines that endpoint server-side (Speechmatics). 0 = engine default.</summary>
        Public Property EouSilenceMs As Integer = 0

        ''' <summary>Whether the STT engine should translate inline (Speechmatics built-in translation).</summary>
        Public Property EnableTranslation As Boolean = False

        ''' <summary>Speechmatics translation target codes (engine-native, e.g. "es","de","cmn"). Max 5.</summary>
        Public Property TranslationTargets As New List(Of String)
    End Class

    ''' <summary>
    ''' Event args for an STT commit that already carries inline translations from
    ''' the engine (e.g. Speechmatics). Translations are keyed by the engine's own
    ''' language codes; the consumer converts them to FLORES.
    ''' </summary>
    Public Class SttTranslatedCommitEventArgs
        Inherits EventArgs
        ''' <summary>Source (transcribed) text.</summary>
        Public Property Text As String
        ''' <summary>Source language (engine code, e.g. whisper/ISO-639-1).</summary>
        Public Property DetectedLanguage As String
        ''' <summary>Translations keyed by engine target code → translated text.</summary>
        Public Property Translations As Dictionary(Of String, String)

        Public Sub New(text As String, detectedLanguage As String, translations As Dictionary(Of String, String))
            Me.Text = If(text, "")
            Me.DetectedLanguage = If(detectedLanguage, "")
            Me.Translations = If(translations, New Dictionary(Of String, String))
        End Sub
    End Class

    ''' <summary>
    ''' Structured audio device information.
    ''' </summary>
    Public Class AudioDeviceInfo
        Public Property Id As Integer
        Public Property Name As String

        Public Sub New()
        End Sub

        Public Sub New(id As Integer, name As String)
            Me.Id = id
            Me.Name = If(name, "")
        End Sub

        Public Overrides Function ToString() As String
            Return $"{Id}: {Name}"
        End Function
    End Class
End Namespace
