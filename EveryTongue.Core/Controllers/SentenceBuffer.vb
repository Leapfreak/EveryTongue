Namespace Controllers

    ''' <summary>
    ''' Accumulates short STT commits into full sentences before translation.
    ''' Flushes on sentence-ending punctuation, timeout, or max length.
    ''' Source-language subtitles are NOT delayed — only translation batching.
    ''' </summary>
    Friend Class SentenceBuffer

        Private Const MAX_CHARS As Integer = 200
        Private ReadOnly _flushTimeoutMs As Integer

        Private ReadOnly _buffer As New Text.StringBuilder()
        Private _lang As String = ""
        Private _lastAddTime As Long = 0  ' Environment.TickCount64

        ''' <summary>
        ''' Create a sentence buffer with a configurable flush timeout.
        ''' NLLB (slow, works best on full sentences): 4000ms.
        ''' Cloud backends (fast, handles fragments OK): 2000ms.
        ''' </summary>
        Public Sub New(Optional flushTimeoutMs As Integer = 4000)
            _flushTimeoutMs = flushTimeoutMs
        End Sub

        ''' <summary>
        ''' Add a commit to the buffer. Returns the text to translate if a flush
        ''' is triggered, or Nothing if still accumulating.
        ''' </summary>
        Public Function Add(text As String, detectedLang As String) As FlushResult
            Dim now = Environment.TickCount64

            ' Language changed — flush old buffer first, then start fresh
            If _buffer.Length > 0 AndAlso Not String.IsNullOrEmpty(detectedLang) AndAlso
               Not String.IsNullOrEmpty(_lang) AndAlso detectedLang <> _lang Then
                Dim prev = Flush()
                _buffer.Append(text)
                _lang = detectedLang
                _lastAddTime = now
                Return prev
            End If

            ' Append to buffer
            If _buffer.Length > 0 Then _buffer.Append(" ")
            _buffer.Append(text)
            If Not String.IsNullOrEmpty(detectedLang) Then _lang = detectedLang
            _lastAddTime = now

            ' Check flush conditions
            If EndsWithSentencePunctuation(text) OrElse _buffer.Length >= MAX_CHARS Then
                Return Flush()
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Check if the buffer should be flushed due to timeout.
        ''' Call periodically (e.g. on each new commit from any room).
        ''' </summary>
        Public Function TryFlushExpired() As FlushResult
            If _buffer.Length = 0 Then Return Nothing
            Dim elapsed = Environment.TickCount64 - _lastAddTime
            If elapsed >= _flushTimeoutMs Then
                Return Flush()
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' Force flush remaining buffer (e.g. on room close).
        ''' </summary>
        Public Function ForceFlush() As FlushResult
            If _buffer.Length = 0 Then Return Nothing
            Return Flush()
        End Function

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return _buffer.Length = 0
            End Get
        End Property

        Private Function Flush() As FlushResult
            Dim result As New FlushResult With {
                .Text = _buffer.ToString().Trim(),
                .DetectedLanguage = _lang
            }
            _buffer.Clear()
            _lang = ""
            _lastAddTime = 0
            Return result
        End Function

        Private Shared Function EndsWithSentencePunctuation(text As String) As Boolean
            If String.IsNullOrEmpty(text) Then Return False
            Dim trimmed = text.TrimEnd()
            If trimmed.Length = 0 Then Return False
            ' Ellipsis (...) is NOT sentence-ending — it means the speaker trailed off mid-thought
            If trimmed.EndsWith("...") Then Return False
            Dim last = trimmed(trimmed.Length - 1)
            Return last = "."c OrElse last = "!"c OrElse last = "?"c
        End Function

    End Class

    Friend Class FlushResult
        Public Property Text As String
        Public Property DetectedLanguage As String
    End Class

End Namespace
