Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the Google Cloud STT engine. Google streams via
    ''' gRPC when available and otherwise falls back to the local VAD pipeline,
    ''' so it owns the VAD/decode knobs that fallback consumes.
    ''' </summary>
    Public Class GoogleSttConfig
        Implements IEngineConfigBlock, ICloudSttEngineConfig

        Public Property BeamSize As Integer = 7
        Public Property BestOf As Integer = 5
        Public Property VadSilenceMs As Integer = 800
        Public Property MaxSegmentSec As Integer = 15
        Public Property InterimIntervalMs As Integer = 1000
        Public Property InitialPrompt As String = ""

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            BestOf = cfg.BestOf
            InterimIntervalMs = cfg.LiveInterimIntervalMs
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            runnerConfig.BeamSize = BeamSize
            runnerConfig.BestOf = BestOf
            runnerConfig.LiveVadSilenceMs = VadSilenceMs
            runnerConfig.LiveMaxSegmentSec = MaxSegmentSec
            runnerConfig.LiveInterimIntervalMs = InterimIntervalMs
            runnerConfig.InitialPrompt = InitialPrompt
        End Sub

        Public Function BuildStartJsonExtras() As String Implements ICloudSttEngineConfig.BuildStartJsonExtras
            ' Google contributes no engine-specific /start fields.
            Return ""
        End Function
    End Class

    Public Class GoogleSttConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of GoogleSttConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "google-cloud-stt"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "VadSilenceMs", .LabelKey = "EngineCfg_VadSilenceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 100, .Max = 5000},
            New EngineConfigField With {.Key = "MaxSegmentSec", .LabelKey = "EngineCfg_MaxSegmentSec", .FieldType = EngineConfigFieldType.Integer, .Min = 3, .Max = 60},
            New EngineConfigField With {.Key = "InterimIntervalMs", .LabelKey = "EngineCfg_InterimIntervalMs", .FieldType = EngineConfigFieldType.Integer, .Min = 200, .Max = 10000, .Advanced = True},
            New EngineConfigField With {.Key = "InitialPrompt", .LabelKey = "EngineCfg_InitialPrompt", .FieldType = EngineConfigFieldType.Text, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
