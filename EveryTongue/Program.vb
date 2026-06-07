Imports System.IO

Friend Module Program

    ' Keep the lock file open for the lifetime of the process
    Private _lockStream As FileStream

    Friend ReadOnly CrashSentinelPath As String =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".crash-sentinel")

    <STAThread()>
    Friend Sub Main(args As String())
        ' Single-instance guard: try to exclusively lock a file in AppData
        Dim lockDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EveryTongue")
        Directory.CreateDirectory(lockDir)
        Dim lockPath = Path.Combine(lockDir, ".lock")

        Try
            _lockStream = New FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
        Catch ex As IOException
            MessageBox.Show(Services.Infrastructure.LanguagePackService.Instance.GetString("App_AlreadyRunning"), "Every Tongue", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End Try

        ' Check for previous crash (sentinel file left behind)
        CheckForPreviousCrash()

        ' Write crash sentinel — deleted on clean exit
        WriteCrashSentinel()

        ' Catch unobserved Task exceptions — suppress harmless connection-pool noise
        AddHandler TaskScheduler.UnobservedTaskException, Sub(s, e)
            e.SetObserved()
            If IsHarmlessNetworkException(e.Exception) Then Return
            FormMain.WriteDebugLog($"[UNOBSERVED TASK] {e.Exception}")
        End Sub

        ' Catch unhandled UI-thread exceptions
        AddHandler Application.ThreadException, Sub(s, e)
            LogCrash("[UI THREAD]", e.Exception)
        End Sub

        ' Catch unhandled non-UI-thread exceptions
        AddHandler AppDomain.CurrentDomain.UnhandledException, Sub(s, e)
            Dim ex = TryCast(e.ExceptionObject, Exception)
            LogCrash("[UNHANDLED]", ex)
        End Sub

        Try
            Application.SetHighDpiMode(HighDpiMode.SystemAware)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New FormMain)
        Finally
            ' Clean exit — remove sentinel
            Try : File.Delete(CrashSentinelPath) : Catch : End Try
            _lockStream?.Dispose()
        End Try
    End Sub

    ''' <summary>
    ''' Writes a sentinel file at startup. If this file exists on next launch,
    ''' the previous session crashed without a clean exit (e.g. StackOverflowException,
    ''' AccessViolation, or process kill).
    ''' </summary>
    Private Sub WriteCrashSentinel()
        Try
            Dim ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
            File.WriteAllText(CrashSentinelPath,
                $"PID={Environment.ProcessId}" & Environment.NewLine &
                $"Started={DateTime.Now:yyyy-MM-dd HH:mm:ss}" & Environment.NewLine &
                $"Version={ver.Major}.{ver.Minor}.{ver.Build}")
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' If the sentinel file exists from a previous run, log the crash and notify the user.
    ''' </summary>
    Private Sub CheckForPreviousCrash()
        Try
            If Not File.Exists(CrashSentinelPath) Then Return
            Dim sentinelInfo = File.ReadAllText(CrashSentinelPath)
            File.Delete(CrashSentinelPath)

            ' Write to today's log
            Dim logPath = Services.Infrastructure.AppLogger.GetLogPath()
            Dim msg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [CRASH DETECTED] Previous session did not exit cleanly." &
                      Environment.NewLine &
                      $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [CRASH DETECTED] Sentinel info: {sentinelInfo.Replace(Environment.NewLine, " | ")}" &
                      Environment.NewLine
            File.AppendAllText(logPath, msg)

            MessageBox.Show(
                "Every Tongue did not shut down cleanly last time." &
                Environment.NewLine & "This may indicate a crash. Details have been written to the log file.",
                "Crash Detected", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Writes crash details to the daily log file. Uses direct file I/O as a
    ''' fallback in case AppLogger or FormMain are not yet initialized.
    ''' </summary>
    ''' <summary>
    ''' Returns True if the AggregateException contains only harmless network/cancellation
    ''' errors from HttpClient connection pool cleanup. Filtered by SocketError code and
    ''' exception type rather than message strings (locale-safe).
    ''' </summary>
    Private Function IsHarmlessNetworkException(agg As AggregateException) As Boolean
        If agg Is Nothing Then Return False
        For Each inner In agg.Flatten().InnerExceptions
            ' Cancellation during shutdown
            If TypeOf inner Is OperationCanceledException Then Continue For
            If TypeOf inner Is TaskCanceledException Then Continue For

            ' IOException wrapping a SocketException — check error code
            Dim ioEx = TryCast(inner, IO.IOException)
            If ioEx IsNot Nothing Then
                Dim sockEx = TryCast(ioEx.InnerException, Net.Sockets.SocketException)
                If sockEx IsNot Nothing Then
                    Select Case sockEx.SocketErrorCode
                        Case Net.Sockets.SocketError.ConnectionReset,       ' 10054
                             Net.Sockets.SocketError.ConnectionAborted,     ' 10053
                             Net.Sockets.SocketError.ConnectionRefused,     ' 10061
                             Net.Sockets.SocketError.Shutdown,              ' 10058
                             Net.Sockets.SocketError.NotConnected           ' 10057
                            Continue For
                    End Select
                End If
            End If

            ' Direct SocketException
            Dim directSock = TryCast(inner, Net.Sockets.SocketException)
            If directSock IsNot Nothing Then
                Select Case directSock.SocketErrorCode
                    Case Net.Sockets.SocketError.ConnectionReset,
                         Net.Sockets.SocketError.ConnectionAborted,
                         Net.Sockets.SocketError.ConnectionRefused,
                         Net.Sockets.SocketError.Shutdown,
                         Net.Sockets.SocketError.NotConnected
                        Continue For
                End Select
            End If

            ' HttpRequestException wrapping a socket error
            Dim httpEx = TryCast(inner, Net.Http.HttpRequestException)
            If httpEx IsNot Nothing Then
                Dim httpSock = TryCast(httpEx.InnerException, Net.Sockets.SocketException)
                If httpSock Is Nothing Then httpSock = TryCast(httpEx.InnerException?.InnerException, Net.Sockets.SocketException)
                If httpSock IsNot Nothing Then
                    Select Case httpSock.SocketErrorCode
                        Case Net.Sockets.SocketError.ConnectionReset,
                             Net.Sockets.SocketError.ConnectionAborted,
                             Net.Sockets.SocketError.ConnectionRefused,
                             Net.Sockets.SocketError.Shutdown,
                             Net.Sockets.SocketError.NotConnected
                            Continue For
                    End Select
                End If
            End If

            ' If we get here, this inner exception is NOT harmless
            Return False
        Next
        ' All inner exceptions were harmless
        Return True
    End Function

    Private Sub LogCrash(tag As String, ex As Exception)
        Dim detail = If(ex?.ToString(), "(no exception details)")
        ' Try AppLogger first
        Try
            FormMain.WriteDebugLog($"{tag} {detail}")
        Catch
        End Try
        ' Also write directly in case AppLogger failed
        Try
            Dim logPath = Services.Infrastructure.AppLogger.GetLogPath()
            File.AppendAllText(logPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {tag} {detail}{Environment.NewLine}")
        Catch
        End Try
    End Sub

End Module
