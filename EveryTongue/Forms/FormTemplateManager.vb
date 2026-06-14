Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Stt
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Translation

Public Class FormTemplateManager

    Private ReadOnly _config As AppConfig
    Private _editingTemplate As ConferenceTemplate
    Private _isNewTemplate As Boolean
    Private _deviceList As New List(Of AudioDeviceInfo)

    Public Sub New(config As AppConfig)
        _config = config
        InitializeComponent()
        ApplyLocale()
        PopulateEngineDropdowns()
        PopulateLanguageDropdown()
        PopulateAudioDevices()
        RefreshList()
    End Sub

    Private Sub ApplyLocale()
        Dim lp = LanguagePackService.Instance
        Me.Text = lp.GetString("Tmpl_Title")
        btnAdd.Text = lp.GetString("Tmpl_Add")
        btnEdit.Text = lp.GetString("Tmpl_Edit")
        btnDelete.Text = lp.GetString("Tmpl_Delete")
        lblName.Text = lp.GetString("Tmpl_Name")
        lblHostingCode.Text = lp.GetString("Tmpl_HostingCode")
        lblSourceLang.Text = lp.GetString("Tmpl_SourceLang")
        lblSttEngine.Text = lp.GetString("Tmpl_SttEngine")
        lblTransEngine.Text = lp.GetString("Tmpl_TransEngine")
        lblVisibility.Text = lp.GetString("Tmpl_Visibility")
        lblAudioDevice.Text = lp.GetString("Tmpl_AudioDevice")
        lblModelPath.Text = lp.GetString("Tmpl_ModelPath")
        lblSttTemplate.Text = lp.GetString("Tmpl_SttTemplate")
        btnManageSttTemplates.Text = lp.GetString("Tmpl_ManageSttTemplates")
        lblMode.Text = lp.GetString("Tmpl_Mode")
        lblSpeakers.Text = lp.GetString("Tmpl_Speakers")
        btnManageSpeakers.Text = lp.GetString("Tmpl_ManageSttTemplates")
        lblDisplayTpl.Text = lp.GetString("Tmpl_DisplayTemplate")
        btnManageDisplay.Text = lp.GetString("Tmpl_ManageSttTemplates")
        lblFilterSet.Text = lp.GetString("Tmpl_FilterSet")
        btnManageFilterSets.Text = lp.GetString("Tmpl_ManageSttTemplates")
        cboMode.Items.Clear()
        cboMode.Items.Add(lp.GetString("Tmpl_ModeOnline"))
        cboMode.Items.Add(lp.GetString("Tmpl_ModeOffline"))
        cboMode.SelectedIndex = 0
        colName.Text = lp.GetString("Tmpl_Name")
        colHostingCode.Text = lp.GetString("Tmpl_HostingCode")
        colLanguage.Text = lp.GetString("Tmpl_SourceLang")
        colSttEngine.Text = lp.GetString("Tmpl_SttEngine")
    End Sub

    Private Sub PopulateEngineDropdowns()
        ' STT engines from registry
        cboSttEngine.Items.Clear()
        For Each entry In SttBackendRegistry.GetAll()
            cboSttEngine.Items.Add(entry.Key & " — " & entry.DisplayName)
        Next
        If cboSttEngine.Items.Count > 0 Then cboSttEngine.SelectedIndex = 0

        ' Translation engines from registry
        cboTransEngine.Items.Clear()
        For Each entry In TranslationBackendRegistry.GetAll()
            cboTransEngine.Items.Add(entry.Key & " — " & entry.DisplayName)
        Next
        If cboTransEngine.Items.Count > 0 Then cboTransEngine.SelectedIndex = 0
    End Sub

    Private Sub PopulateLanguageDropdown()
        cboSourceLang.Items.Clear()
        cboSourceLang.Items.Add("auto")
        Dim langs() As String = {"ca", "es", "en", "fr", "de", "it", "pt", "nl", "ru", "zh", "ja", "ko", "ar", "hi", "tr", "pl", "uk", "vi", "th", "id", "ms", "tl", "sw"}
        For Each lang In langs
            cboSourceLang.Items.Add(lang)
        Next
        cboSourceLang.SelectedIndex = 0

        cboVisibility.SelectedIndex = 0
    End Sub

    Private Sub PopulateAudioDevices()
        Dim previousId As Integer = -1
        If cboAudioDevice.SelectedItem IsNot Nothing Then
            Dim sel = TryCast(cboAudioDevice.SelectedItem, AudioDeviceInfo)
            If sel IsNot Nothing Then previousId = sel.Id
        End If

        cboAudioDevice.Items.Clear()
        _deviceList.Clear()

        ' Add "Default" entry
        Dim defaultDev As New AudioDeviceInfo(-1, LanguagePackService.Instance.GetString("Live_DefaultDevice"))
        _deviceList.Add(defaultDev)
        cboAudioDevice.Items.Add(defaultDev)

        cboAudioDevice.Enabled = False
        btnRefreshDevices.Enabled = False

        Dim pythonPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")

        Threading.Tasks.Task.Run(Sub()
                                     Try
                                         Dim backend = Services.Stt.SttBackendRegistry.CreateBackend()
                                         Dim devices = backend.EnumerateDevicesAsync(pythonPath)
                                         Me.BeginInvoke(Sub()
                                                            For Each d In devices
                                                                _deviceList.Add(d)
                                                                cboAudioDevice.Items.Add(d)
                                                            Next
                                                            ' Restore previous selection
                                                            Dim found = False
                                                            For i = 0 To cboAudioDevice.Items.Count - 1
                                                                Dim dev = TryCast(cboAudioDevice.Items(i), AudioDeviceInfo)
                                                                If dev IsNot Nothing AndAlso dev.Id = previousId Then
                                                                    cboAudioDevice.SelectedIndex = i
                                                                    found = True
                                                                    Exit For
                                                                End If
                                                            Next
                                                            If Not found Then cboAudioDevice.SelectedIndex = 0
                                                            cboAudioDevice.Enabled = True
                                                            btnRefreshDevices.Enabled = True
                                                        End Sub)
                                     Catch ex As Exception
                                         Me.BeginInvoke(Sub()
                                                            cboAudioDevice.SelectedIndex = 0
                                                            cboAudioDevice.Enabled = True
                                                            btnRefreshDevices.Enabled = True
                                                        End Sub)
                                     End Try
                                 End Sub)
    End Sub

    Private Sub RefreshList()
        lvTemplates.Items.Clear()
        For Each t In _config.ConferenceTemplates
            Dim li As New ListViewItem(t.Name)
            li.SubItems.Add(t.HostingCode)
            li.SubItems.Add(t.SourceLanguage)
            li.SubItems.Add(t.SttBackendKey)
            li.Tag = t.Id
            lvTemplates.Items.Add(li)
        Next
        UpdateButtonState()
    End Sub

    Private Sub UpdateButtonState()
        Dim hasSelection = lvTemplates.SelectedItems.Count > 0
        btnEdit.Enabled = hasSelection
        btnDelete.Enabled = hasSelection
    End Sub

    Private Sub lvTemplates_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lvTemplates.SelectedIndexChanged
        UpdateButtonState()
    End Sub

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        _isNewTemplate = True
        _editingTemplate = New ConferenceTemplate()
        LoadTemplateToDetail(_editingTemplate)
        ShowDetail(True)
    End Sub

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)
        _editingTemplate = _config.ConferenceTemplates.FirstOrDefault(Function(t) t.Id = id)
        If _editingTemplate Is Nothing Then Return
        _isNewTemplate = False
        LoadTemplateToDetail(_editingTemplate)
        ShowDetail(True)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim name = lvTemplates.SelectedItems(0).Text
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)
        Dim lp = LanguagePackService.Instance
        Dim msg = String.Format(lp.GetString("Tmpl_DeleteConfirm"), name)
        If MessageBox.Show(msg, lp.GetString("Tmpl_Delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        _config.ConferenceTemplates.RemoveAll(Function(t) t.Id = id)
        Services.Config.TemplateLibraryStore.Instance.DeleteEngineTemplate(
            Services.Config.TemplateLibraryStore.GroupStt, id)
        SaveAndSync()
        RefreshList()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If _editingTemplate Is Nothing Then Return
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show("Name is required.", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If
        If String.IsNullOrWhiteSpace(txtHostingCode.Text) Then
            MessageBox.Show("Hosting code is required.", Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtHostingCode.Focus()
            Return
        End If

        SaveDetailToTemplate(_editingTemplate)

        If _isNewTemplate Then
            _config.ConferenceTemplates.Add(_editingTemplate)
        End If

        Dim sharedId = SelectedSttTemplateId()
        If sharedId = "" Then
            ' Own settings: write through to the 1:1 STT library template (same
            ' Id) — the live pipeline resolves engine knobs from there.
            _editingTemplate.SttTemplateId = _editingTemplate.Id
            Services.Config.TemplateLibraryStore.Instance.UpsertEngineTemplate(
                Services.Config.TemplateLibraryStore.GroupStt,
                Services.Config.ConferenceTemplateMigration.BuildSttTemplate(_editingTemplate, _config.SttBackend))
        Else
            ' Shared library template: reference it, never overwrite it with
            ' this conference template's embedded knobs.
            _editingTemplate.SttTemplateId = sharedId
            Dim sharedTpl = Services.Config.TemplateLibraryStore.Instance.GetEngineTemplate(
                Services.Config.TemplateLibraryStore.GroupStt, sharedId)
            If sharedTpl IsNot Nothing AndAlso Not String.IsNullOrEmpty(sharedTpl.EngineKey) Then
                _editingTemplate.SttBackendKey = sharedTpl.EngineKey
            End If
        End If

        SaveAndSync()
        ShowDetail(False)
        RefreshList()
    End Sub

    Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
        ShowDetail(False)
    End Sub

    Private Sub btnRefreshDevices_Click(sender As Object, e As EventArgs) Handles btnRefreshDevices.Click
        PopulateAudioDevices()
    End Sub

    Private Sub cboSttEngine_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSttEngine.SelectedIndexChanged
        PopulateModelDropdown()
    End Sub

    Private Sub PopulateModelDropdown(Optional selectPath As String = "")
        Dim previousPath = selectPath
        If String.IsNullOrEmpty(previousPath) AndAlso cboModel.SelectedItem IsNot Nothing Then
            Dim sel = TryCast(cboModel.SelectedItem, ModelItem)
            If sel IsNot Nothing Then previousPath = sel.Path
        End If

        cboModel.Items.Clear()

        Dim sttKey = ExtractEngineKey(cboSttEngine)
        Dim regEntry = SttBackendRegistry.Find(sttKey)
        Dim scanPattern = If(regEntry?.ModelScanPattern, "")
        Dim baseDir = AppDomain.CurrentDomain.BaseDirectory

        ' Add "(Default)" option — uses global config path for the selected backend
        Dim defaultLabel = LanguagePackService.Instance.GetString("Tmpl_ModelDefault")
        If String.IsNullOrEmpty(defaultLabel) Then defaultLabel = "(Default)"
        cboModel.Items.Add(New ModelItem(defaultLabel, ""))

        If scanPattern = "-" Then
            ' Online engine — no local model selection; "(Default)" only
        ElseIf Not String.IsNullOrEmpty(scanPattern) Then
            ' Scan for model FILES matching the engine's declared pattern (e.g. "*.bin")
            Try
                Dim minBytes As Long = CLng(If(regEntry?.ModelMinSizeMB, 0)) * 1024L * 1024L
                For Each f In IO.Directory.GetFiles(baseDir, scanPattern)
                    ' Skip tiny files that aren't models
                    If New IO.FileInfo(f).Length > minBytes Then
                        cboModel.Items.Add(New ModelItem(IO.Path.GetFileName(f), f))
                    End If
                Next
            Catch ex As Exception
                AppLogger.Log(LogEvents.UI_ERROR, $"TemplateManager: error scanning model files: {ex.Message}")
            End Try
        Else
            ' Scan for model DIRECTORIES (contain config.json)
            Try
                For Each d In IO.Directory.GetDirectories(baseDir)
                    If IO.File.Exists(IO.Path.Combine(d, "config.json")) Then
                        cboModel.Items.Add(New ModelItem(IO.Path.GetFileName(d), d))
                    End If
                Next
            Catch ex As Exception
                AppLogger.Log(LogEvents.UI_ERROR, $"TemplateManager: error scanning model directories: {ex.Message}")
            End Try
        End If

        ' Select previous or default
        Dim found = False
        If Not String.IsNullOrEmpty(previousPath) Then
            For i = 0 To cboModel.Items.Count - 1
                Dim item = TryCast(cboModel.Items(i), ModelItem)
                If item IsNot Nothing AndAlso item.Path.Equals(previousPath, StringComparison.OrdinalIgnoreCase) Then
                    cboModel.SelectedIndex = i
                    found = True
                    Exit For
                End If
            Next
        End If
        If Not found Then cboModel.SelectedIndex = 0
    End Sub

    Private Sub ShowDetail(visible As Boolean)
        grpDetail.Visible = visible
        lvTemplates.Enabled = Not visible
        btnAdd.Enabled = Not visible
        btnEdit.Enabled = Not visible
        btnDelete.Enabled = Not visible
        btnClose.Enabled = Not visible
        If visible Then txtName.Focus()
    End Sub

    Private Sub LoadTemplateToDetail(t As ConferenceTemplate)
        txtName.Text = t.Name
        txtHostingCode.Text = t.HostingCode

        ' Source language
        Dim langIdx = cboSourceLang.Items.IndexOf(t.SourceLanguage)
        cboSourceLang.SelectedIndex = If(langIdx >= 0, langIdx, 0)

        ' STT engine
        SelectEngineCombo(cboSttEngine, t.SttBackendKey)

        ' Translation engine
        SelectEngineCombo(cboTransEngine, t.TranslationBackendKey)

        ' Audio device — prefer matching by NAME (PortAudio indices drift), else by ID
        Dim deviceFound = False
        If Not String.IsNullOrEmpty(t.AudioDeviceName) Then
            For i = 0 To cboAudioDevice.Items.Count - 1
                Dim dev = TryCast(cboAudioDevice.Items(i), AudioDeviceInfo)
                If dev IsNot Nothing AndAlso
                   String.Equals(dev.Name?.Trim(), t.AudioDeviceName.Trim(), StringComparison.OrdinalIgnoreCase) Then
                    cboAudioDevice.SelectedIndex = i
                    deviceFound = True
                    Exit For
                End If
            Next
        End If
        If Not deviceFound Then
            For i = 0 To cboAudioDevice.Items.Count - 1
                Dim dev = TryCast(cboAudioDevice.Items(i), AudioDeviceInfo)
                If dev IsNot Nothing AndAlso dev.Id = t.AudioDeviceId Then
                    cboAudioDevice.SelectedIndex = i
                    deviceFound = True
                    Exit For
                End If
            Next
        End If
        If Not deviceFound AndAlso cboAudioDevice.Items.Count > 0 Then cboAudioDevice.SelectedIndex = 0

        PopulateModelDropdown(t.ModelPath)

        Dim visIdx = cboVisibility.Items.IndexOf(t.DefaultVisibility)
        cboVisibility.SelectedIndex = If(visIdx >= 0, visIdx, 0)

        PopulateSttTemplateCombo(t)

        cboMode.SelectedIndex = If(t.Mode = Models.Templates.ConnectivityMode.Offline, 1, 0)
        PopulateSpeakerChecklist(t)
        PopulateDisplayCombo(t)
        PopulateFilterSetCombo(t)
    End Sub

    ''' <summary>"(global)" + the named filter sets.</summary>
    Private Sub PopulateFilterSetCombo(t As ConferenceTemplate)
        cboFilterSet.Items.Clear()
        cboFilterSet.Items.Add(New SttTemplateItem(
            LanguagePackService.Instance.GetString("Spk_GlossaryGlobal"), ""))
        Dim found = 0
        For Each fs In Services.Config.TemplateLibraryStore.Instance.GetFilterSets()
            cboFilterSet.Items.Add(New SttTemplateItem(fs.Name, fs.Id))
            If fs.Id = If(t.FilterSetId, "") Then found = cboFilterSet.Items.Count - 1
        Next
        cboFilterSet.SelectedIndex = found
    End Sub

    Private Sub btnManageFilterSets_Click(sender As Object, e As EventArgs) Handles btnManageFilterSets.Click
        Using frm As New FormFilterSets()
            frm.Icon = Me.Icon
            frm.ShowDialog(Me)
        End Using
        If _editingTemplate IsNot Nothing Then PopulateFilterSetCombo(_editingTemplate)
    End Sub

    ''' <summary>"(app default)" + the Display template library.</summary>
    Private Sub PopulateDisplayCombo(t As ConferenceTemplate)
        cboDisplayTpl.Items.Clear()
        cboDisplayTpl.Items.Add(New SttTemplateItem(
            LanguagePackService.Instance.GetString("Tmpl_DisplayDefault"), ""))
        Dim found = 0
        For Each d In Services.Config.TemplateLibraryStore.Instance.GetDisplayTemplates()
            cboDisplayTpl.Items.Add(New SttTemplateItem(d.Name, d.Id))
            If d.Id = If(t.DisplayTemplateId, "") Then found = cboDisplayTpl.Items.Count - 1
        Next
        cboDisplayTpl.SelectedIndex = found
    End Sub

    Private Sub btnManageDisplay_Click(sender As Object, e As EventArgs) Handles btnManageDisplay.Click
        Using frm As New FormDisplayTemplates()
            frm.Icon = Me.Icon
            frm.ShowDialog(Me)
        End Using
        If _editingTemplate IsNot Nothing Then PopulateDisplayCombo(_editingTemplate)
    End Sub

    ''' <summary>All speaker profiles, checked when referenced by this template.</summary>
    Private Sub PopulateSpeakerChecklist(t As ConferenceTemplate)
        clbSpeakers.Items.Clear()
        Dim refs = If(t.SpeakerProfileIds, New List(Of String))
        For Each sp In Services.Config.TemplateLibraryStore.Instance.GetSpeakerProfiles()
            Dim idx = clbSpeakers.Items.Add(New SttTemplateItem(sp.Name, sp.Id))
            clbSpeakers.SetItemChecked(idx, refs.Contains(sp.Id))
        Next
    End Sub

    Private Sub btnManageSpeakers_Click(sender As Object, e As EventArgs) Handles btnManageSpeakers.Click
        Using frm As New FormSpeakerProfiles()
            frm.Icon = Me.Icon
            frm.ShowDialog(Me)
        End Using
        If _editingTemplate IsNot Nothing Then PopulateSpeakerChecklist(_editingTemplate)
    End Sub

    ''' <summary>
    ''' "(this template's own settings)" + the shared STT library templates.
    ''' The 1:1 template that mirrors this conference template (same Id) IS the
    ''' own-settings storage, so it is excluded from the shared list.
    ''' </summary>
    Private Sub PopulateSttTemplateCombo(t As ConferenceTemplate)
        cboSttTemplate.Items.Clear()
        cboSttTemplate.Items.Add(New SttTemplateItem(
            LanguagePackService.Instance.GetString("Tmpl_SttTemplateOwn"), ""))
        For Each tpl In Services.Config.TemplateLibraryStore.Instance.GetEngineTemplates(
                Services.Config.TemplateLibraryStore.GroupStt)
            If tpl.Id = t.Id Then Continue For
            cboSttTemplate.Items.Add(New SttTemplateItem($"{tpl.Name} [{tpl.EngineKey}]", tpl.Id))
        Next

        Dim selectId = If(t.SttTemplateId, "")
        If selectId = t.Id Then selectId = "" ' own 1:1 template = own settings
        Dim found = 0
        For i = 1 To cboSttTemplate.Items.Count - 1
            If DirectCast(cboSttTemplate.Items(i), SttTemplateItem).Id = selectId Then
                found = i
                Exit For
            End If
        Next
        cboSttTemplate.SelectedIndex = found
        UpdateKnobEnabledState()
    End Sub

    ''' <summary>The embedded engine knobs only apply when no shared STT template is referenced.</summary>
    Private Sub UpdateKnobEnabledState()
        Dim ownSettings = SelectedSttTemplateId() = ""
        cboSttEngine.Enabled = ownSettings
        cboModel.Enabled = ownSettings
    End Sub

    Private Function SelectedSttTemplateId() As String
        Dim item = TryCast(cboSttTemplate.SelectedItem, SttTemplateItem)
        Return If(item?.Id, "")
    End Function

    Private Sub cboSttTemplate_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboSttTemplate.SelectedIndexChanged
        UpdateKnobEnabledState()
    End Sub

    Private Sub btnManageSttTemplates_Click(sender As Object, e As EventArgs) Handles btnManageSttTemplates.Click
        Using frm As New FormEngineTemplates(_config)
            frm.Icon = Me.Icon
            frm.ShowDialog(Me)
        End Using
        If _editingTemplate IsNot Nothing Then PopulateSttTemplateCombo(_editingTemplate)
    End Sub

    ''' <summary>Item for the STT template dropdown — displays name, stores library id ("" = own settings).</summary>
    Private Class SttTemplateItem
        Public ReadOnly Property DisplayName As String
        Public ReadOnly Property Id As String

        Public Sub New(displayName As String, id As String)
            Me.DisplayName = displayName
            Me.Id = If(id, "")
        End Sub

        Public Overrides Function ToString() As String
            Return DisplayName
        End Function
    End Class

    Private Sub SaveDetailToTemplate(t As ConferenceTemplate)
        t.Name = txtName.Text.Trim()
        t.HostingCode = txtHostingCode.Text.Trim()
        t.SourceLanguage = If(cboSourceLang.SelectedItem IsNot Nothing, cboSourceLang.SelectedItem.ToString(), "auto")
        t.SttBackendKey = ExtractEngineKey(cboSttEngine)
        t.TranslationBackendKey = ExtractEngineKey(cboTransEngine)
        ' Audio device from combo — store BOTH id and name (name survives PortAudio index drift)
        Dim selDev = TryCast(cboAudioDevice.SelectedItem, AudioDeviceInfo)
        If selDev IsNot Nothing Then
            t.AudioDeviceId = selDev.Id
            t.AudioDeviceName = If(selDev.Id >= 0, If(selDev.Name, ""), "")
            t.AudioSourceLabel = selDev.Name
        Else
            t.AudioDeviceId = -1
            t.AudioDeviceName = ""
            t.AudioSourceLabel = ""
        End If
        Dim selModel = TryCast(cboModel.SelectedItem, ModelItem)
        t.ModelPath = If(selModel IsNot Nothing, selModel.Path, "")
        t.DefaultVisibility = If(cboVisibility.SelectedItem IsNot Nothing, cboVisibility.SelectedItem.ToString(), "public")

        t.Mode = If(cboMode.SelectedIndex = 1,
            Models.Templates.ConnectivityMode.Offline, Models.Templates.ConnectivityMode.Online)
        t.SpeakerProfileIds = New List(Of String)
        For Each idx In clbSpeakers.CheckedIndices.Cast(Of Integer)()
            Dim item = TryCast(clbSpeakers.Items(idx), SttTemplateItem)
            If item IsNot Nothing Then t.SpeakerProfileIds.Add(item.Id)
        Next
        Dim dispItem = TryCast(cboDisplayTpl.SelectedItem, SttTemplateItem)
        t.DisplayTemplateId = If(dispItem?.Id, "")
        Dim fsItem = TryCast(cboFilterSet.SelectedItem, SttTemplateItem)
        t.FilterSetId = If(fsItem?.Id, "")
    End Sub

    Private Sub SaveAndSync()
        ConfigManager.Save(_config)
        ' Sync to TemplateStore if the server is running
        Try
            Dim store = Services.Rooms.TemplateStore.Instance
            If store IsNot Nothing Then store.SyncFromConfig(_config.ConferenceTemplates)
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"TemplateManager: TemplateStore sync failed: {ex.Message}")
        End Try
        Me.DialogResult = DialogResult.OK
    End Sub

    Private Shared Sub SelectEngineCombo(cbo As ComboBox, key As String)
        For i = 0 To cbo.Items.Count - 1
            If cbo.Items(i).ToString().StartsWith(key & " ") Then
                cbo.SelectedIndex = i
                Return
            End If
        Next
        If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
    End Sub

    Private Shared Function ExtractEngineKey(cbo As ComboBox) As String
        If cbo.SelectedItem Is Nothing Then Return ""
        Dim txt = cbo.SelectedItem.ToString()
        Dim dashIdx = txt.IndexOf(" — ")
        If dashIdx > 0 Then Return txt.Substring(0, dashIdx)
        Return txt
    End Function

    ''' <summary>Item for the model dropdown — displays name, stores full path.</summary>
    Private Class ModelItem
        Public ReadOnly Property DisplayName As String
        Public ReadOnly Property Path As String

        Public Sub New(displayName As String, path As String)
            Me.DisplayName = displayName
            Me.Path = If(path, "")
        End Sub

        Public Overrides Function ToString() As String
            Return DisplayName
        End Function
    End Class

End Class
