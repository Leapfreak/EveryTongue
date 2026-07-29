Namespace Services.Testing

    ''' <summary>
    ''' Shared latency aggregation for the benchmark/concurrency runners.
    ''' Was copy-pasted across the three concurrency runners (identical block)
    ''' plus five private copies of Percentile — CLONE-REPORT Group 4.
    ''' </summary>
    Public Module LatencyStats

        ''' <summary>Sort the collected latencies and fill the level result's stats fields.</summary>
        Public Sub Apply(result As ConcurrencyLevelResult, latencies As IEnumerable(Of Long),
                         wallMs As Long, totalRequests As Integer, errors As Integer)
            Dim sorted = latencies.ToList()
            sorted.Sort()

            result.WallTimeMs = wallMs
            result.TotalRequests = totalRequests
            result.Errors = errors

            If sorted.Count > 0 Then
                result.AvgLatencyMs = CLng(sorted.Average())
                result.MinLatencyMs = sorted.First()
                result.MaxLatencyMs = sorted.Last()
                result.P50LatencyMs = Percentile(sorted, 50)
                result.P95LatencyMs = Percentile(sorted, 95)
                result.InferencesPerSec = Math.Round(sorted.Count / (wallMs / 1000.0), 1)
            End If
        End Sub

        ''' <summary>Nearest-rank percentile of an ascending-sorted list (0 if empty).</summary>
        Public Function Percentile(sorted As List(Of Long), p As Integer) As Long
            If sorted.Count = 0 Then Return 0
            Dim idx = CInt(Math.Ceiling(p / 100.0 * sorted.Count)) - 1
            Return sorted(Math.Max(0, Math.Min(idx, sorted.Count - 1)))
        End Function

    End Module

End Namespace
