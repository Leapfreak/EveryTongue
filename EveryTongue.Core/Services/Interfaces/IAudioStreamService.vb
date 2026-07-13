Imports System.Threading
Imports EveryTongue.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' Audio streaming service — serves audio files with range request support
    ''' and manages NDI source capture for live audio forwarding.
    ''' </summary>
    Public Interface IAudioStreamService
        Function GetFileStreamAsync(filePath As String,
                                    rangeStart As Long,
                                    rangeEnd As Long,
                                    ct As CancellationToken
        ) As Task(Of AudioStreamResult)

        Function GetNdiSourcesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of NdiSourceInfo))

        Function StartNdiCaptureAsync(sourceName As String,
                                      ct As CancellationToken
        ) As Task(Of String)

        Sub StopNdiCapture(streamId As String)
    End Interface
End Namespace
