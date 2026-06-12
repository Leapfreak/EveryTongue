Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports EveryTongue.Services.Infrastructure

Namespace Services.Translation
    ''' <summary>
    ''' Local glossary post-processing for translation backends that do NOT apply
    ''' filters themselves (cloud backends — the local NLLB sidecar applies the
    ''' glossary inside translate-server/server.py).
    '''
    ''' This is a faithful port of the Python Glossary class in
    ''' translate-server/server.py — the semantics MUST stay identical so that
    ''' NLLB and cloud outputs receive the same fixups:
    '''   - glossary file format: {"&lt;source FLORES&gt;": [{trigger, comment, enabled, fixes}, ...]}
    '''   - trigger: case-insensitive SUBSTRING match against the SOURCE text
    '''   - fixes: {target FLORES: {wrong: right, ...}} applied to the TRANSLATED text
    '''   - wrong containing punctuation (any non-word, non-space char): plain
    '''     case-sensitive replace first, else case-insensitive literal regex replace
    '''   - wrong without punctuation: case-insensitive word-boundary regex replace
    '''   - entries with enabled=false (or without a trigger property) are skipped
    '''
    ''' Files are cached per path keyed on last-write time (same idea as the
    ''' Python _filter_set_cache). A broken/missing per-request file falls back
    ''' to the global glossary, mirroring _get_glossary().
    ''' </summary>
    Public NotInheritable Class GlossaryPostProcessor

        Private Sub New()
        End Sub

        ''' <summary>One glossary entry: trigger + per-target-language ordered fix list.</summary>
        Private Class GlossaryEntry
            Public Property Trigger As String = ""
            ''' <summary>target FLORES code → ordered (wrong, right) pairs (JSON order preserved).</summary>
            Public ReadOnly Property Fixes As New Dictionary(Of String, List(Of KeyValuePair(Of String, String)))(StringComparer.Ordinal)
        End Class

        Private Class CacheEntry
            Public Property LastWriteUtc As DateTime
            ''' <summary>Parsed data, or Nothing when the file failed to parse (cached negative).</summary>
            Public Property Data As Dictionary(Of String, List(Of GlossaryEntry))
        End Class

        ' path → (mtime, parsed glossary). SyncLock-guarded; negative results are
        ' cached against the same mtime so a broken file is only logged once per edit.
        Private Shared ReadOnly _cache As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly _cacheLock As New Object()

        ''' <summary>
        ''' Apply glossary fixes to a set of translations produced by a cloud backend.
        ''' <paramref name="glossaryPath"/> is the per-request filter set ("" = none);
        ''' a missing/broken per-request file falls back to <paramref name="globalGlossaryPath"/>,
        ''' exactly like the sidecar's _get_glossary(). Returns the (possibly fixed)
        ''' translations dictionary; never throws.
        ''' </summary>
        Public Shared Function Apply(sourceText As String,
                                     sourceLang As String,
                                     translations As Dictionary(Of String, String),
                                     glossaryPath As String,
                                     globalGlossaryPath As String) As Dictionary(Of String, String)
            If translations Is Nothing OrElse translations.Count = 0 Then Return translations

            Try
                Dim data = ResolveGlossary(glossaryPath, globalGlossaryPath)
                If data Is Nothing Then Return translations

                Dim entries As List(Of GlossaryEntry) = Nothing
                If Not data.TryGetValue(If(sourceLang, ""), entries) OrElse entries.Count = 0 Then
                    Return translations
                End If

                Dim result As New Dictionary(Of String, String)(translations.Count, translations.Comparer)
                For Each kvp In translations
                    Dim fixedText = ApplyToText(sourceText, entries, kvp.Key, kvp.Value)
                    If Not String.Equals(fixedText, kvp.Value, StringComparison.Ordinal) Then
                        AppLogger.Log(LogEvents.TRANS_RESULT,
                            $"[GLOSSARY] {kvp.Key}: '{kvp.Value}' -> '{fixedText}'")
                    End If
                    result(kvp.Key) = fixedText
                Next
                Return result
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"GlossaryPostProcessor.Apply failed: {ex.Message}")
                Return translations
            End Try
        End Function

        ''' <summary>
        ''' Apply the entries of one source language to a single translated string.
        ''' Mirrors Glossary.apply() in translate-server/server.py line for line.
        ''' </summary>
        Private Shared Function ApplyToText(sourceText As String,
                                            entries As List(Of GlossaryEntry),
                                            targetLang As String,
                                            translated As String) As String
            If String.IsNullOrEmpty(translated) Then Return translated

            Dim sourceLower = If(sourceText, "").ToLowerInvariant()
            Dim result = translated

            For Each entry In entries
                If String.IsNullOrEmpty(entry.Trigger) Then Continue For

                ' Trigger: case-insensitive substring match against the SOURCE text
                If Not sourceLower.Contains(entry.Trigger.ToLowerInvariant()) Then Continue For

                Dim fixes As List(Of KeyValuePair(Of String, String)) = Nothing
                If Not entry.Fixes.TryGetValue(targetLang, fixes) Then Continue For

                For Each fixPair In fixes
                    Dim wrong = fixPair.Key
                    Dim right = fixPair.Value
                    If String.IsNullOrEmpty(wrong) Then Continue For
                    ' Regex.Replace treats $ specially in the replacement — Python's
                    ' replacement is effectively literal for plain text, so escape it.
                    Dim rightLiteral = right.Replace("$", "$$")

                    If Regex.IsMatch(wrong, "[^\w\s]") Then
                        ' Pattern contains punctuation/hyphens — \b won't match correctly.
                        If result.Contains(wrong) Then
                            result = result.Replace(wrong, right)
                        ElseIf result.ToLowerInvariant().Contains(wrong.ToLowerInvariant()) Then
                            result = Regex.Replace(result, Regex.Escape(wrong), rightLiteral,
                                                   RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)
                        End If
                    Else
                        ' Word-boundary replacement, case-insensitive
                        result = Regex.Replace(result, "\b" & Regex.Escape(wrong) & "\b", rightLiteral,
                                               RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)
                    End If
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' The glossary for a request: the named set at <paramref name="requestPath"/>
        ''' when it loads, else the global one. Nothing = no glossary available (no-op).
        ''' </summary>
        Private Shared Function ResolveGlossary(requestPath As String, globalPath As String
        ) As Dictionary(Of String, List(Of GlossaryEntry))
            If Not String.IsNullOrEmpty(requestPath) Then
                Dim named = GetCached(requestPath)
                If named IsNot Nothing Then Return named
            End If
            If String.IsNullOrEmpty(globalPath) Then Return Nothing
            Return GetCached(globalPath)
        End Function

        ''' <summary>Load a glossary file through the mtime-keyed cache.</summary>
        Private Shared Function GetCached(path As String) As Dictionary(Of String, List(Of GlossaryEntry))
            Dim mtime As DateTime
            Try
                If Not File.Exists(path) Then Return Nothing
                mtime = File.GetLastWriteTimeUtc(path)
            Catch
                Return Nothing
            End Try

            SyncLock _cacheLock
                Dim entry As CacheEntry = Nothing
                If _cache.TryGetValue(path, entry) AndAlso entry.LastWriteUtc = mtime Then
                    Return entry.Data
                End If

                Dim data As Dictionary(Of String, List(Of GlossaryEntry)) = Nothing
                Try
                    data = ParseFile(path)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR, $"Glossary load failed for {path}: {ex.Message}")
                    data = Nothing
                End Try
                _cache(path) = New CacheEntry With {.LastWriteUtc = mtime, .Data = data}
                Return data
            End SyncLock
        End Function

        ''' <summary>
        ''' Parse a glossary.json file. Same filtering as the Python loader:
        ''' keep entries that have a "trigger" property and enabled != false.
        ''' </summary>
        Private Shared Function ParseFile(path As String) As Dictionary(Of String, List(Of GlossaryEntry))
            Dim result As New Dictionary(Of String, List(Of GlossaryEntry))(StringComparer.Ordinal)
            Dim total = 0

            Using doc = JsonDocument.Parse(File.ReadAllText(path))
                For Each langProp In doc.RootElement.EnumerateObject()
                    Dim entries As New List(Of GlossaryEntry)()
                    For Each elem In langProp.Value.EnumerateArray()
                        Dim triggerEl As JsonElement
                        If Not elem.TryGetProperty("trigger", triggerEl) Then Continue For

                        Dim enabledEl As JsonElement
                        If elem.TryGetProperty("enabled", enabledEl) AndAlso
                           enabledEl.ValueKind = JsonValueKind.False Then
                            Continue For
                        End If

                        Dim entry As New GlossaryEntry With {
                            .Trigger = If(triggerEl.ValueKind = JsonValueKind.String, triggerEl.GetString(), "")
                        }

                        Dim fixesEl As JsonElement
                        If elem.TryGetProperty("fixes", fixesEl) AndAlso fixesEl.ValueKind = JsonValueKind.Object Then
                            For Each targetProp In fixesEl.EnumerateObject()
                                If targetProp.Value.ValueKind <> JsonValueKind.Object Then Continue For
                                Dim pairs As New List(Of KeyValuePair(Of String, String))()
                                For Each fixProp In targetProp.Value.EnumerateObject()
                                    pairs.Add(New KeyValuePair(Of String, String)(
                                        fixProp.Name,
                                        If(fixProp.Value.ValueKind = JsonValueKind.String, fixProp.Value.GetString(), "")))
                                Next
                                entry.Fixes(targetProp.Name) = pairs
                            Next
                        End If

                        entries.Add(entry)
                    Next
                    If entries.Count > 0 Then
                        result(langProp.Name) = entries
                        total += entries.Count
                    End If
                Next
            End Using

            AppLogger.Log(LogEvents.TRANS_RESULT,
                $"[GLOSSARY] loaded {total} entries across {result.Count} languages from {path} (cloud post-processing)")
            Return result
        End Function
    End Class
End Namespace
