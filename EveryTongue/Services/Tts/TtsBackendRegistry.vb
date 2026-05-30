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
        End Class

        Private Shared ReadOnly _backends As New List(Of Entry) From {
            New Entry With {.Key = "piper", .DisplayName = "Piper (offline)", .RequiresInternet = False},
            New Entry With {.Key = "mms-tts", .DisplayName = "MMS-TTS (offline)", .RequiresInternet = False},
            New Entry With {.Key = "edgetts", .DisplayName = "Edge TTS (online)", .RequiresInternet = True}
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

    End Class

End Namespace
