Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' ISttBackend using faster-whisper via CTranslate2 (CUDA or CPU).
    ''' Adapter boilerplate lives in RunnerBackedSttBackend; this class only
    ''' assembles the faster-whisper session config.
    ''' </summary>
    Friend Class FasterWhisperBackend
        Inherits RunnerBackedSttBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "faster-whisper (CUDA/CPU)"
            End Get
        End Property

        Public Overrides ReadOnly Property RequiresInternet As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Sub Start(config As SttSessionConfig)
            Dim ec = If(config.Block(Of Configs.FasterWhisperConfig)(), New Configs.FasterWhisperConfig())

            Runner.Backend = "faster-whisper"
            Runner.FiltersHallucinationsPath = config.HallucinationsPath

            Dim appConfig As New AppConfig() With {
                .LiveServerPort = config.ServerPort,
                .PathFasterWhisperModel = ec.ModelPath,
                .NoGpu = False,
                .BeamSize = ec.BeamSize,
                .BestOf = ec.BestOf,
                .LiveVadSilenceMs = ec.VadSilenceMs,
                .LiveMaxSegmentSec = ec.MaxSegmentSec,
                .LiveInterimIntervalMs = ec.InterimIntervalMs,
                .LiveComputeType = ec.ComputeType,
                .InitialPrompt = ec.InitialPrompt
            }
            ' Web-mic rooms: same forward the streaming backends always did
            ' (whisper paths silently captured a local device — 2026-07-29).
            Runner.AudioSource = If(String.IsNullOrEmpty(config.AudioSource), "local", config.AudioSource)
            ' Clause treatment: server-side chunk glue + SaT at real pauses.
            Runner.SatHold = ec.UseSatHold
            Runner.EouAutoTune = ec.AutoTuneEou
            Runner.Start(appConfig, config.DeviceIndex, config.Language, config.TranslateToEnglish)
        End Sub

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            Await Task.CompletedTask
            Return Runner.IsServerReady
        End Function

    End Class

End Namespace
