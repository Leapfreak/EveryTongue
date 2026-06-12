Namespace Services.Interfaces

    ''' <summary>
    ''' Optional capability interface for STT backends whose engine can translate
    ''' inline and accept a new set of translation targets mid-service (e.g. a
    ''' cloud engine that restarts its session to apply them). Orchestration code
    ''' drives this blindly — it never needs to know which engine is behind it.
    ''' </summary>
    Friend Interface IRetargetableSttBackend
        ''' <summary>Whether the active engine session actually supports inline translation.</summary>
        ReadOnly Property SupportsInlineTranslation As Boolean

        ''' <summary>The engine-native translation target codes currently in effect.</summary>
        ReadOnly Property CurrentTranslationTargets As List(Of String)

        ''' <summary>Push a new set of engine-native translation targets to the running engine.</summary>
        Function UpdateTranslationTargetsAsync(targets As List(Of String)) As Task
    End Interface

End Namespace
