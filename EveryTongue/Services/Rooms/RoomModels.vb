Imports System.Collections.Concurrent

Namespace Services.Rooms

    Public Enum RoomType
        Conference
        Conversation
        Workroom
    End Enum

    Public Enum RoomVisibility
        [Public]
        [Private]
    End Enum

    Public Class RoomConfig
        Public Property WhisperModel As String = "medium"
        Public Property TtsEnabled As Boolean = True
        Public Property RetainTranscript As Boolean = True
        Public Property IdleTimeoutMinutes As Integer = 15
        ''' <summary>PTT mode: "hold" (default) or "toggle".</summary>
        Public Property PttMode As String = "hold"
        ''' <summary>When True, commits are blocked from being broadcast to room clients.</summary>
        Public Property IsPaused As Boolean = False
    End Class

    ''' <summary>
    ''' A virtual member represents a person without a phone on a shared device.
    ''' </summary>
    Public Class VirtualMember
        Public Property Id As String
        Public Property Name As String
        Public Property Language As String
    End Class

    Public Class Room
        Public Property Id As String
        Public Property Name As String
        Public Property Type As RoomType = RoomType.Conference
        Public Property Visibility As RoomVisibility = RoomVisibility.Public
        Public Property SourceLang As String = "auto"
        Public Property TemplateId As String = ""
        Public Property CreatedAt As DateTime = DateTime.Now
        Public Property IsActive As Boolean = True
        Public Property HostClientId As String
        Public Property Config As New RoomConfig()

        ''' <summary>Secret token for host reconnection (GUID generated at creation).</summary>
        Public Property HostToken As String = ""

        ''' <summary>When locked, no new clients can join.</summary>
        Public Property IsLocked As Boolean = False

        ''' <summary>
        ''' Set of client IDs currently in this room.
        ''' Client state itself lives in SubtitleService._clients.
        ''' </summary>
        Public ReadOnly Property ClientIds As New ConcurrentDictionary(Of String, Byte)()

        ''' <summary>Virtual members on shared devices (host-managed).</summary>
        Public ReadOnly Property VirtualMembers As New ConcurrentDictionary(Of String, VirtualMember)()

        Public ReadOnly Property ClientCount As Integer
            Get
                Return ClientIds.Count
            End Get
        End Property

        ''' <summary>Timestamp of the last activity (client join/leave, message).</summary>
        Public Property LastActivityAt As DateTime = DateTime.Now

        Public Sub TouchActivity()
            LastActivityAt = DateTime.Now
        End Sub
    End Class

End Namespace
