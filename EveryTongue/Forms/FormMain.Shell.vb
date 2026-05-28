' FormMain.Shell.vb — UI shell event wiring and logic
' All controls are created in FormMain.Designer.vb / InitializeComponent().
' This file wires event handlers and contains shell logic only.

Imports System.Drawing
Imports System.IO.Compression

Partial Class FormMain

    ' ── Runtime state (not controls) ──────────────────────────────────
    Private _activeNavButton As ToolStripButton
    Private _liveStartTime As DateTime
    Private _formQr As FormQrCode
    Private _logPanelVisible As Boolean = False
    Private ReadOnly _unifiedLogBuffer As New System.Collections.Concurrent.ConcurrentQueue(Of (Time As DateTime, Source As String, Text As String, Color As Drawing.Color))
    Private ReadOnly _unifiedLogEntries As New List(Of (Time As DateTime, Source As String, Text As String, Color As Drawing.Color))
    Private _unifiedLogPending As Integer = 0
    Private Const UnifiedLogMaxLines As Integer = 3000
    Private _lastLogFilter As String = "All"
    Friend _logDarkMode As Boolean = True

    ' ── Constants ───────────────────────────────────────────────────
    Private Const NavRailWidth As Integer = 80
    Private Shared ReadOnly NavBackColor As Color = Color.FromArgb(37, 37, 38)
    Private Shared ReadOnly NavForeColor As Color = Color.FromArgb(200, 200, 200)
    Private Shared ReadOnly NavSelectedColor As Color = Color.FromArgb(0, 122, 204)
    Private Shared ReadOnly NavHoverColor As Color = Color.FromArgb(51, 51, 52)
    Private Shared ReadOnly NavAccentBar As Color = Color.FromArgb(0, 122, 204)

    Private _isFullScreen As Boolean = False
    Private _previousWindowState As FormWindowState
    Private _previousBorderStyle As FormBorderStyle

    ' ═══════════════════════════════════════════════════════════════
    ' InitializeShell — called from Form_Load after InitializeComponent
    ' ═══════════════════════════════════════════════════════════════
    Private Sub InitializeShell()
        ' ── Title and icon (runtime) ─────────────────────────────
        Dim ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version
        Me.Text = $"Every Tongue v{ver.Major}.{ver.Minor}.{ver.Build}"
        Me.Icon = Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        trayIcon.Icon = Me.Icon

        ' ── Populate font combo (runtime data) ────────────────────
        For Each fam In Drawing.FontFamily.Families
            cboSubtitleFont.Items.Add(fam.Name)
        Next

        ' ── Send labels to back (z-order fix) ────────────────────
        lblMode.SendToBack()
        For Each grp As Control In {grpInput, grpLiveInput, grpServerSettings}
            For Each child As Control In grp.Controls
                If TypeOf child Is Label Then child.SendToBack()
            Next
        Next

        ' ── Set nav button images (runtime font rendering) ────────
        btnNavLive.Image = RenderFontIcon(ChrW(&HE720), 28, NavForeColor)
        btnNavTranscribe.Image = RenderFontIcon(ChrW(&HE8D4), 28, NavForeColor)
        btnNavTranslate.Image = RenderFontIcon(ChrW(&HE774), 28, NavForeColor)
        btnNavBible.Image = RenderFontIcon(ChrW(&HE736), 28, NavForeColor)

        ' Custom renderer for toolbar theme
        tsNavBar.Renderer = New NavToolStripRenderer()

        ' ── Wire menu event handlers ─────────────────────────────
        AddHandler mnuFileNewSession.Click, Sub(s, e) LaunchSessionWizard()
        AddHandler mnuFileExportDiag.Click, Sub(s, e) ExportDiagnosticsAsync()
        AddHandler mnuFileExit.Click, Sub(s, e) ExitApplication()

        AddHandler mnuToolsTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)
        AddHandler mnuToolsTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)
        AddHandler mnuToolsBible.Click, Sub(s, e)
                                            SwitchWorkspace(tabPageBibleWs, btnNavBible)
                                            NavigateBibleView()
                                        End Sub
        AddHandler mnuToolsGlossary.Click, Sub(s, e)
                                                Using dlg As New FormFilterEditor(AppDomain.CurrentDomain.BaseDirectory, _config.LiveServerPort, _config.TranslationPort, _resMgr)
                                                    dlg.ShowDialog(Me)
                                                End Using
                                            End Sub
        AddHandler mnuToolsDownloadMgr.Click, Sub(s, e) OpenDownloadManager()
        AddHandler mnuToolsCheckDeps.Click, Sub(s, e) CheckDependenciesAsync()
        AddHandler mnuToolsVerifyPaths.Click, Sub(s, e) VerifyAllPaths()
        AddHandler mnuToolsPaths.Click, Sub(s, e) ShowOptionsDialog("paths")
        AddHandler mnuToolsServer.Click, Sub(s, e) ShowOptionsDialog("server")
        AddHandler mnuToolsOptions.Click, Sub(s, e) ShowOptionsDialog("general")

        AddHandler mnuSessionStart.Click, Sub(s, e)
                                               SwitchWorkspace(tabPageLive, btnNavLive)
                                               btnLiveStart.PerformClick()
                                           End Sub
        AddHandler mnuSessionStop.Click, Sub(s, e) btnLiveStop.PerformClick()
        AddHandler mnuSessionQR.Click, Sub(s, e) ShowQrCode()
        AddHandler mnuSessionCopyUrl.Click, Sub(s, e) CopyPhoneUrl()

        AddHandler mnuViewLogPanel.Click, Sub(s, e) ToggleLogPanel()
        AddHandler mnuViewThemeSystem.Click, Sub(s, e) SetThemeFromMenu("System")
        AddHandler mnuViewThemeLight.Click, Sub(s, e) SetThemeFromMenu("Light")
        AddHandler mnuViewThemeDark.Click, Sub(s, e) SetThemeFromMenu("Dark")
        AddHandler mnuViewFullScreen.Click, Sub(s, e) ToggleFullScreen()

        AddHandler mnuHelpQuickStart.Click, Sub(s, e) ShowLegacyTab(tabPageHelp)
        AddHandler mnuHelpShortcuts.Click, Sub(s, e)
                                                MessageBox.Show(
                                                    "Ctrl+1           Live workspace" & vbCrLf &
                                                    "Ctrl+2           Transcribe workspace" & vbCrLf &
                                                    "Ctrl+3           Translate workspace" & vbCrLf &
                                                    "Ctrl+4           Bible workspace" & vbCrLf &
                                                    "Ctrl+N           New Session wizard" & vbCrLf &
                                                    "Ctrl+L           Toggle Log Panel" & vbCrLf &
                                                    "F1               Help" & vbCrLf &
                                                    "F5               Start Live" & vbCrLf &
                                                    "Shift+F5         Stop Live" & vbCrLf &
                                                    "F10              Options" & vbCrLf &
                                                    "F11              Full Screen",
                                                    "Keyboard Shortcuts",
                                                    MessageBoxButtons.OK, MessageBoxIcon.Information)
                                            End Sub
        AddHandler mnuHelpUpdates.Click, Sub(s, e) CheckForUpdatesAsync()
        AddHandler mnuHelpAbout.Click, Sub(s, e)
                                            Using dlg As New FormAbout()
                                                dlg.ShowDialog(Me)
                                            End Using
                                        End Sub

        ' ── Wire nav rail button handlers ────────────────────────
        AddHandler btnNavLive.Click, Sub(s, e) SwitchWorkspace(tabPageLive, btnNavLive)
        AddHandler btnNavTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)
        AddHandler btnNavTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)
        AddHandler btnNavBible.Click, Sub(s, e)
                                          SwitchWorkspace(tabPageBibleWs, btnNavBible)
                                          NavigateBibleView()
                                      End Sub

        ' ── Restore log panel height from config ────────────────
        If _config.LogPanelHeight > 50 Then pnlLogPanel.Height = _config.LogPanelHeight
        AddHandler splitterLog.SplitterMoved, Sub(s, e)
                                                   _config.LogPanelHeight = pnlLogPanel.Height
                                                   SaveUiToConfig()
                                               End Sub

        ' ── Wire status bar handlers ─────────────────────────────
        AddHandler tslLogToggle.Click, Sub(s, e) ToggleLogPanel()
        AddHandler liveElapsedTimer.Tick, Sub(s, e)
                                               Dim elapsed = DateTime.Now - _liveStartTime
                                               tslElapsed.Text = elapsed.ToString("hh\:mm\:ss")
                                           End Sub

        ' ── Wire log panel handlers ──────────────────────────────
        AddHandler cboLogFilter.SelectedIndexChanged, Sub(s, e)
                                                           _lastLogFilter = "" ' Force re-render
                                                           FlushUnifiedLog()
                                                       End Sub
        AddHandler btnLogClear.Click, Sub(s, e)
                                           rtbUnifiedLog.Clear()
                                           _unifiedLogEntries.Clear()
                                           rtbUnifiedLog.Tag = 0
                                       End Sub
        AddHandler btnLogCopy.Click, Sub(s, e)
                                          If rtbUnifiedLog.TextLength > 0 Then
                                              Clipboard.SetText(rtbUnifiedLog.Text)
                                          End If
                                      End Sub
        AddHandler btnLogSearchNext.Click, Sub(s, e) SearchLog()
        AddHandler txtLogSearch.KeyDown, Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  SearchLog()
                                              End If
                                          End Sub

        ' ── Wire translate workspace handlers ────────────────────
        AddHandler btnTransSwap.Click, Sub(s, e) SwapTranslateLanguages()
        AddHandler btnTranslate.Click, Sub(s, e) RunTranslateAsync()
        AddHandler btnTransCopy.Click, Sub(s, e)
                                            If Not String.IsNullOrEmpty(txtTransInput.Text) Then
                                                Clipboard.SetText(txtTransInput.Text)
                                            End If
                                        End Sub
        AddHandler btnTransClear.Click, Sub(s, e) txtTransInput.Clear()
        AddHandler btnTransOutCopy.Click, Sub(s, e)
                                              If Not String.IsNullOrEmpty(txtTransOutput.Text) Then
                                                  Clipboard.SetText(txtTransOutput.Text)
                                              End If
                                          End Sub
        AddHandler btnTransOutClear.Click, Sub(s, e) txtTransOutput.Clear()

        ' ── Populate translate language dropdowns ─────────────────
        For Each lang In _whisperLanguages
            If lang = "auto" Then Continue For
            Dim display = LangDisplayName(lang)
            cboTransSource.Items.Add(display)
            cboTransTarget.Items.Add(display)
        Next
        cboTransSource.Items.Insert(0, LangDisplayName("auto"))
        cboTransSource.SelectedIndex = 0
        For i = 0 To cboTransTarget.Items.Count - 1
            If cboTransTarget.Items(i).ToString().StartsWith("English") Then
                cboTransTarget.SelectedIndex = i
                Exit For
            End If
        Next

        ' ── Keyboard shortcuts ─────────────────────────────────────
        AddHandler Me.KeyDown, AddressOf ShellKeyDown

        ' ── Default to Live workspace ──────────────────────────────
        SwitchWorkspace(tabPageLive, btnNavLive)

        ' ── Portable mode detection ────────────────────────────────
        Dim flagPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portable.flag")
        If IO.File.Exists(flagPath) Then
            tslServerStatus.Text = "Portable | " & tslServerStatus.Text
        End If
    End Sub

    Private Sub ShellKeyDown(sender As Object, e As KeyEventArgs)
        If e.Control Then
            Select Case e.KeyCode
                Case Keys.D1 : SwitchWorkspace(tabPageLive, btnNavLive) : e.Handled = True
                Case Keys.D2 : SwitchWorkspace(tabPageJob, btnNavTranscribe) : e.Handled = True
                Case Keys.D3 : SwitchWorkspace(tabPageTranslate, btnNavTranslate) : e.Handled = True
                Case Keys.D4 : SwitchWorkspace(tabPageBibleWs, btnNavBible) : NavigateBibleView() : e.Handled = True
            End Select
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Font Icon Rendering
    ' ═══════════════════════════════════════════════════════════════

    ''' <summary>Maps nav button label back to its MDL2 icon glyph.</summary>
    Private Shared Function GetNavIcon(label As String) As String
        Select Case label
            Case "Live" : Return ChrW(&HE720)
            Case "Transcribe" : Return ChrW(&HE8D4)
            Case "Translate" : Return ChrW(&HE774)
            Case "Bible" : Return ChrW(&HE736)
            Case Else : Return ChrW(&HE700)
        End Select
    End Function

    ''' <summary>Renders a Segoe MDL2 Assets glyph to a bitmap.</summary>
    Private Shared Function RenderFontIcon(glyph As String, size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g = Graphics.FromImage(bmp)
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
            Using fnt As New Font("Segoe MDL2 Assets", size * 0.65F, FontStyle.Regular, GraphicsUnit.Pixel)
                Dim sz = g.MeasureString(glyph, fnt)
                Dim x = (size - sz.Width) / 2
                Dim y = (size - sz.Height) / 2
                Using br As New SolidBrush(color)
                    g.DrawString(glyph, fnt, br, x, y)
                End Using
            End Using
        End Using
        Return bmp
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Translate Workspace Logic
    ' ═══════════════════════════════════════════════════════════════

    Private Sub SwapTranslateLanguages()
        If cboTransSource.SelectedIndex < 0 OrElse cboTransTarget.SelectedIndex < 0 Then Return
        Dim srcText = cboTransSource.Text
        Dim tgtText = cboTransTarget.Text
        ' Don't swap if source is "Auto Detect"
        If srcText.StartsWith("Auto") Then Return
        cboTransSource.Text = tgtText
        cboTransTarget.Text = srcText
        ' Also swap the text content
        Dim tmp = txtTransInput.Text
        txtTransInput.Text = txtTransOutput.Text
        txtTransOutput.Text = tmp
    End Sub

    Private Async Sub RunTranslateAsync()
        Dim inputText = txtTransInput.Text.Trim()
        If String.IsNullOrEmpty(inputText) Then Return

        Dim sourceWhisper = LangCodeFromDisplay(cboTransSource.Text)
        Dim targetWhisper = LangCodeFromDisplay(cboTransTarget.Text)

        ' Convert whisper ISO codes to NLLB-200 codes for the translation server
        Dim sourceLang = If(sourceWhisper = "auto", "eng_Latn",
            Pipeline.TranslationService.WhisperToNllbLang(sourceWhisper))
        Dim targetLang = Pipeline.TranslationService.WhisperToNllbLang(targetWhisper)

        If String.IsNullOrEmpty(targetLang) Then
            lblTransStatus.Text = $"Unsupported target language: {targetWhisper}"
            lblTransStatus.ForeColor = Color.Red
            Return
        End If

        btnTranslate.Enabled = False
        txtTransOutput.Text = ""

        ' Ensure translation service is running (auto-start if needed)
        If Not Await EnsureTranslationServiceAsync() Then
            btnTranslate.Enabled = True
            Return
        End If

        ' Split input into sentences so the NLLB model translates each one properly
        Dim sentences = SplitIntoSentences(inputText)
        lblTransStatus.Text = $"Translating ({sentences.Count} segment(s))..."
        lblTransStatus.ForeColor = Color.FromArgb(0, 122, 204)

        Try
            Dim port = _config.TranslationPort
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(60)
                Dim url = $"http://127.0.0.1:{port}/translate"
                Dim results As New System.Text.StringBuilder()

                For idx = 0 To sentences.Count - 1
                    Dim sentence = sentences(idx)
                    If String.IsNullOrWhiteSpace(sentence) Then
                        results.AppendLine()
                        Continue For
                    End If

                    lblTransStatus.Text = $"Translating {idx + 1}/{sentences.Count}..."

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
                                If results.Length > 0 Then results.Append(" ")
                                results.Append(resultEl.GetString())
                            End If
                        End If
                    Else
                        lblTransStatus.Text = $"Error: {response.StatusCode}"
                        lblTransStatus.ForeColor = Color.Red
                        txtTransOutput.Text = results.ToString()
                        Return
                    End If
                Next

                txtTransOutput.Text = results.ToString()
                lblTransStatus.Text = "Done"
                lblTransStatus.ForeColor = Color.Green
            End Using
        Catch ex As System.Net.Http.HttpRequestException
            lblTransStatus.Text = "Translation server connection failed — try again"
            lblTransStatus.ForeColor = Color.Red
        Catch ex As TaskCanceledException
            lblTransStatus.Text = "Request timed out"
            lblTransStatus.ForeColor = Color.Red
        Catch ex As Exception
            lblTransStatus.Text = $"Error: {ex.Message}"
            lblTransStatus.ForeColor = Color.Red
        Finally
            btnTranslate.Enabled = True
        End Try
    End Sub

    ''' <summary>
    ''' Ensures the translation service is started and model loaded.
    ''' Auto-starts the service if needed and waits for readiness.
    ''' </summary>
    Private Async Function EnsureTranslationServiceAsync() As Task(Of Boolean)
        WriteDebugLog($"[TRANSLATE] EnsureTranslationServiceAsync: service={_translationService IsNot Nothing}, isRunning={_translationService?.IsRunning}, isModelLoaded={_translationService?.IsModelLoaded}, config.TranslationEnabled={_config.TranslationEnabled}")

        ' Start if not running
        If _translationService Is Nothing OrElse Not _translationService.IsRunning Then
            Dim deps = Pipeline.TranslationService.CheckDependenciesInstalled()
            WriteDebugLog($"[TRANSLATE] Deps check: pythonOk={deps.pythonOk}, depsOk={deps.depsOk}, modelOk={deps.modelOk}")
            If Not deps.pythonOk OrElse Not deps.depsOk OrElse Not deps.modelOk Then
                lblTransStatus.Text = "Translation dependencies not installed. Use Tools > Download Manager."
                lblTransStatus.ForeColor = Color.Red
                Return False
            End If
            lblTransStatus.Text = "Starting translation engine..."
            lblTransStatus.ForeColor = Color.FromArgb(0, 122, 204)
            WriteDebugLog("[TRANSLATE] Calling StartTranslationService...")
            StartTranslationService()
        End If

        ' Guard against null service (e.g. TranslationEnabled=False)
        If _translationService Is Nothing Then
            WriteDebugLog("[TRANSLATE] Service is Nothing after start attempt — TranslationEnabled is likely False")
            lblTransStatus.Text = "Translation is disabled in settings"
            lblTransStatus.ForeColor = Color.Red
            Return False
        End If

        ' Reload model if server is running but model was unloaded
        If _translationService.IsRunning AndAlso Not _translationService.IsModelLoaded Then
            WriteDebugLog("[TRANSLATE] Server running but model not loaded — reloading...")
            lblTransStatus.Text = "Loading translation model..."
            lblTransStatus.ForeColor = Color.FromArgb(0, 122, 204)
            Try
                Await _translationService.LoadModelAsync()
            Catch ex As Exception
                WriteDebugLog($"[TRANSLATE] LoadModelAsync failed: {ex.Message}")
            End Try
        End If

        ' Wait for model to load (up to 120s)
        If Not _translationService.IsModelLoaded Then
            WriteDebugLog("[TRANSLATE] Waiting for model to load (up to 120s)...")
            lblTransStatus.Text = "Waiting for translation model..."
            lblTransStatus.ForeColor = Color.FromArgb(0, 122, 204)
            Dim waited = 0
            While Not _translationService.IsModelLoaded AndAlso waited < 120000
                Await Task.Delay(500)
                waited += 500
            End While
            WriteDebugLog($"[TRANSLATE] Wait complete: waited={waited}ms, isModelLoaded={_translationService.IsModelLoaded}")
            If Not _translationService.IsModelLoaded Then
                lblTransStatus.Text = "Translation model failed to load"
                lblTransStatus.ForeColor = Color.Red
                Return False
            End If
        End If

        WriteDebugLog("[TRANSLATE] Translation service ready")
        Return True
    End Function

    ''' <summary>Split text into sentences, preserving blank lines as empty entries.</summary>
    Private Shared Function SplitIntoSentences(text As String) As List(Of String)
        Dim result As New List(Of String)()
        Dim lines = text.Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)
        For Each line In lines
            Dim trimmed = line.Trim()
            If String.IsNullOrEmpty(trimmed) Then
                result.Add("")
                Continue For
            End If
            Dim sentences = System.Text.RegularExpressions.Regex.Split(
                trimmed, "(?<=[.!?])\s+")
            For Each s In sentences
                Dim st = s.Trim()
                If st.Length > 0 Then result.Add(st)
            Next
        Next
        Return result
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Bible Workspace Logic
    ' ═══════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Navigates the Bible WebView2 to the server's Bible tab.
    ''' Called after the server starts.
    ''' </summary>
    Private Sub NavigateBibleView()
        WriteDebugLog($"[BIBLE] NavigateBibleView called — wvBible={wvBible IsNot Nothing}, _serverPort={_serverPort}")
        If wvBible Is Nothing OrElse _serverPort = 0 Then
            WriteDebugLog("[BIBLE] NavigateBibleView aborted: wvBible or port is not ready")
            Return
        End If
        Try
            Dim rawLang = _config?.OutputLanguage
            Dim lang = If(rawLang, "en")
            If String.IsNullOrEmpty(lang) OrElse lang = "auto" Then lang = "en"
            ' Convert 2-letter Whisper code to 3-letter ISO 639-3 for Bible matching
            lang = WhisperToIso3(lang)
            WriteDebugLog($"[BIBLE] NavigateBibleView: rawOutputLang={rawLang}, resolvedLang={lang}, configInputLang={_config?.Language}, configBiblesDir={_config?.BiblesDirectory}")
            Dim bust = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            Dim url = $"http://127.0.0.1:{_serverPort}/?bibleLang={lang}&preview=1&_cb={bust}"
            WriteDebugLog($"[BIBLE] Navigating to: {url}")
            wvBible.Source = New Uri(url)
            wvBible.Visible = True
            lblBibleStatus.Visible = False
        Catch ex As Exception
            WriteDebugLog($"[ERROR] NavigateBibleView: {ex.Message}")
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Unified Log Panel
    ' ═══════════════════════════════════════════════════════════════

    Private Sub ToggleLogPanel()
        _logPanelVisible = Not _logPanelVisible
        ApplyLogPanelState()
    End Sub

    Friend Sub ShowLogPanel()
        If _logPanelVisible Then Return
        _logPanelVisible = True
        ApplyLogPanelState()
    End Sub

    Private Sub ApplyLogPanelState()
        pnlLogPanel.Visible = _logPanelVisible
        splitterLog.Visible = _logPanelVisible
        mnuViewLogPanel.Checked = _logPanelVisible
        If _logPanelVisible Then
            mnuViewLogPanel.Text = "Hide Log Panel"
            FlushUnifiedLog()
        Else
            mnuViewLogPanel.Text = "Show Log Panel"
        End If
    End Sub

    ''' <summary>
    ''' Appends a message to the unified log panel. Thread-safe.
    ''' </summary>
    Friend Sub AppendUnifiedLog(source As String, text As String, color As Drawing.Color)
        If rtbUnifiedLog Is Nothing Then Return

        _unifiedLogBuffer.Enqueue((DateTime.Now, source, text, color))

        If Threading.Interlocked.CompareExchange(_unifiedLogPending, 1, 0) = 0 Then
            If rtbUnifiedLog.IsHandleCreated Then
                rtbUnifiedLog.BeginInvoke(Sub() FlushUnifiedLog())
            Else
                Threading.Interlocked.Exchange(_unifiedLogPending, 0)
            End If
        End If
    End Sub

    Private Sub FlushUnifiedLog()
        Threading.Interlocked.Exchange(_unifiedLogPending, 0)

        Dim filter = ""
        If cboLogFilter.InvokeRequired Then
            filter = CStr(cboLogFilter.Invoke(Function() If(cboLogFilter.SelectedItem, "All").ToString()))
        Else
            filter = If(cboLogFilter.SelectedItem, "All").ToString()
        End If

        ' Drain queue into backing list
        Dim entry As (Time As DateTime, Source As String, Text As String, Color As Drawing.Color) = (DateTime.MinValue, "", "", Color.White)
        While _unifiedLogBuffer.TryDequeue(entry)
            _unifiedLogEntries.Add(entry)
        End While

        ' Trim backing list
        If _unifiedLogEntries.Count > UnifiedLogMaxLines Then
            _unifiedLogEntries.RemoveRange(0, _unifiedLogEntries.Count - UnifiedLogMaxLines)
        End If

        ' If filter changed, do a full re-render
        If filter <> _lastLogFilter Then
            _lastLogFilter = filter
            RenderUnifiedLog(filter)
            Return
        End If

        ' Append only new entries
        SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
        Try
            For i = Math.Max(0, _unifiedLogEntries.Count - 500) To _unifiedLogEntries.Count - 1
                Dim e = _unifiedLogEntries(i)
                ' Only append entries that haven't been rendered yet
                ' We track rendered count via rtbUnifiedLog.Tag
                Dim rendered = If(TypeOf rtbUnifiedLog.Tag Is Integer, CInt(rtbUnifiedLog.Tag), 0)
                If i < rendered Then Continue For

                If filter <> "All" AndAlso Not e.Source.Equals(filter, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                AppendLogEntry(e)
            Next
            rtbUnifiedLog.Tag = _unifiedLogEntries.Count
        Finally
            SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            rtbUnifiedLog.Invalidate()
        End Try

        SendMessage(rtbUnifiedLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
    End Sub

    ''' <summary>Re-renders the entire unified log for the given filter.</summary>
    Private Sub RenderUnifiedLog(filter As String)
        SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
        Try
            rtbUnifiedLog.Clear()
            For Each e In _unifiedLogEntries
                If filter <> "All" AndAlso Not e.Source.Equals(filter, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                AppendLogEntry(e)
            Next
            rtbUnifiedLog.Tag = _unifiedLogEntries.Count
        Finally
            SendMessage(rtbUnifiedLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
            rtbUnifiedLog.Invalidate()
        End Try
        SendMessage(rtbUnifiedLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
    End Sub

    Private _logSearchPos As Integer = 0

    Private Sub SearchLog()
        Dim keyword = txtLogSearch.Text.Trim()
        If keyword.Length = 0 Then Return

        Dim startAt = If(_logSearchPos < rtbUnifiedLog.TextLength, _logSearchPos, 0)
        Dim pos = rtbUnifiedLog.Find(keyword, startAt, RichTextBoxFinds.None)
        If pos < 0 AndAlso startAt > 0 Then
            ' Wrap around from the beginning
            pos = rtbUnifiedLog.Find(keyword, 0, RichTextBoxFinds.None)
        End If
        If pos >= 0 Then
            rtbUnifiedLog.Select(pos, keyword.Length)
            rtbUnifiedLog.SelectionBackColor = Color.FromArgb(80, 80, 0)
            rtbUnifiedLog.SelectionColor = Color.Yellow
            rtbUnifiedLog.ScrollToCaret()
            _logSearchPos = pos + keyword.Length
        Else
            _logSearchPos = 0
            Media.SystemSounds.Beep.Play()
        End If
    End Sub

    Private Sub AppendLogEntry(entry As (Time As DateTime, Source As String, Text As String, Color As Drawing.Color))
        Dim timeColor = If(_logDarkMode, Color.FromArgb(150, 150, 150), Color.FromArgb(130, 130, 130))
        Dim sourceColor = If(_logDarkMode, Color.FromArgb(120, 120, 120), Color.FromArgb(150, 150, 150))
        Dim textColor = LogColorForTheme(entry.Color)

        rtbUnifiedLog.SelectionStart = rtbUnifiedLog.TextLength
        rtbUnifiedLog.SelectionLength = 0
        rtbUnifiedLog.SelectionColor = timeColor
        rtbUnifiedLog.AppendText($"{entry.Time:HH:mm:ss} ")
        rtbUnifiedLog.SelectionStart = rtbUnifiedLog.TextLength
        rtbUnifiedLog.SelectionColor = sourceColor
        rtbUnifiedLog.AppendText($"[{entry.Source}] ")
        rtbUnifiedLog.SelectionStart = rtbUnifiedLog.TextLength
        rtbUnifiedLog.SelectionColor = textColor
        rtbUnifiedLog.AppendText($"{entry.Text}{Environment.NewLine}")
    End Sub

    ''' <summary>
    ''' Remaps a log entry color so it's readable on the current log background.
    ''' </summary>
    Private Function LogColorForTheme(c As Color) As Color
        If _logDarkMode Then
            ' Dark background — ensure brightness is high enough
            Dim brightness = (CInt(c.R) * 299 + CInt(c.G) * 587 + CInt(c.B) * 114) \ 1000
            If brightness < 90 Then
                ' Too dark to read on dark bg — lighten it
                Return Color.FromArgb(
                    Math.Min(255, c.R + 140),
                    Math.Min(255, c.G + 140),
                    Math.Min(255, c.B + 140))
            End If
            Return c
        Else
            ' Light background — ensure brightness is low enough
            Dim brightness = (CInt(c.R) * 299 + CInt(c.G) * 587 + CInt(c.B) * 114) \ 1000
            If brightness > 180 Then
                ' Too light to read on white bg — darken it
                Return Color.FromArgb(
                    Math.Max(0, c.R - 140),
                    Math.Max(0, c.G - 140),
                    Math.Max(0, c.B - 140))
            End If
            Return c
        End If
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Workspace Switching
    ' ═══════════════════════════════════════════════════════════════
    Private Sub GetNavThemeColors(ByRef inactiveBg As Color, ByRef inactiveFg As Color, ByRef activeBg As Color, ByRef hoverBg As Color)
        Select Case If(_config?.Theme, "System").ToLower()
            Case "light"
                inactiveBg = Color.FromArgb(240, 240, 240)
                inactiveFg = Color.FromArgb(60, 60, 60)
                activeBg = Color.FromArgb(0, 122, 204)
                hoverBg = Color.FromArgb(220, 220, 220)
            Case "dark"
                inactiveBg = NavBackColor
                inactiveFg = NavForeColor
                activeBg = NavSelectedColor
                hoverBg = NavHoverColor
            Case Else
                inactiveBg = SystemColors.Control
                inactiveFg = SystemColors.ControlText
                activeBg = Color.FromArgb(0, 122, 204)
                hoverBg = SystemColors.ControlLight
        End Select
    End Sub

    Private Sub SwitchWorkspace(tabPage As TabPage, navButton As ToolStripButton)
        If tabMain.SelectedTab IsNot tabPage Then
            tabMain.SelectedTab = tabPage
        End If

        Dim inactiveBg, inactiveFg, activeBg, hoverBg As Color
        GetNavThemeColors(inactiveBg, inactiveFg, activeBg, hoverBg)

        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = inactiveBg
            _activeNavButton.ForeColor = inactiveFg
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Text), 28, inactiveFg)
        End If

        navButton.BackColor = activeBg
        navButton.ForeColor = Color.White
        navButton.Image = RenderFontIcon(GetNavIcon(navButton.Text), 28, Color.White)
        _activeNavButton = navButton
    End Sub

    ''' <summary>
    ''' Temporarily show a legacy tab (Paths, Settings, Server, Help)
    ''' that doesn't have a nav rail button yet.
    ''' </summary>
    Private Sub ShowLegacyTab(tabPage As TabPage)
        If _activeNavButton IsNot Nothing Then
            Dim inactiveBg, inactiveFg, activeBg, hoverBg As Color
            GetNavThemeColors(inactiveBg, inactiveFg, activeBg, hoverBg)

            _activeNavButton.BackColor = inactiveBg
            _activeNavButton.ForeColor = inactiveFg
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Text), 28, inactiveFg)
            _activeNavButton = Nothing
        End If

        tabMain.SelectedTab = tabPage
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Shell helpers
    ' ═══════════════════════════════════════════════════════════════

    Private Sub LaunchSessionWizard()
        Dim devices As New List(Of String)
        For Each item In cboLiveDevice.Items
            Dim text = item.ToString()
            If text <> "Detecting devices..." Then devices.Add(text)
        Next

        Using dlg As New FormSessionWizard(_config, devices.ToArray(), _whisperLanguages, _langNames)
            If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.StartSession Then
                LoadConfigToUi()
                SwitchWorkspace(tabPageLive, btnNavLive)
                btnLiveStart.PerformClick()
            End If
        End Using
    End Sub

    Private Sub ShowOptionsDialog(Optional category As String = "general")
        Using dlg As New FormOptions(_config, _uiLocales)
            dlg.SelectCategory(category)
            If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.ConfigChanged Then
                LoadConfigToUi()
                ApplyTheme(_config.Theme)
                If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()
            End If
        End Using
    End Sub

    Private Sub ShowQrCode()
        If _serverPort = 0 Then
            MessageBox.Show("The server is not running. Start a live session first.",
                "QR Code", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim localIp = GetLocalIpAddress()
        Dim url = $"https://{localIp}:{_serverPort + 1}"

        If _formQr Is Nothing OrElse _formQr.IsDisposed Then
            _formQr = New FormQrCode(url)
            _formQr.Show(Me)
        Else
            _formQr.UpdateUrl(url)
            _formQr.BringToFront()
        End If
    End Sub

    Private Async Sub ExportDiagnosticsAsync()
        Dim hwInfo = Await Threading.Tasks.Task.Run(
            Function() Services.Infrastructure.HardwareScanner.Scan())

        Using dlg As New SaveFileDialog()
            dlg.Filter = "ZIP Archive|*.zip"
            dlg.FileName = $"EveryTongue_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.zip"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Try
                Using zipStream As New IO.FileStream(dlg.FileName, IO.FileMode.Create)
                    Using archive As New IO.Compression.ZipArchive(zipStream, IO.Compression.ZipArchiveMode.Create)
                        Dim infoEntry = archive.CreateEntry("system_info.txt")
                        Using writer As New IO.StreamWriter(infoEntry.Open())
                            writer.WriteLine($"Every Tongue Diagnostics — {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            writer.WriteLine($"Version: {Reflection.Assembly.GetExecutingAssembly().GetName().Version}")
                            writer.WriteLine($"OS: {Environment.OSVersion}")
                            writer.WriteLine($".NET: {Environment.Version}")
                            writer.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}")
                            writer.WriteLine($"64-bit process: {Environment.Is64BitProcess}")
                            writer.WriteLine()
                            writer.WriteLine("=== Hardware ===")
                            writer.WriteLine($"GPU: {hwInfo.GpuName} ({hwInfo.GpuMemoryMB} MB) — Score: {hwInfo.GpuScore}/100")
                            writer.WriteLine($"CPU: {hwInfo.CpuName} ({hwInfo.CpuCores} cores) — Score: {hwInfo.CpuScore}/100")
                            writer.WriteLine($"RAM: {hwInfo.RamTotalMB} MB — Score: {hwInfo.RamScore}/100")
                            writer.WriteLine($"Disk free: {hwInfo.DiskFreeMB} MB — Score: {hwInfo.DiskScore}/100")
                            writer.WriteLine($"Overall: {hwInfo.OverallScore}/100 ({hwInfo.Rating})")
                            writer.WriteLine()
                            writer.WriteLine("=== Configuration ===")
                            For Each prop In GetType(Models.AppConfig).GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
                                If Not prop.CanRead Then Continue For
                                Dim val = prop.GetValue(_config)
                                Dim displayVal = If(prop.Name.IndexOf("Pin", StringComparison.OrdinalIgnoreCase) >= 0, "****", val?.ToString())
                                writer.WriteLine($"  {prop.Name} = {displayVal}")
                            Next
                        End Using

                        If rtbServerLog.TextLength > 0 Then
                            Dim logEntry = archive.CreateEntry("server_log.txt")
                            Using writer As New IO.StreamWriter(logEntry.Open())
                                writer.Write(rtbServerLog.Text)
                            End Using
                        End If

                        If rtbUnifiedLog.TextLength > 0 Then
                            Dim logEntry = archive.CreateEntry("unified_log.txt")
                            Using writer As New IO.StreamWriter(logEntry.Open())
                                writer.Write(rtbUnifiedLog.Text)
                            End Using
                        End If

                        Dim debugLogPath = GetPipelineLogPath()
                        If IO.File.Exists(debugLogPath) Then
                            archive.CreateEntryFromFile(debugLogPath, "debug_log.txt")
                        End If
                    End Using
                End Using

                MessageBox.Show($"Diagnostics exported to:{Environment.NewLine}{dlg.FileName}",
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show($"Export failed: {ex.Message}",
                    "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Sub SetThemeFromMenu(theme As String)
        _config.Theme = theme
        Models.ConfigManager.Save(_config)
        ApplyTheme(theme)
    End Sub

    Private Sub ToggleFullScreen()
        If _isFullScreen Then
            Me.FormBorderStyle = _previousBorderStyle
            Me.WindowState = _previousWindowState
            menuMain.Visible = True
            statusMain.Visible = True
            _isFullScreen = False
        Else
            _previousWindowState = Me.WindowState
            _previousBorderStyle = Me.FormBorderStyle
            Me.WindowState = FormWindowState.Normal
            Me.FormBorderStyle = FormBorderStyle.None
            Me.WindowState = FormWindowState.Maximized
            menuMain.Visible = False
            statusMain.Visible = False
            _isFullScreen = True
        End If
    End Sub

    ''' <summary>
    ''' Updates the status bar with current server and live session state.
    ''' </summary>
    Private Sub UpdateShellStatus()
        If statusMain Is Nothing Then Return

        If _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning Then
            tslServerStatus.Text = $"Server: Running :{_serverPort}"
            tslServerStatus.ForeColor = Color.Green
        Else
            tslServerStatus.Text = "Server: Stopped"
            tslServerStatus.ForeColor = Color.Gray
        End If

        Dim svc = SubtitleSvc
        Dim clients = If(svc?.ConnectedClients, 0)
        tslClients.Text = $"Clients: {clients}"

        If _liveRunner IsNot Nothing AndAlso _liveRunner.IsRunning Then
            tslLiveStatus.Text = "Live: Running"
            tslLiveStatus.ForeColor = Color.Green
            If Not liveElapsedTimer.Enabled Then
                _liveStartTime = DateTime.Now
                liveElapsedTimer.Start()
            End If
        Else
            tslLiveStatus.Text = "Ready"
            tslLiveStatus.ForeColor = Color.Gray
            If liveElapsedTimer.Enabled Then
                liveElapsedTimer.Stop()
                tslElapsed.Text = ""
            End If
        End If
    End Sub

    ''' <summary>
    ''' Applies shell-specific theming to the nav rail and status bar.
    ''' </summary>
    Private Sub ApplyShellTheme(theme As String)
        WriteDebugLog($"[THEME] ApplyShellTheme called with theme=""{theme}"", tsNavBar={tsNavBar IsNot Nothing}, activeBtn={_activeNavButton?.Text}")
        If tsNavBar Is Nothing Then Return

        Dim inactiveBg, inactiveFg, activeBg, hoverBg As Color
        GetNavThemeColors(inactiveBg, inactiveFg, activeBg, hoverBg)

        tsNavBar.BackColor = inactiveBg

        For Each item As ToolStripItem In tsNavBar.Items
            If TypeOf item Is ToolStripButton Then
                Dim btn = DirectCast(item, ToolStripButton)
                If btn IsNot _activeNavButton Then
                    btn.BackColor = inactiveBg
                    btn.ForeColor = inactiveFg
                    btn.Image = RenderFontIcon(GetNavIcon(btn.Text), 28, inactiveFg)
                End If
            End If
        Next

        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = activeBg
            _activeNavButton.ForeColor = Color.White
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Text), 28, Color.White)
        End If

        ' Theme the log panel
        If rtbUnifiedLog IsNot Nothing Then
            Select Case theme.ToLower()
                Case "light"
                    _logDarkMode = False
                    rtbUnifiedLog.BackColor = Color.FromArgb(255, 255, 255)
                    rtbUnifiedLog.ForeColor = Color.FromArgb(30, 30, 30)
                Case "dark"
                    _logDarkMode = True
                    rtbUnifiedLog.BackColor = Color.FromArgb(30, 30, 30)
                    rtbUnifiedLog.ForeColor = Color.FromArgb(200, 200, 200)
                Case Else
                    _logDarkMode = True
                    rtbUnifiedLog.BackColor = Color.FromArgb(30, 30, 30)
                    rtbUnifiedLog.ForeColor = Color.FromArgb(200, 200, 200)
            End Select
            ' Re-render with correct colors
            _lastLogFilter = ""
            FlushUnifiedLog()
        End If

        ' Theme the translate workspace
        If txtTransInput IsNot Nothing Then
            Select Case theme.ToLower()
                Case "dark"
                    txtTransInput.BackColor = Color.FromArgb(45, 45, 48)
                    txtTransInput.ForeColor = Color.FromArgb(220, 220, 220)
                    txtTransOutput.BackColor = Color.FromArgb(45, 45, 48)
                    txtTransOutput.ForeColor = Color.FromArgb(220, 220, 220)
                Case "light"
                    txtTransInput.BackColor = Color.White
                    txtTransInput.ForeColor = Color.Black
                    txtTransOutput.BackColor = Color.White
                    txtTransOutput.ForeColor = Color.Black
                Case Else
                    txtTransInput.BackColor = SystemColors.Window
                    txtTransInput.ForeColor = SystemColors.WindowText
                    txtTransOutput.BackColor = SystemColors.Window
                    txtTransOutput.ForeColor = SystemColors.WindowText
            End Select
        End If

        ' Update theme menu checkmarks
        If mnuViewThemeSystem IsNot Nothing Then
            mnuViewThemeSystem.Checked = theme.Equals("System", StringComparison.OrdinalIgnoreCase)
            mnuViewThemeLight.Checked = theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
            mnuViewThemeDark.Checked = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
        End If
    End Sub

    ''' <summary>
    ''' Map 2-letter Whisper/ISO 639-1 codes to 3-letter ISO 639-3 codes for Bible matching.
    ''' </summary>
    Private Shared Function WhisperToIso3(code As String) As String
        Static map As Dictionary(Of String, String) = Nothing
        If map Is Nothing Then
            map = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                {"af", "afr"}, {"am", "amh"}, {"ar", "arb"}, {"hy", "hye"}, {"az", "azj"},
                {"bn", "ben"}, {"bs", "bos"}, {"bg", "bul"}, {"my", "mya"}, {"ca", "cat"},
                {"zh", "zho"}, {"hr", "hrv"}, {"cs", "ces"}, {"cy", "cym"}, {"da", "dan"},
                {"nl", "nld"}, {"en", "eng"}, {"et", "est"}, {"fi", "fin"}, {"fr", "fra"},
                {"gl", "glg"}, {"ka", "kat"}, {"de", "deu"}, {"el", "ell"}, {"gu", "guj"},
                {"ha", "hau"}, {"he", "heb"}, {"hi", "hin"}, {"hu", "hun"}, {"is", "isl"},
                {"id", "ind"}, {"ga", "gle"}, {"it", "ita"}, {"ja", "jpn"}, {"jv", "jav"},
                {"kn", "kan"}, {"kk", "kaz"}, {"km", "khm"}, {"ko", "kor"}, {"lo", "lao"},
                {"lv", "lvs"}, {"lt", "lit"}, {"mk", "mkd"}, {"ms", "zsm"}, {"ml", "mal"},
                {"mt", "mlt"}, {"mi", "mri"}, {"mr", "mar"}, {"mn", "khk"}, {"ne", "npi"},
                {"no", "nob"}, {"fa", "pes"}, {"pl", "pol"}, {"pt", "por"}, {"pa", "pan"},
                {"ro", "ron"}, {"ru", "rus"}, {"sr", "srp"}, {"sk", "slk"}, {"sl", "slv"},
                {"so", "som"}, {"es", "spa"}, {"sw", "swh"}, {"sv", "swe"}, {"tl", "tgl"},
                {"ta", "tam"}, {"te", "tel"}, {"th", "tha"}, {"tr", "tur"}, {"uk", "ukr"},
                {"ur", "urd"}, {"uz", "uzn"}, {"vi", "vie"}, {"yo", "yor"}, {"zu", "zul"},
                {"sq", "sqi"}, {"si", "sin"}
            }
        End If
        Dim result As String = Nothing
        If map.TryGetValue(code, result) Then Return result
        Return code ' return as-is if no mapping
    End Function

End Class

''' <summary>
''' Custom ToolStrip renderer that uses each button's BackColor for fill
''' instead of the default system/professional theme.
''' </summary>
Friend Class NavToolStripRenderer
    Inherits ToolStripProfessionalRenderer

    Protected Overrides Sub OnRenderToolStripBackground(e As ToolStripRenderEventArgs)
        Using br As New Drawing.SolidBrush(e.ToolStrip.BackColor)
            e.Graphics.FillRectangle(br, e.AffectedBounds)
        End Using
    End Sub

    Protected Overrides Sub OnRenderButtonBackground(e As ToolStripItemRenderEventArgs)
        Dim btn = TryCast(e.Item, ToolStripButton)
        If btn Is Nothing Then
            MyBase.OnRenderButtonBackground(e)
            Return
        End If

        Dim bounds = New Drawing.Rectangle(Drawing.Point.Empty, btn.Size)
        Dim bgColor As Drawing.Color

        If btn.Pressed Then
            bgColor = Drawing.Color.FromArgb(0, 100, 180)
        ElseIf btn.Selected Then
            ' Hover — slightly lighter than bg
            Dim r = Math.Min(255, btn.BackColor.R + 20)
            Dim g = Math.Min(255, btn.BackColor.G + 20)
            Dim b = Math.Min(255, btn.BackColor.B + 20)
            bgColor = Drawing.Color.FromArgb(r, g, b)
        Else
            bgColor = btn.BackColor
        End If

        Using br As New Drawing.SolidBrush(bgColor)
            e.Graphics.FillRectangle(br, bounds)
        End Using
    End Sub

    Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
        ' No border — keep it flat
    End Sub
End Class
