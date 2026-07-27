Imports System.IO
Imports EveryTongue.Models
Imports EveryTongue.Services.Infrastructure

''' <summary>
''' Session people-names editor (the STT engines' "service" vocab layer):
''' speaker names and sermon-notes nouns that live for the whole session,
''' surviving scripture-book and language changes. One name per line;
''' optional pronunciation alternates after "=" (Speechmatics sounds_like),
''' e.g. "Eareckson = Erikson, Erickson". Names can be imported from a notes
''' file (pdf/docx/odt/pages/rtf/txt) — extraction is document-derived, never
''' a static per-language list.
''' </summary>
Public Class FormServiceNames

    Private ReadOnly _config As AppConfig
    ''' <summary>Called after OK saved the list — the owner re-pushes to running backends.</summary>
    Private ReadOnly _onSaved As Action

    Public Sub New(config As AppConfig, Optional onSaved As Action = Nothing)
        InitializeComponent()
        _config = config
        _onSaved = onSaved
        ApplyLocale()
        txtNames.Text = String.Join(Environment.NewLine,
            If(_config.ServiceNames, New List(Of ServiceNameEntry)).
                Where(Function(e) Not String.IsNullOrWhiteSpace(e?.Content)).
                Select(Function(e) If(e.SoundsLike IsNot Nothing AndAlso e.SoundsLike.Count > 0,
                                      $"{e.Content} = {String.Join(", ", e.SoundsLike)}",
                                      e.Content)))
    End Sub

    Private Shared Function S(key As String) As String
        Return LanguagePackService.Instance.GetString(key)
    End Function

    Private Sub ApplyLocale()
        Me.Text = S("SvcNames_Title")
        lblInfo.Text = S("SvcNames_Info")
        btnImport.Text = S("SvcNames_Import")
        btnOK.Text = S("Opt_OK")
        btnCancel.Text = S("Btn_Cancel")
        lblStatus.Text = ""
    End Sub

    ''' <summary>Parse the textbox back into entries: "Name" or "Name = alt1, alt2".</summary>
    Private Function ParseEntries() As List(Of ServiceNameEntry)
        Dim entries As New List(Of ServiceNameEntry)
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each rawLine In txtNames.Lines
            Dim line = rawLine.Trim()
            If line.Length = 0 Then Continue For
            Dim content = line
            Dim alts As New List(Of String)
            Dim eq = line.IndexOf("="c)
            If eq > 0 Then
                content = line.Substring(0, eq).Trim()
                alts = line.Substring(eq + 1).Split(","c).
                    Select(Function(a) a.Trim()).
                    Where(Function(a) a.Length > 0).ToList()
            End If
            If content.Length = 0 OrElse Not seen.Add(content) Then Continue For
            entries.Add(New ServiceNameEntry With {.Content = content, .SoundsLike = alts})
        Next
        Return entries
    End Function

    Private Sub btnOK_Click(sender As Object, e As EventArgs) Handles btnOK.Click
        _config.ServiceNames = ParseEntries()
        ConfigManager.Save(_config)
        AppLogger.Log(LogEvents.STT_SERVICE_VOCAB, $"Service names saved: {_config.ServiceNames.Count} entries")
        _onSaved?.Invoke()
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Async Sub btnImport_Click(sender As Object, e As EventArgs) Handles btnImport.Click
        Using dlg As New OpenFileDialog()
            dlg.Filter = $"{S("SvcNames_ImportFilter")}|*.pdf;*.docx;*.odt;*.pages;*.rtf;*.txt;*.md;*.json;*.xml|{S("SvcNames_ImportAllFiles")}|*.*"
            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return
            btnImport.Enabled = False
            lblStatus.Text = S("SvcNames_Importing")
            Try
                Dim mgr As New DependencyManager(_config, AppDomain.CurrentDomain.BaseDirectory)
                Dim found = Await mgr.ExtractNotesNamesAsync(dlg.FileName)
                Dim existing As New HashSet(Of String)(
                    ParseEntries().Select(Function(en) en.Content), StringComparer.OrdinalIgnoreCase)
                Dim added = found.Where(Function(n) Not existing.Contains(n)).ToList()
                If added.Count = 0 Then
                    lblStatus.Text = S("SvcNames_ImportNone")
                Else
                    Dim text = txtNames.Text.TrimEnd()
                    txtNames.Text = If(text.Length = 0, "", text & Environment.NewLine) &
                                    String.Join(Environment.NewLine, added)
                    lblStatus.Text = String.Format(S("SvcNames_ImportAdded"), added.Count)
                End If
            Catch ex As FileNotFoundException
                AppLogger.PromptDownloadManager(S("SvcNames_NeedPython"), S("SvcNames_Title"))
                lblStatus.Text = ""
            Catch ex As Exception
                AppLogger.Log(LogEvents.STT_SERVICE_VOCAB, $"Notes import failed: {ex.Message}")
                lblStatus.Text = String.Format(S("SvcNames_ImportFailed"), ex.Message)
            Finally
                btnImport.Enabled = True
            End Try
        End Using
    End Sub

End Class
