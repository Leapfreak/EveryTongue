Imports Microsoft.Extensions.DependencyInjection
Imports EveryTongue.Services.Infrastructure

Namespace Services.Translation

    ''' <summary>
    ''' Central registry of available translation backends.
    ''' To add a new engine: (1) implement ITranslationBackend, (2) register in DI,
    ''' (3) add one line here. The Options dialog reads from this automatically.
    ''' </summary>
    Friend Class TranslationBackendRegistry

        Public Class Entry
            Public Property Key As String
            Public Property DisplayName As String
            Public Property RequiresInternet As Boolean
            Public Property RequiresApiKey As Boolean
            ''' <summary>Model type passed to the Python sidecar (e.g. "nllb"). Null for cloud backends.</summary>
            Public Property ModelType As String
            ''' <summary>Default model directory path (e.g. ".\nllb-model"). Null for cloud backends.</summary>
            Public Property DefaultModelPath As String
            ''' <summary>Name used by TranslationOrchestrator to identify the backend (e.g. "Local", "DeepL").</summary>
            Public Property BackendName As String
            ''' <summary>Self-description of this engine's config block. Empty (BasicEngineConfigDescriptor) until the engine exposes per-session knobs.</summary>
            Public Property ConfigDescriptor As Config.IEngineConfigDescriptor
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "nllb", .DisplayName = "NLLB 1.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-model", .BackendName = "Local", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("nllb")},
            New Entry With {.Key = "nllb-3.3b", .DisplayName = "NLLB 3.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-3.3b-model", .BackendName = "Local", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("nllb-3.3b")},
            New Entry With {.Key = "google-translate", .DisplayName = "Google Translate (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Google", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("google-translate")},
            New Entry With {.Key = "deepl", .DisplayName = "DeepL (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "DeepL", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("deepl")},
            New Entry With {.Key = "azure-translator", .DisplayName = "Azure Translator (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Azure", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("azure-translator")}
        }

        Public Shared Function GetAll() As IReadOnlyList(Of Entry)
            Return _backends
        End Function

        Public Shared Sub Register(key As String, displayName As String, requiresInternet As Boolean, Optional requiresApiKey As Boolean = False)
            If Not _backends.Any(Function(b) b.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) Then
                _backends.Add(New Entry With {
                    .Key = key,
                    .DisplayName = displayName,
                    .RequiresInternet = requiresInternet,
                    .RequiresApiKey = requiresApiKey
                })
            End If
        End Sub

        ''' <summary>
        ''' Look up a registry entry by key. Returns Nothing if not found.
        ''' </summary>
        Public Shared Function Find(key As String) As Entry
            Return _backends.FirstOrDefault(
                Function(e) e.Key.Equals(If(key, ""), StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>
        ''' Resolve the API key for a translation backend: the engine's own stored
        ''' key wins; when empty, fall back to the key of the STT engine that
        ''' declares this backend as its companion (e.g. one Google Cloud key
        ''' powers both Google STT and Google Translate).
        ''' </summary>
        Friend Shared Function ResolveTranslationApiKey(cfg As EveryTongue.Models.AppConfig, translationKey As String) As String
            If cfg Is Nothing OrElse String.IsNullOrEmpty(translationKey) Then Return ""
            Dim own = cfg.GetTranslationApiKey(translationKey)
            If Not String.IsNullOrEmpty(own) Then Return own
            Dim companionStt = Stt.SttBackendRegistry.GetAll().FirstOrDefault(
                Function(s) translationKey.Equals(If(s.CompanionTranslationKey, ""), StringComparison.OrdinalIgnoreCase))
            If companionStt IsNot Nothing Then Return cfg.GetSttApiKey(companionStt.Key)
            Return ""
        End Function

        ''' <summary>
        ''' Push resolved API keys into every key-requiring cloud translation backend
        ''' registered in DI. Called after the Kestrel host starts and again whenever
        ''' the Options dialog saves, so running backends pick up key changes live.
        ''' </summary>
        Friend Shared Sub ConfigureCloudApiKeys(services As IServiceProvider, cfg As EveryTongue.Models.AppConfig)
            If services Is Nothing OrElse cfg Is Nothing Then Return
            Dim cloudBackends = services.GetServices(Of Interfaces.ITranslationBackend)().
                OfType(Of CloudTranslationBackend)().ToList()
            For Each entry In _backends.Where(Function(b) b.RequiresApiKey)
                Dim backend = cloudBackends.FirstOrDefault(
                    Function(b) b.Name.Equals(entry.BackendName, StringComparison.OrdinalIgnoreCase))
                If backend Is Nothing Then Continue For
                Dim key = ResolveTranslationApiKey(cfg, entry.Key)
                backend.Configure(key)
                AppLogger.Log(LogEvents.TRANS_BACKEND_ACTIVE,
                    $"Cloud translation backend '{entry.Key}': API key {If(String.IsNullOrEmpty(key), "no key", "configured")}")
            Next
        End Sub

    End Class

End Namespace
