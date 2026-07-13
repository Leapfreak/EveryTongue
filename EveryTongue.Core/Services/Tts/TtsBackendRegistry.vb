Imports Microsoft.Extensions.DependencyInjection
Imports EveryTongue.Services.Infrastructure

Namespace Services.Tts

    ''' <summary>
    ''' Central registry of available TTS backends.
    ''' To add a new engine: (1) implement ITtsBackend, (2) register in KestrelHost,
    ''' (3) add one line here. The Options dialog reads from this automatically.
    ''' </summary>
    Friend Class TtsBackendRegistry

        Public Class Entry
            Public Property Key As String
            Public Property DisplayName As String
            Public Property RequiresInternet As Boolean
            ''' <summary>True when the engine needs an API key (Options shows the key field).</summary>
            Public Property RequiresApiKey As Boolean = False
            ''' <summary>True when the engine needs a per-engine endpoint/region value (Options shows the Endpoint field).</summary>
            Public Property RequiresEndpoint As Boolean = False
            ''' <summary>Default endpoint URL (or region name) used when the user hasn't set one.</summary>
            Public Property DefaultEndpoint As String = ""
            ''' <summary>
            ''' Engine-specific key sharing, declared on the entry: when the engine has
            ''' no key of its own, this resolves a companion credential from config
            ''' (e.g. azure-tts reuses the Azure AI Speech STT key). Nothing = no fallback.
            ''' </summary>
            Public Property FallbackApiKey As Func(Of EveryTongue.Models.AppConfig, String)
            ''' <summary>Self-description of this engine's config block. Empty (BasicEngineConfigDescriptor) until the engine exposes per-session knobs.</summary>
            Public Property ConfigDescriptor As Config.IEngineConfigDescriptor
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "piper", .DisplayName = "Piper (offline)", .RequiresInternet = False, .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("piper")},
            New Entry With {.Key = "mms-tts", .DisplayName = "MMS-TTS (offline)", .RequiresInternet = False, .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("mms-tts")},
            New Entry With {.Key = "edgetts", .DisplayName = "Edge TTS (online)", .RequiresInternet = True, .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("edgetts")},
            New Entry With {.Key = "azure-tts", .DisplayName = "Azure AI Speech TTS (online)", .RequiresInternet = True, .RequiresApiKey = True, .RequiresEndpoint = True, .DefaultEndpoint = "westeurope",
                            .FallbackApiKey = Function(cfg) cfg.GetSttApiKey("azure-speech"),
                            .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("azure-tts")},
            New Entry With {.Key = "google-tts", .DisplayName = "Google Cloud TTS (online)", .RequiresInternet = True, .RequiresApiKey = True,
                            .FallbackApiKey = Function(cfg) Translation.TranslationBackendRegistry.ResolveTranslationApiKey(cfg, "google-translate"),
                            .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("google-tts")},
            New Entry With {.Key = "openai-tts", .DisplayName = "OpenAI TTS (online)", .RequiresInternet = True, .RequiresApiKey = True,
                            .FallbackApiKey = Function(cfg) cfg.GetTranslationApiKey("openai"),
                            .ConfigDescriptor = New Config.BasicEngineConfigDescriptor("openai-tts")}
        }

        Public Shared Function GetAll() As IReadOnlyList(Of Entry)
            Return _backends
        End Function

        Public Shared Sub Register(key As String, displayName As String, requiresInternet As Boolean)
            If Not _backends.Any(Function(b) b.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) Then
                _backends.Add(New Entry With {
                    .Key = key,
                    .DisplayName = displayName,
                    .RequiresInternet = requiresInternet
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
        ''' Resolve the API key for a TTS backend: the engine's own stored key
        ''' wins; when empty, the entry's declared FallbackApiKey (e.g. azure-tts
        ''' reuses the Azure AI Speech STT key, google-tts reuses the Google
        ''' Translate/STT key, openai-tts reuses the OpenAI translation key).
        ''' </summary>
        Friend Shared Function ResolveTtsApiKey(cfg As EveryTongue.Models.AppConfig, ttsKey As String) As String
            If cfg Is Nothing OrElse String.IsNullOrEmpty(ttsKey) Then Return ""
            Dim own = cfg.GetTtsApiKey(ttsKey)
            If Not String.IsNullOrEmpty(own) Then Return own
            Dim entry = Find(ttsKey)
            If entry?.FallbackApiKey IsNot Nothing Then Return If(entry.FallbackApiKey(cfg), "")
            Return ""
        End Function

        ''' <summary>
        ''' Resolve the endpoint (URL or region) for a TTS backend: the user's
        ''' stored value wins; when empty, fall back to the registry entry's
        ''' DefaultEndpoint. Returns "" for engines without endpoints.
        ''' </summary>
        Friend Shared Function ResolveTtsEndpoint(cfg As EveryTongue.Models.AppConfig, ttsKey As String) As String
            If cfg Is Nothing OrElse String.IsNullOrEmpty(ttsKey) Then Return ""
            Dim own = cfg.GetTtsEndpoint(ttsKey)
            If Not String.IsNullOrEmpty(own) Then Return own
            Return If(Find(ttsKey)?.DefaultEndpoint, "")
        End Function

        ''' <summary>
        ''' Push resolved API keys (and per-engine endpoints, for engines that
        ''' declare RequiresEndpoint) into every key-requiring cloud TTS backend
        ''' registered in DI. Called after the Kestrel host starts and again
        ''' whenever the Options dialog saves, so running backends pick up
        ''' key/endpoint changes live. Mirrors
        ''' TranslationBackendRegistry.ConfigureCloudApiKeys.
        ''' </summary>
        Friend Shared Sub ConfigureCloudTtsKeys(services As IServiceProvider, cfg As EveryTongue.Models.AppConfig)
            If services Is Nothing OrElse cfg Is Nothing Then Return
            Dim cloudBackends = services.GetServices(Of Interfaces.ITtsBackend)().
                OfType(Of CloudTtsBackend)().ToList()
            For Each entry In _backends.Where(Function(b) b.RequiresApiKey)
                Dim backend = cloudBackends.FirstOrDefault(
                    Function(b) b.Name.Equals(entry.Key, StringComparison.OrdinalIgnoreCase))
                If backend Is Nothing Then Continue For
                Dim key = ResolveTtsApiKey(cfg, entry.Key)
                ' Never initialise a backend that has no key — an unselected /
                ' unconfigured engine must not be touched at startup (also avoids
                ' loading any optional vendor SDK assembly just to configure it).
                If String.IsNullOrEmpty(key) Then
                    AppLogger.Log(LogEvents.TTS_BACKEND_CONFIGURED,
                        $"Cloud TTS backend '{entry.Key}': no key — skipped")
                    Continue For
                End If
                ' Configure defensively: a missing optional dependency must log
                ' and be skipped, never crash server startup.
                Try
                    backend.Configure(key)
                    Dim endpointNote = ""
                    If entry.RequiresEndpoint Then
                        Dim endpoint = ResolveTtsEndpoint(cfg, entry.Key)
                        backend.ConfigureEndpoint(endpoint)
                        endpointNote = $", endpoint '{endpoint}'"
                    End If
                    AppLogger.Log(LogEvents.TTS_BACKEND_CONFIGURED,
                        $"Cloud TTS backend '{entry.Key}': API key configured{endpointNote}")
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TTS_ENGINE_ERROR,
                        $"Cloud TTS backend '{entry.Key}' unavailable — {ex.GetType().Name}: {ex.Message} (engine disabled; a required component may be missing)")
                End Try
            Next
        End Sub

    End Class

End Namespace
