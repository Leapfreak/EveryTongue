Namespace Models.Templates

    ''' <summary>
    ''' Operator-level display/output template: the projected appearance and the
    ''' languages a session offers. Viewer-level preferences (each phone's own
    ''' font size) deliberately stay per-device and are NOT part of this.
    ''' </summary>
    Public Class DisplayTemplate
        Public Property Id As String = Guid.NewGuid().ToString("N").Substring(0, 8)
        Public Property Name As String = ""
        Public Property BgColor As String = "#000000"
        Public Property FgColor As String = "#FFFFFF"
        Public Property FontFamily As String = "Segoe UI"
        Public Property FontSize As Single = 14
        Public Property FontBold As Boolean = True
        ''' <summary>Languages offered to viewers (FLORES codes). Empty = all active languages.</summary>
        Public Property OfferedLanguages As New List(Of String)
        ''' <summary>Projected-view layout hint (e.g. "single", "stacked").</summary>
        Public Property Layout As String = "single"
    End Class

End Namespace
