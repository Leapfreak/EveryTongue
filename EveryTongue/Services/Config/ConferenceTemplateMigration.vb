Imports System.Linq
Imports EveryTongue.Models
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>
    ''' Migrates ConferenceTemplate's legacy embedded engine knobs into the STT
    ''' template library. 1:1 mapping — the library template reuses the conference
    ''' template's Id, which makes the migration idempotent even if config.json
    ''' isn't saved between runs.
    ''' </summary>
    Public Class ConferenceTemplateMigration

        ''' <summary>
        ''' Build the STT library template for a conference template from its
        ''' engine knobs. Each engine's descriptor keeps only the fields it owns,
        ''' and empty ModelPath is omitted so the machine default still applies.
        ''' </summary>
        Public Shared Function BuildSttTemplate(tpl As ConferenceTemplate, fallbackEngineKey As String) As EngineTemplate
            Dim engineKey = If(String.IsNullOrEmpty(tpl.SttBackendKey), If(fallbackEngineKey, ""), tpl.SttBackendKey)
            Dim entry = Stt.SttBackendRegistry.Find(engineKey)
            If entry Is Nothing Then
                entry = Stt.SttBackendRegistry.GetAll()(0)
                engineKey = entry.Key
            End If

            Dim values As New Dictionary(Of String, Object) From {
                {"BeamSize", tpl.BeamSize},
                {"VadSilenceMs", tpl.VadSilenceMs},
                {"MaxSegmentSec", tpl.MaxSegmentSec},
                {"InitialPrompt", If(tpl.InitialPrompt, "")}
            }
            If Not String.IsNullOrEmpty(tpl.ModelPath) Then values("ModelPath") = tpl.ModelPath

            Return New EngineTemplate With {
                .Id = tpl.Id,
                .Name = tpl.Name,
                .EngineKey = engineKey,
                .Config = EngineConfigResolver.BuildTemplateConfig(entry.ConfigDescriptor, values)
            }
        End Function

        ''' <summary>
        ''' The STT library templates a user manages/picks directly — i.e. excluding the
        ''' per-conference companion templates (1:1, sharing the room's Id) that exist only
        ''' to back a room's own engine config. Keeps room setups out of STT-template pickers.
        ''' </summary>
        Public Shared Function StandaloneSttTemplates(cfg As AppConfig) As List(Of EngineTemplate)
            Dim roomIds As New HashSet(Of String)(
                If(cfg?.ConferenceTemplates, New List(Of ConferenceTemplate)).Select(Function(c) c.Id),
                StringComparer.OrdinalIgnoreCase)
            Return TemplateLibraryStore.Instance.GetEngineTemplates(TemplateLibraryStore.GroupStt).
                Where(Function(t) Not roomIds.Contains(t.Id)).ToList()
        End Function

        ''' <summary>
        ''' Ensure every conference template references an STT library template,
        ''' creating one from the legacy embedded fields where missing. Idempotent.
        ''' </summary>
        Public Shared Sub Migrate(cfg As AppConfig)
            If cfg?.ConferenceTemplates Is Nothing Then Return
            Dim migrated = 0
            For Each tpl In cfg.ConferenceTemplates
                Dim existing = TemplateLibraryStore.Instance.GetEngineTemplate(TemplateLibraryStore.GroupStt, tpl.Id)
                If existing Is Nothing Then
                    TemplateLibraryStore.Instance.UpsertEngineTemplate(
                        TemplateLibraryStore.GroupStt, BuildSttTemplate(tpl, cfg.SttBackend))
                    migrated += 1
                End If
                If String.IsNullOrEmpty(tpl.SttTemplateId) Then tpl.SttTemplateId = tpl.Id
            Next
            If migrated > 0 Then
                AppLogger.Log(LogEvents.CONFIG_MIGRATED,
                    $"Migrated {migrated} conference template(s) into the STT template library")
            End If
        End Sub

    End Class

End Namespace
