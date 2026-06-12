Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Server
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Stt

Namespace Controllers
    ''' <summary>
    ''' Manages conference room STT pipelines — creating, configuring, and stopping
    ''' per-room ISttBackend instances when conference rooms are hosted from the web client.
    ''' </summary>
    Friend Class ConferenceController

        Private ReadOnly _config As AppConfig
        Private ReadOnly _getSubtitleSvc As Func(Of ISubtitleService)
        Private ReadOnly _getTranslationService As Func(Of TranslationService)
        Private ReadOnly _getTranslationOrchestrator As Func(Of ITranslationService)
        Private ReadOnly _getRoomManager As Func(Of Services.Rooms.RoomManager)
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _ownerForm As Form

        Private ReadOnly _sttBackends As New Concurrent.ConcurrentDictionary(Of String, ISttBackend)()
        Private ReadOnly _roomTemplateIds As New Concurrent.ConcurrentDictionary(Of String, String)()
        Private ReadOnly _sentenceBuffers As New Concurrent.ConcurrentDictionary(Of String, SentenceBuffer)()
        ' Speechmatics-only clause hold-and-lock accumulators (one per room). Gated
        ' behind AppConfig.SpeechmaticsHoldClauses + backend == "speechmatics".
        Private ReadOnly _clauseAccumulators As New Concurrent.ConcurrentDictionary(Of String, SpeechmaticsClauseAccumulator)()
        ' HYBRID clause dials: rooms whose STT template explicitly stores clause
        ' fields get their resolved block pinned here (fixed for the session);
        ' rooms without an entry keep reading the live app-global dials.
        Private ReadOnly _pinnedClauseDials As New Concurrent.ConcurrentDictionary(Of String, Configs.SpeechmaticsConfig)()
        ' Per-room NAMED filter sets (room template's FilterSetId, with the
        ' active speaker's glossary set overriding the glossary path). No entry
        ' = the sidecars use their own global files.
        Private ReadOnly _roomFilters As New Concurrent.ConcurrentDictionary(Of String, Models.Templates.FilterSet)()
        Private _nextConferencePort As Integer = 5101
        Private _nextWhisperServerPort As Integer = 8179  ' 8178 is used by ConversationAudioHandler
        Private _bufferFlushTimer As System.Threading.Timer

        Public Sub New(config As AppConfig,
                       getSubtitleSvc As Func(Of ISubtitleService),
                       getTranslationService As Func(Of TranslationService),
                       getTranslationOrchestrator As Func(Of ITranslationService),
                       getRoomManager As Func(Of Services.Rooms.RoomManager),
                       log As Action(Of String),
                       ownerForm As Form)
            _config = config
            _getSubtitleSvc = getSubtitleSvc
            _getTranslationService = getTranslationService
            _getTranslationOrchestrator = getTranslationOrchestrator
            _getRoomManager = getRoomManager
            _log = log
            _ownerForm = ownerForm

            ' Timer to flush expired sentence buffers (NLLB path) and lock expired
            ' Speechmatics clause accumulators. Re-arms itself each tick to the
            ' current SpeechmaticsClauseTimerMs dial so poll resolution is live-tunable.
            _bufferFlushTimer = New System.Threading.Timer(
                AddressOf BufferTimerTick, Nothing, 1000, System.Threading.Timeout.Infinite)
        End Sub

        ''' <summary>ConcurrentDictionary remove without the ByRef-out ceremony.</summary>
        Private Shared Sub DropKey(Of TValue)(dict As Concurrent.ConcurrentDictionary(Of String, TValue), key As String)
            Dim ignored As TValue = Nothing
            dict.TryRemove(key, ignored)
        End Sub

        Private Sub BufferTimerTick(state As Object)
            Try
                FlushExpiredBuffers()
                FlushExpiredClauses()
            Catch ex As Exception
                _log($"[Conference] BufferTimerTick error: {ex.Message}")
            Finally
                Try
                    Dim interval = Math.Max(50, _config.SpeechmaticsClauseTimerMs)
                    _bufferFlushTimer?.Change(interval, System.Threading.Timeout.Infinite)
                Catch
                End Try
            End Try
        End Sub

        ''' <summary>
        ''' Wires the endpoint handler callbacks so the web client can create/configure/close conference rooms.
        ''' Call after Kestrel has started.
        ''' </summary>
        Public Sub WireEndpointHandlers()
            EndpointRegistration.ConferenceRoomCreatedHandler = Sub(roomId, templateId)
                                                                     Try
                                                                         _ownerForm.BeginInvoke(Sub()
                                                                                                    Try
                                                                                                        HandleConferenceRoomCreated(roomId, templateId)
                                                                                                    Catch ex As Exception
                                                                                                        _log($"[Conference] ERROR in HandleConferenceRoomCreated: {ex}")
                                                                                                    End Try
                                                                                                End Sub)
                                                                     Catch ex As Exception
                                                                         _log($"[Conference] ERROR invoking room created handler: {ex.Message}")
                                                                     End Try
                                                                 End Sub

            EndpointRegistration.PipelineConfigHandler = Sub(roomId, params)
                                                              Try
                                                                  _ownerForm.BeginInvoke(Sub()
                                                                                             Try
                                                                                                 HandlePipelineConfigCommand(roomId, params)
                                                                                             Catch ex As Exception
                                                                                                 _log($"[Pipeline] ERROR in HandlePipelineConfigCommand: {ex}")
                                                                                             End Try
                                                                                         End Sub)
                                                              Catch ex As Exception
                                                                  _log($"[Pipeline] ERROR invoking pipeline handler: {ex.Message}")
                                                              End Try
                                                          End Sub

            EndpointRegistration.PipelineResetHandler = Sub(roomId)
                                                               Try
                                                                   _ownerForm.BeginInvoke(Sub()
                                                                                              Try
                                                                                                  ResetConferenceBackend(roomId)
                                                                                              Catch ex As Exception
                                                                                                  _log($"[Conference] ERROR in ResetConferenceBackend: {ex}")
                                                                                              End Try
                                                                                          End Sub)
                                                               Catch ex As Exception
                                                                   _log($"[Conference] ERROR invoking pipeline reset handler: {ex.Message}")
                                                               End Try
                                                           End Sub

            EndpointRegistration.RoomClosedHandler = Sub(roomId)
                                                          Try
                                                              _ownerForm.BeginInvoke(Sub()
                                                                                         Try
                                                                                             StopConferenceBackend(roomId)
                                                                                         Catch ex As Exception
                                                                                             _log($"[Conference] ERROR in StopConferenceBackend: {ex}")
                                                                                         End Try
                                                                                     End Sub)
                                                          Catch ex As Exception
                                                              _log($"[Conference] ERROR invoking room closed handler: {ex.Message}")
                                                          End Try
                                                      End Sub
        End Sub

        ''' <summary>
        ''' Called when a conference room is created from a template via the web API.
        ''' Spins up a new ISttBackend for the room with template-derived config.
        ''' </summary>
        Public Sub HandleConferenceRoomCreated(roomId As String, templateId As String)
            Dim template = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = templateId)
            If template Is Nothing Then
                _log($"[Conference] Template '{templateId}' not found for room {roomId}")
                Return
            End If

            If _sttBackends.ContainsKey(roomId) Then
                _log($"[Conference] Backend already exists for room {roomId}")
                Return
            End If

            Dim port = _nextConferencePort
            _nextConferencePort += 1

            ' STT template: active speaker's slot (gated by room mode) wins, else
            ' the room template's reference; legacy embedded knobs are the fallback
            ' for configs that haven't migrated.
            Dim engineTpl = ResolveRoomSttTemplate(roomId, template)
            Dim backendKey = If(engineTpl IsNot Nothing AndAlso Not String.IsNullOrEmpty(engineTpl.EngineKey),
                                engineTpl.EngineKey, If(template.SttBackendKey, _config.SttBackend))

            ' Fail closed: never start an engine the room's mode makes ineligible.
            Dim roomForGate = _getRoomManager()?.GetRoom(roomId)
            If Not ConnectivityGate.IsEngineEligible(TemplateLibraryStore.GroupStt, backendKey, RoomMode(roomForGate)) Then
                AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                    $"[Conference:{roomId}] room start blocked — engine '{backendKey}' is not eligible in {RoomMode(roomForGate)} mode")
                _log($"[Conference] Backend NOT started for room {roomId}: engine '{backendKey}' blocked by {RoomMode(roomForGate)} mode")
                Return
            End If

            Dim tplOverrides = BuildSttOverrides(template, engineTpl, Nothing)
            tplOverrides("WhisperServerPort") = _nextWhisperServerPort
            _nextWhisperServerPort += 1

            Dim sttConfig As New SttSessionConfig With {
                .EngineKey = backendKey,
                .DeviceIndex = If(template.AudioDeviceId >= 0, template.AudioDeviceId, 0),
                .Language = If(template.SourceLanguage, "auto"),
                .TranslateToEnglish = False,
                .ServerPort = port,
                .ApiKey = _config.GetSttApiKey(backendKey),
                .EngineConfig = EngineConfigResolver.ResolveStt(
                    backendKey, _config, template:=engineTpl, fieldOverrides:=tplOverrides, contextLabel:=$"[Conference:{roomId}]")
            }

            SpeechmaticsTranslation.ConfigureSession(sttConfig, _config, sttConfig.Language,
                                                     _getSubtitleSvc()?.GetActiveTranslationLanguages())
            StorePinnedClauseDials(roomId, engineTpl, sttConfig)
            ResolveRoomFilters(roomId, template)
            Dim createFs As Models.Templates.FilterSet = Nothing
            If _roomFilters.TryGetValue(roomId, createFs) Then sttConfig.HallucinationsPath = createFs.HallucinationsPath
            EnsureActiveLanguagesSubscription()

            Dim backend = SttBackendRegistry.CreateBackend(backendKey)

            WireBackendLogging(roomId, backend, backendKey)

            _sttBackends(roomId) = backend
            _roomTemplateIds(roomId) = templateId
            _log($"[Conference] Starting backend for room {roomId} (template={template.Name}, port={port}, lang={sttConfig.Language}, backend={backendKey})")
            backend.Start(sttConfig)

            If backend.IsRunning Then
                _log($"[Conference] Backend started for room {roomId}")
            Else
                _log($"[Conference] Backend FAILED to start for room {roomId}")
                DropKey(_sttBackends, roomId)
                DropKey(_roomTemplateIds, roomId)
            End If
        End Sub

        ''' <summary>
        ''' Handles pipeline config changes from the web host control panel.
        ''' </summary>
        Public Sub HandlePipelineConfigCommand(roomId As String, params As Dictionary(Of String, Object))
            Dim backend As ISttBackend = Nothing
            _sttBackends.TryGetValue(roomId, backend)

            If backend Is Nothing OrElse Not backend.IsRunning Then
                _log($"[Pipeline] No running backend for room '{roomId}'")
                Return
            End If

            Dim runtimeParams As New Dictionary(Of String, Object)
            Dim needsRestart = False

            ' Keep room model in sync so the host panel shows current values
            Dim mgr = _getRoomManager()
            Dim room = mgr?.GetRoom(roomId)

            For Each kvp In params
                Select Case kvp.Key
                    Case "language"
                        runtimeParams("language") = kvp.Value
                        If room IsNot Nothing Then room.SourceLang = CStr(kvp.Value)
                    Case "maxSegmentSec"
                        runtimeParams("vad_max_segment_s") = kvp.Value
                        If room IsNot Nothing Then room.MaxSegmentSec = CInt(kvp.Value)
                    Case "vadSilenceMs"
                        runtimeParams("vad_min_silence_ms") = kvp.Value
                        If room IsNot Nothing Then room.VadSilenceMs = CInt(kvp.Value)
                    Case "beamSize"
                        needsRestart = True
                        If room IsNot Nothing Then room.BeamSize = CInt(kvp.Value)
                    Case "initialPrompt"
                        needsRestart = True
                        If room IsNot Nothing Then room.InitialPrompt = CStr(kvp.Value)
                    Case "speakerId"
                        If TrySwitchSpeaker(roomId, room, CStr(kvp.Value)) Then needsRestart = True
                    Case "mode"
                        If TrySwitchMode(roomId, room, CStr(kvp.Value)) Then needsRestart = True
                End Select
            Next

            If runtimeParams.Count > 0 Then
                _log($"[Pipeline:{roomId}] Updating runtime params: {String.Join(", ", runtimeParams.Keys)}")
                Task.Run(Function() backend.UpdateConfigAsync(runtimeParams))
            End If

            If needsRestart AndAlso Not String.IsNullOrEmpty(roomId) Then
                _log($"[Pipeline:{roomId}] Restart required for params: {String.Join(", ", params.Keys)}")
                RestartConferenceBackend(roomId, params)
            End If
        End Sub

        ''' <summary>
        ''' Stops and restarts a conference backend with updated config.
        ''' </summary>
        Private Sub RestartConferenceBackend(roomId As String, configOverrides As Dictionary(Of String, Object))
            Dim backend As ISttBackend = Nothing
            If Not _sttBackends.TryGetValue(roomId, backend) Then Return

            ' Lock any pending Speechmatics clause before restart.
            ForceLockClause(roomId)

            ' Flush sentence buffer before restart
            Dim buffer As SentenceBuffer = Nothing
            If _sentenceBuffers.TryGetValue(roomId, buffer) Then
                Dim flush = buffer.ForceFlush()
                If flush IsNot Nothing Then
                    TranslateAndBroadcastBufferedAsync(roomId, flush.Text, flush.DetectedLanguage)
                End If
            End If

            Dim tplId As String = Nothing
            _roomTemplateIds.TryGetValue(roomId, tplId)
            Dim template = If(Not String.IsNullOrEmpty(tplId),
                _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = tplId), Nothing)

            backend.Stop()

            Dim hasTpl = template IsNot Nothing

            Dim cfgDevice As Integer = 0
            If hasTpl AndAlso template.AudioDeviceId >= 0 Then cfgDevice = template.AudioDeviceId

            Dim cfgLang As String = "auto"
            If configOverrides.ContainsKey("language") Then
                cfgLang = CStr(configOverrides("language"))
            ElseIf hasTpl AndAlso Not String.IsNullOrEmpty(template.SourceLanguage) Then
                cfgLang = template.SourceLanguage
            End If

            Dim engineTpl = ResolveRoomSttTemplate(roomId, template)
            Dim restartBackendKey = If(engineTpl IsNot Nothing AndAlso Not String.IsNullOrEmpty(engineTpl.EngineKey),
                                       engineTpl.EngineKey, If(template?.SttBackendKey, _config.SttBackend))

            ' Precedence per knob: web override → STT template → app-global baseline.
            Dim ovr = BuildSttOverrides(template, engineTpl, configOverrides)
            ovr("WhisperServerPort") = _nextWhisperServerPort

            Dim sttConfig As New SttSessionConfig With {
                .EngineKey = restartBackendKey,
                .DeviceIndex = cfgDevice,
                .Language = cfgLang,
                .TranslateToEnglish = False,
                .ServerPort = _nextConferencePort,
                .ApiKey = _config.GetSttApiKey(restartBackendKey),
                .EngineConfig = EngineConfigResolver.ResolveStt(
                    restartBackendKey, _config, template:=engineTpl, fieldOverrides:=ovr, contextLabel:=$"[Conference:{roomId}]")
            }
            _nextConferencePort += 1
            _nextWhisperServerPort += 1

            SpeechmaticsTranslation.ConfigureSession(sttConfig, _config, cfgLang,
                                                     _getSubtitleSvc()?.GetActiveTranslationLanguages())
            StorePinnedClauseDials(roomId, engineTpl, sttConfig)
            ResolveRoomFilters(roomId, template)
            Dim restartFs As Models.Templates.FilterSet = Nothing
            If _roomFilters.TryGetValue(roomId, restartFs) Then sttConfig.HallucinationsPath = restartFs.HallucinationsPath

            Dim newBackend = SttBackendRegistry.CreateBackend(If(template?.SttBackendKey, _config.SttBackend))
            WireBackendLogging(roomId, newBackend, restartBackendKey)

            _sttBackends(roomId) = newBackend
            _log($"[Pipeline:{roomId}] Restarting backend (port={sttConfig.ServerPort})")
            newBackend.Start(sttConfig)
        End Sub

        ''' <summary>
        ''' Assemble loose field overrides for STT resolution. When the conference
        ''' template has no resolvable STT library template, its legacy embedded
        ''' knobs (or the app globals) stand in for the template layer; web host
        ''' panel overrides always win.
        ''' </summary>
        Private Function BuildSttOverrides(template As ConferenceTemplate,
                                           engineTpl As Models.Templates.EngineTemplate,
                                           webOverrides As Dictionary(Of String, Object)) As Dictionary(Of String, Object)
            Dim ovr As New Dictionary(Of String, Object)

            If engineTpl Is Nothing Then
                If template IsNot Nothing Then
                    ovr("BeamSize") = template.BeamSize
                    ovr("VadSilenceMs") = template.VadSilenceMs
                    ovr("MaxSegmentSec") = template.MaxSegmentSec
                    ovr("InitialPrompt") = If(template.InitialPrompt, "")
                    If Not String.IsNullOrEmpty(template.ModelPath) Then ovr("ModelPath") = template.ModelPath
                Else
                    ovr("BeamSize") = _config.BeamSize
                    ovr("VadSilenceMs") = _config.LiveVadSilenceMs
                    ovr("MaxSegmentSec") = _config.LiveMaxSegmentSec
                    ovr("InitialPrompt") = ""
                End If
            End If

            If webOverrides IsNot Nothing Then
                If webOverrides.ContainsKey("beamSize") Then ovr("BeamSize") = CInt(webOverrides("beamSize"))
                If webOverrides.ContainsKey("vadSilenceMs") Then ovr("VadSilenceMs") = CInt(webOverrides("vadSilenceMs"))
                If webOverrides.ContainsKey("maxSegmentSec") Then ovr("MaxSegmentSec") = CInt(webOverrides("maxSegmentSec"))
                If webOverrides.ContainsKey("initialPrompt") Then ovr("InitialPrompt") = CStr(webOverrides("initialPrompt"))
            End If

            Return ovr
        End Function

        ''' <summary>
        ''' Resets the conference pipeline for a room — stops the current backend
        ''' and spins up a fresh one with the same config on a new port.
        ''' Called from the host control panel when the pipeline is unresponsive.
        ''' </summary>
        Public Sub ResetConferenceBackend(roomId As String)
            If Not _sttBackends.ContainsKey(roomId) Then
                _log($"[Conference] Reset requested but no backend for room {roomId}")
                Return
            End If
            _log($"[Conference] RESET requested for room {roomId}")
            RestartConferenceBackend(roomId, New Dictionary(Of String, Object))
        End Sub

        ''' <summary>
        ''' Stops and removes a conference backend when a room is closed.
        ''' </summary>
        Public Sub StopConferenceBackend(roomId As String)
            ' Lock any pending Speechmatics clause before stopping.
            ForceLockClause(roomId)
            DropKey(_clauseAccumulators, roomId)
            DropKey(_pinnedClauseDials, roomId)
            DropKey(_roomFilters, roomId)

            ' Flush any remaining buffered text before stopping
            Dim buffer As SentenceBuffer = Nothing
            If _sentenceBuffers.TryGetValue(roomId, buffer) Then
                Dim flush = buffer.ForceFlush()
                If flush IsNot Nothing Then
                    TranslateAndBroadcastBufferedAsync(roomId, flush.Text, flush.DetectedLanguage)
                End If
                DropKey(_sentenceBuffers, roomId)
            End If

            Dim backend As ISttBackend = Nothing
            If _sttBackends.TryGetValue(roomId, backend) Then
                _log($"[Conference] Stopping backend for room {roomId}")
                DropKey(_sttBackends, roomId)
                DropKey(_roomTemplateIds, roomId)
                ' Stop on background thread to avoid freezing the UI
                Task.Run(Sub()
                             Try
                                 If backend.IsRunning Then backend.Stop()
                             Catch ex As Exception
                                 _log($"[Conference] Error stopping backend for room {roomId}: {ex.Message}")
                             End Try
                         End Sub)
            End If
        End Sub

        ''' <summary>
        ''' Stops all conference backends (called on app shutdown).
        ''' </summary>
        Public Sub StopAllConferenceBackends()
            ' Lock all pending Speechmatics clauses
            For Each kvp In _clauseAccumulators.ToList()
                Dim locked = kvp.Value.ForceLock()
                If locked IsNot Nothing Then OnClauseLocked(kvp.Key, locked)
            Next
            _clauseAccumulators.Clear()

            ' Flush all sentence buffers
            For Each kvp In _sentenceBuffers.ToList()
                Dim flush = kvp.Value.ForceFlush()
                If flush IsNot Nothing Then
                    TranslateAndBroadcastBufferedAsync(kvp.Key, flush.Text, flush.DetectedLanguage)
                End If
            Next
            _sentenceBuffers.Clear()

            ' Stop all backends in parallel on background threads to avoid UI freeze
            Dim stopTasks As New List(Of Task)()
            For Each kvp In _sttBackends.ToList()
                _log($"[Conference] Stopping backend for room {kvp.Key}")
                Dim backend = kvp.Value
                Dim rid = kvp.Key
                stopTasks.Add(Task.Run(Sub()
                                           Try
                                               If backend.IsRunning Then backend.Stop()
                                           Catch ex As Exception
                                               _log($"[Conference] Error stopping backend for room {rid}: {ex.Message}")
                                           End Try
                                       End Sub))
            Next
            _sttBackends.Clear()
            _roomTemplateIds.Clear()
            Task.WaitAll(stopTasks.ToArray(), 10000)
        End Sub

        ' ─── Translation Pipeline ─────────────────────────────────────

        Private Sub TranslateAndBroadcastForRoomAsync(roomId As String, commitArgs As SttOutputEventArgs)
            ' Speechmatics clause hold-and-lock: when enabled for this room, route the
            ' fragment into the accumulator instead of broadcasting it immediately.
            If IsHoldEnabled(roomId) Then
                FeedClause(roomId, commitArgs.Text, commitArgs.DetectedLanguage)
                Return
            End If

            ' Check if the room is paused — drop commits silently to keep whisper warm
            Dim mgr = _getRoomManager?.Invoke()
            If mgr IsNot Nothing Then
                Dim room = mgr.GetRoom(roomId)
                If room IsNot Nothing AndAlso room.Config.IsPaused Then
                    AppLogger.Log(LogEvents.CONF_COMMIT_DROPPED, $"room={roomId} reason=paused text=""{Truncate(commitArgs.Text, 80)}""")
                    Return
                End If
            End If

            Dim detectedLang = commitArgs.DetectedLanguage
            Dim line = commitArgs.Text

            Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), "eng_Latn")
            Dim sourceShort = TranslationService.FloresToShortCode(sourceLang)

            Dim subtitleSvc = _getSubtitleSvc()
            If subtitleSvc Is Nothing Then Return

            If IsGarbageCommit(line) Then
                AppLogger.Log(LogEvents.CONF_COMMIT_DROPPED, $"room={roomId} reason=garbage")
                subtitleSvc.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)
                Return
            End If

            ' Always broadcast source text immediately (no delay for source-lang viewers)
            subtitleSvc.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)

            ' Determine if we're using a cloud translation backend
            Dim orchestrator = _getTranslationOrchestrator?.Invoke()
            Dim isCloud = orchestrator IsNot Nothing AndAlso
                Not orchestrator.ActiveBackend.Equals("Local", StringComparison.OrdinalIgnoreCase)

            If isCloud Then
                ' Cloud STT + Cloud Translate: each commit from the pipeline
                ' is already a complete, punctuated sentence (audio was accumulated
                ' on the Python side). Translate immediately — no buffering needed.
                TranslateAndBroadcastBufferedAsync(roomId, line, detectedLang)
            Else
                ' NLLB (local): buffer commits into sentences before translation.
                ' NLLB is slow and works best on complete sentences.
                FlushExpiredBuffers(roomId)

                Dim buffer = _sentenceBuffers.GetOrAdd(roomId, Function(k) New SentenceBuffer(4000))

                Dim flush = buffer.Add(line, detectedLang)
                If flush Is Nothing Then Return

                TranslateAndBroadcastBufferedAsync(roomId, flush.Text, flush.DetectedLanguage)
            End If
        End Sub

        Private Async Sub TranslateAndBroadcastBufferedAsync(roomId As String, bufferedText As String, detectedLang As String)
            ' Async Sub fired from sidecar/timer threads — an unhandled exception
            ' here would terminate the process. Always contain.
            Try
                Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), "eng_Latn")
                Dim sourceShort = TranslationService.FloresToShortCode(sourceLang)

                Dim subtitleSvc = _getSubtitleSvc()
                If subtitleSvc Is Nothing Then Return

                Dim targets = subtitleSvc.GetActiveTranslationLanguages()
                targets?.Remove(sourceLang)
                If targets Is Nothing OrElse targets.Count = 0 Then Return

                Dim translations = Await TranslateTargetsAsync(roomId, bufferedText, sourceLang, targets.ToList())
                If translations.Count = 0 Then Return
                BroadcastTranslated(subtitleSvc, roomId, bufferedText, sourceShort, sourceLang, translations)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"room={roomId} translate/broadcast failed: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Translate text into the given FLORES targets via the orchestrator
        ''' (Google/DeepL/etc.), falling back to the local NLLB sidecar. Returns the
        ''' translations dict (FLORES → text); never broadcasts.
        ''' </summary>
        Private Async Function TranslateTargetsAsync(roomId As String, text As String, sourceLang As String,
                                                     targets As List(Of String)) As Task(Of Dictionary(Of String, String))
            Dim translations As New Dictionary(Of String, String)()
            If targets Is Nothing OrElse targets.Count = 0 Then Return translations

            Dim sw = Diagnostics.Stopwatch.StartNew()
            Dim orchestrator = _getTranslationOrchestrator?.Invoke()
            If orchestrator IsNot Nothing AndAlso orchestrator.GetAllBackends().Any(Function(b) b.IsAvailable) Then
                Try
                    Using cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(10))
                        Dim result = Await orchestrator.TranslateAsync(
                            text, sourceLang, targets, cts.Token, Services.Scheduling.TranslationPriority.Room,
                            filters:=RoomTranslationFilters(roomId))
                        If result IsNot Nothing Then
                            For Each kvp In result : translations(kvp.Key) = kvp.Value : Next
                        End If
                    End Using
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"room={roomId} backend={orchestrator.ActiveBackend} {sourceLang}→[{String.Join(",", targets)}] failed: {ex.Message}")
                End Try
                If translations.Count > 0 Then
                    AppLogger.Log(LogEvents.TRANS_RESULT,
                        $"room={roomId} backend={orchestrator.ActiveBackend} {sourceLang}→[{String.Join(",", translations.Keys)}] ok={translations.Count}/{targets.Count} {sw.ElapsedMilliseconds}ms")
                    Return translations
                End If
            End If

            ' Fallback: direct sidecar (NLLB)
            Dim svc = _getTranslationService()
            If svc IsNot Nothing AndAlso svc.IsRunning AndAlso svc.IsModelLoaded Then
                Try
                    Dim fallbackFp = RoomTranslationFilters(roomId)
                    Dim result = Await svc.TranslateAsync(text, sourceLang, targets, timeoutSeconds:=10,
                        glossaryPath:=If(fallbackFp?.GlossaryPath, ""),
                        profanityPath:=If(fallbackFp?.ProfanityPath, ""))
                    If result IsNot Nothing Then
                        For Each kvp In result : translations(kvp.Key) = kvp.Value : Next
                    End If
                    AppLogger.Log(LogEvents.TRANS_RESULT,
                        $"room={roomId} backend=nllb-direct {sourceLang}→[{String.Join(",", translations.Keys)}] ok={translations.Count}/{targets.Count} {sw.ElapsedMilliseconds}ms")
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"room={roomId} backend=nllb-direct {sourceLang}→[{String.Join(",", targets)}] failed: {ex.Message}")
                End Try
            End If
            Return translations
        End Function

        Private Shared Sub BroadcastTranslated(subtitleSvc As ISubtitleService, roomId As String,
                                               text As String, sourceShort As String, sourceLang As String,
                                               translations As Dictionary(Of String, String))
            Dim langTags As New Dictionary(Of String, String)
            For Each kvp In translations
                langTags(kvp.Key) = TranslationService.FloresToShortCode(kvp.Key)
            Next
            subtitleSvc.BroadcastCommitTranslated(text, sourceShort, translations, langTags,
                                                  targetRoomId:=roomId, sourceFloresLang:=sourceLang)
        End Sub

        ''' <summary>
        ''' Handles a commit that arrived with inline Speechmatics translations: glossary-
        ''' fixes them, NLLB-translates any active targets Speechmatics couldn't cover
        ''' (English-pivot limit), merges, and broadcasts one translated commit per client.
        ''' </summary>
        Private Async Sub HandleTranslatedCommitAsync(roomId As String, args As SttTranslatedCommitEventArgs)
            ' Async Sub fired from sidecar threads — contain all exceptions.
            Try
                Await HandleTranslatedCommitCoreAsync(roomId, args)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"room={roomId} translated-commit handling failed: {ex.Message}")
            End Try
        End Sub

        Private Async Function HandleTranslatedCommitCoreAsync(roomId As String, args As SttTranslatedCommitEventArgs) As Task
            ' Speechmatics clause hold-and-lock: hold + re-translate the whole clause
            ' (ignores Speechmatics' per-fragment inline translations — the locked
            ' clause is translated once, as a whole, via the configured backend).
            If IsHoldEnabled(roomId) Then
                FeedClause(roomId, args.Text, args.DetectedLanguage)
                Return
            End If

            ' Drop commits while the room is paused (keeps the engine warm).
            Dim room = _getRoomManager?.Invoke()?.GetRoom(roomId)
            If room IsNot Nothing AndAlso room.Config.IsPaused Then
                AppLogger.Log(LogEvents.CONF_COMMIT_DROPPED, $"room={roomId} reason=paused text=""{Truncate(args.Text, 80)}""")
                Return
            End If

            Dim sourceLang = If(Not String.IsNullOrEmpty(args.DetectedLanguage), WhisperToNllbCode(args.DetectedLanguage), "eng_Latn")
            Dim sourceShort = TranslationService.FloresToShortCode(sourceLang)
            Dim subtitleSvc = _getSubtitleSvc()
            If subtitleSvc Is Nothing Then Return

            If IsGarbageCommit(args.Text) Then
                AppLogger.Log(LogEvents.CONF_COMMIT_DROPPED, $"room={roomId} reason=garbage")
                subtitleSvc.BroadcastCommit(args.Text, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)
                Return
            End If

            ' Source immediately for source-language viewers.
            subtitleSvc.BroadcastCommit(args.Text, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)

            ' Speechmatics translations (engine codes) → FLORES, then glossary-fix.
            Dim merged As New Dictionary(Of String, String)()
            For Each kvp In args.Translations
                Dim flores = SpeechmaticsTranslation.SpeechmaticsToFlores(kvp.Key)
                If Not String.IsNullOrEmpty(flores) AndAlso Not String.IsNullOrEmpty(kvp.Value) Then
                    merged(flores) = kvp.Value
                End If
            Next
            If merged.Count > 0 Then
                Dim svc = _getTranslationService()
                If svc IsNot Nothing Then
                    Try
                        Dim roomFp = RoomTranslationFilters(roomId)
                        merged = Await svc.ApplyGlossaryAsync(args.Text, sourceLang, merged,
                            glossaryPath:=If(roomFp?.GlossaryPath, ""),
                            profanityPath:=If(roomFp?.ProfanityPath, ""))
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.TRANS_ERROR, $"room={roomId} glossary-apply failed: {ex.Message}")
                    End Try
                End If
            End If

            ' NLLB (or cloud) for active targets Speechmatics didn't cover (English-pivot limit).
            Dim active = subtitleSvc.GetActiveTranslationLanguages()
            active?.Remove(sourceLang)
            Dim remaining As New List(Of String)()
            If active IsNot Nothing Then
                For Each t In active
                    If Not merged.ContainsKey(t) Then remaining.Add(t)
                Next
            End If
            If remaining.Count > 0 Then
                Dim nllb = Await TranslateTargetsAsync(roomId, args.Text, sourceLang, remaining)
                For Each kvp In nllb : merged(kvp.Key) = kvp.Value : Next
            End If

            If merged.Count = 0 Then Return
            BroadcastTranslated(subtitleSvc, roomId, args.Text, sourceShort, sourceLang, merged)
        End Function

        ' ─── Inline translation retargeting (engine-blind) ───────────────

        Private _activeLangSubscribed As Boolean = False

        ''' <summary>Subscribe once to active-language changes so we can re-target inline-translating sessions.</summary>
        Private Sub EnsureActiveLanguagesSubscription()
            If _activeLangSubscribed Then Return
            Dim svc = _getSubtitleSvc()
            If svc Is Nothing Then Return
            AddHandler svc.ActiveLanguagesChanged, AddressOf OnActiveLanguagesChanged
            _activeLangSubscribed = True
        End Sub

        Private Sub OnActiveLanguagesChanged(sender As Object, e As EventArgs)
            If Not _config.UseSpeechmaticsTranslation Then Return
            Dim svc = _getSubtitleSvc()
            If svc Is Nothing Then Return
            For Each kvp In _sttBackends.ToList()
                ' Capability interface — no engine keys here; the backend itself
                ' knows whether its session translates inline.
                Dim retargetable = TryCast(kvp.Value, IRetargetableSttBackend)
                If retargetable Is Nothing OrElse Not retargetable.SupportsInlineTranslation Then Continue For
                Dim srcFlores = SpeechmaticsTranslation.SourceFlores(RoomSourceWhisperLang(kvp.Key))
                Dim computed = SpeechmaticsTranslation.ComputeTargets(srcFlores, svc.GetActiveTranslationLanguages())
                If Not SameTargets(retargetable.CurrentTranslationTargets, computed.SmCodes) Then
                    _log($"[Conference:{kvp.Key}] inline translation targets → [{String.Join(",", computed.SmCodes)}]; restarting session")
                    Dim backend = retargetable
                    Dim targets = computed.SmCodes
                    Task.Run(Function() backend.UpdateTranslationTargetsAsync(targets))
                End If
            Next
        End Sub

        Private Function RoomSourceWhisperLang(roomId As String) As String
            Dim tplId As String = Nothing
            If _roomTemplateIds.TryGetValue(roomId, tplId) Then
                Dim tpl = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = tplId)
                If tpl IsNot Nothing AndAlso Not String.IsNullOrEmpty(tpl.SourceLanguage) Then Return tpl.SourceLanguage
            End If
            Return "en"
        End Function

        Private Shared Function SameTargets(a As List(Of String), b As List(Of String)) As Boolean
            Dim x = If(a, New List(Of String)())
            Dim y = If(b, New List(Of String)())
            If x.Count <> y.Count Then Return False
            For i = 0 To x.Count - 1
                If Not x(i).Equals(y(i), StringComparison.OrdinalIgnoreCase) Then Return False
            Next
            Return True
        End Function

        ''' <summary>
        ''' Flush any sentence buffers that have timed out (4s since last commit).
        ''' Called on every new commit to avoid needing a separate timer.
        ''' </summary>
        Private Sub FlushExpiredBuffers(Optional excludeRoomId As String = Nothing)
            For Each kvp In _sentenceBuffers.ToList()
                If kvp.Key = excludeRoomId Then Continue For
                Dim flush = kvp.Value.TryFlushExpired()
                If flush IsNot Nothing Then
                    TranslateAndBroadcastBufferedAsync(kvp.Key, flush.Text, flush.DetectedLanguage)
                End If
            Next
        End Sub

        ' ─── Speechmatics clause hold-and-lock ───────────────────────────

        ' ─── Speakers + Online/Offline gate (runtime) ─────────────────────

        ''' <summary>The room's connectivity mode ("offline" → Offline, anything else → Online).</summary>
        Private Function RoomMode(room As Services.Rooms.Room) As Models.Templates.ConnectivityMode
            If room IsNot Nothing AndAlso "offline".Equals(If(room.Mode, ""), StringComparison.OrdinalIgnoreCase) Then
                Return Models.Templates.ConnectivityMode.Offline
            End If
            Return Models.Templates.ConnectivityMode.Online
        End Function

        ''' <summary>
        ''' The STT engine template a room should run on: the active speaker's slot
        ''' for the room's mode wins; otherwise the room template's own reference.
        ''' Both are gate-checked — an ineligible template resolves to Nothing
        ''' (caller falls back to legacy embeds / fails closed; never auto-swapped).
        ''' </summary>
        Private Function ResolveRoomSttTemplate(roomId As String, template As ConferenceTemplate) As Models.Templates.EngineTemplate
            Dim store = TemplateLibraryStore.Instance
            Dim room = _getRoomManager()?.GetRoom(roomId)
            Dim mode = RoomMode(room)
            Dim ctx = $"[Conference:{roomId}]"

            If room IsNot Nothing AndAlso Not String.IsNullOrEmpty(room.ActiveSpeakerId) Then
                Dim sp = store.GetSpeakerProfile(room.ActiveSpeakerId)
                Dim slotId = ConnectivityGate.SelectSpeakerSttTemplateId(sp, mode)
                If Not String.IsNullOrEmpty(slotId) Then
                    Dim slotTpl = ConnectivityGate.GateTemplate(
                        TemplateLibraryStore.GroupStt, store.GetEngineTemplate(TemplateLibraryStore.GroupStt, slotId), mode, ctx)
                    If slotTpl IsNot Nothing Then Return slotTpl
                End If
                AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                    $"{ctx} speaker '{sp?.Name}' has no eligible STT template for mode={mode} — using the room template's reference")
            End If

            Return ConnectivityGate.GateTemplate(
                TemplateLibraryStore.GroupStt,
                store.GetEngineTemplate(TemplateLibraryStore.GroupStt, If(template?.SttTemplateId, "")), mode, ctx)
        End Function

        ''' <summary>Validate + apply a host speaker change. Returns True when a restart should follow.</summary>
        Private Function TrySwitchSpeaker(roomId As String, room As Services.Rooms.Room, speakerId As String) As Boolean
            If room Is Nothing Then Return False
            If String.Equals(If(speakerId, ""), If(room.ActiveSpeakerId, ""), StringComparison.Ordinal) Then Return False
            If String.IsNullOrEmpty(speakerId) Then
                If String.IsNullOrEmpty(room.ActiveSpeakerId) Then Return False
                room.ActiveSpeakerId = ""
                AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED, $"room={roomId} speaker cleared (template default)")
                Return True
            End If

            Dim sp = TemplateLibraryStore.Instance.GetSpeakerProfile(speakerId)
            If sp Is Nothing Then
                AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED, $"room={roomId} speaker '{speakerId}' not found — ignored")
                Return False
            End If

            ' Pre-validate: the speaker must have an eligible STT template for the
            ' room's current mode — no auto-fallback, so reject up front.
            Dim mode = RoomMode(room)
            Dim slotId = ConnectivityGate.SelectSpeakerSttTemplateId(sp, mode)
            Dim slotTpl = If(String.IsNullOrEmpty(slotId), Nothing,
                ConnectivityGate.GateTemplate(TemplateLibraryStore.GroupStt,
                    TemplateLibraryStore.Instance.GetEngineTemplate(TemplateLibraryStore.GroupStt, slotId),
                    mode, $"[Conference:{roomId}]"))
            If slotTpl Is Nothing Then
                AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                    $"[Conference:{roomId}] speaker switch to '{sp.Name}' rejected — no eligible STT template for mode={mode}")
                Return False
            End If

            room.ActiveSpeakerId = speakerId
            AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED,
                $"room={roomId} speaker → '{sp.Name}' ({speakerId}), stt template '{slotTpl.Name}' [{slotTpl.EngineKey}], mode={mode}")
            Return True
        End Function

        ''' <summary>Validate + apply a host mode change. Rejects when nothing would be eligible under the new mode.</summary>
        Private Function TrySwitchMode(roomId As String, room As Services.Rooms.Room, modeStr As String) As Boolean
            If room Is Nothing Then Return False
            Dim newModeStr = If("offline".Equals(If(modeStr, ""), StringComparison.OrdinalIgnoreCase), "offline", "online")
            If newModeStr.Equals(If(room.Mode, "online"), StringComparison.OrdinalIgnoreCase) Then Return False

            ' Pre-validate the restart under the new mode before committing to it:
            ' the room must end up with SOME eligible engine (template or speaker
            ' slot) — otherwise reject and stay on the current mode.
            Dim previous = room.Mode
            room.Mode = newModeStr
            Dim tplId As String = Nothing
            _roomTemplateIds.TryGetValue(roomId, tplId)
            Dim template = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = tplId)
            Dim resolved = ResolveRoomSttTemplate(roomId, template)
            Dim fallbackKey = If(template?.SttBackendKey, _config.SttBackend)
            Dim eligible = resolved IsNot Nothing OrElse
                ConnectivityGate.IsEngineEligible(TemplateLibraryStore.GroupStt, fallbackKey, RoomMode(room))
            If Not eligible Then
                room.Mode = previous
                AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                    $"[Conference:{roomId}] mode change to {newModeStr} rejected — no eligible STT engine (speaker slot or template) for that mode")
                Return False
            End If

            AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED, $"room={roomId} mode → {newModeStr}")
            Return True
        End Function

        ' Python sidecar log lines ("2026-… INFO …") tailed from the log file are
        ' ALSO raised through the runner's error stream — matching them here
        ' prevents every Speechmatics commit being logged twice.
        Private Shared ReadOnly _pythonLogLinePattern As New Text.RegularExpressions.Regex(
            "^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}\s")

        ''' <summary>
        ''' Wire a room backend's output/error events to STRUCTURED logging with
        ''' engine attribution (one CONF_COMMIT line per commit: room, engine,
        ''' language, text). Genuine runner errors land as CONF_BACKEND_ERROR;
        ''' protocol chatter and tailed Python log lines are dropped (the tail
        ''' already structured-logs them under PythonLog).
        ''' </summary>
        Private Sub WireBackendLogging(roomId As String, backend As ISttBackend, engineKey As String)
            AddHandler backend.OutputCommitted,
                Sub(s, e)
                    AppLogger.Log(LogEvents.CONF_COMMIT,
                        $"room={roomId} engine={engineKey} lang={e.DetectedLanguage} chars={If(e.Text, "").Length} text=""{Truncate(e.Text, 160)}""")
                    TranslateAndBroadcastForRoomAsync(roomId, e)
                End Sub

            AddHandler backend.OutputCommittedTranslated,
                Sub(s, e)
                    AppLogger.Log(LogEvents.CONF_COMMIT,
                        $"room={roomId} engine={engineKey} lang={e.DetectedLanguage} inlineTx={e.Translations.Count} text=""{Truncate(e.Text, 160)}""")
                    HandleTranslatedCommitAsync(roomId, e)
                End Sub

            AddHandler backend.ErrorReceived,
                Sub(s, line)
                    If line.StartsWith(">>>") OrElse
                       line.Contains("ASGI callable returned without completing response") OrElse
                       _pythonLogLinePattern.IsMatch(line) Then Return
                    AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"room={roomId} engine={engineKey} {line}")
                End Sub
        End Sub

        ''' <summary>The STT backend key for a room (template override, else global config).</summary>
        Private Function RoomBackendKey(roomId As String) As String
            Dim tplId As String = Nothing
            If _roomTemplateIds.TryGetValue(roomId, tplId) Then
                Dim tpl = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = tplId)
                If tpl IsNot Nothing AndAlso Not String.IsNullOrEmpty(tpl.SttBackendKey) Then Return tpl.SttBackendKey
            End If
            Return If(_config.SttBackend, "")
        End Function

        ''' <summary>
        ''' Pin a room's clause dials when its STT template explicitly stores any
        ''' clause field; otherwise the room keeps live app-global dials.
        ''' </summary>
        Private Sub StorePinnedClauseDials(roomId As String, engineTpl As Models.Templates.EngineTemplate, sttConfig As SttSessionConfig)
            DropKey(_pinnedClauseDials, roomId)
            Dim sm = sttConfig.Block(Of Configs.SpeechmaticsConfig)()
            If sm Is Nothing OrElse engineTpl Is Nothing OrElse Not engineTpl.Config.HasValue Then Return
            If engineTpl.Config.Value.ValueKind <> System.Text.Json.JsonValueKind.Object Then Return
            For Each prop In engineTpl.Config.Value.EnumerateObject()
                If prop.Name.Equals("HoldClauses", StringComparison.OrdinalIgnoreCase) OrElse
                   prop.Name.StartsWith("Clause", StringComparison.OrdinalIgnoreCase) Then
                    _pinnedClauseDials(roomId) = sm
                    AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                        $"[Conference:{roomId}] clause dials pinned by template '{engineTpl.Name}' (hold={sm.HoldClauses}, grace={sm.ClauseGraceMs}ms)")
                    Return
                End If
            Next
        End Sub

        ''' <summary>
        ''' Resolve the room's effective NAMED filter set: room template's set,
        ''' with the active speaker's glossary set overriding the glossary path
        ''' (speaker > room > global). No named set anywhere = no entry, and the
        ''' sidecars keep using their own global files.
        ''' </summary>
        Private Sub ResolveRoomFilters(roomId As String, template As ConferenceTemplate)
            Dim fs = SessionResolver.ResolveFilterSet(If(template?.FilterSetId, ""), _config)
            Dim named = Not String.IsNullOrEmpty(fs.Id)

            Dim room = _getRoomManager()?.GetRoom(roomId)
            If room IsNot Nothing AndAlso Not String.IsNullOrEmpty(room.ActiveSpeakerId) Then
                Dim sp = TemplateLibraryStore.Instance.GetSpeakerProfile(room.ActiveSpeakerId)
                If sp IsNot Nothing AndAlso Not String.IsNullOrEmpty(sp.GlossarySetId) Then
                    Dim spSet = SessionResolver.ResolveFilterSet(sp.GlossarySetId, _config)
                    If Not String.IsNullOrEmpty(spSet.Id) Then
                        fs.GlossaryPath = spSet.GlossaryPath
                        named = True
                    End If
                End If
            End If

            If named Then
                _roomFilters(roomId) = fs
                AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                    $"[Conference:{roomId}] filter set '{fs.Name}' active (glossary={fs.GlossaryPath})")
            Else
                DropKey(_roomFilters, roomId)
            End If
        End Sub

        ''' <summary>Per-room translation filter paths for the sidecar (Nothing = global files).</summary>
        Private Function RoomTranslationFilters(roomId As String) As Services.Models.TranslationFilterPaths
            Dim fs As Models.Templates.FilterSet = Nothing
            If Not _roomFilters.TryGetValue(roomId, fs) Then Return Nothing
            Return New Services.Models.TranslationFilterPaths With {
                .GlossaryPath = fs.GlossaryPath,
                .ProfanityPath = fs.ProfanityPath
            }
        End Function

        ''' <summary>Whether clause hold-and-lock applies to this room (pinned template value, else live master switch; Speechmatics only).</summary>
        Private Function IsHoldEnabled(roomId As String) As Boolean
            Dim pinned As Configs.SpeechmaticsConfig = Nothing
            If _pinnedClauseDials.TryGetValue(roomId, pinned) Then Return pinned.HoldClauses
            Return _config.SpeechmaticsHoldClauses AndAlso
                   RoomBackendKey(roomId).Equals("speechmatics", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Clause dials for a room: the template-pinned values when the room's STT
        ''' template set them, otherwise a live read of the app-global dials
        ''' (Options changes apply immediately to unpinned rooms).
        ''' </summary>
        Private Function CurrentThresholds(roomId As String) As SpeechmaticsClauseAccumulator.Thresholds
            Dim pinned As Configs.SpeechmaticsConfig = Nothing
            If _pinnedClauseDials.TryGetValue(roomId, pinned) Then
                Return New SpeechmaticsClauseAccumulator.Thresholds With {
                    .GraceMs = pinned.ClauseGraceMs,
                    .MaxMs = pinned.ClauseMaxMs,
                    .MaxChars = pinned.ClauseMaxChars,
                    .LockOnPunctuation = pinned.ClauseLockOnPunctuation,
                    .MinLockChars = pinned.ClauseMinLockChars,
                    .SentenceEnders = pinned.ClauseSentenceEnders
                }
            End If
            Return New SpeechmaticsClauseAccumulator.Thresholds With {
                .GraceMs = _config.SpeechmaticsClauseGraceMs,
                .MaxMs = _config.SpeechmaticsClauseMaxMs,
                .MaxChars = _config.SpeechmaticsClauseMaxChars,
                .LockOnPunctuation = _config.SpeechmaticsClauseLockOnPunctuation,
                .MinLockChars = _config.SpeechmaticsClauseMinLockChars,
                .SentenceEnders = _config.SpeechmaticsClauseSentenceEnders
            }
        End Function

        ''' <summary>Feed a Speechmatics fragment into the room's clause accumulator; broadcast if it locks.</summary>
        Private Sub FeedClause(roomId As String, text As String, detectedLang As String)
            If String.IsNullOrWhiteSpace(text) Then Return
            Dim acc = _clauseAccumulators.GetOrAdd(roomId, Function(k) New SpeechmaticsClauseAccumulator())
            Dim locked = acc.Add(text, detectedLang, CurrentThresholds(roomId))

            ' Per-fragment trace: the gap before this fragment is the raw signal for
            ' tuning GraceMs (gaps the speaker took mid-clause vs. between clauses).
            AppLogger.Log(LogEvents.CONF_CLAUSE_FRAGMENT,
                $"room={roomId} frag#{acc.LastFragmentIndex} gapMs={acc.LastGapMs} fragChars={text.Trim().Length} lang={detectedLang}")

            If locked IsNot Nothing Then OnClauseLocked(roomId, locked)
        End Sub

        ''' <summary>Lock-on-timeout for all clause accumulators (grace-window expiry). Called from the timer.</summary>
        Private Sub FlushExpiredClauses()
            If _clauseAccumulators.Count = 0 Then Return
            For Each kvp In _clauseAccumulators.ToList()
                Dim locked = kvp.Value.TryLockExpired(CurrentThresholds(kvp.Key))
                If locked IsNot Nothing Then OnClauseLocked(kvp.Key, locked)
            Next
        End Sub

        ''' <summary>Force-lock and broadcast any pending clause for a room (close/reset/shutdown).</summary>
        Private Sub ForceLockClause(roomId As String)
            Dim acc As SpeechmaticsClauseAccumulator = Nothing
            If _clauseAccumulators.TryGetValue(roomId, acc) Then
                Dim locked = acc.ForceLock()
                If locked IsNot Nothing Then OnClauseLocked(roomId, locked)
            End If
        End Sub

        ''' <summary>
        ''' Emit per-lock tuning diagnostics, then broadcast the clause. The log line is
        ''' the single source for optimising the dials: reason shows which threshold
        ''' fired, gaps[] is the inter-fragment silence distribution (→ GraceMs),
        ''' frags/durMs/chars show whether the runaway caps (MaxMs/MaxChars) are biting.
        ''' </summary>
        Private Sub OnClauseLocked(roomId As String, locked As LockResult)
            If locked Is Nothing Then Return
            Dim t = CurrentThresholds(roomId)
            Dim gaps = If(locked.Gaps IsNot Nothing AndAlso locked.Gaps.Count > 0, String.Join(",", locked.Gaps), "")
            Dim maxGap = If(locked.Gaps IsNot Nothing AndAlso locked.Gaps.Count > 0, locked.Gaps.Max(), 0)
            AppLogger.Log(LogEvents.CONF_CLAUSE_LOCK,
                $"room={roomId} reason={locked.Reason} frags={locked.FragmentCount} durMs={locked.DurationMs} " &
                $"chars={If(locked.Text, "").Length} maxGapMs={maxGap} gaps=[{gaps}] " &
                $"dials(graceMs={t.GraceMs},maxMs={t.MaxMs},maxChars={t.MaxChars},minLockChars={t.MinLockChars},lockPunct={t.LockOnPunctuation}) " &
                $"text=""{Truncate(locked.Text, 120)}""")
            BroadcastLockedClauseAsync(roomId, locked.Text, locked.DetectedLanguage)
        End Sub

        Private Shared Function Truncate(s As String, n As Integer) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Return If(s.Length > n, s.Substring(0, n) & "…", s)
        End Function

        ''' <summary>
        ''' Broadcast a fully-locked clause: source immediately, then ONE whole-clause
        ''' translation via the configured backend (orchestrator → NLLB). Mirrors the
        ''' non-held commit path but operates on a complete clause, not a fragment.
        ''' </summary>
        Private Async Sub BroadcastLockedClauseAsync(roomId As String, text As String, detectedLang As String)
            ' Async Sub fired from timer/sidecar threads — contain all exceptions.
            Try
                Await BroadcastLockedClauseCoreAsync(roomId, text, detectedLang)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"room={roomId} clause broadcast failed: {ex.Message}")
            End Try
        End Sub

        Private Async Function BroadcastLockedClauseCoreAsync(roomId As String, text As String, detectedLang As String) As Task
            If String.IsNullOrWhiteSpace(text) Then Return

            ' Drop while paused (keeps the engine warm).
            Dim room = _getRoomManager?.Invoke()?.GetRoom(roomId)
            If room IsNot Nothing AndAlso room.Config.IsPaused Then
                AppLogger.Log(LogEvents.CONF_COMMIT_DROPPED, $"room={roomId} reason=paused-clause text=""{Truncate(text, 80)}""")
                Return
            End If

            Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), "eng_Latn")
            Dim sourceShort = TranslationService.FloresToShortCode(sourceLang)
            Dim subtitleSvc = _getSubtitleSvc()
            If subtitleSvc Is Nothing Then Return

            If IsGarbageCommit(text) Then
                _log($"[Conference:{roomId}] Filtered garbage clause")
                subtitleSvc.BroadcastCommit(text, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)
                Return
            End If


            ' Source immediately for source-language viewers.
            subtitleSvc.BroadcastCommit(text, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)

            Dim targets = subtitleSvc.GetActiveTranslationLanguages()
            targets?.Remove(sourceLang)
            If targets Is Nothing OrElse targets.Count = 0 Then Return

            Dim translations = Await TranslateTargetsAsync(roomId, text, sourceLang, targets.ToList())
            If translations.Count = 0 Then Return
            BroadcastTranslated(subtitleSvc, roomId, text, sourceShort, sourceLang, translations)
        End Function

        ' ─── Helpers ──────────────────────────────────────────────────

        Private Shared Function WhisperToNllbCode(whisperLang As String) As String
            Dim result = TranslationService.WhisperToFloresLang(whisperLang.ToLowerInvariant())
            Return If(String.IsNullOrEmpty(result), "eng_Latn", result)
        End Function

        Private Shared Function IsGarbageCommit(line As String) As Boolean
            Dim trimmed = line.Trim().TrimEnd("."c).Trim()
            If String.IsNullOrWhiteSpace(trimmed) Then Return True
            If trimmed.Length <= 3 AndAlso Not trimmed.Any(Function(c) Char.IsDigit(c)) Then Return True
            If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then Return True
            Return False
        End Function

    End Class
End Namespace
