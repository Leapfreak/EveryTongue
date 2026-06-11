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
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "nllb", .DisplayName = "NLLB 1.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-model", .BackendName = "Local"},
            New Entry With {.Key = "nllb-3.3b", .DisplayName = "NLLB 3.3B (offline)", .RequiresInternet = False, .RequiresApiKey = False, .ModelType = "nllb", .DefaultModelPath = ".\nllb-3.3b-model", .BackendName = "Local"},
            New Entry With {.Key = "google-translate", .DisplayName = "Google Translate (online)", .RequiresInternet = True, .RequiresApiKey = True, .BackendName = "Google"}
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

    End Class

End Namespace
