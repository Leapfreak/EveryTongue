Namespace Services.Config

    ''' <summary>
    ''' Declares which config groups each workspace consumes — the plan's
    ''' workspace × capability matrix in code. UI and session wiring read this
    ''' instead of hardcoding per-workspace assumptions; a workspace that uses
    ''' nothing for a group simply shows nothing.
    ''' </summary>
    Friend Class WorkspaceCapabilities

        Public Class Entry
            Public Property WorkspaceKey As String
            Public Property UsesStt As Boolean
            ''' <summary>Cloud STT engines are offered (subject to the Online/Offline gate).</summary>
            Public Property UsesCloudStt As Boolean
            Public Property UsesTranslate As Boolean
            ''' <summary>Translation is optional in this workspace (e.g. Transcribe output language).</summary>
            Public Property TranslateOptional As Boolean
            Public Property UsesTts As Boolean
            Public Property UsesSpeaker As Boolean
            Public Property UsesDisplay As Boolean
            Public Property UsesFilters As Boolean
        End Class

        Private Shared ReadOnly _workspaces As New List(Of Entry) From {
            New Entry With {.WorkspaceKey = "conference", .UsesStt = True, .UsesCloudStt = True, .UsesTranslate = True, .UsesTts = True, .UsesSpeaker = True, .UsesDisplay = True, .UsesFilters = True},
            New Entry With {.WorkspaceKey = "conversation", .UsesStt = True, .UsesCloudStt = True, .UsesTranslate = True, .UsesTts = True, .UsesSpeaker = False, .UsesDisplay = True, .UsesFilters = True},
            New Entry With {.WorkspaceKey = "transcribe", .UsesStt = True, .UsesCloudStt = True, .UsesTranslate = True, .TranslateOptional = True, .UsesTts = False, .UsesSpeaker = False, .UsesDisplay = False, .UsesFilters = True},
            New Entry With {.WorkspaceKey = "translate", .UsesStt = False, .UsesTranslate = True, .UsesTts = True, .UsesSpeaker = False, .UsesDisplay = False, .UsesFilters = True},
            New Entry With {.WorkspaceKey = "bible", .UsesStt = False, .UsesTranslate = True, .UsesTts = True, .UsesSpeaker = False, .UsesDisplay = False, .UsesFilters = False}
        }

        Public Shared Function GetAll() As IReadOnlyList(Of Entry)
            Return _workspaces
        End Function

        Public Shared Function Find(workspaceKey As String) As Entry
            Return _workspaces.FirstOrDefault(
                Function(w) w.WorkspaceKey.Equals(If(workspaceKey, ""), StringComparison.OrdinalIgnoreCase))
        End Function

    End Class

End Namespace
