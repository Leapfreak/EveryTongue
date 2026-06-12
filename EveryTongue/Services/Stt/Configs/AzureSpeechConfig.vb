Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Config

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Config block owned by the Azure AI Speech engine. Azure endpoints
    ''' server-side, so it has no local VAD knobs — its dials are the service
    ''' region, the segmentation silence timeout and the auto-detect candidate
    ''' language list (continuous language identification, max 10 locales).
    ''' </summary>
    Public Class AzureSpeechConfig
        Implements IEngineConfigBlock, ICloudSttEngineConfig

        ''' <summary>Azure service region (e.g. "westeurope", "eastus", "australiaeast").</summary>
        Public Property Region As String = "westeurope"
        ''' <summary>Segmentation silence timeout (ms). 0 = engine default.</summary>
        Public Property SegmentationSilenceMs As Integer = 0
        ''' <summary>CSV of ISO 639-1 candidate languages used when language is "auto" (Azure caps at 10).</summary>
        Public Property AutoDetectLanguages As String = "en,es,fr,de,it,pt,nl,pl"

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
            ' Azure Speech has no app-global dials — defaults live in this block.
        End Sub

        Public Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig) Implements ICloudSttEngineConfig.ConfigureRunner
            ' Nothing generic to push onto the runner — all session settings
            ' travel as /start JSON via BuildStartJsonExtras.
        End Sub

        Public Function BuildStartJsonExtras() As String Implements ICloudSttEngineConfig.BuildStartJsonExtras
            Dim sb As New Text.StringBuilder()
            sb.Append($",""azure_region"":{ProcessHelper.EscapeJson(If(Region, ""))}")
            If SegmentationSilenceMs > 0 Then
                sb.Append($",""azure_segmentation_ms"":{SegmentationSilenceMs}")
            End If
            If Not String.IsNullOrWhiteSpace(AutoDetectLanguages) Then
                sb.Append($",""azure_autodetect_languages"":{ProcessHelper.EscapeJson(AutoDetectLanguages)}")
            End If
            Return sb.ToString()
        End Function
    End Class

    Public Class AzureSpeechConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of AzureSpeechConfig)

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return "azure-speech"
            End Get
        End Property

        Private Shared ReadOnly _fields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField) From {
            New EngineConfigField With {.Key = "Region", .LabelKey = "EngineCfg_AzRegion", .FieldType = EngineConfigFieldType.Text},
            New EngineConfigField With {.Key = "SegmentationSilenceMs", .LabelKey = "EngineCfg_AzSegmentationMs", .FieldType = EngineConfigFieldType.Integer, .Min = 0, .Max = 5000, .Advanced = True},
            New EngineConfigField With {.Key = "AutoDetectLanguages", .LabelKey = "EngineCfg_AzAutoDetectLanguages", .FieldType = EngineConfigFieldType.Text, .Advanced = True}
        }

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _fields
            End Get
        End Property
    End Class

End Namespace
