Namespace Models.Templates

    ''' <summary>
    ''' One named filter set: glossary + profanity + hallucination lists.
    ''' Modelled as a collection per the config plan, though only the global set
    ''' exists for now; an empty path means "use the global default file".
    ''' Per-type precedence (glossary merges, profanity/hallucination override)
    ''' is deferred until multi-set selection exists.
    ''' </summary>
    Public Class FilterSet
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        Public Property GlossaryPath As String = ""
        Public Property ProfanityPath As String = ""
        Public Property HallucinationsPath As String = ""
    End Class

End Namespace
