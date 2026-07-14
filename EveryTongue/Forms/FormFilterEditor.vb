Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports EveryTongue.Services.Infrastructure

Partial Public Class FormFilterEditor

    Private ReadOnly _baseDir As String
    Private ReadOnly _livePort As Integer
    Private ReadOnly _transPort As Integer
    Private ReadOnly _httpClient As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(5)}
    Private ReadOnly _lcService As LanguageCodeService = LanguageCodeService.Instance

    ' File paths
    Private ReadOnly _hallucinationsPath As String
    Private ReadOnly _glossaryPath As String
    Private ReadOnly _profanityPath As String

    ' Data — all keyed by FLORES code as canonical key
    Private _halData As New Dictionary(Of String, List(Of FilterItem))()     ' FLORES -> phrases
    Private _profData As New Dictionary(Of String, List(Of FilterItem))()    ' FLORES -> words
    Private _glosData As New Dictionary(Of String, List(Of GlossaryEntry))() ' FLORES -> entries
    Private _selectedGlosIdx As Integer = -1
    Private _suppressGlosEvents As Boolean = False
    Private _dirty As Boolean = False

    ' Unified language combo: displayName -> FLORES code
    Private ReadOnly _langMap As New Dictionary(Of String, String)()

    ' Code conversion caches (built once from LanguageCodeService)
    Private ReadOnly _iso1ToFlores As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _floresToIso1 As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

    ' All language display names for the target lang combo column
    Private ReadOnly _allLangDisplayNames As New List(Of String)()

    Private Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Public Sub New(baseDir As String, livePort As Integer, transPort As Integer)
        _baseDir = baseDir
        _livePort = livePort
        _transPort = transPort

        _hallucinationsPath = Path.Combine(baseDir, "live-server", "hallucinations.json")
        _glossaryPath = Path.Combine(baseDir, "translate-server", "glossary.json")
        _profanityPath = Path.Combine(baseDir, "translate-server", "profanity.json")

        InitializeComponent()
        Me.Icon = Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        BuildCodeMappings()
        PopulateTargetLangCombo()
        ApplyLocalization()
        WireUpEvents()
        _suppressGlosEvents = True
        LoadAllData()
        _suppressGlosEvents = False
        _dirty = False
    End Sub

    Private Sub BuildCodeMappings()
        Dim allLangs = _lcService.GetAllLanguagesSorted()
        For Each lang In allLangs
            If Not String.IsNullOrEmpty(lang.Iso1) Then
                _iso1ToFlores(lang.Iso1) = lang.Flores
            End If
            _floresToIso1(lang.Flores) = If(lang.Iso1, "")
        Next
    End Sub

    Private Sub PopulateTargetLangCombo()
        _allLangDisplayNames.Clear()
        Dim allLangs = _lcService.GetAllLanguagesSorted()
        For Each lang In allLangs
            _allLangDisplayNames.Add(lang.Name)
        Next
        colTargetLang.Items.AddRange(_allLangDisplayNames.ToArray())
    End Sub

    Private Sub ApplyLocalization()
        Me.Text = S("FE_Title")
        lblLang.Text = S("FE_Language")
        btnAddLang.Text = S("FE_AddLanguage")
        tabHal.Text = S("FE_Tab_Hallucinations")
        tabProf.Text = S("FE_Tab_Profanity")
        tabGlos.Text = S("FE_Tab_Glossary")

        lblHalDesc.Text = S("FE_Desc_Hallucinations")
        btnHalAdd.Text = S("FE_Add")
        btnHalRemove.Text = S("FE_RemoveSelected")
        btnHalSave.Text = S("FE_SaveReload")
        lblProfDesc.Text = S("FE_Desc_Profanity")
        btnProfAdd.Text = S("FE_Add")
        btnProfRemove.Text = S("FE_RemoveSelected")
        btnProfSave.Text = S("FE_SaveReload")
        lblGlosDesc.Text = S("FE_Desc_Glossary")
        btnGlosAdd.Text = S("FE_Add")
        btnGlosRemove.Text = S("FE_RemoveSelected")
        grpDetail.Text = S("FE_SelectedEntry")
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
        AddHandler cboLang.SelectedIndexChanged, AddressOf LangChanged
        AddHandler clbGlosEntries.SelectedIndexChanged, AddressOf GlosSelectionChanged
        AddHandler clbGlosEntries.ItemCheck, AddressOf GlosItemChecked
        AddHandler txtGlosComment.TextChanged, AddressOf GlossaryDetailChanged
        AddHandler dgvFixes.CellEndEdit, AddressOf FixCellEdited
        AddHandler clbHalPhrases.ItemCheck, AddressOf HalItemChecked
        AddHandler clbProfWords.ItemCheck, AddressOf ProfItemChecked
    End Sub

    Private Function GetLangDisplayName(code As String) As String
        Dim name = _lcService.GetDisplayNameForCode(code)
        If String.IsNullOrEmpty(name) OrElse name = code Then Return code
        Return name
    End Function

    Private Function Iso1ToFlores(iso1 As String) As String
        Dim flores As String = Nothing
        If _iso1ToFlores.TryGetValue(iso1, flores) Then Return flores
        Return iso1 ' fallback
    End Function

    Private Function FloresToIso1(flores As String) As String
        Dim iso1 As String = Nothing
        If _floresToIso1.TryGetValue(flores, iso1) AndAlso Not String.IsNullOrEmpty(iso1) Then Return iso1
        Return flores ' fallback
    End Function

    ' =========================================================================
    ' Data Loading
    ' =========================================================================
    Private Sub LoadAllData()
        LoadHallucinations()
        LoadProfanity()
        LoadGlossary()
        BuildLanguageCombo()
    End Sub

    Private Sub LoadHallucinations()
        _halData.Clear()
        Try
            If File.Exists(_hallucinationsPath) Then
                Dim json = File.ReadAllText(_hallucinationsPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim items As New List(Of FilterItem)()
                        For Each item In prop.Value.EnumerateArray()
                            If item.ValueKind = JsonValueKind.String Then
                                items.Add(New FilterItem With {.Text = item.GetString(), .Enabled = True})
                            ElseIf item.ValueKind = JsonValueKind.Object Then
                                Dim text = ""
                                Dim enabled = True
                                Dim textProp As JsonElement
                                Dim enabledProp As JsonElement
                                If item.TryGetProperty("text", textProp) Then text = textProp.GetString()
                                If item.TryGetProperty("enabled", enabledProp) Then enabled = enabledProp.GetBoolean()
                                items.Add(New FilterItem With {.Text = text, .Enabled = enabled})
                            End If
                        Next
                        ' Convert ISO 639-1 key to FLORES for unified storage
                        Dim floresKey = Iso1ToFlores(prop.Name)
                        _halData(floresKey) = items
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load hallucinations.json: {ex.Message}", S("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub LoadProfanity()
        _profData.Clear()
        Try
            If File.Exists(_profanityPath) Then
                Dim json = File.ReadAllText(_profanityPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim items As New List(Of FilterItem)()
                        For Each item In prop.Value.EnumerateArray()
                            If item.ValueKind = JsonValueKind.String Then
                                items.Add(New FilterItem With {.Text = item.GetString(), .Enabled = True})
                            ElseIf item.ValueKind = JsonValueKind.Object Then
                                Dim text = ""
                                Dim enabled = True
                                Dim wordProp As JsonElement
                                Dim enabledProp As JsonElement
                                If item.TryGetProperty("word", wordProp) Then text = wordProp.GetString()
                                If item.TryGetProperty("enabled", enabledProp) Then enabled = enabledProp.GetBoolean()
                                items.Add(New FilterItem With {.Text = text, .Enabled = enabled})
                            End If
                        Next
                        _profData(prop.Name) = items
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load profanity.json: {ex.Message}", S("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub LoadGlossary()
        _glosData.Clear()
        Try
            If File.Exists(_glossaryPath) Then
                Dim json = File.ReadAllText(_glossaryPath, Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim entries As New List(Of GlossaryEntry)()
                        For Each elem In prop.Value.EnumerateArray()
                            entries.Add(ParseGlossaryEntry(elem))
                        Next
                        _glosData(prop.Name) = entries
                    Next
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show($"Failed to load glossary.json: {ex.Message}", S("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Function ParseGlossaryEntry(elem As JsonElement) As GlossaryEntry
        Dim entry As New GlossaryEntry()
        Dim trigProp As JsonElement
        Dim commentProp As JsonElement
        Dim enabledProp As JsonElement
        Dim fixesProp As JsonElement

        If elem.TryGetProperty("trigger", trigProp) Then entry.Trigger = trigProp.GetString()
        If elem.TryGetProperty("comment", commentProp) Then entry.Comment = commentProp.GetString()
        If elem.TryGetProperty("enabled", enabledProp) Then entry.Enabled = enabledProp.GetBoolean()

        If elem.TryGetProperty("fixes", fixesProp) Then
            For Each tl In fixesProp.EnumerateObject()
                For Each fixItem In tl.Value.EnumerateObject()
                    entry.Fixes.Add(New GlossaryFix() With {
                        .TargetLang = tl.Name,
                        .Wrong = fixItem.Name,
                        .Right = fixItem.Value.GetString()
                    })
                Next
            Next
        End If

        Return entry
    End Function

    ' =========================================================================
    ' Shared language combo
    ' =========================================================================
    Private Sub BuildLanguageCombo()
        Dim allCodes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each k In _halData.Keys : allCodes.Add(k) : Next
        For Each k In _profData.Keys : allCodes.Add(k) : Next
        For Each k In _glosData.Keys : allCodes.Add(k) : Next

        _langMap.Clear()
        cboLang.Items.Clear()
        For Each code In allCodes.OrderBy(Function(c) GetLangDisplayName(c))
            Dim displayName = GetLangDisplayName(code)
            _langMap(displayName) = code
            cboLang.Items.Add(displayName)
        Next
        If cboLang.Items.Count > 0 Then cboLang.SelectedIndex = 0
        LangChanged(Nothing, Nothing)
    End Sub

    Private Function GetSelectedFloresCode() As String
        Dim displayName = TryCast(cboLang.SelectedItem, String)
        If displayName IsNot Nothing AndAlso _langMap.ContainsKey(displayName) Then
            Return _langMap(displayName)
        End If
        Return Nothing
    End Function

    Private Sub LangChanged(sender As Object, e As EventArgs)
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return
        RefreshHalTab(flores)
        RefreshProfTab(flores)
        RefreshGlosTab(flores)
    End Sub

    Private Sub btnAddLang_Click(sender As Object, e As EventArgs) Handles btnAddLang.Click
        ' Use FLORES codes for the unified picker
        Dim existingFloresCodes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each kvp In _langMap
            existingFloresCodes.Add(kvp.Value)
        Next

        Using picker As New FormLanguageChooser("flores", existingFloresCodes)
            If picker.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim code = picker.SelectedCode
            If String.IsNullOrEmpty(code) Then Return

            Dim displayName = GetLangDisplayName(code)
            If _langMap.ContainsKey(displayName) Then
                ' Already exists — select it
                cboLang.SelectedItem = displayName
                Return
            End If

            _langMap(displayName) = code
            cboLang.Items.Add(displayName)
            cboLang.SelectedItem = displayName
            _dirty = True
        End Using
    End Sub

    ' =========================================================================
    ' Hallucinations tab
    ' =========================================================================
    Private Sub RefreshHalTab(flores As String)
        clbHalPhrases.Items.Clear()
        If _halData.ContainsKey(flores) Then
            ' Sort data in-place so display index = data index
            _halData(flores).Sort(Function(a, b) String.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase))
            For Each item In _halData(flores)
                clbHalPhrases.Items.Add(item.Text, item.Enabled)
            Next
        End If
    End Sub

    Private Sub HalItemChecked(sender As Object, e As ItemCheckEventArgs)
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _halData.ContainsKey(flores) Then Return
        If e.Index >= 0 AndAlso e.Index < _halData(flores).Count Then
            _halData(flores)(e.Index).Enabled = (e.NewValue = CheckState.Checked)
            _dirty = True
        End If
    End Sub

    Private Sub btnHalAdd_Click(sender As Object, e As EventArgs) Handles btnHalAdd.Click
        Dim phrase = txtHalPhrase.Text.Trim()
        If String.IsNullOrEmpty(phrase) Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return

        If Not _halData.ContainsKey(flores) Then _halData(flores) = New List(Of FilterItem)()
        If _halData(flores).Any(Function(x) String.Equals(x.Text, phrase, StringComparison.OrdinalIgnoreCase)) Then
            MessageBox.Show(S("FE_DuplicateEntry"), S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        _halData(flores).Add(New FilterItem With {.Text = phrase, .Enabled = True})
        RefreshHalTab(flores)
        txtHalPhrase.Clear()
        _dirty = True
    End Sub

    Private Sub btnHalRemove_Click(sender As Object, e As EventArgs) Handles btnHalRemove.Click
        Dim idx = clbHalPhrases.SelectedIndex
        If idx < 0 Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return

        _halData(flores).RemoveAt(idx)
        clbHalPhrases.Items.RemoveAt(idx)
        _dirty = True
    End Sub

    ' =========================================================================
    ' Profanity tab
    ' =========================================================================
    Private Sub RefreshProfTab(flores As String)
        clbProfWords.Items.Clear()
        If _profData.ContainsKey(flores) Then
            ' Sort data in-place so display index = data index
            _profData(flores).Sort(Function(a, b) String.Compare(a.Text, b.Text, StringComparison.OrdinalIgnoreCase))
            For Each item In _profData(flores)
                clbProfWords.Items.Add(item.Text, item.Enabled)
            Next
        End If
    End Sub

    Private Sub ProfItemChecked(sender As Object, e As ItemCheckEventArgs)
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _profData.ContainsKey(flores) Then Return
        If e.Index >= 0 AndAlso e.Index < _profData(flores).Count Then
            _profData(flores)(e.Index).Enabled = (e.NewValue = CheckState.Checked)
            _dirty = True
        End If
    End Sub

    Private Sub btnProfAdd_Click(sender As Object, e As EventArgs) Handles btnProfAdd.Click
        Dim word = txtProfWord.Text.Trim()
        If String.IsNullOrEmpty(word) Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return

        If Not _profData.ContainsKey(flores) Then _profData(flores) = New List(Of FilterItem)()
        If _profData(flores).Any(Function(x) String.Equals(x.Text, word, StringComparison.OrdinalIgnoreCase)) Then
            MessageBox.Show(S("FE_DuplicateEntry"), S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        _profData(flores).Add(New FilterItem With {.Text = word, .Enabled = True})
        RefreshProfTab(flores)
        txtProfWord.Clear()
        _dirty = True
    End Sub

    Private Sub btnProfRemove_Click(sender As Object, e As EventArgs) Handles btnProfRemove.Click
        Dim idx = clbProfWords.SelectedIndex
        If idx < 0 Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return

        _profData(flores).RemoveAt(idx)
        clbProfWords.Items.RemoveAt(idx)
        _dirty = True
    End Sub

    ' =========================================================================
    ' Glossary tab
    ' =========================================================================
    Private Sub RefreshGlosTab(flores As String)
        _suppressGlosEvents = True
        _selectedGlosIdx = -1
        txtGlosComment.Clear()
        dgvFixes.Rows.Clear()
        grpDetail.Enabled = False

        clbGlosEntries.Items.Clear()
        If _glosData.ContainsKey(flores) Then
            ' Sort data in-place so display index = data index
            _glosData(flores).Sort(Function(a, b) String.Compare(a.Trigger, b.Trigger, StringComparison.OrdinalIgnoreCase))
            For Each entry In _glosData(flores)
                clbGlosEntries.Items.Add(entry.Trigger, entry.Enabled)
            Next
        End If
        _suppressGlosEvents = False
    End Sub

    Private Sub GlosItemChecked(sender As Object, e As ItemCheckEventArgs)
        If _suppressGlosEvents Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _glosData.ContainsKey(flores) Then Return
        If e.Index >= 0 AndAlso e.Index < _glosData(flores).Count Then
            _glosData(flores)(e.Index).Enabled = (e.NewValue = CheckState.Checked)
            _dirty = True
        End If
    End Sub

    Private Sub GlosSelectionChanged(sender As Object, e As EventArgs)
        If _suppressGlosEvents Then Return
        SaveCurrentGlossaryDetail()

        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _glosData.ContainsKey(flores) Then Return

        Dim idx = clbGlosEntries.SelectedIndex
        If idx < 0 OrElse idx >= _glosData(flores).Count Then
            _selectedGlosIdx = -1
            txtGlosComment.Clear()
            dgvFixes.Rows.Clear()
            grpDetail.Enabled = False
            Return
        End If

        _selectedGlosIdx = idx
        Dim entry = _glosData(flores)(idx)
        _suppressGlosEvents = True
        grpDetail.Enabled = True
        grpDetail.Text = $"{S("FE_SelectedEntry")}: {entry.Trigger}"
        txtGlosComment.Text = entry.Comment

        dgvFixes.Rows.Clear()
        dgvFixes.ReadOnly = False
        For Each fixItem In entry.Fixes
            dgvFixes.Rows.Add(GetLangDisplayName(fixItem.TargetLang), fixItem.Wrong, fixItem.Right)
        Next
        _suppressGlosEvents = False
    End Sub

    Private Sub GlossaryDetailChanged(sender As Object, e As EventArgs)
        If _suppressGlosEvents OrElse _selectedGlosIdx < 0 Then Return
        _dirty = True
    End Sub

    Private Sub SaveCurrentGlossaryDetail()
        If _selectedGlosIdx < 0 Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _glosData.ContainsKey(flores) Then Return
        If _selectedGlosIdx >= _glosData(flores).Count Then Return

        Dim entry = _glosData(flores)(_selectedGlosIdx)
        entry.Comment = txtGlosComment.Text.Trim()

        ' Save fixes from grid — convert display names back to FLORES codes
        entry.Fixes.Clear()
        For Each row As DataGridViewRow In dgvFixes.Rows
            Dim tl = TryCast(row.Cells("colTargetLang").Value, String)
            Dim wrong = TryCast(row.Cells("colWrong").Value, String)
            Dim right = TryCast(row.Cells("colRight").Value, String)
            If Not String.IsNullOrWhiteSpace(tl) AndAlso Not String.IsNullOrWhiteSpace(wrong) Then
                entry.Fixes.Add(New GlossaryFix() With {
                    .TargetLang = DisplayNameToFlores(tl.Trim()),
                    .Wrong = wrong.Trim(),
                    .Right = If(right, "").Trim()
                })
            End If
        Next
    End Sub

    Private Function DisplayNameToFlores(input As String) As String
        If String.IsNullOrEmpty(input) Then Return ""

        ' Check if it's already a valid code
        Dim existing = _lcService.GetDisplayNameForCode(input)
        If existing <> input Then Return input

        ' Search by display name
        Dim allLangs = _lcService.GetAllLanguagesSorted()
        For Each lang In allLangs
            If String.Equals(lang.Name, input, StringComparison.OrdinalIgnoreCase) OrElse
               String.Equals(lang.Native, input, StringComparison.OrdinalIgnoreCase) Then
                Return lang.Flores
            End If
        Next

        Return input
    End Function

    Private Sub FixCellEdited(sender As Object, e As DataGridViewCellEventArgs)
        If _suppressGlosEvents Then Return
        _dirty = True
    End Sub

    Private Sub btnGlosAdd_Click(sender As Object, e As EventArgs) Handles btnGlosAdd.Click
        Dim trigger = txtGlosNewTrigger.Text.Trim()
        If String.IsNullOrEmpty(trigger) Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing Then Return

        If Not _glosData.ContainsKey(flores) Then _glosData(flores) = New List(Of GlossaryEntry)()
        If _glosData(flores).Any(Function(x) String.Equals(x.Trigger, trigger, StringComparison.OrdinalIgnoreCase)) Then
            MessageBox.Show(S("FE_DuplicateEntry"), S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        SaveCurrentGlossaryDetail()
        Dim entry As New GlossaryEntry() With {.Trigger = trigger, .Comment = "", .Enabled = True}
        _glosData(flores).Add(entry)
        RefreshGlosTab(flores)

        ' Select the newly added entry
        For i = 0 To clbGlosEntries.Items.Count - 1
            If String.Equals(TryCast(clbGlosEntries.Items(i), String), trigger, StringComparison.OrdinalIgnoreCase) Then
                clbGlosEntries.SelectedIndex = i
                Exit For
            End If
        Next
        txtGlosNewTrigger.Clear()
        _dirty = True
    End Sub

    Private Sub btnGlosRemove_Click(sender As Object, e As EventArgs) Handles btnGlosRemove.Click
        Dim idx = clbGlosEntries.SelectedIndex
        If idx < 0 Then Return
        Dim flores = GetSelectedFloresCode()
        If flores Is Nothing OrElse Not _glosData.ContainsKey(flores) Then Return
        If idx >= _glosData(flores).Count Then Return

        _suppressGlosEvents = True
        _glosData(flores).RemoveAt(idx)
        clbGlosEntries.Items.RemoveAt(idx)
        _selectedGlosIdx = -1
        txtGlosComment.Clear()
        dgvFixes.Rows.Clear()
        grpDetail.Enabled = False
        grpDetail.Text = S("FE_SelectedEntry")
        _suppressGlosEvents = False
        _dirty = True
    End Sub

    Private Sub btnFixAdd_Click(sender As Object, e As EventArgs) Handles btnFixAdd.Click
        If _selectedGlosIdx < 0 Then Return
        dgvFixes.Rows.Add(GetLangDisplayName("eng_Latn"), "", "")
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

        ' Check for duplicates before saving
        Dim dupes = FindDuplicates()
        If dupes IsNot Nothing Then
            MessageBox.Show(dupes, S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            SaveHallucinations()
            SaveProfanity()
            SaveGlossary()
        Catch ex As Exception
            MessageBox.Show($"Failed to save: {ex.Message}", S("Msg_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ReloadServersAsync()
        _dirty = False
        MessageBox.Show(S("FE_Saved"), S("FE_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Function FindDuplicates() As String
        For Each kvp In _halData
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In kvp.Value
                If Not String.IsNullOrWhiteSpace(item.Text) AndAlso Not seen.Add(item.Text) Then
                    Return $"{S("FE_DuplicateEntry")} ({GetLangDisplayName(kvp.Key)}: ""{item.Text}"")"
                End If
            Next
        Next
        For Each kvp In _profData
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In kvp.Value
                If Not String.IsNullOrWhiteSpace(item.Text) AndAlso Not seen.Add(item.Text) Then
                    Return $"{S("FE_DuplicateEntry")} ({GetLangDisplayName(kvp.Key)}: ""{item.Text}"")"
                End If
            Next
        Next
        For Each kvp In _glosData
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each entry In kvp.Value
                If Not String.IsNullOrWhiteSpace(entry.Trigger) AndAlso Not seen.Add(entry.Trigger) Then
                    Return $"{S("FE_DuplicateEntry")} ({GetLangDisplayName(kvp.Key)}: ""{entry.Trigger}"")"
                End If
            Next
        Next
        Return Nothing
    End Function

    Private Sub SaveHallucinations()
        Using ms As New MemoryStream()
            Using writer As New Utf8JsonWriter(ms, New JsonWriterOptions() With {.Indented = True})
                writer.WriteStartObject()
                For Each kvp In _halData.OrderBy(Function(k) k.Key)
                    If kvp.Value.Count = 0 Then Continue For
                    ' Convert FLORES key back to ISO 639-1 for hallucinations.json
                    Dim iso1Key = FloresToIso1(kvp.Key)
                    writer.WriteStartArray(iso1Key)
                    For Each item In kvp.Value
                        writer.WriteStartObject()
                        writer.WriteString("text", item.Text)
                        writer.WriteBoolean("enabled", item.Enabled)
                        writer.WriteEndObject()
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
                    If kvp.Value.Count = 0 Then Continue For
                    writer.WriteStartArray(kvp.Key)
                    For Each item In kvp.Value
                        writer.WriteStartObject()
                        writer.WriteString("word", item.Text)
                        writer.WriteBoolean("enabled", item.Enabled)
                        writer.WriteEndObject()
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
                writer.WriteStartObject()
                For Each kvp In _glosData.OrderBy(Function(k) k.Key)
                    If kvp.Value.Count = 0 Then Continue For
                    writer.WriteStartArray(kvp.Key)
                    For Each entry In kvp.Value
                        writer.WriteStartObject()
                        writer.WriteString("trigger", entry.Trigger)
                        writer.WriteString("comment", entry.Comment)
                        writer.WriteBoolean("enabled", entry.Enabled)

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
                Next
                writer.WriteEndObject()
            End Using
            File.WriteAllBytes(_glossaryPath, ms.ToArray())
        End Using
    End Sub

    Private Async Sub ReloadServersAsync()
        Dim content As New StringContent("{}", Encoding.UTF8, "application/json")
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_livePort}/hallucinations/reload", content)
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"ReloadServersAsync (hallucinations/reload): {ex.Message}")
        End Try
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_transPort}/glossary/reload", New StringContent("{}", Encoding.UTF8, "application/json"))
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"ReloadServersAsync (glossary/reload): {ex.Message}")
        End Try
        Try
            Await _httpClient.PostAsync($"http://127.0.0.1:{_transPort}/profanity/reload", New StringContent("{}", Encoding.UTF8, "application/json"))
        Catch ex As Exception
            AppLogger.Log(LogEvents.UI_ERROR, $"ReloadServersAsync (profanity/reload): {ex.Message}")
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

    Private Class FilterItem
        Public Property Text As String = ""
        Public Property Enabled As Boolean = True
    End Class

    Private Class GlossaryEntry
        Public Property Trigger As String = ""
        Public Property Comment As String = ""
        Public Property Enabled As Boolean = True
        Public Property Fixes As New List(Of GlossaryFix)()
    End Class

    Private Class GlossaryFix
        Public Property TargetLang As String = ""
        Public Property Wrong As String = ""
        Public Property Right As String = ""
    End Class

End Class
