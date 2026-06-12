Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Manager for Display templates — the operator-level projected appearance and
''' offered-languages set a session can reference. Viewer-level preferences
''' (each phone's own font size) deliberately stay per-device.
''' </summary>
Public Class FormDisplayTemplates

    Private _editing As DisplayTemplate
    Private _isNew As Boolean

    ''' <summary>Checklist item: FLORES code + display name.</summary>
    Private Class LangItem
        Public ReadOnly Property Flores As String
        Public ReadOnly Property DisplayName As String

        Public Sub New(flores As String, displayName As String)
            Me.Flores = flores
            Me.DisplayName = displayName
        End Sub

        Public Overrides Function ToString() As String
            Return DisplayName
        End Function
    End Class

    Public Sub New()
        InitializeComponent()
        ApplyLocale()
        PopulateStaticCombos()
        RefreshList()
        AddHandler btnBg.Click, Sub(s, e) PickColor(btnBg)
        AddHandler btnFg.Click, Sub(s, e) PickColor(btnFg)
    End Sub

    Private Shared Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Private Sub ApplyLocale()
        Me.Text = S("Disp_Title")
        colName.Text = S("Tmpl_Name")
        colLangs.Text = S("Disp_OfferedShort")
        btnAdd.Text = S("Tmpl_Add")
        btnEdit.Text = S("Tmpl_Edit")
        btnDelete.Text = S("Tmpl_Delete")
        lblName.Text = S("Tmpl_Name")
        lblBg.Text = S("Disp_Background")
        lblFg.Text = S("Disp_TextColor")
        lblFont.Text = S("Disp_Font")
        lblSize.Text = S("Disp_Size")
        chkBold.Text = S("Opt_Bold")
        lblLayout.Text = S("Disp_Layout")
        lblOffered.Text = S("Disp_Offered")
        grpDetail.Text = S("Disp_Details")
    End Sub

    Private Sub PopulateStaticCombos()
        cboFont.Items.Clear()
        For Each fam In Drawing.FontFamily.Families
            cboFont.Items.Add(fam.Name)
        Next

        cboLayout.Items.Clear()
        cboLayout.Items.Add(S("Disp_LayoutSingle"))
        cboLayout.Items.Add(S("Disp_LayoutStacked"))

        clbOffered.Items.Clear()
        For Each lang In LanguageCodeService.Instance.GetAllLanguagesSorted()
            Dim label = If(String.IsNullOrEmpty(lang.Name), lang.Flores, lang.Name)
            clbOffered.Items.Add(New LangItem(lang.Flores, label))
        Next
    End Sub

    Private Sub RefreshList()
        lvTemplates.Items.Clear()
        For Each d In TemplateLibraryStore.Instance.GetDisplayTemplates()
            Dim li As New ListViewItem(d.Name)
            Dim langCount = If(d.OfferedLanguages, New List(Of String)).Count
            li.SubItems.Add(If(langCount = 0, S("Disp_AllLanguages"), langCount.ToString()))
            li.Tag = d.Id
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

    ' ─── CRUD ─────────────────────────────────────────────────────────

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        _isNew = True
        _editing = New DisplayTemplate()
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)
        _editing = TemplateLibraryStore.Instance.GetDisplayTemplate(id)
        If _editing Is Nothing Then Return
        _isNew = False
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lvTemplates.SelectedItems.Count = 0 Then Return
        Dim name = lvTemplates.SelectedItems(0).Text
        Dim id = CStr(lvTemplates.SelectedItems(0).Tag)
        If MessageBox.Show(String.Format(S("Tmpl_DeleteConfirm"), name), S("Tmpl_Delete"),
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        TemplateLibraryStore.Instance.DeleteDisplayTemplate(id)
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
        _editing.BgColor = Drawing.ColorTranslator.ToHtml(btnBg.BackColor)
        _editing.FgColor = Drawing.ColorTranslator.ToHtml(btnFg.BackColor)
        _editing.FontFamily = If(cboFont.SelectedItem?.ToString(), "Segoe UI")
        _editing.FontSize = CSng(nudSize.Value)
        _editing.FontBold = chkBold.Checked
        _editing.Layout = If(cboLayout.SelectedIndex = 1, "stacked", "single")
        _editing.OfferedLanguages = New List(Of String)
        For Each idx In clbOffered.CheckedIndices.Cast(Of Integer)()
            Dim item = TryCast(clbOffered.Items(idx), LangItem)
            If item IsNot Nothing Then _editing.OfferedLanguages.Add(item.Flores)
        Next

        TemplateLibraryStore.Instance.UpsertDisplayTemplate(_editing)
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

    Private Sub LoadToDetail(d As DisplayTemplate)
        txtName.Text = d.Name
        Try
            btnBg.BackColor = Drawing.ColorTranslator.FromHtml(If(d.BgColor, "#000000"))
            btnFg.BackColor = Drawing.ColorTranslator.FromHtml(If(d.FgColor, "#FFFFFF"))
        Catch
            btnBg.BackColor = Drawing.Color.Black
            btnFg.BackColor = Drawing.Color.White
        End Try

        Dim fontIdx = cboFont.Items.IndexOf(If(d.FontFamily, "Segoe UI"))
        cboFont.SelectedIndex = If(fontIdx >= 0, fontIdx, Math.Max(0, cboFont.Items.IndexOf("Segoe UI")))
        nudSize.Value = Math.Max(nudSize.Minimum, Math.Min(nudSize.Maximum, CDec(d.FontSize)))
        chkBold.Checked = d.FontBold
        cboLayout.SelectedIndex = If("stacked".Equals(If(d.Layout, ""), StringComparison.OrdinalIgnoreCase), 1, 0)

        Dim offered = If(d.OfferedLanguages, New List(Of String))
        For i = 0 To clbOffered.Items.Count - 1
            Dim item = TryCast(clbOffered.Items(i), LangItem)
            clbOffered.SetItemChecked(i, item IsNot Nothing AndAlso offered.Contains(item.Flores))
        Next
    End Sub

    Private Shared Sub PickColor(btn As Button)
        Using dlg As New ColorDialog()
            dlg.Color = btn.BackColor
            If dlg.ShowDialog() = DialogResult.OK Then
                btn.BackColor = dlg.Color
            End If
        End Using
    End Sub

End Class
