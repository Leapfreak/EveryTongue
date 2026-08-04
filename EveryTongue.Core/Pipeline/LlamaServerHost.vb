Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline
    ''' <summary>
    ''' Hosts a native llama-server.exe process (llama.cpp, Vulkan build) serving the
    ''' SalamandraTA GGUF model over local HTTP for SalamandraTranslationBackend.
    '''
    ''' DELIBERATELY self-contained rather than a refactor of PythonSidecarHost:
    ''' that host underpins the live-server, translate-server, MMS-TTS and QE
    ''' sidecars and is python-specific in three load-bearing ways (FindPython as
    ''' the unconditional FileName, the --port/--log-dir arg template, and the
    ''' rotating-python-logfile tail). Destabilising it to add one native server is
    ''' out of scope; if a THIRD process-host species ever appears, extract the
    ''' shared supervision core then (audit-clones note: shapes are shared by
    ''' design, blocks are not copied).
    '''
    ''' Behaviours copied from house patterns:
    '''   - both pipes drained line-by-line (Windows 4KB pipe deadlock rule)
    '''   - KillProcessOnPort before launch (PythonSidecarHost.StartProcess)
    '''   - health poll aborting early when the process exits (SttConcurrencyRunner)
    '''   - deliberate-stop vs crash classification + bounded restart
    '''   - PythonSidecarHost.GlobalShutdown respected (no resurrection at exit)
    '''   - warm-up at load (TranslationService.WarmUpModel), here in TWO shapes:
    '''     one short and one context-length generation, because the first LARGE
    '''     prompt pays a one-time Vulkan shader compile (~5s measured on Jezer)
    '''     that must never land on the first live contextful commit.
    '''   - the Vulkan device line is parsed and logged LOUDLY; silent CPU fallback
    '''     was observed in the field (2026-08-04) and must be visible (4017).
    ''' </summary>
    Public Class LlamaServerHost
        Implements IDisposable

        Public Property ExePath As String = ""
        Public Property ModelPath As String = ""
        Public Property Port As Integer = 5097
        Public Property GpuLayers As Integer = 99
        Public Property ContextTokens As Integer = 4096

        Private ReadOnly _lock As New Object()
        Private _process As Process
        Private _startTask As Task(Of Boolean)
        Private _deliberateStop As Boolean
        Private _restartCount As Integer
        Private _modelReady As Boolean
        Private _vulkanDeviceSeen As Boolean
        Private _gpuLayersSeen As Boolean

        Private Shared ReadOnly _httpClient As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}

        ''' <summary>Process alive (a restart in progress also counts as running).</summary>
        Public ReadOnly Property IsRunning As Boolean
            Get
                SyncLock _lock
                    If _process IsNot Nothing AndAlso Not _process.HasExited Then Return True
                    Return _startTask IsNot Nothing AndAlso Not _startTask.IsCompleted
                End SyncLock
            End Get
        End Property

        ''' <summary>True only after health OK AND warm-up completed — gates
        ''' SalamandraTranslationBackend.IsAvailable, so the room readiness probe
        ''' holds "preparing" until the first real commit will actually be fast.</summary>
        Public ReadOnly Property IsModelLoaded As Boolean
            Get
                Return _modelReady
            End Get
        End Property

        ''' <summary>
        ''' Idempotent start: concurrent callers (several rooms starting at once, the
        ''' benchmark, an Options save) all share ONE launch task. Callers may
        ''' fire-and-forget or await. Never blocks the calling thread.
        ''' </summary>
        Public Function EnsureStartedAsync() As Task(Of Boolean)
            SyncLock _lock
                If _modelReady Then Return Task.FromResult(True)
                If _startTask IsNot Nothing AndAlso Not _startTask.IsCompleted Then Return _startTask
                If PythonSidecarHost.GlobalShutdown Then Return Task.FromResult(False)
                _deliberateStop = False
                _startTask = Task.Run(Function() StartAndWarmAsync())
                Return _startTask
            End SyncLock
        End Function

        Private Async Function StartAndWarmAsync() As Task(Of Boolean)
            Try
                If Not File.Exists(ExePath) Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"llama-server.exe not found at {ExePath} — install 'llama.cpp Server (Vulkan)' via the Download Manager")
                    Return False
                End If
                If Not File.Exists(ModelPath) Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"Salamandra model not found at {ModelPath} — install 'Salamandra Translation Model' via the Download Manager")
                    Return False
                End If

                ProcessHelper.KillProcessOnPort(Port)
                Await Task.Delay(500)

                Dim psi As New ProcessStartInfo() With {
                    .FileName = ExePath,
                    .Arguments = $"-m ""{ModelPath}"" --port {Port} --host 127.0.0.1 -ngl {GpuLayers} -c {ContextTokens} --parallel 1",
                    .WorkingDirectory = Path.GetDirectoryName(ExePath),
                    .UseShellExecute = False,
                    .CreateNoWindow = True,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .StandardOutputEncoding = Text.Encoding.UTF8,
                    .StandardErrorEncoding = Text.Encoding.UTF8
                }

                AppLogger.Log(LogEvents.TRANS_LLAMA, $"starting llama-server: port={Port} ngl={GpuLayers} ctx={ContextTokens} model={Path.GetFileName(ModelPath)}")
                _vulkanDeviceSeen = False
                _gpuLayersSeen = False

                Dim proc As New Process() With {.StartInfo = psi, .EnableRaisingEvents = True}
                AddHandler proc.Exited, AddressOf OnProcessExited
                SyncLock _lock
                    _process = proc
                End SyncLock

                proc.Start()

                ' Drain BOTH pipes line-by-line (4KB pipe rule) and parse as we go —
                ' the Vulkan device / offload lines arrive on stderr during load.
                Dim drainOut = Task.Run(Sub() DrainPipe(Function() proc.StandardOutput.ReadLine()))
                Dim drainErr = Task.Run(Sub() DrainPipe(Function() proc.StandardError.ReadLine()))

                ' Readiness: poll /health, abort early if the process died.
                Dim healthy = False
                Dim deadline = DateTime.UtcNow.AddSeconds(120)
                While DateTime.UtcNow < deadline
                    If proc.HasExited Then
                        AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"llama-server exited during startup (code {proc.ExitCode}) — see 94xx log lines")
                        Return False
                    End If
                    Try
                        Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(3))
                            Dim resp = Await _httpClient.GetAsync($"http://127.0.0.1:{Port}/health", cts.Token)
                            If resp.IsSuccessStatusCode Then healthy = True : Exit While
                        End Using
                    Catch
                        ' not up yet
                    End Try
                    Await Task.Delay(500)
                End While
                If Not healthy Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, "llama-server did not become healthy within 120s — stopping it")
                    [Stop]()
                    Return False
                End If

                ' Field lesson 2026-08-04: Vulkan enumeration occasionally fails cold
                ' and llama silently runs on CPU. Correct output, ~10x slower — say so.
                If Not _vulkanDeviceSeen Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM,
                        "no Vulkan device reported — llama-server is likely running on CPU (~10x slower translations). A restart of the app (or re-selecting the engine) usually recovers the GPU.")
                End If

                ' Warm-up, BOTH pipeline shapes: a short generation compiles the
                ' small-batch path; a context-length one compiles the large-batch
                ' path (the ~5s one-time shader compile observed on PARA-en).
                Dim swShort = Diagnostics.Stopwatch.StartNew()
                Dim okShort = Await WarmupCompletionAsync(
                    "<|im_start|>user" & vbLf & "Translate the following text from English into Spanish." & vbLf &
                    "English: Hello." & vbLf & "Spanish:<|im_end|>" & vbLf & "<|im_start|>assistant" & vbLf, 16)
                swShort.Stop()

                Dim longSrc = String.Join(" ", Enumerable.Repeat("The quick brown fox jumps over the lazy dog near the riverbank while the bells ring.", 12))
                Dim swLong = Diagnostics.Stopwatch.StartNew()
                Dim okLong = Await WarmupCompletionAsync(
                    "<|im_start|>user" & vbLf & "Translate the following text from English into Spanish." & vbLf &
                    "English: " & longSrc & vbLf & "Spanish:<|im_end|>" & vbLf & "<|im_start|>assistant" & vbLf, 24)
                swLong.Stop()

                If Not (okShort AndAlso okLong) Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, "llama-server warm-up failed — backend stays unavailable")
                    [Stop]()
                    Return False
                End If

                SyncLock _lock
                    _modelReady = True
                    _restartCount = 0
                End SyncLock
                AppLogger.Log(LogEvents.TRANS_LLAMA, $"Salamandra ready — warm-up short={swShort.ElapsedMilliseconds}ms, context-length={swLong.ElapsedMilliseconds}ms (large-prompt shader compile paid at load)")
                Return True
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"llama-server start failed: {ex.Message}")
                Return False
            End Try
        End Function

        Private Async Function WarmupCompletionAsync(prompt As String, nPredict As Integer) As Task(Of Boolean)
            Try
                Dim payload = JsonSerializer.Serialize(New With {
                    Key .prompt = prompt, Key .n_predict = nPredict, Key .temperature = 0,
                    Key .stop = New String() {"<|im_end|>"}, Key .cache_prompt = False})
                Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(60))
                    Dim resp = Await _httpClient.PostAsync($"http://127.0.0.1:{Port}/completion",
                        New StringContent(payload, Text.Encoding.UTF8, "application/json"), cts.Token)
                    Return resp.IsSuccessStatusCode
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"warm-up request failed: {ex.Message}")
                Return False
            End Try
        End Function

        Private Sub DrainPipe(readLine As Func(Of String))
            Try
                Do
                    Dim line = readLine()
                    If line Is Nothing Then Exit Do
                    RouteServerLine(line)
                Loop
            Catch
                ' Pipe closes on process exit — normal, nothing to log.
            End Try
        End Sub

        ' b10242 llama-SERVER prefixes every line "0.04.632.054 I content" (elapsed +
        ' level letter) — unlike llama-completion. Field lesson 2026-08-04: this
        ' prefix broke the original StartsWith-based routing AND hid the device line.
        Private Shared ReadOnly _serverLinePrefix As New Text.RegularExpressions.Regex(
            "^\d+[\.\d]*\s+([IWED])\s+", Text.RegularExpressions.RegexOptions.Compiled)

        ''' <summary>Severity-route a llama-server output line, and watch for the
        ''' Vulkan device + layer-offload lines (never silent about the device).</summary>
        Private Sub RouteServerLine(line As String)
            If String.IsNullOrWhiteSpace(line) Then Return
            Dim trimmed = line.Trim()
            Dim level As Char = " "c
            Dim m = _serverLinePrefix.Match(trimmed)
            If m.Success Then
                level = m.Groups(1).Value(0)
                trimmed = trimmed.Substring(m.Length).Trim()
            End If

            ' Device detection: "ggml_vulkan: Found N Vulkan devices" /
            ' "ggml_vulkan: 0 = NVIDIA GeForce ..." — accept any credible shape.
            If trimmed.IndexOf("vulkan", StringComparison.OrdinalIgnoreCase) >= 0 AndAlso
               (trimmed.IndexOf("device", StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                Text.RegularExpressions.Regex.IsMatch(trimmed, "(NVIDIA|GeForce|Radeon|AMD|Intel|Arc)", Text.RegularExpressions.RegexOptions.IgnoreCase)) Then
                _vulkanDeviceSeen = True
                AppLogger.Log(LogEvents.TRANS_LLAMA, $"device: {trimmed}")
                Return
            End If
            Dim offloadIdx = trimmed.IndexOf("offloaded", StringComparison.OrdinalIgnoreCase)
            If Not _gpuLayersSeen AndAlso offloadIdx >= 0 AndAlso trimmed.IndexOf("layers", StringComparison.OrdinalIgnoreCase) > offloadIdx Then
                _gpuLayersSeen = True
                If Text.RegularExpressions.Regex.IsMatch(trimmed, "offloaded\s+0\s*/") Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"0 layers offloaded to GPU — running on CPU: {trimmed}")
                Else
                    ' A successful offload proves the GPU even if the enumeration
                    ' line slipped past — never warn falsely.
                    _vulkanDeviceSeen = True
                    AppLogger.Log(LogEvents.TRANS_LLAMA, $"{trimmed}")
                End If
                Return
            End If

            Select Case level
                Case "E"c
                    AppLogger.Log(LogEvents.LLAMA_SERVER_LOG_ERROR, trimmed)
                Case "W"c
                    AppLogger.Log(LogEvents.LLAMA_SERVER_LOG_WARN, trimmed)
                Case "D"c
                    AppLogger.Log(LogEvents.LLAMA_SERVER_LOG_DEBUG, trimmed)
                Case Else
                    ' No level letter (early ggml/loader banner) or "I": load-time
                    ' lines are Info; per-request slot chatter goes to Debug.
                    If trimmed.StartsWith("slot ") OrElse trimmed.StartsWith("srv ") OrElse
                       trimmed.IndexOf("processing task", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        AppLogger.Log(LogEvents.LLAMA_SERVER_LOG_DEBUG, trimmed)
                    ElseIf trimmed.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        AppLogger.Log(LogEvents.LLAMA_SERVER_LOG_ERROR, trimmed)
                    Else
                        AppLogger.Log(LogEvents.LLAMA_SERVER_LOG, trimmed)
                    End If
            End Select
        End Sub

        Private Sub OnProcessExited(sender As Object, e As EventArgs)
            Dim code = -1
            Try : code = _process.ExitCode : Catch : End Try
            SyncLock _lock
                _modelReady = False
            End SyncLock

            If _deliberateStop OrElse PythonSidecarHost.GlobalShutdown Then
                AppLogger.Log(LogEvents.TRANS_LLAMA, $"llama-server stopped (code {code})")
                Return
            End If

            SyncLock _lock
                _restartCount += 1
                If _restartCount > 1 Then
                    AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM,
                        $"llama-server exited unexpectedly again (code {code}) — giving up. Salamandra is unavailable; the orchestrator falls back to the local NLLB engine when it is running.")
                    Return
                End If
                AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM, $"llama-server exited unexpectedly (code {code}) — restarting once in 5s")
                _startTask = Task.Run(Async Function()
                                          Await Task.Delay(5000)
                                          If PythonSidecarHost.GlobalShutdown OrElse _deliberateStop Then Return False
                                          Return Await StartAndWarmAsync()
                                      End Function)
            End SyncLock
        End Sub

        Public Sub [Stop]()
            SyncLock _lock
                _deliberateStop = True
                _modelReady = False
            End SyncLock
            Try
                Dim proc = _process
                If proc IsNot Nothing AndAlso Not proc.HasExited Then
                    ' llama-server has no graceful-shutdown endpoint worth waiting on.
                    proc.Kill(True)
                    proc.WaitForExit(4000)
                End If
            Catch
                ' Already gone.
            End Try
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
        End Sub
    End Class
End Namespace
