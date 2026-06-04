Namespace Server
    ''' <summary>
    ''' Configuration options for the Kestrel server.
    ''' Bound from AppConfig on startup.
    ''' </summary>
    Public Class ServerOptions
        ''' <summary>HTTP port (default 5080). HTTPS is always HttpPort + 1.</summary>
        Public Property HttpPort As Integer = 5080

        ''' <summary>Whether to bind to all interfaces (True) or localhost only (False).</summary>
        Public Property AllowRemote As Boolean = True

        ''' <summary>Maximum committed lines kept for history replay.</summary>
        Public Property HistoryCap As Integer = 500

        ''' <summary>Maximum entries replayed to a reconnecting client.</summary>
        Public Property MaxReplayEntries As Integer = 200

        ''' <summary>Path to the Bibles directory. If empty, searches near the executable.</summary>
        Public Property BiblesDirectory As String = ""

        ''' <summary>Background/foreground colours for the web client.</summary>
        Public Property BgColor As String = "#000000"
        Public Property FgColor As String = "#FFFFFF"

        ''' <summary>
        ''' Audio output device number for local TTS playback (-1 = disabled, 0+ = device index).
        ''' Use TtsAudioOutput.GetOutputDevices() to list available devices.
        ''' </summary>
        Public Property TtsOutputDevice As Integer = -1

        ''' <summary>Volume for local TTS output (0.0 to 1.0).</summary>
        Public Property TtsOutputVolume As Single = 1.0F

        ''' <summary>
        ''' Comma-separated preferred TTS backends (piper, mms-tts, edgetts).
        ''' Empty = all backends in default priority order.
        ''' </summary>
        Public Property TtsBackends As String = ""

        ''' <summary>
        ''' PIN required to access admin controls from the phone client.
        ''' Empty string means admin features are hidden (no gear button shown).
        ''' </summary>
        Public Property AdminPin As String = ""

        ''' <summary>Whether to display Bible copyright notices on the phone client.</summary>
        Public Property ShowBibleCopyright As Boolean = True

        ''' <summary>Port the live-server (Whisper STT sidecar) listens on. Used by ConversationAudioHandler.</summary>
        Public Property LiveServerPort As Integer = 5091

        ''' <summary>Path to FFmpeg executable. Used by ConversationAudioHandler for audio conversion.</summary>
        Public Property FfmpegPath As String = ""

        ''' <summary>Path to Whisper model (.bin). Used to auto-load model for conversation rooms.</summary>
        Public Property WhisperModelPath As String = ""

        ''' <summary>Compute type for Whisper model (e.g. "int8_float16"). Used for auto-load.</summary>
        Public Property WhisperComputeType As String = "int8_float16"

        ''' <summary>Whether to use CPU instead of CUDA for Whisper.</summary>
        Public Property WhisperUseCpu As Boolean = False

        ''' <summary>Path to whisper-server.exe (for whisper-cpp backend).</summary>
        Public Property WhisperServerPath As String = ""

        ''' <summary>Port for whisper-server.exe HTTP API (default 8178).</summary>
        Public Property WhisperServerPort As Integer = 8178

        ''' <summary>STT backend key (e.g. "whisper-cpp-vulkan", "whisper-cpp-cpu"). Set from AppConfig.</summary>
        Public Property SttBackend As String = ""

        ''' <summary>Path to Silero VAD GGML model for whisper-server built-in VAD.</summary>
        Public Property SileroVadModelPath As String = ""

        ''' <summary>Max concurrent translation requests through the priority queue.</summary>
        Public Property TranslationConcurrency As Integer = 3

        ''' <summary>Max concurrent TTS synthesis requests through the priority queue.</summary>
        Public Property TtsConcurrency As Integer = 3

        ''' <summary>Conference templates to sync on startup.</summary>
        Public Property ConferenceTemplates As List(Of Models.ConferenceTemplate)

        ''' <summary>Computed HTTPS port.</summary>
        Public ReadOnly Property HttpsPort As Integer
            Get
                Return HttpPort + 1
            End Get
        End Property
    End Class
End Namespace
