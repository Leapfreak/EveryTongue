Namespace Services.Tts
    ''' <summary>
    ''' Shared FLORES-language → Microsoft neural voice catalogue. Edge TTS and
    ''' Azure AI Speech TTS expose the SAME voice list, so both backends share
    ''' this single map instead of duplicating it. Google Cloud TTS derives its
    ''' BCP-47 languageCode from the voice's locale prefix (e.g.
    ''' "en-US-JennyNeural" → "en-US").
    ''' </summary>
    Friend Module NeuralVoiceCatalog

        ''' <summary>
        ''' Curated default neural voice per language. Accepts FLORES codes
        ''' (e.g. "eng_Latn") — only the ISO 639-3 prefix is used.
        ''' Falls back to en-US-JennyNeural for unmapped languages.
        ''' </summary>
        Friend Function GetVoiceForLanguage(language As String) As String
            Select Case If(language, "").Split("_"c)(0).ToLower()
                Case "eng" : Return "en-US-JennyNeural"
                Case "spa" : Return "es-ES-ElviraNeural"
                Case "fra" : Return "fr-FR-DeniseNeural"
                Case "deu" : Return "de-DE-KatjaNeural"
                Case "cat" : Return "ca-ES-JoanaNeural"
                Case "por" : Return "pt-BR-FranciscaNeural"
                Case "ita" : Return "it-IT-ElsaNeural"
                Case "jpn" : Return "ja-JP-NanamiNeural"
                Case "zho" : Return "zh-CN-XiaoxiaoNeural"
                Case "kor" : Return "ko-KR-SunHiNeural"
                Case "arb" : Return "ar-SA-ZariyahNeural"
                Case "hin" : Return "hi-IN-SwaraNeural"
                Case "rus" : Return "ru-RU-SvetlanaNeural"
                Case Else : Return "en-US-JennyNeural"
            End Select
        End Function

        ''' <summary>
        ''' BCP-47 locale for a language, derived from the curated voice name
        ''' (e.g. "es-ES-ElviraNeural" → "es-ES"). Used for SSML xml:lang and
        ''' for vendors (Google Cloud TTS) that select voices by languageCode.
        ''' </summary>
        Friend Function GetLocaleForLanguage(language As String) As String
            Dim voice = GetVoiceForLanguage(language)
            Dim parts = voice.Split("-"c)
            If parts.Length >= 2 Then Return $"{parts(0)}-{parts(1)}"
            Return "en-US"
        End Function

    End Module
End Namespace
