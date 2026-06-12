Imports System.Text.Json

Namespace Models.Templates

    ''' <summary>
    ''' Explicit per-session connectivity switch. Filters which engine templates
    ''' are eligible (RequiresInternet) — there is NO auto-fallback when offline;
    ''' an ineligible slot simply resolves empty. (The "looks like you're offline"
    ''' prompt is parked in PLAN.md → Future Work.)
    ''' </summary>
    Public Enum ConnectivityMode
        Online
        Offline
    End Enum

    ''' <summary>
    ''' Tier-3 session template: references one named template per slot by id
    ''' (never embeds them — edit a template once and every session using it
    ''' updates), plus optional per-slot field overrides.
    ''' </summary>
    Public Class SessionTemplate
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        ''' <summary>Online/Offline gate for this session.</summary>
        Public Property Mode As ConnectivityMode = ConnectivityMode.Online
        Public Property SttTemplateId As String = ""
        Public Property TranslateTemplateId As String = ""
        Public Property TtsTemplateId As String = ""
        Public Property DisplayTemplateId As String = ""
        ''' <summary>Filter set reference. Empty = the global set.</summary>
        Public Property FilterSetId As String = ""
        ''' <summary>Conference speakers participating in this session (reference order = display order).</summary>
        Public Property SpeakerProfileIds As New List(Of String)
        ''' <summary>Per-slot overrides: slot key ("stt"/"translate"/"tts") → field name → value.</summary>
        Public Property SlotOverrides As New Dictionary(Of String, Dictionary(Of String, JsonElement))
    End Class

End Namespace
