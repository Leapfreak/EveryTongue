Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Namespace Services.Testing

    ''' <summary>
    ''' Runs STT transcription on a test audio file using each available backend
    ''' (whisper.cpp CUDA / whisper.cpp Vulkan / whisper.cpp CPU)
    ''' and returns a side-by-side comparison of load time, inference latency, and output text.
    ''' </summary>
    Public Class SttComparisonRunner

        Public Event ProgressChanged As EventHandler(Of String)

        Private _cts As CancellationTokenSource

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        ''' <summary>
        ''' Run the comparison benchmark. Tests each backend that has its dependencies available.
        ''' whisper-server.exe is managed per-test for CUDA, Vulkan, and CPU backends.
        ''' </summary>
        Public Async Function RunComparisonAsync(
            audioFilePath As String,
            config As AppConfig,
            liveServerPort As Integer,
            iterations As Integer,
            ct As CancellationToken,
            Optional enabledEngines As HashSet(Of String) = Nothing
        ) As Task(Of SttComparisonResult)

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            Dim token = _cts.Token
            Dim result As New SttComparisonResult()
            Dim audioBytes = File.ReadAllBytes(audioFilePath)

            ' Detect available backends
            Dim hwInfo = HardwareScanner.Scan()
            Dim whisperServerPath = AppConfig.ResolvePath(config.PathWhisperServer)
            Dim ggmlModelPath = AppConfig.ResolvePath(config.PathWhisperCppModel)
            Dim hasWhisperServer = File.Exists(whisperServerPath) AndAlso File.Exists(ggmlModelPath)

            ' CUDA build lives next to the Vulkan build as whisper-server-cuda.exe
            Dim whisperServerDir = Path.GetDirectoryName(whisperServerPath)
            Dim cudaServerPath = If(whisperServerDir IsNot Nothing,
                Path.Combine(whisperServerDir, "whisper-server-cuda.exe"), "")
            Dim hasCudaServer = File.Exists(cudaServerPath) AndAlso File.Exists(ggmlModelPath)

            AppLogger.Log($"[STT-COMPARE] ═══════════════════════════════════════════════")
            AppLogger.Log($"[STT-COMPARE] Audio: {audioFilePath} ({audioBytes.Length \ 1024}KB), Iterations: {iterations}")
            AppLogger.Log($"[STT-COMPARE] CUDA={hwInfo.HasCuda}, Vulkan={hwInfo.HasVulkan}")
            AppLogger.Log($"[STT-COMPARE] whisper-server path: {whisperServerPath} (exists={IO.File.Exists(whisperServerPath)})")
            AppLogger.Log($"[STT-COMPARE] whisper-server-cuda path: {cudaServerPath} (exists={IO.File.Exists(cudaServerPath)})")
            AppLogger.Log($"[STT-COMPARE] GGML model path: {ggmlModelPath} (exists={IO.File.Exists(ggmlModelPath)})")
            AppLogger.Log($"[STT-COMPARE] hasWhisperServer={hasWhisperServer}, hasCudaServer={hasCudaServer}")
            AppLogger.Log($"[STT-COMPARE] config.SttBackend={config.SttBackend}")
            AppLogger.Log($"[STT-COMPARE] enabledEngines={If(enabledEngines Is Nothing, "(all)", String.Join(",", enabledEngines))}")

            ' Helper: check if an engine is enabled (Nothing = all enabled)
            Dim isEnabled = Function(key As String) enabledEngines Is Nothing OrElse enabledEngines.Contains(key)

            ' 1) whisper.cpp (CUDA) — start whisper-server-cuda.exe (NVIDIA only)
            AppLogger.Log($"[STT-COMPARE] CUDA native check: hasCudaServer={hasCudaServer}, HasCuda={hwInfo.HasCuda}")
            If Not isEnabled("whisper-cpp-cuda") Then
                AppLogger.Log($"[STT-COMPARE] CUDA native skipped: deselected by user")
            ElseIf hasCudaServer AndAlso hwInfo.HasCuda Then
                RaiseProgress("Testing whisper.cpp (CUDA) — loading model...")
                Dim cudaResult = Await TestViaWhisperServer(
                    cudaServerPath, ggmlModelPath, useGpu:=True, audioBytes, iterations, token,
                    portOverride:=18103, modeName:="CUDA")
                cudaResult.BackendName = "whisper.cpp (CUDA)"
                cudaResult.BackendKey = "whisper-cpp-cuda"
                result.Backends.Add(cudaResult)
                AppLogger.Log($"[STT-COMPARE] CUDA native done: avg={cudaResult.AvgInferenceMs}ms, failed={cudaResult.Failed}, error={cudaResult.ErrorMessage}")
            ElseIf Not hasCudaServer Then
                AppLogger.Log($"[STT-COMPARE] CUDA native skipped: whisper-server-cuda.exe not found")
                result.Backends.Add(New BackendComparisonResult() With {
                    .BackendName = "whisper.cpp (CUDA)",
                    .BackendKey = "whisper-cpp-cuda",
                    .Skipped = True,
                    .SkipReason = "whisper-server-cuda.exe not installed"})
            ElseIf Not hwInfo.HasCuda Then
                AppLogger.Log($"[STT-COMPARE] CUDA native skipped: no NVIDIA GPU (CUDA required)")
                result.Backends.Add(New BackendComparisonResult() With {
                    .BackendName = "whisper.cpp (CUDA)",
                    .BackendKey = "whisper-cpp-cuda",
                    .Skipped = True,
                    .SkipReason = "No NVIDIA GPU (CUDA required)"})
            End If

            token.ThrowIfCancellationRequested()

            ' Wait for port cleanup between CUDA and Vulkan tests
            If hasCudaServer AndAlso hwInfo.HasCuda Then
                AppLogger.Log($"[STT-COMPARE] Waiting 2s for port cleanup before Vulkan test...")
                Await Task.Delay(2000, token)
            End If

            ' 3) whisper.cpp (Vulkan) — start whisper-server.exe (Vulkan build) without -ng
            AppLogger.Log($"[STT-COMPARE] Vulkan check: hasWhisperServer={hasWhisperServer}, HasVulkan={hwInfo.HasVulkan}")
            If Not isEnabled("whisper-cpp-vulkan") Then
                AppLogger.Log($"[STT-COMPARE] Vulkan skipped: deselected by user")
            ElseIf hasWhisperServer AndAlso hwInfo.HasVulkan Then
                RaiseProgress("Testing whisper.cpp (Vulkan) — loading model...")
                Dim vulkanResult = Await TestViaWhisperServer(
                    whisperServerPath, ggmlModelPath, useGpu:=True, audioBytes, iterations, token)
                vulkanResult.BackendName = "whisper.cpp (Vulkan)"
                vulkanResult.BackendKey = "whisper-cpp-vulkan"
                result.Backends.Add(vulkanResult)
                AppLogger.Log($"[STT-COMPARE] Vulkan done: avg={vulkanResult.AvgInferenceMs}ms, failed={vulkanResult.Failed}, error={vulkanResult.ErrorMessage}")
            ElseIf Not hasWhisperServer Then
                AppLogger.Log($"[STT-COMPARE] Vulkan skipped: whisper-server or GGML model not found")
                result.Backends.Add(New BackendComparisonResult() With {
                    .BackendName = "whisper.cpp (Vulkan)",
                    .BackendKey = "whisper-cpp-vulkan",
                    .Skipped = True,
                    .SkipReason = "whisper-server.exe or GGML model not installed"})
            ElseIf Not hwInfo.HasVulkan Then
                AppLogger.Log($"[STT-COMPARE] Vulkan skipped: no Vulkan runtime")
                result.Backends.Add(New BackendComparisonResult() With {
                    .BackendName = "whisper.cpp (Vulkan)",
                    .BackendKey = "whisper-cpp-vulkan",
                    .Skipped = True,
                    .SkipReason = "Vulkan runtime not detected"})
            End If

            token.ThrowIfCancellationRequested()

            ' Wait a moment for port cleanup between Vulkan and CPU tests
            If hasWhisperServer AndAlso hwInfo.HasVulkan Then
                AppLogger.Log($"[STT-COMPARE] Waiting 2s for port cleanup before CPU test...")
                Await Task.Delay(2000, token)
            End If

            ' 4) whisper.cpp (CPU) — start whisper-server.exe with -ng
            AppLogger.Log($"[STT-COMPARE] CPU check: hasWhisperServer={hasWhisperServer}")
            If Not isEnabled("whisper-cpp-cpu") Then
                AppLogger.Log($"[STT-COMPARE] CPU skipped: deselected by user")
            ElseIf hasWhisperServer Then
                RaiseProgress("Testing whisper.cpp (CPU) — loading model...")
                Dim cpuResult = Await TestViaWhisperServer(
                    whisperServerPath, ggmlModelPath, useGpu:=False, audioBytes, iterations, token)
                cpuResult.BackendName = "whisper.cpp (CPU)"
                cpuResult.BackendKey = "whisper-cpp-cpu"
                result.Backends.Add(cpuResult)
                AppLogger.Log($"[STT-COMPARE] CPU done: avg={cpuResult.AvgInferenceMs}ms, failed={cpuResult.Failed}, error={cpuResult.ErrorMessage}")
            Else
                AppLogger.Log($"[STT-COMPARE] CPU skipped: whisper-server or GGML model not found")
                result.Backends.Add(New BackendComparisonResult() With {
                    .BackendName = "whisper.cpp (CPU)",
                    .BackendKey = "whisper-cpp-cpu",
                    .Skipped = True,
                    .SkipReason = "whisper-server.exe or GGML model not installed"})
            End If

            ' Compute speedup ratios (relative to fastest)
            Dim completed = result.Backends.Where(Function(b) Not b.Skipped AndAlso Not b.Failed).ToList()
            If completed.Count > 0 Then
                Dim fastest = completed.Min(Function(b) b.AvgInferenceMs)
                If fastest > 0 Then
                    For Each b In completed
                        b.SpeedupVsFastest = Math.Round(fastest / b.AvgInferenceMs, 2)
                    Next
                End If
            End If

            RaiseProgress("Comparison complete.")
            Return result
        End Function

        ''' <summary>
        ''' Start whisper-server.exe, wait for health, run inference, stop it.
        ''' Uses a unique port to avoid conflicts with any running instance.
        ''' </summary>
        Private Async Function TestViaWhisperServer(
            serverPath As String, modelPath As String, useGpu As Boolean,
            audioBytes As Byte(), iterations As Integer,
            ct As CancellationToken,
            Optional portOverride As Integer = 0,
            Optional modeName As String = Nothing
        ) As Task(Of BackendComparisonResult)

            Dim result As New BackendComparisonResult()
            ' Use a temp port in the high range to avoid conflicts
            Dim port = If(portOverride > 0, portOverride, 18100 + If(useGpu, 1, 2))
            If modeName Is Nothing Then modeName = If(useGpu, "Vulkan", "CPU")
            Dim proc As Process = Nothing

            AppLogger.Log($"[STT-COMPARE] ── TestViaWhisperServer: mode={modeName}, useGpu={useGpu}, port={port}")
            AppLogger.Log($"[STT-COMPARE]    serverPath={serverPath}")
            AppLogger.Log($"[STT-COMPARE]    modelPath={modelPath}")

            Try
                ' Kill any leftover whisper-server on this port
                Try
                    Using client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(2)}
                        Dim check = Await client.GetAsync($"http://127.0.0.1:{port}/health", ct)
                        AppLogger.Log($"[STT-COMPARE]    WARNING: port {port} already responding (status={check.StatusCode})! Waiting 3s...")
                        Await Task.Delay(3000, ct)
                    End Using
                Catch
                    ' Good — port is free
                End Try

                ' Start whisper-server.exe
                Dim loadSw = Stopwatch.StartNew()
                Dim args = $"-m ""{modelPath}"" --port {port} --host 127.0.0.1"
                If Not useGpu Then args &= " -ng"

                AppLogger.Log($"[STT-COMPARE]    Starting: {serverPath} {args}")
                proc = Process.Start(New ProcessStartInfo(serverPath, args) With {
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                })

                If proc Is Nothing OrElse proc.HasExited Then
                    result.Failed = True
                    result.ErrorMessage = "Failed to start whisper-server.exe"
                    Return result
                End If

                ' Log server stderr asynchronously so we can see GPU detection messages
                Dim stderrTask = Task.Run(Async Function()
                    Try
                        Dim line As String
                        Do
                            line = Await proc.StandardError.ReadLineAsync()
                            If line IsNot Nothing Then
                                AppLogger.Log($"[STT-COMPARE]    [{modeName} stderr] {line}")
                            End If
                        Loop While line IsNot Nothing
                    Catch
                    End Try
                End Function)

                ' Wait for /health to return 200 (up to 120s for large models)
                Dim ready = False
                Using client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(3)}
                    For i = 0 To 239
                        ct.ThrowIfCancellationRequested()
                        If proc.HasExited Then
                            result.Failed = True
                            result.ErrorMessage = $"whisper-server exited during startup (code {proc.ExitCode})"
                            Return result
                        End If
                        Try
                            Dim resp = Await client.GetAsync($"http://127.0.0.1:{port}/health", ct)
                            If resp.IsSuccessStatusCode Then
                                ready = True
                                Exit For
                            End If
                        Catch ex As Exception When TypeOf ex IsNot OperationCanceledException
                        End Try
                        Await Task.Delay(500, ct)
                    Next
                End Using

                loadSw.Stop()
                result.ModelLoadMs = loadSw.ElapsedMilliseconds

                If Not ready Then
                    result.Failed = True
                    result.ErrorMessage = "whisper-server startup timeout (120s)"
                    Return result
                End If

                AppLogger.Log($"[STT-COMPARE]    whisper-server ({modeName}) ready in {result.ModelLoadMs}ms on port {port}")

                ' Run inference — scale timeout with audio size
                Dim inferTimeoutSec = Math.Max(120, CInt(audioBytes.Length / 1024.0 / 1024.0 * 6))
                Dim latencies As New List(Of Long)()
                Using client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(inferTimeoutSec)}
                    For i = 1 To iterations
                        ct.ThrowIfCancellationRequested()
                        RaiseProgress($"whisper.cpp ({modeName}): inference {i}/{iterations}...")

                        Dim sw = Stopwatch.StartNew()
                        Try
                            ' Build multipart form matching whisper-server's /inference API
                            Using formContent As New MultipartFormDataContent()
                                Dim fileContent As New ByteArrayContent(audioBytes)
                                fileContent.Headers.ContentType = New Headers.MediaTypeHeaderValue("audio/wav")
                                formContent.Add(fileContent, "file", "audio.wav")
                                formContent.Add(New StringContent("auto"), "language")
                                formContent.Add(New StringContent("0.0"), "temperature")
                                formContent.Add(New StringContent("0.2"), "temperature_inc")
                                formContent.Add(New StringContent("json"), "response_format")

                                Dim resp = Await client.PostAsync(
                                    $"http://127.0.0.1:{port}/inference", formContent, ct)
                                sw.Stop()

                                If resp.IsSuccessStatusCode Then
                                    latencies.Add(sw.ElapsedMilliseconds)
                                    AppLogger.Log($"[STT-COMPARE]    {modeName} inference {i}/{iterations}: {sw.ElapsedMilliseconds}ms OK")
                                    If String.IsNullOrEmpty(result.TranscribedText) Then
                                        Dim body = Await resp.Content.ReadAsStringAsync()
                                        AppLogger.Log($"[STT-COMPARE]    {modeName} response body: {If(body?.Length > 200, body.Substring(0, 200) & "...", body)}")
                                        result.TranscribedText = ExtractText(body)
                                    End If
                                Else
                                    latencies.Add(sw.ElapsedMilliseconds)
                                    Dim errBody = Await resp.Content.ReadAsStringAsync()
                                    AppLogger.Log($"[STT-COMPARE]    {modeName} inference {i}/{iterations}: {sw.ElapsedMilliseconds}ms HTTP {CInt(resp.StatusCode)} — {errBody}")
                                    If String.IsNullOrEmpty(result.ErrorMessage) Then
                                        result.ErrorMessage = $"HTTP {CInt(resp.StatusCode)}"
                                    End If
                                End If
                            End Using
                        Catch ex As OperationCanceledException
                            Throw
                        Catch ex As Exception
                            sw.Stop()
                            latencies.Add(sw.ElapsedMilliseconds)
                            If String.IsNullOrEmpty(result.ErrorMessage) Then
                                result.ErrorMessage = ex.Message
                            End If
                        End Try
                    Next
                End Using

                If latencies.Count > 0 Then
                    result.AvgInferenceMs = CLng(latencies.Average())
                    result.MinInferenceMs = latencies.Min()
                    result.MaxInferenceMs = latencies.Max()
                    result.Iterations = latencies.Count
                End If

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                result.Failed = True
                result.ErrorMessage = ex.Message
            Finally
                ' Always kill the whisper-server process
                If proc IsNot Nothing AndAlso Not proc.HasExited Then
                    Try
                        AppLogger.Log($"[STT-COMPARE]    Killing whisper-server pid={proc.Id} ({modeName})...")
                        proc.Kill(entireProcessTree:=True)
                        proc.WaitForExit(5000)
                        AppLogger.Log($"[STT-COMPARE]    whisper-server killed, HasExited={proc.HasExited}")
                    Catch ex As Exception
                        AppLogger.Log($"[STT-COMPARE]    Kill failed: {ex.Message}")
                    End Try
                Else
                    AppLogger.Log($"[STT-COMPARE]    whisper-server ({modeName}) already exited or was Nothing")
                End If
                proc?.Dispose()
            End Try

            Return result
        End Function

        Private Shared Function ExtractText(json As String) As String
            Try
                Using doc = JsonDocument.Parse(json)
                    ' whisper-server returns {"text": "...", "segments": [...]}
                    ' or sometimes just segments
                    Dim textProp As JsonElement
                    If doc.RootElement.TryGetProperty("text", textProp) Then
                        Return textProp.GetString()
                    End If
                    ' Fallback: concatenate segment texts
                    Dim segProp As JsonElement
                    If doc.RootElement.TryGetProperty("segments", segProp) Then
                        Dim sb As New Text.StringBuilder()
                        For Each seg In segProp.EnumerateArray()
                            Dim t As JsonElement
                            If seg.TryGetProperty("text", t) Then
                                sb.Append(t.GetString())
                            End If
                        Next
                        Return sb.ToString().Trim()
                    End If
                End Using
            Catch
            End Try
            Return json
        End Function

        Private Sub RaiseProgress(msg As String)
            AppLogger.Log($"[STT-COMPARE] {msg}")
            RaiseEvent ProgressChanged(Me, msg)
        End Sub

    End Class

    ' ── Result models ──

    Public Class SttComparisonResult
        Public Property Backends As New List(Of BackendComparisonResult)()
    End Class

    Public Class BackendComparisonResult
        Public Property BackendName As String = ""
        Public Property BackendKey As String = ""
        Public Property Skipped As Boolean = False
        Public Property SkipReason As String = ""
        Public Property Failed As Boolean = False
        Public Property ErrorMessage As String = ""
        Public Property ModelLoadMs As Long = 0
        Public Property AvgInferenceMs As Long = 0
        Public Property MinInferenceMs As Long = 0
        Public Property MaxInferenceMs As Long = 0
        Public Property SpeedupVsFastest As Double = 0
        Public Property Iterations As Integer = 0
        Public Property TranscribedText As String = ""
    End Class

End Namespace
