Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline
    ''' <summary>
    ''' CometKiwi quality-estimation sidecar wrapper (qe-server/server.py).
    ''' Reference-free scoring: (source, translation) → ~0..1 on ONE scale that
    ''' is roughly comparable across language pairs — the cross-pair ruler chrF
    ''' cannot be. Used by the benchmark's Pair A/B tab; CPU-only inference.
    ''' </summary>
    Public Class QeService
        Implements IDisposable

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(15)
        }

        Private ReadOnly _host As New PythonSidecarHost() With {
            .Label = "QE server",
            .MaxRestarts = 1,
            .LogFileName = "qe-server.log",
            .BaseEventId = Services.Infrastructure.LogEvents.PYLOG_QE
        }

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _host.IsRunning
            End Get
        End Property

        ''' <summary>App-local HF cache holding the CometKiwi checkpoint (populated at install time).</summary>
        Public Shared ReadOnly Property ModelCacheDir As String
            Get
                Return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qe-model")
            End Get
        End Property

        ''' <summary>
        ''' True when the comet package imports AND the model cache has content —
        ''' both are delivered by the Download Manager's CometKiwi install.
        ''' </summary>
        Public Shared Function CheckInstalled() As Boolean
            If Not Directory.Exists(ModelCacheDir) OrElse
               Directory.GetFiles(ModelCacheDir, "*.ckpt", SearchOption.AllDirectories).Length = 0 Then
                Return False
            End If
            Dim pythonPath = ProcessHelper.FindPython()
            If String.IsNullOrEmpty(pythonPath) Then Return False
            Try
                Dim psi As New Diagnostics.ProcessStartInfo() With {
                    .FileName = pythonPath,
                    .Arguments = "-c ""import comet""",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True
                }
                Using proc = Diagnostics.Process.Start(psi)
                    ' Drain both pipes before WaitForExit to prevent pipe buffer deadlock
                    Dim stderrTask = proc.StandardError.ReadToEndAsync()
                    proc.StandardOutput.ReadToEnd()
                    stderrTask.Wait()
                    proc.WaitForExit(20000)
                    Return proc.ExitCode = 0
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.BENCH_ERROR, $"QeService.CheckInstalled: {ex.Message}")
                Return False
            End Try
        End Function

        Public Sub Start(port As Integer)
            If _host.IsRunning Then Return
            _host.Port = port
            Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "qe-server", "server.py")
            _host.Start(serverScript, $"--cache-dir ""{ModelCacheDir}""")
        End Sub

        ''' <summary>Ask the server to load the model (idempotent; first load ~1 min on CPU).</summary>
        Public Async Function EnsureModelLoadedAsync(ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim response = Await _httpClient.PostAsync(
                    $"http://127.0.0.1:{_host.Port}/load", New StringContent("", Encoding.UTF8, "application/json"), ct)
                If Not response.IsSuccessStatusCode Then Return False
                Using doc = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
                    Return doc.RootElement.GetProperty("model_loaded").GetBoolean()
                End Using
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                ' QE readiness probe — the caller treats False as "unavailable".
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Score parallel (source, translation) arrays. Returns per-sentence
        ''' scores (~0..1, higher = better) or Nothing on failure.
        ''' </summary>
        Public Async Function ScoreAsync(sources As IReadOnlyList(Of String),
                                         translations As IReadOnlyList(Of String),
                                         ct As CancellationToken) As Task(Of List(Of Double))
            Dim payload = JsonSerializer.Serialize(New With {
                .sources = sources, .translations = translations})
            Dim response = Await _httpClient.PostAsync(
                $"http://127.0.0.1:{_host.Port}/score",
                New StringContent(payload, Encoding.UTF8, "application/json"), ct)
            If Not response.IsSuccessStatusCode Then
                AppLogger.Log(LogEvents.BENCH_ERROR, $"QE /score HTTP {CInt(response.StatusCode)}")
                Return Nothing
            End If
            Using doc = JsonDocument.Parse(Await response.Content.ReadAsStringAsync())
                Dim scoresEl As JsonElement = Nothing
                If Not doc.RootElement.TryGetProperty("scores", scoresEl) Then
                    Dim errEl As JsonElement = Nothing
                    If doc.RootElement.TryGetProperty("error", errEl) Then
                        AppLogger.Log(LogEvents.BENCH_ERROR, $"QE score error: {errEl.GetString()}")
                    End If
                    Return Nothing
                End If
                Return scoresEl.EnumerateArray().Select(Function(e) e.GetDouble()).ToList()
            End Using
        End Function

        Public Sub [Stop]()
            _host.Stop()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            _host.Dispose()
        End Sub

    End Class
End Namespace
