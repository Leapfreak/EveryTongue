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
        Public Property IsPreview As Boolean = False
        Public Property RoomId As String = ""
        Public Property MessagesSent As Long = 0
        Public Property MessagesDropped As Long = 0

        ''' <summary>Display name set by the user (empty = "Guest").</summary>
        Public Property DisplayName As String = ""

        ''' <summary>Virtual member ID this client is currently speaking as (empty = self).</summary>
        Public Property SpeakingAsVirtualMemberId As String = ""

        ''' <summary>True while this client is the room's web-mic broadcaster —
        ''' its binary frames route to the room's live-server /audio-in instead of
        ''' the PTT one-shot path. Host-gated; one broadcaster per room.</summary>
        Public Property IsBroadcasting As Boolean = False

        ''' <summary>Whether this client has reported its server-TTS state at all (older clients never do — treated as wanting TTS for back-compat).</summary>
        Public Property TtsStateKnown As Boolean = False

        ''' <summary>Whether this client will actually play pushed server TTS (speech toggle on + cloud voice selected).</summary>
        Public Property WantsServerTts As Boolean = False

        ''' <summary>
        ''' Interlocked flag: 0 = ready to send, 1 = send in-flight.
        ''' Used for backpressure — busy clients skip non-critical updates.
        ''' </summary>
        Public SendBusy As Integer = 0
    End Class
End Namespace
