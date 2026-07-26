Namespace Models

    ''' <summary>
    ''' One session-lifetime STT vocab entry (the engine's "service" layer):
    ''' a person/place name from the service — speaker names, sermon-notes
    ''' nouns. Pushed to STT engines as additional_vocab; survives scripture
    ''' book changes AND language changes (names are language-independent).
    ''' Managed via Tools → Service Names (typed or imported from a notes file).
    ''' </summary>
    Public Class ServiceNameEntry
        Public Property Content As String = ""
        ''' <summary>Optional pronunciation alternates (Speechmatics sounds_like) — e.g. "Eareckson" → "Erikson".</summary>
        Public Property SoundsLike As New List(Of String)
    End Class

End Namespace
