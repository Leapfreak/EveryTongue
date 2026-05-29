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
        Public Property CpuClockMHz As Integer = 0
        Public Property CpuScore As Integer = 0

        Public Property RamTotalMB As Long = 0
        Public Property RamScore As Integer = 0

        Public Property DiskFreeMB As Long = 0
        Public Property DiskScore As Integer = 0

        Public Property OsDescription As String = ""
        Public Property Is64Bit As Boolean = True
        Public Property OsScore As Integer = 0

        Public Property OverallScore As Integer = 0

        Public ReadOnly Property Rating As String
            Get
                Select Case OverallScore
                    Case >= 75 : Return "Green"
                    Case >= 50 : Return "Amber"
                    Case Else : Return "Red"
                End Select
            End Get
        End Property

        Public ReadOnly Property RatingDescription As String
            Get
                Select Case OverallScore
                    Case >= 75 : Return "This PC is well suited for live transcription and translation."
                    Case >= 50 : Return "This PC can run transcription but performance may be limited. See recommendations below."
                    Case >= 25 : Return "This PC will struggle with live transcription. Consider using a more powerful laptop."
                    Case Else : Return "This PC does not meet minimum requirements for usable live transcription."
                End Select
            End Get
        End Property

        Public Function GetRecommendations() As List(Of String)
            Dim recs As New List(Of String)

            If GpuScore < 25 Then
                recs.Add("No NVIDIA GPU detected — transcription will run on CPU only, expect significant delays. For live use, an NVIDIA GPU with at least 4GB VRAM is strongly recommended.")
            ElseIf GpuScore <= 55 Then
                Dim vramGB = GpuMemoryMB \ 1024
                recs.Add($"Your GPU has {vramGB}GB VRAM — use the 'small' or 'base' whisper model instead of 'medium' for best real-time performance.")
            End If

            If CpuScore < 25 AndAlso GpuScore < 25 Then
                recs.Add("Without a GPU, consider using the 'tiny' or 'base' whisper model. Accuracy will be lower but speed will be usable.")
            End If

            If RamScore < 50 Then
                Dim ramGB = RamTotalMB \ 1024
                recs.Add($"With {ramGB}GB RAM, running transcription and translation simultaneously may cause slowdowns. Close other applications before starting.")
            End If

            If DiskScore < 40 Then
                Dim freeGB = (DiskFreeMB / 1024.0).ToString("F1")
                recs.Add($"Only {freeGB}GB free disk space. You may not have room for all language models. Free up space or use an external drive.")
            End If

            If OsScore < 100 AndAlso Not Is64Bit Then
                recs.Add("A 64-bit version of Windows is required for optimal performance.")
            End If

            Return recs
        End Function
    End Class

    Public Class HardwareScanner

        ''' <summary>
        ''' Scans GPU, CPU, RAM, disk, and OS. Returns a scored HardwareInfo.
        ''' This runs synchronously and can take 1-3 seconds (nvidia-smi).
        ''' </summary>
        Public Shared Function Scan() As HardwareInfo
            Dim info As New HardwareInfo()

            ScanGpu(info)
            ScanCpu(info)
            ScanRam(info)
            ScanDisk(info)
            ScanOs(info)

            ' Weighted overall: GPU 40%, CPU 25%, RAM 20%, Disk 10%, OS 5%
            info.OverallScore = CInt(
                info.GpuScore * 0.4 +
                info.CpuScore * 0.25 +
                info.RamScore * 0.2 +
                info.DiskScore * 0.1 +
                info.OsScore * 0.05)

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

            ' Score GPU per plan scoring table
            Dim isNvidia = info.GpuName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) OrElse
                           info.GpuName.Contains("GeForce", StringComparison.OrdinalIgnoreCase) OrElse
                           info.GpuName.Contains("RTX", StringComparison.OrdinalIgnoreCase) OrElse
                           info.GpuName.Contains("GTX", StringComparison.OrdinalIgnoreCase)

            If Not isNvidia Then
                info.GpuScore = 10 ' No NVIDIA GPU
            ElseIf info.GpuMemoryMB >= 8000 Then
                info.GpuScore = 100
            ElseIf info.GpuMemoryMB >= 6000 Then
                info.GpuScore = 80
            ElseIf info.GpuMemoryMB >= 4000 Then
                info.GpuScore = 55
            ElseIf info.GpuMemoryMB >= 2000 Then
                info.GpuScore = 25
            Else
                info.GpuScore = 10
            End If
        End Sub

        Private Shared Sub ScanCpu(info As HardwareInfo)
            Try
                Using searcher As New ManagementObjectSearcher("SELECT Name, NumberOfCores, MaxClockSpeed FROM Win32_Processor")
                    For Each obj In searcher.Get()
                        info.CpuName = obj("Name")?.ToString()?.Trim()
                        Dim cores As Integer = 0
                        If obj("NumberOfCores") IsNot Nothing Then
                            Integer.TryParse(obj("NumberOfCores").ToString(), cores)
                        End If
                        info.CpuCores = cores
                        Dim clockMHz As Integer = 0
                        If obj("MaxClockSpeed") IsNot Nothing Then
                            Integer.TryParse(obj("MaxClockSpeed").ToString(), clockMHz)
                        End If
                        info.CpuClockMHz = clockMHz
                        Exit For
                    Next
                End Using
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] WMI CPU scan failed: {ex.Message}")
            End Try

            ' Score CPU by cores and clock speed per plan
            Dim clockGHz = info.CpuClockMHz / 1000.0
            If info.CpuCores >= 8 AndAlso clockGHz >= 3.0 Then
                info.CpuScore = 100
            ElseIf info.CpuCores >= 6 AndAlso clockGHz >= 2.5 Then
                info.CpuScore = 75
            ElseIf info.CpuCores >= 4 AndAlso clockGHz >= 2.0 Then
                info.CpuScore = 45
            Else
                info.CpuScore = 15
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

            ' Score RAM per plan
            If info.RamTotalMB >= 32000 Then
                info.RamScore = 100
            ElseIf info.RamTotalMB >= 16000 Then
                info.RamScore = 85
            ElseIf info.RamTotalMB >= 8000 Then
                info.RamScore = 50
            Else
                info.RamScore = 15
            End If
        End Sub

        Private Shared Sub ScanDisk(info As HardwareInfo)
            Try
                Dim appDrive = IO.Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory)
                Dim driveInfo As New IO.DriveInfo(appDrive)
                info.DiskFreeMB = driveInfo.AvailableFreeSpace \ (1024L * 1024L)
                FormMain.WriteDebugLog($"[HW] Disk: drive={appDrive}, freeBytes={driveInfo.AvailableFreeSpace}, freeMB={info.DiskFreeMB}")
            Catch ex As Exception
                FormMain.WriteDebugLog($"[HW] Disk scan failed: {ex.Message}")
            End Try

            ' Score disk free space per plan
            If info.DiskFreeMB >= 20000 Then
                info.DiskScore = 100
            ElseIf info.DiskFreeMB >= 10000 Then
                info.DiskScore = 70
            ElseIf info.DiskFreeMB >= 5000 Then
                info.DiskScore = 40
            Else
                info.DiskScore = 10
            End If
        End Sub

        Private Shared Sub ScanOs(info As HardwareInfo)
            info.Is64Bit = Environment.Is64BitOperatingSystem
            Try
                Dim ver = Environment.OSVersion.Version
                ' Windows 11 shares major version 10 but has build >= 22000
                Dim winName As String
                If ver.Major >= 10 AndAlso ver.Build >= 22000 Then
                    winName = "Windows 11"
                ElseIf ver.Major >= 10 Then
                    winName = "Windows 10"
                Else
                    winName = $"Windows {ver.Major}.{ver.Minor}"
                End If
                info.OsDescription = $"{winName} ({If(info.Is64Bit, "64-bit", "32-bit")}) Build {ver.Build}"
            Catch ex As Exception
                info.OsDescription = If(info.Is64Bit, "64-bit", "32-bit")
                FormMain.WriteDebugLog($"[HW] OS scan failed: {ex.Message}")
            End Try

            ' Score OS per plan: Win 10/11 64-bit = 100, 32-bit = 20, older = 0
            Dim major = Environment.OSVersion.Version.Major
            If major >= 10 AndAlso info.Is64Bit Then
                info.OsScore = 100
            ElseIf major >= 10 Then
                info.OsScore = 20
            Else
                info.OsScore = 0
            End If
        End Sub
    End Class
End Namespace
