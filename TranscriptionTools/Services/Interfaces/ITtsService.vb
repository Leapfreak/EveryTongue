Imports System.Threading
Imports TranscriptionTools.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' TTS orchestrator — selects the best backend for the language,
    ''' caches audio per commit, and serves via Kestrel static paths.
    ''' </summary>
    Public Interface ITtsService
        Function SynthesiseAsync(text As String,
                                 language As String,
                                 commitId As Integer,
                                 ct As CancellationToken
        ) As Task(Of String)

        Function GetAvailableVoicesAsync(language As String,
                                         ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of TtsVoiceInfo))

        ReadOnly Property CacheStats As TtsCacheStats
    End Interface
End Namespace
