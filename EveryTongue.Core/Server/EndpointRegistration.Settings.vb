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

        ''' <summary>Fires after a web Bible download is installed + rescanned, so the
        ''' DESKTOP Bible tab (the second of the two Bible viewers — see CLAUDE.md)
        ''' can refresh its listing. The head must marshal to its UI thread.
        ''' Lite leaves it unwired (the phone panel re-fetches on open).</summary>
        Public Property BibleLibraryChangedHandler As Action

        ''' <summary>Per-translation download progress for the web Bible installer:
        ''' "downloading" / "converting" / "verifying" / "done" / "error: …".</summary>
        Private ReadOnly _bibleDownloadStates As New Concurrent.ConcurrentDictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        ''' <summary>Guards cfg.ConferenceTemplates (a plain List) against concurrent
        ''' request threads: Add/Remove during another request's enumeration throws.
        ''' Web-only guard — desktop edits go through the modal FormTemplateManager.</summary>
        Private ReadOnly _templatesLock As New Object()

        Private Function IsBibleDownloadActive(state As String) As Boolean
            Return state = "downloading" OrElse state = "converting" OrElse state = "verifying"
        End Function

        Private Function SettingsPinOk(context As HttpContext, pin As String) As Boolean
            Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
            Dim serverOpts = If(opts?.Value, New ServerOptions())
            ' Bootstrap: no PIN configured yet → open (the UI forces setting one).
            If String.IsNullOrEmpty(serverOpts.AdminPin) Then Return True
            Return String.Equals(If(pin, ""), serverOpts.AdminPin, StringComparison.Ordinal)
        End Function

        ''' <summary>String property from a parsed JSON request body (missing or non-string → Nothing).</summary>
        Private Function JsonStr(root As JsonElement, name As String) As String
            Dim p As JsonElement = Nothing
            Return If(root.TryGetProperty(name, p) AndAlso p.ValueKind = JsonValueKind.String,
                      p.GetString(), Nothing)
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
                        .creatorCodeSet = Not String.IsNullOrEmpty(serverOpts.CreatorCode),
                        .sttBackend = If(cfg.SttBackend, ""),
                        .translationBackend = If(cfg.TranslationBackend, ""),
                        .sttEngines = sttEngines,
                        .translationEngines = transEngines
                    })
                End Function)

            ' NOTE: async VB lambdas declared As Task(Of IResult) are NOT executed
            ' by minimal APIs when registered as a Delegate — every branch returned
            ' an empty 200 (an invalid-PIN save looked like success to the client).
            ' Write the response directly instead of returning IResult.
            app.MapPost("/api/settings",
                Async Function(context As HttpContext) As Task
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then
                        context.Response.StatusCode = 503
                        Await context.Response.WriteAsJsonAsync(New With {.error = "settings not available"})
                        Return
                    End If

                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) JsonStr(root, name)

                        If Not SettingsPinOk(context, getStr("pin")) Then
                            context.Response.StatusCode = 403
                            Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                            Return
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

                        ' Creator (volunteer) code: same live-apply treatment. "-" clears
                        ' it (back to open creation); empty field = keep unchanged.
                        Dim newCreator = getStr("creatorCode")
                        If Not String.IsNullOrEmpty(newCreator) Then
                            If newCreator = "-" Then newCreator = ""
                            cfg.CreatorCode = newCreator
                            Dim opts2 = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                            If opts2?.Value IsNot Nothing Then opts2.Value.CreatorCode = newCreator
                            changed.Add(If(newCreator = "", "creatorCode(cleared)", "creatorCode"))
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

                        Await context.Response.WriteAsJsonAsync(New With {.ok = True, .changed = changed.Count})
                    End Using
                End Function)

            ' ── Conference template editor (web) ─────────────────────────────
            ' Admin-PIN-gated CRUD over AppConfig.ConferenceTemplates — the last
            ' piece a headless deployment needed a Windows machine for. Exposes
            ' the fields a Lite deployment needs (name, hosting code, source
            ' language, engines, audio source, offered languages, visibility);
            ' engine tuning templates / speakers / filter sets stay desktop-only.
            ' Offered languages use the same 1:1 pattern as the desktop's STT
            ' library templates: a DisplayTemplate with Id = the template's Id.

            app.MapGet("/api/settings/templates",
                Function(context As HttpContext) As IResult
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then Return Results.Json(New With {.error = "settings not available"}, statusCode:=503)
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                    End If
                    Dim libStore = Services.Config.TemplateLibraryStore.Instance
                    Dim snapshot As List(Of Models.ConferenceTemplate)
                    SyncLock _templatesLock
                        snapshot = If(cfg.ConferenceTemplates, New List(Of Models.ConferenceTemplate)).ToList()
                    End SyncLock
                    Dim templates = snapshot.
                        Select(Function(tpl) New With {
                            .id = tpl.Id,
                            .name = If(tpl.Name, ""),
                            .hostingCode = If(tpl.HostingCode, ""),
                            .sourceLanguage = If(tpl.SourceLanguage, "auto"),
                            .sttBackend = If(tpl.SttBackendKey, ""),
                            .translationBackend = If(tpl.TranslationBackendKey, ""),
                            .audioSource = If(String.IsNullOrEmpty(tpl.AudioSource), "local", tpl.AudioSource),
                            .webMicRaw = tpl.WebMicRaw,
                            .visibility = If(tpl.DefaultVisibility, "public"),
                            .offeredLanguages = If(libStore.GetDisplayTemplate(If(tpl.DisplayTemplateId, ""))?.OfferedLanguages,
                                                   New List(Of String)).ToArray()
                        }).ToList()
                    Return Results.Json(New With {.templates = templates})
                End Function)

            app.MapPost("/api/settings/templates",
                Async Function(context As HttpContext) As Task
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then
                        context.Response.StatusCode = 503
                        Await context.Response.WriteAsJsonAsync(New With {.error = "settings not available"})
                        Return
                    End If

                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) JsonStr(root, name)

                        If Not SettingsPinOk(context, getStr("pin")) Then
                            context.Response.StatusCode = 403
                            Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                            Return
                        End If

                        Dim tplName = If(getStr("name"), "").Trim()
                        Dim hostingCode = If(getStr("hostingCode"), "").Trim()
                        If tplName = "" OrElse hostingCode = "" Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {.error = "name and hostingCode are required"})
                            Return
                        End If

                        ' Engines: reject unknown non-empty keys outright — silently
                        ' substituting a different engine than the caller asked for is
                        ' worse than a 400. Validated BEFORE the template is created so
                        ' an error never leaves a half-built entry in the list.
                        ' Field MISSING from the body = keep the current value (the web
                        ' UI always sends both; this protects raw API callers from
                        ' wiping keys on partial updates). Present-but-empty = explicit.
                        Dim stt = getStr("sttBackend")           ' Nothing = not sent
                        If Not String.IsNullOrEmpty(stt) AndAlso Services.Stt.SttBackendRegistry.Find(stt) Is Nothing Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {.error = $"unknown sttBackend '{stt}'"})
                            Return
                        End If
                        Dim trans = getStr("translationBackend") ' Nothing = not sent
                        If Not String.IsNullOrEmpty(trans) AndAlso Services.Translation.TranslationBackendRegistry.Find(trans) Is Nothing Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {.error = $"unknown translationBackend '{trans}'"})
                            Return
                        End If

                        Dim id = If(getStr("id"), "")
                        Dim tpl As Models.ConferenceTemplate = Nothing
                        SyncLock _templatesLock
                            If cfg.ConferenceTemplates Is Nothing Then cfg.ConferenceTemplates = New List(Of Models.ConferenceTemplate)
                            If id = "" Then
                                tpl = New Models.ConferenceTemplate()
                                cfg.ConferenceTemplates.Add(tpl)
                            Else
                                tpl = cfg.ConferenceTemplates.FirstOrDefault(Function(x) x.Id = id)
                            End If
                        End SyncLock
                        If tpl Is Nothing Then
                            context.Response.StatusCode = 404
                            Await context.Response.WriteAsJsonAsync(New With {.error = "unknown template id"})
                            Return
                        End If

                        tpl.Name = tplName
                        tpl.HostingCode = hostingCode
                        Dim srcLang = If(getStr("sourceLanguage"), "").Trim()
                        tpl.SourceLanguage = If(srcLang = "", "auto", srcLang)

                        ' Store a concrete key when we have one; an empty key now falls
                        ' back to the global default at room start (ConferenceController
                        ' treats empty SttBackendKey like empty TranslationBackendKey).
                        If stt IsNot Nothing Then
                            If stt = "" Then stt = If(cfg.SttBackend, "")
                            If Not String.IsNullOrEmpty(stt) Then tpl.SttBackendKey = stt
                        End If

                        ' "" = follow the server's default translation engine (resolver falls back).
                        If trans IsNot Nothing Then tpl.TranslationBackendKey = trans

                        Dim audio = If(getStr("audioSource"), "web").ToLowerInvariant()
                        tpl.AudioSource = If(audio = "local", "local", "web")
                        Dim rawProp As JsonElement = Nothing
                        tpl.WebMicRaw = root.TryGetProperty("webMicRaw", rawProp) AndAlso rawProp.ValueKind = JsonValueKind.True

                        Dim vis = If(getStr("visibility"), "public").ToLowerInvariant()
                        tpl.DefaultVisibility = If(vis = "private", "private", "public")

                        ' Offered languages → 1:1 display template (Id = template Id).
                        Dim offered As New List(Of String)
                        Dim offProp As JsonElement = Nothing
                        If root.TryGetProperty("offeredLanguages", offProp) AndAlso offProp.ValueKind = JsonValueKind.Array Then
                            For Each el In offProp.EnumerateArray()
                                If el.ValueKind = JsonValueKind.String AndAlso Not String.IsNullOrWhiteSpace(el.GetString()) Then
                                    Dim code = el.GetString().Trim()
                                    If Not offered.Contains(code, StringComparer.OrdinalIgnoreCase) Then offered.Add(code)
                                End If
                            Next
                        End If
                        Dim libStore = Services.Config.TemplateLibraryStore.Instance
                        Dim existingDisplay = libStore.GetDisplayTemplate(If(tpl.DisplayTemplateId, ""))
                        If offered.Count > 0 Then
                            Dim dt = If(existingDisplay, New Models.Templates.DisplayTemplate With {.Id = tpl.Id, .Name = tplName})
                            dt.OfferedLanguages = offered
                            libStore.UpsertDisplayTemplate(dt)
                            tpl.DisplayTemplateId = dt.Id
                        ElseIf existingDisplay IsNot Nothing Then
                            ' Empty = offer all languages; keep the display template's
                            ' colours/layout, just clear the restriction.
                            existingDisplay.OfferedLanguages = New List(Of String)
                            libStore.UpsertDisplayTemplate(existingDisplay)
                        End If

                        SettingsSaveHandler?.Invoke()
                        SyncLock _templatesLock
                            context.RequestServices.GetService(Of Services.Rooms.TemplateStore)?.
                                SyncFromConfig(cfg.ConferenceTemplates)
                        End SyncLock
                        AppLogger.Log(LogCategory.Config, LogSeverity.Info,
                            $"/api/settings/templates: {If(id = "", "created", "updated")} '{tplName}' ({tpl.Id})")
                        Await context.Response.WriteAsJsonAsync(New With {.ok = True, .id = tpl.Id})
                    End Using
                End Function)

            app.MapPost("/api/settings/templates/delete",
                Async Function(context As HttpContext) As Task
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then
                        context.Response.StatusCode = 503
                        Await context.Response.WriteAsJsonAsync(New With {.error = "settings not available"})
                        Return
                    End If
                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) JsonStr(root, name)
                        If Not SettingsPinOk(context, getStr("pin")) Then
                            context.Response.StatusCode = 403
                            Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                            Return
                        End If
                        Dim id = If(getStr("id"), "")
                        Dim tpl As Models.ConferenceTemplate = Nothing
                        SyncLock _templatesLock
                            tpl = cfg.ConferenceTemplates?.FirstOrDefault(Function(x) x.Id = id)
                            If tpl IsNot Nothing Then cfg.ConferenceTemplates.Remove(tpl)
                        End SyncLock
                        If tpl Is Nothing Then
                            context.Response.StatusCode = 404
                            Await context.Response.WriteAsJsonAsync(New With {.error = "unknown template id"})
                            Return
                        End If
                        ' Remove the 1:1 display template we own; leave shared ones alone.
                        If tpl.DisplayTemplateId = tpl.Id Then
                            Services.Config.TemplateLibraryStore.Instance.DeleteDisplayTemplate(tpl.Id)
                        End If
                        SettingsSaveHandler?.Invoke()
                        SyncLock _templatesLock
                            context.RequestServices.GetService(Of Services.Rooms.TemplateStore)?.
                                SyncFromConfig(cfg.ConferenceTemplates)
                        End SyncLock
                        AppLogger.Log(LogCategory.Config, LogSeverity.Info,
                            $"/api/settings/templates: deleted '{tpl.Name}' ({tpl.Id})")
                        Await context.Response.WriteAsJsonAsync(New With {.ok = True})
                    End Using
                End Function)

            ' ── Raw config editor (Advanced) ─────────────────────────────────
            ' The blunt instrument that covers EVERY setting from a browser:
            ' GET returns the live AppConfig serialized exactly as config.json;
            ' POST validates the edited JSON, copies it onto the live config,
            ' hot-applies what the settings pipeline already handles (engines /
            ' keys / PIN / creator code) and flags the rest as needs-restart.
            ' Keys are visible in the JSON — acceptable behind the admin PIN
            ' (the admin owns the server and its config.json anyway).

            app.MapGet("/api/settings/rawconfig",
                Function(context As HttpContext) As IResult
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then Return Results.Json(New With {.error = "settings not available"}, statusCode:=503)
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                    End If
                    Return Results.Json(New With {
                        .json = JsonSerializer.Serialize(cfg, Models.ConfigManager.SerializerOptions)
                    })
                End Function)

            app.MapPost("/api/settings/rawconfig",
                Async Function(context As HttpContext) As Task
                    Dim cfg = SettingsConfigProvider?.Invoke()
                    If cfg Is Nothing Then
                        context.Response.StatusCode = 503
                        Await context.Response.WriteAsJsonAsync(New With {.error = "settings not available"})
                        Return
                    End If

                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) JsonStr(root, name)

                        If Not SettingsPinOk(context, getStr("pin")) Then
                            context.Response.StatusCode = 403
                            Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                            Return
                        End If

                        Dim jsonText = getStr("json")
                        If String.IsNullOrWhiteSpace(jsonText) Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {.error = "empty config"})
                            Return
                        End If

                        Dim parsed As Models.AppConfig = Nothing
                        Dim parseError As String = Nothing
                        Try
                            parsed = JsonSerializer.Deserialize(Of Models.AppConfig)(jsonText, Models.ConfigManager.SerializerOptions)
                        Catch ex As Exception
                            parseError = ex.Message
                        End Try
                        If parsed Is Nothing Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {
                                .error = If(parseError Is Nothing, "invalid config JSON", $"invalid config JSON: {parseError}")
                            })
                            Return
                        End If

                        ' Diff top-level properties (serialize-compare, so nested
                        ' objects and collections diff by value), then copy the
                        ' parsed config onto the LIVE instance — every consumer
                        ' holds a reference to that instance, and the head's save
                        ' handler persists it, so nothing diverges from disk.
                        Dim props = GetType(Models.AppConfig).GetProperties(
                            Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance).
                            Where(Function(p) p.CanRead AndAlso p.CanWrite AndAlso p.GetIndexParameters().Length = 0).
                            ToList()
                        Dim hadPin = Not String.IsNullOrEmpty(cfg.AdminPin)
                        Dim changed As New List(Of String)
                        For Each p In props
                            Dim newValue = p.GetValue(parsed)
                            Try
                                Dim oldJson = JsonSerializer.Serialize(p.GetValue(cfg), p.PropertyType, Models.ConfigManager.SerializerOptions)
                                Dim newJson = JsonSerializer.Serialize(newValue, p.PropertyType, Models.ConfigManager.SerializerOptions)
                                If Not String.Equals(oldJson, newJson, StringComparison.Ordinal) Then changed.Add(p.Name)
                            Catch
                                ' Can't compare → assume changed so the save still fires
                                ' (an unrecorded change that never persists is worse than
                                ' a spurious save).
                                changed.Add(p.Name)
                            End Try
                            p.SetValue(cfg, newValue)
                        Next

                        ' Hot-apply the subset the settings pipeline already handles.
                        Dim opts = context.RequestServices.GetService(Of IOptions(Of ServerOptions))
                        If opts?.Value IsNot Nothing Then
                            opts.Value.AdminPin = If(cfg.AdminPin, "")
                            opts.Value.CreatorCode = If(cfg.CreatorCode, "")
                        End If
                        If changed.Count > 0 Then
                            SettingsSaveHandler?.Invoke()
                            Services.Translation.TranslationBackendRegistry.ConfigureCloudApiKeys(context.RequestServices, cfg)
                            Services.Tts.TtsBackendRegistry.ConfigureCloudTtsKeys(context.RequestServices, cfg)
                            ' The raw editor can rewrite conferenceTemplates too — keep the
                            ' lobby's TemplateStore in step (same sync the template editor does).
                            SyncLock _templatesLock
                                context.RequestServices.GetService(Of Services.Rooms.TemplateStore)?.
                                    SyncFromConfig(cfg.ConferenceTemplates)
                            End SyncLock
                        End If

                        ' Everything outside this set is consumed at startup (ports,
                        ' paths, pipeline dials read into sessions) → needs restart.
                        Dim hotApplied As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
                            "SttBackend", "TranslationBackend",
                            "SttApiKeys", "TranslationApiKeys", "TtsApiKeys",
                            "AdminPin", "CreatorCode"
                        }
                        Dim needsRestart = changed.Any(Function(n) Not hotApplied.Contains(n))
                        Dim pinCleared = hadPin AndAlso String.IsNullOrEmpty(cfg.AdminPin)
                        AppLogger.Log(LogCategory.Config, LogSeverity.Info,
                            $"/api/settings/rawconfig applied: {changed.Count} propert{If(changed.Count = 1, "y", "ies")} changed " &
                            $"({String.Join(", ", changed)}){If(pinCleared, " — WARNING: admin PIN cleared, settings are now open", "")}")

                        Await context.Response.WriteAsJsonAsync(New With {
                            .ok = True, .changed = changed, .needsRestart = needsRestart, .pinCleared = pinCleared
                        })
                    End Using
                End Function)

            ' ── Bible downloads (eBible.org freely-redistributable catalog) ──
            ' Lite ships Bible-free (licensing) — this is how a headless deployment
            ' installs Bibles without a Windows machine. Copyrighted translations
            ' stay manual-copy into the Bibles folder; only the redistributable
            ' catalog is listed here. Downloads run in the background; the client
            ' polls this same GET for per-translation state.

            app.MapGet("/api/settings/bibles",
                Async Function(context As HttpContext) As Task
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        context.Response.StatusCode = 403
                        Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                        Return
                    End If
                    Dim bible = context.RequestServices.GetService(Of Services.Interfaces.IBibleService)
                    If bible Is Nothing Then
                        context.Response.StatusCode = 503
                        Await context.Response.WriteAsJsonAsync(New With {.error = "bible service not available"})
                        Return
                    End If

                    Dim biblesDir = bible.BiblesDirectory
                    Dim catalog As List(Of Services.Bible.BibleCatalogEntry) = Nothing
                    Dim fetchError As String = Nothing
                    Try
                        catalog = Await Services.Bible.BibleDownloadService.GetCatalogAsync(biblesDir)
                    Catch ex As Exception
                        fetchError = ex.Message
                    End Try
                    If catalog Is Nothing Then
                        ' Offline / eBible unreachable — the stale cache (if any) still works.
                        catalog = Services.Bible.BibleDownloadService.LoadCachedCatalog(biblesDir)
                        If catalog.Count = 0 Then
                            context.Response.StatusCode = 502
                            Await context.Response.WriteAsJsonAsync(New With {.error = $"catalog fetch failed: {fetchError}"})
                            Return
                        End If
                    End If

                    Dim installed = Services.Bible.BibleDownloadService.GetInstalledIds(biblesDir)
                    Await context.Response.WriteAsJsonAsync(New With {
                        .catalog = catalog.Select(Function(c) New With {
                            .id = c.TranslationId,
                            .lang = c.LanguageCode,
                            .langName = If(String.IsNullOrEmpty(c.LanguageNameEnglish), c.LanguageName, c.LanguageNameEnglish),
                            .title = c.Title,
                            .ot = c.OTBooks > 0,
                            .nt = c.NTBooks > 0,
                            .installed = installed.Contains(c.TranslationId)
                        }).ToList(),
                        .states = _bibleDownloadStates.ToDictionary(Function(kv) kv.Key, Function(kv) kv.Value)
                    })
                End Function)

            app.MapPost("/api/settings/bibles/download",
                Async Function(context As HttpContext) As Task
                    Using doc = Await JsonDocument.ParseAsync(context.Request.Body)
                        Dim root = doc.RootElement
                        Dim getStr = Function(name As String) JsonStr(root, name)

                        If Not SettingsPinOk(context, getStr("pin")) Then
                            context.Response.StatusCode = 403
                            Await context.Response.WriteAsJsonAsync(New With {.error = "invalid pin"})
                            Return
                        End If
                        Dim translationId = getStr("translationId")
                        If String.IsNullOrWhiteSpace(translationId) Then
                            context.Response.StatusCode = 400
                            Await context.Response.WriteAsJsonAsync(New With {.error = "translationId required"})
                            Return
                        End If
                        Dim bible = context.RequestServices.GetService(Of Services.Interfaces.IBibleService)
                        If bible Is Nothing Then
                            context.Response.StatusCode = 503
                            Await context.Response.WriteAsJsonAsync(New With {.error = "bible service not available"})
                            Return
                        End If

                        Dim biblesDir = bible.BiblesDirectory
                        Dim catalog = Services.Bible.BibleDownloadService.LoadCachedCatalog(biblesDir)
                        Dim entry = catalog.FirstOrDefault(
                            Function(c) String.Equals(c.TranslationId, translationId, StringComparison.OrdinalIgnoreCase))
                        If entry Is Nothing Then
                            context.Response.StatusCode = 404
                            Await context.Response.WriteAsJsonAsync(New With {.error = "unknown translationId (fetch the catalog first)"})
                            Return
                        End If

                        ' Idempotent: a re-POST while a download runs just reports the state.
                        Dim current As String = Nothing
                        If _bibleDownloadStates.TryGetValue(entry.TranslationId, current) AndAlso IsBibleDownloadActive(current) Then
                            Await context.Response.WriteAsJsonAsync(New With {.ok = True, .state = current})
                            Return
                        End If

                        _bibleDownloadStates(entry.TranslationId) = "downloading"
                        AppLogger.Log(LogEvents.DL_DOWNLOAD_START, $"Web Bible download: {entry.TranslationId} ({entry.Title}) → {biblesDir}")
                        Dim fireAndForget = Task.Run(
                            Async Function()
                                Try
                                    Dim issues = Await Services.Bible.BibleDownloadService.DownloadAndConvertAsync(
                                        entry, biblesDir,
                                        Sub(stage) _bibleDownloadStates(entry.TranslationId) = stage)
                                    If issues.Count > 0 Then
                                        AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR,
                                            $"Bible {entry.TranslationId} integrity: {String.Join("; ", issues)}")
                                    End If
                                    bible.RescanTranslations()
                                    _bibleDownloadStates(entry.TranslationId) = "done"
                                    AppLogger.Log(LogEvents.DL_DOWNLOAD_DONE, $"Web Bible download complete: {entry.TranslationId}")
                                    Try
                                        BibleLibraryChangedHandler?.Invoke()
                                    Catch hex As Exception
                                        ' A head-side refresh failure must never flip a
                                        ' completed download's state to error.
                                        AppLogger.Log(LogEvents.UI_ERROR, $"BibleLibraryChangedHandler: {hex.Message}")
                                    End Try
                                Catch ex As Exception
                                    _bibleDownloadStates(entry.TranslationId) = "error: " & ex.Message
                                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"Web Bible download failed: {entry.TranslationId} — {ex.Message}")
                                End Try
                            End Function)

                        Await context.Response.WriteAsJsonAsync(New With {.ok = True, .state = "downloading"})
                    End Using
                End Function)

            ' Lightweight poll target while downloads run — the full GET re-parses
            ' the catalog CSV and rescans the Bibles tree, far too heavy for the
            ' client's 2.5s progress loop. This returns only the states map.
            app.MapGet("/api/settings/bibles/status",
                Function(context As HttpContext) As IResult
                    If Not SettingsPinOk(context, context.Request.Query("pin").ToString()) Then
                        Return Results.Json(New With {.error = "invalid pin"}, statusCode:=403)
                    End If
                    Return Results.Json(New With {
                        .states = _bibleDownloadStates.ToDictionary(Function(kv) kv.Key, Function(kv) kv.Value)
                    })
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
