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

        If _failures = 0 Then
            Console.WriteLine("ALL CHECKS PASSED")
        Else
            Console.WriteLine($"{_failures} CHECK(S) FAILED")
            Environment.Exit(1)
        End If
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
