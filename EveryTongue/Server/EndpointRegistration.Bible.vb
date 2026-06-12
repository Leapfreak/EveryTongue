Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server.Hubs
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Audio
Imports EveryTongue.Services.Rooms
Imports EveryTongue.Services.Subtitle
Imports EveryTongue.Services.Tts
Imports QRCoder

Namespace Server
    Partial Public Module EndpointRegistration

        Private Sub MapBibleEndpoints(app As IEndpointRouteBuilder)

            ' List available translations
            app.MapGet("/bible/translations", Async Function(context As HttpContext) As Task
                                                  Dim logger = context.RequestServices.
                                                      GetService(Of ILoggerFactory)?.CreateLogger("BibleEndpoint")
                                                  Dim bibleService = context.RequestServices.
                                                      GetService(Of IBibleService)
                                                  If bibleService Is Nothing Then
                                                      logger?.LogWarning("Bible service not available")
                                                      Await context.Response.WriteAsJsonAsync(
                                                          New With {.error = "Bible service not available"})
                                                      Return
                                                  End If
                                                  Dim lang = context.Request.Query("lang").FirstOrDefault()
                                                  Dim translations = Await bibleService.GetTranslationsAsync(
                                                      If(lang, ""), context.RequestAborted)
                                                  logger?.LogInformation("Bible translations: lang={Lang}, count={Count}",
                                                      If(lang, "(all)"), translations.Count)
                                                  Await context.Response.WriteAsJsonAsync(translations)
                                              End Function)

            ' List books for a translation
            app.MapGet("/bible/{id}/books",
                Async Function(id As String, context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(Array.Empty(Of Object)())
                        Return
                    End If
                    Dim books = Await bibleService.GetBooksAsync(id, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(books)
                End Function)

            ' Get chapter
            app.MapGet("/bible/{id}/{book}/{chapter:int}",
                Async Function(id As String, book As String, chapter As Integer,
                               context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(
                            New With {.error = "Bible service not available"})
                        Return
                    End If
                    Dim result = Await bibleService.GetChapterAsync(
                        id, book, chapter, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(result)
                End Function)

            ' Get verse or verse range
            app.MapGet("/bible/{id}/{book}/{chapter:int}/{verses}",
                Async Function(id As String, book As String, chapter As Integer,
                               verses As String, context As HttpContext) As Task
                    Dim bibleService = context.RequestServices.GetService(Of IBibleService)
                    If bibleService Is Nothing Then
                        Await context.Response.WriteAsJsonAsync(
                            New With {.error = "Bible service not available"})
                        Return
                    End If
                    Dim parts = verses.Split("-"c)
                    Dim vStart = Integer.Parse(parts(0))
                    Dim vEnd = If(parts.Length > 1, Integer.Parse(parts(1)), -1)
                    Dim result = Await bibleService.GetVersesAsync(
                        id, book, chapter, vStart, vEnd, context.RequestAborted)
                    Await context.Response.WriteAsJsonAsync(result)
                End Function)

            ' Full-text search
            app.MapGet("/bible/search", Async Function(context As HttpContext) As Task
                                            Dim bibleService = context.RequestServices.
                                                GetService(Of IBibleService)
                                            If bibleService Is Nothing Then
                                                Await context.Response.WriteAsJsonAsync(
                                                    New With {.error = "Bible service not available"})
                                                Return
                                            End If
                                            Dim q = context.Request.Query("q").FirstOrDefault()
                                            Dim transId = context.Request.Query("translation").FirstOrDefault()
                                            If String.IsNullOrEmpty(q) OrElse String.IsNullOrEmpty(transId) Then
                                                context.Response.StatusCode = 400
                                                Await context.Response.WriteAsJsonAsync(
                                                    New With {.error = "q and translation required"})
                                                Return
                                            End If
                                            Dim maxResults = 50
                                            Dim maxStr = context.Request.Query("max").FirstOrDefault()
                                            If maxStr IsNot Nothing Then Integer.TryParse(maxStr, maxResults)
                                            Dim searchResults = Await bibleService.SearchAsync(
                                                q, transId, maxResults, context.RequestAborted)
                                            Await context.Response.WriteAsJsonAsync(searchResults)
                                        End Function)

            ' Parse human-readable reference
            app.MapGet("/bible/parse", Async Function(context As HttpContext) As Task
                                           Dim bibleService = context.RequestServices.
                                               GetService(Of IBibleService)
                                           If bibleService Is Nothing Then
                                               Await context.Response.WriteAsJsonAsync(
                                                   New With {.error = "Bible service not available"})
                                               Return
                                           End If
                                           Dim ref = context.Request.Query("ref").FirstOrDefault()
                                           If String.IsNullOrEmpty(ref) Then
                                               context.Response.StatusCode = 400
                                               Await context.Response.WriteAsJsonAsync(
                                                   New With {.error = "ref parameter required"})
                                               Return
                                           End If
                                           Dim lang = If(context.Request.Query("lang").FirstOrDefault(), "en")
                                           Dim transId = context.Request.Query("translation").FirstOrDefault()
                                           Dim result = Await bibleService.ParseReferenceAsync(ref, lang, transId)
                                           Await context.Response.WriteAsJsonAsync(result)
                                       End Function)
        End Sub

    End Module
End Namespace
