Imports System.IO
Imports System.Text.Json
Imports EveryTongue.Services.Infrastructure

Namespace Models
    Public Class ConfigManager
        Private Shared ReadOnly ConfigDir As String = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EveryTongue")
        Private Shared ReadOnly ConfigPath As String = Path.Combine(ConfigDir, "config.json")

        Private Shared ReadOnly JsonOptions As JsonSerializerOptions

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
        End Sub

        Public Shared Sub Save(config As AppConfig)
            Try
                If Not Directory.Exists(ConfigDir) Then
                    Directory.CreateDirectory(ConfigDir)
                End If
                Dim json = JsonSerializer.Serialize(config, JsonOptions)
                File.WriteAllText(ConfigPath, json)
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
