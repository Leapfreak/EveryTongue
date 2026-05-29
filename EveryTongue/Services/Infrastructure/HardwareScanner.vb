' HardwareScanner.vb — WMI + nvidia-smi hardware detection and scoring
' Phase 4 — Feature #12 (Hardware Check)

Imports System.Diagnostics
Imports System.Management

Namespace Services.Infrastructure

    Public Class HardwareInfo
        Public Property GpuName As String = ""
        Public Property GpuMemoryMB As Integer = 0
        Public Property GpuScore As Integer = 0

        Public Property CpuName As String = ""
        Public Property CpuCores As Integer = 0
        Public Property CpuScore As Integer = 0

        Public Property RamTotalMB As Long = 0
        Public Property RamScore As Integer = 0

        Public Property DiskFreeMB As Long = 0
        Public Property DiskScore As Integer = 0

        Public Property OverallScore As Integer = 0

        Public ReadOnly Property Rating As String
            Get
                Select Case OverallScore
                    Case >= 75 : Return "Green"
                    Case >= 45 : Return "Amber"
                    Case Else : Return "Red"
                End Select
            End Get
        End Property

        Public ReadOnly Property RatingDescription As String
            Get
                Select Case OverallScore
                    Case >= 75 : Return "Well suited for live transcription"
                    Case >= 45 : Return "Usable with smaller models"
                    Case Else : Return "May experience slowness"
                End Select
            End Get
        End Property
    End Class

    Public Class HardwareScanner

        ''' <summary>
        ''' Scans GPU, CPU, RAM, and disk. Returns a scored HardwareInfo.
        ''' This runs synchronously and can take 1-3 seconds (nvidia-smi).
        ''' </summary>
        Public Shared Function Scan() As HardwareInfo
            Dim info As New HardwareInfo()

            ScanGpu(info)
            ScanCpu(info)
            ScanRam(info)
            ScanDisk(info)

            ' Weighted overall: GPU 40%, CPU 25%, RAM 20%, Disk 15%
            info.OverallScore = CInt(
                info.GpuScore * 0.4 +
                info.CpuScore * 0.25 +
                info.RamScore * 0.2 +
                info.DiskScore * 0.15)

            Return info
        End Function

        Private Shared Sub ScanGpu(info As HardwareInfo)
            ' Try nvidia-smi first for NVIDIA GPUs
            Try
                Dim psi As New ProcessStartInfo("nvidia-smi",
                    "--query-gpu=name,memory.total --format=csv,noheader,nounits") With {
                    .RedirectStandardOutput = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True}
                Using proc = Process.Start(psi)
                    If proc IsNot Nothing Then
                        Dim output = proc.StandardOutput.ReadToEnd()
                        proc.WaitForExit(5000)
                        If proc.ExitCode = 0 AndAlso Not String.IsNullOrWhiteSpace(output) Then
                            Dim parts = output.Trim().Split(","c)
                            If parts.Length >= 2 Then
                                info.GpuName = parts(0).Trim()
                                Integer.TryParse(parts(1).Trim(), info.GpuMemoryMB)
                            End If
                        End If
                    End If
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] nvidia-smi not available: {ex.Message}")
            End Try

            ' Fallback to WMI if nvidia-smi didn't work
            If String.IsNullOrEmpty(info.GpuName) Then
                Try
                    Using searcher As New ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController")
                        For Each obj In searcher.Get()
                            Dim name = obj("Name")?.ToString()
                            If Not String.IsNullOrEmpty(name) Then
                                info.GpuName = name
                                Dim ram As Long = 0
                                If obj("AdapterRAM") IsNot Nothing Then
                                    Long.TryParse(obj("AdapterRAM").ToString(), ram)
                                    info.GpuMemoryMB = CInt(ram \ (1024 * 1024))
                                End If
                                Exit For
                            End If
                        Next
                    End Using
                Catch ex As Exception
                    FormMain.WriteDebugLog($"[HW] WMI GPU scan failed: {ex.Message}")
                End Try
            End If

            ' Score GPU
            If info.GpuMemoryMB >= 8000 Then
                info.GpuScore = 90
            ElseIf info.GpuMemoryMB >= 6000 Then
                info.GpuScore = 75
            ElseIf info.GpuMemoryMB >= 4000 Then
                info.GpuScore = 60
            ElseIf info.GpuMemoryMB >= 2000 Then
                info.GpuScore = 40
            ElseIf info.GpuMemoryMB > 0 Then
                info.GpuScore = 20
            Else
                info.GpuScore = 10 ' No dedicated GPU detected
            End If

            ' Bonus for known high-end GPUs
            If info.GpuName.Contains("RTX 40", StringComparison.OrdinalIgnoreCase) OrElse
               info.GpuName.Contains("RTX 50", StringComparison.OrdinalIgnoreCase) Then
                info.GpuScore = Math.Min(100, info.GpuScore + 10)
            End If
        End Sub

        Private Shared Sub ScanCpu(info As HardwareInfo)
            Try
                Using searcher As New ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor")
                    For Each obj In searcher.Get()
                        info.CpuName = obj("Name")?.ToString()?.Trim()
                        Dim cores As Integer = 0
                        If obj("NumberOfCores") IsNot Nothing Then
                            Integer.TryParse(obj("NumberOfCores").ToString(), cores)
                        End If
                        info.CpuCores = cores
                        Exit For
                    Next
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] WMI CPU scan failed: {ex.Message}")
            End Try

            ' Score CPU by core count
            If info.CpuCores >= 12 Then
                info.CpuScore = 90
            ElseIf info.CpuCores >= 8 Then
                info.CpuScore = 75
            ElseIf info.CpuCores >= 6 Then
                info.CpuScore = 60
            ElseIf info.CpuCores >= 4 Then
                info.CpuScore = 45
            Else
                info.CpuScore = 25
            End If
        End Sub

        Private Shared Sub ScanRam(info As HardwareInfo)
            Try
                Using searcher As New ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem")
                    For Each obj In searcher.Get()
                        Dim kb As Long = 0
                        If obj("TotalVisibleMemorySize") IsNot Nothing Then
                            Long.TryParse(obj("TotalVisibleMemorySize").ToString(), kb)
                        End If
                        info.RamTotalMB = kb \ 1024
                        Exit For
                    Next
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] WMI RAM scan failed: {ex.Message}")
            End Try

            ' Score RAM
            If info.RamTotalMB >= 32000 Then
                info.RamScore = 95
            ElseIf info.RamTotalMB >= 16000 Then
                info.RamScore = 80
            ElseIf info.RamTotalMB >= 8000 Then
                info.RamScore = 55
            ElseIf info.RamTotalMB >= 4000 Then
                info.RamScore = 30
            Else
                info.RamScore = 15
            End If
        End Sub

        Private Shared Sub ScanDisk(info As HardwareInfo)
            Try
                Dim appDrive = IO.Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory)
                Dim driveInfo As New IO.DriveInfo(appDrive)
                info.DiskFreeMB = driveInfo.AvailableFreeSpace \ (1024 * 1024)
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] Disk scan failed: {ex.Message}")
            End Try

            ' Score disk free space
            If info.DiskFreeMB >= 50000 Then
                info.DiskScore = 100
            ElseIf info.DiskFreeMB >= 20000 Then
                info.DiskScore = 80
            ElseIf info.DiskFreeMB >= 10000 Then
                info.DiskScore = 60
            ElseIf info.DiskFreeMB >= 5000 Then
                info.DiskScore = 40
            Else
                info.DiskScore = 20
            End If
        End Sub
    End Class
End Namespace
