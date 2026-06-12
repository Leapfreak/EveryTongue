Imports System.Reflection
Imports EveryTongue.Models
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Stt

''' <summary>
''' Manager for the STT template library (TemplateLibraryStore, group "stt").
''' One template = one engine + only that engine's knobs; the knob editors are
''' rendered from the engine's IEngineConfigDescriptor, so adding an engine
''' never edits this form. Editing a template here updates every conference
''' template / session that references it.
''' </summary>
Public Class FormEngineTemplates

    Private ReadOnly _config As AppConfig
    Private _editingTemplate As EngineTemplate
    Private _isNewTemplate As Boolean
    ' Field editors generated from the active engine descriptor (key → input control)
    Private ReadOnly _fieldEditors As New Dictionary(Of String, Control)
    Private _suppressEngineChange As Boolean = False

    Public Sub New(config As AppConfig)
        _config = config
        InitializeComponent()
        ApplyLocale()
        PopulateEngineDropdown()
        RefreshList()
    End Sub

    Private Shared Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Private Sub ApplyLocale()
        Me.Text = S("EngTpl_Title")
        colName.Text = S("Tmpl_Name")
        colEngine.Text = S("EngTpl_Engine")
        btnAdd.Text = S("Tmpl_Add")
        btnEdit.Text = S("Tmpl_Edit")
        btnDelete.Text = S("Tmpl_Delete")
        lblName.Text = S("Tmpl_Name")
        lblEngine.Text = S("EngTpl_Engine")
        grpDetail.Text = S("EngTpl_Details")
        chkIncludeAdvanced.Text = S("EngTpl_IncludeAdvanced")
    End Sub

    Private Sub PopulateEngineDropdown()
        cboEngine.Items.Clear()
        For Each entry In SttBackendRegistry.GetAll()
            cboEngine.Items.Add(entry.Key & " — " & entry.DisplayName)
        Next
        If cboEngine.Items.Count > 0 Then cboEngine.SelectedIndex = 0
    End Sub

    Private Shared Function ExtractEngineKey(cbo As ComboBox) As String
        If cbo.SelectedItem Is Nothing Then Return ""
        Dim txt = cbo.SelectedItem.ToString()
        Dim dashIdx = txt.IndexOf(" — ")
        If dashIdx > 0 Then Return txt.Substring(0, dashIdx)
        Return txt
    End Function

    Private Sub RefreshList()
        lvTemplates.Items.Clear()
        For Each t In TemplateLibraryStore.Instance.GetEngineTemplates(TemplateLibraryStore.GroupStt)
            Dim li As New ListViewItem(t.Name)
            li.SubItems.Add(t.EngineKey)
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

    ' ─── Add / Edit / Delete ──────────────────────────────────────────

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        _isNewTemplate = True
        _editingTemplate = New EngineTemplate()
        LoadTemplateToDetail(_editingTemplate)
        ShowDetail(True)
    End Sub

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)
        _editingTemplate = TemplateLibraryStore.Instance.GetEngineTemplate(TemplateLibraryStore.GroupStt, id)
        If _editingTemplate Is Nothing Then Return
        _isNewTemplate = False
        LoadTemplateToDetail(_editingTemplate)
        ShowDetail(True)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim name = lvTemplates.SelectedItems(0).Text
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)

        Dim refCount = _config.ConferenceTemplates.Where(Function(t) t.SttTemplateId = id).Count()
        Dim msg = String.Format(S("Tmpl_DeleteConfirm"), name)
        If refCount > 0 Then
            msg = String.Format(S("EngTpl_DeleteReferenced"), name, refCount)
        End If
        If MessageBox.Show(msg, S("Tmpl_Delete"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        TemplateLibraryStore.Instance.DeleteEngineTemplate(TemplateLibraryStore.GroupStt, id)
        RefreshList()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        If _editingTemplate Is Nothing Then Return
        If String.IsNullOrWhiteSpace(txtName.Text) Then
            MessageBox.Show(S("EngTpl_NameRequired"), Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtName.Focus()
            Return
        End If

        Dim engineKey = ExtractEngineKey(cboEngine)
        Dim descriptor = SttBackendRegistry.Find(engineKey)?.ConfigDescriptor
        If descriptor Is Nothing Then Return

        _editingTemplate.Name = txtName.Text.Trim()
        _editingTemplate.EngineKey = engineKey
        _editingTemplate.Config = EngineConfigResolver.BuildTemplateConfig(descriptor, ReadFieldValues())

        TemplateLibraryStore.Instance.UpsertEngineTemplate(TemplateLibraryStore.GroupStt, _editingTemplate)
        If _isNewTemplate Then _isNewTemplate = False
        ShowDetail(False)
        RefreshList()
    End Sub

    Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
        ShowDetail(False)
    End Sub

    Private Sub cboEngine_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboEngine.SelectedIndexChanged
        If _suppressEngineChange OrElse _editingTemplate Is Nothing Then Return
        ' Engine switch: re-render this engine's fields. Re-apply the stored
        ' block only when it belongs to the same engine.
        Dim engineKey = ExtractEngineKey(cboEngine)
        Dim descriptor = SttBackendRegistry.Find(engineKey)?.ConfigDescriptor
        If descriptor Is Nothing Then Return
        Dim block = descriptor.CreateDefault()
        If _editingTemplate.Config.HasValue AndAlso
           engineKey.Equals(If(_editingTemplate.EngineKey, ""), StringComparison.OrdinalIgnoreCase) Then
            descriptor.ApplyJson(block, _editingTemplate.Config.Value)
        End If
        RenderFields(descriptor, block)
    End Sub

    Private Sub chkIncludeAdvanced_CheckedChanged(sender As Object, e As EventArgs) Handles chkIncludeAdvanced.CheckedChanged
        If _editingTemplate Is Nothing OrElse _fieldEditors.Count = 0 Then Return
        ' Re-render with/without the advanced rows, preserving current edits.
        Dim descriptor = SttBackendRegistry.Find(ExtractEngineKey(cboEngine))?.ConfigDescriptor
        If descriptor Is Nothing Then Return
        Dim block = descriptor.CreateDefault()
        If _editingTemplate.Config.HasValue Then descriptor.ApplyJson(block, _editingTemplate.Config.Value)
        descriptor.ApplyOverrides(block, ReadFieldValues())
        RenderFields(descriptor, block)
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

    Private Sub LoadTemplateToDetail(t As EngineTemplate)
        txtName.Text = t.Name

        Dim engineKey = If(String.IsNullOrEmpty(t.EngineKey), ExtractEngineKey(cboEngine), t.EngineKey)
        _suppressEngineChange = True
        For i = 0 To cboEngine.Items.Count - 1
            If cboEngine.Items(i).ToString().StartsWith(engineKey & " ") Then
                cboEngine.SelectedIndex = i
                Exit For
            End If
        Next
        _suppressEngineChange = False

        Dim descriptor = SttBackendRegistry.Find(ExtractEngineKey(cboEngine))?.ConfigDescriptor
        If descriptor Is Nothing Then Return
        Dim block = descriptor.CreateDefault()
        If t.Config.HasValue Then descriptor.ApplyJson(block, t.Config.Value)
        ' Auto-include advanced fields when the stored template already pins some.
        chkIncludeAdvanced.Checked = TemplateHasAdvancedFields(t, descriptor)
        RenderFields(descriptor, block)
    End Sub

    Private Shared Function TemplateHasAdvancedFields(t As EngineTemplate, descriptor As IEngineConfigDescriptor) As Boolean
        If Not t.Config.HasValue OrElse t.Config.Value.ValueKind <> System.Text.Json.JsonValueKind.Object Then Return False
        For Each prop In t.Config.Value.EnumerateObject()
            For Each field In descriptor.Fields
                If field.Advanced AndAlso field.Key.Equals(prop.Name, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
        Next
        Return False
    End Function

    ' ─── Descriptor-driven field rendering ────────────────────────────
    ' The rows below are runtime-generated BY DESIGN: the field set depends on
    ' which engine the template is bound to (IEngineConfigDescriptor.Fields),
    ' so it cannot live in the Designer file.

    Private Sub RenderFields(descriptor As IEngineConfigDescriptor, block As IEngineConfigBlock)
        pnlFields.SuspendLayout()
        pnlFields.Controls.Clear()
        _fieldEditors.Clear()

        Dim y = 4
        For Each field In descriptor.Fields
            ' Advanced fields are only rendered (and therefore only stored/pinned)
            ' when the operator opts in — otherwise they follow app-global settings.
            If field.Advanced AndAlso Not chkIncludeAdvanced.Checked Then Continue For
            Dim labelText = S(field.LabelKey)
            If String.IsNullOrEmpty(labelText) OrElse labelText = field.LabelKey Then labelText = field.Key

            Dim lbl As New Label With {
                .Text = labelText,
                .Location = New Point(0, y + 3),
                .AutoSize = False,
                .Size = New Size(190, 17)
            }
            pnlFields.Controls.Add(lbl)

            Dim value = GetBlockValue(block, field.Key)
            Dim editor As Control = CreateEditor(field, value)
            editor.Location = New Point(196, y)
            pnlFields.Controls.Add(editor)
            _fieldEditors(field.Key) = editor

            y += 29
        Next
        pnlFields.ResumeLayout()
    End Sub

    Private Shared Function GetBlockValue(block As IEngineConfigBlock, key As String) As Object
        Dim prop = block.GetType().GetProperty(key, BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.IgnoreCase)
        Return prop?.GetValue(block)
    End Function

    Private Shared Function CreateEditor(field As EngineConfigField, value As Object) As Control
        Select Case field.FieldType
            Case EngineConfigFieldType.Integer, EngineConfigFieldType.Decimal
                Dim nud As New NumericUpDown With {
                    .Minimum = CDec(If(field.Min, 0.0)),
                    .Maximum = CDec(If(field.Max, 1000000.0)),
                    .DecimalPlaces = If(field.FieldType = EngineConfigFieldType.Decimal, 2, 0),
                    .Size = New Size(110, 23)
                }
                Try
                    nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, Convert.ToDecimal(If(value, 0))))
                Catch
                End Try
                Return nud
            Case EngineConfigFieldType.Toggle
                Return New CheckBox With {.Checked = CBool(If(value, False)), .Size = New Size(110, 23)}
            Case EngineConfigFieldType.Choice
                Dim cbo As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Size = New Size(180, 23)}
                If field.Choices IsNot Nothing Then
                    For Each c In field.Choices : cbo.Items.Add(c) : Next
                End If
                Dim idx = cbo.Items.IndexOf(Convert.ToString(If(value, "")))
                cbo.SelectedIndex = If(idx >= 0, idx, If(cbo.Items.Count > 0, 0, -1))
                Return cbo
            Case Else ' Text, FilePath, DirectoryPath, StringList
                Return New TextBox With {.Text = Convert.ToString(If(value, "")), .Size = New Size(330, 23)}
        End Select
    End Function

    ''' <summary>Read the current editor values. Empty strings are omitted so machine defaults still apply.</summary>
    Private Function ReadFieldValues() As Dictionary(Of String, Object)
        Dim values As New Dictionary(Of String, Object)
        For Each kvp In _fieldEditors
            Dim nud = TryCast(kvp.Value, NumericUpDown)
            Dim chk = TryCast(kvp.Value, CheckBox)
            Dim cbo = TryCast(kvp.Value, ComboBox)
            If nud IsNot Nothing Then
                values(kvp.Key) = If(nud.DecimalPlaces > 0, CObj(CDbl(nud.Value)), CObj(CInt(nud.Value)))
            ElseIf chk IsNot Nothing Then
                values(kvp.Key) = chk.Checked
            ElseIf cbo IsNot Nothing Then
                If cbo.SelectedItem IsNot Nothing Then values(kvp.Key) = cbo.SelectedItem.ToString()
            Else
                Dim text = kvp.Value.Text.Trim()
                If text.Length > 0 Then values(kvp.Key) = text
            End If
        Next
        Return values
    End Function

End Class
