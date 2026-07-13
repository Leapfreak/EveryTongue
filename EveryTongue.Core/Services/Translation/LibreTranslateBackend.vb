Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation

    ''' <summary>
    ''' LibreTranslate backend. Works against the hosted libretranslate.com
    ''' instance (API key required) or any self-hosted instance (key optional) —
    ''' the endpoint comes from the per-engine endpoint setting. Speaks ISO 639-1
    ''' codes via the shared FLORES→vendor-ISO mapping.
    ''' </summary>
    Public Class LibreTranslateBackend
        Inherits CloudTranslationBackend

        Public Overrides ReadOnly Property Name As String
            Get
                Return "LibreTranslate"
            End Get
        End Property

        ''' <summary>
        ''' Available when an endpoint is resolved (config value or registry
        ''' default, pushed via ConfigureEndpoint). The API key is optional —
        ''' self-hosted instances are typically keyless.
        ''' </summary>
        Public Overrides ReadOnly Property IsAvailable As Boolean
            Get
                Return Not String.IsNullOrEmpty(Endpoint)
            End Get
        End Property

        ''' <summary>
        ''' FLORES → LibreTranslate code (bare ISO 639-1; regional variants like
        ''' "zh-TW" are stripped to the base code). "" when unmapped.
        ''' </summary>
        Private Shared Function ToLibreCode(floresCode As String) As String
            Return FloresToVendorIso(floresCode, stripRegion:=True)
        End Function

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                        Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim libreSource = ToLibreCode(sourceLang)
            If String.IsNullOrEmpty(libreSource) Then
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"LibreTranslateBackend: no ISO 639-1 mapping for source '{sourceLang}' — skipping request")
                Return New Dictionary(Of String, String)()
            End If

            Dim results As New Dictionary(Of String, String)()
            Dim gate As New SemaphoreSlim(4)
            Dim tasks As New List(Of Task)()
            Dim url = $"{Endpoint}/translate"

            For Each targetLang In targetLangs
                Dim tl = targetLang  ' capture for closure
                Dim libreTarget = ToLibreCode(tl)
                If String.IsNullOrEmpty(libreTarget) Then
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"LibreTranslateBackend: no ISO 639-1 mapping for target '{tl}' — skipping target")
                    Continue For
                End If

                tasks.Add(Task.Run(Async Function()
                    Await gate.WaitAsync(ct)
                    Try
                        Dim payload As New Dictionary(Of String, Object) From {
                            {"q", text},
                            {"source", libreSource},
                            {"target", libreTarget},
                            {"format", "text"}
                        }
                        If Not String.IsNullOrEmpty(ApiKey) Then payload("api_key") = ApiKey

                        Dim content As New StringContent(
                            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                        Dim response = Await HttpClient.PostAsync(url, content, ct)
                        If response.IsSuccessStatusCode Then
                            Dim body = Await response.Content.ReadAsStringAsync()
                            Using doc = JsonDocument.Parse(body)
                                Dim translated = doc.RootElement.
                                    GetProperty("translatedText").GetString()
                                SyncLock results
                                    results(tl) = translated
                                End SyncLock
                                Interlocked.Add(CharactersUsed, text.Length)
                            End Using
                        Else
                            Dim errBody = Await response.Content.ReadAsStringAsync()
                            AppLogger.Log(LogEvents.TRANS_ERROR,
                                $"LibreTranslateBackend: {response.StatusCode} for {libreSource}->{libreTarget}: {errBody}")
                        End If
                    Catch ex As OperationCanceledException
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.TRANS_ERROR,
                            $"LibreTranslateBackend.TranslateAsync: target={tl} - {ex.Message}")
                    Finally
                        gate.Release()
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

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Try
                Dim response = Await HttpClient.GetAsync($"{Endpoint}/languages", ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"LibreTranslateBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

End Namespace
