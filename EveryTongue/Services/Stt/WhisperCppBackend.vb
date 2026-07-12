Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' ISttBackend using whisper.cpp via whisper-server.exe (Vulkan or CPU).
    ''' Thin adapter — delegates to LiveStreamRunner with backend="whisper-cpp".
    ''' </summary>
    Friend Class WhisperCppBackend
        Implements ISttBackend

        Private ReadOnly _runner As New LiveStreamRunner()
        Private ReadOnly _useGpu As Boolean
        ' When True, launch the CUDA whisper-server build (whisper-server-cuda.exe)
        ' instead of the default Vulkan binary. NVIDIA-only.
        Private ReadOnly _useCuda As Boolean

        Public Sub New(useGpu As Boolean, Optional useCuda As Boolean = False)
            _useGpu = useGpu
            _useCuda = useCuda

            AddHandler _runner.OutputLineUpdated, Sub(s, line)
                                                      RaiseEvent OutputUpdated(Me, New SttOutputEventArgs(line))
                                                  End Sub

            AddHandler _runner.OutputLineCommitted, Sub(s, line)
                                                        Dim text = line
                                                        Dim lang = ""
                                                        Dim tabIdx = line.IndexOf(vbTab)
                                                        If tabIdx > 0 Then
                                                            lang = line.Substring(0, tabIdx)
                                                            text = line.Substring(tabIdx + 1)
                                                        End If
                                                        RaiseEvent OutputCommitted(Me, New SttOutputEventArgs(text, lang))
                                                    End Sub

            AddHandler _runner.ErrorReceived, Sub(s, line)
                                                  RaiseEvent ErrorReceived(Me, line)
                                              End Sub
        End Sub

        Public ReadOnly Property Name As String Implements ISttBackend.Name
            Get
                Return If(_useGpu, "whisper.cpp (Vulkan)", "whisper.cpp (CPU)")
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ISttBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsAvailable As Boolean Implements ISttBackend.IsAvailable
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean Implements ISttBackend.IsRunning
            Get
                Return _runner.IsRunning
            End Get
        End Property

        Public ReadOnly Property Transcript As String Implements ISttBackend.Transcript
            Get
                Return _runner.Transcript
            End Get
        End Property

        Public Event OutputUpdated As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputUpdated
        Public Event OutputCommitted As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputCommitted
        ' Offline engine — never raised; declared to satisfy the interface.
        Public Event OutputCommittedTranslated As EventHandler(Of SttTranslatedCommitEventArgs) Implements ISttBackend.OutputCommittedTranslated
        Public Event ErrorReceived As EventHandler(Of String) Implements ISttBackend.ErrorReceived

        Public Sub Start(config As SttSessionConfig) Implements ISttBackend.Start
            Dim ec = If(config.Block(Of Configs.WhisperCppConfig)(), New Configs.WhisperCppConfig())

            ' Resolve the server binary. The CUDA engine key must launch the CUDA build
            ' (whisper-server-cuda.exe), which lives next to the Vulkan build. Without this
            ' the CUDA selection silently ran the Vulkan binary — and mixing a Vulkan whisper
            ' context with a resident CUDA context (e.g. NLLB) on the same NVIDIA GPU can crash
            ' ggml on init. Fall back to the Vulkan binary if the CUDA build is missing.
            Dim serverPath = ec.WhisperServerPath
            Dim backendKey = If(_useGpu, "whisper-cpp-vulkan", "whisper-cpp-cpu")
            If _useCuda AndAlso Not String.IsNullOrEmpty(serverPath) Then
                Dim dir = IO.Path.GetDirectoryName(EveryTongue.Models.AppConfig.ResolvePath(serverPath))
                Dim cudaPath = If(String.IsNullOrEmpty(dir), "whisper-server-cuda.exe",
                                  IO.Path.Combine(dir, "whisper-server-cuda.exe"))
                If IO.File.Exists(cudaPath) Then
                    serverPath = cudaPath
                    backendKey = "whisper-cpp-cuda"
                Else
                    AppLogger.Log(LogEvents.STT_WHISPER_SERVER_START,
                        $"whisper-cpp-cuda selected but '{cudaPath}' not found — using the Vulkan build instead")
                End If
            End If

            ' Configure the runner for whisper-cpp backend
            _runner.Backend = backendKey
            _runner.WhisperServerPath = serverPath
            _runner.WhisperServerPort = ec.WhisperServerPort
            _runner.SileroVadModelPath = ec.SileroVadModelPath
            _runner.NoGpu = Not _useGpu
            _runner.FiltersHallucinationsPath = config.HallucinationsPath

            Dim appConfig As New AppConfig() With {
                .LiveServerPort = config.ServerPort,
                .PathWhisperCppModel = ec.ModelPath,
                .PathWhisperServer = serverPath,
                .WhisperServerPort = ec.WhisperServerPort,
                .NoGpu = Not _useGpu,
                .BeamSize = ec.BeamSize,
                .BestOf = ec.BestOf,
                .LiveVadSilenceMs = ec.VadSilenceMs,
                .LiveMaxSegmentSec = ec.MaxSegmentSec,
                .LiveInterimIntervalMs = ec.InterimIntervalMs,
                .InitialPrompt = ec.InitialPrompt
            }
            _runner.Start(appConfig, config.DeviceIndex, config.Language, config.TranslateToEnglish)
        End Sub

        Public Sub [Stop]() Implements ISttBackend.Stop
            _runner.Stop()
        End Sub

        Public Function UpdateConfigAsync(params As Dictionary(Of String, Object)) As Task Implements ISttBackend.UpdateConfigAsync
            Return _runner.UpdateConfigAsync(params)
        End Function

        Public Function EnumerateDevicesAsync(pythonExePath As String) As List(Of AudioDeviceInfo) Implements ISttBackend.EnumerateDevicesAsync
            Dim rawDevices = _runner.EnumerateDevicesAsync(pythonExePath)
            Dim result As New List(Of AudioDeviceInfo)
            For Each raw In rawDevices
                Dim colonIdx = raw.IndexOf(":"c)
                If colonIdx > 0 Then
                    Dim idStr = raw.Substring(0, colonIdx).Trim()
                    Dim name = raw.Substring(colonIdx + 1).Trim()
                    Dim id As Integer
                    If Integer.TryParse(idStr, id) Then
                        result.Add(New AudioDeviceInfo(id, name))
                    Else
                        result.Add(New AudioDeviceInfo(0, raw))
                    End If
                Else
                    result.Add(New AudioDeviceInfo(0, raw))
                End If
            Next
            Return result
        End Function

        Public Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean) Implements ISttBackend.CheckHealthAsync
            ' Deep check: healthy = actually CAPTURING (model loaded, pipeline alive),
            ' not merely "HTTP is up" — the shallow check fired ready-chimes early.
            Return _runner.CheckCapturingAsync(ct)
        End Function

        Public Function GetStatsAsync() As Task(Of String) Implements ISttBackend.GetStatsAsync
            Return _runner.GetStatsAsync()
        End Function

        Public Function SaveTranscript(filePath As String) As Boolean Implements ISttBackend.SaveTranscript
            Return _runner.SaveTranscript(filePath)
        End Function

    End Class

End Namespace
