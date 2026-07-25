Namespace Services.Models
    ''' <summary>
    ''' Information about a registered translation backend.
    ''' </summary>
    Public Class BackendInfo
        Public Property Name As String
        Public Property RequiresInternet As Boolean
        Public Property IsAvailable As Boolean
        Public Property IsActive As Boolean
    End Class

    ''' <summary>One English-pivot routing decision (see PivotPolicy).</summary>
    Public Class PivotDecision
        Public Property ShouldPivot As Boolean
        ''' <summary>FLORES code of the intermediate language when pivoting; "" when direct.</summary>
        Public Property Via As String
        Public Property Reason As String
    End Class

    ''' <summary>
    ''' Explained routing for one target language — what the orchestrator will do
    ''' with a (source, target) pair right now. Served by /api/translation/routing.
    ''' </summary>
    Public Class RouteInfo
        Public Property Lang As String
        ''' <summary>Orchestrator backend name that will handle this target (e.g. "Local", "Google").</summary>
        Public Property Backend As String
        ''' <summary>"direct" or "pivot".</summary>
        Public Property Route As String
        ''' <summary>FLORES code of the intermediate language when pivoting; "" when direct.</summary>
        Public Property Via As String
        Public Property Reason As String
    End Class
End Namespace
