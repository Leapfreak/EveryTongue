Namespace Services.Interfaces
    ''' <summary>
    ''' Collects and exposes runtime metrics for monitoring and alerting.
    ''' </summary>
    Public Interface IMetricsService
        ReadOnly Property ConnectedClients As Integer
        ReadOnly Property ClientsByLanguage As IReadOnlyDictionary(Of String, Integer)
        ReadOnly Property MessagesSent As Long
        ReadOnly Property MessagesDropped As Long
        ReadOnly Property BroadcastLatencyMs As Double
        ReadOnly Property UptimeSeconds As Long

        Sub RecordBroadcast(latencyMs As Double, sentCount As Integer, dropCount As Integer)
        Sub RecordTranslation(backendName As String, latencyMs As Double, charCount As Integer)

        Function GetSnapshot() As MetricsSnapshot
    End Interface

    ''' <summary>
    ''' Point-in-time snapshot of all server metrics.
    ''' </summary>
    Public Class MetricsSnapshot
        Public Property Clients As ClientMetrics
        Public Property Broadcast As BroadcastMetrics
        Public Property Translation As TranslationMetrics
        Public Property Tts As TtsMetrics
        Public Property System As SystemMetrics
    End Class

    Public Class ClientMetrics
        Public Property Connected As Integer
        Public Property ByLanguage As Dictionary(Of String, Integer)
    End Class

    Public Class BroadcastMetrics
        Public Property LatencyMs As Double
        Public Property MessagesSent As Long
        Public Property MessagesDropped As Long
    End Class

    Public Class TranslationMetrics
        Public Property ActiveBackend As String
        Public Property LatencyMs As Double
        Public Property CharactersThisSession As Long
    End Class

    Public Class TtsMetrics
        Public Property CacheHitRate As Double
        Public Property GenerationsThisSession As Integer
        Public Property CacheSizeBytes As Long
    End Class

    Public Class SystemMetrics
        Public Property CpuPercent As Double
        Public Property MemoryMB As Long
        Public Property UptimeSeconds As Long
        Public Property Version As String
    End Class
End Namespace
