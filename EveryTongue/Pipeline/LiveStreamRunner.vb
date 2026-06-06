Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Models

Namespace Pipeline
    Public Class LiveStreamRunner

        Public Event OutputLineUpdated As EventHandler(Of String)   ' interim - in-progress text
        Public Event OutputLineCommitted As EventHandler(Of String) ' final - committed text
        Public Event ErrorReceived As EventHandler(Of String)

        Private Shared ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(5)
        }

        Private ReadOnly _host As New PythonSidecarHost() With {
            .Label = "Live server",
            .AddWhisperToPath = True,
            .GracefulShutdownPath = "/shutdown"
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
                    FormMain.WriteDebugLog($"[Live] EnumerateDevicesAsync /devices request failed: {ex.Message}")
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
                    FormMain.WriteDebugLog($"[Live] EnumerateDevicesAsync Python fallback failed: {ex.Message}")
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
                    FormMain.WriteDebugLog($"[Live] Start health-check failed (will restart): {ex.Message}")
                End Try
            End If

            If Not serverAlive Then
                ' Stop any existing server and start fresh
                _host.Stop()
                _serverReady = False

                Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live-server", "server.py")

                ' Build extra args for backend selection
                Dim extraArgs = ""
                Dim backendKey = If(Backend, "whisper-cpp-vulkan")
                If backendKey.StartsWith("whisper-cpp", StringComparison.OrdinalIgnoreCase) Then
                    Dim wsPath = AppConfig.ResolvePath(If(WhisperServerPath, ""))
                    extraArgs = $"--backend whisper-cpp --whisper-server-path ""{wsPath}"" --whisper-server-port {WhisperServerPort}"
                    Dim vadPath = AppConfig.ResolvePath(If(SileroVadModelPath, ""))
                    If Not String.IsNullOrEmpty(vadPath) AndAlso IO.File.Exists(vadPath) Then
                        extraArgs &= $" --vad-model-path ""{vadPath}"""
                    End If
                    If NoGpu Then extraArgs &= " --no-gpu"
                End If

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
                             FormMain.WriteDebugLog($"[Live] Pipeline task error: {ex.Message}")
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
                    FormMain.WriteDebugLog($"[Live] WaitForReady health poll failed: {ex.Message}")
                End Try
            End While
            If Not ct.IsCancellationRequested Then
                RaiseEvent ErrorReceived(Me, "Live server: startup timeout (30s)")
            End If
            Return False
        End Function

        Private Sub StartCapture(config As AppConfig, deviceIndex As Integer, inputLanguage As String, translateToEnglish As Boolean)
            Try
                ' Resolve model path based on backend
                Dim backendKey = If(Backend, "whisper-cpp-vulkan")
                Dim modelPath = AppConfig.ResolvePath(config.PathWhisperCppModel)

                Dim jsonBody = $"{{""device_index"":{deviceIndex}," &
                    $"""language"":""{inputLanguage}""," &
                    $"""translate"":{If(translateToEnglish, "true", "false")}," &
                    $"""initial_prompt"":""{EscapeJsonUnquoted(config.InitialPrompt)}""," &
                    $"""model_path"":""{EscapeJsonUnquoted(modelPath)}""," &
                    $"""compute_type"":""{config.LiveComputeType}""," &
                    $"""device"":""{If(config.NoGpu, "cpu", "cuda")}""," &
                    $"""beam_size"":{config.BeamSize}," &
                    $"""vad_min_silence_ms"":{config.LiveVadSilenceMs}," &
                    $"""vad_max_segment_s"":{config.LiveMaxSegmentSec}," &
                    $"""interim_interval_ms"":{config.LiveInterimIntervalMs}," &
                    $"""whisper_server_port"":{config.WhisperServerPort}}}"

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
                                        Dim commitData = If(String.IsNullOrEmpty(parsed.Lang), parsed.Text, parsed.Lang & vbTab & parsed.Text)
                                        RaiseEvent OutputLineCommitted(Me, commitData)
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
            _cts?.Cancel()
            _httpClient.CancelPendingRequests()
            _host.Stop(waitMs)
            _isCapturing = False
            _serverReady = False
        End Sub

        Public Sub ShutdownServer()
            DoShutdown(5000)
        End Sub

        Public Async Function UpdateConfigAsync(config As Dictionary(Of String, Object)) As Task
            If Not _serverReady Then Return
            Try
                Dim json = Text.Json.JsonSerializer.Serialize(config)
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Await _httpClient.PostAsync($"http://127.0.0.1:{_host.Port}/config", content)
            Catch ex As Exception
                FormMain.WriteDebugLog($"[Live] UpdateConfigAsync failed: {ex.Message}")
            End Try
        End Function

        Public Function SaveTranscript(filePath As String) As Boolean
            Try
                File.WriteAllText(filePath, _transcript.ToString(), Encoding.UTF8)
                Return True
            Catch ex As Exception
                FormMain.WriteDebugLog($"[Live] SaveTranscript failed: {ex.Message}")
                Return False
            End Try
        End Function

        Private Structure ParsedData
            Public Text As String
            Public Lang As String
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
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[Live] ParseJsonData failed to parse SSE payload: {ex.Message}")
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
                FormMain.WriteDebugLog($"[Live] GetStatsAsync failed: {ex.Message}")
            End Try
            Return Nothing
        End Function

        Private Shared Function EscapeJsonUnquoted(s As String) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Dim quoted = ProcessHelper.EscapeJson(s)
            Return quoted.Substring(1, quoted.Length - 2)
        End Function

    End Class
End Namespace
