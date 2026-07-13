Namespace Services.Scheduling
    ''' <summary>
    ''' Priority levels for translation requests. Lower value = higher priority.
    ''' Used by the priority queue in TranslationOrchestrator to schedule work
    ''' when multiple rooms/features compete for the translation backend.
    ''' </summary>
    Public Enum TranslationPriority
        ''' <summary>Live STT stream — audience waiting in real-time.</summary>
        Live = 0
        ''' <summary>Conversation room — interactive, people waiting on each other.</summary>
        Room = 1
        ''' <summary>Translate workspace — user-initiated, tolerates slight delay.</summary>
        Workspace = 2
        ''' <summary>Benchmark/testing — background, lowest priority.</summary>
        Benchmark = 3
    End Enum
End Namespace
