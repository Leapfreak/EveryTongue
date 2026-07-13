Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Scheduling

Namespace Services.Tts
    ''' <summary>
    ''' TTS orchestrator — selects the best available backend for the requested language,
    ''' caches audio per commit, and returns URL paths for Kestrel to serve.
    ''' Tiered fallback: Piper (local, priority 1) -> MMS-TTS (local, priority 2) -> Edge TTS (cloud, priority 3).
    ''' Uses a priority queue to schedule synthesis requests under multi-room load.
    ''' </summary>
    Public Class TtsOrchestrator
        Implements ITtsService

        Private ReadOnly _backends As List(Of ITtsBackend)
        Private ReadOnly _cache As TtsCache
        Private ReadOnly _logger As ILogger(Of TtsOrchestrator)
        Private ReadOnly _preferredCodec As String = "mp3"
        Private ReadOnly _queue As PriorityWorkQueue(Of String)
        Private _preferredBackends As HashSet(Of String)

        Public Sub New(backends As IEnumerable(Of ITtsBackend),
                       cache As TtsCache,
                       logger As ILogger(Of TtsOrchestrator),
                       options As Server.ServerOptions)
            ' Sort by priority (lowest first = preferred)
            _backends = backends.OrderBy(Function(b) b.Priority).ToList()
            _cache = cache
            _logger = logger
            Dim concurrency = Math.Max(1, If(options?.TtsConcurrency, 3))
            _queue = New PriorityWorkQueue(Of String)(concurrency, starvationMs:=5000)
        End Sub

        ''' <summary>
        ''' Sets which backends to use. Empty/Nothing = all backends.
        ''' Values: "piper", "mms-tts", "edgetts" (case-insensitive).
        ''' </summary>
        Public Sub SetPreferredBackends(backendNames As String)
            If String.IsNullOrWhiteSpace(backendNames) Then
                _preferredBackends = Nothing
                _logger.LogInformation("TTS backend preference: all (fallback order)")
            Else
                _preferredBackends = New HashSet(Of String)(
                    backendNames.Split(","c).
                        Select(Function(s) s.Trim().ToLower()).
                        Where(Function(s) s.Length > 0),
                    StringComparer.OrdinalIgnoreCase)
                _logger.LogInformation("TTS backend preference: {Backends}", backendNames)
            End If
        End Sub

        Public ReadOnly Property CacheStats As TtsCacheStats Implements ITtsService.CacheStats
            Get
                Return _cache.GetStats()
            End Get
        End Property

        Public ReadOnly Property TtsQueueMetrics As QueueMetrics Implements ITtsService.TtsQueueMetrics
            Get
                Return _queue.GetMetrics()
            End Get
        End Property

        Public Async Function SynthesiseAsync(text As String,
                                              language As String,
                                              commitId As Integer,
                                              ct As CancellationToken,
                                              Optional priority As TranslationPriority = TranslationPriority.Workspace
        ) As Task(Of String) Implements ITtsService.SynthesiseAsync

            ' Check cache first (no need to queue for cache hits)
            Dim cached = _cache.TryGet(language, commitId)
            If cached IsNot Nothing Then Return cached

            ' Route through the priority queue for concurrency control
            Return Await _queue.EnqueueAsync(
                Async Function(ct2)
                    Return Await SynthesiseInternal(text, language, commitId, ct2)
                End Function,
                CInt(priority),
                ct)
        End Function

        ''' <summary>
        ''' Actual TTS synthesis logic — tries each backend in priority order.
        ''' Called from within the priority queue's concurrency slot.
        ''' </summary>
        Private Async Function SynthesiseInternal(text As String,
                                                   language As String,
                                                   commitId As Integer,
                                                   ct As CancellationToken
        ) As Task(Of String)

            ' Re-check cache (another task may have filled it while we waited in queue)
            Dim cached = _cache.TryGet(language, commitId)
            If cached IsNot Nothing Then Return cached

            ' Try each backend in priority order (filtered by preference)
            For Each backend In _backends
                If ct.IsCancellationRequested Then Exit For

                ' Skip backends not in the preferred list (if set)
                If _preferredBackends IsNot Nothing AndAlso _preferredBackends.Count > 0 Then
                    If Not _preferredBackends.Contains(backend.Name.ToLower()) Then
                        _logger.LogDebug("TTS skipping {Backend} (not in preferred list) for {Language}",
                            backend.Name, language)
                        Continue For
                    End If
                End If

                Try
                    Dim supported = Await backend.IsLanguageSupportedAsync(language, ct)
                    If Not supported Then
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_SYNTHESISE,
                            $"engine={backend.Name} lang={language} unsupported — trying next engine")
                        Continue For
                    End If

                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_SYNTHESISE,
                        $"engine={backend.Name} lang={language} commit={commitId} chars={If(text, "").Length}")
                    Dim sw = Diagnostics.Stopwatch.StartNew()
                    Dim result = Await backend.SynthesiseAsync(text, language, ct)
                    If result IsNot Nothing AndAlso result.AudioData IsNot Nothing Then
                        ' Store in cache and return URL
                        Dim url = _cache.Store(language, commitId,
                            result.AudioData, If(result.Codec, _preferredCodec))
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_SYNTHESISE_DONE,
                            $"engine={backend.Name} lang={language} commit={commitId} {sw.ElapsedMilliseconds}ms")
                        Return url
                    Else
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_ENGINE_ERROR,
                            $"engine={backend.Name} lang={language} returned no audio — trying next engine")
                    End If
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_ENGINE_ERROR,
                        $"engine={backend.Name} lang={language} failed: {ex.Message} — trying next engine")
                End Try
            Next

            ' No backend could synthesise — client falls back to browser speechSynthesis
            Return Nothing
        End Function

        Public Async Function GetAvailableVoicesAsync(language As String,
                                                       ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of TtsVoiceInfo)) Implements ITtsService.GetAvailableVoicesAsync
            Dim voices As New List(Of TtsVoiceInfo)()
            For Each backend In _backends
                Try
                    Dim supported = Await backend.IsLanguageSupportedAsync(language, ct)
                    If supported Then
                        voices.Add(New TtsVoiceInfo With {
                            .Id = $"{backend.Name}_{language}",
                            .Name = backend.Name,
                            .Language = language,
                            .Backend = backend.Name
                        })
                    End If
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TTS_ENGINE_ERROR, $"TtsOrchestrator.GetAvailableVoicesAsync: {ex.Message}")
                End Try
            Next
            Return voices
        End Function
    End Class
End Namespace
