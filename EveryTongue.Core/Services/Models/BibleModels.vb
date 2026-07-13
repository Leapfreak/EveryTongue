Namespace Services.Models
    ''' <summary>
    ''' A Bible translation (e.g. ESV, RVR1960, NVI).
    ''' </summary>
    Public Class BibleTranslation
        Public Property Id As String
        Public Property Name As String
        Public Property Language As String
        Public Property Abbreviation As String
        Public Property Copyright As String
    End Class

    ''' <summary>
    ''' A book in a Bible translation with its localized name.
    ''' </summary>
    Public Class BibleBook
        Public Property Number As Integer
        Public Property ShortName As String
        Public Property LongName As String
        Public Property Chapters As Integer
    End Class

    ''' <summary>
    ''' A chapter of a Bible book with all its verses.
    ''' </summary>
    Public Class BibleChapter
        Public Property TranslationId As String
        Public Property Book As String
        Public Property Chapter As Integer
        Public Property Verses As List(Of BibleVerse)
    End Class

    ''' <summary>
    ''' A single Bible verse.
    ''' </summary>
    Public Class BibleVerse
        Public Property Book As String
        Public Property Chapter As Integer
        Public Property Verse As Integer
        Public Property Text As String
    End Class

    ''' <summary>
    ''' Search result from Bible full-text search.
    ''' </summary>
    Public Class BibleSearchResult
        Public Property TranslationId As String
        Public Property Book As String
        Public Property Chapter As Integer
        Public Property Verse As Integer
        Public Property Text As String
        Public Property Rank As Double
    End Class

    ''' <summary>
    ''' Parsed Bible reference (e.g. "John 3:16" or "Romans 8:28-30").
    ''' </summary>
    Public Class BibleReference
        Public Property Book As String
        Public Property BookNumber As Integer  ' resolved book_number from DB (e.g. 500=John)
        Public Property Chapter As Integer
        Public Property VerseStart As Integer
        Public Property VerseEnd As Integer
        Public Property IsValid As Boolean
    End Class

    ''' <summary>
    ''' A Bible reference detected in transcription text.
    ''' </summary>
    Public Class DetectedReference
        Public Property Reference As BibleReference
        Public Property MatchedText As String
        Public Property StartIndex As Integer
        Public Property Length As Integer
    End Class
End Namespace
