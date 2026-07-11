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
        Private ReadOnly _cboTranslateTo As ComboBox
        Private ReadOnly _btnSpeak As Button

        ' Service accessor (deferred because server may not be started yet)
        Private ReadOnly _getBibleService As Func(Of IBibleService)
        Private ReadOnly _getDefaultLang As Func(Of String)
        Private ReadOnly _log As Action(Of String)
        Private ReadOnly _config As Models.AppConfig
        Private ReadOnly _getTranslationOrchestrator As Func(Of ITranslationService)
        Private ReadOnly _sttLanguages As String()
        Private ReadOnly _langDisplayName As Func(Of String, String)
        Private ReadOnly _langCodeFromDisplay As Func(Of String, String)
        Private ReadOnly _getString As Func(Of String, String)
        Private ReadOnly _ttsPlayer As Services.Audio.DesktopTtsPlayer

        ' State
        Private _bibleBooks As IReadOnlyList(Of BibleBook)
        Private _bibleViewBookNumber As Integer = -1
        Private _bibleViewChapter As Integer = -1
        Private _bibleViewVerseStart As Integer = 0
        Private _bibleViewVerseEnd As Integer = 0

        ' Verse translation state (current chapter + target only)
        Private _lastTitle As String = ""
        Private _lastChapterNum As Integer = 0
        Private _lastVerses As List(Of BibleVerse)
        Private ReadOnly _txCache As New Dictionary(Of Integer, String)
        Private _txRunId As Integer = 0
        Private _isReading As Boolean = False

        ' Guards against stale async completions overwriting newer navigation
        ' (e.g. Refresh re-selects the language while a load is still in flight).
        Private _transLoadId As Integer = 0
        Private _booksLoadId As Integer = 0

        Public Sub New(cboBibleLang As ComboBox,
                       cboBibleTrans As ComboBox,
                       txtBibleRef As ComboBox,
                       btnBibleGo As Button,
                       btnBibleBack As Button,
                       lblBibleNavTitle As Label,
                       flpBibleNav As FlowLayoutPanel,
                       rtbBibleText As RichTextBox,
                       lblCopyright As Label,
                       cboTranslateTo As ComboBox,
                       btnSpeak As Button,
                       getBibleService As Func(Of IBibleService),
                       getDefaultLang As Func(Of String),
                       getTranslationOrchestrator As Func(Of ITranslationService),
                       getTtsService As Func(Of ITtsService),
                       getTtsCacheDir As Func(Of String),
                       sttLanguages As String(),
                       langDisplayName As Func(Of String, String),
                       langCodeFromDisplay As Func(Of String, String),
                       getString As Func(Of String, String),
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
            _cboTranslateTo = cboTranslateTo
            _btnSpeak = btnSpeak
            _getBibleService = getBibleService
            _getDefaultLang = getDefaultLang
            _getTranslationOrchestrator = getTranslationOrchestrator
            _sttLanguages = sttLanguages
            _langDisplayName = langDisplayName
            _langCodeFromDisplay = langCodeFromDisplay
            _getString = getString
            _log = log
            _config = config
            _ttsPlayer = New Services.Audio.DesktopTtsPlayer(getTtsService, getTtsCacheDir)
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
            AddHandler _cboTranslateTo.SelectedIndexChanged, Sub(s, e) OnTranslateTargetChanged()
            AddHandler _btnSpeak.Click, Sub(s, e) OnSpeakClicked()
        End Sub

        ' ─── Verse translation ────────────────────────────────────────

        ''' <summary>Populate the verse-translation target combo ("(no translation)" + FLORES-mapped languages).</summary>
        Public Sub PopulateTranslateTargets()
            _cboTranslateTo.Items.Clear()
            _cboTranslateTo.Items.Add(_getString("Bible_TranslateNone"))
            Dim floresMap = Pipeline.TranslationService.GetLangMap()
            For Each lang In _sttLanguages
                If lang = "auto" OrElse Not floresMap.ContainsKey(lang) Then Continue For
                _cboTranslateTo.Items.Add(_langDisplayName(lang))
            Next
            _cboTranslateTo.SelectedIndex = 0
        End Sub

        ''' <summary>FLORES code of the active verse-translation target, or "" when off.</summary>
        Private Function ActiveTargetFlores() As String
            If _cboTranslateTo.SelectedIndex <= 0 Then Return ""
            Dim whisper = _langCodeFromDisplay(_cboTranslateTo.Text)
            Return If(Pipeline.TranslationService.WhisperToFloresLang(whisper), "")
        End Function

        ''' <summary>FLORES code of the displayed Bible translation's language, or Nothing if unmapped.</summary>
        Private Function SourceFlores() As String
            Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
            Return Pipeline.TranslationService.Iso3ToFloresLang(trans?.Language)
        End Function

        Private Sub OnTranslateTargetChanged()
            _txRunId += 1
            _txCache.Clear()
            If _lastVerses IsNot Nothing AndAlso _lastVerses.Count > 0 Then
                DisplayVerses(_lastTitle, _lastChapterNum, _lastVerses, _bibleViewVerseStart, _bibleViewVerseEnd)
            End If
        End Sub

        ''' <summary>Translate the displayed chapter verse-by-verse, then re-render with translations.</summary>
        Private Async Sub TranslateDisplayedChapterAsync(verses As List(Of BibleVerse))
            Dim targetFlores = ActiveTargetFlores()
            If String.IsNullOrEmpty(targetFlores) Then Return   ' translation off
            Dim srcFlores = SourceFlores()
            If String.IsNullOrEmpty(srcFlores) Then
                ' Was a silent Return — the combo looked dead with no explanation.
                Dim biblLang = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)?.Language
                _log($"[Bible] verse translation skipped: Bible language '{biblLang}' has no FLORES mapping")
                Return
            End If
            If targetFlores.Equals(srcFlores, StringComparison.OrdinalIgnoreCase) Then Return   ' same language

            Dim orchestrator = _getTranslationOrchestrator()
            If orchestrator Is Nothing Then
                _lblBibleNavTitle.Text = _getString("Bible_TranslateServerRequired")
                Return
            End If

            _txRunId += 1
            Dim runId = _txRunId
            Dim savedTitle = _lblBibleNavTitle.Text
            _lblBibleNavTitle.Text = _getString("Bible_Translating")

            Dim results As New Dictionary(Of Integer, String)
            Try
                For Each v In verses
                    If runId <> _txRunId Then Return ' superseded by chapter/target change
                    Try
                        Dim tx = Await orchestrator.TranslateAsync(
                            v.Text, srcFlores, New List(Of String) From {targetFlores},
                            Threading.CancellationToken.None)
                        If tx IsNot Nothing AndAlso tx.ContainsKey(targetFlores) Then
                            results(v.Verse) = tx(targetFlores)
                        End If
                    Catch ex As Exception
                        _log($"[Bible] Verse {v.Verse} translation failed: {ex.Message}")
                    End Try
                Next
            Finally
                If runId = _txRunId Then _lblBibleNavTitle.Text = savedTitle
            End Try

            If runId <> _txRunId Then Return
            _txCache.Clear()
            For Each kvp In results : _txCache(kvp.Key) = kvp.Value : Next
            If _txCache.Count > 0 Then
                DisplayVerses(_lastTitle, _lastChapterNum, _lastVerses, _bibleViewVerseStart, _bibleViewVerseEnd)
            Else
                ' Every verse failed (typically: translation engine not running) —
                ' say so instead of quietly reverting the title.
                _lblBibleNavTitle.Text = _getString("Bible_TranslateServerRequired")
                _log($"[Bible] chapter translation produced 0/{verses.Count} verses — is the translation engine running?")
            End If
        End Sub

        ' ─── Read aloud (server TTS) ──────────────────────────────────

        Private Async Sub OnSpeakClicked()
            If _isReading Then
                _ttsPlayer.StopPlayback()
                _isReading = False
                _btnSpeak.Text = _getString("Bible_ReadAloud")
                Return
            End If

            If _lastVerses Is Nothing OrElse _lastVerses.Count = 0 Then Return
            If Not _ttsPlayer.IsAvailable Then
                _lblBibleNavTitle.Text = _getString("Bible_TranslateServerRequired")
                Return
            End If

            ' Read the translated chapter when translation is active, else the original.
            Dim targetFlores = ActiveTargetFlores()
            Dim texts As New List(Of String)
            Dim iso3 As String
            If Not String.IsNullOrEmpty(targetFlores) AndAlso _txCache.Count > 0 Then
                For Each v In _lastVerses
                    If _txCache.ContainsKey(v.Verse) Then texts.Add(_txCache(v.Verse))
                Next
                iso3 = Pipeline.TranslationService.FloresToIso3(targetFlores)
            Else
                For Each v In _lastVerses : texts.Add(v.Text) : Next
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                iso3 = If(trans?.Language, "eng")
            End If

            _isReading = True
            _btnSpeak.Text = _getString("Bible_ReadStop")
            Try
                ' Playback continues after queueing completes — the button stays
                ' "Stop" until the user presses it (no playback-finished event).
                Await _ttsPlayer.SpeakAsync(texts, iso3)
            Catch ex As Exception
                _log($"[Bible] Read aloud failed: {ex.Message}")
                _isReading = False
                _btnSpeak.Text = _getString("Bible_ReadAloud")
            End Try
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

        Private Async Sub CopyVerseWithReference()
            Try
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing OrElse _bibleViewChapter < 1 Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim bookName = GetCurrentBookName()
                If String.IsNullOrEmpty(bookName) Then Return

                Dim chapter = Await bibleSvc.GetChapterAsync(trans.Id, GetCurrentBookShortName(),
                    _bibleViewChapter, CancellationToken.None)
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

        Private Async Sub CopyChapter()
            Try
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing OrElse _bibleViewChapter < 1 Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim bookName = GetCurrentBookName()
                If String.IsNullOrEmpty(bookName) Then Return

                Dim chapter = Await bibleSvc.GetChapterAsync(trans.Id, GetCurrentBookShortName(),
                    _bibleViewChapter, CancellationToken.None)
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
        Public Async Sub Initialize()
            Try
                Await InitializeCoreAsync()
            Catch ex As Exception
                _log($"[ERROR] BibleController.Initialize: {ex.Message}")
            End Try
        End Sub

        Private Async Function InitializeCoreAsync() As Task
            Dim bibleSvc = _getBibleService()
            If bibleSvc Is Nothing Then Return

            Dim allTrans = Await bibleSvc.GetTranslationsAsync("", CancellationToken.None)
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

            If _cboTranslateTo.Items.Count = 0 Then PopulateTranslateTargets()
        End Function

        ''' <summary>Refresh after downloading new Bibles.</summary>
        Public Async Sub Refresh()
            Try
                Dim currentLang = _cboBibleLang.SelectedItem?.ToString()
                Await InitializeCoreAsync()
                If currentLang IsNot Nothing Then
                    Dim idx = _cboBibleLang.Items.IndexOf(currentLang)
                    If idx >= 0 Then _cboBibleLang.SelectedIndex = idx
                End If
            Catch ex As Exception
                _log($"[ERROR] BibleController.Refresh: {ex.Message}")
            End Try
        End Sub

        Private Async Sub LoadBibleTranslations()
            Try
                _transLoadId += 1
                Dim loadId = _transLoadId
                _cboBibleTrans.Items.Clear()
                _flpBibleNav.Controls.Clear()
                ClearBibleText()
                If _cboBibleLang.SelectedItem Is Nothing Then Return
                Dim lang = _cboBibleLang.SelectedItem.ToString()
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim trans = Await bibleSvc.GetTranslationsAsync(lang, CancellationToken.None)
                If loadId <> _transLoadId Then Return ' superseded by a newer language selection
                For Each t In trans
                    _cboBibleTrans.Items.Add(t)
                Next
                _cboBibleTrans.DisplayMember = "Name"
                If _cboBibleTrans.Items.Count > 0 Then _cboBibleTrans.SelectedIndex = 0
            Catch ex As Exception
                _log($"[ERROR] BibleController.LoadTranslations: {ex.Message}")
            End Try
        End Sub

        Private Async Sub OnTranslationChanged()
            Try
                Dim savedBookNum = _bibleViewBookNumber
                Dim savedChapter = _bibleViewChapter
                Dim savedVStart = _bibleViewVerseStart
                Dim savedVEnd = _bibleViewVerseEnd

                Await ShowBookButtonsCoreAsync()

                ' Restore position in the new translation
                If savedBookNum > 0 AndAlso savedChapter > 0 AndAlso _bibleBooks IsNot Nothing Then
                    Dim matchedBook = _bibleBooks.FirstOrDefault(Function(b) b.Number = savedBookNum)
                    If matchedBook IsNot Nothing Then
                        ShowChapterButtons(matchedBook)
                        If savedChapter <= matchedBook.Chapters Then
                            Await LoadChapterCoreAsync(matchedBook, savedChapter, savedVStart, savedVEnd)
                        End If
                    End If
                End If
            Catch ex As Exception
                _log($"[ERROR] BibleController.OnTranslationChanged: {ex.Message}")
            End Try
        End Sub

        Private Async Sub ShowBookButtons()
            Try
                Await ShowBookButtonsCoreAsync()
            Catch ex As Exception
                _log($"[ERROR] BibleController.ShowBookButtons: {ex.Message}")
            End Try
        End Sub

        Private Async Function ShowBookButtonsCoreAsync() As Task
            _booksLoadId += 1
            Dim loadId = _booksLoadId
            _flpBibleNav.SuspendLayout()
            Try
                _flpBibleNav.Controls.Clear()
                ClearBibleText()
                _btnBibleBack.Visible = False
                _lblBibleNavTitle.Text = Services.Infrastructure.LanguagePackService.Instance.GetString("Bible_Books")
                If _cboBibleTrans.SelectedItem Is Nothing Then Return

                Dim trans = DirectCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim books = Await bibleSvc.GetBooksAsync(trans.Id, CancellationToken.None)
                If loadId <> _booksLoadId Then Return ' superseded by newer navigation
                _bibleBooks = books
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
            Finally
                _flpBibleNav.ResumeLayout()
            End Try
        End Function

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

        Private Async Sub LoadChapterAt(book As BibleBook, chapterNum As Integer,
                                        Optional highlightStart As Integer = 0, Optional highlightEnd As Integer = 0)
            Try
                Await LoadChapterCoreAsync(book, chapterNum, highlightStart, highlightEnd)
            Catch ex As Exception
                _log($"[ERROR] BibleController.LoadChapter: {ex.Message}")
            End Try
        End Sub

        Private Async Function LoadChapterCoreAsync(book As BibleBook, chapterNum As Integer,
                                                    highlightStart As Integer, highlightEnd As Integer) As Task
            Dim trans = DirectCast(_cboBibleTrans.SelectedItem, BibleTranslation)
            Dim bibleSvc = _getBibleService()
            If bibleSvc Is Nothing Then Return

            Dim chapter = Await bibleSvc.GetChapterAsync(trans.Id, book.ShortName, chapterNum, CancellationToken.None)
            If chapter Is Nothing OrElse chapter.Verses Is Nothing Then Return

            _bibleViewBookNumber = book.Number
            _bibleViewChapter = chapterNum
            _bibleViewVerseStart = highlightStart
            _bibleViewVerseEnd = highlightEnd

            DisplayVerses(book.LongName, chapterNum, chapter.Verses, highlightStart, highlightEnd)
        End Function

        Private Sub DisplayVerses(title As String, chapterNum As Integer, verses As List(Of BibleVerse),
                                  Optional highlightStart As Integer = 0, Optional highlightEnd As Integer = 0)
            ' New chapter invalidates the verse-translation cache.
            If chapterNum <> _lastChapterNum OrElse Not ReferenceEquals(verses, _lastVerses) Then
                _txRunId += 1
                _txCache.Clear()
            End If
            _lastTitle = title
            _lastChapterNum = chapterNum
            _lastVerses = verses

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

                ' Translated line under the original when verse translation is active.
                If _txCache.ContainsKey(v.Verse) Then
                    _rtbBibleText.SelectionFont = New Font(_rtbBibleText.Font.FontFamily, _rtbBibleText.Font.Size, FontStyle.Italic)
                    _rtbBibleText.SelectionColor = Color.FromArgb(0, 102, 153)
                    _rtbBibleText.AppendText($"{_txCache(v.Verse)}{vbCrLf}")
                End If
            Next

            ' Kick off translation when a target is active and nothing is cached yet.
            If _txCache.Count = 0 AndAlso ActiveTargetFlores() <> "" Then
                TranslateDisplayedChapterAsync(verses)
            End If

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

        Private Async Sub GoToReference()
            Try
                Dim refText = _txtBibleRef.Text.Trim()
                If String.IsNullOrEmpty(refText) Then Return
                Dim trans = TryCast(_cboBibleTrans.SelectedItem, BibleTranslation)
                If trans Is Nothing Then Return
                Dim bibleSvc = _getBibleService()
                If bibleSvc Is Nothing Then Return

                Dim parsed = Await bibleSvc.ParseReferenceAsync(refText, "en", trans.Id, CancellationToken.None)
                If parsed Is Nothing OrElse Not parsed.IsValid Then
                    ClearBibleText()
                    _rtbBibleText.AppendText(String.Format(Services.Infrastructure.LanguagePackService.Instance.GetString("Bible_NotFound"), refText))
                    Return
                End If

                Dim chapter = Await bibleSvc.GetChapterAsync(trans.Id, parsed.Book, parsed.Chapter, CancellationToken.None)
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
