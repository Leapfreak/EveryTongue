Imports System.Threading
Imports TranscriptionTools.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' Translation orchestrator — selects the active backend, handles fallback,
    ''' and applies per-language overrides and glossary post-processing.
    ''' </summary>
    Public Interface ITranslationService
        Function TranslateAsync(text As String,
                                sourceLang As String,
                                targetLangs As IReadOnlyList(Of String),
                                ct As CancellationToken
        ) As Task(Of Dictionary(Of String, String))

        ReadOnly Property ActiveBackend As String
        ReadOnly Property FallbackBackend As String
        Function GetAllBackends() As IReadOnlyList(Of BackendInfo)
        Sub SetActiveBackend(name As String)
    End Interface
End Namespace
