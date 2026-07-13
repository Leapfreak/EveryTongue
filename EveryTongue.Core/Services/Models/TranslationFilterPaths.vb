Namespace Services.Models

    ''' <summary>
    ''' Resolved per-session filter file paths for translation post-processing
    ''' (named FilterSet support). Nothing / empty paths = the global files.
    ''' The local sidecar applies these in Python; for cloud backends the
    ''' orchestrator applies the glossary locally via GlossaryPostProcessor.
    ''' </summary>
    Public Class TranslationFilterPaths
        Public Property GlossaryPath As String = ""
        Public Property ProfanityPath As String = ""
    End Class

End Namespace
