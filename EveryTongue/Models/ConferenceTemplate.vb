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
        ''' <summary>
        ''' Name of the saved input device. PortAudio device INDICES (AudioDeviceId) renumber
        ''' when devices are added/removed, so a stored index can drift to the wrong device
        ''' (e.g. an S/PDIF input) → silent capture. At capture start the index is re-resolved
        ''' from this name against the current device enumeration; AudioDeviceId is the fallback
        ''' for templates saved before this field existed.
        ''' </summary>
        Public Property AudioDeviceName As String = ""
        Public Property AudioSourceLabel As String = ""
        Public Property DefaultVisibility As String = "public"
        ''' <summary>Online/Offline gate for sessions hosted from this template (explicit switch, no auto-fallback).</summary>
        Public Property Mode As Templates.ConnectivityMode = Templates.ConnectivityMode.Online
        ''' <summary>Conference speakers (SpeakerProfile ids; list order = display order; first is the default).</summary>
        Public Property SpeakerProfileIds As New List(Of String)
        ''' <summary>Display template reference ("" = app-global subtitle appearance).</summary>
        Public Property DisplayTemplateId As String = ""
        ''' <summary>Filter set reference ("" = the global glossary/profanity/hallucination files).</summary>
        Public Property FilterSetId As String = ""
    End Class

End Namespace
