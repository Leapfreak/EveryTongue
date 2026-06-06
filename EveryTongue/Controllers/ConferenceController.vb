Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Server
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
        Private ReadOnly _getRoomManager As Func(Of Services.Rooms.RoomManager)
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _ownerForm As Form

        Private ReadOnly _sttBackends As New Dictionary(Of String, ISttBackend)()
        Private ReadOnly _roomTemplateIds As New Dictionary(Of String, String)()
        Private _nextConferencePort As Integer = 5101
        Private _nextWhisperServerPort As Integer = 8179  ' 8178 is used by ConversationAudioHandler

        Public Sub New(config As AppConfig,
                       getSubtitleSvc As Func(Of ISubtitleService),
                       getTranslationService As Func(Of TranslationService),
                       getRoomManager As Func(Of Services.Rooms.RoomManager),
                       log As Action(Of String),
                       ownerForm As Form)
            _config = config
            _getSubtitleSvc = getSubtitleSvc
            _getTranslationService = getTranslationService
            _getRoomManager = getRoomManager
            _log = log
            _ownerForm = ownerForm
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

            Dim backendKey = If(template.SttBackendKey, _config.SttBackend)
            Dim defaultModel = _config.PathWhisperCppModel

            Dim sttConfig As New SttConfig() With {
                .DeviceIndex = If(template.AudioDeviceId >= 0, template.AudioDeviceId, 0),
                .Language = If(template.SourceLanguage, "auto"),
                .ModelPath = If(Not String.IsNullOrEmpty(template.ModelPath), template.ModelPath, defaultModel),
                .ComputeType = _config.LiveComputeType,
                .UseGpu = Not _config.NoGpu,
                .BeamSize = template.BeamSize,
                .VadSilenceMs = template.VadSilenceMs,
                .MaxSegmentSec = template.MaxSegmentSec,
                .InterimIntervalMs = _config.LiveInterimIntervalMs,
                .InitialPrompt = If(template.InitialPrompt, ""),
                .TranslateToEnglish = False,
                .ServerPort = port,
                .WhisperServerPath = _config.PathWhisperServer,
                .WhisperServerPort = _nextWhisperServerPort,
                .SileroVadModelPath = _config.PathSileroVadModel
            }
            _nextWhisperServerPort += 1

            Dim backend = SttBackendRegistry.CreateBackend(backendKey)

            AddHandler backend.OutputCommitted, Sub(s, e)
                                                     _log($"[Conference:{roomId}] COMMIT: [{e.DetectedLanguage}] {e.Text}")
                                                     TranslateAndBroadcastForRoomAsync(roomId, e)
                                                 End Sub

            AddHandler backend.ErrorReceived, Sub(s, line)
                                                   If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                      Not line.StartsWith(">>> COMMIT") AndAlso
                                                      Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                      Not line.Contains("ASGI callable returned without completing response") Then
                                                       _log($"[Conference:{roomId}] {line}")
                                                   End If
                                               End Sub

            _sttBackends(roomId) = backend
            _roomTemplateIds(roomId) = templateId
            _log($"[Conference] Starting backend for room {roomId} (template={template.Name}, port={port}, lang={sttConfig.Language})")
            backend.Start(sttConfig)

            If backend.IsRunning Then
                _log($"[Conference] Backend started for room {roomId}")
            Else
                _log($"[Conference] Backend FAILED to start for room {roomId}")
                _sttBackends.Remove(roomId)
                _roomTemplateIds.Remove(roomId)
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

            For Each kvp In params
                Select Case kvp.Key
                    Case "language"
                        runtimeParams("language") = kvp.Value
                    Case "maxSegmentSec"
                        runtimeParams("vad_max_segment_s") = kvp.Value
                    Case "vadSilenceMs"
                        runtimeParams("vad_min_silence_ms") = kvp.Value
                    Case "beamSize", "initialPrompt"
                        needsRestart = True
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

            Dim cfgModel As String = _config.PathWhisperCppModel
            If hasTpl AndAlso Not String.IsNullOrEmpty(template.ModelPath) Then cfgModel = template.ModelPath

            Dim cfgBeam As Integer = _config.BeamSize
            If configOverrides.ContainsKey("beamSize") Then
                cfgBeam = CInt(configOverrides("beamSize"))
            ElseIf hasTpl Then
                cfgBeam = template.BeamSize
            End If

            Dim cfgVad As Integer = _config.LiveVadSilenceMs
            If configOverrides.ContainsKey("vadSilenceMs") Then
                cfgVad = CInt(configOverrides("vadSilenceMs"))
            ElseIf hasTpl Then
                cfgVad = template.VadSilenceMs
            End If

            Dim cfgMaxSeg As Integer = _config.LiveMaxSegmentSec
            If configOverrides.ContainsKey("maxSegmentSec") Then
                cfgMaxSeg = CInt(configOverrides("maxSegmentSec"))
            ElseIf hasTpl Then
                cfgMaxSeg = template.MaxSegmentSec
            End If

            Dim cfgPrompt As String = ""
            If configOverrides.ContainsKey("initialPrompt") Then
                cfgPrompt = CStr(configOverrides("initialPrompt"))
            ElseIf hasTpl AndAlso Not String.IsNullOrEmpty(template.InitialPrompt) Then
                cfgPrompt = template.InitialPrompt
            End If

            Dim sttConfig As New SttConfig() With {
                .DeviceIndex = cfgDevice,
                .Language = cfgLang,
                .ModelPath = cfgModel,
                .ComputeType = _config.LiveComputeType,
                .UseGpu = Not _config.NoGpu,
                .BeamSize = cfgBeam,
                .VadSilenceMs = cfgVad,
                .MaxSegmentSec = cfgMaxSeg,
                .InterimIntervalMs = _config.LiveInterimIntervalMs,
                .InitialPrompt = cfgPrompt,
                .TranslateToEnglish = False,
                .ServerPort = _nextConferencePort,
                .WhisperServerPath = _config.PathWhisperServer,
                .WhisperServerPort = _nextWhisperServerPort,
                .SileroVadModelPath = _config.PathSileroVadModel
            }
            _nextConferencePort += 1
            _nextWhisperServerPort += 1

            Dim newBackend = SttBackendRegistry.CreateBackend(If(template?.SttBackendKey, _config.SttBackend))
            AddHandler newBackend.OutputCommitted, Sub(s, e)
                                                        _log($"[Conference:{roomId}] COMMIT: [{e.DetectedLanguage}] {e.Text}")
                                                        TranslateAndBroadcastForRoomAsync(roomId, e)
                                                    End Sub
            AddHandler newBackend.ErrorReceived, Sub(s, line)
                                                      If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                         Not line.StartsWith(">>> COMMIT") AndAlso
                                                         Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                         Not line.Contains("ASGI callable returned without completing response") Then
                                                          _log($"[Conference:{roomId}] {line}")
                                                      End If
                                                  End Sub

            _sttBackends(roomId) = newBackend
            _log($"[Pipeline:{roomId}] Restarting backend (port={sttConfig.ServerPort})")
            newBackend.Start(sttConfig)
        End Sub

        ''' <summary>
        ''' Stops and removes a conference backend when a room is closed.
        ''' </summary>
        Public Sub StopConferenceBackend(roomId As String)
            Dim backend As ISttBackend = Nothing
            If _sttBackends.TryGetValue(roomId, backend) Then
                _log($"[Conference] Stopping backend for room {roomId}")
                If backend.IsRunning Then backend.Stop()
                _sttBackends.Remove(roomId)
                _roomTemplateIds.Remove(roomId)
            End If
        End Sub

        ''' <summary>
        ''' Stops all conference backends (called on app shutdown).
        ''' </summary>
        Public Sub StopAllConferenceBackends()
            For Each kvp In _sttBackends.ToList()
                _log($"[Conference] Stopping backend for room {kvp.Key}")
                If kvp.Value.IsRunning Then kvp.Value.Stop()
            Next
            _sttBackends.Clear()
            _roomTemplateIds.Clear()
        End Sub

        ' ─── Translation Pipeline ─────────────────────────────────────

        Private Async Sub TranslateAndBroadcastForRoomAsync(roomId As String, commitArgs As SttOutputEventArgs)
            ' Check if the room is paused — drop commits silently to keep whisper warm
            Dim mgr = _getRoomManager?.Invoke()
            If mgr IsNot Nothing Then
                Dim room = mgr.GetRoom(roomId)
                If room IsNot Nothing AndAlso room.Config.IsPaused Then
                    _log($"[Conference:{roomId}] Commit dropped (paused): {commitArgs.Text}")
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
                _log($"[Conference:{roomId}] Filtered garbage commit")
                subtitleSvc.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)
                Return
            End If

            Dim targets = subtitleSvc.GetActiveTranslationLanguages()
            targets?.Remove(sourceLang)

            Dim svc = _getTranslationService()
            Dim translationReady = targets IsNot Nothing AndAlso targets.Count > 0 AndAlso
                                   svc IsNot Nothing AndAlso svc.IsRunning AndAlso svc.IsModelLoaded

            If Not translationReady Then
                subtitleSvc.BroadcastCommit(line, skipTranslationClients:=False, lang:=sourceShort, targetRoomId:=roomId)
                Return
            End If

            Dim translations As New Dictionary(Of String, String)()
            Try
                Dim result = Await svc.TranslateAsync(line, sourceLang, targets)
                If result IsNot Nothing Then
                    For Each kvp In result
                        translations(kvp.Key) = kvp.Value
                    Next
                End If
            Catch ex As Exception
                _log($"[Conference:{roomId}] Translate error: {ex.Message}")
            End Try

            translations(sourceLang) = line

            Dim langTags As New Dictionary(Of String, String)
            For Each kvp In translations
                langTags(kvp.Key) = TranslationService.FloresToShortCode(kvp.Key)
            Next

            subtitleSvc.BroadcastCommitTranslated(line, sourceShort, translations, langTags, targetRoomId:=roomId)
        End Sub

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
