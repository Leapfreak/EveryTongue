Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Server
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Stt

Namespace Controllers
    ''' <summary>
    ''' Manages the Live workspace — session start/stop, audio devices,
    ''' transcript accumulation, tune stats, remote commands, and
    ''' translate-and-broadcast pipeline.
    ''' Extracted from FormMain.
    ''' </summary>
    Friend Class LiveController

        ' UI controls
        Private ReadOnly _cboDevice As ComboBox
        Private ReadOnly _cboInputLang As ComboBox
        Private ReadOnly _btnStart As Button
        Private ReadOnly _btnStop As Button
        Private ReadOnly _btnRefreshDevices As Button
        Private ReadOnly _btnEditFilters As Button
        Private ReadOnly _btnTuneStats As Button
        Private ReadOnly _grpInput As GroupBox
        Private ReadOnly _trkMaxSegment As TrackBar
        Private ReadOnly _trkVadSilence As TrackBar
        Private ReadOnly _lblMaxSegmentValue As Label
        Private ReadOnly _lblVadSilenceValue As Label

        ' Config & callbacks
        Private ReadOnly _config As AppConfig
        Private ReadOnly _saveUiToConfig As Action
        Private ReadOnly _langDisplayName As Func(Of String, String)
        Private ReadOnly _langCodeFromDisplay As Func(Of String, String)
        Private ReadOnly _whisperLanguages As String()
        Private ReadOnly _getSubtitleSvc As Func(Of Services.Interfaces.ISubtitleService)
        Private ReadOnly _getTranslationService As Func(Of TranslationService)
        Private ReadOnly _startTranslationService As Action
        Private ReadOnly _debugLog As Action(Of String)
        Private ReadOnly _showLogPanel As Action
        Private ReadOnly _updateShellStatus As Action
        Private ReadOnly _getString As Func(Of String, String)
        Private ReadOnly _formIcon As Drawing.Icon
        Private ReadOnly _ownerForm As Form
        Private ReadOnly _syncInputLangToJob As Action(Of String)

        ' State
        Private _sttBackend As ISttBackend
        Private ReadOnly _liveTranscript As New System.Text.StringBuilder()
        Private ReadOnly _pendingCommits As New List(Of SttOutputEventArgs)()
        Private _isRemoteCommand As Boolean = False

        ''' <summary>
        ''' Conference room backends keyed by room ID.
        ''' Each gets its own ISttBackend instance on a unique port.
        ''' </summary>
        Private ReadOnly _sttBackends As New Dictionary(Of String, ISttBackend)()
        Private ReadOnly _roomTemplateIds As New Dictionary(Of String, String)()
        Private _nextConferencePort As Integer = 5101

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning
            End Get
        End Property

        ''' <summary>
        ''' When set, desktop Live STT broadcasts go to this Conference room instead of non-room clients.
        ''' Empty = broadcast to all non-room clients (default).
        ''' </summary>
        Public Property TargetRoomId As String
            Get
                Return If(_getSubtitleSvc()?.TargetRoomId, "")
            End Get
            Set(value As String)
                Dim svc = _getSubtitleSvc()
                If svc IsNot Nothing Then svc.TargetRoomId = If(value, "")
            End Set
        End Property

        Public ReadOnly Property Backend As ISttBackend
            Get
                Return _sttBackend
            End Get
        End Property

        Public Sub New(config As AppConfig,
                       cboDevice As ComboBox, cboInputLang As ComboBox,
                       btnStart As Button, btnStop As Button,
                       btnRefreshDevices As Button, btnEditFilters As Button,
                       btnTuneStats As Button,
                       grpInput As GroupBox,
                       trkMaxSegment As TrackBar, trkVadSilence As TrackBar,
                       lblMaxSegmentValue As Label, lblVadSilenceValue As Label,
                       whisperLanguages As String(),
                       langDisplayName As Func(Of String, String),
                       langCodeFromDisplay As Func(Of String, String),
                       saveUiToConfig As Action,
                       getSubtitleSvc As Func(Of Services.Interfaces.ISubtitleService),
                       getTranslationService As Func(Of TranslationService),
                       startTranslationService As Action,
                       debugLog As Action(Of String),
                       showLogPanel As Action,
                       updateShellStatus As Action,
                       getString As Func(Of String, String),
                       formIcon As Drawing.Icon,
                       ownerForm As Form,
                       syncInputLangToJob As Action(Of String))
            _config = config
            _cboDevice = cboDevice
            _cboInputLang = cboInputLang
            _btnStart = btnStart
            _btnStop = btnStop
            _btnRefreshDevices = btnRefreshDevices
            _btnEditFilters = btnEditFilters
            _btnTuneStats = btnTuneStats
            _grpInput = grpInput
            _trkMaxSegment = trkMaxSegment
            _trkVadSilence = trkVadSilence
            _lblMaxSegmentValue = lblMaxSegmentValue
            _lblVadSilenceValue = lblVadSilenceValue
            _whisperLanguages = whisperLanguages
            _langDisplayName = langDisplayName
            _langCodeFromDisplay = langCodeFromDisplay
            _saveUiToConfig = saveUiToConfig
            _getSubtitleSvc = getSubtitleSvc
            _getTranslationService = getTranslationService
            _startTranslationService = startTranslationService
            _debugLog = debugLog
            _showLogPanel = showLogPanel
            _updateShellStatus = updateShellStatus
            _getString = getString
            _formIcon = formIcon
            _ownerForm = ownerForm
            _syncInputLangToJob = syncInputLangToJob
        End Sub

        Public Sub WireEvents()
            AddHandler _cboDevice.SelectedIndexChanged, Sub(s, e) OnDeviceChanged()
            AddHandler _cboInputLang.SelectedIndexChanged, Sub(s, e) OnInputLangChanged()
            AddHandler _cboInputLang.Leave, Sub(s, e) OnInputLangLeave()
            AddHandler _btnRefreshDevices.Click, Sub(s, e) RefreshDevices()
            AddHandler _btnEditFilters.Click, Sub(s, e) EditFilters()
            AddHandler _btnStart.Click, Sub(s, e) StartLiveAsync()
            AddHandler _btnStop.Click, Sub(s, e) StopLive()
            AddHandler _trkMaxSegment.Scroll, Sub(s, e) OnMaxSegmentScroll()
            AddHandler _trkVadSilence.Scroll, Sub(s, e) OnVadSilenceScroll()
            AddHandler _btnTuneStats.Click, Sub(s, e) ShowTuneStats()
        End Sub

        Public Sub PopulateLanguageDropdown()
            _cboInputLang.Items.Clear()
            For Each lang In _whisperLanguages
                _cboInputLang.Items.Add(_langDisplayName(lang))
            Next
            _cboInputLang.AutoCompleteMode = AutoCompleteMode.SuggestAppend
            _cboInputLang.AutoCompleteSource = AutoCompleteSource.ListItems
            SelectInputLang(_config.Language)
        End Sub

        Public Sub SelectInputLang(code As String)
            Dim display = _langDisplayName(code)
            For i = 0 To _cboInputLang.Items.Count - 1
                If _cboInputLang.Items(i).ToString().Equals(display, StringComparison.OrdinalIgnoreCase) Then
                    _cboInputLang.SelectedIndex = i
                    Return
                End If
            Next
            For i = 0 To _cboInputLang.Items.Count - 1
                If _cboInputLang.Items(i).ToString().Contains($"({code})") Then
                    _cboInputLang.SelectedIndex = i
                    Return
                End If
            Next
            If _cboInputLang.Items.Count > 0 Then _cboInputLang.SelectedIndex = 0
        End Sub

        Private Sub OnDeviceChanged()
            If _cboDevice.SelectedItem IsNot Nothing Then
                Dim txt = _cboDevice.SelectedItem.ToString()
                Dim colonIdx = txt.IndexOf(":"c)
                If colonIdx > 0 Then
                    _config.LastLiveDeviceId = txt.Substring(0, colonIdx).Trim()
                    ConfigManager.Save(_config)
                End If
            End If
        End Sub

        Private Sub OnInputLangChanged()
            If _cboInputLang.SelectedItem Is Nothing Then Return
            _config.Language = _langCodeFromDisplay(_cboInputLang.SelectedItem.ToString())
            _syncInputLangToJob(_config.Language)
            ConfigManager.Save(_config)
        End Sub

        Private Sub OnInputLangLeave()
            If _cboInputLang.SelectedIndex < 0 Then
                SelectInputLang(_config.Language)
            End If
        End Sub

        Private Sub RefreshDevices()
            _saveUiToConfig()
            _cboDevice.Items.Clear()
            _cboDevice.Items.Add(_getString("Live_DetectingDevices"))
            _cboDevice.SelectedIndex = 0
            _cboDevice.Enabled = False
            _btnRefreshDevices.Enabled = False

            Dim pythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")

            Task.Run(Sub()
                         Try
                             Dim backend As New FasterWhisperBackend()
                             Dim deviceInfos = backend.EnumerateDevicesAsync(pythonPath)
                             Dim devices As New List(Of String)
                             For Each d In deviceInfos
                                 devices.Add(d.ToString())
                             Next
                             _cboDevice.BeginInvoke(Sub()
                                                         UpdateDeviceCombo(devices)
                                                         _cboDevice.Enabled = True
                                                         _btnRefreshDevices.Enabled = True
                                                     End Sub)
                         Catch ex As Exception
                             _debugLog($"[ERROR] Refresh devices failed: {ex.Message}")
                             _cboDevice.BeginInvoke(Sub()
                                                         _cboDevice.Items.Clear()
                                                         _cboDevice.Items.Add(_getString("Live_DefaultDevice"))
                                                         _cboDevice.SelectedIndex = 0
                                                         _cboDevice.Enabled = True
                                                         _btnRefreshDevices.Enabled = True
                                                     End Sub)
                         End Try
                     End Sub)
        End Sub

        Private Sub EditFilters()
            Using frm As New FormFilterEditor(AppDomain.CurrentDomain.BaseDirectory, _config.LiveServerPort, _config.TranslationPort)
                frm.Icon = _formIcon
                frm.ShowDialog(_ownerForm)
            End Using
        End Sub

        Public Async Sub StartLiveAsync()
            _saveUiToConfig()

            Dim toolsDir = AppDomain.CurrentDomain.BaseDirectory
            Dim mgr As New DependencyManager(_config, toolsDir)
            Dim liveDeps = Await mgr.CheckLiveDepsAsync()

            If Not liveDeps.pythonOk OrElse Not liveDeps.depsOk OrElse Not liveDeps.modelOk Then
                If _isRemoteCommand Then
                    _debugLog("ERROR: Live transcription dependencies not installed.")
                    Return
                End If
                Services.Infrastructure.AppLogger.PromptDownloadManager(
                    _getString("Msg_LiveDepsMissing") & vbCrLf & vbCrLf & _getString("Msg_OpenDownloadManager"),
                    _getString("Msg_DepsMissing"))
                Return
            End If

            Dim deviceId = 0
            If _cboDevice.SelectedItem IsNot Nothing Then
                Dim deviceText = _cboDevice.SelectedItem.ToString()
                Dim colonIdx = deviceText.IndexOf(":"c)
                If colonIdx > 0 Then Integer.TryParse(deviceText.Substring(0, colonIdx).Trim(), deviceId)
            End If

            Dim inputLang = "auto"
            If _cboInputLang.SelectedItem IsNot Nothing Then inputLang = _langCodeFromDisplay(_cboInputLang.SelectedItem.ToString())
            Dim svc1 = _getSubtitleSvc()
            If svc1 IsNot Nothing Then svc1.InputLanguage = inputLang

            _sttBackend = New FasterWhisperBackend()

            AddHandler _sttBackend.OutputUpdated, Sub(s, e)
                                                      _debugLog($"[Live] >>> UPDATE: {e.Text}")
                                                  End Sub

            AddHandler _sttBackend.OutputCommitted, Sub(s, e)
                                                        _liveTranscript.AppendLine(e.Text)
                                                        _debugLog($"[Live] >>> COMMIT: [{e.DetectedLanguage}] {e.Text}")
                                                        TranslateAndBroadcastAsync(e)
                                                    End Sub

            AddHandler _sttBackend.ErrorReceived, Sub(s, line)
                                                      If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                         Not line.StartsWith(">>> COMMIT") AndAlso
                                                         Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                         Not line.Contains("ASGI callable returned without completing response") Then
                                                          _debugLog($"[Live] {line}")
                                                      End If
                                                  End Sub

            _debugLog($"Live transcription starting (device {deviceId}, lang={inputLang})...")
            _debugLog($"[Live] {_sttBackend.Name} + Silero VAD (port {_config.LiveServerPort})")

            Dim sttConfig As New SttConfig() With {
                .DeviceIndex = deviceId,
                .Language = inputLang,
                .ModelPath = _config.PathFasterWhisperModel,
                .ComputeType = _config.LiveComputeType,
                .UseGpu = Not _config.NoGpu,
                .BeamSize = _config.BeamSize,
                .VadSilenceMs = _config.LiveVadSilenceMs,
                .MaxSegmentSec = _config.LiveMaxSegmentSec,
                .InterimIntervalMs = _config.LiveInterimIntervalMs,
                .InitialPrompt = _config.InitialPrompt,
                .TranslateToEnglish = False,
                .ServerPort = _config.LiveServerPort
            }
            _sttBackend.Start(sttConfig)

            If _sttBackend.IsRunning Then
                _getSubtitleSvc()?.BroadcastSystemMessage("[Transcription Started]")
            End If

            If _sttBackend.IsRunning Then
                _btnStart.Enabled = False
                _btnStop.Enabled = True
                _btnTuneStats.Enabled = True
                _grpInput.Enabled = False
                UpdateLiveRunningStatus()
                _showLogPanel()

                ' Check if clients are already requesting translation (e.g. WebView2 preview)
                ' and auto-start NLLB if needed
                Dim targets = _getSubtitleSvc()?.GetActiveTranslationLanguages()
                If targets IsNot Nothing AndAlso targets.Count > 0 Then
                    HandleActiveLanguagesChanged(Nothing, EventArgs.Empty)
                End If
            End If
        End Sub

        Public Sub StopLive()
            If _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning Then
                _getSubtitleSvc()?.BroadcastSystemMessage("[Transcription Stopped]")
                _sttBackend.Stop()
                _debugLog("Live transcription stopped.")
            End If

            _btnStart.Enabled = True
            _btnStop.Enabled = False
            _grpInput.Enabled = True
            UpdateLiveRunningStatus()
        End Sub

        Private Sub OnMaxSegmentScroll()
            _lblMaxSegmentValue.Text = $"{_trkMaxSegment.Value}s"
            _saveUiToConfig()
            PushLiveConfig()
        End Sub

        Private Sub OnVadSilenceScroll()
            _lblVadSilenceValue.Text = $"{_trkVadSilence.Value}ms"
            _saveUiToConfig()
            PushLiveConfig()
        End Sub

        Private Async Sub PushLiveConfig()
            If _sttBackend Is Nothing OrElse Not _sttBackend.IsRunning Then Return
            Dim cfg As New Dictionary(Of String, Object) From {
                {"vad_max_segment_s", _trkMaxSegment.Value},
                {"vad_min_silence_ms", _trkVadSilence.Value}
            }
            Await _sttBackend.UpdateConfigAsync(cfg)
        End Sub

        Public Sub HandleRemoteCommand(command As String)
            Dim isLiveActive = _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning
            _isRemoteCommand = True
            Try
                Select Case command
                    Case "start"
                        If Not isLiveActive Then
                            _debugLog("Remote command: START")
                            StartLiveAsync()
                        End If
                    Case "stop"
                        If isLiveActive Then
                            _debugLog("Remote command: STOP")
                            StopLive()
                        End If
                    Case "restart"
                        _debugLog("Remote command: RESTART")
                        If isLiveActive Then StopLive()
                        StartLiveAsync()
                    Case "clear"
                        _debugLog("Remote command: CLEAR")
                        _getSubtitleSvc()?.BroadcastClear()
                    Case Else
                        If command.StartsWith("setSliders:") Then
                            Dim parts = command.Substring(11).Split(","c)
                            If parts.Length = 2 Then
                                Dim maxSeg As Integer
                                Dim vadSilence As Integer
                                If Integer.TryParse(parts(0), maxSeg) AndAlso Integer.TryParse(parts(1), vadSilence) Then
                                    _debugLog($"Remote command: SET SLIDERS maxSeg={maxSeg}s vadSilence={vadSilence}ms")
                                    _trkMaxSegment.Value = Math.Max(_trkMaxSegment.Minimum, Math.Min(_trkMaxSegment.Maximum, maxSeg))
                                    _lblMaxSegmentValue.Text = $"{_trkMaxSegment.Value}s"
                                    _trkVadSilence.Value = Math.Max(_trkVadSilence.Minimum, Math.Min(_trkVadSilence.Maximum, vadSilence))
                                    _lblVadSilenceValue.Text = $"{_trkVadSilence.Value}ms"
                                    _saveUiToConfig()
                                    PushLiveConfig()
                                End If
                            End If
                        End If
                End Select
            Finally
                _isRemoteCommand = False
            End Try
            UpdateLiveRunningStatus()
        End Sub

        Private Sub UpdateLiveRunningStatus()
            Dim svc = _getSubtitleSvc()
            If svc IsNot Nothing Then
                svc.IsLiveRunning = _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning
            End If
            _updateShellStatus()
        End Sub

        Private Async Sub ShowTuneStats()
            If _sttBackend Is Nothing Then
                MessageBox.Show(_getString("Live_NoSessionData"), _getString("Live_Tune"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim json = Await _sttBackend.GetStatsAsync()
            If String.IsNullOrEmpty(json) Then
                MessageBox.Show(_getString("Live_NoStatsYet"), _getString("Live_Tune"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Try
                Dim result = ParseTuneStats(json, _trkMaxSegment.Value, _trkVadSilence.Value, _getString)
                If result Is Nothing Then
                    MessageBox.Show(_getString("Live_NoCommitsYet"), _getString("Live_Tune"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If

                Dim tips As New List(Of String)
                For Each tip In result.Tips
                    tips.Add($"• {tip}")
                Next
                tips.Add("")
                tips.Add(String.Format(_getString("Live_SessionStats"), result.Commits, result.Hallucinations))
                tips.Add(String.Format(_getString("Live_CommitTypes"), result.TypeInfo))
                If result.WpsAvg > 0 Then tips.Add(String.Format(_getString("Live_SpeakingRate"), result.WpsAvg.ToString("F1")))
                tips.Add(String.Format(_getString("Live_SegmentDuration"), result.DurAvg.ToString("F1"), result.DurMedian.ToString("F1"), result.DurMax.ToString("F1")))
                If result.HasGaps Then tips.Add(String.Format(_getString("Live_SilenceGaps"), result.GapAvg.ToString("F1"), result.GapMedian.ToString("F1")))

                Dim msg = String.Join(Environment.NewLine, tips)
                Dim dlgResult = MessageBox.Show(
                    msg & Environment.NewLine & Environment.NewLine &
                    _getString("Live_ApplySuggested"),
                    _getString("Live_TuningRecommendations"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information)

                If dlgResult = DialogResult.Yes Then
                    If result.SuggestedMaxSeg <> _trkMaxSegment.Value Then
                        _trkMaxSegment.Value = Math.Max(_trkMaxSegment.Minimum, Math.Min(_trkMaxSegment.Maximum, result.SuggestedMaxSeg))
                        _lblMaxSegmentValue.Text = $"{_trkMaxSegment.Value}s"
                    End If
                    If result.SuggestedVadSilence <> _trkVadSilence.Value Then
                        _trkVadSilence.Value = Math.Max(_trkVadSilence.Minimum, Math.Min(_trkVadSilence.Maximum, result.SuggestedVadSilence))
                        _lblVadSilenceValue.Text = $"{_trkVadSilence.Value}ms"
                    End If
                    _saveUiToConfig()
                    PushLiveConfig()
                End If
            Catch ex As Exception
                MessageBox.Show(String.Format(_getString("Live_TuneParseError"), ex.Message), _getString("Live_Tune"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Public Function GetTuneJson() As String
            If _sttBackend Is Nothing Then Return Nothing

            Dim json As String = Nothing
            Try
                json = _sttBackend.GetStatsAsync().Result
            Catch ex As Exception
                _debugLog($"[ERROR] GetTuneJson stats: {ex.Message}")
            End Try
            If String.IsNullOrEmpty(json) Then Return Nothing

            Try
                Dim result = ParseTuneStats(json, _trkMaxSegment.Value, _trkVadSilence.Value, _getString)
                If result Is Nothing Then Return Nothing

                Dim ic = Globalization.CultureInfo.InvariantCulture
                Dim tipsJson = String.Join(",", result.Tips.Select(Function(t) $"""{t.Replace("""", "\""")}"""))
                Return $"{{""currentMaxSeg"":{result.CurrentMaxSeg},""currentVadSilence"":{result.CurrentVadSilence}," &
                       $"""suggestedMaxSeg"":{result.SuggestedMaxSeg},""suggestedVadSilence"":{result.SuggestedVadSilence}," &
                       $"""commits"":{result.Commits},""hallucinations"":{result.Hallucinations}," &
                       $"""durAvg"":{result.DurAvg.ToString("F1", ic)}," &
                       $"""durMax"":{result.DurMax.ToString("F1", ic)}," &
                       $"""wpsAvg"":{result.WpsAvg.ToString("F1", ic)}," &
                       $"""tips"":[{tipsJson}]}}"
            Catch ex As Exception
                _debugLog($"[ERROR] GetTuneJson parse: {ex.Message}")
                Return Nothing
            End Try
        End Function

        Public Sub HandleInputLanguageChanged(lang As String)
            SelectInputLang(lang)
            Dim svc2 = _getSubtitleSvc()
            If svc2 IsNot Nothing Then svc2.InputLanguage = lang
            If _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning Then
                Dim config As New Dictionary(Of String, Object) From {{"language", lang}}
                Task.Run(Function() _sttBackend.UpdateConfigAsync(config))
            End If
            _debugLog($"[Live] Input language changed to '{lang}' via client")
        End Sub

        Public Sub HandleActiveLanguagesChanged(sender As Object, e As EventArgs)
            Dim targets = _getSubtitleSvc()?.GetActiveTranslationLanguages()
            If targets Is Nothing OrElse targets.Count = 0 Then
                ResetTranslationUnloadTimer()
                Return
            End If

            ' Only auto-start translation if a live session is running —
            ' without one there are no subtitles to translate.
            If _sttBackend Is Nothing OrElse Not _sttBackend.IsRunning Then Return

            _translationUnloadTimer?.Change(Timeout.Infinite, Timeout.Infinite)

            Dim svc = _getTranslationService()
            If svc IsNot Nothing AndAlso svc.IsRunning Then Return

            Dim deps = TranslationService.CheckDependenciesInstalled()
            If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
                _debugLog(_getString("Msg_TransDepsMissing"))
                Services.Infrastructure.AppLogger.PromptDownloadManager(
                    _getString("Msg_TransDepsMissing") & vbCrLf & vbCrLf & _getString("Msg_OpenDownloadManager"),
                    _getString("Msg_DepsMissing"))
                Return
            End If
            _startTranslationService()
        End Sub

        Private _translationUnloadTimer As Timer

        Private Sub ResetTranslationUnloadTimer()
            Dim svc = _getTranslationService()
            If svc Is Nothing OrElse Not svc.IsRunning Then Return

            Dim minutes = _config.TranslationUnloadMinutes
            If minutes <= 0 Then Return

            _translationUnloadTimer?.Dispose()
            _translationUnloadTimer = New Timer(
                Sub(state)
                    Dim tgts = _getSubtitleSvc()?.GetActiveTranslationLanguages()
                    If tgts Is Nothing OrElse tgts.Count = 0 Then
                        _debugLog($"No translation clients for {minutes} min, unloading model...")
                        _getTranslationService()?.UnloadModelAsync().Wait()
                    End If
                End Sub, Nothing, TimeSpan.FromMinutes(minutes), Timeout.InfiniteTimeSpan)
        End Sub

        Public Sub UpdateDeviceCombo(devices As List(Of String))
            Dim previousId = _config.LastLiveDeviceId
            If String.IsNullOrEmpty(previousId) AndAlso _cboDevice.SelectedItem IsNot Nothing Then
                Dim txt = _cboDevice.SelectedItem.ToString()
                Dim colonIdx = txt.IndexOf(":"c)
                If colonIdx > 0 Then previousId = txt.Substring(0, colonIdx).Trim()
            End If

            _cboDevice.Items.Clear()
            For Each d In devices
                _cboDevice.Items.Add(d)
            Next

            Dim found = False
            If previousId IsNot Nothing AndAlso previousId.Length > 0 Then
                For i = 0 To _cboDevice.Items.Count - 1
                    If _cboDevice.Items(i).ToString().StartsWith(previousId & ":") Then
                        _cboDevice.SelectedIndex = i
                        found = True
                        Exit For
                    End If
                Next
            End If
            If Not found AndAlso _cboDevice.Items.Count > 0 Then _cboDevice.SelectedIndex = 0
        End Sub


        ''' <summary>
        ''' Wires live-specific events on the SubtitleService after server start.
        ''' </summary>
        Public Sub ConfigureSubtitleService(svc As Services.Interfaces.ISubtitleService)
            svc.TuneCallback = AddressOf GetTuneJson
            svc.IsLiveRunning = (_sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning)
            svc.InputLanguage = "auto"

            EndpointRegistration.RemoteCommandHandler = Sub(cmd)
                                                            _ownerForm.BeginInvoke(Sub() HandleRemoteCommand(cmd))
                                                        End Sub

            EndpointRegistration.ConferenceRoomCreatedHandler = Sub(roomId, templateId)
                                                                     Try
                                                                         _ownerForm.BeginInvoke(Sub()
                                                                                                    Try
                                                                                                        HandleConferenceRoomCreated(roomId, templateId)
                                                                                                    Catch ex As Exception
                                                                                                        _debugLog($"[Conference] ERROR in HandleConferenceRoomCreated: {ex}")
                                                                                                    End Try
                                                                                                End Sub)
                                                                     Catch ex As Exception
                                                                         _debugLog($"[Conference] ERROR invoking room created handler: {ex.Message}")
                                                                     End Try
                                                                 End Sub

            EndpointRegistration.PipelineConfigHandler = Sub(roomId, params)
                                                              Try
                                                                  _ownerForm.BeginInvoke(Sub()
                                                                                             Try
                                                                                                 HandlePipelineConfigCommand(roomId, params)
                                                                                             Catch ex As Exception
                                                                                                 _debugLog($"[Pipeline] ERROR in HandlePipelineConfigCommand: {ex}")
                                                                                             End Try
                                                                                         End Sub)
                                                              Catch ex As Exception
                                                                  _debugLog($"[Pipeline] ERROR invoking pipeline handler: {ex.Message}")
                                                              End Try
                                                          End Sub

            EndpointRegistration.RoomClosedHandler = Sub(roomId)
                                                          Try
                                                              _ownerForm.BeginInvoke(Sub()
                                                                                         Try
                                                                                             StopConferenceBackend(roomId)
                                                                                         Catch ex As Exception
                                                                                             _debugLog($"[Conference] ERROR in StopConferenceBackend: {ex}")
                                                                                         End Try
                                                                                     End Sub)
                                                          Catch ex As Exception
                                                              _debugLog($"[Conference] ERROR invoking room closed handler: {ex.Message}")
                                                          End Try
                                                      End Sub

            AddHandler svc.RemoteCommand, Sub(s, cmd)
                                              _ownerForm.BeginInvoke(Sub() HandleRemoteCommand(cmd))
                                          End Sub
            AddHandler svc.ActiveLanguagesChanged, AddressOf HandleActiveLanguagesChanged
            AddHandler svc.InputLanguageChanged, Sub(s, lang)
                                                     _ownerForm.BeginInvoke(Sub() HandleInputLanguageChanged(lang))
                                                 End Sub
        End Sub

        ' ─── Conference Room Pipeline Management ─────────────────────

        ''' <summary>
        ''' Called when a conference room is created from a template via the web API.
        ''' Spins up a new ISttBackend for the room with template-derived config.
        ''' </summary>
        Public Sub HandleConferenceRoomCreated(roomId As String, templateId As String)
            Dim template = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = templateId)
            If template Is Nothing Then
                _debugLog($"[Conference] Template '{templateId}' not found for room {roomId}")
                Return
            End If

            If _sttBackends.ContainsKey(roomId) Then
                _debugLog($"[Conference] Backend already exists for room {roomId}")
                Return
            End If

            ' Build SttConfig from template
            Dim port = _nextConferencePort
            _nextConferencePort += 1

            Dim sttConfig As New SttConfig() With {
                .DeviceIndex = If(template.AudioDeviceId >= 0, template.AudioDeviceId, 0),
                .Language = If(template.SourceLanguage, "auto"),
                .ModelPath = If(Not String.IsNullOrEmpty(template.ModelPath), template.ModelPath, _config.PathFasterWhisperModel),
                .ComputeType = _config.LiveComputeType,
                .UseGpu = Not _config.NoGpu,
                .BeamSize = template.BeamSize,
                .VadSilenceMs = template.VadSilenceMs,
                .MaxSegmentSec = template.MaxSegmentSec,
                .InterimIntervalMs = _config.LiveInterimIntervalMs,
                .InitialPrompt = If(template.InitialPrompt, ""),
                .TranslateToEnglish = False,
                .ServerPort = port
            }

            Dim backend As ISttBackend = New FasterWhisperBackend()

            ' Wire events — route commits to this room's clients
            AddHandler backend.OutputCommitted, Sub(s, e)
                                                     _debugLog($"[Conference:{roomId}] COMMIT: [{e.DetectedLanguage}] {e.Text}")
                                                     TranslateAndBroadcastForRoomAsync(roomId, e)
                                                 End Sub

            AddHandler backend.ErrorReceived, Sub(s, line)
                                                   If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                      Not line.StartsWith(">>> COMMIT") AndAlso
                                                      Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                      Not line.Contains("ASGI callable returned without completing response") Then
                                                       _debugLog($"[Conference:{roomId}] {line}")
                                                   End If
                                               End Sub

            _sttBackends(roomId) = backend
            _roomTemplateIds(roomId) = templateId
            _debugLog($"[Conference] Starting backend for room {roomId} (template={template.Name}, port={port}, lang={sttConfig.Language})")
            backend.Start(sttConfig)

            If backend.IsRunning Then
                _debugLog($"[Conference] Backend started for room {roomId}")
                _getSubtitleSvc()?.BroadcastSystemMessage("[Transcription Started]")
            Else
                _debugLog($"[Conference] Backend FAILED to start for room {roomId}")
                _sttBackends.Remove(roomId)
                _roomTemplateIds.Remove(roomId)
            End If
        End Sub

        ''' <summary>
        ''' Handles pipeline config changes from the web host control panel.
        ''' Runtime-changeable params update in-place; restart-required params restart the backend.
        ''' </summary>
        Public Sub HandlePipelineConfigCommand(roomId As String, params As Dictionary(Of String, Object))
            ' Check if this is the desktop pipeline (empty roomId) or a conference backend
            Dim backend As ISttBackend = Nothing
            If String.IsNullOrEmpty(roomId) Then
                backend = _sttBackend
            Else
                _sttBackends.TryGetValue(roomId, backend)
            End If

            If backend Is Nothing OrElse Not backend.IsRunning Then
                _debugLog($"[Pipeline] No running backend for room '{roomId}'")
                Return
            End If

            ' Separate runtime-changeable vs restart-required params
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

            ' Apply runtime changes first
            If runtimeParams.Count > 0 Then
                _debugLog($"[Pipeline:{roomId}] Updating runtime params: {String.Join(", ", runtimeParams.Keys)}")
                Task.Run(Function() backend.UpdateConfigAsync(runtimeParams))

                ' Update desktop sliders if this is the desktop pipeline
                If String.IsNullOrEmpty(roomId) Then
                    If runtimeParams.ContainsKey("language") Then
                        Dim lang = CStr(runtimeParams("language"))
                        SelectInputLang(lang)
                        Dim svc = _getSubtitleSvc()
                        If svc IsNot Nothing Then svc.InputLanguage = lang
                    End If
                End If
            End If

            ' Restart if needed (beamSize, initialPrompt require server restart)
            If needsRestart AndAlso Not String.IsNullOrEmpty(roomId) Then
                _debugLog($"[Pipeline:{roomId}] Restart required for params: {String.Join(", ", params.Keys)}")
                RestartConferenceBackend(roomId, params)
            End If
        End Sub

        ''' <summary>
        ''' Stops and restarts a conference backend with updated config.
        ''' </summary>
        Private Sub RestartConferenceBackend(roomId As String, configOverrides As Dictionary(Of String, Object))
            Dim backend As ISttBackend = Nothing
            If Not _sttBackends.TryGetValue(roomId, backend) Then Return

            ' Find the template for this room
            Dim tplId As String = Nothing
            _roomTemplateIds.TryGetValue(roomId, tplId)
            Dim template = If(Not String.IsNullOrEmpty(tplId),
                _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = tplId), Nothing)

            ' Capture current port before stopping
            Dim currentPort = 5101 ' fallback
            ' Stop the old backend
            backend.Stop()

            ' Resolve config values from template + overrides
            Dim hasTpl = template IsNot Nothing

            Dim cfgDevice As Integer = 0
            If hasTpl AndAlso template.AudioDeviceId >= 0 Then cfgDevice = template.AudioDeviceId

            Dim cfgLang As String = "auto"
            If configOverrides.ContainsKey("language") Then
                cfgLang = CStr(configOverrides("language"))
            ElseIf hasTpl AndAlso Not String.IsNullOrEmpty(template.SourceLanguage) Then
                cfgLang = template.SourceLanguage
            End If

            Dim cfgModel As String = _config.PathFasterWhisperModel
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
                .ServerPort = currentPort
            }

            ' Create new backend and wire events
            Dim newBackend As ISttBackend = New FasterWhisperBackend()
            AddHandler newBackend.OutputCommitted, Sub(s, e)
                                                        _debugLog($"[Conference:{roomId}] COMMIT: [{e.DetectedLanguage}] {e.Text}")
                                                        TranslateAndBroadcastForRoomAsync(roomId, e)
                                                    End Sub
            AddHandler newBackend.ErrorReceived, Sub(s, line)
                                                      If Not line.StartsWith(">>> UPDATE:") AndAlso
                                                         Not line.StartsWith(">>> COMMIT") AndAlso
                                                         Not line.StartsWith(">>> SENTENCE-COMMIT") AndAlso
                                                         Not line.Contains("ASGI callable returned without completing response") Then
                                                          _debugLog($"[Conference:{roomId}] {line}")
                                                      End If
                                                  End Sub

            _sttBackends(roomId) = newBackend
            _debugLog($"[Pipeline:{roomId}] Restarting backend (port={sttConfig.ServerPort})")
            newBackend.Start(sttConfig)
        End Sub

        ''' <summary>
        ''' Stops and removes a conference backend when a room is closed.
        ''' </summary>
        Public Sub StopConferenceBackend(roomId As String)
            Dim backend As ISttBackend = Nothing
            If _sttBackends.TryGetValue(roomId, backend) Then
                _debugLog($"[Conference] Stopping backend for room {roomId}")
                If backend.IsRunning Then backend.Stop()
                _sttBackends.Remove(roomId)
                _roomTemplateIds.Remove(roomId)
            End If
        End Sub

        ''' <summary>
        ''' Stops all conference backends (e.g. on app shutdown).
        ''' </summary>
        Public Sub StopAllConferenceBackends()
            For Each kvp In _sttBackends.ToList()
                _debugLog($"[Conference] Stopping backend for room {kvp.Key}")
                If kvp.Value.IsRunning Then kvp.Value.Stop()
            Next
            _sttBackends.Clear()
            _roomTemplateIds.Clear()
        End Sub

        ''' <summary>
        ''' Translate-and-broadcast pipeline for a conference room backend.
        ''' Routes output to the specific room's clients via targetRoomId parameter.
        ''' </summary>
        Private Async Sub TranslateAndBroadcastForRoomAsync(roomId As String, commitArgs As SttOutputEventArgs)
            Dim detectedLang = commitArgs.DetectedLanguage
            Dim line = commitArgs.Text

            Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), "eng_Latn")
            Dim sourceShort = TranslationService.NllbToShortCode(sourceLang)

            Dim subtitleSvc = _getSubtitleSvc()
            If subtitleSvc Is Nothing Then Return

            If IsGarbageCommit(line) Then
                _debugLog($"[Conference:{roomId}] Filtered garbage commit")
                subtitleSvc.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang, targetRoomId:=roomId)
                Return
            End If

            ' Get active translation languages for clients in this room
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
                _debugLog($"[Conference:{roomId}] Translate error: {ex.Message}")
            End Try

            translations(sourceLang) = line

            Dim langTags As New Dictionary(Of String, String)
            For Each kvp In translations
                langTags(kvp.Key) = TranslationService.NllbToShortCode(kvp.Key)
            Next

            subtitleSvc.BroadcastCommitTranslated(line, sourceShort, translations, langTags, targetRoomId:=roomId)
        End Sub

        Public Sub PushLiveConfigFromExternal()
            If _sttBackend IsNot Nothing AndAlso _sttBackend.IsRunning Then
                Dim config As New Dictionary(Of String, Object) From {{"language", _config.Language}}
                Task.Run(Function() _sttBackend.UpdateConfigAsync(config))
            End If
        End Sub

        ' ─── Translation & Broadcast Pipeline ─────────────────────────

        Private Async Sub TranslateAndBroadcastAsync(commitArgs As SttOutputEventArgs)
            Dim detectedLang = commitArgs.DetectedLanguage
            Dim line = commitArgs.Text

            _debugLog($"[WHISPER COMMIT] [{detectedLang}] {line}")

            Dim sourceLang = If(Not String.IsNullOrEmpty(detectedLang), WhisperToNllbCode(detectedLang), GetCurrentSourceNllbLang())
            Dim sourceShort = TranslationService.NllbToShortCode(sourceLang)

            Dim targets = _getSubtitleSvc()?.GetActiveTranslationLanguages()
            Dim activeTargets = If(targets IsNot Nothing, String.Join(",", targets), "none")
            targets?.Remove(sourceLang)

            Dim hasTargets = targets IsNot Nothing AndAlso targets.Count > 0
            Dim svc = _getTranslationService()
            Dim serviceRunning = svc IsNot Nothing AndAlso svc.IsRunning
            Dim modelLoaded = serviceRunning AndAlso svc.IsModelLoaded
            Dim translationReady = hasTargets AndAlso modelLoaded

            _debugLog($"[BROADCAST] targets=[{activeTargets}] source={sourceLang} ready={translationReady} modelLoaded={modelLoaded}")

            If hasTargets AndAlso serviceRunning AndAlso Not modelLoaded Then
                SyncLock _pendingCommits
                    _pendingCommits.Add(commitArgs)
                End SyncLock
                _getSubtitleSvc()?.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang)
                _debugLog($"[BUFFERED] commit queued ({_pendingCommits.Count} pending)")
                Return
            End If

            If Not translationReady Then
                _getSubtitleSvc()?.BroadcastCommit(line, skipTranslationClients:=False, lang:=sourceShort)
                Return
            End If

            If IsGarbageCommit(line) Then
                _debugLog($"[FILTERED] garbage commit skipped for translation")
                _getSubtitleSvc()?.BroadcastCommit(line, skipTranslationClients:=True, lang:=sourceShort, sourceLang:=sourceLang)
                Return
            End If

            Dim sw = Stopwatch.StartNew()
            Dim translations As New Dictionary(Of String, String)()
            Try
                Dim result = Await svc.TranslateAsync(line, sourceLang, targets)
                If result IsNot Nothing Then
                    For Each kvp In result
                        translations(kvp.Key) = kvp.Value
                        _debugLog($"[TRANSLATION {kvp.Key}] {kvp.Value}")
                    Next
                End If
            Catch ex As Exception
                _debugLog($"[TRANSLATE ERROR] {ex.Message}")
            End Try

            translations(sourceLang) = line

            Dim currentTargets = _getSubtitleSvc()?.GetActiveTranslationLanguages()
            If currentTargets IsNot Nothing Then
                Dim missing As New List(Of String)
                For Each t In currentTargets
                    If Not translations.ContainsKey(t) Then missing.Add(t)
                Next
                If missing.Count > 0 Then
                    _debugLog($"[CATCH-UP] new targets: {String.Join(",", missing)}")
                    Try
                        Dim extra = Await svc.TranslateAsync(line, sourceLang, missing)
                        If extra IsNot Nothing Then
                            For Each kvp In extra
                                translations(kvp.Key) = kvp.Value
                            Next
                        End If
                    Catch ex As Exception
                        _debugLog($"[CATCH-UP ERROR] {ex.Message}")
                    End Try
                End If
            End If

            sw.Stop()
            _debugLog($"[ROUND TRIP] {sw.ElapsedMilliseconds}ms")

            Dim langTags As New Dictionary(Of String, String)
            For Each kvp In translations
                langTags(kvp.Key) = TranslationService.NllbToShortCode(kvp.Key)
            Next

            _getSubtitleSvc()?.BroadcastCommitTranslated(line, sourceShort, translations, langTags)
        End Sub

        Public Sub FlushPendingCommits()
            Dim svc = _getTranslationService()
            If svc Is Nothing OrElse Not svc.IsModelLoaded Then Return

            Dim commits As List(Of SttOutputEventArgs)
            SyncLock _pendingCommits
                If _pendingCommits.Count = 0 Then Return
                commits = New List(Of SttOutputEventArgs)(_pendingCommits)
                _pendingCommits.Clear()
            End SyncLock

            _debugLog($"[FLUSH] translating {commits.Count} buffered commits")
            For Each c In commits
                TranslateAndBroadcastAsync(c)
            Next
        End Sub

        Private Function GetCurrentSourceNllbLang() As String
            Dim inputLang = ""
            If _cboInputLang.InvokeRequired Then
                inputLang = CStr(_cboInputLang.Invoke(Function() _langCodeFromDisplay(If(_cboInputLang.SelectedItem, "auto").ToString())))
            Else
                If _cboInputLang.SelectedItem IsNot Nothing Then inputLang = _langCodeFromDisplay(_cboInputLang.SelectedItem.ToString())
            End If
            If inputLang = "auto" Then inputLang = "es"
            Return TranslationService.WhisperToNllbLang(inputLang)
        End Function

        Private Shared Function WhisperToNllbCode(whisperLang As String) As String
            Dim result = TranslationService.WhisperToNllbLang(whisperLang.ToLowerInvariant())
            Return If(String.IsNullOrEmpty(result), "eng_Latn", result)
        End Function

        Private Shared Function IsGarbageCommit(line As String) As Boolean
            Dim trimmed = line.Trim().TrimEnd("."c).Trim()
            If String.IsNullOrWhiteSpace(trimmed) Then Return True
            If trimmed.Length <= 3 AndAlso Not trimmed.Any(Function(c) Char.IsDigit(c)) Then Return True
            If trimmed.StartsWith("[") AndAlso trimmed.EndsWith("]") Then Return True
            Return False
        End Function

        ' ─── TuneStats Parsing ────────────────────────────────────────

        Private Class TuneStatsResult
            Public Property CurrentMaxSeg As Integer
            Public Property CurrentVadSilence As Integer
            Public Property SuggestedMaxSeg As Integer
            Public Property SuggestedVadSilence As Integer
            Public Property Commits As Integer
            Public Property Hallucinations As Integer
            Public Property DurAvg As Double
            Public Property DurMedian As Double
            Public Property DurMax As Double
            Public Property WpsAvg As Double
            Public Property GapAvg As Double
            Public Property GapMedian As Double
            Public Property HasGaps As Boolean
            Public Property LangInfo As String = ""
            Public Property TypeInfo As String = ""
            Public Property Tips As New List(Of String)
        End Class

        Private Shared Function ParseTuneStats(json As String, currentMaxSeg As Integer, currentVadSilence As Integer, getString As Func(Of String, String)) As TuneStatsResult
            Using doc = System.Text.Json.JsonDocument.Parse(json)
                Dim root = doc.RootElement

                Dim commitsProp As System.Text.Json.JsonElement = Nothing
                Dim commits As Integer = 0
                If root.TryGetProperty("commits", commitsProp) Then commits = commitsProp.GetInt32()
                If commits = 0 Then Return Nothing

                Dim r As New TuneStatsResult With {
                    .CurrentMaxSeg = currentMaxSeg,
                    .CurrentVadSilence = currentVadSilence,
                    .SuggestedMaxSeg = currentMaxSeg,
                    .SuggestedVadSilence = currentVadSilence,
                    .Commits = commits,
                    .DurAvg = root.GetProperty("duration").GetProperty("avg").GetDouble(),
                    .DurMedian = root.GetProperty("duration").GetProperty("median").GetDouble(),
                    .DurMax = root.GetProperty("duration").GetProperty("max").GetDouble(),
                    .Hallucinations = root.GetProperty("hallucinations").GetInt32()
                }

                Dim forceRatio = root.GetProperty("force_commit_ratio").GetDouble()
                Dim shortRatio = root.GetProperty("short_segment_ratio").GetDouble()

                Dim gapsProp As System.Text.Json.JsonElement = Nothing
                If root.TryGetProperty("silence_gaps", gapsProp) Then
                    r.GapAvg = gapsProp.GetProperty("avg").GetDouble()
                    r.GapMedian = gapsProp.GetProperty("median").GetDouble()
                    r.HasGaps = True
                End If

                Dim wpsProp As System.Text.Json.JsonElement = Nothing
                If root.TryGetProperty("wps", wpsProp) Then
                    r.WpsAvg = wpsProp.GetProperty("avg").GetDouble()
                End If

                Dim langsProp As System.Text.Json.JsonElement = Nothing
                If root.TryGetProperty("languages", langsProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In langsProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    r.LangInfo = String.Join(", ", parts)
                End If

                Dim typesProp As System.Text.Json.JsonElement = Nothing
                If root.TryGetProperty("commit_types", typesProp) Then
                    Dim parts As New List(Of String)
                    For Each prop In typesProp.EnumerateObject()
                        parts.Add($"{prop.Name}={prop.Value.GetInt32()}")
                    Next
                    r.TypeInfo = String.Join(", ", parts)
                End If

                If forceRatio > 0.15 Then
                    Dim suggested = CInt(Math.Min(60, Math.Ceiling(r.DurMax * 1.3 / 5) * 5))
                    r.Tips.Add(String.Format(getString("Live_MaxSegTooLow"), forceRatio.ToString("P0"), suggested, currentMaxSeg))
                    r.SuggestedMaxSeg = suggested
                ElseIf forceRatio = 0 AndAlso r.DurMax < currentMaxSeg * 0.5 Then
                    Dim suggested = CInt(Math.Max(10, Math.Ceiling(r.DurMax * 1.5 / 5) * 5))
                    r.Tips.Add(String.Format(getString("Live_MaxSegTighter"), r.DurMax.ToString("F1"), currentMaxSeg, suggested))
                    r.SuggestedMaxSeg = suggested
                Else
                    r.Tips.Add(String.Format(getString("Live_MaxSegGood"), currentMaxSeg))
                End If

                If shortRatio > 0.4 Then
                    Dim suggested = CInt(Math.Min(1500, Math.Ceiling((currentVadSilence + 200) / 100) * 100))
                    r.Tips.Add(String.Format(getString("Live_VadTooLow"), shortRatio.ToString("P0"), suggested, currentVadSilence))
                    r.SuggestedVadSilence = suggested
                ElseIf r.HasGaps AndAlso r.GapMedian > 3.0 AndAlso shortRatio < 0.1 Then
                    Dim suggested = CInt(Math.Max(200, Math.Floor((currentVadSilence - 100) / 100) * 100))
                    r.Tips.Add(String.Format(getString("Live_VadLower"), r.GapMedian.ToString("F1"), suggested, currentVadSilence))
                    r.SuggestedVadSilence = suggested
                Else
                    r.Tips.Add(String.Format(getString("Live_VadGood"), currentVadSilence))
                End If

                If r.LangInfo.Contains(",") Then
                    r.Tips.Add(String.Format(getString("Live_MultiLangs"), r.LangInfo))
                ElseIf r.LangInfo <> "" Then
                    r.Tips.Add(String.Format(getString("Live_LangConsistent"), r.LangInfo))
                End If

                Return r
            End Using
        End Function

    End Class
End Namespace
