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

            ' Engine-concurrency floors (a hand-edited 0 must not mean "no engines").
            If cfg.MaxConcurrentSttEngines < 1 Then cfg.MaxConcurrentSttEngines = 1
            If cfg.MaxConcurrentTranslationEngines < 1 Then cfg.MaxConcurrentTranslationEngines = 1
            If cfg.MaxConcurrentTtsEngines < 1 Then cfg.MaxConcurrentTtsEngines = 1
            ' Refresh the ambient idle-timeout every load so all readiness waits —
            ' desktop, Lite, sidecars started from any path — follow the config
            ' without per-caller plumbing (the property itself floors at 5s).
            Pipeline.SidecarReadiness.DefaultIdleTimeoutSeconds = cfg.EngineLoadIdleTimeoutSeconds
            ' Same trick for the residency limits: every config load re-points the
            ' arbiter at the freshest values (covers desktop, Lite, options saves).
            Services.Infrastructure.EngineResidencyArbiter.Instance.LimitProvider =
                Function(cat As Services.Infrastructure.EngineCategory) As Integer
                    Select Case cat
                        Case Services.Infrastructure.EngineCategory.Stt
                            Return cfg.MaxConcurrentSttEngines
                        Case Services.Infrastructure.EngineCategory.Translation
                            Return cfg.MaxConcurrentTranslationEngines
                        Case Else
                            Return cfg.MaxConcurrentTtsEngines
                    End Select
                End Function

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

            ' One-time migration (2026-07-30): the live GGML model default was
            ' large-v3-turbo (speed); the default is now full large-v3 (quality).
            ' Upgrade configs still pointing at the turbo file — but only when the
            ' large-v3 file actually exists beside it, so a turbo-only install keeps
            ' running. The flag makes this once-only: a user who deliberately picks
            ' turbo again afterwards must not be re-flipped on the next load.
            If Not cfg.LiveGgmlTurboDefaultMigrated AndAlso
               Not String.IsNullOrEmpty(cfg.PathWhisperCppModel) AndAlso
               Path.GetFileName(cfg.PathWhisperCppModel).Equals("ggml-large-v3-turbo.bin", StringComparison.OrdinalIgnoreCase) Then
                Dim resolvedDir = Path.GetDirectoryName(AppConfig.ResolvePath(cfg.PathWhisperCppModel))
                If Not String.IsNullOrEmpty(resolvedDir) AndAlso File.Exists(Path.Combine(resolvedDir, "ggml-large-v3.bin")) Then
                    Dim storedDir = Path.GetDirectoryName(cfg.PathWhisperCppModel)
                    cfg.PathWhisperCppModel = If(String.IsNullOrEmpty(storedDir), "ggml-large-v3.bin", Path.Combine(storedDir, "ggml-large-v3.bin"))
                    cfg.LiveGgmlTurboDefaultMigrated = True
                    AppLogger.Log(LogEvents.CONFIG_MIGRATED, $"Live GGML model upgraded from turbo default to full large-v3: {cfg.PathWhisperCppModel}")
                End If
            End If

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
