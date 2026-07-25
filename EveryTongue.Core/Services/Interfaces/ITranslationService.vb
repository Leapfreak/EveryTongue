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
        ''' <param name="backendOverride">
        ''' Optional backend NAME (e.g. "Google", "Local") that forces ALL target
        ''' languages through that single backend for this call, overriding the
        ''' active backend and per-language overrides. Used by conference rooms to
        ''' translate with their template's own engine. Nothing/empty = default routing.
        ''' </param>
        ''' <param name="noPivot">
        ''' True forces DIRECT translation, bypassing the English-pivot policy.
        ''' Used by the benchmark's direct-vs-pivot A/B (a clean baseline needs a
        ''' path the policy can't reroute). Production callers leave this False.
        ''' </param>
        Function TranslateAsync(text As String,
                                sourceLang As String,
                                targetLangs As IReadOnlyList(Of String),
                                ct As CancellationToken,
                                Optional priority As TranslationPriority = TranslationPriority.Workspace,
                                Optional noCache As Boolean = False,
                                Optional filters As TranslationFilterPaths = Nothing,
                                Optional backendOverride As String = Nothing,
                                Optional noPivot As Boolean = False
        ) As Task(Of Dictionary(Of String, String))

        ReadOnly Property ActiveBackend As String
        ReadOnly Property FallbackBackend As String
        ReadOnly Property TranslationQueueMetrics As QueueMetrics
        Function GetAllBackends() As IReadOnlyList(Of BackendInfo)
        Sub SetActiveBackend(name As String)

        ''' <summary>
        ''' Explain — without translating — how each target would be routed right
        ''' now (backend, direct vs English-pivot, reason). Backs the
        ''' /api/translation/routing endpoint and any UI routing preview.
        ''' </summary>
        Function DescribeRouting(sourceLang As String,
                                 targetLangs As IReadOnlyList(Of String)) As IReadOnlyList(Of RouteInfo)
    End Interface
End Namespace
