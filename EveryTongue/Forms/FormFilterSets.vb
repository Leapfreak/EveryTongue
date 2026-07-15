Imports System.IO
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Config
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Manager for named filter sets (glossary / profanity / hallucinations files).
''' A set points at its own JSON files; "Copy global files into this set"
''' seeds a per-set folder under %AppData%\EveryTongue\filters\{id}\ from the
''' current global files, ready to edit (the Filter Editor still edits the
''' GLOBAL files — open the set folder to edit a named set's files).
''' </summary>
Public Class FormFilterSets

    Private _editing As FilterSet
    Private _isNew As Boolean

    Public Sub New()
        InitializeComponent()
        ApplyLocale()
        RefreshList()
        AddHandler btnBrowseGlossary.Click, Sub(s, e) BrowseJson(txtGlossary)
        AddHandler btnBrowseProfanity.Click, Sub(s, e) BrowseJson(txtProfanity)
        AddHandler btnBrowseHalluc.Click, Sub(s, e) BrowseJson(txtHalluc)
    End Sub

    Private Shared Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Private Sub ApplyLocale()
        Me.Text = S("FSet_Title")
        colName.Text = S("Tmpl_Name")
        btnAdd.Text = S("Tmpl_Add")
        btnEdit.Text = S("Tmpl_Edit")
        btnDelete.Text = S("Tmpl_Delete")
        lblName.Text = S("Tmpl_Name")
        lblGlossary.Text = S("FSet_Glossary")
        lblProfanity.Text = S("FSet_Profanity")
        lblHalluc.Text = S("FSet_Hallucinations")
        btnCopyGlobal.Text = S("FSet_CopyGlobal")
        btnOpenFolder.Text = S("FSet_OpenFolder")
        grpDetail.Text = S("FSet_Details")
    End Sub

    Private Shared Function SetFolder(id As String) As String
        ' Same root as filter-sets.json (TemplateLibraryStore) — the set's file
        ' paths are stored there, so the files must move with the config dir.
        Return Path.Combine(Global.EveryTongue.Models.ConfigManager.ConfigDirectory, "filters", id)
    End Function

    Private Sub RefreshList()
        lvSets.Items.Clear()
        For Each fs In TemplateLibraryStore.Instance.GetFilterSets()
            Dim li As New ListViewItem(fs.Name)
            li.Tag = fs.Id
            lvSets.Items.Add(li)
        Next
        UpdateButtonState()
    End Sub

    Private Sub UpdateButtonState()
        Dim hasSelection = lvSets.SelectedItems.Count > 0
        btnEdit.Enabled = hasSelection
        btnDelete.Enabled = hasSelection
    End Sub

    Private Sub lvSets_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lvSets.SelectedIndexChanged
        UpdateButtonState()
    End Sub

    ' ─── CRUD ─────────────────────────────────────────────────────────

    Private Sub btnAdd_Click(sender As Object, e As EventArgs) Handles btnAdd.Click
        _isNew = True
        _editing = New FilterSet()
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnEdit_Click(sender As Object, e As EventArgs) Handles btnEdit.Click
        If lvSets.SelectedItems.Count = 0 Then Return
        Dim id = CStr(lvSets.SelectedItems(0).Tag)
        _editing = TemplateLibraryStore.Instance.GetFilterSet(id)
        If _editing Is Nothing Then Return
        _isNew = False
        LoadToDetail(_editing)
        ShowDetail(True)
    End Sub

    Private Sub btnDelete_Click(sender As Object, e As EventArgs) Handles btnDelete.Click
        If lvSets.SelectedItems.Count = 0 Then Return
        Dim name = lvSets.SelectedItems(0).Text
        Dim id = CStr(lvSets.SelectedItems(0).Tag)
        If MessageBox.Show(String.Format(S("Tmpl_DeleteConfirm"), name), S("Tmpl_Delete"),
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        TemplateLibraryStore.Instance.DeleteFilterSet(id)
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
        _editing.GlossaryPath = txtGlossary.Text.Trim()
        _editing.ProfanityPath = txtProfanity.Text.Trim()
        _editing.HallucinationsPath = txtHalluc.Text.Trim()

        TemplateLibraryStore.Instance.UpsertFilterSet(_editing)
        ShowDetail(False)
        RefreshList()
    End Sub

    Private Sub btnCancelEdit_Click(sender As Object, e As EventArgs) Handles btnCancelEdit.Click
        ShowDetail(False)
    End Sub

    ''' <summary>Seed this set's folder from the current global filter files.</summary>
    Private Sub btnCopyGlobal_Click(sender As Object, e As EventArgs) Handles btnCopyGlobal.Click
        If _editing Is Nothing Then Return
        Try
            Dim folder = SetFolder(_editing.Id)
            Directory.CreateDirectory(folder)
            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory

            Dim copies As New List(Of (Source As String, Target As String, Box As TextBox)) From {
                (Path.Combine(baseDir, "translate-server", "glossary.json"), Path.Combine(folder, "glossary.json"), txtGlossary),
                (Path.Combine(baseDir, "translate-server", "profanity.json"), Path.Combine(folder, "profanity.json"), txtProfanity),
                (Path.Combine(baseDir, "live-server", "hallucinations.json"), Path.Combine(folder, "hallucinations.json"), txtHalluc)
            }
            For Each c In copies
                If File.Exists(c.Source) Then
                    File.Copy(c.Source, c.Target, overwrite:=True)
                Else
                    File.WriteAllText(c.Target, "{}")
                End If
                c.Box.Text = c.Target
            Next
            AppLogger.Log(LogEvents.CONFIG_TEMPLATE_LIB_SAVED, $"Filter set '{_editing.Name}' seeded from global files → {folder}")
        Catch ex As Exception
            MessageBox.Show(ex.Message, Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub btnOpenFolder_Click(sender As Object, e As EventArgs) Handles btnOpenFolder.Click
        If _editing Is Nothing Then Return
        Try
            Dim folder = SetFolder(_editing.Id)
            Directory.CreateDirectory(folder)
            Process.Start(New ProcessStartInfo(folder) With {.UseShellExecute = True})
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"FilterSets: open folder failed: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowDetail(visible As Boolean)
        grpDetail.Visible = visible
        lvSets.Enabled = Not visible
        btnAdd.Enabled = Not visible
        btnEdit.Enabled = Not visible
        btnDelete.Enabled = Not visible
        btnClose.Enabled = Not visible
        If visible Then txtName.Focus()
    End Sub

    Private Sub LoadToDetail(fs As FilterSet)
        txtName.Text = fs.Name
        txtGlossary.Text = fs.GlossaryPath
        txtProfanity.Text = fs.ProfanityPath
        txtHalluc.Text = fs.HallucinationsPath
    End Sub

    Private Sub BrowseJson(target As TextBox)
        Using dlg As New OpenFileDialog()
            dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            If Not String.IsNullOrEmpty(target.Text) Then
                Try : dlg.InitialDirectory = Path.GetDirectoryName(target.Text) : Catch : End Try
            End If
            If dlg.ShowDialog(Me) = DialogResult.OK Then target.Text = dlg.FileName
        End Using
    End Sub

End Class
