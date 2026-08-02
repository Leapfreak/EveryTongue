' Corpus preview for the derived scripture-reference detection: builds the
' real BookAliasIndex from the publish Bibles, runs the REAL
' BibleService.DetectReferencesInText over the recorded sermon commits,
' prints every detection (must catch the real refs, zero false fires).
' Corpus lines run sequentially through one RefContext (like a live room), so
' bare "versículo N" resolution is exercised too — those print with [ctx].
Imports System.IO
Imports Microsoft.Extensions.Logging.Abstractions
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server
Imports EveryTongue.Services.Bible
Imports EveryTongue.Services.Models

Module Program
    Sub Main(args As String())
        Console.OutputEncoding = Text.Encoding.UTF8
        Dim biblesDir = "C:\Users\Jeremy\Desktop\Source\EveryTongue\EveryTongue\bin\Publish\Bibles"
        Dim corpus = Environment.GetEnvironmentVariable("TEMP") & "\sermon_full.txt"

        Dim svc As New BibleService(
            NullLogger(Of BibleService).Instance,
            Options.Create(New ServerOptions With {.BiblesDirectory = biblesDir}))

        ' index builds in background — give it a moment (covers the one-time
        ' cache rescan when locale words / fold migration invalidate it)
        Threading.Thread.Sleep(8000)

        Dim ctx As New RefContext()
        Dim hits = 0, ctxHits = 0, lines = 0
        For Each line In File.ReadLines(corpus)
            lines += 1
            Dim refs = svc.DetectReferencesInText(line, New RefDetectionOptions With {.LangHint = "es", .Context = ctx})
            For Each r In refs
                hits += 1
                If r.FromContext Then ctxHits += 1
                Dim marker = If(r.FromContext, " [ctx]", "")
                Console.WriteLine($"[{r.Reference.Book} {r.Reference.Chapter}:{r.Reference.VerseStart}-{r.Reference.VerseEnd}]{marker}  matched ""{r.MatchedText}""")
                Console.WriteLine($"   in: {line.Substring(0, Math.Min(110, line.Length))}")
            Next
        Next
        Console.WriteLine($"── {lines} lines scanned, {hits} detections ({ctxHits} from context)")

        ' ── targeted probes: (expected, langHint, text) ──────────────────
        ' expected "" = must-miss. Chapter-only refs print as "Book N:0".
        Dim probes = {
            ("Mat 4:0", "ca", "Així que a partir de les temptacions de Jesús, en Mateu 4, podem veure algunes coses."),
            ("1Jn 4:10", "ca", "I a Primera de Joan 4:10 trobem l'amor."),
            ("Ps 27:0", "ca", "El salm 27 Déu ens diu que el Senyor em recollirà."),
            ("", "ca", "En Joan té 8 anys i li agrada jugar."),
            ("", "ca", "Els tres reis van arribar a la ciutat."),
            ("Phil 2:0", "ca", "Va llegir Filipencs 2 amb molta calma."),
            ("1Kin 17:0", "ca", "Primer Reis 17 ens parla d'Elies."),
            ("", "ca", "La feina 3 vegades més dura."),
            ("", "ca", "La feina tres vegades més dura."),
            ("Ps 22:0", "es", "Empezamos con el Salmo 22."),
            ("Ps 22:0", "es", "el salmo 22 nos habla del Mesías."),
            ("", "es", "el salmo del siglo 21."),
            ("Mat 27:0", "es", "Un milenio después, el evangelista Mateo en el capítulo 27, describiendo la crucifixión."),
            ("John 19:0", "es", "Qué dice el evangelista Juan en el capítulo 19, cuando los soldados."),
            ("1Cor 10:0", "es", "Tal como dice Pablo en Primera de Corintios diez, cuando se llega a ese punto."),
            ("Ps 45:0", "es", "Vamos a leer el Salmo cuarenta y cinco."),
            ("Ps 119:11", "es", "Salmo ciento diecinueve, versículo once."),
            ("Ps 22:7", "es", "En el Salmo 22, versículo siete, leemos."),
            ("Ps 22:0", "es", "El Salmo 22 durante 3 años me acompañó."),
            ("", "es", "Mateo dice 4 cosas."),
            ("", "es", "Pedro y cinco amigos."),
            ("", "en", "John once said something."),
            ("", "en", "John set out for Galilee."),
            ("Ps 23:0", "en", "Let us read Psalm 23 together."),
            ("Ps 23:1", "en", "We continue in Psalm 23:1 now."),
            ("Ps 23:1", "en", "Psalm 23, verse one."),
            ("", "es", "Y María, con el niño 3 días."),
            ("", "ca", "mateu 4 vegades el mateix.")}
        Console.WriteLine("── probes:")
        Dim failed = 0
        For Each p In probes
            Dim refs = svc.DetectReferencesInText(p.Item3, New RefDetectionOptions With {.LangHint = p.Item2})
            Dim what = If(refs.Count = 0, "",
                String.Join(", ", refs.Select(Function(r) $"{r.Reference.Book} {r.Reference.Chapter}:{r.Reference.VerseStart}")))
            Dim ok = If(p.Item1 = "", refs.Count = 0, what.Contains(p.Item1))
            If Not ok Then failed += 1
            Console.WriteLine($"  {If(ok, "PASS", "FAIL")}  {If(what = "", "(no match)", what),-22} want [{If(p.Item1 = "", "no match", p.Item1)}]  <- {p.Item3}")
        Next

        ' ── context sequence: the real 11:26–11:28 trap from 2026-08-02 ──
        ' Mateo 27 cited mid-psalm; "del salmo" must rescue back to Ps 22,
        ' and the bare "Versículo 18" must follow the rescue, not Matthew.
        Console.WriteLine("── context sequence:")
        Dim seq As New RefContext()
        Dim steps = {
            ("Ps 22:0", "Empezamos con el Salmo 22."),
            ("Mat 27:0", "Y también en Mateo 27 Más adelante leemos."),
            ("Ps 22:15", "David versículo 15 del salmo."),
            ("Ps 22:18", "Versículo 18."),
            ("John 19:0", "Juan en el capítulo 19, cuando los soldados."),
            ("Ps 22:22", "Dice el versículo 22 del salmo, anunciaré tu nombre.")}
        For Each s In steps
            Dim refs = svc.DetectReferencesInText(s.Item2, New RefDetectionOptions With {.LangHint = "es", .Context = seq})
            Dim what = If(refs.Count = 0, "(no match)",
                String.Join(", ", refs.Select(Function(r) $"{r.Reference.Book} {r.Reference.Chapter}:{r.Reference.VerseStart}{If(r.FromContext, " [ctx]", "")}")))
            Dim ok = what.Contains(s.Item1)
            If Not ok Then failed += 1
            Console.WriteLine($"  {If(ok, "PASS", "FAIL")}  {what,-28} want [{s.Item1}]  <- {s.Item2}")
        Next
        Console.WriteLine($"── probe failures: {failed}")
        Environment.ExitCode = If(failed > 0, 1, 0)
    End Sub
End Module
