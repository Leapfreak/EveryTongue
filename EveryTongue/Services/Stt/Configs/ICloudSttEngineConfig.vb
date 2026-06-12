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
    End Interface

End Namespace
