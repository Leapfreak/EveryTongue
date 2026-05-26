Imports System.IO

Namespace Services.Models
    ''' <summary>
    ''' Result of an audio stream request (file or NDI capture).
    ''' </summary>
    Public Class AudioStreamResult
        Public Property Stream As Stream
        Public Property ContentType As String
        Public Property TotalLength As Long
        Public Property RangeStart As Long
        Public Property RangeEnd As Long
        Public Property FileName As String
    End Class

    ''' <summary>
    ''' Information about an available NDI source on the network.
    ''' </summary>
    Public Class NdiSourceInfo
        Public Property Name As String
        Public Property Url As String
        Public Property MachineName As String
    End Class
End Namespace
