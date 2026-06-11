Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports SttBenchmark.Models

Namespace Services

    ''' <summary>
    ''' JSON persistence for benchmark run history.
    ''' </summary>
    Public Class ResultStore

        Private ReadOnly _filePath As String
        Private ReadOnly _jsonOptions As JsonSerializerOptions
        Private _runs As New List(Of BenchmarkRun)

        Public ReadOnly Property Runs As IReadOnlyList(Of BenchmarkRun)
            Get
                Return _runs.AsReadOnly()
            End Get
        End Property

        Public Sub New(Optional filePath As String = Nothing)
            If filePath Is Nothing Then
                _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "benchmark_results.json")
            Else
                _filePath = filePath
            End If
            _jsonOptions = New JsonSerializerOptions With {
                .WriteIndented = True,
                .PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }
            Load()
        End Sub

        Public Sub Load()
            If Not File.Exists(_filePath) Then
                _runs = New List(Of BenchmarkRun)
                Return
            End If
            Try
                Dim json = File.ReadAllText(_filePath)
                Dim wrapper = JsonSerializer.Deserialize(Of RunsWrapper)(json, _jsonOptions)
                _runs = If(wrapper?.Runs, New List(Of BenchmarkRun))
            Catch
                _runs = New List(Of BenchmarkRun)
            End Try
        End Sub

        Public Sub Save()
            Dim wrapper = New RunsWrapper With {.Runs = _runs}
            Dim json = JsonSerializer.Serialize(wrapper, _jsonOptions)
            File.WriteAllText(_filePath, json)
        End Sub

        Public Sub Add(run As BenchmarkRun)
            _runs.Add(run)
            Save()
        End Sub

        Public Sub Clear()
            _runs.Clear()
            Save()
        End Sub

        Public Sub ExportCsv(csvPath As String)
            Using sw As New StreamWriter(csvPath)
                sw.WriteLine("Id,Timestamp,Backend,Pipeline,Language,WER%,Subs,Dels,Ins,RefWords,HypWords,Commits,Hallucinations,AudioDur,ElapsedS,BeamSize,BestOf,VadSilenceMs,VadMaxSegS,Notes")
                For Each r In _runs
                    sw.WriteLine($"""{r.Id}"",""{r.Timestamp:yyyy-MM-dd HH:mm:ss}"",""{r.Backend}"",""{r.Pipeline}"",""{r.Language}"",{r.Wer},{r.Substitutions},{r.Deletions},{r.Insertions},{r.RefWords},{r.HypWords},{r.CommitCount},{r.HallucinationCount},{r.AudioDurationS:F1},{r.ElapsedS:F1},{r.BeamSize},{r.BestOf},{r.VadMinSilenceMs},{r.VadMaxSegmentS},""{r.Notes?.Replace("""", "''")}""")
                Next
            End Using
        End Sub

        Private Class RunsWrapper
            Public Property Runs As List(Of BenchmarkRun)
        End Class

    End Class

End Namespace
