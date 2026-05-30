Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Controllers
    ''' <summary>
    ''' Manages the Bible workspace tab — language/translation selection,
    ''' book/chapter navigation, verse display, and reference search.
    ''' Extracted from FormMain to separate UI logic from form lifecycle.
    ''' </summary>
    Friend Class BibleController

        ' UI controls (owned by FormMain, passed in)
        Private ReadOnly _cboBibleLang As ComboBox
        Private ReadOnly _cboBibleTrans As ComboBox
        Private ReadOnly _txtBibleRef As ComboBox
        Private ReadOnly _btnBibleGo As Button
        Private ReadOnly _btnBibleBack As Button
        Private ReadOnly _lblBibleNavTitle As Label
        Private ReadOnly _flpBibleNav As FlowLayoutPanel
        Private ReadOnly _rtbBibleText As RichTextBox
        Private ReadOnly _lblCopyright As Label

        ' Service accessor (deferred because server may not be started yet)
        Private ReadOnly _getBibleService As Func(Of IBibleService)
        Private ReadOnly _getDefaultLang As Func(Of String)
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _config As Models.AppConfig

        ' State
        Private _bibleBooks As IReadOnlyList(Of BibleBook)
        Private _bibleViewBookNumber As Integer = -1
        Private _bibleViewChapter As Integer = -1
        Private _bibleViewVerseStart As Integer = 0
        Private _bibleViewVerseEnd As Integer = 0

        Public Sub New(cboBibleLang As ComboBox,
                       cboBibleTrans As ComboBox,
                       txtBibleRef As ComboBox,
                       btnBibleGo As Button,
                       btnBibleBack As Button,
                       lblBibleNavTitle As Label,
                       flpBibleNav As FlowLayoutPanel,
                       rtbBibleText As RichTextBox,
                       lblCopyright As Label,
                       getBibleService As Func(Of IBibleService),
                       getDefaultLang As Func(Of String),
                       log As Action(Of String),
                       config As Models.AppConfig)
            _cboBibleLang = cboBibleLang
            _cboBibleTrans = cboBibleTrans
            _txtBibleRef = txtBibleRef
            _btnBibleGo = btnBibleGo
            _btnBibleBack = btnBibleBack
            _lblBibleNavTitle = lblBibleNavTitle
            _flpBibleNav = flpBibleNav
            _rtbBibleText = rtbBibleText
            _lblCopyright = lblCopyright
            _getBibleService = getBibleService
            _getDefaultLang = getDefaultLang
            _log = log
            _config = config
        End Sub

        Private Sub ClearBibleText()
            _rtbBibleText.Clear()
            _lblCopyright.Text = ""
            _lblCopyright.Visible = False
        End Sub

        ''' <summary>Wire up event handlers. Call once during form init.</summary>
        Public Sub WireEvents()
            AddHandler _cboBibleLang.SelectedIndexChanged, Sub(s, e) LoadBibleTranslations()
            AddHandler _cboBibleTrans.SelectedIndexChanged, Sub(s, e) OnTranslationChanged()
            AddHandler _btnBibleBack.Click, Sub(s, e) ShowBookButtons()
            AddHandler _btnBibleGo.Click, Sub(s, e) GoToReference()
            AddHandler _txtBibleRef.KeyDown, Sub(s, e)
                                                  If e.KeyCode = Keys.Enter Then
                                                      GoToReference()
                                                      e.SuppressKeyPress = True
                                                  End If
                                              End Sub
        End Sub

        ''' <summary>Wire context menu handlers for the Bible RichTextBox.</summary>
        Public Sub WireContextMenu(ctxCopySelection As ToolStripMenuItem,
                                   ctxCopyVerse As ToolStripMenuItem,
                                   ctxCopyChapter As ToolStripMenuItem)
            AddHandler _rtbBibleText.ContextMenuStrip.Opening, Sub(s, e)
                                                                    ctxCopySelection.Enabled = _rtbBibleText.SelectionLength > 0
                                                                    ctxCopyVerse.Enabled = _bibleViewChapter > 0
                                                                    ctxCopyChapter.Enabled = _bibleViewChapter > 0
                                                                End Sub

            AddHandler ctxCopySelection.Click, Sub(s, e)
                                                    If _rtbBibleText.SelectionLength > 0 Then
                                                        Clipboard.SetText(_rtbBibleText.SelectedText)
                                                    End If
                                                End Sub

            AddHandler ctxCopyVerse.Click, Sub(s, e) CopyVerseWithReference()
            AddHandler ctxCopyChapter.Click, Sub(s, e) CopyChapter()
        End Sub

        Private Sub CopyVerseWithReference()
            Try
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing OrElse _bibleViewChapter < 1 Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim bookName = GetCurrentBookName()
                If String.IsNullOrEmpty(bookName) Then Return

                Dim chapter = bibleSvc.GetChapterAsync(trans.Id, GetCurrentBookShortName(),
                    _bibleViewChapter, CancellationToken.None).GetAwaiter().GetResult()
                If chapter Is Nothing OrElse chapter.Verses Is Nothing Then Return

                Dim vStart = If(_bibleViewVerseStart > 0, _bibleViewVerseStart, 1)
                Dim vEnd = If(_bibleViewVerseEnd > 0, _bibleViewVerseEnd, vStart)

                Dim sb As New System.Text.StringBuilder()
                Dim refText = $"{bookName} {_bibleViewChapter}:{vStart}"
                If vEnd > vStart Then refText &= $"-{vEnd}"

                sb.AppendLine(refText)
                For Each v In chapter.Verses
                    If v.Verse >= vStart AndAlso v.Verse <= vEnd Then
                        sb.AppendLine($"{v.Verse} {v.Text}")
                    End If
                Next

                Clipboard.SetText(sb.ToString().TrimEnd())
            Catch ex As Exception
                _log($"[ERROR] BibleController.CopyVerse: {ex.Message}")
            End Try
        End Sub

        Private Sub CopyChapter()
            Try
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing OrElse _bibleViewChapter < 1 Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim bookName = GetCurrentBookName()
                If String.IsNullOrEmpty(bookName) Then Return

                Dim chapter = bibleSvc.GetChapterAsync(trans.Id, GetCurrentBookShortName(),
                    _bibleViewChapter, CancellationToken.None).GetAwaiter().GetResult()
                If chapter Is Nothing OrElse chapter.Verses Is Nothing Then Return

                Dim sb As New System.Text.StringBuilder()
                sb.AppendLine($"{bookName} {_bibleViewChapter}")
                sb.AppendLine()
                For Each v In chapter.Verses
                    sb.AppendLine($"{v.Verse} {v.Text}")
                Next

                Clipboard.SetText(sb.ToString().TrimEnd())
            Catch ex As Exception
                _log($"[ERROR] BibleController.CopyChapter: {ex.Message}")
            End Try
        End Sub

        Private Function GetCurrentBookName() As String
            If _bibleBooks Is Nothing Then Return Nothing
            Dim book = _bibleBooks.FirstOrDefault(Function(b) b.Number = _bibleViewBookNumber)
            Return book?.LongName
        End Function

        Private Function GetCurrentBookShortName() As String
            If _bibleBooks Is Nothing Then Return Nothing
            Dim book = _bibleBooks.FirstOrDefault(Function(b) b.Number = _bibleViewBookNumber)
            Return book?.ShortName
        End Function

        ''' <summary>
        ''' Populate the Bible tab language combo with available languages.
        ''' Call after the server starts.
        ''' </summary>
        Public Sub Initialize()
            Try
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim allTrans = bibleSvc.GetTranslationsAsync("", CancellationToken.None).GetAwaiter().GetResult()
                Dim langs = allTrans.Select(Function(t) t.Language).Distinct().OrderBy(Function(l) l).ToList()

                _cboBibleLang.Items.Clear()
                For Each lang In langs
                    _cboBibleLang.Items.Add(lang)
                Next

                Dim defaultLang = _getDefaultLang()
                If String.IsNullOrEmpty(defaultLang) OrElse defaultLang = "auto" Then defaultLang = "eng"
                Dim idx = _cboBibleLang.Items.IndexOf(defaultLang)
                If idx >= 0 Then
                    _cboBibleLang.SelectedIndex = idx
                ElseIf _cboBibleLang.Items.Count > 0 Then
                    _cboBibleLang.SelectedIndex = 0
                End If
            Catch ex As Exception
                _log($"[ERROR] BibleController.Initialize: {ex.Message}")
            End Try
        End Sub

        ''' <summary>Refresh after downloading new Bibles.</summary>
        Public Sub Refresh()
            Dim currentLang = _cboBibleLang.SelectedItem?.ToString()
            Initialize()
            If currentLang IsNot Nothing Then
                Dim idx = _cboBibleLang.Items.IndexOf(currentLang)
                If idx >= 0 Then _cboBibleLang.SelectedIndex = idx
            End If
        End Sub

        Private Sub LoadBibleTranslations()
            _cboBibleTrans.Items.Clear()
            _flpBibleNav.Controls.Clear()
            ClearBibleText()
            If _cboBibleLang.SelectedItem Is Nothing Then Return
            Try
                Dim lang = _cboBibleLang.SelectedItem.ToString()
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim trans = bibleSvc.GetTranslationsAsync(lang, CancellationToken.None).GetAwaiter().GetResult()
                For Each t In trans
                    _cboBibleTrans.Items.Add(t)
                Next
                _cboBibleTrans.DisplayMember = "Name"
                If _cboBibleTrans.Items.Count > 0 Then _cboBibleTrans.SelectedIndex = 0
            Catch ex As Exception
                _log($"[ERROR] BibleController.LoadTranslations: {ex.Message}")
            End Try
        End Sub

        Private Sub OnTranslationChanged()
            Dim savedBookNum = _bibleViewBookNumber
            Dim savedChapter = _bibleViewChapter
            Dim savedVStart = _bibleViewVerseStart
            Dim savedVEnd = _bibleViewVerseEnd

            ShowBookButtons()

            ' Restore position in the new translation
            If savedBookNum > 0 AndAlso savedChapter > 0 AndAlso _bibleBooks IsNot Nothing Then
                Dim matchedBook = _bibleBooks.FirstOrDefault(Function(b) b.Number = savedBookNum)
                If matchedBook IsNot Nothing Then
                    If savedChapter <= matchedBook.Chapters Then
                        ShowChapterButtons(matchedBook)
                        LoadChapterAt(matchedBook, savedChapter, savedVStart, savedVEnd)
                    Else
                        ShowChapterButtons(matchedBook)
                    End If
                End If
            End If
        End Sub

        Private Sub ShowBookButtons()
            _flpBibleNav.SuspendLayout()
            _flpBibleNav.Controls.Clear()
            ClearBibleText()
            _btnBibleBack.Visible = False
            _lblBibleNavTitle.Text = Services.Infrastructure.LanguagePackService.Instance.GetString("Bible_Books")
            If _cboBibleTrans.SelectedItem Is Nothing Then
                _flpBibleNav.ResumeLayout()
                Return
            End If
            Try
                Dim trans = DirectCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then
                    _flpBibleNav.ResumeLayout()
                    Return
                End If

                _bibleBooks = bibleSvc.GetBooksAsync(trans.Id, CancellationToken.None).GetAwaiter().GetResult()
                For Each book In _bibleBooks
                    Dim bk = book
                    Dim btn As New Button() With {
                        .Text = bk.ShortName,
                        .Size = New Size(72, 32),
                        .FlatStyle = FlatStyle.Flat,
                        .Font = New Font("Segoe UI", 8.5F),
                        .Margin = New Padding(2),
                        .Tag = bk
                    }
                    btn.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180)
                    AddHandler btn.Click, Sub(s, e) ShowChapterButtons(bk)
                    _flpBibleNav.Controls.Add(btn)
                Next
            Catch ex As Exception
                _log($"[ERROR] BibleController.ShowBookButtons: {ex.Message}")
            End Try
            _flpBibleNav.ResumeLayout()
        End Sub

        Private Sub ShowChapterButtons(book As BibleBook)
            _flpBibleNav.SuspendLayout()
            _flpBibleNav.Controls.Clear()
            _btnBibleBack.Visible = True
            _lblBibleNavTitle.Text = book.ShortName

            For ch = 1 To book.Chapters
                Dim chNum = ch
                Dim btn As New Button() With {
                    .Text = ch.ToString(),
                    .Size = New Size(42, 32),
                    .FlatStyle = FlatStyle.Flat,
                    .Font = New Font("Segoe UI", 9F),
                    .Margin = New Padding(2),
                    .Tag = chNum
                }
                btn.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180)
                AddHandler btn.Click, Sub(s, e) LoadChapterAt(book, chNum)
                _flpBibleNav.Controls.Add(btn)
            Next
            _flpBibleNav.ResumeLayout()
        End Sub

        Private Sub LoadChapterAt(book As BibleBook, chapterNum As Integer,
                                  Optional highlightStart As Integer = 0, Optional highlightEnd As Integer = 0)
            Try
                Dim trans = DirectCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim chapter = bibleSvc.GetChapterAsync(trans.Id, book.ShortName, chapterNum, CancellationToken.None).GetAwaiter().GetResult()
                If chapter Is Nothing OrElse chapter.Verses Is Nothing Then Return

                _bibleViewBookNumber = book.Number
                _bibleViewChapter = chapterNum
                _bibleViewVerseStart = highlightStart
                _bibleViewVerseEnd = highlightEnd

                DisplayVerses(book.LongName, chapterNum, chapter.Verses, highlightStart, highlightEnd)
            Catch ex As Exception
                _log($"[ERROR] BibleController.LoadChapter: {ex.Message}")
            End Try
        End Sub

        Private Sub DisplayVerses(title As String, chapterNum As Integer, verses As List(Of BibleVerse),
                                  Optional highlightStart As Integer = 0, Optional highlightEnd As Integer = 0)
            ClearBibleText()
            _rtbBibleText.SelectionFont = New Font(_rtbBibleText.Font.FontFamily, 16, FontStyle.Bold)
            _rtbBibleText.AppendText($"{title} {chapterNum}")
            _rtbBibleText.AppendText(vbCrLf & vbCrLf)

            For Each v In verses
                Dim isHighlighted = (highlightStart > 0 AndAlso
                                     v.Verse >= highlightStart AndAlso
                                     v.Verse <= If(highlightEnd > 0, highlightEnd, highlightStart))
                _rtbBibleText.SelectionFont = New Font(_rtbBibleText.Font.FontFamily, 8, FontStyle.Bold)
                _rtbBibleText.SelectionColor = Color.Gray
                _rtbBibleText.AppendText($"{v.Verse} ")
                _rtbBibleText.SelectionFont = If(isHighlighted,
                    New Font(_rtbBibleText.Font.FontFamily, 12, FontStyle.Bold),
                    _rtbBibleText.Font)
                _rtbBibleText.SelectionColor = If(isHighlighted, Color.DarkBlue, _rtbBibleText.ForeColor)
                _rtbBibleText.AppendText($"{v.Text}{vbCrLf}")
            Next

            _rtbBibleText.SelectionStart = 0
            _rtbBibleText.ScrollToCaret()

            ' Copyright in separate label (not part of selectable text)
            Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
            If _config.ShowBibleCopyright AndAlso trans IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(trans.Copyright) Then
                _lblCopyright.Text = trans.Copyright
                _lblCopyright.Visible = True
            Else
                _lblCopyright.Text = ""
                _lblCopyright.Visible = False
            End If
        End Sub

        Private Sub GoToReference()
            Dim refText = _txtBibleRef.Text.Trim()
            If String.IsNullOrEmpty(refText) Then Return
            Try
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim parsed = bibleSvc.ParseReferenceAsync(refText, "en", trans.Id, CancellationToken.None).GetAwaiter().GetResult()
                If parsed Is Nothing OrElse Not parsed.IsValid Then
                    ClearBibleText()
                    _rtbBibleText.AppendText(String.Format(Services.Infrastructure.LanguagePackService.Instance.GetString("Bible_NotFound"), refText))
                    Return
                End If

                Dim chapter = bibleSvc.GetChapterAsync(trans.Id, parsed.Book, parsed.Chapter, CancellationToken.None).GetAwaiter().GetResult()
                If chapter Is Nothing OrElse chapter.Verses Is Nothing OrElse chapter.Verses.Count = 0 Then
                    ClearBibleText()
                    _rtbBibleText.AppendText(String.Format(Services.Infrastructure.LanguagePackService.Instance.GetString("Bible_NoVerses"), refText))
                    Return
                End If

                Dim bookTitle = parsed.Book
                Dim matchedBook As BibleBook = Nothing
                If _bibleBooks IsNot Nothing AndAlso parsed.BookNumber > 0 Then
                    matchedBook = _bibleBooks.FirstOrDefault(Function(b) b.Number = parsed.BookNumber)
                End If
                If matchedBook IsNot Nothing Then bookTitle = matchedBook.LongName

                _bibleViewBookNumber = If(matchedBook IsNot Nothing, matchedBook.Number, parsed.BookNumber)
                _bibleViewChapter = parsed.Chapter
                _bibleViewVerseStart = parsed.VerseStart
                _bibleViewVerseEnd = parsed.VerseEnd

                If Not _txtBibleRef.Items.Contains(refText) Then
                    _txtBibleRef.Items.Insert(0, refText)
                    If _txtBibleRef.Items.Count > 20 Then _txtBibleRef.Items.RemoveAt(_txtBibleRef.Items.Count - 1)
                End If

                DisplayVerses(bookTitle, parsed.Chapter, chapter.Verses, parsed.VerseStart, parsed.VerseEnd)
            Catch ex As Exception
                _log($"[ERROR] BibleController.GoToReference: {ex.Message}")
            End Try
        End Sub

    End Class
End Namespace
