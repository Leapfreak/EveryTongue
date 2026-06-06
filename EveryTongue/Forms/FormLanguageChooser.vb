Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Modal dialog that lets the user pick a language by display name.
''' Returns the internal code (FLORES or ISO 639-1) for the selected language.
''' </summary>
Partial Public Class FormLanguageChooser

    ''' <summary>
    ''' Each item in the list: display text and internal code.
    ''' </summary>
    Private Class LangItem
        Public Property DisplayText As String
        Public Property Code As String
        Public Overrides Function ToString() As String
            Return DisplayText
        End Function
    End Class

    Private ReadOnly _allItems As New List(Of LangItem)()

    ''' <summary>The selected language code (FLORES or ISO 639-1 depending on mode).</summary>
    Public Property SelectedCode As String = ""

    ''' <summary>
    ''' Creates a language chooser.
    ''' </summary>
    ''' <param name="codeFormat">"flores" to return FLORES codes (e.g. eng_Latn), "iso1" to return ISO 639-1 (e.g. en)</param>
    ''' <param name="excludeCodes">Codes already present (will be excluded from the list)</param>
    Public Sub New(codeFormat As String, excludeCodes As IEnumerable(Of String))
        InitializeComponent()

        Dim lcService = LanguageCodeService.Instance
        Dim allLangs = lcService.GetAllLanguagesSorted()
        Dim excluded As New HashSet(Of String)(excludeCodes, StringComparer.OrdinalIgnoreCase)

        For Each lang In allLangs
            Dim code = If(codeFormat = "iso1", lang.Iso1, lang.Flores)
            If String.IsNullOrEmpty(code) Then Continue For
            If excluded.Contains(code) Then Continue For

            Dim displayText = lang.Name
            If Not String.IsNullOrEmpty(lang.Native) AndAlso lang.Native <> lang.Name Then
                displayText = $"{lang.Name} ({lang.Native})"
            End If

            _allItems.Add(New LangItem With {.DisplayText = displayText, .Code = code})
        Next

        FilterList("")

        AddHandler txtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        AddHandler lstLanguages.DoubleClick, AddressOf LstLanguages_DoubleClick
        AddHandler btnOK.Click, AddressOf BtnOK_Click
    End Sub

    Private Sub FilterList(filter As String)
        lstLanguages.Items.Clear()
        Dim lowerFilter = filter.ToLowerInvariant()
        For Each item In _allItems
            If String.IsNullOrEmpty(lowerFilter) OrElse
               item.DisplayText.ToLowerInvariant().Contains(lowerFilter) Then
                lstLanguages.Items.Add(item)
            End If
        Next
        If lstLanguages.Items.Count > 0 Then lstLanguages.SelectedIndex = 0
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        FilterList(txtSearch.Text.Trim())
    End Sub

    Private Sub LstLanguages_DoubleClick(sender As Object, e As EventArgs)
        AcceptSelection()
    End Sub

    Private Sub BtnOK_Click(sender As Object, e As EventArgs)
        AcceptSelection()
    End Sub

    Private Sub AcceptSelection()
        Dim selected = TryCast(lstLanguages.SelectedItem, LangItem)
        If selected Is Nothing Then Return
        SelectedCode = selected.Code
        DialogResult = DialogResult.OK
        Close()
    End Sub

End Class
