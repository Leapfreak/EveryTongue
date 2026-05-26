Namespace Server
    ''' <summary>
    ''' Configuration options for the Kestrel server.
    ''' Bound from AppConfig on startup.
    ''' </summary>
    Public Class ServerOptions
        ''' <summary>HTTP port (default 5080). HTTPS is always HttpPort + 1.</summary>
        Public Property HttpPort As Integer = 5080

        ''' <summary>Whether to bind to all interfaces (True) or localhost only (False).</summary>
        Public Property AllowRemote As Boolean = True

        ''' <summary>Maximum committed lines kept for history replay.</summary>
        Public Property HistoryCap As Integer = 500

        ''' <summary>Maximum entries replayed to a reconnecting client.</summary>
        Public Property MaxReplayEntries As Integer = 200

        ''' <summary>Background/foreground colours for the web client.</summary>
        Public Property BgColor As String = "#000000"
        Public Property FgColor As String = "#FFFFFF"

        ''' <summary>Computed HTTPS port.</summary>
        Public ReadOnly Property HttpsPort As Integer
            Get
                Return HttpPort + 1
            End Get
        End Property
    End Class
End Namespace
