Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Translation
    ''' <summary>
    ''' Translation orchestrator — selects the active backend, handles automatic
    ''' fallback to NLLB when cloud backends fail, and applies per-language overrides.
    ''' </summary>
    Public Class TranslationOrchestrator
        Implements ITranslationService

        Private ReadOnly _backends As Dictionary(Of String, ITranslationBackend)
        Private ReadOnly _logger As ILogger(Of TranslationOrchestrator)
        Private _activeBackendName As String = "NLLB"
        Private _fallbackBackendName As String = "NLLB"

        ''' <summary>
        ''' Per-language backend overrides. Key = target language code, Value = backend name.
        ''' e.g. "fra_Latn" -> "DeepL" means use DeepL for French, regardless of active backend.
        ''' </summary>
        Public Property LanguageOverrides As New Dictionary(Of String, String)()

        Public Sub New(backends As IEnumerable(Of ITranslationBackend),
                       logger As ILogger(Of TranslationOrchestrator))
            _backends = New Dictionary(Of String, ITranslationBackend)(StringComparer.OrdinalIgnoreCase)
            For Each backend In backends
                _backends(backend.Name) = backend
            Next
            _logger = logger
        End Sub

        Public ReadOnly Property ActiveBackend As String Implements ITranslationService.ActiveBackend
            Get
                Return _activeBackendName
            End Get
        End Property

        Public ReadOnly Property FallbackBackend As String Implements ITranslationService.FallbackBackend
            Get
                Return _fallbackBackendName
            End Get
        End Property

        Public Sub SetActiveBackend(name As String) Implements ITranslationService.SetActiveBackend
            If _backends.ContainsKey(name) Then
                _activeBackendName = name
                _logger.LogInformation("Active translation backend set to {Backend}", name)
            End If
        End Sub

        Public Function GetAllBackends() As IReadOnlyList(Of BackendInfo) Implements ITranslationService.GetAllBackends
            Return _backends.Values.Select(Function(b) New BackendInfo With {
                .Name = b.Name,
                .RequiresInternet = b.RequiresInternet,
                .IsAvailable = b.IsAvailable,
                .IsActive = b.Name.Equals(_activeBackendName, StringComparison.OrdinalIgnoreCase)
            }).ToList()
        End Function

        Public Async Function TranslateAsync(text As String,
                                             sourceLang As String,
                                             targetLangs As IReadOnlyList(Of String),
                                             ct As CancellationToken
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationService.TranslateAsync

            Dim results As New Dictionary(Of String, String)()
            If targetLangs.Count = 0 Then Return results

            ' Group languages by backend (respecting per-language overrides)
            Dim groups As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
            For Each lang In targetLangs
                Dim backendName = _activeBackendName
                Dim overrideName As String = Nothing
                If LanguageOverrides.TryGetValue(lang, overrideName) Then
                    backendName = overrideName
                End If
                If Not groups.ContainsKey(backendName) Then
                    groups(backendName) = New List(Of String)()
                End If
                groups(backendName).Add(lang)
            Next

            ' Translate each group with its assigned backend (with fallback)
            For Each group In groups
                Dim backendName = group.Key
                Dim langs = group.Value

                Dim translated = Await TryTranslateWithFallback(
                    text, sourceLang, langs, backendName, ct)

                For Each kvp In translated
                    results(kvp.Key) = kvp.Value
                Next
            Next

            Return results
        End Function

        Private Async Function TryTranslateWithFallback(
            text As String,
            sourceLang As String,
            targetLangs As List(Of String),
            primaryBackend As String,
            ct As CancellationToken
        ) As Task(Of Dictionary(Of String, String))

            ' Try primary backend
            Dim backend As ITranslationBackend = Nothing
            If _backends.TryGetValue(primaryBackend, backend) AndAlso backend.IsAvailable Then
                Try
                    Dim result = Await backend.TranslateAsync(text, sourceLang, targetLangs, ct)
                    If result.Count > 0 Then Return result
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    _logger.LogWarning(ex, "Translation backend {Backend} failed, falling back",
                                      primaryBackend)
                End Try
            End If

            ' Fall back to NLLB if primary was different
            If Not primaryBackend.Equals(_fallbackBackendName, StringComparison.OrdinalIgnoreCase) Then
                If _backends.TryGetValue(_fallbackBackendName, backend) AndAlso backend.IsAvailable Then
                    Try
                        Return Await backend.TranslateAsync(text, sourceLang, targetLangs, ct)
                    Catch ex As OperationCanceledException
                        Throw
                    Catch ex As Exception
                        _logger.LogWarning(ex, "Fallback backend {Backend} also failed",
                                          _fallbackBackendName)
                    End Try
                End If
            End If

            Return New Dictionary(Of String, String)()
        End Function
    End Class
End Namespace
