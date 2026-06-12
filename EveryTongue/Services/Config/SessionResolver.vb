Imports System.IO
Imports EveryTongue.Models
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>The fully-resolved view of a session template: gate-validated engine slots plus display + filters (never Nothing — they fall back to app-global values).</summary>
    Friend Class ResolvedSession
        Public Property Session As SessionTemplate
        Public Property Mode As ConnectivityMode = ConnectivityMode.Online
        ''' <summary>Nothing when the slot is unset, missing, or blocked by the Offline gate.</summary>
        Public Property SttTemplate As EngineTemplate
        Public Property TranslateTemplate As EngineTemplate
        Public Property TtsTemplate As EngineTemplate
        Public Property Display As DisplayTemplate
        Public Property Filters As FilterSet
        Public Property Speakers As New List(Of SpeakerProfile)
    End Class

    ''' <summary>
    ''' Resolves a session template's references into concrete templates:
    ''' engine slots are looked up in the library and gate-checked (Offline mode
    ''' blocks online engines, no auto-fallback); Display and Filters fall back
    ''' to the app-global baseline when unreferenced. Every step is logged.
    ''' </summary>
    Friend Class SessionResolver

        Public Shared Function Resolve(sessionId As String, cfg As AppConfig) As ResolvedSession
            Dim store = TemplateLibraryStore.Instance
            Dim session = store.GetSessionTemplate(If(sessionId, ""))
            If session Is Nothing Then
                AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION, $"[Session:{sessionId}] session template not found")
                Return Nothing
            End If

            Dim ctx = $"[Session:{session.Name}({session.Id})]"
            Dim result As New ResolvedSession With {.Session = session, .Mode = session.Mode}

            result.SttTemplate = ConnectivityGate.GateTemplate(
                TemplateLibraryStore.GroupStt,
                store.GetEngineTemplate(TemplateLibraryStore.GroupStt, session.SttTemplateId),
                session.Mode, ctx)
            result.TranslateTemplate = ConnectivityGate.GateTemplate(
                TemplateLibraryStore.GroupTranslate,
                store.GetEngineTemplate(TemplateLibraryStore.GroupTranslate, session.TranslateTemplateId),
                session.Mode, ctx)
            result.TtsTemplate = ConnectivityGate.GateTemplate(
                TemplateLibraryStore.GroupTts,
                store.GetEngineTemplate(TemplateLibraryStore.GroupTts, session.TtsTemplateId),
                session.Mode, ctx)

            result.Display = ResolveDisplay(session.DisplayTemplateId, cfg)
            result.Filters = ResolveFilterSet(session.FilterSetId, cfg)

            For Each speakerId In If(session.SpeakerProfileIds, New List(Of String))
                Dim sp = store.GetSpeakerProfile(speakerId)
                If sp IsNot Nothing Then
                    result.Speakers.Add(sp)
                Else
                    AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION, $"{ctx} speaker profile '{speakerId}' not found — skipped")
                End If
            Next

            AppLogger.Log(LogEvents.CONFIG_TEMPLATE_RESOLVED,
                $"{ctx} resolved: mode={session.Mode}, stt={Describe(result.SttTemplate)}, translate={Describe(result.TranslateTemplate)}, tts={Describe(result.TtsTemplate)}, display={result.Display.Name}, filters={result.Filters.Name}, speakers={result.Speakers.Count}")
            Return result
        End Function

        ''' <summary>Display settings for a session: the referenced template, else the app-global subtitle appearance.</summary>
        Public Shared Function ResolveDisplay(displayTemplateId As String, cfg As AppConfig) As DisplayTemplate
            Dim tpl = TemplateLibraryStore.Instance.GetDisplayTemplate(If(displayTemplateId, ""))
            If tpl IsNot Nothing Then Return tpl
            Return New DisplayTemplate With {
                .Id = "",
                .Name = "(app default)",
                .BgColor = If(cfg.SubtitleBgColor, "#000000"),
                .FgColor = If(cfg.SubtitleFgColor, "#FFFFFF"),
                .FontFamily = If(cfg.SubtitleFontFamily, "Segoe UI"),
                .FontSize = cfg.SubtitleFontSize,
                .FontBold = cfg.SubtitleFontBold
            }
        End Function

        ''' <summary>Filter set for a session: the referenced set, else the global files (paths filled in either way).</summary>
        Public Shared Function ResolveFilterSet(filterSetId As String, cfg As AppConfig) As FilterSet
            Dim baseDir = AppDomain.CurrentDomain.BaseDirectory
            Dim globalGlossary = AppConfig.ResolvePath(If(cfg.TranslationGlossaryPath, ".\translate-server\glossary.json"))
            Dim globalProfanity = Path.Combine(baseDir, "translate-server", "profanity.json")
            Dim globalHallucinations = Path.Combine(baseDir, "live-server", "hallucinations.json")

            Dim selected = TemplateLibraryStore.Instance.GetFilterSet(If(filterSetId, ""))
            If selected Is Nothing Then
                Return New FilterSet With {
                    .Id = "",
                    .Name = "(global)",
                    .GlossaryPath = globalGlossary,
                    .ProfanityPath = globalProfanity,
                    .HallucinationsPath = globalHallucinations
                }
            End If
            ' Empty paths in a named set mean "use the global default file".
            Return New FilterSet With {
                .Id = selected.Id,
                .Name = selected.Name,
                .GlossaryPath = If(String.IsNullOrEmpty(selected.GlossaryPath), globalGlossary, AppConfig.ResolvePath(selected.GlossaryPath)),
                .ProfanityPath = If(String.IsNullOrEmpty(selected.ProfanityPath), globalProfanity, AppConfig.ResolvePath(selected.ProfanityPath)),
                .HallucinationsPath = If(String.IsNullOrEmpty(selected.HallucinationsPath), globalHallucinations, AppConfig.ResolvePath(selected.HallucinationsPath))
            }
        End Function

        Private Shared Function Describe(tpl As EngineTemplate) As String
            If tpl Is Nothing Then Return "(none)"
            Return $"{tpl.Name}[{tpl.EngineKey}]"
        End Function

    End Class

End Namespace
