Imports TranscriptionTools.Services.Models

Namespace Services.Interfaces
    ''' <summary>
    ''' Single source of truth for language code conversions across all subsystems
    ''' (Whisper, NLLB, DeepL, Google, Azure, display names).
    ''' Loaded from Data/language-codes.json.
    ''' </summary>
    Public Interface ILanguageMapService
        Function WhisperToNllb(whisperCode As String) As String
        Function NllbToWhisper(nllbCode As String) As String
        Function ToDeepL(code As String) As String
        Function ToGoogle(code As String) As String
        Function ToAzure(code As String) As String
        Function ToDisplayName(code As String) As String
        Function ToNativeName(code As String) As String
        Function GetAllLanguages() As IReadOnlyList(Of LanguageInfo)
    End Interface
End Namespace
