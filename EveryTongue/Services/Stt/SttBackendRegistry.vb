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
            ''' <summary>Factory function that creates an ISttBackend instance for this entry.</summary>
            Public Property Factory As Func(Of Interfaces.ISttBackend)
            ''' <summary>Self-description of this engine's config block (fields, defaults, serialization).</summary>
            Public Property ConfigDescriptor As Config.IEngineConfigDescriptor
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "whisper-cpp-vulkan", .DisplayName = "whisper.cpp Vulkan (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "whisper-cpp", .Factory = Function() New WhisperCppBackend(useGpu:=True), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-vulkan")},
            New Entry With {.Key = "whisper-cpp-cuda", .DisplayName = "whisper.cpp CUDA (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "whisper-cpp", .Factory = Function() New WhisperCppBackend(useGpu:=True), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-cuda")},
            New Entry With {.Key = "whisper-cpp-cpu", .DisplayName = "whisper.cpp CPU (offline)", .RequiresInternet = False, .UseGpu = False, .SidecarMode = "whisper-cpp", .Factory = Function() New WhisperCppBackend(useGpu:=False), .ConfigDescriptor = New Configs.WhisperCppConfigDescriptor("whisper-cpp-cpu")},
            New Entry With {.Key = "faster-whisper", .DisplayName = "faster-whisper CUDA (offline)", .RequiresInternet = False, .UseGpu = True, .SidecarMode = "faster-whisper", .Factory = Function() New FasterWhisperBackend(), .ConfigDescriptor = New Configs.FasterWhisperConfigDescriptor()},
            New Entry With {.Key = "google-cloud-stt", .DisplayName = "Google Cloud STT (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .Factory = Function() New CloudStreamingSttBackend("google-cloud-stt", "Google Cloud STT (online)"), .ConfigDescriptor = New Configs.GoogleSttConfigDescriptor()},
            New Entry With {.Key = "speechmatics", .DisplayName = "Speechmatics (online)", .RequiresInternet = True, .UseGpu = False, .RequiresApiKey = True, .SidecarMode = "online", .Factory = Function() New CloudStreamingSttBackend("speechmatics", "Speechmatics (online)"), .ConfigDescriptor = New Configs.SpeechmaticsConfigDescriptor()}
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
