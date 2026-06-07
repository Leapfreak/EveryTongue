' AppLogger.vb — Centralized logging (file + optional UI callback)
' Decoupled from FormMain so any class can log without a form reference.
'
' Two calling patterns:
'   AppLogger.Log(LogEvents.PIPELINE_SIDECAR_STARTED, "Started on port 5091")  ← new (structured)
'   AppLogger.Log("[Pipeline] Started on port 5091")                            ← legacy (still works)

Imports System.Collections.Concurrent

Namespace Services.Infrastructure

    Public Module AppLogger

        ''' <summary>
        ''' Optional callback for routing log messages to the UI.
        ''' Signature: (source As String, message As String, color As Drawing.Color).
        ''' Set by FormMain during Load; Nothing before that (logs still go to file).
        ''' </summary>
        Public UiCallback As Action(Of String, String, Drawing.Color)

        ''' <summary>
        ''' Callback to open the Download Manager. Set by FormMain during Load.
        ''' </summary>
        Public OpenDownloadManager As Action

        ''' <summary>
        ''' Active routing configuration. Loaded from AppConfig on startup.
        ''' Defaults to Normal preset until config is loaded.
        ''' </summary>
        Public Routing As LogRoutingConfig = LogRoutingConfig.CreateNormal()

        ' ── Rate limiter state ──
        Private ReadOnly _rateCounts As New ConcurrentDictionary(Of Integer, RateState)
        Private Const RATE_WINDOW_MS As Integer = 10000  ' 10-second window
        Private Const RATE_THRESHOLD As Integer = 5       ' After 5 identical events, start collapsing

        Private Class RateState
            Public Count As Integer
            Public WindowStart As Long  ' Stopwatch ticks
            Public LastMessage As String
        End Class

        ''' <summary>
        ''' Shows a standard "missing dependencies" dialog pointing to the Download Manager.
        ''' Call from any controller when deps are missing.
        ''' </summary>
        Public Sub PromptDownloadManager(message As String, title As String)
            Log($"[WARN] {message}")
            Dim result = System.Windows.Forms.MessageBox.Show(
                message,
                title,
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning)
            If result = System.Windows.Forms.DialogResult.Yes Then
                Dim cb = OpenDownloadManager
                cb?.Invoke()
            End If
        End Sub

        Public Function GetLogDir() As String
            Dim dir = IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveryTongue", "logs")
            If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)
            Return dir
        End Function

        Public Function GetLogPath() As String
            Return IO.Path.Combine(GetLogDir(), $"{DateTime.Now:yyyyMMdd}.log")
        End Function

        ' ─── Structured log (new) ───────────────────────────────────────

        ''' <summary>
        ''' Log a structured event by ID. The event must be registered in LogEvents.
        ''' Routing (file vs UI) is controlled by the active LogRoutingConfig.
        ''' </summary>
        Public Sub Log(eventId As Integer, message As String)
            Dim info = LogEventRegistry.Lookup(eventId)
            If info Is Nothing Then
                ' Unregistered event — treat as legacy
                Log($"[?{eventId}] {message}")
                Return
            End If

            Dim category = info.Category
            Dim level = info.DefaultLevel
            Dim routing = AppLogger.Routing

            ' Rate limiting: collapse repeated events
            Dim suppressed = CheckRateLimit(eventId, message)
            If suppressed Then Return

            Dim levelTag = level.ToString().ToUpperInvariant()
            Dim catTag = category.ToString()
            Dim formatted = $"[{eventId}] [{catTag}:{levelTag}] {message}"

            ' Write to file if routing allows
            If routing.ShouldLogToFile(category, level) Then
                Try
                    Dim logPath = GetLogPath()
                    IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {formatted}{Environment.NewLine}")
                Catch
                End Try
            End If

            ' Route to UI if routing allows
            If routing.ShouldLogToUi(category, level) Then
                Try
                    Dim cb = UiCallback
                    If cb IsNot Nothing Then
                        Dim color = GetColorForLevel(level, category)
                        cb(catTag, formatted, color)
                    End If
                Catch
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Check rate limit for an event. Returns True if the event should be suppressed.
        ''' When the rate window expires, emits a summary line "(×N in last 10s)".
        ''' </summary>
        Private Function CheckRateLimit(eventId As Integer, message As String) As Boolean
            Dim now = Environment.TickCount64
            Dim state = _rateCounts.GetOrAdd(eventId, Function(id) New RateState With {
                .Count = 0,
                .WindowStart = now,
                .LastMessage = ""
            })

            SyncLock state
                ' Window expired — flush and reset
                If now - state.WindowStart > RATE_WINDOW_MS Then
                    If state.Count > RATE_THRESHOLD Then
                        ' Emit the suppression summary
                        EmitRateSummary(eventId, state)
                    End If
                    state.Count = 1
                    state.WindowStart = now
                    state.LastMessage = message
                    Return False
                End If

                state.Count += 1
                state.LastMessage = message

                ' Under threshold — let through
                If state.Count <= RATE_THRESHOLD Then
                    Return False
                End If

                ' Over threshold — suppress (summary emitted when window expires)
                Return True
            End SyncLock
        End Function

        Private Sub EmitRateSummary(eventId As Integer, state As RateState)
            Dim info = LogEventRegistry.Lookup(eventId)
            If info Is Nothing Then Return
            Dim suppressed = state.Count - RATE_THRESHOLD
            Dim msg = $"{state.LastMessage} ({ChrW(&H00D7)}{state.Count} in last {RATE_WINDOW_MS / 1000}s, {suppressed} suppressed)"
            Dim catTag = info.Category.ToString()
            Dim formatted = $"[{eventId}] [{catTag}:INFO] {msg}"

            Try
                Dim logPath = GetLogPath()
                IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {formatted}{Environment.NewLine}")
            Catch
            End Try

            Try
                Dim cb = UiCallback
                If cb IsNot Nothing Then
                    cb(catTag, formatted, Drawing.Color.FromArgb(180, 140, 60))
                End If
            Catch
            End Try
        End Sub

        Private Function GetColorForLevel(level As LogSeverity, category As LogCategory) As Drawing.Color
            Select Case level
                Case LogSeverity.[Error]
                    Return Drawing.Color.Red
                Case LogSeverity.Warning
                    Return Drawing.Color.Orange
                Case LogSeverity.Debug
                    Return Drawing.Color.FromArgb(120, 120, 120)
                Case Else
                    ' Info — color by category
                    Select Case category
                        Case LogCategory.Server : Return Drawing.Color.FromArgb(0, 120, 180)
                        Case LogCategory.Stt, LogCategory.Pipeline : Return Drawing.Color.FromArgb(0, 140, 80)
                        Case LogCategory.Conference : Return Drawing.Color.FromArgb(140, 80, 200)
                        Case LogCategory.Translation : Return Drawing.Color.FromArgb(200, 120, 0)
                        Case LogCategory.Tts : Return Drawing.Color.FromArgb(0, 160, 160)
                        Case LogCategory.Rooms : Return Drawing.Color.FromArgb(80, 120, 200)
                        Case LogCategory.PythonLog : Return Drawing.Color.FromArgb(100, 140, 100)
                        Case Else : Return Drawing.Color.FromArgb(100, 100, 100)
                    End Select
            End Select
        End Function

        ' ─── Legacy log (unchanged API) ─────────────────────────────────

        ''' <summary>
        ''' Legacy log method. Routes through the structured system as Legacy category.
        ''' Will be removed once all 334 call sites are migrated to use event IDs.
        ''' </summary>
        Public Sub Log(msg As String)
            Dim routing = AppLogger.Routing

            ' Write to file (legacy always goes to file at Info level)
            If routing.ShouldLogToFile(LogCategory.Legacy, LogSeverity.Info) Then
                Try
                    Dim logPath = GetLogPath()
                    IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}{Environment.NewLine}")
                Catch
                End Try
            End If

            ' Route to UI if callback is set
            If routing.ShouldLogToUi(LogCategory.Legacy, LogSeverity.Info) Then
                Try
                    Dim cb = UiCallback
                    If cb IsNot Nothing Then
                        Dim source = "Debug"
                        If msg.StartsWith("[Server]") Then
                            source = "Server"
                        ElseIf msg.StartsWith("[Live]") Then
                            source = "Live"
                        End If

                        Dim color As Drawing.Color
                        If msg.Contains("[ERROR]") Then
                            color = Drawing.Color.Red
                        ElseIf msg.Contains("[WARN]") Then
                            color = Drawing.Color.Orange
                        Else
                            Select Case source
                                Case "Server" : color = Drawing.Color.FromArgb(0, 120, 180)
                                Case "Live" : color = Drawing.Color.FromArgb(0, 140, 80)
                                Case Else : color = Drawing.Color.FromArgb(100, 100, 100)
                            End Select
                        End If
                        cb(source, msg, color)
                    End If
                Catch
                End Try
            End If
        End Sub

        ' ─── Cleanup ────────────────────────────────────────────────────

        Public Sub CleanupOldLogFiles(keepDays As Integer)
            Try
                Dim logDir = GetLogDir()
                Dim cutoff = DateTime.Now.AddDays(-keepDays)

                ' Clean daily .NET log files older than keepDays
                For Each f In IO.Directory.GetFiles(logDir, "????????.log")
                    If IO.File.GetLastWriteTime(f) < cutoff Then
                        IO.File.Delete(f)
                    End If
                Next

                ' Clean Python rotated log files (e.g. live-server.log.1, .log.2)
                For Each f In IO.Directory.GetFiles(logDir, "*-server.log.*")
                    If IO.File.GetLastWriteTime(f) < cutoff Then
                        IO.File.Delete(f)
                    End If
                Next
            Catch
            End Try

            ' Clean up legacy logs from the old location (install directory)
            Try
                Dim oldDir = AppDomain.CurrentDomain.BaseDirectory
                For Each f In IO.Directory.GetFiles(oldDir, "????????.log")
                    IO.File.Delete(f)
                Next
                For Each f In IO.Directory.GetFiles(oldDir, "*_pipeline-debug.log")
                    IO.File.Delete(f)
                Next
            Catch
            End Try
        End Sub

    End Module

End Namespace
