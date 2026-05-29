' FormLanguagePicker.vb — First-run language selection
' Shows logo + combo box of available languages in native tongue.
' Locale list passed in from FormMain (single source of truth).

Imports System.Drawing
Imports System.Windows.Forms

Public Class FormLanguagePicker
    Inherits Form

    ''' <summary>The selected language code (e.g. "en", "es", "fr"). Empty if cancelled.</summary>
    Public Property SelectedLanguage As String = ""

    Private ReadOnly _locales As (Code As String, Name As String)()
    Private WithEvents _cboLang As ComboBox
    Private WithEvents _btnOk As Button

    Public Sub New(locales As (Code As String, Name As String)())
        _locales = locales
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        Me.Text = "Every Tongue"
        Me.ClientSize = New Size(600, 480)
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
            .Size = New Size(300, 36),
            .Location = New Point((600 - 300) \ 2, 290),
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .Font = New Font("Segoe UI", 14),
            .BackColor = Color.FromArgb(50, 50, 55),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        For Each locale In _locales
            _cboLang.Items.Add(locale.Name)
        Next
        ' Default to English if present, otherwise first item
        Dim engIdx = Array.FindIndex(_locales, Function(l) l.Code = "en")
        _cboLang.SelectedIndex = If(engIdx >= 0, engIdx, 0)
        Me.Controls.Add(_cboLang)

        ' OK button
        _btnOk = New Button() With {
            .Size = New Size(300, 50),
            .Location = New Point((600 - 300) \ 2, 365),
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
        If _cboLang.SelectedIndex >= 0 AndAlso _cboLang.SelectedIndex < _locales.Length Then
            SelectedLanguage = _locales(_cboLang.SelectedIndex).Code
        End If
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
End Class
