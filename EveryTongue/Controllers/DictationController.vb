Imports System.Windows.Forms
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Input

Namespace Controllers

    ''' <summary>
    ''' Coordinates system-wide dictation (no tab): owns the DictationService + GlobalHotkeys,
    ''' builds the systray submenu (on/off, output language, mode), routes committed text to the
    ''' focused window via TextInjector, and applies settings from Tools → Options.
    ''' </summary>
    Public Class DictationController

        Private ReadOnly _config As AppConfig
        Private ReadOnly _ownerForm As Form
        Private ReadOnly _service As DictationService
        Private ReadOnly _hotkeys As GlobalHotkeys
        Private ReadOnly _trayRoot As ToolStripMenuItem
        Private ReadOnly _getString As Func(Of String, String)
        Private ReadOnly _nameForLang As Func(Of String, String)
        Private ReadOnly _saveConfig As Action

        Private _miEnable As ToolStripMenuItem
        Private _miOutput As ToolStripMenuItem
        Private _miMode As ToolStripMenuItem

        Public Sub New(config As AppConfig, ownerForm As Form, service As DictationService,
                       hotkeys As GlobalHotkeys, trayRoot As ToolStripMenuItem,
                       getString As Func(Of String, String), nameForLang As Func(Of String, String),
                       saveConfig As Action)
            _config = config
            _ownerForm = ownerForm
            _service = service
            _hotkeys = hotkeys
            _trayRoot = trayRoot
            _getString = getString
            _nameForLang = nameForLang
            _saveConfig = saveConfig
        End Sub

        Public Sub WireEvents()
            AddHandler _hotkeys.HotkeyToggle, Sub() _ownerForm.BeginInvoke(Sub() ToggleDictation())
            AddHandler _hotkeys.PttDown, Sub() _service.SetPttHeld(True)
            AddHandler _hotkeys.PttUp, Sub() _service.SetPttHeld(False)

            _service.TextSink = Sub(text)
                                    Try
                                        If _ownerForm.IsHandleCreated Then _ownerForm.BeginInvoke(Sub() RouteText(text))
                                    Catch
                                    End Try
                                End Sub

            BuildTrayMenu()
        End Sub

        ' ── Tray submenu (data-driven content; built at runtime) ───────────────
        Private Sub BuildTrayMenu()
            _trayRoot.DropDownItems.Clear()
            _trayRoot.Text = _getString("Tray_Dictation")

            _miEnable = New ToolStripMenuItem(_getString("Dict_Enable")) With {.CheckOnClick = False}
            AddHandler _miEnable.Click, Sub(s, e) ToggleDictation()
            _trayRoot.DropDownItems.Add(_miEnable)

            _trayRoot.DropDownItems.Add(New ToolStripSeparator())

            _miOutput = New ToolStripMenuItem(_getString("Dict_OutputLanguage"))
            _trayRoot.DropDownItems.Add(_miOutput)

            _miMode = New ToolStripMenuItem(_getString("Dict_Mode"))
            Dim miCont = New ToolStripMenuItem(_getString("Dict_ModeContinuous"))
            AddHandler miCont.Click, Sub(s, e) SetMode(DictationStyle.Continuous)
            Dim miPtt = New ToolStripMenuItem(_getString("Dict_ModePushToTalk"))
            AddHandler miPtt.Click, Sub(s, e) SetMode(DictationStyle.PushToTalk)
            _miMode.DropDownItems.AddRange(New ToolStripItem() {miCont, miPtt})
            _trayRoot.DropDownItems.Add(_miMode)

            RefreshOutputLanguageMenu()
            RefreshChecks()
        End Sub

        ''' <summary>Rebuild the output-language submenu from the curated list ("None" + targets).</summary>
        Public Sub RefreshOutputLanguageMenu()
            If _miOutput Is Nothing Then Return
            _miOutput.DropDownItems.Clear()
            Dim none = New ToolStripMenuItem(_getString("Dict_OutputNone")) With {.Tag = ""}
            AddHandler none.Click, Sub(s, e) SetOutputLanguage("")
            _miOutput.DropDownItems.Add(none)
            For Each flores In _config.DictationTargetLanguages
                Dim code = flores
                Dim item = New ToolStripMenuItem(_nameForLang(code)) With {.Tag = code}
                AddHandler item.Click, Sub(s, e) SetOutputLanguage(code)
                _miOutput.DropDownItems.Add(item)
            Next
            RefreshChecks()
        End Sub

        Private Sub RefreshChecks()
            If _miEnable IsNot Nothing Then
                _miEnable.Checked = _service.IsArmed
                _miEnable.Text = If(_service.IsArmed, _getString("Dict_Disable"), _getString("Dict_Enable"))
                _miEnable.Enabled = _config.DictationEnabled
            End If
            If _miMode IsNot Nothing Then
                For Each it As ToolStripMenuItem In _miMode.DropDownItems.OfType(Of ToolStripMenuItem)()
                    it.Checked = False
                Next
                Dim idx = If(_config.DictationStyle = DictationStyle.PushToTalk, 1, 0)
                If _miMode.DropDownItems.Count > idx Then DirectCast(_miMode.DropDownItems(idx), ToolStripMenuItem).Checked = True
            End If
            If _miOutput IsNot Nothing Then
                For Each it As ToolStripMenuItem In _miOutput.DropDownItems.OfType(Of ToolStripMenuItem)()
                    it.Checked = String.Equals(CStr(it.Tag), _config.DictationActiveTargetLanguage, StringComparison.OrdinalIgnoreCase)
                Next
            End If
        End Sub

        ' ── Actions ────────────────────────────────────────────────────────────
        Public Sub ToggleDictation()
            If Not _config.DictationEnabled Then Return
            If _service.IsArmed Then
                _service.Disarm()
            Else
                If Not _service.Arm() Then
                    AppLogger.PromptDownloadManager(_getString("Dict_EngineMissing"), _getString("Tray_Dictation"))
                End If
            End If
            RefreshChecks()
        End Sub

        Private Sub SetOutputLanguage(flores As String)
            _config.DictationActiveTargetLanguage = If(flores, "")
            _saveConfig?.Invoke()
            RefreshChecks()
        End Sub

        Private Sub SetMode(style As DictationStyle)
            _config.DictationStyle = style
            ApplyHotkeys()
            _saveConfig?.Invoke()
            RefreshChecks()
        End Sub

        ' ── Settings application (startup + after Options) ──────────────────────
        ''' <summary>Register hotkeys per config. Call once the form handle exists.</summary>
        Public Sub ApplyHotkeys()
            If Not _config.DictationEnabled Then
                _hotkeys.ClearPttHotkey()
                Return
            End If
            _hotkeys.SetToggleHotkey(_config.DictationToggleHotkey)
            If _config.DictationStyle = DictationStyle.PushToTalk Then
                _hotkeys.SetPttHotkey(_config.DictationPttHotkey)
            Else
                _hotkeys.ClearPttHotkey()
            End If
        End Sub

        ''' <summary>Re-apply everything after the Options dialog changes settings.</summary>
        Public Sub ReapplySettings()
            ApplyHotkeys()
            RefreshOutputLanguageMenu()
            RefreshChecks()
        End Sub

        Private Sub RouteText(text As String)
            ' Never type into our own window — system-wide dictation targets OTHER apps.
            ' If EveryTongue is focused, there's nowhere to type; log it so it's not silent
            ' (the operator must click into the target textbox in another app).
            Dim fg = TextInjector.ForegroundWindow()
            If fg = _ownerForm.Handle Then
                AppLogger.Log(LogEvents.DICT_COMMIT, $"Skipped ""{text}"" — EveryTongue is focused; click into the target app's textbox")
                Return
            End If
            TextInjector.InjectText(text & " ", _config.DictationInsertMode = DictationInsertMode.ClipboardPaste)
            AppLogger.Log(LogEvents.DICT_COMMIT, $"Injected {text.Length} chars")
        End Sub

        Public Sub Shutdown()
            _service.Disarm()
            _hotkeys.Dispose()
        End Sub

    End Class

End Namespace
