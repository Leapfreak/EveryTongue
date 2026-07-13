Imports EveryTongue.Services.Config

Namespace Services.Models

    ''' <summary>
    ''' Engine-agnostic session configuration for one STT run. Holds only what
    ''' every engine genuinely shares (device, language, port, resolved API key);
    ''' all engine-specific knobs live in the typed EngineConfig block owned by
    ''' the selected engine. Replaces the old SttConfig god-object.
    ''' </summary>
    Public Class SttSessionConfig
        ''' <summary>Registry key of the engine this session runs on.</summary>
        Public Property EngineKey As String = ""
        Public Property DeviceIndex As Integer = 0
        ''' <summary>Who provides the audio: "local" = a capture device on this machine
        ''' (DeviceIndex), "web" = a browser broadcasts frames via the hub → /audio-in.</summary>
        Public Property AudioSource As String = "local"
        Public Property Language As String = "auto"
        Public Property TranslateToEnglish As Boolean = False
        ''' <summary>Port the Python live-server sidecar listens on for this session.</summary>
        Public Property ServerPort As Integer = 5100
        ''' <summary>Resolved API key for online engines (machine-level secret; empty for offline engines).</summary>
        Public Property ApiKey As String = ""
        ''' <summary>Per-session hallucination filter file ("" = the live-server's default). Session/filter concern, not engine config.</summary>
        Public Property HallucinationsPath As String = ""
        ''' <summary>The selected engine's own config block (e.g. WhisperCppConfig, SpeechmaticsConfig).</summary>
        Public Property EngineConfig As IEngineConfigBlock

        ''' <summary>Typed view of the engine block; Nothing if the block is a different engine's.</summary>
        Public Function Block(Of TBlock As Class)() As TBlock
            Return TryCast(EngineConfig, TBlock)
        End Function
    End Class

End Namespace
