<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormConnectedClients
    Inherits System.Windows.Forms.Form

    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing Then
                tmrRefresh?.Stop()
                components?.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()

        tabMain = New TabControl()
        tabClients = New TabPage()
        tabPerformance = New TabPage()
        pnlBottom = New Panel()
        btnClose = New Button()
        btnRefresh = New Button()
        chkAutoRefresh = New CheckBox()
        dgvClients = New DataGridView()
        lblClientSummary = New Label()
        tmrRefresh = New Timer(Me.components)

        colDevice = New DataGridViewTextBoxColumn()
        colOS = New DataGridViewTextBoxColumn()
        colBrowser = New DataGridViewTextBoxColumn()
        colLanguage = New DataGridViewTextBoxColumn()
        colConnected = New DataGridViewTextBoxColumn()
        colIP = New DataGridViewTextBoxColumn()
        colSent = New DataGridViewTextBoxColumn()
        colDropped = New DataGridViewTextBoxColumn()

        ' Performance tab controls
        lblPerfMemory = New Label()
        lblPerfUptime = New Label()
        lblPerfCpu = New Label()
        lblPerfGpu = New Label()
        lblPerfNetwork = New Label()
        lblPerfBroadcast = New Label()
        lblPerfTranslation = New Label()
        lblPerfMsgSent = New Label()
        lblPerfMsgDropped = New Label()
        lblPerfClients = New Label()

        Me.SuspendLayout()
        pnlBottom.SuspendLayout()
        CType(dgvClients, System.ComponentModel.ISupportInitialize).BeginInit()

        ' tabMain
        tabMain.Dock = DockStyle.Fill
        tabMain.TabPages.AddRange(New TabPage() {tabClients, tabPerformance})
        tabMain.Font = New Font("Segoe UI", 9.0F)

        ' tabClients
        tabClients.Text = "Clients"
        tabClients.Padding = New Padding(0)
        tabClients.Controls.Add(dgvClients)
        tabClients.Controls.Add(lblClientSummary)

        ' lblClientSummary
        lblClientSummary.Dock = DockStyle.Bottom
        lblClientSummary.Height = 36
        lblClientSummary.Font = New Font("Segoe UI", 9.0F)
        lblClientSummary.TextAlign = ContentAlignment.MiddleLeft
        lblClientSummary.Padding = New Padding(8, 0, 8, 0)
        lblClientSummary.Text = ""

        ' tabPerformance
        tabPerformance.Text = "Performance"
        tabPerformance.Padding = New Padding(16, 12, 16, 12)

        Dim perfY = 8
        Dim perfGap = 30

        ' Performance labels — arranged vertically
        For Each lbl In {lblPerfClients, lblPerfCpu, lblPerfGpu, lblPerfMemory,
                         lblPerfNetwork, lblPerfBroadcast, lblPerfTranslation,
                         lblPerfMsgSent, lblPerfMsgDropped, lblPerfUptime}
            lbl.AutoSize = False
            lbl.Location = New Point(16, perfY)
            lbl.Size = New Size(650, 24)
            lbl.Font = New Font("Segoe UI", 10.0F)
            lbl.TextAlign = ContentAlignment.MiddleLeft
            tabPerformance.Controls.Add(lbl)
            perfY += perfGap
        Next

        lblPerfClients.Text = "Clients: —"
        lblPerfCpu.Text = "CPU: —"
        lblPerfGpu.Text = "GPU: —"
        lblPerfMemory.Text = "Memory: —"
        lblPerfNetwork.Text = "Network: —"
        lblPerfBroadcast.Text = "Broadcast Latency: —"
        lblPerfTranslation.Text = "Translation Latency: —"
        lblPerfMsgSent.Text = "Messages Sent: —"
        lblPerfMsgDropped.Text = "Messages Dropped: —"
        lblPerfUptime.Text = "Server Uptime: —"

        ' pnlBottom
        pnlBottom.Dock = DockStyle.Bottom
        pnlBottom.Height = 44
        pnlBottom.Padding = New Padding(8, 6, 8, 6)
        pnlBottom.Controls.AddRange(New Control() {btnClose, btnRefresh, chkAutoRefresh})

        ' btnClose
        btnClose.Text = "Close"
        btnClose.Size = New Size(80, 28)
        btnClose.Dock = DockStyle.Right
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.DialogResult = DialogResult.Cancel

        ' btnRefresh
        btnRefresh.Text = "Refresh"
        btnRefresh.Size = New Size(80, 28)
        btnRefresh.Dock = DockStyle.Left
        btnRefresh.FlatStyle = FlatStyle.Flat

        ' chkAutoRefresh
        chkAutoRefresh.Text = "Auto-refresh (5s)"
        chkAutoRefresh.Dock = DockStyle.Left
        chkAutoRefresh.Width = 140
        chkAutoRefresh.Checked = True
        chkAutoRefresh.Padding = New Padding(12, 0, 0, 0)

        ' dgvClients
        dgvClients.Dock = DockStyle.Fill
        dgvClients.AllowUserToAddRows = False
        dgvClients.AllowUserToDeleteRows = False
        dgvClients.AllowUserToResizeRows = False
        dgvClients.[ReadOnly] = True
        dgvClients.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        dgvClients.MultiSelect = False
        dgvClients.RowHeadersVisible = False
        dgvClients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        dgvClients.BorderStyle = BorderStyle.None
        dgvClients.DefaultCellStyle.Font = New Font("Segoe UI", 9.0F)
        dgvClients.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI Semibold", 9.0F)
        dgvClients.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        dgvClients.ColumnHeadersHeight = 30
        dgvClients.EnableHeadersVisualStyles = False
        dgvClients.RowTemplate.Height = 28

        ' Columns
        colDevice.HeaderText = "Device"
        colDevice.Name = "colDevice"
        colDevice.FillWeight = 22

        colOS.HeaderText = "OS"
        colOS.Name = "colOS"
        colOS.FillWeight = 14

        colBrowser.HeaderText = "Browser"
        colBrowser.Name = "colBrowser"
        colBrowser.FillWeight = 14

        colLanguage.HeaderText = "Language"
        colLanguage.Name = "colLanguage"
        colLanguage.FillWeight = 14

        colConnected.HeaderText = "Connected"
        colConnected.Name = "colConnected"
        colConnected.FillWeight = 12

        colIP.HeaderText = "IP Address"
        colIP.Name = "colIP"
        colIP.FillWeight = 12

        colSent.HeaderText = "Sent"
        colSent.Name = "colSent"
        colSent.FillWeight = 6

        colDropped.HeaderText = "Dropped"
        colDropped.Name = "colDropped"
        colDropped.FillWeight = 6

        dgvClients.Columns.AddRange(New DataGridViewColumn() {
            colDevice, colOS, colBrowser, colLanguage, colConnected, colIP, colSent, colDropped
        })

        ' tmrRefresh
        tmrRefresh.Interval = 5000

        ' FormConnectedClients
        Me.AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(720, 450)
        Me.Controls.Add(tabMain)
        Me.Controls.Add(pnlBottom)
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(550, 350)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.Text = "Connected Clients"
        Me.CancelButton = btnClose
        Me.Font = New Font("Segoe UI", 9.0F)

        pnlBottom.ResumeLayout(False)
        CType(dgvClients, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents tabMain As TabControl
    Friend WithEvents tabClients As TabPage
    Friend WithEvents tabPerformance As TabPage
    Friend WithEvents pnlBottom As Panel
    Friend WithEvents btnClose As Button
    Friend WithEvents btnRefresh As Button
    Friend WithEvents chkAutoRefresh As CheckBox
    Friend WithEvents dgvClients As DataGridView
    Friend WithEvents lblClientSummary As Label
    Friend WithEvents tmrRefresh As Timer
    Friend WithEvents colDevice As DataGridViewTextBoxColumn
    Friend WithEvents colOS As DataGridViewTextBoxColumn
    Friend WithEvents colBrowser As DataGridViewTextBoxColumn
    Friend WithEvents colLanguage As DataGridViewTextBoxColumn
    Friend WithEvents colConnected As DataGridViewTextBoxColumn
    Friend WithEvents colIP As DataGridViewTextBoxColumn
    Friend WithEvents colSent As DataGridViewTextBoxColumn
    Friend WithEvents colDropped As DataGridViewTextBoxColumn
    Friend WithEvents lblPerfMemory As Label
    Friend WithEvents lblPerfUptime As Label
    Friend WithEvents lblPerfCpu As Label
    Friend WithEvents lblPerfGpu As Label
    Friend WithEvents lblPerfNetwork As Label
    Friend WithEvents lblPerfBroadcast As Label
    Friend WithEvents lblPerfTranslation As Label
    Friend WithEvents lblPerfMsgSent As Label
    Friend WithEvents lblPerfMsgDropped As Label
    Friend WithEvents lblPerfClients As Label
End Class
