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
            ''' <summary>True when the engine needs a per-engine endpoint/region value (Options shows the Endpoint field).</summary>
            Public Property RequiresEndpoint As Boolean = False
            ''' <summary>Default endpoint URL (or region name) used when the user hasn't set one.</summary>
            Public Property DefaultEndpoint As String = ""
            ''' <summary>Self-description of this engine's config block. Empty (BasicEngineConfigDescriptor) until the engine exposes per-session knobs.</summary>
            Public Property ConfigDescriptor As Config.IEngineConfigDescriptor
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "nllb", .DisplayName = "NLLB 1.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-model", .BackendName = "Local", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("nllb")},
            New Entry With {.Key = "nllb-3.3b", .DisplayName = "NLLB 3.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-3.3b-model", .BackendName = "Local", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("nllb-3.3b")},
            New Entry With {.Key = "google-translate", .DisplayName = "Google Translate (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Google", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("google-translate")},
            New Entry With {.Key = "deepl", .DisplayName = "DeepL (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "DeepL", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("deepl")},
            New Entry With {.Key = "azure-translator", .DisplayName = "Azure Translator (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Azure", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("azure-translator")},
            New Entry With {.Key = "deepseek", .DisplayName = "DeepSeek (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "DeepSeek", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("deepseek")},
            New Entry With {.Key = "openai", .DisplayName = "OpenAI (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "OpenAI", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("openai")},
            New Entry With {.Key = "libretranslate", .DisplayName = "LibreTranslate (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "LibreTranslate", .RequiresEndpoint = True, .DefaultEndpoint = "https://libretranslate.com", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("libretranslate")},
            New Entry With {.Key = "amazon-translate", .DisplayName = "Amazon Translate (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Amazon", .RequiresEndpoint = True, .DefaultEndpoint = "us-east-1", .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("amazon-translate")}
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
        ''' Look up a registry entry by the orchestrator backend name (e.g. "DeepL"
        ''' → the "deepl" entry). Returns Nothing if not found.
        ''' </summary>
        Public Shared Function FindByBackendName(backendName As String) As Entry
            Return _backends.FirstOrDefault(
                Function(e) If(e.BackendName, "").Equals(If(backendName, ""), StringComparison.OrdinalIgnoreCase))
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
        ''' Resolve the EFFECTIVE translation engine key from config: the user's
        ''' selected engine, unless the configured STT engine declares a companion
        ''' translation backend and an STT API key is present (one cloud key powers
        ''' both STT and translation, and the companion is much faster than the
        ''' local model). Single source of truth — FormMain.StartTranslationService
        ''' and the desktop pipes (Translate workspace, Transcribe job) all use this.
        ''' </summary>
        Friend Shared Function ResolveEffectiveBackendKey(cfg As EveryTongue.Models.AppConfig) As String
            Dim configKey = If(cfg?.TranslationBackend, "nllb")
            Dim sttKey = If(cfg?.SttBackend, "")
            Dim companionKey = If(Stt.SttBackendRegistry.Find(sttKey)?.CompanionTranslationKey, "")
            If Not String.IsNullOrEmpty(companionKey) AndAlso
               Not String.IsNullOrEmpty(cfg.GetSttApiKey(sttKey)) Then
                Return companionKey
            End If
            Return configKey
        End Function

        ''' <summary>
        ''' When the effective configured engine is a CLOUD backend that is
        ''' registered and available (has an API key) on the orchestrator, make it
        ''' the orchestrator's active backend (syncs config → orchestrator, which
        ''' covers cloud-only machines where the NLLB sidecar never starts) and
        ''' return True — the caller should route through the orchestrator.
        ''' Returns False when the effective engine is the local sidecar, the
        ''' orchestrator is unavailable (server down), or the cloud backend has no
        ''' key — the caller keeps its existing local/NLLB path.
        ''' </summary>
        Friend Shared Function TryActivateConfiguredCloudBackend(svc As Interfaces.ITranslationService,
                                                                 cfg As EveryTongue.Models.AppConfig) As Boolean
            If svc Is Nothing OrElse cfg Is Nothing Then Return False
            Dim entry = Find(ResolveEffectiveBackendKey(cfg))
            If entry Is Nothing OrElse Not entry.RequiresInternet Then Return False
            Dim info = svc.GetAllBackends().FirstOrDefault(
                Function(b) If(b.Name, "").Equals(If(entry.BackendName, ""), StringComparison.OrdinalIgnoreCase))
            If info Is Nothing OrElse Not info.IsAvailable Then Return False
            If Not info.IsActive Then
                svc.SetActiveBackend(entry.BackendName)
                AppLogger.Log(LogEvents.TRANS_BACKEND_ACTIVE,
                    $"Synced orchestrator active backend to configured engine '{entry.Key}' ({entry.BackendName})")
            End If
            Return True
        End Function

        ''' <summary>
        ''' Resolve the endpoint (URL or region) for a translation backend: the
        ''' user's stored value wins; when empty, fall back to the registry
        ''' entry's DefaultEndpoint. Returns "" for engines without endpoints.
        ''' </summary>
        Friend Shared Function ResolveTranslationEndpoint(cfg As EveryTongue.Models.AppConfig, translationKey As String) As String
            If cfg Is Nothing OrElse String.IsNullOrEmpty(translationKey) Then Return ""
            Dim own = cfg.GetTranslationEndpoint(translationKey)
            If Not String.IsNullOrEmpty(own) Then Return own
            Return If(Find(translationKey)?.DefaultEndpoint, "")
        End Function

        ''' <summary>
        ''' Push resolved API keys (and per-engine endpoints, for engines that
        ''' declare RequiresEndpoint) into every key-requiring cloud translation
        ''' backend registered in DI. Called after the Kestrel host starts and
        ''' again whenever the Options dialog saves, so running backends pick up
        ''' key/endpoint changes live.
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
                ' Never initialise a backend that has no key. An unselected /
                ' unconfigured engine must not be touched at startup — and this
                ' also avoids loading an optional vendor SDK assembly (e.g.
                ' AWSSDK for Amazon) merely to configure an engine nobody uses.
                If String.IsNullOrEmpty(key) Then
                    AppLogger.Log(LogEvents.TRANS_BACKEND_ACTIVE,
                        $"Cloud translation backend '{entry.Key}': no key — skipped")
                    Continue For
                End If
                ' Configure defensively: a backend whose optional dependency (a
                ' managed SDK DLL) is missing must log and be skipped, never take
                ' down server startup for every other engine.
                Try
                    backend.Configure(key)
                    Dim endpointNote = ""
                    If entry.RequiresEndpoint Then
                        Dim endpoint = ResolveTranslationEndpoint(cfg, entry.Key)
                        backend.ConfigureEndpoint(endpoint)
                        endpointNote = $", endpoint '{endpoint}'"
                    End If
                    AppLogger.Log(LogEvents.TRANS_BACKEND_ACTIVE,
                        $"Cloud translation backend '{entry.Key}': API key configured{endpointNote}")
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_BACKEND_FALLBACK,
                        $"Cloud translation backend '{entry.Key}' unavailable — {ex.GetType().Name}: {ex.Message} (engine disabled; a required component may be missing)")
                End Try
            Next
        End Sub

    End Class

End Namespace
