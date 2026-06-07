Imports EveryTongue.Services.Infrastructure

Public Class FormLogConfig

    Private ReadOnly _config As Models.AppConfig
    Private ReadOnly _getString As Func(Of String, String)
    Private _suppressPresetChange As Boolean = False

    Public Property Routing As LogRoutingConfig

    Public Sub New(config As Models.AppConfig, getString As Func(Of String, String))
        InitializeComponent()
        _config = config
        _getString = getString

        ' Populate combo box items for level columns
        Dim levels = [Enum].GetNames(GetType(LogSeverity))
        colFileLevel.Items.AddRange(levels)
        colUiLevel.Items.AddRange(levels)

        ' Load current routing (or default)
        Routing = If(config.LogRouting, LogRoutingConfig.CreateNormal())
        LoadRouting(Routing)
        DetectPreset()
    End Sub

    Private Sub LoadRouting(cfg As LogRoutingConfig)
        _suppressPresetChange = True
        dgvRouting.Rows.Clear()
        For Each cat In [Enum].GetValues(GetType(LogCategory)).Cast(Of LogCategory)()
            Dim route As LogCategoryRouting = Nothing
            If Not cfg.Routes.TryGetValue(cat, route) Then
                route = New LogCategoryRouting()
            End If
            Dim idx = dgvRouting.Rows.Add(
                cat.ToString(),
                route.Enabled,
                route.FileLevel.ToString(),
                route.UiLevel.ToString())
            dgvRouting.Rows(idx).Tag = cat
        Next
        _suppressPresetChange = False
    End Sub

    Private Sub DetectPreset()
        ' Check if current config matches a preset
        Dim minimal = LogRoutingConfig.CreateMinimal()
        Dim normal = LogRoutingConfig.CreateNormal()
        Dim verbose = LogRoutingConfig.CreateVerbose()

        If MatchesPreset(minimal) Then
            cboPreset.SelectedIndex = 0
        ElseIf MatchesPreset(normal) Then
            cboPreset.SelectedIndex = 1
        ElseIf MatchesPreset(verbose) Then
            cboPreset.SelectedIndex = 2
        Else
            cboPreset.SelectedIndex = 3 ' Custom
        End If
    End Sub

    Private Function MatchesPreset(preset As LogRoutingConfig) As Boolean
        For Each cat In [Enum].GetValues(GetType(LogCategory)).Cast(Of LogCategory)()
            Dim cur As LogCategoryRouting = Nothing
            Dim pre As LogCategoryRouting = Nothing
            Routing.Routes.TryGetValue(cat, cur)
            preset.Routes.TryGetValue(cat, pre)
            If cur Is Nothing OrElse pre Is Nothing Then Return False
            If cur.Enabled <> pre.Enabled OrElse cur.FileLevel <> pre.FileLevel OrElse cur.UiLevel <> pre.UiLevel Then
                Return False
            End If
        Next
        Return True
    End Function

    Private Sub cboPreset_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboPreset.SelectedIndexChanged
        If _suppressPresetChange Then Return
        Select Case cboPreset.SelectedIndex
            Case 0 : LoadRouting(LogRoutingConfig.CreateMinimal())
            Case 1 : LoadRouting(LogRoutingConfig.CreateNormal())
            Case 2 : LoadRouting(LogRoutingConfig.CreateVerbose())
            Case 3 ' Custom — no change
        End Select
    End Sub

    Private Sub dgvRouting_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles dgvRouting.CellValueChanged
        If e.RowIndex < 0 Then Return
        If _suppressPresetChange Then Return
        ' Any manual edit → switch preset to "Custom"
        _suppressPresetChange = True
        cboPreset.SelectedIndex = 3
        _suppressPresetChange = False
    End Sub

    Private Sub dgvRouting_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles dgvRouting.CurrentCellDirtyStateChanged
        ' Commit checkbox changes immediately
        If dgvRouting.IsCurrentCellDirty Then
            dgvRouting.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        ' Build routing config from grid
        Dim cfg As New LogRoutingConfig()
        For Each row As DataGridViewRow In dgvRouting.Rows
            Dim cat = DirectCast(row.Tag, LogCategory)
            Dim enabled = CBool(If(row.Cells(1).Value, True))
            Dim fileLevel As LogSeverity = LogSeverity.Info
            Dim uiLevel As LogSeverity = LogSeverity.Info
            [Enum].TryParse(If(row.Cells(2).Value?.ToString(), "Info"), fileLevel)
            [Enum].TryParse(If(row.Cells(3).Value?.ToString(), "Info"), uiLevel)
            cfg.Routes(cat) = New LogCategoryRouting With {
                .Enabled = enabled,
                .FileLevel = fileLevel,
                .UiLevel = uiLevel
            }
        Next
        Routing = cfg
    End Sub

End Class
