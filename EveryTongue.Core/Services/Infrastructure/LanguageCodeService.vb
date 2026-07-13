Imports System.IO
Imports System.Text.Json

Namespace Services.Infrastructure

    ''' <summary>
    ''' Centralised language code lookup service. Loads language-codes.json once
    ''' and provides fast conversion between any code format (FLORES, ISO 639-1,
    ''' ISO 639-3, Whisper, DeepL, Google, Azure).
    ''' </summary>
    Public Class LanguageCodeService

        Private Shared _instance As LanguageCodeService
        Private Shared ReadOnly _lock As New Object()

        ' Primary index: FLORES code -> entry
        Private ReadOnly _byFlores As New Dictionary(Of String, LangEntry)(StringComparer.OrdinalIgnoreCase)
        ' Reverse indexes for fast lookup
        Private ReadOnly _byIso1 As New Dictionary(Of String, LangEntry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _byIso3 As New Dictionary(Of String, LangEntry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _byWhisper As New Dictionary(Of String, LangEntry)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _byName As New Dictionary(Of String, LangEntry)(StringComparer.OrdinalIgnoreCase)

        Private Class LangEntry
            Public Property Flores As String
            Public Property Iso1 As String
            Public Property Iso3 As String
            Public Property Whisper As String
            Public Property DeepL As String
            Public Property Google As String
            Public Property Azure As String
            Public Property Name As String
            Public Property Native As String
        End Class

        Private Sub New(jsonPath As String)
            If Not File.Exists(jsonPath) Then
                AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"language-codes.json not found: {jsonPath}")
                Return
            End If

            Try
                Dim json = File.ReadAllText(jsonPath)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        Dim entry As New LangEntry With {
                            .Flores = prop.Name
                        }
                        Dim el = prop.Value
                        If el.TryGetProperty("iso1", Nothing) Then entry.Iso1 = el.GetProperty("iso1").GetString()
                        If el.TryGetProperty("iso3", Nothing) Then entry.Iso3 = el.GetProperty("iso3").GetString()
                        If el.TryGetProperty("whisper", Nothing) Then entry.Whisper = el.GetProperty("whisper").GetString()
                        If el.TryGetProperty("deepl", Nothing) Then entry.DeepL = el.GetProperty("deepl").GetString()
                        If el.TryGetProperty("google", Nothing) Then entry.Google = el.GetProperty("google").GetString()
                        If el.TryGetProperty("azure", Nothing) Then entry.Azure = el.GetProperty("azure").GetString()
                        If el.TryGetProperty("name", Nothing) Then entry.Name = el.GetProperty("name").GetString()
                        If el.TryGetProperty("native", Nothing) Then entry.Native = el.GetProperty("native").GetString()

                        _byFlores(entry.Flores) = entry
                        If Not String.IsNullOrEmpty(entry.Iso1) Then _byIso1(entry.Iso1) = entry
                        If Not String.IsNullOrEmpty(entry.Iso3) Then _byIso3(entry.Iso3) = entry
                        If Not String.IsNullOrEmpty(entry.Whisper) Then _byWhisper(entry.Whisper) = entry
                        If Not String.IsNullOrEmpty(entry.Name) Then _byName(entry.Name) = entry
                    Next
                End Using
                AppLogger.Log(LogEvents.LOCALE_LOADED, $"Loaded {_byFlores.Count} languages from language-codes.json")
            Catch ex As Exception
                AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"Failed to load language-codes.json: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Gets the singleton instance (loads from wwwroot/data/language-codes.json).
        ''' </summary>
        Public Shared ReadOnly Property Instance As LanguageCodeService
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            Dim jsonPath = Path.Combine(
                                AppDomain.CurrentDomain.BaseDirectory,
                                "wwwroot", "data", "language-codes.json")
                            _instance = New LanguageCodeService(jsonPath)
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ' ── Conversion methods ──

        ''' <summary>Whisper code (e.g. "es", "no", "jw") -> FLORES code (e.g. "spa_Latn")</summary>
        Public Function WhisperToFlores(whisperCode As String) As String
            If String.IsNullOrEmpty(whisperCode) Then Return ""
            Dim entry As LangEntry = Nothing
            ' Try whisper-specific code first (handles non-standard codes like "no", "jw")
            If _byWhisper.TryGetValue(whisperCode, entry) Then Return entry.Flores
            ' Fall back to ISO 639-1
            If _byIso1.TryGetValue(whisperCode, entry) Then Return entry.Flores
            ' Fall back to language name (whisper-server returns "spanish" not "es")
            If _byName.TryGetValue(whisperCode, entry) Then Return entry.Flores
            Return ""
        End Function

        ''' <summary>FLORES code (e.g. "spa_Latn") -> ISO 639-1 short code (e.g. "ES")</summary>
        Public Function FloresToShortCode(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return "??"
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) AndAlso Not String.IsNullOrEmpty(entry.Iso1) Then
                Return entry.Iso1.ToUpperInvariant()
            End If
            ' Fallback: first 2 chars of prefix
            Dim prefix = floresCode.Split("_"c)(0)
            Return prefix.Substring(0, Math.Min(2, prefix.Length)).ToUpperInvariant()
        End Function

        ''' <summary>ISO 639-1 (e.g. "es") -> ISO 639-3 (e.g. "spa")</summary>
        Public Function Iso1ToIso3(iso1Code As String) As String
            If String.IsNullOrEmpty(iso1Code) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byIso1.TryGetValue(iso1Code, entry) Then Return entry.Iso3
            Return iso1Code
        End Function

        ''' <summary>ISO 639-3 (e.g. "spa") -> FLORES code (e.g. "spa_Latn")</summary>
        Public Function Iso3ToFlores(iso3Code As String) As String
            If String.IsNullOrEmpty(iso3Code) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byIso3.TryGetValue(iso3Code, entry) Then Return entry.Flores
            Return ""
        End Function

        ''' <summary>FLORES code (e.g. "spa_Latn") -> ISO 639-3 (e.g. "spa")</summary>
        Public Function FloresToIso3(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return entry.Iso3
            ' Fallback: split prefix
            Return floresCode.Split("_"c)(0)
        End Function

        ''' <summary>FLORES code -> DeepL code (e.g. "ES")</summary>
        Public Function FloresToDeepL(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.DeepL, "")
            Return ""
        End Function

        ''' <summary>FLORES code -> Google Translate code (e.g. "es")</summary>
        Public Function FloresToGoogle(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.Google, "")
            Return ""
        End Function

        ''' <summary>FLORES code -> Azure Translator code (e.g. "es")</summary>
        Public Function FloresToAzure(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.Azure, "")
            Return ""
        End Function

        ''' <summary>Any code format -> FLORES code. Tries FLORES, then whisper, then ISO 639-1, then ISO 639-3.</summary>
        Public Function ToFlores(anyCode As String) As String
            If String.IsNullOrEmpty(anyCode) Then Return ""
            ' Already FLORES?
            If _byFlores.ContainsKey(anyCode) Then Return anyCode
            ' Whisper-specific code? (e.g. "no" -> Norwegian, "jw" -> Javanese)
            Dim entry As LangEntry = Nothing
            If _byWhisper.TryGetValue(anyCode, entry) Then Return entry.Flores
            ' ISO 639-1?
            If _byIso1.TryGetValue(anyCode, entry) Then Return entry.Flores
            ' ISO 639-3?
            If _byIso3.TryGetValue(anyCode, entry) Then Return entry.Flores
            Return ""
        End Function

        ''' <summary>Normalize any code to ISO 639-3. Handles 2-letter and 3-letter inputs.</summary>
        Public Function NormalizeToIso3(code As String) As String
            If String.IsNullOrEmpty(code) Then Return ""
            If code.Length >= 3 Then
                ' Already 3-letter — verify it exists
                Dim entry As LangEntry = Nothing
                If _byIso3.TryGetValue(code, entry) Then Return entry.Iso3
                Return code
            End If
            ' 2-letter -> 3-letter
            Return Iso1ToIso3(code)
        End Function

        ''' <summary>FLORES code -> display name (e.g. "Spanish")</summary>
        Public Function GetDisplayName(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.Name, "")
            Return ""
        End Function

        ''' <summary>FLORES code -> native name (e.g. "Español")</summary>
        Public Function GetNativeName(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.Native, "")
            Return ""
        End Function

        ''' <summary>Returns whisper->FLORES dictionary for compatibility with existing code.</summary>
        Public Function GetWhisperToFloresMap() As Dictionary(Of String, String)
            Dim map As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            ' Include ISO 639-1 codes
            For Each kvp In _byIso1
                map(kvp.Key) = kvp.Value.Flores
            Next
            ' Include whisper-specific codes (overrides iso1 where they differ, e.g. "no" vs "nb")
            For Each kvp In _byWhisper
                map(kvp.Key) = kvp.Value.Flores
            Next
            Return map
        End Function

        ''' <summary>FLORES code -> ISO 639-1 (e.g. "spa_Latn" -> "es")</summary>
        Public Function FloresToIso1(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(floresCode, entry) Then Return If(entry.Iso1, "")
            Return ""
        End Function

        ''' <summary>
        ''' Resolves any code format (FLORES, ISO 639-1, ISO 639-3, whisper) to a display name.
        ''' Returns the code itself if not found.
        ''' </summary>
        Public Function GetDisplayNameForCode(code As String) As String
            If String.IsNullOrEmpty(code) Then Return ""
            Dim entry As LangEntry = Nothing
            If _byFlores.TryGetValue(code, entry) Then Return If(entry.Name, code)
            If _byIso1.TryGetValue(code, entry) Then Return If(entry.Name, code)
            If _byIso3.TryGetValue(code, entry) Then Return If(entry.Name, code)
            If _byWhisper.TryGetValue(code, entry) Then Return If(entry.Name, code)
            Return code
        End Function

        ''' <summary>
        ''' Returns all languages sorted by display name.
        ''' Each item includes FLORES code, ISO 639-1 code, English name, and native name.
        ''' </summary>
        Public Function GetAllLanguagesSorted() As List(Of (Flores As String, Iso1 As String, Name As String, Native As String))
            Dim result As New List(Of (String, String, String, String))()
            For Each kvp In _byFlores
                Dim e = kvp.Value
                result.Add((e.Flores, If(e.Iso1, ""), If(e.Name, ""), If(e.Native, "")))
            Next
            result.Sort(Function(a, b) String.Compare(a.Item3, b.Item3, StringComparison.OrdinalIgnoreCase))
            Return result
        End Function

        ''' <summary>
        ''' Returns all languages as a list of (FloresCode, NativeName, EnglishName, Iso1Code)
        ''' for serving to web clients. Only includes entries that have an ISO 639-1 code.
        ''' </summary>
        Public Function GetAllLanguagesForWeb() As List(Of (Flores As String, Native As String, Name As String, Iso1 As String))
            Dim result As New List(Of (String, String, String, String))()
            Dim seenIso1 As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each kvp In _byFlores
                Dim e = kvp.Value
                If String.IsNullOrEmpty(e.Iso1) Then Continue For
                If Not seenIso1.Add(e.Iso1) Then Continue For
                result.Add((e.Flores, If(e.Native, e.Name), If(e.Name, ""), e.Iso1))
            Next
            result.Sort(Function(a, b) String.Compare(a.Item3, b.Item3, StringComparison.OrdinalIgnoreCase))
            Return result
        End Function

    End Class

End Namespace
