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

        Public Function GetLogPath() As String
            Return IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMdd}.log")
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
                Dim logDir = AppDomain.CurrentDomain.BaseDirectory
                Dim cutoff = DateTime.Now.AddDays(-keepDays)
                For Each f In IO.Directory.GetFiles(logDir, "????????.log")
                    If IO.File.GetLastWriteTime(f) < cutoff Then
                        IO.File.Delete(f)
                    End If
                Next
            Catch
            End Try
        End Sub

    End Module

End Namespace
