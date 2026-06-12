Imports EveryTongue.Models
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the whisper.cpp engine family
    ''' (whisper-cpp-vulkan / whisper-cpp-cuda / whisper-cpp-cpu).
    ''' Only this engine's knobs live here — no other engine reads it.
    ''' </summary>
    Public Class WhisperCppConfig
        Implements IEngineConfigBlock

        Public Property ModelPath As String = ""
        Public Property WhisperServerPath As String = ""
        Public Property WhisperServerPort As Integer = 8178
        Public Property SileroVadModelPath As String = ""
        Public Property BeamSize As Integer = 7
        Public Property BestOf As Integer = 5
        Public Property VadSilenceMs As Integer = 800
        Public Property MaxSegmentSec As Integer = 15
        Public Property InterimIntervalMs As Integer = 1000
        Public Property InitialPrompt As String = ""

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            If String.IsNullOrEmpty(ModelPath) Then ModelPath = cfg.PathWhisperCppModel
            If String.IsNullOrEmpty(WhisperServerPath) Then WhisperServerPath = AppConfig.ResolvePath(cfg.PathWhisperServer)
            If String.IsNullOrEmpty(SileroVadModelPath) Then SileroVadModelPath = AppConfig.ResolvePath(cfg.PathSileroVadModel)
            WhisperServerPort = cfg.WhisperServerPort
            BestOf = cfg.BestOf
            InterimIntervalMs = cfg.LiveInterimIntervalMs
        End Sub
    End Class

    ''' <summary>Descriptor for the whisper.cpp engine family (one instance per engine key, shared block type).</summary>
    Public Class WhisperCppConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of WhisperCppConfig)

        Private ReadOnly _engineKey As String

        Public Sub New(engineKey As String)
            _engineKey = engineKey
        End Sub

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return _engineKey
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "ModelPath", .LabelKey = "EngineCfg_ModelPath", .FieldType = EngineConfigFieldType.FilePath},
            New EngineConfigField With {.Key = "BeamSize", .LabelKey = "EngineCfg_BeamSize", .FieldType = EngineConfigFieldType.Integer, .Min = 1, .Max = 10},
            New EngineConfigField With {.Key = "BestOf", .LabelKey = "EngineCfg_BestOf", .FieldType = EngineConfigFieldType.Integer, .Min = 1, .Max = 10, .Advanced = True},
            New EngineConfigField With {.Key = "VadSilenceMs", .LabelKey = "EngineCfg_VadSilenceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 100, .Max = 5000},
            New EngineConfigField With {.Key = "MaxSegmentSec", .LabelKey = "EngineCfg_MaxSegmentSec", .FieldType = EngineConfigFieldType.Integer, .Min = 3, .Max = 60},
            New EngineConfigField With {.Key = "InterimIntervalMs", .LabelKey = "EngineCfg_InterimIntervalMs", .FieldType = EngineConfigFieldType.Integer, .Min = 200, .Max = 10000, .Advanced = True},
            New EngineConfigField With {.Key = "InitialPrompt", .LabelKey = "EngineCfg_InitialPrompt", .FieldType = EngineConfigFieldType.Text, .Advanced = True},
            New EngineConfigField With {.Key = "WhisperServerPath", .LabelKey = "EngineCfg_WhisperServerPath", .FieldType = EngineConfigFieldType.FilePath, .Advanced = True},
            New EngineConfigField With {.Key = "WhisperServerPort", .LabelKey = "EngineCfg_WhisperServerPort", .FieldType = EngineConfigFieldType.Integer, .Min = 1024, .Max = 65535, .Advanced = True},
            New EngineConfigField With {.Key = "SileroVadModelPath", .LabelKey = "EngineCfg_SileroVadModelPath", .FieldType = EngineConfigFieldType.FilePath, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
