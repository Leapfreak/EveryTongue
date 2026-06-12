Namespace Models

    Public Class ConferenceTemplate
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        Public Property HostingCode As String = ""
        Public Property SourceLanguage As String = "auto"
        Public Property SttBackendKey As String = "whisper-cpp-vulkan"
        Public Property TranslationBackendKey As String = "nllb"
        ''' <summary>
        ''' Reference into the STT template library (TemplateLibraryStore, group "stt").
        ''' The engine knobs below are legacy embeds kept for config.json back-compat;
        ''' they are migrated into the referenced template on load and only used as
        ''' a fallback when the reference can't be resolved.
        ''' </summary>
        Public Property SttTemplateId As String = ""
        Public Property BeamSize As Integer = 7
        Public Property MaxSegmentSec As Integer = 15
        Public Property VadSilenceMs As Integer = 800
        Public Property InitialPrompt As String = ""
        Public Property ModelPath As String = ""
        Public Property AudioDeviceId As Integer = -1
        Public Property AudioSourceLabel As String = ""
        Public Property DefaultVisibility As String = "public"
    End Class

End Namespace
