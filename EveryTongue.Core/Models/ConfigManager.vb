Imports System.IO
Imports System.Text.Json
Imports EveryTongue.Services.Infrastructure

Namespace Models
    Public Class ConfigManager
        ' EVERYTONGUE_CONFIG_DIR overrides the default %APPDATA%\EveryTongue — the
        ' Docker/Lite deployments mount a /config volume and point this at it.
        Private Shared ReadOnly ConfigDir As String =
            If(Environment.GetEnvironmentVariable("EVERYTONGUE_CONFIG_DIR"),
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveryTongue"))
        Private Shared ReadOnly ConfigPath As String = Path.Combine(ConfigDir, "config.json")

        ''' <summary>The resolved config directory (env override or %APPDATA%) — for hosts that co-locate certs/logs.</summary>
        Public Shared ReadOnly Property ConfigDirectory As String
            Get
                Return ConfigDir
            End Get
        End Property

        Private Shared ReadOnly JsonOptions As JsonSerializerOptions

        ''' <summary>The canonical config.json serializer settings — shared so the
        ''' raw-config web editor round-trips exactly what Save/Load produce.</summary>
        Public Shared ReadOnly Property SerializerOptions As JsonSerializerOptions
            Get
                Return JsonOptions
            End Get
        End Property

        Shared Sub New()
            JsonOptions = New JsonSerializerOptions With {
                .WriteIndented = True,
                .PropertyNameCaseInsensitive = True
            }
            JsonOptions.Converters.Add(New System.Text.Json.Serialization.JsonStringEnumConverter())
        End Sub

        Public Shared Function Load() As AppConfig
            Try
                If Not File.Exists(ConfigPath) Then Return New AppConfig()
                Dim json = File.ReadAllText(ConfigPath)
                Dim cfg = JsonSerializer.Deserialize(Of AppConfig)(json, JsonOptions)
                If cfg Is Nothing Then Return New AppConfig()
                ApplyDefaults(cfg)
                Return cfg
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_LOAD_FAILED, $"Load failed: {ex.Message}")
                Return New AppConfig()
            End Try
        End Function

        Private Shared Sub ApplyDefaults(cfg As AppConfig)
            If String.IsNullOrEmpty(cfg.SubtitleBgColor) OrElse Not cfg.SubtitleBgColor.StartsWith("#") Then cfg.SubtitleBgColor = "#000000"
            If String.IsNullOrEmpty(cfg.SubtitleFgColor) OrElse Not cfg.SubtitleFgColor.StartsWith("#") Then cfg.SubtitleFgColor = "#FFFFFF"

            ' Migrate old whisper\ subdirectory path to flat (Download Manager puts whisper-cli.exe at root)
            If cfg.PathWhisper IsNot Nothing AndAlso cfg.PathWhisper.EndsWith("\whisper\whisper-cli.exe") Then
                cfg.PathWhisper = cfg.PathWhisper.Replace("\whisper\whisper-cli.exe", "\whisper-cli.exe")
            End If

            ' One-time migration: legacy single Google STT key (pre-1.8.x) → per-engine SttApiKeys.
            ' The legacy property is deserialize-only (JsonIgnore WhenWritingDefault), so clearing
            ' it to Nothing here means it disappears from config.json on the next save.
            If Not String.IsNullOrEmpty(cfg.GoogleCloudSttApiKey) Then
                If cfg.SttApiKeys Is Nothing Then cfg.SttApiKeys = New Dictionary(Of String, String)
                Dim existing As String = Nothing
                If Not (cfg.SttApiKeys.TryGetValue("google-cloud-stt", existing) AndAlso Not String.IsNullOrEmpty(existing)) Then
                    cfg.SttApiKeys("google-cloud-stt") = cfg.GoogleCloudSttApiKey
                    AppLogger.Log(LogEvents.CONFIG_MIGRATED, "Migrated legacy GoogleCloudSttApiKey into SttApiKeys[""google-cloud-stt""]")
                End If
            End If
            ' Always normalise to Nothing (covers ""), so JsonIgnore(WhenWritingDefault) drops it on save.
            cfg.GoogleCloudSttApiKey = Nothing

            ' Migrate conference templates' embedded engine knobs into the STT template library (idempotent)
            Services.Config.ConferenceTemplateMigration.Migrate(cfg)
        End Sub

        Private Shared ReadOnly _saveLock As New Object()

        Public Shared Sub Save(config As AppConfig)
            Try
                ' Serialize + atomic-replace under a lock: the web settings endpoints
                ' save from request threads while the desktop saves from the UI thread —
                ' two overlapping File.WriteAllText calls to the same path can tear
                ' config.json. Write-to-temp-then-move means a crash mid-write leaves
                ' the previous good file, never a truncated one.
                SyncLock _saveLock
                    If Not Directory.Exists(ConfigDir) Then
                        Directory.CreateDirectory(ConfigDir)
                    End If
                    Dim json = JsonSerializer.Serialize(config, JsonOptions)
                    Dim tmpPath = ConfigPath & ".tmp"
                    File.WriteAllText(tmpPath, json)
                    File.Move(tmpPath, ConfigPath, overwrite:=True)
                End SyncLock
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_SAVE_FAILED, $"Save failed: {ex.Message}")
            End Try
        End Sub

        Public Shared Sub Reset()
            Try
                If File.Exists(ConfigPath) Then File.Delete(ConfigPath)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_RESET, $"Reset failed: {ex.Message}")
            End Try
        End Sub
    End Class
End Namespace
