Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports SttBenchmark.Models

Namespace Services

    ''' <summary>
    ''' Orchestrates a benchmark run: calls the live-server /benchmark endpoint
    ''' and returns the results. Assumes the live-server is already running and
    ''' the model is already loaded.
    ''' </summary>
    Public Class PipelineBenchmarkRunner

        Private ReadOnly _httpClient As New HttpClient()

        Public Property ServerBaseUrl As String = "http://127.0.0.1:5091"

        Public Event StatusChanged(message As String)
        Public Event ProgressChanged(percent As Integer)

        Public Async Function RunAsync(
            audioPath As String,
            language As String,
            pipeline As String,
            beamSize As Integer,
            bestOf As Integer,
            initialPrompt As String,
            vadMinSilenceMs As Integer,
            vadMaxSegmentS As Integer,
            interimIntervalMs As Integer,
            realtimeFactor As Double,
            cancellation As CancellationToken
        ) As Task(Of BenchmarkRunResult)

            RaiseEvent StatusChanged("Calling /benchmark endpoint...")
            RaiseEvent ProgressChanged(10)

            Dim requestBody = New Dictionary(Of String, Object) From {
                {"audio_path", audioPath},
                {"language", language},
                {"pipeline", If(pipeline = "AccumulatingPipeline", "accumulating", "vad")},
                {"beam_size", beamSize},
                {"best_of", bestOf},
                {"initial_prompt", initialPrompt},
                {"vad_min_silence_ms", vadMinSilenceMs},
                {"vad_max_segment_s", vadMaxSegmentS},
                {"interim_interval_ms", interimIntervalMs},
                {"realtime_factor", realtimeFactor}
            }

            Dim jsonContent = New StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8, "application/json")

            ' Long timeout — benchmark can take a while
            _httpClient.Timeout = TimeSpan.FromMinutes(30)

            RaiseEvent ProgressChanged(20)

            Dim response = Await _httpClient.PostAsync(
                $"{ServerBaseUrl}/benchmark", jsonContent, cancellation)

            RaiseEvent ProgressChanged(80)

            Dim responseJson = Await response.Content.ReadAsStringAsync(cancellation)

            If Not response.IsSuccessStatusCode Then
                Throw New Exception($"Server returned {response.StatusCode}: {responseJson}")
            End If

            RaiseEvent StatusChanged("Parsing results...")

            Dim doc = JsonDocument.Parse(responseJson)
            Dim root = doc.RootElement

            If root.GetProperty("status").GetString() <> "ok" Then
                Dim detail = ""
                If root.TryGetProperty("detail", Nothing) Then
                    detail = root.GetProperty("detail").GetString()
                End If
                Throw New Exception($"Benchmark failed: {detail}")
            End If

            ' Extract commits
            Dim commits As New List(Of String)
            For Each commitEl In root.GetProperty("commits").EnumerateArray()
                commits.Add(commitEl.GetProperty("text").GetString())
            Next

            Dim result As New BenchmarkRunResult With {
                .OutputText = String.Join(Environment.NewLine, commits),
                .CommitCount = root.GetProperty("commit_count").GetInt32(),
                .HallucinationCount = root.GetProperty("hallucination_count").GetInt32(),
                .AudioDurationS = root.GetProperty("total_duration_s").GetDouble(),
                .ElapsedS = root.GetProperty("elapsed_s").GetDouble(),
                .StatsSummary = If(root.TryGetProperty("stats_summary", Nothing),
                                   root.GetProperty("stats_summary").GetString(), "")
            }

            RaiseEvent ProgressChanged(100)
            RaiseEvent StatusChanged("Done.")

            Return result
        End Function

        ''' <summary>Check if live-server is reachable.</summary>
        Public Async Function CheckHealthAsync() As Task(Of Boolean)
            Try
                _httpClient.Timeout = TimeSpan.FromSeconds(5)
                Dim resp = Await _httpClient.GetAsync($"{ServerBaseUrl}/health")
                Return resp.IsSuccessStatusCode
            Catch
                Return False
            End Try
        End Function

        ''' <summary>Load model on the server if not already loaded.</summary>
        Public Async Function LoadModelAsync(modelPath As String) As Task(Of Boolean)
            Try
                _httpClient.Timeout = TimeSpan.FromSeconds(120)
                Dim body = New Dictionary(Of String, Object) From {
                    {"model_path", modelPath}
                }
                Dim content = New StringContent(
                    JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                Dim resp = Await _httpClient.PostAsync($"{ServerBaseUrl}/load-model", content)
                Return resp.IsSuccessStatusCode
            Catch
                Return False
            End Try
        End Function

    End Class

    Public Class BenchmarkRunResult
        Public Property OutputText As String = ""
        Public Property CommitCount As Integer = 0
        Public Property HallucinationCount As Integer = 0
        Public Property AudioDurationS As Double = 0.0
        Public Property ElapsedS As Double = 0.0
        Public Property StatsSummary As String = ""
    End Class

End Namespace
