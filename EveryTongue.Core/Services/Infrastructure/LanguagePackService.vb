Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading

Namespace Services.Infrastructure

    ''' <summary>
    ''' JSON-based localization service. Two locale directories, mirroring the
    ''' Bibles pattern: the CONFIG directory's locales\ (user/volume packs -
    ''' survive app and Docker-image updates, checked FIRST) and the app
    ''' directory's locales\ (packs shipped with the build). Every load starts
    ''' from embedded English and OVERLAYS the pack, so a partial pack shows
    ''' English for missing keys instead of raw key names. English is always
    ''' built-in; other languages are downloaded (into the config dir) or
    ''' generated via translation.
    ''' </summary>
    Public Class LanguagePackService

        Private Shared _instance As LanguagePackService
        Private Shared ReadOnly _lock As New Object()

        Private Const GitHubBaseUrl As String = "https://raw.githubusercontent.com/LeapFreak/EveryTongue/main/locales/"

        Private ReadOnly _localesDir As String
        Private ReadOnly _userLocalesDir As String
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
            ' Config-dir packs (Docker: the /config volume; desktop: %APPDATA%\EveryTongue).
            Try
                _userLocalesDir = Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory, "locales")
            Catch
                _userLocalesDir = ""
            End Try
        End Sub

        ''' <summary>User/volume pack path first, then the app-shipped pack; Nothing if neither exists.</summary>
        Private Function ResolveLangPath(normalized As String) As String
            If Not String.IsNullOrEmpty(_userLocalesDir) Then
                Dim userPath = Path.Combine(_userLocalesDir, $"{normalized}.json")
                If File.Exists(userPath) Then Return userPath
            End If
            Dim appPath = Path.Combine(_localesDir, $"{normalized}.json")
            If File.Exists(appPath) Then Return appPath
            Return Nothing
        End Function

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

            ' English base first, then overlay the pack (config-dir wins over
            ' app-dir) - a partial pack degrades to English, never to key names.
            LoadEmbeddedEnglish(_strings)
            Dim langPath = ResolveLangPath(normalized)
            If langPath IsNot Nothing Then
                LoadJsonInto(langPath, _strings)
            End If

            _currentLang = normalized
            AppLogger.Log(LogEvents.LOCALE_LOADED,
                $"Loaded '{normalized}' with {_strings.Count} keys" &
                If(langPath IsNot Nothing, $" (pack: {langPath})", " (embedded English)"))
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
        ''' Gets web.* strings for a specific language (for per-client locale serving).
        ''' Loads the requested locale file on the fly without changing the server's current language.
        ''' Falls back to the server's current language if the requested locale is unavailable.
        ''' </summary>
        Public Function GetWebStringsForLanguage(langCode As String) As Dictionary(Of String, String)
            If String.IsNullOrEmpty(langCode) Then Return GetWebStrings()
            Dim normalized = NormalizeLangCode(langCode)

            ' If it's the same as the current server language, just return current strings
            If normalized.Equals(_currentLang, StringComparison.OrdinalIgnoreCase) Then
                Return GetWebStrings()
            End If

            ' Try to load the requested locale file (config-dir pack wins)
            Dim langPath = ResolveLangPath(normalized)
            If langPath Is Nothing Then
                Return GetWebStrings() ' Fall back to server language
            End If

            ' English base + overlay so partial packs still return complete web strings
            Dim tempStrings As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            LoadEmbeddedEnglish(tempStrings)
            LoadJsonInto(langPath, tempStrings)

            Dim result As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For Each kvp In tempStrings
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
            ' Download into the CONFIG-dir packs (writable; survives app/image updates).
            Dim targetDir = If(String.IsNullOrEmpty(_userLocalesDir), _localesDir, _userLocalesDir)
            If Not Directory.Exists(targetDir) Then Directory.CreateDirectory(targetDir)
            Dim localPath = Path.Combine(targetDir, $"{normalized}.json")

            Try
                AppLogger.Log(LogEvents.LOCALE_LOADED, $"Downloading {normalized}.json from GitHub...")
                Dim json = Await _httpClient.GetStringAsync(url)

                ' Validate it's valid JSON
                Using doc = JsonDocument.Parse(json)
                    If doc.RootElement.ValueKind <> JsonValueKind.Object Then
                        AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"Downloaded file is not a valid locale JSON")
                        Return False
                    End If
                End Using

                File.WriteAllText(localPath, json, Text.Encoding.UTF8)
                AppLogger.Log(LogEvents.LOCALE_LOADED, $"Downloaded {normalized}.json successfully")
                Return True
            Catch ex As Exception
                AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"Download failed for {normalized}: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Downloads and loads a language pack. If download fails, stays on current language.
        ''' </summary>
        Public Async Function EnsureLanguageAsync(langCode As String, Optional ct As CancellationToken = Nothing) As Task(Of Boolean)
            Dim normalized = NormalizeLangCode(langCode)

            If ResolveLangPath(normalized) Is Nothing AndAlso Not normalized.Equals("en", StringComparison.OrdinalIgnoreCase) Then
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
            Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each dirPath In {_userLocalesDir, _localesDir}
                If String.IsNullOrEmpty(dirPath) OrElse Not Directory.Exists(dirPath) Then Continue For
                For Each f In Directory.GetFiles(dirPath, "*.json")
                    seen.Add(Path.GetFileNameWithoutExtension(f))
                Next
            Next
            Dim result = seen.ToList()
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
                Dim flores = langService.ToFlores(code)
                Dim displayName = If(Not String.IsNullOrEmpty(flores), langService.GetDisplayName(flores), "")
                Dim nativeName = If(Not String.IsNullOrEmpty(flores), langService.GetNativeName(flores), "")
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
                AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"Failed to load {filePath}: {ex.Message}")
            End Try
        End Sub

        Private Shared Sub LoadEmbeddedEnglish(dict As Dictionary(Of String, String))
            Try
                Dim asm = Reflection.Assembly.GetExecutingAssembly()
                Dim stream = asm.GetManifestResourceStream("EveryTongue.Assets.en.json")
                If stream Is Nothing Then
                    AppLogger.Log(LogEvents.LOCALE_FALLBACK, "Embedded en.json not found")
                    Return
                End If
                Using reader As New IO.StreamReader(stream, Text.Encoding.UTF8)
                    Dim json = reader.ReadToEnd()
                    Using doc = JsonDocument.Parse(json)
                        For Each prop In doc.RootElement.EnumerateObject()
                            dict(prop.Name) = prop.Value.GetString()
                        Next
                    End Using
                End Using
                AppLogger.Log(LogEvents.LOCALE_LOADED, $"Loaded embedded en.json with {dict.Count} keys")
            Catch ex As Exception
                AppLogger.Log(LogEvents.LOCALE_FALLBACK, $"Failed to load embedded en.json: {ex.Message}")
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
