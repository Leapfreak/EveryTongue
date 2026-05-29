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

        ' Whisper language code (ISO 639-1) -> NLLB-200 language code (FLORES-200)
        Private Shared ReadOnly _langMap As New Dictionary(Of String, String) From {
            {"en", "eng_Latn"}, {"es", "spa_Latn"}, {"fr", "fra_Latn"},
            {"de", "deu_Latn"}, {"pt", "por_Latn"}, {"it", "ita_Latn"},
            {"ca", "cat_Latn"}, {"ro", "ron_Latn"}, {"nl", "nld_Latn"},
            {"pl", "pol_Latn"}, {"ru", "rus_Cyrl"}, {"uk", "ukr_Cyrl"},
            {"zh", "zho_Hans"}, {"ja", "jpn_Jpan"}, {"ko", "kor_Hang"},
            {"ar", "arb_Arab"}, {"hi", "hin_Deva"}, {"tr", "tur_Latn"},
            {"vi", "vie_Latn"}, {"th", "tha_Thai"}, {"cs", "ces_Latn"},
            {"el", "ell_Grek"}, {"hu", "hun_Latn"}, {"da", "dan_Latn"},
            {"fi", "fin_Latn"}, {"no", "nob_Latn"}, {"sv", "swe_Latn"},
            {"sk", "slk_Latn"}, {"bg", "bul_Cyrl"}, {"hr", "hrv_Latn"},
            {"sr", "srp_Cyrl"}, {"sl", "slv_Latn"}, {"et", "est_Latn"},
            {"lv", "lvs_Latn"}, {"lt", "lit_Latn"}, {"sq", "sqi_Latn"},
            {"mk", "mkd_Cyrl"}, {"bs", "bos_Latn"}, {"is", "isl_Latn"},
            {"ms", "zsm_Latn"}, {"sw", "swh_Latn"}, {"tl", "tgl_Latn"},
            {"ta", "tam_Taml"}, {"te", "tel_Telu"}, {"ml", "mal_Mlym"},
            {"bn", "ben_Beng"}, {"gu", "guj_Gujr"}, {"kn", "kan_Knda"},
            {"mr", "mar_Deva"}, {"ne", "npi_Deva"}, {"pa", "pan_Guru"},
            {"ur", "urd_Arab"}, {"my", "mya_Mymr"}, {"km", "khm_Khmr"},
            {"ga", "gle_Latn"}, {"cy", "cym_Latn"}, {"mt", "mlt_Latn"},
            {"af", "afr_Latn"}, {"am", "amh_Ethi"}, {"hy", "hye_Armn"},
            {"az", "azj_Latn"}, {"eu", "eus_Latn"}, {"be", "bel_Cyrl"},
            {"fa", "pes_Arab"}, {"gl", "glg_Latn"}, {"ka", "kat_Geor"},
            {"ht", "hat_Latn"}, {"ha", "hau_Latn"}, {"he", "heb_Hebr"},
            {"id", "ind_Latn"}, {"jw", "jav_Latn"}, {"kk", "kaz_Cyrl"},
            {"lo", "lao_Laoo"}, {"lb", "ltz_Latn"}, {"mi", "mri_Latn"},
            {"mn", "khk_Cyrl"}, {"ps", "pbt_Arab"}, {"sd", "snd_Arab"},
            {"si", "sin_Sinh"}, {"sn", "sna_Latn"}, {"so", "som_Latn"},
            {"su", "sun_Latn"}, {"tg", "tgk_Cyrl"}, {"tt", "tat_Cyrl"},
            {"tk", "tuk_Latn"}, {"uz", "uzn_Latn"}, {"yo", "yor_Latn"},
            {"zu", "zul_Latn"}
        }

        ' NLLB prefix (3-letter) -> ISO 639-1 display code
        Private Shared ReadOnly _nllbToShortMap As New Dictionary(Of String, String) From {
            {"ENG", "EN"}, {"SPA", "ES"}, {"FRA", "FR"}, {"DEU", "DE"},
            {"POR", "PT"}, {"ITA", "IT"}, {"CAT", "CA"}, {"RON", "RO"},
            {"NLD", "NL"}, {"POL", "PL"}, {"RUS", "RU"}, {"UKR", "UK"},
            {"ZHO", "ZH"}, {"JPN", "JA"}, {"KOR", "KO"}, {"ARB", "AR"},
            {"SWE", "SV"}, {"NOB", "NO"}, {"DAN", "DA"}, {"FIN", "FI"},
            {"HUN", "HU"}, {"CES", "CS"}, {"SLK", "SK"}, {"SLV", "SL"},
            {"HRV", "HR"}, {"SRP", "SR"}, {"BUL", "BG"}, {"ELL", "EL"},
            {"TUR", "TR"}, {"LIT", "LT"}, {"LVS", "LV"}, {"EST", "ET"},
            {"AFR", "AF"}, {"AMH", "AM"}, {"HYE", "HY"}, {"AZJ", "AZ"},
            {"EUS", "EU"}, {"BEL", "BE"}, {"BEN", "BN"}, {"BOS", "BS"},
            {"CYM", "CY"}, {"PES", "FA"}, {"GLG", "GL"}, {"KAT", "KA"},
            {"GUJ", "GU"}, {"HAT", "HT"}, {"HAU", "HA"}, {"HEB", "HE"},
            {"HIN", "HI"}, {"ISL", "IS"}, {"IND", "ID"}, {"JAV", "JW"},
            {"KAN", "KN"}, {"KAZ", "KK"}, {"KHM", "KM"}, {"LAO", "LO"},
            {"LTZ", "LB"}, {"MKD", "MK"}, {"ZSM", "MS"}, {"MAL", "ML"},
            {"MLT", "MT"}, {"MRI", "MI"}, {"MAR", "MR"}, {"KHK", "MN"},
            {"MYA", "MY"}, {"NPI", "NE"}, {"PBT", "PS"}, {"PAN", "PA"},
            {"SND", "SD"}, {"SIN", "SI"}, {"SNA", "SN"}, {"SOM", "SO"},
            {"SUN", "SU"}, {"SWH", "SW"}, {"TGL", "TL"}, {"TGK", "TG"},
            {"TAM", "TA"}, {"TAT", "TT"}, {"TEL", "TE"}, {"THA", "TH"},
            {"TUK", "TK"}, {"URD", "UR"}, {"UZN", "UZ"}, {"VIE", "VI"},
            {"YOR", "YO"}, {"ZUL", "ZU"}, {"SQI", "SQ"}, {"GLE", "GA"}
        }

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

        Public Shared Function WhisperToNllbLang(whisperLang As String) As String
            Dim nllb As String = Nothing
            If _langMap.TryGetValue(whisperLang, nllb) Then Return nllb
            Return ""
        End Function

        Public Shared Function GetLangMap() As Dictionary(Of String, String)
            Return _langMap
        End Function

        Public Shared Function NllbToShortCode(nllbCode As String) As String
            If String.IsNullOrEmpty(nllbCode) Then Return "??"
            Dim prefix = nllbCode.Split("_"c)(0).ToUpperInvariant()
            Dim result As String = Nothing
            If _nllbToShortMap.TryGetValue(prefix, result) Then Return result
            Return prefix.Substring(0, Math.Min(2, prefix.Length))
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
                        proc.WaitForExit(10000)
                        depsOk = (proc.ExitCode = 0)
                    End Using
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[Translation] dependency check failed: {ex.Message}")
                End Try
            End If

            Dim modelDir = Path.Combine(baseDir, "nllb-model")
            Dim modelOk = Directory.Exists(modelDir) AndAlso
                          File.Exists(Path.Combine(modelDir, "model.bin")) AndAlso
                          File.Exists(Path.Combine(modelDir, "sentencepiece.bpe.model"))

            Return (pythonOk, depsOk, modelOk)
        End Function

        Public Sub Start(port As Integer, modelPath As String, device As String, Optional glossaryPath As String = "")
            _port = port
            _device = device
            _host.Port = port

            Dim resolvedModelPath = Models.AppConfig.ResolvePath(modelPath)
            Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nllb-server", "server.py")

            Dim extraArgs = $"--model-path ""{resolvedModelPath}"" --device {device} --log-dir ""{AppDomain.CurrentDomain.BaseDirectory.TrimEnd({"\"c, "/"c})}"""

            If Not String.IsNullOrEmpty(glossaryPath) Then
                Dim resolvedGlossary = Models.AppConfig.ResolvePath(glossaryPath)
                If File.Exists(resolvedGlossary) Then
                    extraArgs &= $" --glossary ""{resolvedGlossary}"""
                End If
            End If

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

        Public Async Function TranslateAsync(text As String, sourceLang As String, targetLangs As List(Of String)) As Task(Of Dictionary(Of String, String))
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

                Dim json = $"{{""text"":{ProcessHelper.EscapeJson(text)},""source_lang"":""{sourceLang}"",""target_langs"":{targetsJson}}}"
                Dim content As New StringContent(json, Encoding.UTF8, "application/json")

                Using cts As New CancellationTokenSource(TimeSpan.FromSeconds(12))
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
