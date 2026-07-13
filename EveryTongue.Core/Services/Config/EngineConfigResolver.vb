Imports System.Text.Json
Imports EveryTongue.Models
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>
    ''' Resolves an engine config block in the plan's fixed order:
    '''   engine defaults → machine baseline → named template → per-session overrides
    ''' Every step is logged so a session's effective config is readable from the
    ''' log with no reactive instrumentation. Engine-agnostic — driven entirely
    ''' through IEngineConfigDescriptor.
    ''' </summary>
    Public Class EngineConfigResolver

        ''' <summary>Resolve a block from an explicit descriptor (any group).</summary>
        Public Shared Function Resolve(descriptor As IEngineConfigDescriptor,
                                       appConfig As AppConfig,
                                       Optional template As EngineTemplate = Nothing,
                                       Optional fieldOverrides As IDictionary(Of String, Object) = Nothing,
                                       Optional contextLabel As String = "") As IEngineConfigBlock
            Dim block = descriptor.CreateDefault()
            block.ApplyMachineBaseline(appConfig)
            AppLogger.Log(LogEvents.CONFIG_ENGINE_RESOLVED,
                $"{contextLabel} engine={descriptor.EngineKey}: defaults + machine baseline")

            If template IsNot Nothing Then
                If Not String.IsNullOrEmpty(template.EngineKey) AndAlso
                   Not template.EngineKey.Equals(descriptor.EngineKey, StringComparison.OrdinalIgnoreCase) Then
                    AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                        $"{contextLabel} template '{template.Name}' is bound to engine '{template.EngineKey}', not '{descriptor.EngineKey}' — skipped")
                ElseIf template.Config.HasValue Then
                    descriptor.ApplyJson(block, template.Config.Value)
                    AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                        $"{contextLabel} engine={descriptor.EngineKey}: template '{template.Name}' ({template.Id}) applied")
                End If
            End If

            If fieldOverrides IsNot Nothing AndAlso fieldOverrides.Count > 0 Then
                Dim applied = descriptor.ApplyOverrides(block, fieldOverrides)
                AppLogger.Log(LogEvents.CONFIG_OVERRIDE_APPLIED,
                    $"{contextLabel} engine={descriptor.EngineKey}: overrides [{String.Join(", ", applied)}]")
            End If

            For Each warning In descriptor.Validate(block)
                AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                    $"{contextLabel} engine={descriptor.EngineKey}: {warning}")
            Next

            Return block
        End Function

        ''' <summary>Resolve an STT engine's block by registry key.</summary>
        Public Shared Function ResolveStt(engineKey As String,
                                          appConfig As AppConfig,
                                          Optional template As EngineTemplate = Nothing,
                                          Optional fieldOverrides As IDictionary(Of String, Object) = Nothing,
                                          Optional contextLabel As String = "") As IEngineConfigBlock
            Dim entry = Stt.SttBackendRegistry.Find(engineKey)
            If entry Is Nothing Then
                ' Mirror SttBackendRegistry.CreateBackend: unknown keys fall back to the first registered engine.
                entry = Stt.SttBackendRegistry.GetAll()(0)
                AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                    $"{contextLabel} unknown STT engine '{engineKey}' — falling back to '{entry.Key}'")
            End If
            If entry.ConfigDescriptor Is Nothing Then
                AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                    $"{contextLabel} no config descriptor registered for STT engine '{entry.Key}'")
                Return Nothing
            End If
            Return Resolve(entry.ConfigDescriptor, appConfig, template, fieldOverrides, contextLabel)
        End Function

        ''' <summary>
        ''' Build a partial template config block from loose values: only keys the
        ''' engine actually declares survive, and only provided keys are stored —
        ''' so applying the template later never clobbers machine-baseline values.
        ''' </summary>
        Public Shared Function BuildTemplateConfig(descriptor As IEngineConfigDescriptor,
                                                   values As IDictionary(Of String, Object)) As JsonElement
            Dim filtered As New Dictionary(Of String, Object)
            If values IsNot Nothing Then
                For Each field In descriptor.Fields
                    For Each kvp In values
                        If kvp.Key.Equals(field.Key, StringComparison.OrdinalIgnoreCase) Then
                            filtered(field.Key) = kvp.Value
                            Exit For
                        End If
                    Next
                Next
            End If
            Using doc = JsonDocument.Parse(JsonSerializer.Serialize(filtered))
                Return doc.RootElement.Clone()
            End Using
        End Function

    End Class

End Namespace
