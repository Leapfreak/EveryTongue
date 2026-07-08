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
        ''' <summary>Real-time max_delay (ms) — finalization lookahead. 0 = engine default.</summary>
        Public Property MaxDelayMs As Integer = 0
        ''' <summary>Continuously auto-tune the EOU silence trigger to each speaker's pace (default on).</summary>
        Public Property AutoTuneEou As Boolean = True

        ' Clause hold-and-lock dials. HYBRID semantics: a template that stores
        ' these pins them for the session; when a template doesn't, the room
        ' keeps reading the live app-global dials (Options stays live-tunable).
        Public Property HoldClauses As Boolean = False
        Public Property ClauseGraceMs As Integer = 1400
        Public Property ClauseMaxMs As Integer = 8000
        Public Property ClauseMaxChars As Integer = 300

        ''' <summary>Re-segment each held clause into proper sentences with SaT at the pause (engine-agnostic, list-free). Requires HoldClauses on.</summary>
        Public Property UseSat As Boolean = False
        ''' <summary>SaT split threshold ×100 (10 = 0.10).</summary>
        Public Property SatThresholdPercent As Integer = 10
        ''' <summary>SaT model name (wtpsplit).</summary>
        Public Property SatModel As String = "sat-3l-sm"

        ''' <summary>Feed biblical proper-noun additional_vocab (auto-selected by language). Default OFF — see AppConfig.SpeechmaticsBiblicalVocab.</summary>
        Public Property BiblicalVocab As Boolean = False

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
            MaxDelayMs = cfg.SpeechmaticsMaxDelayMs
            HoldClauses = cfg.SpeechmaticsHoldClauses
            ClauseGraceMs = cfg.SpeechmaticsClauseGraceMs
            ClauseMaxMs = cfg.SpeechmaticsClauseMaxMs
            ClauseMaxChars = cfg.SpeechmaticsClauseMaxChars
            AutoTuneEou = cfg.SpeechmaticsAutoTuneEou
            UseSat = cfg.SpeechmaticsUseSat
            SatThresholdPercent = cfg.SpeechmaticsSatThresholdPercent
            SatModel = If(String.IsNullOrEmpty(cfg.SpeechmaticsSatModel), "sat-3l-sm", cfg.SpeechmaticsSatModel)
            BiblicalVocab = cfg.SpeechmaticsBiblicalVocab
            ' SaT runs on the hold/buffer path, so turning it on implies Hold & merge.
            If UseSat Then HoldClauses = True
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            ' Speechmatics has nothing generic to push onto the runner — all of
            ' its session settings travel as /start JSON via BuildStartJsonExtras.
        End Sub

        ''' <summary>
        ''' Speechmatics' extra /start JSON fields, byte-for-byte what the runner
        ''' used to emit from its (removed) Speechmatics-only properties.
        ''' </summary>
        Public Function BuildStartJsonExtras() As String Implements ICloudSttEngineConfig.BuildStartJsonExtras
            Dim sb As New Text.StringBuilder()
            sb.Append($",""speechmatics_region"":""{EscapeJsonUnquoted(Region)}""")
            sb.Append($",""speechmatics_operating_point"":""{EscapeJsonUnquoted(OperatingPoint)}""")
            If EouSilenceMs > 0 Then
                sb.Append($",""speechmatics_eou_silence_s"":{(EouSilenceMs / 1000.0).ToString(Globalization.CultureInfo.InvariantCulture)}")
            End If
            If MaxDelayMs > 0 Then
                sb.Append($",""speechmatics_max_delay_s"":{(MaxDelayMs / 1000.0).ToString(Globalization.CultureInfo.InvariantCulture)}")
            End If
            sb.Append($",""speechmatics_auto_tune_eou"":{If(AutoTuneEou, "true", "false")}")
            If UseSat Then
                ' Tell live-server to warm the SaT model now (so the first pause isn't slow).
                sb.Append(",""speechmatics_sat"":true")
                sb.Append($",""sat_model"":""{EscapeJsonUnquoted(SatModel)}""")
            End If
            sb.Append($",""speechmatics_biblical_vocab"":{If(BiblicalVocab, "true", "false")}")
            sb.Append($",""enable_translation"":{If(EnableTranslation, "true", "false")}")
            sb.Append($",""translation_targets"":{SerializeStringArray(TranslationTargets)}")
            Return sb.ToString()
        End Function

        ''' <summary>JSON-escape a string value WITHOUT surrounding quotes (same output as the runner's old helper).</summary>
        Private Shared Function EscapeJsonUnquoted(s As String) As String
            If String.IsNullOrEmpty(s) Then Return ""
            Dim quoted = ProcessHelper.EscapeJson(s)
            Return quoted.Substring(1, quoted.Length - 2)
        End Function

        ''' <summary>Serialize a string list to a JSON array (quoted, escaped).</summary>
        Private Shared Function SerializeStringArray(items As List(Of String)) As String
            If items Is Nothing OrElse items.Count = 0 Then Return "[]"
            Dim sb As New Text.StringBuilder("[")
            For i = 0 To items.Count - 1
                If i > 0 Then sb.Append(","c)
                sb.Append(ProcessHelper.EscapeJson(If(items(i), "")))
            Next
            sb.Append("]"c)
            Return sb.ToString()
        End Function
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
            New EngineConfigField With {.Key = "MaxDelayMs", .LabelKey = "EngineCfg_SmMaxDelayMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 6000},
            New EngineConfigField With {.Key = "AutoTuneEou", .LabelKey = "EngineCfg_SmAutoTuneEou", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True},
            New EngineConfigField With {.Key = "HoldClauses", .LabelKey = "EngineCfg_SmHoldClauses", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseGraceMs", .LabelKey = "EngineCfg_SmClauseGraceMs", .FieldType = EngineConfigFieldType.Integer, .Min = 200, .Max = 5000, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseMaxMs", .LabelKey = "EngineCfg_SmClauseMaxMs", .FieldType = EngineConfigFieldType.Integer, .Min = 1000, .Max = 30000, .Advanced = True},
            New EngineConfigField With {.Key = "ClauseMaxChars", .LabelKey = "EngineCfg_SmClauseMaxChars", .FieldType = EngineConfigFieldType.Integer, .Min = 50, .Max = 2000, .Advanced = True},
            New EngineConfigField With {.Key = "UseSat", .LabelKey = "EngineCfg_SmUseSat", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True},
            New EngineConfigField With {.Key = "SatThresholdPercent", .LabelKey = "EngineCfg_SmSatThreshold", .FieldType = EngineConfigFieldType.Integer, .Min = 1, .Max = 90, .Advanced = True},
            New EngineConfigField With {.Key = "BiblicalVocab", .LabelKey = "EngineCfg_SmBiblicalVocab", .FieldType = EngineConfigFieldType.Toggle, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
