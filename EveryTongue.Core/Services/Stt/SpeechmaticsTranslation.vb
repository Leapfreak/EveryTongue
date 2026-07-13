Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' Speechmatics real-time translation facts, centralized so the rest of the app
    ''' doesn't hard-code them (mirrors how the engine registries centralize backend
    ''' facts). Speechmatics translation is English-pivot only — one side of every
    ''' pair must be English — supports ~34 languages, and at most 5 targets per
    ''' session. Anything not eligible here falls back to the NLLB/cloud translator.
    ''' </summary>
    Friend Module SpeechmaticsTranslation

        ''' <summary>Max target languages Speechmatics accepts in one session.</summary>
        Public Const MaxTargets As Integer = 5

        ''' <summary>
        ''' Speechmatics translation language codes (mostly ISO 639-1; Mandarin is
        ''' "cmn", Norwegian "no"). English ("en") is the pivot.
        ''' </summary>
        Private ReadOnly _supported As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
            "en", "bg", "ca", "cmn", "cs", "da", "de", "el", "es", "et", "fi", "fr",
            "gl", "hi", "hr", "hu", "id", "it", "ja", "ko", "lt", "lv", "ms", "nl",
            "no", "pl", "pt", "ro", "ru", "sk", "sl", "sv", "tr", "uk", "vi"
        }

        ' ISO 639-1 → Speechmatics code where they diverge.
        Private ReadOnly _iso1Overrides As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
            {"zh", "cmn"}, {"nb", "no"}, {"nn", "no"}
        }

        ''' <summary>
        ''' Convert a FLORES code to its Speechmatics translation code, or "" if
        ''' Speechmatics can't translate that language.
        ''' </summary>
        Public Function FloresToSpeechmatics(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim iso1 = LanguageCodeService.Instance.FloresToIso1(floresCode)
            If String.IsNullOrEmpty(iso1) Then Return ""
            iso1 = iso1.ToLowerInvariant()
            Dim sm As String = Nothing
            If Not _iso1Overrides.TryGetValue(iso1, sm) Then sm = iso1
            Return If(_supported.Contains(sm), sm, "")
        End Function

        ''' <summary>
        ''' Convert a Speechmatics translation code back to a FLORES code (e.g. "es"
        ''' → "spa_Latn", "cmn" → Mandarin). Returns "" if unknown.
        ''' </summary>
        Public Function SpeechmaticsToFlores(smCode As String) As String
            If String.IsNullOrEmpty(smCode) Then Return ""
            Dim code = smCode.ToLowerInvariant()
            ' Reverse the engine-specific divergences to a code the table understands.
            Select Case code
                Case "cmn" : code = "zh"
                Case "no" : code = "nb"
            End Select
            Return LanguageCodeService.Instance.ToFlores(code)
        End Function

        ''' <summary>
        ''' Whether Speechmatics can translate sourceFlores → targetFlores in one
        ''' session: both supported, distinct, and one side is English.
        ''' </summary>
        Public Function IsEligiblePair(sourceFlores As String, targetFlores As String) As Boolean
            Dim smSource = FloresToSpeechmatics(sourceFlores)
            Dim smTarget = FloresToSpeechmatics(targetFlores)
            If smSource = "" OrElse smTarget = "" OrElse smSource.Equals(smTarget, StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If
            Return smSource.Equals("en", StringComparison.OrdinalIgnoreCase) OrElse
                   smTarget.Equals("en", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Split active target languages (FLORES) into the subset Speechmatics will
        ''' translate (returned as Speechmatics codes, capped at MaxTargets) and the
        ''' FLORES codes it covers (so the caller can route the rest to NLLB).
        ''' </summary>
        Public Function ComputeTargets(sourceFlores As String, activeTargetsFlores As IEnumerable(Of String)) _
            As (SmCodes As List(Of String), CoveredFlores As List(Of String))
            Dim smCodes As New List(Of String)
            Dim covered As New List(Of String)
            If activeTargetsFlores Is Nothing Then Return (smCodes, covered)
            For Each tgt In activeTargetsFlores
                If smCodes.Count >= MaxTargets Then Exit For
                If IsEligiblePair(sourceFlores, tgt) Then
                    Dim sm = FloresToSpeechmatics(tgt)
                    If Not smCodes.Contains(sm, StringComparer.OrdinalIgnoreCase) Then
                        smCodes.Add(sm)
                        covered.Add(tgt)
                    End If
                End If
            Next
            Return (smCodes, covered)
        End Function

        ''' <summary>
        ''' Configure a session for Speechmatics inline translation, when enabled by
        ''' the caller (the room picked the Speechmatics inline translation engine AND
        ''' its STT engine is Speechmatics). Engine-owned: finds the engine's own
        ''' config block on the session (a non-Speechmatics session has none → no-op),
        ''' and computes the engine-native target codes. Callers stay blind to the
        ''' engine's fields. cfg is unused for gating now (kept for signature stability).
        ''' </summary>
        Public Sub ConfigureSession(sttConfig As SttSessionConfig, cfg As AppConfig,
                                    sourceWhisperLang As String, activeLangs As List(Of String),
                                    enabled As Boolean)
            If Not enabled Then Return
            ' Only a Speechmatics session carries a SpeechmaticsConfig block.
            Dim sm = sttConfig.Block(Of Configs.SpeechmaticsConfig)()
            If sm Is Nothing Then Return
            sm.EnableTranslation = True
            sm.TranslationTargets = ComputeTargets(SourceFlores(sourceWhisperLang), activeLangs).SmCodes
        End Sub

        ''' <summary>
        ''' FLORES source for target computation — Speechmatics treats an
        ''' unset/"auto" source language as English.
        ''' </summary>
        Public Function SourceFlores(sourceWhisperLang As String) As String
            Dim lang = If(String.IsNullOrEmpty(sourceWhisperLang) OrElse sourceWhisperLang = "auto", "en", sourceWhisperLang)
            Dim flores = TranslationService.WhisperToFloresLang(lang.ToLowerInvariant())
            Return If(String.IsNullOrEmpty(flores), "eng_Latn", flores)
        End Function

    End Module

End Namespace
