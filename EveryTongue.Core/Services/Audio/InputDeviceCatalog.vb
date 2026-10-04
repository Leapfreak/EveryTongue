Imports EveryTongue.Services.Models
Imports EveryTongue.Services.Stt

Namespace Services.Audio

    ''' <summary>
    ''' The server machine's audio INPUT devices, matched the way every picker and the
    ''' room-start resolver must match them: by NAME. PortAudio indices renumber whenever
    ''' a device is plugged in or removed (the detachable USB line-in renumbers between
    ''' services), so an index is only valid for the enumeration it came from.
    ''' One place for "enumerate / find by name / list once per name" so the room-start
    ''' resolver, the host-panel picker and the desktop template editor agree.
    ''' </summary>
    Public NotInheritable Class InputDeviceCatalog

        Private Sub New()
        End Sub

        ''' <summary>Current input devices (slow: asks the live-server or Python — call off the UI thread).</summary>
        Public Shared Function Enumerate() As List(Of AudioDeviceInfo)
            Dim pythonPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "python-embed", "python.exe")
            Dim devices = SttBackendRegistry.CreateBackend().EnumerateDevicesAsync(pythonPath)
            Return If(devices, New List(Of AudioDeviceInfo)())
        End Function

        ''' <summary>The first device whose name matches (trimmed, case-insensitive), or Nothing.
        ''' "First" matches the room-start resolver: Windows lists one physical input once per
        ''' host API under the same name, and the first entry is the one that is used.</summary>
        Public Shared Function FindByName(devices As IEnumerable(Of AudioDeviceInfo), name As String) As AudioDeviceInfo
            If devices Is Nothing OrElse String.IsNullOrWhiteSpace(name) Then Return Nothing
            Dim wanted = name.Trim()
            Return devices.FirstOrDefault(Function(d) d IsNot Nothing AndAlso d.Id >= 0 AndAlso
                String.Equals(If(d.Name, "").Trim(), wanted, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>One entry per device name (the entry FindByName would pick), in enumeration order.</summary>
        Public Shared Function DistinctByName(devices As IEnumerable(Of AudioDeviceInfo)) As List(Of AudioDeviceInfo)
            Dim result As New List(Of AudioDeviceInfo)
            If devices Is Nothing Then Return result
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each d In devices
                If d Is Nothing OrElse d.Id < 0 OrElse String.IsNullOrWhiteSpace(d.Name) Then Continue For
                If seen.Add(d.Name.Trim()) Then result.Add(d)
            Next
            Return result
        End Function

    End Class

End Namespace
