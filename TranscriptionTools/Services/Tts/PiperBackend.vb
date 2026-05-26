Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Piper TTS backend — high-quality local ONNX-based speech synthesis.
    ''' Priority 1 (preferred). ~30 European languages, ~15-50MB per voice.
    ''' Runs as a Python sidecar or direct ONNX inference.
    ''' </summary>
    Public Class PiperBackend
        Implements ITtsBackend

        Private ReadOnly _modelsDir As String
        Private _supportedLangs As List(Of String)

        Public Sub New()
            _modelsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tts-models", "piper")
        End Sub

        Public ReadOnly Property Name As String Implements ITtsBackend.Name
            Get
                Return "Piper"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITtsBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property Priority As Integer Implements ITtsBackend.Priority
            Get
                Return 1
            End Get
        End Property

        Public Function SynthesiseAsync(text As String, language As String,
                                        ct As CancellationToken
        ) As Task(Of TtsResult) Implements ITtsBackend.SynthesiseAsync
            ' TODO: Implement Piper synthesis (Python sidecar or ONNX)
            Return Task.FromResult(DirectCast(Nothing, TtsResult))
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of String)) Implements ITtsBackend.GetSupportedLanguagesAsync
            If _supportedLangs Is Nothing Then
                _supportedLangs = New List(Of String)()
                If Directory.Exists(_modelsDir) Then
                    For Each langDir In Directory.GetDirectories(_modelsDir)
                        _supportedLangs.Add(Path.GetFileName(langDir))
                    Next
                End If
            End If
            Return Task.FromResult(DirectCast(_supportedLangs, IReadOnlyList(Of String)))
        End Function

        Public Async Function IsLanguageSupportedAsync(language As String,
                                                       ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.IsLanguageSupportedAsync
            Dim langs = Await GetSupportedLanguagesAsync(ct)
            Return langs.Contains(language)
        End Function

        Public Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITtsBackend.CheckHealthAsync
            Return Task.FromResult(Directory.Exists(_modelsDir))
        End Function
    End Class
End Namespace
