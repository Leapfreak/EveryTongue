Imports System.Collections.Concurrent
Imports System.IO
Imports System.Text.Json
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

        ' Book NAMES live in data, never here: the derived BookAliasIndex
        ' (installed Bibles' own books tables) plus the locale channel
        ' (Bible_BookNames / Bible_SpokenBookNames). This table holds only the
        ' app's WIRE CODES — identifiers, not language text: RefDto.book on the
        ' wire, log lines ("1Tim 6:10"), StandardNumberForCode.
        ' The numbers are the universal USFM book_number scheme every Bible DB
        ' uses in every language (10=Gen, 230=Ps, ..., 730=Rev).
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
        ' Supports: "John 3:16", "John 3:16-18", "John 3" (chapter only), and the
        ' spoken-verse tail "Salmo 22, versículo 7" (versword validated in code).
        ' Book group allows capitalized continuation words so ordinal spans
        ' ("Primera de Joan", "Primer Reis") arrive whole; ResolveBookAlias
        ' drops unmatchable leading tokens, so a preceding capitalized word
        ' can't swallow a real reference.
        ' Between book and chapter: an optional comma, up to two short lowercase
        ' connectors, then ONE filler word ("Mateo, en el capítulo 27" — the
        ' spoken announcement form). The filler is only ACCEPTED when it is a
        ' known chapter word (see SpokenChapterWords); any other filler rejects
        ' the match, so the loosening adds no false positives ("Mateu, en 4 dies").
        ' Chapword/versword may arrive capitalized (STT capitalizes after the
        ' periods it inserts); validation sets are folded, so case is free.
        '
        ' DELIBERATELY NOT covered (decided 2026-09-18): lowercase-start books
        ' ("el salmo 22"), clock-garbled verse words ("7:35" for "verset 35"),
        ' and trailing-filler unwinding for comma-less announcements ("Mateo en
        ' el capítulo 27"). Each was a pass or rescue here once; the Bible
        ' button now reopens at the room's remembered reading position, so a
        ' missed exotic announcement costs one tap instead of a lost verse.
        ' Only the well-formed announcement (with the filler re-anchor for
        ' "La primera, Timoteu 6:10"-shaped prefixes) and the bare verse word
        ' (context pass) remain.
        Private Shared ReadOnly RefPattern As New Regex(
            "(?<book>(?:\d\s*)?[\p{Lu}][\p{Ll}]+(?:\s+(?:[\p{Ll}]{1,3}\s+)?[\p{Lu}\p{Ll}][\p{Ll}]+)*)(?:\s*,)?\s+(?:(?:[\p{Ll}]{1,3}\s+){0,2}(?<chapword>[\p{Lu}\p{Ll}][\p{Ll}'’]+)\s+)?(?<chapter>\d{1,3})(?:\s*:\s*(?<verse>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3})|\s+(?<rangeword>[\p{Ll}]{1,7})\s+(?<vend>\d{1,3})(?!\d))?|(?:\s*,)?\s+(?<versword>[\p{Lu}\p{Ll}][\p{Ll}'’]+)\s+(?<verse>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3})|\s+(?<rangeword>[\p{Ll}]{1,7})\s+(?<vend>\d{1,3})(?!\d))?)?",
            RegexOptions.Compiled)

        ''' <summary>
        ''' Spoken chapter words ("capítol", "chapter", "capítulo"), aggregated
        ''' across ALL locale files (app + user overlay) — the sanctioned
        ''' per-language channel; a new language pack brings its own words.
        ''' A chapter word between book and number is STRONG evidence ("Mateu
        ''' capítol 4" cannot be the verb reading of "mateu"). Folded entries.
        ''' </summary>
        Private Shared ReadOnly SpokenChapterWords As New Lazy(Of HashSet(Of String))(
            Function() LoadLocaleWordSet("Bible_SpokenChapterWords"))

        ''' <summary>Spoken verse words ("versículo", "verse", "verset") — same channel, folded.</summary>
        Private Shared ReadOnly SpokenVerseWords As New Lazy(Of HashSet(Of String))(
            Function() LoadLocaleWordSet("Bible_SpokenVerseWords"))

        ''' <summary>Spoken range connectors ("38 al 40", "38 to 40") — same
        ''' channel, folded. An unknown connector between two numbers keeps the
        ''' single verse and never rejects the reference.</summary>
        Private Shared ReadOnly SpokenRangeWords As New Lazy(Of HashSet(Of String))(
            Function() LoadLocaleWordSet("Bible_SpokenRangeWords"))

        ''' <summary>Locale dirs that feed the spoken-word sets: app locales + user overlay.</summary>
        Private Shared Function LocaleDirs() As List(Of String)
            Dim dirs As New List(Of String) From {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales")}
            Try
                dirs.Add(Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory, "locales"))
            Catch
                ' Config dir unavailable — app locales alone are fine.
            End Try
            Return dirs
        End Function

        Private Shared Function LoadLocaleWordSet(key As String) As HashSet(Of String)
            Dim words As New HashSet(Of String)(StringComparer.Ordinal)
            For Each localeDir In LocaleDirs()
                Try
                    If Not Directory.Exists(localeDir) Then Continue For
                    For Each f In Directory.GetFiles(localeDir, "*.json")
                        Try
                            Using doc = JsonDocument.Parse(File.ReadAllText(f))
                                Dim el As JsonElement = Nothing
                                If doc.RootElement.TryGetProperty(key, el) Then
                                    For Each w In If(el.GetString(), "").Split(","c, "|"c)
                                        If Not String.IsNullOrWhiteSpace(w) Then words.Add(BookAliasIndex.Fold(w.Trim()))
                                    Next
                                End If
                            End Using
                        Catch
                            ' Malformed locale file — skip; others still contribute.
                        End Try
                    Next
                Catch
                    ' Locale dir unreadable — detection degrades to the no-chapter-word forms.
                End Try
            Next
            Return words
        End Function

        ''' <summary>
        ''' Spoken number phrases per language ("Bible_NumberWords": the FULL
        ''' enumeration 1-200, every value spelled out — the file IS the
        ''' coverage; code does no arithmetic). Keyed by ISO3 of the locale
        ''' file name; "" = union across languages. Phrase keys are folded.
        ''' </summary>
        Private Class NumberTable
            Public ReadOnly Phrases As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
            Public MaxWords As Integer = 1
            Public Sub Add(phrase As String, value As Integer)
                Phrases(phrase) = value
                MaxWords = Math.Max(MaxWords, phrase.Split(" "c).Length)
            End Sub
        End Class

        Private Shared ReadOnly NumberWordsByLang As New Lazy(Of Dictionary(Of String, NumberTable))(AddressOf LoadNumberWords)

        Private Shared Function LoadNumberWords() As Dictionary(Of String, NumberTable)
            Dim tables As New Dictionary(Of String, NumberTable)(StringComparer.OrdinalIgnoreCase)
            Dim union As New NumberTable()
            tables("") = union
            For Each localeDir In LocaleDirs()
                Try
                    If Not Directory.Exists(localeDir) Then Continue For
                    For Each f In Directory.GetFiles(localeDir, "*.json")
                        Try
                            Dim lang = NormalizeLangCode(Path.GetFileNameWithoutExtension(f))
                            Using doc = JsonDocument.Parse(File.ReadAllText(f))
                                Dim el As JsonElement = Nothing
                                If doc.RootElement.TryGetProperty("Bible_NumberWords", el) Then
                                    Dim table As NumberTable = Nothing
                                    If Not tables.TryGetValue(lang, table) Then
                                        table = New NumberTable()
                                        tables(lang) = table
                                    End If
                                    For Each pair In If(el.GetString(), "").Split(","c)
                                        Dim sep = pair.LastIndexOf(":"c)
                                        Dim v = 0
                                        If sep > 0 AndAlso Integer.TryParse(pair.Substring(sep + 1).Trim(), v) AndAlso v >= 1 AndAlso v <= 999 Then
                                            Dim phrase = BookAliasIndex.Fold(pair.Substring(0, sep).Trim())
                                            If phrase.Length >= 3 Then
                                                table.Add(phrase, v)
                                                union.Add(phrase, v)
                                            End If
                                        End If
                                    Next
                                End If
                            End Using
                        Catch
                            ' Malformed locale file — skip; others still contribute.
                        End Try
                    Next
                Catch
                    ' Locale dir unreadable — number words degrade to digits-only.
                End Try
            Next
            Return tables
        End Function

        ' Internal class to track DB path alongside translation info
        Private Class BibleTranslationEntry
            Public Property Info As BibleTranslation
            Public Property DbPath As String
            Public Property BookMap As Dictionary(Of String, Integer) ' short_name/long_name -> book_number
            ''' <summary>Every book_number in this Bible - also those whose names were all
            ''' dropped as shared (a book must stay openable by its number).</summary>
            Public Property BookNumbers As HashSet(Of Integer)
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

        Public ReadOnly Property BiblesDirectory As String Implements IBibleService.BiblesDirectory
            Get
                Return _biblesDir
            End Get
        End Property

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

            ' Build the DERIVED book-alias index (reference detection in every
            ' installed Bible's language) in the background. Cached per Bible
            ' file, so steady-state cost is one small JSON read. ALWAYS built,
            ' even with zero Bibles — the locale files' Bible_BookNames feed it,
            ' which is the whole no-Bibles fallback (no static table in code).
            ' Until the index lands (~1-2s), detection finds nothing.
            Dim dbPaths = _translations.Values.Select(Function(e) e.DbPath).ToList()
            Task.Run(Sub()
                         Try
                             Dim idx = BookAliasIndex.Build(dbPaths)
                             _aliasIndex = idx
                             ' Structured event → lands in session.log, so field
                             ' verification can confirm the index built.
                             Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ALIAS_INDEX,
                                 $"Book-alias index ready: {idx.Count} names from {dbPaths.Count} Bible(s), {idx.AmbiguousCount} ambiguous")
                         Catch ex As Exception
                             Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR,
                                 $"Book-alias index build failed: {ex.Message} — reference detection disabled")
                         End Try
                     End Sub)
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
            Dim bookNumbers As New HashSet(Of Integer)

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
                                        ' MyBible modules may carry HTML here (<br/> etc.) —
                                        ' both viewers render the string as plain text, so
                                        ' sanitize once at ingestion (e.g. the BEC's
                                        ' YouVersion-derived attribution block).
                                        If copyright Is Nothing AndAlso Not String.IsNullOrEmpty(val) Then copyright = CleanCopyrightMarkup(val)
                                End Select
                            End While
                        End Using
                    End Using
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR, $"BibleService.LoadTranslation: failed reading info for '{dbFile}' - {ex.Message}")
                End Try

                ' Build book_number map from books table (both short_name and long_name).
                ' A name this Bible gives to TWO books identifies neither, so it is left
                ' out (BCI names both Jeremiah 300 and the Letter of Jeremiah 315
                ' "Jeremies": typed "Jeremies 5" opened the one-chapter letter).
                Dim sharedNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT book_number, short_name, long_name FROM books"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim bookNum = reader.GetInt32(0)
                            bookNumbers.Add(bookNum)
                            For Each bookName In {reader.GetString(1), reader.GetString(2)}
                                Dim known As Integer
                                If bookMap.TryGetValue(bookName, known) AndAlso known <> bookNum Then
                                    sharedNames.Add(bookName)
                                Else
                                    bookMap(bookName) = bookNum
                                End If
                            Next
                        End While
                    End Using
                End Using
                For Each sharedName In sharedNames
                    bookMap.Remove(sharedName)
                Next
                If sharedNames.Count > 0 Then
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ALIAS_INDEX,
                        $"Bible '{id}': book name(s) used for more than one book, not used for lookup: {String.Join(", ", sharedNames)}")
                End If
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
                .BookMap = bookMap,
                .BookNumbers = bookNumbers
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
                                ElseIf maxCh > 0 AndAlso CodeForNumber.ContainsKey(reader.GetInt32(1)) Then
                                    ' Check for gaps in chapters — ONLY for the 66-book
                                    ' canon (books with a standard wire code), where
                                    ' chapters are contiguous by definition. Supplementary
                                    ' books legitimately skip chapters (e.g. BCI's
                                    ' "Daniel (fragments grecs)" holds only 3, 13, 14 —
                                    ' the Greek additions), so a gap there is design,
                                    ' not corruption.
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

            ' A universal book_number ("480") - what the web viewer sends. Names are
            ' not unique across Bibles (BCI's "Jer" is book 315, every other Bible's
            ' "Jer" is Jeremiah, 300), so a client that carries a name from one Bible
            ' into another can open the wrong book; a number cannot.
            Dim bookNum As Integer
            If Integer.TryParse(book, Globalization.NumberStyles.None, Globalization.CultureInfo.InvariantCulture, bookNum) Then
                Return If(entry.BookNumbers.Contains(bookNum), bookNum, -1)
            End If

            ' Try direct match on short_name or long_name from DB
            If entry.BookMap.TryGetValue(book, bookNum) Then Return bookNum

            ' Derived alias index: a name in ANY installed Bible's language (or
            ' a locale-file fallback name) → universal book_number.
            Dim info = _aliasIndex?.Lookup(book)
            If info IsNot Nothing AndAlso entry.BookNumbers.Contains(info.BookNumber) Then
                Return info.BookNumber
            End If

            ' A wire code ("Ps", "1Tim") → universal book_number.
            Dim stdNum As Integer
            If StandardBookNumbers.TryGetValue(book, stdNum) Then
                If entry.BookNumbers.Contains(stdNum) Then Return stdNum
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
            Dim bookName = ""
            Dim chapterCount = 0
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
                ' The book's own name and chapter count in THIS Bible: the web viewer
                ' builds its chapter grid from these (field 2026-10-04: a caption link
                ' for "Mar" matched no short_name and showed a 50-chapter default).
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT b.long_name, (SELECT MAX(v.chapter) FROM verses v WHERE v.book_number = @bn) " &
                        "FROM books b WHERE b.book_number = @bn"
                    cmd.Parameters.AddWithValue("@bn", bookNum)
                    Using reader = cmd.ExecuteReader()
                        If reader.Read() Then
                            bookName = If(reader.IsDBNull(0), "", reader.GetString(0))
                            chapterCount = If(reader.IsDBNull(1), 0, reader.GetInt32(1))
                        End If
                    End Using
                End Using
            End Using

            Return Task.FromResult(New BibleChapter With {
                .TranslationId = translationId, .Book = book,
                .Chapter = chapter, .Verses = verses,
                .BookNumber = bookNum, .BookName = bookName, .ChapterCount = chapterCount})
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
                                .BookNumber = reader.GetInt32(0),
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
            ' The number each step resolved. The code is NOT looked up again by
            ' name: a wire code can be another book's short_name in this Bible
            ' (wire "Jer" = Jeremiah 300; BCI's short_name "Jer" = book 315).
            Dim resolvedBookNum = 0

            ' 1. Try translation's own BookMap (short_name + long_name from DB)
            If Not String.IsNullOrEmpty(translationId) Then
                Dim entry As BibleTranslationEntry = Nothing
                If _translations.TryGetValue(translationId, entry) Then
                    Dim bookNum As Integer
                    If entry.BookMap.TryGetValue(bookName, bookNum) Then
                        resolvedBookNum = bookNum
                        bookCode = entry.BookMap.
                            Where(Function(kv) kv.Value = bookNum).
                            OrderBy(Function(kv) kv.Key.Length).
                            Select(Function(kv) kv.Key).FirstOrDefault()
                    End If
                End If
            End If

            ' 2. Derived alias index: any installed Bible's language + locale
            '    fallback names, with span-shedding and ordinal forms ("Primera
            '    de Joan"). Typed lookups accept abbreviations ("Gèn 1"), so the
            '    Abbreviation flag is not checked here.
            If bookCode Is Nothing Then
                Dim rb = ResolveBookAlias(bookName)
                If rb IsNot Nothing Then
                    bookCode = rb.Code
                    resolvedBookNum = rb.BookNumber
                End If
            End If

            ' 3. A wire code typed directly ("Ps 23", "1Tim 6").
            If bookCode Is Nothing Then
                Dim stdNum = 0
                If StandardBookNumbers.TryGetValue(bookName, stdNum) Then
                    bookCode = CodeForNumber(stdNum)
                    resolvedBookNum = stdNum
                End If
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
                If m.Groups("vend").Success AndAlso
                   (Not m.Groups("rangeword").Success OrElse
                    SpokenRangeWords.Value.Contains(BookAliasIndex.Fold(m.Groups("rangeword").Value))) Then
                    vEnd = Integer.Parse(m.Groups("vend").Value)
                End If
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

        ''' <summary>Alias index derived from the installed Bibles (built in background at startup).</summary>
        Private Shared _aliasIndex As BookAliasIndex

        ''' <summary>number → wire code, the reverse of StandardBookNumbers (built once).</summary>
        Private Shared ReadOnly CodeForNumber As Dictionary(Of Integer, String) =
            StandardBookNumbers.GroupBy(Function(k) k.Value).
                ToDictionary(Function(g) g.Key, Function(g) g.First().Key)

        ''' <summary>Universal book_number for a standard wire code ("Mat" → 470); 0 if unknown.</summary>
        Public Shared Function StandardNumberForCode(code As String) As Integer
            Dim n = 0
            If StandardBookNumbers.TryGetValue(If(code, ""), n) Then Return n
            Return 0
        End Function

        Private Class ResolvedBook
            Public Property Code As String
            Public Property Ambiguous As Boolean
            Public Property HadOrdinal As Boolean
            Public Property BookNumber As Integer
            Public Property Abbreviation As Boolean
            ''' <summary>The resolved name began lowercase in the caption (STT
            ''' case-drop). Accepted only for names the frequency data proves
            ''' are rare as ordinary prose words ("salmo" yes, "mateu" no).</summary>
            Public Property LowercaseStart As Boolean
            ''' <summary>The candidate span that actually resolved (after leading
            ''' drops) — lets the caller anchor the underline at the book name
            ''' instead of the whole announcement prefix.</summary>
            Public Property ResolvedName As String = ""
        End Class

        ''' <summary>
        ''' Resolve a matched book name via the derived index (any installed
        ''' Bible's language, plus the locale files' fallback names). Tries the
        ''' full captured span, then drops leading tokens (a preceding
        ''' capitalized word must not hide a reference). Ordinal prefixes
        ''' resolve through pairs derived from the Bibles' own numbered names
        ''' ("Primera de Joan" → 1 John, never the gospel). Nothing resolves
        ''' until the index lands (~1-2s after startup, cached).
        ''' </summary>
        Private Shared Function ResolveBookAlias(rawName As String) As ResolvedBook
            Dim name = BookAliasIndex.NormName(rawName)
            Dim idx = _aliasIndex
            If idx IsNot Nothing Then
                ' Try every suffix — announcement prefixes run long ("Empezamos
                ' con el Salmo 22", "Qué dice el evangelista Juan…") and a
                ' capped drop silently hid the reference. Resolution + the
                ' evidence tiers stay the gate; the drop is just span-shedding.
                Dim parts = name.Split(" "c)
                For dropCount = 0 To parts.Length - 1
                    Dim sub_ = parts.Skip(dropCount).ToArray()
                    Dim candidate = String.Join(" ", sub_)
                    Dim info = idx.Lookup(candidate)
                    Dim hadOrdinal = False
                    ' Ordinal path: first token is an ordinal word → pair with
                    ' the LAST token ("Primera de Joan" → (1, joan)).
                    If info Is Nothing AndAlso sub_.Length >= 2 Then
                        Dim n = 0
                        If idx.TryOrdinalWord(sub_(0), n) Then
                            info = idx.OrdinalLookup(n, sub_.Last())
                            hadOrdinal = info IsNot Nothing
                        End If
                    End If
                    If info IsNot Nothing Then
                        Dim code As String = Nothing
                        If CodeForNumber.TryGetValue(info.BookNumber, code) Then
                            Return New ResolvedBook With {.Code = code, .Ambiguous = info.Ambiguous, .HadOrdinal = hadOrdinal,
                                                          .BookNumber = info.BookNumber, .Abbreviation = info.Abbreviation,
                                                          .LowercaseStart = sub_(0).Length > 0 AndAlso Char.IsLower(sub_(0)(0)),
                                                          .ResolvedName = candidate}
                        End If
                        ' Deuterocanonical numbers have no wire code yet — skip.
                        Return Nothing
                    End If
                Next
            End If
            Return Nothing
        End Function

        Public Function DetectReferencesInText(text As String
        ) As IReadOnlyList(Of DetectedReference) Implements IBibleService.DetectReferencesInText
            Return DetectReferencesInText(text, Nothing)
        End Function

        Public Function DetectReferencesInText(text As String, opts As RefDetectionOptions
        ) As IReadOnlyList(Of DetectedReference) Implements IBibleService.DetectReferencesInText
            Dim detectedRefs As New List(Of DetectedReference)()
            If String.IsNullOrEmpty(text) Then Return detectedRefs

            ' Spoken numbers → digits (equal-length shadow; original offsets stay
            ' valid). The digit-anchored pattern then sees "Salmo 45" where the
            ' preacher said "Salmo cuarenta y cinco".
            Dim subRanges As New List(Of SubRange)()
            Dim scanText = NormalizeSpokenNumbers(text, opts?.LangHint, subRanges)

            For Each m As Match In RefPattern.Matches(scanText)
                Dim bookName = m.Groups("book").Value.Trim()
                Dim refStart = m.Index
                ' A filler word between book and number ("Mateu, capítol 4",
                ' "Mateo, en el capítulo 27") is accepted ONLY when it is a known
                ' chapter word — and then it is STRONG evidence ("Mateu capítol 4"
                ' cannot be the verb reading of "mateu", even sentence-initial,
                ' which is exactly where reading announcements live).
                Dim hadChapterWord = False
                Dim resolved As ResolvedBook = Nothing
                If m.Groups("chapword").Success Then
                    If SpokenChapterWords.Value.Contains(BookAliasIndex.Fold(m.Groups("chapword").Value)) Then
                        hadChapterWord = True
                    Else
                        ' Not a chapter word — but it may BE the book: in
                        ' "La primera, Timoteu 6:10" the filler slot captures
                        ' "Timoteu" (a preceding capitalized word swallowed the
                        ' book slot). Re-anchor on the filler word; anything
                        ' that resolves to no book rejects the match.
                        Dim cw = m.Groups("chapword")
                        resolved = ResolveBookAlias(cw.Value)
                        If resolved Is Nothing Then Continue For
                        bookName = cw.Value
                        refStart = cw.Index
                    End If
                End If
                If resolved Is Nothing Then resolved = ResolveBookAlias(bookName)
                If resolved Is Nothing Then Continue For
                ' Anchor the underline at the resolved name, not the whole
                ' announcement prefix ("Los primeros intérpretes … el Salmo 22"
                ' must underline from "Salmo", not the sentence start).
                If resolved.ResolvedName.Length > 0 Then
                    Dim bg = m.Groups("book")
                    Dim within = Math.Min(bg.Length, scanText.Length - bg.Index)
                    Dim pos = scanText.LastIndexOf(resolved.ResolvedName, bg.Index + within - 1, within, StringComparison.OrdinalIgnoreCase)
                    If pos >= refStart Then refStart = pos
                End If
                ' Spoken text contains FULL book names only — abbreviations in
                ' captions are STT garbles ("Am 10 set anys" false-fired Amos
                ' and re-scoped the vocab book, 2026-07-31). Typed lookups
                ' (ParseReferenceAsync) still accept abbreviations.
                If resolved.Abbreviation Then Continue For
                ' Lowercase-start names (STT case-drop, "el salmo 22") pass only
                ' when the frequency data proves the word is rare as ordinary
                ' prose — the capital remains required for everything ambiguous.
                If resolved.LowercaseStart AndAlso resolved.Ambiguous Then Continue For
                Dim bookCode = resolved.Code

                ' Spoken-verse tail: "Salmo 22, versículo 7". An invalid versword
                ' ("Salmo 22 durante 3") degrades to chapter-only — never rejects.
                Dim hasVerse = m.Groups("verse").Success
                Dim effLength = m.Index + m.Length - refStart
                If m.Groups("versword").Success Then
                    If Not SpokenVerseWords.Value.Contains(BookAliasIndex.Fold(m.Groups("versword").Value)) Then
                        hasVerse = False
                        effLength = m.Groups("chapter").Index + m.Groups("chapter").Length - refStart
                    End If
                End If

                ' Tiered evidence: names that are also ordinary words ("mateu"
                ' = the verb "kill", "fets" = deeds — derived from the Bibles'
                ' own text, never curated) need MORE than name+chapter: a
                ' chapter:verse, a digit/ordinal prefix, OR a mid-sentence
                ' capital ("en Mateu 4" — the STT capitalized it as a name;
                ' sentence-initial capitals prove nothing).
                If resolved.Ambiguous AndAlso
                   Not hasVerse AndAlso
                   Not hadChapterWord AndAlso
                   Not resolved.HadOrdinal AndAlso
                   Not Char.IsDigit(bookName(0)) Then
                    Dim midSentence = False
                    Dim i = refStart - 1
                    While i >= 0 AndAlso Char.IsWhiteSpace(text(i))
                        i -= 1
                    End While
                    If i >= 0 AndAlso Not ".!?…".Contains(text(i)) Then midSentence = True
                    If Not midSentence Then Continue For
                End If

                Dim chap = Integer.Parse(m.Groups("chapter").Value)
                ' Impossible chapters can't be references — validate against the
                ' Bibles' own structure (Amos has 9 chapters, so "Am 10"/"Amós 10"
                ' dies here). 0 = book structure unknown (no Bibles) → skip check.
                Dim maxCh = If(_aliasIndex?.MaxChapter(resolved.BookNumber), 0)
                If chap < 1 OrElse (maxCh > 0 AndAlso chap > maxCh) Then Continue For
                Dim vStart = 0
                Dim vEnd = 0
                If hasVerse Then
                    vStart = Integer.Parse(m.Groups("verse").Value)
                    vEnd = vStart
                    If m.Groups("vend").Success Then
                        ' A spoken connector must be a known range word ("38 al
                        ' 40"); an unknown one ("37 en 197…") keeps the single
                        ' verse and gives the span back — never rejects.
                        If Not m.Groups("rangeword").Success OrElse
                           SpokenRangeWords.Value.Contains(BookAliasIndex.Fold(m.Groups("rangeword").Value)) Then
                            vEnd = Integer.Parse(m.Groups("vend").Value)
                            If vEnd < vStart Then vEnd = vStart
                        Else
                            effLength = m.Groups("verse").Index + m.Groups("verse").Length - refStart
                        End If
                    End If
                    ' Nonexistent verses can't be references either — degrade to
                    ' chapter-only, like an invalid versword (the announced
                    ' chapter is still real). 0 = structure unknown, skip check.
                    Dim maxV = If(_aliasIndex?.MaxVerse(resolved.BookNumber, chap), 0)
                    If maxV > 0 AndAlso vStart > maxV Then
                        hasVerse = False
                        vStart = 0
                        vEnd = 0
                        effLength = m.Groups("chapter").Index + m.Groups("chapter").Length - refStart
                    ElseIf maxV > 0 AndAlso vEnd > maxV Then
                        vEnd = maxV
                    End If
                End If

                ' A match ending inside a substituted span underlines the whole
                ' spoken phrase ("…capítulo veintisiete", not "…capítulo ve").
                For Each r In subRanges
                    If refStart + effLength > r.Start AndAlso refStart + effLength <= r.EndEx Then
                        effLength = r.EndEx - refStart
                        Exit For
                    End If
                Next

                detectedRefs.Add(New DetectedReference With {
                    .Reference = New BibleReference With {
                        .Book = bookCode,
                        .BookNumber = resolved.BookNumber,
                        .Chapter = chap,
                        .VerseStart = vStart,
                        .VerseEnd = vEnd,
                        .IsValid = True
                    },
                    .MatchedText = text.Substring(refStart, effLength),
                    .StartIndex = refStart,
                    .Length = effLength
                })
            Next

            ' ── Reading-context pass: bare "versículo N" ─────────────────────
            ' Owner semantics: a full reference sets the room's book; later bare
            ' verse words belong to it; a new book resets it. A numberless book
            ' word in the phrase ("versículo 15 del salmo") rescues back to that
            ' book's remembered chapter — the preacher's own disambiguation.
            Dim ctx = opts?.Context
            If ctx IsNot Nothing Then
                If opts.UpdateContext Then
                    For Each d In detectedRefs
                        RememberContext(ctx, d.Reference)
                    Next
                End If
                For Each m As Match In BareVersePattern.Matches(scanText)
                    If Not SpokenVerseWords.Value.Contains(BookAliasIndex.Fold(m.Groups("versword").Value)) Then Continue For
                    Dim mIndex = m.Index, mLen = m.Length
                    If detectedRefs.Any(Function(d) mIndex < d.StartIndex + d.Length AndAlso mIndex + mLen > d.StartIndex) Then Continue For

                    Dim vStart = Integer.Parse(m.Groups("verse").Value)
                    If vStart < 1 Then Continue For
                    Dim vEnd = vStart
                    Dim effLength = m.Length
                    If m.Groups("vend").Success Then
                        If Not m.Groups("rangeword").Success OrElse
                           SpokenRangeWords.Value.Contains(BookAliasIndex.Fold(m.Groups("rangeword").Value)) Then
                            vEnd = Integer.Parse(m.Groups("vend").Value)
                            If vEnd < vStart Then vEnd = vStart
                        Else
                            effLength = m.Groups("verse").Index + m.Groups("verse").Length - m.Index
                        End If
                    End If

                    ' Candidate owners: an explicit book mention ("versículo 15
                    ' del salmo") is the preacher's own disambiguation — trust
                    ' it alone. Otherwise recency order — BUT the verse must
                    ' EXIST in the candidate's remembered chapter, else try the
                    ' next remembered book. The Jezer 2026-09-06 failure: a
                    ' one-off cross-reference (1Tim 6:10) stole LastBook from
                    ' the sermon psalm, and "verset 37" linked 1Tim 6:37 — a
                    ' verse that doesn't exist (1Tim 6 has 21); falling back
                    ' finds Ps 119:37, the verse the preacher then quoted.
                    Dim candidates = ContextCandidates(ctx, scanText)

                    Dim bookNum = 0
                    Dim entry As RefContext.BookEntry = Nothing
                    For Each cand In candidates
                        Dim e As RefContext.BookEntry = Nothing
                        If Not ctx.Books.TryGetValue(cand, e) Then Continue For
                        If (DateTime.UtcNow - e.LastSeenUtc).TotalMinutes > ContextExpiryMinutes Then Continue For
                        Dim maxV = If(_aliasIndex?.MaxVerse(cand, e.Chapter), 0)
                        If maxV > 0 AndAlso vStart > maxV Then Continue For
                        If maxV > 0 AndAlso vEnd > maxV Then vEnd = maxV
                        bookNum = cand
                        entry = e
                        Exit For
                    Next
                    If bookNum = 0 Then Continue For
                    For Each r In subRanges
                        If m.Index + effLength > r.Start AndAlso m.Index + effLength <= r.EndEx Then
                            effLength = r.EndEx - m.Index
                            Exit For
                        End If
                    Next

                    entry.LastSeenUtc = DateTime.UtcNow
                    entry.Verse = vStart
                    ctx.LastBook = bookNum
                    detectedRefs.Add(New DetectedReference With {
                        .Reference = New BibleReference With {
                            .Book = entry.BookCode,
                            .BookNumber = bookNum,
                            .Chapter = entry.Chapter,
                            .VerseStart = vStart,
                            .VerseEnd = vEnd,
                            .IsValid = True
                        },
                        .MatchedText = text.Substring(m.Index, effLength),
                        .StartIndex = m.Index,
                        .Length = effLength,
                        .FromContext = True
                    })
                Next

            End If

            Return detectedRefs
        End Function

        ''' <summary>Owner candidates for a context-resolved token: an explicit
        ''' book mention in the phrase wins alone; otherwise every remembered
        ''' book in recency order, LastBook first.</summary>
        Private Shared Function ContextCandidates(ctx As RefContext, scanText As String) As List(Of Integer)
            Dim mentioned = ContextBookMention(ctx, scanText)
            If mentioned <> 0 Then Return New List(Of Integer) From {mentioned}
            Dim candidates = ctx.Books.
                OrderByDescending(Function(kv) kv.Value.LastSeenUtc).
                Select(Function(kv) kv.Key).ToList()
            If ctx.LastBook <> 0 AndAlso candidates.Remove(ctx.LastBook) Then
                candidates.Insert(0, ctx.LastBook)
            End If
            Return candidates
        End Function

        ''' <summary>Reading-context freshness window — a memory older than this
        ''' can't resolve bare verses (no stale carry-over into a next service).
        ''' Public: the /api/rooms/{id}/reading endpoint applies the same window.</summary>
        Public Const ContextExpiryMinutes As Integer = 30

        ''' <summary>Bare "versword N" ("Versículo 18", "versículo 7-9") — versword
        ''' validated against the locale set by the caller; capital allowed (STT
        ''' capitalizes after the periods it inserts).</summary>
        Private Shared ReadOnly BareVersePattern As New Regex(
            "(?<versword>[\p{Lu}\p{Ll}][\p{Ll}'’]+)\s+(?<verse>\d{1,3})(?:\s*-\s*(?<vend>\d{1,3})|\s+(?<rangeword>[\p{Ll}]{1,7})\s+(?<vend>\d{1,3})(?!\d))?",
            RegexOptions.Compiled)

        ''' <summary>Record a full detection in the room's reading memory (bounded per-book map + recency pointer).</summary>
        Private Shared Sub RememberContext(ctx As RefContext, ref As BibleReference)
            If ref Is Nothing OrElse ref.BookNumber = 0 OrElse ref.Chapter < 1 Then Return
            Dim e As RefContext.BookEntry = Nothing
            If Not ctx.Books.TryGetValue(ref.BookNumber, e) Then
                If ctx.Books.Count >= RefContext.MaxBooks Then
                    Dim oldest = ctx.Books.OrderBy(Function(kv) kv.Value.LastSeenUtc).First().Key
                    ctx.Books.Remove(oldest)
                End If
                e = New RefContext.BookEntry()
                ctx.Books(ref.BookNumber) = e
            End If
            e.BookCode = ref.Book
            e.Chapter = ref.Chapter
            e.Verse = ref.VerseStart
            e.LastSeenUtc = DateTime.UtcNow
            ctx.LastBook = ref.BookNumber
        End Sub

        ''' <summary>
        ''' A numberless book word in the text that names a REMEMBERED book
        ''' ("del salmo" → the psalm entry) — resolved via the same alias index
        ''' (spoken singulars from Bible_SpokenBookNames, titles from the
        ''' Bibles), so no language knowledge lives here. Last mention wins.
        ''' </summary>
        Private Shared Function ContextBookMention(ctx As RefContext, scanText As String) As Integer
            Dim idx = _aliasIndex
            If idx Is Nothing Then Return 0
            Dim found = 0
            For Each tok As Match In Regex.Matches(scanText, "[\p{L}][\p{L}'’-]{2,}")
                Dim info = idx.Lookup(tok.Value)
                If info IsNot Nothing AndAlso Not info.Abbreviation AndAlso ctx.Books.ContainsKey(info.BookNumber) Then
                    found = info.BookNumber
                End If
            Next
            Return found
        End Function

        ''' <summary>One substituted span in the number-normalization shadow (original-text offsets; EndEx exclusive).</summary>
        Private Structure SubRange
            Public Start As Integer
            Public EndEx As Integer
        End Structure

        ''' <summary>
        ''' Replace spoken number phrases with equal-length digit substitutions
        ''' ("cuarenta y cinco" → "45" + padding), so the digit-anchored
        ''' RefPattern sees numbers while every original-text offset stays
        ''' valid. NO arithmetic — a phrase either appears verbatim in the
        ''' locale table (longest match first) or it is not a number.
        ''' Substitution happens only in reference-shaped positions: right
        ''' after a chapter/verse word (that word is the anchor — union table
        ''' is safe), or right after a capitalized token (book-adjacent), which
        ''' uses ONLY the language-hinted table with a ≥4-char first-word floor
        ''' (cross-language collisions: es "once"=11 is the English word
        ''' "once"; ca "set"/"nou"/"deu"). No hint → book-adjacent is off.
        ''' </summary>
        Private Shared Function NormalizeSpokenNumbers(text As String, langHint As String,
                                                       ranges As List(Of SubRange)) As String
            Dim tables = NumberWordsByLang.Value
            Dim union As NumberTable = Nothing
            If Not tables.TryGetValue("", union) OrElse union.Phrases.Count = 0 Then Return text
            Dim hinted As NumberTable = Nothing
            If Not String.IsNullOrEmpty(langHint) Then tables.TryGetValue(NormalizeLangCode(langHint), hinted)

            Dim toks = Regex.Matches(text, "\S+")
            If toks.Count < 2 Then Return text
            ' Punctuation-trimmed core span per token ("veintidós," → "veintidós").
            Dim coreStart(toks.Count - 1) As Integer
            Dim coreLen(toks.Count - 1) As Integer
            Dim core(toks.Count - 1) As String
            For i = 0 To toks.Count - 1
                Dim s = toks(i).Index
                Dim e = toks(i).Index + toks(i).Length - 1
                While s <= e AndAlso Not Char.IsLetterOrDigit(text(s))
                    s += 1
                End While
                While e >= s AndAlso Not Char.IsLetterOrDigit(text(e))
                    e -= 1
                End While
                coreStart(i) = s
                coreLen(i) = Math.Max(0, e - s + 1)
                core(i) = If(coreLen(i) > 0, BookAliasIndex.Fold(text.Substring(s, coreLen(i))), "")
            Next

            Dim result As Char() = Nothing
            Dim i2 = 1
            While i2 < toks.Count
                If core(i2).Length = 0 Then i2 += 1 : Continue While
                Dim prev = core(i2 - 1)
                Dim anchorTrigger = prev.Length > 0 AndAlso
                    (SpokenChapterWords.Value.Contains(prev) OrElse SpokenVerseWords.Value.Contains(prev))
                Dim capTrigger = hinted IsNot Nothing AndAlso coreLen(i2 - 1) > 0 AndAlso
                    Char.IsUpper(text(coreStart(i2 - 1))) AndAlso core(i2).Length >= 4
                If Not anchorTrigger AndAlso Not capTrigger Then i2 += 1 : Continue While
                Dim table = If(anchorTrigger, union, hinted)

                Dim matched = 0
                Dim value = 0
                For wc = Math.Min(table.MaxWords, toks.Count - i2) To 1 Step -1
                    Dim ok = True
                    For k = i2 To i2 + wc - 1
                        If core(k).Length = 0 Then ok = False : Exit For
                    Next
                    If Not ok Then Continue For
                    Dim candidate = String.Join(" ", Enumerable.Range(i2, wc).Select(Function(k) core(k)))
                    If table.Phrases.TryGetValue(candidate, value) Then
                        matched = wc
                        Exit For
                    End If
                Next
                If matched = 0 Then i2 += 1 : Continue While

                Dim spanStart = coreStart(i2)
                Dim spanEnd = coreStart(i2 + matched - 1) + coreLen(i2 + matched - 1)
                Dim digits = value.ToString(Globalization.CultureInfo.InvariantCulture)
                If digits.Length <= spanEnd - spanStart Then
                    If result Is Nothing Then result = text.ToCharArray()
                    For k = spanStart To spanEnd - 1
                        result(k) = " "c
                    Next
                    For k = 0 To digits.Length - 1
                        result(spanStart + k) = digits(k)
                    Next
                    ranges.Add(New SubRange With {.Start = spanStart, .EndEx = spanEnd})
                End If
                i2 += matched
            End While
            Return If(result Is Nothing, text, New String(result))
        End Function

        ''' <summary>
        ''' Normalize a language code to 3-letter ISO 639-3 for consistent matching.
        ''' Handles both 2-letter (en, es) and 3-letter (eng, spa) inputs.
        ''' </summary>
        Private Shared Function NormalizeLangCode(code As String) As String
            Return Services.Infrastructure.LanguageCodeService.Instance.NormalizeToIso3(code)
        End Function

        ''' <summary>
        ''' Flatten the HTML that MyBible modules may carry in copyright/detailed_info
        ''' into display-safe plain text: &lt;br&gt; variants become line separators,
        ''' remaining tags are stripped, entities decoded, and the lines joined with
        ''' " · " so both viewers (WinForms Label + web textContent) render one clean
        ''' attribution line regardless of layout.
        ''' </summary>
        Private Shared Function CleanCopyrightMarkup(val As String) As String
            Dim s = Text.RegularExpressions.Regex.Replace(val, "<br\s*/?>", vbLf,
                Text.RegularExpressions.RegexOptions.IgnoreCase)
            s = Text.RegularExpressions.Regex.Replace(s, "<[^>]+>", "")
            s = Net.WebUtility.HtmlDecode(s)
            Dim lines = s.Split(New Char() {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries).
                Select(Function(l) l.Trim()).
                Where(Function(l) l.Length > 0)
            Return String.Join(" · ", lines)
        End Function
    End Class
End Namespace
