Imports System.Net
Imports System.Text.Json
Imports Microsoft.AspNetCore.Http
Imports Microsoft.Extensions.Logging

Namespace Server.Middleware
    ''' <summary>
    ''' Catch-all error handler. Returns structured JSON error responses
    ''' without leaking stack traces to clients.
    ''' </summary>
    Public Class ErrorHandlingMiddleware
        Private ReadOnly _next As RequestDelegate
        Private ReadOnly _logger As ILogger(Of ErrorHandlingMiddleware)

        Public Sub New([next] As RequestDelegate, logger As ILogger(Of ErrorHandlingMiddleware))
            _next = [next]
            _logger = logger
        End Sub

        Public Async Function InvokeAsync(context As HttpContext) As Task
            ' VB.NET does not allow Await inside Catch blocks,
            ' so we capture the exception and handle it after the Try.
            Dim caught As Exception = Nothing
            Try
                Await _next(context)
                Return
            Catch ex As OperationCanceledException
                ' Client disconnected — not an error
                context.Response.StatusCode = 499
                Return
            Catch ex As Exception
                caught = ex
            End Try

            ' Handle outside the Catch block so we can Await
            If TypeOf caught Is BadHttpRequestException Then
                _logger.LogWarning(caught, "Bad request: {Path}", context.Request.Path)
                context.Response.StatusCode = CInt(HttpStatusCode.BadRequest)
                Await WriteErrorResponse(context, "Bad request", caught.Message)
            ElseIf caught IsNot Nothing Then
                _logger.LogError(caught, "Unhandled exception: {Method} {Path}",
                                context.Request.Method, context.Request.Path)
                context.Response.StatusCode = CInt(HttpStatusCode.InternalServerError)
                Await WriteErrorResponse(context, "Internal server error")
            End If
        End Function

        Private Shared Async Function WriteErrorResponse(
                context As HttpContext,
                error_msg As String,
                Optional detail As String = Nothing) As Task
            If context.Response.HasStarted Then Return

            context.Response.ContentType = "application/json"
            Dim errorObj As New Dictionary(Of String, String) From {
                {"error", error_msg}
            }
            If detail IsNot Nothing Then
                errorObj("detail") = detail
            End If
            Await context.Response.WriteAsync(JsonSerializer.Serialize(errorObj))
        End Function
    End Class
End Namespace
