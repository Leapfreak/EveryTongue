Imports System.Collections.Concurrent
Imports System.Net.WebSockets
Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Services.Rooms

    ''' <summary>
    ''' Routes continuous web-mic PCM frames from a room's broadcasting browser to that
    ''' room's live-server /audio-in WebSocket. One forwarder connection per room, opened
    ''' lazily on the first frame and re-opened on failure. Rooms register when their STT
    ''' backend starts with audio_source=web (ConferenceController) and unregister on stop.
    '''
    ''' Shared-instance pattern (like TemplateLibraryStore): the SubtitleHub (DI) and
    ''' ConferenceController (delegate-wired) both need it without ctor churn.
    ''' </summary>
    Public Class WebMicRouter

        Public Shared ReadOnly Instance As New WebMicRouter()

        Private Class RoomRoute
            Public Port As Integer
            Public Socket As ClientWebSocket
            Public BroadcasterClientId As String = ""
            ''' <summary>Serializes frame sends + reconnects for this room.</summary>
            Public ReadOnly SendLock As New SemaphoreSlim(1, 1)
            Public FramesForwarded As Long
            Public LastErrorLogged As DateTime = DateTime.MinValue
        End Class

        Private ReadOnly _routes As New ConcurrentDictionary(Of String, RoomRoute)()

        ''' <summary>Called when a room's STT backend starts with a web audio source.</summary>
        Public Sub RegisterRoom(roomId As String, liveServerPort As Integer)
            _routes(roomId) = New RoomRoute With {.Port = liveServerPort}
            AppLogger.Log(LogEvents.CONF_WEBMIC, $"room={roomId} web-mic route registered (live-server port {liveServerPort})")
        End Sub

        ''' <summary>Called when the room's backend stops. Closes the forwarder socket.</summary>
        Public Sub UnregisterRoom(roomId As String)
            Dim route As RoomRoute = Nothing
            If _routes.TryRemove(roomId, route) Then
                CloseSocket(route)
                AppLogger.Log(LogEvents.CONF_WEBMIC, $"room={roomId} web-mic route removed ({route.FramesForwarded} frames forwarded)")
            End If
        End Sub

        ''' <summary>True when the room accepts web-mic broadcast (backend started with audio_source=web).</summary>
        Public Function IsWebMicRoom(roomId As String) As Boolean
            Return _routes.ContainsKey(roomId)
        End Function

        ''' <summary>
        ''' Make clientId the room's broadcaster (last-claimer-wins).
        ''' Returns the PREVIOUS broadcaster's clientId ("" if none) so the hub can notify it.
        ''' </summary>
        Public Function SetBroadcaster(roomId As String, clientId As String) As String
            Dim route As RoomRoute = Nothing
            If Not _routes.TryGetValue(roomId, route) Then Return ""
            Dim previous = route.BroadcasterClientId
            route.BroadcasterClientId = If(clientId, "")
            Return If(previous = route.BroadcasterClientId, "", previous)
        End Function

        Public Function GetBroadcaster(roomId As String) As String
            Dim route As RoomRoute = Nothing
            Return If(_routes.TryGetValue(roomId, route), route.BroadcasterClientId, "")
        End Function

        ''' <summary>Clear the broadcaster if it is clientId (disconnect/stop). Returns True if cleared.</summary>
        Public Function ClearBroadcaster(roomId As String, clientId As String) As Boolean
            Dim route As RoomRoute = Nothing
            If _routes.TryGetValue(roomId, route) AndAlso route.BroadcasterClientId = clientId Then
                route.BroadcasterClientId = ""
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' Forward one PCM frame to the room's live-server. Opens/reopens the localhost
        ''' socket as needed. Failures are swallowed after logging (rate-limited) — a
        ''' dropped frame is 100ms of audio; the engine's queue tolerates gaps.
        ''' </summary>
        Public Async Function SendFrameAsync(roomId As String, frame As Byte(), ct As CancellationToken) As Task
            Dim route As RoomRoute = Nothing
            If Not _routes.TryGetValue(roomId, route) OrElse frame Is Nothing OrElse frame.Length = 0 Then Return

            Await route.SendLock.WaitAsync(ct).ConfigureAwait(False)
            Try
                If route.Socket Is Nothing OrElse route.Socket.State <> WebSocketState.Open Then
                    CloseSocket(route)
                    Dim sock As New ClientWebSocket()
                    Await sock.ConnectAsync(New Uri($"ws://127.0.0.1:{route.Port}/audio-in"), ct).ConfigureAwait(False)
                    route.Socket = sock
                    AppLogger.Log(LogEvents.CONF_WEBMIC, $"room={roomId} web-mic forwarder connected to live-server :{route.Port}")
                End If
                Await route.Socket.SendAsync(New ArraySegment(Of Byte)(frame),
                    WebSocketMessageType.Binary, True, ct).ConfigureAwait(False)
                route.FramesForwarded += 1
            Catch ex As Exception
                CloseSocket(route)
                ' Rate-limit: at most one error line per 5s per room (frames arrive 10/s).
                If (DateTime.Now - route.LastErrorLogged).TotalSeconds > 5 Then
                    route.LastErrorLogged = DateTime.Now
                    AppLogger.Log(LogEvents.CONF_WEBMIC_ERROR, $"room={roomId} web-mic forward failed: {ex.Message}")
                End If
            Finally
                route.SendLock.Release()
            End Try
        End Function

        Private Shared Sub CloseSocket(route As RoomRoute)
            Try : route.Socket?.Abort() : Catch : End Try
            Try : route.Socket?.Dispose() : Catch : End Try
            route.Socket = Nothing
        End Sub

    End Class

End Namespace
