Imports System.IO
Imports System.Text.Json
Imports Microsoft.Data.Sqlite

Namespace Services.Bible

    ''' <summary>
    ''' Book-name alias index DERIVED from the installed Bibles' own books tables
    ''' (short_name + long_name per translation, union across all installed
    ''' Bibles, keyed to the universal book_number). Replaces the static
    ''' English-only alias table as the primary source for scripture-reference
    ''' detection — install a Bible and its language's book names become
    ''' detectable; nothing is authored or maintained.
    '''
    ''' AMBIGUITY is derived too: a single-word book name that appears often as
    ''' an ordinary lowercase word in the Bibles' own verse text ("reis",
    ''' "job") is flagged ambiguous, and detection demands stronger evidence
    ''' for it (chapter:verse or an ordinal prefix). Computed, never curated.
    '''
    ''' Everything is CACHED per Bible file (size + mtime) in
    ''' &lt;config-dir&gt;\bible-alias-cache.json so steady-state startup cost is one
    ''' small JSON read; only new/changed Bibles are rescanned.
    ''' </summary>
    Public Class BookAliasIndex

        Public Class AliasInfo
            Public Property BookNumber As Integer
            Public Property Ambiguous As Boolean
        End Class

        ''' <summary>Lowercase standalone-word occurrences in verse text at/above this ⇒ ambiguous.</summary>
        Private Const AmbigMinCount As Integer = 12

        Private ReadOnly _aliases As New Dictionary(Of String, AliasInfo)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>
        ''' (ordinal, base word) → book number, derived from the Bibles' own
        ''' numbered/ordinal book names ("1 Juan" → (1,"juan"); BCI's
        ''' "Primera carta de Joan" → (1,"joan")). Lets spoken forms like
        ''' "Primera de Joan" resolve to the epistle, not the gospel.
        ''' </summary>
        Private ReadOnly _ordinalPairs As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>Ordinal words → digit. Grammar constants, not language data.</summary>
        Public Shared ReadOnly OrdinalWords As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase) From {
            {"primera", 1}, {"primer", 1}, {"primero", 1}, {"first", 1}, {"erste", 1}, {"1a", 1}, {"1r", 1},
            {"segona", 2}, {"segon", 2}, {"segunda", 2}, {"segundo", 2}, {"second", 2}, {"zweite", 2}, {"2a", 2}, {"2n", 2},
            {"tercera", 3}, {"tercer", 3}, {"tercero", 3}, {"third", 3}, {"dritte", 3}, {"3a", 3}}

        Public Function OrdinalLookup(n As Integer, baseWord As String) As AliasInfo
            Dim num = 0
            If _ordinalPairs.TryGetValue($"{n}|{baseWord.ToLowerInvariant()}", num) Then
                Return New AliasInfo With {.BookNumber = num, .Ambiguous = False}
            End If
            Return Nothing
        End Function

        Public ReadOnly Property Count As Integer
            Get
                Return _aliases.Count
            End Get
        End Property

        Public ReadOnly Property AmbiguousCount As Integer
            Get
                Return _aliases.Values.Where(Function(a) a.Ambiguous).Count()
            End Get
        End Property

        Public Function Lookup(name As String) As AliasInfo
            Dim info As AliasInfo = Nothing
            If _aliases.TryGetValue(NormName(name), info) Then Return info
            Return Nothing
        End Function

        ''' <summary>Normalise an alias/matched name: trim, drop trailing dots, collapse spaces.</summary>
        Public Shared Function NormName(s As String) As String
            Dim t = If(s, "").Trim().TrimEnd("."c)
            Return System.Text.RegularExpressions.Regex.Replace(t, "\s+", " ")
        End Function

        ' ── cache plumbing ──────────────────────────────────────────────

        Private Class CacheEntry
            Public Property Size As Long
            Public Property MTimeTicks As Long
            Public Property Aliases As Dictionary(Of String, Integer)
            Public Property Freq As Dictionary(Of String, Integer)
        End Class

        Private Shared ReadOnly Property CachePath As String
            Get
                Return Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory, "bible-alias-cache.json")
            End Get
        End Property

        ''' <summary>
        ''' Build the index for the given Bible DB files. Cheap when cached
        ''' (JSON read); a new/changed Bible costs one books-table read plus one
        ''' verse-text frequency pass (~0.5s), after which it is cached.
        ''' </summary>
        Public Shared Function Build(dbPaths As IEnumerable(Of String)) As BookAliasIndex
            Dim cache As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
            Try
                If File.Exists(CachePath) Then
                    cache = JsonSerializer.Deserialize(Of Dictionary(Of String, CacheEntry))(
                        File.ReadAllText(CachePath))
                End If
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR,
                    $"bible-alias-cache.json unreadable ({ex.Message}) — rebuilding")
                cache = New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
            End Try

            Dim live As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
            Dim cacheDirty = False
            For Each dbPath In dbPaths
                Try
                    Dim fi As New FileInfo(dbPath)
                    Dim entry As CacheEntry = Nothing
                    If cache.TryGetValue(dbPath, entry) AndAlso entry IsNot Nothing AndAlso
                       entry.Size = fi.Length AndAlso entry.MTimeTicks = fi.LastWriteTimeUtc.Ticks Then
                        live(dbPath) = entry
                    Else
                        live(dbPath) = ScanBible(dbPath, fi)
                        cacheDirty = True
                    End If
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR,
                        $"BookAliasIndex: scan failed for '{dbPath}': {ex.Message}")
                End Try
            Next

            If cacheDirty OrElse cache.Count <> live.Count Then
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(CachePath))
                    File.WriteAllText(CachePath, JsonSerializer.Serialize(live))
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.BIBLE_ERROR,
                        $"BookAliasIndex: cache write failed: {ex.Message}")
                End Try
            End If

            ' Union into one index. Ambiguity: ANY Bible reporting the word as
            ' frequent marks it ambiguous (conservative). Digit-prefixed and
            ' multi-word names are inherently safe.
            Dim idx As New BookAliasIndex()
            For Each entry In live.Values
                If entry?.Aliases Is Nothing Then Continue For
                For Each kvp In entry.Aliases
                    Dim name = NormName(kvp.Key)
                    If name.Length < 3 Then Continue For ' "Sl"/"Mt" abbreviations: high false-positive risk, never spoken
                    Dim singleWord = Not name.Contains(" "c)
                    Dim ambiguous = False
                    If singleWord AndAlso Not Char.IsDigit(name(0)) Then
                        For Each e2 In live.Values
                            Dim c = 0
                            If e2?.Freq IsNot Nothing AndAlso e2.Freq.TryGetValue(name.ToLowerInvariant(), c) AndAlso c >= AmbigMinCount Then
                                ambiguous = True
                                Exit For
                            End If
                        Next
                    End If
                    ' Ordinal-pair derivation: an alias that starts with a digit
                    ' or ordinal word teaches us its (n, base) pairing.
                    Dim toks = name.Split(" "c)
                    Dim ordN = 0
                    If Char.IsDigit(name(0)) AndAlso toks.Length >= 2 Then
                        ordN = CInt(AscW(name(0))) - AscW("0"c)
                    ElseIf toks.Length >= 2 AndAlso OrdinalWords.TryGetValue(toks(0), ordN) Then
                        ' ordN set by TryGetValue
                    End If
                    If ordN >= 1 AndAlso ordN <= 3 Then
                        Dim baseTok = toks.Last().ToLowerInvariant()
                        If baseTok.Length >= 3 Then idx._ordinalPairs($"{ordN}|{baseTok}") = kvp.Value
                    End If

                    Dim existing As AliasInfo = Nothing
                    If idx._aliases.TryGetValue(name, existing) Then
                        ' Same name in two Bibles: same number is the norm (the
                        ' scheme guarantees it); on conflict keep the first and
                        ' stay ambiguous-if-either.
                        existing.Ambiguous = existing.Ambiguous OrElse ambiguous
                    Else
                        idx._aliases(name) = New AliasInfo With {.BookNumber = kvp.Value, .Ambiguous = ambiguous}
                    End If
                Next
            Next
            Return idx
        End Function

        ''' <summary>One-time scan of a Bible: books table + verse-text word frequencies for its book-name words.</summary>
        Private Shared Function ScanBible(dbPath As String, fi As FileInfo) As CacheEntry
            Dim entry As New CacheEntry With {
                .Size = fi.Length, .MTimeTicks = fi.LastWriteTimeUtc.Ticks,
                .Aliases = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase),
                .Freq = New Dictionary(Of String, Integer)()
            }
            Using conn As New SqliteConnection(New SqliteConnectionStringBuilder() With {
                .DataSource = dbPath, .Mode = SqliteOpenMode.ReadOnly}.ToString())
                conn.Open()
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT short_name, long_name, book_number FROM books"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim bookNum = reader.GetInt32(2)
                            For col = 0 To 1
                                Dim name = NormName(reader.GetString(col))
                                If name.Length >= 3 Then entry.Aliases(name) = bookNum
                            Next
                        End While
                    End Using
                End Using

                ' Frequency pass: count ONLY the words that are single-word book
                ' names, as lowercase standalone words in the verse text.
                Dim targets As New HashSet(Of String)(
                    entry.Aliases.Keys.Where(Function(n) Not n.Contains(" "c)).
                        Select(Function(n) n.ToLowerInvariant()))
                If targets.Count > 0 Then
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT text FROM verses"
                        Using reader = cmd.ExecuteReader()
                            Dim sep = " .,;:!?()[]{}""'«»—–-".ToCharArray()
                            While reader.Read()
                                If reader.IsDBNull(0) Then Continue While
                                For Each w In reader.GetString(0).Split(sep, StringSplitOptions.RemoveEmptyEntries)
                                    ' lowercase occurrences only — capitalized uses ARE the book/person
                                    If w.Length >= 3 AndAlso Char.IsLower(w(0)) Then
                                        Dim wl = w.ToLowerInvariant()
                                        If targets.Contains(wl) Then
                                            Dim c = 0
                                            entry.Freq.TryGetValue(wl, c)
                                            entry.Freq(wl) = c + 1
                                        End If
                                    End If
                                Next
                            End While
                        End Using
                    End Using
                End If
            End Using
            Return entry
        End Function

    End Class

End Namespace
