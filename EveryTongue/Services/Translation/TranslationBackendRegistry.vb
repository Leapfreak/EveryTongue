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
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "nllb", .DisplayName = "NLLB (offline)", .RequiresInternet = False, .RequiresApiKey = False}
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
