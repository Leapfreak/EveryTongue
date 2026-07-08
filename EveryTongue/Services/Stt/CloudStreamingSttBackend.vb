Imports System.Threading
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Stt

    ''' <summary>
    ''' Generic ISttBackend for online streaming engines (Google Cloud STT,
    ''' Speechmatics, …). A thin adapter that delegates to LiveStreamRunner with
    ''' the engine's backend key. Engine-specific behaviour lives entirely in the
    ''' Python live-server engine module — adding a new cloud engine needs only a
    ''' registry entry, not a new class here.
    ''' </summary>
    Friend Class CloudStreamingSttBackend
        Implements ISttBackend, IRetargetableSttBackend

        Private ReadOnly _runner As New LiveStreamRunner()
        Private ReadOnly _backendKey As String
        Private ReadOnly _displayName As String
        ''' <summary>The engine config block hosted for the active session (set in Start).</summary>
        Private _engineBlock As Configs.ICloudSttEngineConfig

        Public Sub New(backendKey As String, displayName As String)
            _backendKey = backendKey
            _displayName = displayName

            AddHandler _runner.OutputLineUpdated, Sub(s, line)
                                                      RaiseEvent OutputUpdated(Me, New SttOutputEventArgs(line))
                                                  End Sub

            AddHandler _runner.OutputLineCommitted, Sub(s, line)
                                                        Dim text = line
                                                        Dim lang = ""
                                                        Dim tabIdx = line.IndexOf(vbTab)
                                                        If tabIdx > 0 Then
                                                            lang = line.Substring(0, tabIdx)
                                                            text = line.Substring(tabIdx + 1)
                                                        End If
                                                        RaiseEvent OutputCommitted(Me, New SttOutputEventArgs(text, lang))
                                                    End Sub

            AddHandler _runner.OutputLineCommittedTranslated, Sub(s, tc)
                                                                  RaiseEvent OutputCommittedTranslated(
                                                                      Me, New SttTranslatedCommitEventArgs(tc.Text, tc.Lang, tc.Translations))
                                                              End Sub

            AddHandler _runner.ErrorReceived, Sub(s, line)
                                                  RaiseEvent ErrorReceived(Me, line)
                                              End Sub
        End Sub

        Public ReadOnly Property Name As String Implements ISttBackend.Name
            Get
                Return _displayName
            End Get
        End Property

        Public ReadOnly Property RequiresInternet As Boolean Implements ISttBackend.RequiresInternet
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsAvailable As Boolean Implements ISttBackend.IsAvailable
            Get
                Return True
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean Implements ISttBackend.IsRunning
            Get
                Return _runner.IsRunning
            End Get
        End Property

        Public ReadOnly Property Transcript As String Implements ISttBackend.Transcript
            Get
                Return _runner.Transcript
            End Get
        End Property

        Public Event OutputUpdated As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputUpdated
        Public Event OutputCommitted As EventHandler(Of SttOutputEventArgs) Implements ISttBackend.OutputCommitted
        Public Event OutputCommittedTranslated As EventHandler(Of SttTranslatedCommitEventArgs) Implements ISttBackend.OutputCommittedTranslated
        Public Event ErrorReceived As EventHandler(Of String) Implements ISttBackend.ErrorReceived

        Public Sub Start(config As SttSessionConfig) Implements ISttBackend.Start
            _runner.Backend = _backendKey
            _runner.SttApiKey = config.ApiKey
            _runner.FiltersHallucinationsPath = config.HallucinationsPath

            Dim appConfig As New AppConfig() With {
                .LiveServerPort = config.ServerPort,
                .NoGpu = False
            }

            ' The engine's own config block pushes its settings onto the runner
            ' and contributes its /start JSON fields, so this shared backend
            ' never knows any engine's fields.
            _engineBlock = TryCast(config.EngineConfig, Configs.ICloudSttEngineConfig)
            _engineBlock?.ConfigureRunner(_runner, appConfig)
            _runner.CloudEngineStartExtras = If(_engineBlock?.BuildStartJsonExtras(), "")

            _runner.Start(appConfig, config.DeviceIndex, config.Language, config.TranslateToEnglish)
        End Sub

        Public Sub [Stop]() Implements ISttBackend.Stop
            _runner.Stop()
        End Sub

        Public Function UpdateConfigAsync(params As Dictionary(Of String, Object)) As Task Implements ISttBackend.UpdateConfigAsync
            Return _runner.UpdateConfigAsync(params)
        End Function

        ''' <summary>Split a held clause into sentences via live-server's SaT segmenter (blocking; used by the clause accumulator flush).</summary>
        Public Function Segment(text As String, thresholdPercent As Integer, model As String) As List(Of String)
            Return _runner.Segment(text, thresholdPercent, model)
        End Function

        ''' <summary>The engine key this backend was created for (e.g. "speechmatics").</summary>
        Public ReadOnly Property BackendKey As String
            Get
                Return _backendKey
            End Get
        End Property

        ' ── IRetargetableSttBackend — inline-translation retargeting ──
        ' This backend hosts the engine config block, so the knowledge of WHICH
        ' block type supports inline translation lives here, not in controllers.

        ''' <summary>True when the hosted engine block supports inline translation (Speechmatics).</summary>
        Public ReadOnly Property SupportsInlineTranslation As Boolean Implements IRetargetableSttBackend.SupportsInlineTranslation
            Get
                Return TryCast(_engineBlock, Configs.SpeechmaticsConfig) IsNot Nothing
            End Get
        End Property

        ''' <summary>Current inline translation targets (engine codes) for this backend.</summary>
        Public ReadOnly Property TranslationTargets As List(Of String) Implements IRetargetableSttBackend.CurrentTranslationTargets
            Get
                Dim sm = TryCast(_engineBlock, Configs.SpeechmaticsConfig)
                Return If(sm?.TranslationTargets, New List(Of String))
            End Get
        End Property

        ''' <summary>
        ''' Push a new set of inline translation targets to the running engine
        ''' (the engine restarts its session to apply them — brief audio gap).
        ''' Updates the hosted block + the runner's /start extras so any later
        ''' capture restart carries the new targets too.
        ''' </summary>
        Public Async Function UpdateTranslationTargetsAsync(targets As List(Of String)) As Task Implements IRetargetableSttBackend.UpdateTranslationTargetsAsync
            Dim sm = TryCast(_engineBlock, Configs.SpeechmaticsConfig)
            If sm Is Nothing Then Return
            sm.TranslationTargets = If(targets, New List(Of String))
            _runner.CloudEngineStartExtras = sm.BuildStartJsonExtras()
            Await _runner.UpdateConfigAsync(New Dictionary(Of String, Object) From {
                {"translation_targets", sm.TranslationTargets}
            })
        End Function

        Public Function EnumerateDevicesAsync(pythonExePath As String) As List(Of AudioDeviceInfo) Implements ISttBackend.EnumerateDevicesAsync
            Dim rawDevices = _runner.EnumerateDevicesAsync(pythonExePath)
            Dim result As New List(Of AudioDeviceInfo)
            For Each raw In rawDevices
                Dim colonIdx = raw.IndexOf(":"c)
                If colonIdx > 0 Then
                    Dim idStr = raw.Substring(0, colonIdx).Trim()
                    Dim name = raw.Substring(colonIdx + 1).Trim()
                    Dim id As Integer
                    If Integer.TryParse(idStr, id) Then
                        result.Add(New AudioDeviceInfo(id, name))
                    Else
                        result.Add(New AudioDeviceInfo(0, raw))
                    End If
                Else
                    result.Add(New AudioDeviceInfo(0, raw))
                End If
            Next
            Return result
        End Function

        Public Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean) Implements ISttBackend.CheckHealthAsync
            Await Task.CompletedTask
            Return _runner.IsServerReady
        End Function

        Public Function GetStatsAsync() As Task(Of String) Implements ISttBackend.GetStatsAsync
            Return _runner.GetStatsAsync()
        End Function

        Public Function SaveTranscript(filePath As String) As Boolean Implements ISttBackend.SaveTranscript
            Return _runner.SaveTranscript(filePath)
        End Function

    End Class

End Namespace
