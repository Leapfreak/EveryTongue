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
        Private ReadOnly _globalGlossaryPath As String
        Private ReadOnly _globalProfanityPath As String

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
            _globalGlossaryPath = If(options?.GlossaryFilePath, "")
            _globalProfanityPath = If(options?.ProfanityFilePath, "")
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
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_BACKEND_ACTIVE,
                    $"Active translation backend → {name}")
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
                                             Optional noCache As Boolean = False,
                                             Optional filters As TranslationFilterPaths = Nothing,
                                             Optional backendOverride As String = Nothing
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationService.TranslateAsync

            If targetLangs.Count = 0 Then Return New Dictionary(Of String, String)()

            Dim skipCache = noCache
            Dim ovr = backendOverride
            ' Route through the priority queue — the queue gates concurrency
            ' so the translation backend isn't overwhelmed under multi-room load.
            Return Await _queue.EnqueueAsync(
                Async Function(ct2)
                    Return Await TranslateInternal(text, sourceLang, targetLangs, ct2, skipCache, filters, ovr)
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
                                                  noCache As Boolean,
                                                  filters As TranslationFilterPaths,
                                                  Optional backendOverride As String = Nothing
        ) As Task(Of Dictionary(Of String, String))

            Dim results As New Dictionary(Of String, String)()

            ' Group languages by backend.
            Dim groups As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)
            If Not String.IsNullOrWhiteSpace(backendOverride) AndAlso _backends.ContainsKey(backendOverride) Then
                ' Explicit per-call override (e.g. a conference room's own engine):
                ' route ALL targets through the one named backend, bypassing the
                ' active backend AND per-language overrides — the room's choice wins.
                ' The fallback chain in TryTranslateWithFallback still applies.
                groups(backendOverride) = targetLangs.ToList()
            Else
                ' Default routing: active backend, with per-language overrides applied.
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
            End If

            ' Translate each group with its assigned backend (with fallback)
            For Each group In groups
                Dim backendName = group.Key
                Dim langs = group.Value

                Dim translated = Await TryTranslateWithFallback(
                    text, sourceLang, langs, backendName, ct, noCache, filters)

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
            noCache As Boolean,
            filters As TranslationFilterPaths
        ) As Task(Of Dictionary(Of String, String))

            ' Try primary backend
            Dim backend As ITranslationBackend = Nothing
            If _backends.TryGetValue(primaryBackend, backend) AndAlso backend.IsAvailable Then
                Try
                    Dim result = Await InvokeBackendAsync(backend, text, sourceLang, targetLangs, ct, noCache, filters)
                    If result.Count > 0 Then Return ApplyLocalFilters(backend, text, sourceLang, result, filters)
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_BACKEND_FALLBACK,
                        $"backend={primaryBackend} failed for {sourceLang}→[{String.Join(",", targetLangs)}]: {ex.Message} — falling back to {_fallbackBackendName}")
                End Try
            ElseIf Not primaryBackend.Equals(_fallbackBackendName, StringComparison.OrdinalIgnoreCase) Then
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_BACKEND_FALLBACK,
                    $"backend={primaryBackend} unavailable for {sourceLang}→[{String.Join(",", targetLangs)}] — falling back to {_fallbackBackendName}")
            End If

            ' Fall back to local sidecar if primary was different
            If Not primaryBackend.Equals(_fallbackBackendName, StringComparison.OrdinalIgnoreCase) Then
                If _backends.TryGetValue(_fallbackBackendName, backend) AndAlso backend.IsAvailable Then
                    Try
                        Dim fallbackResult = Await InvokeBackendAsync(backend, text, sourceLang, targetLangs, ct, noCache, filters)
                        Return ApplyLocalFilters(backend, text, sourceLang, fallbackResult, filters)
                    Catch ex As OperationCanceledException
                        Throw
                    Catch ex As Exception
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                            $"fallback backend={_fallbackBackendName} also failed for {sourceLang}→[{String.Join(",", targetLangs)}]: {ex.Message}")
                    End Try
                End If
            End If

            Return New Dictionary(Of String, String)()
        End Function

        ''' <summary>
        ''' Invoke a backend, recording cost (characters submitted) and latency for
        ''' CLOUD backends (RequiresInternet). Vendors bill source characters per
        ''' target language, so a multi-target request counts text.Length × targets.
        ''' Usage is counted at submission; latency is recorded around the call.
        ''' The local sidecar is passed straight through (no vendor cost).
        ''' </summary>
        Private Async Function InvokeBackendAsync(backend As ITranslationBackend,
                                                  text As String,
                                                  sourceLang As String,
                                                  targetLangs As IReadOnlyList(Of String),
                                                  ct As CancellationToken,
                                                  noCache As Boolean,
                                                  filters As TranslationFilterPaths
        ) As Task(Of Dictionary(Of String, String))
            If Not backend.RequiresInternet Then
                Return Await backend.TranslateAsync(text, sourceLang, targetLangs, ct, noCache, filters)
            End If

            ' Map the orchestrator backend name to the registry engine key
            ' (registry metadata — no engine-key literals here).
            Dim usageKey = If(TranslationBackendRegistry.FindByBackendName(backend.Name)?.Key, backend.Name)
            TranslationUsageTracker.RecordUsage(usageKey, CLng(If(text, "").Length) * targetLangs.Count)

            Dim sw = Diagnostics.Stopwatch.StartNew()
            Try
                Return Await backend.TranslateAsync(text, sourceLang, targetLangs, ct, noCache, filters)
            Finally
                sw.Stop()
                TranslationUsageTracker.RecordLatency(usageKey, sw.ElapsedMilliseconds)
            End Try
        End Function

        ''' <summary>
        ''' Apply local glossary + profanity post-processing to a backend's output
        ''' when the backend does not run filters itself (cloud backends). The local
        ''' sidecar applies glossary/profanity in Python, so it is skipped here —
        ''' applying twice would corrupt its output. Per-request filter set wins;
        ''' otherwise the global files are used (same resolution as the sidecar,
        ''' and same order: glossary first, then profanity).
        ''' </summary>
        Private Function ApplyLocalFilters(backend As ITranslationBackend,
                                           sourceText As String,
                                           sourceLang As String,
                                           translations As Dictionary(Of String, String),
                                           filters As TranslationFilterPaths) As Dictionary(Of String, String)
            If backend.AppliesFiltersInternally Then Return translations
            Dim afterGlossary = GlossaryPostProcessor.Apply(
                sourceText, sourceLang, translations,
                If(filters?.GlossaryPath, ""), _globalGlossaryPath)
            Return ProfanityPostProcessor.Apply(
                afterGlossary, If(filters?.ProfanityPath, ""), _globalProfanityPath)
        End Function
    End Class
End Namespace
