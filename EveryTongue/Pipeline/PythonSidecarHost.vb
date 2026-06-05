' PythonSidecarHost.vb — Shared lifecycle manager for Python FastAPI sidecar processes.
' Used by TranslationService, LiveStreamRunner, and MmsTtsBackend.

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading

Namespace Pipeline

    Public Class PythonSidecarHost
        Implements IDisposable

        Private _process As Process
        Private _isRunning As Boolean = False
        Private _isRestarting As Boolean = False
        Private _restartCount As Integer = 0
        Private _cts As CancellationTokenSource
        Private ReadOnly _lock As New Object()

        ' ── Configuration (set before calling Start) ──

        Public Property Port As Integer
        Public Property Label As String = "Sidecar"
        Public Property MaxRestarts As Integer = 0
        Public Property AddWhisperToPath As Boolean = False
        Public Property GracefulShutdownPath As String = Nothing

        ' ── Events ──

        Public Event StderrLine As EventHandler(Of String)
        Public Event ProcessExited As EventHandler
        Public Event StatusMessage As EventHandler(Of String)

        ' ── State ──

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning OrElse _isRestarting
            End Get
        End Property

        Public ReadOnly Property IsProcessRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ' ── Start ──

        Public Sub Start(scriptPath As String, extraArgs As String)
            SyncLock _lock
                If _isRunning OrElse _isRestarting Then Return
                _restartCount = 0
                _cts = New CancellationTokenSource()
                StartProcess(scriptPath, extraArgs)
            End SyncLock
        End Sub

        Private _scriptPath As String
        Private _extraArgs As String

        Private Sub StartProcess(scriptPath As String, extraArgs As String)
            SyncLock _lock
                If _isRunning Then Return
                _isRestarting = False
                _scriptPath = scriptPath
                _extraArgs = extraArgs

                _cts?.Cancel()
                _cts = New CancellationTokenSource()

                ProcessHelper.KillProcessOnPort(Port)
                Thread.Sleep(500)

                Dim pythonPath = ProcessHelper.FindPython()
                If String.IsNullOrEmpty(pythonPath) Then
                    RaiseEvent StatusMessage(Me, $"{Label}: Python not found")
                    Return
                End If

                If Not File.Exists(scriptPath) Then
                    RaiseEvent StatusMessage(Me, $"{Label}: {scriptPath} not found")
                    Return
                End If

                Dim args = $"""{scriptPath}"" --port {Port}"
                If Not String.IsNullOrEmpty(extraArgs) Then
                    args &= " " & extraArgs
                End If

                Dim psi As New ProcessStartInfo() With {
                    .FileName = pythonPath,
                    .Arguments = args,
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True,
                    .StandardOutputEncoding = Encoding.UTF8,
                    .StandardErrorEncoding = Encoding.UTF8
                }

                ' Ensure Python writes UTF-8 to stdout/stderr (Windows defaults to cp1252)
                psi.Environment("PYTHONIOENCODING") = "utf-8"
                psi.Environment("PYTHONLEGACYWINDOWSSTDIO") = "0"

                If AddWhisperToPath Then
                    Dim whisperDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whisper")
                    If Directory.Exists(whisperDir) Then
                        Dim currentPath = If(Environment.GetEnvironmentVariable("PATH"), "")
                        psi.Environment("PATH") = whisperDir & ";" & currentPath
                    End If
                End If

                Try
                    _process = New Process()
                    _process.StartInfo = psi
                    _process.EnableRaisingEvents = True

                    AddHandler _process.ErrorDataReceived, Sub(s, e)
                                                               If e.Data IsNot Nothing Then
                                                                   Dim line = e.Data.Trim()
                                                                   If line.Length > 0 Then
                                                                       RaiseEvent StderrLine(Me, line)
                                                                   End If
                                                               End If
                                                           End Sub

                    AddHandler _process.Exited, Sub(s, e)
                                                    SyncLock _lock
                                                        _isRunning = False
                                                        If MaxRestarts > 0 AndAlso _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested Then
                                                            _restartCount += 1
                                                            If _restartCount <= MaxRestarts Then
                                                                Dim delay = Math.Min(5000 * _restartCount, 15000)
                                                                _isRestarting = True
                                                                RaiseEvent StatusMessage(Me, $"{Label} exited, restarting in {delay / 1000}s...")
                                                                Task.Delay(delay).ContinueWith(
                                                                    Sub(t)
                                                                        If _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested Then
                                                                            StartProcess(_scriptPath, _extraArgs)
                                                                        Else
                                                                            _isRestarting = False
                                                                        End If
                                                                    End Sub)
                                                            Else
                                                                RaiseEvent StatusMessage(Me, $"{Label} failed too many times, giving up")
                                                                RaiseEvent ProcessExited(Me, EventArgs.Empty)
                                                            End If
                                                        Else
                                                            RaiseEvent ProcessExited(Me, EventArgs.Empty)
                                                        End If
                                                    End SyncLock
                                                End Sub

                    _process.Start()
                    _process.BeginErrorReadLine()

                    ' Drain stdout to prevent deadlock
                    Dim proc = _process
                    Task.Run(Sub()
                                 Try : proc.StandardOutput.ReadToEnd() : Catch : End Try
                             End Sub)

                    _isRunning = True
                    _restartCount = If(_restartCount > 0, _restartCount, 0)
                    RaiseEvent StatusMessage(Me, $"{Label} starting on port {Port}")

                Catch ex As Exception
                    _isRunning = False
                    _isRestarting = False
                    RaiseEvent StatusMessage(Me, $"{Label} failed to start: {ex.Message}")
                End Try
            End SyncLock
        End Sub

        ' ── Stop ──

        Public Sub [Stop](Optional waitMs As Integer = 5000)
            _cts?.Cancel()

            ' Nothing to stop if no process is running
            If _process Is Nothing OrElse _process.HasExited Then
                SyncLock _lock
                    _isRestarting = False
                    _isRunning = False
                    _process = Nothing
                End SyncLock
                Return
            End If

            ' Try graceful HTTP shutdown first
            If GracefulShutdownPath IsNot Nothing Then
                Try
                    Services.Infrastructure.AppLogger.Log($"[{Label}] Requesting graceful shutdown on port {Port}...")
                    Using client As New Net.Http.HttpClient() With {.Timeout = TimeSpan.FromSeconds(3)}
                        Dim content As New Net.Http.StringContent("{}", Encoding.UTF8, "application/json")
                        client.PostAsync($"http://127.0.0.1:{Port}{GracefulShutdownPath}", content).Wait(3000)
                    End Using
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log($"[{Label}] Graceful shutdown failed, force-killing: {ex.Message}")
                End Try
            End If

            SyncLock _lock
                _isRestarting = False
                Try
                    If _process IsNot Nothing AndAlso Not _process.HasExited Then
                        _process.Kill(True)
                        _process.WaitForExit(waitMs)
                    End If
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[{Label}] Stop kill failed: {ex.Message}")
                End Try
                _isRunning = False
                _process = Nothing
            End SyncLock
        End Sub

        Public Sub ResetRestartCount()
            _restartCount = 0
        End Sub

        Public Sub CancelCts()
            _cts?.Cancel()
        End Sub

        Public ReadOnly Property CancellationToken As CancellationToken
            Get
                Return If(_cts?.Token, CancellationToken.None)
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
            _cts?.Dispose()
        End Sub

    End Class

End Namespace
