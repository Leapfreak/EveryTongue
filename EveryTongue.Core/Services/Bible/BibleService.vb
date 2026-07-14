Imports System.Collections.Concurrent
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.Data.Sqlite
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Bible
    ''' <summary>
    ''' Bible service — serves verses from per-translation SQLite databases.
    ''' Scans Bibles/ directory on startup for .db/.sqlite/.sqlite3 files.
    ''' Schema: books(book_number, short_name, long_name), verses(book_number, chapter, verse, text).
    ''' Strips Strong's number tags (&lt;S&gt;...&lt;/S&gt;) from verse text on output.
    ''' </summary>
    Public Class BibleService
        Implements IBibleService

        Private ReadOnly _biblesDir As String
        Private ReadOnly _logger As ILogger(Of BibleService)
        Private ReadOnly _translations As New ConcurrentDictionary(Of String, BibleTranslationEntry)()

        ' Regex to strip markup from verse text:
        '   <S>num</S>  Strong's numbers — strip tag AND content (numbers are not readable text)
        '   All other tags (<J>, </J>, <pb/>, <t>, </t>, <n>, </n>, etc.) — strip tag only, keep content
        '   [bracketed] commentary notes — strip brackets AND content
        Private Shared ReadOnly StrongsPattern As New Regex("<S>\d+</S>", RegexOptions.Compiled)
        Private Shared ReadOnly TagPattern As New Regex("<[^>]+>", RegexOptions.Compiled)
        Private Shared ReadOnly BracketPattern As New Regex("\[.*?\]", RegexOptions.Compiled)

        ' Book name aliases for reference parsing (English)
        ' Maps display name/abbreviation -> short_name used in DB queries
        ' Maps display name/abbreviation -> short_name matching KJV+ schema
        ' These are used by ParseReference and DetectReferences; ResolveBookNumber
        ' also checks the DB's own bookMap (short_name + long_name) for direct matches
        Private Shared ReadOnly BookAliases As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"Genesis", "Gen"}, {"Gen", "Gen"}, {"Ge", "Gen"},
            {"Exodus", "Exo"}, {"Exod", "Exo"}, {"Ex", "Exo"},
            {"Leviticus", "Lev"}, {"Lev", "Lev"},
            {"Numbers", "Num"}, {"Num", "Num"},
            {"Deuteronomy", "Deu"}, {"Deut", "Deu"}, {"Dt", "Deu"},
            {"Joshua", "Josh"}, {"Josh", "Josh"}, {"Jos", "Josh"},
            {"Judges", "Judg"}, {"Judg", "Judg"}, {"Jdg", "Judg"},
            {"Ruth", "Ruth"}, {"Rut", "Ruth"},
            {"1 Samuel", "1Sam"}, {"1Sam", "1Sam"}, {"1 Sam", "1Sam"}, {"1Sa", "1Sam"},
            {"2 Samuel", "2Sam"}, {"2Sam", "2Sam"}, {"2 Sam", "2Sam"}, {"2Sa", "2Sam"},
            {"1 Kings", "1Kin"}, {"1Kings", "1Kin"}, {"1 Kgs", "1Kin"}, {"1Ki", "1Kin"},
            {"2 Kings", "2Kin"}, {"2Kings", "2Kin"}, {"2 Kgs", "2Kin"}, {"2Ki", "2Kin"},
            {"1 Chronicles", "1Chr"}, {"1Chr", "1Chr"}, {"1 Chr", "1Chr"}, {"1Ch", "1Chr"},
            {"2 Chronicles", "2Chr"}, {"2Chr", "2Chr"}, {"2 Chr", "2Chr"}, {"2Ch", "2Chr"},
            {"Ezra", "Ezr"}, {"Ezr", "Ezr"},
            {"Nehemiah", "Neh"}, {"Neh", "Neh"},
            {"Esther", "Esth"}, {"Esth", "Esth"}, {"Est", "Esth"},
            {"Job", "Job"},
            {"Psalms", "Ps"}, {"Psalm", "Ps"}, {"Ps", "Ps"}, {"Psa", "Ps"},
            {"Proverbs", "Prov"}, {"Prov", "Prov"}, {"Pr", "Prov"}, {"Pro", "Prov"},
            {"Ecclesiastes", "Eccl"}, {"Eccl", "Eccl"}, {"Ecc", "Eccl"},
            {"Song of Solomon", "Song"}, {"Song", "Song"}, {"SoS", "Song"}, {"Sol", "Song"},
            {"Isaiah", "Isa"}, {"Isa", "Isa"}, {"Is", "Isa"},
            {"Jeremiah", "Jer"}, {"Jer", "Jer"},
            {"Lamentations", "Lam"}, {"Lam", "Lam"},
            {"Ezekiel", "Ezek"}, {"Ezek", "Ezek"}, {"Eze", "Ezek"},
            {"Daniel", "Dan"}, {"Dan", "Dan"},
            {"Hosea", "Hos"}, {"Hos", "Hos"},
            {"Joel", "Joel"}, {"Joe", "Joel"},
            {"Amos", "Am"}, {"Am", "Am"}, {"Amo", "Am"},
            {"Obadiah", "Oba"}, {"Obad", "Oba"},
            {"Jonah", "Jona"}, {"Jona", "Jona"}, {"Jon", "Jona"},
            {"Micah", "Mic"}, {"Mic", "Mic"},
            {"Nahum", "Nah"}, {"Nah", "Nah"},
            {"Habakkuk", "Hab"}, {"Hab", "Hab"},
            {"Zephaniah", "Zeph"}, {"Zeph", "Zeph"}, {"Zep", "Zeph"},
            {"Haggai", "Hag"}, {"Hag", "Hag"},
            {"Zechariah", "Zech"}, {"Zech", "Zech"}, {"Zec", "Zech"},
            {"Malachi", "Mal"}, {"Mal", "Mal"},
            {"Matthew", "Mat"}, {"Matt", "Mat"}, {"Mt", "Mat"},
            {"Mark", "Mar"}, {"Mk", "Mar"},
            {"Luke", "Luk"}, {"Lk", "Luk"},
            {"John", "John"}, {"Jn", "John"}, {"Joh", "John"},
            {"Acts", "Acts"}, {"Act", "Acts"},
            {"Romans", "Rom"}, {"Rom", "Rom"},
            {"1 Corinthians", "1Cor"}, {"1Cor", "1Cor"}, {"1 Cor", "1Cor"}, {"1Co", "1Cor"},
            {"2 Corinthians", "2Cor"}, {"2Cor", "2Cor"}, {"2 Cor", "2Cor"}, {"2Co", "2Cor"},
            {"Galatians", "Gal"}, {"Gal", "Gal"},
            {"Ephesians", "Eph"}, {"Eph", "Eph"},
            {"Philippians", "Phil"}, {"Phil", "Phil"}, {"Phi", "Phil"},
            {"Colossians", "Col"}, {"Col", "Col"},
            {"1 Thessalonians", "1Ths"}, {"1Thess", "1Ths"}, {"1 Thess", "1Ths"}, {"1Th", "1Ths"},
            {"2 Thessalonians", "2Ths"}, {"2Thess", "2Ths"}, {"2 Thess", "2Ths"}, {"2Th", "2Ths"},
            {"1 Timothy", "1Tim"}, {"1Tim", "1Tim"}, {"1 Tim", "1Tim"}, {"1Ti", "1Tim"},
            {"2 Timothy", "2Tim"}, {"2Tim", "2Tim"}, {"2 Tim", "2Tim"}, {"2Ti", "2Tim"},
            {"Titus", "Tit"}, {"Tit", "Tit"},
            {"Philemon", "Phlm"}, {"Phlm", "Phlm"}, {"Phm", "Phlm"},
            {"Hebrews", "Heb"}, {"Heb", "Heb"},
            {"James", "Jam"}, {"Jas", "Jam"},
            {"1 Peter", "1Pet"}, {"1Pet", "1Pet"}, {"1 Pet", "1Pet"}, {"1Pe", "1Pet"},
            {"2 Peter", "2Pet"}, {"2Pet", "2Pet"}, {"2 Pet", "2Pet"}, {"2Pe", "2Pet"},
            {"1 John", "1Jn"}, {"1John", "1Jn"}, {"1Jn", "1Jn"}, {"1Jo", "1Jn"},
            {"2 John", "2Jn"}, {"2John", "2Jn"}, {"2Jn", "2Jn"}, {"2Jo", "2Jn"},
            {"3 John", "3Jn"}, {"3John", "3Jn"}, {"3Jn", "3Jn"}, {"3Jo", "3Jn"},
            {"Jude", "Jud"}, {"Jud", "Jud"},
            {"Revelation", "Rev"}, {"Rev", "Rev"}, {"Apocalypse", "Rev"}
        }

        ' Standard USFM book_number values for alias targets
        ' Used as final fallback: maps KJV-style alias target → standard book_number used in USFM Bible DBs
        ' These numbers match the numbering scheme used by the Bible databases (10=Gen, 20=Exo, ..., 730=Rev)
        Private Shared ReadOnly StandardBookNumbers As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
            {"Gen", 10}, {"Exo", 20}, {"Lev", 30}, {"Num", 40}, {"Deu", 50},
            {"Josh", 60}, {"Judg", 70}, {"Ruth", 80}, {"1Sam", 90}, {"2Sam", 100},
            {"1Kin", 110}, {"2Kin", 120}, {"1Chr", 130}, {"2Chr", 140}, {"Ezr", 150},
            {"Neh", 160}, {"Esth", 190}, {"Job", 220}, {"Ps", 230}, {"Prov", 240},
            {"Eccl", 250}, {"Song", 260}, {"Isa", 290}, {"Jer", 300}, {"Lam", 310},
            {"Ezek", 330}, {"Dan", 340}, {"Hos", 350}, {"Joel", 360}, {"Am", 370},
            {"Oba", 380}, {"Jona", 390}, {"Mic", 400}, {"Nah", 410}, {"Hab", 420},
            {"Zeph", 430}, {"Hag", 440}, {"Zech", 450}, {"Mal", 460},
            {"Mat", 470}, {"Mar", 480}, {"Luk", 490}, {"John", 500}, {"Acts", 510},
            {"Rom", 520}, {"1Cor", 530}, {"2Cor", 540}, {"Gal", 550}, {"Eph", 560},
            {"Phil", 570}, {"Col", 580}, {"1Ths", 590}, {"2Ths", 600},
            {"1Tim", 610}, {"2Tim", 620}, {"Tit", 630}, {"Phlm", 640}, {"Heb", 650},
            {"Jam", 660}, {"1Pet", 670}, {"2Pet", 680}, {"1Jn", 690}, {"2Jn", 700},
            {"3Jn", 710}, {"Jud", 720}, {"Rev", 730}
        }

        ' Regex for detecting Bible references in text (supports accented characters for non-English)
        ' Supports: "John 3:16", "John 3:16-18", "John 3" (chapter only, verse optional)
        Private Shared ReadOnly RefPattern As New Regex(
            "(?<book>(?:\d\s*)?[\p{Lu}][\p{Ll}]+(?:\s+[\p{Ll}]+)*)\s+(?<chapter>\d{1,3})(?:\s*:\s*(?<verse>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3}))?)?",
            RegexOptions.Compiled)

        ' Internal class to track DB path alongside translation info
        Private Class BibleTranslationEntry
            Public Property Info As BibleTranslation
            Public Property DbPath As String
            Public Property BookMap As Dictionary(Of String, Integer) ' short_name/long_name -> book_number
        End Class

        Public Sub New(logger As ILogger(Of BibleService), options As IOptions(Of ServerOptions))
            _logger = logger

            ' Use configured path if provided, otherwise search near executable
            Dim configured = options?.Value?.BiblesDirectory
            If Not String.IsNullOrEmpty(configured) AndAlso Directory.Exists(configured) Then
                _biblesDir = configured
            Else
                _biblesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bibles")
                If Not Directory.Exists(_biblesDir) Then
                    ' Walk up directories looking for Bibles folder (handles bin/Debug, bin/Publish, etc.)
                    Dim searchDir = AppDomain.CurrentDomain.BaseDirectory
                    For i = 0 To 5
                        searchDir = Path.GetDirectoryName(searchDir)
                        If searchDir Is Nothing Then Exit For
                        Dim candidate = Path.Combine(searchDir, "Bibles")
                        If Directory.Exists(candidate) Then
                            _biblesDir = candidate
                            Exit For
                        End If
                    Next
                End If
            End If

            _logger.LogInformation("Bible: scanning {Dir} (exists={Exists})",
                _biblesDir, Directory.Exists(_biblesDir))
            ScanTranslations()
        End Sub

        Public Sub RescanTranslations() Implements IBibleService.RescanTranslations
            _translations.Clear()
            ScanTranslations()
        End Sub

        Private Sub ScanTranslations()
            _logger.LogInformation("Bible: scanning directory: {Dir}", _biblesDir)
            If Not Directory.Exists(_biblesDir) Then
                _logger.LogWarning("Bible: directory NOT FOUND: {Dir}", _biblesDir)
                Return
            End If

            ' Scan in preference order: .sqlite3 first, then .sqlite, then .db
            ' Skip files whose ID (filename without extension) is already loaded.
            ' MatchCasing.CaseInsensitive: downloaded Bibles arrive with mixed-case
            ' extensions (e.g. BCI.SQLite3) — Windows matched them anyway, but the
            ' Linux container silently skipped them (found by container testing).
            Dim scanOpts As New EnumerationOptions With {
                .RecurseSubdirectories = True,
                .MatchCasing = MatchCasing.CaseInsensitive
            }
            Dim extensions = {"*.sqlite3", "*.sqlite", "*.db"}
            For Each ext In extensions
                For Each dbFile In Directory.GetFiles(_biblesDir, ext, scanOpts)
                    Dim id = Path.GetFileNameWithoutExtension(dbFile)
                    If _translations.ContainsKey(id) Then Continue For
                    Try
                        LoadTranslation(dbFile)
                    Catch ex As Exception
                        _logger.LogWarning("Bible: skipped {File} — {Error}", dbFile, ex.Message)
                    End Try
                Next
            Next

            _logger.LogInformation("Bible: found {Count} translation(s) in {Dir}", _translations.Count, _biblesDir)
        End Sub

        Private Sub LoadTranslation(dbFile As String)
            Dim id = Path.GetFileNameWithoutExtension(dbFile)
            Dim folderName = Path.GetFileName(Path.GetDirectoryName(dbFile))

            Dim connStr = New SqliteConnectionStringBuilder() With {
                .DataSource = dbFile,
                .Mode = SqliteOpenMode.ReadOnly
            }.ToString()

            Dim name = id
            Dim lang = folderName  ' fallback: folder name
            Dim copyright As String = Nothing
            Dim bookMap As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

            Using conn As New SqliteConnection(connStr)
                conn.Open()

                ' Read metadata from info table
                Try
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT name, value FROM info WHERE name IN ('description', 'language', 'copyright', 'detailed_info')"
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                Dim key = reader.GetString(0)
                                Dim val = reader.GetString(1)
                                Select Case key
                                    Case "description"
                                        If Not String.IsNullOrEmpty(val) Then name = val
                                    Case "language"
                                        ' Use ISO code from DB, normalized to 3-letter (e.g. "ca" -> "cat")
                                        If Not String.IsNullOrEmpty(val) Then lang = NormalizeLangCode(val)
                                    Case "copyright", "detailed_info"
                                        If copyright Is Nothing AndAlso Not String.IsNullOrEmpty(val) Then copyright = val
                                End Select
                            End While
                        End Using
                    End Using
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR, $"BibleService.LoadTranslation: failed reading info for '{dbFile}' - {ex.Message}")
                End Try

                ' Build book_number map from books table (both short_name and long_name)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT book_number, short_name, long_name FROM books"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim bookNum = reader.GetInt32(0)
                            Dim shortName = reader.GetString(1)
                            Dim longName = reader.GetString(2)
                            bookMap(shortName) = bookNum
                            bookMap(longName) = bookNum
                        End While
                    End Using
                End Using
            End Using

            _translations(id) = New BibleTranslationEntry With {
                .Info = New BibleTranslation With {
                    .Id = id,
                    .Language = lang,
                    .Name = name,
                    .Abbreviation = id.ToUpper(),
                    .Copyright = copyright
                },
                .DbPath = dbFile,
                .BookMap = bookMap
            }

            _logger.LogInformation("Bible: loaded {Id} ({Lang}) — {Name}", id, lang, name)

            ' Integrity check: report issues with this translation
            VerifyTranslation(id, dbFile, bookMap)
        End Sub

        Private Sub VerifyTranslation(id As String, dbFile As String, bookMap As Dictionary(Of String, Integer))
            Try
                Dim bookNumbers = bookMap.Values.Distinct().OrderBy(Function(n) n).ToList()
                Dim bookCount = bookNumbers.Count
                Dim issues As New List(Of String)()

                ' Check book count
                If bookCount < 66 Then
                    issues.Add($"{bookCount} books (expected 66)")
                End If

                ' Check for empty chapters
                Dim connStr = New SqliteConnectionStringBuilder() With {
                    .DataSource = dbFile,
                    .Mode = SqliteOpenMode.ReadOnly
                }.ToString()

                Using conn As New SqliteConnection(connStr)
                    conn.Open()
                    Using cmd = conn.CreateCommand()
                        ' Find chapters that exist in the books table but have no verses
                        cmd.CommandText =
                            "SELECT b.short_name, b.book_number, " &
                            "  (SELECT MAX(v.chapter) FROM verses v WHERE v.book_number = b.book_number) AS max_ch, " &
                            "  (SELECT COUNT(*) FROM verses v WHERE v.book_number = b.book_number) AS verse_count " &
                            "FROM books b ORDER BY b.book_number"
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                Dim shortName = reader.GetString(0)
                                Dim verseCount = If(reader.IsDBNull(3), 0, reader.GetInt32(3))
                                Dim maxCh = If(reader.IsDBNull(2), 0, reader.GetInt32(2))
                                If verseCount = 0 Then
                                    issues.Add($"{shortName}: no verses")
                                ElseIf maxCh > 0 Then
                                    ' Check for gaps in chapters
                                    Using cmd2 = conn.CreateCommand()
                                        cmd2.CommandText = "SELECT DISTINCT chapter FROM verses WHERE book_number = @bn ORDER BY chapter"
                                        cmd2.Parameters.AddWithValue("@bn", reader.GetInt32(1))
                                        Dim chapters As New List(Of Integer)()
                                        Using r2 = cmd2.ExecuteReader()
                                            While r2.Read()
                                                chapters.Add(r2.GetInt32(0))
                                            End While
                                        End Using
                                        Dim expectedChapters = Enumerable.Range(1, maxCh).ToList()
                                        Dim missing = expectedChapters.Except(chapters).ToList()
                                        If missing.Count > 0 Then
                                            issues.Add($"{shortName}: missing chapters {String.Join(",", missing.Take(10))}")
                                        End If
                                    End Using
                                End If
                            End While
                        End Using
                    End Using
                End Using

                If issues.Count > 0 Then
                    _logger.LogWarning("Bible: {Id} integrity issues: {Issues}", id, String.Join("; ", issues))
                End If
            Catch ex As Exception
                _logger.LogWarning("Bible: {Id} verification failed: {Err}", id, ex.Message)
            End Try
        End Sub

        Private Function GetConnection(translationId As String) As SqliteConnection
            Dim entry As BibleTranslationEntry = Nothing
            If Not _translations.TryGetValue(translationId, entry) Then Return Nothing
            Dim conn As New SqliteConnection(New SqliteConnectionStringBuilder() With {
                .DataSource = entry.DbPath,
                .Mode = SqliteOpenMode.ReadOnly
            }.ToString())
            conn.Open()
            Return conn
        End Function

        Private Function ResolveBookNumber(translationId As String, book As String) As Integer
            Dim entry As BibleTranslationEntry = Nothing
            If Not _translations.TryGetValue(translationId, entry) Then Return -1

            ' Try direct match on short_name or long_name from DB
            Dim bookNum As Integer
            If entry.BookMap.TryGetValue(book, bookNum) Then Return bookNum

            ' Try alias lookup -> KJV short_name -> direct DB match
            Dim shortName As String = Nothing
            If BookAliases.TryGetValue(book, shortName) Then
                If entry.BookMap.TryGetValue(shortName, bookNum) Then Return bookNum
            End If

            ' Final fallback: use standard USFM book_number (10=Gen, 500=John, etc.)
            Dim stdNum As Integer
            Dim target = If(shortName, book)
            If StandardBookNumbers.TryGetValue(target, stdNum) Then
                If entry.BookMap.ContainsValue(stdNum) Then Return stdNum
            End If

            Return -1
        End Function

        Private Shared Function StripTags(text As String) As String
            If text Is Nothing Then Return ""
            ' First remove Strong's numbers with their content, then strip remaining tags and bracketed commentary
            Dim result = StrongsPattern.Replace(text, "")
            result = BracketPattern.Replace(result, "")
            Return TagPattern.Replace(result, "").Trim()
        End Function

        Public Function GetTranslationsAsync(language As String, ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of BibleTranslation)) Implements IBibleService.GetTranslationsAsync
            Dim queryLang = NormalizeLangCode(language)
            Dim result = _translations.Values.
                Where(Function(e) String.IsNullOrEmpty(language) OrElse
                                  NormalizeLangCode(e.Info.Language).Equals(queryLang, StringComparison.OrdinalIgnoreCase)).
                Select(Function(e) e.Info).ToList()
            Return Task.FromResult(DirectCast(result, IReadOnlyList(Of BibleTranslation)))
        End Function

        Public Function GetBooksAsync(translationId As String, ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of BibleBook)) Implements IBibleService.GetBooksAsync
            Dim books As New List(Of BibleBook)()
            Using conn = GetConnection(translationId)
                If conn Is Nothing Then Return Task.FromResult(DirectCast(books, IReadOnlyList(Of BibleBook)))
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT b.book_number, b.short_name, b.long_name, " &
                        "(SELECT MAX(v.chapter) FROM verses v WHERE v.book_number = b.book_number) AS chapters " &
                        "FROM books b ORDER BY b.book_number"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            books.Add(New BibleBook With {
                                .Number = reader.GetInt32(0),
                                .ShortName = reader.GetString(1),
                                .LongName = reader.GetString(2),
                                .Chapters = If(reader.IsDBNull(3), 1, reader.GetInt32(3))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return Task.FromResult(DirectCast(books, IReadOnlyList(Of BibleBook)))
        End Function

        Public Function GetChapterAsync(translationId As String, book As String,
                                        chapter As Integer, ct As CancellationToken
        ) As Task(Of BibleChapter) Implements IBibleService.GetChapterAsync
            Dim verses As New List(Of BibleVerse)()
            Dim bookNum = ResolveBookNumber(translationId, book)
            If bookNum < 0 Then
                Return Task.FromResult(New BibleChapter With {
                    .TranslationId = translationId, .Book = book,
                    .Chapter = chapter, .Verses = verses})
            End If

            Using conn = GetConnection(translationId)
                If conn Is Nothing Then
                    Return Task.FromResult(New BibleChapter With {
                        .TranslationId = translationId, .Book = book,
                        .Chapter = chapter, .Verses = verses})
                End If
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT verse, text FROM verses WHERE book_number = @bn AND chapter = @ch ORDER BY verse"
                    cmd.Parameters.AddWithValue("@bn", bookNum)
                    cmd.Parameters.AddWithValue("@ch", chapter)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            verses.Add(New BibleVerse With {
                                .Book = book,
                                .Chapter = chapter,
                                .Verse = reader.GetInt32(0),
                                .Text = StripTags(reader.GetString(1))
                            })
                        End While
                    End Using
                End Using
            End Using

            Return Task.FromResult(New BibleChapter With {
                .TranslationId = translationId, .Book = book,
                .Chapter = chapter, .Verses = verses})
        End Function

        Public Function GetVersesAsync(translationId As String, book As String,
                                       chapter As Integer, verseStart As Integer,
                                       Optional verseEnd As Integer = -1,
                                       Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleVerse)) Implements IBibleService.GetVersesAsync
            Dim verses As New List(Of BibleVerse)()
            Dim bookNum = ResolveBookNumber(translationId, book)
            If bookNum < 0 Then Return Task.FromResult(DirectCast(verses, IReadOnlyList(Of BibleVerse)))

            Using conn = GetConnection(translationId)
                If conn Is Nothing Then Return Task.FromResult(DirectCast(verses, IReadOnlyList(Of BibleVerse)))
                Using cmd = conn.CreateCommand()
                    If verseEnd > 0 Then
                        cmd.CommandText = "SELECT verse, text FROM verses WHERE book_number = @bn AND chapter = @ch AND verse BETWEEN @v1 AND @v2 ORDER BY verse"
                        cmd.Parameters.AddWithValue("@v2", verseEnd)
                    Else
                        cmd.CommandText = "SELECT verse, text FROM verses WHERE book_number = @bn AND chapter = @ch AND verse = @v1"
                    End If
                    cmd.Parameters.AddWithValue("@bn", bookNum)
                    cmd.Parameters.AddWithValue("@ch", chapter)
                    cmd.Parameters.AddWithValue("@v1", verseStart)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            verses.Add(New BibleVerse With {
                                .Book = book,
                                .Chapter = chapter,
                                .Verse = reader.GetInt32(0),
                                .Text = StripTags(reader.GetString(1))
                            })
                        End While
                    End Using
                End Using
            End Using

            Return Task.FromResult(DirectCast(verses, IReadOnlyList(Of BibleVerse)))
        End Function

        Public Function SearchAsync(query As String, translationId As String,
                                    Optional maxResults As Integer = 50,
                                    Optional ct As CancellationToken = Nothing
        ) As Task(Of IReadOnlyList(Of BibleSearchResult)) Implements IBibleService.SearchAsync
            Dim searchResults As New List(Of BibleSearchResult)()

            Using conn = GetConnection(translationId)
                If conn Is Nothing Then
                    Return Task.FromResult(DirectCast(searchResults, IReadOnlyList(Of BibleSearchResult)))
                End If

                ' Use LIKE search (FTS5 may not be available in all Bible DBs)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT v.book_number, v.chapter, v.verse, v.text, b.short_name " &
                        "FROM verses v JOIN books b ON v.book_number = b.book_number " &
                        "WHERE v.text LIKE @q ORDER BY v.book_number, v.chapter, v.verse LIMIT @max"
                    cmd.Parameters.AddWithValue("@q", $"%{query}%")
                    cmd.Parameters.AddWithValue("@max", maxResults)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            searchResults.Add(New BibleSearchResult With {
                                .TranslationId = translationId,
                                .Book = reader.GetString(4),
                                .Chapter = reader.GetInt32(1),
                                .Verse = reader.GetInt32(2),
                                .Text = StripTags(reader.GetString(3)),
                                .Rank = 1.0
                            })
                        End While
                    End Using
                End Using
            End Using

            Return Task.FromResult(DirectCast(searchResults, IReadOnlyList(Of BibleSearchResult)))
        End Function

        Public Function ParseReferenceAsync(reference As String,
                                            Optional language As String = "en",
                                            Optional translationId As String = Nothing,
                                            Optional ct As CancellationToken = Nothing
        ) As Task(Of BibleReference) Implements IBibleService.ParseReferenceAsync
            Dim m = RefPattern.Match(reference)
            If Not m.Success Then
                Return Task.FromResult(New BibleReference With {.IsValid = False})
            End If

            Dim bookName = m.Groups("book").Value.Trim()
            Dim bookCode As String = Nothing

            ' 1. Try translation's own BookMap (short_name + long_name from DB)
            If Not String.IsNullOrEmpty(translationId) Then
                Dim entry As BibleTranslationEntry = Nothing
                If _translations.TryGetValue(translationId, entry) Then
                    Dim bookNum As Integer
                    If entry.BookMap.TryGetValue(bookName, bookNum) Then
                        bookCode = entry.BookMap.
                            Where(Function(kv) kv.Value = bookNum).
                            OrderBy(Function(kv) kv.Key.Length).
                            Select(Function(kv) kv.Key).FirstOrDefault()
                    End If
                End If
            End If

            ' 2. Fall back to English aliases
            If bookCode Is Nothing Then
                BookAliases.TryGetValue(bookName, bookCode)
            End If

            If bookCode Is Nothing Then
                Return Task.FromResult(New BibleReference With {.IsValid = False})
            End If

            Dim chap = Integer.Parse(m.Groups("chapter").Value)
            Dim vStart = 0
            Dim vEnd = 0
            If m.Groups("verse").Success Then
                vStart = Integer.Parse(m.Groups("verse").Value)
                vEnd = vStart
                If m.Groups("vend").Success Then
                    vEnd = Integer.Parse(m.Groups("vend").Value)
                End If
            End If

            ' Resolve the book_number for the caller
            Dim resolvedBookNum = 0
            If Not String.IsNullOrEmpty(translationId) Then
                resolvedBookNum = ResolveBookNumber(translationId, bookCode)
            End If
            If resolvedBookNum <= 0 Then
                StandardBookNumbers.TryGetValue(bookCode, resolvedBookNum)
            End If

            Return Task.FromResult(New BibleReference With {
                .Book = bookCode,
                .BookNumber = resolvedBookNum,
                .Chapter = chap,
                .VerseStart = vStart,
                .VerseEnd = vEnd,
                .IsValid = True
            })
        End Function

        Public Function DetectReferencesInText(text As String
        ) As IReadOnlyList(Of DetectedReference) Implements IBibleService.DetectReferencesInText
            Dim detectedRefs As New List(Of DetectedReference)()
            If String.IsNullOrEmpty(text) Then Return detectedRefs

            For Each m As Match In RefPattern.Matches(text)
                Dim bookName = m.Groups("book").Value.Trim()
                Dim bookCode As String = Nothing
                If Not BookAliases.TryGetValue(bookName, bookCode) Then Continue For

                Dim chap = Integer.Parse(m.Groups("chapter").Value)
                Dim vStart = 0
                Dim vEnd = 0
                If m.Groups("verse").Success Then
                    vStart = Integer.Parse(m.Groups("verse").Value)
                    vEnd = vStart
                    If m.Groups("vend").Success Then
                        vEnd = Integer.Parse(m.Groups("vend").Value)
                    End If
                End If

                detectedRefs.Add(New DetectedReference With {
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

            Return detectedRefs
        End Function

        ''' <summary>
        ''' Normalize a language code to 3-letter ISO 639-3 for consistent matching.
        ''' Handles both 2-letter (en, es) and 3-letter (eng, spa) inputs.
        ''' </summary>
        Private Shared Function NormalizeLangCode(code As String) As String
            Return Services.Infrastructure.LanguageCodeService.Instance.NormalizeToIso3(code)
        End Function
    End Class
End Namespace
