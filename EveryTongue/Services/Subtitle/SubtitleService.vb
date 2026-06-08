Imports System.Collections.Concurrent
Imports System.Net.WebSockets
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server
Imports EveryTongue.Services.Audio
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Subtitle
    ''' <summary>
    ''' Core subtitle service — manages client state, broadcast, and history.
    ''' </summary>
    Public Class SubtitleService
        Implements ISubtitleService

        Private ReadOnly _logger As ILogger(Of SubtitleService)
        Private ReadOnly _options As ServerOptions
        Private ReadOnly _clients As New ConcurrentDictionary(Of String, ClientConnection)()
        Private ReadOnly _committedLines As New ConcurrentQueue(Of CommittedEntry)()
        Private _lastCommittedEntry As CommittedEntry
        Private _commitCounter As Integer = 0
        Private _currentLine As String = ""

        ' Chains TTS tasks per (room, lang) so notifications go out in commit order
        Private ReadOnly _ttsChains As New ConcurrentDictionary(Of String, Task)()

        ' ── Events ──

        Public Event StatusChanged As EventHandler(Of String) Implements ISubtitleService.StatusChanged
        Public Event RemoteCommand As EventHandler(Of String) Implements ISubtitleService.RemoteCommand
        Public Event ActiveLanguagesChanged As EventHandler Implements ISubtitleService.ActiveLanguagesChanged
        Public Event InputLanguageChanged As EventHandler(Of String) Implements ISubtitleService.InputLanguageChanged
        Public Event LogMessage As EventHandler(Of String) Implements ISubtitleService.LogMessage

        ' ── Properties ──

        Public Property IsRunning As Boolean = True Implements ISubtitleService.IsRunning
        Public Property IsLiveRunning As Boolean = False Implements ISubtitleService.IsLiveRunning
        Public Property InputLanguage As String = "auto" Implements ISubtitleService.InputLanguage
        Public Property TuneCallback As Func(Of String) = Nothing Implements ISubtitleService.TuneCallback
        Public Property BgColor As String = "#000000" Implements ISubtitleService.BgColor
        Public Property FgColor As String = "#FFFFFF" Implements ISubtitleService.FgColor

        ''' <summary>
        ''' When set, desktop STT broadcasts go only to clients in this room instead of non-room clients.
        ''' Empty = broadcast to non-room clients (default behaviour).
        ''' </summary>
        Public Property TargetRoomId As String = "" Implements ISubtitleService.TargetRoomId

        Public ReadOnly Property ConnectedClients As Integer Implements ISubtitleService.ConnectedClients
            Get
                Return _clients.Values.Where(Function(c) Not c.IsPreview).Count()
            End Get
        End Property

        Public Sub New(logger As ILogger(Of SubtitleService),
                       options As IOptions(Of ServerOptions),
                       bibleService As IBibleService,
                       ttsService As ITtsService,
                       ttsCache As Tts.TtsCache)
            _logger = logger
            _options = options.Value
            Me.BibleService = bibleService
            Me.TtsService = ttsService
            Me.TtsCacheDirectory = If(ttsCache?.CacheDirectory, "")
        End Sub

        Public ReadOnly Property BibleService As IBibleService
        Public ReadOnly Property TtsService As ITtsService
        Public ReadOnly Property TtsCacheDirectory As String

        ''' <summary>Set after Build() — conditionally created based on TTS output device config.</summary>
        Public Property TtsAudioOutput As TtsAudioOutput

        ' ── Client management ──

        Public Function AddClient(client As ClientConnection) As Boolean Implements ISubtitleService.AddClient
            If _clients.TryAdd(client.Id, client) Then
                If Not client.IsPreview Then
                    Dim shortUa = ParseUserAgent(client.UserAgent)
                    RaiseEvent StatusChanged(Me, $"Client connected: {client.RemoteEndpoint} — {shortUa} ({ConnectedClients} clients)")
                End If
                Return True
            End If
            Return False
        End Function

        Public Function RemoveClient(clientId As String) As Boolean Implements ISubtitleService.RemoveClient
            Dim removed As ClientConnection = Nothing
            If _clients.TryRemove(clientId, removed) Then
                If Not removed.IsPreview Then
                    RaiseEvent StatusChanged(Me, $"Client disconnected: {removed.RemoteEndpoint} ({ConnectedClients} clients)")
                End If
                RaiseEvent ActiveLanguagesChanged(Me, EventArgs.Empty)
                Return True
            End If
            Return False
        End Function

        Public Function GetClientSnapshots() As List(Of Models.ClientSnapshot) Implements ISubtitleService.GetClientSnapshots
            Dim result As New List(Of Models.ClientSnapshot)()
            For Each kvp In _clients
                Dim c = kvp.Value
                If c.IsPreview Then Continue For
                Dim parts = ParseUserAgentParts(c.UserAgent)
                result.Add(New Models.ClientSnapshot With {
                    .RemoteEndpoint = c.RemoteEndpoint,
                    .Device = parts.Device,
                    .OS = parts.OS,
                    .Browser = parts.Browser,
                    .Language = c.Language,
                    .ConnectedAt = c.ConnectedAt,
                    .LastMessageAt = c.LastMessageAt,
                    .MessagesSent = c.MessagesSent,
                    .MessagesDropped = c.MessagesDropped,
                    .RawUserAgent = c.UserAgent
                })
            Next
            Return result
        End Function

        ''' <summary>
        ''' Get a client connection by ID. Used by ConversationAudioHandler.
        ''' </summary>
        Public Function GetClient(clientId As String) As ClientConnection
            Dim client As ClientConnection = Nothing
            _clients.TryGetValue(clientId, client)
            Return client
        End Function

        Public Function GetActiveTranslationLanguages() As List(Of String) Implements ISubtitleService.GetActiveTranslationLanguages
            Dim langs As New HashSet(Of String)()
            For Each kvp In _clients
                Dim lang = kvp.Value.Language
                If Not String.IsNullOrEmpty(lang) Then langs.Add(lang)
            Next
            Return langs.ToList()
        End Function

        ' ── Message processing ──

        Public Sub ProcessClientMessage(clientId As String, jsonText As String) Implements ISubtitleService.ProcessClientMessage
            Try
                Using doc = JsonDocument.Parse(jsonText)
                    Dim root = doc.RootElement
                    Dim typeProp As JsonElement = Nothing
                    If Not root.TryGetProperty("type", typeProp) Then Return
                    Dim typeStr = typeProp.GetString()

                    If typeStr = "setLanguage" Then
                        Dim langProp As JsonElement = Nothing
                        If Not root.TryGetProperty("language", langProp) Then Return
                        Dim lang = langProp.GetString()
                        Dim info As ClientConnection = Nothing
                        If _clients.TryGetValue(clientId, info) Then
                            Dim oldLang = info.Language
                            info.Language = If(lang, "")
                            If oldLang <> info.Language Then
                                RaiseEvent LogMessage(Me, $"[SUBTITLE] LANG CHANGE {info.RemoteEndpoint}: '{oldLang}' -> '{info.Language}'")
                                RaiseEvent ActiveLanguagesChanged(Me, EventArgs.Empty)
                            End If
                        End If

                    ElseIf typeStr = "setInputLanguage" Then
                        Dim langProp As JsonElement = Nothing
                        If Not root.TryGetProperty("language", langProp) Then Return
                        Dim lang = If(langProp.GetString(), "auto")
                        RaiseEvent LogMessage(Me, $"[SUBTITLE] INPUT LANG CHANGE -> '{lang}'")
                        RaiseEvent InputLanguageChanged(Me, lang)

                    ElseIf typeStr = "requestTts" Then
                        ' Client requests TTS for arbitrary text (e.g. Bible verse, room message)
                        Dim textProp As JsonElement = Nothing
                        Dim langProp2 As JsonElement = Nothing
                        If Not root.TryGetProperty("text", textProp) Then Return
                        If Not root.TryGetProperty("language", langProp2) Then Return
                        Dim ttsText = textProp.GetString()
                        Dim ttsLang = langProp2.GetString()
                        ' Convert FLORES codes (e.g. "spa_Latn") to ISO 639-3 (e.g. "spa") for TTS backends
                        If Not String.IsNullOrEmpty(ttsLang) AndAlso ttsLang.Contains("_") Then
                            ttsLang = Pipeline.TranslationService.FloresToIso3(ttsLang)
                        End If
                        If TtsService IsNot Nothing AndAlso
                           Not String.IsNullOrEmpty(ttsText) AndAlso
                           Not String.IsNullOrEmpty(ttsLang) Then
                            Dim capturedId = clientId
                            Task.Run(Async Function()
                                         Try
                                             ' Use a hash-based cache key (not commit ID)
                                             Dim hashId = Math.Abs(ttsText.GetHashCode()) Mod 1000000
                                             Dim url = Await TtsService.SynthesiseAsync(
                                                 ttsText, ttsLang, -hashId, CancellationToken.None)
                                             If url IsNot Nothing Then
                                                 ' Send tts response to requesting client only
                                                 Dim info As ClientConnection = Nothing
                                                 If _clients.TryGetValue(capturedId, info) Then
                                                     Dim json = $"{{""type"":""tts"",""id"":-1,""url"":{EscapeJson(url)},""lang"":{EscapeJson(ttsLang)}}}"
                                                     Dim buf = Encoding.UTF8.GetBytes(json)
                                                     TrySendToClient(info, buf)
                                                 End If
                                             End If
                                         Catch ex As Exception
                                             AppLogger.Log(LogEvents.TTS_ENGINE_ERROR, $"TTS synthesis for requestTts failed: {ex.Message}")
                                         End Try
                                     End Function)
                        End If

                    ElseIf typeStr = "ping" Then
                        Dim info As ClientConnection = Nothing
                        If _clients.TryGetValue(clientId, info) Then
                            Dim pong = Encoding.UTF8.GetBytes("{""type"":""pong""}")
                            TrySendToClient(info, pong)
                        End If
                    End If
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"ProcessClientMessage failed: {ex.Message}")
            End Try
        End Sub

        ' ── Broadcast methods ──

        Public Sub BroadcastUpdate(text As String) Implements ISubtitleService.BroadcastUpdate
            If Not IsRunning Then Return
            _currentLine = text

            ' Pre-encode once, send to all non-translation clients in the default (no-room) scope
            Dim json = $"{{""type"":""update"",""text"":{EscapeJson(text)}}}"
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)

            For Each kvp In _clients
                Try
                    ' Room scoping: desktop STT broadcasts to clients with no room assigned
                    If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    If Not String.IsNullOrEmpty(kvp.Value.Language) Then Continue For
                    If Not TrySendToClient(kvp.Value, buffer) Then
                        RaiseEvent LogMessage(Me, $"[WS] BroadcastUpdate send failed: {kvp.Value.RemoteEndpoint}")
                        deadKeys.Add(kvp.Key)
                    End If
                Catch ex As Exception
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)
        End Sub

        Public Function BroadcastCommit(text As String,
                                         Optional skipTranslationClients As Boolean = False,
                                         Optional lang As String = "",
                                         Optional sourceLang As String = "",
                                         Optional targetRoomId As String = Nothing) As Integer Implements ISubtitleService.BroadcastCommit
            Dim commitId = Interlocked.Increment(_commitCounter)
            If Not IsRunning Then Return commitId
            _currentLine = ""

            Dim entry As New CommittedEntry(commitId, text, lang, Nothing)
            _lastCommittedEntry = entry
            EnqueueWithCap(entry)

            ' Detect Bible references in committed text
            Dim refsJson = DetectAndSerializeRefs(text, entry)

            Dim ts = entry.Timestamp.ToString("HH:mm:ss")
            Dim json = $"{{""type"":""commit"",""text"":{EscapeJson(text)},""lang"":{EscapeJson(lang)},""time"":{EscapeJson(ts)},""id"":{commitId}{refsJson}}}"
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)

            Dim targetRoom = If(targetRoomId, TargetRoomId)
            For Each kvp In _clients
                Try
                    ' Room scoping: if targetRoom is set, only send to that room's clients
                    If Not String.IsNullOrEmpty(targetRoom) Then
                        If kvp.Value.RoomId <> targetRoom Then Continue For
                    Else
                        ' Default: desktop STT broadcasts to clients with no room
                        If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    End If
                    If skipTranslationClients AndAlso Not String.IsNullOrEmpty(kvp.Value.Language) AndAlso kvp.Value.Language <> sourceLang Then
                        Continue For
                    End If
                    If Not TrySendToClient(kvp.Value, buffer) Then
                        deadKeys.Add(kvp.Key)
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket send failed for {kvp.Key}: {ex.Message}")
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)

            ' Fire-and-forget TTS for non-translation commits
            If Not skipTranslationClients Then
                FireTtsForCommit(commitId, text, lang, Nothing, If(targetRoomId, TargetRoomId))
            End If

            Return commitId
        End Function

        Public Function BroadcastCommitTranslated(originalText As String,
                                              sourceLang As String,
                                              translations As Dictionary(Of String, String),
                                              langTags As Dictionary(Of String, String),
                                              Optional targetRoomId As String = Nothing,
                                              Optional sourceFloresLang As String = Nothing) As Integer Implements ISubtitleService.BroadcastCommitTranslated
            If Not IsRunning Then Return 0
            _currentLine = ""

            Dim entry As New CommittedEntry(Interlocked.Increment(_commitCounter), originalText, sourceLang, translations, langTags)
            _lastCommittedEntry = entry
            EnqueueWithCap(entry)

            ' Detect Bible references in original text
            Dim refsJson = DetectAndSerializeRefs(originalText, entry)

            Dim ts = entry.Timestamp.ToString("HH:mm:ss")
            Dim deadKeys As New List(Of String)

            Dim targetRoom = If(targetRoomId, TargetRoomId)
            For Each kvp In _clients
                Try
                    ' Room scoping: if targetRoom is set, only send to that room's clients
                    If Not String.IsNullOrEmpty(targetRoom) Then
                        If kvp.Value.RoomId <> targetRoom Then Continue For
                    Else
                        ' Default: desktop STT broadcasts to clients with no room
                        If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    End If

                    Dim clientLang = kvp.Value.Language

                    ' When source clients already received an immediate broadcast, skip them here
                    If Not String.IsNullOrEmpty(sourceFloresLang) AndAlso
                       (String.IsNullOrEmpty(clientLang) OrElse clientLang = sourceFloresLang) Then
                        Continue For
                    End If

                    Dim text As String
                    Dim tag As String

                    If String.IsNullOrEmpty(clientLang) Then
                        text = originalText
                        tag = sourceLang
                    Else
                        Dim translated As String = Nothing
                        If translations IsNot Nothing AndAlso translations.TryGetValue(clientLang, translated) Then
                            text = translated
                            tag = clientLang
                        Else
                            Continue For
                        End If
                    End If

                    Dim langTag As String = ""
                    If langTags IsNot Nothing Then langTags.TryGetValue(tag, langTag)
                    If langTag Is Nothing Then langTag = tag
                    Dim json = $"{{""type"":""commit"",""text"":{EscapeJson(text)},""lang"":{EscapeJson(langTag)},""time"":{EscapeJson(ts)},""id"":{entry.Id}{refsJson}}}"
                    Dim buffer = Encoding.UTF8.GetBytes(json)
                    If Not TrySendToClient(kvp.Value, buffer) Then
                        deadKeys.Add(kvp.Key)
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket send failed for {kvp.Key}: {ex.Message}")
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)

            ' Fire-and-forget TTS generation for each translated language
            FireTtsForCommit(entry.Id, originalText, sourceLang, translations, If(targetRoomId, TargetRoomId))

            Return entry.Id
        End Function

        Public Sub BroadcastTranslationsOnly(translations As Dictionary(Of String, String),
                                              langTags As Dictionary(Of String, String)) Implements ISubtitleService.BroadcastTranslationsOnly
            If Not IsRunning OrElse translations Is Nothing Then Return

            If _lastCommittedEntry IsNot Nothing Then
                For Each kvp In translations
                    _lastCommittedEntry.Translations(kvp.Key) = kvp.Value
                Next
            End If

            Dim ts = DateTime.Now.ToString("HH:mm:ss")
            Dim deadKeys As New List(Of String)

            For Each kvp In _clients
                Try
                    ' Room scoping: desktop STT broadcasts to clients with no room
                    If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For

                    Dim lang = kvp.Value.Language
                    If String.IsNullOrEmpty(lang) Then Continue For
                    Dim translated As String = Nothing
                    If Not translations.TryGetValue(lang, translated) Then Continue For

                    Dim tagValue As String = ""
                    If langTags IsNot Nothing Then langTags.TryGetValue(lang, tagValue)
                    If tagValue Is Nothing Then tagValue = lang
                    Dim entryId = If(_lastCommittedEntry IsNot Nothing, _lastCommittedEntry.Id, 0)
                    Dim json = $"{{""type"":""commit"",""text"":{EscapeJson(translated)},""lang"":{EscapeJson(tagValue)},""time"":{EscapeJson(ts)},""id"":{entryId}}}"
                    Dim buffer = Encoding.UTF8.GetBytes(json)
                    If Not TrySendToClient(kvp.Value, buffer) Then
                        deadKeys.Add(kvp.Key)
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket send failed for {kvp.Key}: {ex.Message}")
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)
        End Sub

        Public Sub BroadcastClear() Implements ISubtitleService.BroadcastClear
            If Not IsRunning Then Return
            _currentLine = ""
            While _committedLines.Count > 0
                Dim discard As CommittedEntry = Nothing
                _committedLines.TryDequeue(discard)
            End While
            ' Only send clear to non-room clients (room clients have their own transcript)
            Dim json = "{""type"":""clear""}"
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)
            For Each kvp In _clients
                Try
                    If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    If Not TrySendToClient(kvp.Value, buffer) Then deadKeys.Add(kvp.Key)
                Catch ex As Exception
                    deadKeys.Add(kvp.Key)
                End Try
            Next
            CleanupDeadClients(deadKeys)
        End Sub

        Public Sub BroadcastSystemMessage(text As String) Implements ISubtitleService.BroadcastSystemMessage
            If Not IsRunning Then Return
            Dim json = $"{{""type"":""commit"",""text"":{EscapeJson(text)}}}"
            ' Only send system messages to non-room clients
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)
            For Each kvp In _clients
                Try
                    If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    If Not TrySendToClient(kvp.Value, buffer) Then deadKeys.Add(kvp.Key)
                Catch ex As Exception
                    deadKeys.Add(kvp.Key)
                End Try
            Next
            CleanupDeadClients(deadKeys)
        End Sub

        ' ── History replay ──

        Public Async Function ReplayHistoryAsync(client As ClientConnection,
                                                  lastId As Integer,
                                                  ct As CancellationToken) As Task Implements ISubtitleService.ReplayHistoryAsync
            Try
                ' Wait for any in-flight send to finish
                Dim spinWait = 0
                While Interlocked.CompareExchange(client.SendBusy, 1, 0) <> 0
                    Await Task.Delay(10, ct).ConfigureAwait(False)
                    spinWait += 1
                    If spinWait > 100 Then Exit Function
                End While
                Try
                    Dim replayed = 0
                    For Each entry In _committedLines
                        If entry.Id <= lastId Then Continue For
                        If replayed >= _options.MaxReplayEntries Then Exit For

                        Dim text = GetTextForClient(client, entry.OriginalText, entry.Translations)
                        Dim clientLang = GetLangForClient(client, entry.SourceLang, entry.Translations, entry.LangTags)
                        Dim ts = entry.Timestamp.ToString("HH:mm:ss")
                        Dim refsJson = SerializeStoredRefs(entry)
                        Dim json = $"{{""type"":""commit"",""text"":{EscapeJson(text)},""lang"":{EscapeJson(clientLang)},""time"":{EscapeJson(ts)},""id"":{entry.Id}{refsJson}}}"
                        Dim buf = Encoding.UTF8.GetBytes(json)
                        If client.WebSocket.State = WebSocketState.Open Then
                            Await client.WebSocket.SendAsync(
                                New ArraySegment(Of Byte)(buf),
                                WebSocketMessageType.Text, True, ct).ConfigureAwait(False)
                            replayed += 1
                        End If
                    Next

                    ' Send current in-progress line if any
                    If _currentLine.Length > 0 AndAlso client.WebSocket.State = WebSocketState.Open Then
                        Dim json = $"{{""type"":""update"",""text"":{EscapeJson(_currentLine)}}}"
                        Dim buf = Encoding.UTF8.GetBytes(json)
                        Await client.WebSocket.SendAsync(
                            New ArraySegment(Of Byte)(buf),
                            WebSocketMessageType.Text, True, ct).ConfigureAwait(False)
                    End If
                Finally
                    Interlocked.Exchange(client.SendBusy, 0)
                End Try
            Catch ex As Exception
                AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"ReplayHistoryAsync failed: {ex.Message}")
            End Try
        End Function

        ' ── Private helpers ──

        Private Function TrySendToClient(client As ClientConnection, data As Byte()) As Boolean
            Dim ws = client.WebSocket
            If ws Is Nothing Then Return False
            If ws.State = WebSocketState.Closed OrElse ws.State = WebSocketState.Aborted Then
                Return False
            End If

            ' Backpressure: if previous send still in-flight, skip this message
            If Interlocked.CompareExchange(client.SendBusy, 1, 0) <> 0 Then
                Interlocked.Increment(client.MessagesDropped)
                Return True ' Not dead, just busy
            End If

            ' Fire-and-forget async send
            Task.Run(Async Function()
                         Try
                             If ws.State = WebSocketState.Open Then
                                 Await ws.SendAsync(
                                     New ArraySegment(Of Byte)(data),
                                     WebSocketMessageType.Text, True,
                                     CancellationToken.None).ConfigureAwait(False)
                                 Interlocked.Increment(client.MessagesSent)
                             End If
                         Catch ex As Exception
                             AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"Async WebSocket send failed: {ex.Message}")
                         Finally
                             Interlocked.Exchange(client.SendBusy, 0)
                         End Try
                     End Function)
            Return True
        End Function

        Private Sub BroadcastMessage(json As String)
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)

            For Each kvp In _clients
                Try
                    If Not TrySendToClient(kvp.Value, buffer) Then deadKeys.Add(kvp.Key)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket send failed for {kvp.Key}: {ex.Message}")
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)
        End Sub

        Private Sub EnqueueWithCap(entry As CommittedEntry)
            _committedLines.Enqueue(entry)
            ' Enforce history cap
            While _committedLines.Count > _options.HistoryCap
                Dim discard As CommittedEntry = Nothing
                _committedLines.TryDequeue(discard)
            End While
        End Sub

        ''' <summary>
        ''' Detect Bible references in text and return a JSON fragment like: ,"refs":[...]
        ''' Also stores refs on the CommittedEntry for replay.
        ''' </summary>
        Private Function DetectAndSerializeRefs(text As String, entry As CommittedEntry) As String
            If BibleService Is Nothing Then Return ""
            Try
                Dim refs = BibleService.DetectReferencesInText(text)
                If refs Is Nothing OrElse refs.Count = 0 Then Return ""
                entry.BibleRefs = refs.ToList()
                Dim sb As New StringBuilder(",""refs"":[")
                For i = 0 To refs.Count - 1
                    If i > 0 Then sb.Append(",")
                    Dim r = refs(i)
                    sb.Append($"{{""book"":{EscapeJson(r.Reference.Book)},""chapter"":{r.Reference.Chapter},""verseStart"":{r.Reference.VerseStart},""verseEnd"":{r.Reference.VerseEnd},""matched"":{EscapeJson(r.MatchedText)},""start"":{r.StartIndex},""len"":{r.Length}}}")
                Next
                sb.Append("]")
                Return sb.ToString()
            Catch ex As Exception
                AppLogger.Log(LogEvents.BIBLE_ERROR, $"Bible reference detection failed: {ex.Message}")
                Return ""
            End Try
        End Function

        Private Function SerializeStoredRefs(entry As CommittedEntry) As String
            If entry.BibleRefs Is Nothing OrElse entry.BibleRefs.Count = 0 Then Return ""
            Try
                Dim sb As New StringBuilder(",""refs"":[")
                For i = 0 To entry.BibleRefs.Count - 1
                    If i > 0 Then sb.Append(",")
                    Dim r = entry.BibleRefs(i)
                    sb.Append($"{{""book"":{EscapeJson(r.Reference.Book)},""chapter"":{r.Reference.Chapter},""verseStart"":{r.Reference.VerseStart},""verseEnd"":{r.Reference.VerseEnd},""matched"":{EscapeJson(r.MatchedText)},""start"":{r.StartIndex},""len"":{r.Length}}}")
                Next
                sb.Append("]")
                Return sb.ToString()
            Catch ex As Exception
                AppLogger.Log(LogEvents.BIBLE_ERROR, $"Serializing stored Bible refs failed: {ex.Message}")
                Return ""
            End Try
        End Function

        ''' <summary>
        ''' Fire-and-forget TTS generation for each language with connected clients.
        ''' On success, sends a tts WebSocket message to matching clients.
        ''' </summary>
        Private Sub FireTtsForCommit(commitId As Integer, originalText As String,
                                     sourceLang As String,
                                     translations As Dictionary(Of String, String),
                                     Optional targetRoomId As String = Nothing)
            If TtsService Is Nothing Then Return

            ' Collect languages from clients scoped to the target room
            Dim clientLangs As New HashSet(Of String)()
            Dim hasSourceLangClients As Boolean = False
            For Each kvp In _clients
                ' Room scoping: only consider clients in the target room
                If Not String.IsNullOrEmpty(targetRoomId) Then
                    If kvp.Value.RoomId <> targetRoomId Then Continue For
                Else
                    If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                End If
                Dim lang = kvp.Value.Language
                If Not String.IsNullOrEmpty(lang) Then
                    clientLangs.Add(lang)
                Else
                    hasSourceLangClients = True
                End If
            Next

            ' Generate TTS for each active translation language
            If translations IsNot Nothing Then
                For Each kvp In translations
                    If clientLangs.Contains(kvp.Key) Then
                        ChainTtsTask(kvp.Key, kvp.Value, commitId, targetRoomId)
                    End If
                Next
            End If

            ' Also generate for source-language clients (no translation needed)
            If Not String.IsNullOrEmpty(sourceLang) AndAlso
               (clientLangs.Count = 0 OrElse hasSourceLangClients) Then
                ChainTtsTask(sourceLang, originalText, commitId, targetRoomId)
            End If
        End Sub

        ''' <summary>
        ''' Chains a TTS task so that notifications for the same (room, lang) pair
        ''' are sent in commit order, preventing out-of-order audio playback.
        ''' </summary>
        Private Sub ChainTtsTask(lang As String, text As String, commitId As Integer,
                                  Optional roomId As String = Nothing)
            Dim chainKey = $"{If(roomId, "desktop")}__{lang}"
            Dim priority = If(String.IsNullOrEmpty(roomId),
                Scheduling.TranslationPriority.Live,
                Scheduling.TranslationPriority.Room)

            ' Capture the previous task in the chain for this (room, lang) pair
            Dim previous As Task = Nothing
            _ttsChains.TryGetValue(chainKey, previous)
            If previous Is Nothing Then previous = Task.CompletedTask

            Dim newTask = Task.Run(Async Function()
                                       ' Wait for the previous TTS for this room+lang to finish notifying
                                       Try : Await previous : Catch : End Try
                                       Try
                                           Dim url = Await TtsService.SynthesiseAsync(
                                               text, lang, commitId, CancellationToken.None, priority)
                                           If url IsNot Nothing Then
                                               NotifyTtsReady(commitId, lang, url, roomId)
                                           End If
                                       Catch ex As Exception
                                           _logger.LogDebug(ex, "TTS generation failed for {Lang} commit {Id}",
                                                           lang, commitId)
                                       End Try
                                   End Function)
            _ttsChains(chainKey) = newTask
        End Sub

        ''' <summary>
        ''' Send a tts WebSocket message to clients whose language matches.
        ''' </summary>
        Private Sub NotifyTtsReady(commitId As Integer, language As String, url As String,
                                   Optional targetRoomId As String = Nothing)
            Dim json = $"{{""type"":""tts"",""id"":{commitId},""url"":{EscapeJson(url)},""lang"":{EscapeJson(language)}}}"
            Dim buffer = Encoding.UTF8.GetBytes(json)
            Dim deadKeys As New List(Of String)

            For Each kvp In _clients
                Try
                    ' Room scoping: only send to clients in the target room
                    If Not String.IsNullOrEmpty(targetRoomId) Then
                        If kvp.Value.RoomId <> targetRoomId Then Continue For
                    Else
                        If Not String.IsNullOrEmpty(kvp.Value.RoomId) Then Continue For
                    End If

                    ' Send to clients watching this language, or source-language clients
                    Dim clientLang = kvp.Value.Language
                    If clientLang = language OrElse
                       (String.IsNullOrEmpty(clientLang) AndAlso String.IsNullOrEmpty(language)) Then
                        If Not TrySendToClient(kvp.Value, buffer) Then
                            deadKeys.Add(kvp.Key)
                        End If
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket send failed for {kvp.Key}: {ex.Message}")
                    deadKeys.Add(kvp.Key)
                End Try
            Next

            CleanupDeadClients(deadKeys)

            ' Also play to local audio output if configured (only for non-room / desktop stream)
            If String.IsNullOrEmpty(targetRoomId) AndAlso
               TtsAudioOutput IsNot Nothing AndAlso TtsAudioOutput.IsRunning Then
                TtsAudioOutput.EnqueueFromUrl(url, TtsCacheDirectory)
            End If
        End Sub

        Private Sub CleanupDeadClients(deadKeys As List(Of String))
            For Each key In deadKeys
                Dim removed As ClientConnection = Nothing
                If _clients.TryRemove(key, removed) Then
                    _logger.LogDebug("Cleanup dead client: {Endpoint}", removed.RemoteEndpoint)
                    Try : removed.WebSocket?.Dispose() : Catch ex As Exception : AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"WebSocket dispose failed: {ex.Message}") : End Try
                End If
            Next
            If deadKeys.Count > 0 Then
                RaiseEvent StatusChanged(Me, $"Clients: {ConnectedClients}")
                RaiseEvent ActiveLanguagesChanged(Me, EventArgs.Empty)
            End If
        End Sub

        Private Shared Function GetTextForClient(client As ClientConnection,
                                                  originalText As String,
                                                  translations As Dictionary(Of String, String)) As String
            If String.IsNullOrEmpty(client.Language) Then Return originalText
            If translations IsNot Nothing Then
                Dim translated As String = Nothing
                If translations.TryGetValue(client.Language, translated) Then Return translated
            End If
            Return originalText
        End Function

        Private Shared Function GetLangForClient(client As ClientConnection,
                                                  sourceLang As String,
                                                  translations As Dictionary(Of String, String),
                                                  langTags As Dictionary(Of String, String)) As String
            Dim rawTag As String
            If String.IsNullOrEmpty(client.Language) Then
                rawTag = sourceLang
            ElseIf translations IsNot Nothing AndAlso translations.ContainsKey(client.Language) Then
                rawTag = client.Language
            Else
                rawTag = sourceLang
            End If
            Dim displayTag As String = Nothing
            If langTags IsNot Nothing Then langTags.TryGetValue(rawTag, displayTag)
            Return If(displayTag, rawTag)
        End Function

        Public Function ExtractLastId(jsonMsg As String) As Integer
            Try
                Using doc = JsonDocument.Parse(jsonMsg)
                    Dim root = doc.RootElement
                    Dim lastIdProp As JsonElement = Nothing
                    If root.TryGetProperty("lastId", lastIdProp) Then
                        Return lastIdProp.GetInt32()
                    End If
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.SUB_SEND_ERROR, $"ExtractLastId JSON parse failed: {ex.Message}")
            End Try
            Return 0
        End Function

        ' ── Shared utilities ──

        Public Shared Function EscapeJson(s As String) As String
            Return Pipeline.ProcessHelper.EscapeJson(s)
        End Function

        Public Structure UserAgentParts
            Public Device As String
            Public OS As String
            Public Browser As String
        End Structure

        Public Shared Function ParseUserAgentParts(ua As String) As UserAgentParts
            Dim result As New UserAgentParts With {.Device = "", .OS = "", .Browser = ""}
            If String.IsNullOrWhiteSpace(ua) Then Return result

            ' Device
            If ua.Contains("iPhone") Then
                result.Device = "iPhone"
            ElseIf ua.Contains("iPad") Then
                result.Device = "iPad"
            ElseIf ua.Contains("Android") Then
                ' Try to extract model from "Build/..." or "; MODEL Build/"
                Dim modelMatch = Text.RegularExpressions.Regex.Match(ua, ";\s*([^;)]+)\s+Build/")
                If modelMatch.Success Then
                    result.Device = modelMatch.Groups(1).Value.Trim()
                Else
                    result.Device = "Android"
                End If
            ElseIf ua.Contains("Windows") Then
                result.Device = "Windows PC"
            ElseIf ua.Contains("Macintosh") Then
                result.Device = "Mac"
            ElseIf ua.Contains("Linux") Then
                result.Device = "Linux"
            ElseIf ua.Contains("CrOS") Then
                result.Device = "ChromeOS"
            End If

            ' OS
            Dim m = Text.RegularExpressions.Regex.Match(ua, "iPhone OS (\d+[_\.]\d+)")
            If m.Success Then
                result.OS = "iOS " & m.Groups(1).Value.Replace("_", ".")
            Else
                m = Text.RegularExpressions.Regex.Match(ua, "CPU OS (\d+[_\.]\d+)")
                If m.Success Then
                    result.OS = "iPadOS " & m.Groups(1).Value.Replace("_", ".")
                Else
                    m = Text.RegularExpressions.Regex.Match(ua, "Android (\d+[\.\d]*)")
                    If m.Success Then
                        result.OS = "Android " & m.Groups(1).Value
                    ElseIf ua.Contains("Windows NT 10.0") Then
                        result.OS = "Windows 10/11"
                    ElseIf ua.Contains("Windows NT") Then
                        result.OS = "Windows"
                    ElseIf ua.Contains("Mac OS X") Then
                        Dim macM = Text.RegularExpressions.Regex.Match(ua, "Mac OS X (\d+[_\.]\d+[_\.\d]*)")
                        If macM.Success Then result.OS = "macOS " & macM.Groups(1).Value.Replace("_", ".")
                    End If
                End If
            End If

            ' Browser
            If ua.Contains("EdgA/") OrElse ua.Contains("Edg/") OrElse ua.Contains("Edge/") Then
                result.Browser = "Edge"
            ElseIf ua.Contains("SamsungBrowser/") Then
                Dim sv = Text.RegularExpressions.Regex.Match(ua, "SamsungBrowser/([\d.]+)")
                result.Browser = If(sv.Success, "Samsung " & sv.Groups(1).Value, "Samsung Browser")
            ElseIf ua.Contains("OPR/") OrElse ua.Contains("Opera") Then
                result.Browser = "Opera"
            ElseIf ua.Contains("CriOS/") Then
                Dim cv = Text.RegularExpressions.Regex.Match(ua, "CriOS/([\d]+)")
                result.Browser = If(cv.Success, "Chrome " & cv.Groups(1).Value, "Chrome (iOS)")
            ElseIf ua.Contains("FxiOS/") Then
                result.Browser = "Firefox (iOS)"
            ElseIf ua.Contains("Firefox/") Then
                Dim fv = Text.RegularExpressions.Regex.Match(ua, "Firefox/([\d]+)")
                result.Browser = If(fv.Success, "Firefox " & fv.Groups(1).Value, "Firefox")
            ElseIf ua.Contains("Chrome/") Then
                Dim cv = Text.RegularExpressions.Regex.Match(ua, "Chrome/([\d]+)")
                result.Browser = If(cv.Success, "Chrome " & cv.Groups(1).Value, "Chrome")
            ElseIf ua.Contains("Safari/") Then
                Dim sv = Text.RegularExpressions.Regex.Match(ua, "Version/([\d.]+)")
                result.Browser = If(sv.Success, "Safari " & sv.Groups(1).Value, "Safari")
            ElseIf ua.Contains("MSIE") OrElse ua.Contains("Trident") Then
                result.Browser = "IE"
            End If

            Return result
        End Function

        Public Shared Function ParseUserAgent(ua As String) As String
            If String.IsNullOrWhiteSpace(ua) Then Return "Unknown"

            Dim device = ""
            Dim os = ""
            Dim browser = ""

            If ua.Contains("iPhone") Then
                device = "iPhone"
            ElseIf ua.Contains("iPad") Then
                device = "iPad"
            ElseIf ua.Contains("Android") Then
                device = "Android"
            ElseIf ua.Contains("Windows") Then
                device = "Windows"
            ElseIf ua.Contains("Macintosh") Then
                device = "Mac"
            ElseIf ua.Contains("Linux") Then
                device = "Linux"
            ElseIf ua.Contains("CrOS") Then
                device = "ChromeOS"
            End If

            Dim m = Text.RegularExpressions.Regex.Match(ua, "iPhone OS (\d+[_\.]\d+)")
            If m.Success Then
                os = "iOS " & m.Groups(1).Value.Replace("_", ".")
            Else
                m = Text.RegularExpressions.Regex.Match(ua, "CPU OS (\d+[_\.]\d+)")
                If m.Success Then
                    os = "iPadOS " & m.Groups(1).Value.Replace("_", ".")
                Else
                    m = Text.RegularExpressions.Regex.Match(ua, "Android (\d+[\.\d]*)")
                    If m.Success Then os = "Android " & m.Groups(1).Value
                End If
            End If

            If ua.Contains("EdgA/") OrElse ua.Contains("Edg/") OrElse ua.Contains("Edge/") Then
                browser = "Edge"
            ElseIf ua.Contains("SamsungBrowser/") Then
                browser = "Samsung Browser"
            ElseIf ua.Contains("OPR/") OrElse ua.Contains("Opera") Then
                browser = "Opera"
            ElseIf ua.Contains("CriOS/") Then
                browser = "Chrome (iOS)"
            ElseIf ua.Contains("FxiOS/") Then
                browser = "Firefox (iOS)"
            ElseIf ua.Contains("Firefox/") Then
                browser = "Firefox"
            ElseIf ua.Contains("Chrome/") Then
                browser = "Chrome"
            ElseIf ua.Contains("Safari/") Then
                browser = "Safari"
            ElseIf ua.Contains("MSIE") OrElse ua.Contains("Trident") Then
                browser = "IE"
            End If

            Dim parts As New List(Of String)()
            If device.Length > 0 Then parts.Add(device)
            If os.Length > 0 AndAlso Not os.StartsWith(device) Then parts.Add(os)
            If os.Length > 0 AndAlso os.StartsWith(device) Then parts.Clear() : parts.Add(os)
            If browser.Length > 0 Then parts.Add(browser)

            If parts.Count = 0 Then Return If(ua.Length > 80, ua.Substring(0, 80) & "...", ua)
            Return String.Join(" / ", parts)
        End Function
    End Class
End Namespace
