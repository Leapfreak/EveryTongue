Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading
Imports Microsoft.Extensions.Logging
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Subtitle
Imports EveryTongue.Pipeline

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
        Private _nextCommitId As Integer = 1

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

        ''' <summary>Path to faster-whisper model directory.</summary>
        Public Property WhisperModelPath As String = ""

        ''' <summary>Compute type for Whisper model.</summary>
        Public Property WhisperComputeType As String = "int8_float16"

        ''' <summary>Whether to use CPU instead of CUDA.</summary>
        Public Property WhisperUseCpu As Boolean = False

        ''' <summary>
        ''' Callback invoked when conversation rooms need translation but no backend is available.
        ''' FormMain wires this to start the NLLB translation service and register NllbBackend.
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
            If room.Type <> RoomType.Conversation Then
                _logger.LogWarning("ProcessAudioAsync: room type is {Type}, not Conversation", room.Type)
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

            ' Use the client's language setting so Whisper doesn't guess wrong
            ' Client language is NLLB code (e.g. "eng_Latn") — convert to Whisper code (e.g. "en")
            Dim clientLangNllb = If(client.Language, "")
            Dim whisperLang = If(Not String.IsNullOrEmpty(clientLangNllb),
                TranslationService.NllbToShortCode(clientLangNllb).ToLowerInvariant(), "")
            If Not String.IsNullOrEmpty(whisperLang) Then
                transcribeUrl &= $"?lang={Uri.EscapeDataString(whisperLang)}"
                _logger.LogInformation("Transcribe with forced language: {Lang} (from client {Nllb})", whisperLang, clientLangNllb)
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

                    _logger.LogInformation("Room {RoomId} [{Lang}]: {Text}",
                        client.RoomId, detectedLang, text)

                    ' Broadcast to room members
                    Await BroadcastToRoomAsync(client, room, text, detectedLang, ct).ConfigureAwait(False)
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
        ''' Checks health first — if LiveController already started it, reuses it.
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

                ' Check if server is already running (started by LiveController or previous call)
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

                _sidecar.Start(serverScript, "")

                ' Wait for server to be ready (up to 30s for model loading)
                Dim deadline = DateTime.UtcNow.AddSeconds(30)
                Dim ready = False
                While DateTime.UtcNow < deadline AndAlso Not ct.IsCancellationRequested
                    Await Task.Delay(500, ct).ConfigureAwait(False)
                    ready = Await CheckHealthAsync(ct).ConfigureAwait(False)
                    If ready Then Exit While
                End While

                If Not ready Then
                    _logger.LogWarning("Live server failed to start within 30s")
                    Return False
                End If

                ' Load the Whisper model via /load-model
                _logger.LogInformation("Loading Whisper model for conversation rooms...")
                Dim loadResult = Await LoadModelAsync(ct).ConfigureAwait(False)
                If Not loadResult Then
                    _logger.LogWarning("Failed to load Whisper model")
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
                        If modelLoaded.GetBoolean() Then Return True
                    End If
                End Using
                ' Server is up but model not loaded — need to call /load-model
                Return Await LoadModelAsync(ct).ConfigureAwait(False)
            Catch
                Return False
            End Try
        End Function

        Private Async Function LoadModelAsync(ct As CancellationToken) As Task(Of Boolean)
            Try
                Dim device = If(WhisperUseCpu, "cpu", "cuda")
                Dim jsonBody = $"{{""model_path"":{JsonSerializer.Serialize(WhisperModelPath)},""compute_type"":{JsonSerializer.Serialize(WhisperComputeType)},""device"":{JsonSerializer.Serialize(device)}}}"
                Dim content As New StringContent(jsonBody, Encoding.UTF8, "application/json")

                ' Model loading can take 10-20s on first load
                Using cts = CancellationTokenSource.CreateLinkedTokenSource(ct)
                    cts.CancelAfter(TimeSpan.FromSeconds(60))
                    Dim response = Await _httpClient.PostAsync(
                        $"http://127.0.0.1:{LiveServerPort}/load-model", content, cts.Token).ConfigureAwait(False)
                    If response.IsSuccessStatusCode Then
                        _logger.LogInformation("Whisper model loaded successfully")
                        Return True
                    End If
                    Dim body = Await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(False)
                    _logger.LogWarning("Failed to load model: {Status} {Body}", response.StatusCode, body)
                    Return False
                End Using
            Catch ex As Exception
                _logger.LogWarning("LoadModelAsync failed: {Message}", ex.Message)
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

            _logger.LogInformation("BroadcastToRoom: {Count} recipients (incl. speaker), speaker={Speaker}, targetLangs=[{Langs}]",
                roomClients.Count, speaker.RemoteEndpoint, String.Join(",", targetLangs))

            ' Convert detected whisper lang (e.g. "en") to NLLB code for source
            Dim sourceNllb = TranslationService.WhisperToNllbLang(sourceLang)
            Dim sourceShort = TranslationService.NllbToShortCode(sourceNllb)
            _logger.LogInformation("BroadcastToRoom: sourceNllb={Source} sourceShort={Short}", sourceNllb, sourceShort)

            ' Remove source language from targets (no self-translation)
            targetLangs.Remove(sourceNllb)

            ' Translate to all needed languages in one batch call
            Dim translations As Dictionary(Of String, String) = Nothing
            If targetLangs.Count > 0 AndAlso _translationService IsNot Nothing Then
                ' Check if any backend is actually available; if not, request NLLB startup
                Dim backends = _translationService.GetAllBackends()
                Dim anyAvailable = backends.Any(Function(b) b.IsAvailable)
                If Not anyAvailable Then
                    _logger.LogInformation("No translation backend available — requesting NLLB startup")
                    Try
                        EnsureTranslationAvailable?.Invoke()
                    Catch ex As Exception
                        _logger.LogWarning("EnsureTranslationAvailable callback failed: {Message}", ex.Message)
                    End Try
                End If

                Try
                    translations = Await _translationService.TranslateAsync(
                        text, sourceNllb, targetLangs.ToList(), ct).ConfigureAwait(False)
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

            ' Broadcast to each room member (including speaker for self-echo)
            Dim ts = DateTime.Now.ToString("HH:mm:ss")
            For Each client In roomClients
                Dim clientText As String
                Dim clientLangNllb As String

                If client.Id = speaker.Id Then
                    ' Speaker sees their own original text
                    clientText = text
                    clientLangNllb = sourceNllb
                ElseIf String.IsNullOrEmpty(client.Language) OrElse client.Language = sourceNllb Then
                    clientText = text
                    clientLangNllb = sourceNllb
                ElseIf translations IsNot Nothing AndAlso translations.ContainsKey(client.Language) Then
                    clientText = translations(client.Language)
                    clientLangNllb = client.Language
                Else
                    clientText = text
                    clientLangNllb = sourceNllb
                End If

                ' Convert NLLB code to short code for display (e.g. "spa_Latn" -> "es")
                Dim langShort = TranslationService.NllbToShortCode(clientLangNllb)

                Dim commitId = Interlocked.Increment(_nextCommitId)
                Dim json = $"{{""type"":""commit"",""id"":{commitId},""text"":{SubtitleService.EscapeJson(clientText)},""lang"":{SubtitleService.EscapeJson(langShort)},""time"":{SubtitleService.EscapeJson(ts)},""speaker"":{SubtitleService.EscapeJson(speaker.RemoteEndpoint)}}}"
                Dim buffer = Encoding.UTF8.GetBytes(json)
                _logger.LogInformation("BroadcastToRoom: sending id={Id} to {Endpoint} lang={Lang} text={Text}",
                    commitId, client.RemoteEndpoint, langShort, If(clientText.Length > 80, clientText.Substring(0, 80) & "...", clientText))
                TrySendToClient(client, buffer)
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

            ' Use generic extension — Android Firefox may produce ogg/opus instead of webm
            Dim tempIn = Path.Combine(Path.GetTempPath(), $"et_ptt_{Guid.NewGuid():N}.bin")
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
