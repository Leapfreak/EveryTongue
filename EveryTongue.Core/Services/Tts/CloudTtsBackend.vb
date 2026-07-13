Imports System.Net.Http
Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Base class for key-requiring cloud TTS backends (Azure AI Speech,
    ''' Google Cloud TTS, OpenAI TTS). Provides shared HttpClient and
    ''' key/endpoint configuration. Mirrors CloudTranslationBackend.
    ''' Keys are pushed in via TtsBackendRegistry.ConfigureCloudTtsKeys at
    ''' server start and on Options save — never logged.
    ''' </summary>
    Public MustInherit Class CloudTtsBackend
        Implements ITtsBackend

        Protected ReadOnly HttpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(30)
        }

        Protected Property ApiKey As String = ""
        Protected Property Endpoint As String = ""

        ''' <summary>
        ''' Backend name. Lowercased it MUST equal the TtsBackendRegistry key —
        ''' the orchestrator's preference filter and ConfigureCloudTtsKeys both
        ''' match on Name case-insensitively (e.g. "Azure-TTS" ↔ "azure-tts").
        ''' </summary>
        Public MustOverride ReadOnly Property Name As String Implements ITtsBackend.Name

        Public MustOverride ReadOnly Property Priority As Integer Implements ITtsBackend.Priority

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return True
            End Get
        End Property

        Protected ReadOnly Property IsAvailable As Boolean
            Get
                Return Not String.IsNullOrEmpty(ApiKey)
            End Get
        End Property

        Public Overridable Sub Configure(apiKey As String)
            Me.ApiKey = If(apiKey, "")
        End Sub

        ''' <summary>
        ''' Push the per-engine endpoint (URL, or region name for engines that
        ''' use regions — Azure stores its Speech region here). No-op for
        ''' engines with a fixed endpoint.
        ''' </summary>
        Public Overridable Sub ConfigureEndpoint(url As String)
            Me.Endpoint = If(url, "").Trim().TrimEnd("/"c)
        End Sub

        Public MustOverride Function SynthesiseAsync(text As String,
                                                     language As String,
                                                     ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync

        Public MustOverride Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync

        Public Overridable Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            ' Broad coverage — same convention as EdgeTtsBackend (empty = no fixed list)
            Return Task.FromResult(DirectCast(New List(Of String)(), IReadOnlyList(Of String)))
        End Function

        Public Overridable Function IsLanguageSupportedAsync(language As String,
                                                             ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            ' Cloud engines have broad coverage; without a key the engine can't
            ' synthesise, so report unsupported and let the orchestrator fall
            ' through to the next preference cleanly.
            Return Task.FromResult(IsAvailable)
        End Function
    End Class
End Namespace
