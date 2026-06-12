Imports System.Threading
Imports Microsoft.Extensions.Logging.Abstractions
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces

Namespace Services.Audio

    ''' <summary>
    ''' Desktop-side TTS playback: synthesises text through the server's
    ''' ITtsService (Kestrel must be running) and plays the cached audio on the
    ''' default output device. Shared by the Translate and Bible workspaces.
    ''' </summary>
    Friend Class DesktopTtsPlayer

        Private ReadOnly _getTtsService As Func(Of ITtsService)
        Private ReadOnly _getCacheDir As Func(Of String)
        Private _output As TtsAudioOutput
        Private _cts As CancellationTokenSource

        Public Sub New(getTtsService As Func(Of ITtsService), getCacheDir As Func(Of String))
            _getTtsService = getTtsService
            _getCacheDir = getCacheDir
        End Sub

        ''' <summary>True when the server is running and TTS can be used.</summary>
        Public ReadOnly Property IsAvailable As Boolean
            Get
                Return _getTtsService() IsNot Nothing AndAlso Not String.IsNullOrEmpty(_getCacheDir())
            End Get
        End Property

        ''' <summary>
        ''' Synthesise and play a sequence of texts in order. Cancels any playback
        ''' already in progress. Returns the number of items successfully queued.
        ''' </summary>
        Public Async Function SpeakAsync(texts As IEnumerable(Of String), iso3Lang As String) As Task(Of Integer)
            Dim svc = _getTtsService()
            Dim cacheDir = _getCacheDir()
            If svc Is Nothing OrElse String.IsNullOrEmpty(cacheDir) Then Return 0

            StopPlayback()
            _cts = New CancellationTokenSource()
            Dim ct = _cts.Token
            _output = New TtsAudioOutput(NullLogger(Of TtsAudioOutput).Instance)
            _output.Start()

            Dim queued = 0
            For Each item In texts
                If ct.IsCancellationRequested Then Exit For
                Dim trimmed = If(item, "").Trim()
                If trimmed.Length = 0 Then Continue For
                Try
                    Dim url = Await svc.SynthesiseAsync(trimmed, iso3Lang, -StableHash(trimmed), ct)
                    If Not String.IsNullOrEmpty(url) AndAlso Not ct.IsCancellationRequested Then
                        _output?.EnqueueFromUrl(url, cacheDir)
                        queued += 1
                    End If
                Catch ex As OperationCanceledException
                    Exit For
                Catch ex As Exception
                    AppLogger.Log(LogEvents.AUDIO_PLAYBACK_ERROR, $"DesktopTtsPlayer: synthesis failed ({iso3Lang}): {ex.Message}")
                End Try
            Next
            Return queued
        End Function

        ''' <summary>Stop playback and cancel any in-flight synthesis.</summary>
        Public Sub StopPlayback()
            Try : _cts?.Cancel() : Catch : End Try
            ' TtsAudioOutput's queue can't be reused after Stop — dispose and recreate per session.
            Try : _output?.Dispose() : Catch : End Try
            _output = Nothing
        End Sub

        ''' <summary>Deterministic hash so repeated reads of the same text hit the TTS cache.</summary>
        Private Shared Function StableHash(text As String) As Integer
            Dim hash As Integer = 23
            For Each c In text
                hash = (hash * 31 + AscW(c)) And &H7FFFFF
            Next
            Return hash
        End Function

    End Class

End Namespace
