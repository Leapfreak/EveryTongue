Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Namespace Services.Testing

    ''' <summary>
    ''' Tests concurrent STT throughput by firing N parallel inference requests
    ''' at whisper-server and measuring latency/throughput at each concurrency level.
    ''' Simulates multiple room participants speaking simultaneously.
    ''' </summary>
    Public Class SttConcurrencyRunner

        Public Event ProgressChanged As EventHandler(Of String)

        Private _cts As CancellationTokenSource

        Public Sub Cancel()
            _cts?.Cancel()
        End Sub

        ''' <summary>
        ''' Run the concurrent throughput test. Starts its own whisper-server instance,
        ''' then tests at each concurrency level (e.g. 1, 2, 5, 10, 15, 20).
        ''' </summary>
        Public Async Function RunAsync(
            audioFilePath As String,
            config As AppConfig,
            concurrencyLevels As Integer(),
            iterationsPerLevel As Integer,
            ct As CancellationToken
        ) As Task(Of ConcurrencyTestResult)

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
            Dim token = _cts.Token
            Dim result As New ConcurrencyTestResult()
            Dim audioBytes = File.ReadAllBytes(audioFilePath)

            Dim whisperServerPath = AppConfig.ResolvePath(config.PathWhisperServer)
            Dim ggmlModelPath = AppConfig.ResolvePath(config.PathWhisperCppModel)

            If Not File.Exists(whisperServerPath) OrElse Not File.Exists(ggmlModelPath) Then
                result.ErrorMessage = "whisper-server.exe or GGML model not found"
                Return result
            End If

            ' Also check for CUDA build
            Dim whisperServerDir = Path.GetDirectoryName(whisperServerPath)
            Dim cudaServerPath = If(whisperServerDir IsNot Nothing,
                Path.Combine(whisperServerDir, "whisper-server-cuda.exe"), "")
            Dim hwInfo = HardwareScanner.Scan()
            Dim hasCudaServer = File.Exists(cudaServerPath) AndAlso hwInfo.HasCuda

            ' Prefer CUDA > Vulkan
            Dim serverExe = If(hasCudaServer, cudaServerPath, whisperServerPath)
            Dim backendName = If(hasCudaServer, "CUDA", "Vulkan")

            Dim port = 18110
            Dim proc As Process = Nothing

            AppLogger.Log($"[STT-CONCURRENCY] ═══════════════════════════════════════════════")
            AppLogger.Log($"[STT-CONCURRENCY] Audio: {audioFilePath} ({audioBytes.Length \ 1024}KB)")
            AppLogger.Log($"[STT-CONCURRENCY] Backend: {backendName} ({Path.GetFileName(serverExe)})")
            AppLogger.Log($"[STT-CONCURRENCY] Levels: {String.Join(", ", concurrencyLevels)}, iterations/level: {iterationsPerLevel}")

            Try
                ' Start whisper-server
                RaiseProgress($"Starting whisper-server ({backendName})...")
                Dim loadSw = Stopwatch.StartNew()
                Dim args = $"-m ""{ggmlModelPath}"" --port {port} --host 127.0.0.1"

                proc = Process.Start(New ProcessStartInfo(serverExe, args) With {
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                })

                If proc Is Nothing OrElse proc.HasExited Then
                    result.ErrorMessage = "Failed to start whisper-server"
                    Return result
                End If

                ' Drain stdout AND stderr to prevent pipe deadlock
                Dim stdoutTask = Task.Run(Async Function()
                    Try
                        Do While Await proc.StandardOutput.ReadLineAsync() IsNot Nothing
                        Loop
                    Catch
                    End Try
                End Function)
                Dim stderrTask = Task.Run(Async Function()
                    Try
                        Dim line As String
                        Do
                            line = Await proc.StandardError.ReadLineAsync()
                            If line IsNot Nothing Then
                                AppLogger.Log($"[STT-CONCURRENCY] [stderr] {line}")
                            End If
                        Loop While line IsNot Nothing
                    Catch
                    End Try
                End Function)

                ' Wait for /health
                Dim ready = False
                Using client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(3)}
                    For i = 0 To 239
                        token.ThrowIfCancellationRequested()
                        If proc.HasExited Then
                            result.ErrorMessage = $"whisper-server exited during startup (code {proc.ExitCode})"
                            Return result
                        End If
                        Try
                            Dim resp = Await client.GetAsync($"http://127.0.0.1:{port}/health", token)
                            If resp.IsSuccessStatusCode Then
                                ready = True
                                Exit For
                            End If
                        Catch ex As OperationCanceledException When token.IsCancellationRequested
                            Throw ' Real user cancellation
                        Catch
                            ' HttpClient timeout or connection refused — retry
                        End Try
                        Await Task.Delay(500, token)
                    Next
                End Using

                loadSw.Stop()
                result.ModelLoadMs = loadSw.ElapsedMilliseconds
                result.BackendName = backendName

                If Not ready Then
                    result.ErrorMessage = "whisper-server startup timeout (120s)"
                    Return result
                End If

                AppLogger.Log($"[STT-CONCURRENCY] Server ready in {result.ModelLoadMs}ms")

                ' Start resource monitoring (CPU, RAM, GPU) — after model load so we capture test load
                Dim monitor As New ResourceMonitor(500)
                monitor.Start()

                ' Warm up with 2 inferences
                RaiseProgress("Warming up (2 inferences)...")
                For i = 1 To 2
                    token.ThrowIfCancellationRequested()
                    Await PostInferenceAsync(port, audioBytes, token)
                Next
                AppLogger.Log($"[STT-CONCURRENCY] Warm-up complete")

                ' Test each concurrency level
                For Each level In concurrencyLevels
                    token.ThrowIfCancellationRequested()
                    RaiseProgress($"Testing {level} concurrent sessions ({iterationsPerLevel} iterations)...")
                    AppLogger.Log($"[STT-CONCURRENCY] ── Level {level} ──")

                    Dim levelResult = Await TestConcurrencyLevel(port, audioBytes, level, iterationsPerLevel, token)
                    result.Levels.Add(levelResult)

                    AppLogger.Log($"[STT-CONCURRENCY]   Wall: {levelResult.WallTimeMs}ms, Avg: {levelResult.AvgLatencyMs}ms, " &
                                  $"Max: {levelResult.MaxLatencyMs}ms, Throughput: {levelResult.InferencesPerSec:F1}/s, " &
                                  $"Errors: {levelResult.Errors}")
                Next

                ' Stop resource monitoring and attach report
                result.Resources = monitor.Stop()
                AppLogger.Log($"[STT-CONCURRENCY] Resources: {result.Resources.ToSummaryText()}")

                RaiseProgress("Concurrent throughput test complete.")

            Catch ex As OperationCanceledException When token.IsCancellationRequested
                Throw
            Catch ex As Exception
                result.ErrorMessage = ex.Message
            Finally
                If proc IsNot Nothing AndAlso Not proc.HasExited Then
                    Try
                        proc.Kill(entireProcessTree:=True)
                        proc.WaitForExit(5000)
                    Catch
                    End Try
                End If
                proc?.Dispose()
            End Try

            Return result
        End Function

        Private Async Function TestConcurrencyLevel(
            port As Integer, audioBytes As Byte(),
            concurrency As Integer, iterations As Integer,
            ct As CancellationToken
        ) As Task(Of ConcurrencyLevelResult)

            Dim levelResult As New ConcurrencyLevelResult() With {
                .Concurrency = concurrency
            }
            Dim allLatencies As New Collections.Concurrent.ConcurrentBag(Of Long)()
            Dim errorCount As Integer = 0
            Dim totalRequests = concurrency * iterations

            ' Scale timeout: at least 120s, plus 5s per concurrent request
            Dim timeoutSec = Math.Max(120, totalRequests * 5)

            Dim wallSw = Stopwatch.StartNew()

            ' Run `iterations` rounds, each round fires `concurrency` parallel requests
            For round = 1 To iterations
                ct.ThrowIfCancellationRequested()

                Dim tasks As New List(Of Task)()
                For i = 1 To concurrency
                    tasks.Add(Task.Run(Async Function()
                        Dim sw = Stopwatch.StartNew()
                        Try
                            Dim ok = Await PostInferenceAsync(port, audioBytes, ct, timeoutSec)
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                            If Not ok Then Interlocked.Increment(errorCount)
                        Catch ex As OperationCanceledException When ct.IsCancellationRequested
                            Throw ' Real user cancellation
                        Catch ex As Exception
                            sw.Stop()
                            allLatencies.Add(sw.ElapsedMilliseconds)
                            Interlocked.Increment(errorCount)
                            AppLogger.Log($"[STT-CONCURRENCY] Inference error: {ex.Message}")
                        End Try
                    End Function))
                Next

                Await Task.WhenAll(tasks)
            Next

            wallSw.Stop()

            Dim latencyList = allLatencies.ToList()
            latencyList.Sort()

            levelResult.WallTimeMs = wallSw.ElapsedMilliseconds
            levelResult.TotalRequests = totalRequests
            levelResult.Errors = errorCount

            If latencyList.Count > 0 Then
                levelResult.AvgLatencyMs = CLng(latencyList.Average())
                levelResult.MinLatencyMs = latencyList.First()
                levelResult.MaxLatencyMs = latencyList.Last()
                levelResult.P50LatencyMs = Percentile(latencyList, 50)
                levelResult.P95LatencyMs = Percentile(latencyList, 95)
                levelResult.InferencesPerSec = Math.Round(latencyList.Count / (wallSw.ElapsedMilliseconds / 1000.0), 1)
            End If

            Return levelResult
        End Function

        Private Shared Async Function PostInferenceAsync(
            port As Integer, audioBytes As Byte(), ct As CancellationToken,
            Optional timeoutSec As Integer = 120
        ) As Task(Of Boolean)
            Using client As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(timeoutSec)}
                Using formContent As New MultipartFormDataContent()
                    Dim fileContent As New ByteArrayContent(audioBytes)
                    fileContent.Headers.ContentType = New Headers.MediaTypeHeaderValue("audio/wav")
                    formContent.Add(fileContent, "file", "audio.wav")
                    formContent.Add(New StringContent("auto"), "language")
                    formContent.Add(New StringContent("0.0"), "temperature")
                    formContent.Add(New StringContent("0.2"), "temperature_inc")
                    formContent.Add(New StringContent("json"), "response_format")

                    Dim resp = Await client.PostAsync($"http://127.0.0.1:{port}/inference", formContent, ct)
                    Return resp.IsSuccessStatusCode
                End Using
            End Using
        End Function

        Private Shared Function Percentile(sorted As List(Of Long), p As Integer) As Long
            If sorted.Count = 0 Then Return 0
            Dim idx = CInt(Math.Ceiling(p / 100.0 * sorted.Count)) - 1
            Return sorted(Math.Max(0, Math.Min(idx, sorted.Count - 1)))
        End Function

        Private Sub RaiseProgress(msg As String)
            AppLogger.Log($"[STT-CONCURRENCY] {msg}")
            RaiseEvent ProgressChanged(Me, msg)
        End Sub

    End Class

    ' ── Result models ──

    Public Class ConcurrencyTestResult
        Public Property BackendName As String = ""
        Public Property ModelLoadMs As Long = 0
        Public Property ErrorMessage As String = ""
        Public Property Levels As New List(Of ConcurrencyLevelResult)()
        Public Property Resources As ResourceReport
    End Class

    Public Class ConcurrencyLevelResult
        Public Property Concurrency As Integer
        Public Property TotalRequests As Integer
        Public Property WallTimeMs As Long
        Public Property AvgLatencyMs As Long
        Public Property MinLatencyMs As Long
        Public Property MaxLatencyMs As Long
        Public Property P50LatencyMs As Long
        Public Property P95LatencyMs As Long
        Public Property InferencesPerSec As Double
        Public Property Errors As Integer
    End Class

End Namespace
