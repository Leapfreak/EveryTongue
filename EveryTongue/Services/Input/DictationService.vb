Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Scheduling
Imports EveryTongue.Services.Stt

Namespace Services.Input

    ''' <summary>
    ''' Owns ONE warm STT session (the configured global engine) used for system-wide
    ''' dictation. ISttBackend.Stop() tears down the whole sidecar, so the session is kept
    ''' running for the whole armed period:
    '''  - Continuous: inject every committed utterance.
    '''  - Push-to-talk: session stays warm + capturing; commits are injected only while the
    '''    PTT key is held (commits between presses are discarded) → ~0 press latency.
    ''' When an output language is active, each commit is translated before it reaches the sink.
    ''' </summary>
    Public Class DictationService

        Private ReadOnly _config As AppConfig
        Private ReadOnly _getOrchestrator As Func(Of ITranslationService)
        Private ReadOnly _ensureTranslation As Action
        Private ReadOnly _log As Action(Of Integer, String)

        Private _backend As ISttBackend
        Private _armed As Boolean
        Private _pttHeld As Boolean

        ''' <summary>Receives the final text to insert (already translated if applicable).</summary>
        Public Property TextSink As Action(Of String)

        ''' <summary>
        ''' Raised with True when the armed engine is actually capturing (model loaded), so the
        ''' controller can tell the user when to start talking — the engine process starts before
        ''' the model finishes loading, and words spoken in that window are otherwise lost.
        ''' </summary>
        Public Property ReadinessChanged As Action(Of Boolean)
        Public ReadOnly Property IsArmed As Boolean
            Get
                Return _armed
            End Get
        End Property

        Public Sub New(config As AppConfig,
                       getOrchestrator As Func(Of ITranslationService),
                       ensureTranslation As Action,
                       log As Action(Of Integer, String))
            _config = config
            _getOrchestrator = getOrchestrator
            _ensureTranslation = ensureTranslation
            _log = log
        End Sub

        Public Sub SetPttHeld(held As Boolean)
            _pttHeld = held
        End Sub

        ''' <summary>Start the warm STT session. Returns False if the engine couldn't start.</summary>
        Public Function Arm() As Boolean
            If _armed Then Return True
            Try
                Dim backendKey = _config.SttBackend
                Dim ovr As New Dictionary(Of String, Object) From {{"WhisperServerPort", FreePort()}}
                Dim cfg As New SttSessionConfig With {
                    .EngineKey = backendKey,
                    .DeviceIndex = ResolveDeviceIndex(),
                    .Language = If(String.IsNullOrEmpty(_config.DictationSourceLanguage), "auto", _config.DictationSourceLanguage),
                    .TranslateToEnglish = False,
                    .ServerPort = FreePort(),
                    .ApiKey = _config.GetSttApiKey(backendKey),
                    .EngineConfig = EngineConfigResolver.ResolveStt(backendKey, _config, fieldOverrides:=ovr, contextLabel:="[Dictation]")
                }
                _backend = SttBackendRegistry.CreateBackend(backendKey)
                AddHandler _backend.OutputCommitted, AddressOf OnCommitted
                AddHandler _backend.ErrorReceived, AddressOf OnError
                _backend.Start(cfg)
                If Not _backend.IsRunning Then
                    _log(LogEvents.DICT_SESSION_ERROR, $"Dictation engine '{backendKey}' failed to start")
                    Disarm()
                    Return False
                End If
                _armed = True
                _log(LogEvents.DICT_SESSION_STARTED, $"Dictation armed (engine={backendKey}, port={cfg.ServerPort})")
                ' The process is up but the model may still be loading — poll readiness so the
                ' controller can notify the user when capture is actually live.
                StartReadinessPoll(_backend)
                Return True
            Catch ex As Exception
                _log(LogEvents.DICT_SESSION_ERROR, $"Dictation arm failed: {ex.Message}")
                Disarm()
                Return False
            End Try
        End Function

        ''' <summary>Poll the engine health until it's capturing, then raise ReadinessChanged(True). Fail-open after 60s.</summary>
        Private Sub StartReadinessPoll(backend As ISttBackend)
            Dim cb = ReadinessChanged
            If cb Is Nothing Then Return
            Dim b = backend
            Task.Run(Async Function()
                         Dim startTick = Environment.TickCount64
                         While Environment.TickCount64 - startTick < 60000
                             If Not _armed OrElse Not Object.ReferenceEquals(b, _backend) Then Return
                             Dim ok = False
                             Try : ok = Await b.CheckHealthAsync(CancellationToken.None).ConfigureAwait(False) : Catch : ok = False : End Try
                             If ok Then
                                 Try : cb.Invoke(True) : Catch : End Try
                                 Return
                             End If
                             Await Task.Delay(400).ConfigureAwait(False)
                         End While
                         ' Timed out — assume ready so the user isn't left waiting on a notification.
                         If _armed Then Try : cb.Invoke(True) : Catch : End Try
                     End Function)
        End Sub

        Public Sub Disarm()
            _armed = False
            _pttHeld = False
            Dim b = _backend
            _backend = Nothing
            If b Is Nothing Then Return
            ' Stop() kills the sidecar — off the UI thread to avoid a freeze.
            Task.Run(Sub()
                         Try
                             RemoveHandler b.OutputCommitted, AddressOf OnCommitted
                             RemoveHandler b.ErrorReceived, AddressOf OnError
                             b.Stop()
                         Catch
                         End Try
                     End Sub)
            _log(LogEvents.DICT_SESSION_STOPPED, "Dictation disarmed")
        End Sub

        Private Async Sub OnCommitted(sender As Object, e As SttOutputEventArgs)
            Try
                Dim text = If(e?.Text, "").Trim()
                ' Diagnostic: log every commit + the gating decision so we can see why text
                ' does or doesn't reach the injector.
                _log(LogEvents.DICT_COMMIT, $"commit received: '{text}' armed={_armed} style={_config.DictationStyle} pttHeld={_pttHeld} target='{_config.DictationActiveTargetLanguage}'")
                If Not _armed Then Return
                If _config.DictationStyle = DictationStyle.PushToTalk AndAlso Not _pttHeld Then Return
                If text.Length = 0 Then Return

                Dim target = _config.DictationActiveTargetLanguage
                If Not String.IsNullOrEmpty(target) Then
                    text = Await TranslateAsync(text, If(e.DetectedLanguage, ""), target)
                End If

                Dim sink = TextSink
                If sink IsNot Nothing AndAlso Not String.IsNullOrEmpty(text) Then sink.Invoke(text)
            Catch ex As Exception
                _log(LogEvents.DICT_INJECT_ERROR, $"Dictation commit handling failed: {ex.Message}")
            End Try
        End Sub

        Private Async Function TranslateAsync(rawText As String, detectedWhisper As String, targetFlores As String) As Task(Of String)
            Try
                Dim orch = _getOrchestrator()
                If orch Is Nothing Then Return rawText
                _ensureTranslation()   ' idempotent — starts the NLLB sidecar if a local engine is selected
                Dim src = ResolveSourceFlores(detectedWhisper)
                If String.Equals(src, targetFlores, StringComparison.OrdinalIgnoreCase) Then Return rawText
                Dim res = Await orch.TranslateAsync(rawText, src, {targetFlores}, CancellationToken.None, TranslationPriority.Workspace)
                Dim outText As String = Nothing
                If res IsNot Nothing AndAlso res.TryGetValue(targetFlores, outText) AndAlso Not String.IsNullOrWhiteSpace(outText) Then
                    _log(LogEvents.DICT_TRANSLATE, $"{src}→{targetFlores}: '{rawText}' → '{outText}'")
                    Return outText
                End If
                Return rawText   ' translation empty → inject source rather than nothing
            Catch ex As Exception
                _log(LogEvents.DICT_INJECT_ERROR, $"Dictation translate failed ({ex.Message}) — injecting source")
                Return rawText
            End Try
        End Function

        Private Function ResolveSourceFlores(detectedWhisper As String) As String
            If Not String.IsNullOrEmpty(detectedWhisper) Then Return TranslationService.WhisperToFloresLang(detectedWhisper)
            Dim cfgSrc = _config.DictationSourceLanguage
            If Not String.IsNullOrEmpty(cfgSrc) AndAlso Not cfgSrc.Equals("auto", StringComparison.OrdinalIgnoreCase) Then
                Return TranslationService.WhisperToFloresLang(cfgSrc)
            End If
            Return "eng_Latn"
        End Function

        ' ErrorReceived carries every tailed live-server log line (DEBUG/INFO included), not
        ' just errors — those are already captured via the PythonLog routing, so drop them here
        ' (mirrors ConferenceController.WireBackendLogging) and only surface genuine errors.
        Private Shared ReadOnly _pythonLogLinePattern As New Text.RegularExpressions.Regex(
            "^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}\s")

        Private Sub OnError(sender As Object, msg As String)
            If String.IsNullOrEmpty(msg) Then Return
            If msg.StartsWith(">>>") OrElse
               msg.Contains("ASGI callable returned without completing response") OrElse
               _pythonLogLinePattern.IsMatch(msg) Then Return
            _log(LogEvents.DICT_SESSION_ERROR, $"Dictation engine error: {msg}")
        End Sub

        ''' <summary>Dictation mic: explicit DictationDeviceIndex if set, else the device last used in Live, else default (0).</summary>
        Private Function ResolveDeviceIndex() As Integer
            If _config.DictationDeviceIndex > 0 Then Return _config.DictationDeviceIndex
            Dim idx As Integer
            If Integer.TryParse(If(_config.LastLiveDeviceId, ""), idx) AndAlso idx >= 0 Then Return idx
            Return 0
        End Function

        Private Shared Function FreePort() As Integer
            Dim l As New TcpListener(IPAddress.Loopback, 0)
            l.Start()
            Try
                Return CType(l.LocalEndpoint, IPEndPoint).Port
            Finally
                l.Stop()
            End Try
        End Function

    End Class

End Namespace
