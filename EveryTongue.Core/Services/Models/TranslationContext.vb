Namespace Services.Models

    ''' <summary>How a terminology term is matched against the current source sentence.</summary>
    Public Enum TerminologyMatchMode
        WholeWord
        Substring
    End Enum

    ''' <summary>
    ''' One terminology entry from translate-server\terminology.json: a source-language
    ''' term whose rendering must be enforced in specific target languages (the
    ''' translation-side analogue of the STT additional_vocab layer — fixes the
    ''' cultural-vocab tail like "gomet" that context alone does not fix).
    ''' </summary>
    Public Class TerminologyEntry
        Public Property Term As String = ""
        Public Property MatchMode As TerminologyMatchMode = TerminologyMatchMode.WholeWord
        ''' <summary>FLORES target code (or bare short code) → required rendering.</summary>
        Public Property Translations As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    End Class

    ''' <summary>One committed source sentence remembered in a room's rolling window.</summary>
    Public Class ContextSentence
        Public Property SourceText As String = ""
        ''' <summary>FLORES source code, kept per-sentence — ca/es code-switching means
        ''' the window legitimately holds mixed languages and is never cleared on a flap.</summary>
        Public Property Lang As String = ""
        Public Property TimestampUtc As DateTime
        ''' <summary>FLORES target → broadcast translation, attached when the primary
        ''' result arrives (empty until then). Not needed by DeepL; stored for the
        ''' future LLM backend's prefix-forced continuation.</summary>
        Public Property Translations As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        Friend Function Clone() As ContextSentence
            Dim c As New ContextSentence With {
                .SourceText = SourceText,
                .Lang = Lang,
                .TimestampUtc = TimestampUtc
            }
            SyncLock Translations
                For Each kvp In Translations : c.Translations(kvp.Key) = kvp.Value : Next
            End SyncLock
            Return c
        End Function
    End Class

    ''' <summary>
    ''' Immutable per-request context snapshot handed down the orchestrator to
    ''' context-capable backends (registry Entry.SupportsContext — the orchestrator
    ''' strips it for everyone else, so backends stay dumb).
    ''' </summary>
    Public Class TranslationContext
        ''' <summary>Prior committed sentences, oldest → newest. NEVER includes the
        ''' sentence currently being translated.</summary>
        Public Property Sentences As IReadOnlyList(Of ContextSentence) = Array.Empty(Of ContextSentence)()
        ''' <summary>Terminology entries whose term occurs in the current sentence (≤10,
        ''' the DeepL custom_instructions limit).</summary>
        Public Property Terminology As IReadOnlyList(Of TerminologyEntry) = Array.Empty(Of TerminologyEntry)()

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return (Sentences Is Nothing OrElse Sentences.Count = 0) AndAlso
                       (Terminology Is Nothing OrElse Terminology.Count = 0)
            End Get
        End Property

        ''' <summary>Compact log marker for TRANS_RESULT/TRANS_SHADOW lines, e.g. "ctx=3s/412ch term=2".</summary>
        Public Function Describe() As String
            Dim chars = 0
            If Sentences IsNot Nothing Then
                For Each s In Sentences : chars += If(s.SourceText?.Length, 0) : Next
            End If
            Dim d = $"ctx={If(Sentences?.Count, 0)}s/{chars}ch"
            If Terminology IsNot Nothing AndAlso Terminology.Count > 0 Then d &= $" term={Terminology.Count}"
            Return d
        End Function
    End Class

    ''' <summary>
    ''' Mutable per-room rolling window of recent committed sentences (precedent:
    ''' RefContext — bounded per-room state keyed by roomId, cleared on room close).
    ''' All access is lock-guarded; snapshots deep-copy so a late AttachTranslations
    ''' never mutates a snapshot a backend is still reading.
    ''' </summary>
    Public Class RoomTranslationContext
        Private ReadOnly _sentences As New List(Of ContextSentence)()
        Private ReadOnly _lock As New Object()

        ''' <summary>
        ''' Prune (age, then count, then total chars — oldest first), snapshot the
        ''' PRIOR window, then append the current sentence — all under one lock, so
        ''' overlapping commits each see a consistent window that excludes their own
        ''' sentence. Returns the snapshot plus the appended entry (hand it to
        ''' AttachTranslations when the broadcast result arrives).
        ''' </summary>
        Public Function SnapshotAndAppend(sourceText As String, lang As String,
                                          maxSentences As Integer, maxChars As Integer,
                                          maxAge As TimeSpan) As (Snapshot As List(Of ContextSentence), Entry As ContextSentence)
            Dim entry As New ContextSentence With {
                .SourceText = If(sourceText, ""),
                .Lang = If(lang, ""),
                .TimestampUtc = DateTime.UtcNow
            }
            SyncLock _lock
                Dim cutoff = DateTime.UtcNow - maxAge
                _sentences.RemoveAll(Function(s) s.TimestampUtc < cutoff)
                While _sentences.Count > Math.Max(0, maxSentences)
                    _sentences.RemoveAt(0)
                End While
                Dim totalChars = _sentences.Sum(Function(s) If(s.SourceText?.Length, 0))
                While _sentences.Count > 0 AndAlso totalChars > Math.Max(0, maxChars)
                    totalChars -= If(_sentences(0).SourceText?.Length, 0)
                    _sentences.RemoveAt(0)
                End While

                Dim snapshot = _sentences.Select(Function(s) s.Clone()).ToList()
                _sentences.Add(entry)
                ' Keep the stored window itself bounded (the prune above runs on the
                ' NEXT entry, but never let a burst grow the list past the cap + 1).
                While _sentences.Count > Math.Max(1, maxSentences + 1)
                    _sentences.RemoveAt(0)
                End While
                Return (snapshot, entry)
            End SyncLock
        End Function

        ''' <summary>Attach the broadcast translations to a previously appended entry.</summary>
        Public Sub AttachTranslations(entry As ContextSentence, translations As Dictionary(Of String, String))
            If entry Is Nothing OrElse translations Is Nothing Then Return
            SyncLock entry.Translations
                For Each kvp In translations
                    entry.Translations(kvp.Key) = kvp.Value
                Next
            End SyncLock
        End Sub
    End Class

End Namespace
