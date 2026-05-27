Imports System.Threading
Imports EveryTongue.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' A pluggable translation backend (NLLB, DeepL, Google, Azure, etc.).
    ''' Each backend implements this interface and is registered in DI.
    ''' </summary>
    Public Interface ITranslationBackend
        ReadOnly Property Name As String
        ReadOnly Property RequiresInternet As Boolean
        ReadOnly Property IsAvailable As Boolean

        Function TranslateAsync(text As String,
                                sourceLang As String,
                                targetLangs As IReadOnlyList(Of String),
                                ct As CancellationToken
        ) As Task(Of Dictionary(Of String, String))

        Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))

        Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
    End Interface
End Namespace
