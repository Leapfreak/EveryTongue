Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' TTS orchestrator — selects the best available backend for the requested language,
    ''' caches audio per commit, and returns URL paths for Kestrel to serve.
    ''' Tiered fallback: Piper (local, priority 1) -> MMS-TTS (local, priority 2) -> Edge TTS (cloud, priority 3).
    ''' </summary>
    Public Class TtsOrchestrator
        Implements ITtsService

        Private ReadOnly _backends As List(Of ITtsBackend)
        Private ReadOnly _cache As TtsCache
        Private ReadOnly _logger As ILogger(Of TtsOrchestrator)
        Private ReadOnly _preferredCodec As String = "mp3"
        Private ReadOnly _semaphore As New SemaphoreSlim(3, 3)

        Public Sub New(backends As IEnumerable(Of ITtsBackend),
                       cache As TtsCache,
                       logger As ILogger(Of TtsOrchestrator))
            ' Sort by priority (lowest first = preferred)
            _backends = backends.OrderBy(Function(b) b.Priority).ToList()
            _cache = cache
            _logger = logger
        End Sub

        Public ReadOnly Property CacheStats As TtsCacheStats Implements ITtsService.CacheStats
            Get
                Return _cache.GetStats()
            End Get
        End Property

        Public Async Function SynthesiseAsync(text As String,
                                              language As String,
                                              commitId As Integer,
                                              ct As CancellationToken
        ) As Task(Of String) Implements ITtsService.SynthesiseAsync

            ' Check cache first
            Dim cached = _cache.TryGet(language, commitId, _preferredCodec)
            If cached IsNot Nothing Then Return cached

            ' Limit concurrent TTS processes
            Await _semaphore.WaitAsync(ct)
            Try
                ' Re-check cache after acquiring semaphore (another task may have filled it)
                cached = _cache.TryGet(language, commitId, _preferredCodec)
                If cached IsNot Nothing Then Return cached

                ' Try each backend in priority order
                For Each backend In _backends
                    If ct.IsCancellationRequested Then Exit For

                    Try
                        Dim supported = Await backend.IsLanguageSupportedAsync(language, ct)
                        If Not supported Then Continue For

                        Dim result = Await backend.SynthesiseAsync(text, language, ct)
                        If result IsNot Nothing AndAlso result.AudioData IsNot Nothing Then
                            ' Store in cache and return URL
                            Dim url = _cache.Store(language, commitId,
                                result.AudioData, If(result.Codec, _preferredCodec))
                            _logger.LogInformation("TTS synthesised via {Backend} for {Language}, commit {Id}",
                                            backend.Name, language, commitId)
                            Return url
                        End If
                    Catch ex As OperationCanceledException
                        Throw
                    Catch ex As Exception
                        _logger.LogWarning(ex, "TTS backend {Backend} failed for {Language}",
                                          backend.Name, language)
                    End Try
                Next
            Finally
                _semaphore.Release()
            End Try

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
                Catch
                End Try
            Next
            Return voices
        End Function
    End Class
End Namespace
