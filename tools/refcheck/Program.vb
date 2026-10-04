Imports System.IO
Imports System.Threading
Imports EveryTongue.Server
Imports EveryTongue.Services.Bible
Imports EveryTongue.Services.Models
Imports Microsoft.Extensions.Logging.Abstractions
Imports Microsoft.Extensions.Options

Module Program

    Private _failures As Integer = 0

    Sub Main()
        Dim biblesDir = "C:\Users\Jeremy\Desktop\Source\EveryTongue\EveryTongue\bin\Publish\Bibles"
        Dim svc As New BibleService(
            NullLogger(Of BibleService).Instance,
            Options.Create(New ServerOptions With {.BiblesDirectory = biblesDir}))

        ' The alias index builds on a background task — wait until "Salm 119"
        ' detects (the static English fallback can't resolve "Salm").
        Dim ready = False
        For i = 1 To 120
            If svc.DetectReferencesInText("Salm 119").Count > 0 Then
                ready = True
                Exit For
            End If
            Thread.Sleep(500)
        Next
        If Not ready Then
            Console.WriteLine("FATAL: alias index never became ready")
            Environment.Exit(2)
        End If
        Console.WriteLine("alias index ready")

        ' ── Replay the Jezer 2026-09-06 sequence ──
        Dim ctx As New RefContext()
        Dim opts As New RefDetectionOptions With {.LangHint = "ca", .Context = ctx, .UpdateContext = True}

        Check(svc, opts, "Estem al Salm 119, mireu el que diu.",
              "Ps", 119, 0, 0, "sermon chapter announcement")
        Check(svc, opts, "Salm 119, Verset 36.",
              "Ps", 119, 36, 36, "explicit verse inside the psalm")
        Check(svc, opts, "La primera, Timoteu 6:10 , perquè l'amor al diner és l'arrel de tots els mals.",
              "1Tim", 6, 10, 10, "cross-reference to Timothy")
        Check(svc, opts, "I a més a més, necessitem, com diu el verset 37.",
              "Ps", 119, 37, 37, "THE FIX: bare verse falls back past 1Tim 6 (21 verses) to Ps 119")
        Check(svc, opts, "Versets 38 al 40.",
              "Ps", 119, 38, 40, "THE FIX: spoken range + fallback")

        ' Direct impossible verse degrades to chapter-only.
        Check(svc, opts, "Timoteu 6:37 ho diu.",
              "1Tim", 6, 0, 0, "impossible direct verse degrades to chapter-only")

        ' Invalid range connector keeps the single verse.
        Check(svc, opts, "Salm 119, verset 40 durant 3 dies.",
              "Ps", 119, 40, 40, "unknown connector 'durant' must not create a range")

        ' Verse valid in the last-heard book: no fallback, stays put.
        Dim ctx2 As New RefContext()
        Dim opts2 As New RefDetectionOptions With {.LangHint = "ca", .Context = ctx2, .UpdateContext = True}
        Check(svc, opts2, "Estem al Salm 119.", "Ps", 119, 0, 0, "fresh context")
        Check(svc, opts2, "Timoteu 6:10 ens ho diu.", "1Tim", 6, 10, 10, "cross-ref")
        Check(svc, opts2, "Mireu el verset 12.", "1Tim", 6, 12, 12, "verse 12 exists in 1Tim 6 — stays with the last-heard book")

        ' Explicit book mention pins the owner (the preacher's own disambiguation).
        Check(svc, opts2, "El verset 15 del salm.", "Ps", 119, 15, 15, "'del salm' rescues back to the psalm")
        ' A verse that fits NO remembered book emits no link at all.
        Check(svc, opts2, "Mireu el verset 500.", "", 0, 0, 0, "verse fits no remembered book -> no link")
        ' Hyphen ranges in the bare pass are unchanged.
        Check(svc, opts2, "Els versets 5-9 ho diuen.", "Ps", 119, 5, 9, "hyphen range in bare pass")
        ' English re-detection over a translated feed uses the en.json range words.
        Dim optsEn As New RefDetectionOptions With {.LangHint = "en", .Context = ctx2, .UpdateContext = False}
        Check(svc, optsEn, "Look at verses 12 to 14.", "Ps", 119, 12, 14, "English 'to' range against the same room context")

        ' ── Replay the Jezer 2026-09-12 time-garbles ──
        ' Speechmatics heard ca "verset 35" as "7:35" ("set" = 7) three times.
        ' The garble-rescue pass that recovered these was REMOVED 2026-09-18:
        ' the Bible button now reopens at the room's remembered position, so a
        ' garbled verse costs one tap, not a rescue pass. Time-shaped tokens
        ' must produce NO link — clock or garble alike.
        Dim ctx3 As New RefContext()
        Dim opts3 As New RefDetectionOptions With {.LangHint = "ca", .Context = ctx3, .UpdateContext = True}
        Check(svc, opts3, "Estem al Salm 119, mireu el que diu.",
              "Ps", 119, 0, 0, "fresh context for the garble replay")
        Check(svc, opts3, "Guia'm per la senda per a 7:35 dels manaments, que jo l'estimo de debò.",
              "", 0, 0, 0, "garbled 'per a 7:35' is NOT rescued (removed 2026-09-18) -> no link")
        Check(svc, opts3, "Per a 7:38 al 40.",
              "", 0, 0, 0, "garbled range is NOT rescued -> no link")
        Check(svc, opts3, "El culte comença a les 7:30.",
              "", 0, 0, 0, "clock time -> no link")
        Check(svc, opts3, "Joan 3:16 ho diu.",
              "John", 3, 16, 16, "cross-ref to John still detects")
        Check(svc, opts3, "Mireu, 7:12.",
              "", 0, 0, 0, "time-shaped token -> no link")

        ' ── Field misses 2026-09-13 / 2026-09-27 (fixed 2026-10-05) ──
        ' Language-neutral only: digits, capitals, caption structure, context, and
        ' words from the locale files (Catalan "u"/"un" = 1 added to Bible_NumberWords
        ' 2026-10-05 for parity with es "uno" / en "one").
        Dim ctx4 As New RefContext()
        Dim opts4 As New RefDetectionOptions With {.LangHint = "ca", .Context = ctx4, .UpdateContext = True}
        Check(svc, opts4, "Romans 8 39.",
              "Rom", 8, 39, 39, "second number is evidence for an ordinary-word name, and is the verse")
        Check(svc, opts4, "Joan 1, 29.",
              "John", 1, 29, 29, "verse after a comma, no colon")
        Check(svc, opts4, "Joan 1, 41 42.",
              "John", 1, 41, 42, "third number ends a verse range")
        Check(svc, opts4, "Guardarà els vostres pensaments, Filipencs 4, 6 7.",
              "Phil", 4, 6, 7, "chapter, verse range with commas and spaces")
        Check(svc, opts4, "Joan 1, 60.",
              "John", 1, 0, 0, "verse 60 does not exist in John 1 (51) -> chapter only")
        Check(svc, opts4, "Joan 1, 5 3.",
              "John", 1, 5, 5, "range end lower than the start -> single verse")
        Check(svc, opts4, "Joan 1. 29 persones hi eren.",
              "John", 1, 0, 0, "a full stop ends it -> chapter only")
        Check(svc, opts4, "Joan 1, 3:16.",
              "John", 1, 0, 0, "a number followed by ':' is the next reference, not a verse")
        Check(svc, opts4, "amb el salm 119 que ja vàrem escoltar.",
              "Ps", 119, 0, 0, "lowercase spoken book name from the locale mid-sentence")
        Check(svc, opts4, "I lost my job 3 years ago.",
              "", 0, 0, 0, "lowercase book name that is NOT a locale spoken name -> no link")
        Check(svc, opts4, "marc 3 gols.",
              "", 0, 0, 0, "lowercase 'marc' -> no link")
        Check(svc, opts4, "Llegim Jeremies capítol 31.",
              "Jer", 31, 0, 0, "locale chapter word with no comma")
        Check(svc, opts4, "Leemos Mateo capítulo 5.",
              "Mat", 5, 0, 0, "Spanish locale chapter word with no comma")
        Check(svc, opts4, "Veniu i ho veureu. Joan.",
              "", 0, 0, 0, "caption ends with a book name (no reference yet)")
        Check(svc, opts4, "1 39 .",
              "John", 1, 39, 39, "split reference: next caption starts with two numbers")
        Check(svc, opts4, "l'Evangeli de Joan.",
              "", 0, 0, 0, "caption ends with a book name again")
        Check(svc, opts4, "El capítol 1, versets del 35 al 42.",
              "John", 1, 0, 0, "split reference: next caption starts with a locale chapter word")
        Check(svc, opts4, "Tu ets Simó, fill de Joan.",
              "", 0, 0, 0, "name at the end of a caption ...")
        Check(svc, opts4, "3 anys després va tornar.",
              "", 0, 0, 0, "... then a lone number is NOT a split reference")
        ' Catalan counting form "u" = 1 (as es "uno", en "one"); "un"/"una" are the article.
        Check(svc, opts4, "Llegim Joan, capítol u.",
              "John", 1, 0, 0, "Catalan 'u' after a chapter word")
        Check(svc, opts4, "Joan 3, verset u.",
              "John", 3, 1, 1, "Catalan 'u' after a verse word")
        ' Short number words after a book name: read only when another number follows
        ' (book + chapter + verse shape).
        Check(svc, opts4, "Que busqueu Joan, un 38.",
              "John", 1, 38, 38, "Catalan 'un' + number after a book (field 2026-09-27)")
        Check(svc, New RefDetectionOptions With {.LangHint = "es"}, "Leemos Juan uno, 38.",
              "John", 1, 38, 38, "Spanish 'uno' + number after a book")
        Check(svc, New RefDetectionOptions With {.LangHint = "en"}, "Read John two 3.",
              "John", 2, 3, 3, "English 'two' + number after a book")
        Check(svc, opts4, "Va venir Joan un dia.",
              "", 0, 0, 0, "'un' (the article) with no number after it -> no link")
        Check(svc, opts4, "Ho va dir Joan nou vegades.",
              "", 0, 0, 0, "'nou' with no number after it -> no link")
        Check(svc, opts4, "Ho va dir Joan u. 38 persones hi eren.",
              "", 0, 0, 0, "the second number is in the next sentence -> no link")

        ' A passing mention of another chapter of the same book must not erase
        ' the chapter being read (2026-09-13: "salm 1" inside a Psalm 119 sermon).
        Dim ctx5 As New RefContext()
        Dim opts5 As New RefDetectionOptions With {.LangHint = "ca", .Context = ctx5, .UpdateContext = True}
        Check(svc, opts5, "Estem al Salm 119.",
              "Ps", 119, 0, 0, "sermon psalm")
        Check(svc, opts5, "Ens ressona al principi del salm 1.",
              "Ps", 1, 0, 0, "passing mention of Psalm 1")
        Check(svc, opts5, "El verset 7 diu.",
              "Ps", 119, 7, 7, "verse 7 does not exist in Psalm 1 -> back to Psalm 119")
        Check(svc, opts5, "I el verset 165 diu.",
              "Ps", 119, 165, 165, "stays with Psalm 119")

        ' ── Typed lookups (ParseReferenceAsync) after the static-table removal ──
        CheckParse(svc, "Salms 23", "Ps", 230, 23, "typed Catalan title resolves via the derived index")
        CheckParse(svc, "Ps 23", "Ps", 230, 23, "typed wire code resolves via StandardBookNumbers")
        CheckParse(svc, "1 Corinthians 13", "1Cor", 530, 13, "typed English name resolves via locale fallback names")

        ' ── Zero Bibles installed: locale Bible_BookNames IS the fallback ──
        ' The alias index is class-shared, so this section must stay LAST:
        ' rebuilding from an empty dir replaces the Bible-derived index.
        Dim emptyDir = Path.Combine(Path.GetTempPath(), "refcheck-empty-bibles")
        Directory.CreateDirectory(emptyDir)
        Dim svcEmpty As New BibleService(
            NullLogger(Of BibleService).Instance,
            Options.Create(New ServerOptions With {.BiblesDirectory = emptyDir}))
        ' German names exist ONLY in the German Bible (no de.json) — when
        ' "Johannes 3" stops detecting, the locale-only rebuild has landed.
        Dim rebuilt = False
        For i = 1 To 120
            If svcEmpty.DetectReferencesInText("Johannes 3").Count = 0 Then
                rebuilt = True
                Exit For
            End If
            Thread.Sleep(500)
        Next
        If Not rebuilt Then
            Console.WriteLine("FATAL: locale-only alias index never replaced the Bible-derived one")
            Environment.Exit(2)
        End If
        Check(svcEmpty, Nothing, "John 3:16 says it plainly.",
              "John", 3, 16, 16, "NO Bibles: English detection via en.json Bible_BookNames")
        Check(svcEmpty, Nothing, "Estem al Salm 119.",
              "Ps", 119, 0, 0, "NO Bibles: Catalan detection via ca.json locale names")

        If _failures = 0 Then
            Console.WriteLine("ALL CHECKS PASSED")
        Else
            Console.WriteLine($"{_failures} CHECK(S) FAILED")
            Environment.Exit(1)
        End If
    End Sub

    Private Sub CheckParse(svc As BibleService, reference As String,
                           book As String, bookNumber As Integer, chapter As Integer, label As String)
        Dim r = svc.ParseReferenceAsync(reference).Result
        Dim ok = r.IsValid AndAlso r.Book = book AndAlso r.BookNumber = bookNumber AndAlso r.Chapter = chapter
        Dim got = If(r.IsValid, $"{r.Book}({r.BookNumber}) {r.Chapter}", "invalid")
        Dim tag = If(ok, "PASS", "FAIL")
        If Not ok Then _failures += 1
        Console.WriteLine($"{tag}  [{label}] ""{reference}"" -> {got} (want {book}({bookNumber}) {chapter})")
    End Sub

    Private Sub Check(svc As BibleService, opts As RefDetectionOptions, text As String,
                      book As String, chapter As Integer, vStart As Integer, vEnd As Integer,
                      label As String)
        Dim refs = svc.DetectReferencesInText(text, opts)
        Dim got = "none"
        Dim ok = (book = "" AndAlso refs.Count = 0)
        If refs.Count > 0 Then
            Dim r = refs(0).Reference
            got = $"{r.Book} {r.Chapter}:{r.VerseStart}-{r.VerseEnd}"
            ok = r.Book = book AndAlso r.Chapter = chapter AndAlso
                 r.VerseStart = vStart AndAlso r.VerseEnd = vEnd
        End If
        Dim tag = If(ok, "PASS", "FAIL")
        If Not ok Then _failures += 1
        Console.WriteLine($"{tag}  [{label}] ""{text}"" -> {got} (want {book} {chapter}:{vStart}-{vEnd})")
    End Sub

End Module
