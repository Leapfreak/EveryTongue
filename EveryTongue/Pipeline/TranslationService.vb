Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading

Namespace Pipeline
    Public Class TranslationService

        Public Event StatusChanged As EventHandler(Of String)

        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromMinutes(5)
        }

        Private ReadOnly _host As New PythonSidecarHost() With {
            .Label = "Translation server",
            .MaxRestarts = 3,
            .AddWhisperToPath = True
        }

        Private _port As Integer = 5090
        Private _device As String = "cuda"
        Private _modelLoaded As Boolean = False

        Private Shared ReadOnly _langService As Services.Infrastructure.LanguageCodeService =
            Services.Infrastructure.LanguageCodeService.Instance

        Public Sub New()
            AddHandler _host.StderrLine, Sub(s, line)
                                              RaiseEvent StatusChanged(Me, $"Translation: {line}")
                                          End Sub
            AddHandler _host.StatusMessage, Sub(s, msg)
                                                RaiseEvent StatusChanged(Me, msg)
                                            End Sub
            AddHandler _host.ProcessExited, Sub(s, e)
                                                _modelLoaded = False
                                            End Sub
        End Sub

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _host.IsRunning
            End Get
        End Property

        Public ReadOnly Property IsModelLoaded As Boolean
            Get
                Return _modelLoaded
            End Get
        End Property

        Public Shared Function WhisperToFloresLang(whisperLang As String) As String
            Return _langService.WhisperToFlores(whisperLang)
        End Function

        Public Shared Function GetLangMap() As Dictionary(Of String, String)
            Return _langService.GetWhisperToFloresMap()
        End Function

        Public Shared Function FloresToShortCode(floresCode As String) As String
            Return _langService.FloresToShortCode(floresCode)
        End Function

        Public Shared Function FloresToIso3(floresCode As String) As String
            Return _langService.FloresToIso3(floresCode)
        End Function

        Public Shared Function CheckDependenciesInstalled() As (pythonOk As Boolean, depsOk As Boolean, modelOk As Boolean)
            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
            Dim pythonPath = Path.Combine(baseDir, "python-embed", "python.exe")
            Dim pythonOk = File.Exists(pythonPath)

            Dim depsOk = False
            If pythonOk Then
                Try
                    Dim psi As New ProcessStartInfo() With {
                        .FileName = pythonPath,
                        .Arguments = "-c ""import ctranslate2; import sentencepiece; import fastapi; import uvicorn""",
                        .UseShellExecute = False,
                        .RedirectStandardOutput = True,
                        .RedirectStandardError = True,
                        .CreateNoWindow = True
                    }
                    Using proc = Process.Start(psi)
                        ' Drain both pipes before WaitForExit to prevent pipe buffer deadlock
                        Dim stderrTask = proc.StandardError.ReadToEndAsync()
                        proc.StandardOutput.ReadToEnd()
                        stderrTask.Wait()
                        proc.WaitForExit(10000)
                        depsOk = (proc.ExitCode = 0)
                    End Using
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[Translation] dependency check failed: {ex.Message}")
                End Try
            End If

            ' Check the configured model path, not just the default nllb-model dir
            Dim config = Models.ConfigManager.Load()
            Dim modelDir = Models.AppConfig.ResolvePath(If(config.TranslationModelPath, ".\nllb-model"))
            Dim modelType = If(config.TranslationModelType, "nllb")
            Dim hasModelBin = Directory.Exists(modelDir) AndAlso File.Exists(Path.Combine(modelDir, "model.bin"))
            Dim hasSp As Boolean
            hasSp = File.Exists(Path.Combine(modelDir, "sentencepiece.bpe.model"))
            Dim modelOk = hasModelBin AndAlso hasSp

            Return (pythonOk, depsOk, modelOk)
        End Function

        Public Sub Start(port As Integer, modelPath As String, device As String,
                         Optional glossaryPath As String = "", Optional modelType As String = "nllb")
            _port = port
            _device = device
            _host.Port = port

            Dim resolvedModelPath = Models.AppConfig.ResolvePath(modelPath)
            Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translate-server", "server.py")

            Dim extraArgs = $"--model-path ""{resolvedModelPath}"" --device {device} --model-type {modelType}"

            If Not String.IsNullOrEmpty(glossaryPath) Then
                Dim resolvedGlossary = Models.AppConfig.ResolvePath(glossaryPath)
                If File.Exists(resolvedGlossary) Then
                    extraArgs &= $" --glossary ""{resolvedGlossary}"""
                End If
            End If

            Dim logLevel = Models.ConfigManager.Load().LogLevel.ToString().ToLowerInvariant()
            extraArgs &= $" --log-level {logLevel}"

            _host.Start(serverScript, extraArgs)

            ' Wait for health check in background
            Dim ct = _host.CancellationToken
            Task.Run(Sub() WaitForReady(ct))
        End Sub

        Private Sub WaitForReady(ct As CancellationToken)
            Dim deadline = DateTime.UtcNow.AddSeconds(30)
            While DateTime.UtcNow < deadline AndAlso Not ct.IsCancellationRequested
                Try
                    Threading.Thread.Sleep(1000)
                    If ct.IsCancellationRequested Then Return
                    Using cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                        cts.CancelAfter(5000)
                        Dim response = _httpClient.GetAsync($"http://127.0.0.1:{_port}/health", cts.Token).Result
                        If response.IsSuccessStatusCode Then
                            If ct.IsCancellationRequested Then Return
                            RaiseEvent StatusChanged(Me, "Translation server ready, loading model...")
                            Try
                                LoadModelAsync().Wait()
                                If _modelLoaded Then
                                    WarmUpModel()
                                End If
                            Catch ex As Exception
                                Services.Infrastructure.AppLogger.Log($"[ERROR] WaitForReady: LoadModelAsync failed — {ex.Message}")
                            End Try
                            Return
                        End If
                    End Using
                Catch ex As OperationCanceledException
                    Return
                Catch ex As Exception
                    If ct.IsCancellationRequested Then Return
                End Try
            End While
            If Not ct.IsCancellationRequested Then
                RaiseEvent StatusChanged(Me, "Translation server: startup timeout")
            End If
        End Sub

        Public Async Function LoadModelAsync() As Task
            Try
                Dim json = $"{{""device"":""{_device}""}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")
                Dim response = Await _httpClient.PostAsync($"http://127.0.0.1:{_port}/load", content)
                If response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync()
                    Using doc = JsonDocument.Parse(body)
                        Dim actualDevice = doc.RootElement.GetProperty("device").GetString()
                        _modelLoaded = doc.RootElement.GetProperty("model_loaded").GetBoolean()
                        RaiseEvent StatusChanged(Me, $"Translation model loaded on {actualDevice}")
                    End Using
                    _host.ResetRestartCount()
                End If
            Catch ex As Exception
                RaiseEvent StatusChanged(Me, $"Translation: model load failed: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Send a throwaway translation to prime the PyTorch JIT cache.
        ''' Without this, the first real translation takes ~1000ms+ (cold start).
        ''' After warmup, translations complete in ~20ms.
        ''' </summary>
        Private Sub WarmUpModel()
            Try
                RaiseEvent StatusChanged(Me, "Translation: warming up engine...")
                Dim sw = Diagnostics.Stopwatch.StartNew()

                Dim json = "{""text"":""Hello"",""source_lang"":""eng_Latn"",""target_langs"":[""spa_Latn""]}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")

                Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(30))
                    Dim response = _httpClient.PostAsync($"http://127.0.0.1:{_port}/translate", content, cts.Token).Result
                End Using

                sw.Stop()
                RaiseEvent StatusChanged(Me, $"Translation engine warm — first inference took {sw.ElapsedMilliseconds}ms, subsequent requests will be fast")
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[WARN] Translation warmup failed (non-fatal): {ex.Message}")
            End Try
        End Sub

        Public Async Function ReloadGlossaryAsync() As Task
            Try
                Dim content As New StringContent("{}", Encoding.UTF8, "application/json")
                Dim response = Await _httpClient.PostAsync($"http://127.0.0.1:{_port}/glossary/reload", content)
                If response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync()
                    Using doc = JsonDocument.Parse(body)
                        Dim count = doc.RootElement.GetProperty("entries").GetInt32()
                        RaiseEvent StatusChanged(Me, $"Glossary reloaded: {count} entries")
                    End Using
                End If
            Catch ex As Exception
                RaiseEvent StatusChanged(Me, $"Glossary reload failed: {ex.Message}")
            End Try
        End Function

        Public Async Function UnloadModelAsync() As Task
            Try
                Dim content As New StringContent("{}", Encoding.UTF8, "application/json")
                Await _httpClient.PostAsync($"http://127.0.0.1:{_port}/unload", content)
                _modelLoaded = False
                RaiseEvent StatusChanged(Me, "Translation model unloaded")
            Catch ex As Exception
                FormMain.WriteDebugLog($"[Translation] unload model failed: {ex.Message}")
            End Try
        End Function

        Public Async Function TranslateAsync(text As String, sourceLang As String, targetLangs As List(Of String),
                                              Optional noCache As Boolean = False,
                                              Optional timeoutSeconds As Integer = 35) As Task(Of Dictionary(Of String, String))
            If Not _host.IsProcessRunning OrElse Not _modelLoaded OrElse targetLangs.Count = 0 Then
                Return New Dictionary(Of String, String)()
            End If

            Try
                Dim targetsJson As New StringBuilder("[")
                For i = 0 To targetLangs.Count - 1
                    If i > 0 Then targetsJson.Append(",")
                    targetsJson.Append($"""{targetLangs(i)}""")
                Next
                targetsJson.Append("]")

                Dim noCacheJson = If(noCache, ",""no_cache"":true", "")
                Dim json = $"{{""text"":{ProcessHelper.EscapeJson(text)},""source_lang"":""{sourceLang}"",""target_langs"":{targetsJson}{noCacheJson}}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")

                Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds))
                    Dim response = Await _httpClient.PostAsync($"http://127.0.0.1:{_port}/translate", content, cts.Token)
                    If response.IsSuccessStatusCode Then
                        Dim body = Await response.Content.ReadAsStringAsync()
                        Using doc = JsonDocument.Parse(body)
                            Dim result As New Dictionary(Of String, String)()
                            Dim translations = doc.RootElement.GetProperty("translations")
                            For Each prop In translations.EnumerateObject()
                                result(prop.Name) = prop.Value.GetString()
                            Next
                            Return result
                        End Using
                    End If
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[Translation] translate request failed: {ex.Message}")
            End Try

            Return New Dictionary(Of String, String)()
        End Function

        Public Sub [Stop]()
            _host.Stop()
            _modelLoaded = False
            RaiseEvent StatusChanged(Me, "Translation server stopped")
        End Sub

    End Class
End Namespace
