Imports System.Text.Json.Serialization

Namespace Models

    <JsonConverter(GetType(JsonStringEnumConverter))>
    Public Enum ThemeMode
        System
        Light
        Dark
    End Enum

    <JsonConverter(GetType(JsonStringEnumConverter))>
    Public Enum LogVerbosity
        Minimal
        Normal
        Verbose
    End Enum

    Public Class AppConfig

        ' --- Paths & Tools ---

        Public Property PathWhisper As String = ".\whisper-cli.exe"

        Public Property PathYtdlp As String = ".\yt-dlp.exe"

        Public Property PathFfmpeg As String = ".\ffmpeg.exe"

        Public Property PathFfprobe As String = ".\ffprobe.exe"

        Public Property PathModel As String = ".\ggml-large-v3.bin"

        Public Property PathModelAudio As String = ".\ggml-large-v3.bin"

        Public Property PathSubtitleEdit As String = ".\SubtitleEdit\SubtitleEdit.exe"

        Public Property PathOutputRoot As String = "."

        Public Property YtdlpFormat As String = "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]"

        ' --- Settings tab ---

        Public Property UiLanguage As String = "en"

        Public Property ParallelJobs As Integer = 4

        Public Property ChunkSizeSec As Integer = 300

        Public Property PollIntervalMs As Integer = 2000

        Public Property ChunkTimeoutMin As Integer = 60

        Public Property KeepChunkFiles As Boolean = False

        Public Property KeepPreview As Boolean = True


        Public Property Theme As ThemeMode = ThemeMode.Light

        Public Property LogLevel As LogVerbosity = LogVerbosity.Normal

        Public Property LastLiveDeviceId As String = ""

        ' --- Output formats (Main tab) ---

        Public Property OutputSrt As Boolean = True

        Public Property OutputVtt As Boolean = False

        Public Property OutputTxt As Boolean = False

        Public Property OutputJson As Boolean = False

        Public Property OutputCsv As Boolean = False

        Public Property OutputLrc As Boolean = False

        ' --- STT Parameters ---

        Public Property Language As String = "auto"

        Public Property OutputLanguage As String = "en"

        Public Property Threads As Integer = 4

        Public Property Processors As Integer = 1

        Public Property BeamSize As Integer = 5

        Public Property BestOf As Integer = 1

        Public Property Temperature As Single = 0.0F

        Public Property TemperatureInc As Single = 0.2F

        Public Property MaxContext As Integer = 0

        Public Property WordThreshold As Single = 0.01F

        Public Property EntropyThreshold As Single = 2.4F

        Public Property LogProbThreshold As Single = -1.0F

        Public Property NoSpeechThreshold As Single = 0.6F

        Public Property SplitOnWord As Boolean = True

        Public Property NoGpu As Boolean = False

        Public Property FlashAttn As Boolean = False

        Public Property PrintProgress As Boolean = False

        Public Property PrintColours As Boolean = False

        Public Property Diarize As Boolean = False

        Public Property Tinydiarize As Boolean = False


        Public Property NoTimestamps As Boolean = False

        Public Property MaxSegmentLength As Integer = 0

        Public Property MaxTokens As Integer = 0

        Public Property AudioContext As Integer = 0

        Public Property InitialPrompt As String = ""


        Public Property TranslateToEnglish As Boolean = False

        Public Property VadThreshold As Single = 0.6F


        ' --- Subtitle Server ---

        Public Property SubtitleServerPort As Integer = 5080

        Public Property SubtitleBgColor As String = "#000000"

        Public Property SubtitleFgColor As String = "#FFFFFF"

        Public Property SubtitleFontFamily As String = "Segoe UI"
        Public Property SubtitleFontSize As Single = 14
        Public Property SubtitleFontBold As Boolean = True

        ' --- Live Server (STT + VAD) ---

        Public Property SttBackend As String = "whisper-cpp-vulkan"

        ''' <summary>
        ''' API keys for online STT engines, keyed by backend key (e.g.
        ''' "google-cloud-stt", "speechmatics"). Each engine keeps its own key so
        ''' switching engines doesn't clobber another's credential.
        ''' </summary>
        Public Property SttApiKeys As New Dictionary(Of String, String)

        ''' <summary>
        ''' Legacy single Google STT key — deserialize-only. Read from pre-1.8.x
        ''' configs and migrated once into SttApiKeys by ConfigManager.ApplyDefaults,
        ''' which then clears it to Nothing so it is never written back out.
        ''' </summary>
        <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingDefault)>
        Public Property GoogleCloudSttApiKey As String = Nothing

        ''' <summary>Speechmatics real-time region: "eu2" (default) or "us".</summary>
        Public Property SpeechmaticsRegion As String = "eu2"

        ''' <summary>Speechmatics operating point: "enhanced" (best accuracy) or "standard" (lower latency).</summary>
        Public Property SpeechmaticsOperatingPoint As String = "enhanced"

        ''' <summary>
        ''' Speechmatics end-of-utterance silence trigger (ms): how long a pause before
        ''' Speechmatics declares an utterance finished and emits a commit. The root
        ''' control for fragmentation — raise it (e.g. 1500) for pause-heavy speakers so
        ''' Speechmatics stops splitting mid-sentence. Default 800 = Speechmatics' own default.
        ''' </summary>
        Public Property SpeechmaticsEouSilenceMs As Integer = 800

        ''' <summary>Resolve the API key for an STT backend from the per-engine store.</summary>
        Public Function GetSttApiKey(backendKey As String) As String
            If backendKey Is Nothing Then Return ""
            Dim value As String = Nothing
            If SttApiKeys IsNot Nothing AndAlso SttApiKeys.TryGetValue(backendKey, value) AndAlso Not String.IsNullOrEmpty(value) Then
                Return value
            End If
            Return ""
        End Function

        ''' <summary>Store the API key for an STT backend.</summary>
        Public Sub SetSttApiKey(backendKey As String, value As String)
            If String.IsNullOrEmpty(backendKey) Then Return
            If SttApiKeys Is Nothing Then SttApiKeys = New Dictionary(Of String, String)
            SttApiKeys(backendKey) = If(value, "")
        End Sub

        Public Property LiveServerPort As Integer = 5091

        Public Property LiveComputeType As String = "int8_float16"
        Public Property LiveVadSilenceMs As Integer = 300
        Public Property LiveMaxSegmentSec As Integer = 30
        Public Property LiveInterimIntervalMs As Integer = 1000

        ''' <summary>Path to whisper-server.exe (Vulkan build) for whisper-cpp backends.</summary>
        Public Property PathWhisperServer As String = ".\whisper-server.exe"

        ''' <summary>Path to GGML model file for whisper-cpp backends.</summary>
        Public Property PathWhisperCppModel As String = ".\ggml-large-v3-turbo.bin"

        ''' <summary>Path to CTranslate2 model directory for faster-whisper backend.</summary>
        Public Property PathFasterWhisperModel As String = ".\faster-whisper-large-v3"

        ''' <summary>Port for whisper-server.exe inference endpoint.</summary>
        Public Property WhisperServerPort As Integer = 8178

        ''' <summary>Path to Silero VAD GGML model for whisper-server built-in VAD.</summary>
        Public Property PathSileroVadModel As String = ".\ggml-silero-v6.2.0.bin"

        ' --- Translation ---

        Public Property TranslationBackend As String = "nllb"
        Public Property TranslationEnabled As Boolean = True

        ''' <summary>
        ''' API keys for online translation engines, keyed by backend key (e.g.
        ''' "google-translate", "deepl"). Each engine keeps its own key so
        ''' switching engines doesn't clobber another's credential.
        ''' </summary>
        Public Property TranslationApiKeys As New Dictionary(Of String, String)

        ''' <summary>Resolve the API key for a translation backend from the per-engine store.</summary>
        Public Function GetTranslationApiKey(backendKey As String) As String
            If backendKey Is Nothing Then Return ""
            Dim value As String = Nothing
            If TranslationApiKeys IsNot Nothing AndAlso TranslationApiKeys.TryGetValue(backendKey, value) AndAlso Not String.IsNullOrEmpty(value) Then
                Return value
            End If
            Return ""
        End Function

        ''' <summary>Store the API key for a translation backend.</summary>
        Public Sub SetTranslationApiKey(backendKey As String, value As String)
            If String.IsNullOrEmpty(backendKey) Then Return
            If TranslationApiKeys Is Nothing Then TranslationApiKeys = New Dictionary(Of String, String)
            TranslationApiKeys(backendKey) = If(value, "")
        End Sub

        ''' <summary>
        ''' Endpoints (URL, or region name for engines that use regions) for online
        ''' translation engines, keyed by backend key (e.g. "libretranslate",
        ''' "amazon-translate"). Empty/absent = use the registry entry's
        ''' DefaultEndpoint. Mirrors the TranslationApiKeys per-engine pattern.
        ''' </summary>
        Public Property TranslationEndpoints As New Dictionary(Of String, String)

        ''' <summary>Resolve the endpoint for a translation backend from the per-engine store ("" when unset).</summary>
        Public Function GetTranslationEndpoint(backendKey As String) As String
            If backendKey Is Nothing Then Return ""
            Dim value As String = Nothing
            If TranslationEndpoints IsNot Nothing AndAlso TranslationEndpoints.TryGetValue(backendKey, value) AndAlso Not String.IsNullOrEmpty(value) Then
                Return value
            End If
            Return ""
        End Function

        ''' <summary>Store the endpoint for a translation backend.</summary>
        Public Sub SetTranslationEndpoint(backendKey As String, value As String)
            If String.IsNullOrEmpty(backendKey) Then Return
            If TranslationEndpoints Is Nothing Then TranslationEndpoints = New Dictionary(Of String, String)
            TranslationEndpoints(backendKey) = If(value, "")
        End Sub

        ''' <summary>
        ''' Monthly character budgets for cloud translation engines, keyed by
        ''' backend key (e.g. "deepl"). 0 or absent = no budget. Crossing a budget
        ''' logs ONE warning per backend per month — translation is never blocked.
        ''' </summary>
        Public Property TranslationMonthlyCharBudgets As New Dictionary(Of String, Long)

        ''' <summary>Resolve the monthly character budget for a translation backend (0 = no budget).</summary>
        Public Function GetTranslationCharBudget(backendKey As String) As Long
            If String.IsNullOrEmpty(backendKey) Then Return 0
            Dim value As Long
            If TranslationMonthlyCharBudgets IsNot Nothing AndAlso
               TranslationMonthlyCharBudgets.TryGetValue(backendKey, value) Then
                Return Math.Max(0L, value)
            End If
            Return 0
        End Function

        ''' <summary>Store the monthly character budget for a translation backend (0 = no budget).</summary>
        Public Sub SetTranslationCharBudget(backendKey As String, value As Long)
            If String.IsNullOrEmpty(backendKey) Then Return
            If TranslationMonthlyCharBudgets Is Nothing Then TranslationMonthlyCharBudgets = New Dictionary(Of String, Long)
            TranslationMonthlyCharBudgets(backendKey) = Math.Max(0L, value)
        End Sub

        ''' <summary>
        ''' When the STT engine is Speechmatics, use its built-in (English-pivot)
        ''' translation for eligible languages; others fall back to the configured
        ''' translation backend.
        ''' </summary>
        Public Property UseSpeechmaticsTranslation As Boolean = False

        ' --- Speechmatics clause hold-and-lock (translate-and-replace, Phase 1) ---
        ' Speechmatics-only. When on, the conference pipeline holds each
        ' END_OF_UTTERANCE fragment and merges consecutive fragments into one
        ' clause before translating, so pause-heavy speakers don't get their
        ' sentences chopped into separately-translated pieces. Every threshold
        ' below is a live-tunable dial (read fresh on each evaluation — changes
        ' apply without restarting a room). All gated behind the master switch
        ' AND backend == "speechmatics"; other STT pipelines are never affected.

        ''' <summary>Master switch for Speechmatics clause hold-and-lock. Default OFF (zero behaviour change).</summary>
        Public Property SpeechmaticsHoldClauses As Boolean = False

        ''' <summary>Silence (ms) after the last fragment before an accumulated clause is locked and broadcast.</summary>
        Public Property SpeechmaticsClauseGraceMs As Integer = 1200

        ''' <summary>Hard cap (ms): lock a clause once it is this old, regardless of pauses (runaway guard).</summary>
        Public Property SpeechmaticsClauseMaxMs As Integer = 8000

        ''' <summary>Hard cap (chars): lock a clause once it reaches this length (runaway guard).</summary>
        Public Property SpeechmaticsClauseMaxChars As Integer = 300

        ''' <summary>When True, a fragment ending in sentence-final punctuation locks the clause immediately (lower latency).</summary>
        Public Property SpeechmaticsClauseLockOnPunctuation As Boolean = True

        ''' <summary>Minimum clause length (chars) required before punctuation can trigger an immediate lock (avoids "Yes." locking too eagerly).</summary>
        Public Property SpeechmaticsClauseMinLockChars As Integer = 12

        ''' <summary>Characters treated as sentence-final for the punctuation-lock rule (Latin + CJK + Arabic).</summary>
        Public Property SpeechmaticsClauseSentenceEnders As String = ".?!…。？！۔؟"

        ''' <summary>How often (ms) the conference controller polls accumulators for grace-window expiry.</summary>
        Public Property SpeechmaticsClauseTimerMs As Integer = 300

        Public Property TranslationPort As Integer = 5090
        Public Property TranslationModelPath As String = ".\nllb-model"
        Public Property TranslationModelType As String = "nllb"
        Public Property TranslationDevice As String = "cuda"
        Public Property TranslationGlossaryPath As String = ".\translate-server\glossary.json"
        Public Property TranslationConcurrency As Integer = 3

        ' --- TTS ---

        ''' <summary>
        ''' Comma-separated list of preferred TTS backends in priority order.
        ''' Values: piper, mms-tts, edgetts. Empty = all (default fallback order).
        ''' </summary>
        Public Property TtsBackends As String = ""
        Public Property TtsConcurrency As Integer = 3

        Public Property FirstRunComplete As Boolean = False
        Public Property StartWithWindows As Boolean = False
        Public Property AllowFirewall As Boolean = True

        Public Property AdminPin As String = "1234"

        Public Property ConferenceTemplates As List(Of ConferenceTemplate) = New List(Of ConferenceTemplate)()

        Public Property BiblesDirectory As String = ".\Bibles"
        Public Property ShowBibleCopyright As Boolean = True

        Public Property MinimizeToTray As Boolean = True
        Public Property StartMinimized As Boolean = True

        Public Property LogsDirectory As String = ".\logs"

        Public Property LogPanelHeight As Integer = 250

        Public Property LogRouting As Services.Infrastructure.LogRoutingConfig

        Public Shared Function CreateDefault() As AppConfig
            Return New AppConfig()
        End Function

        Public Shared Function ResolvePath(configPath As String) As String
            If String.IsNullOrWhiteSpace(configPath) Then Return ""
            If IO.Path.IsPathRooted(configPath) Then Return IO.Path.GetFullPath(configPath)
            Return IO.Path.GetFullPath(IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath))
        End Function
    End Class
End Namespace
