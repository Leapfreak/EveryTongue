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
        Protected Property CharactersUsed As Long = 0

        Public MustOverride ReadOnly Property Name As String Implements ITranslationBackend.Name

        Public ReadOnly Property RequiresInternet As Boolean Implements ITranslationBackend.RequiresInternet
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsAvailable As Boolean Implements ITranslationBackend.IsAvailable
            Get
                Return Not String.IsNullOrEmpty(ApiKey)
            End Get
        End Property

        Public Sub Configure(apiKey As String)
            Me.ApiKey = If(apiKey, "")
        End Sub

        Public MustOverride Function TranslateAsync(text As String,
                                                     sourceLang As String,
                                                     targetLangs As IReadOnlyList(Of String),
                                                     ct As CancellationToken,
                                                     Optional noCache As Boolean = False
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
                                                        Optional noCache As Boolean = False
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim results As New Dictionary(Of String, String)()
            For Each targetLang In targetLangs
                Try
                    Dim content As New FormUrlEncodedContent(New Dictionary(Of String, String) From {
                        {"auth_key", ApiKey},
                        {"text", text},
                        {"source_lang", sourceLang.ToUpper()},
                        {"target_lang", targetLang.ToUpper()}
                    })
                    Dim response = Await HttpClient.PostAsync(
                        "https://api-free.deepl.com/v2/translate", content, ct)
                    If response.IsSuccessStatusCode Then
                        Dim body = Await response.Content.ReadAsStringAsync()
                        Using doc = JsonDocument.Parse(body)
                            Dim translated = doc.RootElement.
                                GetProperty("translations")(0).
                                GetProperty("text").GetString()
                            results(targetLang) = translated
                            CharactersUsed += text.Length
                        End Using
                    End If
                Catch ex As OperationCanceledException
                    Exit For
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log($"[ERROR] DeepLBackend.TranslateAsync: target={targetLang} - {ex.Message}")
                End Try
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
                Dim response = Await HttpClient.GetAsync(
                    $"https://api-free.deepl.com/v2/usage?auth_key={ApiKey}", ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] DeepLBackend.CheckHealthAsync: {ex.Message}")
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

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim results As New Dictionary(Of String, String)()
            For Each targetLang In targetLangs
                Try
                    Dim requestBody = $"{{""q"":{EscapeJson(text)},""source"":""{sourceLang}"",""target"":""{targetLang}"",""format"":""text""}}"
                    Dim content As New StringContent(requestBody, Encoding.UTF8, "application/json")
                    Dim response = Await HttpClient.PostAsync(
                        $"https://translation.googleapis.com/language/translate/v2?key={ApiKey}",
                        content, ct)
                    If response.IsSuccessStatusCode Then
                        Dim body = Await response.Content.ReadAsStringAsync()
                        Using doc = JsonDocument.Parse(body)
                            Dim translated = doc.RootElement.
                                GetProperty("data").
                                GetProperty("translations")(0).
                                GetProperty("translatedText").GetString()
                            results(targetLang) = translated
                            CharactersUsed += text.Length
                        End Using
                    End If
                Catch ex As OperationCanceledException
                    Exit For
                Catch ex As Exception
                    Services.Infrastructure.AppLogger.Log($"[ERROR] GoogleBackend.TranslateAsync: target={targetLang} - {ex.Message}")
                End Try
            Next
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
                                                        Optional noCache As Boolean = False
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim results As New Dictionary(Of String, String)()
            Try
                ' Azure supports multiple target languages in one call
                Dim targetParams = String.Join("", targetLangs.Select(Function(tl) $"&to={tl}"))
                Dim url = $"https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from={sourceLang}{targetParams}"

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
                            results(toLang) = translated
                        Next
                        CharactersUsed += text.Length * targetLangs.Count
                    End Using
                End If
            Catch ex As OperationCanceledException
            Catch ex As Exception
                Services.Infrastructure.AppLogger.Log($"[ERROR] AzureBackend.TranslateAsync: {ex.Message}")
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
    End Module
End Namespace
