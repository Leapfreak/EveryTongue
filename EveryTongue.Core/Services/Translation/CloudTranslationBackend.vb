Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation
    ''' <summary>
    ''' Base class for cloud translation backends (DeepL, Google, Azure).
    ''' Provides shared HttpClient, API key validation, and usage tracking.
    ''' </summary>
    Public MustInherit Class CloudTranslationBackend
        Implements ITranslationBackend

        Protected ReadOnly HttpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(10)
        }

        Protected Property ApiKey As String = ""
        Protected Property Endpoint As String = ""
        Protected Property CharactersUsed As Long = 0

        Public MustOverride ReadOnly Property Name As String Implements ITranslationBackend.Name

        Public ReadOnly Property RequiresInternet As Boolean Implements ITranslationBackend.RequiresInternet
            Get
                Return True
            End Get
        End Property

        Public Overridable ReadOnly Property IsAvailable As Boolean Implements ITranslationBackend.IsAvailable
            Get
                Return Not String.IsNullOrEmpty(ApiKey)
            End Get
        End Property

        ''' <summary>
        ''' Cloud APIs return raw vendor output — the orchestrator applies local
        ''' glossary post-processing so cloud results match the NLLB sidecar.
        ''' </summary>
        Public ReadOnly Property AppliesFiltersInternally As Boolean Implements ITranslationBackend.AppliesFiltersInternally
            Get
                Return False
            End Get
        End Property

        Public Overridable Sub Configure(apiKey As String)
            Me.ApiKey = If(apiKey, "")
        End Sub

        ''' <summary>
        ''' Push the per-engine endpoint (URL, or region name for engines that use
        ''' regions) into the backend. No-op for engines that have a fixed endpoint;
        ''' backends with RequiresEndpoint registry entries override or use the
        ''' stored Endpoint value. Trailing slashes are trimmed so callers can
        ''' concatenate paths safely.
        ''' </summary>
        Public Overridable Sub ConfigureEndpoint(url As String)
            Me.Endpoint = If(url, "").Trim().TrimEnd("/"c)
        End Sub

        Public MustOverride Function TranslateAsync(text As String,
                                                     sourceLang As String,
                                                     targetLangs As IReadOnlyList(Of String),
                                                     ct As CancellationToken,
                                                     Optional noCache As Boolean = False,
                                                     Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationBackend.TranslateAsync

        Public MustOverride Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo)) Implements ITranslationBackend.GetSupportedLanguagesAsync

        Public MustOverride Function CheckHealthAsync(ct As CancellationToken
        ) As Task(Of Boolean) Implements ITranslationBackend.CheckHealthAsync
    End Class

    ''' <summary>
    ''' DeepL translation backend. Requires API key from deepl.com.
    ''' </summary>
    Public Class DeepLBackend
        Inherits CloudTranslationBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "DeepL"
            End Get
        End Property

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                     Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            ' Streaming commits arrive as one short text per request, so the only
            ' parallelism available is across target languages — issue the
            ' per-target requests concurrently (bounded) instead of sequentially.
            ' Results land in a per-index array so dictionary order matches the
            ' caller's target order.
            Dim translatedByIndex(targetLangs.Count - 1) As String
            Dim gate As New SemaphoreSlim(4)
            Dim tasks As New List(Of Task)()

            For i = 0 To targetLangs.Count - 1
                Dim idx = i                       ' capture for closure
                Dim targetLang = targetLangs(i)
                tasks.Add(Task.Run(Async Function()
                    Await gate.WaitAsync(ct)
                    Try
                        ' DeepL needs ITS OWN codes (CA/EN/…), not FLORES — sending
                        ' "CAT_LATN" was rejected and silently swallowed, so every
                        ' request fell back to the local engine wearing DeepL's label.
                        Dim dlTarget = Services.Infrastructure.LanguageCodeService.Instance.FloresToDeepL(targetLang)
                        If String.IsNullOrEmpty(dlTarget) Then
                            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                                $"DeepLBackend: no DeepL code for target '{targetLang}' — skipped")
                            Return
                        End If
                        Dim form As New Dictionary(Of String, String) From {
                            {"text", text},
                            {"target_lang", dlTarget.ToUpper()}
                        }
                        ' Unmapped source → omit and let DeepL auto-detect.
                        Dim dlSource = Services.Infrastructure.LanguageCodeService.Instance.FloresToDeepL(sourceLang)
                        If Not String.IsNullOrEmpty(dlSource) Then form("source_lang") = dlSource.ToUpper()
                        ' DeepL dropped form-body auth_key ("legacy authentication") —
                        ' the key must travel as an Authorization header.
                        Dim req As New HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate")
                        req.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {ApiKey}")
                        req.Content = New FormUrlEncodedContent(form)
                        Dim response = Await HttpClient.SendAsync(req, ct)
                        If response.IsSuccessStatusCode Then
                            Dim body = Await response.Content.ReadAsStringAsync()
                            Using doc = JsonDocument.Parse(body)
                                translatedByIndex(idx) = doc.RootElement.
                                    GetProperty("translations")(0).
                                    GetProperty("text").GetString()
                            End Using
                        Else
                            ' A rejected request must be VISIBLE — silence here masked
                            ' the FLORES-code bug behind the orchestrator's fallback.
                            Dim errBody = Await response.Content.ReadAsStringAsync()
                            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                                $"DeepLBackend: HTTP {CInt(response.StatusCode)} for {If(dlSource, "auto")}→{dlTarget}: {If(errBody, "").Substring(0, Math.Min(120, If(errBody, "").Length))}")
                        End If
                    Catch ex As OperationCanceledException
                    Catch ex As Exception
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR, $"DeepLBackend.TranslateAsync: target={targetLang} - {ex.Message}")
                    Finally
                        gate.Release()
                    End Try
                End Function))
            Next

            Await Task.WhenAll(tasks)

            Dim results As New Dictionary(Of String, String)()
            For i = 0 To targetLangs.Count - 1
                If translatedByIndex(i) IsNot Nothing Then
                    results(targetLangs(i)) = translatedByIndex(i)
                    CharactersUsed += text.Length
                End If
            Next
            Return results
        End Function

        Public Overrides Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))
            Return Task.FromResult(DirectCast(New List(Of LanguageInfo)(), IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Try
                ' Header-based auth (DeepL dropped legacy auth_key-in-URL/body).
                Dim req As New HttpRequestMessage(HttpMethod.Get, "https://api-free.deepl.com/v2/usage")
                req.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {ApiKey}")
                Dim response = Await HttpClient.SendAsync(req, ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR, $"DeepLBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

    ''' <summary>
    ''' Google Cloud Translation backend. Requires API key.
    ''' </summary>
    Public Class GoogleBackend
        Inherits CloudTranslationBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Google"
            End Get
        End Property

        ''' <summary>
        ''' Convert a FLORES code (e.g. "cat_Latn") to a Google Translate ISO code (e.g. "ca").
        ''' Falls back to extracting the ISO 639-3 prefix from the FLORES code.
        ''' </summary>
        Private Shared Function ToGoogleCode(floresCode As String) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim code = FloresToVendorIso(floresCode)
            If Not String.IsNullOrEmpty(code) Then Return code
            ' Fallback: extract ISO 639-3 prefix (e.g. "cat_Latn" -> "cat")
            Dim underscore = floresCode.IndexOf("_"c)
            Return If(underscore > 0, floresCode.Substring(0, underscore), floresCode)
        End Function

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                     Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            ' Convert FLORES codes to Google Translate codes
            Dim googleSource = ToGoogleCode(sourceLang)

            ' Launch all target translations in parallel for speed
            Dim results As New Dictionary(Of String, String)()
            Dim tasks As New List(Of Task)()
            Dim url = $"https://translation.googleapis.com/language/translate/v2?key={ApiKey}"

            For Each targetLang In targetLangs
                Dim tl = targetLang  ' capture for closure
                Dim googleTarget = ToGoogleCode(tl)
                If String.IsNullOrEmpty(googleTarget) Then Continue For

                tasks.Add(Task.Run(Async Function()
                    Try
                        Dim requestBody = $"{{""q"":{EscapeJson(text)},""source"":""{googleSource}"",""target"":""{googleTarget}"",""format"":""text""}}"
                        Dim content As New StringContent(requestBody, Encoding.UTF8, "application/json")
                        Dim response = Await HttpClient.PostAsync(url, content, ct)
                        If response.IsSuccessStatusCode Then
                            Dim body = Await response.Content.ReadAsStringAsync()
                            Using doc = JsonDocument.Parse(body)
                                Dim translated = doc.RootElement.
                                    GetProperty("data").
                                    GetProperty("translations")(0).
                                    GetProperty("translatedText").GetString()
                                SyncLock results
                                    results(tl) = translated
                                End SyncLock
                                Interlocked.Add(CharactersUsed, text.Length)
                            End Using
                        Else
                            Dim errBody = Await response.Content.ReadAsStringAsync()
                            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                                $"GoogleBackend: {response.StatusCode} for {googleSource}->{googleTarget}: {errBody}")
                        End If
                    Catch ex As OperationCanceledException
                    Catch ex As Exception
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR, $"GoogleBackend.TranslateAsync: target={tl} - {ex.Message}")
                    End Try
                End Function))
            Next

            Await Task.WhenAll(tasks)
            Return results
        End Function

        Public Overrides Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))
            Return Task.FromResult(DirectCast(New List(Of LanguageInfo)(), IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Overrides Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            Return Task.FromResult(IsAvailable)
        End Function
    End Class

    ''' <summary>
    ''' Azure Cognitive Services Translator backend. Requires API key + region.
    ''' </summary>
    Public Class AzureBackend
        Inherits CloudTranslationBackend

        Public Property Region As String = "global"

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Azure"
            End Get
        End Property

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                     Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim results As New Dictionary(Of String, String)()
            Try
                ' Azure needs ITS OWN codes (en/es/…), not FLORES — same class of bug
                ' as the DeepL one: raw "cat_Latn" was rejected silently. Map the
                ' targets AND remember azure→flores so results come back keyed the
                ' way the caller (orchestrator) expects.
                Dim svc = Services.Infrastructure.LanguageCodeService.Instance
                Dim azToFlores As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                For Each tl In targetLangs
                    Dim az = svc.FloresToAzure(tl)
                    If String.IsNullOrEmpty(az) Then
                        Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                            $"AzureBackend: no Azure code for target '{tl}' — skipped")
                    Else
                        azToFlores(az) = tl
                    End If
                Next
                If azToFlores.Count = 0 Then Return results
                ' Azure supports multiple target languages in one call; unmapped
                ' source → omit &from= and let Azure auto-detect.
                Dim targetParams = String.Join("", azToFlores.Keys.Select(Function(az) $"&to={az}"))
                Dim azSource = svc.FloresToAzure(sourceLang)
                Dim fromParam = If(String.IsNullOrEmpty(azSource), "", $"&from={azSource}")
                Dim url = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0{fromParam}{targetParams}"

                Dim requestBody = $"[{{""Text"":{EscapeJson(text)}}}]"
                Dim content As New StringContent(requestBody, Encoding.UTF8, "application/json")

                Dim request As New HttpRequestMessage(HttpMethod.Post, url)
                request.Content = content
                request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey)
                request.Headers.Add("Ocp-Apim-Subscription-Region", Region)

                Dim response = Await HttpClient.SendAsync(request, ct)
                If response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync()
                    Using doc = JsonDocument.Parse(body)
                        Dim translations = doc.RootElement(0).GetProperty("translations")
                        For Each trans In translations.EnumerateArray()
                            Dim toLang = trans.GetProperty("to").GetString()
                            Dim translated = trans.GetProperty("text").GetString()
                            ' Key by the FLORES code the caller asked for.
                            Dim flores As String = Nothing
                            results(If(azToFlores.TryGetValue(toLang, flores), flores, toLang)) = translated
                        Next
                        CharactersUsed += text.Length * azToFlores.Count
                    End Using
                Else
                    Dim errBody = Await response.Content.ReadAsStringAsync()
                    Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                        $"AzureBackend: HTTP {CInt(response.StatusCode)}: {If(errBody, "").Substring(0, Math.Min(120, If(errBody, "").Length))}")
                End If
            Catch ex As OperationCanceledException
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR, $"AzureBackend.TranslateAsync: {ex.Message}")
            End Try
            Return results
        End Function

        Public Overrides Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))
            Return Task.FromResult(DirectCast(New List(Of LanguageInfo)(), IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Overrides Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            Return Task.FromResult(IsAvailable)
        End Function
    End Class

    ' Shared helper
    Module CloudTranslationHelper
        Friend Function EscapeJson(s As String) As String
            Return Pipeline.ProcessHelper.EscapeJson(s)
        End Function

        ''' <summary>
        ''' Shared FLORES → vendor ISO mapping used by all cloud backends that
        ''' speak ISO 639-1-style codes (Google, LibreTranslate, Amazon).
        ''' Tries the language table's google column first (carries regional
        ''' variants like "zh-TW"), then plain ISO 639-1. Returns "" when the
        ''' table has no mapping — callers decide their own fallback (Google
        ''' tries the ISO 639-3 prefix; Amazon/LibreTranslate skip the target).
        ''' When <paramref name="stripRegion"/> is True, regional suffixes are
        ''' removed ("zh-TW" → "zh") for vendors that only accept bare codes.
        ''' </summary>
        Friend Function FloresToVendorIso(floresCode As String, Optional stripRegion As Boolean = False) As String
            If String.IsNullOrEmpty(floresCode) Then Return ""
            Dim svc = Infrastructure.LanguageCodeService.Instance
            Dim code = svc.FloresToGoogle(floresCode)
            If String.IsNullOrEmpty(code) Then code = svc.FloresToIso1(floresCode)
            If String.IsNullOrEmpty(code) Then Return ""
            If stripRegion Then
                Dim dash = code.IndexOf("-"c)
                If dash > 0 Then code = code.Substring(0, dash)
            End If
            Return code
        End Function
    End Module
End Namespace
