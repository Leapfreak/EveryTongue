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

        ''' <summary>
        ''' Send a request, retrying on HTTP 429 with exponential backoff
        ''' (2s/4s/8s, or the server's Retry-After when present). Without this,
        ''' rate-limit bursts (DeepL free tier especially) surface as failures
        ''' and trigger the orchestrator's Local fallback — silently replacing
        ''' this engine's output with another engine's in benchmarks and shadow
        ''' comparisons. makeRequest must build a FRESH HttpRequestMessage per
        ''' attempt (requests are single-use).
        ''' </summary>
        Protected Async Function SendWithRetryAsync(makeRequest As Func(Of HttpRequestMessage),
                                                    ct As CancellationToken) As Task(Of HttpResponseMessage)
            Dim attempt = 0
            Do
                Dim response = Await HttpClient.SendAsync(makeRequest(), ct)
                If CInt(response.StatusCode) <> 429 OrElse attempt >= 3 Then Return response
                Dim waitSeconds = If(response.Headers.RetryAfter?.Delta?.TotalSeconds, 2.0 * Math.Pow(2, attempt))
                Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_CLOUD_REQUEST,
                    $"{Name}: HTTP 429 rate-limited — backing off {waitSeconds:F0}s (attempt {attempt + 1}/3)")
                response.Dispose()
                Await Task.Delay(TimeSpan.FromSeconds(waitSeconds), ct)
                attempt += 1
            Loop
        End Function

        Public MustOverride Function TranslateAsync(text As String,
                                                     sourceLang As String,
                                                     targetLangs As IReadOnlyList(Of String),
                                                     ct As CancellationToken,
                                                     Optional noCache As Boolean = False,
                                                     Optional filters As TranslationFilterPaths = Nothing,
                                                     Optional context As TranslationContext = Nothing
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
                                                     Optional filters As TranslationFilterPaths = Nothing,
                                                     Optional context As TranslationContext = Nothing
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
                        ' custom_instructions is a form ARRAY (repeated keys), which a
                        ' Dictionary cannot express — the form is a KeyValuePair list.
                        Dim form As New List(Of KeyValuePair(Of String, String)) From {
                            New KeyValuePair(Of String, String)("text", text),
                            New KeyValuePair(Of String, String)("target_lang", dlTarget.ToUpper())
                        }
                        ' Unmapped source → omit and let DeepL auto-detect.
                        Dim dlSource = Services.Infrastructure.LanguageCodeService.Instance.FloresToDeepL(sourceLang)
                        If Not String.IsNullOrEmpty(dlSource) Then form.Add(New KeyValuePair(Of String, String)("source_lang", dlSource.ToUpper()))

                        ' Rolling window of prior source sentences → DeepL's context
                        ' parameter ("surrounding document content, not commands" —
                        ' not translated, not billed). Excludes the current sentence.
                        Dim ctxChars = 0
                        If context?.Sentences IsNot Nothing AndAlso context.Sentences.Count > 0 Then
                            Dim ctxText = String.Join(" ", context.Sentences.Select(Function(s) s.SourceText))
                            If ctxText.Length > 0 Then
                                form.Add(New KeyValuePair(Of String, String)("context", ctxText))
                                ctxChars = ctxText.Length
                            End If
                        End If

                        ' Terminology → custom_instructions ("Translate 'X' as 'Y'").
                        ' Vendor constraint: instructions are only accepted for these
                        ' target-language families (variants like EN-GB qualify).
                        Dim instructions As New List(Of String)()
                        Dim baseTarget = dlTarget.Split("-"c)(0).ToLowerInvariant()
                        If context?.Terminology IsNot Nothing AndAlso context.Terminology.Count > 0 AndAlso
                           _instructionTargets.Contains(baseTarget) Then
                            For Each term In context.Terminology
                                Dim rendering As String = Nothing
                                If Not term.Translations.TryGetValue(targetLang, rendering) Then
                                    term.Translations.TryGetValue(baseTarget, rendering)
                                End If
                                If String.IsNullOrWhiteSpace(rendering) Then Continue For
                                ' "instrText", not "instr" — InStr is a VB built-in and wins name resolution.
                                Dim instrText = $"Translate '{term.Term}' as '{rendering}'"
                                If instrText.Length <= 300 Then instructions.Add(instrText)
                                If instructions.Count >= 10 Then Exit For
                            Next
                            For Each instrText In instructions
                                form.Add(New KeyValuePair(Of String, String)("custom_instructions", instrText))
                            Next
                        End If

                        If ctxChars > 0 OrElse instructions.Count > 0 Then
                            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_CLOUD_REQUEST,
                                $"DeepL →{dlTarget}: context={ctxChars}ch instructions={instructions.Count}")
                        End If

                        Dim res = Await SendDeepLRequestAsync(form, dlSource, dlTarget, ct)
                        If res.TranslatedText Is Nothing AndAlso res.Status = 400 AndAlso instructions.Count > 0 Then
                            ' Fail-safe: custom_instructions rejected (unsupported pair /
                            ' tier / wire format) — retry once without them. context is a
                            ' GA parameter and stays. Never costs the caption.
                            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_CONTEXT,
                                $"DeepLBackend: HTTP 400 with custom_instructions for →{dlTarget} — retrying without instructions")
                            Dim retryForm = form.Where(Function(kv) kv.Key <> "custom_instructions").ToList()
                            res = Await SendDeepLRequestAsync(retryForm, dlSource, dlTarget, ct)
                        End If
                        If res.TranslatedText IsNot Nothing Then translatedByIndex(idx) = res.TranslatedText
                    Catch ex As OperationCanceledException
                        ' Cancelled — normal; the Exception branch below logs real failures.
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

        ''' <summary>
        ''' DeepL custom_instructions vendor constraint: the API accepts instructions
        ''' only for these target-language families (docs 2026-08). A vendor fact,
        ''' not a language list — allowlisted in audit-language-lists with this reason.
        ''' </summary>
        Private Shared ReadOnly _instructionTargets As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
            "de", "en", "es", "fr", "it", "ja", "ko", "zh"
        }

        ''' <summary>
        ''' One DeepL /v2/translate POST + parse, shared by the normal path and the
        ''' retry-without-instructions path. Returns (HTTP status, translated text or
        ''' Nothing). Failures are logged here — a rejected request must be VISIBLE
        ''' (silence once masked the FLORES-code bug behind the orchestrator fallback).
        ''' </summary>
        Private Async Function SendDeepLRequestAsync(form As List(Of KeyValuePair(Of String, String)),
                                                     dlSource As String, dlTarget As String,
                                                     ct As CancellationToken
        ) As Task(Of (Status As Integer, TranslatedText As String))
            ' DeepL dropped form-body auth_key ("legacy authentication") —
            ' the key must travel as an Authorization header.
            Dim response = Await SendWithRetryAsync(
                Function()
                    Dim req As New HttpRequestMessage(HttpMethod.Post, "https://api-free.deepl.com/v2/translate")
                    req.Headers.TryAddWithoutValidation("Authorization", $"DeepL-Auth-Key {ApiKey}")
                    req.Content = New FormUrlEncodedContent(form)
                    Return req
                End Function, ct)
            If response.IsSuccessStatusCode Then
                Dim body = Await response.Content.ReadAsStringAsync()
                Using doc = JsonDocument.Parse(body)
                    Return (CInt(response.StatusCode), doc.RootElement.
                        GetProperty("translations")(0).
                        GetProperty("text").GetString())
                End Using
            End If
            Dim errBody = Await response.Content.ReadAsStringAsync()
            Services.Infrastructure.AppLogger.Log(Services.Infrastructure.LogEvents.TRANS_ERROR,
                $"DeepLBackend: HTTP {CInt(response.StatusCode)} for {If(dlSource, "auto")}→{dlTarget}: {If(errBody, "").Substring(0, Math.Min(120, If(errBody, "").Length))}")
            Return (CInt(response.StatusCode), Nothing)
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
                                                     Optional filters As TranslationFilterPaths = Nothing,
                                                     Optional context As TranslationContext = Nothing
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
                        Dim response = Await SendWithRetryAsync(
                            Function()
                                Dim req As New HttpRequestMessage(HttpMethod.Post, url)
                                req.Content = New StringContent(requestBody, Encoding.UTF8, "application/json")
                                Return req
                            End Function, ct)
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
                        ' Cancelled — normal; the Exception branch below logs real failures.
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
                                                     Optional filters As TranslationFilterPaths = Nothing,
                                                     Optional context As TranslationContext = Nothing
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

                Dim response = Await SendWithRetryAsync(
                    Function()
                        Dim request As New HttpRequestMessage(HttpMethod.Post, url)
                        request.Content = New StringContent(requestBody, Encoding.UTF8, "application/json")
                        request.Headers.Add("Ocp-Apim-Subscription-Key", ApiKey)
                        request.Headers.Add("Ocp-Apim-Subscription-Region", Region)
                        Return request
                    End Function, ct)
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
                ' Cancelled — normal; the Exception branch below logs real failures.
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
