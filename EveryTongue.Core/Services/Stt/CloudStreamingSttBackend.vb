Imports EveryTongue.Models
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' Generic ISttBackend for online streaming engines (Google Cloud STT,
    ''' Speechmatics, …). Adapter boilerplate lives in RunnerBackedSttBackend;
    ''' engine-specific behaviour lives entirely in the Python live-server engine
    ''' module — adding a new cloud engine needs only a registry entry, not a new
    ''' class here. Also hosts the engine config block for inline-translation
    ''' retargeting (IRetargetableSttBackend).
    ''' </summary>
    Friend Class CloudStreamingSttBackend
        Inherits RunnerBackedSttBackend
        Implements IRetargetableSttBackend

        Private ReadOnly _backendKey As String
        Private ReadOnly _displayName As String
        ''' <summary>The engine config block hosted for the active session (set in Start).</summary>
        Private _engineBlock As Configs.ICloudSttEngineConfig

        Public Sub New(backendKey As String, displayName As String)
            _backendKey = backendKey
            _displayName = displayName
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _displayName
            End Get
        End Property

        Public Overrides ReadOnly Property RequiresInternet As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides Sub Start(config As SttSessionConfig)
            Runner.Backend = _backendKey
            Runner.SttApiKey = config.ApiKey
            Runner.FiltersHallucinationsPath = config.HallucinationsPath
            Runner.AudioSource = If(String.IsNullOrEmpty(config.AudioSource), "local", config.AudioSource)

            Dim appConfig As New AppConfig() With {
                .LiveServerPort = config.ServerPort,
                .NoGpu = False
            }

            ' The engine's own config block pushes its settings onto the runner
            ' and contributes its /start JSON fields, so this shared backend
            ' never knows any engine's fields.
            _engineBlock = TryCast(config.EngineConfig, Configs.ICloudSttEngineConfig)
            _engineBlock?.ConfigureRunner(Runner, appConfig)
            Runner.CloudEngineStartExtras = If(_engineBlock?.BuildStartJsonExtras(), "")

            Runner.Start(appConfig, config.DeviceIndex, config.Language, config.TranslateToEnglish)
        End Sub

        ''' <summary>The engine key this backend was created for (e.g. "speechmatics").</summary>
        Public ReadOnly Property BackendKey As String
            Get
                Return _backendKey
            End Get
        End Property

        ''' <summary>
        ''' Warm spare (ENGINE_CONCURRENCY_PLAN): boot the live-server for this
        ''' engine and pre-load SaT — no capture, no Speechmatics session. The
        ''' backend is then parked; a room claims it and calls Start() as normal,
        ''' which reuses the healthy warm process.
        ''' </summary>
        Public Sub WarmOnly(port As Integer, satModel As String)
            Runner.Backend = _backendKey
            Runner.WarmServerOnly(port, satModel)
        End Sub

        ' ── IRetargetableSttBackend — inline-translation retargeting ──
        ' This backend hosts the engine config block, so the knowledge of WHICH
        ' block type supports inline translation lives here, not in controllers.

        ''' <summary>True when the hosted engine block supports inline translation (Speechmatics).</summary>
        Public ReadOnly Property SupportsInlineTranslation As Boolean Implements IRetargetableSttBackend.SupportsInlineTranslation
            Get
                Return TryCast(_engineBlock, Configs.SpeechmaticsConfig) IsNot Nothing
            End Get
        End Property

        ''' <summary>Current inline translation targets (engine codes) for this backend.</summary>
        Public ReadOnly Property TranslationTargets As List(Of String) Implements IRetargetableSttBackend.CurrentTranslationTargets
            Get
                Dim sm = TryCast(_engineBlock, Configs.SpeechmaticsConfig)
                Return If(sm?.TranslationTargets, New List(Of String))
            End Get
        End Property

        ''' <summary>
        ''' Push a new set of inline translation targets to the running engine
        ''' (the engine restarts its session to apply them — brief audio gap).
        ''' Updates the hosted block + the runner's /start extras so any later
        ''' capture restart carries the new targets too.
        ''' </summary>
        Public Async Function UpdateTranslationTargetsAsync(targets As List(Of String)) As Task Implements IRetargetableSttBackend.UpdateTranslationTargetsAsync
            Dim sm = TryCast(_engineBlock, Configs.SpeechmaticsConfig)
            If sm Is Nothing Then Return
            sm.TranslationTargets = If(targets, New List(Of String))
            Runner.CloudEngineStartExtras = sm.BuildStartJsonExtras()
            Await Runner.UpdateConfigAsync(New Dictionary(Of String, Object) From {
                {"translation_targets", sm.TranslationTargets}
            })
        End Function

    End Class

End Namespace
