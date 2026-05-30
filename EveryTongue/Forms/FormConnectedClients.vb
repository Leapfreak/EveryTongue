Imports System.Diagnostics
Imports EveryTongue.Models
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Public Class FormConnectedClients

    Private ReadOnly _getSubtitleSvc As Func(Of ISubtitleService)
    Private ReadOnly _getMetrics As Func(Of IMetricsService)
    Private _lastCpuTime As TimeSpan = TimeSpan.Zero
    Private _lastCpuCheck As DateTime = DateTime.MinValue

    Public Sub New(getSubtitleSvc As Func(Of ISubtitleService),
                   getMetrics As Func(Of IMetricsService),
                   theme As ThemeMode)
        InitializeComponent()
        _getSubtitleSvc = getSubtitleSvc
        _getMetrics = getMetrics
        ApplyTheme(theme)

        AddHandler btnRefresh.Click, Sub(s, e) RefreshData()
        AddHandler chkAutoRefresh.CheckedChanged, Sub(s, e)
                                                      tmrRefresh.Enabled = chkAutoRefresh.Checked
                                                  End Sub
        AddHandler tmrRefresh.Tick, Sub(s, e) RefreshData()
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        RefreshData()
        tmrRefresh.Enabled = chkAutoRefresh.Checked
    End Sub

    Private Sub RefreshData()
        Try
            RefreshGrid()
            RefreshPerformance()
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ConnectedClients] Refresh failed: {ex.Message}")
        End Try
    End Sub

    Private Sub RefreshGrid()
        Dim svc = _getSubtitleSvc?.Invoke()
        If svc Is Nothing Then
            dgvClients.Rows.Clear()
            lblClientSummary.Text = "Server not running"
            Return
        End If

        Dim clients = svc.GetClientSnapshots()
        Dim now = DateTime.Now

        dgvClients.SuspendLayout()
        dgvClients.Rows.Clear()

        For Each c In clients
            Dim duration = FormatDuration(now - c.ConnectedAt)
            Dim lang = If(String.IsNullOrEmpty(c.Language), "(original)", c.Language)
            dgvClients.Rows.Add(
                If(c.Device, ""),
                If(c.OS, ""),
                If(c.Browser, ""),
                lang,
                duration,
                If(c.RemoteEndpoint, ""),
                c.MessagesSent.ToString(),
                c.MessagesDropped.ToString()
            )

            If c.MessagesDropped > 0 Then
                Dim row = dgvClients.Rows(dgvClients.Rows.Count - 1)
                row.Cells("colDropped").Style.ForeColor = Color.FromArgb(255, 100, 100)
            End If
        Next

        dgvClients.ResumeLayout()

        ' Summary line
        Dim langGroups = clients.GroupBy(Function(c) If(String.IsNullOrEmpty(c.Language), "(original)", c.Language)).
                                  OrderByDescending(Function(g) g.Count()).
                                  Select(Function(g) $"{g.Key}: {g.Count()}").
                                  ToList()

        Dim platforms = clients.GroupBy(Function(c)
                                           If String.IsNullOrEmpty(c.OS) Then Return "Unknown"
                                           If c.OS.StartsWith("iOS") OrElse c.OS.StartsWith("iPadOS") Then Return "iOS"
                                           If c.OS.StartsWith("Android") Then Return "Android"
                                           Return c.OS
                                       End Function).
                                Select(Function(g) $"{g.Count()} {g.Key}").
                                ToList()

        Dim summary = $"{clients.Count} client{If(clients.Count <> 1, "s", "")}"
        If langGroups.Count > 0 Then summary &= $"  —  {String.Join(", ", langGroups)}"
        If platforms.Count > 0 Then summary &= $"  —  {String.Join(", ", platforms)}"
        lblClientSummary.Text = summary
    End Sub

    Private Sub RefreshPerformance()
        Dim proc = Process.GetCurrentProcess()
        Dim memMb = proc.WorkingSet64 \ (1024 * 1024)

        ' CPU usage estimate
        Dim cpuText = "—"
        Dim now = DateTime.UtcNow
        Dim currentCpuTime = proc.TotalProcessorTime
        If _lastCpuCheck > DateTime.MinValue Then
            Dim elapsed = (now - _lastCpuCheck).TotalMilliseconds
            If elapsed > 0 Then
                Dim cpuUsed = (currentCpuTime - _lastCpuTime).TotalMilliseconds
                Dim cpuPercent = (cpuUsed / elapsed / Environment.ProcessorCount) * 100
                cpuText = $"{cpuPercent:F1}%"
            End If
        End If
        _lastCpuTime = currentCpuTime
        _lastCpuCheck = now

        ' GPU info via nvidia-smi (quick, cached-ish by timer interval)
        Dim gpuText = GetGpuUsage()

        Dim metrics = _getMetrics?.Invoke()
        Dim snapshot = metrics?.GetSnapshot()

        lblPerfCpu.Text = $"CPU Usage:  {cpuText}  ({Environment.ProcessorCount} cores)"
        lblPerfGpu.Text = $"GPU:  {gpuText}"
        lblPerfMemory.Text = $"Memory:  {memMb} MB"

        If snapshot IsNot Nothing Then
            lblPerfClients.Text = $"Connected Clients:  {snapshot.Clients.Connected}"
            lblPerfBroadcast.Text = $"Broadcast Latency:  {snapshot.Broadcast.LatencyMs:F1} ms"
            lblPerfTranslation.Text = $"Translation Latency:  {snapshot.Translation.LatencyMs:F1} ms" &
                If(Not String.IsNullOrEmpty(snapshot.Translation.ActiveBackend),
                   $"  ({snapshot.Translation.ActiveBackend})", "")
            lblPerfMsgSent.Text = $"Messages Sent:  {snapshot.Broadcast.MessagesSent:N0}"
            lblPerfMsgDropped.Text = $"Messages Dropped:  {snapshot.Broadcast.MessagesDropped:N0}"
            If snapshot.Broadcast.MessagesDropped > 0 Then
                lblPerfMsgDropped.ForeColor = Color.FromArgb(255, 100, 100)
            End If
            lblPerfUptime.Text = $"Server Uptime:  {FormatDuration(TimeSpan.FromSeconds(snapshot.System.UptimeSeconds))}"

            ' Network estimate from bytes sent
            ' Not currently tracked in MetricsService — show client count as proxy
            Dim langBreakdown = ""
            If snapshot.Clients.ByLanguage?.Count > 0 Then
                langBreakdown = "  —  " & String.Join(", ",
                    snapshot.Clients.ByLanguage.Select(Function(kv) $"{kv.Key}: {kv.Value}"))
            End If
            lblPerfNetwork.Text = $"Languages Active:  {If(snapshot.Clients.ByLanguage?.Count, 0)}{langBreakdown}"
        Else
            lblPerfClients.Text = "Connected Clients:  —"
            lblPerfBroadcast.Text = "Broadcast Latency:  —"
            lblPerfTranslation.Text = "Translation Latency:  —"
            lblPerfMsgSent.Text = "Messages Sent:  —"
            lblPerfMsgDropped.Text = "Messages Dropped:  —"
            lblPerfUptime.Text = "Server Uptime:  —"
            lblPerfNetwork.Text = "Languages Active:  —"
        End If
    End Sub

    Private Shared Function GetGpuUsage() As String
        Try
            Dim psi As New ProcessStartInfo("nvidia-smi",
                "--query-gpu=utilization.gpu,memory.used,memory.total --format=csv,noheader,nounits") With {
                .RedirectStandardOutput = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            Using p = Process.Start(psi)
                If p Is Nothing Then Return "N/A (nvidia-smi not found)"
                Dim output = p.StandardOutput.ReadToEnd().Trim()
                p.WaitForExit(2000)
                If String.IsNullOrEmpty(output) Then Return "N/A"
                Dim parts = output.Split(","c)
                If parts.Length >= 3 Then
                    Return $"{parts(0).Trim()}% utilisation,  {parts(1).Trim()} / {parts(2).Trim()} MB VRAM"
                End If
                Return output
            End Using
        Catch
            Return "N/A (no NVIDIA GPU)"
        End Try
    End Function

    Private Sub ApplyTheme(theme As ThemeMode)
        Dim backColor, foreColor, controlBack, borderColor, headerBack, headerFore, tabBack As Color

        Select Case theme
            Case ThemeMode.Dark
                backColor = Color.FromArgb(30, 30, 35)
                foreColor = Color.FromArgb(220, 220, 220)
                controlBack = Color.FromArgb(30, 30, 35)
                borderColor = Color.FromArgb(60, 60, 65)
                headerBack = Color.FromArgb(45, 45, 50)
                headerFore = Color.FromArgb(180, 180, 180)
                tabBack = Color.FromArgb(37, 37, 42)
            Case ThemeMode.Light
                backColor = Color.White
                foreColor = Color.Black
                controlBack = Color.White
                borderColor = Color.FromArgb(200, 200, 200)
                headerBack = Color.FromArgb(230, 230, 230)
                headerFore = Color.FromArgb(60, 60, 60)
                tabBack = Color.FromArgb(245, 245, 245)
            Case Else ' System
                backColor = SystemColors.Control
                foreColor = SystemColors.ControlText
                controlBack = SystemColors.Window
                borderColor = SystemColors.ControlDark
                headerBack = SystemColors.Control
                headerFore = SystemColors.ControlText
                tabBack = SystemColors.Control
        End Select

        Me.BackColor = backColor
        Me.ForeColor = foreColor

        tabMain.BackColor = backColor
        tabClients.BackColor = tabBack
        tabClients.ForeColor = foreColor
        tabPerformance.BackColor = tabBack
        tabPerformance.ForeColor = foreColor

        lblClientSummary.BackColor = tabBack
        lblClientSummary.ForeColor = foreColor

        pnlBottom.BackColor = backColor
        btnClose.BackColor = backColor
        btnClose.ForeColor = foreColor
        btnClose.FlatAppearance.BorderColor = borderColor
        btnRefresh.BackColor = backColor
        btnRefresh.ForeColor = foreColor
        btnRefresh.FlatAppearance.BorderColor = borderColor
        chkAutoRefresh.ForeColor = foreColor

        dgvClients.BackgroundColor = controlBack
        dgvClients.GridColor = borderColor
        dgvClients.DefaultCellStyle.BackColor = controlBack
        dgvClients.DefaultCellStyle.ForeColor = foreColor
        dgvClients.DefaultCellStyle.SelectionBackColor = If(theme = ThemeMode.Light,
            Color.FromArgb(200, 220, 255), Color.FromArgb(60, 60, 80))
        dgvClients.DefaultCellStyle.SelectionForeColor = foreColor
        dgvClients.ColumnHeadersDefaultCellStyle.BackColor = headerBack
        dgvClients.ColumnHeadersDefaultCellStyle.ForeColor = headerFore

        ' Performance labels
        For Each ctrl As Control In tabPerformance.Controls
            If TypeOf ctrl Is Label Then
                ctrl.ForeColor = foreColor
            End If
        Next
    End Sub

    Private Shared Function FormatDuration(ts As TimeSpan) As String
        If ts.TotalDays >= 1 Then Return $"{CInt(ts.TotalDays)}d {ts.Hours}h"
        If ts.TotalHours >= 1 Then Return $"{CInt(ts.TotalHours)}h {ts.Minutes}m"
        If ts.TotalMinutes >= 1 Then Return $"{CInt(ts.TotalMinutes)}m"
        Return $"{CInt(ts.TotalSeconds)}s"
    End Function
End Class
