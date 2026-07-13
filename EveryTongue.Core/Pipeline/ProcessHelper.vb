Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports EveryTongue.Services.Infrastructure

Namespace Pipeline
    ''' <summary>
    ''' Shared utilities for Python process management.
    ''' </summary>
    Friend Module ProcessHelper

        ''' <summary>
        ''' Find the Python executable — embedded first, then system.
        ''' </summary>
        Public Function FindPython() As String
            Dim embedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
            If File.Exists(embedPath) Then Return embedPath

            ' No embedded runtime (Lite/Docker/macOS): probe the system interpreter.
            ' Debian/Ubuntu images ship "python3" only; Windows installs ship "python".
            For Each candidate In {"python3", "python"}
                Try
                    Dim psi As New ProcessStartInfo() With {
                        .FileName = candidate,
                        .Arguments = "--version",
                        .UseShellExecute = False,
                        .RedirectStandardOutput = True,
                        .RedirectStandardError = True,
                        .CreateNoWindow = True
                    }
                    Using proc = Process.Start(psi)
                        proc.WaitForExit(5000)
                        If proc.ExitCode = 0 Then Return candidate
                    End Using
                Catch
                    ' not on PATH — try the next candidate
                End Try
            Next
            AppLogger.Log(LogEvents.PIPELINE_SIDECAR_ERROR, "FindPython: no embedded or system python found")
            Return ""
        End Function

        ''' <summary>
        ''' Kill any process listening on the specified TCP port.
        ''' </summary>
        Public Sub KillProcessOnPort(port As Integer)
            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = "netstat",
                    .Arguments = "-ano",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .CreateNoWindow = True
                }
                Using proc = Process.Start(psi)
                    Dim output = proc.StandardOutput.ReadToEnd()
                    proc.WaitForExit(5000)
                    For Each line In output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                        If line.Contains($":{port}") AndAlso line.Contains("LISTENING") Then
                            Dim parts = line.Trim().Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
                            Dim pid As Integer
                            If parts.Length > 0 AndAlso Integer.TryParse(parts(parts.Length - 1), pid) AndAlso pid > 0 Then
                                Try
                                    AppLogger.Log(LogEvents.PIPELINE_PROCESS_KILL, $"KillProcessOnPort({port}): killing PID {pid} (line: {line.Trim()})")
                                    Process.GetProcessById(pid).Kill(True)
                                Catch ex As Exception
                                    AppLogger.Log(LogEvents.PIPELINE_PROCESS_KILL, $"Failed to kill PID {pid} on port {port} — {ex.Message}")
                                End Try
                            End If
                        End If
                    Next
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.PIPELINE_PROCESS_KILL, $"Failed to enumerate processes on port {port} — {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' JSON-escapes a string and wraps it in double quotes.
        ''' </summary>
        Public Function EscapeJson(s As String) As String
            Dim sb As New StringBuilder("""")
            For Each c In s
                Select Case c
                    Case """"c : sb.Append("\""")
                    Case "\"c : sb.Append("\\")
                    Case ChrW(8) : sb.Append("\b")
                    Case ChrW(9) : sb.Append("\t")
                    Case ChrW(10) : sb.Append("\n")
                    Case ChrW(12) : sb.Append("\f")
                    Case ChrW(13) : sb.Append("\r")
                    Case Else
                        If AscW(c) < 32 Then
                            sb.Append($"\u{AscW(c):X4}")
                        Else
                            sb.Append(c)
                        End If
                End Select
            Next
            sb.Append(""""c)
            Return sb.ToString()
        End Function

    End Module
End Namespace
