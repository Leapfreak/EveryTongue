Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Meta MMS-TTS backend — 1100+ languages via PyTorch VITS.
    ''' Priority 2 (fallback when Piper doesn't cover the language).
    ''' Runs as a Python FastAPI sidecar on port 5092 (same pattern as NLLB).
    ''' Optional — requires PyTorch (~200 MB CPU-only) and transformers.
    ''' Models auto-download from HuggingFace on first use per language (~100 MB each).
    ''' </summary>
    Public Class MmsTtsBackend
        Implements ITtsBackend
        Implements IDisposable

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(60)
        }
        Private _port As Integer = 5092
        Private _isRunning As Boolean = False
        Private _process As Process

        Public Event StatusChanged As EventHandler(Of String)

        Public ReadOnly Property Name As String Implements ITtsBackend.Name
            Get
                Return "MMS-TTS"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Priority As Integer Implements ITtsBackend.Priority
            Get
                Return 2
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Launches the MMS-TTS Python sidecar process.
        ''' </summary>
        Public Sub Start(Optional port As Integer = 5092)
            If _isRunning Then Return
            _port = port

            Dim pythonPath = FindPython()
            If String.IsNullOrEmpty(pythonPath) Then
                RaiseEvent StatusChanged(Me, "MMS-TTS: Python not found")
                Return
            End If

            Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "mms-tts-server", "server.py")
            If Not File.Exists(serverScript) Then
                RaiseEvent StatusChanged(Me, "MMS-TTS: server.py not found")
                Return
            End If

            ' Kill any leftover process on this port
            KillProcessOnPort(_port)

            Dim cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "tts-models", "mms-tts-cache")
            If Not Directory.Exists(cacheDir) Then Directory.CreateDirectory(cacheDir)

            Dim psi As New ProcessStartInfo() With {
                .FileName = pythonPath,
                .Arguments = $"""{serverScript}"" --port {_port} --cache-dir ""{cacheDir}""",
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True,
                .StandardOutputEncoding = Encoding.UTF8,
                .StandardErrorEncoding = Encoding.UTF8
            }

            Try
                _process = New Process()
                _process.StartInfo = psi
                _process.EnableRaisingEvents = True

                AddHandler _process.ErrorDataReceived, Sub(s, e)
                                                           If e.Data IsNot Nothing Then
                                                               Dim line = e.Data.Trim()
                                                               If line.Length > 0 Then
                                                                   RaiseEvent StatusChanged(Me, $"MMS-TTS: {line}")
                                                               End If
                                                           End If
                                                       End Sub

                AddHandler _process.Exited, Sub(s, e)
                                                _isRunning = False
                                                RaiseEvent StatusChanged(Me, "MMS-TTS server stopped")
                                            End Sub

                _process.Start()
                _process.BeginErrorReadLine()
                _process.BeginOutputReadLine()

                _isRunning = True
                RaiseEvent StatusChanged(Me, $"MMS-TTS server starting on port {_port}")

            Catch ex As Exception
                _isRunning = False
                RaiseEvent StatusChanged(Me, $"MMS-TTS failed to start: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Stops the MMS-TTS sidecar process.
        ''' </summary>
        Public Sub [Stop]()
            _isRunning = False
            Try
                If _process IsNot Nothing AndAlso Not _process.HasExited Then
                    _process.Kill(True)
                    _process.WaitForExit(5000)
                End If
            Catch
            End Try
            _process = Nothing
            RaiseEvent StatusChanged(Me, "MMS-TTS server stopped")
        End Sub

        Public Async Function SynthesiseAsync(text As String, language As String,
                                              ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            If Not _isRunning Then Return Nothing

            Try
                Dim json = $"{{""text"":{JsonSerializer.Serialize(text)},""language"":""{language}""}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Dim response = Await _httpClient.PostAsync(
                    $"http://127.0.0.1:{_port}/synthesise", content, ct)

                If response.IsSuccessStatusCode Then
                    Dim audioBytes = Await response.Content.ReadAsByteArrayAsync(ct)
                    Return New TtsResult With {
                        .AudioData = audioBytes,
                        .Codec = "wav",
                        .SampleRate = 16000
                    }
                End If
            Catch ex As OperationCanceledException
                Throw
            Catch
            End Try

            Return Nothing
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            ' MMS-TTS supports 1100+ languages — return empty (too many to enumerate)
            Return Task.FromResult(DirectCast(New List(Of String)(), IReadOnlyList(Of String)))
        End Function

        Public Function IsLanguageSupportedAsync(language As String,
                                                  ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            ' MMS-TTS has very broad coverage — assume supported if server is running
            Return Task.FromResult(_isRunning)
        End Function

        Public Async Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            If Not _isRunning Then Return False
            Try
                Dim response = Await _httpClient.GetAsync(
                    $"http://127.0.0.1:{_port}/health", ct)
                Return response.IsSuccessStatusCode
            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Returns True if the MMS-TTS Python dependencies are installed.
        ''' </summary>
        Public Shared Function CheckDepsInstalled() As Boolean
            Dim pythonPath = FindPython()
            If String.IsNullOrEmpty(pythonPath) Then Return False
            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = pythonPath,
                    .Arguments = "-c ""import torch; import transformers""",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True
                }
                Using proc = Process.Start(psi)
                    proc.WaitForExit(15000)
                    Return proc.ExitCode = 0
                End Using
            Catch
                Return False
            End Try
        End Function

        Private Shared Function FindPython() As String
            Dim embedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "python-embed", "python.exe")
            If File.Exists(embedPath) Then Return embedPath
            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = "python",
                    .Arguments = "--version",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .CreateNoWindow = True
                }
                Using proc = Process.Start(psi)
                    proc.WaitForExit(5000)
                    If proc.ExitCode = 0 Then Return "python"
                End Using
            Catch
            End Try
            Return ""
        End Function

        Private Shared Sub KillProcessOnPort(port As Integer)
            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = "netstat",
                    .Arguments = $"-ano",
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .CreateNoWindow = True
                }
                Using proc = Process.Start(psi)
                    Dim output = proc.StandardOutput.ReadToEnd()
                    proc.WaitForExit(5000)
                    For Each line In output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                        If line.Contains($":{port} ") AndAlso line.Contains("LISTENING") Then
                            Dim parts = line.Trim().Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
                            Dim pid As Integer
                            If Integer.TryParse(parts(parts.Length - 1), pid) AndAlso pid > 0 Then
                                Try
                                    Process.GetProcessById(pid).Kill(True)
                                Catch
                                End Try
                            End If
                        End If
                    Next
                End Using
            Catch
            End Try
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
        End Sub
    End Class
End Namespace
