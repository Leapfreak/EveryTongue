' Corpus preview for the derived scripture-reference detection: builds the
' real BookAliasIndex from the publish Bibles, runs the REAL
' BibleService.DetectReferencesInText over the recorded sermon commits,
' prints every detection (must catch the real refs, zero false fires).
Imports System.IO
Imports Microsoft.Extensions.Logging.Abstractions
Imports Microsoft.Extensions.Options
Imports EveryTongue.Server
Imports EveryTongue.Services.Bible

Module Program
    Sub Main(args As String())
        Console.OutputEncoding = Text.Encoding.UTF8
        Dim biblesDir = "C:\Users\Jeremy\Desktop\Source\EveryTongue\EveryTongue\bin\Publish\Bibles"
        Dim corpus = Environment.GetEnvironmentVariable("TEMP") & "\sermon_full.txt"

        Dim svc As New BibleService(
            NullLogger(Of BibleService).Instance,
            Options.Create(New ServerOptions With {.BiblesDirectory = biblesDir}))

        ' index builds in background — give it a moment, then sanity-check
        Threading.Thread.Sleep(6000)

        Dim hits = 0, lines = 0
        For Each line In File.ReadLines(corpus)
            lines += 1
            Dim refs = svc.DetectReferencesInText(line)
            For Each r In refs
                hits += 1
                Console.WriteLine($"[{r.Reference.Book} {r.Reference.Chapter}:{r.Reference.VerseStart}-{r.Reference.VerseEnd}]  matched ""{r.MatchedText}""")
                Console.WriteLine($"   in: {line.Substring(0, Math.Min(110, line.Length))}")
            Next
        Next
        Console.WriteLine($"── {lines} lines scanned, {hits} detections")

        ' targeted probes: must-hit and must-miss cases
        Dim probes = {
            "Així que a partir de les temptacions de Jesús, en Mateu 4, podem veure algunes coses.",
            "I a Primera de Joan 4:10 trobem l'amor.",
            "El salm 27 Déu ens diu que el Senyor em recollirà.",
            "En Joan té 8 anys i li agrada jugar.",
            "Els tres reis van arribar a la ciutat.",
            "Va llegir Filipencs 2 amb molta calma.",
            "Primer Reis 17 ens parla d'Elies.",
            "La feina 3 vegades més dura."}
        Console.WriteLine("── probes:")
        For Each p In probes
            Dim refs = svc.DetectReferencesInText(p)
            Dim what = If(refs.Count = 0, "(no match)",
                String.Join(", ", refs.Select(Function(r) $"{r.Reference.Book} {r.Reference.Chapter}:{r.Reference.VerseStart}")))
            Console.WriteLine($"  {what,-22} <- {p}")
        Next
    End Sub
End Module
