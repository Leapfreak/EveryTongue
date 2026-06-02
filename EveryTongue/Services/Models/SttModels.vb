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
        Public Property VadSilenceMs As Integer = 800
        Public Property MaxSegmentSec As Integer = 15
        Public Property InterimIntervalMs As Integer = 1000
        Public Property InitialPrompt As String = ""
        Public Property TranslateToEnglish As Boolean = False
        Public Property ServerPort As Integer = 5100
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
