Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Stt
Imports EveryTongue.Services.Translation

Public Class FormTemplateManager

    Private ReadOnly _config As AppConfig
    Private _editingTemplate As ConferenceTemplate
    Private _isNewTemplate As Boolean

    Public Sub New(config As AppConfig)
        _config = config
        InitializeComponent()
        ApplyLocale()
        PopulateEngineDropdowns()
        PopulateLanguageDropdown()
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
        lblBeamSize.Text = lp.GetString("Tmpl_BeamSize")
        lblMaxSegment.Text = lp.GetString("Tmpl_MaxSegment")
        lblVadSilence.Text = lp.GetString("Tmpl_VadSilence")
        lblInitialPrompt.Text = lp.GetString("Tmpl_InitialPrompt")
        lblVisibility.Text = lp.GetString("Tmpl_Visibility")
        lblAudioDevice.Text = lp.GetString("Tmpl_AudioDevice")
        lblModelPath.Text = lp.GetString("Tmpl_ModelPath")
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

        SaveAndSync()
        ShowDetail(False)
        RefreshList()
    End Sub

    Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
        ShowDetail(False)
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

        nudBeamSize.Value = Math.Max(nudBeamSize.Minimum, Math.Min(nudBeamSize.Maximum, t.BeamSize))
        nudMaxSegment.Value = Math.Max(nudMaxSegment.Minimum, Math.Min(nudMaxSegment.Maximum, t.MaxSegmentSec))
        nudVadSilence.Value = Math.Max(nudVadSilence.Minimum, Math.Min(nudVadSilence.Maximum, t.VadSilenceMs))
        txtInitialPrompt.Text = t.InitialPrompt
        txtAudioDevice.Text = t.AudioSourceLabel
        txtModelPath.Text = t.ModelPath

        Dim visIdx = cboVisibility.Items.IndexOf(t.DefaultVisibility)
        cboVisibility.SelectedIndex = If(visIdx >= 0, visIdx, 0)
    End Sub

    Private Sub SaveDetailToTemplate(t As ConferenceTemplate)
        t.Name = txtName.Text.Trim()
        t.HostingCode = txtHostingCode.Text.Trim()
        t.SourceLanguage = If(cboSourceLang.SelectedItem IsNot Nothing, cboSourceLang.SelectedItem.ToString(), "auto")
        t.SttBackendKey = ExtractEngineKey(cboSttEngine)
        t.TranslationBackendKey = ExtractEngineKey(cboTransEngine)
        t.BeamSize = CInt(nudBeamSize.Value)
        t.MaxSegmentSec = CInt(nudMaxSegment.Value)
        t.VadSilenceMs = CInt(nudVadSilence.Value)
        t.InitialPrompt = txtInitialPrompt.Text.Trim()
        t.AudioSourceLabel = txtAudioDevice.Text.Trim()
        t.ModelPath = txtModelPath.Text.Trim()
        t.DefaultVisibility = If(cboVisibility.SelectedItem IsNot Nothing, cboVisibility.SelectedItem.ToString(), "public")
    End Sub

    Private Sub SaveAndSync()
        ConfigManager.Save(_config)
        ' Sync to TemplateStore if the server is running
        Try
            Dim store = Services.Rooms.TemplateStore.Instance
            If store IsNot Nothing Then store.SyncFromConfig(_config.ConferenceTemplates)
        Catch
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

End Class
