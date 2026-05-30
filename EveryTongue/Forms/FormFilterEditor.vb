Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json

Partial Public Class FormFilterEditor

    Private ReadOnly _baseDir As String
    Private ReadOnly _livePort As Integer
    Private ReadOnly _nllbPort As Integer
    Private ReadOnly _httpClient As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(5)}
    ' File paths
    Private ReadOnly _hallucinationsPath As String
    Private ReadOnly _glossaryPath As String
    Private ReadOnly _profanityPath As String

    ' Data
    Private _halData As New Dictionary(Of String, List(Of String))()
    Private _profData As New Dictionary(Of String, List(Of String))()
    Private _glossaryData As New List(Of GlossaryEntry)()
    Private _selectedGlosIdx As Integer = -1
    Private _suppressGlosEvents As Boolean = False
    Private _dirty As Boolean = False

    Private Function S(key As String) As String
        Return Services.Infrastructure.LanguagePackService.Instance.GetString(key)
    End Function

    Public Sub New(baseDir As String, livePort As Integer, nllbPort As Integer)
        _baseDir = baseDir
        _livePort = livePort
        _nllbPort = nllbPort

        _hallucinationsPath = Path.Combine(baseDir, "live-server", "hallucinations.json")
        _glossaryPath = Path.Combine(baseDir, "nllb-server", "glossary.json")
        _profanityPath = Path.Combine(baseDir, "nllb-server", "profanity.json")

        InitializeComponent()
        ApplyLocalization()
        WireUpEvents()
        LoadAllData()
    End Sub

    Private Sub ApplyLocalization()
        Me.Text = S("FE_Title")
        tabHal.Text = S("FE_Tab_Hallucinations")
        tabProf.Text = S("FE_Tab_Profanity")
        tabGlos.Text = S("FE_Tab_Glossary")

        lblHalDesc.Text = S("FE_Desc_Hallucinations")
        lblHalLang.Text = S("FE_Language")
        btnHalAddLang.Text = S("FE_AddLanguage")
        btnHalAdd.Text = S("FE_Add")
        btnHalRemove.Text = S("FE_RemoveSelected")
        btnHalSave.Text = S("FE_SaveReload")
        lblProfDesc.Text = S("FE_Desc_Profanity")
        lblProfLang.Text = S("FE_Language")
        btnProfAddLang.Text = S("FE_AddLanguage")
        btnProfAdd.Text = S("FE_Add")
        btnProfRemove.Text = S("FE_RemoveSelected")
        btnProfSave.Text = S("FE_SaveReload")
        lblGlosDesc.Text = S("FE_Desc_Glossary")
        colTrigger.HeaderText = S("FE_ColTrigger")
        colSourceLangs.HeaderText = S("FE_ColSourceLangs")
        colComment.HeaderText = S("FE_ColComment")
        btnGlosAdd.Text = S("FE_AddEntry")
        btnGlosRemove.Text = S("FE_RemoveEntry")
        grpDetail.Text = S("FE_SelectedEntry")
        lblTrigger.Text = S("FE_Trigger")
        lblSrcLangs.Text = S("FE_SourceLangs")
        lblComment.Text = S("FE_Comment")
        lblFixes.Text = S("FE_Fixes")
        colTargetLang.HeaderText = S("FE_ColTargetLang")
        colWrong.HeaderText = S("FE_ColWrong")
        colRight.HeaderText = S("FE_ColRight")
        btnFixAdd.Text = S("FE_AddFix")
        btnFixRemove.Text = S("FE_RemoveFix")
        btnGlosSave.Text = S("FE_SaveReload")
    End Sub

    Private Sub WireUpEvents()
        AddHandler dgvGlossary.SelectionChanged, AddressOf GlossarySelectionChanged
        AddHandler txtGlosTrigger.TextChanged, AddressOf GlossaryDetailChanged
        AddHandler txtGlosSourceLangs.TextChanged, AddressOf GlossaryDetailChanged
        AddHandler txtGlosComment.TextChanged, AddressOf GlossaryDetailChanged
        AddHandler dgvFixes.CellEndEdit, AddressOf FixCellEdited
    End Sub

    ' =========================================================================
    ' Data Loading
    ' =========================================================================
    Private Sub LoadAllData()
        LoadHallucinations()
        LoadProfanity()
        LoadGlossary()
    End Sub

    Private Sub LoadHallucinations()
        _halData.Clear()
        Try
            If File.Exists(_hallucinationsPath) Then
                Dim json = File.ReadAllText(_hallucinationsPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim items As New List(Of String)()
                        For Each item In prop.Value.EnumerateArray()
                            items.Add(item.GetString())
                        Next
                        _halData(prop.Name) = items
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load hallucinations.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        cboHalLang.Items.Clear()
        For Each lang In _halData.Keys.OrderBy(Function(k) k)
            cboHalLang.Items.Add(lang)
        Next
        If cboHalLang.Items.Count > 0 Then cboHalLang.SelectedIndex = 0
        AddHandler cboHalLang.SelectedIndexChanged, AddressOf HalLangChanged
        HalLangChanged(Nothing, Nothing)
    End Sub

    Private Sub LoadProfanity()
        _profData.Clear()
        Try
            If File.Exists(_profanityPath) Then
                Dim json = File.ReadAllText(_profanityPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim items As New List(Of String)()
                        For Each item In prop.Value.EnumerateArray()
                            items.Add(item.GetString())
                        Next
                        _profData(prop.Name) = items
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load profanity.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        cboProfLang.Items.Clear()
        For Each lang In _profData.Keys.OrderBy(Function(k) k)
            cboProfLang.Items.Add(lang)
        Next
        If cboProfLang.Items.Count > 0 Then cboProfLang.SelectedIndex = 0
        AddHandler cboProfLang.SelectedIndexChanged, AddressOf ProfLangChanged
        ProfLangChanged(Nothing, Nothing)
    End Sub

    Private Sub LoadGlossary()
        _glossaryData.Clear()
        Try
            If File.Exists(_glossaryPath) Then
                Dim json = File.ReadAllText(_glossaryPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each elem In doc.RootElement.EnumerateArray()
                        Dim entry As New GlossaryEntry()
                        If elem.TryGetProperty("trigger", entry._triggerElem) Then entry.Trigger = entry._triggerElem.GetString()
                        If elem.TryGetProperty("comment", entry._commentElem) Then entry.Comment = entry._commentElem.GetString()
                        If elem.TryGetProperty("source_langs", entry._srcElem) Then
                            For Each sl In entry._srcElem.EnumerateArray()
                                entry.SourceLangs.Add(sl.GetString())
                            Next
                        End If
                        If elem.TryGetProperty("fixes", entry._fixesElem) Then
                            For Each tl In entry._fixesElem.EnumerateObject()
                                For Each fixItem In tl.Value.EnumerateObject()
                                    entry.Fixes.Add(New GlossaryFix() With {
                                        .TargetLang = tl.Name,
                                        .Wrong = fixItem.Name,
                                        .Right = fixItem.Value.GetString()
                                    })
                                Next
                            Next
                        End If
                        _glossaryData.Add(entry)
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load glossary.json: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try

        RefreshGlossaryGrid()
    End Sub

    ' =========================================================================
    ' Hallucinations events
    ' =========================================================================
    Private Sub HalLangChanged(sender As Object, e As EventArgs)
        lstHalPhrases.Items.Clear()
        Dim lang = TryCast(cboHalLang.SelectedItem, String)
        If lang IsNot Nothing AndAlso _halData.ContainsKey(lang) Then
            For Each phrase In _halData(lang)
                lstHalPhrases.Items.Add(phrase)
            Next
        End If
    End Sub

    Private Sub btnHalAdd_Click(sender As Object, e As EventArgs) Handles btnHalAdd.Click
        Dim phrase = txtHalPhrase.Text.Trim()
        If String.IsNullOrEmpty(phrase) Then Return
        Dim lang = TryCast(cboHalLang.SelectedItem, String)
        If lang Is Nothing Then Return

        If Not _halData.ContainsKey(lang) Then _halData(lang) = New List(Of String)()
        If Not _halData(lang).Contains(phrase) Then
            _halData(lang).Add(phrase)
            lstHalPhrases.Items.Add(phrase)
            txtHalPhrase.Clear()
            _dirty = True
        End If
    End Sub

    Private Sub btnHalRemove_Click(sender As Object, e As EventArgs) Handles btnHalRemove.Click
        Dim idx = lstHalPhrases.SelectedIndex
        If idx < 0 Then Return
        Dim lang = TryCast(cboHalLang.SelectedItem, String)
        If lang Is Nothing Then Return

        _halData(lang).RemoveAt(idx)
        lstHalPhrases.Items.RemoveAt(idx)
        _dirty = True
    End Sub

    Private Sub btnHalAddLang_Click(sender As Object, e As EventArgs) Handles btnHalAddLang.Click
        Dim lang = InputBox(S("FE_PromptLangCode"), S("FE_AddLanguage"))
        If String.IsNullOrWhiteSpace(lang) Then Return
        lang = lang.Trim().ToLower()
        If _halData.ContainsKey(lang) Then
            cboHalLang.SelectedItem = lang
            Return
        End If
        _halData(lang) = New List(Of String)()
        cboHalLang.Items.Add(lang)
        cboHalLang.SelectedItem = lang
        _dirty = True
    End Sub

    ' =========================================================================
    ' Profanity events
    ' =========================================================================
    Private Sub ProfLangChanged(sender As Object, e As EventArgs)
        lstProfWords.Items.Clear()
        Dim lang = TryCast(cboProfLang.SelectedItem, String)
        If lang IsNot Nothing AndAlso _profData.ContainsKey(lang) Then
            For Each word In _profData(lang)
                lstProfWords.Items.Add(word)
            Next
        End If
    End Sub

    Private Sub btnProfAdd_Click(sender As Object, e As EventArgs) Handles btnProfAdd.Click
        Dim word = txtProfWord.Text.Trim()
        If String.IsNullOrEmpty(word) Then Return
        Dim lang = TryCast(cboProfLang.SelectedItem, String)
        If lang Is Nothing Then Return

        If Not _profData.ContainsKey(lang) Then _profData(lang) = New List(Of String)()
        If Not _profData(lang).Contains(word) Then
            _profData(lang).Add(word)
            lstProfWords.Items.Add(word)
            txtProfWord.Clear()
            _dirty = True
        End If
    End Sub

    Private Sub btnProfRemove_Click(sender As Object, e As EventArgs) Handles btnProfRemove.Click
        Dim idx = lstProfWords.SelectedIndex
        If idx < 0 Then Return
        Dim lang = TryCast(cboProfLang.SelectedItem, String)
        If lang Is Nothing Then Return

        _profData(lang).RemoveAt(idx)
        lstProfWords.Items.RemoveAt(idx)
        _dirty = True
    End Sub

    Private Sub btnProfAddLang_Click(sender As Object, e As EventArgs) Handles btnProfAddLang.Click
        Dim lang = InputBox(S("FE_PromptNllbCode"), S("FE_AddLanguage"))
        If String.IsNullOrWhiteSpace(lang) Then Return
        lang = lang.Trim()
        If _profData.ContainsKey(lang) Then
            cboProfLang.SelectedItem = lang
            Return
        End If
        _profData(lang) = New List(Of String)()
        cboProfLang.Items.Add(lang)
        cboProfLang.SelectedItem = lang
        _dirty = True
    End Sub

    ' =========================================================================
    ' Glossary events
    ' =========================================================================
    Private Sub RefreshGlossaryGrid()
        dgvGlossary.Rows.Clear()
        For Each entry In _glossaryData
            dgvGlossary.Rows.Add(entry.Trigger, String.Join(", ", entry.SourceLangs), entry.Comment)
        Next
    End Sub

    Private Sub GlossarySelectionChanged(sender As Object, e As EventArgs)
        If _suppressGlosEvents Then Return
        SaveCurrentGlossaryDetail()

        If dgvGlossary.SelectedRows.Count = 0 Then
            _selectedGlosIdx = -1
            txtGlosTrigger.Clear()
            txtGlosComment.Clear()
            txtGlosSourceLangs.Clear()
            dgvFixes.Rows.Clear()
            Return
        End If

        _selectedGlosIdx = dgvGlossary.SelectedRows(0).Index
        If _selectedGlosIdx < 0 OrElse _selectedGlosIdx >= _glossaryData.Count Then Return

        Dim entry = _glossaryData(_selectedGlosIdx)
        _suppressGlosEvents = True
        txtGlosTrigger.Text = entry.Trigger
        txtGlosComment.Text = entry.Comment
        txtGlosSourceLangs.Text = String.Join(", ", entry.SourceLangs)

        dgvFixes.Rows.Clear()
        dgvFixes.ReadOnly = False
        For Each fixItem In entry.Fixes
            dgvFixes.Rows.Add(fixItem.TargetLang, fixItem.Wrong, fixItem.Right)
        Next
        _suppressGlosEvents = False
    End Sub

    Private Sub GlossaryDetailChanged(sender As Object, e As EventArgs)
        If _suppressGlosEvents OrElse _selectedGlosIdx < 0 OrElse _selectedGlosIdx >= _glossaryData.Count Then Return
        _dirty = True
    End Sub

    Private Sub SaveCurrentGlossaryDetail()
        If _selectedGlosIdx < 0 OrElse _selectedGlosIdx >= _glossaryData.Count Then Return

        Dim entry = _glossaryData(_selectedGlosIdx)
        entry.Trigger = txtGlosTrigger.Text.Trim()
        entry.Comment = txtGlosComment.Text.Trim()
        entry.SourceLangs = txtGlosSourceLangs.Text.Split({","c}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(s) s.Trim()).Where(Function(s) s.Length > 0).ToList()

        ' Save fixes from grid
        entry.Fixes.Clear()
        For Each row As DataGridViewRow In dgvFixes.Rows
            Dim tl = TryCast(row.Cells("colTargetLang").Value, String)
            Dim wrong = TryCast(row.Cells("colWrong").Value, String)
            Dim right = TryCast(row.Cells("colRight").Value, String)
            If Not String.IsNullOrWhiteSpace(tl) AndAlso Not String.IsNullOrWhiteSpace(wrong) Then
                entry.Fixes.Add(New GlossaryFix() With {
                    .TargetLang = tl.Trim(),
                    .Wrong = wrong.Trim(),
                    .Right = If(right, "").Trim()
                })
            End If
        Next

        ' Update the main grid row
        If _selectedGlosIdx < dgvGlossary.Rows.Count Then
            dgvGlossary.Rows(_selectedGlosIdx).Cells("colTrigger").Value = entry.Trigger
            dgvGlossary.Rows(_selectedGlosIdx).Cells("colSourceLangs").Value = String.Join(", ", entry.SourceLangs)
            dgvGlossary.Rows(_selectedGlosIdx).Cells("colComment").Value = entry.Comment
        End If
    End Sub

    Private Sub FixCellEdited(sender As Object, e As DataGridViewCellEventArgs)
        _dirty = True
    End Sub

    Private Sub btnGlosAdd_Click(sender As Object, e As EventArgs) Handles btnGlosAdd.Click
        Dim entry As New GlossaryEntry() With {.Trigger = "new_trigger", .Comment = ""}
        _glossaryData.Add(entry)
        dgvGlossary.Rows.Add(entry.Trigger, "", "")
        dgvGlossary.ClearSelection()
        dgvGlossary.Rows(dgvGlossary.Rows.Count - 1).Selected = True
        _dirty = True
    End Sub

    Private Sub btnGlosRemove_Click(sender As Object, e As EventArgs) Handles btnGlosRemove.Click
        If _selectedGlosIdx < 0 OrElse _selectedGlosIdx >= _glossaryData.Count Then Return
        _glossaryData.RemoveAt(_selectedGlosIdx)
        _suppressGlosEvents = True
        dgvGlossary.Rows.RemoveAt(_selectedGlosIdx)
        _suppressGlosEvents = False
        _selectedGlosIdx = -1
        txtGlosTrigger.Clear()
        txtGlosComment.Clear()
        txtGlosSourceLangs.Clear()
        dgvFixes.Rows.Clear()
        _dirty = True
    End Sub

    Private Sub btnFixAdd_Click(sender As Object, e As EventArgs) Handles btnFixAdd.Click
        If _selectedGlosIdx < 0 Then Return
        dgvFixes.Rows.Add("eng_Latn", "", "")
        _dirty = True
    End Sub

    Private Sub btnFixRemove_Click(sender As Object, e As EventArgs) Handles btnFixRemove.Click
        If dgvFixes.SelectedRows.Count = 0 Then Return
        dgvFixes.Rows.RemoveAt(dgvFixes.SelectedRows(0).Index)
        _dirty = True
    End Sub

    ' =========================================================================
    ' Save & Reload
    ' =========================================================================
    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnHalSave.Click, btnProfSave.Click, btnGlosSave.Click
        SaveCurrentGlossaryDetail()

        Try
            SaveHallucinations()
            SaveProfanity()
            SaveGlossary()
        Catch ex As Exception
            MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ' Reload on servers (fire-and-forget, don't block on errors)
        ReloadServersAsync()

        _dirty = False
        MessageBox.Show(S("FE_Saved"), S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub SaveHallucinations()
        Using ms As New MemoryStream()
            Using writer As New Utf8JsonWriter(ms, New JsonWriterOptions() With {.Indented = True})
                writer.WriteStartObject()
                For Each kvp In _halData.OrderBy(Function(k) k.Key)
                    writer.WriteStartArray(kvp.Key)
                    For Each phrase In kvp.Value
                        writer.WriteStringValue(phrase)
                    Next
                    writer.WriteEndArray()
                Next
                writer.WriteEndObject()
            End Using
            File.WriteAllBytes(_hallucinationsPath, ms.ToArray())
        End Using
    End Sub

    Private Sub SaveProfanity()
        Using ms As New MemoryStream()
            Using writer As New Utf8JsonWriter(ms, New JsonWriterOptions() With {.Indented = True})
                writer.WriteStartObject()
                For Each kvp In _profData.OrderBy(Function(k) k.Key)
                    writer.WriteStartArray(kvp.Key)
                    For Each word In kvp.Value
                        writer.WriteStringValue(word)
                    Next
                    writer.WriteEndArray()
                Next
                writer.WriteEndObject()
            End Using
            File.WriteAllBytes(_profanityPath, ms.ToArray())
        End Using
    End Sub

    Private Sub SaveGlossary()
        Using ms As New MemoryStream()
            Using writer As New Utf8JsonWriter(ms, New JsonWriterOptions() With {.Indented = True, .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping})
                writer.WriteStartArray()
                For Each entry In _glossaryData
                    writer.WriteStartObject()
                    writer.WriteString("comment", entry.Comment)
                    writer.WriteString("trigger", entry.Trigger)

                    writer.WriteStartArray("source_langs")
                    For Each sl In entry.SourceLangs
                        writer.WriteStringValue(sl)
                    Next
                    writer.WriteEndArray()

                    ' Group fixes by target language
                    writer.WriteStartObject("fixes")
                    Dim grouped = entry.Fixes.GroupBy(Function(f) f.TargetLang)
                    For Each grp In grouped
                        writer.WriteStartObject(grp.Key)
                        For Each fixItem In grp
                            writer.WriteString(fixItem.Wrong, fixItem.Right)
                        Next
                        writer.WriteEndObject()
                    Next
                    writer.WriteEndObject()

                    writer.WriteEndObject()
                Next
                writer.WriteEndArray()
            End Using
            File.WriteAllBytes(_glossaryPath, ms.ToArray())
        End Using
    End Sub

    Private Async Sub ReloadServersAsync()
        Dim content As New StringContent("{}", Encoding.UTF8, "application/json")
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_livePort}/hallucinations/reload", content)
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ERROR] ReloadServersAsync (hallucinations/reload): {ex.Message}")
        End Try
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_nllbPort}/glossary/reload", New StringContent("{}", Encoding.UTF8, "application/json"))
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ERROR] ReloadServersAsync (glossary/reload): {ex.Message}")
        End Try
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_nllbPort}/profanity/reload", New StringContent("{}", Encoding.UTF8, "application/json"))
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ERROR] ReloadServersAsync (profanity/reload): {ex.Message}")
        End Try
    End Sub

    Private Sub FormFilterEditor_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If _dirty Then
            Dim result = MessageBox.Show(S("FE_DiscardChanges"), S("FE_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then e.Cancel = True
        End If
    End Sub

    ' =========================================================================
    ' Data classes
    ' =========================================================================
    Private Class GlossaryEntry
        Public Property Trigger As String = ""
        Public Property Comment As String = ""
        Public Property SourceLangs As New List(Of String)()
        Public Property Fixes As New List(Of GlossaryFix)()

        ' Temp fields used during parsing only
        Friend _triggerElem As JsonElement
        Friend _commentElem As JsonElement
        Friend _srcElem As JsonElement
        Friend _fixesElem As JsonElement
    End Class

    Private Class GlossaryFix
        Public Property TargetLang As String = ""
        Public Property Wrong As String = ""
        Public Property Right As String = ""
    End Class

End Class
