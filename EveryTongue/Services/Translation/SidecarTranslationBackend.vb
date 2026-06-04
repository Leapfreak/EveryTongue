Imports System.Threading
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation
    ''' <summary>
    ''' Wraps the existing TranslationService (local Python sidecar) as an ITranslationBackend.
    ''' Always available offline. Used as the default fallback in the orchestrator.
    ''' </summary>
    Public Class SidecarTranslationBackend
        Implements ITranslationBackend

        Private ReadOnly _legacyService As TranslationService

        Public Sub New(legacyService As TranslationService)
            _legacyService = legacyService
        End Sub

        Public ReadOnly Property Name As String Implements ITranslationBackend.Name
            Get
                Return "Local"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITranslationBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsAvailable As Boolean Implements ITranslationBackend.IsAvailable
            Get
                Return _legacyService.IsRunning AndAlso _legacyService.IsModelLoaded
            End Get
        End Property

        Public Async Function TranslateAsync(text As String,
                                             sourceLang As String,
                                             targetLangs As IReadOnlyList(Of String),
                                             ct As CancellationToken,
                                             Optional noCache As Boolean = False
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationBackend.TranslateAsync
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            ' Delegate to existing service (it uses its own internal timeout)
            Return Await _legacyService.TranslateAsync(
                text, sourceLang, New List(Of String)(targetLangs), noCache)
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo)) Implements ITranslationBackend.GetSupportedLanguagesAsync
            ' Convert the existing lang map to LanguageInfo list
            Dim result As New List(Of LanguageInfo)()
            For Each kvp In TranslationService.GetLangMap()
                result.Add(New LanguageInfo With {
                    .WhisperCode = kvp.Key,
                    .FloresCode = kvp.Value
                })
            Next
            Return Task.FromResult(DirectCast(result, IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean) Implements ITranslationBackend.CheckHealthAsync
            Return Task.FromResult(IsAvailable)
        End Function
    End Class
End Namespace
