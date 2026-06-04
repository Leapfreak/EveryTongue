Imports System.Diagnostics
Imports System.Threading

Namespace Services.Testing

    ''' <summary>
    ''' Samples CPU, RAM, and GPU metrics at regular intervals during benchmark runs.
    ''' Produces a ResourceReport with min/max/avg and warning flags for pressure points.
    ''' </summary>
    Public Class ResourceMonitor

        Private ReadOnly _samples As New List(Of ResourceSample)()
        Private _timer As Timer
        Private _process As Process
        Private ReadOnly _intervalMs As Integer

        Public Sub New(Optional intervalMs As Integer = 500)
            _intervalMs = intervalMs
        End Sub

        Public Sub Start()
            _samples.Clear()
            _process = Process.GetCurrentProcess()
            TakeSample() ' immediate first sample
            _timer = New Timer(Sub(state) TakeSample(), Nothing, _intervalMs, _intervalMs)
        End Sub

        Public Function [Stop]() As ResourceReport
            _timer?.Dispose()
            _timer = Nothing
            TakeSample() ' final sample
            Return BuildReport()
        End Function

        Private Sub TakeSample()
            Try
                _process?.Refresh()
                Dim sample As New ResourceSample() With {
                    .Timestamp = DateTime.UtcNow,
                    .ProcessMemoryMB = If(_process IsNot Nothing,
                        CInt(_process.WorkingSet64 \ (1024L * 1024L)), 0),
                    .PrivateMemoryMB = If(_process IsNot Nothing,
                        CInt(_process.PrivateMemorySize64 \ (1024L * 1024L)), 0)
                }

                ' GPU metrics via nvidia-smi (fast query, ~50ms)
                QueryGpu(sample)

                SyncLock _samples
                    _samples.Add(sample)
                End SyncLock
            Catch
                ' Sampling failures are non-fatal
            End Try
        End Sub

        Private Shared Sub QueryGpu(sample As ResourceSample)
            Try
                Dim psi As New ProcessStartInfo("nvidia-smi",
                    "--query-gpu=utilization.gpu,memory.used,memory.total,temperature.gpu --format=csv,noheader,nounits") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }
                Using p = Process.Start(psi)
                    If p Is Nothing Then Return
                    Dim output = p.StandardOutput.ReadToEnd().Trim()
                    p.WaitForExit(2000)
                    If String.IsNullOrEmpty(output) Then Return
                    Dim parts = output.Split(","c)
                    If parts.Length >= 4 Then
                        Integer.TryParse(parts(0).Trim(), sample.GpuUtilPercent)
                        Integer.TryParse(parts(1).Trim(), sample.GpuMemoryUsedMB)
                        Integer.TryParse(parts(2).Trim(), sample.GpuMemoryTotalMB)
                        Integer.TryParse(parts(3).Trim(), sample.GpuTempC)
                    ElseIf parts.Length >= 3 Then
                        Integer.TryParse(parts(0).Trim(), sample.GpuUtilPercent)
                        Integer.TryParse(parts(1).Trim(), sample.GpuMemoryUsedMB)
                        Integer.TryParse(parts(2).Trim(), sample.GpuMemoryTotalMB)
                    End If
                End Using
            Catch
                ' No NVIDIA GPU or nvidia-smi not available
            End Try
        End Sub

        Private Function BuildReport() As ResourceReport
            Dim report As New ResourceReport()

            SyncLock _samples
                If _samples.Count = 0 Then Return report
                report.SampleCount = _samples.Count

                ' Process memory
                report.ProcessMemoryMinMB = _samples.Min(Function(s) s.ProcessMemoryMB)
                report.ProcessMemoryMaxMB = _samples.Max(Function(s) s.ProcessMemoryMB)
                report.ProcessMemoryAvgMB = CInt(_samples.Average(Function(s) CDbl(s.ProcessMemoryMB)))

                ' GPU utilisation
                Dim gpuSamples = _samples.Where(Function(s) s.GpuMemoryTotalMB > 0).ToList()
                If gpuSamples.Count > 0 Then
                    report.HasGpu = True
                    report.GpuUtilMinPercent = gpuSamples.Min(Function(s) s.GpuUtilPercent)
                    report.GpuUtilMaxPercent = gpuSamples.Max(Function(s) s.GpuUtilPercent)
                    report.GpuUtilAvgPercent = CInt(gpuSamples.Average(Function(s) CDbl(s.GpuUtilPercent)))

                    report.GpuMemoryTotalMB = gpuSamples.First().GpuMemoryTotalMB
                    report.GpuMemoryMinMB = gpuSamples.Min(Function(s) s.GpuMemoryUsedMB)
                    report.GpuMemoryMaxMB = gpuSamples.Max(Function(s) s.GpuMemoryUsedMB)
                    report.GpuMemoryAvgMB = CInt(gpuSamples.Average(Function(s) CDbl(s.GpuMemoryUsedMB)))

                    report.GpuTempMinC = gpuSamples.Min(Function(s) s.GpuTempC)
                    report.GpuTempMaxC = gpuSamples.Max(Function(s) s.GpuTempC)

                    ' Compute peak VRAM usage percentage
                    If report.GpuMemoryTotalMB > 0 Then
                        report.GpuMemoryPeakPercent = CInt(
                            report.GpuMemoryMaxMB / CDbl(report.GpuMemoryTotalMB) * 100)
                    End If
                End If

                ' Warning flags
                report.Warnings = New List(Of String)()
                If report.GpuMemoryPeakPercent >= 95 Then
                    report.Warnings.Add($"VRAM critically full: {report.GpuMemoryMaxMB}/{report.GpuMemoryTotalMB} MB ({report.GpuMemoryPeakPercent}%)")
                ElseIf report.GpuMemoryPeakPercent >= 85 Then
                    report.Warnings.Add($"VRAM high: {report.GpuMemoryMaxMB}/{report.GpuMemoryTotalMB} MB ({report.GpuMemoryPeakPercent}%)")
                End If

                If report.GpuUtilMaxPercent >= 98 Then
                    report.Warnings.Add($"GPU saturated: peaked at {report.GpuUtilMaxPercent}% utilisation")
                End If

                If report.GpuTempMaxC >= 85 Then
                    report.Warnings.Add($"GPU temperature high: peaked at {report.GpuTempMaxC}C")
                End If

                If report.ProcessMemoryMaxMB >= 4000 Then
                    report.Warnings.Add($"Process memory high: peaked at {report.ProcessMemoryMaxMB} MB")
                End If
            End SyncLock

            Return report
        End Function

    End Class

    Public Class ResourceSample
        Public Property Timestamp As DateTime
        Public Property ProcessMemoryMB As Integer
        Public Property PrivateMemoryMB As Integer
        Public Property GpuUtilPercent As Integer
        Public Property GpuMemoryUsedMB As Integer
        Public Property GpuMemoryTotalMB As Integer
        Public Property GpuTempC As Integer
    End Class

    Public Class ResourceReport
        Public Property SampleCount As Integer
        Public Property HasGpu As Boolean

        ' Process memory
        Public Property ProcessMemoryMinMB As Integer
        Public Property ProcessMemoryMaxMB As Integer
        Public Property ProcessMemoryAvgMB As Integer

        ' GPU utilisation
        Public Property GpuUtilMinPercent As Integer
        Public Property GpuUtilMaxPercent As Integer
        Public Property GpuUtilAvgPercent As Integer

        ' GPU memory
        Public Property GpuMemoryTotalMB As Integer
        Public Property GpuMemoryMinMB As Integer
        Public Property GpuMemoryMaxMB As Integer
        Public Property GpuMemoryAvgMB As Integer
        Public Property GpuMemoryPeakPercent As Integer

        ' GPU temperature
        Public Property GpuTempMinC As Integer
        Public Property GpuTempMaxC As Integer

        ' Pressure warnings
        Public Property Warnings As New List(Of String)()

        Public Function ToSummaryText() As String
            Dim sb As New Text.StringBuilder()
            sb.Append($"RAM: {ProcessMemoryAvgMB} MB avg, {ProcessMemoryMaxMB} MB peak")

            If HasGpu Then
                sb.Append($"  |  GPU: {GpuUtilAvgPercent}% avg, {GpuUtilMaxPercent}% peak")
                sb.Append($"  |  VRAM: {GpuMemoryAvgMB}/{GpuMemoryTotalMB} MB avg, " &
                           $"{GpuMemoryMaxMB} MB peak ({GpuMemoryPeakPercent}%)")
                If GpuTempMaxC > 0 Then
                    sb.Append($"  |  Temp: {GpuTempMaxC}C peak")
                End If
            End If

            Return sb.ToString()
        End Function
    End Class

End Namespace
