Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' ISttBackend using whisper.cpp via whisper-server.exe (Vulkan, CUDA or CPU).
    ''' Adapter boilerplate lives in RunnerBackedSttBackend; this class only
    ''' resolves the server binary and assembles the whisper-cpp session config.
    ''' </summary>
    Friend Class WhisperCppBackend
        Inherits RunnerBackedSttBackend

        Private ReadOnly _useGpu As Boolean
        ' When True, launch the CUDA whisper-server build (whisper-server-cuda.exe)
        ' instead of the default Vulkan binary. NVIDIA-only.
        Private ReadOnly _useCuda As Boolean

        Public Sub New(useGpu As Boolean, Optional useCuda As Boolean = False)
            _useGpu = useGpu
            _useCuda = useCuda
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return If(_useGpu, "whisper.cpp (Vulkan)", "whisper.cpp (CPU)")
            End Get
        End Property

        Public Overrides ReadOnly Property RequiresInternet As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Sub Start(config As SttSessionConfig)
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
            Runner.Backend = backendKey
            Runner.WhisperServerPath = serverPath
            Runner.WhisperServerPort = ec.WhisperServerPort
            Runner.SileroVadModelPath = ec.SileroVadModelPath
            Runner.NoGpu = Not _useGpu
            Runner.FiltersHallucinationsPath = config.HallucinationsPath

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
            ' Web-mic rooms: the live-server must skip local capture and take
            ' frames from /audio-in. The streaming backends always forwarded
            ' this; the whisper path silently captured a local device instead
            ' (field finding 2026-07-29).
            Runner.AudioSource = If(String.IsNullOrEmpty(config.AudioSource), "local", config.AudioSource)
            ' Clause treatment (measured 1.35x -> 0.96 fragmentation): the
            ' live-server glues chunks cut without a pause and SaT re-splits
            ' at genuine silences.
            Runner.SatHold = ec.UseSatHold
            Runner.EouAutoTune = ec.AutoTuneEou
            Runner.Start(appConfig, config.DeviceIndex, config.Language, config.TranslateToEnglish)
        End Sub

    End Class

End Namespace
