Namespace Models.Templates

    ''' <summary>
    ''' Conference speaker profile — a thin bundle of REFERENCES to preferred
    ''' templates (online + offline STT variants, translate, TTS) plus a glossary
    ''' set. Swapping a speaker's Sunday "sharing" template for a "reading" one
    ''' means changing one slot's reference; nothing is embedded here.
    ''' </summary>
    Public Class SpeakerProfile
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        ''' <summary>STT template used when the session runs online.</summary>
        Public Property OnlineSttTemplateId As String = ""
        ''' <summary>STT template used when the session runs offline.</summary>
        Public Property OfflineSttTemplateId As String = ""
        Public Property TranslateTemplateId As String = ""
        Public Property TtsTemplateId As String = ""
        ''' <summary>Glossary/filter set reference. Empty = the global set.</summary>
        Public Property GlossarySetId As String = ""
    End Class

End Namespace
