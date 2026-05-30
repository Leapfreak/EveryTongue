Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading

Namespace Services.Infrastructure

    ''' <summary>
    ''' JSON-based localization service. Loads locale strings from JSON files in the
    ''' locales/ folder. Downloads missing packs from GitHub on demand.
    ''' English is always built-in; all other languages are downloaded or generated via NLLB.
    ''' </summary>
    Public Class LanguagePackService

        Private Shared _instance As LanguagePackService
        Private Shared ReadOnly _lock As New Object()

        Private Const GitHubBaseUrl As String = "https://raw.githubusercontent.com/LeapFreak/EveryTongue/main/locales/"

        Private ReadOnly _localesDir As String
        Private ReadOnly _strings As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(15)
        }

        Private _currentLang As String = "en"

        Private Sub New(localesDir As String)
            _localesDir = localesDir
            If Not Directory.Exists(_localesDir) Then
                Directory.CreateDirectory(_localesDir)
            End If
        End Sub

        ''' <summary>Gets the singleton instance.</summary>
        Public Shared ReadOnly Property Instance As LanguagePackService
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            Dim dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "locales")
                            _instance = New LanguagePackService(dir)
                            _instance.LoadLanguage("en")
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        ''' <summary>The currently loaded language code (e.g. "en", "es").</summary>
        Public ReadOnly Property CurrentLanguage As String
            Get
                Return _currentLang
            End Get
        End Property

        ''' <summary>Path to the locales directory.</summary>
        Public ReadOnly Property LocalesDirectory As String
            Get
                Return _localesDir
            End Get
        End Property

        ''' <summary>
        ''' Loads a language pack from locales/{lang}.json. If the file doesn't exist locally,
        ''' attempts to download it from GitHub. Falls back to English for missing keys.
        ''' </summary>
        Public Sub LoadLanguage(langCode As String)
            If String.IsNullOrEmpty(langCode) Then langCode = "en"

            ' Normalize: "zh-Hans" -> "zh", "pt-BR" -> "pt"
            Dim normalized = NormalizeLangCode(langCode)

            _strings.Clear()

            ' Always load English first as fallback
            Dim enPath = Path.Combine(_localesDir, "en.json")
            If File.Exists(enPath) Then
                LoadJsonInto(enPath, _strings)
            End If

            ' Then overlay the requested language
            If Not normalized.Equals("en", StringComparison.OrdinalIgnoreCase) Then
                Dim langPath = Path.Combine(_localesDir, $"{normalized}.json")
                If File.Exists(langPath) Then
                    LoadJsonInto(langPath, _strings)
                Else
                    AppLogger.Log($"[LanguagePack] {normalized}.json not found locally, will attempt download")
                End If
            End If

            _currentLang = normalized
            AppLogger.Log($"[LanguagePack] Loaded '{normalized}' with {_strings.Count} keys")
        End Sub

        ''' <summary>
        ''' Gets a localized string by key. Returns the key itself if not found.
        ''' </summary>
        Public Function GetString(key As String) As String
            If String.IsNullOrEmpty(key) Then Return ""
            Dim val As String = Nothing
            If _strings.TryGetValue(key, val) Then Return val
            Return key
        End Function

        ''' <summary>
        ''' Gets all strings for the current language (used by API endpoint to serve to web client).
        ''' Only returns keys prefixed with "web." (stripped of the prefix).
        ''' </summary>
        Public Function GetWebStrings() As Dictionary(Of String, String)
            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each kvp In _strings
                If kvp.Key.StartsWith("web.", StringComparison.OrdinalIgnoreCase) Then
                    result(kvp.Key.Substring(4)) = kvp.Value
                End If
            Next
            Return result
        End Function

        ''' <summary>
        ''' Gets all raw strings for the current language (for serving full locale JSON).
        ''' </summary>
        Public Function GetAllStrings() As Dictionary(Of String, String)
            Return New Dictionary(Of String, String)(_strings, StringComparer.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Attempts to download a language pack from GitHub.
        ''' Returns True if successful.
        ''' </summary>
        Public Async Function DownloadLanguageAsync(langCode As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Dim normalized = NormalizeLangCode(langCode)
            Dim url = GitHubBaseUrl & normalized & ".json"
            Dim localPath = Path.Combine(_localesDir, $"{normalized}.json")

            Try
                AppLogger.Log($"[LanguagePack] Downloading {normalized}.json from GitHub...")
                Dim json = Await _httpClient.GetStringAsync(url)

                ' Validate it's valid JSON
                Using doc = JsonDocument.Parse(json)
                    If doc.RootElement.ValueKind <> JsonValueKind.Object Then
                        AppLogger.Log($"[LanguagePack] Downloaded file is not a valid locale JSON")
                        Return False
                    End If
                End Using

                File.WriteAllText(localPath, json, Text.Encoding.UTF8)
                AppLogger.Log($"[LanguagePack] Downloaded {normalized}.json successfully")
                Return True
            Catch ex As Exception
                AppLogger.Log($"[LanguagePack] Download failed for {normalized}: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Downloads and loads a language pack. If download fails, stays on current language.
        ''' </summary>
        Public Async Function EnsureLanguageAsync(langCode As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Dim normalized = NormalizeLangCode(langCode)
            Dim localPath = Path.Combine(_localesDir, $"{normalized}.json")

            If Not File.Exists(localPath) AndAlso Not normalized.Equals("en", StringComparison.OrdinalIgnoreCase) Then
                Dim ok = Await DownloadLanguageAsync(normalized, ct)
                If Not ok Then Return False
            End If

            LoadLanguage(normalized)
            Return True
        End Function

        ''' <summary>
        ''' Returns list of locally available language codes (based on files in locales/).
        ''' </summary>
        Public Function GetAvailableLanguages() As List(Of String)
            Dim result As New List(Of String)()
            If Not Directory.Exists(_localesDir) Then Return result
            For Each f In Directory.GetFiles(_localesDir, "*.json")
                Dim name = Path.GetFileNameWithoutExtension(f)
                result.Add(name)
            Next
            result.Sort()
            Return result
        End Function

        ''' <summary>
        ''' Returns available languages with display names from language-codes.json.
        ''' </summary>
        Public Function GetAvailableLanguagesWithNames() As List(Of (Code As String, Name As String, Native As String))
            Dim result As New List(Of (Code As String, Name As String, Native As String))()
            Dim langService = LanguageCodeService.Instance
            For Each code In GetAvailableLanguages()
                Dim nllb = langService.ToNllb(code)
                Dim displayName = If(Not String.IsNullOrEmpty(nllb), langService.GetDisplayName(nllb), "")
                Dim nativeName = If(Not String.IsNullOrEmpty(nllb), langService.GetNativeName(nllb), "")
                If String.IsNullOrEmpty(displayName) Then
                    Try
                        Dim ci = Globalization.CultureInfo.GetCultureInfo(code)
                        displayName = ci.EnglishName
                        nativeName = ci.NativeName
                    Catch
                        displayName = code
                        nativeName = code
                    End Try
                End If
                result.Add((code, displayName, nativeName))
            Next
            Return result
        End Function

        ' ── Private helpers ──

        Private Shared Sub LoadJsonInto(filePath As String, dict As Dictionary(Of String, String))
            Try
                Dim json = File.ReadAllText(filePath, Text.Encoding.UTF8)
                Using doc = JsonDocument.Parse(json)
                    For Each prop In doc.RootElement.EnumerateObject()
                        dict(prop.Name) = prop.Value.GetString()
                    Next
                End Using
            Catch ex As Exception
                AppLogger.Log($"[LanguagePack] Failed to load {filePath}: {ex.Message}")
            End Try
        End Sub

        Private Shared Function NormalizeLangCode(code As String) As String
            If String.IsNullOrEmpty(code) Then Return "en"
            ' "zh-Hans" -> "zh", "pt-BR" -> "pt"
            Dim dash = code.IndexOf("-"c)
            If dash > 0 Then Return code.Substring(0, dash).ToLowerInvariant()
            Return code.ToLowerInvariant()
        End Function

    End Class

End Namespace
