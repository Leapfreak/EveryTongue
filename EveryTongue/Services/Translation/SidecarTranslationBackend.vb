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
        Private ReadOnly _name As String

        ''' <summary>The offline engine key this sidecar runs (e.g. nllb-3.3b); "" for the legacy default.</summary>
        Public ReadOnly Property EngineKey As String

        ''' <param name="name">Orchestrator backend name. "Local" = the global-default offline model;
        ''' per-model sidecars use a distinct name (e.g. "Local#&lt;sig&gt;") so rooms route to a specific model.</param>
        Public Sub New(legacyService As TranslationService, Optional name As String = "Local", Optional engineKey As String = "")
            _legacyService = legacyService
            _name = If(String.IsNullOrEmpty(name), "Local", name)
            EngineKey = If(engineKey, "")
        End Sub

        Public ReadOnly Property Name As String Implements ITranslationBackend.Name
            Get
                Return _name
            End Get
        End Property

        ''' <summary>The underlying sidecar service (used by the pool for lifecycle management).</summary>
        Public ReadOnly Property Service As TranslationService
            Get
                Return _legacyService
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

        ''' <summary>The Python sidecar applies glossary/profanity filters itself.</summary>
        Public ReadOnly Property AppliesFiltersInternally As Boolean Implements ITranslationBackend.AppliesFiltersInternally
            Get
                Return True
            End Get
        End Property

        Public Async Function TranslateAsync(text As String,
                                             sourceLang As String,
                                             targetLangs As IReadOnlyList(Of String),
                                             ct As CancellationToken,
                                             Optional noCache As Boolean = False,
                                             Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationBackend.TranslateAsync
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            ' Delegate to existing service (it uses its own internal timeout)
            Return Await _legacyService.TranslateAsync(
                text, sourceLang, New List(Of String)(targetLangs), noCache,
                glossaryPath:=If(filters?.GlossaryPath, ""),
                profanityPath:=If(filters?.ProfanityPath, ""))
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
