Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Subtitle
Imports EveryTongue.Pipeline
Imports EveryTongue.Controllers

Namespace Services.Rooms
    ''' <summary>
    ''' Handles audio from conversation room clients (push-to-talk).
    ''' Receives WebM/Opus audio, converts to WAV via FFmpeg, sends to the
    ''' live-server /transcribe endpoint, translates, and broadcasts to room members.
    ''' Auto-starts the live-server and loads the Whisper model if needed.
    ''' </summary>
    Public Class ConversationAudioHandler

        Private Shared ReadOnly _httpClient As New HttpClient() With {
            .Timeout = TimeSpan.FromSeconds(30)
        }

        Private ReadOnly _roomManager As RoomManager
        Private ReadOnly _subtitleService As SubtitleService
        Private ReadOnly _translationService As ITranslationService
        Private ReadOnly _logger As ILogger(Of ConversationAudioHandler)
        Private _nextCommitId As Integer = 0

        ' Live-server auto-start
        Private ReadOnly _sidecar As New PythonSidecarHost() With {
            .Label = "Live server (conversation)",
            .AddWhisperToPath = True,
            .GracefulShutdownPath = "/shutdown"
        }
        Private _serverEnsured As Boolean = False
        Private ReadOnly _ensureLock As New SemaphoreSlim(1, 1)

        ''' <summary>Port the live-server listens on (default 5091).</summary>
        Public Property LiveServerPort As Integer = 5091

        ''' <summary>Path to FFmpeg executable.</summary>
        Public Property FfmpegPath As String = ""

        ''' <summary>Path to Whisper model (.bin).</summary>
        Public Property WhisperModelPath As String = ""

        ''' <summary>Compute type for Whisper model.</summary>
        Public Property WhisperComputeType As String = "int8_float16"

        ''' <summary>Whether to use CPU instead of CUDA.</summary>
        Public Property WhisperUseCpu As Boolean = False

        ''' <summary>Path to whisper-server.exe (for whisper-cpp backend).</summary>
        Public Property WhisperServerPath As String = ""

        ''' <summary>Port for whisper-server.exe HTTP API.</summary>
        Public Property WhisperServerPort As Integer = 8178

        ''' <summary>STT backend key (e.g. "whisper-cpp-vulkan", "whisper-cpp-cpu"). Set from AppConfig.</summary>
        Public Property SttBackend As String = ""

        ''' <summary>Path to Silero VAD GGML model for whisper-server built-in VAD.</summary>
        Public Property SileroVadModelPath As String = ""

        ''' <summary>
        ''' Callback invoked when conversation rooms need translation but no backend is available.
        ''' FormMain wires this to start the translation service and register SidecarTranslationBackend.
        ''' </summary>
        Public Property EnsureTranslationAvailable As Action

        Public Sub New(roomManager As RoomManager,
                       subtitleService As ISubtitleService,
                       translationService As ITranslationService,
                       logger As ILogger(Of ConversationAudioHandler))
            _roomManager = roomManager
            _subtitleService = TryCast(subtitleService, SubtitleService)
            _translationService = translationService
            _logger = logger
        End Sub

        ''' <summary>
        ''' Process audio from a conversation room client.
        ''' Converts from WebM/Opus to WAV via FFmpeg, transcribes via the live-server,
        ''' translates, and broadcasts to the room.
        ''' </summary>
        Public Async Function ProcessAudioAsync(client As ClientConnection,
                                                 audioData As Byte(),
                                                 ct As CancellationToken) As Task
            _logger.LogInformation("ProcessAudioAsync: {Bytes} bytes from {Endpoint} room={Room}",
                audioData.Length, client.RemoteEndpoint, client.RoomId)

            If _subtitleService Is Nothing Then
                _logger.LogWarning("ProcessAudioAsync: _subtitleService is Nothing")
                Return
            End If

            Dim room = _roomManager.GetRoom(client.RoomId)
            If room Is Nothing Then
                _logger.LogWarning("ProcessAudioAsync: room not found for id={Room}", client.RoomId)
                Return
            End If

            ' Conference rooms: only the host can send audio
            If room.Type = RoomType.Conference Then
                If client.Id <> room.HostClientId Then
                    _logger.LogDebug("ProcessAudioAsync: non-host {Endpoint} tried to speak in Conference room, ignoring", client.RemoteEndpoint)
                    Return
                End If
            ElseIf room.Type <> RoomType.Conversation Then
                _logger.LogWarning("ProcessAudioAsync: room type {Type} does not support audio", room.Type)
                Return
            End If

            ' Ensure live-server is running and model is loaded
            Dim serverReady = Await EnsureLiveServerAsync(ct).ConfigureAwait(False)
            If Not serverReady Then
                _logger.LogWarning("Live server not available — cannot transcribe conversation audio")
                Return
            End If

            ' Convert WebM/Opus audio to WAV (16kHz mono PCM) using FFmpeg
            _logger.LogInformation("Converting audio with FFmpeg (path={Path})", FfmpegPath)
            Dim wavData = Await ConvertToWavAsync(audioData, ct).ConfigureAwait(False)
            If wavData Is Nothing OrElse wavData.Length < 1000 Then
                _logger.LogWarning("Audio conversion produced no usable data (input={InBytes} bytes, output={OutBytes} bytes)",
                    audioData.Length, If(wavData?.Length, 0))
                Return
            End If
            _logger.LogInformation("FFmpeg conversion OK: {InBytes} -> {OutBytes} bytes", audioData.Length, wavData.Length)

            ' Send WAV to live-server /transcribe endpoint
            Dim transcribeUrl = $"http://127.0.0.1:{LiveServerPort}/transcribe"

            ' Determine speaker identity (self or virtual member)
            Dim speakerName As String
            Dim speakerLang As String
            If Not String.IsNullOrEmpty(client.SpeakingAsVirtualMemberId) Then
                Dim vm As VirtualMember = Nothing
                room.VirtualMembers.TryGetValue(client.SpeakingAsVirtualMemberId, vm)
                If vm IsNot Nothing Then
                    speakerName = vm.Name
                    speakerLang = If(vm.Language, client.Language)
                Else
                    speakerName = If(client.DisplayName, "Guest")
                    speakerLang = If(client.Language, "")
                End If
            Else
                speakerName = If(String.IsNullOrEmpty(client.DisplayName), "Guest", client.DisplayName)
                speakerLang = If(client.Language, "")
            End If

            ' Use the client's language setting so Whisper doesn't guess wrong
            ' Client language is FLORES code (e.g. "eng_Latn") — convert to Whisper code (e.g. "en")
            Dim clientLangFlores = If(speakerLang, "")
            Dim whisperLang = If(Not String.IsNullOrEmpty(clientLangFlores),
                TranslationService.FloresToShortCode(clientLangFlores).ToLowerInvariant(), "")
            If Not String.IsNullOrEmpty(whisperLang) Then
                transcribeUrl &= $"?lang={Uri.EscapeDataString(whisperLang)}"
                _logger.LogInformation("Transcribe with forced language: {Lang} (from client {Flores})", whisperLang, clientLangFlores)
            End If

            Try
                Dim content As New ByteArrayContent(wavData)
                content.Headers.ContentType = New Headers.MediaTypeHeaderValue("audio/wav")
                Dim response = Await _httpClient.PostAsync(transcribeUrl, content, ct).ConfigureAwait(False)

                If Not response.IsSuccessStatusCode Then
                    Dim body = Await response.Content.ReadAsStringAsync(ct).ConfigureAwait(False)
                    _logger.LogWarning("Transcribe API returned {Status}: {Body}", response.StatusCode, body)
                    Return
                End If

                Dim json = Await response.Content.ReadAsStringAsync(ct).ConfigureAwait(False)
                Using doc = JsonDocument.Parse(json)
                    Dim root = doc.RootElement
                    Dim textProp As JsonElement = Nothing
                    If Not root.TryGetProperty("text", textProp) Then Return
                    Dim text = textProp.GetString()
                    If String.IsNullOrWhiteSpace(text) Then Return

                    Dim langProp As JsonElement = Nothing
                    Dim detectedLang = ""
                    If root.TryGetProperty("lang", langProp) Then
                        detectedLang = If(langProp.GetString(), "")
                    End If

                    _logger.LogInformation("Room {RoomId} [{Lang}] {Speaker}: {Text}",
                        client.RoomId, detectedLang, speakerName, text)

                    ' Broadcast to room members
                    Await BroadcastToRoomAsync(client, room, text, detectedLang, speakerName, ct).ConfigureAwait(False)
                End Using

            Catch ex As OperationCanceledException
                ' Normal cancellation
            Catch ex As HttpRequestException
                _logger.LogWarning("Live server not available for transcription: {Message}", ex.Message)
                ' Reset ensured flag so next attempt retries server startup
                _serverEnsured = False
            Catch ex As Exception
                _logger.LogError(ex, "ConversationAudioHandler.ProcessAudioAsync failed")
            End Try
        End Function

        ' ── Live-server auto-start ──

        ''' <summary>
        ''' Ensures the live-server is running and the Whisper model is loaded.
        ''' Checks health first — if already started, reuses it.
        ''' Otherwise starts its own instance and calls /load-model.
        ''' </summary>
        Public Async Function EnsureLiveServerAsync(ct As CancellationToken) As Task(Of Boolean)
            ' Fast path: already confirmed running
            If _serverEnsured Then
                Return True
            End If

            Await _ensureLock.WaitAsync(ct).ConfigureAwait(False)
            Try
                If _serverEnsured Then Return True

                ' Check if server is already running (started on warm-up or by a previous call)
                Dim healthy = Await CheckHealthAsync(ct).ConfigureAwait(False)
                If healthy Then
                    _logger.LogInformation("Live server already running on port {Port}", LiveServerPort)
                    _serverEnsured = True
                    Return True
                End If

                ' Server not running — start it ourselves
                If String.IsNullOrEmpty(WhisperModelPath) Then
                    _logger.LogWarning("Cannot auto-start live server: WhisperModelPath not configured")
                    Return False
                End If

                _logger.LogInformation("Auto-starting live server for conversation rooms...")
                _sidecar.Port = LiveServerPort

                Dim serverScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "live-server", "server.py")
                If Not File.Exists(serverScript) Then
                    _logger.LogWarning("Live server script not found: {Path}", serverScript)
                    Return False
                End If

                ' Build args based on STT backend
                Dim useWhisperCpp = SttBackend IsNot Nothing AndAlso SttBackend.StartsWith("whisper-cpp", StringComparison.OrdinalIgnoreCase)
                Dim sidecarArgs As String
                If useWhisperCpp Then
                    Dim wsPath = If(WhisperServerPath, "")
                    Dim wsPort = WhisperServerPort
                    Dim sttEntry = Stt.SttBackendRegistry.Find(SttBackend)
                    Dim noGpuFlag = If(sttEntry IsNot Nothing AndAlso Not sttEntry.UseGpu, " --no-gpu", "")
                    Dim vadPath = If(SileroVadModelPath, "")
                    Dim vadFlag = If(Not String.IsNullOrEmpty(vadPath) AndAlso File.Exists(vadPath),
                        $" --vad-model-path ""{vadPath}""", "")
                    sidecarArgs = $"--backend whisper-cpp --whisper-server-path ""{wsPath}"" --whisper-server-port {wsPort}{noGpuFlag}{vadFlag}"
                Else
                    sidecarArgs = ""
                End If

                _sidecar.Start(serverScript, sidecarArgs)

                ' Wait for server HTTP to be reachable (up to 15s)
                Dim deadline = DateTime.UtcNow.AddSeconds(15)
                Dim serverUp = False
                While DateTime.UtcNow < deadline AndAlso Not ct.IsCancellationRequested
                    Await Task.Delay(500, ct).ConfigureAwait(False)
                    serverUp = Await CheckServerUpAsync(ct).ConfigureAwait(False)
                    If serverUp Then Exit While
                End While

                If Not serverUp Then
                    _logger.LogWarning("Live server failed to start within 15s")
                    Return False
                End If

                ' Check if model is already loaded (e.g. server was already running)
                Dim alreadyReady = Await CheckHealthAsync(ct).ConfigureAwait(False)
                If alreadyReady Then
                    _logger.LogInformation("Live server already has model loaded")
                    _serverEnsured = True
                    Return True
                End If

                ' Load the Whisper model via /load-model (single attempt, not in a retry loop)
                _logger.LogInformation("Loading Whisper model for conversation rooms...")
                Dim loadResult = Await LoadModelAsync(ct).ConfigureAwait(False)
                If Not loadResult Then
                    _logger.LogWarning("Failed to load Whisper model — check CUDA/GPU availability")
                    Return False
                End If

                _logger.LogInformation("Live server ready for conversation rooms")
                _serverEnsured = True
                Return True

            Catch ex As OperationCanceledException
                Return False
            Catch ex As Exception
                _logger.LogError(ex, "EnsureLiveServerAsync failed")
                Return False
            Finally
                _ensureLock.Release()
            End Try
        End Function

        Private Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim response = Await _httpClient.GetAsync($"http://127.0.0.1:{LiveServerPort}/health", ct).ConfigureAwait(False)
                If Not response.IsSuccessStatusCode Then Return False
                ' Check if model is loaded
                Dim json = Await response.Content.ReadAsStringAsync(ct).ConfigureAwait(False)
                Using doc = JsonDocument.Parse(json)
                    Dim modelLoaded As JsonElement = Nothing
                    If doc.RootElement.TryGetProperty("model_loaded", modelLoaded) Then
                        Return modelLoaded.GetBoolean()
                    End If
                End Using
                ' Server is up (no model_loaded field = legacy server, assume ready)
                Return True
            Catch
                Return False
            End Try
        End Function

        ''' <summary>Check if the server HTTP endpoint is reachable (ignoring model state).</summary>
        Private Async Function CheckServerUpAsync(ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim response = Await _httpClient.GetAsync($"http://127.0.0.1:{LiveServerPort}/health", ct).ConfigureAwait(False)
                Return response.IsSuccessStatusCode
            Catch
                Return False
            End Try
        End Function

        Private Async Function LoadModelAsync(ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim device = If(WhisperUseCpu, "cpu", "cuda")
                Dim loaded = Await TryLoadModelWithDeviceAsync(device, ct).ConfigureAwait(False)

                ' Auto-fallback: if CUDA failed, retry on CPU
                If Not loaded AndAlso device = "cuda" Then
                    _logger.LogWarning("CUDA model load failed — retrying on CPU...")
                    loaded = Await TryLoadModelWithDeviceAsync("cpu", ct).ConfigureAwait(False)
                End If

                Return loaded
            Catch ex As Exception
                _logger.LogWarning("LoadModelAsync failed: {Message}", ex.Message)
                Return False
            End Try
        End Function

        Private Async Function TryLoadModelWithDeviceAsync(device As String, ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim jsonBody = $"{{""model_path"":{JsonSerializer.Serialize(WhisperModelPath)},""compute_type"":{JsonSerializer.Serialize(WhisperComputeType)},""device"":{JsonSerializer.Serialize(device)}}}"
                Dim content As New StringContent(jsonBody, Encoding.UTF8, "application/json")

                Using cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                    cts.CancelAfter(TimeSpan.FromSeconds(60))
                    Dim response = Await _httpClient.PostAsync(
                        $"http://127.0.0.1:{LiveServerPort}/load-model", content, cts.Token).ConfigureAwait(False)
                    If response.IsSuccessStatusCode Then
                        _logger.LogInformation("Whisper model loaded successfully on {Device}", device)
                        Return True
                    End If
                    Dim body = Await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(False)
                    _logger.LogWarning("Failed to load model on {Device}: {Status} {Body}", device, response.StatusCode, body)
                    Return False
                End Using
            Catch ex As Exception
                _logger.LogWarning("LoadModel ({Device}) exception: {Message}", device, ex.Message)
                Return False
            End Try
        End Function

        ' ── Broadcast ──

        ''' <summary>
        ''' Broadcast transcribed text to all members of a conversation room,
        ''' translating for each client's language. Excludes the speaker.
        ''' </summary>
        Private Async Function BroadcastToRoomAsync(speaker As ClientConnection,
                                                     room As Room,
                                                     text As String,
                                                     sourceLang As String,
                                                     speakerName As String,
                                                     ct As CancellationToken) As Task
            ' Collect all room members (including speaker for self-echo)
            Dim targetLangs As New HashSet(Of String)()
            Dim roomClients As New List(Of ClientConnection)()

            For Each clientId In room.ClientIds.Keys
                Dim client = GetClient(clientId)
                If client Is Nothing Then Continue For
                roomClients.Add(client)
                ' Collect target languages from non-speaker clients for translation
                If client.Id <> speaker.Id AndAlso Not String.IsNullOrEmpty(client.Language) Then
                    targetLangs.Add(client.Language)
                End If
            Next

            ' Also collect languages from virtual members (for shared-device translation)
            For Each vm In room.VirtualMembers.Values
                If Not String.IsNullOrEmpty(vm.Language) Then
                    targetLangs.Add(vm.Language)
                End If
            Next

            ' Shared-device: host's own language must be a target too (they get excluded
            ' above as the speaker, but need translations when switching identity back to self)
            If room.VirtualMembers.Count > 0 AndAlso Not String.IsNullOrEmpty(speaker.Language) Then
                targetLangs.Add(speaker.Language)
            End If

            _logger.LogInformation("BroadcastToRoom: {Count} recipients (incl. speaker), speaker={Speaker}, targetLangs=[{Langs}]",
                roomClients.Count, speaker.RemoteEndpoint, String.Join(",", targetLangs))

            ' Convert detected whisper lang (e.g. "en") to FLORES code for source
            Dim sourceFlores = TranslationService.WhisperToFloresLang(sourceLang)
            Dim sourceShort = TranslationService.FloresToShortCode(sourceFlores)
            _logger.LogInformation("BroadcastToRoom: sourceFlores={Source} sourceShort={Short}", sourceFlores, sourceShort)

            ' Remove source language from targets (no self-translation)
            targetLangs.Remove(sourceFlores)

            ' Translate to all needed languages in one batch call
            Dim translations As Dictionary(Of String, String) = Nothing
            If targetLangs.Count > 0 AndAlso _translationService IsNot Nothing Then
                ' Check if any backend is actually available; if not, request translation startup
                Dim backends = _translationService.GetAllBackends()
                Dim anyAvailable = backends.Any(Function(b) b.IsAvailable)
                If Not anyAvailable Then
                    _logger.LogInformation("No translation backend available — requesting translation startup")
                    Try
                        EnsureTranslationAvailable?.Invoke()
                    Catch ex As Exception
                        _logger.LogWarning("EnsureTranslationAvailable callback failed: {Message}", ex.Message)
                    End Try
                End If

                Try
                    ' Split into lines/sentences for translation (same as Translate workspace)
                    Dim textLines = TranslateController.SplitIntoLines(text)
                    Dim totalSentences = textLines.Sum(Function(tl) tl.Sentences.Count)

                    If totalSentences <= 1 Then
                        ' Single sentence — translate directly
                        translations = Await _translationService.TranslateAsync(
                            text, sourceFlores, targetLangs.ToList(), ct,
                            Scheduling.TranslationPriority.Room).ConfigureAwait(False)
                    Else
                        ' Multi-sentence — translate per sentence, reassemble with line breaks
                        Dim perLang As New Dictionary(Of String, Text.StringBuilder)()
                        For Each tl In targetLangs
                            perLang(tl) = New Text.StringBuilder()
                        Next

                        For lineIdx = 0 To textLines.Count - 1
                            Dim tl = textLines(lineIdx)
                            If lineIdx > 0 Then
                                For Each lang In targetLangs
                                    perLang(lang).Append(vbLf)
                                Next
                            End If
                            If tl.IsBlank Then Continue For

                            For Each sentence In tl.Sentences
                                Dim sentResult = Await _translationService.TranslateAsync(
                                    sentence, sourceFlores, targetLangs.ToList(), ct,
                                    Scheduling.TranslationPriority.Room).ConfigureAwait(False)
                                For Each lang In targetLangs
                                    If perLang(lang).Length > 0 AndAlso Not perLang(lang).ToString().EndsWith(vbLf) Then
                                        perLang(lang).Append(" ")
                                    End If
                                    If sentResult IsNot Nothing AndAlso sentResult.ContainsKey(lang) Then
                                        perLang(lang).Append(sentResult(lang))
                                    Else
                                        perLang(lang).Append(sentence)
                                    End If
                                Next
                            Next
                        Next

                        translations = New Dictionary(Of String, String)()
                        For Each lang In targetLangs
                            translations(lang) = perLang(lang).ToString()
                        Next
                    End If
                Catch ex As Exception
                    _logger.LogWarning(ex, "Translation failed for conversation room")
                End Try
            End If

            If translations IsNot Nothing Then
                _logger.LogInformation("BroadcastToRoom: translations available for [{Langs}]",
                    String.Join(",", translations.Keys))
            Else
                _logger.LogInformation("BroadcastToRoom: no translations (sending original text)")
            End If

            ' Determine if the room has virtual members (shared-device scenario)
            Dim hasVirtualMembers = room.VirtualMembers.Count > 0

            ' Broadcast to each room member (including speaker for self-echo)
            Dim ts = DateTime.Now.ToString("HH:mm:ss")
            For Each client In roomClients
                ' Shared-device clients (host with virtual members) get ALL translations
                Dim isSharedDevice = hasVirtualMembers AndAlso client.Id = room.HostClientId

                If isSharedDevice Then
                    ' Build translations dict including original text under source language
                    Dim allTranslations As New Dictionary(Of String, String)()
                    allTranslations(sourceFlores) = text
                    If translations IsNot Nothing Then
                        For Each kvp In translations
                            allTranslations(kvp.Key) = kvp.Value
                        Next
                    End If
                    ' Build JSON with translations dict
                    Dim commitId = Interlocked.Increment(_nextCommitId)
                    Dim transJson As New Text.StringBuilder()
                    transJson.Append("{")
                    Dim first = True
                    For Each kvp In allTranslations
                        If Not first Then transJson.Append(",")
                        first = False
                        transJson.Append(SubtitleService.EscapeJson(kvp.Key))
                        transJson.Append(":")
                        transJson.Append(SubtitleService.EscapeJson(kvp.Value))
                    Next
                    transJson.Append("}")
                    Dim json = $"{{""type"":""commit"",""id"":{commitId},""speaker"":{SubtitleService.EscapeJson(speakerName)},""lang"":{SubtitleService.EscapeJson(sourceShort)},""time"":{SubtitleService.EscapeJson(ts)},""sourceLang"":{SubtitleService.EscapeJson(sourceFlores)},""translations"":{transJson.ToString()}}}"
                    Dim buffer = Encoding.UTF8.GetBytes(json)
                    _logger.LogInformation("BroadcastToRoom: sending id={Id} to shared-device {Endpoint} with {Count} translations",
                        commitId, client.RemoteEndpoint, allTranslations.Count)
                    TrySendToClient(client, buffer)
                Else
                    ' Normal single-language client
                    Dim clientText As String

                    If client.Id = speaker.Id Then
                        clientText = text
                    ElseIf String.IsNullOrEmpty(client.Language) OrElse client.Language = sourceFlores Then
                        clientText = text
                    ElseIf translations IsNot Nothing AndAlso translations.ContainsKey(client.Language) Then
                        clientText = translations(client.Language)
                    Else
                        clientText = text
                    End If

                    ' Lang tag = language of the TEXT being sent (target lang for translated, source for original)
                    Dim textLang As String
                    If client.Id = speaker.Id OrElse String.IsNullOrEmpty(client.Language) OrElse client.Language = sourceFlores Then
                        textLang = sourceShort
                    Else
                        textLang = TranslationService.FloresToShortCode(client.Language)
                    End If
                    Dim commitId = Interlocked.Increment(_nextCommitId)
                    Dim json = $"{{""type"":""commit"",""id"":{commitId},""text"":{SubtitleService.EscapeJson(clientText)},""lang"":{SubtitleService.EscapeJson(textLang)},""time"":{SubtitleService.EscapeJson(ts)},""speaker"":{SubtitleService.EscapeJson(speakerName)},""sourceLang"":{SubtitleService.EscapeJson(sourceFlores)}}}"
                    Dim buffer = Encoding.UTF8.GetBytes(json)
                    _logger.LogInformation("BroadcastToRoom: sending id={Id} to {Endpoint} lang={Lang} text={Text}",
                        commitId, client.RemoteEndpoint, textLang, If(clientText.Length > 80, clientText.Substring(0, 80) & "...", clientText))
                    TrySendToClient(client, buffer)
                End If
            Next
        End Function

        Private Function GetClient(clientId As String) As ClientConnection
            Return _subtitleService?.GetClient(clientId)
        End Function

        Private Sub TrySendToClient(client As ClientConnection, data As Byte())
            If client.WebSocket Is Nothing Then
                _logger.LogWarning("TrySendToClient: WebSocket is Nothing for {Endpoint}", client.RemoteEndpoint)
                Return
            End If
            If client.WebSocket.State <> Net.WebSockets.WebSocketState.Open Then
                _logger.LogWarning("TrySendToClient: WebSocket state is {State} for {Endpoint}", client.WebSocket.State, client.RemoteEndpoint)
                Return
            End If

            If Interlocked.CompareExchange(client.SendBusy, 1, 0) <> 0 Then
                Interlocked.Increment(client.MessagesDropped)
                _logger.LogWarning("TrySendToClient: client {Endpoint} busy, message dropped", client.RemoteEndpoint)
                Return
            End If

            Task.Run(Async Function()
                         Try
                             If client.WebSocket.State = Net.WebSockets.WebSocketState.Open Then
                                 Await client.WebSocket.SendAsync(
                                     New ArraySegment(Of Byte)(data),
                                     Net.WebSockets.WebSocketMessageType.Text, True,
                                     CancellationToken.None).ConfigureAwait(False)
                                 Interlocked.Increment(client.MessagesSent)
                                 _logger.LogInformation("TrySendToClient: sent {Bytes} bytes to {Endpoint} OK", data.Length, client.RemoteEndpoint)
                             Else
                                 _logger.LogWarning("TrySendToClient: WebSocket closed before send for {Endpoint}", client.RemoteEndpoint)
                             End If
                         Catch ex As Exception
                             _logger.LogWarning("WebSocket send failed for {Endpoint}: {Message}", client.RemoteEndpoint, ex.Message)
                         Finally
                             Interlocked.Exchange(client.SendBusy, 0)
                         End Try
                     End Function)
        End Sub

        ' ── Text chat ──

        ''' <summary>
        ''' Process a typed text message from a conversation room client.
        ''' Same as audio flow but skips transcription — translates and broadcasts directly.
        ''' </summary>
        Public Async Function ProcessTextAsync(client As ClientConnection,
                                                text As String,
                                                ct As CancellationToken) As Task
            If _subtitleService Is Nothing Then Return

            Dim room = _roomManager.GetRoom(client.RoomId)
            If room Is Nothing OrElse room.Type <> RoomType.Conversation Then Return

            ' Determine speaker identity
            Dim speakerName As String
            Dim speakerLang As String
            If Not String.IsNullOrEmpty(client.SpeakingAsVirtualMemberId) Then
                Dim vm As VirtualMember = Nothing
                room.VirtualMembers.TryGetValue(client.SpeakingAsVirtualMemberId, vm)
                If vm IsNot Nothing Then
                    speakerName = vm.Name
                    speakerLang = If(vm.Language, client.Language)
                Else
                    speakerName = If(client.DisplayName, "Guest")
                    speakerLang = If(client.Language, "")
                End If
            Else
                speakerName = If(String.IsNullOrEmpty(client.DisplayName), "Guest", client.DisplayName)
                speakerLang = If(client.Language, "")
            End If

            ' Convert FLORES code to Whisper short code for source lang resolution
            Dim sourceFlores = If(speakerLang, "eng_Latn")
            Dim sourceLang = TranslationService.FloresToShortCode(sourceFlores)

            _logger.LogInformation("Room {RoomId} chat [{Lang}] {Speaker}: {Text}",
                client.RoomId, sourceLang, speakerName, text)

            Await BroadcastToRoomAsync(client, room, text, sourceLang, speakerName, ct).ConfigureAwait(False)
        End Function

        ' ── FFmpeg conversion ──

        ''' <summary>
        ''' Convert audio data (WebM/Opus or other format) to 16kHz mono WAV using FFmpeg.
        ''' </summary>
        Private Async Function ConvertToWavAsync(audioData As Byte(), ct As CancellationToken) As Task(Of Byte())
            Dim ffmpeg = FfmpegPath
            If String.IsNullOrEmpty(ffmpeg) OrElse Not File.Exists(ffmpeg) Then
                _logger.LogWarning("FFmpeg not available for audio conversion")
                Return Nothing
            End If

            ' Detect format from magic bytes and use correct extension (helps FFmpeg + avoids antivirus on .bin)
            Dim ext = ".webm"
            If audioData.Length >= 4 Then
                If audioData(0) = &H4F AndAlso audioData(1) = &H67 AndAlso audioData(2) = &H67 AndAlso audioData(3) = &H53 Then
                    ext = ".ogg"  ' OggS magic
                End If
            End If
            Dim tempIn = Path.Combine(Path.GetTempPath(), $"et_ptt_{Guid.NewGuid():N}{ext}")
            Dim tempOut = Path.Combine(Path.GetTempPath(), $"et_ptt_{Guid.NewGuid():N}.wav")

            Try
                Await File.WriteAllBytesAsync(tempIn, audioData, ct).ConfigureAwait(False)

                Dim psi As New Diagnostics.ProcessStartInfo() With {
                    .FileName = ffmpeg,
                    .Arguments = $"-y -i ""{tempIn}"" -ac 1 -ar 16000 -c:a pcm_s16le ""{tempOut}""",
                    .UseShellExecute = False,
                    .CreateNoWindow = True,
                    .RedirectStandardError = True
                }

                Using proc = Diagnostics.Process.Start(psi)
                    If proc Is Nothing Then Return Nothing
                    Dim exited = proc.WaitForExit(10000)
                    If Not exited Then
                        Try
                            proc.Kill(True)
                        Catch
                        End Try
                        Return Nothing
                    End If
                    If proc.ExitCode <> 0 Then
                        Dim stderr = proc.StandardError.ReadToEnd()
                        _logger.LogWarning("FFmpeg conversion failed (exit={Code}): {Error}", proc.ExitCode, stderr)
                        Return Nothing
                    End If
                End Using

                If File.Exists(tempOut) Then
                    Return Await File.ReadAllBytesAsync(tempOut, ct).ConfigureAwait(False)
                End If
                Return Nothing

            Catch ex As Exception
                _logger.LogWarning(ex, "Audio conversion failed")
                Return Nothing
            Finally
                Try
                    If File.Exists(tempIn) Then File.Delete(tempIn)
                Catch
                End Try
                Try
                    If File.Exists(tempOut) Then File.Delete(tempOut)
                Catch
                End Try
            End Try
        End Function

    End Class
End Namespace
