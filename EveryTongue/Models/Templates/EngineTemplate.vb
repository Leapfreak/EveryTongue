Imports System.Text.Json

Namespace Models.Templates

    ''' <summary>
    ''' One named template in a preset library (STT / Translate / TTS group).
    ''' Bound to exactly one engine and carries only that engine's config block —
    ''' the block is stored as opaque JSON and (de)serialized solely by the
    ''' engine's own IEngineConfigDescriptor.
    ''' </summary>
    Public Class EngineTemplate
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        ''' <summary>Registry key of the one engine this template configures.</summary>
        Public Property EngineKey As String = ""
        ''' <summary>The engine's config block; only fields present here override engine defaults.</summary>
        Public Property Config As JsonElement?
    End Class

End Namespace
