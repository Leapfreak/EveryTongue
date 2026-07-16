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

        Private Sub MapTemplateEndpoints(app As IEndpointRouteBuilder)

            ' List templates (id + name only, no hosting codes) — volunteer-gated
            ' like the room list (three-tier IA); open mode = legacy public.
            app.MapGet("/api/templates", Function(context As HttpContext) As Task
                                              If Not CreatorCodeOk(context) Then
                                                  context.Response.StatusCode = 403
                                                  Return context.Response.WriteAsJsonAsync(New With {.error = "creator code required"})
                                              End If
                                              Dim store = context.RequestServices.GetRequiredService(Of TemplateStore)()
                                              Dim result = store.GetAll().Select(Function(t) New With {
                                                  .id = t.Id,
                                                  .name = t.Name
                                              }).ToArray()
                                              Return context.Response.WriteAsJsonAsync(result)
                                          End Function)

            ' ── Permanent template-pointer join flow (three-tier IA) ─────────
            ' The laminated QR encodes the TEMPLATE (stable forever), not the
            ' week's room instance. Guests resolve it to whichever room is
            ' currently running from that template; no room yet → the client
            ' shows a waiting page and polls until the host starts the service.
            ' Open by design: possessing the printed QR *is* the authorization.
            app.MapGet("/api/templates/{id}/active-room",
                Function(id As String, context As HttpContext) As IResult
                    Dim store = context.RequestServices.GetRequiredService(Of TemplateStore)()
                    If store.GetById(id) Is Nothing Then
                        Return Results.Json(New With {.error = "unknown template"}, statusCode:=404)
                    End If
                    Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                    Dim room = mgr.GetAllRooms().
                        Where(Function(r) String.Equals(r.TemplateId, id, StringComparison.OrdinalIgnoreCase)).
                        OrderByDescending(Function(r) r.CreatedAt).
                        FirstOrDefault()
                    If room Is Nothing Then Return Results.Json(New With {.active = False})
                    Return Results.Json(New With {.active = True, .roomId = room.Id, .name = room.Name})
                End Function)

            app.MapGet("/api/templates/{id}/qr",
                Function(id As String, context As HttpContext) As IResult
                    Dim store = context.RequestServices.GetRequiredService(Of TemplateStore)()
                    If store.GetById(id) Is Nothing Then
                        Return Results.Json(New With {.error = "unknown template"}, statusCode:=404)
                    End If
                    Dim scheme = If(context.Request.IsHttps, "https", "http")
                    Dim joinUrl = $"{scheme}://{PublicHostFor(context)}/index.html?join={id}"
                    Return Results.File(GenerateQrPng(joinUrl), "image/png", $"template-{id}.png")
                End Function)

            ' Create a conference room from a template (validates hosting code)
            app.MapPost("/api/rooms/from-template", Async Function(context As HttpContext) As Task
                                                         Dim store = context.RequestServices.GetRequiredService(Of TemplateStore)()
                                                         Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                         Dim doc As JsonDocument = Nothing
                                                         Try
                                                             doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                             Dim root = doc.RootElement

                                                             Dim templateIdProp As JsonElement = Nothing
                                                             Dim codeProp As JsonElement = Nothing
                                                             Dim hostProp As JsonElement = Nothing
                                                             Dim templateId = ""
                                                             Dim hostingCode = ""
                                                             Dim hostClientId = ""

                                                             If root.TryGetProperty("templateId", templateIdProp) Then templateId = If(templateIdProp.GetString(), "")
                                                             If root.TryGetProperty("hostingCode", codeProp) Then hostingCode = If(codeProp.GetString(), "")
                                                             If root.TryGetProperty("hostClientId", hostProp) Then hostClientId = If(hostProp.GetString(), "")

                                                             ' Validate hosting code (rate-limited like every credential gate)
                                                             Dim codeValid = Not AuthBlocked(context) AndAlso
                                                                             Not String.IsNullOrEmpty(hostingCode) AndAlso
                                                                             store.ValidateHostingCode(templateId, hostingCode)
                                                             If Not String.IsNullOrEmpty(hostingCode) AndAlso Not AuthBlocked(context) Then
                                                                 If codeValid Then RecordAuthSuccess(context) Else RecordAuthFailure(context)
                                                             End If
                                                             If Not codeValid Then
                                                                 context.Response.StatusCode = 403
                                                                 Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid hosting code"})
                                                                 Return
                                                             End If

                                                             Dim template = store.GetById(templateId)

                                                             ' Determine visibility from template
                                                             Dim visibility = If(template.DefaultVisibility?.ToLower() = "private",
                                                                 RoomVisibility.Private, RoomVisibility.Public)

                                                             ' Create conference room linked to template
                                                             Dim room = mgr.CreateRoom(
                                                                 If(template.Name, "Conference"),
                                                                 RoomType.Conference,
                                                                 visibility,
                                                                 hostClientId,
                                                                 templateId)

                                                             ' Set source language + connectivity mode + display from template
                                                             room.SourceLang = If(template.SourceLanguage, "auto")
                                                             room.AudioSource = If(String.IsNullOrEmpty(template.AudioSource), "local", template.AudioSource)
                                                             room.WebMicRaw = template.WebMicRaw
                                                             room.Mode = If(template.Mode = Models.Templates.ConnectivityMode.Offline, "offline", "online")
                                                             room.Display = Services.Config.TemplateLibraryStore.Instance.GetDisplayTemplate(
                                                                 If(template.DisplayTemplateId, ""))

                                                             context.Response.StatusCode = 201
                                                             Await context.Response.WriteAsJsonAsync(New With {
                                                                 .id = room.Id,
                                                                 .name = room.Name,
                                                                 .type = room.Type.ToString().ToLower(),
                                                                 .visibility = room.Visibility.ToString().ToLower(),
                                                                 .hostToken = room.HostToken
                                                             })

                                                             ' Notify desktop to spin up the pipeline
                                                             ConferenceRoomCreatedHandler?.Invoke(room.Id, templateId)

                                                         Catch ex As Exception
                                                             context.Response.StatusCode = 400
                                                             context.Response.WriteAsync("{""error"":""Invalid request""}").Wait()
                                                         Finally
                                                             doc?.Dispose()
                                                         End Try
                                                     End Function)

            ' Pipeline config — web host adjusts live pipeline settings
            app.MapPost("/api/control/pipeline", Async Function(context As HttpContext) As Task
                                                      Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                      Dim doc As JsonDocument = Nothing
                                                      Try
                                                          doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                          Dim root = doc.RootElement

                                                          ' Require roomId and host authentication
                                                          Dim roomIdProp As JsonElement = Nothing
                                                          Dim hostTokenProp As JsonElement = Nothing
                                                          Dim clientIdProp As JsonElement = Nothing
                                                          Dim roomId = ""
                                                          Dim hostToken = ""
                                                          Dim clientId = ""

                                                          If root.TryGetProperty("roomId", roomIdProp) Then roomId = If(roomIdProp.GetString(), "")
                                                          If root.TryGetProperty("hostToken", hostTokenProp) Then hostToken = If(hostTokenProp.GetString(), "")
                                                          If root.TryGetProperty("clientId", clientIdProp) Then clientId = If(clientIdProp.GetString(), "")

                                                          ' Validate room exists and caller is host
                                                          Dim room = mgr.GetRoom(roomId)
                                                          If room Is Nothing Then
                                                              context.Response.StatusCode = 404
                                                              Await context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                                              Return
                                                          End If

                                                          Dim isHost = (Not String.IsNullOrEmpty(hostToken) AndAlso room.HostToken = hostToken) OrElse
                                                                       (Not String.IsNullOrEmpty(clientId) AndAlso room.HostClientId = clientId)
                                                          If Not isHost Then
                                                              context.Response.StatusCode = 403
                                                              Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized"})
                                                              Return
                                                          End If

                                                          ' Build params dictionary from optional fields
                                                          Dim params As New Dictionary(Of String, Object)
                                                          Dim langProp As JsonElement = Nothing
                                                          Dim maxSegProp As JsonElement = Nothing
                                                          Dim vadProp As JsonElement = Nothing
                                                          Dim beamProp As JsonElement = Nothing
                                                          Dim promptProp As JsonElement = Nothing

                                                          If root.TryGetProperty("language", langProp) Then params("language") = langProp.GetString()
                                                          If root.TryGetProperty("maxSegmentSec", maxSegProp) Then params("maxSegmentSec") = maxSegProp.GetInt32()
                                                          If root.TryGetProperty("vadSilenceMs", vadProp) Then params("vadSilenceMs") = vadProp.GetInt32()
                                                          If root.TryGetProperty("beamSize", beamProp) Then params("beamSize") = beamProp.GetInt32()
                                                          If root.TryGetProperty("initialPrompt", promptProp) Then params("initialPrompt") = promptProp.GetString()
                                                          Dim speakerProp As JsonElement = Nothing
                                                          Dim modeProp As JsonElement = Nothing
                                                          If root.TryGetProperty("speakerId", speakerProp) Then params("speakerId") = If(speakerProp.GetString(), "")
                                                          If root.TryGetProperty("mode", modeProp) Then params("mode") = If(modeProp.GetString(), "")

                                                          If params.Count = 0 Then
                                                              Await context.Response.WriteAsJsonAsync(New With {.ok = True, .changed = 0})
                                                              Return
                                                          End If

                                                          ' Invoke handler on UI thread
                                                          Dim handler = PipelineConfigHandler
                                                          If handler Is Nothing Then
                                                              context.Response.StatusCode = 503
                                                              Await context.Response.WriteAsJsonAsync(New With {.error = "Pipeline not available"})
                                                              Return
                                                          End If

                                                          handler.Invoke(roomId, params)
                                                          room.TouchActivity()

                                                          Await context.Response.WriteAsJsonAsync(New With {
                                                              .ok = True,
                                                              .changed = params.Count
                                                          })

                                                      Catch ex As Exception
                                                          context.Response.StatusCode = 400
                                                          context.Response.WriteAsync("{""error"":""Invalid request""}").Wait()
                                                      Finally
                                                          doc?.Dispose()
                                                      End Try
                                                  End Function)

            ' Pipeline reset — host can force-restart the entire STT pipeline
            app.MapPost("/api/control/pipeline/reset", Async Function(context As HttpContext) As Task
                                                            Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                            Dim doc As JsonDocument = Nothing
                                                            Try
                                                                doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                                Dim root = doc.RootElement

                                                                Dim roomIdProp As JsonElement = Nothing
                                                                Dim clientIdProp As JsonElement = Nothing
                                                                Dim roomId = ""
                                                                Dim clientId = ""

                                                                If root.TryGetProperty("roomId", roomIdProp) Then roomId = If(roomIdProp.GetString(), "")
                                                                If root.TryGetProperty("clientId", clientIdProp) Then clientId = If(clientIdProp.GetString(), "")

                                                                Dim room = mgr.GetRoom(roomId)
                                                                If room Is Nothing Then
                                                                    context.Response.StatusCode = 404
                                                                    Await context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                                                    Return
                                                                End If

                                                                If String.IsNullOrEmpty(clientId) OrElse room.HostClientId <> clientId Then
                                                                    context.Response.StatusCode = 403
                                                                    Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized"})
                                                                    Return
                                                                End If

                                                                Dim handler = PipelineResetHandler
                                                                If handler Is Nothing Then
                                                                    context.Response.StatusCode = 503
                                                                    Await context.Response.WriteAsJsonAsync(New With {.error = "Pipeline not available"})
                                                                    Return
                                                                End If

                                                                handler.Invoke(roomId)
                                                                room.TouchActivity()
                                                                Await context.Response.WriteAsJsonAsync(New With {.ok = True})

                                                            Catch ex As Exception
                                                                context.Response.StatusCode = 400
                                                                context.Response.WriteAsync("{""error"":""Invalid request""}").Wait()
                                                            Finally
                                                                doc?.Dispose()
                                                            End Try
                                                        End Function)

        End Sub

    End Module
End Namespace
