Imports System.Text.Json
Imports Microsoft.AspNetCore.Builder
Imports Microsoft.AspNetCore.Http
Imports Microsoft.AspNetCore.Routing
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Options
Imports EveryTongue.Services.Infrastructure

Namespace Server

    ''' <summary>
    ''' /api/settings — browser-based server configuration (engine selection + API
    ''' keys + admin PIN). The path that lets a headless (Lite/Docker) deployment be
    ''' onboarded without ever editing config.json by hand.
    '''
    ''' Gating: requires the admin PIN — EXCEPT when no PIN is configured yet
    ''' (fresh install bootstrap): then settings are open and the UI's first save
    ''' is expected to set a PIN. Keys are write-only: GET never returns a stored
    ''' key, only whether one is set.
    ''' </summary>
    Partial Public Module EndpointRegistration

        ''' <summary>The live AppConfig — wired by the head (FormMain / LiteProgram).</summary>
        Public Property SettingsConfigProvider As Func(Of Models.AppConfig)

        ''' <summary>Persist + apply after a settings change — wired by the head.</summary>
        Public Property SettingsSaveHandler As Action

        Private Function SettingsPinOk(context As HttpContext, pin As String) As Boolean
            Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
            Dim serverOpts = If(opts?.Value, New ServerOptions())
            ' Bootstrap: no PIN configured yet → open (the UI forces setting one).
            If String.IsNullOrEmpty(serverOpts.AdminPin) Then Return True
            Return String.Equals(If(pin, ""), serverOpts.AdminPin, StringComparison.Ordinal)
        End Function

        Private Sub MapSettingsEndpoints(app As IEndpointRouteBuilder)

            app.MapGet("/api/settings",
                Function(context As HttpContext) As IResult
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then Return Results.Json(New With {.error = "settings not available"}, statusCode:=503)
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                    End If

                    Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                    Dim serverOpts = If(opts?.Value, New ServerOptions())

                    Dim sttEngines = Services.Stt.SttBackendRegistry.GetAll().
                        Select(Function(e) New With {
                            .key = e.Key, .name = e.DisplayName,
                            .online = e.RequiresInternet, .requiresKey = e.RequiresApiKey,
                            .keySet = Not String.IsNullOrEmpty(cfg.GetSttApiKey(e.Key))
                        }).ToList()
                    Dim transEngines = Services.Translation.TranslationBackendRegistry.GetAll().
                        Select(Function(e) New With {
                            .key = e.Key, .name = e.DisplayName,
                            .online = e.RequiresInternet, .requiresKey = e.RequiresApiKey,
                            .keySet = Not String.IsNullOrEmpty(
                                Services.Translation.TranslationBackendRegistry.ResolveTranslationApiKey(cfg, e.Key))
                        }).ToList()

                    Return Results.Json(New With {
                        .adminPinSet = Not String.IsNullOrEmpty(serverOpts.AdminPin),
                        .sttBackend = If(cfg.SttBackend, ""),
                        .translationBackend = If(cfg.TranslationBackend, ""),
                        .sttEngines = sttEngines,
                        .translationEngines = transEngines
                    })
                End Function)

            app.MapPost("/api/settings",
                Async Function(context As HttpContext) As Task(Of IResult)
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then Return Results.Json(New With {.error = "settings not available"}, statusCode:=503)

                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) As String
                                         Dim p As JsonElement = Nothing
                                         Return If(root.TryGetProperty(name, p) AndAlso p.ValueKind = JsonValueKind.String,
                                                   p.GetString(), Nothing)
                                     End Function

                        If Not SettingsPinOk(context, getStr("pin")) Then
                            Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                        End If

                        Dim changed As New List(Of String)

                        Dim newStt = getStr("sttBackend")
                        If Not String.IsNullOrEmpty(newStt) AndAlso Services.Stt.SttBackendRegistry.Find(newStt) IsNot Nothing Then
                            cfg.SttBackend = newStt : changed.Add($"sttBackend={newStt}")
                        End If
                        Dim newTrans = getStr("translationBackend")
                        If Not String.IsNullOrEmpty(newTrans) AndAlso Services.Translation.TranslationBackendRegistry.Find(newTrans) IsNot Nothing Then
                            cfg.TranslationBackend = newTrans : changed.Add($"translationBackend={newTrans}")
                        End If

                        ' API keys: only non-empty values overwrite (empty field in the UI = "keep").
                        Dim keysProp As JsonElement = Nothing
                        If root.TryGetProperty("sttKeys", keysProp) AndAlso keysProp.ValueKind = JsonValueKind.Object Then
                            For Each kv In keysProp.EnumerateObject()
                                If kv.Value.ValueKind = JsonValueKind.String AndAlso Not String.IsNullOrEmpty(kv.Value.GetString()) Then
                                    cfg.SetSttApiKey(kv.Name, kv.Value.GetString())
                                    changed.Add($"sttKey:{kv.Name}")
                                End If
                            Next
                        End If
                        If root.TryGetProperty("translationKeys", keysProp) AndAlso keysProp.ValueKind = JsonValueKind.Object Then
                            For Each kv In keysProp.EnumerateObject()
                                If kv.Value.ValueKind = JsonValueKind.String AndAlso Not String.IsNullOrEmpty(kv.Value.GetString()) Then
                                    cfg.SetTranslationApiKey(kv.Name, kv.Value.GetString())
                                    changed.Add($"transKey:{kv.Name}")
                                End If
                            Next
                        End If

                        ' Admin PIN: applied to the LIVE ServerOptions too so subsequent
                        ' requests are gated by the new PIN immediately.
                        Dim newPin = getStr("adminPin")
                        If Not String.IsNullOrEmpty(newPin) Then
                            cfg.AdminPin = newPin
                            Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                            If opts?.Value IsNot Nothing Then opts.Value.AdminPin = newPin
                            changed.Add("adminPin")
                        End If

                        If changed.Count > 0 Then
                            SettingsSaveHandler?.Invoke()
                            ' Re-push cloud keys into already-registered backends so a key
                            ' change takes effect without a server restart.
                            Services.Translation.TranslationBackendRegistry.ConfigureCloudApiKeys(context.RequestServices, cfg)
                            Services.Tts.TtsBackendRegistry.ConfigureCloudTtsKeys(context.RequestServices, cfg)
                            AppLogger.Log(LogCategory.Config, LogSeverity.Info,
                                $"/api/settings applied: {String.Join(", ", changed)} (keys logged by name only)")
                        End If

                        Return Results.Json(New With {.ok = True, .changed = changed.Count})
                    End Using
                End Function)

            ' Recent log tail for headless ops (PIN-gated, session log file)
            app.MapGet("/api/settings/logtail",
                Function(context As HttpContext) As IResult
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                    End If
                    Try
                        Dim logFile = AppLogger.GetLogPath()
                        If Not IO.File.Exists(logFile) Then Return Results.Json(New With {.lines = New String() {}})
                        ' Shared read — the logger appends concurrently.
                        Dim lines As New List(Of String)
                        Using fs As New IO.FileStream(logFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                            Using sr As New IO.StreamReader(fs)
                                While Not sr.EndOfStream
                                    lines.Add(sr.ReadLine())
                                    If lines.Count > 4000 Then lines.RemoveAt(0)
                                End While
                            End Using
                        End Using
                        Dim tail = lines.Skip(Math.Max(0, lines.Count - 200)).ToList()
                        Return Results.Json(New With {.lines = tail})
                    Catch ex As Exception
                        Return Results.Json(New With {.error = ex.Message}, statusCode:=500)
                    End Try
                End Function)
        End Sub

    End Module

End Namespace
