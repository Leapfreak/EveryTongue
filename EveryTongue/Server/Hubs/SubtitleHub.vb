Imports System.IO
Imports System.Net.WebSockets
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Rooms
Imports EveryTongue.Services.Subtitle

Namespace Server.Hubs
    ''' <summary>
    ''' WebSocket endpoint handler for /ws.
    ''' Accepts connections, runs per-client read loops, delegates to ISubtitleService.
    ''' Binary messages from conversation room clients are routed to ConversationAudioHandler.
    ''' </summary>
    Public Class SubtitleHub

        Private ReadOnly _subtitleService As ISubtitleService
        Private ReadOnly _roomManager As RoomManager
        Private ReadOnly _audioHandler As ConversationAudioHandler
        Private ReadOnly _logger As ILogger(Of SubtitleHub)

        Public Sub New(subtitleService As ISubtitleService,
                       roomManager As RoomManager,
                       audioHandler As ConversationAudioHandler,
                       logger As ILogger(Of SubtitleHub))
            _subtitleService = subtitleService
            _roomManager = roomManager
            _audioHandler = audioHandler
            _logger = logger
        End Sub

        ''' <summary>
        ''' Handle an incoming WebSocket connection.
        ''' Called from the /ws endpoint — runs for the lifetime of the connection.
        ''' </summary>
        Public Async Function HandleAsync(context As HttpContext) As Task
            If Not context.WebSockets.IsWebSocketRequest Then
                context.Response.StatusCode = 400
                Return
            End If

            Dim ws = Await context.WebSockets.AcceptWebSocketAsync()
            Dim clientId = Guid.NewGuid().ToString()
            Dim userAgent = If(context.Request.Headers("User-Agent").FirstOrDefault(), "")
            Dim remoteEp = context.Connection.RemoteIpAddress?.ToString()

            ' Extract room ID from query string
            Dim roomId = If(context.Request.Query("room").FirstOrDefault(), "")

            ' Check both the WebSocket query string and the Referer header for preview flag.
            Dim isPreview = context.Request.Query.ContainsKey("preview")
            If Not isPreview Then
                Dim referer = If(context.Request.Headers("Referer").FirstOrDefault(), "")
                isPreview = referer.Contains("preview")
            End If

            Dim client As New ClientConnection() With {
                .Id = clientId,
                .WebSocket = ws,
                .UserAgent = userAgent,
                .RemoteEndpoint = remoteEp,
                .IsPreview = isPreview,
                .RoomId = roomId
            }

            ' Join the room if specified
            If Not String.IsNullOrEmpty(roomId) Then
                Dim room = _roomManager.GetRoom(roomId)
                If room Is Nothing Then
                    ' Room not found — still connect but with no room
                    client.RoomId = ""
                ElseIf room.IsLocked Then
                    ' Room is locked — send error and close
                    Try
                        Dim errJson = Encoding.UTF8.GetBytes("{""type"":""error"",""message"":""Room is locked""}")
                        Await ws.SendAsync(New ArraySegment(Of Byte)(errJson),
                            WebSocketMessageType.Text, True, context.RequestAborted).ConfigureAwait(False)
                        Await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Room is locked", context.RequestAborted).ConfigureAwait(False)
                    Catch
                    End Try
                    Return
                Else
                    If Not _roomManager.JoinRoom(roomId, clientId) Then
                        client.RoomId = ""
                    End If
                End If
            End If

            _subtitleService.AddClient(client)

            ' Send the client its assigned ID so it can query host status
            Try
                Dim welcomeJson = Encoding.UTF8.GetBytes("{""type"":""welcome"",""clientId"":""" & clientId & """}")
                Await ws.SendAsync(New ArraySegment(Of Byte)(welcomeJson),
                    WebSocketMessageType.Text, True, context.RequestAborted).ConfigureAwait(False)
            Catch
            End Try

            ' Broadcast memberJoined to room
            If Not String.IsNullOrEmpty(client.RoomId) Then
                BroadcastToRoom(client.RoomId, "{""type"":""memberJoined"",""clientId"":""" & clientId & """,""displayName"":""Guest"",""language"":""""}", clientId)
            End If

            Try
                Await ReadLoopAsync(client, context.RequestAborted)
            Finally
                ' Broadcast memberLeft before removing
                If Not String.IsNullOrEmpty(client.RoomId) Then
                    BroadcastToRoom(client.RoomId, "{""type"":""memberLeft"",""clientId"":""" & clientId & """}", clientId)
                End If
                _subtitleService.RemoveClient(clientId)
                If Not String.IsNullOrEmpty(client.RoomId) Then
                    _roomManager.LeaveRoom(client.RoomId, clientId)
                End If
                _roomManager.RemoveClientFromAllRooms(clientId)
                Try : ws.Dispose() : Catch : End Try
            End Try
        End Function

        ''' <summary>
        ''' Per-client read loop. Runs until the client disconnects or the server shuts down.
        ''' Handles both text messages (JSON commands) and binary messages (audio data).
        ''' </summary>
        Private Async Function ReadLoopAsync(client As ClientConnection,
                                              ct As CancellationToken) As Task
            ' Use a larger buffer for audio data (up to 1MB)
            Dim recvBuf = New Byte(65535) {}
            Dim historyReplayed = False

            Try
                While client.WebSocket.State = WebSocketState.Open AndAlso Not ct.IsCancellationRequested
                    Dim result = Await client.WebSocket.ReceiveAsync(
                        New ArraySegment(Of Byte)(recvBuf), ct).ConfigureAwait(False)

                    If result.MessageType = WebSocketMessageType.Close Then
                        Exit While
                    End If

                    client.LastMessageAt = DateTime.Now

                    ' Touch room activity
                    If Not String.IsNullOrEmpty(client.RoomId) Then
                        _roomManager.GetRoom(client.RoomId)?.TouchActivity()
                    End If

                    If result.MessageType = WebSocketMessageType.Text AndAlso result.Count > 0 Then
                        Dim msg = Encoding.UTF8.GetString(recvBuf, 0, result.Count)

                        ' Handle room-specific messages
                        If HandleRoomMessage(client, msg, ct) Then
                            Continue While
                        End If

                        If Not historyReplayed Then
                            historyReplayed = True
                            _subtitleService.ProcessClientMessage(client.Id, msg)
                            ' Only replay desktop STT history for non-room clients
                            If String.IsNullOrEmpty(client.RoomId) Then
                                Dim lastId = DirectCast(_subtitleService, SubtitleService).ExtractLastId(msg)
                                Await _subtitleService.ReplayHistoryAsync(client, lastId, ct).ConfigureAwait(False)
                            End If
                        Else
                            _subtitleService.ProcessClientMessage(client.Id, msg)
                        End If

                    ElseIf result.MessageType = WebSocketMessageType.Binary AndAlso result.Count > 0 Then
                        ' Binary message = audio data from conversation room push-to-talk
                        ' Collect the complete message (may span multiple frames)
                        Dim audioStream As New MemoryStream()
                        audioStream.Write(recvBuf, 0, result.Count)

                        While Not result.EndOfMessage
                            result = Await client.WebSocket.ReceiveAsync(
                                New ArraySegment(Of Byte)(recvBuf), ct).ConfigureAwait(False)
                            audioStream.Write(recvBuf, 0, result.Count)
                        End While

                        Dim audioData = audioStream.ToArray()
                        audioStream.Dispose()

                        ' Process audio in background (don't block the read loop)
                        If Not String.IsNullOrEmpty(client.RoomId) AndAlso _audioHandler IsNot Nothing Then
                            _logger.LogInformation("PTT audio received from {Endpoint} ({Bytes} bytes, room {Room})",
                                client.RemoteEndpoint, audioData.Length, client.RoomId)
                            Dim capturedClient = client
                            Dim capturedAudio = audioData
                            Dim fireAndForget = Task.Run(Async Function()
                                         Await _audioHandler.ProcessAudioAsync(capturedClient, capturedAudio, ct)
                                     End Function)
                        ElseIf String.IsNullOrEmpty(client.RoomId) Then
                            _logger.LogDebug("Binary message from non-room client {Endpoint} ignored ({Bytes} bytes)",
                                client.RemoteEndpoint, audioData.Length)
                        End If
                    End If
                End While
            Catch ex As OperationCanceledException
                ' Server shutting down — normal
            Catch ex As WebSocketException
                _logger.LogDebug("WebSocket closed: {Endpoint} ({Message})",
                                client.RemoteEndpoint, ex.Message)
            Catch ex As Exception
                _logger.LogWarning(ex, "WebSocket error: {Endpoint}", client.RemoteEndpoint)
            End Try
        End Function

        ''' <summary>
        ''' Handle room-specific JSON messages (setDisplayName, speakAs, chatMessage).
        ''' Returns True if the message was handled (caller should skip normal processing).
        ''' </summary>
        Private Function HandleRoomMessage(client As ClientConnection, msg As String, ct As CancellationToken) As Boolean
            Try
                Using doc = JsonDocument.Parse(msg)
                    Dim root = doc.RootElement
                    Dim typeProp As JsonElement = Nothing
                    If Not root.TryGetProperty("type", typeProp) Then Return False
                    Dim msgType = If(typeProp.GetString(), "")

                    Select Case msgType
                        Case "setDisplayName"
                            Dim nameProp As JsonElement = Nothing
                            If root.TryGetProperty("name", nameProp) Then
                                client.DisplayName = If(nameProp.GetString(), "")
                            End If
                            ' Broadcast memberUpdated to room
                            If Not String.IsNullOrEmpty(client.RoomId) Then
                                Dim json = "{""type"":""memberUpdated"",""clientId"":""" & client.Id & """,""displayName"":" & SubtitleService.EscapeJson(If(client.DisplayName, "Guest")) & "}"
                                BroadcastToRoom(client.RoomId, json, "")
                            End If
                            Return True

                        Case "speakAs"
                            Dim vmProp As JsonElement = Nothing
                            If root.TryGetProperty("virtualMemberId", vmProp) Then
                                client.SpeakingAsVirtualMemberId = If(vmProp.GetString(), "")
                            Else
                                client.SpeakingAsVirtualMemberId = ""
                            End If
                            Return True

                        Case "startSpeaking"
                            If Not String.IsNullOrEmpty(client.RoomId) Then
                                Dim spkName = If(String.IsNullOrEmpty(client.DisplayName), "Guest", client.DisplayName)
                                BroadcastToRoom(client.RoomId,
                                    "{""type"":""speaking"",""clientId"":""" & client.Id & """,""displayName"":" & SubtitleService.EscapeJson(spkName) & ",""active"":true}", "")
                            End If
                            Return True

                        Case "stopSpeaking"
                            If Not String.IsNullOrEmpty(client.RoomId) Then
                                BroadcastToRoom(client.RoomId,
                                    "{""type"":""speaking"",""clientId"":""" & client.Id & """,""active"":false}", "")
                            End If
                            Return True

                        Case "chatMessage"
                            If Not String.IsNullOrEmpty(client.RoomId) AndAlso _audioHandler IsNot Nothing Then
                                Dim textProp As JsonElement = Nothing
                                If root.TryGetProperty("text", textProp) Then
                                    Dim text = If(textProp.GetString(), "")
                                    If Not String.IsNullOrEmpty(text) Then
                                        Task.Run(Async Function()
                                                     Await _audioHandler.ProcessTextAsync(client, text, ct)
                                                 End Function)
                                    End If
                                End If
                            End If
                            Return True

                        Case "clientLog"
                            Dim msgProp As JsonElement = Nothing
                            If root.TryGetProperty("msg", msgProp) Then
                                _logger.LogInformation("[ClientLog] {Endpoint}: {Msg}", client.RemoteEndpoint, If(msgProp.GetString(), ""))
                            End If
                            Return True

                        Case Else
                            Return False
                    End Select
                End Using
            Catch ex As Exception
                _logger.LogWarning(ex, "[SubtitleHub] HandleSpecialCommand parse error for {Endpoint}", client.RemoteEndpoint)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Send a JSON message to all clients in a room.
        ''' Optionally exclude a client by ID.
        ''' </summary>
        Public Sub BroadcastToRoom(roomId As String, json As String, excludeClientId As String)
            Dim room = _roomManager.GetRoom(roomId)
            If room Is Nothing Then Return
            Dim data = Encoding.UTF8.GetBytes(json)
            Dim svc = TryCast(_subtitleService, SubtitleService)
            If svc Is Nothing Then Return

            For Each cId In room.ClientIds.Keys
                If cId = excludeClientId Then Continue For
                Dim client = svc.GetClient(cId)
                If client Is Nothing OrElse client.WebSocket Is Nothing Then Continue For
                If client.WebSocket.State <> WebSocketState.Open Then Continue For
                ' Use SendBusy to avoid concurrent WebSocket sends (SendAsync is not thread-safe)
                If Interlocked.CompareExchange(client.SendBusy, 1, 0) <> 0 Then Continue For
                Dim capturedClient = client
                Task.Run(Async Function()
                             Try
                                 If capturedClient.WebSocket.State = WebSocketState.Open Then
                                     Await capturedClient.WebSocket.SendAsync(
                                         New ArraySegment(Of Byte)(data),
                                         WebSocketMessageType.Text, True, CancellationToken.None).ConfigureAwait(False)
                                 End If
                             Catch
                             Finally
                                 Interlocked.Exchange(capturedClient.SendBusy, 0)
                             End Try
                         End Function)
            Next
        End Sub

        ''' <summary>
        ''' Send a message to a specific client by ID.
        ''' </summary>
        Public Sub SendToClient(clientId As String, json As String)
            Dim svc = TryCast(_subtitleService, SubtitleService)
            If svc Is Nothing Then Return
            Dim client = svc.GetClient(clientId)
            If client Is Nothing OrElse client.WebSocket Is Nothing Then Return
            If client.WebSocket.State <> WebSocketState.Open Then Return
            If Interlocked.CompareExchange(client.SendBusy, 1, 0) <> 0 Then Return
            Dim data = Encoding.UTF8.GetBytes(json)
            Task.Run(Async Function()
                         Try
                             If client.WebSocket.State = WebSocketState.Open Then
                                 Await client.WebSocket.SendAsync(
                                     New ArraySegment(Of Byte)(data),
                                     WebSocketMessageType.Text, True, CancellationToken.None).ConfigureAwait(False)
                             End If
                         Catch
                         Finally
                             Interlocked.Exchange(client.SendBusy, 0)
                         End Try
                     End Function)
        End Sub

    End Class
End Namespace
