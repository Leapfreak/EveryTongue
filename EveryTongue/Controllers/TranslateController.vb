Imports System.Text.RegularExpressions

Namespace Controllers
    ''' <summary>
    ''' Manages the Translate workspace — source/target language selection,
    ''' text translation, swap, copy/clear buttons.
    ''' Extracted from FormMain.Shell.vb.
    ''' </summary>
    Friend Class TranslateController

        ' UI controls
        Private ReadOnly _txtInput As TextBox
        Private ReadOnly _txtOutput As TextBox
        Private ReadOnly _cboSource As ComboBox
        Private ReadOnly _cboTarget As ComboBox
        Private ReadOnly _btnSwap As Button
        Private ReadOnly _btnTranslate As Button
        Private ReadOnly _btnCopy As Button
        Private ReadOnly _btnClear As Button
        Private ReadOnly _btnOutCopy As Button
        Private ReadOnly _btnOutClear As Button
        Private ReadOnly _btnSpeak As Button
        Private ReadOnly _lblStatus As Label

        ' Callbacks
        Private ReadOnly _config As Models.AppConfig
        Private ReadOnly _langDisplayName As Func(Of String, String)
        Private ReadOnly _langCodeFromDisplay As Func(Of String, String)
        Private ReadOnly _sttLanguages As String()
        Private ReadOnly _startTranslationService As Action
        Private ReadOnly _getTranslationService As Func(Of Pipeline.TranslationService)
        Private ReadOnly _debugLog As Action(Of String)
        Private ReadOnly _getString As Func(Of String, String)
        Private ReadOnly _ttsPlayer As Services.Audio.DesktopTtsPlayer
        Private _isSpeaking As Boolean = False

        Public Sub New(config As Models.AppConfig,
                       txtInput As TextBox, txtOutput As TextBox,
                       cboSource As ComboBox, cboTarget As ComboBox,
                       btnSwap As Button, btnTranslate As Button,
                       btnCopy As Button, btnClear As Button,
                       btnOutCopy As Button, btnOutClear As Button,
                       btnSpeak As Button,
                       lblStatus As Label,
                       sttLanguages As String(),
                       langDisplayName As Func(Of String, String),
                       langCodeFromDisplay As Func(Of String, String),
                       startTranslationService As Action,
                       getTranslationService As Func(Of Pipeline.TranslationService),
                       getTtsService As Func(Of Services.Interfaces.ITtsService),
                       getTtsCacheDir As Func(Of String),
                       debugLog As Action(Of String),
                       getString As Func(Of String, String))
            _config = config
            _txtInput = txtInput
            _txtOutput = txtOutput
            _cboSource = cboSource
            _cboTarget = cboTarget
            _btnSwap = btnSwap
            _btnTranslate = btnTranslate
            _btnCopy = btnCopy
            _btnClear = btnClear
            _btnOutCopy = btnOutCopy
            _btnOutClear = btnOutClear
            _btnSpeak = btnSpeak
            _lblStatus = lblStatus
            _ttsPlayer = New Services.Audio.DesktopTtsPlayer(getTtsService, getTtsCacheDir)
            _sttLanguages = sttLanguages
            _langDisplayName = langDisplayName
            _langCodeFromDisplay = langCodeFromDisplay
            _startTranslationService = startTranslationService
            _getTranslationService = getTranslationService
            _debugLog = debugLog
            _getString = getString
        End Sub

        Public Sub WireEvents()
            AddHandler _btnSwap.Click, Sub(s, e) SwapLanguages()
            AddHandler _btnTranslate.Click, Sub(s, e) RunTranslateAsync()
            AddHandler _btnCopy.Click, Sub(s, e)
                                            If Not String.IsNullOrEmpty(_txtInput.Text) Then
                                                Clipboard.SetText(_txtInput.Text)
                                            End If
                                        End Sub
            AddHandler _btnClear.Click, Sub(s, e) _txtInput.Clear()
            AddHandler _btnOutCopy.Click, Sub(s, e)
                                              If Not String.IsNullOrEmpty(_txtOutput.Text) Then
                                                  Clipboard.SetText(_txtOutput.Text)
                                              End If
                                          End Sub
            AddHandler _btnOutClear.Click, Sub(s, e) _txtOutput.Clear()
            AddHandler _btnSpeak.Click, Sub(s, e) OnSpeakClicked()
        End Sub

        ''' <summary>Speak the translated output via server TTS (toggles to Stop while playing).</summary>
        Private Async Sub OnSpeakClicked()
            If _isSpeaking Then
                _ttsPlayer.StopPlayback()
                _isSpeaking = False
                _btnSpeak.Text = _getString("Trans_Speak")
                Return
            End If

            Dim outputText = _txtOutput.Text.Trim()
            If String.IsNullOrEmpty(outputText) Then Return

            If Not _ttsPlayer.IsAvailable Then
                _lblStatus.Text = _getString("Trans_SpeakServerRequired")
                _lblStatus.ForeColor = Drawing.Color.Red
                Return
            End If

            Dim targetWhisper = _langCodeFromDisplay(_cboTarget.Text)
            Dim targetFlores = Pipeline.TranslationService.WhisperToFloresLang(targetWhisper)
            Dim iso3 = If(String.IsNullOrEmpty(targetFlores), "eng",
                Pipeline.TranslationService.FloresToIso3(targetFlores))

            _isSpeaking = True
            _btnSpeak.Text = _getString("Trans_SpeakStop")
            _lblStatus.Text = _getString("Trans_Synthesizing")
            _lblStatus.ForeColor = Drawing.Color.FromArgb(0, 122, 204)

            Try
                ' Sentence-sized chunks synthesize fast and queue in order.
                Dim chunks = SplitIntoLines(outputText).
                    Where(Function(tl) Not tl.IsBlank).
                    SelectMany(Function(tl) tl.Sentences).ToList()
                Dim queued = Await _ttsPlayer.SpeakAsync(chunks, iso3)
                If queued = 0 AndAlso _isSpeaking Then
                    _lblStatus.Text = _getString("Trans_SpeakFailed")
                    _lblStatus.ForeColor = Drawing.Color.Red
                    _isSpeaking = False
                    _btnSpeak.Text = _getString("Trans_Speak")
                ElseIf _isSpeaking Then
                    ' Playback continues in the background — the button stays
                    ' "Stop" until the user presses it.
                    _lblStatus.Text = _getString("Trans_Done")
                    _lblStatus.ForeColor = Drawing.Color.Green
                End If
            Catch ex As Exception
                _debugLog($"[TRANSLATE] Speak failed: {ex.Message}")
                _lblStatus.Text = _getString("Trans_SpeakFailed")
                _lblStatus.ForeColor = Drawing.Color.Red
                _isSpeaking = False
                _btnSpeak.Text = _getString("Trans_Speak")
            End Try
        End Sub

        ''' <summary>Wire context menu handlers for the Translate textboxes.</summary>
        Public Sub WireContextMenus(ctxInputCut As ToolStripMenuItem, ctxInputCopy As ToolStripMenuItem,
                                    ctxInputPaste As ToolStripMenuItem, ctxInputSelectAll As ToolStripMenuItem,
                                    ctxOutputCopy As ToolStripMenuItem, ctxOutputSelectAll As ToolStripMenuItem)
            ' Input context menu
            AddHandler _txtInput.ContextMenuStrip.Opening, Sub(s, e)
                                                                ctxInputCut.Enabled = _txtInput.SelectionLength > 0
                                                                ctxInputCopy.Enabled = _txtInput.SelectionLength > 0
                                                                ctxInputPaste.Enabled = Clipboard.ContainsText()
                                                                ctxInputSelectAll.Enabled = _txtInput.TextLength > 0
                                                            End Sub
            AddHandler ctxInputCut.Click, Sub(s, e) _txtInput.Cut()
            AddHandler ctxInputCopy.Click, Sub(s, e) _txtInput.Copy()
            AddHandler ctxInputPaste.Click, Sub(s, e) _txtInput.Paste()
            AddHandler ctxInputSelectAll.Click, Sub(s, e) _txtInput.SelectAll()

            ' Output context menu
            AddHandler _txtOutput.ContextMenuStrip.Opening, Sub(s, e)
                                                                 ctxOutputCopy.Enabled = _txtOutput.SelectionLength > 0
                                                                 ctxOutputSelectAll.Enabled = _txtOutput.TextLength > 0
                                                             End Sub
            AddHandler ctxOutputCopy.Click, Sub(s, e) _txtOutput.Copy()
            AddHandler ctxOutputSelectAll.Click, Sub(s, e) _txtOutput.SelectAll()
        End Sub

        Public Sub PopulateLanguageDropdowns()
            ' Only show languages that have FLORES mappings (i.e. the translation
            ' engine can actually handle them). Codes like "no", "si", "jw" have
            ' no FLORES equivalent and produce garbage if selected.
            Dim floresMap = Pipeline.TranslationService.GetLangMap()
            For Each lang In _sttLanguages
                If lang = "auto" Then Continue For
                If Not floresMap.ContainsKey(lang) Then Continue For
                Dim display = _langDisplayName(lang)
                _cboSource.Items.Add(display)
                _cboTarget.Items.Add(display)
            Next
            _cboSource.Items.Insert(0, _langDisplayName("auto"))
            _cboSource.SelectedIndex = 0
            For i = 0 To _cboTarget.Items.Count - 1
                If _cboTarget.Items(i).ToString().StartsWith("English") Then
                    _cboTarget.SelectedIndex = i
                    Exit For
                End If
            Next
        End Sub

        Private Sub SwapLanguages()
            If _cboSource.SelectedIndex < 0 OrElse _cboTarget.SelectedIndex < 0 Then Return
            Dim srcText = _cboSource.Text
            Dim tgtText = _cboTarget.Text
            If srcText.StartsWith("Auto") Then Return
            _cboSource.Text = tgtText
            _cboTarget.Text = srcText
            Dim tmp = _txtInput.Text
            _txtInput.Text = _txtOutput.Text
            _txtOutput.Text = tmp
        End Sub

        Private Async Sub RunTranslateAsync()
            Dim inputText = _txtInput.Text.Trim()
            If String.IsNullOrEmpty(inputText) Then Return

            Dim sourceWhisper = _langCodeFromDisplay(_cboSource.Text)
            Dim targetWhisper = _langCodeFromDisplay(_cboTarget.Text)

            Dim sourceLang = If(sourceWhisper = "auto", "eng_Latn",
                Pipeline.TranslationService.WhisperToFloresLang(sourceWhisper))
            Dim targetLang = Pipeline.TranslationService.WhisperToFloresLang(targetWhisper)

            If String.IsNullOrEmpty(targetLang) Then
                _lblStatus.Text = String.Format(_getString("Trans_UnsupportedLang"), targetWhisper)
                _lblStatus.ForeColor = Drawing.Color.Red
                Return
            End If

            _btnTranslate.Enabled = False
            _txtOutput.Text = ""

            If Not Await EnsureTranslationServiceAsync() Then
                _btnTranslate.Enabled = True
                Return
            End If

            Dim textLines = SplitIntoLines(inputText)
            Dim totalSegments = textLines.Sum(Function(tl) tl.Sentences.Count)
            _lblStatus.Text = String.Format(_getString("Trans_Translating"), totalSegments)
            _lblStatus.ForeColor = Drawing.Color.FromArgb(0, 122, 204)

            Try
                Dim port = _config.TranslationPort
                Using client As New System.Net.Http.HttpClient()
                    client.Timeout = TimeSpan.FromSeconds(60)
                    Dim url = $"http://127.0.0.1:{port}/translate"
                    Dim output As New System.Text.StringBuilder()
                    Dim segNum = 0

                    For lineIdx = 0 To textLines.Count - 1
                        Dim tl = textLines(lineIdx)
                        If lineIdx > 0 Then output.Append(vbCrLf)

                        If tl.IsBlank Then Continue For

                        Dim lineResult As New System.Text.StringBuilder()
                        For Each sentence In tl.Sentences
                            segNum += 1
                            _lblStatus.Text = String.Format(_getString("Trans_TranslatingProgress"), segNum, totalSegments)

                            Dim bodyObj As New Dictionary(Of String, Object) From {
                                {"text", sentence},
                                {"source_lang", sourceLang},
                                {"target_langs", New String() {targetLang}}
                            }
                            Dim bodyJson = System.Text.Json.JsonSerializer.Serialize(bodyObj)
                            Dim content As New System.Net.Http.StringContent(
                                bodyJson, System.Text.Encoding.UTF8, "application/json")

                            Dim response = Await client.PostAsync(url, content)
                            If response.IsSuccessStatusCode Then
                                Dim json = Await response.Content.ReadAsStringAsync()
                                Dim doc = System.Text.Json.JsonDocument.Parse(json)
                                Dim root = doc.RootElement

                                Dim translationsEl As System.Text.Json.JsonElement
                                Dim resultEl As System.Text.Json.JsonElement
                                If root.TryGetProperty("translations", translationsEl) Then
                                    If translationsEl.TryGetProperty(targetLang, resultEl) Then
                                        If lineResult.Length > 0 Then lineResult.Append(" ")
                                        lineResult.Append(resultEl.GetString())
                                    End If
                                End If
                            Else
                                _lblStatus.Text = String.Format(_getString("Trans_TransError"), response.StatusCode)
                                _lblStatus.ForeColor = Drawing.Color.Red
                                _txtOutput.Text = output.ToString()
                                Return
                            End If
                        Next

                        output.Append(lineResult.ToString())
                    Next

                    _txtOutput.Text = output.ToString()
                    _lblStatus.Text = _getString("Trans_Done")
                    _lblStatus.ForeColor = Drawing.Color.Green
                End Using
            Catch ex As System.Net.Http.HttpRequestException
                _lblStatus.Text = _getString("Trans_ConnectionFailed")
                _lblStatus.ForeColor = Drawing.Color.Red
            Catch ex As TaskCanceledException
                _lblStatus.Text = _getString("Trans_Timeout")
                _lblStatus.ForeColor = Drawing.Color.Red
            Catch ex As Exception
                _lblStatus.Text = $"Error: {ex.Message}"
                _lblStatus.ForeColor = Drawing.Color.Red
            Finally
                _btnTranslate.Enabled = True
            End Try
        End Sub

        Private Async Function EnsureTranslationServiceAsync() As Task(Of Boolean)
            Dim svc = _getTranslationService()
            _debugLog($"[TRANSLATE] EnsureTranslationServiceAsync: service={svc IsNot Nothing}, isRunning={svc?.IsRunning}, isModelLoaded={svc?.IsModelLoaded}, config.TranslationEnabled={_config.TranslationEnabled}")

            ' Start if not running
            If svc Is Nothing OrElse Not svc.IsRunning Then
                Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
                _debugLog($"[TRANSLATE] Deps check: pythonOk={deps.pythonOk}, depsOk={deps.depsOk}, modelOk={deps.modelOk}")
                If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
                    _lblStatus.Text = _getString("Msg_TransDepsMissing")
                    _lblStatus.ForeColor = Drawing.Color.Red
                    Services.Infrastructure.AppLogger.PromptDownloadManager(
                        _getString("Msg_TransDepsMissing") & vbCrLf & vbCrLf & _getString("Msg_OpenDownloadManager"),
                        _getString("Msg_DepsMissing"))
                    Return False
                End If
                _lblStatus.Text = _getString("Trans_StartingEngine")
                _lblStatus.ForeColor = Drawing.Color.FromArgb(0, 122, 204)
                _debugLog("[TRANSLATE] Calling StartTranslationService...")
                _startTranslationService()
            End If

            ' Re-fetch after start attempt
            svc = _getTranslationService()

            ' Guard against null service (e.g. TranslationEnabled=False)
            If svc Is Nothing Then
                _debugLog("[TRANSLATE] Service is Nothing after start attempt — TranslationEnabled is likely False")
                _lblStatus.Text = _getString("Trans_Disabled")
                _lblStatus.ForeColor = Drawing.Color.Red
                Return False
            End If

            ' Reload model if server is running but model was unloaded
            If svc.IsRunning AndAlso Not svc.IsModelLoaded Then
                _debugLog("[TRANSLATE] Server running but model not loaded — reloading...")
                _lblStatus.Text = _getString("Trans_LoadingModel")
                _lblStatus.ForeColor = Drawing.Color.FromArgb(0, 122, 204)
                Try
                    Await svc.LoadModelAsync()
                Catch ex As Exception
                    _debugLog($"[TRANSLATE] LoadModelAsync failed: {ex.Message}")
                End Try
            End If

            ' Wait for model to load (up to 120s)
            If Not svc.IsModelLoaded Then
                _debugLog("[TRANSLATE] Waiting for model to load (up to 120s)...")
                _lblStatus.Text = _getString("Trans_WaitingModel")
                _lblStatus.ForeColor = Drawing.Color.FromArgb(0, 122, 204)
                Dim waited = 0
                While Not svc.IsModelLoaded AndAlso waited < 120000
                    Await Task.Delay(500)
                    waited += 500
                End While
                _debugLog($"[TRANSLATE] Wait complete: waited={waited}ms, isModelLoaded={svc.IsModelLoaded}")
                If Not svc.IsModelLoaded Then
                    _lblStatus.Text = _getString("Trans_ModelFailed")
                    _lblStatus.ForeColor = Drawing.Color.Red
                    Return False
                End If
            End If

            _debugLog("[TRANSLATE] Translation service ready")
            Return True
        End Function

        Friend Class TextLine
            Public IsBlank As Boolean
            Public Sentences As New List(Of String)()
        End Class

        ''' <summary>
        ''' Splits input preserving every line break. Each line is further split
        ''' into sentences for translation. Blank lines are kept as-is.
        ''' </summary>
        Friend Shared Function SplitIntoLines(text As String) As List(Of TextLine)
            Dim result As New List(Of TextLine)()
            Dim lines = text.Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)

            For Each line In lines
                Dim tl As New TextLine()
                Dim trimmed = line.Trim()
                If String.IsNullOrEmpty(trimmed) Then
                    tl.IsBlank = True
                Else
                    Dim sentences = Regex.Split(trimmed, "(?<=[.!?])\s+")
                    For Each s In sentences
                        Dim st = s.Trim()
                        If st.Length > 0 Then tl.Sentences.Add(st)
                    Next
                End If
                result.Add(tl)
            Next

            Return result
        End Function

    End Class
End Namespace
