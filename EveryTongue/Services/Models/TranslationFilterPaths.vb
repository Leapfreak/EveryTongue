Namespace Services.Models

    ''' <summary>
    ''' Resolved per-session filter file paths for translation post-processing
    ''' (named FilterSet support). Nothing / empty paths = the sidecar's global
    ''' files. Cloud translation backends ignore this — glossary/profanity are
    ''' a sidecar post-processing concern.
    ''' </summary>
    Public Class TranslationFilterPaths
        Public Property GlossaryPath As String = ""
        Public Property ProfanityPath As String = ""
    End Class

End Namespace
