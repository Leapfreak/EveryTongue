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

    ''' NOTE: the old SttConfig god-object (whisper + cloud fields co-mingled)
    ''' lived here. It was split into SttSessionConfig (engine-agnostic session
    ''' fields) + per-engine config blocks in Services/Stt/Configs/.

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
