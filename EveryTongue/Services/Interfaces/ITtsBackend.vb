Imports System.Threading
Imports EveryTongue.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' A pluggable TTS backend (Piper, MMS-TTS, Edge TTS).
    ''' Priority determines fallback order (lower = preferred).
    ''' </summary>
    Public Interface ITtsBackend
        ReadOnly Property Name As String
        ReadOnly Property RequiresInternet As Boolean
        ReadOnly Property Priority As Integer

        Function SynthesiseAsync(text As String,
                                 language As String,
                                 ct As CancellationToken
        ) As Task(Of TtsResult)

        Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String))

        Function IsLanguageSupportedAsync(language As String,
                                          ct As CancellationToken
        ) As Task(Of Boolean)

        Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
    End Interface
End Namespace
