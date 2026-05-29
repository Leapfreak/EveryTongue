Imports System.IO
Imports System.Text
Imports Microsoft.Data.Sqlite
Imports USFMToolsSharp
Imports USFMToolsSharp.Models.Markers

Namespace Services.Bible

    ''' <summary>
    ''' Converts USFM Bible files into SQLite3 databases compatible with BibleService.
    ''' Uses USFMToolsSharp for spec-compliant parsing.
    ''' Schema: info(name, value), books(book_number, short_name, long_name), verses(book_number, chapter, verse, text)
    ''' </summary>
    Public Class UsfmConverter

        ''' <summary>
        ''' USFM 3-letter book code to MyBible-compatible book number.
        ''' Uses the same numbering scheme as MyBible databases (10, 20, 30... with gaps
        ''' for deuterocanonical books) so all Bibles work consistently in BibleService.
        ''' </summary>
        Private Shared ReadOnly BookNumbers As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
            {"GEN", 10}, {"EXO", 20}, {"LEV", 30}, {"NUM", 40}, {"DEU", 50},
            {"JOS", 60}, {"JDG", 70}, {"RUT", 80}, {"1SA", 90}, {"2SA", 100},
            {"1KI", 110}, {"2KI", 120}, {"1CH", 130}, {"2CH", 140}, {"EZR", 150},
            {"NEH", 160}, {"EST", 190}, {"JOB", 220}, {"PSA", 230}, {"PRO", 240},
            {"ECC", 250}, {"SNG", 260}, {"ISA", 290}, {"JER", 300}, {"LAM", 310},
            {"EZK", 330}, {"DAN", 340}, {"HOS", 350}, {"JOL", 360}, {"AMO", 370},
            {"OBA", 380}, {"JON", 390}, {"MIC", 400}, {"NAM", 410}, {"HAB", 420},
            {"ZEP", 430}, {"HAG", 440}, {"ZEC", 450}, {"MAL", 460},
            {"MAT", 470}, {"MRK", 480}, {"LUK", 490}, {"JHN", 500}, {"ACT", 510},
            {"ROM", 520}, {"1CO", 530}, {"2CO", 540}, {"GAL", 550}, {"EPH", 560},
            {"PHP", 570}, {"COL", 580}, {"1TH", 590}, {"2TH", 600}, {"1TI", 610},
            {"2TI", 620}, {"TIT", 630}, {"PHM", 640}, {"HEB", 650}, {"JAS", 660},
            {"1PE", 670}, {"2PE", 680}, {"1JN", 690}, {"2JN", 700}, {"3JN", 710},
            {"JUD", 720}, {"REV", 730},
            {"TOB", 170}, {"JDT", 180}, {"ESG", 192}, {"WIS", 270}, {"SIR", 280},
            {"BAR", 320}, {"LJE", 315}, {"S3Y", 342}, {"SUS", 343}, {"BEL", 344},
            {"1MA", 462}, {"2MA", 464}, {"3MA", 466}, {"4MA", 468},
            {"1ES", 165}, {"2ES", 166}, {"MAN", 790}, {"PS2", 231},
            {"DAG", 341}, {"EZA", 167}, {"5EZ", 168}, {"6EZ", 169}
        }

        ''' <summary>Default short names for books (used when USFM \toc3 is missing). Uses MyBible numbering.</summary>
        Private Shared ReadOnly DefaultShortNames As New Dictionary(Of Integer, String) From {
            {10, "Gen"}, {20, "Exo"}, {30, "Lev"}, {40, "Num"}, {50, "Deu"},
            {60, "Josh"}, {70, "Judg"}, {80, "Ruth"}, {90, "1Sam"}, {100, "2Sam"},
            {110, "1Kin"}, {120, "2Kin"}, {130, "1Chr"}, {140, "2Chr"}, {150, "Ezr"},
            {160, "Neh"}, {190, "Esth"}, {220, "Job"}, {230, "Ps"}, {240, "Prov"},
            {250, "Eccl"}, {260, "Song"}, {290, "Isa"}, {300, "Jer"}, {310, "Lam"},
            {330, "Ezek"}, {340, "Dan"}, {350, "Hos"}, {360, "Joel"}, {370, "Am"},
            {380, "Oba"}, {390, "Jona"}, {400, "Mic"}, {410, "Nah"}, {420, "Hab"},
            {430, "Zeph"}, {440, "Hag"}, {450, "Zech"}, {460, "Mal"},
            {470, "Mat"}, {480, "Mar"}, {490, "Luk"}, {500, "John"}, {510, "Acts"},
            {520, "Rom"}, {530, "1Cor"}, {540, "2Cor"}, {550, "Gal"}, {560, "Eph"},
            {570, "Phil"}, {580, "Col"}, {590, "1Ths"}, {600, "2Ths"}, {610, "1Tim"},
            {620, "2Tim"}, {630, "Tit"}, {640, "Phlm"}, {650, "Heb"}, {660, "Jam"},
            {670, "1Pet"}, {680, "2Pet"}, {690, "1Jn"}, {700, "2Jn"}, {710, "3Jn"},
            {720, "Jud"}, {730, "Rev"},
            {170, "Tob"}, {180, "Jdt"}, {192, "EstGr"}, {270, "Wis"}, {280, "Sir"},
            {320, "Bar"}, {315, "LJe"}, {342, "S3Y"}, {343, "Sus"}, {344, "Bel"},
            {462, "1Mac"}, {464, "2Mac"}, {466, "3Mac"}, {468, "4Mac"},
            {165, "1Esd"}, {166, "2Esd"}, {790, "Man"}, {231, "Ps151"},
            {341, "DanGr"}, {167, "EzrA"}, {168, "5Ezr"}, {169, "6Ezr"}
        }

        ' Marker types to skip entirely (footnotes, cross-references, figures)
        Private Shared ReadOnly SkippedMarkerTypes As New HashSet(Of Type) From {
            GetType(FMarker), GetType(FEndMarker),
            GetType(XMarker), GetType(XEndMarker),
            GetType(FIGMarker), GetType(FIGEndMarker)
        }

        Private Class ParsedBook
            Public Property BookCode As String
            Public Property BookNumber As Integer
            Public Property ShortName As String
            Public Property LongName As String
            Public Property Verses As New List(Of (Chapter As Integer, Verse As Integer, Text As String))
        End Class

        ''' <summary>
        ''' Convert a directory of USFM files into a single SQLite3 database.
        ''' </summary>
        Public Shared Sub Convert(usfmDir As String, outputPath As String,
                                  description As String, languageCode As String)

            Dim parser As New USFMParser()
            Dim books As New List(Of ParsedBook)

            For Each usfmFile In Directory.GetFiles(usfmDir, "*.usfm")
                Dim parsed = ParseUsfmFile(parser, usfmFile)
                If parsed IsNot Nothing AndAlso parsed.Verses.Count > 0 Then
                    books.Add(parsed)
                End If
            Next

            If books.Count = 0 Then
                Throw New InvalidOperationException("No valid USFM books found in " & usfmDir)
            End If

            books.Sort(Function(a, b) a.BookNumber.CompareTo(b.BookNumber))

            If File.Exists(outputPath) Then File.Delete(outputPath)

            Dim connStr = New SqliteConnectionStringBuilder() With {
                .DataSource = outputPath,
                .Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString()

            Using conn As New SqliteConnection(connStr)
                conn.Open()

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "
                        CREATE TABLE info (name TEXT NOT NULL, value TEXT NOT NULL);
                        CREATE TABLE books (book_number INTEGER NOT NULL, short_name TEXT NOT NULL, long_name TEXT NOT NULL);
                        CREATE TABLE verses (book_number INTEGER NOT NULL, chapter INTEGER NOT NULL, verse INTEGER NOT NULL, text TEXT NOT NULL);
                        CREATE INDEX idx_verses ON verses (book_number, chapter, verse);
                    "
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "INSERT INTO info (name, value) VALUES (@n, @v)"
                    cmd.Parameters.Add("@n", SqliteType.Text)
                    cmd.Parameters.Add("@v", SqliteType.Text)

                    cmd.Parameters("@n").Value = "description"
                    cmd.Parameters("@v").Value = description
                    cmd.ExecuteNonQuery()

                    cmd.Parameters("@n").Value = "language"
                    cmd.Parameters("@v").Value = languageCode
                    cmd.ExecuteNonQuery()

                    cmd.Parameters("@n").Value = "source"
                    cmd.Parameters("@v").Value = "eBible.org (USFM)"
                    cmd.ExecuteNonQuery()
                End Using

                Using tx = conn.BeginTransaction()
                    Using bookCmd = conn.CreateCommand()
                        bookCmd.CommandText = "INSERT INTO books (book_number, short_name, long_name) VALUES (@bn, @sn, @ln)"
                        bookCmd.Parameters.Add("@bn", SqliteType.Integer)
                        bookCmd.Parameters.Add("@sn", SqliteType.Text)
                        bookCmd.Parameters.Add("@ln", SqliteType.Text)

                        For Each bk In books
                            bookCmd.Parameters("@bn").Value = bk.BookNumber
                            bookCmd.Parameters("@sn").Value = bk.ShortName
                            bookCmd.Parameters("@ln").Value = bk.LongName
                            bookCmd.ExecuteNonQuery()
                        Next
                    End Using

                    Using verseCmd = conn.CreateCommand()
                        verseCmd.CommandText = "INSERT INTO verses (book_number, chapter, verse, text) VALUES (@bn, @ch, @vs, @tx)"
                        verseCmd.Parameters.Add("@bn", SqliteType.Integer)
                        verseCmd.Parameters.Add("@ch", SqliteType.Integer)
                        verseCmd.Parameters.Add("@vs", SqliteType.Integer)
                        verseCmd.Parameters.Add("@tx", SqliteType.Text)

                        For Each bk In books
                            For Each v In bk.Verses
                                verseCmd.Parameters("@bn").Value = bk.BookNumber
                                verseCmd.Parameters("@ch").Value = v.Chapter
                                verseCmd.Parameters("@vs").Value = v.Verse
                                verseCmd.Parameters("@tx").Value = v.Text
                                verseCmd.ExecuteNonQuery()
                            Next
                        Next
                    End Using

                    tx.Commit()
                End Using
            End Using
        End Sub

        ''' <summary>
        ''' Parse a single USFM file using USFMToolsSharp.
        ''' Returns Nothing for non-canonical files.
        ''' </summary>
        Private Shared Function ParseUsfmFile(parser As USFMParser, filePath As String) As ParsedBook
            Dim content = File.ReadAllText(filePath, Encoding.UTF8)
            ' Pre-process USFM to work around USFMToolsSharp limitations:
            ' 1. Strip nested marker prefix \+ (e.g. \+w -> \w, \+wh -> \wh, \+nd -> \nd)
            '    The parser can't handle nested character markers and loses subsequent verses.
            ' 2. Strip footnotes (\f...\f*) and cross-references (\x...\x*) entirely.
            ' 3. Strip \d lines (descriptive titles like "A Psalm of David") — the parser's
            '    DMarker swallows all subsequent content when \d contains \w markers.
            content = content.Replace("\+", "\")
            content = StripNotesFromRaw(content)
            content = StripDescriptiveTitles(content)
            Dim doc = parser.ParseFromString(content)

            ' Extract book identification
            Dim idMarkers = doc.GetChildMarkers(Of IDMarker)()
            If idMarkers.Count = 0 Then Return Nothing

            Dim bookCode = idMarkers(0).TextIdentifier
            If String.IsNullOrEmpty(bookCode) Then Return Nothing
            bookCode = bookCode.Split(" "c)(0).ToUpper()

            If Not BookNumbers.ContainsKey(bookCode) Then Return Nothing

            Dim bookNum = BookNumbers(bookCode)

            ' Extract names from TOC markers and header
            Dim toc1Text As String = Nothing
            Dim toc2Text As String = Nothing
            Dim toc3Text As String = Nothing
            Dim headerText As String = Nothing

            Dim toc1List = doc.GetChildMarkers(Of TOC1Marker)()
            If toc1List.Count > 0 Then toc1Text = toc1List(0).LongTableOfContentsText

            Dim toc2List = doc.GetChildMarkers(Of TOC2Marker)()
            If toc2List.Count > 0 Then toc2Text = toc2List(0).ShortTableOfContentsText

            Dim toc3List = doc.GetChildMarkers(Of TOC3Marker)()
            If toc3List.Count > 0 Then toc3Text = toc3List(0).BookAbbreviation

            Dim hList = doc.GetChildMarkers(Of HMarker)()
            If hList.Count > 0 Then headerText = hList(0).HeaderText

            Dim shortName = If(toc3Text, If(DefaultShortNames.ContainsKey(bookNum), DefaultShortNames(bookNum), bookCode))
            Dim longName = If(toc1Text, If(toc2Text, If(headerText, bookCode)))

            ' Extract verses by walking chapters and verses
            Dim verses As New List(Of (Chapter As Integer, Verse As Integer, Text As String))
            Dim chapters = doc.GetChildMarkers(Of CMarker)()

            For Each chapter In chapters
                Dim chapterNum = chapter.Number
                Dim verseMarkers = chapter.GetChildMarkers(Of VMarker)()

                For Each verse In verseMarkers
                    Dim verseNum = verse.StartingVerse
                    Dim sb As New StringBuilder()
                    ExtractText(verse, sb)
                    Dim text = CollapseWhitespace(sb.ToString())

                    If Not String.IsNullOrEmpty(text) Then
                        ' Handle verse ranges (e.g. 3-4)
                        If verse.EndingVerse > verse.StartingVerse Then
                            For v = verse.StartingVerse To verse.EndingVerse
                                verses.Add((chapterNum, v, text))
                            Next
                        Else
                            verses.Add((chapterNum, verseNum, text))
                        End If
                    End If
                Next
            Next

            Return New ParsedBook With {
                .BookCode = bookCode,
                .BookNumber = bookNum,
                .ShortName = shortName,
                .LongName = longName,
                .Verses = verses
            }
        End Function

        ''' <summary>
        ''' Recursively extract plain text from a marker tree, skipping
        ''' footnotes, cross-references, and figures.
        ''' </summary>
        Private Shared Sub ExtractText(marker As Marker, sb As StringBuilder)
            ' For word-level markers, use the Term field (clean word without attributes)
            If TypeOf marker Is WMarker Then
                Dim wm = DirectCast(marker, WMarker)
                If Not String.IsNullOrEmpty(wm.Term) Then
                    sb.Append(wm.Term)
                End If
                Return
            End If

            ' TextBlock holds actual text content
            If TypeOf marker Is TextBlock Then
                Dim tb = DirectCast(marker, TextBlock)
                If Not String.IsNullOrEmpty(tb.Text) Then
                    sb.Append(tb.Text)
                End If
                Return
            End If

            ' Recurse into children, skipping footnotes/cross-refs/figures
            For Each child In marker.Contents
                If SkippedMarkerTypes.Contains(child.GetType()) Then Continue For
                ExtractText(child, sb)
            Next
        End Sub

        ''' <summary>
        ''' Strip \d lines (descriptive titles) and \s/\s1/\s2 lines (section headings) from USFM text.
        ''' The parser's DMarker consumes all subsequent content when \d contains \w markers.
        ''' Section headings (\s1) act as containers — verses nested inside them become invisible
        ''' to GetChildMarkers(Of VMarker) on the chapter (e.g. Psalm 119 Hebrew acrostic headings).
        ''' </summary>
        Private Shared Function StripDescriptiveTitles(usfm As String) As String
            Dim lines = usfm.Split({vbCrLf, vbLf}, StringSplitOptions.None)
            Dim sb As New StringBuilder(usfm.Length)
            For Each line In lines
                Dim trimmed = line.TrimStart()
                ' Strip \d (descriptive title) and \s/\s1/\s2/\s3 (section heading) lines
                If trimmed.StartsWith("\d ") Then Continue For
                If trimmed.StartsWith("\s ") OrElse trimmed.StartsWith("\s1 ") OrElse
                   trimmed.StartsWith("\s2 ") OrElse trimmed.StartsWith("\s3 ") Then Continue For
                sb.AppendLine(line)
            Next
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Strip \f...\f* footnotes and \x...\x* cross-references from raw USFM text
        ''' before parsing, to work around USFMToolsSharp nested marker issues.
        ''' </summary>
        Private Shared Function StripNotesFromRaw(usfm As String) As String
            Dim sb As New StringBuilder(usfm.Length)
            Dim i = 0
            While i < usfm.Length
                If i + 2 < usfm.Length AndAlso usfm(i) = "\"c Then
                    Dim tag = usfm(i + 1)
                    Dim after = usfm(i + 2)
                    ' Match \f or \x followed by space or +
                    If (tag = "f"c OrElse tag = "x"c) AndAlso (after = " "c OrElse after = "+"c) Then
                        Dim closer = "\" & tag & "*"
                        Dim endIdx = usfm.IndexOf(closer, i + 3)
                        If endIdx >= 0 Then
                            i = endIdx + closer.Length
                            Continue While
                        End If
                    End If
                End If
                sb.Append(usfm(i))
                i += 1
            End While
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Verify a converted Bible database for integrity issues.
        ''' Returns a list of human-readable issue strings (empty if no issues).
        ''' </summary>
        Public Shared Function VerifyDatabase(dbPath As String) As List(Of String)
            Dim issues As New List(Of String)()
            Try
                Dim connStr = New SqliteConnectionStringBuilder() With {
                    .DataSource = dbPath,
                    .Mode = SqliteOpenMode.ReadOnly
                }.ToString()

                Using conn As New SqliteConnection(connStr)
                    conn.Open()

                    ' Count distinct books
                    Dim bookCount = 0
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT COUNT(*) FROM books"
                        bookCount = CInt(cmd.ExecuteScalar())
                    End Using

                    If bookCount < 27 Then
                        issues.Add($"Only {bookCount} books found (expected at least 27 for NT)")
                    ElseIf bookCount < 66 Then
                        issues.Add($"{bookCount} books (expected 66 for full Bible)")
                    End If

                    ' Check for empty books and missing chapters
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText =
                            "SELECT b.short_name, b.book_number, " &
                            "  (SELECT MAX(v.chapter) FROM verses v WHERE v.book_number = b.book_number) AS max_ch, " &
                            "  (SELECT COUNT(*) FROM verses v WHERE v.book_number = b.book_number) AS verse_count " &
                            "FROM books b ORDER BY b.book_number"
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                Dim shortName = reader.GetString(0)
                                Dim bookNum = reader.GetInt32(1)
                                Dim verseCount = If(reader.IsDBNull(3), 0, reader.GetInt32(3))
                                Dim maxCh = If(reader.IsDBNull(2), 0, reader.GetInt32(2))

                                If verseCount = 0 Then
                                    issues.Add($"{shortName}: no verses at all")
                                ElseIf maxCh > 1 Then
                                    ' Check for chapter gaps
                                    Using cmd2 = conn.CreateCommand()
                                        cmd2.CommandText = "SELECT DISTINCT chapter FROM verses WHERE book_number = @bn ORDER BY chapter"
                                        cmd2.Parameters.AddWithValue("@bn", bookNum)
                                        Dim chapters As New List(Of Integer)()
                                        Using r2 = cmd2.ExecuteReader()
                                            While r2.Read()
                                                chapters.Add(r2.GetInt32(0))
                                            End While
                                        End Using
                                        Dim missing = Enumerable.Range(1, maxCh).Except(chapters).ToList()
                                        If missing.Count > 0 Then
                                            issues.Add($"{shortName}: missing chapter(s) {String.Join(", ", missing.Take(10))}")
                                        End If
                                    End Using
                                End If
                            End While
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                issues.Add($"Verification error: {ex.Message}")
            End Try
            Return issues
        End Function

        ''' <summary>Collapse multiple whitespace characters into a single space.</summary>
        Private Shared Function CollapseWhitespace(text As String) As String
            If String.IsNullOrEmpty(text) Then Return ""
            Dim sb As New StringBuilder(text.Length)
            Dim lastWasSpace = True ' treat start as space to trim leading
            For Each c In text
                If Char.IsWhiteSpace(c) Then
                    If Not lastWasSpace Then
                        sb.Append(" "c)
                        lastWasSpace = True
                    End If
                Else
                    sb.Append(c)
                    lastWasSpace = False
                End If
            Next
            ' Trim trailing space
            If sb.Length > 0 AndAlso sb(sb.Length - 1) = " "c Then
                sb.Length -= 1
            End If
            Return sb.ToString()
        End Function

    End Class

End Namespace
