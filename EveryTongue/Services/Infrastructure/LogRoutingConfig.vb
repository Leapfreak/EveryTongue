Namespace Services.Infrastructure

    ''' <summary>
    ''' Per-category routing: minimum level for file output and UI output.
    ''' </summary>
    Public Class LogCategoryRouting
        Public Property FileLevel As LogSeverity = LogSeverity.Debug
        Public Property UiLevel As LogSeverity = LogSeverity.Info
        Public Property Enabled As Boolean = True
    End Class

    ''' <summary>
    ''' Complete log routing configuration. Stored in AppConfig.
    ''' </summary>
    Public Class LogRoutingConfig
        Public Property Routes As New Dictionary(Of LogCategory, LogCategoryRouting)

        ''' <summary>
        ''' Check if an event should be written to the log file.
        ''' </summary>
        Public Function ShouldLogToFile(category As LogCategory, level As LogSeverity) As Boolean
            Dim route As LogCategoryRouting = Nothing
            If Not Routes.TryGetValue(category, route) Then Return level >= LogSeverity.Info
            Return route.Enabled AndAlso level >= route.FileLevel
        End Function

        ''' <summary>
        ''' Check if an event should be shown in the UI.
        ''' </summary>
        Public Function ShouldLogToUi(category As LogCategory, level As LogSeverity) As Boolean
            Dim route As LogCategoryRouting = Nothing
            If Not Routes.TryGetValue(category, route) Then Return level >= LogSeverity.Info
            Return route.Enabled AndAlso level >= route.UiLevel
        End Function

        ' ── Presets ──

        Public Shared Function CreateMinimal() As LogRoutingConfig
            Dim cfg As New LogRoutingConfig()
            For Each cat In [Enum].GetValues(GetType(LogCategory)).Cast(Of LogCategory)()
                cfg.Routes(cat) = New LogCategoryRouting With {
                    .FileLevel = LogSeverity.Warning,
                    .UiLevel = LogSeverity.[Error],
                    .Enabled = True
                }
            Next
            Return cfg
        End Function

        Public Shared Function CreateNormal() As LogRoutingConfig
            Dim cfg As New LogRoutingConfig()
            For Each cat In [Enum].GetValues(GetType(LogCategory)).Cast(Of LogCategory)()
                cfg.Routes(cat) = New LogCategoryRouting With {
                    .FileLevel = LogSeverity.Info,
                    .UiLevel = LogSeverity.Info,
                    .Enabled = True
                }
            Next
            ' Quiet down noisy categories in UI
            cfg.Routes(LogCategory.Benchmark).UiLevel = LogSeverity.Warning
            cfg.Routes(LogCategory.Hardware).UiLevel = LogSeverity.Warning
            cfg.Routes(LogCategory.Legacy).UiLevel = LogSeverity.Info
            Return cfg
        End Function

        Public Shared Function CreateVerbose() As LogRoutingConfig
            Dim cfg As New LogRoutingConfig()
            For Each cat In [Enum].GetValues(GetType(LogCategory)).Cast(Of LogCategory)()
                cfg.Routes(cat) = New LogCategoryRouting With {
                    .FileLevel = LogSeverity.Debug,
                    .UiLevel = LogSeverity.Debug,
                    .Enabled = True
                }
            Next
            Return cfg
        End Function

    End Class

End Namespace
