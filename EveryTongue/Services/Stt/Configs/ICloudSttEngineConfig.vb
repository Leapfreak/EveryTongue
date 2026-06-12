Imports EveryTongue.Models
Imports EveryTongue.Pipeline

Namespace Services.Stt.Configs

    ''' <summary>
    ''' Implemented by cloud STT engine config blocks. The shared
    ''' CloudStreamingSttBackend drives the LiveStreamRunner entirely through
    ''' this — it never knows which engine's fields exist, so adding a cloud
    ''' engine never touches the shared backend.
    ''' </summary>
    Friend Interface ICloudSttEngineConfig
        ''' <summary>
        ''' Push this engine's settings onto the runner and the runner's
        ''' transport config before the session starts.
        ''' </summary>
        Sub ConfigureRunner(runner As LiveStreamRunner, runnerConfig As AppConfig)

        ''' <summary>
        ''' This engine's extra /start JSON fields as a leading-comma fragment
        ''' (e.g. ',"speechmatics_region":"eu2"'), or "" when the engine adds
        ''' nothing. Appended verbatim to the live-server /start body so the
        ''' shared runner never knows any engine's field names.
        ''' </summary>
        Function BuildStartJsonExtras() As String
    End Interface

End Namespace
