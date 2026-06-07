' AppLogger.vb — Centralized logging (file + optional UI callback)
' Decoupled from FormMain so any class can log without a form reference.

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

        Public Sub Log(msg As String)
            ' Always write to daily log file
            Try
                Dim logPath = GetLogPath()
                IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {msg}{Environment.NewLine}")
            Catch
            End Try

            ' Route to UI if callback is set
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
        End Sub

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
