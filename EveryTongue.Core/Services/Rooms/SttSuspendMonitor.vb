Imports System.Collections.Concurrent
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Rooms
    ''' <summary>
    ''' Closes a conference room's metered STT session (Speechmatics) while the room is
    ''' PAUSED, so a pause uses no free-tier minutes (Jeremy, 2026-10-06). The room stays
    ''' open. Two ways into a pause:
    '''   - the host presses Pause;
    '''   - nobody at all (host included) has been connected for EmptyGraceSeconds - the
    '''     server pauses the room itself, and only the host resumes it.
    ''' Rejected: suspending on silence (a speaker below a level threshold would never
    ''' wake it) and on "no guests" (phones drop off whenever their screens lock).
    ''' Engines without a metered session ignore the command.
    ''' </summary>
    Public NotInheritable Class SttSuspendMonitor
        Implements IDisposable

        Private Const TickMs As Integer = 5000
        Private Const EmptyGraceSeconds As Integer = 60
        ''' <summary>Re-send an unchanged pause this often: a capture restart inside the
        ''' same engine (e.g. an audio-input change) starts unpaused.</summary>
        Private Const RefreshSeconds As Integer = 300

        Private NotInheritable Class PushState
            Public Backend As ISttBackend
            Public Reasons As String
            Public SentAt As DateTime
        End Class

        Private ReadOnly _getRoomManager As Func(Of RoomManager)
        Private ReadOnly _getSubtitleSvc As Func(Of ISubtitleService)
        Private ReadOnly _getBackends As Func(Of IEnumerable(Of KeyValuePair(Of String, ISttBackend)))
        Private ReadOnly _pushed As New ConcurrentDictionary(Of String, PushState)()
        Private ReadOnly _emptySince As New ConcurrentDictionary(Of String, DateTime)()
        Private ReadOnly _timer As Timer
        Private _busy As Integer

        Public Sub New(getRoomManager As Func(Of RoomManager),
                       getSubtitleSvc As Func(Of ISubtitleService),
                       getBackends As Func(Of IEnumerable(Of KeyValuePair(Of String, ISttBackend))))
            _getRoomManager = getRoomManager
            _getSubtitleSvc = getSubtitleSvc
            _getBackends = getBackends
            _timer = New Timer(Sub(state) Tick(), Nothing, TickMs, TickMs)
        End Sub

        ''' <summary>Re-evaluate now (a host pause or resume must not wait for the next tick).</summary>
        Public Sub EvaluateNow()
            ThreadPool.QueueUserWorkItem(Sub(state) Tick())
        End Sub

        ''' <summary>The room's engine stopped: forget its state.</summary>
        Public Sub Forget(roomId As String)
            Dim ignoredPush As PushState = Nothing
            _pushed.TryRemove(roomId, ignoredPush)
            Dim ignoredSince As DateTime
            _emptySince.TryRemove(roomId, ignoredSince)
        End Sub

        Private Sub Tick()
            If Interlocked.Exchange(_busy, 1) = 1 Then Return
            Try
                Dim mgr = _getRoomManager?.Invoke()
                If mgr Is Nothing Then Return
                Dim svc = _getSubtitleSvc?.Invoke()
                For Each kvp In _getBackends().ToList()
                    If kvp.Value Is Nothing Then Continue For
                    Dim room = mgr.GetRoom(kvp.Key)
                    If room Is Nothing OrElse room.Type <> RoomType.Conference Then Continue For
                    Evaluate(mgr, room, kvp.Value, svc)
                Next
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_STT_SUSPEND, $"suspend check failed: {ex.Message}")
            Finally
                Interlocked.Exchange(_busy, 0)
            End Try
        End Sub

        Private Sub Evaluate(mgr As RoomManager, room As Room, backend As ISttBackend, svc As ISubtitleService)
            Dim now = DateTime.Now

            ' Nobody connected (host included) for the grace period -> the server pauses.
            Dim connected = room.ClientIds.Keys.Where(
                Function(c) svc Is Nothing OrElse svc.IsClientConnected(c)).Count()
            If connected = 0 AndAlso Not room.Config.IsPaused Then
                Dim since = _emptySince.GetOrAdd(room.Id, now)
                If (now - since).TotalSeconds >= EmptyGraceSeconds Then
                    mgr.AutoPause(room.Id, $"nobody connected for {EmptyGraceSeconds}s")
                End If
            Else
                Dim ignored As DateTime
                _emptySince.TryRemove(room.Id, ignored)
            End If

            Dim reasons = If(room.Config.IsPaused, "pause", "")
            Dim prev As PushState = Nothing
            _pushed.TryGetValue(room.Id, prev)
            Dim sameEngine = prev IsNot Nothing AndAlso prev.Backend Is backend
            ' Nothing pushed to this engine yet = it is not suspended.
            Dim prevReasons = If(sameEngine, prev.Reasons, "")
            Dim changed = reasons <> prevReasons
            Dim refresh = sameEngine AndAlso reasons <> "" AndAlso (now - prev.SentAt).TotalSeconds >= RefreshSeconds
            If Not changed AndAlso Not refresh Then Return

            _pushed(room.Id) = New PushState With {.Backend = backend, .Reasons = reasons, .SentAt = now}
            If changed Then
                AppLogger.Log(LogEvents.CONF_STT_SUSPEND,
                    $"room={room.Id} STT session {If(reasons = "", "reopened (room resumed)", "suspended (room paused)")}, connected={connected}")
            End If
            Dim send = backend.UpdateConfigAsync(New Dictionary(Of String, Object) From {{"suspend", reasons}})
            send.ContinueWith(Sub(t) AppLogger.Log(LogEvents.CONF_STT_SUSPEND,
                                  $"room={room.Id} suspend push failed: {t.Exception?.GetBaseException().Message}"),
                              TaskContinuationOptions.OnlyOnFaulted)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _timer.Dispose()
        End Sub
    End Class
End Namespace
