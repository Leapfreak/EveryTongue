' FormMain.Shell.vb — UI shell event wiring and logic
' All controls are created in FormMain.Designer.vb / InitializeComponent().
' This file wires event handlers and contains shell logic only.

Imports System.Drawing
Imports System.Threading
Imports System.IO.Compression
Imports Microsoft.Extensions.DependencyInjection
Imports EveryTongue.Services.Infrastructure

Partial Class FormMain

    ' ── Controllers ───────────────────────────────────────────────────
    Private _bibleController As Controllers.BibleController
    Private _translateController As Controllers.TranslateController

    ' ── Runtime state (not controls) ──────────────────────────────────
    Private _activeNavButton As ToolStripButton
    Private _formQr As FormQrCode
    Private _logPanelVisible As Boolean = False
    Private ReadOnly _unifiedLogBuffer As New System.Collections.Concurrent.ConcurrentQueue(Of Services.Infrastructure.LogEntry)
    Private ReadOnly _unifiedLogEntries As New List(Of Services.Infrastructure.LogEntry)
    Private _unifiedLogPending As Integer = 0
    Private Const UnifiedLogMaxLines As Integer = 3000
    Private _logAutoScroll As Boolean = True
    Private _lastCategoryFilter As String = "All"
    Private _lastLevelFilter As String = "All"
    Friend _logDarkMode As Boolean = False

    ' ── Constants ───────────────────────────────────────────────────
    Private Shared ReadOnly NavBackColor As Color = Color.FromArgb(37, 37, 38)
    Private Shared ReadOnly NavForeColor As Color = Color.FromArgb(200, 200, 200)
    Private Shared ReadOnly NavSelectedColor As Color = Color.FromArgb(0, 122, 204)

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

        ' ── Send labels to back (z-order fix) ────────────────────
        lblMode.SendToBack()
        For Each child As Control In grpInput.Controls
            If TypeOf child Is Label Then child.SendToBack()
        Next

        ' ── Set nav button images + colours (theme-aware) ────────
        Dim inactiveBg, inactiveFg, activeBg As Color
        GetNavThemeColors(inactiveBg, inactiveFg, activeBg)

        tsNavBar.BackColor = inactiveBg
        btnNavLog.Image = RenderFontIcon(ChrW(&HE7BA), 28, inactiveFg)
        btnNavLog.ForeColor = inactiveFg
        btnNavTranscribe.Image = RenderFontIcon(ChrW(&HE8D4), 28, inactiveFg)
        btnNavTranscribe.ForeColor = inactiveFg
        btnNavTranslate.Image = RenderFontIcon(ChrW(&HE774), 28, inactiveFg)
        btnNavTranslate.ForeColor = inactiveFg
        btnNavBible.Image = RenderFontIcon(ChrW(&HE736), 28, inactiveFg)
        btnNavBible.ForeColor = inactiveFg

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
                                        End Sub
        AddHandler mnuToolsGlossary.Click, Sub(s, e)
                                                Using dlg As New FormFilterEditor(AppDomain.CurrentDomain.BaseDirectory, _config.LiveServerPort, _config.TranslationPort)
                                                    dlg.ShowDialog(Me)
                                                End Using
                                            End Sub
        AddHandler mnuToolsDownloadMgr.Click, Sub(s, e) OpenDownloadManager()
        AddHandler mnuToolsVerifyPaths.Click, Sub(s, e) VerifyAllPaths()
        AddHandler mnuToolsVerifyIntegrity.Click, Sub(s, e) VerifyFileIntegrity()
        AddHandler mnuToolsBenchmark.Click, Sub(s, e) OpenTranslationBenchmark()
        AddHandler mnuToolsOptions.Click, Sub(s, e) ShowOptionsDialog("general")

        AddHandler mnuSessionQR.Click, Sub(s, e) ShowQrCode()
        AddHandler mnuSessionCopyUrl.Click, Sub(s, e) _serverController?.CopyPhoneUrl()

        AddHandler mnuViewLogPanel.Click, Sub(s, e) ToggleLogPanel()
        AddHandler mnuViewThemeSystem.Click, Sub(s, e) SetThemeFromMenu(Models.ThemeMode.System)
        AddHandler mnuViewThemeLight.Click, Sub(s, e) SetThemeFromMenu(Models.ThemeMode.Light)
        AddHandler mnuViewThemeDark.Click, Sub(s, e) SetThemeFromMenu(Models.ThemeMode.Dark)
        AddHandler mnuViewClients.Click, Sub(s, e) ShowConnectedClients()
        AddHandler mnuViewFullScreen.Click, Sub(s, e) ToggleFullScreen()

        AddHandler mnuHelpQuickStart.Click, Sub(s, e) ShowLegacyTab(tabPageHelp)
        AddHandler mnuHelpShortcuts.Click, Sub(s, e)
                                                MessageBox.Show(
                                                    "Ctrl+0" & vbTab & "Log workspace" & vbCrLf &
                                                    "Ctrl+1" & vbTab & "Transcribe workspace" & vbCrLf &
                                                    "Ctrl+2" & vbTab & "Translate workspace" & vbCrLf &
                                                    "Ctrl+3" & vbTab & "Bible workspace" & vbCrLf &
                                                    "Ctrl+N" & vbTab & "New Session wizard" & vbCrLf &
                                                    "Ctrl+L" & vbTab & "Toggle Log Panel" & vbCrLf &
                                                    "F12" & vbTab & "Options" & vbCrLf &
                                                    "F1" & vbTab & "Help" & vbCrLf &
                                                    "F11" & vbTab & "Full Screen",
                                                    "Keyboard Shortcuts",
                                                    MessageBoxButtons.OK, MessageBoxIcon.Information)
                                            End Sub
        AddHandler mnuHelpHardware.Click, Sub(s, e) ShowOptionsDialog("hardware")
        AddHandler mnuHelpUpdates.Click, Sub(s, e) CheckForUpdatesAsync()
        AddHandler mnuHelpAbout.Click, Sub(s, e)
                                            Using dlg As New FormAbout()
                                                dlg.ShowDialog(Me)
                                            End Using
                                        End Sub

        ' ── Wire nav rail button handlers ────────────────────────
        AddHandler btnNavLog.Click, Sub(s, e) SwitchWorkspace(Nothing, btnNavLog)
        AddHandler btnNavTranscribe.Click, Sub(s, e) SwitchWorkspace(tabPageJob, btnNavTranscribe)
        AddHandler btnNavTranslate.Click, Sub(s, e) SwitchWorkspace(tabPageTranslate, btnNavTranslate)
        AddHandler btnNavBible.Click, Sub(s, e)
                                          SwitchWorkspace(tabPageBibleWs, btnNavBible)
                                      End Sub

        ' ── Restore log panel height from config ────────────────
        If _config.LogPanelHeight > 50 Then pnlLogPanel.Height = _config.LogPanelHeight
        AddHandler splitterLog.SplitterMoved, Sub(s, e)
                                                   _config.LogPanelHeight = pnlLogPanel.Height
                                                   SaveUiToConfig()
                                               End Sub

        ' ── Bible controller ────────────────────────────────────
        _bibleController = New Controllers.BibleController(
            cboBibleLang, cboBibleTrans, txtBibleRef, btnBibleGo,
            btnBibleBack, lblBibleNavTitle, flpBibleNav, rtbBibleText,
            lblBibleCopyright,
            cboBibleTransTo, btnBibleSpeak,
            AddressOf GetBibleService,
            Function() WhisperToIso3(If(_config?.OutputLanguage, "en")),
            Function() _serverController?.GetTranslationOrchestrator(),
            Function() _serverController?.GetTtsService(),
            Function() _serverController?.GetTtsCacheDirectory(),
            _sttLanguages,
            AddressOf LangDisplayName,
            AddressOf LangCodeFromDisplay,
            AddressOf GetString,
            AddressOf WriteDebugLog,
            _config)
        _bibleController.WireEvents()
        _bibleController.WireContextMenu(ctxBibleCopySelection, ctxBibleCopyVerse, ctxBibleCopyChapter)

        ' ── Wire status bar handlers ─────────────────────────────
        tslClients.IsLink = True
        tslClients.LinkBehavior = LinkBehavior.HoverUnderline
        tslClients.LinkColor = tslClients.ForeColor
        AddHandler tslClients.Click, Sub(s, e) ShowConnectedClients()
        AddHandler tslLogToggle.Click, Sub(s, e) ToggleLogPanel()

        ' ── Wire log panel handlers ──────────────────────────────
        ' Populate category filter from enum
        cboLogCategory.Items.Add("All")
        For Each cat In [Enum].GetValues(GetType(Services.Infrastructure.LogCategory)).Cast(Of Services.Infrastructure.LogCategory)()
            cboLogCategory.Items.Add(cat.ToString())
        Next
        cboLogCategory.SelectedIndex = 0
        cboLogLevel.SelectedIndex = 0

        AddHandler cboLogCategory.SelectedIndexChanged, Sub(s, e) RenderFilteredLog()
        AddHandler cboLogLevel.SelectedIndexChanged, Sub(s, e) RenderFilteredLog()
        AddHandler btnLogClear.Click, Sub(s, e)
                                           _unifiedLogEntries.Clear()
                                           dgvLog.Rows.Clear()
                                       End Sub
        AddHandler btnLogCopy.Click, Sub(s, e)
                                          Dim sb As New System.Text.StringBuilder()
                                          For Each row As DataGridViewRow In dgvLog.SelectedRows
                                              sb.AppendLine($"{row.Cells(0).Value}  {row.Cells(1).Value}  {row.Cells(2).Value}  {row.Cells(3).Value}")
                                          Next
                                          If sb.Length = 0 Then
                                              ' Nothing selected — copy all visible rows
                                              For Each row As DataGridViewRow In dgvLog.Rows
                                                  sb.AppendLine($"{row.Cells(0).Value}  {row.Cells(1).Value}  {row.Cells(2).Value}  {row.Cells(3).Value}")
                                              Next
                                          End If
                                          If sb.Length > 0 Then Clipboard.SetText(sb.ToString())
                                      End Sub
        AddHandler btnLogPause.Click, Sub(s, e)
                                           _logAutoScroll = Not _logAutoScroll
                                           btnLogPause.Text = If(_logAutoScroll, "Pause", ChrW(&H25B6))
                                       End Sub
        AddHandler btnLogSearchNext.Click, Sub(s, e) SearchLog()
        AddHandler txtLogSearch.KeyDown, Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  SearchLog()
                                              End If
                                          End Sub
        ' Enable double-buffered rendering on the DataGridView
        Dim dgvType = GetType(DataGridView)
        Dim pi = dgvType.GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
        pi?.SetValue(dgvLog, True)

        AddHandler mnuToolsLogConfig.Click, Sub(s, e) ShowLogConfig()
        AddHandler mnuToolsLogViewer.Click, Sub(s, e) ShowLogViewer()

        ' Show event description tooltip on hover
        dgvLog.ShowCellToolTips = True
        AddHandler dgvLog.CellToolTipTextNeeded, Sub(s, e2)
                                                      If e2.RowIndex < 0 OrElse e2.RowIndex >= dgvLog.Rows.Count Then Return
                                                      Dim entry = TryCast(dgvLog.Rows(e2.RowIndex).Tag, LogEntry)
                                                      If entry Is Nothing OrElse entry.EventId = 0 Then Return
                                                      Dim info = LogEventRegistry.Lookup(entry.EventId)
                                                      If info IsNot Nothing Then
                                                          e2.ToolTipText = $"[{info.Id}] {info.Description}"
                                                      End If
                                                  End Sub

        ' ── Wire translate workspace — delegated to TranslateController ──
        _translateController = New Controllers.TranslateController(
            _config,
            txtTransInput, txtTransOutput,
            cboTransSource, cboTransTarget,
            btnTransSwap, btnTranslate,
            btnTransCopy, btnTransClear,
            btnTransOutCopy, btnTransOutClear,
            btnTransSpeak,
            lblTransStatus,
            _sttLanguages,
            AddressOf LangDisplayName,
            AddressOf LangCodeFromDisplay,
            AddressOf StartTranslationService,
            Function() _translationService,
            Function() _serverController?.GetTranslationOrchestrator(),
            Function() _serverController?.GetTtsService(),
            Function() _serverController?.GetTtsCacheDirectory(),
            Sub(msg) AppLogger.Log(LogEvents.TRANS_REQUEST, msg),
            AddressOf GetString)
        _translateController.WireEvents()
        _translateController.WireContextMenus(ctxTransInputCut, ctxTransInputCopy, ctxTransInputPaste,
                                              ctxTransInputSelectAll, ctxTransOutputCopy, ctxTransOutputSelectAll)
        _translateController.PopulateLanguageDropdowns()

        ' ── Keep log panel full-height on Log workspace when form resizes ──
        AddHandler pnlContent.Resize, Sub(s, e)
                                           If _activeNavButton Is btnNavLog Then
                                               pnlLogPanel.Height = pnlContent.ClientSize.Height
                                           End If
                                       End Sub

        ' ── Position toolbar and dgv inside log panel (no docking) ──
        Dim layoutLogPanel = Sub()
                                  Dim w = pnlLogPanel.ClientSize.Width
                                  Dim h = pnlLogPanel.ClientSize.Height
                                  If w > 0 AndAlso h > 0 Then
                                      Dim tbH = 30
                                      dgvLog.SetBounds(0, 38, w, h - tbH - 38)
                                      pnlLogToolbar.SetBounds(0, h - tbH, w, tbH)
                                      pnlLogToolbar.BringToFront()
                                  End If
                              End Sub
        AddHandler pnlLogPanel.Resize, Sub(s, e) layoutLogPanel()

        ' ── Force layout once form is fully shown ──
        AddHandler Me.Shown, Sub(s, e)
                                  If _activeNavButton Is btnNavLog Then
                                      pnlLogPanel.Height = pnlContent.ClientSize.Height
                                  End If
                                  layoutLogPanel()
                              End Sub

        ' ── Keyboard shortcuts ─────────────────────────────────────
        AddHandler Me.KeyDown, AddressOf ShellKeyDown

        ' ── Default to Log workspace ──────────────────────────────
        SwitchWorkspace(Nothing, btnNavLog)

        ' ── Portable mode detection ────────────────────────────────
        Dim flagPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "portable.flag")
        If IO.File.Exists(flagPath) Then
            tslServerStatus.Text = GetString("Shell_Portable") & " | " & tslServerStatus.Text
        End If
    End Sub

    Private Sub ShellKeyDown(sender As Object, e As KeyEventArgs)
        If e.Control Then
            Select Case e.KeyCode
                Case Keys.D0 : SwitchWorkspace(Nothing, btnNavLog) : e.Handled = True
                Case Keys.D1 : SwitchWorkspace(tabPageJob, btnNavTranscribe) : e.Handled = True
                Case Keys.D2 : SwitchWorkspace(tabPageTranslate, btnNavTranslate) : e.Handled = True
                Case Keys.D3 : SwitchWorkspace(tabPageBibleWs, btnNavBible) : e.Handled = True
            End Select
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Font Icon Rendering
    ' ═══════════════════════════════════════════════════════════════

    ''' <summary>Maps nav button label back to its MDL2 icon glyph.</summary>
    Private Shared Function GetNavIcon(name As String) As String
        Select Case name
            Case "btnNavLog" : Return ChrW(&HE7BA)
            Case "btnNavTranscribe" : Return ChrW(&HE8D4)
            Case "btnNavTranslate" : Return ChrW(&HE774)
            Case "btnNavBible" : Return ChrW(&HE736)
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
    ' Bible Workspace — delegated to BibleController
    ' ═══════════════════════════════════════════════════════════════

    Private Sub InitBibleTab()
        _bibleController?.Initialize()
    End Sub

    Private Function GetBibleService() As Services.Interfaces.IBibleService
        Dim host = _serverController?.KestrelHost
        Return TryCast(host?.Services?.GetService(
            GetType(Services.Interfaces.IBibleService)), Services.Interfaces.IBibleService)
    End Function

    Friend Sub RefreshBibleTab()
        _bibleController?.Refresh()
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Unified Log Panel
    ' ═══════════════════════════════════════════════════════════════

    Private Sub ToggleLogPanel()
        ' If on Log workspace, clicking the log link is a no-op
        If _activeNavButton Is btnNavLog Then Return
        _logPanelVisible = Not _logPanelVisible
        ApplyLogPanelState()
    End Sub

    Friend Sub ShowLogPanel()
        If _logPanelVisible Then Return
        _logPanelVisible = True
        ApplyLogPanelState()
    End Sub

    Private Sub ApplyLogPanelState()
        ' Don't touch panel visibility when Log workspace is active —
        ' SwitchWorkspace manages it directly
        If _activeNavButton IsNot btnNavLog Then
            pnlLogPanel.Visible = _logPanelVisible
            splitterLog.Visible = _logPanelVisible
        End If
        mnuViewLogPanel.Checked = _logPanelVisible
        If _logPanelVisible Then
            mnuViewLogPanel.Text = GetString("Menu_ViewHideLogPanel")
            FlushUnifiedLog()
        Else
            mnuViewLogPanel.Text = GetString("Menu_ViewLogPanel")
        End If
    End Sub

    ''' <summary>
    ''' Appends a structured log entry to the log panel. Thread-safe.
    ''' </summary>
    Friend Sub AppendUnifiedLog(entry As Services.Infrastructure.LogEntry)
        If dgvLog Is Nothing Then Return

        _unifiedLogBuffer.Enqueue(entry)

        If Threading.Interlocked.CompareExchange(_unifiedLogPending, 1, 0) = 0 Then
            If dgvLog.IsHandleCreated Then
                dgvLog.BeginInvoke(Sub() FlushUnifiedLog())
            Else
                Threading.Interlocked.Exchange(_unifiedLogPending, 0)
            End If
        End If
    End Sub

    Private Sub FlushUnifiedLog()
        Threading.Interlocked.Exchange(_unifiedLogPending, 0)

        ' Drain queue into backing list
        Dim entry As Services.Infrastructure.LogEntry = Nothing
        While _unifiedLogBuffer.TryDequeue(entry)
            _unifiedLogEntries.Add(entry)
        End While

        ' Trim backing list
        If _unifiedLogEntries.Count > UnifiedLogMaxLines Then
            _unifiedLogEntries.RemoveRange(0, _unifiedLogEntries.Count - UnifiedLogMaxLines)
            RenderFilteredLog()
            Return
        End If

        ' Append only new entries that pass the current filter
        Dim catFilter = If(cboLogCategory.SelectedItem, "All").ToString()
        Dim lvlFilter = If(cboLogLevel.SelectedItem, "All").ToString()
        Dim rendered = If(TypeOf dgvLog.Tag Is Integer, CInt(dgvLog.Tag), 0)

        For i = rendered To _unifiedLogEntries.Count - 1
            Dim e = _unifiedLogEntries(i)
            If PassesFilter(e, catFilter, lvlFilter) Then
                AddLogRow(e)
            End If
        Next
        dgvLog.Tag = _unifiedLogEntries.Count

        If _logAutoScroll AndAlso dgvLog.Rows.Count > 0 Then
            dgvLog.FirstDisplayedScrollingRowIndex = dgvLog.Rows.Count - 1
        End If
    End Sub

    ''' <summary>Re-renders the entire log for current filter settings.</summary>
    Private Sub RenderFilteredLog()
        Dim catFilter = If(cboLogCategory.SelectedItem, "All").ToString()
        Dim lvlFilter = If(cboLogLevel.SelectedItem, "All").ToString()

        dgvLog.Rows.Clear()
        For Each e In _unifiedLogEntries
            If PassesFilter(e, catFilter, lvlFilter) Then
                AddLogRow(e)
            End If
        Next
        dgvLog.Tag = _unifiedLogEntries.Count

        If _logAutoScroll AndAlso dgvLog.Rows.Count > 0 Then
            dgvLog.FirstDisplayedScrollingRowIndex = dgvLog.Rows.Count - 1
        End If
    End Sub

    Private Function PassesFilter(entry As Services.Infrastructure.LogEntry, catFilter As String, lvlFilter As String) As Boolean
        If catFilter <> "All" Then
            Dim catName = entry.Category.ToString()
            ' Legacy entries also match by source name
            If Not catName.Equals(catFilter, StringComparison.OrdinalIgnoreCase) AndAlso
               Not entry.Source.Equals(catFilter, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
        End If
        If lvlFilter <> "All" Then
            Dim lvl As Services.Infrastructure.LogSeverity
            If [Enum].TryParse(lvlFilter, lvl) Then
                If entry.Level < lvl Then Return False
            End If
        End If
        Return True
    End Function

    Private Sub AddLogRow(entry As Services.Infrastructure.LogEntry)
        Dim idx = dgvLog.Rows.Add(
            entry.Time.ToString("HH:mm:ss"),
            entry.Category.ToString(),
            entry.Level.ToString(),
            entry.Message)
        Dim row = dgvLog.Rows(idx)
        row.Tag = entry
        ' Color the row by level
        Dim fg = LogColorForTheme(entry.Color)
        row.DefaultCellStyle.ForeColor = fg
    End Sub

    Private _logSearchRow As Integer = 0

    Private Sub SearchLog()
        Dim keyword = txtLogSearch.Text.Trim()
        If keyword.Length = 0 Then Return

        Dim startRow = If(_logSearchRow < dgvLog.Rows.Count, _logSearchRow, 0)
        For i = startRow To dgvLog.Rows.Count - 1
            Dim msg = If(dgvLog.Rows(i).Cells(3).Value?.ToString(), "")
            If msg.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 Then
                dgvLog.ClearSelection()
                dgvLog.Rows(i).Selected = True
                dgvLog.FirstDisplayedScrollingRowIndex = i
                _logSearchRow = i + 1
                Return
            End If
        Next
        ' Wrap around
        If startRow > 0 Then
            For i = 0 To startRow - 1
                Dim msg = If(dgvLog.Rows(i).Cells(3).Value?.ToString(), "")
                If msg.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    dgvLog.ClearSelection()
                    dgvLog.Rows(i).Selected = True
                    dgvLog.FirstDisplayedScrollingRowIndex = i
                    _logSearchRow = i + 1
                    Return
                End If
            Next
        End If
        _logSearchRow = 0
        Media.SystemSounds.Beep.Play()
    End Sub

    ''' <summary>
    ''' Remaps a log entry color so it's readable on the current log background.
    ''' </summary>
    Private Function LogColorForTheme(c As Color) As Color
        If _logDarkMode Then
            Dim brightness = (CInt(c.R) * 299 + CInt(c.G) * 587 + CInt(c.B) * 114) \ 1000
            If brightness < 90 Then
                Return Color.FromArgb(
                    Math.Min(255, c.R + 140),
                    Math.Min(255, c.G + 140),
                    Math.Min(255, c.B + 140))
            End If
            Return c
        Else
            Dim brightness = (CInt(c.R) * 299 + CInt(c.G) * 587 + CInt(c.B) * 114) \ 1000
            If brightness > 180 Then
                Return Color.FromArgb(
                    Math.Max(0, c.R - 140),
                    Math.Max(0, c.G - 140),
                    Math.Max(0, c.B - 140))
            End If
            Return c
        End If
    End Function

    Private Sub ShowLogConfig()
        Using dlg As New FormLogConfig(_config, AddressOf GetString)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                _config.LogRouting = dlg.Routing
                Services.Infrastructure.AppLogger.Routing = dlg.Routing
                Models.ConfigManager.Save(_config)
            End If
        End Using
    End Sub

    Private Sub ShowLogViewer()
        AppLogger.Log(LogEvents.UI_LOG_VIEWER_OPENED, "Session log viewer opened")
        Using dlg As New FormLogViewer(AddressOf GetString, _logDarkMode)
            dlg.ShowDialog(Me)
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Workspace Switching
    ' ═══════════════════════════════════════════════════════════════
    Private Sub GetNavThemeColors(ByRef inactiveBg As Color, ByRef inactiveFg As Color, ByRef activeBg As Color)
        Select Case If(_config?.Theme, Models.ThemeMode.System)
            Case Models.ThemeMode.Light
                inactiveBg = Color.FromArgb(240, 240, 240)
                inactiveFg = Color.FromArgb(60, 60, 60)
                activeBg = Color.FromArgb(0, 122, 204)
            Case Models.ThemeMode.Dark
                inactiveBg = NavBackColor
                inactiveFg = NavForeColor
                activeBg = NavSelectedColor
            Case Else
                inactiveBg = SystemColors.Control
                inactiveFg = SystemColors.ControlText
                activeBg = Color.FromArgb(0, 122, 204)
        End Select
    End Sub

    Private Sub SwitchWorkspace(tabPage As TabPage, navButton As ToolStripButton)
        If navButton Is btnNavLog Then
            ' Log workspace — show log panel full-size
            tabMain.Visible = False
            splitterLog.Visible = False
            pnlLogPanel.Height = pnlContent.ClientSize.Height
            pnlLogPanel.Visible = True
        Else
            ' Normal workspace — log panel is a sub-panel
            If _config.LogPanelHeight > 50 Then pnlLogPanel.Height = _config.LogPanelHeight
            pnlLogPanel.Visible = _logPanelVisible
            splitterLog.Visible = _logPanelVisible
            tabMain.Visible = True
            If tabMain.SelectedTab IsNot tabPage Then
                tabMain.SelectedTab = tabPage
            End If
        End If

        Dim inactiveBg, inactiveFg, activeBg As Color
        GetNavThemeColors(inactiveBg, inactiveFg, activeBg)

        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = inactiveBg
            _activeNavButton.ForeColor = inactiveFg
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Name), 28, inactiveFg)
        End If

        navButton.BackColor = activeBg
        navButton.ForeColor = Color.White
        navButton.Image = RenderFontIcon(GetNavIcon(navButton.Name), 28, Color.White)
        _activeNavButton = navButton
    End Sub

    ''' <summary>
    ''' Temporarily show a legacy tab (Paths, Settings, Server, Help)
    ''' that doesn't have a nav rail button yet.
    ''' </summary>
    Private Sub ShowLegacyTab(tabPage As TabPage)
        If _activeNavButton IsNot Nothing Then
            Dim inactiveBg, inactiveFg, activeBg As Color
            GetNavThemeColors(inactiveBg, inactiveFg, activeBg)

            _activeNavButton.BackColor = inactiveBg
            _activeNavButton.ForeColor = inactiveFg
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Name), 28, inactiveFg)
            _activeNavButton = Nothing
        End If

        tabMain.SelectedTab = tabPage
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Shell helpers
    ' ═══════════════════════════════════════════════════════════════

    Private Sub LaunchSessionWizard()
        Using dlg As New FormSessionWizard(_config, Array.Empty(Of String)(), _sttLanguages)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                LoadConfigToUi()
            End If
        End Using
    End Sub

    Private Sub ShowOptionsDialog(Optional category As String = "general")
        _uiLocales = DiscoverUiLocales()

        ' Snapshot translation settings before Options opens
        Dim oldModelType = If(_config.TranslationModelType, "nllb")
        Dim oldModelPath = If(_config.TranslationModelPath, "")
        Dim oldDevice = If(_config.TranslationDevice, "cuda")
        Dim oldEnabled = _config.TranslationEnabled

        Using dlg As New FormOptions(_config, _uiLocales)
            dlg.SelectCategory(category)
            If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.ConfigChanged Then
                LoadConfigToUi()
                _langPack.LoadLanguage(_config.UiLanguage)
                ApplyLocale()
                ApplyTheme(_config.Theme)
                If _config.StartWithWindows Then RegisterStartup() Else UnregisterStartup()

                ' Propagate updated API keys to running cloud backends
                Try
                    Dim svc = _serverController?.KestrelHost?.Services
                    If svc IsNot Nothing Then
                        ' Re-apply per-engine keys to every cloud translation backend
                        ' (own translation key first, else the companion STT engine's key)
                        Services.Translation.TranslationBackendRegistry.ConfigureCloudApiKeys(svc, _config)
                        ' Update ConversationAudioHandler with the active backend's key
                        Dim cah = TryCast(svc.GetService(GetType(Services.Rooms.ConversationAudioHandler)), Services.Rooms.ConversationAudioHandler)
                        If cah IsNot Nothing Then cah.SttApiKey = _config.GetSttApiKey(If(_config.SttBackend, ""))
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.CONFIG_SAVED, $"Failed to propagate API key: {ex.Message}")
                End Try

                ' If translation backend/model/device changed, restart the sidecar
                Dim newModelType = If(_config.TranslationModelType, "nllb")
                Dim newModelPath = If(_config.TranslationModelPath, "")
                Dim newDevice = If(_config.TranslationDevice, "cuda")
                If _translationService IsNot Nothing AndAlso _translationService.IsRunning AndAlso
                   (Not String.Equals(oldModelType, newModelType, StringComparison.OrdinalIgnoreCase) OrElse
                    Not String.Equals(oldModelPath, newModelPath, StringComparison.OrdinalIgnoreCase) OrElse
                    Not String.Equals(oldDevice, newDevice, StringComparison.OrdinalIgnoreCase)) Then
                    AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, $"Backend changed ({oldModelType}→{newModelType}, {oldModelPath}→{newModelPath}), restarting sidecar")
                    _translationService.Stop()
                    _translationService = Nothing
                    _translationStarting = False
                    StartTranslationService()
                ElseIf _config.TranslationEnabled AndAlso Not oldEnabled Then
                    ' Translation was just enabled
                    StartTranslationService()
                End If
            End If
        End Using
    End Sub

    Private _formClients As FormConnectedClients

    Private Sub ShowConnectedClients()
        If _formClients Is Nothing OrElse _formClients.IsDisposed Then
            _formClients = New FormConnectedClients(
                Function() SubtitleSvc,
                Function() _serverController?.GetMetricsService(),
                _config.Theme)
            _formClients.Show(Me)
        Else
            _formClients.BringToFront()
        End If
    End Sub

    Private Sub ShowQrCode()
        If _serverController Is Nothing OrElse _serverController.Port = 0 Then
            MessageBox.Show(GetString("Shell_QrNotRunning"),
                GetString("Shell_QrTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        Dim localIp = Controllers.ServerController.GetLocalIpAddress()
        Dim url = $"https://{localIp}:{_serverController.Port + 1}"

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

                        ' ── system_info.txt ──
                        Dim infoEntry = archive.CreateEntry("system_info.txt")
                        Using writer As New IO.StreamWriter(infoEntry.Open())
                            writer.WriteLine($"Every Tongue Diagnostics — {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                            writer.WriteLine($"Hostname: {Environment.MachineName}")
                            writer.WriteLine($"Version: {Reflection.Assembly.GetExecutingAssembly().GetName().Version}")
                            writer.WriteLine($"OS: {hwInfo.OsDescription}")
                            writer.WriteLine($".NET: {Environment.Version}")
                            writer.WriteLine($"64-bit OS: {Environment.Is64BitOperatingSystem}")
                            writer.WriteLine($"64-bit process: {Environment.Is64BitProcess}")
                            writer.WriteLine()

                            writer.WriteLine("=== Hardware ===")
                            writer.WriteLine($"GPU: {hwInfo.GpuName} ({hwInfo.GpuMemoryMB} MB VRAM) — Score: {hwInfo.GpuScore}/100")
                            Dim clockGHz = (hwInfo.CpuClockMHz / 1000.0).ToString("F1")
                            writer.WriteLine($"CPU: {hwInfo.CpuName} ({hwInfo.CpuCores} cores, {clockGHz} GHz) — Score: {hwInfo.CpuScore}/100")
                            writer.WriteLine($"RAM: {hwInfo.RamTotalMB} MB ({hwInfo.RamTotalMB \ 1024} GB) — Score: {hwInfo.RamScore}/100")
                            writer.WriteLine($"Disk free: {hwInfo.DiskFreeMB} MB ({hwInfo.DiskFreeMB / 1024.0:F1} GB) — Score: {hwInfo.DiskScore}/100")
                            writer.WriteLine($"OS: {hwInfo.OsDescription} — Score: {hwInfo.OsScore}/100")
                            Dim lp = Services.Infrastructure.LanguagePackService.Instance
                            writer.WriteLine($"Overall: {hwInfo.OverallScore}/100 ({hwInfo.Rating}) — {hwInfo.GetRatingDescription(lp)}")
                            writer.WriteLine()

                            ' Recommendations
                            Dim recs = hwInfo.GetRecommendations(lp)
                            If recs.Count > 0 Then
                                writer.WriteLine("=== Recommendations ===")
                                For Each rec In recs
                                    writer.WriteLine($"  • {rec}")
                                Next
                                writer.WriteLine()
                            End If

                            ' Models loaded
                            writer.WriteLine("=== Models ===")
                            writer.WriteLine($"  STT model (job): {_config.PathModel}")
                            writer.WriteLine($"  STT model (audio): {_config.PathModelAudio}")
                            writer.WriteLine($"  STT model (live): {_config.PathWhisperCppModel}")
                            writer.WriteLine($"  STT backend: {_config.SttBackend}")
                            writer.WriteLine($"  Translation model: {_config.TranslationModelPath}")
                            writer.WriteLine($"  GPU enabled: {Not _config.NoGpu}")
                            writer.WriteLine($"  Flash attention: {_config.FlashAttn}")
                            writer.WriteLine()

                            ' Server status
                            writer.WriteLine("=== Server ===")
                            writer.WriteLine($"  Subtitle port: {_config.SubtitleServerPort}")
                            writer.WriteLine($"  Live port: {_config.LiveServerPort}")
                            writer.WriteLine($"  Translation port: {_config.TranslationPort}")
                            writer.WriteLine($"  Translation enabled: {_config.TranslationEnabled}")
                            writer.WriteLine($"  Translation device: {_config.TranslationDevice}")

                            ' Metrics snapshot
                            Try
                                Dim metricsSvc = _serverController?.KestrelHost?.Services?.GetService(
                                    GetType(Services.Interfaces.IMetricsService))
                                If metricsSvc IsNot Nothing Then
                                    Dim metrics = DirectCast(metricsSvc, Services.Interfaces.IMetricsService).GetSnapshot()
                                    writer.WriteLine()
                                    writer.WriteLine("=== Metrics ===")
                                    writer.WriteLine($"  Connected clients: {metrics.Clients.Connected}")
                                    If metrics.Clients.ByLanguage?.Count > 0 Then
                                        writer.WriteLine($"  Clients by language: {String.Join(", ", metrics.Clients.ByLanguage.Select(Function(kv) $"{kv.Key}={kv.Value}"))}")
                                    End If
                                    writer.WriteLine($"  Broadcast latency: {metrics.Broadcast.LatencyMs} ms")
                                    writer.WriteLine($"  Messages sent: {metrics.Broadcast.MessagesSent}")
                                    writer.WriteLine($"  Messages dropped: {metrics.Broadcast.MessagesDropped}")
                                    writer.WriteLine($"  Translation backend: {metrics.Translation.ActiveBackend}")
                                    writer.WriteLine($"  Translation latency: {metrics.Translation.LatencyMs} ms")
                                    writer.WriteLine($"  Translation chars: {metrics.Translation.CharactersThisSession}")
                                    writer.WriteLine($"  Memory: {metrics.System.MemoryMB} MB")
                                    writer.WriteLine($"  Uptime: {metrics.System.UptimeSeconds} s")
                                End If
                            Catch ex As Exception
                                writer.WriteLine($"  (metrics unavailable: {ex.Message})")
                            End Try
                            writer.WriteLine()

                            ' Configuration
                            writer.WriteLine("=== Configuration ===")
                            For Each prop In GetType(Models.AppConfig).GetProperties(Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance)
                                If Not prop.CanRead Then Continue For
                                Dim val = prop.GetValue(_config)
                                Dim displayVal = If(prop.Name.IndexOf("Pin", StringComparison.OrdinalIgnoreCase) >= 0, "****", val?.ToString())
                                writer.WriteLine($"  {prop.Name} = {displayVal}")
                            Next
                        End Using

                        ' ── Log files (last 30 days — session directories + legacy flat files) ──
                        Dim logDir = Services.Infrastructure.AppLogger.GetLogDir()
                        Dim cutoff = DateTime.Now.AddDays(-30)
                        ' Session directories (yyyyMMdd_HHmmss/)
                        For Each sessionDir In IO.Directory.GetDirectories(logDir, "????????_??????")
                            If IO.Directory.GetLastWriteTime(sessionDir) > cutoff Then
                                Dim dirName = IO.Path.GetFileName(sessionDir)
                                For Each f In IO.Directory.GetFiles(sessionDir)
                                    archive.CreateEntryFromFile(f, $"logs/{dirName}/{IO.Path.GetFileName(f)}")
                                Next
                            End If
                        Next
                        ' Legacy flat log files
                        For Each logFile In IO.Directory.GetFiles(logDir, "*.log")
                            If IO.File.GetLastWriteTime(logFile) > cutoff Then
                                archive.CreateEntryFromFile(logFile, $"logs/{IO.Path.GetFileName(logFile)}")
                            End If
                        Next

                        ' ── Integrity check ──
                        Dim integrityResult = Services.Infrastructure.IntegrityChecker.Check()
                        Dim intEntry = archive.CreateEntry("integrity_check.txt")
                        Using iw As New IO.StreamWriter(intEntry.Open())
                            For Each line In Services.Infrastructure.IntegrityChecker.ToReportLines(integrityResult)
                                iw.WriteLine(line)
                            Next
                        End Using

                        ' ── Glossary ──
                        Dim glossaryPath = Models.AppConfig.ResolvePath(_config.TranslationGlossaryPath)
                        If IO.File.Exists(glossaryPath) Then
                            archive.CreateEntryFromFile(glossaryPath, "glossary.json")
                        End If
                    End Using
                End Using

                MessageBox.Show(String.Format(GetString("Shell_ExportedTo"), dlg.FileName),
                    GetString("Shell_ExportComplete"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(String.Format(GetString("Shell_ExportFailed"), ex.Message),
                    GetString("Shell_ExportError"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    Private Async Sub VerifyFileIntegrity()
        mnuToolsVerifyIntegrity.Enabled = False
        AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK, "Starting file integrity check...")

        Dim result = Await Threading.Tasks.Task.Run(
            Function() Services.Infrastructure.IntegrityChecker.Check())

        mnuToolsVerifyIntegrity.Enabled = True

        If Not result.ManifestFound Then
            MessageBox.Show(GetString("Shell_IntegrityNotFound"),
                            GetString("Shell_IntegrityTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine($"Manifest version: {result.ManifestVersion}")
        sb.AppendLine($"Generated: {result.ManifestGenerated}")
        sb.AppendLine()

        For Each f In result.Files
            Dim icon = If(f.Status = Services.Infrastructure.IntegrityChecker.FileStatus.Pass, "PASS",
                       If(f.Status = Services.Infrastructure.IntegrityChecker.FileStatus.Fail, "FAIL", "MISSING"))
            sb.AppendLine($"  [{icon}]  {f.RelativePath}")
        Next

        sb.AppendLine()
        sb.AppendLine(String.Format(GetString("Shell_IntegrityResult"), result.PassCount, result.FailCount, result.MissingCount))

        Dim msgIcon = If(result.AllPassed, MessageBoxIcon.Information, MessageBoxIcon.Warning)
        Dim title = If(result.AllPassed, GetString("Shell_IntegrityAllOk"), GetString("Shell_IntegrityIssues"))

        ' Log full results to the daily log
        For Each line In Services.Infrastructure.IntegrityChecker.ToReportLines(result)
            AppLogger.Log(LogEvents.STARTUP_DEPENDENCY_CHECK, line)
        Next

        MessageBox.Show(sb.ToString(), title, MessageBoxButtons.OK, msgIcon)
    End Sub

    Private Sub OpenTranslationBenchmark()
        If _translationService Is Nothing OrElse Not _translationService.IsRunning Then
            MessageBox.Show("The translation server must be running to use the benchmark.",
                            "Pipeline Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Get services from Kestrel DI — benchmark routes through the real pipeline
        Dim diServices = _serverController?.KestrelHost?.Services
        Dim translationSvc = TryCast(diServices?.GetService(
            GetType(Services.Interfaces.ITranslationService)), Services.Interfaces.ITranslationService)
        If translationSvc Is Nothing Then
            MessageBox.Show("Translation service not available.",
                            "Pipeline Benchmark", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim ttsSvc = TryCast(diServices?.GetService(
            GetType(Services.Interfaces.ITtsService)), Services.Interfaces.ITtsService)
        Dim ttsBackends = If(diServices IsNot Nothing,
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.
                GetServices(Of Services.Interfaces.ITtsBackend)(diServices),
            Enumerable.Empty(Of Services.Interfaces.ITtsBackend)())
        Dim livePort = If(_config?.LiveServerPort, 0)

        Using frm As New FormTranslationBenchmark(translationSvc, ttsSvc, livePort, _config, ttsBackends)
            frm.ShowDialog(Me)
        End Using
    End Sub

    Private Sub SetThemeFromMenu(theme As Models.ThemeMode)
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

        If _serverController IsNot Nothing AndAlso _serverController.IsRunning Then
            tslServerStatus.Text = String.Format(GetString("Shell_ServerRunning"), _serverController.Port)
            tslServerStatus.ForeColor = Color.Green
        Else
            tslServerStatus.Text = GetString("Shell_ServerStopped")
            tslServerStatus.ForeColor = Color.Gray
        End If

        Dim svc = SubtitleSvc
        Dim clients = If(svc?.ConnectedClients, 0)
        tslClients.Text = String.Format(GetString("Shell_Clients"), clients)
    End Sub

    ''' <summary>
    ''' Applies shell-specific theming to the nav rail and status bar.
    ''' </summary>
    Private Sub ApplyShellTheme(theme As Models.ThemeMode)
        AppLogger.Log(LogEvents.UI_THEME_CHANGED, $"ApplyShellTheme called with theme=""{theme}"", tsNavBar={tsNavBar IsNot Nothing}, activeBtn={_activeNavButton?.Text}")
        If tsNavBar Is Nothing Then Return

        Dim inactiveBg, inactiveFg, activeBg As Color
        GetNavThemeColors(inactiveBg, inactiveFg, activeBg)

        tsNavBar.BackColor = inactiveBg

        For Each item As ToolStripItem In tsNavBar.Items
            If TypeOf item Is ToolStripButton Then
                Dim btn = DirectCast(item, ToolStripButton)
                If btn IsNot _activeNavButton Then
                    btn.BackColor = inactiveBg
                    btn.ForeColor = inactiveFg
                    btn.Image = RenderFontIcon(GetNavIcon(btn.Name), 28, inactiveFg)
                End If
            End If
        Next

        If _activeNavButton IsNot Nothing Then
            _activeNavButton.BackColor = activeBg
            _activeNavButton.ForeColor = Color.White
            _activeNavButton.Image = RenderFontIcon(GetNavIcon(_activeNavButton.Name), 28, Color.White)
        End If

        ' Theme the log panel
        If dgvLog IsNot Nothing Then
            Select Case theme
                Case Models.ThemeMode.Light
                    _logDarkMode = False
                    Dim lightStyle = New DataGridViewCellStyle With {
                        .BackColor = Color.White, .ForeColor = Color.FromArgb(30, 30, 30),
                        .Font = New Font("Consolas", 9F),
                        .SelectionBackColor = Color.FromArgb(200, 220, 240),
                        .SelectionForeColor = Color.FromArgb(30, 30, 30)
                    }
                    dgvLog.DefaultCellStyle = lightStyle
                    dgvLog.BackgroundColor = Color.White
                    dgvLog.GridColor = Color.FromArgb(220, 220, 220)
                    dgvLog.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
                        .BackColor = Color.FromArgb(240, 240, 240),
                        .ForeColor = Color.FromArgb(30, 30, 30),
                        .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
                    }
                Case Models.ThemeMode.Dark
                    _logDarkMode = True
                    Dim darkStyle = New DataGridViewCellStyle With {
                        .BackColor = Color.FromArgb(24, 24, 24), .ForeColor = Color.FromArgb(200, 200, 200),
                        .Font = New Font("Consolas", 9F),
                        .SelectionBackColor = Color.FromArgb(60, 60, 80),
                        .SelectionForeColor = Color.FromArgb(220, 220, 220)
                    }
                    dgvLog.DefaultCellStyle = darkStyle
                    dgvLog.BackgroundColor = Color.FromArgb(24, 24, 24)
                    dgvLog.GridColor = Color.FromArgb(50, 50, 50)
                    dgvLog.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
                        .BackColor = Color.FromArgb(45, 45, 48),
                        .ForeColor = Color.FromArgb(200, 200, 200),
                        .Font = New Font("Segoe UI", 8.5F, FontStyle.Bold)
                    }
                Case Else
                    _logDarkMode = True
                    dgvLog.DefaultCellStyle.BackColor = SystemColors.Window
                    dgvLog.DefaultCellStyle.ForeColor = SystemColors.WindowText
                    dgvLog.BackgroundColor = SystemColors.Window
            End Select
            RenderFilteredLog()
        End If

        ' Theme the translate workspace
        If txtTransInput IsNot Nothing Then
            Select Case theme
                Case Models.ThemeMode.Dark
                    txtTransInput.BackColor = Color.FromArgb(60, 63, 65)
                    txtTransInput.ForeColor = Color.FromArgb(220, 220, 220)
                    txtTransOutput.BackColor = Color.FromArgb(60, 63, 65)
                    txtTransOutput.ForeColor = Color.FromArgb(220, 220, 220)
                Case Models.ThemeMode.Light
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
            mnuViewThemeSystem.Checked = (theme = Models.ThemeMode.System)
            mnuViewThemeLight.Checked = (theme = Models.ThemeMode.Light)
            mnuViewThemeDark.Checked = (theme = Models.ThemeMode.Dark)
        End If
    End Sub

    ''' <summary>
    ''' Map 2-letter ISO 639-1 codes to 3-letter ISO 639-3 codes for Bible matching.
    ''' </summary>
    Private Shared Function WhisperToIso3(code As String) As String
        If String.IsNullOrEmpty(code) Then Return code
        Dim result = Services.Infrastructure.LanguageCodeService.Instance.Iso1ToIso3(code)
        Return If(Not String.IsNullOrEmpty(result), result, code)
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
