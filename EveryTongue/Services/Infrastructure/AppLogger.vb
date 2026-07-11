' AppLogger.vb — Centralized logging (file + optional UI callback)
' Decoupled from FormMain so any class can log without a form reference.
'
' All log calls use structured event IDs:
'   AppLogger.Log(LogEvents.PIPELINE_SIDECAR_STARTED, "Started on port 5091")

Imports System.Collections.Concurrent

Namespace Services.Infrastructure

    ''' <summary>
    ''' Structured log entry passed to the UI callback.
    ''' </summary>
    Public Class LogEntry
        Public Time As DateTime
        Public EventId As Integer       ' 0 = legacy (unstructured)
        Public Category As LogCategory
        Public Level As LogSeverity
        Public Source As String         ' Category tag or legacy source (e.g. "Server", "Debug")
        Public Message As String
        Public Color As Drawing.Color
    End Class

    Public Module AppLogger

        ''' <summary>
        ''' Optional callback for routing log messages to the UI.
        ''' Set by FormMain during Load; Nothing before that (logs still go to file).
        ''' </summary>
        Public UiCallback As Action(Of LogEntry)

        ''' <summary>
        ''' Callback to open the Download Manager. Set by FormMain during Load.
        ''' </summary>
        Public OpenDownloadManager As Action

        ''' <summary>
        ''' Active routing configuration. Loaded from AppConfig on startup.
        ''' Defaults to Normal preset until config is loaded.
        ''' </summary>
        Public Routing As LogRoutingConfig = LogRoutingConfig.CreateNormal()

        ' ── File write lock ──
        Private ReadOnly _fileLock As New Object
        Private ReadOnly _sessionTimestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")

        ' ── Session tracking ──
        Private ReadOnly _sessionStart As DateTime = DateTime.Now
        Private ReadOnly _categoryCounts As New ConcurrentDictionary(Of LogCategory, Integer)
        Private _totalEventCount As Integer = 0
        Private _errorCount As Integer = 0

        ' ── Rate limiter state ──
        Private ReadOnly _rateCounts As New ConcurrentDictionary(Of Integer, RateState)
        Private Const RATE_WINDOW_MS As Integer = 10000  ' 10-second window
        Private Const RATE_THRESHOLD As Integer = 5       ' After 5 identical events, start collapsing

        Private Class RateState
            Public Count As Integer
            Public WindowStart As Long  ' Stopwatch ticks
            Public LastMessage As String
            ' Display context captured per call so the suppression summary can format
            ' without a registry lookup (bridged logs have no registered event ID).
            Public Category As LogCategory
            Public Level As LogSeverity
            Public DisplayId As Integer
            Public HasId As Boolean
        End Class

        ''' <summary>
        ''' Shows a standard "missing dependencies" dialog pointing to the Download Manager.
        ''' Call from any controller when deps are missing.
        ''' </summary>
        Public Sub PromptDownloadManager(message As String, title As String)
            Log(LogEvents.DL_CHECK_RESULT, message)
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

        ''' <summary>
        ''' Configured logs directory. Set from AppConfig.LogsDirectory during startup.
        ''' Defaults to .\logs (program directory) until config is loaded.
        ''' </summary>
        Public LogDirectory As String = ""

        Public Function GetLogDir() As String
            Dim dir As String
            If Not String.IsNullOrEmpty(LogDirectory) Then
                If IO.Path.IsPathRooted(LogDirectory) Then
                    dir = IO.Path.GetFullPath(LogDirectory)
                Else
                    dir = IO.Path.GetFullPath(IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogDirectory))
                End If
            Else
                dir = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")
            End If
            If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)
            Return dir
        End Function

        ''' <summary>
        ''' Returns the per-session subdirectory inside GetLogDir().
        ''' Python sidecars also write their logs here via --log-dir.
        ''' </summary>
        Public Function GetSessionDir() As String
            Dim dir = IO.Path.Combine(GetLogDir(), _sessionTimestamp)
            If Not IO.Directory.Exists(dir) Then IO.Directory.CreateDirectory(dir)
            Return dir
        End Function

        Public Function GetLogPath() As String
            Return IO.Path.Combine(GetSessionDir(), "session.log")
        End Function

        ' ─── Structured log (new) ───────────────────────────────────────

        ''' <summary>
        ''' Log a structured event by ID. The event must be registered in LogEvents.
        ''' Routing (file vs UI) is controlled by the active LogRoutingConfig.
        ''' </summary>
        Public Sub Log(eventId As Integer, message As String)
            Dim info = LogEventRegistry.Lookup(eventId)
            If info Is Nothing Then
                ' Unregistered event — log as Legacy category
                Try
                    SyncLock _fileLock
                        Dim logPath = GetLogPath()
                        IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [?{eventId}] {message}{Environment.NewLine}")
                    End SyncLock
                Catch
                End Try
                Return
            End If

            LogCore(eventId, eventId, True, info.Category, info.DefaultLevel, message)
        End Sub

        ''' <summary>
        ''' Log with an explicit category + severity but no registered event ID. Used by the
        ''' ILogger→AppLogger bridge so framework/service logs route to their REAL category
        ''' (filterable, correct severity) and rate-limit per-category, instead of all
        ''' collapsing onto the event-0 [Legacy] catch-all.
        ''' </summary>
        Public Sub Log(category As LogCategory, level As LogSeverity, message As String)
            LogCore(BridgeRateKey(category), 0, False, category, level, message)
        End Sub

        ' Bridge rate-limit keys sit above the event-ID space so they never collide with
        ' registered events; one counter per category (a noisy area collapses on its own).
        Private Function BridgeRateKey(category As LogCategory) As Integer
            Return 1000000 + CInt(category)
        End Function

        ' Events never collapsed by the rate limiter — they ARE the product/transcript, and
        ' each line is unique text we must not lose (translation output is bounded by speech
        ' rate, so it can't truly runaway).
        Private ReadOnly _rateExempt As New HashSet(Of Integer) From {LogEvents.TRANS_RESULT, LogEvents.TRANS_SHADOW}

        Private Function IsRateExempt(rateKey As Integer) As Boolean
            Return _rateExempt.Contains(rateKey)
        End Function

        ''' <summary>Shared write path for structured (hasId=True) and bridged (hasId=False) logs.</summary>
        Private Sub LogCore(rateKey As Integer, displayId As Integer, hasId As Boolean,
                            category As LogCategory, level As LogSeverity, message As String)
            Dim routing = AppLogger.Routing

            ' Track session statistics
            Threading.Interlocked.Increment(_totalEventCount)
            _categoryCounts.AddOrUpdate(category, 1, Function(k, v) v + 1)
            If level >= LogSeverity.[Error] Then Threading.Interlocked.Increment(_errorCount)

            ' Rate limiting: collapse repeated events. Exempt events are the actual product /
            ' transcript (e.g. translation output) — collapsing them would silently DROP unique
            ' text we logged on purpose, so they always pass through.
            If Not IsRateExempt(rateKey) AndAlso CheckRateLimit(rateKey, displayId, hasId, category, level, message) Then Return

            Dim levelTag = level.ToString().ToUpperInvariant()
            Dim catTag = category.ToString()
            Dim idTag = If(hasId, $"[{displayId}] ", "")
            Dim formatted = $"{idTag}[{catTag}:{levelTag}] {message}"

            ' Write to file if routing allows
            If routing.ShouldLogToFile(category, level) Then
                Try
                    SyncLock _fileLock
                        Dim logPath = GetLogPath()
                        IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {formatted}{Environment.NewLine}")
                    End SyncLock
                Catch ex As Exception
                    ' Surface file-write errors to UI so they don't vanish silently
                    Try
                        Dim cb = UiCallback
                        If cb IsNot Nothing Then
                            cb(New LogEntry With {
                                .Time = DateTime.Now,
                                .EventId = 0,
                                .Category = LogCategory.Legacy,
                                .Level = LogSeverity.[Error],
                                .Source = "AppLogger",
                                .Message = $"[LOG FILE ERROR] {ex.GetType().Name}: {ex.Message} (path={GetLogPath()})",
                                .Color = Drawing.Color.Red
                            })
                        End If
                    Catch
                    End Try
                End Try
            End If

            ' Route to UI if routing allows
            If routing.ShouldLogToUi(category, level) Then
                Try
                    Dim cb = UiCallback
                    If cb IsNot Nothing Then
                        Dim color = GetColorForLevel(level, category)
                        cb(New LogEntry With {
                            .Time = DateTime.Now,
                            .EventId = displayId,
                            .Category = category,
                            .Level = level,
                            .Source = catTag,
                            .Message = formatted,
                            .Color = color
                        })
                    End If
                Catch
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Check rate limit for an event. Returns True if the event should be suppressed.
        ''' When the rate window expires, emits a summary line "(×N in last 10s)".
        ''' </summary>
        Private Function CheckRateLimit(rateKey As Integer, displayId As Integer, hasId As Boolean,
                                        category As LogCategory, level As LogSeverity, message As String) As Boolean
            Dim now = Environment.TickCount64
            Dim state = _rateCounts.GetOrAdd(rateKey, Function(id) New RateState With {
                .Count = 0,
                .WindowStart = now,
                .LastMessage = ""
            })

            SyncLock state
                ' Capture display context so the summary can format without a registry lookup.
                state.Category = category
                state.Level = level
                state.DisplayId = displayId
                state.HasId = hasId

                ' Window expired — flush and reset
                If now - state.WindowStart > RATE_WINDOW_MS Then
                    If state.Count > RATE_THRESHOLD Then
                        ' Emit the suppression summary
                        EmitRateSummary(state)
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

        Private Sub EmitRateSummary(state As RateState)
            Dim suppressed = state.Count - RATE_THRESHOLD
            Dim msg = $"{state.LastMessage} ({ChrW(&H00D7)}{state.Count} in last {RATE_WINDOW_MS / 1000}s, {suppressed} suppressed)"
            Dim catTag = state.Category.ToString()
            Dim idTag = If(state.HasId, $"[{state.DisplayId}] ", "")
            Dim formatted = $"{idTag}[{catTag}:INFO] {msg}"

            Try
                SyncLock _fileLock
                    Dim logPath = GetLogPath()
                    IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {formatted}{Environment.NewLine}")
                End SyncLock
            Catch
            End Try

            Try
                Dim cb = UiCallback
                If cb IsNot Nothing Then
                    cb(New LogEntry With {
                        .Time = DateTime.Now,
                        .EventId = state.DisplayId,
                        .Category = state.Category,
                        .Level = LogSeverity.Info,
                        .Source = catTag,
                        .Message = formatted,
                        .Color = Drawing.Color.FromArgb(180, 140, 60)
                    })
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

        ' Legacy Log(msg) overload removed — all calls now use Log(eventId, message).

        ' ─── Session summary ────────────────────────────────────────────

        ''' <summary>
        ''' Emits a session summary event with event counts by category, errors, and duration.
        ''' Call during application shutdown.
        ''' </summary>
        Public Sub EmitSessionSummary()
            Dim duration = DateTime.Now - _sessionStart
            Dim parts As New List(Of String)
            parts.Add($"Duration={duration.Hours}h{duration.Minutes}m{duration.Seconds}s")
            parts.Add($"TotalEvents={_totalEventCount}")
            parts.Add($"Errors={_errorCount}")

            ' Top categories by count
            Dim topCats = _categoryCounts.OrderByDescending(Function(kv) kv.Value).Take(5)
            For Each kv In topCats
                parts.Add($"{kv.Key}={kv.Value}")
            Next

            Log(LogEvents.STARTUP_SESSION_SUMMARY, String.Join(", ", parts))
        End Sub

        ' ─── Cleanup ────────────────────────────────────────────────────

        Public Sub CleanupOldLogFiles(keepDays As Integer)
            Try
                Dim logDir = GetLogDir()
                Dim cutoff = DateTime.Now.AddDays(-keepDays)

                ' Clean old session directories
                For Each d In IO.Directory.GetDirectories(logDir, "????????_??????")
                    If IO.Directory.GetLastWriteTime(d) < cutoff Then
                        IO.Directory.Delete(d, True)
                    End If
                Next

                ' Clean legacy flat log files (pre-session-directory format)
                For Each f In IO.Directory.GetFiles(logDir, "????????*.log")
                    If IO.File.GetLastWriteTime(f) < cutoff Then
                        IO.File.Delete(f)
                    End If
                Next

                ' Clean Python rotated log files (e.g. live-server.log.1, .log.2)
                For Each f In IO.Directory.GetFiles(logDir, "*-server.log*")
                    If IO.File.GetLastWriteTime(f) < cutoff Then
                        IO.File.Delete(f)
                    End If
                Next
            Catch
            End Try

            ' Clean up legacy logs from the old location (install directory)
            Try
                Dim oldDir = AppDomain.CurrentDomain.BaseDirectory
                For Each f In IO.Directory.GetFiles(oldDir, "????????*.log")
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
