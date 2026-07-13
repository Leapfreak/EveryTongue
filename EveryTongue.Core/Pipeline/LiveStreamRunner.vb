Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline
    Public Class LiveStreamRunner

        Public Event OutputLineUpdated As EventHandler(Of String)   ' interim - in-progress text
        Public Event OutputLineCommitted As EventHandler(Of String) ' final - committed text
        ''' <summary>Final commit that carries inline engine translations (Speechmatics).</summary>
        Public Event OutputLineCommittedTranslated As EventHandler(Of TranslatedCommit)
        Public Event ErrorReceived As EventHandler(Of String)

        ''' <summary>A committed line plus inline translations keyed by engine target code.</summary>
        Public Structure TranslatedCommit
            Public Text As String
            Public Lang As String
            Public Translations As Dictionary(Of String, String)
        End Structure

        Private Shared ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(5)
        }

        Private ReadOnly _host As New PythonSidecarHost() With {
            .Label = "Live server",
            .AddWhisperToPath = True,
            .GracefulShutdownPath = "/shutdown",
            .LogFileName = "live-server.log",
            .BaseEventId = Services.Infrastructure.LogEvents.PYLOG_LIVE
        }

        Private _isCapturing As Boolean = False
        Private _serverReady As Boolean = False
        Private _transcript As New StringBuilder()
        Private _cts As CancellationTokenSource

        Public Sub New()
            AddHandler _host.StderrLine, Sub(s, line)
                                              RaiseEvent ErrorReceived(Me, line)
                                          End Sub
            AddHandler _host.ProcessExited, Sub(s, e)
                                                _serverReady = False
                                                If _isCapturing Then
                                                    RaiseEvent ErrorReceived(Me, "Live server process exited unexpectedly")
                                                End If
                                            End Sub
        End Sub

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isCapturing
            End Get
        End Property

        Public ReadOnly Property Transcript As String
            Get
                Return _transcript.ToString()
            End Get
        End Property

        Public ReadOnly Property IsServerReady As Boolean
            Get
                Return _serverReady
            End Get
        End Property

        ''' <summary>
        ''' Enumerate audio input devices via the live-server's /devices endpoint,
        ''' or by running a quick Python one-liner if the server isn't up.
        ''' </summary>
        Public Function EnumerateDevicesAsync(pythonExePath As String) As List(Of String)
            Dim devices As New List(Of String)

            ' Try the running server first
            If _serverReady Then
                Try
                    Dim response = _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/devices").Result
                    If response.IsSuccessStatusCode Then
                        Dim body = response.Content.ReadAsStringAsync().Result
                        Using doc = JsonDocument.Parse(body)
                            For Each dev In doc.RootElement.GetProperty("devices").EnumerateArray()
                                Dim id = dev.GetProperty("id").GetInt32()
                                Dim name = dev.GetProperty("name").GetString()
                                devices.Add($"{id}: {name}")
                            Next
                        End Using
                        If devices.Count > 0 Then Return devices
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"EnumerateDevicesAsync /devices request failed: {ex.Message}")
                End Try
            End If

            ' Fallback: run Python one-liner to get devices
            If Not String.IsNullOrEmpty(pythonExePath) AndAlso File.Exists(pythonExePath) Then
                Try
                    Dim script = "import sounddevice as sd; import json; a=sd.query_devices(sd.default.device[0])['hostapi']; ds=[{'id':i,'name':d['name']} for i,d in enumerate(sd.query_devices()) if d['max_input_channels']>0 and d['hostapi']==a]; print(json.dumps(ds))"
                    Dim psi As New ProcessStartInfo() With {
                        .FileName = pythonExePath,
                        .Arguments = $"-c ""{script}""",
                        .UseShellExecute = False,
                        .RedirectStandardOutput = True,
                        .RedirectStandardError = True,
                        .CreateNoWindow = True,
                        .StandardOutputEncoding = Encoding.UTF8
                    }

                    Using proc = Process.Start(psi)
                        ' Drain stderr async to prevent pipe buffer deadlock
                        Dim stderrTask = proc.StandardError.ReadToEndAsync()
                        Dim output = proc.StandardOutput.ReadToEnd()
                        stderrTask.Wait()
                        proc.WaitForExit(10000)
                        If proc.ExitCode = 0 AndAlso Not String.IsNullOrWhiteSpace(output) Then
                            Using doc = JsonDocument.Parse(output.Trim())
                                For Each dev In doc.RootElement.EnumerateArray()
                                    Dim id = dev.GetProperty("id").GetInt32()
                                    Dim name = dev.GetProperty("name").GetString()
                                    devices.Add($"{id}: {name}")
                                Next
                            End Using
                        End If
                    End Using
                Catch ex As Exception
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"EnumerateDevicesAsync Python fallback failed: {ex.Message}")
                End Try
            End If

            If devices.Count = 0 Then
                devices.Add("0: Default Device")
            End If

            Return devices
        End Function

        ''' <summary>Backend key for the sidecar (whisper-cpp-vulkan, whisper-cpp-cuda, whisper-cpp-cpu).</summary>
        Public Property Backend As String = "whisper-cpp-vulkan"

        ''' <summary>Path to whisper-server.exe (only used when Backend is whisper-cpp).</summary>
        Public Property WhisperServerPath As String = ""

        ''' <summary>Port for whisper-server.exe inference (only used when Backend is whisper-cpp).</summary>
        Public Property WhisperServerPort As Integer = 8178

        ''' <summary>Disable GPU for whisper-cpp (CPU-only mode).</summary>
        Public Property NoGpu As Boolean = False

        ''' <summary>Path to Silero VAD GGML model for whisper-server built-in VAD.</summary>
        Public Property SileroVadModelPath As String = ""

        ''' <summary>API key for online STT backends (e.g. Google Cloud STT, Speechmatics).</summary>
        Public Property SttApiKey As String = ""

        ''' <summary>Per-session hallucination filter file ("" = the live-server's default).</summary>
        Public Property FiltersHallucinationsPath As String = ""

        ''' <summary>"local" (default) = live-server captures from a device on this machine;
        ''' "web" = frames arrive via the live-server's /audio-in (browser web-mic broadcast).</summary>
        Public Property AudioSource As String = "local"

        ''' <summary>
        ''' Engine-specific extra /start JSON fields as a leading-comma fragment,
        ''' produced by the engine's config block (ICloudSttEngineConfig.
        ''' BuildStartJsonExtras). Appended verbatim — the runner never knows
        ''' which engine's fields these are. "" for engines with no extras.
        ''' </summary>
        Public Property CloudEngineStartExtras As String = ""

        ''' <summary>
        ''' Start the live-server Python process and begin capturing.
        ''' </summary>
        Public Sub Start(config As AppConfig, deviceIndex As Integer, inputLanguage As String, translateToEnglish As Boolean)
            If _isCapturing Then Return

            _host.Port = config.LiveServerPort
            _transcript.Clear()
            _cts = New CancellationTokenSource()
            _isCapturing = True

            ' If server is already running and healthy, reuse it
            Dim serverAlive = False
            If _host.IsProcessRunning Then
                Try
                    Dim resp = _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/health").Result
                    serverAlive = resp.IsSuccessStatusCode
                    If serverAlive Then _serverReady = True
                Catch ex As Exception
                    AppLogger.Log(LogEvents.STT_HEALTH_CHECK, $"Start health-check failed (will restart): {ex.Message}")
                End Try
            End If

            If Not serverAlive Then
                ' Stop any existing server and start fresh
                _host.Stop()
                _serverReady = False

                Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live-server", "server.py")

                ' Build extra args for backend selection. The registry's
                ' SidecarMode metadata decides the hosting mode — no per-engine
                ' key matching in this shared runner.
                Dim extraArgs = ""
                Dim backendKey = If(Backend, "whisper-cpp-vulkan")
                Select Case SidecarModeFor(backendKey)
                    Case "online"
                        ' Online engine — pass the backend key straight through; the
                        ' sidecar looks it up in its engine registry. No local model.
                        extraArgs = $"--backend {backendKey}"
                    Case "faster-whisper"
                        extraArgs = "--backend faster-whisper"
                    Case Else ' "whisper-cpp" (safe default for unknown keys)
                        Dim wsPath = AppConfig.ResolvePath(If(WhisperServerPath, ""))
                        extraArgs = $"--backend whisper-cpp --whisper-server-path ""{wsPath}"" --whisper-server-port {WhisperServerPort}"
                        Dim vadPath = AppConfig.ResolvePath(If(SileroVadModelPath, ""))
                        If Not String.IsNullOrEmpty(vadPath) AndAlso IO.File.Exists(vadPath) Then
                            extraArgs &= $" --vad-model-path ""{vadPath}"""
                        End If
                        If NoGpu Then extraArgs &= " --no-gpu"
                End Select

                Dim logLevel = Models.ConfigManager.Load().LogLevel.ToString().ToLowerInvariant()
                extraArgs &= $" --log-level {logLevel}"

                _host.Start(serverScript, extraArgs)
            End If

            ' Wait for server ready, then start capture
            Dim ct = _cts.Token
            Task.Run(Sub()
                         Try
                             Dim ready = If(serverAlive, True, WaitForReady(ct))
                             If ready Then
                                 StartCapture(config, deviceIndex, inputLanguage, translateToEnglish)
                                 ReadSseLoop(ct)
                             End If
                         Catch ex As Exception When ct.IsCancellationRequested
                             ' Expected during shutdown — ignore
                         Catch ex As Exception
                             AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"Pipeline task error: {ex.Message}")
                         End Try
                     End Sub)
        End Sub

        Private Function WaitForReady(ct As CancellationToken) As Boolean
            Dim deadline = DateTime.UtcNow.AddSeconds(30)
            While DateTime.UtcNow < deadline AndAlso Not ct.IsCancellationRequested
                Try
                    Thread.Sleep(500)
                    Dim response = _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/health", ct).Result
                    If response.IsSuccessStatusCode Then
                        _serverReady = True
                        Return True
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.STT_HEALTH_CHECK, $"WaitForReady health poll failed: {ex.Message}")
                End Try
            End While
            If Not ct.IsCancellationRequested Then
                RaiseEvent ErrorReceived(Me, "Live server: startup timeout (30s)")
            End If
            Return False
        End Function

        Private Sub StartCapture(config As AppConfig, deviceIndex As Integer, inputLanguage As String, translateToEnglish As Boolean)
            Try
                ' Resolve model path based on the backend's sidecar mode
                Dim backendKey = If(Backend, "whisper-cpp-vulkan")
                Dim modelPath As String
                Select Case SidecarModeFor(backendKey)
                    Case "online"
                        modelPath = ""
                    Case "faster-whisper"
                        modelPath = AppConfig.ResolvePath(config.PathFasterWhisperModel)
                    Case Else
                        modelPath = AppConfig.ResolvePath(config.PathWhisperCppModel)
                End Select

                Dim jsonBody = $"{{""device_index"":{deviceIndex}," &
                    $"""language"":""{inputLanguage}""," &
                    $"""translate"":{If(translateToEnglish, "true", "false")}," &
                    $"""initial_prompt"":""{EscapeJsonUnquoted(config.InitialPrompt)}""," &
                    $"""model_path"":""{EscapeJsonUnquoted(modelPath)}""," &
                    $"""compute_type"":""{config.LiveComputeType}""," &
                    $"""device"":""{If(config.NoGpu, "cpu", "cuda")}""," &
                    $"""beam_size"":{config.BeamSize}," &
                    $"""best_of"":{config.BestOf}," &
                    $"""vad_min_silence_ms"":{config.LiveVadSilenceMs}," &
                    $"""vad_max_segment_s"":{config.LiveMaxSegmentSec}," &
                    $"""interim_interval_ms"":{config.LiveInterimIntervalMs}," &
                    $"""whisper_server_port"":{config.WhisperServerPort}"

                ' Per-session hallucination filter set (empty = server default file)
                If Not String.IsNullOrEmpty(FiltersHallucinationsPath) Then
                    jsonBody &= $",""hallucinations_path"":""{EscapeJsonUnquoted(FiltersHallucinationsPath)}"""
                End If

                ' Web-mic sessions: the engine skips local capture and waits for
                ' frames on /audio-in ("local" is the default — omit to keep the
                ' /start payload identical for every existing deployment).
                If String.Equals(AudioSource, "web", StringComparison.OrdinalIgnoreCase) Then
                    jsonBody &= ",""audio_source"":""web"""
                End If

                ' Add API key for online backends (not logged in plaintext)
                If IsOnlineBackend(backendKey) AndAlso Not String.IsNullOrEmpty(SttApiKey) Then
                    jsonBody &= $",""stt_api_key"":""{EscapeJsonUnquoted(SttApiKey)}"""
                End If

                ' Engine-specific /start fields, pre-built by the engine's config
                ' block (leading-comma fragment, appended verbatim).
                If Not String.IsNullOrEmpty(CloudEngineStartExtras) Then
                    jsonBody &= CloudEngineStartExtras
                End If

                jsonBody &= "}"

                Dim content As New StringContent(jsonBody, Encoding.UTF8, "application/json")
                Dim response = _httpClient.PostAsync($"http://127.0.0.1:{_host.Port}/start", content).Result

                If Not response.IsSuccessStatusCode Then
                    Dim body = response.Content.ReadAsStringAsync().Result
                    RaiseEvent ErrorReceived(Me, $"Failed to start capture: {body}")
                End If
            Catch ex As Exception
                RaiseEvent ErrorReceived(Me, $"Failed to start capture: {ex.Message}")
            End Try
        End Sub

        Private Sub ReadSseLoop(ct As CancellationToken)
            Try
                Dim request As New HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{_host.Port}/stream")
                Dim response = _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).Result
                Using stream = response.Content.ReadAsStreamAsync().Result
                    Using reader As New StreamReader(stream, Encoding.UTF8)
                        Dim eventType = ""
                        While Not ct.IsCancellationRequested
                            Dim line = reader.ReadLine()
                            If line Is Nothing Then Exit While

                            If line.StartsWith("event:") Then
                                eventType = line.Substring(6).Trim()
                            ElseIf line.StartsWith("data:") Then
                                Dim dataStr = line.Substring(5).Trim()
                                Dim parsed = ParseJsonData(dataStr)
                                If Not String.IsNullOrEmpty(parsed.Text) Then
                                    If eventType = "update" Then
                                        RaiseEvent OutputLineUpdated(Me, parsed.Text)
                                    ElseIf eventType = "commit" Then
                                        _transcript.AppendLine(parsed.Text)
                                        If parsed.Translations IsNot Nothing AndAlso parsed.Translations.Count > 0 Then
                                            RaiseEvent OutputLineCommittedTranslated(Me, New TranslatedCommit With {
                                                .Text = parsed.Text, .Lang = parsed.Lang, .Translations = parsed.Translations})
                                        Else
                                            Dim commitData = If(String.IsNullOrEmpty(parsed.Lang), parsed.Text, parsed.Lang & vbTab & parsed.Text)
                                            RaiseEvent OutputLineCommitted(Me, commitData)
                                        End If
                                    ElseIf eventType = "error" Then
                                        RaiseEvent ErrorReceived(Me, parsed.Text)
                                    End If
                                End If
                                eventType = ""
                            End If
                        End While
                    End Using
                End Using
            Catch ex As OperationCanceledException
                ' Expected on stop
            Catch ex As Exception
                If Not ct.IsCancellationRequested Then
                    RaiseEvent ErrorReceived(Me, $"SSE connection lost: {ex.Message}")
                End If
            End Try
        End Sub

        Public Sub [Stop]()
            DoShutdown(3000)
        End Sub

        Private Sub DoShutdown(waitMs As Integer)
            ' Clear capturing state BEFORE stopping the process so the
            ' ProcessExited handler treats this exit as intentional (graceful)
            ' rather than logging a false "exited unexpectedly" error.
            _isCapturing = False
            _serverReady = False
            _cts?.Cancel()
            _httpClient.CancelPendingRequests()
            _host.Stop(waitMs)
        End Sub

        Public Sub ShutdownServer()
            DoShutdown(5000)
        End Sub

        ''' <summary>
        ''' TRUE only when the engine is ACTUALLY CAPTURING audio — not merely when the
        ''' live-server's HTTP is up. /health has always reported `capturing` and
        ''' `pipeline_alive`, but the old checks ignored them, so "ready" fired seconds
        ''' before the model was loaded and words spoken in that gap were lost.
        ''' </summary>
        Public Async Function CheckCapturingAsync(ct As Threading.CancellationToken) As Task(Of Boolean)
            If Not _serverReady Then Return False
            Try
                Dim response = Await _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/health", ct)
                If Not response.IsSuccessStatusCode Then Return False
                Dim body = Await response.Content.ReadAsStringAsync()
                Using doc = JsonDocument.Parse(body)
                    Dim root = doc.RootElement
                    Dim statusProp As JsonElement = Nothing
                    If root.TryGetProperty("status", statusProp) AndAlso statusProp.GetString() <> "ok" Then Return False
                    Dim capProp As JsonElement = Nothing
                    If Not (root.TryGetProperty("capturing", capProp) AndAlso capProp.GetBoolean()) Then Return False
                    Dim pipeProp As JsonElement = Nothing
                    If root.TryGetProperty("pipeline_alive", pipeProp) AndAlso Not pipeProp.GetBoolean() Then Return False
                    Return True
                End Using
            Catch
                Return False
            End Try
        End Function

        Public Async Function UpdateConfigAsync(config As Dictionary(Of String, Object)) As Task
            If Not _serverReady Then Return
            Try
                Dim json = Text.Json.JsonSerializer.Serialize(config)
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Await _httpClient.PostAsync($"http://127.0.0.1:{_host.Port}/config", content)
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"UpdateConfigAsync failed: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Split a buffered clause into proper sentences via live-server's SaT
        ''' segmenter (engine-agnostic, list-free). Returns {text} unchanged if the
        ''' server isn't ready or SaT is unavailable — never throws.
        ''' </summary>
        Public Async Function SegmentAsync(text As String, thresholdPercent As Integer, model As String) As Task(Of List(Of String))
            Dim fallback As New List(Of String) From {text}
            If Not _serverReady OrElse String.IsNullOrWhiteSpace(text) Then Return fallback
            Try
                Dim payload As New Dictionary(Of String, Object) From {
                    {"text", text},
                    {"threshold", Math.Max(1, thresholdPercent) / 100.0},
                    {"model", If(String.IsNullOrEmpty(model), "sat-3l-sm", model)}}
                Dim json = System.Text.Json.JsonSerializer.Serialize(payload)
                Dim content As New StringContent(json, System.Text.Encoding.UTF8, "application/json")
                Dim resp = Await _httpClient.PostAsync($"http://127.0.0.1:{_host.Port}/segment", content)
                Dim body = Await resp.Content.ReadAsStringAsync()
                Using doc = JsonDocument.Parse(body)
                    Dim arr As JsonElement = Nothing
                    If doc.RootElement.TryGetProperty("sentences", arr) AndAlso arr.ValueKind = JsonValueKind.Array Then
                        Dim list As New List(Of String)
                        For Each el In arr.EnumerateArray()
                            Dim s = el.GetString()
                            If Not String.IsNullOrWhiteSpace(s) Then list.Add(s)
                        Next
                        If list.Count > 0 Then Return list
                    End If
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"SegmentAsync failed: {ex.Message}")
            End Try
            Return fallback
        End Function

        ''' <summary>Blocking wrapper for the clause accumulator's flush (runs on the flush-timer thread). Bounded so a slow sidecar can't stall the timer; on timeout returns {text} unsplit.</summary>
        Public Function Segment(text As String, thresholdPercent As Integer, model As String) As List(Of String)
            Try
                Dim t = SegmentAsync(text, thresholdPercent, model)
                If t.Wait(1500) Then Return t.Result
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"Segment (sync) failed: {ex.Message}")
            End Try
            Return New List(Of String) From {text}
        End Function

        Public Function SaveTranscript(filePath As String) As Boolean
            Try
                File.WriteAllText(filePath, _transcript.ToString(), Encoding.UTF8)
                Return True
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"SaveTranscript failed: {ex.Message}")
                Return False
            End Try
        End Function

        Private Structure ParsedData
            Public Text As String
            Public Lang As String
            Public Translations As Dictionary(Of String, String)
        End Structure

        Private Shared Function ParseJsonData(json As String) As ParsedData
            Dim result As New ParsedData()
            Try
                Using doc = JsonDocument.Parse(json)
                    Dim root = doc.RootElement
                    result.Text = If(root.TryGetProperty("text", Nothing), root.GetProperty("text").GetString(), "")
                    Dim langProp As JsonElement = Nothing
                    If root.TryGetProperty("lang", langProp) Then
                        result.Lang = langProp.GetString()
                    End If
                    Dim txProp As JsonElement = Nothing
                    If root.TryGetProperty("translations", txProp) AndAlso txProp.ValueKind = JsonValueKind.Object Then
                        result.Translations = New Dictionary(Of String, String)
                        For Each prop In txProp.EnumerateObject()
                            result.Translations(prop.Name) = prop.Value.GetString()
                        Next
                    End If
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"ParseJsonData failed to parse SSE payload: {ex.Message}")
            End Try
            Return result
        End Function

        Public Async Function GetStatsAsync() As Task(Of String)
            Try
                Dim response = Await _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/stats")
                If response.IsSuccessStatusCode Then
                    Return Await response.Content.ReadAsStringAsync()
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_WHISPER_SERVER_ERROR, $"GetStatsAsync failed: {ex.Message}")
            End Try
            Return Nothing
        End Function

        ''' <summary>
        ''' The backend's sidecar hosting mode ("whisper-cpp", "faster-whisper" or
        ''' "online") from registry metadata. Unknown keys default to "whisper-cpp"
        ''' so new engines work without editing this class.
        ''' </summary>
        Private Shared Function SidecarModeFor(backendKey As String) As String
            Dim entry = Services.Stt.SttBackendRegistry.Find(backendKey)
            Return If(entry?.SidecarMode, "whisper-cpp")
        End Function

        ''' <summary>
        ''' An "online" backend is a registered cloud engine hosted by the
        ''' sidecar's engine registry (no local model), per registry metadata.
        ''' </summary>
        Private Shared Function IsOnlineBackend(backendKey As String) As Boolean
            Return SidecarModeFor(backendKey).Equals("online", StringComparison.OrdinalIgnoreCase)
        End Function

        Private Shared Function EscapeJsonUnquoted(s As String) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Dim quoted = ProcessHelper.EscapeJson(s)
            Return quoted.Substring(1, quoted.Length - 2)
        End Function

    End Class
End Namespace
