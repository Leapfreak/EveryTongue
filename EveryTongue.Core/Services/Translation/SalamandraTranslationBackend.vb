Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation
    ''' <summary>
    ''' SalamandraTA-7B offline translation backend — BSC's Catalan-first MT-tuned
    ''' LLM served by a local llama-server (LlamaServerHost, Vulkan). The first
    ''' context-CAPABLE offline engine: prior sentences and their broadcast
    ''' translations (rolling window, v2.13.0) become a prefix-forced continuation
    ''' prompt — the model completes ONLY the current sentence's translation, so no
    ''' fragile output splitting is needed.
    '''
    ''' Field-validated facts this class encodes (Jezer, 2026-08-04):
    '''   - the bare prompt format below is byte-what the test kit validated;
    '''   - given a paragraph the model translates ALL of it → prefix forcing;
    '''   - the model IGNORES glossary prompt instructions → SupportsTerminology is
    '''     False in the registry and no glossary section is prompted; glossary
    '''     fixes run in the orchestrator's post-processors instead
    '''     (AppliesFiltersInternally = False).
    ''' Fresh ITranslationBackend implementation — the cloud base classes hardcode
    ''' RequiresInternet/ApiKey semantics that do not fit a local engine.
    ''' </summary>
    Public Class SalamandraTranslationBackend
        Implements ITranslationBackend

        Private ReadOnly _host As LlamaServerHost
        ''' <summary>llama-server runs one slot (--parallel 1) — serialize requests.</summary>
        Private ReadOnly _gate As New SemaphoreSlim(1, 1)
        Private Shared ReadOnly _httpClient As New HttpClient() With {.Timeout = TimeSpan.FromSeconds(30)}

        Public Sub New(host As LlamaServerHost)
            _host = host
        End Sub

        Public ReadOnly Property Name As String Implements ITranslationBackend.Name
            Get
                Return "Salamandra"
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ITranslationBackend.RequiresInternet
            Get
                Return False
            End Get
        End Property

        Public ReadOnly Property IsAvailable As Boolean Implements ITranslationBackend.IsAvailable
            Get
                Return _host IsNot Nothing AndAlso _host.IsRunning AndAlso _host.IsModelLoaded
            End Get
        End Property

        ''' <summary>False — the orchestrator's GlossaryPostProcessor/ProfanityPostProcessor
        ''' run on this backend's output. That IS the terminology channel for this
        ''' engine (the model provably ignores prompt glossaries).</summary>
        Public ReadOnly Property AppliesFiltersInternally As Boolean Implements ITranslationBackend.AppliesFiltersInternally
            Get
                Return False
            End Get
        End Property

        Private Shared Function LanguageName(code As String) As String
            Dim friendly = LanguageCodeService.Instance.GetDisplayNameForCode(code)
            Return If(String.IsNullOrEmpty(friendly), code, friendly)
        End Function

        ''' <summary>Quote/whitespace strip (LLM habit), plus a leading "{TargetName}:"
        ''' echo strip and a defensive trailing partial-ChatML-tag strip.</summary>
        Private Shared Function CleanReply(reply As String, targetName As String) As String
            Dim s = If(reply, "").Trim()
            Dim tagIdx = s.IndexOf("<|im", StringComparison.Ordinal)
            If tagIdx >= 0 Then s = s.Substring(0, tagIdx).Trim()
            If s.StartsWith(targetName & ":", StringComparison.OrdinalIgnoreCase) Then
                s = s.Substring(targetName.Length + 1).Trim()
            End If
            If s.Length >= 2 Then
                Dim first = s(0)
                Dim last = s(s.Length - 1)
                Dim quotePairs = New (Char, Char)() {
                    (""""c, """"c), ("'"c, "'"c),
                    (ChrW(&H201C), ChrW(&H201D)),
                    (ChrW(&H2018), ChrW(&H2019)),
                    (ChrW(&HAB), ChrW(&HBB))
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

        ''' <summary>
        ''' Prompt assembly. Bare (validated byte-format from the test kit):
        '''   &lt;|im_start|&gt;user\nTranslate the following text from X into Y.\nX: text\nY:&lt;|im_end|&gt;\n&lt;|im_start|&gt;assistant\n
        ''' With context, prefix-forced continuation: the user turn carries the prior
        ''' source sentences + the current one; the assistant turn is PRE-FILLED with
        ''' the prior sentences' broadcast translations, so the model's completion is
        ''' exactly the current sentence's translation.
        ''' Qualifying prior sentences: same source language (the window legitimately
        ''' holds ca/es code-switch; a different-language sentence would contradict
        ''' the "from X" instruction), non-empty translation for THIS target, and a
        ''' 1200-char hard ceiling (the upstream window already caps at ~4s/700ch).
        ''' </summary>
        Private Shared Function BuildPrompt(text As String, sourceLang As String, targetLang As String,
                                            context As TranslationContext) As (Prompt As String, PriorTranslations As List(Of String))
            Dim srcName = LanguageName(sourceLang)
            Dim tgtName = LanguageName(targetLang)

            Dim priorSrc As New List(Of String)()
            Dim priorTrans As New List(Of String)()
            If context?.Sentences IsNot Nothing Then
                Dim budget = 1200
                ' Newest → oldest until the budget is spent, then restore order.
                For i = context.Sentences.Count - 1 To 0 Step -1
                    Dim s = context.Sentences(i)
                    If Not String.Equals(s.Lang, sourceLang, StringComparison.OrdinalIgnoreCase) Then Continue For
                    Dim tr As String = Nothing
                    If Not s.Translations.TryGetValue(targetLang, tr) OrElse String.IsNullOrWhiteSpace(tr) Then Continue For
                    Dim cost = If(s.SourceText?.Length, 0) + tr.Length
                    If cost > budget Then Exit For
                    budget -= cost
                    priorSrc.Insert(0, s.SourceText)
                    priorTrans.Insert(0, tr)
                Next
            End If

            If priorSrc.Count = 0 Then
                Return ("<|im_start|>user" & vbLf &
                        $"Translate the following text from {srcName} into {tgtName}." & vbLf &
                        $"{srcName}: {text}" & vbLf &
                        $"{tgtName}:<|im_end|>" & vbLf &
                        "<|im_start|>assistant" & vbLf, New List(Of String)())
            End If

            ' NO trailing space after the prefill — models expect the next token to
            ' start with its own leading space; a dangling space causes degenerate
            ' continuations (field 2026-08-04: mid-word Cyrillic artifacts and
            ' whole-window echo repeats both traced to this).
            Return ("<|im_start|>user" & vbLf &
                    $"Translate the following text from {srcName} into {tgtName}." & vbLf &
                    $"{srcName}: {String.Join(" ", priorSrc)} {text}" & vbLf &
                    $"{tgtName}:<|im_end|>" & vbLf &
                    "<|im_start|>assistant" & vbLf &
                    String.Join(" ", priorTrans), priorTrans)
        End Function

        ''' <summary>One /completion round trip → cleaned reply ("" on error/empty).</summary>
        Private Async Function RequestCompletionAsync(prompt As String, nPredict As Integer,
                                                      targetName As String, sourceLang As String, targetLang As String,
                                                      ct As CancellationToken) As Task(Of String)
            Dim payload = JsonSerializer.Serialize(New With {
                Key .prompt = prompt,
                Key .n_predict = nPredict,
                Key .temperature = 0,
                Key .stop = New String() {"<|im_end|>"},
                Key .cache_prompt = True})
            Dim resp = Await _httpClient.PostAsync($"http://127.0.0.1:{_host.Port}/completion",
                New StringContent(payload, System.Text.Encoding.UTF8, "application/json"), ct)
            If Not resp.IsSuccessStatusCode Then
                Dim errBody = Await resp.Content.ReadAsStringAsync()
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"SalamandraBackend: HTTP {CInt(resp.StatusCode)} for {sourceLang}→{targetLang}: {If(errBody, "").Substring(0, Math.Min(160, If(errBody, "").Length))}")
                Return ""
            End If
            Dim body = Await resp.Content.ReadAsStringAsync()
            Using doc = JsonDocument.Parse(body)
                Dim contentEl As JsonElement = Nothing
                If doc.RootElement.TryGetProperty("content", contentEl) Then
                    Return CleanReply(contentEl.GetString(), targetName)
                End If
            End Using
            Return ""
        End Function

        Public Async Function TranslateAsync(text As String,
                                             sourceLang As String,
                                             targetLangs As IReadOnlyList(Of String),
                                             ct As CancellationToken,
                                             Optional noCache As Boolean = False,
                                             Optional filters As TranslationFilterPaths = Nothing,
                                             Optional context As TranslationContext = Nothing
        ) As Task(Of Dictionary(Of String, String)) Implements ITranslationBackend.TranslateAsync
            Dim results As New Dictionary(Of String, String)()
            If Not IsAvailable OrElse String.IsNullOrWhiteSpace(text) Then Return results

            If Not LanguageCodeService.Instance.SupportsSalamandra(sourceLang) Then
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"SalamandraBackend: source '{sourceLang}' is not among the model's languages — no output (the orchestrator will fall back)")
                Return results
            End If

            For Each targetLang In targetLangs
                ct.ThrowIfCancellationRequested()
                If Not LanguageCodeService.Instance.SupportsSalamandra(targetLang) Then
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"SalamandraBackend: target '{targetLang}' is not among the model's languages — no output for this target")
                    Continue For
                End If

                Await _gate.WaitAsync(ct)
                Try
                    Dim built = BuildPrompt(text, sourceLang, targetLang, context)
                    Dim tgtName = LanguageName(targetLang)
                    ' n_predict: output is ONLY the current sentence's translation.
                    ' chars/3 ≈ tokens for Latin scripts, so chars-as-budget is ~3x
                    ' headroom; the stop token ends normal runs early; 1024 bounds a
                    ' pathological loop at ~8s.
                    Dim nPredict = Math.Max(96, Math.Min(1024, text.Length))
                    Dim cleaned = Await RequestCompletionAsync(built.Prompt, nPredict, tgtName, sourceLang, targetLang, ct)

                    If built.PriorTranslations.Count > 0 Then
                        ' Salvage: the model occasionally restarts the whole window —
                        ' strip a verbatim re-emitted prefill first.
                        Dim prefill = String.Join(" ", built.PriorTranslations)
                        If cleaned.StartsWith(prefill, StringComparison.Ordinal) Then
                            cleaned = cleaned.Substring(prefill.Length).Trim()
                        End If
                        ' Degenerate continuation (field 2026-08-04: echoed prior
                        ' translations / runaway repeats / empty on short sentences):
                        ' detect and retry ONCE with the bare no-context prompt —
                        ' a slightly less contextual translation always beats a
                        ' repeated or missing caption.
                        Dim lastPrior = built.PriorTranslations(built.PriorTranslations.Count - 1)
                        Dim degenerate = cleaned.Length = 0 OrElse
                                         cleaned.Length > Math.Max(60, 4 * text.Length) OrElse
                                         (lastPrior.Length > 12 AndAlso cleaned.IndexOf(lastPrior, StringComparison.Ordinal) >= 0)
                        If degenerate Then
                            AppLogger.Log(LogEvents.TRANS_LLAMA_PROBLEM,
                                $"context echo/empty for {sourceLang}→{targetLang} (""{text.Substring(0, Math.Min(40, text.Length))}"") — retrying without context")
                            Dim bare = BuildPrompt(text, sourceLang, targetLang, Nothing)
                            cleaned = Await RequestCompletionAsync(bare.Prompt, nPredict, tgtName, sourceLang, targetLang, ct)
                        End If
                    End If

                    If cleaned.Length > 0 Then results(targetLang) = cleaned
                Catch ex As OperationCanceledException
                    Throw
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR, $"SalamandraBackend.TranslateAsync: target={targetLang} - {ex.Message}")
                Finally
                    _gate.Release()
                End Try
            Next

            ' The orchestrator treats an empty dict as a SILENT fallthrough to the
            ' fallback backend — leave an explanation on the record.
            If results.Count = 0 AndAlso targetLangs.Count > 0 Then
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"SalamandraBackend: no output produced for {sourceLang}→[{String.Join(",", targetLangs)}] (see prior lines)")
            End If
            Return results
        End Function

        Public Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo)) Implements ITranslationBackend.GetSupportedLanguagesAsync
            ' Dead surface today (zero callers) — the salamandra column in
            ' language-codes.json is the real coverage source.
            Return Task.FromResult(DirectCast(New List(Of LanguageInfo)(), IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean) Implements ITranslationBackend.CheckHealthAsync
            If Not IsAvailable Then Return False
            Try
                Dim resp = Await _httpClient.GetAsync($"http://127.0.0.1:{_host.Port}/health", ct)
                Return resp.IsSuccessStatusCode
            Catch
                ' Health probe — unreachable simply means unhealthy; the host's own
                ' lifecycle logging (4016/4017) covers why.
                Return False
            End Try
        End Function
    End Class
End Namespace
