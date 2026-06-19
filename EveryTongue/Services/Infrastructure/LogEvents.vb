Namespace Services.Infrastructure

    ''' <summary>
    ''' All log event ID constants. Each category gets a 100-block range.
    ''' New events are added here and registered in the Shared Sub New().
    ''' Migrate existing AppLogger.Log() calls to use these IDs — Phase 3.
    ''' </summary>
    Public Module LogEvents

        ' ── Legacy (0) — unregistered calls via old Log(msg) overload ──
        Public Const LEGACY As Integer = 0

        ' ── Startup (1000–1099) ──
        Public Const STARTUP_APP_STARTED As Integer = 1000
        Public Const STARTUP_FIRST_RUN As Integer = 1001
        Public Const STARTUP_DEPENDENCY_CHECK As Integer = 1002
        Public Const STARTUP_CRASH_DETECTED As Integer = 1003
        Public Const STARTUP_INTEGRITY_FAIL As Integer = 1004
        Public Const STARTUP_SESSION_SUMMARY As Integer = 1099

        ' ── Config (1100–1199) ──
        Public Const CONFIG_LOADED As Integer = 1100
        Public Const CONFIG_SAVED As Integer = 1101
        Public Const CONFIG_RESET As Integer = 1102
        Public Const CONFIG_LOAD_FAILED As Integer = 1103
        Public Const CONFIG_SAVE_FAILED As Integer = 1104
        Public Const CONFIG_MIGRATED As Integer = 1105
        Public Const CONFIG_TEMPLATE_LIB_LOADED As Integer = 1106
        Public Const CONFIG_TEMPLATE_LIB_SAVED As Integer = 1107
        Public Const CONFIG_TEMPLATE_LIB_ERROR As Integer = 1108
        Public Const CONFIG_ENGINE_RESOLVED As Integer = 1109
        Public Const CONFIG_TEMPLATE_RESOLVED As Integer = 1110
        Public Const CONFIG_OVERRIDE_APPLIED As Integer = 1111
        Public Const CONFIG_ENGINE_VALIDATION As Integer = 1112
        Public Const CONFIG_GATE_DECISION As Integer = 1113

        ' ── Server (1200–1299) ──
        Public Const SERVER_STARTING As Integer = 1200
        Public Const SERVER_STARTED As Integer = 1201
        Public Const SERVER_STOPPED As Integer = 1202
        Public Const SERVER_ERROR As Integer = 1203
        Public Const SERVER_ENDPOINT_CALLED As Integer = 1204

        ' ── Pipeline (2000–2099) ──
        Public Const PIPELINE_SIDECAR_STARTING As Integer = 2000
        Public Const PIPELINE_SIDECAR_STARTED As Integer = 2001
        Public Const PIPELINE_SIDECAR_EXITED As Integer = 2002
        Public Const PIPELINE_SIDECAR_RESTART As Integer = 2003
        Public Const PIPELINE_SIDECAR_STOP As Integer = 2004
        Public Const PIPELINE_SIDECAR_ERROR As Integer = 2005
        Public Const PIPELINE_PROCESS_KILL As Integer = 2006
        Public Const PIPELINE_LOG_TAIL_ERROR As Integer = 2007

        ' ── STT (3000–3099) ──
        Public Const STT_SESSION_START As Integer = 3000
        Public Const STT_SESSION_STOP As Integer = 3001
        Public Const STT_VAD_STATE_CHANGE As Integer = 3002
        Public Const STT_INFERENCE_START As Integer = 3003
        Public Const STT_INFERENCE_RESULT As Integer = 3004
        Public Const STT_COMMIT As Integer = 3005
        Public Const STT_WHISPER_SERVER_START As Integer = 3006
        Public Const STT_WHISPER_SERVER_STOP As Integer = 3007
        Public Const STT_WHISPER_SERVER_ERROR As Integer = 3008
        Public Const STT_HEALTH_CHECK As Integer = 3009

        ' ── Translation (4000–4099) ──
        Public Const TRANS_SERVER_STARTING As Integer = 4000
        Public Const TRANS_SERVER_READY As Integer = 4001
        Public Const TRANS_MODEL_LOADING As Integer = 4002
        Public Const TRANS_MODEL_LOADED As Integer = 4003
        Public Const TRANS_REQUEST As Integer = 4004
        Public Const TRANS_RESULT As Integer = 4005
        Public Const TRANS_ERROR As Integer = 4006
        Public Const TRANS_CUDA_FALLBACK As Integer = 4007
        Public Const TRANS_CLOUD_REQUEST As Integer = 4008
        Public Const TRANS_BACKEND_FALLBACK As Integer = 4009
        Public Const TRANS_BACKEND_ACTIVE As Integer = 4010
        Public Const TRANS_BUDGET_EXCEEDED As Integer = 4011

        ' ── TTS (4100–4199) ──
        Public Const TTS_SYNTHESISE As Integer = 4100
        Public Const TTS_SYNTHESISE_DONE As Integer = 4101
        Public Const TTS_ENGINE_START As Integer = 4102
        Public Const TTS_ENGINE_STOP As Integer = 4103
        Public Const TTS_ENGINE_ERROR As Integer = 4104
        Public Const TTS_PLAYBACK As Integer = 4105
        Public Const TTS_BACKEND_CONFIGURED As Integer = 4106

        ' ── Conference (5000–5099) ──
        Public Const CONF_BACKEND_STARTING As Integer = 5000
        Public Const CONF_BACKEND_STARTED As Integer = 5001
        Public Const CONF_BACKEND_STOPPED As Integer = 5002
        Public Const CONF_BACKEND_ERROR As Integer = 5003
        Public Const CONF_BACKEND_RESET As Integer = 5004
        Public Const CONF_COMMIT As Integer = 5005
        Public Const CONF_PIPELINE_CONFIG As Integer = 5006
        Public Const CONF_PIPELINE_RESTART As Integer = 5007
        Public Const CONF_COMMIT_DROPPED As Integer = 5008
        Public Const CONF_CLAUSE_LOCK As Integer = 5009
        Public Const CONF_CLAUSE_FRAGMENT As Integer = 5010
        Public Const CONF_SPEAKER_SWITCHED As Integer = 5011

        ' ── Rooms (5100–5199) ──
        Public Const ROOM_CREATED As Integer = 5100
        Public Const ROOM_CLOSED As Integer = 5101
        Public Const ROOM_CLIENT_JOINED As Integer = 5102
        Public Const ROOM_CLIENT_LEFT As Integer = 5103
        Public Const ROOM_LOCKED As Integer = 5104
        Public Const ROOM_PAUSED As Integer = 5105
        Public Const ROOM_EXPIRED As Integer = 5106
        Public Const ROOM_TRANSLATION_ROUTING As Integer = 5107
        Public Const ROOM_READINESS As Integer = 5108

        ' ── Subtitle (5200–5299) ──
        Public Const SUB_BROADCAST As Integer = 5200
        Public Const SUB_CLIENT_CONNECTED As Integer = 5201
        Public Const SUB_CLIENT_DISCONNECTED As Integer = 5202
        Public Const SUB_TTS_FIRED As Integer = 5203
        Public Const SUB_SEND_ERROR As Integer = 5204
        Public Const SUB_LANG_CHANGE As Integer = 5205
        Public Const SUB_INPUT_LANG_CHANGE As Integer = 5206

        ' ── Bible (6000–6099) ──
        Public Const BIBLE_LOOKUP As Integer = 6000
        Public Const BIBLE_DOWNLOAD As Integer = 6001
        Public Const BIBLE_ERROR As Integer = 6002

        ' ── Download (6100–6199) ──
        Public Const DL_CHECK_START As Integer = 6100
        Public Const DL_CHECK_RESULT As Integer = 6101
        Public Const DL_DOWNLOAD_START As Integer = 6102
        Public Const DL_DOWNLOAD_DONE As Integer = 6103
        Public Const DL_DOWNLOAD_ERROR As Integer = 6104
        Public Const DL_INSTALL As Integer = 6105

        ' ── Audio (6200–6299) ──
        Public Const AUDIO_DEVICE_SELECTED As Integer = 6200
        Public Const AUDIO_PLAYBACK_ERROR As Integer = 6201

        ' ── Localization (6300–6399) ──
        Public Const LOCALE_LOADED As Integer = 6300
        Public Const LOCALE_FALLBACK As Integer = 6301
        Public Const LOCALE_MISSING_KEY As Integer = 6302

        ' ── Update (6400–6499) ──
        Public Const UPDATE_CHECK As Integer = 6400
        Public Const UPDATE_AVAILABLE As Integer = 6401
        Public Const UPDATE_ERROR As Integer = 6402

        ' ── Hardware (6500–6599) ──
        Public Const HW_SCAN_START As Integer = 6500
        Public Const HW_SCAN_RESULT As Integer = 6501
        Public Const HW_SCAN_ERROR As Integer = 6502

        ' ── Benchmark (7000–7099) ──
        Public Const BENCH_START As Integer = 7000
        Public Const BENCH_PROGRESS As Integer = 7001
        Public Const BENCH_RESULT As Integer = 7002
        Public Const BENCH_ERROR As Integer = 7003
        Public Const BENCH_COMPLETE As Integer = 7004

        ' ── UI (8000–8099) ──
        Public Const UI_THEME_CHANGED As Integer = 8000
        ' 8001/8002 (workspace switch, dialog opened) retired — never logged
        Public Const UI_ERROR As Integer = 8003
        Public Const UI_LOG_VIEWER_OPENED As Integer = 8004
        Public Const DICT_SESSION_STARTED As Integer = 8010
        Public Const DICT_SESSION_STOPPED As Integer = 8011
        Public Const DICT_SESSION_ERROR As Integer = 8012
        Public Const DICT_COMMIT As Integer = 8013
        Public Const DICT_TRANSLATE As Integer = 8014
        Public Const DICT_INJECT_ERROR As Integer = 8015
        Public Const DICT_HOTKEY As Integer = 8016

        ' ── Python Log (9000–9299) ──
        ' Each server gets base+0=Info, base+1=Debug, base+2=Warning, base+3=Error
        Public Const PYLOG_LIVE As Integer = 9000
        Public Const PYLOG_LIVE_DEBUG As Integer = 9001
        Public Const PYLOG_LIVE_WARN As Integer = 9002
        Public Const PYLOG_LIVE_ERROR As Integer = 9003
        Public Const PYLOG_TRANSLATE As Integer = 9100
        Public Const PYLOG_TRANSLATE_DEBUG As Integer = 9101
        Public Const PYLOG_TRANSLATE_WARN As Integer = 9102
        Public Const PYLOG_TRANSLATE_ERROR As Integer = 9103
        Public Const PYLOG_MMS_TTS As Integer = 9200
        Public Const PYLOG_MMS_TTS_DEBUG As Integer = 9201
        Public Const PYLOG_MMS_TTS_WARN As Integer = 9202
        Public Const PYLOG_MMS_TTS_ERROR As Integer = 9203

        ''' <summary>
        ''' Register all events. Called by LogEventRegistry.Sub New() since
        ''' Public Const fields are inlined and this module's constructor won't fire.
        ''' </summary>
        Public Sub RegisterAll()
            Dim R = Sub(id As Integer, cat As LogCategory, lvl As LogSeverity, desc As String)
                        LogEventRegistry.Register(id, cat, lvl, desc)
                    End Sub

            R(LEGACY, LogCategory.Legacy, LogSeverity.Info, "Unregistered legacy log call")

            ' Startup
            R(STARTUP_APP_STARTED, LogCategory.Startup, LogSeverity.Info, "Application started")
            R(STARTUP_FIRST_RUN, LogCategory.Startup, LogSeverity.Info, "First-run setup initiated")
            R(STARTUP_DEPENDENCY_CHECK, LogCategory.Startup, LogSeverity.Info, "Dependency check running")
            R(STARTUP_CRASH_DETECTED, LogCategory.Startup, LogSeverity.Warning, "Previous session did not exit cleanly")
            R(STARTUP_INTEGRITY_FAIL, LogCategory.Startup, LogSeverity.Warning, "File failed the startup integrity check (modified or missing vs manifest)")
            R(STARTUP_SESSION_SUMMARY, LogCategory.Startup, LogSeverity.Info, "Session summary on shutdown")

            ' Config
            R(CONFIG_LOADED, LogCategory.Config, LogSeverity.Info, "Configuration loaded from disk")
            R(CONFIG_SAVED, LogCategory.Config, LogSeverity.Debug, "Configuration saved to disk")
            R(CONFIG_RESET, LogCategory.Config, LogSeverity.Info, "Configuration reset to defaults")
            R(CONFIG_LOAD_FAILED, LogCategory.Config, LogSeverity.[Error], "Configuration load failed")
            R(CONFIG_SAVE_FAILED, LogCategory.Config, LogSeverity.[Error], "Configuration save failed")
            R(CONFIG_MIGRATED, LogCategory.Config, LogSeverity.Info, "Configuration migrated from older version")
            R(CONFIG_TEMPLATE_LIB_LOADED, LogCategory.Config, LogSeverity.Info, "Template library loaded from disk")
            R(CONFIG_TEMPLATE_LIB_SAVED, LogCategory.Config, LogSeverity.Debug, "Template library saved to disk")
            R(CONFIG_TEMPLATE_LIB_ERROR, LogCategory.Config, LogSeverity.[Error], "Template library load/save failed")
            R(CONFIG_ENGINE_RESOLVED, LogCategory.Config, LogSeverity.Debug, "Engine config block resolved (defaults + machine baseline)")
            R(CONFIG_TEMPLATE_RESOLVED, LogCategory.Config, LogSeverity.Info, "Named template applied to a session slot")
            R(CONFIG_OVERRIDE_APPLIED, LogCategory.Config, LogSeverity.Debug, "Per-session config overrides applied")
            R(CONFIG_ENGINE_VALIDATION, LogCategory.Config, LogSeverity.Warning, "Engine config validation warning")
            R(CONFIG_GATE_DECISION, LogCategory.Config, LogSeverity.Info, "Online/Offline gate decision (engine eligibility)")

            ' Server
            R(SERVER_STARTING, LogCategory.Server, LogSeverity.Info, "Kestrel web server starting")
            R(SERVER_STARTED, LogCategory.Server, LogSeverity.Info, "Kestrel web server started")
            R(SERVER_STOPPED, LogCategory.Server, LogSeverity.Info, "Kestrel web server stopped")
            R(SERVER_ERROR, LogCategory.Server, LogSeverity.[Error], "Kestrel web server error")
            R(SERVER_ENDPOINT_CALLED, LogCategory.Server, LogSeverity.Debug, "API endpoint called")

            ' Pipeline
            R(PIPELINE_SIDECAR_STARTING, LogCategory.Pipeline, LogSeverity.Info, "Python sidecar process starting")
            R(PIPELINE_SIDECAR_STARTED, LogCategory.Pipeline, LogSeverity.Info, "Python sidecar process started")
            R(PIPELINE_SIDECAR_EXITED, LogCategory.Pipeline, LogSeverity.Warning, "Python sidecar process exited")
            R(PIPELINE_SIDECAR_RESTART, LogCategory.Pipeline, LogSeverity.Warning, "Python sidecar process restarting")
            R(PIPELINE_SIDECAR_STOP, LogCategory.Pipeline, LogSeverity.Info, "Python sidecar process stopped")
            R(PIPELINE_SIDECAR_ERROR, LogCategory.Pipeline, LogSeverity.[Error], "Python sidecar process error")
            R(PIPELINE_PROCESS_KILL, LogCategory.Pipeline, LogSeverity.Debug, "Killed process on port")
            R(PIPELINE_LOG_TAIL_ERROR, LogCategory.Pipeline, LogSeverity.[Error], "Log file tail failed")

            ' STT
            R(STT_SESSION_START, LogCategory.Stt, LogSeverity.Info, "STT session started")
            R(STT_SESSION_STOP, LogCategory.Stt, LogSeverity.Info, "STT session stopped")
            R(STT_VAD_STATE_CHANGE, LogCategory.Stt, LogSeverity.Debug, "VAD state changed")
            R(STT_INFERENCE_START, LogCategory.Stt, LogSeverity.Debug, "Whisper inference started")
            R(STT_INFERENCE_RESULT, LogCategory.Stt, LogSeverity.Debug, "Whisper inference result")
            R(STT_COMMIT, LogCategory.Stt, LogSeverity.Info, "STT text committed")
            R(STT_WHISPER_SERVER_START, LogCategory.Stt, LogSeverity.Info, "whisper-server process started")
            R(STT_WHISPER_SERVER_STOP, LogCategory.Stt, LogSeverity.Info, "whisper-server process stopped")
            R(STT_WHISPER_SERVER_ERROR, LogCategory.Stt, LogSeverity.[Error], "whisper-server error")
            R(STT_HEALTH_CHECK, LogCategory.Stt, LogSeverity.Debug, "STT health check")

            ' Translation
            R(TRANS_SERVER_STARTING, LogCategory.Translation, LogSeverity.Info, "Translation server starting")
            R(TRANS_SERVER_READY, LogCategory.Translation, LogSeverity.Info, "Translation server ready")
            R(TRANS_MODEL_LOADING, LogCategory.Translation, LogSeverity.Info, "Translation model loading")
            R(TRANS_MODEL_LOADED, LogCategory.Translation, LogSeverity.Info, "Translation model loaded")
            R(TRANS_REQUEST, LogCategory.Translation, LogSeverity.Debug, "Translation request sent")
            R(TRANS_RESULT, LogCategory.Translation, LogSeverity.Info, "Translation result (backend, source→targets, timing)")
            R(TRANS_ERROR, LogCategory.Translation, LogSeverity.[Error], "Translation failed")
            R(TRANS_CUDA_FALLBACK, LogCategory.Translation, LogSeverity.Warning, "CUDA error, falling back to CPU")
            R(TRANS_CLOUD_REQUEST, LogCategory.Translation, LogSeverity.Debug, "Cloud translation API request")
            R(TRANS_BACKEND_FALLBACK, LogCategory.Translation, LogSeverity.Warning, "Translation backend failed/unavailable — fell back to the local sidecar")
            R(TRANS_BACKEND_ACTIVE, LogCategory.Translation, LogSeverity.Info, "Active translation backend changed")
            R(TRANS_BUDGET_EXCEEDED, LogCategory.Translation, LogSeverity.Warning, "Cloud translation monthly character budget exceeded (translation continues)")

            ' TTS
            R(TTS_SYNTHESISE, LogCategory.Tts, LogSeverity.Debug, "TTS synthesis requested")
            R(TTS_SYNTHESISE_DONE, LogCategory.Tts, LogSeverity.Info, "TTS synthesis complete (engine, language, timing)")
            R(TTS_ENGINE_START, LogCategory.Tts, LogSeverity.Info, "TTS engine started")
            R(TTS_ENGINE_STOP, LogCategory.Tts, LogSeverity.Info, "TTS engine stopped")
            R(TTS_ENGINE_ERROR, LogCategory.Tts, LogSeverity.[Error], "TTS engine error")
            R(TTS_PLAYBACK, LogCategory.Tts, LogSeverity.Debug, "TTS audio playback")
            R(TTS_BACKEND_CONFIGURED, LogCategory.Tts, LogSeverity.Info, "Cloud TTS backend API key/endpoint configured")

            ' Conference
            R(CONF_BACKEND_STARTING, LogCategory.Conference, LogSeverity.Info, "Conference backend starting")
            R(CONF_BACKEND_STARTED, LogCategory.Conference, LogSeverity.Info, "Conference backend started")
            R(CONF_BACKEND_STOPPED, LogCategory.Conference, LogSeverity.Info, "Conference backend stopped")
            R(CONF_BACKEND_ERROR, LogCategory.Conference, LogSeverity.[Error], "Conference backend error")
            R(CONF_BACKEND_RESET, LogCategory.Conference, LogSeverity.Warning, "Conference backend reset by host")
            R(CONF_COMMIT, LogCategory.Conference, LogSeverity.Info, "Conference text committed")
            R(CONF_PIPELINE_CONFIG, LogCategory.Conference, LogSeverity.Info, "Conference pipeline config changed")
            R(CONF_PIPELINE_RESTART, LogCategory.Conference, LogSeverity.Warning, "Conference pipeline restarting")
            R(CONF_COMMIT_DROPPED, LogCategory.Conference, LogSeverity.Debug, "Conference commit dropped (paused)")
            R(CONF_CLAUSE_LOCK, LogCategory.Conference, LogSeverity.Info, "Speechmatics clause locked (hold-and-lock diagnostics for tuning dials)")
            R(CONF_CLAUSE_FRAGMENT, LogCategory.Conference, LogSeverity.Debug, "Speechmatics clause fragment accumulated (inter-fragment gap trace)")
            R(CONF_SPEAKER_SWITCHED, LogCategory.Conference, LogSeverity.Info, "Conference active speaker or connectivity mode changed")

            ' Rooms
            R(ROOM_CREATED, LogCategory.Rooms, LogSeverity.Info, "Room created")
            R(ROOM_CLOSED, LogCategory.Rooms, LogSeverity.Info, "Room closed")
            R(ROOM_CLIENT_JOINED, LogCategory.Rooms, LogSeverity.Info, "Client joined room")
            R(ROOM_CLIENT_LEFT, LogCategory.Rooms, LogSeverity.Info, "Client left room")
            R(ROOM_LOCKED, LogCategory.Rooms, LogSeverity.Info, "Room lock toggled")
            R(ROOM_PAUSED, LogCategory.Rooms, LogSeverity.Info, "Room paused/resumed")
            R(ROOM_EXPIRED, LogCategory.Rooms, LogSeverity.Info, "Room expired due to inactivity")
            R(ROOM_TRANSLATION_ROUTING, LogCategory.Rooms, LogSeverity.Info, "Conversation-room translation routing (recipients, languages, targets)")
            R(ROOM_READINESS, LogCategory.Rooms, LogSeverity.Info, "Room engine readiness (STT/translation preparing → ready) relayed to clients")

            ' Subtitle
            R(SUB_BROADCAST, LogCategory.Subtitle, LogSeverity.Debug, "Subtitle broadcast to clients")
            R(SUB_CLIENT_CONNECTED, LogCategory.Subtitle, LogSeverity.Info, "WebSocket client connected")
            R(SUB_CLIENT_DISCONNECTED, LogCategory.Subtitle, LogSeverity.Info, "WebSocket client disconnected")
            R(SUB_TTS_FIRED, LogCategory.Subtitle, LogSeverity.Debug, "Server-side TTS triggered for commit")
            R(SUB_SEND_ERROR, LogCategory.Subtitle, LogSeverity.Warning, "WebSocket send failed")
            R(SUB_LANG_CHANGE, LogCategory.Subtitle, LogSeverity.Debug, "Client display-language changed")
            R(SUB_INPUT_LANG_CHANGE, LogCategory.Subtitle, LogSeverity.Info, "Desktop input language changed")

            ' Bible
            R(BIBLE_LOOKUP, LogCategory.Bible, LogSeverity.Debug, "Bible verse lookup")
            R(BIBLE_DOWNLOAD, LogCategory.Bible, LogSeverity.Info, "Bible translation downloaded")
            R(BIBLE_ERROR, LogCategory.Bible, LogSeverity.[Error], "Bible operation failed")

            ' Download
            R(DL_CHECK_START, LogCategory.Download, LogSeverity.Info, "Dependency check started")
            R(DL_CHECK_RESULT, LogCategory.Download, LogSeverity.Info, "Dependency check result")
            R(DL_DOWNLOAD_START, LogCategory.Download, LogSeverity.Info, "Download started")
            R(DL_DOWNLOAD_DONE, LogCategory.Download, LogSeverity.Info, "Download completed")
            R(DL_DOWNLOAD_ERROR, LogCategory.Download, LogSeverity.[Error], "Download failed")
            R(DL_INSTALL, LogCategory.Download, LogSeverity.Info, "Component installed")

            ' Audio
            R(AUDIO_DEVICE_SELECTED, LogCategory.Audio, LogSeverity.Info, "Audio device selected")
            R(AUDIO_PLAYBACK_ERROR, LogCategory.Audio, LogSeverity.[Error], "Audio playback error")

            ' Localization
            R(LOCALE_LOADED, LogCategory.Localization, LogSeverity.Info, "Language pack loaded")
            R(LOCALE_FALLBACK, LogCategory.Localization, LogSeverity.Warning, "Locale fallback to embedded English")
            R(LOCALE_MISSING_KEY, LogCategory.Localization, LogSeverity.Debug, "Missing localization key")

            ' Update
            R(UPDATE_CHECK, LogCategory.Update, LogSeverity.Info, "Checking for updates")
            R(UPDATE_AVAILABLE, LogCategory.Update, LogSeverity.Info, "Update available")
            R(UPDATE_ERROR, LogCategory.Update, LogSeverity.[Error], "Update check failed")

            ' Hardware
            R(HW_SCAN_START, LogCategory.Hardware, LogSeverity.Info, "Hardware scan started")
            R(HW_SCAN_RESULT, LogCategory.Hardware, LogSeverity.Debug, "Hardware scan result")
            R(HW_SCAN_ERROR, LogCategory.Hardware, LogSeverity.[Error], "Hardware scan failed")

            ' Benchmark
            R(BENCH_START, LogCategory.Benchmark, LogSeverity.Info, "Benchmark started")
            R(BENCH_PROGRESS, LogCategory.Benchmark, LogSeverity.Debug, "Benchmark progress update")
            R(BENCH_RESULT, LogCategory.Benchmark, LogSeverity.Info, "Benchmark result")
            R(BENCH_ERROR, LogCategory.Benchmark, LogSeverity.[Error], "Benchmark error")
            R(BENCH_COMPLETE, LogCategory.Benchmark, LogSeverity.Info, "Benchmark complete")

            ' UI
            R(UI_THEME_CHANGED, LogCategory.UI, LogSeverity.Debug, "Theme changed")
            R(UI_ERROR, LogCategory.UI, LogSeverity.[Error], "UI error")
            R(UI_LOG_VIEWER_OPENED, LogCategory.UI, LogSeverity.Debug, "Log viewer opened")
            R(DICT_SESSION_STARTED, LogCategory.UI, LogSeverity.Info, "Dictation session started")
            R(DICT_SESSION_STOPPED, LogCategory.UI, LogSeverity.Info, "Dictation session stopped")
            R(DICT_SESSION_ERROR, LogCategory.UI, LogSeverity.[Error], "Dictation session error")
            R(DICT_COMMIT, LogCategory.UI, LogSeverity.Info, "Dictation text injected")
            R(DICT_TRANSLATE, LogCategory.UI, LogSeverity.Debug, "Dictation translated before inject")
            R(DICT_INJECT_ERROR, LogCategory.UI, LogSeverity.[Error], "Dictation injection error")
            R(DICT_HOTKEY, LogCategory.UI, LogSeverity.Debug, "Dictation hotkey event")

            ' Python log lines (tailed from files) — base+0=Info, base+1=Debug, base+2=Warning, base+3=Error
            R(PYLOG_LIVE, LogCategory.PythonLog, LogSeverity.Info, "Live server log line")
            R(PYLOG_LIVE_DEBUG, LogCategory.PythonLog, LogSeverity.Debug, "Live server debug")
            R(PYLOG_LIVE_WARN, LogCategory.PythonLog, LogSeverity.Warning, "Live server warning")
            R(PYLOG_LIVE_ERROR, LogCategory.PythonLog, LogSeverity.[Error], "Live server error")
            R(PYLOG_TRANSLATE, LogCategory.PythonLog, LogSeverity.Info, "Translation server log line")
            R(PYLOG_TRANSLATE_DEBUG, LogCategory.PythonLog, LogSeverity.Debug, "Translation server debug")
            R(PYLOG_TRANSLATE_WARN, LogCategory.PythonLog, LogSeverity.Warning, "Translation server warning")
            R(PYLOG_TRANSLATE_ERROR, LogCategory.PythonLog, LogSeverity.[Error], "Translation server error")
            R(PYLOG_MMS_TTS, LogCategory.PythonLog, LogSeverity.Info, "MMS-TTS server log line")
            R(PYLOG_MMS_TTS_DEBUG, LogCategory.PythonLog, LogSeverity.Debug, "MMS-TTS server debug")
            R(PYLOG_MMS_TTS_WARN, LogCategory.PythonLog, LogSeverity.Warning, "MMS-TTS server warning")
            R(PYLOG_MMS_TTS_ERROR, LogCategory.PythonLog, LogSeverity.[Error], "MMS-TTS server error")
        End Sub

    End Module

End Namespace
