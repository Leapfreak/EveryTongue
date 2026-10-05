Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports EveryTongue.Services.Audio
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Rooms

Namespace Server

    ''' <summary>
    ''' /api/rooms/{id}/audio-input - the host panel's audio-input picker for conference
    ''' rooms that capture on the server machine. The input used to be changeable only in
    ''' the desktop Template Manager (Tools, Options, Template Manager, Edit, Save) - on a
    ''' different device from the phone the host runs the room with, for a setting that
    ''' changes whenever the detachable USB line-in is reconnected.
    '''   GET  lists the server's input devices (one per name) with the room's current and
    '''        saved input, and the saved input that was missing at room start, if any.
    '''   POST switches the room to an input: saves it to the room's template BY NAME (so the
    '''        next room starts with it) and restarts the room's capture on it.
    ''' Host only (room host client id or host token), like the other room controls.
    ''' </summary>
    Partial Public Module EndpointRegistration

        Private Sub MapAudioInputEndpoints(app As IEndpointRouteBuilder)

            app.MapGet("/api/rooms/{id}/audio-input", Async Function(id As String, context As HttpContext) As Task
                Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                Dim room = mgr.GetRoom(id)
                If room Is Nothing Then
                    context.Response.StatusCode = 404
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Room not found", .errorCode = "roomNotFound"})
                    Return
                End If
                If Not IsRoomHost(room, context.Request.Query("clientId").ToString(), context.Request.Query("hostToken").ToString()) Then
                    context.Response.StatusCode = 403
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized", .errorCode = "notAuthorized"})
                    Return
                End If
                If room.Type <> RoomType.Conference OrElse
                   String.Equals(room.AudioSource, "web", StringComparison.OrdinalIgnoreCase) Then
                    ' Web-mic rooms take their audio from the host's browser - nothing to pick here.
                    Await context.Response.WriteAsJsonAsync(New With {.source = "web"})
                    Return
                End If

                Dim devices = Await EnumerateInputsLoggedAsync(id).ConfigureAwait(False)
                If devices Is Nothing Then
                    context.Response.StatusCode = 503
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Device list unavailable", .errorCode = "devicesUnavailable"})
                    Return
                End If

                Dim saved = ""
                Dim cfg = SettingsConfigProvider?.Invoke()
                If cfg IsNot Nothing Then
                    SyncLock _templatesLock
                        Dim tpl = cfg.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = room.TemplateId)
                        If tpl IsNot Nothing Then saved = If(tpl.AudioDeviceName, "").Trim()
                    End SyncLock
                End If

                Await context.Response.WriteAsJsonAsync(New With {
                    .source = "local",
                    .current = If(room.AudioDeviceName, ""),
                    .saved = saved,
                    .missing = If(room.AudioDeviceMissing, ""),
                    .devices = InputDeviceCatalog.DistinctByName(devices).Select(Function(d) d.Name.Trim()).ToList()
                })
            End Function)

            app.MapPost("/api/rooms/{id}/audio-input", Async Function(id As String, context As HttpContext) As Task
                Dim mgr = context.RequestServices.GetRequiredService(Of RoomManager)()
                Dim room = mgr.GetRoom(id)
                If room Is Nothing Then
                    context.Response.StatusCode = 404
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Room not found", .errorCode = "roomNotFound"})
                    Return
                End If

                Dim clientId = ""
                Dim hostToken = ""
                Dim deviceName = ""
                Dim badBody = False
                Try
                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body).ConfigureAwait(False)
                        clientId = If(JsonStr(doc.RootElement, "clientId"), "")
                        hostToken = If(JsonStr(doc.RootElement, "hostToken"), "")
                        deviceName = If(JsonStr(doc.RootElement, "deviceName"), "").Trim()
                    End Using
                Catch ex As JsonException
                    AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{id}/audio-input: malformed request body ({ex.Message})")
                    badBody = True
                End Try
                If badBody Then
                    context.Response.StatusCode = 400
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid request", .errorCode = "invalidRequest"})
                    Return
                End If

                If Not IsRoomHost(room, clientId, hostToken) Then
                    context.Response.StatusCode = 403
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Not authorized", .errorCode = "notAuthorized"})
                    Return
                End If
                If room.Type <> RoomType.Conference OrElse
                   String.Equals(room.AudioSource, "web", StringComparison.OrdinalIgnoreCase) OrElse
                   deviceName.Length = 0 Then
                    context.Response.StatusCode = 400
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Invalid request", .errorCode = "invalidRequest"})
                    Return
                End If

                ' The name must be a CURRENT input device - re-checked here, because the list
                ' the host picked from may be stale (the USB box was unplugged since).
                Dim current = Await EnumerateInputsLoggedAsync(id).ConfigureAwait(False)
                If current Is Nothing Then
                    context.Response.StatusCode = 503
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Device list unavailable", .errorCode = "devicesUnavailable"})
                    Return
                End If
                Dim chosen = InputDeviceCatalog.FindByName(current, deviceName)
                If chosen Is Nothing Then
                    context.Response.StatusCode = 409
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Device not found", .errorCode = "deviceNotFound"})
                    Return
                End If

                Dim cfg = SettingsConfigProvider?.Invoke()
                Dim handler = PipelineConfigHandler
                If cfg Is Nothing OrElse handler Is Nothing Then
                    context.Response.StatusCode = 503
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Pipeline not available", .errorCode = "pipelineUnavailable"})
                    Return
                End If

                Dim chosenName = chosen.Name.Trim()
                Dim previous = ""
                Dim tplName = ""
                SyncLock _templatesLock
                    Dim tpl = cfg.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = room.TemplateId)
                    If tpl IsNot Nothing Then
                        previous = If(tpl.AudioDeviceName, "")
                        tplName = tpl.Name
                        tpl.AudioSource = "local"
                        tpl.WebMicRaw = False
                        tpl.AudioDeviceId = chosen.Id
                        tpl.AudioDeviceName = chosenName
                        tpl.AudioSourceLabel = chosenName
                        context.RequestServices.GetService(Of TemplateStore)?.SyncFromConfig(cfg.ConferenceTemplates)
                    End If
                End SyncLock
                If tplName.Length = 0 Then
                    context.Response.StatusCode = 409
                    Await context.Response.WriteAsJsonAsync(New With {.error = "Template not found", .errorCode = "templateNotFound"})
                    Return
                End If
                SettingsSaveHandler?.Invoke()

                AppLogger.Log(LogEvents.AUDIO_DEVICE_CHANGED,
                    $"room={id}: audio input changed by the host '{previous}' -> '{chosenName}' (saved to template '{tplName}'); restarting capture")
                handler.Invoke(id, New Dictionary(Of String, Object) From {{"audioDevice", chosenName}})
                room.TouchActivity()

                Await context.Response.WriteAsJsonAsync(New With {.ok = True, .device = chosenName})
            End Function)

        End Sub

        ''' <summary>The server's input devices off the request thread; Nothing (logged) when
        ''' enumeration fails.</summary>
        Private Async Function EnumerateInputsLoggedAsync(roomId As String) As Task(Of List(Of Services.Models.AudioDeviceInfo))
            Try
                Return Await Task.Run(Function() InputDeviceCatalog.Enumerate()).ConfigureAwait(False)
            Catch ex As Exception
                AppLogger.Log(LogEvents.SERVER_ERROR, $"/rooms/{roomId}/audio-input: device enumeration failed: {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>True when the caller is the room's host (client id or host token).</summary>
        Private Function IsRoomHost(room As Room, clientId As String, hostToken As String) As Boolean
            Return (Not String.IsNullOrEmpty(hostToken) AndAlso room.HostToken = hostToken) OrElse
                   RoomManager.IsHost(room, clientId)
        End Function

    End Module

End Namespace
