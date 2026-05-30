' FormLanguagePicker.vb — First-run language selection
' Shows logo + combo box of ALL languages from language-codes.json.
' After selection, downloads/generates the language pack if needed.

Imports System.Drawing
Imports System.Windows.Forms
Imports EveryTongue.Services.Infrastructure

Public Class FormLanguagePicker
    Inherits Form

    ''' <summary>The selected language code (e.g. "en", "es", "fr"). Empty if cancelled.</summary>
    Public Property SelectedLanguage As String = ""

    Private ReadOnly _langs As List(Of (Code As String, Native As String, Name As String))
    Private WithEvents _cboLang As ComboBox
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
            .Size = New Size(560, 260),
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

        ' Language combo box
        _cboLang = New ComboBox() With {
            .Size = New Size(400, 36),
            .Location = New Point((600 - 400) \ 2, 290),
            .DropDownStyle = ComboBoxStyle.DropDown,
            .AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            .AutoCompleteSource = AutoCompleteSource.ListItems,
            .Font = New Font("Segoe UI", 14),
            .BackColor = Color.FromArgb(50, 50, 55),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .MaxDropDownItems = 8,
            .DropDownHeight = 200
        }
        For Each lang In _langs
            _cboLang.Items.Add($"{lang.Native}  ({lang.Name})")
        Next
        _cboLang.SelectedIndex = 0 ' English is first
        Me.Controls.Add(_cboLang)

        ' OK button
        _btnOk = New Button() With {
            .Size = New Size(300, 50),
            .Location = New Point((600 - 300) \ 2, 345),
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

    Private Sub BtnOk_Click(sender As Object, e As EventArgs) Handles _btnOk.Click
        If _cboLang.SelectedIndex >= 0 AndAlso _cboLang.SelectedIndex < _langs.Count Then
            SelectedLanguage = _langs(_cboLang.SelectedIndex).Code
        Else
            ' Match typed text against items
            Dim typed = _cboLang.Text.Trim()
            For i = 0 To _langs.Count - 1
                If _cboLang.Items(i).ToString().Equals(typed, StringComparison.OrdinalIgnoreCase) Then
                    SelectedLanguage = _langs(i).Code
                    Exit For
                End If
            Next
        End If
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
End Class
