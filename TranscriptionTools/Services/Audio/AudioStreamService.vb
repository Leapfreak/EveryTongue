Imports System.Collections.Concurrent
Imports System.IO
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports TranscriptionTools.Services.Interfaces
Imports TranscriptionTools.Services.Models

Namespace Services.Audio
    ''' <summary>
    ''' Audio streaming service — serves audio files with HTTP range request support
    ''' and manages NDI source capture for live audio forwarding.
    ''' </summary>
    Public Class AudioStreamService
        Implements IAudioStreamService

        Private ReadOnly _logger As ILogger(Of AudioStreamService)
        Private ReadOnly _activeStreams As New ConcurrentDictionary(Of String, NdiCaptureState)()

        ''' <summary>
        ''' Allowed directories for file serving (prevents directory traversal).
        ''' </summary>
        Public Property AllowedDirectories As New List(Of String)()

        ' MIME type map for audio files
        Private Shared ReadOnly MimeTypes As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {".mp3", "audio/mpeg"},
            {".ogg", "audio/ogg"},
            {".opus", "audio/opus"},
            {".wav", "audio/wav"},
            {".flac", "audio/flac"},
            {".m4a", "audio/mp4"},
            {".aac", "audio/aac"},
            {".wma", "audio/x-ms-wma"},
            {".webm", "audio/webm"}
        }

        Public Sub New(logger As ILogger(Of AudioStreamService))
            _logger = logger
        End Sub

        Public Function GetFileStreamAsync(filePath As String,
                                           rangeStart As Long,
                                           rangeEnd As Long,
                                           ct As CancellationToken
        ) As Task(Of AudioStreamResult) Implements IAudioStreamService.GetFileStreamAsync
            ' Validate path is within allowed directories
            Dim fullPath = Path.GetFullPath(filePath)
            Dim allowed = AllowedDirectories.Any(
                Function(d) fullPath.StartsWith(Path.GetFullPath(d), StringComparison.OrdinalIgnoreCase))
            If Not allowed AndAlso AllowedDirectories.Count > 0 Then
                Return Task.FromResult(DirectCast(Nothing, AudioStreamResult))
            End If

            If Not File.Exists(fullPath) Then
                Return Task.FromResult(DirectCast(Nothing, AudioStreamResult))
            End If

            Dim fi As New FileInfo(fullPath)
            Dim ext = fi.Extension.ToLower()
            Dim contentType As String = Nothing
            If Not MimeTypes.TryGetValue(ext, contentType) Then
                contentType = "application/octet-stream"
            End If

            ' Handle range request
            If rangeEnd <= 0 OrElse rangeEnd >= fi.Length Then
                rangeEnd = fi.Length - 1
            End If
            If rangeStart < 0 Then rangeStart = 0

            Dim stream = fi.OpenRead()
            stream.Seek(rangeStart, SeekOrigin.Begin)

            Return Task.FromResult(New AudioStreamResult With {
                .Stream = stream,
                .ContentType = contentType,
                .TotalLength = fi.Length,
                .RangeStart = rangeStart,
                .RangeEnd = rangeEnd,
                .FileName = fi.Name
            })
        End Function

        Public Function GetNdiSourcesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of NdiSourceInfo)) Implements IAudioStreamService.GetNdiSourcesAsync
            ' TODO: NDI SDK discovery via P/Invoke
            Dim empty As IReadOnlyList(Of NdiSourceInfo) = New List(Of NdiSourceInfo)()
            Return Task.FromResult(empty)
        End Function

        Public Function StartNdiCaptureAsync(sourceName As String,
                                             ct As CancellationToken
        ) As Task(Of String) Implements IAudioStreamService.StartNdiCaptureAsync
            ' TODO: Start NDI capture via SDK
            Dim streamId = Guid.NewGuid().ToString("N").Substring(0, 8)
            _activeStreams(streamId) = New NdiCaptureState With {
                .SourceName = sourceName,
                .StartedAt = DateTime.UtcNow
            }
            _logger.LogInformation("NDI capture started: {Source} -> {StreamId}", sourceName, streamId)
            Return Task.FromResult(streamId)
        End Function

        Public Sub StopNdiCapture(streamId As String) Implements IAudioStreamService.StopNdiCapture
            Dim state As NdiCaptureState = Nothing
            If _activeStreams.TryRemove(streamId, state) Then
                _logger.LogInformation("NDI capture stopped: {StreamId}", streamId)
            End If
        End Sub

        Private Class NdiCaptureState
            Public Property SourceName As String
            Public Property StartedAt As DateTime
        End Class
    End Class
End Namespace
