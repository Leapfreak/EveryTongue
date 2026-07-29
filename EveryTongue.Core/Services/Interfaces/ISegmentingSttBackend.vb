Namespace Services.Interfaces

    ''' <summary>
    ''' STT backends whose live-server can split a held clause into sentences
    ''' via the SaT segmenter (POST /segment). The clause coordinator uses this
    ''' to re-segment merged clauses at the pause — engine-agnostic: implemented
    ''' by the streaming backend and the whisper-family backends alike.
    ''' </summary>
    Public Interface ISegmentingSttBackend
        Function Segment(text As String, thresholdPercent As Integer, model As String) As List(Of String)
    End Interface

End Namespace
