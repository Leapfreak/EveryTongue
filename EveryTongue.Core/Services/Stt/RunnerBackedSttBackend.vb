Imports System.Threading
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' Shared base for the ISttBackend adapters around LiveStreamRunner
    ''' (whisper-cpp, faster-whisper, cloud streaming). The adapter boilerplate —
    ''' runner event wiring, delegating properties, Stop/Segment/UpdateConfig/
    ''' device enumeration/stats/transcript — was copy-pasted across all three
    ''' backends, so every new capability had to be added two or three times
    ''' (CLONE-REPORT Group 2). Derived classes supply only Name/RequiresInternet
    ''' and their engine-specific Start().
    '''
    ''' VB note: the events MUST live here (a derived class cannot RaiseEvent a
    ''' base event), which is why the runner wiring is in this constructor.
    ''' </summary>
    Friend MustInherit Class RunnerBackedSttBackend
        Implements ISttBackend, ISegmentingSttBackend

        ''' <summary>The live-server session runner this adapter delegates to.</summary>
        Protected ReadOnly Runner As New LiveStreamRunner()

        Protected Sub New()
            AddHandler Runner.OutputLineUpdated, Sub(s, line)
                                                     RaiseEvent OutputUpdated(Me, New SttOutputEventArgs(line))
                                                 End Sub

            ' Committed lines arrive as "lang<TAB>text" when the engine detected
            ' a language; plain text otherwise.
            AddHandler Runner.OutputLineCommitted, Sub(s, line)
                                                       Dim text = line
                                                       Dim lang = ""
                                                       Dim tabIdx = line.IndexOf(vbTab)
                                                       If tabIdx > 0 Then
                                                           lang = line.Substring(0, tabIdx)
                                                           text = line.Substring(tabIdx + 1)
                                                       End If
                                                       RaiseEvent OutputCommitted(Me, New SttOutputEventArgs(text, lang))
                                                   End Sub

            ' Inline engine translations (Speechmatics). The offline runners never
            ' raise this — wiring it unconditionally is harmless.
            AddHandler Runner.OutputLineCommittedTranslated, Sub(s, tc)
                                                                 RaiseEvent OutputCommittedTranslated(
                                                                     Me, New SttTranslatedCommitEventArgs(tc.Text, tc.Lang, tc.Translations))
                                                             End Sub

            AddHandler Runner.ErrorReceived, Sub(s, line)
                                                 RaiseEvent ErrorReceived(Me, line)
                                             End Sub
        End Sub

        Public MustOverride ReadOnly Property Name As String Implements ISttBackend.Name
        Public MustOverride ReadOnly Property RequiresInternet As Boolean Implements ISttBackend.RequiresInternet
        Public MustOverride Sub Start(config As SttSessionConfig) Implements ISttBackend.Start

        Public Overridable ReadOnly Property IsAvailable As Boolean Implements ISttBackend.IsAvailable
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean Implements ISttBackend.IsRunning
            Get
                Return Runner.IsRunning
            End Get
        End Property

        Public ReadOnly Property Transcript As String Implements ISttBackend.Transcript
            Get
                Return Runner.Transcript
            End Get
        End Property

        Public Event OutputUpdated As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputUpdated
        Public Event OutputCommitted As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputCommitted
        Public Event OutputCommittedTranslated As EventHandler(Of SttTranslatedCommitEventArgs) Implements ISttBackend.OutputCommittedTranslated
        Public Event ErrorReceived As EventHandler(Of String) Implements ISttBackend.ErrorReceived

        Public Sub [Stop]() Implements ISttBackend.Stop
            Runner.Stop()
        End Sub

        ''' <summary>Park (warm spare): stop capture, keep the live-server process +
        ''' loaded SaT resident for the next room (ENGINE_CONCURRENCY_PLAN).</summary>
        Public Sub StopCaptureOnly()
            Runner.StopCaptureOnly()
        End Sub

        ''' <summary>The parked/live server port (for spare reuse).</summary>
        Public ReadOnly Property ServerPort As Integer
            Get
                Return Runner.ServerPort
            End Get
        End Property

        ''' <summary>Live-server HTTP answered at least once (spare warm indicator).</summary>
        Public ReadOnly Property ServerReady As Boolean
            Get
                Return Runner.IsServerReady
            End Get
        End Property

        ''' <summary>Split a held clause into sentences via live-server's SaT segmenter (clause coordinator flush).</summary>
        Public Function Segment(text As String, thresholdPercent As Integer, model As String) As List(Of String) Implements ISegmentingSttBackend.Segment
            Return Runner.Segment(text, thresholdPercent, model)
        End Function

        Public Function UpdateConfigAsync(params As Dictionary(Of String, Object)) As Task Implements ISttBackend.UpdateConfigAsync
            Return Runner.UpdateConfigAsync(params)
        End Function

        Public Function EnumerateDevicesAsync(pythonExePath As String) As List(Of AudioDeviceInfo) Implements ISttBackend.EnumerateDevicesAsync
            Dim rawDevices = Runner.EnumerateDevicesAsync(pythonExePath)
            Dim result As New List(Of AudioDeviceInfo)
            For Each raw In rawDevices
                Dim colonIdx = raw.IndexOf(":"c)
                If colonIdx > 0 Then
                    Dim idStr = raw.Substring(0, colonIdx).Trim()
                    Dim devName = raw.Substring(colonIdx + 1).Trim()
                    Dim id As Integer
                    If Integer.TryParse(idStr, id) Then
                        result.Add(New AudioDeviceInfo(id, devName))
                    Else
                        result.Add(New AudioDeviceInfo(0, raw))
                    End If
                Else
                    result.Add(New AudioDeviceInfo(0, raw))
                End If
            Next
            Return result
        End Function

        ''' <summary>Deep check: healthy = actually CAPTURING (model loaded / session live,
        ''' pipeline alive), not merely "HTTP is up" — the shallow check fired ready
        ''' signals early. faster-whisper overrides with its readiness flag.</summary>
        Public Overridable Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean) Implements ISttBackend.CheckHealthAsync
            Return Runner.CheckCapturingAsync(ct)
        End Function

        ''' <summary>Live-server process alive — progress signal for SidecarReadiness.</summary>
        Public ReadOnly Property ServerProcessRunning As Boolean
            Get
                Return Runner.IsServerProcessRunning
            End Get
        End Property

        ''' <summary>Ms since the live-server last showed activity (log tail / start).</summary>
        Public ReadOnly Property ServerMillisecondsSinceLastActivity As Long
            Get
                Return Runner.MillisecondsSinceLastActivity
            End Get
        End Property

        Public Function GetStatsAsync() As Task(Of String) Implements ISttBackend.GetStatsAsync
            Return Runner.GetStatsAsync()
        End Function

        Public Function SaveTranscript(filePath As String) As Boolean Implements ISttBackend.SaveTranscript
            Return Runner.SaveTranscript(filePath)
        End Function

    End Class

End Namespace
