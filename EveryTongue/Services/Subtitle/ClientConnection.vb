Imports System.Net.WebSockets
Imports System.Threading

Namespace Services.Subtitle
    ''' <summary>
    ''' Tracks a single connected phone/browser WebSocket client.
    ''' </summary>
    Public Class ClientConnection
        Public Property Id As String
        Public Property WebSocket As WebSocket
        Public Property Language As String = ""  ' Empty = original (no translation)
        Public Property UserAgent As String = ""
        Public Property RemoteEndpoint As String = ""
        Public Property ConnectedAt As DateTime = DateTime.Now
        Public Property LastMessageAt As DateTime = DateTime.Now
        Public Property MessagesSent As Long = 0
        Public Property MessagesDropped As Long = 0

        ''' <summary>
        ''' Interlocked flag: 0 = ready to send, 1 = send in-flight.
        ''' Used for backpressure — busy clients skip non-critical updates.
        ''' </summary>
        Public SendBusy As Integer = 0
    End Class
End Namespace
