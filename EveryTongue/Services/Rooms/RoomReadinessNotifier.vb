Imports System.Collections.Concurrent
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Rooms
    ''' <summary>
    ''' Tracks per-room engine readiness and relays it to web room clients so people
    ''' don't speak before STT is capturing (lost first words) and can see when the
    ''' translation model has finished warming. Messages are TRANSIENT: the client shows
    ''' them while loading and removes them once ready (no persistent clutter).
    '''
    ''' Wire protocol (room-scoped WebSocket text frame):
    '''   {"type":"roomStatus","scope":"stt"|"translation","state":"preparing"|"ready"}
    '''
    ''' STT readiness gates the mic/PTT on the client; translation readiness is a
    ''' non-blocking note (you can speak before it loads — the translation just appears
    ''' once it's ready). Cloud/inline translation engines are treated ready immediately
    ''' (no note). The poll loops are fail-open: on timeout they broadcast "ready" so a
    ''' dropped message can never permanently trap a client's mic.
    ''' </summary>
    Public Class RoomReadinessNotifier
        Private ReadOnly _subtitle As ISubtitleService
        Private ReadOnly _logger As ILogger(Of RoomReadinessNotifier)
        Private ReadOnly _states As New ConcurrentDictionary(Of String, RoomState)()

        Private Class RoomState
            Public Watching As Boolean
            Public SttReady As Boolean
            Public TranslationApplies As Boolean
            Public TranslationReady As Boolean
        End Class

        Public Sub New(subtitle As ISubtitleService, logger As ILogger(Of RoomReadinessNotifier))
            _subtitle = subtitle
            _logger = logger
        End Sub

        ''' <summary>True when a readiness watch is already running for the room.</summary>
        Public Function IsWatching(roomId As String) As Boolean
            Dim st As RoomState = Nothing
            Return _states.TryGetValue(roomId, st) AndAlso st.Watching
        End Function

        ''' <summary>
        ''' Begin (idempotent) watching a room's readiness. Broadcasts "preparing"
        ''' immediately for STT (and translation when a probe is supplied), polls each
        ''' probe until ready or timeout, then broadcasts the "ready" transition.
        ''' A Nothing translationProbe means translation needs no note (cloud/inline/none).
        ''' </summary>
        Public Sub Watch(roomId As String,
                         sttProbe As Func(Of CancellationToken, Task(Of Boolean)),
                         translationProbe As Func(Of CancellationToken, Task(Of Boolean)),
                         Optional sttAlreadyReady As Boolean = False,
                         Optional timeoutSeconds As Integer = 60)
            If String.IsNullOrEmpty(roomId) OrElse sttProbe Is Nothing Then Return
            Dim st = _states.GetOrAdd(roomId, Function(k) New RoomState())
            SyncLock st
                If st.Watching Then
                    ' Already tracked — do NOT re-broadcast to the whole room (that would flash
                    ' the indicator at members who already saw it / are unaffected). A new client
                    ' gets the current state targeted via ResendStateToClient instead.
                    Return
                End If
                st.Watching = True
                st.SttReady = sttAlreadyReady
                st.TranslationApplies = (translationProbe IsNot Nothing)
                st.TranslationReady = (translationProbe Is Nothing)
            End SyncLock

            ' Only announce engines that are STILL loading. If STT is already ready (e.g. a second
            ' conversation room reusing the already-warm shared live-server) nothing is broadcast,
            ' so unaffected people never see a message.
            BroadcastPreparing(roomId, st)

            Task.Run(Async Function()
                         Try
                             If Not st.SttReady Then
                                 Await PollUntil(sttProbe, timeoutSeconds, 400).ConfigureAwait(False)
                                 st.SttReady = True
                                 Send(roomId, "stt", "ready")
                                 AppLogger.Log(LogEvents.ROOM_READINESS, $"room={roomId} STT ready")
                             End If
                             If st.TranslationApplies AndAlso Not st.TranslationReady AndAlso translationProbe IsNot Nothing Then
                                 Await PollUntil(translationProbe, timeoutSeconds, 500).ConfigureAwait(False)
                                 st.TranslationReady = True
                                 Send(roomId, "translation", "ready")
                                 AppLogger.Log(LogEvents.ROOM_READINESS, $"room={roomId} translation ready")
                             End If
                         Catch
                         End Try
                     End Function)
        End Sub

        ''' <summary>Poll the probe every intervalMs until it returns True or the timeout elapses (fail-open).</summary>
        Private Shared Async Function PollUntil(probe As Func(Of CancellationToken, Task(Of Boolean)),
                                                timeoutSeconds As Integer, intervalMs As Integer) As Task
            Dim startTick = Environment.TickCount64
            Dim limit As Long = CLng(timeoutSeconds) * 1000
            While Environment.TickCount64 - startTick < limit
                Dim ok = False
                Try
                    ok = Await probe(CancellationToken.None).ConfigureAwait(False)
                Catch
                    ok = False
                End Try
                If ok Then Return
                Await Task.Delay(intervalMs).ConfigureAwait(False)
            End While
            ' Timed out — caller treats the engine as ready so the mic is never trapped.
        End Function

        ''' <summary>
        ''' Re-send the current readiness state to one (late-joining) client — but ONLY for
        ''' engines that are still loading. A joiner of an already-ready room is told nothing
        ''' (their mic stays enabled by default), so unaffected people never see a message.
        ''' </summary>
        Public Sub ResendStateToClient(roomId As String, clientId As String)
            Dim st As RoomState = Nothing
            If Not _states.TryGetValue(roomId, st) Then Return
            If Not st.SttReady Then SendTo(clientId, "stt", "preparing")
            If st.TranslationApplies AndAlso Not st.TranslationReady Then
                SendTo(clientId, "translation", "preparing")
            End If
        End Sub

        ''' <summary>Forget a room's readiness state (on room close).</summary>
        Public Sub ClearRoom(roomId As String)
            Dim st As RoomState = Nothing
            _states.TryRemove(roomId, st)
        End Sub

        ''' <summary>Broadcast "preparing" to the room, but only for engines that are still loading.</summary>
        Private Sub BroadcastPreparing(roomId As String, st As RoomState)
            If Not st.SttReady Then Send(roomId, "stt", "preparing")
            If st.TranslationApplies AndAlso Not st.TranslationReady Then
                Send(roomId, "translation", "preparing")
            End If
        End Sub

        Private Sub Send(roomId As String, scope As String, state As String)
            _subtitle.BroadcastRawToRoom(roomId, Msg(scope, state), "")
        End Sub

        Private Sub SendTo(clientId As String, scope As String, state As String)
            _subtitle.SendRawToClient(clientId, Msg(scope, state))
        End Sub

        Private Shared Function Msg(scope As String, state As String) As String
            Return $"{{""type"":""roomStatus"",""scope"":""{scope}"",""state"":""{state}""}}"
        End Function
    End Class
End Namespace
