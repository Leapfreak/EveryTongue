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
            "/api/config"
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

                If QuietPaths.Any(Function(p) path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) Then
                    _logger.LogDebug("{Method} {Path} -> {StatusCode} ({Duration}ms)",
                                    method, path, statusCode, durationMs)
                Else
                    _logger.LogInformation("{Method} {Path} -> {StatusCode} ({Duration}ms)",
                                           method, path, statusCode, durationMs)
                End If
            End Try
        End Function
    End Class
End Namespace
