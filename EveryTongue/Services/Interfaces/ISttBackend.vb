Imports System.Threading
Imports EveryTongue.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' A pluggable STT backend (whisper.cpp, Vosk, Azure Speech, etc.).
    ''' Each backend implements this interface. Instances are created per-session.
    ''' </summary>
    Public Interface ISttBackend
        ReadOnly Property Name As String
        ReadOnly Property RequiresInternet As Boolean
        ReadOnly Property IsAvailable As Boolean
        ReadOnly Property IsRunning As Boolean

        ' Events for real-time streaming output
        Event OutputUpdated As EventHandler(Of SttOutputEventArgs)
        Event OutputCommitted As EventHandler(Of SttOutputEventArgs)
        ''' <summary>
        ''' Raised when a commit arrives with inline translations from the engine
        ''' (Speechmatics built-in translation). Engines that don't translate never
        ''' raise this.
        ''' </summary>
        Event OutputCommittedTranslated As EventHandler(Of SttTranslatedCommitEventArgs)
        Event ErrorReceived As EventHandler(Of String)

        ' Lifecycle
        Sub Start(config As SttConfig)
        Sub [Stop]()

        ' Runtime config updates (language, VAD, etc.)
        Function UpdateConfigAsync(params As Dictionary(Of String, Object)) As Task

        ' Device enumeration
        Function EnumerateDevicesAsync(pythonExePath As String) As List(Of AudioDeviceInfo)

        ' Health + stats
        Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
        Function GetStatsAsync() As Task(Of String)

        ' Transcript
        ReadOnly Property Transcript As String
        Function SaveTranscript(filePath As String) As Boolean
    End Interface
End Namespace
