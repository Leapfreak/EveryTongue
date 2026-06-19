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

        ''' <summary>Serialized speaker shape for a room's host panel. Serialized camelCase: {id, name}.</summary>
        Private Class SpeakerDto
            Public Property Id As String
            Public Property Name As String
        End Class

        ''' <summary>Speaker list for a room's host panel: the room template's speaker refs resolved to {id, name}.</summary>
        Private Function SpeakerListFor(room As Services.Rooms.Room) As SpeakerDto()
            Try
                Dim tpl = Services.Rooms.TemplateStore.Instance?.GetById(If(room.TemplateId, ""))
                If tpl?.SpeakerProfileIds Is Nothing Then Return Array.Empty(Of SpeakerDto)()
                Dim list As New List(Of SpeakerDto)
                For Each spId In tpl.SpeakerProfileIds
                    Dim sp = Services.Config.TemplateLibraryStore.Instance.GetSpeakerProfile(spId)
                    If sp IsNot Nothing Then list.Add(New SpeakerDto With {.Id = sp.Id, .Name = sp.Name})
                Next
                Return list.ToArray()
            Catch
                Return Array.Empty(Of SpeakerDto)()
            End Try
        End Function

        ''' <summary>The room's resolved Display template for web clients (Nothing = viewer defaults).</summary>
        Private Function DisplayFor(room As Services.Rooms.Room) As Object
            Dim d = room.Display
            If d Is Nothing Then Return Nothing
            Return New With {
                .bgColor = d.BgColor,
                .fgColor = d.FgColor,
                .fontFamily = d.FontFamily,
                .fontSize = d.FontSize,
                .fontBold = d.FontBold,
                .layout = d.Layout,
                .offeredLanguages = If(d.OfferedLanguages, New List(Of String)).ToArray()
            }
        End Function

        Private Sub MapControlEndpoint(app As IEndpointRouteBuilder)

            ' Admin remote control — /api/control?action=start|stop|restart|clear|status|tune|setsliders
            app.MapGet("/api/control", Function(context As HttpContext) As IResult
                                           Dim subtitleService = context.RequestServices.
                                               GetService(Of ISubtitleService)
                                           If subtitleService Is Nothing Then
                                               Return Results.Json(New With {.error = "service unavailable"})
                                           End If

                                           Dim action = context.Request.Query("action").FirstOrDefault()

                                           ' No action = status (same as legacy)
                                           If String.IsNullOrEmpty(action) OrElse
                                              action.Equals("status", StringComparison.OrdinalIgnoreCase) Then
                                               Return Results.Json(New With {
                                                   .live = subtitleService.IsLiveRunning,
                                                   .inputLang = If(subtitleService.InputLanguage, "")
                                               })
                                           End If

                                           Select Case action.ToLower()
                                               Case "start", "stop", "restart", "clear"
                                                   RemoteCommandHandler?.Invoke(action.ToLower())
                                                   Return Results.Json(New With {
                                                       .ok = True,
                                                       .action = action.ToLower()
                                                   })

                                               Case "tune"
                                                   If subtitleService.TuneCallback IsNot Nothing Then
                                                       Dim json = subtitleService.TuneCallback.Invoke()
                                                       If json IsNot Nothing Then
                                                           Return Results.Text(json, "application/json")
                                                       End If
                                                   End If
                                                   Return Results.Json(New With {.error = "not available"})

                                               Case "setsliders"
                                                   Dim maxSeg = context.Request.Query("maxSeg").FirstOrDefault()
                                                   Dim vadSilence = context.Request.Query("vadSilence").FirstOrDefault()
                                                   If maxSeg IsNot Nothing AndAlso vadSilence IsNot Nothing Then
                                                       RemoteCommandHandler?.Invoke($"setSliders:{maxSeg},{vadSilence}")
                                                       Return Results.Json(New With {.ok = True})
                                                   End If
                                                   Return Results.Json(New With {.error = "missing params"})

                                               Case Else
                                                   Return Results.BadRequest(New With {.error = "unknown action"})
                                           End Select
                                       End Function)
        End Sub

        ''' <summary>Serialized room-member shape for /api/rooms/{id}/members. Serialized camelCase: {clientId, displayName, language, virtual}.</summary>
        Private Class RoomMemberDto
            Public Property ClientId As String
            Public Property DisplayName As String
            Public Property Language As String
            Public Property [Virtual] As Boolean
        End Class

        Private Sub MapRoomEndpoints(app As IEndpointRouteBuilder)

            ' List public rooms (for lobby)
            app.MapGet("/api/rooms", Function(context As HttpContext) As Task
                                         Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                         Dim rooms = mgr.GetPublicRooms()
                                         Dim result = rooms.Select(Function(r) New With {
                                             .id = r.Id,
                                             .name = r.Name,
                                             .type = r.Type.ToString().ToLower(),
                                             .clients = r.ClientCount,
                                             .createdAt = r.CreatedAt
                                         }).ToArray()
                                         Return context.Response.WriteAsJsonAsync(result)
                                     End Function)

            ' Get a specific room (works for both public and private — you need the ID)
            ' Optional ?clientId= param tells the client if they are the host.
            app.MapGet("/api/rooms/{id}", Function(id As String, context As HttpContext) As Task
                                               Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                               Dim room = mgr.GetRoom(id)
                                               If room Is Nothing Then
                                                   context.Response.StatusCode = 404
                                                   Return context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                               End If
                                               Dim clientId = If(context.Request.Query("clientId").FirstOrDefault(), "")
                                               Dim isHost = Not String.IsNullOrEmpty(clientId) AndAlso room.HostClientId = clientId
                                               Return context.Response.WriteAsJsonAsync(New With {
                                                   .id = room.Id,
                                                   .name = room.Name,
                                                   .type = room.Type.ToString().ToLower(),
                                                   .visibility = room.Visibility.ToString().ToLower(),
                                                   .clients = room.ClientCount,
                                                   .createdAt = room.CreatedAt,
                                                   .isHost = isHost,
                                                   .isLocked = room.IsLocked,
                                                   .pttMode = room.Config.PttMode,
                                                   .sourceLang = room.SourceLang,
                                                   .maxSegmentSec = room.MaxSegmentSec,
                                                   .vadSilenceMs = room.VadSilenceMs,
                                                   .beamSize = room.BeamSize,
                                                   .initialPrompt = room.InitialPrompt,
                                                   .activeSpeakerId = room.ActiveSpeakerId,
                                                   .mode = room.Mode,
                                                   .speakers = SpeakerListFor(room),
                                                   .display = DisplayFor(room)
                                               })
                                           End Function)

            ' Create a room — returns hostToken for reconnection
            app.MapPost("/api/rooms", Async Function(context As HttpContext) As Task
                                           Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                           Dim doc As JsonDocument = Nothing
                                           Try
                                               doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                               Dim root = doc.RootElement
                                               Dim name = ""
                                               Dim nameProp As JsonElement = Nothing
                                               Dim typeProp As JsonElement = Nothing
                                               Dim visProp As JsonElement = Nothing
                                               Dim hostId = ""

                                               If root.TryGetProperty("name", nameProp) Then name = If(nameProp.GetString(), "")
                                               Dim roomType As RoomType = RoomType.Conference
                                               If root.TryGetProperty("type", typeProp) Then
                                                   Dim typeStr = If(typeProp.GetString(), "")
                                                   Select Case typeStr.ToLower()
                                                       Case "conversation" : roomType = RoomType.Conversation
                                                       Case "workroom" : roomType = RoomType.Workroom
                                                       Case Else : roomType = RoomType.Conference
                                                   End Select
                                               End If
                                               Dim visibility As RoomVisibility = If(roomType = RoomType.Conversation,
                                                   RoomVisibility.Private, RoomVisibility.Public)
                                               If root.TryGetProperty("visibility", visProp) Then
                                                   Dim visStr = If(visProp.GetString(), "")
                                                   If visStr.ToLower() = "private" Then visibility = RoomVisibility.Private
                                                   If visStr.ToLower() = "public" Then visibility = RoomVisibility.Public
                                               End If
                                               Dim hostProp As JsonElement = Nothing
                                               If root.TryGetProperty("hostClientId", hostProp) Then hostId = If(hostProp.GetString(), "")

                                               ' Optional per-room translation engine (conversation rooms); "" = global default.
                                               Dim engProp As JsonElement = Nothing
                                               Dim translationEngine = ""
                                               If root.TryGetProperty("translationEngine", engProp) Then translationEngine = If(engProp.GetString(), "")

                                               Dim room = mgr.CreateRoom(name, roomType, visibility, hostId, "", translationEngine)
                                               context.Response.StatusCode = 201
                                               Await context.Response.WriteAsJsonAsync(New With {
                                                   .id = room.Id,
                                                   .name = room.Name,
                                                   .type = room.Type.ToString().ToLower(),
                                                   .visibility = room.Visibility.ToString().ToLower(),
                                                   .hostToken = room.HostToken
                                               })
                                           Catch ex As Exception
                                               context.Response.StatusCode = 400
                                               context.Response.WriteAsync("{""error"":""Invalid request""}").Wait()
                                           Finally
                                               doc?.Dispose()
                                           End Try
                                       End Function)

            ' List selectable translation engines (for the lobby's per-room engine picker).
            ' Excludes inline engines (they only work inside a matching STT session).
            app.MapGet("/api/translation-engines", Function(context As HttpContext) As Task
                                                       Dim engines = Services.Translation.TranslationBackendRegistry.GetAll().
                                                           Where(Function(e) String.IsNullOrEmpty(e.InlineWithStt)).
                                                           Select(Function(e) New With {
                                                               .key = e.Key,
                                                               .name = e.DisplayName,
                                                               .offline = Not String.IsNullOrEmpty(e.ModelType)
                                                           }).ToList()
                                                       Return context.Response.WriteAsJsonAsync(engines)
                                                   End Function)

            ' Close a room — broadcasts roomClosed to all clients
            app.MapDelete("/api/rooms/{id}", Function(id As String, context As HttpContext) As Task
                                                  Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                  Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                  Dim clientId = context.Request.Query("clientId").FirstOrDefault()
                                                  Dim ok = mgr.CloseRoom(id, If(clientId, ""))
                                                  If Not ok Then
                                                      context.Response.StatusCode = 403
                                                      Return context.Response.WriteAsJsonAsync(New With {.error = "Not authorized or room not found"})
                                                  End If
                                                  ' Broadcast roomClosed to all room members
                                                  hub.BroadcastToRoom(id, "{""type"":""roomClosed""}", "")
                                                  ' Stop conference backend if any
                                                  RoomClosedHandler?.Invoke(id)
                                                  Return context.Response.WriteAsJsonAsync(New With {.ok = True})
                                              End Function)

            ' QR code PNG for a room join URL
            app.MapGet("/api/rooms/{id}/qr", Function(id As String, context As HttpContext) As IResult
                                                  Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                  Dim room = mgr.GetRoom(id)
                                                  If room Is Nothing Then
                                                      Return Results.NotFound(New With {.error = "Room not found"})
                                                  End If
                                                  Dim scheme = If(context.Request.IsHttps, "https", "http")
                                                  Dim host = context.Request.Host.ToString()
                                                  Dim joinUrl = $"{scheme}://{host}/index.html?room={id}"
                                                  Dim pngBytes = GenerateQrPng(joinUrl)
                                                  Return Results.File(pngBytes, "image/png", $"room-{id}.png")
                                              End Function)

            ' Claim host via stored token or admin PIN
            app.MapPost("/api/rooms/{id}/claim-host", Async Function(id As String, context As HttpContext) As Task
                                                            Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                            Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                                                            Dim serverOpts = If(opts?.Value, New ServerOptions())
                                                            Dim doc As JsonDocument = Nothing
                                                            Dim ok = False
                                                            Dim failed = False
                                                            Try
                                                                doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                                Dim root = doc.RootElement
                                                                Dim tokenProp As JsonElement = Nothing
                                                                Dim cidProp As JsonElement = Nothing
                                                                Dim pinProp As JsonElement = Nothing
                                                                Dim hostToken = ""
                                                                Dim clientId = ""
                                                                Dim pin = ""
                                                                If root.TryGetProperty("hostToken", tokenProp) Then hostToken = If(tokenProp.GetString(), "")
                                                                If root.TryGetProperty("clientId", cidProp) Then clientId = If(cidProp.GetString(), "")
                                                                If root.TryGetProperty("pin", pinProp) Then pin = If(pinProp.GetString(), "")
                                                                Dim adminPinValid = Not String.IsNullOrEmpty(pin) AndAlso
                                                                                    Not String.IsNullOrEmpty(serverOpts.AdminPin) AndAlso
                                                                                    String.Equals(pin, serverOpts.AdminPin, StringComparison.Ordinal)
                                                                ok = mgr.ClaimHost(id, hostToken, clientId, adminPinValid)
                                                            Catch ex As Exception
                                                                AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/claim-host error: {ex.Message}")
                                                                failed = True
                                                            Finally
                                                                doc?.Dispose()
                                                            End Try
                                                            If failed Then context.Response.StatusCode = 400
                                                            Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                        End Function)

            ' Kick a client from a room (host only)
            app.MapPost("/api/rooms/{id}/kick", Async Function(id As String, context As HttpContext) As Task
                                                      Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                      Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                      Dim doc As JsonDocument = Nothing
                                                      Dim ok = False
                                                      Dim failed = False
                                                      Try
                                                          doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                          Dim root = doc.RootElement
                                                          Dim targetProp As JsonElement = Nothing
                                                          Dim reqProp As JsonElement = Nothing
                                                          Dim targetClientId = ""
                                                          Dim requestingClientId = ""
                                                          If root.TryGetProperty("clientId", targetProp) Then targetClientId = If(targetProp.GetString(), "")
                                                          If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                          ok = mgr.KickClient(id, targetClientId, requestingClientId)
                                                          If ok Then
                                                              hub.SendToClient(targetClientId, "{""type"":""kicked""}")
                                                              hub.BroadcastToRoom(id, "{""type"":""memberLeft"",""clientId"":""" & targetClientId & """}", "")
                                                          End If
                                                      Catch ex As Exception
                                                          AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/kick error: {ex.Message}")
                                                          failed = True
                                                      Finally
                                                          doc?.Dispose()
                                                      End Try
                                                      If failed Then context.Response.StatusCode = 400
                                                      Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                  End Function)

            ' Lock/unlock a room (host only)
            app.MapPost("/api/rooms/{id}/lock", Async Function(id As String, context As HttpContext) As Task
                                                      Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                      Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                      Dim doc As JsonDocument = Nothing
                                                      Dim ok = False
                                                      Dim failed = False
                                                      Try
                                                          doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                          Dim root = doc.RootElement
                                                          Dim lockedProp As JsonElement = Nothing
                                                          Dim reqProp As JsonElement = Nothing
                                                          Dim locked = False
                                                          Dim requestingClientId = ""
                                                          If root.TryGetProperty("locked", lockedProp) Then locked = lockedProp.GetBoolean()
                                                          If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                          ok = mgr.SetLocked(id, locked, requestingClientId)
                                                          If ok Then
                                                              hub.BroadcastToRoom(id, "{""type"":""roomLocked"",""locked"":" & If(locked, "true", "false") & "}", "")
                                                          End If
                                                      Catch ex As Exception
                                                          AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/lock error: {ex.Message}")
                                                          failed = True
                                                      Finally
                                                          doc?.Dispose()
                                                      End Try
                                                      If failed Then context.Response.StatusCode = 400
                                                      Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                  End Function)

            ' Set PTT mode for a room (host only)
            app.MapPost("/api/rooms/{id}/ptt-mode", Async Function(id As String, context As HttpContext) As Task
                                                          Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                          Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                          Dim doc As JsonDocument = Nothing
                                                          Dim ok = False
                                                          Dim failed = False
                                                          Try
                                                              doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                              Dim root = doc.RootElement
                                                              Dim modeProp As JsonElement = Nothing
                                                              Dim reqProp As JsonElement = Nothing
                                                              Dim mode = "hold"
                                                              Dim requestingClientId = ""
                                                              If root.TryGetProperty("mode", modeProp) Then mode = If(modeProp.GetString(), "hold")
                                                              If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                              ok = mgr.SetPttMode(id, mode, requestingClientId)
                                                              If ok Then
                                                                  hub.BroadcastToRoom(id, "{""type"":""pttModeChanged"",""mode"":""" & mode & """}", "")
                                                              End If
                                                          Catch ex As Exception
                                                              AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/ptt-mode error: {ex.Message}")
                                                              failed = True
                                                          Finally
                                                              doc?.Dispose()
                                                          End Try
                                                          If failed Then context.Response.StatusCode = 400
                                                          Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                      End Function)

            ' Clear every client's captions in the room back to empty (host only)
            app.MapPost("/api/rooms/{id}/clear", Async Function(id As String, context As HttpContext) As Task
                                                       Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                       Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                       Dim doc As JsonDocument = Nothing
                                                       Dim ok = False
                                                       Dim failed = False
                                                       Try
                                                           doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                           Dim reqProp As JsonElement = Nothing
                                                           Dim requestingClientId = ""
                                                           If doc.RootElement.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                           Dim room = mgr.GetRoom(id)
                                                           If room IsNot Nothing AndAlso room.HostClientId = requestingClientId Then
                                                               hub.BroadcastToRoom(id, "{""type"":""clear""}", "")
                                                               ok = True
                                                           End If
                                                       Catch ex As Exception
                                                           AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/clear error: {ex.Message}")
                                                           failed = True
                                                       Finally
                                                           doc?.Dispose()
                                                       End Try
                                                       If failed Then context.Response.StatusCode = 400
                                                       Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                   End Function)

            ' Pause/resume commit broadcasting (host only)
            app.MapPost("/api/rooms/{id}/pause", Async Function(id As String, context As HttpContext) As Task
                                                       Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                       Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                       Dim doc As JsonDocument = Nothing
                                                       Dim ok = False
                                                       Dim failed = False
                                                       Try
                                                           doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                           Dim root = doc.RootElement
                                                           Dim pausedProp As JsonElement = Nothing
                                                           Dim reqProp As JsonElement = Nothing
                                                           Dim paused = False
                                                           Dim requestingClientId = ""
                                                           If root.TryGetProperty("paused", pausedProp) Then paused = pausedProp.GetBoolean()
                                                           If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                           ok = mgr.SetPaused(id, paused, requestingClientId)
                                                           If ok Then
                                                               hub.BroadcastToRoom(id, "{""type"":""pauseStateChanged"",""paused"":" & If(paused, "true", "false") & "}", "")
                                                           End If
                                                       Catch ex As Exception
                                                           AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/pause error: {ex.Message}")
                                                           failed = True
                                                       Finally
                                                           doc?.Dispose()
                                                       End Try
                                                       If failed Then context.Response.StatusCode = 400
                                                       Await context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                   End Function)

            ' Get room members (clients + virtual members)
            app.MapGet("/api/rooms/{id}/members", Function(id As String, context As HttpContext) As Task
                                                        Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                        Dim subtitleSvc = TryCast(context.RequestServices.GetRequiredService(Of ISubtitleService)(), SubtitleService)
                                                        Dim room = mgr.GetRoom(id)
                                                        If room Is Nothing Then
                                                            context.Response.StatusCode = 404
                                                            Return context.Response.WriteAsJsonAsync(New With {.error = "Room not found"})
                                                        End If
                                                        Dim members As New List(Of RoomMemberDto)()
                                                        ' Real clients
                                                        For Each cid In room.ClientIds.Keys
                                                            Dim client = subtitleSvc?.GetClient(cid)
                                                            If client IsNot Nothing Then
                                                                members.Add(New RoomMemberDto With {
                                                                    .ClientId = cid,
                                                                    .DisplayName = If(String.IsNullOrEmpty(client.DisplayName), "Guest", client.DisplayName),
                                                                    .Language = If(client.Language, ""),
                                                                    .Virtual = False
                                                                })
                                                            End If
                                                        Next
                                                        ' Virtual members
                                                        For Each vm In room.VirtualMembers.Values
                                                            members.Add(New RoomMemberDto With {
                                                                .ClientId = vm.Id,
                                                                .DisplayName = vm.Name,
                                                                .Language = If(vm.Language, ""),
                                                                .Virtual = True
                                                            })
                                                        Next
                                                        Return context.Response.WriteAsJsonAsync(members)
                                                    End Function)

            ' Add a virtual member (host only)
            app.MapPost("/api/rooms/{id}/virtual-members", Async Function(id As String, context As HttpContext) As Task
                                                                 Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                                 Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                                 Dim doc As JsonDocument = Nothing
                                                                 Dim vm As VirtualMember = Nothing
                                                                 Dim failed = False
                                                                 Dim notAuth = False
                                                                 Try
                                                                     doc = Await JsonDocument.ParseAsync(context.Request.Body)
                                                                     Dim root = doc.RootElement
                                                                     Dim nameProp As JsonElement = Nothing
                                                                     Dim langProp As JsonElement = Nothing
                                                                     Dim reqProp As JsonElement = Nothing
                                                                     Dim vmName = ""
                                                                     Dim vmLang = ""
                                                                     Dim requestingClientId = ""
                                                                     If root.TryGetProperty("name", nameProp) Then vmName = If(nameProp.GetString(), "")
                                                                     If root.TryGetProperty("language", langProp) Then vmLang = If(langProp.GetString(), "")
                                                                     If root.TryGetProperty("requestingClientId", reqProp) Then requestingClientId = If(reqProp.GetString(), "")
                                                                     vm = mgr.AddVirtualMember(id, vmName, vmLang, requestingClientId)
                                                                     If vm Is Nothing Then notAuth = True
                                                                     If vm IsNot Nothing Then
                                                                         hub.BroadcastToRoom(id, "{""type"":""virtualMemberAdded"",""id"":""" & vm.Id & """,""name"":" & SubtitleService.EscapeJson(vm.Name) & ",""language"":""" & If(vm.Language, "") & """}", "")
                                                                     End If
                                                                 Catch ex As Exception
                                                                     AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/virtual-members error: {ex.Message}")
                                                                     failed = True
                                                                 Finally
                                                                     doc?.Dispose()
                                                                 End Try
                                                                 If failed Then
                                                                     context.Response.StatusCode = 400
                                                                     Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid request"})
                                                                 ElseIf notAuth Then
                                                                     context.Response.StatusCode = 403
                                                                     Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized"})
                                                                 Else
                                                                     context.Response.StatusCode = 201
                                                                     Await context.Response.WriteAsJsonAsync(New With {
                                                                         .id = vm.Id,
                                                                         .name = vm.Name,
                                                                         .language = vm.Language
                                                                     })
                                                                 End If
                                                             End Function)

            ' Remove a virtual member (host only)
            app.MapDelete("/api/rooms/{id}/virtual-members/{vmId}", Function(id As String, vmId As String, context As HttpContext) As Task
                                                                          Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                                                          Dim hub = context.RequestServices.GetRequiredService(Of SubtitleHub)()
                                                                          Dim requestingClientId = If(context.Request.Query("requestingClientId").FirstOrDefault(), "")
                                                                          Dim ok = mgr.RemoveVirtualMember(id, vmId, requestingClientId)
                                                                          If ok Then
                                                                              hub.BroadcastToRoom(id, "{""type"":""virtualMemberRemoved"",""id"":""" & vmId & """}", "")
                                                                          End If
                                                                          If Not ok Then
                                                                              context.Response.StatusCode = 403
                                                                          End If
                                                                          Return context.Response.WriteAsJsonAsync(New With {.ok = ok})
                                                                      End Function)

            ' List all rooms (server dashboard — includes private rooms)
            app.MapGet("/api/rooms/all", Function(context As HttpContext) As Task
                                              Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                                              Dim rooms = mgr.GetAllRooms()
                                              Dim result = rooms.Select(Function(r) New With {
                                                  .id = r.Id,
                                                  .name = r.Name,
                                                  .type = r.Type.ToString().ToLower(),
                                                  .visibility = r.Visibility.ToString().ToLower(),
                                                  .clients = r.ClientCount,
                                                  .createdAt = r.CreatedAt
                                              }).ToArray()
                                              Return context.Response.WriteAsJsonAsync(result)
                                          End Function)

            ' Translate text on demand (shared-device clients translate cached messages on identity switch)
            app.MapPost("/api/translate", Async Function(context As HttpContext) As Task
                                              Dim translationSvc = context.RequestServices.GetService(Of ITranslationService)()
                                              If translationSvc Is Nothing Then
                                                  context.Response.StatusCode = 503
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Translation not available"})
                                                  Return
                                              End If

                                              Dim body As JsonElement = Nothing
                                              Dim parseOk = True
                                              Try
                                                  body = Await JsonSerializer.DeserializeAsync(Of JsonElement)(context.Request.Body)
                                              Catch
                                                  parseOk = False
                                              End Try
                                              If Not parseOk Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid JSON"})
                                                  Return
                                              End If

                                              Dim textProp As JsonElement = Nothing
                                              Dim srcProp As JsonElement = Nothing
                                              Dim tgtProp As JsonElement = Nothing
                                              If Not body.TryGetProperty("text", textProp) OrElse
                                                 Not body.TryGetProperty("sourceLang", srcProp) OrElse
                                                 Not body.TryGetProperty("targetLang", tgtProp) Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Missing text, sourceLang, or targetLang"})
                                                  Return
                                              End If

                                              Dim text = If(textProp.GetString(), "")
                                              Dim sourceLang = If(srcProp.GetString(), "")
                                              Dim targetLang = If(tgtProp.GetString(), "")

                                              If String.IsNullOrEmpty(text) OrElse String.IsNullOrEmpty(sourceLang) OrElse String.IsNullOrEmpty(targetLang) Then
                                                  context.Response.StatusCode = 400
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Empty text, sourceLang, or targetLang"})
                                                  Return
                                              End If

                                              If sourceLang = targetLang Then
                                                  Await context.Response.WriteAsJsonAsync(New With {.text = text})
                                                  Return
                                              End If

                                              Dim translated = text
                                              Dim translateOk = True
                                              Try
                                                  Dim results = Await translationSvc.TranslateAsync(
                                                      text, sourceLang, New List(Of String) From {targetLang}, context.RequestAborted)
                                                  If results IsNot Nothing AndAlso results.ContainsKey(targetLang) Then
                                                      translated = results(targetLang)
                                                  End If
                                              Catch ex As Exception
                                                  translateOk = False
                                              End Try
                                              If Not translateOk Then
                                                  context.Response.StatusCode = 500
                                                  Await context.Response.WriteAsJsonAsync(New With {.error = "Translation failed"})
                                                  Return
                                              End If
                                              Await context.Response.WriteAsJsonAsync(New With {.text = translated})
                                          End Function)

        End Sub

    End Module
End Namespace
