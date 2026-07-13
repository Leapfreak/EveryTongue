Imports System.Threading
Imports EveryTongue.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' A pluggable translation backend (Local sidecar, DeepL, Google, Azure, etc.).
    ''' Each backend implements this interface and is registered in DI.
    ''' </summary>
    Public Interface ITranslationBackend
        ReadOnly Property Name As String
        ReadOnly Property RequiresInternet As Boolean
        ReadOnly Property IsAvailable As Boolean

        ''' <summary>
        ''' True when the backend applies glossary/profanity filters to its own
        ''' output (the local sidecar applies them inside translate-server/server.py).
        ''' When False, the orchestrator applies local glossary post-processing
        ''' (GlossaryPostProcessor) so cloud results get the same fixups as NLLB.
        ''' </summary>
        ReadOnly Property AppliesFiltersInternally As Boolean

        Function TranslateAsync(text As String,
                                sourceLang As String,
                                targetLangs As IReadOnlyList(Of String),
                                ct As CancellationToken,
                                Optional noCache As Boolean = False,
                                Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))

        Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))

        Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
    End Interface
End Namespace
