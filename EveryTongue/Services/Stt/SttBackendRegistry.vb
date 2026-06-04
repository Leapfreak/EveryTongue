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
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "whisper-cpp-vulkan", .DisplayName = "whisper.cpp Vulkan (offline)", .RequiresInternet = False},
            New Entry With {.Key = "whisper-cpp-cuda", .DisplayName = "whisper.cpp CUDA (offline)", .RequiresInternet = False},
            New Entry With {.Key = "whisper-cpp-cpu", .DisplayName = "whisper.cpp CPU (offline)", .RequiresInternet = False}
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
        ''' Create an ISttBackend instance for the given key.
        ''' Falls back to WhisperCppBackend (Vulkan) for unknown keys.
        ''' </summary>
        Public Shared Function CreateBackend(Optional key As String = "whisper-cpp-vulkan") As Interfaces.ISttBackend
            Select Case If(key, "whisper-cpp-vulkan").ToLowerInvariant()
                Case "whisper-cpp-vulkan", "whisper-cpp-cuda"
                    Return New WhisperCppBackend(useGpu:=True)
                Case "whisper-cpp-cpu"
                    Return New WhisperCppBackend(useGpu:=False)
                Case Else
                    Return New WhisperCppBackend(useGpu:=True)
            End Select
        End Function

    End Class

End Namespace
