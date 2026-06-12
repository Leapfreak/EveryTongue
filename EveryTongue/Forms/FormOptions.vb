' FormOptions.vb — VS-style Options dialog with tree categories
' Phase 2 of the UI redesign — consolidates Settings, Paths, and Server tabs.

Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

Public Class FormOptions
    Inherits Form

    ' ── Result ────────────────────────────────────────────────────
    ''' <summary>True if user clicked OK and config was modified.</summary>
    Public Property ConfigChanged As Boolean = False

    ' ── Config reference ──────────────────────────────────────────
    Private ReadOnly _config As AppConfig
    Private ReadOnly _uiLocales As (Code As String, Name As String)()
    Private _hwInfo As HardwareInfo

    ' ═══════════════════════════════════════════════════════════════
    ' Constructor
    ' ═══════════════════════════════════════════════════════════════
    Public Sub New(config As AppConfig, uiLocales As (Code As String, Name As String)())
        _config = config
        _uiLocales = uiLocales
        InitializeComponent()
        treeNav.SelectedNode = treeNav.Nodes("general")
        WireEvents()
        PopulateFontCombo()
        LoadFromConfig()
        ApplyLocale()
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Localization
    ' ═══════════════════════════════════════════════════════════════
    Private Sub ApplyLocale()
        Dim langPack = LanguagePackService.Instance

        Me.Text = langPack.GetString("Opt_Title")
        btnOk.Text = langPack.GetString("Opt_OK")
        btnCancel.Text = langPack.GetString("Opt_Cancel")

        ' Tree nav nodes
        treeNav.Nodes("general").Text = langPack.GetString("Opt_NavGeneral")
        treeNav.Nodes("paths").Text = langPack.GetString("Opt_NavPaths")
        treeNav.Nodes("stt").Text = langPack.GetString("Opt_NavStt")
        treeNav.Nodes("server").Text = langPack.GetString("Opt_NavServer")
        treeNav.Nodes("display").Text = langPack.GetString("Opt_NavDisplay")
        treeNav.Nodes("translation").Text = langPack.GetString("Opt_NavTranslation")
        treeNav.Nodes("tts").Text = langPack.GetString("Opt_NavTts")
        treeNav.Nodes("hardware").Text = langPack.GetString("Opt_NavHardware")
        treeNav.Nodes("advanced").Text = langPack.GetString("Opt_NavAdvanced")
        btnManageSttTemplatesOpt.Text = langPack.GetString("Opt_ManageSttTemplates")
        btnManageDisplayTplOpt.Text = langPack.GetString("Opt_ManageDisplayTemplates")
        btnManageTransTplOpt.Text = langPack.GetString("Opt_ManageTransTemplates")
        btnManageTtsTplOpt.Text = langPack.GetString("Opt_ManageTtsTemplates")

        ' General panel
        lblAppearanceHeader.Text = langPack.GetString("Opt_AppearanceHeader")
        lblUiLang.Text = langPack.GetString("Opt_UiLanguage")
        lblTheme.Text = langPack.GetString("Opt_Theme")
        lblLogLevel.Text = langPack.GetString("Opt_LogLevel")
        lblStartupHeader.Text = langPack.GetString("Opt_StartupHeader")
        chkStartWindows.Text = langPack.GetString("Opt_StartWithWindows")
        chkStartMinimized.Text = langPack.GetString("Opt_StartMinimized")
        chkMinimizeToTray.Text = langPack.GetString("Opt_MinimizeToTray")
        chkResetFirstRun.Text = langPack.GetString("Opt_ResetFirstRun")

        ' Paths panel
        lblToolPathsHeader.Text = langPack.GetString("Opt_ToolPathsHeader")
        lblWhisperPath.Text = langPack.GetString("Opt_WhisperPath")
        lblYtdlpPath.Text = langPack.GetString("Opt_YtdlpPath")
        lblFfmpegPath.Text = langPack.GetString("Opt_FfmpegPath")
        lblFfprobePath.Text = langPack.GetString("Opt_FfprobePath")
        lblSubtitleEditPath.Text = langPack.GetString("Opt_SubtitleEditPath")
        lblModelPathsHeader.Text = langPack.GetString("Opt_ModelPathsHeader")
        lblWhisperServerPath.Text = langPack.GetString("Opt_WhisperServerPath")
        lblGgmlModelPath.Text = langPack.GetString("Opt_GgmlModelPath")
        lblFwModelPath.Text = langPack.GetString("Opt_FwModelPath")
        lblTransModelPath.Text = langPack.GetString("Opt_TransModelPath")
        lblModelPath.Text = langPack.GetString("Opt_ModelPath")
        lblModelAudioPath.Text = langPack.GetString("Opt_ModelAudioPath")
        lblDirectoriesHeader.Text = langPack.GetString("Opt_DirectoriesHeader")
        lblOutputRootPath.Text = langPack.GetString("Opt_OutputRootPath")
        lblGlossaryPath.Text = langPack.GetString("Opt_GlossaryPath")
        lblBiblesPath.Text = langPack.GetString("Opt_BiblesPath")
        lblLogsPath.Text = langPack.GetString("Opt_LogsPath")
        lblAdvancedHeader.Text = langPack.GetString("Opt_PathsAdvancedHeader")
        lblYtdlpFormat.Text = langPack.GetString("Opt_YtdlpFormat")

        ' Server panel
        lblNetworkHeader.Text = langPack.GetString("Opt_NetworkHeader")
        lblPort.Text = langPack.GetString("Opt_SubtitlePort")
        lblLivePort.Text = langPack.GetString("Opt_LivePort")
        lblTransPort.Text = langPack.GetString("Opt_TransPort")
        chkFirewall.Text = langPack.GetString("Opt_AllowFirewall")
        lblPin.Text = langPack.GetString("Opt_AdminPin")
        lblSubtitleAppearanceHeader.Text = langPack.GetString("Opt_SubtitleAppearanceHeader")
        lblBgColor.Text = langPack.GetString("Opt_BgColor")
        lblFgColor.Text = langPack.GetString("Opt_FgColor")
        lblFont.Text = langPack.GetString("Opt_Font")
        lblFontSize.Text = langPack.GetString("Opt_FontSize")
        chkBold.Text = langPack.GetString("Opt_Bold")
        lblTemplatesHeader.Text = langPack.GetString("Tmpl_Title")
        btnManageTemplates.Text = langPack.GetString("Tmpl_Title") & "..."

        ' Translation panel
        lblTranslationHeader.Text = langPack.GetString("Opt_TranslationHeader")
        chkTransEnabled.Text = langPack.GetString("Opt_TransEnabled")
        chkUseSpeechmaticsTranslation.Text = langPack.GetString("Opt_UseSpeechmaticsTranslation")
        lblClauseHeader.Text = langPack.GetString("Opt_ClauseHeader")
        chkSpeechmaticsHoldClauses.Text = langPack.GetString("Opt_HoldClauses")
        chkClauseLockOnPunctuation.Text = langPack.GetString("Opt_ClauseLockOnPunct")
        lblClauseGraceMs.Text = langPack.GetString("Opt_ClauseGraceMs")
        lblClauseMaxMs.Text = langPack.GetString("Opt_ClauseMaxMs")
        lblClauseMaxChars.Text = langPack.GetString("Opt_ClauseMaxChars")
        lblClauseMinLockChars.Text = langPack.GetString("Opt_ClauseMinLockChars")
        lblClauseTimerMs.Text = langPack.GetString("Opt_ClauseTimerMs")
        lblClauseSentenceEnders.Text = langPack.GetString("Opt_ClauseSentenceEnders")
        lblTransBackend.Text = langPack.GetString("Opt_TransBackend")
        lblDevice.Text = langPack.GetString("Opt_TransDevice")

        ' TTS panel
        lblTtsHeader.Text = langPack.GetString("Opt_TtsHeader")
        lblTtsPref1.Text = langPack.GetString("Opt_TtsPref1")
        lblTtsPref2.Text = langPack.GetString("Opt_TtsPref2")
        lblTtsPref3.Text = langPack.GetString("Opt_TtsPref3")
        lblTtsNote.Text = langPack.GetString("Opt_TtsNote")

        ' Hardware panel
        lblHwHeader.Text = langPack.GetString("Opt_HwHeader")
        lblHwOverallCaption.Text = langPack.GetString("Opt_HwOverallCaption")
        lblHwBreakdownHeader.Text = langPack.GetString("Opt_HwBreakdownHeader")
        lblHwRecsHeader.Text = langPack.GetString("Opt_HwRecsHeader")
        btnHwRescan.Text = langPack.GetString("Opt_HwRescan")
        If _hwInfo Is Nothing Then
            lblHwVerdict.Text = langPack.GetString("Opt_HwVerdict")
        End If
        lblSttEngineHeader.Text = langPack.GetString("Opt_SttEngineHeader")
        lblSttBackend.Text = langPack.GetString("Opt_SttBackend")
        lblSttApiKey.Text = langPack.GetString("Opt_SttApiKey")
        lblSttOperatingPoint.Text = langPack.GetString("Opt_SttOperatingPoint")
        lblSttRegion.Text = langPack.GetString("Opt_SttRegion")
        lblSttEouSilence.Text = langPack.GetString("Opt_SttEouSilence")

        ' Advanced panel
        lblAdvPipelineHeader.Text = langPack.GetString("Opt_AdvPipelineHeader")
        lblParallelJobs.Text = langPack.GetString("Opt_ParallelJobs")
        lblChunkSize.Text = langPack.GetString("Opt_ChunkSize")
        lblPollInterval.Text = langPack.GetString("Opt_PollInterval")
        lblChunkTimeout.Text = langPack.GetString("Opt_ChunkTimeout")
        chkKeepChunks.Text = langPack.GetString("Opt_KeepChunks")
        chkKeepPreview.Text = langPack.GetString("Opt_KeepPreview")
        lblAdvOutputHeader.Text = langPack.GetString("Opt_AdvOutputHeader")
        chkOutTxt.Text = langPack.GetString("Opt_OutPlainText")
        lblAdvWhisperHeader.Text = langPack.GetString("Opt_AdvWhisperHeader")
        lblThreads.Text = langPack.GetString("Opt_Threads")
        lblProcessors.Text = langPack.GetString("Opt_Processors")
        lblBeamSize.Text = langPack.GetString("Opt_BeamSize")
        lblBestOf.Text = langPack.GetString("Opt_BestOf")
        lblTemperature.Text = langPack.GetString("Opt_Temperature")
        lblTempInc.Text = langPack.GetString("Opt_TempInc")
        lblMaxContext.Text = langPack.GetString("Opt_MaxContext")
        lblMaxSegLen.Text = langPack.GetString("Opt_MaxSegLen")
        lblMaxTokens.Text = langPack.GetString("Opt_MaxTokens")
        lblAudioContext.Text = langPack.GetString("Opt_AudioContext")
        lblWordThresh.Text = langPack.GetString("Opt_WordThresh")
        lblEntropyThresh.Text = langPack.GetString("Opt_EntropyThresh")
        lblLogProbThresh.Text = langPack.GetString("Opt_LogProbThresh")
        lblNoSpeechThresh.Text = langPack.GetString("Opt_NoSpeechThresh")
        lblVadThresh.Text = langPack.GetString("Opt_VadThresh")
        lblInitialPrompt.Text = langPack.GetString("Opt_InitialPrompt")
        lblAdvFlagsHeader.Text = langPack.GetString("Opt_AdvFlagsHeader")
        chkSplitOnWord.Text = langPack.GetString("Opt_SplitOnWord")
        chkNoGpu.Text = langPack.GetString("Opt_NoGpu")
        chkFlashAttn.Text = langPack.GetString("Opt_FlashAttn")
        chkDiarize.Text = langPack.GetString("Opt_Diarize")
        chkTinyDiarize.Text = langPack.GetString("Opt_TinyDiarize")
        chkTranslateEn.Text = langPack.GetString("Opt_TranslateEn")
        chkNoTimestamps.Text = langPack.GetString("Opt_NoTimestamps")
        chkPrintProgress.Text = langPack.GetString("Opt_PrintProgress")
        chkPrintColours.Text = langPack.GetString("Opt_PrintColours")
        lblAdvLiveHeader.Text = langPack.GetString("Opt_AdvLiveHeader")
        lblComputeType.Text = langPack.GetString("Opt_ComputeType")
        lblLiveVadSilence.Text = langPack.GetString("Opt_LiveVadSilence")
        lblLiveMaxSeg.Text = langPack.GetString("Opt_LiveMaxSeg")
        lblLiveInterim.Text = langPack.GetString("Opt_LiveInterim")
        lblAdvBibleHeader.Text = langPack.GetString("Opt_AdvBibleHeader")
        chkShowBibleCopyright.Text = langPack.GetString("Opt_ShowBibleCopyright")
        lblAdvLivePipelineHeader.Text = langPack.GetString("Opt_AdvLivePipelineHeader")
        lblTranslationConcurrency.Text = langPack.GetString("Opt_TranslationConcurrency")
        lblTtsConcurrency.Text = langPack.GetString("Opt_TtsConcurrency")
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Post-designer wiring (event handlers that reference runtime state)
    ' ═══════════════════════════════════════════════════════════════
    Private Sub WireEvents()
        ' OK button
        AddHandler btnOk.Click, Sub(s, e) ApplyToConfig()

        ' Tree navigation
        AddHandler treeNav.AfterSelect, AddressOf TreeNav_AfterSelect

        ' Color pickers
        AddHandler btnBgColor.Click, Sub(s, e) PickColor(btnBgColor)
        AddHandler btnFgColor.Click, Sub(s, e) PickColor(btnFgColor)

        ' STT template library manager
        AddHandler btnManageSttTemplatesOpt.Click, Sub(s, e)
                                                       Using frm As New FormEngineTemplates(_config)
                                                           frm.Icon = Me.Icon
                                                           frm.ShowDialog(Me)
                                                       End Using
                                                   End Sub

        ' Display template library manager
        AddHandler btnManageDisplayTplOpt.Click, Sub(s, e)
                                                     Using frm As New FormDisplayTemplates()
                                                         frm.Icon = Me.Icon
                                                         frm.ShowDialog(Me)
                                                     End Using
                                                 End Sub

        ' Translation / TTS template library managers
        AddHandler btnManageTransTplOpt.Click, Sub(s, e)
                                                   Using frm As New FormEngineTemplates(_config, Services.Config.TemplateLibraryStore.GroupTranslate)
                                                       frm.Icon = Me.Icon
                                                       frm.ShowDialog(Me)
                                                   End Using
                                               End Sub
        AddHandler btnManageTtsTplOpt.Click, Sub(s, e)
                                                 Using frm As New FormEngineTemplates(_config, Services.Config.TemplateLibraryStore.GroupTts)
                                                     frm.Icon = Me.Icon
                                                     frm.ShowDialog(Me)
                                                 End Using
                                             End Sub

        ' Browse buttons — file pickers
        AddHandler btnBrowseWhisper.Click, Sub(s, e) BrowseFile(txtWhisper)
        AddHandler btnBrowseYtdlp.Click, Sub(s, e) BrowseFile(txtYtdlp)
        AddHandler btnBrowseFfmpeg.Click, Sub(s, e) BrowseFile(txtFfmpeg)
        AddHandler btnBrowseFfprobe.Click, Sub(s, e) BrowseFile(txtFfprobe)
        AddHandler btnBrowseSubtitleEdit.Click, Sub(s, e) BrowseFile(txtSubtitleEdit)
        AddHandler btnBrowseWhisperServer.Click, Sub(s, e) BrowseFile(txtWhisperServer)
        AddHandler btnBrowseGgmlModel.Click, Sub(s, e) BrowseFile(txtGgmlModel)
        AddHandler btnBrowseFwModel.Click, Sub(s, e) BrowseFolder(txtFwModel)
        AddHandler btnBrowseModel.Click, Sub(s, e) BrowseFile(txtModel)
        AddHandler btnBrowseModelAudio.Click, Sub(s, e) BrowseFile(txtModelAudio)
        AddHandler btnBrowseGlossary.Click, Sub(s, e) BrowseFile(txtGlossary)

        ' Hardware re-scan
        AddHandler btnHwRescan.Click, Sub(s, e) RunHardwareScan()

        ' Template manager
        AddHandler btnManageTemplates.Click, Sub(s, e) OpenTemplateManager()

        ' Browse buttons — folder pickers
        AddHandler btnBrowseTransModel.Click, Sub(s, e) BrowseFolder(txtTransModel)
        AddHandler btnBrowseOutputRoot.Click, Sub(s, e) BrowseFolder(txtOutputRoot)
        AddHandler btnBrowseBibles.Click, Sub(s, e) BrowseFolder(txtBibles)
        AddHandler btnBrowseLogs.Click, Sub(s, e) BrowseFolder(txtLogs)
    End Sub

    Private Sub PopulateFontCombo()
        For Each fam In Drawing.FontFamily.Families
            cboFont.Items.Add(fam.Name)
        Next
        For Each locale In _uiLocales
            cboUiLang.Items.Add(locale.Name)
        Next
    End Sub

    Private Sub OpenTemplateManager()
        Using frm As New FormTemplateManager(_config)
            frm.Icon = Me.Icon
            If frm.ShowDialog(Me) = DialogResult.OK Then
                ConfigChanged = True
            End If
        End Using
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Navigation
    ' ═══════════════════════════════════════════════════════════════
    Private Sub TreeNav_AfterSelect(sender As Object, e As TreeViewEventArgs)
        pnlGeneral.Visible = (e.Node.Name = "general")
        pnlPaths.Visible = (e.Node.Name = "paths")
        pnlStt.Visible = (e.Node.Name = "stt")
        pnlServer.Visible = (e.Node.Name = "server")
        pnlDisplay.Visible = (e.Node.Name = "display")
        pnlTranslation.Visible = (e.Node.Name = "translation")
        pnlTts.Visible = (e.Node.Name = "tts")
        pnlHardware.Visible = (e.Node.Name = "hardware")
        pnlAdvanced.Visible = (e.Node.Name = "advanced")

        ' Auto-scan on first visit to hardware panel
        If e.Node.Name = "hardware" AndAlso _hwInfo Is Nothing Then
            RunHardwareScan()
        End If
    End Sub

    ''' <summary>Opens the dialog pre-selecting the given category.</summary>
    Public Sub SelectCategory(key As String)
        Dim node = treeNav.Nodes(key)
        If node IsNot Nothing Then treeNav.SelectedNode = node
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Config load / save
    ' ═══════════════════════════════════════════════════════════════
    Private Sub LoadFromConfig()
        ' General
        For i = 0 To _uiLocales.Length - 1
            If _uiLocales(i).Code.Equals(_config.UiLanguage, StringComparison.OrdinalIgnoreCase) Then
                cboUiLang.SelectedIndex = i
                Exit For
            End If
        Next
        SelectItem(cboTheme, _config.Theme.ToString())
        SelectItem(cboLogLevel, _config.LogLevel.ToString())
        chkStartWindows.Checked = _config.StartWithWindows
        chkStartMinimized.Checked = _config.StartMinimized
        chkMinimizeToTray.Checked = _config.MinimizeToTray

        ' Paths
        txtWhisper.Text = _config.PathWhisper
        txtYtdlp.Text = _config.PathYtdlp
        txtFfmpeg.Text = _config.PathFfmpeg
        txtFfprobe.Text = _config.PathFfprobe
        txtWhisperServer.Text = _config.PathWhisperServer
        txtGgmlModel.Text = _config.PathWhisperCppModel
        txtFwModel.Text = _config.PathFasterWhisperModel
        txtTransModel.Text = _config.TranslationModelPath
        txtModel.Text = _config.PathModel
        txtModelAudio.Text = _config.PathModelAudio
        txtOutputRoot.Text = _config.PathOutputRoot
        txtYtdlpFormat.Text = _config.YtdlpFormat
        txtSubtitleEdit.Text = _config.PathSubtitleEdit
        txtGlossary.Text = _config.TranslationGlossaryPath
        txtBibles.Text = _config.BiblesDirectory
        txtLogs.Text = _config.LogsDirectory

        ' Server
        nudPort.Value = _config.SubtitleServerPort
        nudLivePort.Value = _config.LiveServerPort
        chkFirewall.Checked = _config.AllowFirewall
        txtPin.Text = _config.AdminPin

        btnBgColor.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleBgColor, "#000000"))
        btnFgColor.BackColor = Drawing.ColorTranslator.FromHtml(If(_config.SubtitleFgColor, "#FFFFFF"))
        SelectItem(cboFont, _config.SubtitleFontFamily)
        nudFontSize.Value = CDec(_config.SubtitleFontSize)
        chkBold.Checked = _config.SubtitleFontBold

        ' Translation
        PopulateTransBackendCombo()
        SelectTransBackend(_config.TranslationBackend)
        chkTransEnabled.Checked = _config.TranslationEnabled
        chkUseSpeechmaticsTranslation.Checked = _config.UseSpeechmaticsTranslation
        chkSpeechmaticsHoldClauses.Checked = _config.SpeechmaticsHoldClauses
        chkClauseLockOnPunctuation.Checked = _config.SpeechmaticsClauseLockOnPunctuation
        nudClauseGraceMs.Value = ClampNud(nudClauseGraceMs, _config.SpeechmaticsClauseGraceMs)
        nudClauseMaxMs.Value = ClampNud(nudClauseMaxMs, _config.SpeechmaticsClauseMaxMs)
        nudClauseMaxChars.Value = ClampNud(nudClauseMaxChars, _config.SpeechmaticsClauseMaxChars)
        nudClauseMinLockChars.Value = ClampNud(nudClauseMinLockChars, _config.SpeechmaticsClauseMinLockChars)
        nudClauseTimerMs.Value = ClampNud(nudClauseTimerMs, _config.SpeechmaticsClauseTimerMs)
        txtClauseSentenceEnders.Text = _config.SpeechmaticsClauseSentenceEnders
        SelectItem(cboDevice, _config.TranslationDevice)
        nudTransPort.Value = _config.TranslationPort

        ' TTS
        PopulateTtsCombos()
        LoadTtsPreferences(_config.TtsBackends)

        ' STT Engine
        PopulateSttBackendCombo()
        PopulateSpeechmaticsCombos()
        SelectSttBackend(_config.SttBackend)
        _currentApiKeyBackend = CurrentSttKey()
        txtSttApiKey.Text = _config.GetSttApiKey(_currentApiKeyBackend)
        SelectComboByValue(cboSttOperatingPoint, _opKeys, _config.SpeechmaticsOperatingPoint)
        SelectComboByValue(cboSttRegion, _regionKeys, _config.SpeechmaticsRegion)
        nudSttEouSilence.Value = ClampNud(nudSttEouSilence, _config.SpeechmaticsEouSilenceMs)
        UpdateSttApiKeyVisibility()

        ' Advanced — Pipeline
        nudParallelJobs.Value = _config.ParallelJobs
        nudChunkSize.Value = _config.ChunkSizeSec
        nudPollInterval.Value = _config.PollIntervalMs
        nudChunkTimeout.Value = _config.ChunkTimeoutMin
        chkKeepChunks.Checked = _config.KeepChunkFiles
        chkKeepPreview.Checked = _config.KeepPreview

        ' Advanced — Output formats
        chkOutSrt.Checked = _config.OutputSrt
        chkOutVtt.Checked = _config.OutputVtt
        chkOutTxt.Checked = _config.OutputTxt
        chkOutJson.Checked = _config.OutputJson
        chkOutCsv.Checked = _config.OutputCsv
        chkOutLrc.Checked = _config.OutputLrc

        ' Advanced — Whisper parameters
        nudThreads.Value = _config.Threads
        nudProcessors.Value = _config.Processors
        nudBeamSize.Value = _config.BeamSize
        nudBestOf.Value = _config.BestOf
        nudTemperature.Value = CDec(_config.Temperature)
        nudTempInc.Value = CDec(_config.TemperatureInc)
        nudMaxContext.Value = _config.MaxContext
        nudMaxSegLen.Value = _config.MaxSegmentLength
        nudMaxTokens.Value = _config.MaxTokens
        nudAudioContext.Value = _config.AudioContext
        nudWordThresh.Value = CDec(_config.WordThreshold)
        nudEntropyThresh.Value = CDec(_config.EntropyThreshold)
        nudLogProbThresh.Value = CDec(_config.LogProbThreshold)
        nudNoSpeechThresh.Value = CDec(_config.NoSpeechThreshold)
        nudVadThresh.Value = CDec(_config.VadThreshold)
        txtInitialPrompt.Text = _config.InitialPrompt

        ' Advanced — Whisper flags
        chkSplitOnWord.Checked = _config.SplitOnWord
        chkNoGpu.Checked = _config.NoGpu
        chkFlashAttn.Checked = _config.FlashAttn
        chkDiarize.Checked = _config.Diarize
        chkTinyDiarize.Checked = _config.Tinydiarize
        chkTranslateEn.Checked = _config.TranslateToEnglish
        chkNoTimestamps.Checked = _config.NoTimestamps
        chkPrintProgress.Checked = _config.PrintProgress
        chkPrintColours.Checked = _config.PrintColours

        ' Advanced — Bible
        chkShowBibleCopyright.Checked = _config.ShowBibleCopyright

        ' Advanced — Live transcription
        SelectItem(cboComputeType, _config.LiveComputeType)
        nudLiveVadSilence.Value = _config.LiveVadSilenceMs
        nudLiveMaxSeg.Value = _config.LiveMaxSegmentSec
        nudLiveInterim.Value = _config.LiveInterimIntervalMs

        ' Advanced — Live Pipeline concurrency
        nudTranslationConcurrency.Value = Math.Max(1, Math.Min(10, _config.TranslationConcurrency))
        nudTtsConcurrency.Value = Math.Max(1, Math.Min(10, _config.TtsConcurrency))
    End Sub

    Private Sub ApplyToConfig()
        ' General
        If cboUiLang.SelectedIndex >= 0 AndAlso cboUiLang.SelectedIndex < _uiLocales.Length Then
            _config.UiLanguage = _uiLocales(cboUiLang.SelectedIndex).Code
        End If
        If cboTheme.SelectedItem IsNot Nothing Then
            Dim parsed As Models.ThemeMode
            If [Enum].TryParse(cboTheme.SelectedItem.ToString(), True, parsed) Then
                _config.Theme = parsed
            End If
        End If
        If cboLogLevel.SelectedItem IsNot Nothing Then
            Dim parsed As Models.LogVerbosity
            If [Enum].TryParse(cboLogLevel.SelectedItem.ToString(), True, parsed) Then
                _config.LogLevel = parsed
            End If
        End If
        _config.StartWithWindows = chkStartWindows.Checked
        _config.StartMinimized = chkStartMinimized.Checked
        _config.MinimizeToTray = chkMinimizeToTray.Checked
        If chkResetFirstRun.Checked Then _config.FirstRunComplete = False

        ' Paths
        _config.PathWhisper = txtWhisper.Text
        _config.PathYtdlp = txtYtdlp.Text
        _config.PathFfmpeg = txtFfmpeg.Text
        _config.PathFfprobe = txtFfprobe.Text
        _config.PathWhisperServer = txtWhisperServer.Text
        _config.PathWhisperCppModel = txtGgmlModel.Text
        _config.PathFasterWhisperModel = txtFwModel.Text
        _config.TranslationModelPath = txtTransModel.Text
        _config.PathModel = txtModel.Text
        _config.PathModelAudio = txtModelAudio.Text
        _config.PathOutputRoot = txtOutputRoot.Text
        _config.YtdlpFormat = txtYtdlpFormat.Text
        _config.PathSubtitleEdit = txtSubtitleEdit.Text
        _config.TranslationGlossaryPath = txtGlossary.Text
        _config.BiblesDirectory = txtBibles.Text
        _config.LogsDirectory = txtLogs.Text

        ' Update the live logger directory
        Services.Infrastructure.AppLogger.LogDirectory = _config.LogsDirectory

        ' Server
        _config.SubtitleServerPort = CInt(nudPort.Value)
        _config.LiveServerPort = CInt(nudLivePort.Value)
        _config.AllowFirewall = chkFirewall.Checked
        _config.AdminPin = txtPin.Text.Trim()
        _config.SubtitleBgColor = Drawing.ColorTranslator.ToHtml(btnBgColor.BackColor)
        _config.SubtitleFgColor = Drawing.ColorTranslator.ToHtml(btnFgColor.BackColor)
        _config.SubtitleFontFamily = If(cboFont.SelectedItem?.ToString(), "Segoe UI")
        _config.SubtitleFontSize = CSng(nudFontSize.Value)
        _config.SubtitleFontBold = chkBold.Checked

        ' Translation
        If cboTransBackend.SelectedIndex >= 0 AndAlso cboTransBackend.SelectedIndex < _transEntries.Count Then
            Dim entry = _transEntries(cboTransBackend.SelectedIndex)
            _config.TranslationBackend = entry.Key
            If entry.ModelType IsNot Nothing Then
                _config.TranslationModelType = entry.ModelType
            End If
        End If
        _config.TranslationEnabled = chkTransEnabled.Checked
        _config.UseSpeechmaticsTranslation = chkUseSpeechmaticsTranslation.Checked
        _config.SpeechmaticsHoldClauses = chkSpeechmaticsHoldClauses.Checked
        _config.SpeechmaticsClauseLockOnPunctuation = chkClauseLockOnPunctuation.Checked
        _config.SpeechmaticsClauseGraceMs = CInt(nudClauseGraceMs.Value)
        _config.SpeechmaticsClauseMaxMs = CInt(nudClauseMaxMs.Value)
        _config.SpeechmaticsClauseMaxChars = CInt(nudClauseMaxChars.Value)
        _config.SpeechmaticsClauseMinLockChars = CInt(nudClauseMinLockChars.Value)
        _config.SpeechmaticsClauseTimerMs = CInt(nudClauseTimerMs.Value)
        _config.SpeechmaticsClauseSentenceEnders = txtClauseSentenceEnders.Text
        If cboDevice.SelectedItem IsNot Nothing Then _config.TranslationDevice = cboDevice.SelectedItem.ToString()
        _config.TranslationPort = CInt(nudTransPort.Value)

        ' TTS
        _config.TtsBackends = BuildTtsBackendsString()

        ' STT Engine
        If cboSttBackend.SelectedIndex >= 0 AndAlso cboSttBackend.SelectedIndex < _sttKeys.Length Then
            _config.SttBackend = _sttKeys(cboSttBackend.SelectedIndex)
        End If
        ' Save the API key against the backend currently shown in the textbox.
        If Not String.IsNullOrEmpty(_currentApiKeyBackend) Then
            _config.SetSttApiKey(_currentApiKeyBackend, txtSttApiKey.Text.Trim())
        End If
        If cboSttOperatingPoint.SelectedIndex >= 0 Then
            _config.SpeechmaticsOperatingPoint = _opKeys(cboSttOperatingPoint.SelectedIndex)
        End If
        If cboSttRegion.SelectedIndex >= 0 Then
            _config.SpeechmaticsRegion = _regionKeys(cboSttRegion.SelectedIndex)
        End If
        _config.SpeechmaticsEouSilenceMs = CInt(nudSttEouSilence.Value)

        ' Advanced — Pipeline
        _config.ParallelJobs = CInt(nudParallelJobs.Value)
        _config.ChunkSizeSec = CInt(nudChunkSize.Value)
        _config.PollIntervalMs = CInt(nudPollInterval.Value)
        _config.ChunkTimeoutMin = CInt(nudChunkTimeout.Value)
        _config.KeepChunkFiles = chkKeepChunks.Checked
        _config.KeepPreview = chkKeepPreview.Checked

        ' Advanced — Output formats
        _config.OutputSrt = chkOutSrt.Checked
        _config.OutputVtt = chkOutVtt.Checked
        _config.OutputTxt = chkOutTxt.Checked
        _config.OutputJson = chkOutJson.Checked
        _config.OutputCsv = chkOutCsv.Checked
        _config.OutputLrc = chkOutLrc.Checked

        ' Advanced — Whisper parameters
        _config.Threads = CInt(nudThreads.Value)
        _config.Processors = CInt(nudProcessors.Value)
        _config.BeamSize = CInt(nudBeamSize.Value)
        _config.BestOf = CInt(nudBestOf.Value)
        _config.Temperature = CSng(nudTemperature.Value)
        _config.TemperatureInc = CSng(nudTempInc.Value)
        _config.MaxContext = CInt(nudMaxContext.Value)
        _config.MaxSegmentLength = CInt(nudMaxSegLen.Value)
        _config.MaxTokens = CInt(nudMaxTokens.Value)
        _config.AudioContext = CInt(nudAudioContext.Value)
        _config.WordThreshold = CSng(nudWordThresh.Value)
        _config.EntropyThreshold = CSng(nudEntropyThresh.Value)
        _config.LogProbThreshold = CSng(nudLogProbThresh.Value)
        _config.NoSpeechThreshold = CSng(nudNoSpeechThresh.Value)
        _config.VadThreshold = CSng(nudVadThresh.Value)
        _config.InitialPrompt = txtInitialPrompt.Text

        ' Advanced — Whisper flags
        _config.SplitOnWord = chkSplitOnWord.Checked
        _config.NoGpu = chkNoGpu.Checked
        _config.FlashAttn = chkFlashAttn.Checked
        _config.Diarize = chkDiarize.Checked
        _config.Tinydiarize = chkTinyDiarize.Checked
        _config.TranslateToEnglish = chkTranslateEn.Checked
        _config.NoTimestamps = chkNoTimestamps.Checked
        _config.PrintProgress = chkPrintProgress.Checked
        _config.PrintColours = chkPrintColours.Checked

        ' Advanced — Bible
        _config.ShowBibleCopyright = chkShowBibleCopyright.Checked

        ' Advanced — Live transcription
        If cboComputeType.SelectedItem IsNot Nothing Then _config.LiveComputeType = cboComputeType.SelectedItem.ToString()
        _config.LiveVadSilenceMs = CInt(nudLiveVadSilence.Value)
        _config.LiveMaxSegmentSec = CInt(nudLiveMaxSeg.Value)
        _config.LiveInterimIntervalMs = CInt(nudLiveInterim.Value)

        ' Advanced — Live Pipeline concurrency
        _config.TranslationConcurrency = CInt(nudTranslationConcurrency.Value)
        _config.TtsConcurrency = CInt(nudTtsConcurrency.Value)

        AppLogger.Log(LogEvents.CONFIG_SAVED, $"ApplyToConfig: Language={_config.Language}, OutputLanguage={_config.OutputLanguage}, BiblesDirectory={_config.BiblesDirectory}, Theme={_config.Theme}, UiLanguage={_config.UiLanguage}, TranslationEnabled={_config.TranslationEnabled}")
        ConfigManager.Save(_config)
        ConfigChanged = True
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Hardware scan
    ' ═══════════════════════════════════════════════════════════════
    Private Sub RunHardwareScan()
        btnHwRescan.Enabled = False
        lblHwVerdict.Text = LanguagePackService.Instance.GetString("Opt_HwScanning")
        Me.Cursor = Cursors.WaitCursor

        Threading.ThreadPool.QueueUserWorkItem(
            Sub()
                Dim info = HardwareScanner.Scan()
                Me.BeginInvoke(Sub() DisplayHardwareInfo(info))
            End Sub)
    End Sub

    Private Sub DisplayHardwareInfo(info As HardwareInfo)
        _hwInfo = info
        Me.Cursor = Cursors.Default
        btnHwRescan.Enabled = True

        ' Overall score
        lblHwOverallScore.Text = $"{info.OverallScore}/100"

        ' Traffic light indicator
        Select Case info.Rating
            Case "Green"
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(0, 180, 0)
            Case "Amber"
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(255, 180, 0)
            Case Else
                pnlHwIndicator.BackColor = Drawing.Color.FromArgb(220, 40, 40)
        End Select

        Dim lp = LanguagePackService.Instance
        lblHwVerdict.Text = info.GetRatingDescription(lp)

        ' Component breakdown
        Dim gpuVram = If(info.GpuMemoryMB > 0, String.Format(lp.GetString("Opt_HwVram"), info.GpuMemoryMB \ 1024), lp.GetString("Opt_HwVramNa"))
        lblHwGpu.Text = String.Format(lp.GetString("Opt_HwGpu"), info.GpuScore, info.GpuName, gpuVram)
        Dim clockGHz = (info.CpuClockMHz / 1000.0).ToString("F1")
        lblHwCpu.Text = String.Format(lp.GetString("Opt_HwCpu"), info.CpuScore, info.CpuName, info.CpuCores, clockGHz)
        lblHwRam.Text = String.Format(lp.GetString("Opt_HwRam"), info.RamScore, info.RamTotalMB \ 1024)
        Dim diskGB = (info.DiskFreeMB / 1024.0).ToString("F1")
        lblHwDisk.Text = String.Format(lp.GetString("Opt_HwDisk"), info.DiskScore, diskGB)
        lblHwOs.Text = String.Format(lp.GetString("Opt_HwOs"), info.OsScore, info.OsDescription)

        ' Recommendations
        Dim recs = info.GetRecommendations(lp)
        If recs.Count = 0 Then
            txtHwRecs.Text = lp.GetString("Opt_HwNoRecs")
        Else
            txtHwRecs.Text = String.Join(Environment.NewLine & Environment.NewLine, recs)
        End If

        ' Auto-select best STT backend based on hardware
        Dim suggested = HardwareScanner.SuggestSttBackend(info)
        SelectSttBackend(suggested)
        AppLogger.Log(LogEvents.HW_SCAN_RESULT, $"Suggested STT backend: {suggested}")

        AppLogger.Log(LogEvents.HW_SCAN_RESULT, $"Scan complete: Overall={info.OverallScore}, GPU={info.GpuScore}, CPU={info.CpuScore}, RAM={info.RamScore}, Disk={info.DiskScore}, OS={info.OsScore}, Rating={info.Rating}")
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' STT backend combo — driven by SttBackendRegistry
    ' ═══════════════════════════════════════════════════════════════
    Private _sttKeys As String()
    ' Engine-specific option value maps (parallel to combo item order).
    Private ReadOnly _opKeys As String() = {"enhanced", "standard"}
    Private ReadOnly _regionKeys As String() = {"eu2", "us"}
    ' Which backend's API key is currently shown in txtSttApiKey.
    Private _currentApiKeyBackend As String = ""

    Private Sub PopulateSttBackendCombo()
        Dim entries = Services.Stt.SttBackendRegistry.GetAll()
        _sttKeys = New String(entries.Count - 1) {}
        cboSttBackend.Items.Clear()
        For i = 0 To entries.Count - 1
            _sttKeys(i) = entries(i).Key
            cboSttBackend.Items.Add(entries(i).DisplayName)
        Next
        AddHandler cboSttBackend.SelectedIndexChanged, AddressOf SttBackendCombo_Changed
    End Sub

    Private Sub PopulateSpeechmaticsCombos()
        Dim lp = LanguagePackService.Instance
        cboSttOperatingPoint.Items.Clear()
        cboSttOperatingPoint.Items.Add(lp.GetString("Opt_SttOpEnhanced"))
        cboSttOperatingPoint.Items.Add(lp.GetString("Opt_SttOpStandard"))
        cboSttRegion.Items.Clear()
        cboSttRegion.Items.Add(lp.GetString("Opt_SttRegionEu"))
        cboSttRegion.Items.Add(lp.GetString("Opt_SttRegionUs"))
    End Sub

    ''' <summary>Currently-selected backend key, or "" if none.</summary>
    Private Function CurrentSttKey() As String
        If _sttKeys IsNot Nothing AndAlso cboSttBackend.SelectedIndex >= 0 AndAlso
           cboSttBackend.SelectedIndex < _sttKeys.Length Then
            Return _sttKeys(cboSttBackend.SelectedIndex)
        End If
        Return ""
    End Function

    Private Shared Sub SelectComboByValue(combo As ComboBox, values As String(), value As String)
        Dim idx = Array.IndexOf(values, If(value, "").ToLowerInvariant())
        combo.SelectedIndex = If(idx >= 0, idx, 0)
    End Sub

    Private Sub SelectSttBackend(key As String)
        Dim idx = Array.IndexOf(_sttKeys, If(key, "whisper-cpp-vulkan"))
        cboSttBackend.SelectedIndex = If(idx >= 0, idx, 0)
    End Sub

    Private Sub SttBackendCombo_Changed(sender As Object, e As EventArgs)
        ' Persist the key shown for the previously-selected backend, then load the
        ' new backend's stored key so each engine keeps its own credential.
        If Not String.IsNullOrEmpty(_currentApiKeyBackend) Then
            _config.SetSttApiKey(_currentApiKeyBackend, txtSttApiKey.Text.Trim())
        End If
        _currentApiKeyBackend = CurrentSttKey()
        txtSttApiKey.Text = _config.GetSttApiKey(_currentApiKeyBackend)
        UpdateSttApiKeyVisibility()
    End Sub

    Private Sub UpdateSttApiKeyVisibility()
        Dim entries = Services.Stt.SttBackendRegistry.GetAll()
        Dim showApiKey = False
        If cboSttBackend.SelectedIndex >= 0 AndAlso cboSttBackend.SelectedIndex < entries.Count Then
            showApiKey = entries(cboSttBackend.SelectedIndex).RequiresApiKey
        End If
        lblSttApiKey.Visible = showApiKey
        txtSttApiKey.Visible = showApiKey

        ' Speechmatics-specific tuning (operating point + region) only applies to
        ' the Speechmatics backend.
        Dim showSpeechmatics = String.Equals(CurrentSttKey(), "speechmatics", StringComparison.OrdinalIgnoreCase)
        lblSttOperatingPoint.Visible = showSpeechmatics
        cboSttOperatingPoint.Visible = showSpeechmatics
        lblSttRegion.Visible = showSpeechmatics
        cboSttRegion.Visible = showSpeechmatics
        lblSttEouSilence.Visible = showSpeechmatics
        nudSttEouSilence.Visible = showSpeechmatics
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Translation backend combo — driven by TranslationBackendRegistry
    ' ═══════════════════════════════════════════════════════════════
    Private _transKeys As String()
    Private _transEntries As IReadOnlyList(Of Services.Translation.TranslationBackendRegistry.Entry)

    Private Sub PopulateTransBackendCombo()
        _transEntries = Services.Translation.TranslationBackendRegistry.GetAll()
        _transKeys = New String(_transEntries.Count - 1) {}
        cboTransBackend.Items.Clear()
        For i = 0 To _transEntries.Count - 1
            _transKeys(i) = _transEntries(i).Key
            cboTransBackend.Items.Add(_transEntries(i).DisplayName)
        Next

        AddHandler cboTransBackend.SelectedIndexChanged, AddressOf TransBackendCombo_Changed
    End Sub

    ''' <summary>
    ''' Select the backend combo using the backend key (e.g. "nllb", "nllb-3.3b").
    ''' </summary>
    Private Sub SelectTransBackend(key As String)
        Dim idx = Array.IndexOf(_transKeys, If(key, "nllb"))
        If idx < 0 Then
            ' Fallback: try matching by model type for older configs
            Dim modelType = If(_config.TranslationModelType, "nllb")
            idx = Array.IndexOf(_transKeys, modelType)
        End If
        cboTransBackend.SelectedIndex = If(idx >= 0, idx, 0)
    End Sub

    ''' <summary>
    ''' When the user switches translation engine, auto-update the model path
    ''' if it currently matches the previous engine's default path.
    ''' </summary>
    Private Sub TransBackendCombo_Changed(sender As Object, e As EventArgs)
        If cboTransBackend.SelectedIndex < 0 OrElse cboTransBackend.SelectedIndex >= _transEntries.Count Then Return
        Dim entry = _transEntries(cboTransBackend.SelectedIndex)
        If String.IsNullOrEmpty(entry.DefaultModelPath) Then Return

        ' Collect all known default paths from the registry (both relative and resolved)
        Dim defaultPaths As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each en In _transEntries
            If Not String.IsNullOrEmpty(en.DefaultModelPath) Then
                defaultPaths.Add(en.DefaultModelPath)
                defaultPaths.Add(Models.AppConfig.ResolvePath(en.DefaultModelPath))
            End If
        Next

        ' If current model path is one of the known defaults (or empty), swap to the new default
        Dim currentPath = txtTransModel.Text.Trim()
        If String.IsNullOrEmpty(currentPath) OrElse
           defaultPaths.Contains(currentPath) OrElse
           defaultPaths.Contains(Models.AppConfig.ResolvePath(currentPath)) Then
            txtTransModel.Text = entry.DefaultModelPath
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' TTS preference combos — driven by TtsBackendRegistry
    ' ═══════════════════════════════════════════════════════════════
    Private _ttsDisplayNames As String()
    Private _ttsKeys As String()

    Private Sub PopulateTtsCombos()
        Dim entries = Services.Tts.TtsBackendRegistry.GetAll()
        _ttsDisplayNames = New String(entries.Count) {}
        _ttsKeys = New String(entries.Count) {}
        _ttsDisplayNames(0) = "(none)"
        _ttsKeys(0) = ""
        For i = 0 To entries.Count - 1
            _ttsDisplayNames(i + 1) = entries(i).DisplayName
            _ttsKeys(i + 1) = entries(i).Key
        Next

        For Each cbo In {cboTtsPref1, cboTtsPref2, cboTtsPref3}
            cbo.Items.Clear()
            cbo.Items.AddRange(_ttsDisplayNames)
        Next
    End Sub

    Private Sub LoadTtsPreferences(backendsStr As String)
        Dim backends As String() = {}
        If Not String.IsNullOrWhiteSpace(backendsStr) Then
            backends = backendsStr.Split(","c).
                Select(Function(s) s.Trim().ToLower()).
                Where(Function(s) s.Length > 0).ToArray()
        End If

        ' Default: all backends in registry order
        If backends.Length = 0 Then
            backends = _ttsKeys.Where(Function(k) k.Length > 0).ToArray()
        End If

        Dim combos = {cboTtsPref1, cboTtsPref2, cboTtsPref3}
        For i = 0 To 2
            If i < backends.Length Then
                Dim idx = Array.IndexOf(_ttsKeys, backends(i))
                combos(i).SelectedIndex = If(idx >= 0, idx, 0)
            Else
                combos(i).SelectedIndex = 0
            End If
        Next
    End Sub

    Private Function BuildTtsBackendsString() As String
        Dim result As New List(Of String)
        For Each cbo In {cboTtsPref1, cboTtsPref2, cboTtsPref3}
            If cbo.SelectedIndex > 0 Then
                Dim key = _ttsKeys(cbo.SelectedIndex)
                If Not result.Contains(key) Then result.Add(key)
            End If
        Next
        Return String.Join(",", result)
    End Function

    ' ═══════════════════════════════════════════════════════════════
    ' Helpers
    ' ═══════════════════════════════════════════════════════════════
    Private Shared Sub BrowseFile(txt As TextBox)
        Using dlg As New OpenFileDialog()
            dlg.FileName = txt.Text
            If dlg.ShowDialog() = DialogResult.OK Then
                txt.Text = dlg.FileName
            End If
        End Using
    End Sub

    Private Shared Sub BrowseFolder(txt As TextBox)
        Using dlg As New FolderBrowserDialog()
            dlg.SelectedPath = AppConfig.ResolvePath(txt.Text)
            If dlg.ShowDialog() = DialogResult.OK Then
                txt.Text = dlg.SelectedPath
            End If
        End Using
    End Sub

    Private Shared Sub PickColor(btn As Button)
        Using dlg As New ColorDialog()
            dlg.Color = btn.BackColor
            If dlg.ShowDialog() = DialogResult.OK Then
                btn.BackColor = dlg.Color
            End If
        End Using
    End Sub

    ''' <summary>Clamp an integer config value into a NumericUpDown's valid range before assigning.</summary>
    Private Shared Function ClampNud(nud As NumericUpDown, value As Integer) As Decimal
        Return Math.Max(nud.Minimum, Math.Min(nud.Maximum, CDec(value)))
    End Function

    Private Shared Sub SelectItem(cbo As ComboBox, value As String)
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().Equals(value, StringComparison.OrdinalIgnoreCase) Then
                cbo.SelectedIndex = i
                Return
            End If
        Next
        If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
    End Sub
End Class
