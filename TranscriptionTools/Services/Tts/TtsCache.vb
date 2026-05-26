Imports System.Collections.Concurrent
Imports System.IO
Imports TranscriptionTools.Services.Models

Namespace Services.Tts
    ''' <summary>
    ''' Ring-buffer cache for TTS audio files. Stores per-language audio keyed by commit ID.
    ''' Files are served directly by Kestrel static file middleware.
    ''' </summary>
    Public Class TtsCache

        Private ReadOnly _cacheDir As String
        Private ReadOnly _maxEntriesPerLang As Integer
        Private ReadOnly _entries As New ConcurrentDictionary(Of String, ConcurrentQueue(Of String))()
        Private _hitCount As Long = 0
        Private _missCount As Long = 0

        Public Sub New(Optional cacheDir As String = Nothing, Optional maxEntriesPerLang As Integer = 200)
            _cacheDir = If(cacheDir,
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "TranscriptionTools", "tts-cache"))
            _maxEntriesPerLang = maxEntriesPerLang
            Directory.CreateDirectory(_cacheDir)
        End Sub

        Public ReadOnly Property CacheDirectory As String
            Get
                Return _cacheDir
            End Get
        End Property

        ''' <summary>
        ''' Store audio bytes and return the relative URL path for serving.
        ''' </summary>
        Public Function Store(language As String, commitId As Integer,
                             audioBytes As Byte(), codec As String) As String
            Dim fileName = $"{language}_commit_{commitId}.{codec}"
            Dim filePath = Path.Combine(_cacheDir, fileName)

            File.WriteAllBytes(filePath, audioBytes)

            ' Track in ring buffer per language
            Dim queue = _entries.GetOrAdd(language,
                Function(k) New ConcurrentQueue(Of String)())
            queue.Enqueue(fileName)

            ' Evict oldest if over limit
            While queue.Count > _maxEntriesPerLang
                Dim oldFile As String = Nothing
                If queue.TryDequeue(oldFile) Then
                    Dim oldPath = Path.Combine(_cacheDir, oldFile)
                    Try : File.Delete(oldPath) : Catch : End Try
                End If
            End While

            Return $"/tts/cache/{fileName}"
        End Function

        ''' <summary>
        ''' Check if audio already cached for this language + commit.
        ''' </summary>
        Public Function TryGet(language As String, commitId As Integer,
                              codec As String) As String
            Dim fileName = $"{language}_commit_{commitId}.{codec}"
            Dim filePath = Path.Combine(_cacheDir, fileName)
            If File.Exists(filePath) Then
                Threading.Interlocked.Increment(_hitCount)
                Return $"/tts/cache/{fileName}"
            End If
            Threading.Interlocked.Increment(_missCount)
            Return Nothing
        End Function

        Public Function GetStats() As TtsCacheStats
            Dim totalSize As Long = 0
            Dim entryCount = 0
            Dim oldest = DateTime.MaxValue

            If Directory.Exists(_cacheDir) Then
                For Each f In Directory.GetFiles(_cacheDir)
                    Dim fi As New FileInfo(f)
                    totalSize += fi.Length
                    entryCount += 1
                    If fi.CreationTimeUtc < oldest Then oldest = fi.CreationTimeUtc
                Next
            End If

            Dim total = _hitCount + _missCount
            Return New TtsCacheStats With {
                .EntryCount = entryCount,
                .TotalSizeBytes = totalSize,
                .HitRate = If(total > 0, _hitCount / CDbl(total), 0),
                .OldestEntryAge = If(entryCount > 0,
                    DateTime.UtcNow - oldest, TimeSpan.Zero)
            }
        End Function

        ''' <summary>
        ''' Clear all cached audio files.
        ''' </summary>
        Public Sub Clear()
            Try
                For Each f In Directory.GetFiles(_cacheDir)
                    Try : File.Delete(f) : Catch : End Try
                Next
                _entries.Clear()
            Catch
            End Try
        End Sub
    End Class
End Namespace
