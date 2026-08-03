Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Text.RegularExpressions
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Translation
    ''' <summary>
    ''' Loads translate-server\terminology.json (the translation-side analogue of the
    ''' STT additional_vocab layer) and selects the entries relevant to one source
    ''' sentence. Context-capable backends enforce the selected renderings — DeepL via
    ''' custom_instructions, future LLM backends via a prompt glossary section.
    '''
    ''' File format: {"entries":[{"term":"gomet","matchMode":"WholeWord",
    '''                           "translations":{"eng_Latn":"sticker","spa_Latn":"pegatina"}}]}
    '''
    ''' Files are cached per path keyed on last-write time (same pattern as
    ''' GlossaryPostProcessor); a broken file caches the negative against the same
    ''' mtime so it is logged once per edit, and selection never throws.
    ''' </summary>
    Public NotInheritable Class TerminologyStore

        Private Sub New()
        End Sub

        ''' <summary>DeepL custom_instructions cap — also a sane prompt-glossary bound.</summary>
        Public Const MaxSelected As Integer = 10

        Private Class TerminologyFile
            Public Property Entries As New List(Of TerminologyEntry)()
        End Class

        Private Class CacheEntry
            Public Property LastWriteUtc As DateTime
            ''' <summary>Parsed entries, or Nothing when the file failed to parse (cached negative).</summary>
            Public Property Data As List(Of TerminologyEntry)
        End Class

        Private Shared ReadOnly _cache As New Dictionary(Of String, CacheEntry)(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly _cacheLock As New Object()

        Private Shared ReadOnly _jsonOptions As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True
        }

        Shared Sub New()
            _jsonOptions.Converters.Add(New JsonStringEnumConverter())
        End Sub

        ''' <summary>
        ''' Entries whose Term occurs in <paramref name="sentenceText"/> (case-insensitive;
        ''' WholeWord uses word boundaries so "gomet" also matches "gomets" is NOT assumed —
        ''' plural forms need their own entry or Substring mode). Returns an empty list on
        ''' missing/broken file. Capped at <see cref="MaxSelected"/>.
        ''' </summary>
        Public Shared Function SelectRelevant(sentenceText As String, path As String) As List(Of TerminologyEntry)
            Dim result As New List(Of TerminologyEntry)()
            If String.IsNullOrWhiteSpace(sentenceText) OrElse String.IsNullOrWhiteSpace(path) Then Return result

            Dim entries = LoadEntries(path)
            If entries Is Nothing OrElse entries.Count = 0 Then Return result

            For Each entry In entries
                If String.IsNullOrWhiteSpace(entry.Term) Then Continue For
                Dim matched As Boolean
                If entry.MatchMode = TerminologyMatchMode.Substring Then
                    matched = sentenceText.IndexOf(entry.Term, StringComparison.OrdinalIgnoreCase) >= 0
                Else
                    matched = Regex.IsMatch(sentenceText, "\b" & Regex.Escape(entry.Term) & "\b",
                                            RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)
                End If
                If matched Then
                    result.Add(entry)
                    If result.Count >= MaxSelected Then Exit For
                End If
            Next
            Return result
        End Function

        Private Shared Function LoadEntries(path As String) As List(Of TerminologyEntry)
            Try
                If Not File.Exists(path) Then Return Nothing
                Dim mtime = File.GetLastWriteTimeUtc(path)

                SyncLock _cacheLock
                    Dim cached As CacheEntry = Nothing
                    If _cache.TryGetValue(path, cached) AndAlso cached.LastWriteUtc = mtime Then
                        Return cached.Data
                    End If

                    Dim data As List(Of TerminologyEntry) = Nothing
                    Try
                        Dim parsed = JsonSerializer.Deserialize(Of TerminologyFile)(File.ReadAllText(path), _jsonOptions)
                        data = If(parsed?.Entries, New List(Of TerminologyEntry)())
                    Catch ex As Exception
                        ' Cached negative: logged once per edit, selection stays empty.
                        AppLogger.Log(LogEvents.TRANS_CONTEXT, $"terminology file failed to parse: {path} — {ex.Message}")
                    End Try
                    _cache(path) = New CacheEntry With {.LastWriteUtc = mtime, .Data = data}
                    Return data
                End SyncLock
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_CONTEXT, $"terminology file read failed: {path} — {ex.Message}")
                Return Nothing
            End Try
        End Function
    End Class
End Namespace
