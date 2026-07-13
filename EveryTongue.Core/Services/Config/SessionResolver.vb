Imports System.IO
Imports EveryTongue.Models
Imports EveryTongue.Models.Templates

Namespace Services.Config

    ''' <summary>
    ''' Resolves per-session filter-set references into concrete file paths.
    ''' (The Tier-3 SessionTemplate model this class once resolved was retired
    ''' in v1.9.6 — ConferenceTemplate is the app's session unit; only the
    ''' filter-set resolution survives because conference rooms use it.)
    ''' </summary>
    Friend Class SessionResolver

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

    End Class

End Namespace
