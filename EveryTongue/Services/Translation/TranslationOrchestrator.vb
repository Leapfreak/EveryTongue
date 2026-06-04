Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Scheduling

Namespace Services.Translation
    ''' <summary>
    ''' Translation orchestrator — selects the active backend, handles automatic
    ''' fallback to local sidecar when cloud backends fail, and applies per-language overrides.
    ''' Requests are scheduled through a priority queue with bounded concurrency
    ''' to prevent overwhelming the translation backend under multi-room load.
    ''' </summary>
    Public Class TranslationOrchestrator
        Implements ITranslationService

        Private ReadOnly _backends As Dictionary(Of String, ITranslationBackend)
        Private ReadOnly _logger As ILogger(Of TranslationOrchestrator)
        Private ReadOnly _queue As PriorityWorkQueue(Of Dictionary(Of String, String))
        Private _activeBackendName As String = "Local"
        Private _fallbackBackendName As String = "Local"
        Private _latencyProfile As LatencyProfile

        ''' <summary>
        ''' Per-language backend overrides. Key = target language code, Value = backend name.
        ''' e.g. "fra_Latn" -> "DeepL" means use DeepL for French, regardless of active backend.
        ''' </summary>
        Public Property LanguageOverrides As New Dictionary(Of String, String)()

        ''' <summary>
        ''' Latency profile from the most recent benchmark run.
        ''' Used for estimating completion times and detecting performance degradation.
        ''' </summary>
        Public ReadOnly Property LatencyProfile As LatencyProfile
            Get
                Return _latencyProfile
            End Get
        End Property

        Public Sub New(backends As IEnumerable(Of ITranslationBackend),
                       logger As ILogger(Of TranslationOrchestrator),
                       options As Server.ServerOptions)
            _backends = New Dictionary(Of String, ITranslationBackend)(StringComparer.OrdinalIgnoreCase)
            For Each backend In backends
                _backends(backend.Name) = backend
            Next
            _logger = logger
            Dim concurrency = Math.Max(1, If(options?.TranslationConcurrency, 3))
            _queue = New PriorityWorkQueue(Of Dictionary(Of String, String))(
                concurrency, starvationMs:=5000)

            ' Load saved latency profile from previous benchmark run
            _latencyProfile = LatencyProfile.Load()
            If _latencyProfile IsNot Nothing Then
                _logger.LogInformation(
                    "Loaded latency profile from {Timestamp}: {Pairs} language pairs, avg {AvgMs:F0}ms",
                    _latencyProfile.Timestamp, _latencyProfile.Pairs.Count,
                    _latencyProfile.OverallAvgLatencyMs)
            End If
        End Sub

        ''' <summary>
        ''' Reload the latency profile from disk (called after a benchmark run completes).
        ''' </summary>
        Public Sub ReloadLatencyProfile()
            _latencyProfile = LatencyProfile.Load()
            If _latencyProfile IsNot Nothing Then
                _logger.LogInformation(
                    "Reloaded latency profile: {Pairs} pairs, avg {AvgMs:F0}ms, {ReqSec} req/s",
                    _latencyProfile.Pairs.Count, _latencyProfile.OverallAvgLatencyMs,
                    _latencyProfile.OverallReqPerSec)
            End If
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

        ''' <summary>
        ''' Register a backend dynamically (e.g. SidecarTranslationBackend after the translation sidecar starts).
        ''' Overwrites any existing backend with the same name.
        ''' </summary>
        Public Sub RegisterBackend(backend As ITranslationBackend)
            _backends(backend.Name) = backend
            _logger.LogInformation("Registered translation backend: {Backend}", backend.Name)
        End Sub

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

        Public ReadOnly Property TranslationQueueMetrics As QueueMetrics Implements ITranslationService.TranslationQueueMetrics
            Get
                Return _queue.GetMetrics()
            End Get
        End Property

        Public Async Function TranslateAsync(text As String,
                                             sourceLang As String,
                                             targetLangs As IReadOnlyList(Of String),
                                             ct As CancellationToken,
                                             Optional priority As TranslationPriority = TranslationPriority.Workspace,
                                             Optional noCache As Boolean = False
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationService.TranslateAsync

            If targetLangs.Count = 0 Then Return New Dictionary(Of String, String)()

            Dim skipCache = noCache
            ' Route through the priority queue — the queue gates concurrency
            ' so the translation backend isn't overwhelmed under multi-room load.
            Return Await _queue.EnqueueAsync(
                Async Function(ct2)
                    Return Await TranslateInternal(text, sourceLang, targetLangs, ct2, skipCache)
                End Function,
                CInt(priority),
                ct)
        End Function

        ''' <summary>
        ''' Actual translation logic — groups languages by backend and translates with fallback.
        ''' Called from within the priority queue's concurrency slot.
        ''' </summary>
        Private Async Function TranslateInternal(text As String,
                                                  sourceLang As String,
                                                  targetLangs As IReadOnlyList(Of String),
                                                  ct As CancellationToken,
                                                  noCache As Boolean
        ) As Task(Of Dictionary(Of String, String))

            Dim results As New Dictionary(Of String, String)()

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
                    text, sourceLang, langs, backendName, ct, noCache)

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
            ct As CancellationToken,
            noCache As Boolean
        ) As Task(Of Dictionary(Of String, String))

            ' Try primary backend
            Dim backend As ITranslationBackend = Nothing
            If _backends.TryGetValue(primaryBackend, backend) AndAlso backend.IsAvailable Then
                Try
                    Dim result = Await backend.TranslateAsync(text, sourceLang, targetLangs, ct, noCache)
                    If result.Count > 0 Then Return result
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    _logger.LogWarning(ex, "Translation backend {Backend} failed, falling back",
                                      primaryBackend)
                End Try
            End If

            ' Fall back to local sidecar if primary was different
            If Not primaryBackend.Equals(_fallbackBackendName, StringComparison.OrdinalIgnoreCase) Then
                If _backends.TryGetValue(_fallbackBackendName, backend) AndAlso backend.IsAvailable Then
                    Try
                        Return Await backend.TranslateAsync(text, sourceLang, targetLangs, ct, noCache)
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
