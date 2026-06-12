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

        ' Clause hold-and-lock dials. HYBRID semantics: a template that stores
        ' these pins them for the session; when a template doesn't, the room
        ' keeps reading the live app-global dials (Options stays live-tunable).
        Public Property HoldClauses As Boolean = False
        Public Property ClauseGraceMs As Integer = 1200
        Public Property ClauseMaxMs As Integer = 8000
        Public Property ClauseMaxChars As Integer = 300
        Public Property ClauseLockOnPunctuation As Boolean = True
        Public Property ClauseMinLockChars As Integer = 12
        Public Property ClauseSentenceEnders As String = ".?!…。？！۔؟"

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
            HoldClauses = cfg.SpeechmaticsHoldClauses
            ClauseGraceMs = cfg.SpeechmaticsClauseGraceMs
            ClauseMaxMs = cfg.SpeechmaticsClauseMaxMs
            ClauseMaxChars = cfg.SpeechmaticsClauseMaxChars
            ClauseLockOnPunctuation = cfg.SpeechmaticsClauseLockOnPunctuation
            ClauseMinLockChars = cfg.SpeechmaticsClauseMinLockChars
            ClauseSentenceEnders = If(String.IsNullOrEmpty(cfg.SpeechmaticsClauseSentenceEnders), ClauseSentenceEnders, cfg.SpeechmaticsClauseSentenceEnders)
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
            New EngineConfigField With {.Key = "EouSilenceMs", .LabelKey = "EngineCfg_SmEouSilenceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 5000},
            New EngineConfigField With {.Key = "HoldClauses", .LabelKey = "EngineCfg_SmHoldClauses", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseGraceMs", .LabelKey = "EngineCfg_SmClauseGraceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 200, .Max = 5000, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseMaxMs", .LabelKey = "EngineCfg_SmClauseMaxMs", .FieldType = EngineConfigFieldType.Integer, .Min = 1000, .Max = 30000, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseMaxChars", .LabelKey = "EngineCfg_SmClauseMaxChars", .FieldType = EngineConfigFieldType.Integer, .Min = 50, .Max = 2000, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseLockOnPunctuation", .LabelKey = "EngineCfg_SmClauseLockOnPunctuation", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseMinLockChars", .LabelKey = "EngineCfg_SmClauseMinLockChars", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 200, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseSentenceEnders", .LabelKey = "EngineCfg_SmClauseSentenceEnders", .FieldType = EngineConfigFieldType.Text, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
