Imports System.Collections.Concurrent
Imports System.IO
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports NAudio.Wave

Namespace Services.Audio
    ''' <summary>
    ''' Plays TTS audio files to a selected Windows audio output device.
    ''' Used for routing TTS to church PA/NDI via a Virtual Audio Cable or
    ''' directly to a physical output device.
    ''' </summary>
    Public Class TtsAudioOutput
        Implements IDisposable

        Private ReadOnly _logger As ILogger(Of TtsAudioOutput)
        Private ReadOnly _queue As New BlockingCollection(Of String)(50)
        Private _playbackThread As Thread
        Private _cts As CancellationTokenSource
        Private _deviceNumber As Integer = -1  ' -1 = default device
        Private _volume As Single = 1.0F
        Private _isRunning As Boolean = False

        Public Sub New(logger As ILogger(Of TtsAudioOutput))
            _logger = logger
        End Sub

        ''' <summary>
        ''' Gets or sets the output device number. -1 = system default.
        ''' Use GetOutputDevices() to enumerate available devices.
        ''' </summary>
        Public Property DeviceNumber As Integer
            Get
                Return _deviceNumber
            End Get
            Set(value As Integer)
                _deviceNumber = value
            End Set
        End Property

        ''' <summary>Volume 0.0 to 1.0.</summary>
        Public Property Volume As Single
            Get
                Return _volume
            End Get
            Set(value As Single)
                _volume = Math.Max(0.0F, Math.Min(1.0F, value))
            End Set
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        ''' <summary>
        ''' Returns available audio output devices.
        ''' </summary>
        Public Shared Function GetOutputDevices() As List(Of AudioOutputDevice)
            Dim devices As New List(Of AudioOutputDevice)()
            For i = -1 To WaveOut.DeviceCount - 1
                Try
                    Dim caps = WaveOut.GetCapabilities(i)
                    devices.Add(New AudioOutputDevice With {
                        .DeviceNumber = i,
                        .Name = If(i = -1, "Default", caps.ProductName)
                    })
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.AUDIO_PLAYBACK_ERROR, $"TtsAudioOutput.GetOutputDevices: device {i} - {ex.Message}")
                End Try
            Next
            Return devices
        End Function

        ''' <summary>
        ''' Start the playback loop on a background thread.
        ''' </summary>
        Public Sub Start()
            If _isRunning Then Return
            _cts = New CancellationTokenSource()
            _playbackThread = New Thread(AddressOf PlaybackLoop) With {
                .IsBackground = True,
                .Name = "TtsAudioOutput"
            }
            _isRunning = True
            _playbackThread.Start()
            _logger.LogInformation("TTS audio output started on device {Device}", _deviceNumber)
        End Sub

        ''' <summary>
        ''' Stop the playback loop.
        ''' </summary>
        Public Sub [Stop]()
            If Not _isRunning Then Return
            _isRunning = False
            _cts?.Cancel()
            _queue.CompleteAdding()
            _playbackThread?.Join(3000)
            _logger.LogInformation("TTS audio output stopped")
        End Sub

        ''' <summary>
        ''' Enqueue an audio file for playback. Non-blocking.
        ''' </summary>
        Public Sub Enqueue(filePath As String)
            If Not _isRunning Then Return
            If Not File.Exists(filePath) Then Return
            Try
                _queue.TryAdd(filePath)
            Catch ex As InvalidOperationException
                ' Queue completed
            End Try
        End Sub

        ''' <summary>
        ''' Enqueue a TTS cache URL (converts /tts/cache/file to actual path).
        ''' </summary>
        Public Sub EnqueueFromUrl(url As String, cacheDirectory As String)
            If String.IsNullOrEmpty(url) OrElse String.IsNullOrEmpty(cacheDirectory) Then Return
            ' Extract filename from URL like /tts/cache/fra_commit_42.mp3
            Dim fileName = url
            Dim lastSlash = url.LastIndexOf("/"c)
            If lastSlash >= 0 Then fileName = url.Substring(lastSlash + 1)
            Dim filePath = Path.Combine(cacheDirectory, fileName)
            Enqueue(filePath)
        End Sub

        Private Sub PlaybackLoop()
            Try
                While Not _cts.Token.IsCancellationRequested
                    Dim filePath As String = Nothing
                    Try
                        If Not _queue.TryTake(filePath, Timeout.Infinite, _cts.Token) Then
                            Continue While
                        End If
                    Catch ex As OperationCanceledException
                        Exit While
                    End Try

                    Try
                        PlayFile(filePath)
                    Catch ex As Exception
                        _logger.LogDebug(ex, "Failed to play TTS audio: {File}", filePath)
                    End Try
                End While
            Catch ex As OperationCanceledException
            End Try
        End Sub

        Private Sub PlayFile(filePath As String)
            Dim ext = Path.GetExtension(filePath).ToLower()

            Using reader As WaveStream = CreateReader(filePath, ext)
                If reader Is Nothing Then Return

                Using outputDevice As New WaveOutEvent() With {
                    .DeviceNumber = _deviceNumber
                }
                    outputDevice.Volume = _volume
                    outputDevice.Init(reader)
                    outputDevice.Play()

                    ' Wait for playback to complete or cancellation
                    While outputDevice.PlaybackState = PlaybackState.Playing AndAlso
                          Not _cts.Token.IsCancellationRequested
                        Thread.Sleep(50)
                    End While

                    outputDevice.Stop()
                End Using
            End Using
        End Sub

        Private Shared Function CreateReader(filePath As String, ext As String) As WaveStream
            Select Case ext
                Case ".mp3"
                    Return New Mp3FileReader(filePath)
                Case ".wav"
                    Return New WaveFileReader(filePath)
                Case Else
                    ' Try as mp3 (most common from Edge TTS)
                    Try
                        Return New Mp3FileReader(filePath)
                    Catch ex As Exception
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.AUDIO_PLAYBACK_ERROR, $"TtsAudioOutput.CreateReader: failed to read '{filePath}' - {ex.Message}")
                        Return Nothing
                    End Try
            End Select
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            [Stop]()
            _cts?.Dispose()
        End Sub
    End Class

    Public Class AudioOutputDevice
        Public Property DeviceNumber As Integer
        Public Property Name As String
    End Class
End Namespace
