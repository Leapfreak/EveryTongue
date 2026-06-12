Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline

Namespace Controllers
    ''' <summary>
    ''' Manages the Transcribe workspace tab — mode switching, pipeline execution,
    ''' browse/output buttons, model selection. Extracted from FormMain.
    ''' </summary>
    Friend Class TranscribeController

        ' UI controls (owned by FormMain, passed in)
        Private ReadOnly _cboMode As ComboBox
        Private ReadOnly _cboModel As ComboBox
        Private ReadOnly _cboInputLanguage As ComboBox
        Private ReadOnly _cboOutputLanguage As ComboBox
        Private ReadOnly _txtUrl As TextBox
        Private ReadOnly _txtOutputDir As TextBox
        Private ReadOnly _btnStart As Button
        Private ReadOnly _btnResume As Button
        Private ReadOnly _btnCancel As Button
        Private ReadOnly _btnBrowseFile As Button
        Private ReadOnly _btnBrowseOutput As Button
        Private ReadOnly _btnOpenOutput As Button
        Private ReadOnly _btnOpenSubtitleEdit As Button
        Private ReadOnly _lnkPreviewSrt As LinkLabel
        Private ReadOnly _lblStepStatus As Label
        Private ReadOnly _lblUrl As Label
        Private ReadOnly _lblInputLanguage As Label
        Private ReadOnly _lblOutputLanguage As Label
        Private ReadOnly _lblModel As Label
        Private ReadOnly _lblStartTime As Label
        Private ReadOnly _lblEndTime As Label
        Private ReadOnly _lblStartColon1 As Label
        Private ReadOnly _lblStartColon2 As Label
        Private ReadOnly _lblEndColon1 As Label
        Private ReadOnly _lblEndColon2 As Label
        Private ReadOnly _txtStartHH As TextBox
        Private ReadOnly _txtStartMM As TextBox
        Private ReadOnly _txtStartSS As TextBox
        Private ReadOnly _txtEndHH As TextBox
        Private ReadOnly _txtEndMM As TextBox
        Private ReadOnly _txtEndSS As TextBox
        Private ReadOnly _pbOverall As ProgressBar
        Private ReadOnly _pbChunk As ProgressBar
        Private ReadOnly _grpOutputFormats As GroupBox
        Private ReadOnly _tabMain As TabControl
        Private ReadOnly _tabPageJob As TabPage

        ' Callbacks
        Private ReadOnly _config As AppConfig
        Private ReadOnly _saveUiToConfig As Action
        Private ReadOnly _showLogPanel As Action
        Private ReadOnly _appendLog As Action(Of String, String, Drawing.Color)
        Private ReadOnly _getString As Func(Of String, String)
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _sttLanguages As String()
        Private ReadOnly _langDisplayName As Func(Of String, String)
        Private ReadOnly _langCodeFromDisplay As Func(Of String, String)
        Private ReadOnly _notify As Action(Of String, String, MessageBoxIcon)

        ' State
        Private _isRunning As Boolean = False
        Private _cts As CancellationTokenSource
        Private _currentOutputDir As String = ""

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        Public Sub New(config As AppConfig,
                       cboMode As ComboBox, cboModel As ComboBox,
                       cboInputLanguage As ComboBox, cboOutputLanguage As ComboBox,
                       txtUrl As TextBox, txtOutputDir As TextBox,
                       btnStart As Button, btnResume As Button, btnCancel As Button,
                       btnBrowseFile As Button, btnBrowseOutput As Button,
                       btnOpenOutput As Button, btnOpenSubtitleEdit As Button,
                       lnkPreviewSrt As LinkLabel,
                       lblStepStatus As Label, lblUrl As Label,
                       lblInputLanguage As Label, lblOutputLanguage As Label, lblModel As Label,
                       lblStartTime As Label, lblEndTime As Label,
                       lblStartColon1 As Label, lblStartColon2 As Label,
                       lblEndColon1 As Label, lblEndColon2 As Label,
                       txtStartHH As TextBox, txtStartMM As TextBox, txtStartSS As TextBox,
                       txtEndHH As TextBox, txtEndMM As TextBox, txtEndSS As TextBox,
                       pbOverall As ProgressBar, pbChunk As ProgressBar,
                       grpOutputFormats As GroupBox,
                       tabMain As TabControl, tabPageJob As TabPage,
                       sttLanguages As String(),
                       langDisplayName As Func(Of String, String),
                       langCodeFromDisplay As Func(Of String, String),
                       saveUiToConfig As Action,
                       showLogPanel As Action,
                       appendLog As Action(Of String, String, Drawing.Color),
                       getString As Func(Of String, String),
                       log As Action(Of String),
                       notify As Action(Of String, String, MessageBoxIcon))
            _config = config
            _cboMode = cboMode
            _cboModel = cboModel
            _cboInputLanguage = cboInputLanguage
            _cboOutputLanguage = cboOutputLanguage
            _txtUrl = txtUrl
            _txtOutputDir = txtOutputDir
            _btnStart = btnStart
            _btnResume = btnResume
            _btnCancel = btnCancel
            _btnBrowseFile = btnBrowseFile
            _btnBrowseOutput = btnBrowseOutput
            _btnOpenOutput = btnOpenOutput
            _btnOpenSubtitleEdit = btnOpenSubtitleEdit
            _lnkPreviewSrt = lnkPreviewSrt
            _lblStepStatus = lblStepStatus
            _lblUrl = lblUrl
            _lblInputLanguage = lblInputLanguage
            _lblOutputLanguage = lblOutputLanguage
            _lblModel = lblModel
            _lblStartTime = lblStartTime
            _lblEndTime = lblEndTime
            _lblStartColon1 = lblStartColon1
            _lblStartColon2 = lblStartColon2
            _lblEndColon1 = lblEndColon1
            _lblEndColon2 = lblEndColon2
            _txtStartHH = txtStartHH
            _txtStartMM = txtStartMM
            _txtStartSS = txtStartSS
            _txtEndHH = txtEndHH
            _txtEndMM = txtEndMM
            _txtEndSS = txtEndSS
            _pbOverall = pbOverall
            _pbChunk = pbChunk
            _grpOutputFormats = grpOutputFormats
            _tabMain = tabMain
            _tabPageJob = tabPageJob
            _sttLanguages = sttLanguages
            _langDisplayName = langDisplayName
            _langCodeFromDisplay = langCodeFromDisplay
            _saveUiToConfig = saveUiToConfig
            _showLogPanel = showLogPanel
            _appendLog = appendLog
            _getString = getString
            _log = log
            _notify = notify
        End Sub

        ''' <summary>Wire event handlers. Call once during form init.</summary>
        Public Sub WireEvents()
            AddHandler _cboMode.SelectedIndexChanged, Sub(s, e) OnModeChanged()
            AddHandler _btnStart.Click, AddressOf OnStartClick
            AddHandler _btnResume.Click, AddressOf OnResumeClick
            AddHandler _btnCancel.Click, Sub(s, e) _cts?.Cancel()
            AddHandler _btnBrowseFile.Click, Sub(s, e) BrowseFile()
            AddHandler _btnBrowseOutput.Click, Sub(s, e) BrowseOutput()
            AddHandler _btnOpenOutput.Click, Sub(s, e) OpenOutput()
            AddHandler _btnOpenSubtitleEdit.Click, Sub(s, e) OpenSubtitleEdit()
            AddHandler _lnkPreviewSrt.LinkClicked, Sub(s, e) PreviewSrt()
            AddHandler _cboModel.SelectedIndexChanged, Sub(s, e) OnModelChanged()
            ' Snap language combos back to a valid item if user types something not in list
            For Each cbo In {_cboInputLanguage, _cboOutputLanguage}
                Dim c = cbo ' capture for closure
                AddHandler c.Validating, Sub(s, e)
                                              If c.SelectedIndex < 0 AndAlso c.Items.Count > 0 Then
                                                  ' Try to find a match for what was typed
                                                  Dim typed = c.Text.Trim()
                                                  For i = 0 To c.Items.Count - 1
                                                      If c.Items(i).ToString().StartsWith(typed, StringComparison.OrdinalIgnoreCase) Then
                                                          c.SelectedIndex = i
                                                          Return
                                                      End If
                                                  Next
                                                  c.SelectedIndex = 0
                                              End If
                                          End Sub
            Next
            For Each tb In {_txtStartHH, _txtStartMM, _txtStartSS, _txtEndHH, _txtEndMM, _txtEndSS}
                AddHandler tb.KeyPress, Sub(s, e)
                                            If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
                                                e.Handled = True
                                            End If
                                        End Sub
            Next
        End Sub

        Public Sub PopulateLanguageDropdowns()
            _cboInputLanguage.Items.Clear()
            _cboOutputLanguage.Items.Clear()
            For Each lang In _sttLanguages
                Dim display = _langDisplayName(lang)
                _cboInputLanguage.Items.Add(display)
                _cboOutputLanguage.Items.Add(display)
            Next

            ' Enable autocomplete typing
            For Each cbo In {_cboInputLanguage, _cboOutputLanguage}
                cbo.DropDownStyle = ComboBoxStyle.DropDown
                cbo.AutoCompleteMode = AutoCompleteMode.SuggestAppend
                cbo.AutoCompleteSource = AutoCompleteSource.ListItems
            Next
        End Sub

        Public Sub PopulateModelDropdown()
            _cboModel.Items.Clear()
            Try
                Dim modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModel))
                If Not String.IsNullOrWhiteSpace(modelDir) AndAlso Directory.Exists(modelDir) Then
                    For Each binFile In Directory.GetFiles(modelDir, "ggml-*.bin")
                        _cboModel.Items.Add(Path.GetFileName(binFile))
                    Next
                End If
            Catch ex As Exception
                _log($"[ERROR] PopulateModelDropdown: {ex.Message}")
            End Try

            Dim currentModel = If(_cboMode.SelectedIndex = 0, _config.PathModelAudio, _config.PathModel)
            Dim currentName = Path.GetFileName(currentModel)
            SelectComboItem(_cboModel, currentName)

            If _cboModel.SelectedIndex < 0 AndAlso Not String.IsNullOrWhiteSpace(currentName) Then
                _cboModel.Items.Add(currentName)
                SelectComboItem(_cboModel, currentName)
            End If
        End Sub

        Private Sub OnModeChanged()
            If _config Is Nothing Then Return
            Dim isYouTubeLike = (_cboMode.SelectedIndex <> 0)
            Dim isAudioFile = (_cboMode.SelectedIndex = 0)

            _lblStartTime.Visible = isYouTubeLike
            _txtStartHH.Visible = isYouTubeLike
            _lblStartColon1.Visible = isYouTubeLike
            _txtStartMM.Visible = isYouTubeLike
            _lblStartColon2.Visible = isYouTubeLike
            _txtStartSS.Visible = isYouTubeLike
            _lblEndTime.Visible = isYouTubeLike
            _txtEndHH.Visible = isYouTubeLike
            _lblEndColon1.Visible = isYouTubeLike
            _txtEndMM.Visible = isYouTubeLike
            _lblEndColon2.Visible = isYouTubeLike
            _txtEndSS.Visible = isYouTubeLike
            _btnResume.Visible = isYouTubeLike

            Dim hasSubtitles = (_cboMode.SelectedIndex = 0 OrElse _cboMode.SelectedIndex = 3)
            _grpOutputFormats.Visible = hasSubtitles

            ' Language and model only matter for modes that transcribe (0=Audio File, 3=YouTube→Subtitles)
            Dim hasTranscription = hasSubtitles
            _lblInputLanguage.Visible = hasTranscription
            _cboInputLanguage.Visible = hasTranscription
            _lblOutputLanguage.Visible = hasTranscription
            _cboOutputLanguage.Visible = hasTranscription
            _lblModel.Visible = hasTranscription
            _cboModel.Visible = hasTranscription

            _lblUrl.Text = If(isAudioFile, _getString("Lbl_AudioFile"), _getString("Lbl_Url"))
            _btnBrowseFile.Text = If(isAudioFile, _getString("Btn_BrowseAudio"), _getString("Btn_BrowseFile"))

            Dim modelName = Path.GetFileName(If(isAudioFile, _config.PathModelAudio, _config.PathModel))
            SelectComboItem(_cboModel, modelName)
        End Sub

        Private Async Sub OnStartClick(sender As Object, e As EventArgs)
            If _isRunning Then Return

            _saveUiToConfig()
            If _cboMode.SelectedIndex = 0 Then
                _config.PathModel = _config.PathModelAudio
            End If

            _isRunning = True
            _cts = New CancellationTokenSource()
            _currentOutputDir = _txtOutputDir.Text

            SetUiRunning(True)
            _tabMain.SelectedTab = _tabPageJob
            _showLogPanel()
            Application.DoEvents()

            Dim progress = CreateProgress()
            Dim runner As New PipelineRunner(_config, progress, _cts.Token)
            AddHandler runner.LogMessage, Sub(s, entry) LogToUnified(entry.Message, entry.Level)

            Try
                Dim url = _txtUrl.Text.Trim()
                Dim startTime = BuildTimeString(_txtStartHH.Text, _txtStartMM.Text, _txtStartSS.Text)
                Dim endTime = BuildTimeString(_txtEndHH.Text, _txtEndMM.Text, _txtEndSS.Text)

                Select Case _cboMode.SelectedIndex
                    Case 0 : Await runner.RunAudioFileAsync(url, _currentOutputDir)
                    Case 1 : Await runner.RunExtractAudioAsync(url, startTime, endTime, _currentOutputDir)
                    Case 2 : Await runner.RunDownloadOnlyAsync(url, startTime, endTime, _currentOutputDir)
                    Case Else : Await runner.RunAsync(url, startTime, endTime, _currentOutputDir)
                End Select

                _lblStepStatus.Text = _getString("Msg_Done")
                _pbOverall.Value = 100
                _btnOpenOutput.Enabled = True
                _btnOpenSubtitleEdit.Enabled = (_cboMode.SelectedIndex = 0 OrElse _cboMode.SelectedIndex = 3)
                _lnkPreviewSrt.Visible = (_cboMode.SelectedIndex = 0 OrElse _cboMode.SelectedIndex = 3)
            Catch ex As OperationCanceledException
                _lblStepStatus.Text = _getString("Msg_Cancelled")
                LogToUnified("Pipeline cancelled by user.", PipelineRunner.LogLevel.Err)
            Catch ex As PipelineException
                _lblStepStatus.Text = _getString(ex.MessageKey)
                LogToUnified($"ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
                If ex.MessageKey = "Err_ToolNotFound" Then
                    Services.Infrastructure.AppLogger.PromptDownloadManager(
                        ex.Message & vbCrLf & vbCrLf & _getString("Msg_OpenDownloadManager"),
                        _getString("Msg_DepsMissing"))
                Else
                    _notify?.Invoke(ex.Message, _getString("Msg_PipelineError"), MessageBoxIcon.Error)
                End If
            Catch ex As Exception
                _lblStepStatus.Text = _getString("Transcribe_Error")
                LogToUnified($"UNEXPECTED ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
                _notify?.Invoke(ex.Message, _getString("Msg_UnexpectedError"), MessageBoxIcon.Error)
            Finally
                _isRunning = False
                SetUiRunning(False)
            End Try
        End Sub

        Private Async Sub OnResumeClick(sender As Object, e As EventArgs)
            If _isRunning Then Return

            Using dlg As New FolderBrowserDialog()
                dlg.Description = _getString("Transcribe_SelectFolder")
                If Not String.IsNullOrWhiteSpace(_config.PathOutputRoot) Then
                    dlg.SelectedPath = AppConfig.ResolvePath(_config.PathOutputRoot)
                End If
                If dlg.ShowDialog() <> DialogResult.OK Then Return
                _txtOutputDir.Text = dlg.SelectedPath
            End Using

            _saveUiToConfig()

            _isRunning = True
            _cts = New CancellationTokenSource()
            _currentOutputDir = _txtOutputDir.Text

            SetUiRunning(True)
            _tabMain.SelectedTab = _tabPageJob
            _showLogPanel()
            Application.DoEvents()

            Dim progress = CreateProgress()
            Dim runner As New PipelineRunner(_config, progress, _cts.Token)
            AddHandler runner.LogMessage, Sub(s, entry) LogToUnified(entry.Message, entry.Level)

            Try
                Dim startTime = BuildTimeString(_txtStartHH.Text, _txtStartMM.Text, _txtStartSS.Text)
                Dim endTime = BuildTimeString(_txtEndHH.Text, _txtEndMM.Text, _txtEndSS.Text)

                Select Case _cboMode.SelectedIndex
                    Case 1 : Await runner.RunExtractAudioAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
                    Case 2 : Await runner.RunDownloadOnlyAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
                    Case Else : Await runner.RunAsync("", startTime, endTime, _currentOutputDir, resumeMode:=True)
                End Select

                _lblStepStatus.Text = _getString("Msg_Done")
                _pbOverall.Value = 100
                _btnOpenOutput.Enabled = True
                _lnkPreviewSrt.Visible = (_cboMode.SelectedIndex = 3)
            Catch ex As OperationCanceledException
                _lblStepStatus.Text = _getString("Msg_Cancelled")
                LogToUnified("Pipeline cancelled by user.", PipelineRunner.LogLevel.Err)
            Catch ex As PipelineException
                _lblStepStatus.Text = _getString(ex.MessageKey)
                LogToUnified($"ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
                If ex.MessageKey = "Err_ToolNotFound" Then
                    Services.Infrastructure.AppLogger.PromptDownloadManager(
                        ex.Message & vbCrLf & vbCrLf & _getString("Msg_OpenDownloadManager"),
                        _getString("Msg_DepsMissing"))
                Else
                    _notify?.Invoke(ex.Message, _getString("Msg_PipelineError"), MessageBoxIcon.Error)
                End If
            Catch ex As Exception
                _lblStepStatus.Text = _getString("Transcribe_Error")
                LogToUnified($"UNEXPECTED ERROR: {ex.Message}", PipelineRunner.LogLevel.Err)
                _notify?.Invoke(ex.Message, _getString("Msg_UnexpectedError"), MessageBoxIcon.Error)
            Finally
                _isRunning = False
                SetUiRunning(False)
            End Try
        End Sub

        Private Sub SetUiRunning(running As Boolean)
            _btnStart.Enabled = Not running
            _btnResume.Enabled = Not running
            _btnCancel.Enabled = running
            If running Then
                _btnOpenOutput.Enabled = False
                _btnOpenSubtitleEdit.Enabled = False
                _lnkPreviewSrt.Visible = False
                _pbOverall.Value = 0
                _pbChunk.Value = 0
                _pbChunk.Visible = False
            Else
                _pbChunk.Visible = False
            End If
        End Sub

        Private Function CreateProgress() As Progress(Of PipelineProgress)
            Return New Progress(Of PipelineProgress)(
                Sub(p)
                    _lblStepStatus.Text = p.StatusMessage
                    _pbOverall.Value = Math.Min(100, p.OverallPercent)
                    If p.ChunkTotal > 0 Then
                        _pbChunk.Visible = True
                        _pbChunk.Maximum = p.ChunkTotal
                        _pbChunk.Value = Math.Min(p.ChunkTotal, p.ChunkDone)
                    End If
                End Sub)
        End Function

        Private Sub LogToUnified(message As String, level As PipelineRunner.LogLevel)
            If level = PipelineRunner.LogLevel.Verbose Then Return
            Dim color As Drawing.Color
            Select Case level
                Case PipelineRunner.LogLevel.Success : color = Drawing.Color.FromArgb(80, 200, 120)
                Case PipelineRunner.LogLevel.Err : color = Drawing.Color.FromArgb(255, 100, 100)
                Case Else : color = Drawing.Color.FromArgb(200, 200, 200)
            End Select
            _appendLog("Pipeline", message, color)
        End Sub

        Private Sub BrowseFile()
            Using dlg As New OpenFileDialog()
                If _cboMode.SelectedIndex = 0 Then
                    dlg.Filter = "Audio/Video files|*.wav;*.mp3;*.ogg;*.flac;*.m4a;*.wma;*.aac;*.opus;*.webm;*.mp4;*.mkv;*.avi;*.mov;*.wmv|All files|*.*"
                    Dim resolvedRoot = AppConfig.ResolvePath(_config.PathOutputRoot)
                    If Not String.IsNullOrWhiteSpace(resolvedRoot) AndAlso Directory.Exists(resolvedRoot) Then
                        dlg.InitialDirectory = resolvedRoot
                    End If
                Else
                    dlg.Filter = "Video/Audio files|*.mp4;*.mkv;*.avi;*.webm;*.wav;*.mp3;*.m4a;*.flac|All files|*.*"
                End If
                If dlg.ShowDialog() = DialogResult.OK Then
                    _txtUrl.Text = dlg.FileName
                End If
            End Using
        End Sub

        Private Sub BrowseOutput()
            Using dlg As New FolderBrowserDialog()
                dlg.SelectedPath = _txtOutputDir.Text
                If dlg.ShowDialog() = DialogResult.OK Then
                    _txtOutputDir.Text = dlg.SelectedPath
                End If
            End Using
        End Sub

        Private Sub OpenOutput()
            If Directory.Exists(_currentOutputDir) Then
                Process.Start("explorer.exe", _currentOutputDir)
            End If
        End Sub

        Private Sub PreviewSrt()
            Dim srtPath = FindOutputSrt()
            If srtPath IsNot Nothing Then
                Process.Start(New ProcessStartInfo(srtPath) With {.UseShellExecute = True})
            End If
        End Sub

        Private Sub OpenSubtitleEdit()
            Dim srtPath = FindOutputSrt()
            If srtPath Is Nothing Then
                _notify?.Invoke(_getString("Msg_NoSrtFound"), "Subtitle Edit", MessageBoxIcon.Warning)
                Return
            End If
            Dim subtitleEditPath = AppConfig.ResolvePath(_config.PathSubtitleEdit)
            If Not File.Exists(subtitleEditPath) Then
                _notify?.Invoke($"{_getString("Msg_SubtitleEditNotFound")} {subtitleEditPath}", "Subtitle Edit", MessageBoxIcon.Warning)
                Return
            End If
            Process.Start(subtitleEditPath, $"""{srtPath}""")
        End Sub

        Private Function FindOutputSrt() As String
            If String.IsNullOrWhiteSpace(_currentOutputDir) OrElse Not Directory.Exists(_currentOutputDir) Then Return Nothing
            Dim previewSrt = Path.Combine(_currentOutputDir, "preview.srt")
            If File.Exists(previewSrt) Then Return previewSrt
            Dim srtFiles = Directory.GetFiles(_currentOutputDir, "*.srt")
            If srtFiles.Length > 0 Then Return srtFiles(0)
            Return Nothing
        End Function

        Private Sub OnModelChanged()
            If _config Is Nothing OrElse _cboModel.SelectedItem Is Nothing Then Return
            Dim modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModel))
            If String.IsNullOrWhiteSpace(modelDir) Then modelDir = Path.GetDirectoryName(AppConfig.ResolvePath(_config.PathModelAudio))
            Dim fullPath = Path.Combine(If(modelDir, ""), _cboModel.SelectedItem.ToString())

            Select Case _cboMode.SelectedIndex
                Case 0 : _config.PathModelAudio = fullPath
                Case 3 : _config.PathModel = fullPath
                Case Else : Return
            End Select
            ConfigManager.Save(_config)
        End Sub

        Private Shared Function BuildTimeString(hh As String, mm As String, ss As String) As String
            hh = hh.Trim()
            mm = mm.Trim()
            ss = ss.Trim()
            If hh.Length = 0 AndAlso mm.Length = 0 AndAlso ss.Length = 0 Then Return ""
            Return $"{hh.PadLeft(2, "0"c)}:{mm.PadLeft(2, "0"c)}:{ss.PadLeft(2, "0"c)}"
        End Function

        Private Shared Sub SelectComboItem(cbo As ComboBox, text As String)
            If String.IsNullOrEmpty(text) Then Return
            For i = 0 To cbo.Items.Count - 1
                If String.Equals(cbo.Items(i).ToString(), text, StringComparison.OrdinalIgnoreCase) Then
                    cbo.SelectedIndex = i
                    Return
                End If
            Next
        End Sub

    End Class
End Namespace
