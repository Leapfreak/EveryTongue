Imports System.Threading
Imports TranscriptionTools.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' Bible service — serves verses from SQLite databases,
    ''' supports full-text search (FTS5), reference parsing,
    ''' and automatic reference detection in transcription text.
    ''' </summary>
    Public Interface IBibleService
        Function GetTranslationsAsync(language As String,
                                      ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of BibleTranslation))

        Function GetBooksAsync(translationId As String,
                               ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of BibleBook))

        Function GetChapterAsync(translationId As String,
                                 book As String,
                                 chapter As Integer,
                                 ct As CancellationToken
        ) As Task(Of BibleChapter)

        Function GetVersesAsync(translationId As String,
                                book As String,
                                chapter As Integer,
                                verseStart As Integer,
                                Optional verseEnd As Integer = -1,
                                Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleVerse))

        Function SearchAsync(query As String,
                             translationId As String,
                             Optional maxResults As Integer = 50,
                             Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleSearchResult))

        Function ParseReferenceAsync(reference As String,
                                     Optional language As String = "en",
                                     Optional translationId As String = Nothing,
                                     Optional ct As CancellationToken = Nothing
        ) As Task(Of BibleReference)

        Function DetectReferencesInText(text As String
        ) As IReadOnlyList(Of DetectedReference)
    End Interface
End Namespace
