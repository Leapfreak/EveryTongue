Imports System.Text.Json.Serialization

Namespace Services.Infrastructure

    <JsonConverter(GetType(JsonStringEnumConverter))>
    Public Enum LogCategory
        Legacy = 0
        Startup = 1
        Config = 2
        Server = 3
        Pipeline = 4
        Stt = 5
        Translation = 6
        Tts = 7
        Conference = 8
        Rooms = 9
        Subtitle = 10
        Bible = 11
        Download = 12
        Audio = 13
        Localization = 14
        Update = 15
        Hardware = 16
        Benchmark = 17
        UI = 18
        PythonLog = 19
    End Enum

    <JsonConverter(GetType(JsonStringEnumConverter))>
    Public Enum LogSeverity
        Debug = 0
        Info = 1
        Warning = 2
        [Error] = 3
        Off = 4
    End Enum

End Namespace
