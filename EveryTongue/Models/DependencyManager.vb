Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Tts

Namespace Models

    Public Enum ToolStatus
        Missing
        Installed
        UpdateAvailable
        UpToDate
        CheckFailed
    End Enum

    Public Class ToolState
        Public Property Name As String = ""
        Public Property Status As ToolStatus = ToolStatus.Missing
        Public Property InstalledVersion As String = ""
        Public Property LatestVersion As String = ""
        Public Property DownloadUrl As String = ""
    End Class

    Public Class DependencyManager

        Private Shared ReadOnly _client As New HttpClient()
        Private ReadOnly _toolsDir As String
        Private ReadOnly _config As AppConfig
        Private _versions As Dictionary(Of String, String)

        Private Shared ReadOnly _downloadClient As New HttpClient()

        Shared Sub New()
            _client.DefaultRequestHeaders.UserAgent.ParseAdd("EveryTongue-DependencyManager")
            _client.Timeout = TimeSpan.FromSeconds(30)

            _downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("EveryTongue-DependencyManager")
            _downloadClient.Timeout = TimeSpan.FromMinutes(30)
        End Sub

        Public Sub New(config As AppConfig, toolsDir As String)
            _config = config
            _toolsDir = toolsDir
            If Not Directory.Exists(_toolsDir) Then
                Directory.CreateDirectory(_toolsDir)
            End If
            _versions = LoadVersions()
        End Sub

        ' ──────────────────────────────────────────
        '  Version tracking (tool-versions.json)
        ' ──────────────────────────────────────────

        Private Function VersionsFilePath() As String
            Return Path.Combine(_toolsDir, "tool-versions.json")
        End Function

        Private Function LoadVersions() As Dictionary(Of String, String)
            Try
                Dim path = VersionsFilePath()
                If File.Exists(path) Then
                    Dim json = File.ReadAllText(path)
                    Return JsonSerializer.Deserialize(Of Dictionary(Of String, String))(json)
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"Failed to load tool versions: {ex.Message}")
            End Try
            Return New Dictionary(Of String, String)()
        End Function

        Private Sub SaveVersion(toolName As String, version As String)
            _versions(toolName) = version
            Try
                Dim json = JsonSerializer.Serialize(_versions, New JsonSerializerOptions With {.WriteIndented = True})
                File.WriteAllText(VersionsFilePath(), json)
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"Failed to save tool versions: {ex.Message}")
            End Try
        End Sub

        Private Function GetSavedVersion(toolName As String) As String
            Dim ver As String = Nothing
            If _versions.TryGetValue(toolName, ver) Then Return ver
            Return ""
        End Function

        Public ReadOnly Property ToolsDirectory As String
            Get
                Return _toolsDir
            End Get
        End Property

        ' ──────────────────────────────────────────
        '  Check all tools
        ' ──────────────────────────────────────────

        Public Async Function CheckAllToolsAsync() As Task(Of List(Of ToolState))
            Dim tasks As New List(Of Task(Of ToolState)) From {
                CheckYtDlpAsync(),
                CheckFfmpegAsync(),
                CheckModelAsync(),
                CheckSubtitleEditAsync(),
                CheckWhisperServerAsync(),
                CheckWhisperServerCudaAsync(),
                CheckGgmlModelAsync(),
                CheckFasterWhisperModelAsync(),
                CheckSileroVadModelAsync(),
                CheckNllbModelAsync(),
                CheckNllb33bModelAsync(),
                CheckPiperAsync()
            }
            Await Task.WhenAll(tasks)
            Return tasks.Select(Function(t) t.Result).ToList()
        End Function

        Public Function GetMissingTools(states As List(Of ToolState)) As List(Of ToolState)
            Return states.Where(Function(s) s.Status = ToolStatus.Missing).ToList()
        End Function

        Public Function GetUpdatableTools(states As List(Of ToolState)) As List(Of ToolState)
            Return states.Where(Function(s) s.Status = ToolStatus.UpdateAvailable).ToList()
        End Function

        ' ──────────────────────────────────────────
        '  yt-dlp
        ' ──────────────────────────────────────────

        Private Function YtDlpInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathYtdlp)
        End Function

        Private Function YtDlpDownloadPath() As String
            Return Path.Combine(_toolsDir, "yt-dlp.exe")
        End Function

        Public Async Function CheckYtDlpAsync() As Task(Of ToolState)
            Dim state As New ToolState With {.Name = "yt-dlp"}
            Try
                Dim installedPath = YtDlpInstalledPath()
                If File.Exists(installedPath) Then
                    state.InstalledVersion = GetSavedVersion("yt-dlp")
                    ' Fall back to running --version if no saved version
                    If String.IsNullOrEmpty(state.InstalledVersion) Then
                        state.InstalledVersion = Await GetProcessOutputAsync(installedPath, "--version")
                        ' Persist the detected version for future checks
                        If Not String.IsNullOrEmpty(state.InstalledVersion) Then
                            SaveVersion("yt-dlp", state.InstalledVersion)
                        End If
                    End If
                    If String.IsNullOrEmpty(state.InstalledVersion) Then state.InstalledVersion = "unknown"
                    state.Status = ToolStatus.Installed
                End If

                Dim release = Await GetLatestReleaseAsync("yt-dlp/yt-dlp")
                If release IsNot Nothing Then
                    state.LatestVersion = release.Value.TagName
                    state.DownloadUrl = FindAsset(release.Value.Assets, "^yt-dlp\.exe$")

                    If state.Status = ToolStatus.Installed Then
                        state.Status = CompareVersionTags(state.InstalledVersion, state.LatestVersion)
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"yt-dlp check failed: {ex.Message}")
                If state.Status = ToolStatus.Missing Then state.Status = ToolStatus.CheckFailed
            End Try
            Return state
        End Function

        Public Async Function DownloadYtDlpAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Await DownloadFileAsync(url, YtDlpDownloadPath(), progress)
        End Function

        ' ──────────────────────────────────────────
        '  FFmpeg + FFprobe
        ' ──────────────────────────────────────────

        Private Function FfmpegInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathFfmpeg)
        End Function

        Private Function FfprobeInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathFfprobe)
        End Function

        Public Async Function CheckFfmpegAsync() As Task(Of ToolState)
            Dim state As New ToolState With {.Name = "FFmpeg"}
            Try
                Dim ffmpegPath = FfmpegInstalledPath()
                Dim ffprobePath = FfprobeInstalledPath()
                If File.Exists(ffmpegPath) AndAlso File.Exists(ffprobePath) Then
                    state.InstalledVersion = GetSavedVersion("FFmpeg")
                    If String.IsNullOrEmpty(state.InstalledVersion) Then
                        Dim output = Await GetProcessOutputAsync(ffmpegPath, "-version")
                        Dim m = Regex.Match(output, "ffmpeg version (\S+)")
                        state.InstalledVersion = If(m.Success, m.Groups(1).Value, "unknown")
                    End If
                    state.Status = ToolStatus.Installed
                End If

                Dim release = Await GetLatestReleaseAsync("BtbN/FFmpeg-Builds")
                If release IsNot Nothing Then
                    state.LatestVersion = release.Value.TagName
                    state.DownloadUrl = FindAsset(release.Value.Assets, "ffmpeg-master-latest-win64-gpl\.zip$")

                    ' FFmpeg uses rolling "autobuild-*" tags with no meaningful semver —
                    ' if the exe exists, treat as up to date and save the tag
                    If state.Status = ToolStatus.Installed Then
                        If String.IsNullOrEmpty(GetSavedVersion("FFmpeg")) Then
                            SaveVersion("FFmpeg", state.LatestVersion)
                            state.InstalledVersion = state.LatestVersion
                        End If
                        state.Status = ToolStatus.UpToDate
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"FFmpeg check failed: {ex.Message}")
                If state.Status = ToolStatus.Missing Then state.Status = ToolStatus.CheckFailed
            End Try
            Return state
        End Function

        Public Async Function DownloadFfmpegAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim zipPath = Path.Combine(_toolsDir, "ffmpeg-temp.zip")
            Try
                Await DownloadFileAsync(url, zipPath, progress)
                ExtractFilesFromZip(zipPath, {"ffmpeg.exe", "ffprobe.exe"}, _toolsDir)
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        ' ──────────────────────────────────────────
        '  GGML Model
        ' ──────────────────────────────────────────

        Private Function ModelInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathModel)
        End Function

        Private Function ModelDownloadPath() As String
            Return Path.Combine(_toolsDir, "ggml-large-v3.bin")
        End Function

        Public Function CheckModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "Whisper Model (ggml-large-v3)",
                .DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"
            }
            If File.Exists(ModelInstalledPath()) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadModelAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Await DownloadFileAsync(url, ModelDownloadPath(), progress)
        End Function

        ' ──────────────────────────────────────────
        '  SubtitleEdit
        ' ──────────────────────────────────────────

        Private Function SubtitleEditInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathSubtitleEdit)
        End Function

        Private Function SubtitleEditDownloadDir() As String
            Return Path.Combine(_toolsDir, "SubtitleEdit")
        End Function

        Public Async Function CheckSubtitleEditAsync() As Task(Of ToolState)
            Dim state As New ToolState With {.Name = "Subtitle Edit"}
            Try
                Dim sePath = SubtitleEditInstalledPath()
                If File.Exists(sePath) Then
                    state.InstalledVersion = GetSavedVersion("Subtitle Edit")
                    state.Status = ToolStatus.Installed
                End If

                Dim release = Await GetLatestReleaseAsync("SubtitleEdit/subtitleedit")
                If release IsNot Nothing Then
                    state.LatestVersion = release.Value.TagName
                    state.DownloadUrl = FindAsset(release.Value.Assets, "^SE\d+\.zip$")

                    If state.Status = ToolStatus.Installed Then
                        If String.IsNullOrEmpty(state.InstalledVersion) Then
                            ' No saved version but exe exists — assume current, save latest
                            SaveVersion("Subtitle Edit", state.LatestVersion)
                            state.InstalledVersion = state.LatestVersion
                            state.Status = ToolStatus.UpToDate
                        Else
                            state.Status = CompareVersionTags(state.InstalledVersion, state.LatestVersion)
                        End If
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"Subtitle Edit check failed: {ex.Message}")
                If state.Status = ToolStatus.Missing Then state.Status = ToolStatus.CheckFailed
            End Try
            Return state
        End Function

        Public Async Function DownloadSubtitleEditAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim zipPath = Path.Combine(_toolsDir, "subtitleedit-temp.zip")
            Try
                Await DownloadFileAsync(url, zipPath, progress)
                Dim seDir = SubtitleEditDownloadDir()
                If Not Directory.Exists(seDir) Then Directory.CreateDirectory(seDir)
                ExtractAllFromZip(zipPath, seDir)
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        ' ──────────────────────────────────────────
        '  Python Embedded + Translation pip deps
        ' ──────────────────────────────────────────

        Private Function PythonEmbedDir() As String
            Return Path.Combine(_toolsDir, "python-embed")
        End Function

        Private Function PythonExePath() As String
            Return Path.Combine(PythonEmbedDir(), "python.exe")
        End Function

        Public Function CheckPythonEmbedAsync() As Task(Of ToolState)
            Dim state As New ToolState With {.Name = "Python Embedded"}
            If File.Exists(PythonExePath()) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadPythonEmbedAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim embedDir = PythonEmbedDir()
            Dim zipPath = Path.Combine(_toolsDir, "python-embed-temp.zip")
            Try
                Dim url = "https://www.python.org/ftp/python/3.12.9/python-3.12.9-embed-amd64.zip"
                Await DownloadFileAsync(url, zipPath, progress)

                ' Extract to python-embed/
                If Directory.Exists(embedDir) Then Directory.Delete(embedDir, True)
                ZipFile.ExtractToDirectory(zipPath, embedDir)

                ' Enable import site in python312._pth so pip works
                Dim pthFile = Path.Combine(embedDir, "python312._pth")
                If File.Exists(pthFile) Then
                    Dim lines = File.ReadAllLines(pthFile).ToList()
                    Dim found = False
                    For i = 0 To lines.Count - 1
                        If lines(i).TrimStart().StartsWith("#") AndAlso lines(i).Contains("import site") Then
                            lines(i) = "import site"
                            found = True
                            Exit For
                        End If
                    Next
                    If Not found Then lines.Add("import site")
                    File.WriteAllLines(pthFile, lines)
                End If

                ' Download and run get-pip.py
                Dim getPipPath = Path.Combine(embedDir, "get-pip.py")
                Await DownloadFileAsync("https://bootstrap.pypa.io/get-pip.py", getPipPath, Nothing)
                Await RunProcessAsync(PythonExePath(), $"""{getPipPath}""", embedDir)
                If File.Exists(getPipPath) Then File.Delete(getPipPath)
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        Public Async Function InstallPythonDepsAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim translateReq = Path.Combine(_toolsDir, "translate-server", "requirements.txt")
            Dim liveReq = Path.Combine(_toolsDir, "live-server", "requirements.txt")

            If Not File.Exists(translateReq) AndAlso Not File.Exists(liveReq) Then
                Throw New FileNotFoundException("No requirements.txt files found")
            End If

            ' Install each requirements file separately, falling back to individual packages
            Dim translateFailed = False
            If File.Exists(translateReq) Then
                Try
                    Await RunProcessAsync(PythonExePath(),
                        $"-m pip install -r ""{translateReq}"" --no-warn-script-location", _toolsDir, 600000)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"InstallPythonDepsAsync: Translation requirements install failed — {ex.Message}")
                    translateFailed = True
                End Try
            End If

            ' Fallback: install core translation packages individually (nvidia-cublas may fail on non-CUDA)
            If translateFailed Then
                For Each pkg In {"ctranslate2", "sentencepiece", "fastapi", "uvicorn"}
                    Try
                        Await RunProcessAsync(PythonExePath(),
                            $"-m pip install {pkg} --no-warn-script-location", _toolsDir, 300000)
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"InstallPythonDepsAsync: Failed to install package '{pkg}' — {ex.Message}")
                    End Try
                Next
            End If

            If File.Exists(liveReq) Then
                Try
                    Await RunProcessAsync(PythonExePath(),
                        $"-m pip install -r ""{liveReq}"" --no-warn-script-location", _toolsDir, 600000)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"InstallPythonDepsAsync: Live requirements install failed — {ex.Message}")
                End Try
            End If

            ' Standalone packages not covered by requirements files
            For Each pkg In {"edge-tts", "faster-whisper"}
                Try
                    Await RunProcessAsync(PythonExePath(),
                        $"-m pip install {pkg} --no-warn-script-location", _toolsDir, 300000)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"InstallPythonDepsAsync: {pkg} install failed — {ex.Message}")
                End Try
            Next
        End Function

        Public Function CheckPythonDepsStateAsync() As Task(Of ToolState)
            Dim state As New ToolState With {.Name = "Python Packages"}
            If CheckPythonDepsInstalled() Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Function CheckPythonDepsInstalled() As Boolean
            Return GetMissingPythonPackages().Count = 0
        End Function

        ''' <summary>
        ''' Returns a list of Python package names that failed to import.
        ''' Empty list means all required packages are installed.
        ''' </summary>
        Public Function GetMissingPythonPackages() As List(Of String)
            Dim packages As New List(Of String) From {
                "ctranslate2", "sentencepiece", "fastapi", "uvicorn",
                "silero-vad", "sounddevice", "edge-tts", "faster-whisper"}

            ' Online STT engines need their SDK only when that engine is the
            ' selected backend — these are optional and not required for the
            ' offline whisper engines. (pip distribution names, matched by pip show.)
            Dim sttBackend = If(_config.SttBackend, "")
            If sttBackend.Equals("speechmatics", StringComparison.OrdinalIgnoreCase) Then
                packages.Add("speechmatics-rt")
            ElseIf sttBackend.Equals("google-cloud-stt", StringComparison.OrdinalIgnoreCase) Then
                packages.Add("google-cloud-speech")
            End If

            If Not File.Exists(PythonExePath()) Then
                Return New List(Of String) From {"(Python not installed)"}
            End If

            ' Single pip call to check all packages at once
            Try
                Dim psi As New Diagnostics.ProcessStartInfo With {
                    .FileName = PythonExePath(),
                    .Arguments = "-m pip show " & String.Join(" ", packages),
                    .UseShellExecute = False,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .CreateNoWindow = True
                }
                Dim output As String
                Using proc = Diagnostics.Process.Start(psi)
                    ' Drain stderr async to prevent pipe buffer deadlock
                    Dim stderrTask = proc.StandardError.ReadToEndAsync()
                    output = proc.StandardOutput.ReadToEnd()
                    stderrTask.Wait()
                    proc.WaitForExit(15000)
                End Using

                ' pip show lists "Name: xxx" for each found package
                Dim found As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each line In output.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    If line.StartsWith("Name: ", StringComparison.OrdinalIgnoreCase) Then
                        found.Add(line.Substring(6).Trim())
                    End If
                Next

                Return packages.Where(Function(p) Not found.Contains(p)).ToList()
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"GetMissingPythonPackages: Failed to check packages — {ex.Message}")
                Return packages.ToList()
            End Try
        End Function

        Public Function CheckTranslationDepsAsync() As Task(Of (pythonOk As Boolean, depsOk As Boolean, modelOk As Boolean))
            Dim pythonOk = File.Exists(PythonExePath())
            Dim depsOk = If(pythonOk, CheckPythonDepsInstalled(), False)

            ' Check the model directory for the currently selected backend
            Dim modelDir = AppConfig.ResolvePath(If(_config.TranslationModelPath, ".\nllb-model"))
            Dim modelType = If(_config.TranslationModelType, "nllb")
            Dim hasModelBin = Directory.Exists(modelDir) AndAlso File.Exists(Path.Combine(modelDir, "model.bin"))
            Dim hasSp As Boolean
            hasSp = File.Exists(Path.Combine(modelDir, "sentencepiece.bpe.model"))
            Dim modelOk = hasModelBin AndAlso hasSp

            Return Task.FromResult((pythonOk, depsOk, modelOk))
        End Function

        Private Shared Async Function RunProcessAsync(exePath As String, args As String,
                                                       workDir As String,
                                                       Optional timeoutMs As Integer = 300000) As Task
            Dim psi As New Diagnostics.ProcessStartInfo With {
                .FileName = exePath,
                .Arguments = args,
                .WorkingDirectory = workDir,
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }
            Using proc = Diagnostics.Process.Start(psi)
                Dim stdoutTask = proc.StandardOutput.ReadToEndAsync()
                Dim stderrTask = proc.StandardError.ReadToEndAsync()
                Using cts As New Threading.CancellationTokenSource(timeoutMs)
                    Try
                        Await proc.WaitForExitAsync(cts.Token)
                    Catch ex As OperationCanceledException
                        Try : proc.Kill(True) : Catch killEx As Exception : AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"Failed to kill timed-out process: {killEx.Message}") : End Try
                        Throw New TimeoutException($"Process timed out after {timeoutMs / 1000}s")
                    End Try
                End Using
                Dim stderr = Await stderrTask
                If proc.ExitCode <> 0 Then
                    Dim stdout = Await stdoutTask
                    Throw New Exception($"Process exited with code {proc.ExitCode}: {If(stderr, stdout)}")
                End If
            End Using
        End Function

        ' ──────────────────────────────────────────
        '  NLLB Translation Model
        ' ──────────────────────────────────────────

        Private Function NllbModelDir() As String
            Return Path.Combine(_toolsDir, "nllb-model")
        End Function

        Public Function CheckNllbModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "NLLB Translation Model",
                .DownloadUrl = "https://huggingface.co/JustFrederik/nllb-200-1.3B-ct2-float16/resolve/main"
            }
            Dim modelDir = NllbModelDir()
            If Directory.Exists(modelDir) AndAlso
               File.Exists(Path.Combine(modelDir, "model.bin")) AndAlso
               File.Exists(Path.Combine(modelDir, "sentencepiece.bpe.model")) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadNllbModelAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim modelDir = NllbModelDir()
            If Not Directory.Exists(modelDir) Then Directory.CreateDirectory(modelDir)

            Dim baseUrl = "https://huggingface.co/JustFrederik/nllb-200-1.3B-ct2-float16/resolve/main"
            Dim files = {"model.bin", "sentencepiece.bpe.model", "shared_vocabulary.txt", "config.json", "tokenizer_config.json"}

            For Each f In files
                Dim destPath = Path.Combine(modelDir, f)
                If Not File.Exists(destPath) Then
                    Dim url = $"{baseUrl}/{f}"
                    Await DownloadFileAsync(url, destPath, progress)
                End If
            Next
        End Function

        ' ──────────────────────────────────────────
        '  NLLB 3.3B Translation Model
        ' ──────────────────────────────────────────

        Private Function Nllb33bModelDir() As String
            Return Path.Combine(_toolsDir, "nllb-3.3b-model")
        End Function

        Public Function CheckNllb33bModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "NLLB 3.3B Translation Model",
                .DownloadUrl = "https://huggingface.co/entai2965/nllb-200-3.3B-ctranslate2-float16/resolve/main"
            }
            Dim modelDir = Nllb33bModelDir()
            If Directory.Exists(modelDir) AndAlso
               File.Exists(Path.Combine(modelDir, "model.bin")) AndAlso
               File.Exists(Path.Combine(modelDir, "sentencepiece.bpe.model")) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadNllb33bModelAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim modelDir = Nllb33bModelDir()
            If Not Directory.Exists(modelDir) Then Directory.CreateDirectory(modelDir)

            Dim baseUrl = "https://huggingface.co/entai2965/nllb-200-3.3B-ctranslate2-float16/resolve/main"
            Dim files = {"model.bin", "sentencepiece.bpe.model", "shared_vocabulary.json", "config.json", "tokenizer_config.json"}

            For Each f In files
                Dim destPath = Path.Combine(modelDir, f)
                If Not File.Exists(destPath) Then
                    Dim url = $"{baseUrl}/{f}"
                    Await DownloadFileAsync(url, destPath, progress)
                End If
            Next
        End Function

        ' ──────────────────────────────────────────
        '  Piper TTS (offline speech synthesis)
        ' ──────────────────────────────────────────

        Private Shared ReadOnly PiperReleaseUrl As String =
            "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip"

        Private Function PiperDir() As String
            Return Path.Combine(_toolsDir, "tts-models", "piper")
        End Function

        Private Function PiperVoicesDir() As String
            Return Path.Combine(PiperDir(), "voices")
        End Function

        Private Function PiperExePath() As String
            Return Path.Combine(PiperDir(), "piper.exe")
        End Function

        Public Function CheckPiperAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "Piper TTS",
                .DownloadUrl = PiperReleaseUrl
            }
            If File.Exists(PiperExePath()) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "2023.11.14-2"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadPiperAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim ttsModelsDir = Path.Combine(_toolsDir, "tts-models")
            If Not Directory.Exists(ttsModelsDir) Then Directory.CreateDirectory(ttsModelsDir)

            Dim destDir As String = PiperDir()
            Dim zipPath = Path.Combine(_toolsDir, "piper-temp.zip")
            Try
                Await DownloadFileAsync(PiperReleaseUrl, zipPath, progress)

                ' Extract to tts-models/ — the zip may or may not contain a top-level directory
                If Not Directory.Exists(destDir) Then Directory.CreateDirectory(destDir)
                ZipFile.ExtractToDirectory(zipPath, destDir, True)

                ' If the zip had a subdirectory containing piper.exe, move contents up
                If Not File.Exists(Path.Combine(destDir, "piper.exe")) Then
                    For Each subDir As String In Directory.GetDirectories(destDir)
                        If File.Exists(Path.Combine(subDir, "piper.exe")) Then
                            For Each f As String In Directory.GetFiles(subDir)
                                File.Move(f, Path.Combine(destDir, Path.GetFileName(f)), True)
                            Next
                            For Each d As String In Directory.GetDirectories(subDir)
                                Dim destSubDir = Path.Combine(destDir, Path.GetFileName(d))
                                If Directory.Exists(destSubDir) Then Directory.Delete(destSubDir, True)
                                Directory.Move(d, destSubDir)
                            Next
                            Directory.Delete(subDir, True)
                            Exit For
                        End If
                    Next
                End If

                ' Create voices directory
                Dim vDir As String = PiperVoicesDir()
                If Not Directory.Exists(vDir) Then Directory.CreateDirectory(vDir)

                SaveVersion("Piper TTS", "2023.11.14-2")
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        ''' <summary>
        ''' Downloads a Piper voice model for the given FLORES language prefix (e.g. "fra", "deu").
        ''' Downloads both the .onnx model and .onnx.json config from HuggingFace.
        ''' </summary>
        Public Async Function DownloadPiperVoiceAsync(floresLanguage As String,
                                                       progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim voicesDir = PiperVoicesDir()
            If Not Directory.Exists(voicesDir) Then Directory.CreateDirectory(voicesDir)

            Dim modelFile = PiperBackend.GetModelFileName(floresLanguage)
            If modelFile Is Nothing Then
                Throw New ArgumentException($"No Piper voice available for language: {floresLanguage}")
            End If

            Dim baseUrl = PiperBackend.GetModelDownloadUrl(floresLanguage)
            If baseUrl Is Nothing Then Return

            ' Download .onnx model file
            Dim onnxPath = Path.Combine(voicesDir, modelFile)
            If Not File.Exists(onnxPath) Then
                Await DownloadFileAsync($"{baseUrl}.onnx", onnxPath, progress)
            End If

            ' Download .onnx.json config file (required by piper)
            Dim jsonPath = onnxPath & ".json"
            If Not File.Exists(jsonPath) Then
                Await DownloadFileAsync($"{baseUrl}.onnx.json", jsonPath, Nothing)
            End If
        End Function

        ''' <summary>
        ''' Returns a list of FLORES language prefixes that have Piper voice models installed.
        ''' </summary>
        Public Function GetInstalledPiperVoices() As List(Of String)
            Dim installed As New List(Of String)()
            Dim voicesDir = PiperVoicesDir()
            If Not Directory.Exists(voicesDir) Then Return installed

            For Each kvp In PiperBackend.VoiceMap
                Dim modelFile = PiperBackend.GetModelFileName(kvp.Key)
                If modelFile IsNot Nothing AndAlso File.Exists(Path.Combine(voicesDir, modelFile)) Then
                    installed.Add(kvp.Key)
                End If
            Next
            Return installed
        End Function

        ''' <summary>
        ''' Returns all FLORES language prefixes that have Piper voices available for download.
        ''' </summary>
        Public Shared Function GetAvailablePiperVoices() As List(Of String)
            Return PiperBackend.VoiceMap.Keys.ToList()
        End Function

        ' ──────────────────────────────────────────
        '  MMS-TTS (Meta offline TTS — optional)
        ' ──────────────────────────────────────────

        Public Function CheckMmsTtsAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "MMS-TTS (optional)"
            }
            If MmsTtsBackend.CheckDepsInstalled() Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        ''' <summary>
        ''' Installs PyTorch (CPU-only, ~200 MB) and transformers for MMS-TTS.
        ''' Requires Python embedded to be installed first.
        ''' </summary>
        Public Async Function InstallMmsTtsDepsAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            If Not File.Exists(PythonExePath()) Then
                Throw New InvalidOperationException("Python embedded must be installed first")
            End If

            ' Install CPU-only PyTorch (~200 MB instead of ~2.5 GB with CUDA)
            Await RunProcessAsync(PythonExePath(),
                "-m pip install torch --index-url https://download.pytorch.org/whl/cpu --no-warn-script-location",
                _toolsDir, 600000)

            ' Install remaining deps (transformers, numpy, etc.)
            Dim mmsTtsReq = Path.Combine(_toolsDir, "mms-tts-server", "requirements.txt")
            If File.Exists(mmsTtsReq) Then
                Await RunProcessAsync(PythonExePath(),
                    $"-m pip install -r ""{mmsTtsReq}"" --no-warn-script-location",
                    _toolsDir, 600000)
            End If
        End Function

        ' ──────────────────────────────────────────
        '  whisper-server (Vulkan build for whisper.cpp)
        ' ──────────────────────────────────────────

        Private Function WhisperServerInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathWhisperServer)
        End Function

        Public Async Function CheckWhisperServerAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "whisper-server (Vulkan)"
            }
            Try
                If File.Exists(WhisperServerInstalledPath()) Then
                    state.InstalledVersion = GetSavedVersion("whisper-server (Vulkan)")
                    If String.IsNullOrEmpty(state.InstalledVersion) Then state.InstalledVersion = "installed"
                    state.Status = ToolStatus.Installed
                End If

                ' Vulkan build hosted on EveryTongue releases (built from whisper.cpp source with -DGGML_VULKAN=ON)
                ' whisper-server is versioned independently — search across recent releases
                Dim found = Await FindAssetAcrossReleasesAsync("LeapFreak/EveryTongue", "whisper-server-vulkan.*x64\.zip$")
                If found IsNot Nothing Then
                    state.LatestVersion = found.Value.TagName
                    state.DownloadUrl = found.Value.Url

                    If state.Status = ToolStatus.Installed Then
                        state.Status = CompareVersionTags(state.InstalledVersion, state.LatestVersion)
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"whisper-server check failed: {ex.Message}")
                If state.Status = ToolStatus.Missing Then state.Status = ToolStatus.CheckFailed
            End Try
            Return state
        End Function

        Public Async Function DownloadWhisperServerAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            ' Download the Vulkan build ZIP from GitHub releases
            Dim zipPath = Path.Combine(_toolsDir, "whisper-server-vulkan.zip")
            Try
                Await DownloadFileAsync(url, zipPath, progress)
                ' Extract whisper-server.exe and required DLLs
                ExtractAllFromZip(zipPath, _toolsDir)
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        ' ──────────────────────────────────────────
        '  whisper-server-cuda (CUDA build for whisper.cpp — NVIDIA only)
        ' ──────────────────────────────────────────

        Private Function WhisperServerCudaInstalledPath() As String
            Dim vulkanDir = Path.GetDirectoryName(WhisperServerInstalledPath())
            If vulkanDir Is Nothing Then Return ""
            Return Path.Combine(vulkanDir, "whisper-server-cuda.exe")
        End Function

        Public Async Function CheckWhisperServerCudaAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "whisper-server (CUDA)"
            }
            Try
                If File.Exists(WhisperServerCudaInstalledPath()) Then
                    state.InstalledVersion = GetSavedVersion("whisper-server (CUDA)")
                    If String.IsNullOrEmpty(state.InstalledVersion) Then state.InstalledVersion = "installed"
                    state.Status = ToolStatus.Installed
                End If

                ' CUDA build hosted on EveryTongue releases — versioned independently
                Dim found = Await FindAssetAcrossReleasesAsync("LeapFreak/EveryTongue", "whisper-server-cuda.*x64\.zip$")
                If found IsNot Nothing Then
                    state.LatestVersion = found.Value.TagName
                    state.DownloadUrl = found.Value.Url

                    If state.Status = ToolStatus.Installed Then
                        state.Status = CompareVersionTags(state.InstalledVersion, state.LatestVersion)
                    End If
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"whisper-server-cuda check failed: {ex.Message}")
                If state.Status = ToolStatus.Missing Then state.Status = ToolStatus.CheckFailed
            End Try
            Return state
        End Function

        Public Async Function DownloadWhisperServerCudaAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim zipPath = Path.Combine(_toolsDir, "whisper-server-cuda.zip")
            Try
                Await DownloadFileAsync(url, zipPath, progress)
                ExtractAllFromZip(zipPath, _toolsDir)
            Finally
                If File.Exists(zipPath) Then File.Delete(zipPath)
            End Try
        End Function

        ' ──────────────────────────────────────────
        '  GGML Whisper Model (for whisper.cpp)
        ' ──────────────────────────────────────────

        Private Function GgmlModelInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathWhisperCppModel)
        End Function

        Public Function CheckGgmlModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "GGML Whisper Model",
                .DownloadUrl = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3-turbo.bin"
            }
            If File.Exists(GgmlModelInstalledPath()) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadGgmlModelAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim destPath = Path.Combine(_toolsDir, "ggml-large-v3-turbo.bin")
            Await DownloadFileAsync(url, destPath, progress)
        End Function

        ' ──────────────────────────────────────────
        '  faster-whisper CTranslate2 Model
        ' ──────────────────────────────────────────

        Private Function FasterWhisperModelInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathFasterWhisperModel)
        End Function

        Public Function CheckFasterWhisperModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "faster-whisper Model",
                .DownloadUrl = ""
            }
            Dim modelDir = FasterWhisperModelInstalledPath()
            ' Check for model.bin + vocabulary file (vocabulary.json or vocabulary.txt)
            If IO.Directory.Exists(modelDir) AndAlso
               File.Exists(Path.Combine(modelDir, "model.bin")) AndAlso
               (File.Exists(Path.Combine(modelDir, "vocabulary.json")) OrElse
                File.Exists(Path.Combine(modelDir, "vocabulary.txt"))) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        ''' <summary>
        ''' Downloads the faster-whisper model using faster-whisper's own download_model(),
        ''' which delegates to huggingface_hub.snapshot_download with integrity checks.
        ''' </summary>
        Public Async Function DownloadFasterWhisperModelAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim modelDir = FasterWhisperModelInstalledPath()
            Dim pythonPath = PythonExePath()
            If Not File.Exists(pythonPath) Then
                Throw New FileNotFoundException("Python not installed — install Python packages first")
            End If

            ' Use faster-whisper's built-in downloader (handles partial downloads, integrity, correct file list)
            Dim script = $"from faster_whisper.utils import download_model; download_model('large-v3', output_dir=r'{modelDir}')"
            Await RunProcessAsync(pythonPath, $"-c ""{script}""", _toolsDir, 1200000)
        End Function

        ' ──────────────────────────────────────────
        '  Silero VAD Model (for whisper-server built-in VAD)
        ' ──────────────────────────────────────────

        Private Function SileroVadModelInstalledPath() As String
            Return AppConfig.ResolvePath(_config.PathSileroVadModel)
        End Function

        Public Function CheckSileroVadModelAsync() As Task(Of ToolState)
            Dim state As New ToolState With {
                .Name = "Silero VAD Model",
                .DownloadUrl = "https://huggingface.co/ggml-org/whisper-vad/resolve/main/ggml-silero-v6.2.0.bin"
            }
            If File.Exists(SileroVadModelInstalledPath()) Then
                state.Status = ToolStatus.UpToDate
                state.InstalledVersion = "installed"
            End If
            Return Task.FromResult(state)
        End Function

        Public Async Function DownloadSileroVadModelAsync(url As String, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Dim destPath = Path.Combine(_toolsDir, "ggml-silero-v6.2.0.bin")
            Await DownloadFileAsync(url, destPath, progress)
        End Function

        ' ──────────────────────────────────────────
        '  Download a tool by name
        ' ──────────────────────────────────────────

        ''' <summary>
        ''' Ensures Python embedded + all pip packages are installed.
        ''' Called automatically after downloading tools — not shown as a separate item to the user.
        ''' </summary>
        Public Async Function EnsurePythonReadyAsync(progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            If Not File.Exists(PythonExePath()) Then
                Await DownloadPythonEmbedAsync(progress)
            End If
            If Not CheckPythonDepsInstalled() Then
                Await Task.Run(Function() InstallPythonDepsAsync(Nothing))
            End If
        End Function

        Public Async Function DownloadToolAsync(state As ToolState, progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            AppLogger.Log(LogEvents.DL_DOWNLOAD_START, $"Starting download: {state.Name} (Status={state.Status}, URL={state.DownloadUrl})")
            Try
                Select Case state.Name
                    Case "yt-dlp"
                        Await DownloadYtDlpAsync(state.DownloadUrl, progress)
                    Case "FFmpeg"
                        Await DownloadFfmpegAsync(state.DownloadUrl, progress)
                    Case "Whisper Model (ggml-large-v3)"
                        Await DownloadModelAsync(state.DownloadUrl, progress)
                    Case "Subtitle Edit"
                        Await DownloadSubtitleEditAsync(state.DownloadUrl, progress)
                    Case "NLLB Translation Model"
                        Await DownloadNllbModelAsync(progress)
                    Case "NLLB 3.3B Translation Model"
                        Await DownloadNllb33bModelAsync(progress)
                    Case "Piper TTS"
                        Await DownloadPiperAsync(progress)
                    Case "whisper-server (Vulkan)"
                        Await DownloadWhisperServerAsync(state.DownloadUrl, progress)
                    Case "whisper-server (CUDA)"
                        Await DownloadWhisperServerCudaAsync(state.DownloadUrl, progress)
                    Case "GGML Whisper Model"
                        Await DownloadGgmlModelAsync(state.DownloadUrl, progress)
                    Case "faster-whisper Model"
                        Await DownloadFasterWhisperModelAsync(progress)
                    Case "Silero VAD Model"
                        Await DownloadSileroVadModelAsync(state.DownloadUrl, progress)
                    Case Else
                        AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"Unknown tool name: '{state.Name}' — no download handler")
                End Select

                AppLogger.Log(LogEvents.DL_DOWNLOAD_DONE, $"Completed: {state.Name}")

                ' Save the downloaded version
                If Not String.IsNullOrEmpty(state.LatestVersion) Then
                    SaveVersion(state.Name, state.LatestVersion)
                End If
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"FAILED: {state.Name} — {ex.GetType().Name}: {ex.Message}")
                Throw
            End Try
        End Function

        ' ──────────────────────────────────────────
        '  Version comparison
        ' ──────────────────────────────────────────

        Private Shared Function CompareVersionTags(installed As String, latest As String) As ToolStatus
            If String.IsNullOrEmpty(installed) OrElse installed = "unknown" Then
                Return ToolStatus.UpdateAvailable
            End If
            If String.IsNullOrEmpty(latest) Then
                Return ToolStatus.UpToDate
            End If
            ' Normalize: strip leading v, trim whitespace
            Dim a = installed.Trim().TrimStart("v"c, "V"c)
            Dim b = latest.Trim().TrimStart("v"c, "V"c)
            ' Exact match
            If a = b Then Return ToolStatus.UpToDate
            ' Try semver comparison
            Dim vA As Version = Nothing
            Dim vB As Version = Nothing
            If Version.TryParse(a, vA) AndAlso Version.TryParse(b, vB) Then
                Return If(vB > vA, ToolStatus.UpdateAvailable, ToolStatus.UpToDate)
            End If
            ' If only one side parses, or neither parses, and they don't match,
            ' default to UpToDate to avoid false update prompts
            Return ToolStatus.UpToDate
        End Function

        ' ──────────────────────────────────────────
        '  GitHub API helpers
        ' ──────────────────────────────────────────

        Private Structure ReleaseInfo
            Public TagName As String
            Public Assets As List(Of (Name As String, Url As String))
        End Structure

        Private Shared Async Function GetLatestReleaseAsync(repo As String) As Task(Of ReleaseInfo?)
            Dim url = $"https://api.github.com/repos/{repo}/releases/latest"
            Dim response = Await _client.GetAsync(url)

            ' Follow redirect if repo was moved
            If response.StatusCode = Net.HttpStatusCode.MovedPermanently Then
                Dim redirectUrl = response.Headers.Location?.ToString()
                If Not String.IsNullOrEmpty(redirectUrl) Then
                    response = Await _client.GetAsync(redirectUrl)
                End If
            End If

            If Not response.IsSuccessStatusCode Then Return Nothing

            Dim json = Await response.Content.ReadAsStringAsync()
            Using doc = JsonDocument.Parse(json)
                Dim root = doc.RootElement
                Dim tagName = root.GetProperty("tag_name").GetString()
                Dim assets As New List(Of (Name As String, Url As String))

                For Each asset In root.GetProperty("assets").EnumerateArray()
                    Dim assetName = asset.GetProperty("name").GetString()
                    Dim assetUrl = asset.GetProperty("browser_download_url").GetString()
                    assets.Add((assetName, assetUrl))
                Next

                Return New ReleaseInfo With {
                    .TagName = tagName,
                    .Assets = assets
                }
            End Using
        End Function

        Private Shared Function FindAsset(assets As List(Of (Name As String, Url As String)), pattern As String) As String
            Dim match = assets.FirstOrDefault(Function(a) Regex.IsMatch(a.Name, pattern, RegexOptions.IgnoreCase))
            Return match.Url
        End Function

        ''' <summary>
        ''' Searches recent releases for an asset matching the pattern.
        ''' Used for assets like whisper-server that are versioned independently
        ''' and may not be attached to every release.
        ''' </summary>
        Private Shared Async Function FindAssetAcrossReleasesAsync(repo As String, pattern As String) As Task(Of (Url As String, TagName As String)?)
            Dim url = $"https://api.github.com/repos/{repo}/releases?per_page=10"
            Dim response = Await _client.GetAsync(url)
            If Not response.IsSuccessStatusCode Then Return Nothing

            Dim json = Await response.Content.ReadAsStringAsync()
            Using doc = JsonDocument.Parse(json)
                For Each release In doc.RootElement.EnumerateArray()
                    Dim tagName = release.GetProperty("tag_name").GetString()
                    For Each asset In release.GetProperty("assets").EnumerateArray()
                        Dim assetName = asset.GetProperty("name").GetString()
                        If Regex.IsMatch(assetName, pattern, RegexOptions.IgnoreCase) Then
                            Return (asset.GetProperty("browser_download_url").GetString(), tagName)
                        End If
                    Next
                Next
            End Using
            Return Nothing
        End Function

        ' ──────────────────────────────────────────
        '  Download / Extract helpers
        ' ──────────────────────────────────────────

        Private Shared Async Function DownloadFileAsync(url As String, destPath As String,
                                                        progress As IProgress(Of (downloaded As Long, total As Long))) As Task
            Using response = Await _downloadClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                response.EnsureSuccessStatusCode()
                Dim totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault(-1)

                Using contentStream = Await response.Content.ReadAsStreamAsync(),
                      fileStream As New FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, True)

                    Dim buffer(81919) As Byte
                    Dim totalRead As Long = 0
                    Dim bytesRead As Integer

                    Do
                        bytesRead = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                        If bytesRead > 0 Then
                            Await fileStream.WriteAsync(buffer, 0, bytesRead)
                            totalRead += bytesRead
                            progress?.Report((totalRead, totalBytes))
                        End If
                    Loop While bytesRead > 0
                End Using
            End Using
        End Function

        Private Shared Sub ExtractFilesFromZip(zipPath As String, fileNames As String(), destDir As String)
            Using archive = ZipFile.OpenRead(zipPath)
                For Each entry In archive.Entries
                    Dim name = Path.GetFileName(entry.FullName)
                    If fileNames.Any(Function(f) f.Equals(name, StringComparison.OrdinalIgnoreCase)) Then
                        Dim destPath = Path.Combine(destDir, name)
                        entry.ExtractToFile(destPath, overwrite:=True)
                    End If
                Next
            End Using
        End Sub

        Private Shared Sub ExtractAllFromZip(zipPath As String, destDir As String)
            Using archive = ZipFile.OpenRead(zipPath)
                For Each entry In archive.Entries
                    ' Skip directories
                    If String.IsNullOrEmpty(entry.Name) Then Continue For
                    Dim destPath = Path.Combine(destDir, entry.Name)
                    entry.ExtractToFile(destPath, overwrite:=True)
                Next
            End Using
        End Sub

        Private Shared Async Function GetProcessOutputAsync(exePath As String, args As String,
                                                            Optional timeoutMs As Integer = 10000) As Task(Of String)
            Dim psi As New Diagnostics.ProcessStartInfo With {
                .FileName = exePath,
                .Arguments = args,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }
            Using proc = Diagnostics.Process.Start(psi)
                Using cts As New Threading.CancellationTokenSource(timeoutMs)
                    Try
                        Dim stdoutTask = proc.StandardOutput.ReadToEndAsync()
                        Dim stderrTask = proc.StandardError.ReadToEndAsync()
                        Await proc.WaitForExitAsync(cts.Token)
                        Dim stdout = Await stdoutTask
                        Dim stderr = Await stderrTask
                        ' Return stdout if it has content, otherwise stderr
                        Dim result = If(String.IsNullOrWhiteSpace(stdout), stderr, stdout)
                        Return result.Trim()
                    Catch ex As OperationCanceledException
                        Try : proc.Kill() : Catch killEx As Exception : AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"Failed to kill timed-out process: {killEx.Message}") : End Try
                        Return ""
                    End Try
                End Using
            End Using
        End Function

    End Class
End Namespace
