Imports System.Diagnostics
Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Server

Namespace Controllers
    ''' <summary>
    ''' Manages the Kestrel subtitle server lifecycle — start/stop/restart,
    ''' server log, subtitle appearance, firewall rules, URL copy.
    ''' Extracted from FormMain.
    ''' </summary>
    Friend Class ServerController

        ' UI controls
        Private ReadOnly _btnServerStart As Button
        Private ReadOnly _btnServerStop As Button
        Private ReadOnly _btnServerRestart As Button
        Private ReadOnly _nudServerPort As NumericUpDown
        Private ReadOnly _btnCopyUrl As Button
        Private ReadOnly _lblServerStatus As Label
        Private ReadOnly _lblServerUrl As Label
        Private ReadOnly _lblServerClients As Label
        Private ReadOnly _rtbServerLog As RichTextBox
        Private ReadOnly _btnSubtitleBg As Button
        Private ReadOnly _btnSubtitleFg As Button
        Private ReadOnly _cboSubtitleFont As ComboBox
        Private ReadOnly _nudSubtitleSize As NumericUpDown
        Private ReadOnly _chkSubtitleBold As CheckBox
        Private ReadOnly _wvLiveClients As Microsoft.Web.WebView2.WinForms.WebView2

        ' Callbacks
        Private ReadOnly _config As AppConfig
        Private ReadOnly _saveUiToConfig As Action
        Private ReadOnly _updateShellStatus As Action
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _sendMessage As Action(Of IntPtr, Integer, IntPtr, IntPtr)

        ' Win32 constants for log flushing
        Private Const WM_SETREDRAW As Integer = &HB
        Private Const WM_VSCROLL As Integer = &H115
        Private Const SB_BOTTOM As Integer = 7

        ' State
        Private _kestrelHost As KestrelHost
        Private _serverPort As Integer = 0
        Private _isInitializing As Boolean = True
        Private ReadOnly _serverLogBuffer As New System.Collections.Concurrent.ConcurrentQueue(Of String)
        Private _serverLogPending As Integer = 0
        Private Const ServerLogMaxLines As Integer = 2000

        Public ReadOnly Property KestrelHost As KestrelHost
            Get
                Return _kestrelHost
            End Get
        End Property

        Public ReadOnly Property Port As Integer
            Get
                Return _serverPort
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning
            End Get
        End Property

        Public Sub New(config As AppConfig,
                       btnServerStart As Button, btnServerStop As Button, btnServerRestart As Button,
                       nudServerPort As NumericUpDown, btnCopyUrl As Button,
                       lblServerStatus As Label, lblServerUrl As Label, lblServerClients As Label,
                       rtbServerLog As RichTextBox,
                       btnSubtitleBg As Button, btnSubtitleFg As Button,
                       cboSubtitleFont As ComboBox, nudSubtitleSize As NumericUpDown, chkSubtitleBold As CheckBox,
                       wvLiveClients As Microsoft.Web.WebView2.WinForms.WebView2,
                       saveUiToConfig As Action, updateShellStatus As Action, log As Action(Of String),
                       sendMessage As Action(Of IntPtr, Integer, IntPtr, IntPtr))
            _config = config
            _btnServerStart = btnServerStart
            _btnServerStop = btnServerStop
            _btnServerRestart = btnServerRestart
            _nudServerPort = nudServerPort
            _btnCopyUrl = btnCopyUrl
            _lblServerStatus = lblServerStatus
            _lblServerUrl = lblServerUrl
            _lblServerClients = lblServerClients
            _rtbServerLog = rtbServerLog
            _btnSubtitleBg = btnSubtitleBg
            _btnSubtitleFg = btnSubtitleFg
            _cboSubtitleFont = cboSubtitleFont
            _nudSubtitleSize = nudSubtitleSize
            _chkSubtitleBold = chkSubtitleBold
            _wvLiveClients = wvLiveClients
            _saveUiToConfig = saveUiToConfig
            _updateShellStatus = updateShellStatus
            _log = log
            _sendMessage = sendMessage
        End Sub

        Public Sub WireEvents()
            AddHandler _btnServerStart.Click, Sub(s, e) StartServer()
            AddHandler _btnServerStop.Click, Sub(s, e) StopServer()
            AddHandler _btnServerRestart.Click, Sub(s, e) RestartServer()
            AddHandler _btnSubtitleBg.Click, Sub(s, e) PickSubtitleBgColor()
            AddHandler _btnSubtitleFg.Click, Sub(s, e) PickSubtitleFgColor()
            AddHandler _cboSubtitleFont.SelectedIndexChanged, Sub(s, e) ApplyLiveOutputFont()
            AddHandler _nudSubtitleSize.ValueChanged, Sub(s, e) ApplyLiveOutputFont()
            AddHandler _chkSubtitleBold.CheckedChanged, Sub(s, e) ApplyLiveOutputFont()
            AddHandler _btnCopyUrl.Click, Sub(s, e) CopyPhoneUrl()
        End Sub

        Public Sub DoneInitializing()
            _isInitializing = False
        End Sub

        ''' <summary>
        ''' Start the Kestrel server. Caller provides live-specific callbacks
        ''' that wire SubtitleService events to the live session.
        ''' </summary>
        Public Sub StartServer(Optional configureSubtitleSvc As Action(Of Services.Interfaces.ISubtitleService) = Nothing)
            _saveUiToConfig()
            Dim port = CInt(_nudServerPort.Value)
            _serverPort = port
            If _config.AllowFirewall Then EnsureFirewallRule(port)

            Try
                _kestrelHost = New KestrelHost()
                AddHandler _kestrelHost.StatusChanged, Sub(s, msg) AppendServerLog(msg)

                Dim resolvedBiblesDir = AppConfig.ResolvePath(If(_config.BiblesDirectory, ".\Bibles"))

                Dim kestrelOptions As New ServerOptions() With {
                    .HttpPort = port,
                    .AllowRemote = _config.AllowFirewall,
                    .BgColor = _config.SubtitleBgColor,
                    .FgColor = _config.SubtitleFgColor,
                    .AdminPin = If(_config.AdminPin, ""),
                    .BiblesDirectory = resolvedBiblesDir,
                    .TtsBackends = If(_config.TtsBackends, "")
                }

                _kestrelHost.Start(kestrelOptions, Sub(msg) AppendServerLog(msg))

                ' Let caller wire live-specific subtitle service events
                Dim svc = GetSubtitleService()
                If svc IsNot Nothing Then
                    svc.BgColor = _config.SubtitleBgColor
                    svc.FgColor = _config.SubtitleFgColor

                    AddHandler svc.StatusChanged, Sub(s, msg)
                                                      AppendServerLog(msg)
                                                      If _lblServerClients.InvokeRequired Then
                                                          _lblServerClients.BeginInvoke(Sub()
                                                                                            Dim sv = GetSubtitleService()
                                                                                            If sv IsNot Nothing Then
                                                                                                _lblServerClients.Text = $"Connected clients: {sv.ConnectedClients}"
                                                                                            End If
                                                                                            _updateShellStatus()
                                                                                        End Sub)
                                                      Else
                                                          _lblServerClients.Text = $"Connected clients: {svc.ConnectedClients}"
                                                          _updateShellStatus()
                                                      End If
                                                  End Sub

                    AddHandler svc.LogMessage, Sub(s, msg) _log(msg)

                    configureSubtitleSvc?.Invoke(svc)
                End If

                UpdateServerUi(True)
                AppendServerLog($"Server started on HTTP:{port} HTTPS:{port + 1}")
                NavigateLivePreview(port)
                Dim localIp = GetLocalIpAddress()
                AppendServerLog($"Phones should open: https://{localIp}:{port + 1}")
                AppendServerLog($"(Accept the certificate warning on first visit)")
            Catch ex As Exception
                AppendServerLog($"ERROR: {ex.Message}")
                AppendServerLog("Tip: Try running as Administrator, or use a different port.")
                _kestrelHost = Nothing
            End Try
        End Sub

        Public Sub StopServer()
            EndpointRegistration.RemoteCommandHandler = Nothing
            Try : _kestrelHost?.Dispose() : Catch : End Try
            _kestrelHost = Nothing
            _serverPort = 0
            UpdateServerUi(False)
            AppendServerLog("Server stopped.")
        End Sub

        Private Sub RestartServer()
            EndpointRegistration.RemoteCommandHandler = Nothing
            Try : _kestrelHost?.Dispose() : Catch : End Try
            _kestrelHost = Nothing
            _serverPort = 0
            AppendServerLog("Restarting server...")
            StartServer()
        End Sub

        Public Function GetSubtitleService() As Services.Interfaces.ISubtitleService
            Return TryCast(_kestrelHost?.Services?.GetService(
                GetType(Services.Interfaces.ISubtitleService)), Services.Interfaces.ISubtitleService)
        End Function

        Friend Sub AppendServerLog(text As String)
            _log($"[Server] {text}")
            _serverLogBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss}] {text}")
            If Interlocked.CompareExchange(_serverLogPending, 1, 0) = 0 Then
                _rtbServerLog.BeginInvoke(Sub() FlushServerLog())
            End If
        End Sub

        Private Sub FlushServerLog()
            Interlocked.Exchange(_serverLogPending, 0)
            Dim lines As New System.Text.StringBuilder()
            Dim line As String = Nothing
            While _serverLogBuffer.TryDequeue(line)
                lines.AppendLine(line)
            End While
            If lines.Length = 0 Then Return

            _sendMessage(_rtbServerLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero)
            Try
                _rtbServerLog.AppendText(lines.ToString())
                If _rtbServerLog.Lines.Length > ServerLogMaxLines Then
                    Dim removeUpTo = _rtbServerLog.GetFirstCharIndexFromLine(_rtbServerLog.Lines.Length - ServerLogMaxLines)
                    _rtbServerLog.Select(0, removeUpTo)
                    _rtbServerLog.SelectedText = ""
                End If
            Finally
                _sendMessage(_rtbServerLog.Handle, WM_SETREDRAW, New IntPtr(1), IntPtr.Zero)
                _rtbServerLog.Invalidate()
            End Try
            _sendMessage(_rtbServerLog.Handle, WM_VSCROLL, New IntPtr(SB_BOTTOM), IntPtr.Zero)
        End Sub

        Private Sub NavigateLivePreview(port As Integer)
            Try
                Dim bust = DateTime.Now.Ticks
                _wvLiveClients.Source = New Uri($"http://127.0.0.1:{port}/?preview=1&_cb={bust}")
            Catch ex As Exception
                AppendServerLog($"Live preview: {ex.Message}")
            End Try
        End Sub

        Private Sub UpdateServerUi(running As Boolean)
            _btnServerStart.Enabled = Not running
            _btnServerStop.Enabled = running
            _btnServerRestart.Enabled = running
            _nudServerPort.Enabled = Not running
            _btnCopyUrl.Enabled = running

            If running Then
                Dim ip = GetLocalIpAddress()
                Dim url = $"http://{ip}:{_serverPort}"
                _lblServerStatus.Text = "Status: Running"
                _lblServerStatus.ForeColor = Drawing.Color.Green
                _lblServerUrl.Text = $"URL: {url}"
            Else
                _lblServerStatus.Text = "Status: Stopped"
                _lblServerStatus.ForeColor = Drawing.SystemColors.ControlText
                _lblServerUrl.Text = "URL: (not running)"
                _lblServerClients.Text = "Connected clients: 0"
            End If

            _updateShellStatus()
        End Sub

        Private Sub PickSubtitleBgColor()
            Using dlg As New ColorDialog()
                dlg.Color = _btnSubtitleBg.BackColor
                dlg.FullOpen = True
                If dlg.ShowDialog() = DialogResult.OK Then
                    _btnSubtitleBg.BackColor = dlg.Color
                    _config.SubtitleBgColor = ColorToHex(dlg.Color)
                    ConfigManager.Save(_config)
                    Dim svc = GetSubtitleService()
                    If svc IsNot Nothing Then svc.BgColor = _config.SubtitleBgColor
                End If
            End Using
        End Sub

        Private Sub PickSubtitleFgColor()
            Using dlg As New ColorDialog()
                dlg.Color = _btnSubtitleFg.BackColor
                dlg.FullOpen = True
                If dlg.ShowDialog() = DialogResult.OK Then
                    _btnSubtitleFg.BackColor = dlg.Color
                    _config.SubtitleFgColor = ColorToHex(dlg.Color)
                    ConfigManager.Save(_config)
                    Dim svc = GetSubtitleService()
                    If svc IsNot Nothing Then svc.FgColor = _config.SubtitleFgColor
                End If
            End Using
        End Sub

        Private Sub ApplyLiveOutputFont()
            If _nudSubtitleSize Is Nothing OrElse _cboSubtitleFont Is Nothing OrElse _chkSubtitleBold Is Nothing Then Return
            If Not _isInitializing Then
                _config.SubtitleFontFamily = If(_cboSubtitleFont.SelectedItem?.ToString(), "Segoe UI")
                _config.SubtitleFontSize = CSng(_nudSubtitleSize.Value)
                _config.SubtitleFontBold = _chkSubtitleBold.Checked
                ConfigManager.Save(_config)
            End If
        End Sub

        Public Sub CopyPhoneUrl()
            If _kestrelHost IsNot Nothing AndAlso _kestrelHost.IsRunning Then
                Dim url = $"https://{GetLocalIpAddress()}:{_serverPort + 1}"
                Clipboard.SetText(url)
                AppendServerLog("URL copied to clipboard.")
            End If
        End Sub

        Public Sub VerifyAllPaths()
            Dim sb As New Text.StringBuilder()
            Dim allOk = True

            Dim checks As (Label As String, Path As String, IsDir As Boolean)() = {
                ("Whisper", AppConfig.ResolvePath(_config.PathWhisper), False),
                ("Whisper Model", AppConfig.ResolvePath(_config.PathModel), False),
                ("Audio Model", AppConfig.ResolvePath(_config.PathModelAudio), False),
                ("FFmpeg", AppConfig.ResolvePath(_config.PathFfmpeg), False),
                ("FFprobe", AppConfig.ResolvePath(_config.PathFfprobe), False),
                ("yt-dlp", AppConfig.ResolvePath(_config.PathYtdlp), False),
                ("Bibles", AppConfig.ResolvePath(_config.BiblesDirectory), True)
            }

            For Each item In checks
                Dim resolved = item.Path
                Dim exists = If(item.IsDir, IO.Directory.Exists(resolved), IO.File.Exists(resolved))
                If String.IsNullOrWhiteSpace(resolved) Then
                    sb.AppendLine($"  NOT SET: {item.Label}")
                    allOk = False
                ElseIf Not exists Then
                    sb.AppendLine($"  MISSING: {item.Label} → {resolved}")
                    allOk = False
                Else
                    sb.AppendLine($"  OK: {item.Label}")
                End If
            Next

            If allOk Then
                MessageBox.Show("All paths verified successfully." & Environment.NewLine & Environment.NewLine & sb.ToString(),
                    "Verify Paths", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                MessageBox.Show("Some paths are missing or not set:" & Environment.NewLine & Environment.NewLine & sb.ToString(),
                    "Verify Paths", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Friend Shared Function GetLocalIpAddress() As String
            Try
                For Each addr In System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList
                    If addr.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
                        Dim ip = addr.ToString()
                        If Not ip.StartsWith("127.") Then Return ip
                    End If
                Next
            Catch ex As Exception
                FormMain.WriteDebugLog($"[ERROR] GetLocalIpAddress: {ex.Message}")
            End Try
            Return "127.0.0.1"
        End Function

        Private Shared Function ColorToHex(c As Drawing.Color) As String
            Return $"#{c.R:X2}{c.G:X2}{c.B:X2}"
        End Function

        Private Shared Sub EnsureFirewallRule(port As Integer)
            Const ruleName As String = "EveryTongue Subtitle Server"
            Dim httpsPort = port + 1

            Dim cmd = $"advfirewall firewall delete rule name=""{ruleName}"" & " &
                      $"netsh advfirewall firewall add rule name=""{ruleName}"" dir=in action=allow protocol=TCP localport={port},{httpsPort}"

            Try
                Dim psi As New ProcessStartInfo() With {
                    .FileName = "netsh",
                    .Arguments = cmd,
                    .UseShellExecute = False,
                    .CreateNoWindow = True,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True
                }
                Dim p = Process.Start(psi)
                p.WaitForExit(5000)
                If p.ExitCode = 0 Then Return
            Catch ex As Exception
                FormMain.WriteDebugLog($"[ERROR] EnsureFirewallRule (netsh direct): {ex.Message}")
            End Try

            Try
                Dim fullCmd = $"/c netsh advfirewall firewall delete rule name=""{ruleName}"" & " &
                              $"netsh advfirewall firewall add rule name=""{ruleName}"" dir=in action=allow protocol=TCP localport={port},{httpsPort}"
                Dim psi As New ProcessStartInfo() With {
                    .FileName = "cmd.exe",
                    .Arguments = fullCmd,
                    .Verb = "runas",
                    .UseShellExecute = True,
                    .CreateNoWindow = True,
                    .WindowStyle = ProcessWindowStyle.Hidden
                }
                Dim p = Process.Start(psi)
                p?.WaitForExit(10000)
            Catch ex As Exception
                FormMain.WriteDebugLog($"[ERROR] EnsureFirewallRule (elevated cmd): {ex.Message}")
            End Try
        End Sub

    End Class
End Namespace
