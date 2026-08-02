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
            ''' <summary>Name came ONLY from short_name entries (an abbreviation
            ''' like "Gèn"). Live caption detection skips these — nobody SAYS an
            ''' abbreviation; in spoken text they only arise from STT garbles
            ''' ("Amb disset anys" → "Am 10 set anys" false-fired Amos 10).</summary>
            Public Property Abbreviation As Boolean
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

        ''' <summary>
        ''' Ordinal words DERIVED from the Bibles: sibling books (1/2 Samuel,
        ''' 1/2/3 John — the groups are numbering-scheme facts) are named
        ''' identically except for the ordinal ("Primera carta de Joan" /
        ''' "Segona carta de Joan") — diffing sibling names within each Bible
        ''' yields that language's ordinal vocabulary. No word list anywhere.
        ''' </summary>
        Private ReadOnly _ordinalWords As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>Numbered-sibling groups of the book-numbering scheme (protocol facts).</summary>
        Private Shared ReadOnly SiblingGroups As Integer()() = {
            New Integer() {90, 100}, New Integer() {110, 120}, New Integer() {130, 140},
            New Integer() {530, 540}, New Integer() {590, 600}, New Integer() {610, 620},
            New Integer() {670, 680}, New Integer() {690, 700, 710}}

        Public Function TryOrdinalWord(word As String, ByRef n As Integer) As Boolean
            Return _ordinalWords.TryGetValue(Fold(word), n)
        End Function

        Public Function OrdinalLookup(n As Integer, baseWord As String) As AliasInfo
            Dim num = 0
            If _ordinalPairs.TryGetValue($"{n}|{Fold(baseWord)}", num) Then
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
            If _aliases.TryGetValue(FoldName(name), info) Then Return info
            Return Nothing
        End Function

        ''' <summary>Normalise an alias/matched name: trim, drop trailing dots, collapse spaces.</summary>
        Public Shared Function NormName(s As String) As String
            Dim t = If(s, "").Trim().TrimEnd("."c)
            Return System.Text.RegularExpressions.Regex.Replace(t, "\s+", " ")
        End Function

        ''' <summary>
        ''' Accent-and-case fold for spoken-text comparison: STT drops accents
        ''' unpredictably ("versiculo", "Exodo"), so every dictionary in the
        ''' spoken-detection layer keys and probes through this. Unicode
        ''' decomposition + strip combining marks — no language knowledge.
        ''' </summary>
        Public Shared Function Fold(s As String) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Dim d = s.Normalize(System.Text.NormalizationForm.FormD)
            Dim sb As New System.Text.StringBuilder(d.Length)
            For Each ch In d
                If Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) <> Globalization.UnicodeCategory.NonSpacingMark Then sb.Append(ch)
            Next
            Return sb.ToString().Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant()
        End Function

        ''' <summary>Fold applied on top of NormName — the canonical key form for alias dictionaries.</summary>
        Public Shared Function FoldName(s As String) As String
            Return Fold(NormName(s))
        End Function

        ' ── cache plumbing ──────────────────────────────────────────────

        Private Class CacheEntry
            Public Property Size As Long
            Public Property MTimeTicks As Long
            Public Property Aliases As Dictionary(Of String, Integer)
            Public Property Freq As Dictionary(Of String, Integer)
            ''' <summary>Aliases that came ONLY from short_name (abbreviations).</summary>
            Public Property AbbrevNames As List(Of String)
            ''' <summary>book_number → highest chapter in this Bible (impossible-reference guard).</summary>
            Public Property MaxChapters As Dictionary(Of Integer, Integer)
            ''' <summary>Locale-supplied spoken book words (folded) whose verse-text
            ''' frequency this scan counted — a cache entry missing any currently
            ''' requested word is stale (also gates the accent-fold migration).</summary>
            Public Property ExtraTargets As List(Of String)
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

            Dim idx As New BookAliasIndex()

            ' LOCALE-FILE words FIRST (the sanctioned per-language channel —
            ' translated with each language pack, never hardcoded), because the
            ' spoken book names feed the Bible scan below as frequency targets:
            '   Bible_Ordinals        = "primera:1,segona:2,..." → ordinal words
            '   Bible_SpokenBookNames = "salmo:230,..."          → spoken forms the
            '     Bibles' own books tables don't teach (titles are plural "Salmos";
            '     preachers say the singular). App locales + user overlay dir.
            Dim localeBookWords As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
            Dim localeDirs As New List(Of String) From {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales")}
            Try
                localeDirs.Add(Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory, "locales"))
            Catch
                ' Config dir unavailable (unit-test host) — app locales still load.
            End Try
            For Each dirPath In localeDirs
                Try
                    If Not Directory.Exists(dirPath) Then Continue For
                    For Each lf In Directory.GetFiles(dirPath, "*.json")
                        Try
                            Using doc = JsonDocument.Parse(File.ReadAllText(lf))
                                Dim el As JsonElement = Nothing
                                If doc.RootElement.TryGetProperty("Bible_Ordinals", el) AndAlso
                                   el.ValueKind = JsonValueKind.String Then
                                    For Each pair In el.GetString().Split(","c)
                                        Dim bits = pair.Split(":"c)
                                        Dim n = 0
                                        If bits.Length = 2 AndAlso Integer.TryParse(bits(1).Trim(), n) Then
                                            idx._ordinalWords(Fold(bits(0).Trim())) = n
                                        End If
                                    Next
                                End If
                                If doc.RootElement.TryGetProperty("Bible_SpokenBookNames", el) AndAlso
                                   el.ValueKind = JsonValueKind.String Then
                                    For Each pair In el.GetString().Split(","c)
                                        Dim bits = pair.Split(":"c)
                                        Dim n = 0
                                        If bits.Length = 2 AndAlso Integer.TryParse(bits(1).Trim(), n) Then
                                            Dim w = FoldName(bits(0))
                                            If w.Length >= 3 Then localeBookWords(w) = n
                                        End If
                                    Next
                                End If
                            End Using
                        Catch
                            ' Per-file parse is best-effort — a bad entry skips, index still builds.
                        End Try
                    Next
                Catch
                    ' Locale dir walk is best-effort — the index works with what loaded.
                End Try
            Next

            Dim live As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
            Dim cacheDirty = False
            For Each dbPath In dbPaths
                Try
                    Dim fi As New FileInfo(dbPath)
                    Dim entry As CacheEntry = Nothing
                    If cache.TryGetValue(dbPath, entry) AndAlso entry IsNot Nothing AndAlso
                       entry.Size = fi.Length AndAlso entry.MTimeTicks = fi.LastWriteTimeUtc.Ticks AndAlso
                       entry.AbbrevNames IsNot Nothing AndAlso entry.MaxChapters IsNot Nothing AndAlso
                       entry.ExtraTargets IsNot Nothing AndAlso
                       localeBookWords.Keys.All(Function(w) entry.ExtraTargets.Contains(w, StringComparer.Ordinal)) Then
                        ' ExtraTargets check doubles as the fold-migration gate: old caches
                        ' (unfolded Freq keys, no locale-word counts) rescan exactly once.
                        live(dbPath) = entry
                    Else
                        live(dbPath) = ScanBible(dbPath, fi, localeBookWords.Keys)
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

            ' Derive each language's ORDINAL WORDS by diffing sibling-book
            ' names within each Bible ("Primera carta de Joan" vs "Segona
            ' carta de Joan" → primera=1, segona=2).
            For Each entry In live.Values
                If entry?.Aliases Is Nothing Then Continue For
                Dim byNum = entry.Aliases.GroupBy(Function(k) k.Value).
                    ToDictionary(Function(g) g.Key,
                                 Function(g) g.Select(Function(k) NormName(k.Key)).ToList())
                For Each grp In SiblingGroups
                    For i = 0 To grp.Length - 1
                        For j = 0 To grp.Length - 1
                            If i = j Then Continue For
                            Dim aNames As List(Of String) = Nothing, bNames As List(Of String) = Nothing
                            If Not byNum.TryGetValue(grp(i), aNames) OrElse
                               Not byNum.TryGetValue(grp(j), bNames) Then Continue For
                            For Each an In aNames
                                Dim at = an.Split(" "c)
                                For Each bn In bNames
                                    Dim bt = bn.Split(" "c)
                                    If at.Length <> bt.Length OrElse at.Length < 2 Then Continue For
                                    Dim diffIdx = -1, diffs = 0
                                    For t = 0 To at.Length - 1
                                        If Not at(t).Equals(bt(t), StringComparison.OrdinalIgnoreCase) Then
                                            diffs += 1 : diffIdx = t
                                        End If
                                    Next
                                    If diffs = 1 Then
                                        Dim w = at(diffIdx)
                                        If w.Length >= 2 AndAlso Not Char.IsDigit(w(0)) Then
                                            idx._ordinalWords(Fold(w)) = i + 1
                                        End If
                                    End If
                                Next
                            Next
                        Next
                    Next
                Next
            Next

            ' Union into one index. Ambiguity: ANY Bible reporting the word as
            ' frequent marks it ambiguous (conservative). Digit-prefixed and
            ' multi-word names are inherently safe.
            For Each entry In live.Values
                If entry?.Aliases Is Nothing Then Continue For
                Dim entryAbbrevs As New HashSet(Of String)(
                    If(entry.AbbrevNames, New List(Of String)).Select(AddressOf Fold), StringComparer.Ordinal)
                For Each kvp In entry.Aliases
                    Dim name = FoldName(kvp.Key)
                    If name.Length < 3 Then Continue For ' "Sl"/"Mt" abbreviations: high false-positive risk, never spoken
                    Dim isAbbrev = entryAbbrevs.Contains(name)
                    Dim singleWord = Not name.Contains(" "c)
                    Dim ambiguous = False
                    If singleWord AndAlso Not Char.IsDigit(name(0)) Then
                        For Each e2 In live.Values
                            Dim c = 0
                            If e2?.Freq IsNot Nothing AndAlso e2.Freq.TryGetValue(name, c) AndAlso c >= AmbigMinCount Then
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
                    ElseIf toks.Length >= 2 AndAlso idx._ordinalWords.TryGetValue(toks(0), ordN) Then
                        ' ordN set by TryGetValue (derived ordinal word)
                    End If
                    If ordN >= 1 AndAlso ordN <= 3 Then
                        Dim baseTok = toks.Last()
                        If baseTok.Length >= 3 Then idx._ordinalPairs($"{ordN}|{baseTok}") = kvp.Value
                    End If

                    Dim existing As AliasInfo = Nothing
                    If idx._aliases.TryGetValue(name, existing) Then
                        ' Same name in two Bibles: same number is the norm (the
                        ' scheme guarantees it); on conflict keep the first and
                        ' stay ambiguous-if-either. Abbreviation only sticks if
                        ' EVERY Bible knows the name solely as a short_name.
                        existing.Ambiguous = existing.Ambiguous OrElse ambiguous
                        existing.Abbreviation = existing.Abbreviation AndAlso isAbbrev
                    Else
                        idx._aliases(name) = New AliasInfo With {.BookNumber = kvp.Value, .Ambiguous = ambiguous, .Abbreviation = isAbbrev}
                    End If
                Next
                ' Union max chapters (max across Bibles — permissive: a reference
                ' is possible if ANY installed Bible has that chapter).
                If entry.MaxChapters IsNot Nothing Then
                    For Each mc In entry.MaxChapters
                        Dim cur = 0
                        idx._maxChapter.TryGetValue(mc.Key, cur)
                        idx._maxChapter(mc.Key) = Math.Max(cur, mc.Value)
                    Next
                End If
            Next

            ' Inject the locale-file SPOKEN book names last. These are DELIBERATE
            ' operator data (same trust level as Bible_Ordinals): the entry
            ' asserts "this word, spoken, means this book" — so they are never
            ' ambiguous, and they OVERRIDE a same-named Bible-title entry's
            ' ambiguity (English "Psalm" exists as a title, but Wycliffe's verse
            ' text is full of lowercase "psalm", which wrongly demoted the
            ' announcement form "Psalm 23" to needs-more-evidence).
            For Each lb In localeBookWords
                Dim existing As AliasInfo = Nothing
                If idx._aliases.TryGetValue(lb.Key, existing) Then
                    If existing.BookNumber = lb.Value Then
                        existing.Ambiguous = False
                        existing.Abbreviation = False
                    End If
                    Continue For
                End If
                idx._aliases(lb.Key) = New AliasInfo With {.BookNumber = lb.Value, .Ambiguous = False, .Abbreviation = False}
            Next
            Return idx
        End Function

        Private ReadOnly _maxChapter As New Dictionary(Of Integer, Integer)

        ''' <summary>Highest chapter of a book across the installed Bibles (0 = unknown, skip validation).</summary>
        Public Function MaxChapter(bookNumber As Integer) As Integer
            Dim n = 0
            _maxChapter.TryGetValue(bookNumber, n)
            Return n
        End Function

        ''' <summary>One-time scan of a Bible: books table + verse-text word frequencies
        ''' for its book-name words plus the locale-supplied spoken book words.
        ''' Freq keys are accent-folded (matching probe-side folding).</summary>
        Private Shared Function ScanBible(dbPath As String, fi As FileInfo,
                                          extraTargets As IEnumerable(Of String)) As CacheEntry
            Dim entry As New CacheEntry With {
                .Size = fi.Length, .MTimeTicks = fi.LastWriteTimeUtc.Ticks,
                .Aliases = New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase),
                .Freq = New Dictionary(Of String, Integer)(),
                .ExtraTargets = extraTargets.ToList()
            }
            Using conn As New SqliteConnection(New SqliteConnectionStringBuilder() With {
                .DataSource = dbPath, .Mode = SqliteOpenMode.ReadOnly}.ToString())
                conn.Open()
                Dim shortNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Dim longNames As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT short_name, long_name, book_number FROM books"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim bookNum = reader.GetInt32(2)
                            For col = 0 To 1
                                Dim name = NormName(reader.GetString(col))
                                If name.Length >= 3 Then
                                    entry.Aliases(name) = bookNum
                                    If col = 0 Then shortNames.Add(name) Else longNames.Add(name)
                                End If
                            Next
                        End While
                    End Using
                End Using
                ' Abbreviation = appears only as a short_name, never as a full name.
                entry.AbbrevNames = shortNames.Where(Function(n) Not longNames.Contains(n)).ToList()

                ' Highest chapter per book — the impossible-reference guard
                ' ("Am 10": Amos has 9 chapters) derives from the Bible itself.
                entry.MaxChapters = New Dictionary(Of Integer, Integer)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT book_number, MAX(chapter) FROM verses GROUP BY book_number"
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            If Not reader.IsDBNull(1) Then entry.MaxChapters(reader.GetInt32(0)) = reader.GetInt32(1)
                        End While
                    End Using
                End Using

                ' Frequency pass: count the single-word book names PLUS the
                ' locale-supplied spoken words, as lowercase standalone words in
                ' the verse text. Folded keys so accent variants pool together.
                Dim targets As New HashSet(Of String)(
                    entry.Aliases.Keys.Where(Function(n) Not n.Contains(" "c)).
                        Select(AddressOf Fold), StringComparer.Ordinal)
                targets.UnionWith(entry.ExtraTargets.Where(Function(t) Not t.Contains(" "c)))
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
                                        Dim wl = Fold(w)
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
