Imports System.Threading
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Scheduling

Namespace Services.Interfaces
    ''' <summary>
    ''' TTS orchestrator — selects the best backend for the language,
    ''' caches audio per commit, and serves via Kestrel static paths.
    ''' Requests are scheduled through a priority queue under load.
    ''' </summary>
    Public Interface ITtsService
        Function SynthesiseAsync(text As String,
                                 language As String,
                                 commitId As Integer,
                                 ct As CancellationToken,
                                 Optional priority As TranslationPriority = TranslationPriority.Workspace
        ) As Task(Of String)

        Function GetAvailableVoicesAsync(language As String,
                                         ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of TtsVoiceInfo))

        ReadOnly Property CacheStats As TtsCacheStats
        ReadOnly Property TtsQueueMetrics As QueueMetrics
    End Interface
End Namespace
