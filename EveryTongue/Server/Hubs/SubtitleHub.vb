Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Subtitle

Namespace Server.Hubs
    ''' <summary>
    ''' WebSocket endpoint handler for /ws.
    ''' Accepts connections, runs per-client read loops, delegates to ISubtitleService.
    ''' </summary>
    Public Class SubtitleHub

        Private ReadOnly _subtitleService As ISubtitleService
        Private ReadOnly _logger As ILogger(Of SubtitleHub)

        Public Sub New(subtitleService As ISubtitleService,
                       logger As ILogger(Of SubtitleHub))
            _subtitleService = subtitleService
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

            ' Check both the WebSocket query string and the Referer header for preview flag.
            ' The Referer fallback handles cases where app.js is served from browser cache
            ' and doesn't forward the preview param to the WebSocket URL.
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
                .IsPreview = isPreview
            }

            _subtitleService.AddClient(client)

            Try
                Await ReadLoopAsync(client, context.RequestAborted)
            Finally
                _subtitleService.RemoveClient(clientId)
                Try : ws.Dispose() : Catch : End Try
            End Try
        End Function

        ''' <summary>
        ''' Per-client read loop. Runs until the client disconnects or the server shuts down.
        ''' </summary>
        Private Async Function ReadLoopAsync(client As ClientConnection,
                                              ct As CancellationToken) As Task
            Dim recvBuf = New Byte(4095) {}
            Dim historyReplayed = False

            Try
                While client.WebSocket.State = WebSocketState.Open AndAlso Not ct.IsCancellationRequested
                    Dim result = Await client.WebSocket.ReceiveAsync(
                        New ArraySegment(Of Byte)(recvBuf), ct).ConfigureAwait(False)

                    If result.MessageType = WebSocketMessageType.Close Then
                        Exit While
                    End If

                    If result.MessageType = WebSocketMessageType.Text AndAlso result.Count > 0 Then
                        Dim msg = Encoding.UTF8.GetString(recvBuf, 0, result.Count)
                        client.LastMessageAt = DateTime.Now

                        If Not historyReplayed Then
                            ' First message contains lastId for history replay
                            historyReplayed = True
                            Dim lastId = DirectCast(_subtitleService, SubtitleService).ExtractLastId(msg)
                            _subtitleService.ProcessClientMessage(client.Id, msg)
                            Await _subtitleService.ReplayHistoryAsync(client, lastId, ct).ConfigureAwait(False)
                        Else
                            _subtitleService.ProcessClientMessage(client.Id, msg)
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
