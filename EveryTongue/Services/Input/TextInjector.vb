Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports EveryTongue.Services.Infrastructure

Namespace Services.Input

    ''' <summary>
    ''' Types text into whatever window/control currently has focus, system-wide.
    ''' Two strategies: synthesized Unicode keystrokes (SendInput) — clean, no clipboard
    ''' impact — or clipboard paste (Ctrl+V) for apps where typing misbehaves.
    '''
    ''' 64-bit note: the INPUT union must use the x64 layout and SendInput must be passed
    ''' Marshal.SizeOf(INPUT); a wrong cbSize makes SendInput silently return 0.
    ''' UIPI note: a non-elevated process cannot inject into an elevated foreground window.
    ''' </summary>
    Public Module TextInjector

        ' ── Win32 ────────────────────────────────────────────────────────────
        <StructLayout(LayoutKind.Sequential)>
        Private Structure KEYBDINPUT
            Public wVk As UShort
            Public wScan As UShort
            Public dwFlags As UInteger
            Public time As UInteger
            Public dwExtraInfo As IntPtr
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Private Structure MOUSEINPUT
            Public dx As Integer
            Public dy As Integer
            Public mouseData As UInteger
            Public dwFlags As UInteger
            Public time As UInteger
            Public dwExtraInfo As IntPtr
        End Structure

        <StructLayout(LayoutKind.Explicit)>
        Private Structure InputUnion
            <FieldOffset(0)> Public ki As KEYBDINPUT
            <FieldOffset(0)> Public mi As MOUSEINPUT
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Private Structure INPUT
            Public type As UInteger
            Public u As InputUnion
        End Structure

        Private Const INPUT_KEYBOARD As UInteger = 1UI
        Private Const KEYEVENTF_KEYUP As UInteger = 2UI
        Private Const KEYEVENTF_UNICODE As UInteger = 4UI
        Private Const VK_RETURN As UShort = &HDUS
        Private Const VK_TAB As UShort = &H9US
        Private Const VK_CONTROL As UShort = &H11US
        Private Const VK_V As UShort = &H56US

        <DllImport("user32.dll", SetLastError:=True)>
        Private Function SendInput(nInputs As UInteger, pInputs As INPUT(), cbSize As Integer) As UInteger
        End Function

        <DllImport("user32.dll")>
        Private Function GetForegroundWindow() As IntPtr
        End Function

        ''' <summary>Handle of the window that currently has the OS keyboard focus.</summary>
        Public Function ForegroundWindow() As IntPtr
            Return GetForegroundWindow()
        End Function

        ''' <summary>
        ''' Insert text into the focused control. useClipboard=False synthesizes Unicode
        ''' keystrokes; True copies + sends Ctrl+V (restoring the prior clipboard).
        ''' Clipboard mode MUST run on the UI/STA thread.
        ''' </summary>
        Public Sub InjectText(text As String, useClipboard As Boolean)
            If String.IsNullOrEmpty(text) Then Return
            Try
                If useClipboard Then
                    PasteViaClipboard(text)
                Else
                    TypeUnicode(text)
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DICT_INJECT_ERROR, $"Text injection failed: {ex.Message}")
            End Try
        End Sub

        Private Sub TypeUnicode(text As String)
            Dim inputs As New List(Of INPUT)()
            Dim prev As Char = ChrW(0)
            For Each ch As Char In text                ' iterate UTF-16 code units — surrogate pairs send as two units
                If ch = vbLf OrElse ch = vbCr Then
                    If ch = vbLf AndAlso prev = vbCr Then prev = ch : Continue For   ' collapse CRLF → one Enter
                    AddVk(inputs, VK_RETURN)
                ElseIf ch = vbTab Then
                    AddVk(inputs, VK_TAB)
                Else
                    AddUnicode(inputs, AscW(ch))
                End If
                prev = ch
            Next
            If inputs.Count = 0 Then Return
            Dim arr = inputs.ToArray()
            Dim sent = SendInput(CUInt(arr.Length), arr, Marshal.SizeOf(GetType(INPUT)))
            If sent = 0 Then
                AppLogger.Log(LogEvents.DICT_INJECT_ERROR,
                    $"SendInput delivered 0 events (err={Marshal.GetLastWin32Error()}) — focused window may be elevated")
            End If
        End Sub

        Private Sub AddUnicode(inputs As List(Of INPUT), code As Integer)
            inputs.Add(MakeKey(0, CUShort(code And &HFFFF), KEYEVENTF_UNICODE))
            inputs.Add(MakeKey(0, CUShort(code And &HFFFF), KEYEVENTF_UNICODE Or KEYEVENTF_KEYUP))
        End Sub

        Private Sub AddVk(inputs As List(Of INPUT), vk As UShort)
            inputs.Add(MakeKey(vk, 0, 0))
            inputs.Add(MakeKey(vk, 0, KEYEVENTF_KEYUP))
        End Sub

        Private Function MakeKey(vk As UShort, scan As UShort, flags As UInteger) As INPUT
            Dim i As New INPUT With {.type = INPUT_KEYBOARD}
            i.u.ki = New KEYBDINPUT With {.wVk = vk, .wScan = scan, .dwFlags = flags, .time = 0, .dwExtraInfo = IntPtr.Zero}
            Return i
        End Function

        Private Sub PasteViaClipboard(text As String)
            ' Save → set → Ctrl+V → restore. Restore in Finally so we never leak the
            ' dictated text onto the clipboard, even on error.
            Dim saved As IDataObject = Nothing
            Try
                Try : saved = Clipboard.GetDataObject() : Catch : End Try
                Clipboard.SetText(text)
                Dim inputs As New List(Of INPUT)()
                inputs.Add(MakeKey(VK_CONTROL, 0, 0))
                inputs.Add(MakeKey(VK_V, 0, 0))
                inputs.Add(MakeKey(VK_V, 0, KEYEVENTF_KEYUP))
                inputs.Add(MakeKey(VK_CONTROL, 0, KEYEVENTF_KEYUP))
                Dim arr = inputs.ToArray()
                SendInput(CUInt(arr.Length), arr, Marshal.SizeOf(GetType(INPUT)))
            Finally
                ' Let the target app read the clipboard before we restore it.
                Threading.Thread.Sleep(150)
                Try
                    If saved IsNot Nothing Then Clipboard.SetDataObject(saved, True) Else Clipboard.Clear()
                Catch
                End Try
            End Try
        End Sub

    End Module

End Namespace
