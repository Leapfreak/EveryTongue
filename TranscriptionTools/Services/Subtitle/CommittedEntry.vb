Namespace Services.Subtitle
    ''' <summary>
    ''' A finalised subtitle line with optional translations.
    ''' Stored in the history queue for replay on client reconnect.
    ''' </summary>
    Public Class CommittedEntry
        Public Property Id As Integer
        Public Property OriginalText As String = ""
        Public Property SourceLang As String = ""
        Public Property Translations As Dictionary(Of String, String)
        Public Property LangTags As Dictionary(Of String, String)
        Public Property Timestamp As DateTime

        Public Sub New(id As Integer,
                       text As String,
                       Optional lang As String = "",
                       Optional translations As Dictionary(Of String, String) = Nothing,
                       Optional langTags As Dictionary(Of String, String) = Nothing)
            Me.Id = id
            OriginalText = text
            SourceLang = If(lang, "")
            Me.Translations = If(translations, New Dictionary(Of String, String)())
            Me.LangTags = langTags
            Timestamp = DateTime.Now
        End Sub
    End Class
End Namespace
