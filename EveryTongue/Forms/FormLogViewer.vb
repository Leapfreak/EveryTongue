Imports System.Text.RegularExpressions
Imports EveryTongue.Services.Infrastructure

Public Class FormLogViewer

    Private ReadOnly _getString As Func(Of String, String)
    Private ReadOnly _isDarkMode As Boolean
    Private ReadOnly _logLineRegex As New Regex(
        "^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \[(\??\d+)\] \[(\w+):(\w+)\] (.*)$",
        RegexOptions.Compiled)

    Private Class ParsedEntry
        Public Time As String
        Public Category As String
        Public Level As String
        Public Message As String
    End Class

    Private _allEntries As New List(Of ParsedEntry)
    Private _sessionPaths As New List(Of String)
    Private _searchIndex As Integer = -1

    Public Sub New(getString As Func(Of String, String), isDarkMode As Boolean)
        InitializeComponent()
        _getString = getString
        _isDarkMode = isDarkMode

        ApplyLocalization()
        ApplyTheme()
        PopulateFilterCombos()
        LoadSessionList()

        AddHandler cboSession.SelectedIndexChanged, AddressOf cboSession_SelectedIndexChanged
        AddHandler cboFilterCategory.SelectedIndexChanged, AddressOf Filter_Changed
        AddHandler cboFilterLevel.SelectedIndexChanged, AddressOf Filter_Changed
        AddHandler txtSearch.TextChanged, AddressOf Filter_Changed
        AddHandler btnSearchNext.Click, AddressOf btnSearchNext_Click
        AddHandler btnCopy.Click, AddressOf btnCopy_Click

        ' Double-buffer the DataGridView
        Dim dgvType = GetType(DataGridView)
        Dim pi = dgvType.GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
        pi?.SetValue(dgvLogHistory, True)
    End Sub

    Private Sub ApplyLocalization()
        Text = _getString("LogViewer_Title")
        lblSession.Text = _getString("LogViewer_SessionLabel")
        lblCategory.Text = _getString("LogViewer_CategoryFilter")
        lblLevel.Text = _getString("LogViewer_LevelFilter")
        txtSearch.PlaceholderText = _getString("LogViewer_Search")
        btnCopy.Text = _getString("LogViewer_Copy")
        btnClose.Text = _getString("LogViewer_Close")
    End Sub

    Private Sub ApplyTheme()
        If _isDarkMode Then
            BackColor = Drawing.Color.FromArgb(30, 30, 30)
            ForeColor = Drawing.Color.FromArgb(220, 220, 220)
            dgvLogHistory.BackgroundColor = Drawing.Color.FromArgb(25, 25, 25)
            dgvLogHistory.DefaultCellStyle.BackColor = Drawing.Color.FromArgb(30, 30, 30)
            dgvLogHistory.DefaultCellStyle.ForeColor = Drawing.Color.FromArgb(220, 220, 220)
            dgvLogHistory.DefaultCellStyle.SelectionBackColor = Drawing.Color.FromArgb(60, 60, 80)
            dgvLogHistory.DefaultCellStyle.SelectionForeColor = Drawing.Color.White
            dgvLogHistory.ColumnHeadersDefaultCellStyle.BackColor = Drawing.Color.FromArgb(45, 45, 45)
            dgvLogHistory.ColumnHeadersDefaultCellStyle.ForeColor = Drawing.Color.FromArgb(220, 220, 220)
            dgvLogHistory.EnableHeadersVisualStyles = False
            dgvLogHistory.GridColor = Drawing.Color.FromArgb(50, 50, 50)
            For Each ctrl In Controls.OfType(Of ComboBox)()
                ctrl.BackColor = Drawing.Color.FromArgb(45, 45, 45)
                ctrl.ForeColor = Drawing.Color.FromArgb(220, 220, 220)
            Next
            txtSearch.BackColor = Drawing.Color.FromArgb(45, 45, 45)
            txtSearch.ForeColor = Drawing.Color.FromArgb(220, 220, 220)
            For Each btn In Controls.OfType(Of Button)()
                btn.BackColor = Drawing.Color.FromArgb(55, 55, 55)
                btn.ForeColor = Drawing.Color.FromArgb(220, 220, 220)
                btn.FlatStyle = FlatStyle.Flat
                btn.FlatAppearance.BorderColor = Drawing.Color.FromArgb(80, 80, 80)
            Next
        End If
    End Sub

    Private Sub PopulateFilterCombos()
        Dim allText = _getString("LogViewer_FilterAll")
        cboFilterCategory.Items.Add(allText)
        For Each cat In [Enum].GetNames(GetType(LogCategory))
            cboFilterCategory.Items.Add(cat)
        Next
        cboFilterCategory.SelectedIndex = 0

        cboFilterLevel.Items.Add(allText)
        For Each lvl In [Enum].GetNames(GetType(LogSeverity))
            cboFilterLevel.Items.Add(lvl)
        Next
        cboFilterLevel.SelectedIndex = 0
    End Sub

    Private Sub LoadSessionList()
        cboSession.Items.Clear()
        _sessionPaths.Clear()

        Dim logDir = AppLogger.GetLogDir()
        If Not IO.Directory.Exists(logDir) Then
            cboSession.Items.Add(_getString("LogViewer_NoSessions"))
            cboSession.SelectedIndex = 0
            Return
        End If

        ' Collect session log paths: new session directories + legacy flat files
        Dim entries As New List(Of (Path As String, Display As String, SortKey As DateTime))

        ' Session directories (yyyyMMdd_HHmmss/session.log)
        For Each sessionDir In IO.Directory.GetDirectories(logDir, "????????_??????")
            Dim sessionLog = IO.Path.Combine(sessionDir, "session.log")
            If Not IO.File.Exists(sessionLog) Then Continue For
            Dim dirName = IO.Path.GetFileName(sessionDir)
            Dim fileInfo As New IO.FileInfo(sessionLog)
            Dim sizeKb = Math.Max(1, fileInfo.Length \ 1024)
            Dim dt As DateTime
            If DateTime.TryParseExact(dirName, "yyyyMMdd_HHmmss",
                Globalization.CultureInfo.InvariantCulture,
                Globalization.DateTimeStyles.None, dt) Then
                entries.Add((sessionLog, $"{dt:yyyy-MM-dd HH:mm:ss} ({sizeKb} KB)", dt))
            Else
                entries.Add((sessionLog, $"{dirName} ({sizeKb} KB)", fileInfo.LastWriteTime))
            End If
        Next

        ' Legacy flat log files (yyyyMMdd.log, yyyyMMdd_HHmmss.log)
        For Each filePath In IO.Directory.GetFiles(logDir, "*.log")
            If IO.Path.GetFileName(filePath).Contains("-server.") Then Continue For
            Dim fileName = IO.Path.GetFileNameWithoutExtension(filePath)
            Dim fileInfo As New IO.FileInfo(filePath)
            Dim sizeKb = Math.Max(1, fileInfo.Length \ 1024)

            If fileName.Length = 15 AndAlso fileName(8) = "_"c Then
                Dim dt As DateTime
                If DateTime.TryParseExact(fileName, "yyyyMMdd_HHmmss",
                    Globalization.CultureInfo.InvariantCulture,
                    Globalization.DateTimeStyles.None, dt) Then
                    entries.Add((filePath, $"{dt:yyyy-MM-dd HH:mm:ss} (legacy, {sizeKb} KB)", dt))
                Else
                    entries.Add((filePath, $"{fileName} ({sizeKb} KB)", fileInfo.LastWriteTime))
                End If
            ElseIf fileName.Length = 8 Then
                Dim dt As DateTime
                If DateTime.TryParseExact(fileName, "yyyyMMdd",
                    Globalization.CultureInfo.InvariantCulture,
                    Globalization.DateTimeStyles.None, dt) Then
                    entries.Add((filePath, $"{dt:yyyy-MM-dd} (legacy, {sizeKb} KB)", dt))
                Else
                    entries.Add((filePath, $"{fileName} ({sizeKb} KB)", fileInfo.LastWriteTime))
                End If
            Else
                entries.Add((filePath, $"{fileName} ({sizeKb} KB)", fileInfo.LastWriteTime))
            End If
        Next

        If entries.Count = 0 Then
            cboSession.Items.Add(_getString("LogViewer_NoSessions"))
            cboSession.SelectedIndex = 0
            Return
        End If

        For Each entry In entries.OrderByDescending(Function(e) e.SortKey)
            cboSession.Items.Add(entry.Display)
            _sessionPaths.Add(entry.Path)
        Next

        If cboSession.Items.Count > 0 Then
            cboSession.SelectedIndex = 0
        End If
    End Sub

    Private Sub cboSession_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim idx = cboSession.SelectedIndex
        If idx < 0 OrElse idx >= _sessionPaths.Count Then Return
        LoadSession(_sessionPaths(idx))
    End Sub

    Private Sub LoadSession(filePath As String)
        _allEntries.Clear()
        _searchIndex = -1

        Try
            ' Read with FileShare.ReadWrite so we can read while AppLogger is writing
            Dim lines As String()
            Using fs As New IO.FileStream(filePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                Using reader As New IO.StreamReader(fs)
                    lines = reader.ReadToEnd().Split({Environment.NewLine, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                End Using
            End Using

            For Each line In lines
                _allEntries.Add(ParseLogLine(line))
            Next

            lblSessionInfo.Text = String.Format(_getString("LogViewer_Lines"), _allEntries.Count)
        Catch ex As Exception
            lblSessionInfo.Text = $"Error: {ex.Message}"
        End Try

        ApplyFilter()
    End Sub

    Private Function ParseLogLine(line As String) As ParsedEntry
        Dim m = _logLineRegex.Match(line)
        If m.Success Then
            Dim timeStr = m.Groups(1).Value
            ' Extract just HH:mm:ss.fff for display
            Dim spaceIdx = timeStr.IndexOf(" "c)
            Dim shortTime = If(spaceIdx > 0, timeStr.Substring(spaceIdx + 1), timeStr)

            Return New ParsedEntry With {
                .Time = shortTime,
                .Category = m.Groups(3).Value,
                .Level = m.Groups(4).Value,
                .Message = m.Groups(5).Value
            }
        End If

        ' Unparseable line — treat as Legacy/Info
        Dim entry As New ParsedEntry With {
            .Category = "Legacy",
            .Level = "INFO",
            .Message = line
        }
        ' Try to extract timestamp from start
        If line.Length >= 23 Then
            Dim ts = line.Substring(0, 23)
            Dim dt As DateTime
            If DateTime.TryParseExact(ts, "yyyy-MM-dd HH:mm:ss.fff",
                Globalization.CultureInfo.InvariantCulture,
                Globalization.DateTimeStyles.None, dt) Then
                entry.Time = ts.Substring(11) ' HH:mm:ss.fff
                entry.Message = If(line.Length > 24, line.Substring(24), "")
            Else
                entry.Time = ""
            End If
        End If
        Return entry
    End Function

    Private Sub ApplyFilter()
        dgvLogHistory.Rows.Clear()
        _searchIndex = -1

        Dim catFilter = If(cboFilterCategory.SelectedIndex <= 0, "", cboFilterCategory.SelectedItem?.ToString())
        Dim lvlFilter = If(cboFilterLevel.SelectedIndex <= 0, "", cboFilterLevel.SelectedItem?.ToString())
        Dim searchText = txtSearch.Text.Trim()

        dgvLogHistory.SuspendLayout()
        Try
            For Each entry In _allEntries
                If Not String.IsNullOrEmpty(catFilter) AndAlso
                    Not entry.Category.Equals(catFilter, StringComparison.OrdinalIgnoreCase) Then Continue For
                If Not String.IsNullOrEmpty(lvlFilter) AndAlso
                    Not entry.Level.Equals(lvlFilter, StringComparison.OrdinalIgnoreCase) Then Continue For
                If Not String.IsNullOrEmpty(searchText) AndAlso
                    entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0 Then Continue For

                Dim idx = dgvLogHistory.Rows.Add(entry.Time, entry.Category, entry.Level, entry.Message)
                Dim row = dgvLogHistory.Rows(idx)

                ' Color code by level
                Select Case entry.Level.ToUpperInvariant()
                    Case "ERROR"
                        row.DefaultCellStyle.ForeColor = Drawing.Color.Red
                    Case "WARNING"
                        row.DefaultCellStyle.ForeColor = Drawing.Color.Orange
                    Case "DEBUG"
                        row.DefaultCellStyle.ForeColor = If(_isDarkMode,
                            Drawing.Color.FromArgb(140, 140, 140),
                            Drawing.Color.FromArgb(120, 120, 120))
                End Select
            Next
        Finally
            dgvLogHistory.ResumeLayout()
        End Try
    End Sub

    Private Sub Filter_Changed(sender As Object, e As EventArgs)
        If _allEntries.Count > 0 Then ApplyFilter()
    End Sub

    Private Sub btnSearchNext_Click(sender As Object, e As EventArgs)
        Dim searchText = txtSearch.Text.Trim()
        If String.IsNullOrEmpty(searchText) Then Return
        If dgvLogHistory.Rows.Count = 0 Then Return

        Dim startRow = _searchIndex + 1
        If startRow >= dgvLogHistory.Rows.Count Then startRow = 0

        For i = 0 To dgvLogHistory.Rows.Count - 1
            Dim rowIdx = (startRow + i) Mod dgvLogHistory.Rows.Count
            Dim msg = dgvLogHistory.Rows(rowIdx).Cells(3).Value?.ToString()
            If msg IsNot Nothing AndAlso msg.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 Then
                _searchIndex = rowIdx
                dgvLogHistory.ClearSelection()
                dgvLogHistory.Rows(rowIdx).Selected = True
                dgvLogHistory.FirstDisplayedScrollingRowIndex = rowIdx
                Return
            End If
        Next
    End Sub

    Private Sub btnCopy_Click(sender As Object, e As EventArgs)
        If dgvLogHistory.Rows.Count = 0 Then Return

        Dim sb As New Text.StringBuilder()
        For Each row As DataGridViewRow In dgvLogHistory.Rows
            sb.AppendLine($"{row.Cells(0).Value}{vbTab}{row.Cells(1).Value}{vbTab}{row.Cells(2).Value}{vbTab}{row.Cells(3).Value}")
        Next
        Clipboard.SetText(sb.ToString())
    End Sub

End Class
