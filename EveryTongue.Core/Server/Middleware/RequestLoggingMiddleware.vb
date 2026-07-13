Imports System.Diagnostics
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging

Namespace Server.Middleware
    ''' <summary>
    ''' Logs HTTP request method, path, status code, and duration.
    ''' Skips noisy endpoints (health checks, static files) at Info level.
    ''' </summary>
    Public Class RequestLoggingMiddleware
        Private ReadOnly _next As RequestDelegate
        Private ReadOnly _logger As ILogger(Of RequestLoggingMiddleware)

        ' Paths that are polled frequently — log at Debug, not Info
        Private Shared ReadOnly QuietPaths As String() = {
            "/api/health",
            "/api/config",
            "/api/rooms"
        }

        Public Sub New([next] As RequestDelegate, logger As ILogger(Of RequestLoggingMiddleware))
            _next = [next]
            _logger = logger
        End Sub

        Public Async Function InvokeAsync(context As HttpContext) As Task
            ' Skip WebSocket upgrades — they're long-lived, not request/response
            If context.WebSockets.IsWebSocketRequest Then
                Await _next(context)
                Return
            End If

            Dim sw = Stopwatch.StartNew()
            Try
                Await _next(context)
            Finally
                sw.Stop()
                Dim path = context.Request.Path.Value
                Dim statusCode = context.Response.StatusCode
                Dim method = context.Request.Method
                Dim durationMs = sw.ElapsedMilliseconds

                ' Only surface requests worth seeing at Info — errors (>=400) or slow (>500ms).
                ' Everything else (the constant stream of 2xx polls/control calls) goes to Debug,
                ' which the ILogger→AppLogger bridge drops, so it no longer floods the event-0 log.
                Dim isQuiet = QuietPaths.Any(Function(p) path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                If statusCode >= 400 OrElse (durationMs > 500 AndAlso Not isQuiet) Then
                    _logger.LogInformation("{Method} {Path} -> {StatusCode} ({Duration}ms)",
                                           method, path, statusCode, durationMs)
                Else
                    _logger.LogDebug("{Method} {Path} -> {StatusCode} ({Duration}ms)",
                                    method, path, statusCode, durationMs)
                End If
            End Try
        End Function
    End Class
End Namespace
