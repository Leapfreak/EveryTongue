' PythonSidecarHost.vb — Shared lifecycle manager for Python FastAPI sidecar processes.
' Used by TranslationService, LiveStreamRunner, and MmsTtsBackend.

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Threading
Imports EveryTongue.Services.Infrastructure

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
        Public Property LogFileName As String = Nothing
        Public Property BaseEventId As Integer = LogEvents.PYLOG_LIVE

        ' ── Regex for parsing Python log lines: "2024-01-01 12:00:00,123 INFO message"
        Private Shared ReadOnly _pyLogRegex As New System.Text.RegularExpressions.Regex(
            "^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2},\d{3}\s+(\w+)\s+(.*)$",
            System.Text.RegularExpressions.RegexOptions.Compiled)

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

                Dim logDir = Services.Infrastructure.AppLogger.GetSessionDir()
                Dim args = $"""{scriptPath}"" --port {Port} --log-dir ""{logDir}"""
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

                    AddHandler _process.Exited, Sub(s, e)
                                                    SyncLock _lock
                                                        Dim exitCode = -1
                                                        Try : exitCode = _process?.ExitCode : Catch : End Try
                                                        AppLogger.Log(LogEvents.LEGACY, $"{Label} process exited with code {exitCode}")
                                                        _isRunning = False
                                                        If MaxRestarts > 0 AndAlso _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested Then
                                                            _restartCount += 1
                                                            If _restartCount <= MaxRestarts Then
                                                                Dim delay = Math.Min(5000 * _restartCount, 15000)
                                                                _isRestarting = True
                                                                RaiseEvent StatusMessage(Me, $"{Label} exited (code={exitCode}), restarting in {delay / 1000}s...")
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

                    ' Drain both pipes to nothing — prevents OS pipe buffer deadlock.
                    ' All meaningful logging goes to the Python log file, not the pipe.
                    Dim proc = _process
                    Task.Run(Sub()
                                 Try : proc.StandardOutput.ReadToEnd() : Catch : End Try
                             End Sub)
                    Task.Run(Sub()
                                 Try : proc.StandardError.ReadToEnd() : Catch : End Try
                             End Sub)

                    ' Start file tail to read Python log and raise StderrLine events.
                    ' Record initial file size BEFORE Python starts writing so we don't
                    ' miss startup lines but also skip stale content from previous sessions.
                    If Not String.IsNullOrEmpty(LogFileName) Then
                        Dim logFile = IO.Path.Combine(logDir, LogFileName)
                        Dim initialPos As Long = 0
                        If IO.File.Exists(logFile) Then
                            Try : initialPos = New IO.FileInfo(logFile).Length : Catch : End Try
                        End If
                        Dim ct = _cts.Token
                        Task.Run(Sub() TailLogFile(logFile, initialPos, ct))
                    End If

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

        ' ── File tail ──

        Private Sub TailLogFile(logFile As String, initialPos As Long, ct As CancellationToken)
            Dim lastPos As Long = initialPos
            Try
                ' Wait for the file to appear (Python may take a moment to start)
                Dim waited = 0
                While Not IO.File.Exists(logFile) AndAlso waited < 10000 AndAlso Not ct.IsCancellationRequested
                    Thread.Sleep(250)
                    waited += 250
                End While

                ' Poll loop: reopen file each cycle so we survive RotatingFileHandler rotation.
                ' When Python rotates (live-server.log -> .log.1), the old handle would follow
                ' the renamed file. By reopening, we always read the current file.
                While Not ct.IsCancellationRequested
                    Try
                        If Not IO.File.Exists(logFile) Then
                            ' File may be momentarily missing during rotation
                            Thread.Sleep(250)
                            Continue While
                        End If

                        Dim fi As New IO.FileInfo(logFile)
                        If fi.Length < lastPos Then
                            ' File is smaller than last read — rotation happened, start from beginning
                            lastPos = 0
                        End If

                        If fi.Length > lastPos Then
                            Using fs As New IO.FileStream(logFile, IO.FileMode.Open, IO.FileAccess.Read,
                                                          IO.FileShare.ReadWrite Or IO.FileShare.Delete)
                                fs.Seek(lastPos, IO.SeekOrigin.Begin)
                                Using reader As New IO.StreamReader(fs, Encoding.UTF8)
                                    Dim line = reader.ReadLine()
                                    While line IsNot Nothing
                                        line = line.Trim()
                                        If line.Length > 0 Then
                                            ' Parse Python log line and route to structured logging
                                            ParseAndLogPythonLine(line)
                                            RaiseEvent StderrLine(Me, line)
                                        End If
                                        line = reader.ReadLine()
                                    End While
                                    lastPos = fs.Position
                                End Using
                            End Using
                        End If
                    Catch ex As IO.IOException
                        ' File may be locked during rotation — retry next cycle
                    End Try
                    Thread.Sleep(250)
                End While
            Catch ex As OperationCanceledException
                ' Normal shutdown
            Catch ex As Exception
                AppLogger.Log(LogEvents.PIPELINE_LOG_TAIL_ERROR, $"{Label}: Log tail failed: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Parse a Python log line (format: "2024-01-01 12:00:00,123 LEVEL message")
        ''' and route it through structured logging with the server's BaseEventId.
        ''' </summary>
        Private Sub ParseAndLogPythonLine(line As String)
            Dim m = _pyLogRegex.Match(line)
            If m.Success Then
                Dim levelStr = m.Groups(1).Value.ToUpperInvariant()
                Dim message = m.Groups(2).Value

                ' Map Python level to event ID offset: base+0=Info, +1=Debug, +2=Warning, +3=Error
                Dim eventId = BaseEventId
                Select Case levelStr
                    Case "DEBUG" : eventId = BaseEventId + 1
                    Case "INFO" : eventId = BaseEventId
                    Case "WARNING" : eventId = BaseEventId + 2
                    Case "ERROR", "CRITICAL" : eventId = BaseEventId + 3
                End Select

                AppLogger.Log(eventId, $"{Label}: {message}")
            Else
                ' Non-standard line (e.g. traceback continuation) — log as-is
                AppLogger.Log(BaseEventId, $"{Label}: {line}")
            End If
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
                    AppLogger.Log(LogEvents.PIPELINE_SIDECAR_STOP, $"{Label}: Requesting graceful shutdown on port {Port}...")
                    Using client As New Net.Http.HttpClient() With {.Timeout = TimeSpan.FromSeconds(3)}
                        Dim content As New Net.Http.StringContent("{}", Encoding.UTF8, "application/json")
                        client.PostAsync($"http://127.0.0.1:{Port}{GracefulShutdownPath}", content).Wait(3000)
                    End Using
                Catch ex As Exception
                    AppLogger.Log(LogEvents.PIPELINE_SIDECAR_ERROR, $"{Label}: Graceful shutdown failed, force-killing: {ex.Message}")
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
                    AppLogger.Log(LogEvents.PIPELINE_SIDECAR_ERROR, $"{Label}: Stop kill failed: {ex.Message}")
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
