<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormPivotPreview
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.lblSource = New System.Windows.Forms.Label()
        Me.cboSource = New System.Windows.Forms.ComboBox()
        Me.lblEngine = New System.Windows.Forms.Label()
        Me.cboEngine = New System.Windows.Forms.ComboBox()
        Me.lvRoutes = New System.Windows.Forms.ListView()
        Me.colLang = New System.Windows.Forms.ColumnHeader()
        Me.colRoute = New System.Windows.Forms.ColumnHeader()
        Me.colWhy = New System.Windows.Forms.ColumnHeader()
        Me.lblSummary = New System.Windows.Forms.Label()
        Me.btnClose = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        ' lblSource
        '
        Me.lblSource.AutoSize = True
        Me.lblSource.Location = New System.Drawing.Point(12, 14)
        Me.lblSource.Name = "lblSource"
        Me.lblSource.Size = New System.Drawing.Size(100, 15)
        Me.lblSource.TabIndex = 0
        Me.lblSource.Text = "Source language:"
        '
        ' cboSource
        '
        Me.cboSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboSource.FormattingEnabled = True
        Me.cboSource.Location = New System.Drawing.Point(12, 32)
        Me.cboSource.Name = "cboSource"
        Me.cboSource.Size = New System.Drawing.Size(250, 23)
        Me.cboSource.TabIndex = 1
        '
        ' lblEngine
        '
        Me.lblEngine.AutoSize = True
        Me.lblEngine.Location = New System.Drawing.Point(280, 14)
        Me.lblEngine.Name = "lblEngine"
        Me.lblEngine.Size = New System.Drawing.Size(47, 15)
        Me.lblEngine.TabIndex = 2
        Me.lblEngine.Text = "Engine:"
        '
        ' cboEngine
        '
        Me.cboEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cboEngine.FormattingEnabled = True
        Me.cboEngine.Location = New System.Drawing.Point(280, 32)
        Me.cboEngine.Name = "cboEngine"
        Me.cboEngine.Size = New System.Drawing.Size(262, 23)
        Me.cboEngine.DropDownWidth = 320
        Me.cboEngine.TabIndex = 3
        '
        ' lvRoutes
        '
        Me.lvRoutes.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lvRoutes.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.colLang, Me.colRoute, Me.colWhy})
        Me.lvRoutes.FullRowSelect = True
        Me.lvRoutes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable
        Me.lvRoutes.Location = New System.Drawing.Point(12, 68)
        Me.lvRoutes.MultiSelect = False
        Me.lvRoutes.Name = "lvRoutes"
        Me.lvRoutes.Size = New System.Drawing.Size(530, 352)
        Me.lvRoutes.TabIndex = 4
        Me.lvRoutes.UseCompatibleStateImageBehavior = False
        Me.lvRoutes.View = System.Windows.Forms.View.Details
        '
        ' colLang
        '
        Me.colLang.Text = "Target language"
        Me.colLang.Width = 190
        '
        ' colRoute
        '
        Me.colRoute.Text = "Route"
        Me.colRoute.Width = 100
        '
        ' colWhy
        '
        Me.colWhy.Text = "Why"
        Me.colWhy.Width = 380
        '
        ' lblSummary
        '
        Me.lblSummary.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lblSummary.AutoSize = True
        Me.lblSummary.ForeColor = System.Drawing.SystemColors.GrayText
        Me.lblSummary.Location = New System.Drawing.Point(12, 432)
        Me.lblSummary.Name = "lblSummary"
        Me.lblSummary.Size = New System.Drawing.Size(120, 15)
        Me.lblSummary.TabIndex = 5
        Me.lblSummary.Text = "-"
        '
        ' btnClose
        '
        Me.btnClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnClose.Location = New System.Drawing.Point(455, 427)
        Me.btnClose.Name = "btnClose"
        Me.btnClose.Size = New System.Drawing.Size(87, 26)
        Me.btnClose.TabIndex = 6
        Me.btnClose.Text = "Close"
        '
        ' FormPivotPreview
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.btnClose
        Me.ClientSize = New System.Drawing.Size(554, 461)
        Me.Controls.Add(Me.lblSource)
        Me.Controls.Add(Me.cboSource)
        Me.Controls.Add(Me.lblEngine)
        Me.Controls.Add(Me.cboEngine)
        Me.Controls.Add(Me.lvRoutes)
        Me.Controls.Add(Me.lblSummary)
        Me.Controls.Add(Me.btnClose)
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New System.Drawing.Size(480, 360)
        Me.Name = "FormPivotPreview"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Translation routing preview"
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents lblSource As System.Windows.Forms.Label
    Friend WithEvents cboSource As System.Windows.Forms.ComboBox
    Friend WithEvents lblEngine As System.Windows.Forms.Label
    Friend WithEvents cboEngine As System.Windows.Forms.ComboBox
    Friend WithEvents lvRoutes As System.Windows.Forms.ListView
    Friend WithEvents colLang As System.Windows.Forms.ColumnHeader
    Friend WithEvents colRoute As System.Windows.Forms.ColumnHeader
    Friend WithEvents colWhy As System.Windows.Forms.ColumnHeader
    Friend WithEvents lblSummary As System.Windows.Forms.Label
    Friend WithEvents btnClose As System.Windows.Forms.Button
End Class
