Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Server
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Stt
Imports EveryTongue.Services.Translation

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
        ' Ensures the translation backend for a room's engine KEY is ready before the
        ' room translates: for a local/NLLB engine, starts the sidecar with that key's
        ' model on demand; for cloud engines, a no-op (configured at server start).
        ' The Boolean is "may reload" — True when no other active room is using a local
        ' engine, so the sidecar may be reloaded to THIS room's model (room overrides
        ' the global default); False = another local room is active, so share its model.
        ' Resolve + ensure a room's translation backend, returning the orchestrator backend
        ' NAME to route by (per-model offline sidecars run concurrently). Paired with release.
        Private ReadOnly _acquireTranslationBackend As Func(Of String, String, String)
        Private ReadOnly _releaseTranslationBackend As Action(Of String)
        ' Relays per-room engine readiness ("preparing" → "ready") to web room clients.
        Private ReadOnly _readiness As Services.Rooms.RoomReadinessNotifier
        ' Per-room resolved translation backend name (what AcquireRoomTranslationBackend returned).
        Private ReadOnly _roomTranslationBackendName As New Concurrent.ConcurrentDictionary(Of String, String)()
        ' Resolved backend name per warmed SHADOW engine, keyed "roomId|engineKey".
        ' BackendNameForKey returns "Local" for every NLLB variant, so without this
        ' a pooled shadow (e.g. int8) mis-routes to the default sidecar and only
        ' reaches the right model via the orchestrator's fallback (a 4009 per commit).
        Private ReadOnly _shadowBackendNames As New Concurrent.ConcurrentDictionary(Of String, String)()
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _ownerForm As Form

        Private ReadOnly _sttBackends As New Concurrent.ConcurrentDictionary(Of String, ISttBackend)()
        Private ReadOnly _roomTemplateIds As New Concurrent.ConcurrentDictionary(Of String, String)()
        Private ReadOnly _sentenceBuffers As New Concurrent.ConcurrentDictionary(Of String, SentenceBuffer)()
        ' Speechmatics-only clause hold-and-lock decision state machine (engine-owned).
        ' The coordinator owns the accumulators, pinned dials and flush timer; this
        ' controller keeps translation + broadcast execution via the callbacks below.
        Private ReadOnly _clauseCoordinator As SpeechmaticsClauseCoordinator
        ' Per-room NAMED filter sets (room template's FilterSetId, with the
        ' active speaker's glossary set overriding the glossary path). No entry
        ' = the sidecars use their own global files.
        Private ReadOnly _roomFilters As New Concurrent.ConcurrentDictionary(Of String, Models.Templates.FilterSet)()
        ' Per-room TRANSLATION engine key (the room template's TranslationBackendKey,
        ' else the global config default). The room's translation always uses ITS OWN
        ' engine, overriding the global Options translation engine — the global setting
        ' is only the default for rooms whose template doesn't specify one (and for all
        ' non-conference paths).
        '
        ' CONSTRAINT: the NLLB sidecar holds ONE model at a time, so two simultaneous
        ' rooms requesting DIFFERENT NLLB models share the first-loaded model (logged
        ' once). Cloud engines have no such limit — multiple rooms can run different
        ' cloud translation engines concurrently.
        Private ReadOnly _roomTranslationKey As New Concurrent.ConcurrentDictionary(Of String, String)()
        Private _nextConferencePort As Integer = 5101
        Private _nextWhisperServerPort As Integer = 8179  ' 8178 is used by ConversationAudioHandler

        Public Sub New(config As AppConfig,
                       getSubtitleSvc As Func(Of ISubtitleService),
                       getTranslationService As Func(Of TranslationService),
                       getTranslationOrchestrator As Func(Of ITranslationService),
                       getRoomManager As Func(Of Services.Rooms.RoomManager),
                       acquireTranslationBackend As Func(Of String, String, String),
                       releaseTranslationBackend As Action(Of String),
                       readiness As Services.Rooms.RoomReadinessNotifier,
                       log As Action(Of String),
                       ownerForm As Form)
            _config = config
            _getSubtitleSvc = getSubtitleSvc
            _getTranslationService = getTranslationService
            _getTranslationOrchestrator = getTranslationOrchestrator
            _getRoomManager = getRoomManager
            _acquireTranslationBackend = acquireTranslationBackend
            _releaseTranslationBackend = releaseTranslationBackend
            _readiness = readiness
            _log = log
            _ownerForm = ownerForm

            ' Clause hold-and-lock decisions live in the engine-owned coordinator;
            ' its internal flush timer also drives the sentence-buffer flush
            ' (NLLB path) via the onTimerTick hook, exactly as before.
            _clauseCoordinator = New SpeechmaticsClauseCoordinator(
                config,
                roomBackendKey:=AddressOf RoomBackendKey,
                onClauseLocked:=AddressOf BroadcastLockedClauseAsync,
                onTimerTick:=Sub() FlushExpiredBuffers(),
                log:=log,
                segment:=Function(roomId As String, text As String) As List(Of String)
                             ' Split a held clause into sentences via the room's
                             ' live-server SaT segmenter (Speechmatics/cloud only).
                             Dim backend As ISttBackend = Nothing
                             If _sttBackends.TryGetValue(roomId, backend) AndAlso TypeOf backend Is CloudStreamingSttBackend Then
                                 Return DirectCast(backend, CloudStreamingSttBackend).Segment(
                                     text, config.SpeechmaticsSatThresholdPercent, config.SpeechmaticsSatModel)
                             End If
                             Return New List(Of String) From {text}
                         End Function)
        End Sub

        ''' <summary>ConcurrentDictionary remove without the ByRef-out ceremony.</summary>
        Private Shared Sub DropKey(Of TValue)(dict As Concurrent.ConcurrentDictionary(Of String, TValue), key As String)
            Dim ignored As TValue = Nothing
            dict.TryRemove(key, ignored)
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

            ' Host pause → reset the STT per-speaker pace auto-tune (context change).
            ' Speechmatics handles reset_pace; other engines' pipelines ignore it.
            EndpointRegistration.RoomPausedHandler = Sub(roomId As String, paused As Boolean)
                                                         If Not paused Then Return   ' reset on pause, not resume
                                                         Try
                                                             Dim backend As ISttBackend = Nothing
                                                             If _sttBackends.TryGetValue(roomId, backend) AndAlso backend IsNot Nothing Then
                                                                 backend.UpdateConfigAsync(New Dictionary(Of String, Object) From {{"reset_pace", True}})
                                                             End If
                                                         Catch ex As Exception
                                                             _log($"[Conference] pace-reset-on-pause failed: {ex.Message}")
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

            ' Apply the template's default speaker so the room boots pre-selected on that
            ' speaker's engine (the host menu shows them selected). Only when the speaker has
            ' an eligible STT template for the room's mode; otherwise leave the template default.
            Dim roomForDefault = _getRoomManager()?.GetRoom(roomId)
            If roomForDefault IsNot Nothing AndAlso String.IsNullOrEmpty(roomForDefault.ActiveSpeakerId) AndAlso
               Not String.IsNullOrEmpty(template.DefaultSpeakerId) AndAlso
               template.SpeakerProfileIds IsNot Nothing AndAlso template.SpeakerProfileIds.Contains(template.DefaultSpeakerId) Then
                Dim dsp = TemplateLibraryStore.Instance.GetSpeakerProfile(template.DefaultSpeakerId)
                Dim dMode = RoomMode(roomForDefault)
                Dim dSlotId = ConnectivityGate.SelectSpeakerSttTemplateId(dsp, dMode)
                Dim dSlotTpl = If(String.IsNullOrEmpty(dSlotId), Nothing,
                    ConnectivityGate.GateTemplate(TemplateLibraryStore.GroupStt,
                        TemplateLibraryStore.Instance.GetEngineTemplate(TemplateLibraryStore.GroupStt, dSlotId),
                        dMode, $"[Conference:{roomId}]"))
                If dSlotTpl IsNot Nothing Then
                    roomForDefault.ActiveSpeakerId = template.DefaultSpeakerId
                    AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED,
                        $"room={roomId} default speaker → '{dsp.Name}' ({template.DefaultSpeakerId}), stt template '{dSlotTpl.Name}' [{dSlotTpl.EngineKey}], mode={dMode}")
                Else
                    AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                        $"[Conference:{roomId}] default speaker '{dsp?.Name}' has no eligible STT template for mode={dMode} — using template default")
                End If
            End If

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
                .DeviceIndex = ResolveAudioDeviceIndex(template),
                .Language = If(template.SourceLanguage, "auto"),
                .TranslateToEnglish = False,
                .ServerPort = port,
                .ApiKey = _config.GetSttApiKey(backendKey),
                .EngineConfig = EngineConfigResolver.ResolveStt(
                    backendKey, _config, template:=engineTpl, fieldOverrides:=tplOverrides, contextLabel:=$"[Conference:{roomId}]")
            }

            ' Resolve + remember the room's OWN translation engine (template wins;
            ' else the global config default) BEFORE configuring inline translation,
            ' so the inline-vs-fallback decision can read the resolved engine. This
            ' overrides the global Options translation engine for this room's calls.
            ResolveRoomTranslationEngine(roomId, template)

            SpeechmaticsTranslation.ConfigureSession(sttConfig, _config, sttConfig.Language,
                                                     _getSubtitleSvc()?.GetActiveTranslationLanguages(),
                                                     ResolveInlineEnabled(roomId, backendKey))
            _clauseCoordinator.StorePinnedClauseDials(roomId, engineTpl, sttConfig)
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
                StartReadinessWatch(roomId, backend)
            Else
                _log($"[Conference] Backend FAILED to start for room {roomId}")
                DropKey(_sttBackends, roomId)
                DropKey(_roomTemplateIds, roomId)
                DropKey(_roomTranslationKey, roomId)
                _releaseTranslationBackend?.Invoke(roomId)
                DropKey(_roomTranslationBackendName, roomId)
                DropShadowBackendNames(roomId)
            End If
        End Sub

        ''' <summary>
        ''' Resolve the CURRENT PortAudio input-device index for a template at capture start.
        ''' PortAudio indices renumber when devices are added/removed, so a stored index can
        ''' drift to the wrong device (e.g. S/PDIF, no input) → silent capture. If the template
        ''' has a saved device NAME, we enumerate the current input devices and use the index of
        ''' the device whose name matches. Otherwise (or if not found/invalid) we fall back to the
        ''' stored AudioDeviceId, validated against the current enumeration, finally defaulting to 0.
        ''' Enumeration happens once per room start (cheap relative to the capture session) — never
        ''' per audio frame.
        ''' </summary>
        Private Function ResolveAudioDeviceIndex(template As ConferenceTemplate) As Integer
            Dim storedId = If(template IsNot Nothing AndAlso template.AudioDeviceId >= 0, template.AudioDeviceId, 0)
            Dim storedName = If(template?.AudioDeviceName, "")

            Dim devices As List(Of AudioDeviceInfo) = Nothing
            Try
                Dim pythonPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
                devices = SttBackendRegistry.CreateBackend().EnumerateDevicesAsync(pythonPath)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONF_BACKEND_ERROR,
                    $"ResolveAudioDeviceIndex: device enumeration failed ({ex.Message}) — using stored index {storedId}")
                Return storedId
            End Try

            If devices Is Nothing OrElse devices.Count = 0 Then
                ' Couldn't enumerate — trust the stored index.
                Return storedId
            End If

            ' Prefer match by NAME (survives index drift).
            If Not String.IsNullOrWhiteSpace(storedName) Then
                Dim byName = devices.FirstOrDefault(Function(d) d IsNot Nothing AndAlso d.Id >= 0 AndAlso
                    String.Equals(d.Name?.Trim(), storedName.Trim(), StringComparison.OrdinalIgnoreCase))
                If byName IsNot Nothing Then
                    If byName.Id <> storedId Then
                        AppLogger.Log(LogEvents.CONF_BACKEND_STARTING,
                            $"Audio device '{storedName}' re-resolved to current index {byName.Id} (saved index was {storedId}).")
                    End If
                    Return byName.Id
                End If
            End If

            ' No name match — validate the stored index is still a real input device.
            Dim byId = devices.FirstOrDefault(Function(d) d IsNot Nothing AndAlso d.Id = storedId)
            If byId IsNot Nothing Then
                Return storedId
            End If

            ' Stored index isn't present/valid → fall back to default and warn.
            AppLogger.Log(LogEvents.CONF_BACKEND_ERROR,
                $"Audio device '{storedName}' (saved index {storedId}) not found among current input devices — using default. PortAudio indices change when devices are added/removed.")
            Return 0
        End Function

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

            ' Skip the live config update when a restart is already coming: the restart
            ' re-applies every param (language → cfgLang) on the fresh backend, and pushing
            ' an HTTP config call to a live-server that is about to be shut down just races
            ' the shutdown and logs a spurious "UpdateConfigAsync failed" error.
            If runtimeParams.Count > 0 AndAlso Not needsRestart Then
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
            _clauseCoordinator.ForceLockClause(roomId)

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
            If hasTpl Then cfgDevice = ResolveAudioDeviceIndex(template)

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

            ' Re-resolve the room's translation engine before configuring inline
            ' translation so the inline-vs-fallback decision is current.
            ResolveRoomTranslationEngine(roomId, template)

            SpeechmaticsTranslation.ConfigureSession(sttConfig, _config, cfgLang,
                                                     _getSubtitleSvc()?.GetActiveTranslationLanguages(),
                                                     ResolveInlineEnabled(roomId, restartBackendKey))
            _clauseCoordinator.StorePinnedClauseDials(roomId, engineTpl, sttConfig)
            ResolveRoomFilters(roomId, template)
            Dim restartFs As Models.Templates.FilterSet = Nothing
            If _roomFilters.TryGetValue(roomId, restartFs) Then sttConfig.HallucinationsPath = restartFs.HallucinationsPath

            ' Must create the backend for the RESOLVED engine (speaker STT template wins),
            ' not the conference template's default — otherwise switching to a speaker whose
            ' STT template uses a different engine (e.g. speechmatics) builds the wrong backend
            ' type (whisper-cpp) and the speechmatics session config is silently ignored.
            Dim newBackend = SttBackendRegistry.CreateBackend(restartBackendKey)
            WireBackendLogging(roomId, newBackend, restartBackendKey)

            _sttBackends(roomId) = newBackend
            _log($"[Pipeline:{roomId}] Restarting backend (port={sttConfig.ServerPort})")
            newBackend.Start(sttConfig)
            If newBackend.IsRunning Then StartReadinessWatch(roomId, newBackend)
        End Sub

        ''' <summary>
        ''' Relay this room's engine readiness ("preparing" → "ready") to its web clients.
        ''' STT readiness gates the (informational, no web mic) indicator; translation readiness
        ''' is shown separately and only for OFFLINE engines that need to load (cloud/inline = instant).
        ''' </summary>
        Private Sub StartReadinessWatch(roomId As String, backend As ISttBackend)
            If _readiness Is Nothing Then Return
            Dim capturedBackend = backend
            Dim sttProbe As Func(Of Threading.CancellationToken, Task(Of Boolean)) =
                Function(ct) capturedBackend.CheckHealthAsync(ct)
            _readiness.Watch(roomId, sttProbe, BuildConferenceTranslationProbe(roomId))
        End Sub

        ''' <summary>Translation-readiness probe for a conference room, or Nothing when no note is needed.</summary>
        Private Function BuildConferenceTranslationProbe(roomId As String) As Func(Of Threading.CancellationToken, Task(Of Boolean))
            Dim roomKey = RoomTranslationKey(roomId)
            If TranslationBackendRegistry.IsInlineEngine(roomKey) Then Return Nothing
            Dim entry = TranslationBackendRegistry.Find(roomKey)
            Dim isOffline = entry IsNot Nothing AndAlso Not String.IsNullOrEmpty(entry.ModelType)
            If Not isOffline Then Return Nothing  ' cloud — instant
            Dim nm As String = Nothing
            _roomTranslationBackendName.TryGetValue(roomId, nm)
            If String.IsNullOrEmpty(nm) Then nm = TranslationBackendRegistry.BackendNameForKey(roomKey)
            Dim orch = _getTranslationOrchestrator?.Invoke()
            If orch Is Nothing OrElse String.IsNullOrEmpty(nm) Then Return Nothing
            Dim capturedName = nm
            Return Function(ct) Task.FromResult(
                orch.GetAllBackends().Any(Function(b) b.Name.Equals(capturedName, StringComparison.OrdinalIgnoreCase) AndAlso b.IsAvailable))
        End Function

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
            _clauseCoordinator.ForceLockClause(roomId)
            _clauseCoordinator.ClearRoom(roomId)
            _readiness?.ClearRoom(roomId)
            DropKey(_roomFilters, roomId)
            DropKey(_roomTranslationKey, roomId)
            ' Release this room's offline translation sidecar (refcounted; frees VRAM when last room leaves).
            _releaseTranslationBackend?.Invoke(roomId)
            DropKey(_roomTranslationBackendName, roomId)
            DropShadowBackendNames(roomId)

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
            _clauseCoordinator.ForceLockAll()

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
            _roomTranslationKey.Clear()
            For Each rid In _roomTranslationBackendName.Keys.ToList()
                _releaseTranslationBackend?.Invoke(rid)
            Next
            _roomTranslationBackendName.Clear()
            Task.WaitAll(stopTasks.ToArray(), 10000)
        End Sub

        ' ─── Translation Pipeline ─────────────────────────────────────

        Private Sub TranslateAndBroadcastForRoomAsync(roomId As String, commitArgs As SttOutputEventArgs)
            ' Speechmatics clause hold-and-lock: when enabled for this room, route the
            ' fragment into the accumulator instead of broadcasting it immediately.
            If _clauseCoordinator.IsHoldEnabled(roomId) Then
                _clauseCoordinator.FeedClause(roomId, commitArgs.Text, commitArgs.DetectedLanguage)
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

            ' Buffer-vs-immediate is a property of THIS room's engine, not the global
            ' active backend: offline (NLLB, ModelType set) buffers into sentences;
            ' cloud + inline engines translate each commit immediately.
            Dim roomTransEntry = TranslationBackendRegistry.Find(RoomTranslationKey(roomId))
            Dim isCloud = roomTransEntry Is Nothing OrElse String.IsNullOrEmpty(roomTransEntry.ModelType)

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

            ' Second/third-opinion shadow translations (log-only, never broadcast,
            ' lowest scheduler priority) — fire-and-forget so the live path is never
            ' delayed. Pairs with this commit's TRANS_RESULT by source text.
            FireShadowTranslations(roomId, text, sourceLang, targets)

            ' The room translates with ITS OWN engine (template's, else the global
            ' default), passed as a per-call override so it wins over the global
            ' orchestrator active backend and any per-language overrides.
            '
            ' INLINE engines (e.g. Speechmatics) are NOT orchestrator backends — the
            ' inline translations arrive on the commit itself. Here the orchestrator is
            ' only the FALLBACK for languages the inline engine couldn't cover, so we
            ' pass no override and let the orchestrator use the GLOBAL DEFAULT backend.
            Dim roomKey = RoomTranslationKey(roomId)
            Dim isInline = TranslationBackendRegistry.IsInlineEngine(roomKey)
            ' Route to the backend name resolved at room start (per-model offline sidecar,
            ' "Local" for the default model, or the cloud backend name).
            Dim storedName As String = Nothing
            _roomTranslationBackendName.TryGetValue(roomId, storedName)
            Dim backendName = If(isInline, Nothing,
                                 If(String.IsNullOrEmpty(storedName), TranslationBackendRegistry.BackendNameForKey(roomKey), storedName))

            Dim sw = Diagnostics.Stopwatch.StartNew()
            Dim orchestrator = _getTranslationOrchestrator?.Invoke()
            If orchestrator IsNot Nothing AndAlso orchestrator.GetAllBackends().Any(Function(b) b.IsAvailable) Then
                Dim actualBackend = If(isInline, orchestrator.ActiveBackend, backendName)
                Dim engineLabel = If(isInline, $"{roomKey}->fallback", roomKey)
                Try
                    Using cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(10))
                        Dim result = Await orchestrator.TranslateAsync(
                            text, sourceLang, targets, cts.Token, Services.Scheduling.TranslationPriority.Room,
                            filters:=RoomTranslationFilters(roomId), backendOverride:=backendName)
                        If result IsNot Nothing Then
                            For Each kvp In result : translations(kvp.Key) = kvp.Value : Next
                        End If
                    End Using
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"room={roomId} engine={engineLabel} backend={actualBackend} {sourceLang}→[{String.Join(",", targets)}] failed: {ex.Message}")
                End Try
                If translations.Count > 0 Then
                    AppLogger.Log(LogEvents.TRANS_RESULT,
                        $"room={roomId} engine={engineLabel} backend={actualBackend} {sourceLang}→[{String.Join(",", translations.Keys)}] ok={translations.Count}/{targets.Count} {sw.ElapsedMilliseconds}ms" &
                        FormatTransBlock(sourceLang, text, translations))
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
                        $"room={roomId} backend=nllb-direct {sourceLang}→[{String.Join(",", translations.Keys)}] ok={translations.Count}/{targets.Count} {sw.ElapsedMilliseconds}ms" &
                        FormatTransBlock(sourceLang, text, translations))
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"room={roomId} backend=nllb-direct {sourceLang}→[{String.Join(",", targets)}] failed: {ex.Message}")
                End Try
            End If
            Return translations
        End Function

        ''' <summary>
        ''' LOG-ONLY "second opinion" translations of a commit from the engines in
        ''' AppConfig.ShadowTranslationEngines, for post-session engine comparison
        ''' (which engine handles Catalan best?). Never broadcast. Raw engine output —
        ''' no glossary/profanity filters — so the comparison is engine-vs-engine.
        ''' Runs at Benchmark (lowest) priority and never blocks the live path.
        ''' </summary>
        Private Sub FireShadowTranslations(roomId As String, text As String, sourceLang As String, targets As List(Of String))
            If Not _config.ShadowTranslationsEnabled Then Return
            Dim raw = _config.ShadowTranslationEngines
            If String.IsNullOrWhiteSpace(raw) Then Return
            Dim orchestrator = _getTranslationOrchestrator?.Invoke()
            If orchestrator Is Nothing Then Return
            Dim primary = RoomTranslationKey(roomId)
            Dim available = orchestrator.GetAllBackends()
            Dim targetsCopy = targets.ToList()
            Dim primaryBackend As String = Nothing
            _roomTranslationBackendName.TryGetValue(roomId, primaryBackend)

            For Each engineRaw In raw.Split(","c)
                Dim engineKey = engineRaw.Trim().ToLowerInvariant()
                If engineKey.Length = 0 Then Continue For
                If engineKey.Equals(primary, StringComparison.OrdinalIgnoreCase) Then Continue For   ' opinion must differ from primary
                If TranslationBackendRegistry.IsInlineEngine(engineKey) Then Continue For
                ' Warmed offline shadows resolved to a specific sidecar (pool name);
                ' fall back to the registry name for cloud engines.
                Dim backendName As String = Nothing
                If Not _shadowBackendNames.TryGetValue($"{roomId}|{engineKey}", backendName) Then
                    backendName = TranslationBackendRegistry.BackendNameForKey(engineKey)
                End If
                If String.IsNullOrEmpty(backendName) OrElse
                   Not available.Any(Function(b) b.Name = backendName AndAlso b.IsAvailable) Then Continue For
                ' Different key but SAME resolved backend as the primary (e.g. "nllb"
                ' shadow while primary is "nllb-3.3b" — both = Local) → duplicate, skip.
                If Not String.IsNullOrEmpty(primaryBackend) AndAlso backendName = primaryBackend Then Continue For

                Dim key = engineKey
                Dim bn = backendName
                Task.Run(Async Function()
                             Dim sw = Diagnostics.Stopwatch.StartNew()
                             Try
                                 Using cts As New Threading.CancellationTokenSource(TimeSpan.FromSeconds(15))
                                     Dim result = Await orchestrator.TranslateAsync(
                                         text, sourceLang, targetsCopy, cts.Token,
                                         Services.Scheduling.TranslationPriority.Benchmark,
                                         filters:=Nothing, backendOverride:=bn)
                                     If result IsNot Nothing AndAlso result.Count > 0 Then
                                         AppLogger.Log(LogEvents.TRANS_SHADOW,
                                             $"room={roomId} opinion={key} {sourceLang}→[{String.Join(",", result.Keys)}] {sw.ElapsedMilliseconds}ms" &
                                             FormatTransBlock(sourceLang, text, result))
                                     Else
                                         AppLogger.Log(LogEvents.TRANS_SHADOW,
                                             $"room={roomId} opinion={key} {sourceLang}→[{String.Join(",", targetsCopy)}] no result ({sw.ElapsedMilliseconds}ms)")
                                     End If
                                 End Using
                             Catch ex As Exception
                                 ' Best-effort — a failed opinion is data, not an error.
                                 AppLogger.Log(LogEvents.TRANS_SHADOW,
                                     $"room={roomId} opinion={key} {sourceLang}→[{String.Join(",", targetsCopy)}] failed: {ex.Message}")
                             End Try
                         End Function)
            Next
        End Sub

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
            If _clauseCoordinator.IsHoldEnabled(roomId) Then
                _clauseCoordinator.FeedClause(roomId, args.Text, args.DetectedLanguage)
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
            ' Shadow comparison: log the engine's own inline translations as an opinion
            ' (RAW, pre-glossary — engine-vs-engine) and fire the configured shadow
            ' engines on the SAME commit text, so every engine translates identical
            ' units. Only the inline-covered targets are shadowed here; the uncovered
            ' `remaining` targets go through TranslateTargetsAsync, which shadows them.
            If _config.ShadowTranslationsEnabled AndAlso merged.Count > 0 Then
                AppLogger.Log(LogEvents.TRANS_SHADOW,
                    $"room={roomId} opinion=speechmatics-inline {sourceLang}→[{String.Join(",", merged.Keys)}] inline" &
                    FormatTransBlock(sourceLang, args.Text, merged))
                FireShadowTranslations(roomId, args.Text, sourceLang, merged.Keys.ToList())
            End If

            If merged.Count > 0 Then
                ' Apply glossary + profanity locally (self-contained, file-based) so the
                ' fixes land regardless of whether the NLLB sidecar is running. The old
                ' /glossary/apply path silently no-op'd when a cloud translation backend
                ' was selected (sidecar skipped), so Speechmatics-inline lines bypassed
                ' the glossary entirely.
                Try
                    Dim roomFp = RoomTranslationFilters(roomId)
                    Dim globalGloss = AppConfig.ResolvePath(
                        If(_config.TranslationGlossaryPath, ".\translate-server\glossary.json"))
                    Dim globalProf = AppConfig.ResolvePath(".\translate-server\profanity.json")
                    merged = GlossaryPostProcessor.Apply(
                        args.Text, sourceLang, merged, If(roomFp?.GlossaryPath, ""), globalGloss)
                    merged = ProfanityPostProcessor.Apply(
                        merged, If(roomFp?.ProfanityPath, ""), globalProf)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR, $"room={roomId} glossary/profanity post-process failed: {ex.Message}")
                End Try
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
            Dim svc = _getSubtitleSvc()
            If svc Is Nothing Then Return
            For Each kvp In _sttBackends.ToList()
                ' Only retarget rooms whose translation engine is an INLINE engine —
                ' other rooms translate via the orchestrator and need no session
                ' restart on language change.
                If Not TranslationBackendRegistry.IsInlineEngine(RoomTranslationKey(kvp.Key)) Then Continue For
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

            ' Resolve the STT template that is effective RIGHT NOW (the old speaker, or the
            ' conference template default if none) so the log can say whether this switch
            ' actually changes the engine config or just the speaker name.
            Dim store = TemplateLibraryStore.Instance
            Dim prevId As String
            If Not String.IsNullOrEmpty(room.ActiveSpeakerId) Then
                prevId = ConnectivityGate.SelectSpeakerSttTemplateId(store.GetSpeakerProfile(room.ActiveSpeakerId), mode)
            Else
                Dim cTplId As String = Nothing
                _roomTemplateIds.TryGetValue(roomId, cTplId)
                Dim cTpl = If(Not String.IsNullOrEmpty(cTplId),
                    _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = cTplId), Nothing)
                prevId = If(cTpl?.SttTemplateId, "")
            End If
            Dim prevName = If(String.IsNullOrEmpty(prevId), "",
                If(store.GetEngineTemplate(TemplateLibraryStore.GroupStt, prevId)?.Name, prevId))
            Dim sameCfg = String.Equals(If(prevId, ""), slotId, StringComparison.OrdinalIgnoreCase)

            room.ActiveSpeakerId = speakerId
            Dim changeNote As String
            If sameCfg Then
                changeNote = " — STT config UNCHANGED (same template)"
            ElseIf Not String.IsNullOrEmpty(prevName) Then
                changeNote = $" (was '{prevName}')"
            Else
                changeNote = ""
            End If
            AppLogger.Log(LogEvents.CONF_SPEAKER_SWITCHED,
                $"room={roomId} speaker → '{sp.Name}' ({speakerId}), stt template '{slotTpl.Name}' [{slotTpl.EngineKey}], mode={mode}{changeNote}")
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
                    ' Connection-lifecycle events (SSE stream ended, live-server exited,
                    ' socket forcibly closed/reset) are benign at end-of-session and now
                    ' auto-recover mid-service — log them Info, not a red ERROR.
                    If IsBenignDisconnect(line) Then
                        AppLogger.Log(LogEvents.CONF_BACKEND_DISCONNECT, $"room={roomId} engine={engineKey} {line}")
                    Else
                        AppLogger.Log(LogEvents.CONF_BACKEND_ERROR, $"room={roomId} engine={engineKey} {line}")
                    End If
                End Sub
        End Sub

        ''' <summary>True if a backend error line is a benign connection-lifecycle event (disconnect / process exit / socket close) rather than a genuine engine error.</summary>
        Private Shared Function IsBenignDisconnect(line As String) As Boolean
            If String.IsNullOrEmpty(line) Then Return False
            Dim l = line.ToLowerInvariant()
            Return l.Contains("sse connection lost") OrElse
                   l.Contains("exited unexpectedly") OrElse
                   l.Contains("forcibly closed") OrElse
                   l.Contains("connection reset") OrElse
                   l.Contains("connection was aborted") OrElse
                   l.Contains("connection closed") OrElse
                   l.Contains("operation was canceled") OrElse
                   l.Contains("operation was cancelled")
        End Function

        ''' <summary>
        ''' Resolve the room's TRANSLATION engine key (template's TranslationBackendKey,
        ''' else the global config default), store it, and ensure the backend is ready:
        ''' for a local/NLLB engine (registry ModelType non-empty) start the sidecar with
        ''' that key's model on demand; cloud engines are a no-op (configured at server
        ''' start). The global Options translation engine remains only the default.
        ''' </summary>
        Private Sub ResolveRoomTranslationEngine(roomId As String, template As ConferenceTemplate)
            Dim roomKey = If(String.IsNullOrWhiteSpace(template?.TranslationBackendKey),
                             If(_config.TranslationBackend, "nllb"),
                             template.TranslationBackendKey)
            _roomTranslationKey(roomId) = roomKey

            ' Resolve + ensure the room's translation backend. For offline engines this
            ' spins up (or shares) a per-model sidecar so different rooms can run different
            ' offline models concurrently — same model+precision shares one sidecar.
            Dim backendName = If(_acquireTranslationBackend IsNot Nothing,
                                 _acquireTranslationBackend(roomId, roomKey),
                                 TranslationBackendRegistry.BackendNameForKey(roomKey))
            _roomTranslationBackendName(roomId) = If(backendName, "")
            AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                $"[Conference:{roomId}] translation engine '{roomKey}' (backend {If(String.IsNullOrEmpty(backendName), "default", backendName)})")
            WarmShadowEngines(roomId)
        End Sub

        ''' <summary>
        ''' Warm any OFFLINE shadow-opinion engines at room start so their opinions
        ''' actually appear in the log — shadows never start engines themselves, and
        ''' an unavailable sidecar would be skipped silently ("can't trust myself to
        ''' remember to start it"). Acquired under the room's ID (idempotent,
        ''' refcounted) so the normal room-close release frees them too. Cloud and
        ''' inline engines need no warm-up. Runs in the background — never delays
        ''' room start.
        ''' </summary>
        ''' <summary>Drop all warmed-shadow backend names for a room (keys are "roomId|engineKey").</summary>
        Private Sub DropShadowBackendNames(roomId As String)
            Dim prefix = roomId & "|"
            For Each k In _shadowBackendNames.Keys.Where(Function(x) x.StartsWith(prefix, StringComparison.Ordinal)).ToList()
                DropKey(_shadowBackendNames, k)
            Next
        End Sub

        Private Sub WarmShadowEngines(roomId As String)
            If Not _config.ShadowTranslationsEnabled Then Return
            Dim raw = _config.ShadowTranslationEngines
            If String.IsNullOrWhiteSpace(raw) OrElse _acquireTranslationBackend Is Nothing Then Return
            Dim primary = RoomTranslationKey(roomId)
            Dim keys = raw.Split(","c).Select(Function(s) s.Trim().ToLowerInvariant()).
                           Where(Function(s) s.Length > 0 AndAlso Not s.Equals(primary, StringComparison.OrdinalIgnoreCase)).
                           Distinct().ToList()
            If keys.Count = 0 Then Return
            Task.Run(Sub()
                         For Each key In keys
                             Try
                                 Dim entry = TranslationBackendRegistry.Find(key)
                                 If entry Is Nothing OrElse String.IsNullOrEmpty(entry.ModelType) Then Continue For   ' cloud/inline: nothing to warm
                                 Dim name = _acquireTranslationBackend(roomId, key)
                                 ' Remember the RESOLVED backend (pool name or "Local") so the
                                 ' shadow requests route directly to the warmed sidecar.
                                 If Not String.IsNullOrEmpty(name) Then _shadowBackendNames($"{roomId}|{key}") = name
                                 AppLogger.Log(LogCategory.Translation, LogSeverity.Info,
                                     $"[Conference:{roomId}] shadow engine '{key}' warmed for 2nd-opinion logging (backend {If(name, "?")})")
                             Catch ex As Exception
                                 AppLogger.Log(LogCategory.Translation, LogSeverity.Warning,
                                     $"[Conference:{roomId}] shadow engine '{key}' warm-up failed: {ex.Message}")
                             End Try
                         Next
                     End Sub)
        End Sub

        ''' <summary>
        ''' Decide whether a room should use INLINE (STT-engine-native) translation:
        ''' true only when the room's chosen translation engine is an inline engine
        ''' AND the room's STT engine matches that inline engine's STT requirement.
        ''' When the room picked an inline translation engine but its STT engine isn't
        ''' the required one, log a fallback warning and return False — the room then
        ''' uses the global default translation engine for everything.
        ''' </summary>
        Private Function ResolveInlineEnabled(roomId As String, sttBackendKey As String) As Boolean
            Dim roomKey = RoomTranslationKey(roomId)
            Dim transEntry = TranslationBackendRegistry.Find(roomKey)
            If transEntry Is Nothing OrElse String.IsNullOrEmpty(transEntry.InlineWithStt) Then Return False
            Dim inlineEnabled = transEntry.InlineWithStt.Equals(If(sttBackendKey, ""), StringComparison.OrdinalIgnoreCase)
            If Not inlineEnabled Then
                AppLogger.Log(LogEvents.TRANS_BACKEND_FALLBACK,
                    $"[Conference:{roomId}] translation engine '{roomKey}' requires {transEntry.InlineWithStt} STT (room STT is '{sttBackendKey}') — falling back to the global default translation engine")
            End If
            Return inlineEnabled
        End Function

        ''' <summary>The TRANSLATION engine key for a room (template override, else global config default).</summary>
        Private Function RoomTranslationKey(roomId As String) As String
            Dim key As String = Nothing
            If _roomTranslationKey.TryGetValue(roomId, key) AndAlso Not String.IsNullOrEmpty(key) Then Return key
            Return If(_config.TranslationBackend, "nllb")
        End Function

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

        Private Shared Function Truncate(s As String, n As Integer) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Return If(s.Length > n, s.Substring(0, n) & "…", s)
        End Function

        ''' <summary>
        ''' Full (untruncated) source + per-language output as an indented multi-line block,
        ''' appended to the TRANS_RESULT metadata line. One line per language keeps it readable
        ''' whether a room serves 1 or 20 targets (a single physical line would be an unreadable
        ''' wall at 5+ languages). Emitted as ONE log event so the rate limiter counts it once.
        ''' </summary>
        Public Shared Function FormatTransBlock(sourceLang As String, sourceText As String,
                                                translations As Dictionary(Of String, String)) As String
            Dim sb As New Text.StringBuilder()
            sb.Append(Environment.NewLine).Append($"    {Pad(sourceLang)} (src): ""{sourceText}""")
            If translations IsNot Nothing Then
                For Each kvp In translations
                    sb.Append(Environment.NewLine).Append($"    {Pad(kvp.Key)}      : ""{kvp.Value}""")
                Next
            End If
            Return sb.ToString()
        End Function

        Private Shared Function Pad(code As String) As String
            Return If(code, "").PadRight(8)
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
