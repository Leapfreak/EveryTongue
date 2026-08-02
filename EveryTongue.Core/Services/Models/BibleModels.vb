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
        ''' <summary>Resolved from the room's reading context (bare "versículo N"
        ''' against the last-heard book/chapter) rather than a full spoken reference.</summary>
        Public Property FromContext As Boolean
    End Class

    ''' <summary>
    ''' Per-room reading context for bare-verse resolution ("versículo 18" =
    ''' the book/chapter the room last heard). Holds a small per-book memory so
    ''' the preacher's own disambiguation ("versículo 15 del salmo" right after
    ''' citing Matthew) can steer recency back to the expounded text.
    ''' Mutated only by DetectReferencesInText; callers keep one per room.
    ''' </summary>
    Public Class RefContext
        Public Class BookEntry
            Public Property BookCode As String
            Public Property Chapter As Integer
            Public Property LastSeenUtc As DateTime
        End Class
        ''' <summary>bookNumber → last chapter+timestamp heard for that book.</summary>
        Public ReadOnly Property Books As New Dictionary(Of Integer, BookEntry)
        ''' <summary>bookNumber of the most recently heard (or rescued) reference; 0 = none.</summary>
        Public Property LastBook As Integer
        Public Const MaxBooks As Integer = 8
    End Class

    ''' <summary>Options for reference detection over live caption text.</summary>
    Public Class RefDetectionOptions
        ''' <summary>Caption language (any code form) — scopes number-word
        ''' substitution so one language's number word can't fire inside
        ''' another language's text (es "once"=11 vs English "once").</summary>
        Public Property LangHint As String
        ''' <summary>Reading context to resolve bare verse references against; Nothing = off.</summary>
        Public Property Context As RefContext
        ''' <summary>Whether full detections may update the context (re-detection
        ''' passes over translated text must not).</summary>
        Public Property UpdateContext As Boolean = True
    End Class
End Namespace
