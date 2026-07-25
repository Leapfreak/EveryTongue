Namespace Services.Testing

    ''' <summary>
    ''' chrF (character n-gram F-score, Popović 2015) — the standard automatic
    ''' translation-quality metric for scoring against FLORES references. Works
    ''' across scripts and morphologies where BLEU breaks down. Corpus-level:
    ''' n-gram match counts accumulate across sentences (AddSentence), the final
    ''' score comes from the totals (Score). n = 1..6, β = 2 (recall-weighted),
    ''' whitespace removed — matches sacreBLEU's chrF defaults.
    ''' </summary>
    Public Class ChrFScorer

        Private Const MaxN As Integer = 6
        Private Const Beta As Double = 2.0

        Private ReadOnly _matches(MaxN - 1) As Long
        Private ReadOnly _hypCounts(MaxN - 1) As Long
        Private ReadOnly _refCounts(MaxN - 1) As Long

        Public Sub AddSentence(hypothesis As String, reference As String)
            Dim hyp = Strip(hypothesis)
            Dim ref = Strip(reference)
            For n = 1 To MaxN
                Dim hypGrams = NGramCounts(hyp, n)
                Dim refGrams = NGramCounts(ref, n)
                Dim matched = 0L
                For Each kvp In hypGrams
                    Dim refCount = 0
                    If refGrams.TryGetValue(kvp.Key, refCount) Then
                        matched += Math.Min(kvp.Value, refCount)
                    End If
                Next
                _matches(n - 1) += matched
                _hypCounts(n - 1) += hypGrams.Values.Sum(Function(v) CLng(v))
                _refCounts(n - 1) += refGrams.Values.Sum(Function(v) CLng(v))
            Next
        End Sub

        ''' <summary>Corpus chrF score in 0–100 (higher = closer to the reference).</summary>
        Public Function Score() As Double
            ' Average precision and recall over the n-gram orders, then F_β.
            Dim precisions As New List(Of Double)
            Dim recalls As New List(Of Double)
            For n = 0 To MaxN - 1
                If _hypCounts(n) > 0 OrElse _refCounts(n) > 0 Then
                    precisions.Add(If(_hypCounts(n) > 0, _matches(n) / CDbl(_hypCounts(n)), 0.0))
                    recalls.Add(If(_refCounts(n) > 0, _matches(n) / CDbl(_refCounts(n)), 0.0))
                End If
            Next
            If precisions.Count = 0 Then Return 0.0
            Dim p = precisions.Average()
            Dim r = recalls.Average()
            If p + r = 0 Then Return 0.0
            Dim b2 = Beta * Beta
            Return 100.0 * (1 + b2) * p * r / (b2 * p + r)
        End Function

        Private Shared Function Strip(text As String) As String
            Return New String(If(text, "").Where(Function(c) Not Char.IsWhiteSpace(c)).ToArray())
        End Function

        Private Shared Function NGramCounts(text As String, n As Integer) As Dictionary(Of String, Integer)
            Dim counts As New Dictionary(Of String, Integer)
            For i = 0 To text.Length - n
                Dim gram = text.Substring(i, n)
                Dim c = 0
                counts.TryGetValue(gram, c)
                counts(gram) = c + 1
            Next
            Return counts
        End Function

    End Class

End Namespace
