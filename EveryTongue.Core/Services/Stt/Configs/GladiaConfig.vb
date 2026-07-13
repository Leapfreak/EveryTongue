Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the Gladia engine. Gladia endpoints server-side,
    ''' so its only dial is the endpointing silence; language detection and
    ''' session setup are negotiated by the Python engine module.
    ''' </summary>
    Public Class GladiaConfig
        Implements IEngineConfigBlock, ICloudSttEngineConfig

        ''' <summary>Endpointing silence (ms). 0 = engine default.</summary>
        Public Property EndpointingMs As Integer = 0

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            ' Gladia has no app-global dials — defaults live in this block.
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            ' Nothing generic to push onto the runner — all session settings
            ' travel as /start JSON via BuildStartJsonExtras.
        End Sub

        Public Function BuildStartJsonExtras() As String Implements ICloudSttEngineConfig.BuildStartJsonExtras
            If EndpointingMs > 0 Then
                Return $",""gladia_endpointing_s"":{(EndpointingMs / 1000.0).ToString(Globalization.CultureInfo.InvariantCulture)}"
            End If
            Return ""
        End Function
    End Class

    Public Class GladiaConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of GladiaConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "gladia"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "EndpointingMs", .LabelKey = "EngineCfg_GlEndpointingMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 10000, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
