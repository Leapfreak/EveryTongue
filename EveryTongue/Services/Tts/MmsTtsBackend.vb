Imports System.Diagnostics
Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Meta MMS-TTS backend — 1100+ languages via PyTorch VITS.
    ''' Priority 2 (fallback when Piper doesn't cover the language).
    ''' Runs as a Python FastAPI sidecar on port 5092 (same pattern as translate-server).
    ''' Optional — requires PyTorch (~200 MB CPU-only) and transformers.
    ''' Models auto-download from HuggingFace on first use per language (~100 MB each).
    ''' </summary>
    Public Class MmsTtsBackend
        Implements ITtsBackend
        Implements IDisposable

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(60)
        }
        Private ReadOnly _host As New Pipeline.PythonSidecarHost() With {
            .Label = "MMS-TTS",
            .Port = 5092
        }

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
                Return _host.IsRunning
            End Get
        End Property

        Public Sub New()
            AddHandler _host.StderrLine, Sub(s, line)
                                              RaiseEvent StatusChanged(Me, $"MMS-TTS: {line}")
                                          End Sub
            AddHandler _host.StatusMessage, Sub(s, msg)
                                                RaiseEvent StatusChanged(Me, msg)
                                            End Sub
            AddHandler _host.ProcessExited, Sub(s, e)
                                                RaiseEvent StatusChanged(Me, "MMS-TTS server stopped")
                                            End Sub
        End Sub

        Public Sub Start(Optional port As Integer = 5092)
            If _host.IsRunning Then Return
            _host.Port = port

            Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "mms-tts-server", "server.py")

            Dim cacheDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "tts-models", "mms-tts-cache")
            If Not Directory.Exists(cacheDir) Then Directory.CreateDirectory(cacheDir)

            _host.Start(serverScript, $"--cache-dir ""{cacheDir}""")
        End Sub

        Public Sub [Stop]()
            _host.Stop()
            RaiseEvent StatusChanged(Me, "MMS-TTS server stopped")
        End Sub

        Public Async Function SynthesiseAsync(text As String, language As String,
                                              ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            If Not _host.IsRunning Then Return Nothing

            Try
                Dim json = $"{{""text"":{JsonSerializer.Serialize(text)},""language"":""{language}""}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Dim response = Await _httpClient.PostAsync(
                    $"http://127.0.0.1:{_host.Port}/synthesise", content, ct)

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
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] MmsTtsBackend.SynthesiseAsync: {ex.Message}")
            End Try

            Return Nothing
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            Return Task.FromResult(DirectCast(New List(Of String)(), IReadOnlyList(Of String)))
        End Function

        Public Function IsLanguageSupportedAsync(language As String,
                                                  ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            Return Task.FromResult(_host.IsRunning)
        End Function

        Public Async Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            If Not _host.IsRunning Then Return False
            Try
                Dim response = Await _httpClient.GetAsync(
                    $"http://127.0.0.1:{_host.Port}/health", ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] MmsTtsBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function

        Public Shared Function CheckDepsInstalled() As Boolean
            Dim pythonPath = Pipeline.ProcessHelper.FindPython()
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
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] MmsTtsBackend.CheckDepsInstalled: {ex.Message}")
                Return False
            End Try
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            _host.Dispose()
        End Sub
    End Class
End Namespace
