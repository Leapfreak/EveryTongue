Imports System.Text.RegularExpressions
Imports SttBenchmark.Models

Namespace Services

    ''' <summary>
    ''' Word Error Rate calculator using Levenshtein edit distance at word level.
    ''' WER = (Substitutions + Deletions + Insertions) / Reference_Words
    ''' </summary>
    Public Class WerCalculator

        Public Property IgnoreCase As Boolean = True
        Public Property StripPunctuation As Boolean = True

        Public Function Compute(referenceText As String, hypothesisText As String) As WerResult
            Dim refWords = Normalize(referenceText)
            Dim hypWords = Normalize(hypothesisText)

            If refWords.Length = 0 Then
                Return New WerResult With {
                    .Wer = If(hypWords.Length = 0, 0.0, 1.0),
                    .Insertions = hypWords.Length,
                    .RefWords = 0,
                    .HypWords = hypWords.Length
                }
            End If

            ' Levenshtein DP with backtrace for S/D/I counts
            Dim n = refWords.Length
            Dim m = hypWords.Length

            ' dp(i,j) = edit distance between ref[0..i-1] and hyp[0..j-1]
            Dim dp(n, m) As Integer
            ' backtrace: 0=match/sub, 1=deletion, 2=insertion
            Dim bt(n, m) As Integer

            For i = 0 To n
                dp(i, 0) = i
            Next
            For j = 0 To m
                dp(0, j) = j
            Next

            For i = 1 To n
                For j = 1 To m
                    Dim cost = If(refWords(i - 1) = hypWords(j - 1), 0, 1)
                    Dim sub_cost = dp(i - 1, j - 1) + cost
                    Dim del_cost = dp(i - 1, j) + 1
                    Dim ins_cost = dp(i, j - 1) + 1

                    If sub_cost <= del_cost AndAlso sub_cost <= ins_cost Then
                        dp(i, j) = sub_cost
                        bt(i, j) = 0 ' match or substitution
                    ElseIf del_cost <= ins_cost Then
                        dp(i, j) = del_cost
                        bt(i, j) = 1 ' deletion
                    Else
                        dp(i, j) = ins_cost
                        bt(i, j) = 2 ' insertion
                    End If
                Next
            Next

            ' Backtrace to count S/D/I
            Dim subs = 0, dels = 0, ins = 0
            Dim ri = n, hi = m
            While ri > 0 OrElse hi > 0
                If ri > 0 AndAlso hi > 0 AndAlso bt(ri, hi) = 0 Then
                    If refWords(ri - 1) <> hypWords(hi - 1) Then
                        subs += 1
                    End If
                    ri -= 1
                    hi -= 1
                ElseIf ri > 0 AndAlso (hi = 0 OrElse bt(ri, hi) = 1) Then
                    dels += 1
                    ri -= 1
                Else
                    ins += 1
                    hi -= 1
                End If
            End While

            Dim wer = CDbl(subs + dels + ins) / CDbl(n) * 100.0

            Return New WerResult With {
                .Wer = Math.Round(wer, 2),
                .Substitutions = subs,
                .Deletions = dels,
                .Insertions = ins,
                .RefWords = n,
                .HypWords = m
            }
        End Function

        Public Function Normalize(text As String) As String()
            If String.IsNullOrWhiteSpace(text) Then Return Array.Empty(Of String)()

            ' Strip language tags like [CA], [ES]
            Dim result = Regex.Replace(text, "\[[A-Z]{2,4}\]", "")

            If IgnoreCase Then result = result.ToLowerInvariant()

            If StripPunctuation Then
                result = Regex.Replace(result, "[^\w\s]", " ", RegexOptions.None)
            End If

            ' Collapse whitespace and split
            result = Regex.Replace(result.Trim(), "\s+", " ")

            Return result.Split(" "c, StringSplitOptions.RemoveEmptyEntries)
        End Function

    End Class

End Namespace
