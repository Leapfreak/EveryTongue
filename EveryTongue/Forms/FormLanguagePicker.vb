' FormLanguagePicker.vb — First-run language selection
' Shows logo + search box with filtered language list.
' After selection, downloads/generates the language pack if needed.

Imports System.Drawing
Imports System.Windows.Forms
Imports EveryTongue.Services.Infrastructure

Public Class FormLanguagePicker
    Inherits Form

    ''' <summary>The selected language code (e.g. "en", "es", "fr"). Empty if cancelled.</summary>
    Public Property SelectedLanguage As String = ""

    Private ReadOnly _langs As List(Of (Code As String, Native As String, Name As String))
    Private _txtSearch As TextBox
    Private _lstLangs As ListBox
    Private WithEvents _btnOk As Button

    Public Sub New()
        ' Get all languages from LanguageCodeService
        _langs = New List(Of (String, String, String))()
        _langs.Add(("en", "English", "English"))
        Dim langSvc = LanguageCodeService.Instance
        Dim langPack = LanguagePackService.Instance
        Dim installed = langPack.GetAvailableLanguages()
        Dim all = langSvc.GetAllLanguagesForWeb()

        ' Installed languages first (excluding English which is already added)
        For Each l In all
            If l.Iso1.Equals("en", StringComparison.OrdinalIgnoreCase) Then Continue For
            If Not installed.Any(Function(c) c.Equals(l.Iso1, StringComparison.OrdinalIgnoreCase)) Then Continue For
            _langs.Add((l.Iso1, If(Not String.IsNullOrEmpty(l.Native), l.Native, l.Name), l.Name))
        Next

        ' Then the rest
        For Each l In all
            If l.Iso1.Equals("en", StringComparison.OrdinalIgnoreCase) Then Continue For
            If installed.Any(Function(c) c.Equals(l.Iso1, StringComparison.OrdinalIgnoreCase)) Then Continue For
            _langs.Add((l.Iso1, If(Not String.IsNullOrEmpty(l.Native), l.Native, l.Name), l.Name))
        Next

        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        Me.Text = "Every Tongue"
        Me.ClientSize = New Size(600, 520)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = Color.Black
        Me.ForeColor = Color.White
        Me.Font = New Font("Segoe UI", 10)

        ' Logo (embedded black logo)
        Dim picLogo As New PictureBox() With {
            .Size = New Size(560, 220),
            .Location = New Point((600 - 560) \ 2, 10),
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Transparent
        }
        Try
            Dim asm = Reflection.Assembly.GetExecutingAssembly()
            Dim stream = asm.GetManifestResourceStream("EveryTongue.Assets.Logo.png")
            If stream IsNot Nothing Then
                picLogo.Image = Image.FromStream(stream)
            End If
        Catch ex As Exception
            FormMain.WriteDebugLog($"[ERROR] FormLanguagePicker.New (load logo): {ex.Message}")
        End Try
        Me.Controls.Add(picLogo)

        ' Search text box
        _txtSearch = New TextBox() With {
            .Size = New Size(400, 32),
            .Location = New Point((600 - 400) \ 2, 240),
            .Font = New Font("Segoe UI", 12),
            .BackColor = Color.FromArgb(50, 50, 55),
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle
        }
        _txtSearch.PlaceholderText = "Search languages..."
        AddHandler _txtSearch.TextChanged, AddressOf SearchChanged
        AddHandler _txtSearch.KeyDown, AddressOf SearchKeyDown
        Me.Controls.Add(_txtSearch)

        ' Language list box
        _lstLangs = New ListBox() With {
            .Size = New Size(400, 150),
            .Location = New Point((600 - 400) \ 2, 278),
            .Font = New Font("Segoe UI", 12),
            .BackColor = Color.FromArgb(50, 50, 55),
            .ForeColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .IntegralHeight = False
        }
        AddHandler _lstLangs.DoubleClick, AddressOf ListDoubleClick
        Me.Controls.Add(_lstLangs)

        ' Populate list
        PopulateList("")

        ' OK button
        _btnOk = New Button() With {
            .Size = New Size(300, 45),
            .Location = New Point((600 - 300) \ 2, 438),
            .FlatStyle = FlatStyle.Flat,
            .BackColor = Color.FromArgb(0, 122, 204),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 14, FontStyle.Bold),
            .Text = "OK",
            .Cursor = Cursors.Hand
        }
        _btnOk.FlatAppearance.BorderSize = 0
        Me.Controls.Add(_btnOk)
        Me.AcceptButton = _btnOk

        Me.ResumeLayout(False)
    End Sub

    Private Sub PopulateList(filter As String)
        _lstLangs.BeginUpdate()
        _lstLangs.Items.Clear()
        Dim q = filter.Trim().ToLowerInvariant()
        For Each lang In _langs
            If q.Length > 0 Then
                If lang.Native.ToLowerInvariant().IndexOf(q) < 0 AndAlso
                   lang.Name.ToLowerInvariant().IndexOf(q) < 0 AndAlso
                   lang.Code.ToLowerInvariant().IndexOf(q) < 0 Then
                    Continue For
                End If
            End If
            _lstLangs.Items.Add(New LangItem With {.Code = lang.Code, .Display = $"{lang.Native}  ({lang.Name})"})
        Next
        If _lstLangs.Items.Count > 0 Then _lstLangs.SelectedIndex = 0
        _lstLangs.EndUpdate()
    End Sub

    Private Sub SearchChanged(sender As Object, e As EventArgs)
        PopulateList(_txtSearch.Text)
    End Sub

    Private Sub SearchKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Down Then
            If _lstLangs.Items.Count > 0 Then
                Dim idx = _lstLangs.SelectedIndex
                If idx < _lstLangs.Items.Count - 1 Then
                    _lstLangs.SelectedIndex = idx + 1
                End If
            End If
            e.Handled = True
        ElseIf e.KeyCode = Keys.Up Then
            If _lstLangs.Items.Count > 0 Then
                Dim idx = _lstLangs.SelectedIndex
                If idx > 0 Then
                    _lstLangs.SelectedIndex = idx - 1
                End If
            End If
            e.Handled = True
        ElseIf e.KeyCode = Keys.Enter Then
            If _lstLangs.SelectedItem IsNot Nothing Then
                SelectedLanguage = DirectCast(_lstLangs.SelectedItem, LangItem).Code
                Me.DialogResult = DialogResult.OK
                Me.Close()
            End If
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub ListDoubleClick(sender As Object, e As EventArgs)
        If _lstLangs.SelectedItem IsNot Nothing Then
            SelectedLanguage = DirectCast(_lstLangs.SelectedItem, LangItem).Code
            Me.DialogResult = DialogResult.OK
            Me.Close()
        End If
    End Sub

    Private Sub BtnOk_Click(sender As Object, e As EventArgs) Handles _btnOk.Click
        If _lstLangs.SelectedItem IsNot Nothing Then
            SelectedLanguage = DirectCast(_lstLangs.SelectedItem, LangItem).Code
        End If
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Class LangItem
        Public Property Code As String
        Public Property Display As String
        Public Overrides Function ToString() As String
            Return Display
        End Function
    End Class
End Class
