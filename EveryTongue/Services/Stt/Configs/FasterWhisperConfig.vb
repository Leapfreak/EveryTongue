Imports EveryTongue.Models
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>Config block owned by the faster-whisper engine.</summary>
    Public Class FasterWhisperConfig
        Implements IEngineConfigBlock

        Public Property ModelPath As String = ""
        Public Property ComputeType As String = "int8_float16"
        Public Property BeamSize As Integer = 7
        Public Property BestOf As Integer = 5
        Public Property VadSilenceMs As Integer = 800
        Public Property MaxSegmentSec As Integer = 15
        Public Property InterimIntervalMs As Integer = 1000
        Public Property InitialPrompt As String = ""

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            If String.IsNullOrEmpty(ModelPath) Then ModelPath = cfg.PathFasterWhisperModel
            ComputeType = cfg.LiveComputeType
            BestOf = cfg.BestOf
            InterimIntervalMs = cfg.LiveInterimIntervalMs
        End Sub
    End Class

    Public Class FasterWhisperConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of FasterWhisperConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "faster-whisper"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "ModelPath", .LabelKey = "EngineCfg_ModelPath", .FieldType = EngineConfigFieldType.DirectoryPath},
            New EngineConfigField With {.Key = "ComputeType", .LabelKey = "EngineCfg_ComputeType", .FieldType = EngineConfigFieldType.Choice, .Choices = New List(Of String) From {"int8", "int8_float16", "float16", "float32"}, .Advanced = True},
            New EngineConfigField With {.Key = "BeamSize", .LabelKey = "EngineCfg_BeamSize", .FieldType = EngineConfigFieldType.Integer, .Min = 1, .Max = 10},
            New EngineConfigField With {.Key = "BestOf", .LabelKey = "EngineCfg_BestOf", .FieldType = EngineConfigFieldType.Integer, .Min = 1, .Max = 10, .Advanced = True},
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
