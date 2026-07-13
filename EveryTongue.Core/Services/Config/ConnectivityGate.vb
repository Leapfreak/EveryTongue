Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>
    ''' The Online/Offline gate: filters which engines (and therefore which
    ''' engine templates) are eligible for a session, purely on the registries'
    ''' RequiresInternet flag. An explicit switch — there is NO auto-fallback
    ''' from an ineligible engine to an eligible one.
    ''' </summary>
    Friend Class ConnectivityGate

        Public Shared Function EligibleSttEngines(mode As ConnectivityMode) As List(Of Stt.SttBackendRegistry.Entry)
            Return Stt.SttBackendRegistry.GetAll().
                Where(Function(e) mode = ConnectivityMode.Online OrElse Not e.RequiresInternet).ToList()
        End Function

        Public Shared Function EligibleTranslationEngines(mode As ConnectivityMode) As List(Of Translation.TranslationBackendRegistry.Entry)
            Return Translation.TranslationBackendRegistry.GetAll().
                Where(Function(e) mode = ConnectivityMode.Online OrElse Not e.RequiresInternet).ToList()
        End Function

        Public Shared Function EligibleTtsEngines(mode As ConnectivityMode) As List(Of Tts.TtsBackendRegistry.Entry)
            Return Tts.TtsBackendRegistry.GetAll().
                Where(Function(e) mode = ConnectivityMode.Online OrElse Not e.RequiresInternet).ToList()
        End Function

        ''' <summary>Whether an engine key passes the gate for the given group ("stt"/"translate"/"tts"). Unknown keys fail closed when offline.</summary>
        Public Shared Function IsEngineEligible(group As String, engineKey As String, mode As ConnectivityMode) As Boolean
            If mode = ConnectivityMode.Online Then Return True
            Select Case If(group, "").ToLowerInvariant()
                Case TemplateLibraryStore.GroupStt
                    Dim e = Stt.SttBackendRegistry.Find(engineKey)
                    Return e IsNot Nothing AndAlso Not e.RequiresInternet
                Case TemplateLibraryStore.GroupTranslate
                    Dim e = Translation.TranslationBackendRegistry.GetAll().
                        FirstOrDefault(Function(x) x.Key.Equals(If(engineKey, ""), StringComparison.OrdinalIgnoreCase))
                    Return e IsNot Nothing AndAlso Not e.RequiresInternet
                Case TemplateLibraryStore.GroupTts
                    Dim e = Tts.TtsBackendRegistry.GetAll().
                        FirstOrDefault(Function(x) x.Key.Equals(If(engineKey, ""), StringComparison.OrdinalIgnoreCase))
                    Return e IsNot Nothing AndAlso Not e.RequiresInternet
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Pick a speaker's STT template reference for the session mode.
        ''' Online → online slot, offline → offline slot; an empty slot returns ""
        ''' (no cross-mode fallback — the gate is explicit).
        ''' </summary>
        Public Shared Function SelectSpeakerSttTemplateId(speaker As SpeakerProfile, mode As ConnectivityMode) As String
            If speaker Is Nothing Then Return ""
            Return If(mode = ConnectivityMode.Online, If(speaker.OnlineSttTemplateId, ""), If(speaker.OfflineSttTemplateId, ""))
        End Function

        ''' <summary>Gate-check an engine template for a slot; logs and returns Nothing when ineligible.</summary>
        Public Shared Function GateTemplate(group As String, template As EngineTemplate, mode As ConnectivityMode,
                                            contextLabel As String) As EngineTemplate
            If template Is Nothing Then Return Nothing
            If IsEngineEligible(group, template.EngineKey, mode) Then
                Return template
            End If
            AppLogger.Log(LogEvents.CONFIG_GATE_DECISION,
                $"{contextLabel} {group} template '{template.Name}' ({template.Id}) uses online engine '{template.EngineKey}' — blocked by Offline mode (no auto-fallback)")
            Return Nothing
        End Function

    End Class

End Namespace
