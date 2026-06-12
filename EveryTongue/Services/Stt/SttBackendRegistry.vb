Namespace Services.Stt

    ''' <summary>
    ''' Central registry of available STT backends.
    ''' To add a new engine: (1) implement ISttBackend, (2) add one line here.
    ''' The Options dialog reads from this automatically.
    ''' </summary>
    Friend Class SttBackendRegistry

        Public Class Entry
            Public Property Key As String
            Public Property DisplayName As String
            Public Property RequiresInternet As Boolean
            ''' <summary>Whether this backend uses GPU acceleration.</summary>
            Public Property UseGpu As Boolean
            ''' <summary>Whether the backend requires an API key to function.</summary>
            Public Property RequiresApiKey As Boolean
            ''' <summary>
            ''' How the Python live-server hosts this engine: "whisper-cpp"
            ''' (whisper-server.exe sidecar), "faster-whisper" (in-process model),
            ''' or "online" (cloud engine module, no local model).
            ''' </summary>
            Public Property SidecarMode As String = "whisper-cpp"
            ''' <summary>
            ''' File glob used when scanning for selectable model files (e.g. "*.bin").
            ''' Empty string = models are DIRECTORIES containing config.json.
            ''' "-" = engine has NO local model selection (online engines).
            ''' </summary>
            Public Property ModelScanPattern As String = ""
            ''' <summary>Minimum file size in MB for a scanned file to count as a model (filters stray small files).</summary>
            Public Property ModelMinSizeMB As Integer = 0
            ''' <summary>
            ''' Resolves this engine's configured model path from AppConfig.
            ''' Returns "" for engines with no local model (online engines).
            ''' Nothing = caller should fall back to its own default.
            ''' </summary>
            Public Property ModelPathFromConfig As Func(Of EveryTongue.Models.AppConfig, String)
            ''' <summary>
            ''' Translation backend key that pairs naturally with this STT engine
            ''' (e.g. shares the same API key). "" = no companion.
            ''' </summary>
            Public Property CompanionTranslationKey As String = ""
            ''' <summary>Factory function that creates an ISttBackend instance for this entry.</summary>
            Public Property Factory As Func(Of Interfaces.ISttBackend)
            ''' <summary>Self-description of this engine's config block (fields, defaults, serialization).</summary>
            Public Property ConfigDescriptor As Config.IEngineConfigDescriptor
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "whisper-cpp-vulkan", .DisplayName = "whisper.cpp Vulkan (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "whisper-cpp", .ModelScanPattern = "*.bin", .ModelMinSizeMB = 10, .ModelPathFromConfig = Function(cfg) cfg.PathWhisperCppModel, .Factory = Function() New WhisperCppBackend(useGpu:=True), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-vulkan")},
            New Entry With {.Key = "whisper-cpp-cuda", .DisplayName = "whisper.cpp CUDA (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "whisper-cpp", .ModelScanPattern = "*.bin", .ModelMinSizeMB = 10, .ModelPathFromConfig = Function(cfg) cfg.PathWhisperCppModel, .Factory = Function() New WhisperCppBackend(useGpu:=True), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-cuda")},
            New Entry With {.Key = "whisper-cpp-cpu", .DisplayName = "whisper.cpp CPU (offline)", .RequiresInternet = False, .UseGpu = False, .SidecarMode = "whisper-cpp", .ModelScanPattern = "*.bin", .ModelMinSizeMB = 10, .ModelPathFromConfig = Function(cfg) cfg.PathWhisperCppModel, .Factory = Function() New WhisperCppBackend(useGpu:=False), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-cpu")},
            New Entry With {.Key = "faster-whisper", .DisplayName = "faster-whisper CUDA (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "faster-whisper", .ModelPathFromConfig = Function(cfg) cfg.PathFasterWhisperModel, .Factory = Function() New FasterWhisperBackend(), .ConfigDescriptor = New Configs.FasterWhisperConfigDescriptor()},
            New Entry With {.Key = "google-cloud-stt", .DisplayName = "Google Cloud STT (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .ModelScanPattern = "-", .ModelPathFromConfig = Function(cfg) "", .CompanionTranslationKey = "google-translate", .Factory = Function() New CloudStreamingSttBackend("google-cloud-stt", "Google Cloud STT (online)"), .ConfigDescriptor = New Configs.GoogleSttConfigDescriptor()},
            New Entry With {.Key = "speechmatics", .DisplayName = "Speechmatics (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .ModelScanPattern = "-", .ModelPathFromConfig = Function(cfg) "", .Factory = Function() New CloudStreamingSttBackend("speechmatics", "Speechmatics (online)"), .ConfigDescriptor = New Configs.SpeechmaticsConfigDescriptor()},
            New Entry With {.Key = "deepgram", .DisplayName = "Deepgram (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .ModelScanPattern = "-", .ModelPathFromConfig = Function(cfg) "", .Factory = Function() New CloudStreamingSttBackend("deepgram", "Deepgram (online)"), .ConfigDescriptor = New Configs.DeepgramConfigDescriptor()},
            New Entry With {.Key = "gladia", .DisplayName = "Gladia (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .ModelScanPattern = "-", .ModelPathFromConfig = Function(cfg) "", .Factory = Function() New CloudStreamingSttBackend("gladia", "Gladia (online)"), .ConfigDescriptor = New Configs.GladiaConfigDescriptor()},
            New Entry With {.Key = "azure-speech", .DisplayName = "Azure AI Speech (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .ModelScanPattern = "-", .ModelPathFromConfig = Function(cfg) "", .Factory = Function() New CloudStreamingSttBackend("azure-speech", "Azure AI Speech (online)"), .ConfigDescriptor = New Configs.AzureSpeechConfigDescriptor()}
        }

        Public Shared Function GetAll() As IReadOnlyList(Of Entry)
            Return _backends
        End Function

        Public Shared Sub Register(entry As Entry)
            If Not _backends.Any(Function(b) b.Key.Equals(entry.Key, StringComparison.OrdinalIgnoreCase)) Then
                _backends.Add(entry)
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
        ''' Create an ISttBackend instance for the given key.
        ''' Falls back to the first registered backend for unknown keys.
        ''' </summary>
        Public Shared Function CreateBackend(Optional key As String = Nothing) As Interfaces.ISttBackend
            Dim entry = Find(If(key, _backends(0).Key))
            If entry Is Nothing Then entry = _backends(0)
            Return entry.Factory.Invoke()
        End Function

    End Class

End Namespace
