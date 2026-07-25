Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Routing preview for the English-pivot policy: pick a source language and an
''' engine, see every target language's route (direct vs via English) and why.
''' Uses the SAME PivotPolicy class the orchestrator routes with, constructed
''' from the Options dialog's UNSAVED mode selection — so the preview shows the
''' effect of the mode the user is about to save, and can never drift from the
''' real routing logic.
''' </summary>
Public Class FormPivotPreview

    Private ReadOnly _policy As Services.Translation.PivotPolicy
    Private ReadOnly _sourceCodes As New List(Of String)
    Private ReadOnly _engineBackendNames As New List(Of String)
    Private ReadOnly _pivotLangName As String
    Private _routeDirectText As String = "direct"
    Private _routeViaText As String = "via {0}"
    Private _summaryText As String = "{0} direct / {1} via {2}"

    ''' <param name="mode">The (possibly unsaved) pivot mode selected in Options.</param>
    ''' <param name="pivotLanguage">FLORES code of the pivot language (config value).</param>
    ''' <param name="initialEngineKey">Registry key of the engine selected in Options.</param>
    Public Sub New(mode As TranslationPivotMode, pivotLanguage As String, initialEngineKey As String)
        InitializeComponent()

        _policy = New Services.Translation.PivotPolicy(
            New Server.ServerOptions With {
                .TranslationPivotMode = mode,
                .TranslationPivotLanguage = pivotLanguage
            })

        Dim pivotName = pivotLanguage
        For Each lang In LanguageCodeService.Instance.GetAllLanguagesSorted()
            If pivotLanguage.Equals(lang.Flores, StringComparison.OrdinalIgnoreCase) Then
                pivotName = lang.Name
                Exit For
            End If
        Next
        _pivotLangName = pivotName

        ApplyLocale()
        PopulateSources()
        PopulateEngines(initialEngineKey)

        AddHandler cboSource.SelectedIndexChanged, Sub(s, e) RefreshRoutes()
        AddHandler cboEngine.SelectedIndexChanged, Sub(s, e) RefreshRoutes()
        RefreshRoutes()
    End Sub

    Private Sub ApplyLocale()
        Dim lp = LanguagePackService.Instance
        Me.Text = lp.GetString("PivotPrev_Title")
        lblSource.Text = lp.GetString("PivotPrev_Source")
        lblEngine.Text = lp.GetString("PivotPrev_Engine")
        colLang.Text = lp.GetString("PivotPrev_ColLang")
        colRoute.Text = lp.GetString("PivotPrev_ColRoute")
        colWhy.Text = lp.GetString("PivotPrev_ColWhy")
        btnClose.Text = lp.GetString("Opt_Cancel")
        _routeDirectText = lp.GetString("PivotPrev_RouteDirect")
        _routeViaText = lp.GetString("PivotPrev_RouteVia")
        _summaryText = lp.GetString("PivotPrev_Summary")
    End Sub

    Private Sub PopulateSources()
        cboSource.BeginUpdate()
        cboSource.Items.Clear()
        _sourceCodes.Clear()
        Dim selectIdx = 0
        For Each lang In LanguageCodeService.Instance.GetAllLanguagesSorted()
            If String.IsNullOrEmpty(lang.Flores) Then Continue For
            _sourceCodes.Add(lang.Flores)
            cboSource.Items.Add($"{lang.Name} ({lang.Flores})")
        Next
        cboSource.EndUpdate()
        If cboSource.Items.Count > 0 Then cboSource.SelectedIndex = selectIdx
    End Sub

    Private Sub PopulateEngines(initialEngineKey As String)
        cboEngine.BeginUpdate()
        cboEngine.Items.Clear()
        _engineBackendNames.Clear()
        Dim selectIdx = 0
        For Each entry In Services.Translation.TranslationBackendRegistry.GetAll()
            ' Inline engines (Speechmatics) are not orchestrator backends — the
            ' pivot policy never routes them.
            If Services.Translation.TranslationBackendRegistry.IsInlineEngine(entry.Key) Then Continue For
            If entry.Key.Equals(If(initialEngineKey, ""), StringComparison.OrdinalIgnoreCase) Then
                selectIdx = _engineBackendNames.Count
            End If
            _engineBackendNames.Add(entry.BackendName)
            cboEngine.Items.Add(entry.DisplayName)
        Next
        cboEngine.EndUpdate()
        If cboEngine.Items.Count > 0 Then cboEngine.SelectedIndex = selectIdx
    End Sub

    Private Sub RefreshRoutes()
        If cboSource.SelectedIndex < 0 OrElse cboEngine.SelectedIndex < 0 Then Return
        Dim sourceFlores = _sourceCodes(cboSource.SelectedIndex)
        Dim backendName = _engineBackendNames(cboEngine.SelectedIndex)

        Dim directCount = 0
        Dim pivotCount = 0
        lvRoutes.BeginUpdate()
        lvRoutes.Items.Clear()
        Dim idx = 0
        For Each lang In LanguageCodeService.Instance.GetAllLanguagesSorted()
            If String.IsNullOrEmpty(lang.Flores) Then Continue For
            If lang.Flores.Equals(sourceFlores, StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim d = _policy.Decide(sourceFlores, lang.Flores, backendName)
            Dim item As New ListViewItem($"{lang.Name} ({lang.Flores})")
            If d.ShouldPivot Then
                pivotCount += 1
                item.SubItems.Add(String.Format(_routeViaText, _pivotLangName))
                item.ForeColor = Drawing.Color.DarkOrange
            Else
                directCount += 1
                item.SubItems.Add(_routeDirectText)
            End If
            item.SubItems.Add(d.Reason)
            lvRoutes.Items.Add(item)
            idx += 1
        Next
        lvRoutes.EndUpdate()
        lblSummary.Text = String.Format(_summaryText, directCount, pivotCount, _pivotLangName)
    End Sub

End Class
