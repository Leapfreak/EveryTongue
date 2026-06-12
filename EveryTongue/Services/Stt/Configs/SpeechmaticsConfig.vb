Imports System.Text.Json.Serialization
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the Speechmatics engine. Speechmatics endpoints
    ''' server-side, so it has no local VAD knobs — its dials are the region,
    ''' quality tier and end-of-utterance silence.
    ''' </summary>
    Public Class SpeechmaticsConfig
        Implements IEngineConfigBlock, ICloudSttEngineConfig

        Public Property Region As String = "eu2"
        Public Property OperatingPoint As String = "enhanced"
        ''' <summary>End-of-utterance silence trigger (ms). 0 = engine default.</summary>
        Public Property EouSilenceMs As Integer = 0

        ''' <summary>Session-computed, not template content: inline translation toggle.</summary>
        <JsonIgnore>
        Public Property EnableTranslation As Boolean = False
        ''' <summary>Session-computed, not template content: Speechmatics target codes (max 5).</summary>
        <JsonIgnore>
        Public Property TranslationTargets As New List(Of String)

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            Region = If(String.IsNullOrEmpty(cfg.SpeechmaticsRegion), "eu2", cfg.SpeechmaticsRegion)
            OperatingPoint = If(String.IsNullOrEmpty(cfg.SpeechmaticsOperatingPoint), "enhanced", cfg.SpeechmaticsOperatingPoint)
            EouSilenceMs = cfg.SpeechmaticsEouSilenceMs
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            runner.SttRegion = Region
            runner.SttOperatingPoint = OperatingPoint
            runner.SttEouSilenceMs = EouSilenceMs
            runner.SttEnableTranslation = EnableTranslation
            runner.SttTranslationTargets = If(TranslationTargets, New List(Of String))
        End Sub
    End Class

    Public Class SpeechmaticsConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of SpeechmaticsConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "speechmatics"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "Region", .LabelKey = "EngineCfg_SmRegion", .FieldType = EngineConfigFieldType.Choice, .Choices = New List(Of String) From {"eu2", "us"}},
            New EngineConfigField With {.Key = "OperatingPoint", .LabelKey = "EngineCfg_SmOperatingPoint", .FieldType = EngineConfigFieldType.Choice, .Choices = New List(Of String) From {"enhanced", "standard"}},
            New EngineConfigField With {.Key = "EouSilenceMs", .LabelKey = "EngineCfg_SmEouSilenceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 5000}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
