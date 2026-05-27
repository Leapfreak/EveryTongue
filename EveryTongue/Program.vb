Imports System.IO

Friend Module Program

    ' Keep the lock file open for the lifetime of the process
    Private _lockStream As FileStream

    <STAThread()>
    Friend Sub Main(args As String())
        ' Single-instance guard: try to exclusively lock a file in AppData
        Dim lockDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EveryTongue")
        Directory.CreateDirectory(lockDir)
        Dim lockPath = Path.Combine(lockDir, ".lock")

        Try
            _lockStream = New FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
        Catch ex As IOException
            MessageBox.Show("Every Tongue is already running.", "Every Tongue", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End Try

        ' Catch unobserved Task exceptions so fire-and-forget failures get logged
        AddHandler TaskScheduler.UnobservedTaskException, Sub(s, e)
            FormMain.WriteDebugLog($"[UNOBSERVED TASK] {e.Exception}")
            e.SetObserved()
        End Sub

        ' Catch unhandled UI-thread exceptions
        AddHandler Application.ThreadException, Sub(s, e)
            FormMain.WriteDebugLog($"[UI THREAD] {e.Exception}")
        End Sub

        ' Catch unhandled non-UI-thread exceptions
        AddHandler AppDomain.CurrentDomain.UnhandledException, Sub(s, e)
            Dim ex = TryCast(e.ExceptionObject, Exception)
            FormMain.WriteDebugLog($"[UNHANDLED] {If(ex?.ToString(), e.ExceptionObject?.ToString())}")
        End Sub

        Try
            Application.SetHighDpiMode(HighDpiMode.SystemAware)
            Application.EnableVisualStyles()
            Application.SetCompatibleTextRenderingDefault(False)
            Application.Run(New FormMain)
        Finally
            _lockStream?.Dispose()
        End Try
    End Sub

End Module
