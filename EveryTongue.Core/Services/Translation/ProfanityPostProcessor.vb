Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports EveryTongue.Services.Infrastructure

Namespace Services.Translation
    ''' <summary>
    ''' Local profanity post-processing for translation backends that do NOT apply
    ''' filters themselves (cloud backends — the local NLLB sidecar applies the
    ''' profanity filter inside translate-server/server.py).
    '''
    ''' This is a faithful port of _compile_profanity_file/_filter_profanity in
    ''' translate-server/server.py — the semantics MUST stay identical so that
    ''' NLLB and cloud outputs receive the same scrubbing:
    '''   - profanity file format: {"&lt;target FLORES&gt;": [items]} where each item is
    '''     either a plain string (legacy, always enabled) or {word, enabled}
    '''   - all enabled words of a language compile into ONE case-insensitive
    '''     word-boundary regex: \b(word1|word2|...)\b
    '''   - matches in the TRANSLATED text are replaced with "[...]"
    '''   - a language with no enabled words has no pattern (no-op)
    '''
    ''' Files are cached per path keyed on last-write time (same idea as the
    ''' Python _filter_set_cache). A broken/missing per-request file falls back
    ''' to the global profanity file, mirroring _get_profanity_patterns().
    ''' </summary>
    Public NotInheritable Class ProfanityPostProcessor

        Private Sub New()
        End Sub

        Private Class CacheEntry
            Public Property LastWriteUtc As DateTime
            ''' <summary>Parsed data, or Nothing when the file failed to parse (cached negative).</summary>
            Public Property Data As Dictionary(Of String, Regex)
        End Class

        ' path → (mtime, compiled patterns). SyncLock-guarded; negative results are
        ' cached against the same mtime so a broken file is only logged once per edit.
        Private Shared ReadOnly _cache As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly _cacheLock As New Object()

        ''' <summary>
        ''' Apply the profanity filter to a set of translations produced by a cloud
        ''' backend. <paramref name="profanityPath"/> is the per-request filter set
        ''' ("" = none); a missing/broken per-request file falls back to
        ''' <paramref name="globalProfanityPath"/>, exactly like the sidecar's
        ''' _get_profanity_patterns(). Returns the (possibly scrubbed) translations
        ''' dictionary; never throws.
        ''' </summary>
        Public Shared Function Apply(translations As Dictionary(Of String, String),
                                     profanityPath As String,
                                     globalProfanityPath As String) As Dictionary(Of String, String)
            If translations Is Nothing OrElse translations.Count = 0 Then Return translations

            Try
                Dim patterns = ResolvePatterns(profanityPath, globalProfanityPath)
                If patterns Is Nothing OrElse patterns.Count = 0 Then Return translations

                Dim result As New Dictionary(Of String, String)(translations.Count, translations.Comparer)
                For Each kvp In translations
                    Dim filteredText = ApplyToText(patterns, kvp.Key, kvp.Value)
                    If Not String.Equals(filteredText, kvp.Value, StringComparison.Ordinal) Then
                        AppLogger.Log(LogEvents.TRANS_RESULT,
                            $"[PROFANITY] {kvp.Key}: '{kvp.Value}' -> '{filteredText}'")
                    End If
                    result(kvp.Key) = filteredText
                Next
                Return result
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"ProfanityPostProcessor.Apply failed: {ex.Message}")
                Return translations
            End Try
        End Function

        ''' <summary>
        ''' Scrub one translated string. Mirrors _filter_profanity() in
        ''' translate-server/server.py: replace matches with "[...]".
        ''' </summary>
        Private Shared Function ApplyToText(patterns As Dictionary(Of String, Regex),
                                            targetLang As String,
                                            translated As String) As String
            If String.IsNullOrEmpty(translated) Then Return translated
            Dim pattern As Regex = Nothing
            If Not patterns.TryGetValue(If(targetLang, ""), pattern) OrElse pattern Is Nothing Then
                Return translated
            End If
            Return pattern.Replace(translated, "[...]")
        End Function

        ''' <summary>
        ''' The patterns for a request: the named set at <paramref name="requestPath"/>
        ''' when it loads, else the global one. Nothing = no profanity file (no-op).
        ''' </summary>
        Private Shared Function ResolvePatterns(requestPath As String, globalPath As String
        ) As Dictionary(Of String, Regex)
            If Not String.IsNullOrEmpty(requestPath) Then
                Dim named = GetCached(requestPath)
                If named IsNot Nothing Then Return named
            End If
            If String.IsNullOrEmpty(globalPath) Then Return Nothing
            Return GetCached(globalPath)
        End Function

        ''' <summary>Load a profanity file through the mtime-keyed cache.</summary>
        Private Shared Function GetCached(path As String) As Dictionary(Of String, Regex)
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

                Dim data As Dictionary(Of String, Regex) = Nothing
                Try
                    data = ParseFile(path)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR, $"Profanity load failed for {path}: {ex.Message}")
                    data = Nothing
                End Try
                _cache(path) = New CacheEntry With {.LastWriteUtc = mtime, .Data = data}
                Return data
            End SyncLock
        End Function

        ''' <summary>
        ''' Parse a profanity.json file into compiled per-language patterns. Same
        ''' semantics as the Python _compile_profanity_file: plain strings are
        ''' always enabled (legacy), objects honour the enabled flag, empty words
        ''' are dropped, and languages with no words get no pattern.
        ''' </summary>
        Private Shared Function ParseFile(path As String) As Dictionary(Of String, Regex)
            Dim result As New Dictionary(Of String, Regex)(StringComparer.Ordinal)
            Dim totalWords = 0

            Using doc = JsonDocument.Parse(File.ReadAllText(path))
                For Each langProp In doc.RootElement.EnumerateObject()
                    Dim words As New List(Of String)()
                    For Each elem In langProp.Value.EnumerateArray()
                        If elem.ValueKind = JsonValueKind.String Then
                            ' Legacy format: plain string (always enabled)
                            words.Add(elem.GetString())
                        ElseIf elem.ValueKind = JsonValueKind.Object Then
                            ' New format: {"word": "...", "enabled": true/false}
                            Dim enabledEl As JsonElement
                            If elem.TryGetProperty("enabled", enabledEl) AndAlso
                               enabledEl.ValueKind = JsonValueKind.False Then
                                Continue For
                            End If
                            Dim wordEl As JsonElement
                            If elem.TryGetProperty("word", wordEl) AndAlso
                               wordEl.ValueKind = JsonValueKind.String Then
                                words.Add(wordEl.GetString())
                            End If
                        End If
                    Next
                    words.RemoveAll(Function(w) String.IsNullOrEmpty(w))
                    If words.Count > 0 Then
                        Dim pattern = "\b(" & String.Join("|", words.Select(AddressOf Regex.Escape)) & ")\b"
                        result(langProp.Name) = New Regex(pattern,
                            RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant Or RegexOptions.Compiled)
                        totalWords += words.Count
                    End If
                Next
            End Using

            AppLogger.Log(LogEvents.TRANS_RESULT,
                $"[PROFANITY] loaded {totalWords} words across {result.Count} languages from {path} (cloud post-processing)")
            Return result
        End Function
    End Class
End Namespace
