Imports System.Threading
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Scheduling

Namespace Services.Interfaces
    ''' <summary>
    ''' Translation orchestrator — selects the active backend, handles fallback,
    ''' and applies per-language overrides and glossary post-processing.
    ''' Requests are scheduled through a priority queue when multiple callers compete.
    ''' </summary>
    Public Interface ITranslationService
        Function TranslateAsync(text As String,
                                sourceLang As String,
                                targetLangs As IReadOnlyList(Of String),
                                ct As CancellationToken,
                                Optional priority As TranslationPriority = TranslationPriority.Workspace,
                                Optional noCache As Boolean = False,
                                Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))

        ReadOnly Property ActiveBackend As String
        ReadOnly Property FallbackBackend As String
        ReadOnly Property TranslationQueueMetrics As QueueMetrics
        Function GetAllBackends() As IReadOnlyList(Of BackendInfo)
        Sub SetActiveBackend(name As String)
    End Interface
End Namespace
