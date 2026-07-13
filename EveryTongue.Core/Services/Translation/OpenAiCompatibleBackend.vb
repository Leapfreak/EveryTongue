Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation

    ''' <summary>
    ''' Shared LLM translation backend for any OpenAI-compatible chat-completions
    ''' API (POST {base}/chat/completions with a Bearer key). Parameterised by
    ''' backend name, base URL, and model so concrete engines (DeepSeek, OpenAI)
    ''' are one-line subclasses. Translation is prompt-driven: a system prompt
    ''' instructs the model to output ONLY the translated text; the reply is
    ''' defensively stripped of surrounding quotes/whitespace.
    ''' </summary>
    Public MustInherit Class OpenAiCompatibleBackend
        Inherits CloudTranslationBackend

        Private ReadOnly _backendName As String
        Private ReadOnly _baseUrl As String
        Private ReadOnly _model As String

        Protected Sub New(backendName As String, baseUrl As String, model As String)
            _backendName = backendName
            _baseUrl = If(baseUrl, "").TrimEnd("/"c)
            _model = model
            ' LLM completions are slower than dedicated MT APIs — allow more
            ' headroom than the base class's 10s default.
            HttpClient.Timeout = TimeSpan.FromSeconds(30)
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _backendName
            End Get
        End Property

        ''' <summary>
        ''' Resolve a language code (FLORES in; any format accepted) to an English
        ''' display name for the prompt. GetDisplayNameForCode falls back to the
        ''' raw code itself when the table has no entry.
        ''' </summary>
        Private Shared Function LanguageName(code As String) As String
            Dim friendly = LanguageCodeService.Instance.GetDisplayNameForCode(code)
            Return If(String.IsNullOrEmpty(friendly), code, friendly)
        End Function

        ''' <summary>
        ''' Strip surrounding whitespace and one layer of matching quotes that
        ''' LLMs sometimes wrap around the translation despite instructions.
        ''' </summary>
        Private Shared Function CleanReply(reply As String) As String
            Dim s = If(reply, "").Trim()
            If s.Length >= 2 Then
                Dim first = s(0)
                Dim last = s(s.Length - 1)
                Dim quotePairs = New(Char, Char)() {
                    (""""c, """"c), ("'"c, "'"c),
                    (ChrW(&H201C), ChrW(&H201D)),  ' “ ”
                    (ChrW(&H2018), ChrW(&H2019)),  ' ‘ ’
                    (ChrW(&HAB), ChrW(&HBB))       ' « »
                }
                For Each pair In quotePairs
                    If first = pair.Item1 AndAlso last = pair.Item2 Then
                        s = s.Substring(1, s.Length - 2).Trim()
                        Exit For
                    End If
                Next
            End If
            Return s
        End Function

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                        Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim sourceName = LanguageName(sourceLang)

            ' One short text per request — the only parallelism available is
            ' across target languages (bounded, same pattern as DeepLBackend).
            Dim translatedByIndex(targetLangs.Count - 1) As String
            Dim gate As New SemaphoreSlim(4)
            Dim tasks As New List(Of Task)()

            For i = 0 To targetLangs.Count - 1
                Dim idx = i                       ' capture for closure
                Dim targetLang = targetLangs(i)
                tasks.Add(Task.Run(Async Function()
                    Await gate.WaitAsync(ct)
                    Try
                        Dim systemPrompt =
                            "You are a translation engine. Translate the user's text from " &
                            LanguageName(sourceLang) & " to " & LanguageName(targetLang) &
                            ". Output ONLY the translated text — no quotes, no explanations, no notes."

                        Dim payload As New Dictionary(Of String, Object) From {
                            {"model", _model},
                            {"temperature", 0},
                            {"messages", New Object() {
                                New Dictionary(Of String, Object) From {{"role", "system"}, {"content", systemPrompt}},
                                New Dictionary(Of String, Object) From {{"role", "user"}, {"content", text}}
                            }}
                        }

                        Dim request As New HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
                        request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", ApiKey)
                        request.Content = New StringContent(
                            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")

                        Dim response = Await HttpClient.SendAsync(request, ct)
                        If response.IsSuccessStatusCode Then
                            Dim body = Await response.Content.ReadAsStringAsync()
                            Using doc = JsonDocument.Parse(body)
                                Dim reply = doc.RootElement.
                                    GetProperty("choices")(0).
                                    GetProperty("message").
                                    GetProperty("content").GetString()
                                Dim cleaned = CleanReply(reply)
                                If Not String.IsNullOrEmpty(cleaned) Then
                                    translatedByIndex(idx) = cleaned
                                End If
                            End Using
                        Else
                            Dim errBody = Await response.Content.ReadAsStringAsync()
                            AppLogger.Log(LogEvents.TRANS_ERROR,
                                $"{_backendName}Backend: {response.StatusCode} for {sourceName}->{targetLang}: {errBody}")
                        End If
                    Catch ex As OperationCanceledException
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.TRANS_ERROR,
                            $"{_backendName}Backend.TranslateAsync: target={targetLang} - {ex.Message}")
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
                Dim request As New HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/models")
                request.Headers.Authorization = New AuthenticationHeaderValue("Bearer", ApiKey)
                Dim response = Await HttpClient.SendAsync(request, ct)
                Return response.IsSuccessStatusCode
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"{_backendName}Backend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

    ''' <summary>
    ''' DeepSeek translation backend (OpenAI-compatible API). Requires API key
    ''' from platform.deepseek.com.
    ''' </summary>
    Public Class DeepSeekBackend
        Inherits OpenAiCompatibleBackend

        Public Sub New()
            MyBase.New("DeepSeek", "https://api.deepseek.com", "deepseek-chat")
        End Sub
    End Class

    ''' <summary>
    ''' OpenAI translation backend (chat completions). Requires API key from
    ''' platform.openai.com.
    ''' </summary>
    Public Class OpenAiBackend
        Inherits OpenAiCompatibleBackend

        Public Sub New()
            MyBase.New("OpenAI", "https://api.openai.com/v1", "gpt-4o-mini")
        End Sub
    End Class

End Namespace
