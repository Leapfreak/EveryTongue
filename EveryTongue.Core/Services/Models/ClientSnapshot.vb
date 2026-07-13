Namespace Services.Models
    ''' <summary>
    ''' A read-only snapshot of a connected WebSocket client for display purposes.
    ''' </summary>
    Public Class ClientSnapshot
        Public Property RemoteEndpoint As String
        Public Property Device As String
        Public Property OS As String
        Public Property Browser As String
        Public Property Language As String
        Public Property ConnectedAt As DateTime
        Public Property LastMessageAt As DateTime
        Public Property MessagesSent As Long
        Public Property MessagesDropped As Long
        Public Property RawUserAgent As String
    End Class
End Namespace
