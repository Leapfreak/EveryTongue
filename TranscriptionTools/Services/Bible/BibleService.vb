Imports System.Collections.Concurrent
Imports System.Data
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Bible
    ''' <summary>
    ''' Bible service — serves verses from per-translation SQLite databases.
    ''' Scans bibles/ directory on startup, supports FTS5 full-text search,
    ''' multilingual reference parsing, and automatic reference detection in text.
    ''' </summary>
    Public Class BibleService
        Implements IBibleService

        Private ReadOnly _biblesDir As String
        Private ReadOnly _logger As ILogger(Of BibleService)
        Private ReadOnly _translations As New ConcurrentDictionary(Of String, BibleTranslation)()

        ' Book name aliases for reference parsing (English defaults)
        Private Shared ReadOnly BookAliases As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"Genesis", "GEN"}, {"Gen", "GEN"}, {"Ge", "GEN"},
            {"Exodus", "EXO"}, {"Exod", "EXO"}, {"Ex", "EXO"},
            {"Leviticus", "LEV"}, {"Lev", "LEV"},
            {"Numbers", "NUM"}, {"Num", "NUM"},
            {"Deuteronomy", "DEU"}, {"Deut", "DEU"}, {"Dt", "DEU"},
            {"Joshua", "JOS"}, {"Josh", "JOS"},
            {"Judges", "JDG"}, {"Judg", "JDG"},
            {"Ruth", "RUT"},
            {"1 Samuel", "1SA"}, {"1Samuel", "1SA"}, {"1Sam", "1SA"}, {"1 Sam", "1SA"},
            {"2 Samuel", "2SA"}, {"2Samuel", "2SA"}, {"2Sam", "2SA"}, {"2 Sam", "2SA"},
            {"1 Kings", "1KI"}, {"1Kings", "1KI"}, {"1 Kgs", "1KI"},
            {"2 Kings", "2KI"}, {"2Kings", "2KI"}, {"2 Kgs", "2KI"},
            {"1 Chronicles", "1CH"}, {"1Chronicles", "1CH"}, {"1 Chr", "1CH"},
            {"2 Chronicles", "2CH"}, {"2Chronicles", "2CH"}, {"2 Chr", "2CH"},
            {"Ezra", "EZR"},
            {"Nehemiah", "NEH"}, {"Neh", "NEH"},
            {"Esther", "EST"}, {"Esth", "EST"},
            {"Job", "JOB"},
            {"Psalms", "PSA"}, {"Psalm", "PSA"}, {"Ps", "PSA"}, {"Psa", "PSA"},
            {"Proverbs", "PRO"}, {"Prov", "PRO"}, {"Pr", "PRO"},
            {"Ecclesiastes", "ECC"}, {"Eccl", "ECC"}, {"Ecc", "ECC"},
            {"Song of Solomon", "SNG"}, {"Song", "SNG"}, {"SoS", "SNG"},
            {"Isaiah", "ISA"}, {"Isa", "ISA"}, {"Is", "ISA"},
            {"Jeremiah", "JER"}, {"Jer", "JER"},
            {"Lamentations", "LAM"}, {"Lam", "LAM"},
            {"Ezekiel", "EZK"}, {"Ezek", "EZK"},
            {"Daniel", "DAN"}, {"Dan", "DAN"},
            {"Hosea", "HOS"}, {"Hos", "HOS"},
            {"Joel", "JOL"},
            {"Amos", "AMO"},
            {"Obadiah", "OBA"}, {"Obad", "OBA"},
            {"Jonah", "JON"},
            {"Micah", "MIC"}, {"Mic", "MIC"},
            {"Nahum", "NAH"}, {"Nah", "NAH"},
            {"Habakkuk", "HAB"}, {"Hab", "HAB"},
            {"Zephaniah", "ZEP"}, {"Zeph", "ZEP"},
            {"Haggai", "HAG"}, {"Hag", "HAG"},
            {"Zechariah", "ZEC"}, {"Zech", "ZEC"},
            {"Malachi", "MAL"}, {"Mal", "MAL"},
            {"Matthew", "MAT"}, {"Matt", "MAT"}, {"Mt", "MAT"},
            {"Mark", "MRK"}, {"Mk", "MRK"},
            {"Luke", "LUK"}, {"Lk", "LUK"},
            {"John", "JHN"}, {"Jn", "JHN"},
            {"Acts", "ACT"},
            {"Romans", "ROM"}, {"Rom", "ROM"},
            {"1 Corinthians", "1CO"}, {"1Corinthians", "1CO"}, {"1 Cor", "1CO"}, {"1Cor", "1CO"},
            {"2 Corinthians", "2CO"}, {"2Corinthians", "2CO"}, {"2 Cor", "2CO"}, {"2Cor", "2CO"},
            {"Galatians", "GAL"}, {"Gal", "GAL"},
            {"Ephesians", "EPH"}, {"Eph", "EPH"},
            {"Philippians", "PHP"}, {"Phil", "PHP"},
            {"Colossians", "COL"}, {"Col", "COL"},
            {"1 Thessalonians", "1TH"}, {"1Thessalonians", "1TH"}, {"1 Thess", "1TH"},
            {"2 Thessalonians", "2TH"}, {"2Thessalonians", "2TH"}, {"2 Thess", "2TH"},
            {"1 Timothy", "1TI"}, {"1Timothy", "1TI"}, {"1 Tim", "1TI"},
            {"2 Timothy", "2TI"}, {"2Timothy", "2TI"}, {"2 Tim", "2TI"},
            {"Titus", "TIT"},
            {"Philemon", "PHM"}, {"Phlm", "PHM"},
            {"Hebrews", "HEB"}, {"Heb", "HEB"},
            {"James", "JAS"}, {"Jas", "JAS"},
            {"1 Peter", "1PE"}, {"1Peter", "1PE"}, {"1 Pet", "1PE"},
            {"2 Peter", "2PE"}, {"2Peter", "2PE"}, {"2 Pet", "2PE"},
            {"1 John", "1JN"}, {"1John", "1JN"},
            {"2 John", "2JN"}, {"2John", "2JN"},
            {"3 John", "3JN"}, {"3John", "3JN"},
            {"Jude", "JUD"},
            {"Revelation", "REV"}, {"Rev", "REV"}, {"Apocalypse", "REV"}
        }

        ' Regex for detecting Bible references in text
        Private Shared ReadOnly RefPattern As New Regex(
            "(?<book>(?:\d\s*)?[A-Z][a-z]+(?:\s+[a-z]+)*)\s+(?<chapter>\d{1,3})\s*:\s*(?<verse>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3}))?",
            RegexOptions.Compiled)

        Public Sub New(logger As ILogger(Of BibleService))
            _logger = logger
            _biblesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bibles")
            ScanTranslations()
        End Sub

        Private Sub ScanTranslations()
            If Not Directory.Exists(_biblesDir) Then Return

            For Each dbFile In Directory.GetFiles(_biblesDir, "*.db", SearchOption.AllDirectories)
                Try
                    Dim id = Path.GetFileNameWithoutExtension(dbFile)
                    Dim lang = Path.GetFileName(Path.GetDirectoryName(dbFile))
                    _translations(id) = New BibleTranslation With {
                        .Id = id,
                        .Language = lang,
                        .Name = id,
                        .Abbreviation = id.ToUpper()
                    }
                Catch
                End Try
            Next

            If _translations.Count > 0 Then
                _logger.LogInformation("Bible: found {Count} translations", _translations.Count)
            End If
        End Sub

        Public Function GetTranslationsAsync(language As String, ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of BibleTranslation)) Implements IBibleService.GetTranslationsAsync
            Dim result = _translations.Values.
                Where(Function(t) String.IsNullOrEmpty(language) OrElse
                                  t.Language.Equals(language, StringComparison.OrdinalIgnoreCase)).
                ToList()
            Return Task.FromResult(DirectCast(result, IReadOnlyList(Of BibleTranslation)))
        End Function

        Public Function GetChapterAsync(translationId As String, book As String,
                                        chapter As Integer, ct As CancellationToken
        ) As Task(Of BibleChapter) Implements IBibleService.GetChapterAsync
            ' TODO: Query SQLite database for chapter verses
            Return Task.FromResult(New BibleChapter With {
                .TranslationId = translationId,
                .Book = book,
                .Chapter = chapter,
                .Verses = New List(Of BibleVerse)()
            })
        End Function

        Public Function GetVersesAsync(translationId As String, book As String,
                                       chapter As Integer, verseStart As Integer,
                                       Optional verseEnd As Integer = -1,
                                       Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleVerse)) Implements IBibleService.GetVersesAsync
            ' TODO: Query SQLite database for verse range
            Return Task.FromResult(DirectCast(New List(Of BibleVerse)(), IReadOnlyList(Of BibleVerse)))
        End Function

        Public Function SearchAsync(query As String, translationId As String,
                                    Optional maxResults As Integer = 50,
                                    Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleSearchResult)) Implements IBibleService.SearchAsync
            ' TODO: FTS5 search in SQLite
            Return Task.FromResult(DirectCast(New List(Of BibleSearchResult)(), IReadOnlyList(Of BibleSearchResult)))
        End Function

        Public Function ParseReferenceAsync(reference As String,
                                            Optional language As String = "en",
                                            Optional ct As CancellationToken = Nothing
        ) As Task(Of BibleReference) Implements IBibleService.ParseReferenceAsync
            Dim m = RefPattern.Match(reference)
            If Not m.Success Then
                Return Task.FromResult(New BibleReference With {.IsValid = False})
            End If

            Dim bookName = m.Groups("book").Value.Trim()
            Dim bookCode As String = Nothing
            If Not BookAliases.TryGetValue(bookName, bookCode) Then
                Return Task.FromResult(New BibleReference With {.IsValid = False})
            End If

            Dim chap = Integer.Parse(m.Groups("chapter").Value)
            Dim vStart = Integer.Parse(m.Groups("verse").Value)
            Dim vEnd = vStart
            If m.Groups("vend").Success Then
                vEnd = Integer.Parse(m.Groups("vend").Value)
            End If

            Return Task.FromResult(New BibleReference With {
                .Book = bookCode,
                .Chapter = chap,
                .VerseStart = vStart,
                .VerseEnd = vEnd,
                .IsValid = True
            })
        End Function

        Public Function DetectReferencesInText(text As String
        ) As IReadOnlyList(Of DetectedReference) Implements IBibleService.DetectReferencesInText
            Dim results As New List(Of DetectedReference)()
            If String.IsNullOrEmpty(text) Then Return results

            For Each m As Match In RefPattern.Matches(text)
                Dim bookName = m.Groups("book").Value.Trim()
                Dim bookCode As String = Nothing
                If Not BookAliases.TryGetValue(bookName, bookCode) Then Continue For

                Dim chap = Integer.Parse(m.Groups("chapter").Value)
                Dim vStart = Integer.Parse(m.Groups("verse").Value)
                Dim vEnd = vStart
                If m.Groups("vend").Success Then
                    vEnd = Integer.Parse(m.Groups("vend").Value)
                End If

                results.Add(New DetectedReference With {
                    .Reference = New BibleReference With {
                        .Book = bookCode,
                        .Chapter = chap,
                        .VerseStart = vStart,
                        .VerseEnd = vEnd,
                        .IsValid = True
                    },
                    .MatchedText = m.Value,
                    .StartIndex = m.Index,
                    .Length = m.Length
                })
            Next

            Return results
        End Function
    End Class
End Namespace
