Imports System.IO
Imports System.Net.WebSockets
Imports System.Text
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
                If Not _roomManager.JoinRoom(roomId, clientId) Then
                    ' Room not found — still connect but with no room
                    client.RoomId = ""
                End If
            End If

            _subtitleService.AddClient(client)

            Try
                Await ReadLoopAsync(client, context.RequestAborted)
            Finally
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
                            Task.Run(Async Function()
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

    End Class
End Namespace
