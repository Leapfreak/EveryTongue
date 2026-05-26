Namespace Services.Models
    ''' <summary>
    ''' Result of a TTS synthesis operation.
    ''' </summary>
    Public Class TtsResult
        Public Property AudioData As Byte()
        Public Property Codec As String
        Public Property DurationMs As Integer
        Public Property SampleRate As Integer
    End Class

    ''' <summary>
    ''' Information about an available TTS voice.
    ''' </summary>
    Public Class TtsVoiceInfo
        Public Property Id As String
        Public Property Name As String
        Public Property Language As String
        Public Property Gender As String
        Public Property Backend As String
    End Class

    ''' <summary>
    ''' Cache statistics for the TTS service.
    ''' </summary>
    Public Class TtsCacheStats
        Public Property EntryCount As Integer
        Public Property TotalSizeBytes As Long
        Public Property HitRate As Double
        Public Property OldestEntryAge As TimeSpan
    End Class
End Namespace
