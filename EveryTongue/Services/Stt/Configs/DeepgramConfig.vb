Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the Deepgram engine. Deepgram endpoints
    ''' server-side, so it has no local VAD knobs — its dials are the model
    ''' (nova-3 default; templates can pin nova-2, whisper, etc.) and the
    ''' endpointing silence.
    ''' </summary>
    Public Class DeepgramConfig
        Implements IEngineConfigBlock, ICloudSttEngineConfig

        ''' <summary>Deepgram model name (e.g. "nova-3", "nova-2", "whisper-large").</summary>
        Public Property Model As String = "nova-3"
        ''' <summary>Endpointing silence (ms). 0 = engine default.</summary>
        Public Property EndpointingMs As Integer = 0

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            ' Deepgram has no app-global dials — defaults live in this block.
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            ' Nothing generic to push onto the runner — all session settings
            ' travel as /start JSON via BuildStartJsonExtras.
        End Sub

        Public Function BuildStartJsonExtras() As String Implements ICloudSttEngineConfig.BuildStartJsonExtras
            Dim sb As New Text.StringBuilder()
            sb.Append($",""deepgram_model"":{ProcessHelper.EscapeJson(If(Model, ""))}")
            If EndpointingMs > 0 Then
                sb.Append($",""deepgram_endpointing_ms"":{EndpointingMs}")
            End If
            Return sb.ToString()
        End Function
    End Class

    Public Class DeepgramConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of DeepgramConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "deepgram"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "Model", .LabelKey = "EngineCfg_DgModel", .FieldType = EngineConfigFieldType.Text, .Advanced = True},
            New EngineConfigField With {.Key = "EndpointingMs", .LabelKey = "EngineCfg_DgEndpointingMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 10000, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
