Namespace Services.Infrastructure

    ''' <summary>
    ''' Metadata for a registered log event.
    ''' </summary>
    Public Class LogEventInfo
        Public ReadOnly Id As Integer
        Public ReadOnly Category As LogCategory
        Public ReadOnly DefaultLevel As LogSeverity
        Public ReadOnly Description As String

        Public Sub New(id As Integer, category As LogCategory, level As LogSeverity, description As String)
            Me.Id = id
            Me.Category = category
            Me.DefaultLevel = level
            Me.Description = description
        End Sub
    End Class

    ''' <summary>
    ''' Central registry of all log events. Call Register() at module init,
    ''' then Lookup() at runtime to get event metadata for routing.
    ''' </summary>
    Public Module LogEventRegistry

        Private ReadOnly _events As New Dictionary(Of Integer, LogEventInfo)
        Private _initialized As Boolean = False

        ''' <summary>
        ''' Register a log event. Called from LogEvents module init.
        ''' </summary>
        Public Sub Register(id As Integer, category As LogCategory, level As LogSeverity, description As String)
            _events(id) = New LogEventInfo(id, category, level, description)
        End Sub

        ''' <summary>
        ''' Look up event metadata by ID. Returns Nothing for unregistered IDs.
        ''' </summary>
        ''' <summary>
        ''' Ensures all events are registered. Called lazily on first Lookup.
        ''' LogEvents uses Public Const fields which are inlined at compile time,
        ''' so its module constructor never fires — we must call RegisterAll() explicitly.
        ''' </summary>
        Private Sub EnsureInitialized()
            If _initialized Then Return
            _initialized = True
            LogEvents.RegisterAll()
        End Sub

        Public Function Lookup(id As Integer) As LogEventInfo
            If Not _initialized Then EnsureInitialized()
            Dim info As LogEventInfo = Nothing
            _events.TryGetValue(id, info)
            Return info
        End Function

        ''' <summary>
        ''' Get all registered events (for the config dialog).
        ''' </summary>
        Public Function GetAll() As IReadOnlyDictionary(Of Integer, LogEventInfo)
            Return _events
        End Function

        ''' <summary>
        ''' Get all registered events for a specific category.
        ''' </summary>
        Public Function GetByCategory(category As LogCategory) As IEnumerable(Of LogEventInfo)
            Return _events.Values.Where(Function(e) e.Category = category)
        End Function

    End Module

End Namespace
