Imports System.Text.RegularExpressions

Namespace Services.Translation

    ''' <summary>
    ''' Shared sentence/line splitting for translation inputs (NLLB translates best
    ''' one sentence at a time). Used by the Translate workspace (desktop head) and
    ''' ConversationAudioHandler (core) — lives here so the core never references a
    ''' desktop controller.
    ''' </summary>
    Public Module SentenceSplit

        Public Class TextLine
            Public IsBlank As Boolean
            Public Sentences As New List(Of String)()
        End Class

        ''' <summary>
        ''' Splits input preserving every line break. Each line is further split
        ''' into sentences for translation. Blank lines are kept as-is.
        ''' </summary>
        Public Function SplitIntoLines(text As String) As List(Of TextLine)
            Dim result As New List(Of TextLine)()
            Dim lines = text.Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)

            For Each line In lines
                Dim tl As New TextLine()
                Dim trimmed = line.Trim()
                If String.IsNullOrEmpty(trimmed) Then
                    tl.IsBlank = True
                Else
                    Dim sentences = Regex.Split(trimmed, "(?<=[.!?])\s+")
                    For Each s In sentences
                        Dim st = s.Trim()
                        If st.Length > 0 Then tl.Sentences.Add(st)
                    Next
                End If
                result.Add(tl)
            Next

            Return result
        End Function

    End Module

End Namespace
