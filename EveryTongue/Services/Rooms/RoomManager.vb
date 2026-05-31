Imports System.Collections.Concurrent
Imports EveryTongue.Services.Infrastructure

Namespace Services.Rooms

    ''' <summary>
    ''' Manages room lifecycle: create, list, join, leave, destroy.
    ''' Thread-safe — called from HTTP endpoints and WebSocket handlers concurrently.
    ''' </summary>
    Public Class RoomManager

        Private ReadOnly _rooms As New ConcurrentDictionary(Of String, Room)()
        Private ReadOnly _idleTimer As System.Timers.Timer

        Public Sub New()
            ' Check for idle rooms every 60 seconds
            _idleTimer = New System.Timers.Timer(60000)
            AddHandler _idleTimer.Elapsed, Sub(s, e) CleanupIdleRooms()
            _idleTimer.AutoReset = True
            _idleTimer.Start()
        End Sub

        ''' <summary>
        ''' Create a new room. Returns the created room.
        ''' </summary>
        Public Function CreateRoom(name As String,
                                    type As RoomType,
                                    visibility As RoomVisibility,
                                    hostClientId As String) As Room

            Dim room As New Room() With {
                .Id = GenerateRoomId(),
                .Name = If(name, ""),
                .Type = type,
                .Visibility = visibility,
                .HostClientId = hostClientId,
                .HostToken = Guid.NewGuid().ToString("N")
            }

            ' Conversation rooms default to private
            If type = RoomType.Conversation AndAlso visibility = RoomVisibility.Public Then
                room.Visibility = RoomVisibility.Private
            End If

            ' Private conversation rooms don't retain transcripts by default
            If type = RoomType.Conversation AndAlso room.Visibility = RoomVisibility.Private Then
                room.Config.RetainTranscript = False
            End If

            _rooms.TryAdd(room.Id, room)
            AppLogger.Log($"[RoomManager] Created room '{room.Name}' ({room.Type}, {room.Visibility}) id={room.Id}")
            Return room
        End Function

        ''' <summary>
        ''' Get a room by ID. Returns Nothing if not found or inactive.
        ''' </summary>
        Public Function GetRoom(roomId As String) As Room
            If String.IsNullOrEmpty(roomId) Then Return Nothing
            Dim room As Room = Nothing
            If _rooms.TryGetValue(roomId, room) AndAlso room.IsActive Then
                Return room
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' List all active public rooms (for the lobby).
        ''' </summary>
        Public Function GetPublicRooms() As List(Of Room)
            Return _rooms.Values.
                Where(Function(r) r.IsActive AndAlso r.Visibility = RoomVisibility.Public).
                OrderByDescending(Function(r) r.CreatedAt).
                ToList()
        End Function

        ''' <summary>
        ''' List all active rooms (for the server dashboard).
        ''' </summary>
        Public Function GetAllRooms() As List(Of Room)
            Return _rooms.Values.
                Where(Function(r) r.IsActive).
                OrderByDescending(Function(r) r.CreatedAt).
                ToList()
        End Function

        ''' <summary>
        ''' Add a client to a room. Returns False if room not found or is locked.
        ''' </summary>
        Public Function JoinRoom(roomId As String, clientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If room.IsLocked Then
                AppLogger.Log($"[RoomManager] Client {clientId} rejected from locked room '{room.Name}'")
                Return False
            End If
            room.ClientIds.TryAdd(clientId, 0)
            room.TouchActivity()

            ' If the room has no host yet, the first joiner becomes host
            If String.IsNullOrEmpty(room.HostClientId) Then
                room.HostClientId = clientId
                AppLogger.Log($"[RoomManager] Client {clientId} claimed host of room '{room.Name}'")
            End If

            AppLogger.Log($"[RoomManager] Client {clientId} joined room '{room.Name}' ({room.ClientCount} clients)")
            Return True
        End Function

        ''' <summary>
        ''' Reclaim host via stored host token. Returns True on success.
        ''' </summary>
        Public Function ClaimHost(roomId As String, hostToken As String, newClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If String.IsNullOrEmpty(hostToken) OrElse room.HostToken <> hostToken Then Return False
            room.HostClientId = newClientId
            room.TouchActivity()
            AppLogger.Log($"[RoomManager] Host reclaimed for room '{room.Name}' by {newClientId}")
            Return True
        End Function

        ''' <summary>
        ''' Kick a client from a room. Only the host can kick.
        ''' </summary>
        Public Function KickClient(roomId As String, clientId As String, requestingClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If room.HostClientId <> requestingClientId Then Return False
            Dim dummy As Byte
            Dim removed = room.ClientIds.TryRemove(clientId, dummy)
            If removed Then
                room.TouchActivity()
                AppLogger.Log($"[RoomManager] Client {clientId} kicked from room '{room.Name}' by host {requestingClientId}")
            End If
            Return removed
        End Function

        ''' <summary>
        ''' Lock or unlock a room. Only the host can change lock state.
        ''' </summary>
        Public Function SetLocked(roomId As String, locked As Boolean, requestingClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If room.HostClientId <> requestingClientId Then Return False
            room.IsLocked = locked
            room.TouchActivity()
            AppLogger.Log($"[RoomManager] Room '{room.Name}' locked={locked} by host {requestingClientId}")
            Return True
        End Function

        ''' <summary>
        ''' Set PTT mode for a room. Only the host can change.
        ''' </summary>
        Public Function SetPttMode(roomId As String, mode As String, requestingClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If room.HostClientId <> requestingClientId Then Return False
            If mode <> "hold" AndAlso mode <> "toggle" Then Return False
            room.Config.PttMode = mode
            room.TouchActivity()
            AppLogger.Log($"[RoomManager] Room '{room.Name}' PTT mode={mode} by host {requestingClientId}")
            Return True
        End Function

        ''' <summary>
        ''' Add a virtual member to a room. Host only.
        ''' </summary>
        Public Function AddVirtualMember(roomId As String, name As String, language As String, requestingClientId As String) As VirtualMember
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return Nothing
            If room.HostClientId <> requestingClientId Then Return Nothing
            Dim vm As New VirtualMember() With {
                .Id = "vm" & Guid.NewGuid().ToString("N").Substring(0, 8),
                .Name = If(name, "Guest"),
                .Language = If(language, "")
            }
            room.VirtualMembers.TryAdd(vm.Id, vm)
            room.TouchActivity()
            AppLogger.Log($"[RoomManager] Virtual member '{vm.Name}' ({vm.Language}) added to room '{room.Name}'")
            Return vm
        End Function

        ''' <summary>
        ''' Remove a virtual member from a room. Host only.
        ''' </summary>
        Public Function RemoveVirtualMember(roomId As String, vmId As String, requestingClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False
            If room.HostClientId <> requestingClientId Then Return False
            Dim removed As VirtualMember = Nothing
            Dim ok = room.VirtualMembers.TryRemove(vmId, removed)
            If ok Then
                room.TouchActivity()
                AppLogger.Log($"[RoomManager] Virtual member '{removed.Name}' removed from room '{room.Name}'")
            End If
            Return ok
        End Function

        ''' <summary>
        ''' Remove a client from a room.
        ''' </summary>
        Public Sub LeaveRoom(roomId As String, clientId As String)
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return
            Dim dummy As Byte
            room.ClientIds.TryRemove(clientId, dummy)
            room.TouchActivity()
            AppLogger.Log($"[RoomManager] Client {clientId} left room '{room.Name}' ({room.ClientCount} clients)")
        End Sub

        ''' <summary>
        ''' Close a room. Only the host or server can do this.
        ''' </summary>
        Public Function CloseRoom(roomId As String, requestingClientId As String) As Boolean
            Dim room = GetRoom(roomId)
            If room Is Nothing Then Return False

            ' Only host can close (empty requestingClientId = server-initiated)
            If Not String.IsNullOrEmpty(requestingClientId) AndAlso
               room.HostClientId <> requestingClientId Then
                Return False
            End If

            room.IsActive = False
            AppLogger.Log($"[RoomManager] Room '{room.Name}' closed (id={room.Id})")
            Return True
        End Function

        ''' <summary>
        ''' Remove a client from all rooms they might be in.
        ''' Called when a WebSocket disconnects.
        ''' </summary>
        Public Sub RemoveClientFromAllRooms(clientId As String)
            For Each room In _rooms.Values
                If room.IsActive Then
                    Dim dummy As Byte
                    If room.ClientIds.TryRemove(clientId, dummy) Then
                        room.TouchActivity()
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' Clean up rooms that have been idle (no clients) beyond their timeout.
        ''' </summary>
        Private Sub CleanupIdleRooms()
            Dim now = DateTime.Now
            For Each room In _rooms.Values
                If Not room.IsActive Then Continue For
                If room.ClientCount > 0 Then Continue For

                Dim idleMinutes = (now - room.LastActivityAt).TotalMinutes
                If idleMinutes >= room.Config.IdleTimeoutMinutes Then
                    room.IsActive = False
                    AppLogger.Log($"[RoomManager] Room '{room.Name}' auto-closed after {CInt(idleMinutes)}min idle (id={room.Id})")
                End If
            Next

            ' Purge inactive rooms older than 1 hour to free memory
            Dim cutoff = now.AddHours(-1)
            For Each kvp In _rooms
                If Not kvp.Value.IsActive AndAlso kvp.Value.LastActivityAt < cutoff Then
                    Dim removed As Room = Nothing
                    _rooms.TryRemove(kvp.Key, removed)
                End If
            Next
        End Sub

        Private Shared Function GenerateRoomId() As String
            ' Short, URL-safe ID — 6 chars from base36
            Dim chars = "abcdefghijklmnopqrstuvwxyz0123456789"
            Dim rng As New Random()
            Return New String(Enumerable.Range(0, 6).Select(Function(i) chars(rng.Next(chars.Length))).ToArray())
        End Function

    End Class

End Namespace
