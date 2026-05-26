Imports TranscriptionTools.Services.Subtitle

Namespace Services.Interfaces
    ''' <summary>
    ''' Core subtitle service — manages client state, broadcast, and history.
    ''' Bridge between FormMain (producer) and WebSocket clients (consumers).
    ''' </summary>
    Public Interface ISubtitleService

        ' ── Events (raised on background threads — marshal to UI thread in handlers) ──

        Event StatusChanged As EventHandler(Of String)
        Event RemoteCommand As EventHandler(Of String)
        Event ActiveLanguagesChanged As EventHandler
        Event InputLanguageChanged As EventHandler(Of String)
        Event LogMessage As EventHandler(Of String)

        ' ── Properties ──

        ReadOnly Property IsRunning As Boolean
        ReadOnly Property ConnectedClients As Integer
        Property IsLiveRunning As Boolean
        Property IsSimulating As Boolean
        Property InputLanguage As String
        Property TuneCallback As Func(Of String)
        Property BgColor As String
        Property FgColor As String

        ' ── Client management ──

        Function AddClient(client As ClientConnection) As Boolean
        Function RemoveClient(clientId As String) As Boolean
        Function GetActiveTranslationLanguages() As List(Of String)

        ' ── Message processing ──

        Sub ProcessClientMessage(clientId As String, jsonText As String)

        ' ── Broadcast methods (called by FormMain) ──

        Sub BroadcastUpdate(text As String)
        Function BroadcastCommit(text As String,
                                  Optional skipTranslationClients As Boolean = False,
                                  Optional lang As String = "",
                                  Optional sourceLang As String = "") As Integer
        Sub BroadcastCommitTranslated(originalText As String,
                                       sourceLang As String,
                                       translations As Dictionary(Of String, String),
                                       langTags As Dictionary(Of String, String))
        Sub BroadcastTranslationsOnly(translations As Dictionary(Of String, String),
                                       langTags As Dictionary(Of String, String))
        Sub BroadcastClear()
        Sub BroadcastSystemMessage(text As String)

        ' ── History replay ──

        Function ReplayHistoryAsync(client As ClientConnection,
                                     lastId As Integer,
                                     ct As System.Threading.CancellationToken) As Task
    End Interface
End Namespace
