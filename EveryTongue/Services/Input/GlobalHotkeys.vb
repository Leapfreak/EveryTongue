Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports EveryTongue.Services.Infrastructure

Namespace Services.Input

    ''' <summary>
    ''' System-wide hotkeys for dictation, using only stable Win32 (RegisterHotKey +
    ''' GetAsyncKeyState) — deliberately NO low-level keyboard hook (avoids AV/EDR
    ''' scrutiny and global input-path cost).
    '''
    ''' Toggle: RegisterHotKey fires WM_HOTKEY on press → HotkeyToggle.
    ''' Push-to-talk: RegisterHotKey gives the press (PttDown); a short polling timer
    ''' watches GetAsyncKeyState for the release (PttUp), since RegisterHotKey can't see key-up.
    ''' WM_HOTKEY is delivered to the owning window — FormMain forwards it via OnWmHotkey.
    ''' </summary>
    Public Class GlobalHotkeys
        Implements IDisposable

        Private Const WM_HOTKEY As Integer = &H312
        Private Const MOD_ALT As UInteger = &H1UI
        Private Const MOD_CONTROL As UInteger = &H2UI
        Private Const MOD_SHIFT As UInteger = &H4UI
        Private Const MOD_WIN As UInteger = &H8UI
        Private Const MOD_NOREPEAT As UInteger = &H4000UI
        Private Const ID_TOGGLE As Integer = &HD1C7   ' arbitrary, app-unique
        Private Const ID_PTT As Integer = &HD1C8

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function RegisterHotKey(hWnd As IntPtr, id As Integer, fsModifiers As UInteger, vk As UInteger) As Boolean
        End Function

        <DllImport("user32.dll", SetLastError:=True)>
        Private Shared Function UnregisterHotKey(hWnd As IntPtr, id As Integer) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
        End Function

        Private ReadOnly _hwnd As IntPtr
        Private _toggleRegistered As Boolean
        Private _pttRegistered As Boolean
        Private _pttVk As Integer
        Private ReadOnly _pttPoll As New Timer() With {.Interval = 30}
        Private _disposed As Boolean

        ''' <summary>Raised on the toggle hotkey press.</summary>
        Public Event HotkeyToggle()
        ''' <summary>Raised when the push-to-talk key goes down.</summary>
        Public Event PttDown()
        ''' <summary>Raised when the push-to-talk key is released.</summary>
        Public Event PttUp()

        Public Sub New(ownerHandle As IntPtr)
            _hwnd = ownerHandle
            AddHandler _pttPoll.Tick, AddressOf OnPttPoll
        End Sub

        ''' <summary>Called by FormMain.WndProc for WM_HOTKEY messages.</summary>
        Public Sub OnWmHotkey(id As Integer)
            If id = ID_TOGGLE Then
                RaiseEvent HotkeyToggle()
            ElseIf id = ID_PTT Then
                ' Press: open the gate and start watching for release.
                If Not _pttPoll.Enabled Then
                    RaiseEvent PttDown()
                    _pttPoll.Start()
                End If
            End If
        End Sub

        Private Sub OnPttPoll(sender As Object, e As EventArgs)
            If (GetAsyncKeyState(_pttVk) And &H8000) = 0 Then
                _pttPoll.Stop()
                RaiseEvent PttUp()
            End If
        End Sub

        ''' <summary>(Re)register the toggle hotkey from a "Ctrl+Alt+D" style string. Returns False if taken.</summary>
        Public Function SetToggleHotkey(spec As String) As Boolean
            If _toggleRegistered Then UnregisterHotKey(_hwnd, ID_TOGGLE) : _toggleRegistered = False
            Dim mods As UInteger, vk As Integer
            If Not TryParse(spec, mods, vk) Then Return False
            _toggleRegistered = RegisterHotKey(_hwnd, ID_TOGGLE, mods Or MOD_NOREPEAT, CUInt(vk))
            If Not _toggleRegistered Then
                AppLogger.Log(LogEvents.DICT_HOTKEY, $"Toggle hotkey '{spec}' could not be registered (already in use?)")
            End If
            Return _toggleRegistered
        End Function

        ''' <summary>(Re)register the push-to-talk hotkey. Returns False if taken/invalid.</summary>
        Public Function SetPttHotkey(spec As String) As Boolean
            _pttPoll.Stop()
            If _pttRegistered Then UnregisterHotKey(_hwnd, ID_PTT) : _pttRegistered = False
            Dim mods As UInteger, vk As Integer
            If Not TryParse(spec, mods, vk) Then Return False
            _pttVk = vk
            _pttRegistered = RegisterHotKey(_hwnd, ID_PTT, mods Or MOD_NOREPEAT, CUInt(vk))
            If Not _pttRegistered Then
                AppLogger.Log(LogEvents.DICT_HOTKEY, $"Push-to-talk hotkey '{spec}' could not be registered (already in use?)")
            End If
            Return _pttRegistered
        End Function

        ''' <summary>Unregister the PTT hotkey (e.g. when leaving push-to-talk mode).</summary>
        Public Sub ClearPttHotkey()
            _pttPoll.Stop()
            If _pttRegistered Then UnregisterHotKey(_hwnd, ID_PTT) : _pttRegistered = False
        End Sub

        ''' <summary>Parse "Control+Alt+D" → modifier flags + virtual-key code.</summary>
        Private Shared Function TryParse(spec As String, ByRef mods As UInteger, ByRef vk As Integer) As Boolean
            mods = 0 : vk = 0
            If String.IsNullOrWhiteSpace(spec) Then Return False
            Dim keyName As String = Nothing
            For Each partRaw In spec.Split("+"c)
                Dim part = partRaw.Trim()
                If part.Length = 0 Then Continue For
                Select Case part.ToLowerInvariant()
                    Case "ctrl", "control" : mods = mods Or MOD_CONTROL
                    Case "alt" : mods = mods Or MOD_ALT
                    Case "shift" : mods = mods Or MOD_SHIFT
                    Case "win", "windows" : mods = mods Or MOD_WIN
                    Case Else : keyName = part
                End Select
            Next
            If String.IsNullOrEmpty(keyName) Then Return False
            Dim k As Keys
            If Not [Enum].TryParse(keyName, True, k) Then Return False
            vk = CInt(k) And &HFFFF
            Return vk <> 0
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            If _disposed Then Return
            _disposed = True
            _pttPoll.Stop()
            _pttPoll.Dispose()
            If _toggleRegistered Then UnregisterHotKey(_hwnd, ID_TOGGLE)
            If _pttRegistered Then UnregisterHotKey(_hwnd, ID_PTT)
        End Sub

    End Class

End Namespace
