Namespace Services.Audio

    ''' <summary>
    ''' Local TTS playback sink (PA/NDI routing on the server machine). The server
    ''' core only ever talks to this interface; the Windows head supplies the NAudio
    ''' implementation (TtsAudioOutput) via ServerOptions.TtsSinkFactory. Headless
    ''' hosts leave the factory unset — phones still play TTS themselves; only
    ''' local-machine playback is absent.
    ''' </summary>
    Public Interface ITtsAudioSink
        Inherits IDisposable
        ReadOnly Property IsRunning As Boolean
        Sub Start()
        Sub [Stop]()
        ''' <summary>Queue a TTS cache URL (e.g. /tts/cache/file.mp3) for local playback.</summary>
        Sub EnqueueFromUrl(url As String, cacheDirectory As String)
    End Interface

    ''' <summary>An audio output device as reported by the head's audio stack.</summary>
    Public Class AudioOutputDevice
        Public Property DeviceNumber As Integer
        Public Property Name As String
    End Class

End Namespace
