Imports System.Diagnostics
Imports System.Threading
Imports TranscriptionTools.Services.Interfaces

Namespace Services.Infrastructure
    ''' <summary>
    ''' Collects runtime metrics for the /api/metrics endpoint and threshold alerting.
    ''' Thread-safe via Interlocked operations.
    ''' </summary>
    Public Class MetricsService
        Implements IMetricsService

        Private ReadOnly _startTime As DateTime = DateTime.UtcNow
        Private _messagesSent As Long = 0
        Private _messagesDropped As Long = 0
        Private _lastBroadcastLatencyMs As Double = 0
        Private _translationChars As Long = 0
        Private _lastTranslationLatencyMs As Double = 0
        Private _lastTranslationBackend As String = ""
        Private _connectedClients As Integer = 0
        Private _clientsByLang As New Concurrent.ConcurrentDictionary(Of String, Integer)()

        Public ReadOnly Property ConnectedClients As Integer Implements IMetricsService.ConnectedClients
            Get
                Return _connectedClients
            End Get
        End Property

        Public ReadOnly Property ClientsByLanguage As IReadOnlyDictionary(Of String, Integer) Implements IMetricsService.ClientsByLanguage
            Get
                Return _clientsByLang
            End Get
        End Property

        Public ReadOnly Property MessagesSent As Long Implements IMetricsService.MessagesSent
            Get
                Return _messagesSent
            End Get
        End Property

        Public ReadOnly Property MessagesDropped As Long Implements IMetricsService.MessagesDropped
            Get
                Return _messagesDropped
            End Get
        End Property

        Public ReadOnly Property BroadcastLatencyMs As Double Implements IMetricsService.BroadcastLatencyMs
            Get
                Return _lastBroadcastLatencyMs
            End Get
        End Property

        Public ReadOnly Property UptimeSeconds As Long Implements IMetricsService.UptimeSeconds
            Get
                Return CLng((DateTime.UtcNow - _startTime).TotalSeconds)
            End Get
        End Property

        Public Sub RecordBroadcast(latencyMs As Double, sentCount As Integer, dropCount As Integer) Implements IMetricsService.RecordBroadcast
            _lastBroadcastLatencyMs = latencyMs
            Interlocked.Add(_messagesSent, sentCount)
            Interlocked.Add(_messagesDropped, dropCount)
        End Sub

        Public Sub RecordTranslation(backendName As String, latencyMs As Double, charCount As Integer) Implements IMetricsService.RecordTranslation
            _lastTranslationBackend = backendName
            _lastTranslationLatencyMs = latencyMs
            Interlocked.Add(_translationChars, charCount)
        End Sub

        Public Sub UpdateClientCount(connected As Integer, langCounts As Dictionary(Of String, Integer))
            _connectedClients = connected
            _clientsByLang.Clear()
            If langCounts IsNot Nothing Then
                For Each kvp In langCounts
                    _clientsByLang(kvp.Key) = kvp.Value
                Next
            End If
        End Sub

        Public Function GetSnapshot() As MetricsSnapshot Implements IMetricsService.GetSnapshot
            Dim proc = Process.GetCurrentProcess()

            Return New MetricsSnapshot With {
                .Clients = New ClientMetrics With {
                    .Connected = _connectedClients,
                    .ByLanguage = New Dictionary(Of String, Integer)(_clientsByLang)
                },
                .Broadcast = New BroadcastMetrics With {
                    .LatencyMs = _lastBroadcastLatencyMs,
                    .MessagesSent = _messagesSent,
                    .MessagesDropped = _messagesDropped
                },
                .Translation = New TranslationMetrics With {
                    .ActiveBackend = _lastTranslationBackend,
                    .LatencyMs = _lastTranslationLatencyMs,
                    .CharactersThisSession = _translationChars
                },
                .Tts = New TtsMetrics With {
                    .CacheHitRate = 0,
                    .GenerationsThisSession = 0,
                    .CacheSizeBytes = 0
                },
                .System = New SystemMetrics With {
                    .MemoryMB = proc.WorkingSet64 \ (1024 * 1024),
                    .UptimeSeconds = UptimeSeconds,
                    .Version = If(GetType(MetricsService).Assembly.GetName().Version?.ToString(), "unknown")
                }
            }
        End Function
    End Class
End Namespace
