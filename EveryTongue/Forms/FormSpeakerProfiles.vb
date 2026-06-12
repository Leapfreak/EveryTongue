Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Manager for conference speaker profiles — thin bundles of REFERENCES to
''' preferred templates (online + offline STT, translate, TTS). Nothing is
''' embedded: swapping a speaker's Sunday "sharing" template for a "reading"
''' one means changing one slot's reference here.
''' </summary>
Public Class FormSpeakerProfiles

    Private _editing As SpeakerProfile
    Private _isNew As Boolean

    ''' <summary>Combo item mapping a display name to a template id ("" = none).</summary>
    Private Class RefItem
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

    Public Sub New()
        InitializeComponent()
        ApplyLocale()
        RefreshList()
    End Sub

    Private Shared Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Private Sub ApplyLocale()
        Me.Text = S("Spk_Title")
        colName.Text = S("Tmpl_Name")
        colOnline.Text = S("Spk_OnlineStt")
        colOffline.Text = S("Spk_OfflineStt")
        btnAdd.Text = S("Tmpl_Add")
        btnEdit.Text = S("Tmpl_Edit")
        btnDelete.Text = S("Tmpl_Delete")
        lblName.Text = S("Tmpl_Name")
        lblOnlineStt.Text = S("Spk_OnlineStt")
        lblOfflineStt.Text = S("Spk_OfflineStt")
        lblTranslate.Text = S("Spk_Translate")
        lblTts.Text = S("Spk_Tts")
        lblGlossary.Text = S("Spk_Glossary")
        grpDetail.Text = S("Spk_Details")
    End Sub

    Private Sub RefreshList()
        lvSpeakers.Items.Clear()
        Dim store = TemplateLibraryStore.Instance
        For Each sp In store.GetSpeakerProfiles()
            Dim li As New ListViewItem(sp.Name)
            li.SubItems.Add(TemplateName(sp.OnlineSttTemplateId))
            li.SubItems.Add(TemplateName(sp.OfflineSttTemplateId))
            li.Tag = sp.Id
            lvSpeakers.Items.Add(li)
        Next
        UpdateButtonState()
    End Sub

    Private Shared Function TemplateName(id As String) As String
        If String.IsNullOrEmpty(id) Then Return ""
        Dim tpl = TemplateLibraryStore.Instance.GetEngineTemplate(TemplateLibraryStore.GroupStt, id)
        Return If(tpl?.Name, "")
    End Function

    Private Sub UpdateButtonState()
        Dim hasSelection = lvSpeakers.SelectedItems.Count > 0
        btnEdit.Enabled = hasSelection
        btnDelete.Enabled = hasSelection
    End Sub

    Private Sub lvSpeakers_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lvSpeakers.SelectedIndexChanged
        UpdateButtonState()
    End Sub

    ' ─── CRUD ─────────────────────────────────────────────────────────

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        _isNew = True
        _editing = New SpeakerProfile()
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If lvSpeakers.SelectedItems.Count = 0 Then Return
        Dim id = CStr(lvSpeakers.SelectedItems(0).Tag)
        _editing = TemplateLibraryStore.Instance.GetSpeakerProfile(id)
        If _editing Is Nothing Then Return
        _isNew = False
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lvSpeakers.SelectedItems.Count = 0 Then Return
        Dim name = lvSpeakers.SelectedItems(0).Text
        Dim id = CStr(lvSpeakers.SelectedItems(0).Tag)
        If MessageBox.Show(String.Format(S("Tmpl_DeleteConfirm"), name), S("Tmpl_Delete"),
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        TemplateLibraryStore.Instance.DeleteSpeakerProfile(id)
        RefreshList()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If _editing Is Nothing Then Return
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show(S("EngTpl_NameRequired"), Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If

        _editing.Name = txtName.Text.Trim()
        _editing.OnlineSttTemplateId = SelectedId(cboOnlineStt)
        _editing.OfflineSttTemplateId = SelectedId(cboOfflineStt)
        _editing.TranslateTemplateId = SelectedId(cboTranslate)
        _editing.TtsTemplateId = SelectedId(cboTts)
        _editing.GlossarySetId = SelectedId(cboGlossary)

        TemplateLibraryStore.Instance.UpsertSpeakerProfile(_editing)
        ShowDetail(False)
        RefreshList()
    End Sub

    Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
        ShowDetail(False)
    End Sub

    Private Sub ShowDetail(visible As Boolean)
        grpDetail.Visible = visible
        lvSpeakers.Enabled = Not visible
        btnAdd.Enabled = Not visible
        btnEdit.Enabled = Not visible
        btnDelete.Enabled = Not visible
        btnClose.Enabled = Not visible
        If visible Then txtName.Focus()
    End Sub

    Private Sub LoadToDetail(sp As SpeakerProfile)
        txtName.Text = sp.Name
        Dim store = TemplateLibraryStore.Instance

        PopulateRefCombo(cboOnlineStt, store.GetEngineTemplates(TemplateLibraryStore.GroupStt), sp.OnlineSttTemplateId)
        PopulateRefCombo(cboOfflineStt, store.GetEngineTemplates(TemplateLibraryStore.GroupStt), sp.OfflineSttTemplateId)
        PopulateRefCombo(cboTranslate, store.GetEngineTemplates(TemplateLibraryStore.GroupTranslate), sp.TranslateTemplateId)
        PopulateRefCombo(cboTts, store.GetEngineTemplates(TemplateLibraryStore.GroupTts), sp.TtsTemplateId)

        ' Glossary set: "(global)" + named filter sets — the speaker's set
        ' overrides the room's glossary while that speaker is active.
        cboGlossary.Enabled = True
        cboGlossary.Items.Clear()
        cboGlossary.Items.Add(New RefItem(S("Spk_GlossaryGlobal"), ""))
        Dim glossIdx = 0
        For Each fs In store.GetFilterSets()
            cboGlossary.Items.Add(New RefItem(fs.Name, fs.Id))
            If fs.Id = If(sp.GlossarySetId, "") Then glossIdx = cboGlossary.Items.Count - 1
        Next
        cboGlossary.SelectedIndex = glossIdx
    End Sub

    Private Shared Sub PopulateRefCombo(cbo As ComboBox, templates As List(Of EngineTemplate), selectedId As String)
        cbo.Items.Clear()
        cbo.Items.Add(New RefItem(S("Spk_None"), ""))
        Dim selectIdx = 0
        For Each tpl In templates
            cbo.Items.Add(New RefItem($"{tpl.Name} [{tpl.EngineKey}]", tpl.Id))
            If tpl.Id = selectedId Then selectIdx = cbo.Items.Count - 1
        Next
        cbo.SelectedIndex = selectIdx
    End Sub

    Private Shared Function SelectedId(cbo As ComboBox) As String
        Dim item = TryCast(cbo.SelectedItem, RefItem)
        Return If(item?.Id, "")
    End Function

End Class
