Imports System.IO
Imports System.Text.Json
Imports EveryTongue.Models.Templates
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>
    ''' Persistence for the preset libraries: one JSON file per config group
    ''' under %AppData%\EveryTongue\templates\ (separate from the flat
    ''' config.json), plus the session-template list. Thread-safe singleton.
    ''' </summary>
    Public NotInheritable Class TemplateLibraryStore

        Public Const GroupStt As String = "stt"
        Public Const GroupTranslate As String = "translate"
        Public Const GroupTts As String = "tts"

        Private Shared ReadOnly _instance As New TemplateLibraryStore()
        Public Shared ReadOnly Property Instance As TemplateLibraryStore
            Get
                Return _instance
            End Get
        End Property

        Private Shared ReadOnly _jsonOptions As JsonSerializerOptions

        Shared Sub New()
            _jsonOptions = New JsonSerializerOptions With {
                .WriteIndented = True,
                .PropertyNameCaseInsensitive = True
            }
            _jsonOptions.Converters.Add(New Serialization.JsonStringEnumConverter())
        End Sub

        Private ReadOnly _lock As New Object()
        Private ReadOnly _engineTemplates As New Dictionary(Of String, List(Of EngineTemplate))
        Private _sessionTemplates As New List(Of SessionTemplate)
        Private _speakerProfiles As New List(Of SpeakerProfile)
        Private _displayTemplates As New List(Of DisplayTemplate)
        Private _filterSets As New List(Of FilterSet)
        Private _loaded As Boolean = False

        Private Sub New()
        End Sub

        Private Shared ReadOnly Property TemplatesDir As String
            Get
                Return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EveryTongue", "templates")
            End Get
        End Property

        Private Shared Function GroupFile(group As String) As String
            Return Path.Combine(TemplatesDir, $"{group}-templates.json")
        End Function

        Private Shared ReadOnly Property SessionsFile As String
            Get
                Return Path.Combine(TemplatesDir, "session-templates.json")
            End Get
        End Property

        Private Shared ReadOnly Property SpeakersFile As String
            Get
                Return Path.Combine(TemplatesDir, "speaker-profiles.json")
            End Get
        End Property

        Private Shared ReadOnly Property DisplayFile As String
            Get
                Return Path.Combine(TemplatesDir, "display-templates.json")
            End Get
        End Property

        Private Shared ReadOnly Property FiltersFile As String
            Get
                Return Path.Combine(TemplatesDir, "filter-sets.json")
            End Get
        End Property

        ' ─── Load / save ──────────────────────────────────────────────

        Private Sub EnsureLoaded()
            If _loaded Then Return
            For Each group In {GroupStt, GroupTranslate, GroupTts}
                _engineTemplates(group) = LoadFile(Of List(Of EngineTemplate))(GroupFile(group), New List(Of EngineTemplate))
            Next
            _sessionTemplates = LoadFile(Of List(Of SessionTemplate))(SessionsFile, New List(Of SessionTemplate))
            _speakerProfiles = LoadFile(Of List(Of SpeakerProfile))(SpeakersFile, New List(Of SpeakerProfile))
            _displayTemplates = LoadFile(Of List(Of DisplayTemplate))(DisplayFile, New List(Of DisplayTemplate))
            _filterSets = LoadFile(Of List(Of FilterSet))(FiltersFile, New List(Of FilterSet))
            _loaded = True
            AppLogger.Log(LogEvents.CONFIG_TEMPLATE_LIB_LOADED,
                $"Template library loaded: stt={_engineTemplates(GroupStt).Count}, translate={_engineTemplates(GroupTranslate).Count}, tts={_engineTemplates(GroupTts).Count}, sessions={_sessionTemplates.Count}, speakers={_speakerProfiles.Count}, display={_displayTemplates.Count}, filters={_filterSets.Count}")
        End Sub

        Private Shared Function LoadFile(Of T As Class)(filePath As String, fallback As T) As T
            Try
                If Not File.Exists(filePath) Then Return fallback
                Dim result = JsonSerializer.Deserialize(Of T)(File.ReadAllText(filePath), _jsonOptions)
                Return If(result, fallback)
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_TEMPLATE_LIB_ERROR, $"Load failed for {Path.GetFileName(filePath)}: {ex.Message}")
                Return fallback
            End Try
        End Function

        Private Shared Sub SaveFile(filePath As String, payload As Object)
            Try
                Directory.CreateDirectory(TemplatesDir)
                File.WriteAllText(filePath, JsonSerializer.Serialize(payload, _jsonOptions))
                AppLogger.Log(LogEvents.CONFIG_TEMPLATE_LIB_SAVED, $"Saved {Path.GetFileName(filePath)}")
            Catch ex As Exception
                AppLogger.Log(LogEvents.CONFIG_TEMPLATE_LIB_ERROR, $"Save failed for {Path.GetFileName(filePath)}: {ex.Message}")
            End Try
        End Sub

        ' ─── Engine template libraries ────────────────────────────────

        Public Function GetEngineTemplates(group As String) As List(Of EngineTemplate)
            SyncLock _lock
                EnsureLoaded()
                Dim list As List(Of EngineTemplate) = Nothing
                If Not _engineTemplates.TryGetValue(group, list) Then Return New List(Of EngineTemplate)
                Return New List(Of EngineTemplate)(list)
            End SyncLock
        End Function

        Public Function GetEngineTemplate(group As String, id As String) As EngineTemplate
            SyncLock _lock
                EnsureLoaded()
                Dim list As List(Of EngineTemplate) = Nothing
                If Not _engineTemplates.TryGetValue(group, list) Then Return Nothing
                Return list.FirstOrDefault(Function(t) t.Id = id)
            End SyncLock
        End Function

        Public Sub UpsertEngineTemplate(group As String, template As EngineTemplate)
            SyncLock _lock
                EnsureLoaded()
                Dim list As List(Of EngineTemplate) = Nothing
                If Not _engineTemplates.TryGetValue(group, list) Then
                    list = New List(Of EngineTemplate)
                    _engineTemplates(group) = list
                End If
                list.RemoveAll(Function(t) t.Id = template.Id)
                list.Add(template)
                SaveFile(GroupFile(group), list)
            End SyncLock
        End Sub

        Public Sub DeleteEngineTemplate(group As String, id As String)
            SyncLock _lock
                EnsureLoaded()
                Dim list As List(Of EngineTemplate) = Nothing
                If Not _engineTemplates.TryGetValue(group, list) Then Return
                If list.RemoveAll(Function(t) t.Id = id) > 0 Then
                    SaveFile(GroupFile(group), list)
                End If
            End SyncLock
        End Sub

        ' ─── Session templates ────────────────────────────────────────

        Public Function GetSessionTemplates() As List(Of SessionTemplate)
            SyncLock _lock
                EnsureLoaded()
                Return New List(Of SessionTemplate)(_sessionTemplates)
            End SyncLock
        End Function

        Public Function GetSessionTemplate(id As String) As SessionTemplate
            SyncLock _lock
                EnsureLoaded()
                Return _sessionTemplates.FirstOrDefault(Function(t) t.Id = id)
            End SyncLock
        End Function

        Public Sub UpsertSessionTemplate(template As SessionTemplate)
            SyncLock _lock
                EnsureLoaded()
                _sessionTemplates.RemoveAll(Function(t) t.Id = template.Id)
                _sessionTemplates.Add(template)
                SaveFile(SessionsFile, _sessionTemplates)
            End SyncLock
        End Sub

        Public Sub DeleteSessionTemplate(id As String)
            SyncLock _lock
                EnsureLoaded()
                If _sessionTemplates.RemoveAll(Function(t) t.Id = id) > 0 Then
                    SaveFile(SessionsFile, _sessionTemplates)
                End If
            End SyncLock
        End Sub

        ' ─── Speaker profiles ─────────────────────────────────────────

        Public Function GetSpeakerProfiles() As List(Of SpeakerProfile)
            SyncLock _lock
                EnsureLoaded()
                Return New List(Of SpeakerProfile)(_speakerProfiles)
            End SyncLock
        End Function

        Public Function GetSpeakerProfile(id As String) As SpeakerProfile
            SyncLock _lock
                EnsureLoaded()
                Return _speakerProfiles.FirstOrDefault(Function(s) s.Id = id)
            End SyncLock
        End Function

        Public Sub UpsertSpeakerProfile(profile As SpeakerProfile)
            SyncLock _lock
                EnsureLoaded()
                _speakerProfiles.RemoveAll(Function(s) s.Id = profile.Id)
                _speakerProfiles.Add(profile)
                SaveFile(SpeakersFile, _speakerProfiles)
            End SyncLock
        End Sub

        Public Sub DeleteSpeakerProfile(id As String)
            SyncLock _lock
                EnsureLoaded()
                If _speakerProfiles.RemoveAll(Function(s) s.Id = id) > 0 Then
                    SaveFile(SpeakersFile, _speakerProfiles)
                End If
            End SyncLock
        End Sub

        ' ─── Display templates ────────────────────────────────────────

        Public Function GetDisplayTemplates() As List(Of DisplayTemplate)
            SyncLock _lock
                EnsureLoaded()
                Return New List(Of DisplayTemplate)(_displayTemplates)
            End SyncLock
        End Function

        Public Function GetDisplayTemplate(id As String) As DisplayTemplate
            SyncLock _lock
                EnsureLoaded()
                Return _displayTemplates.FirstOrDefault(Function(d) d.Id = id)
            End SyncLock
        End Function

        Public Sub UpsertDisplayTemplate(template As DisplayTemplate)
            SyncLock _lock
                EnsureLoaded()
                _displayTemplates.RemoveAll(Function(d) d.Id = template.Id)
                _displayTemplates.Add(template)
                SaveFile(DisplayFile, _displayTemplates)
            End SyncLock
        End Sub

        Public Sub DeleteDisplayTemplate(id As String)
            SyncLock _lock
                EnsureLoaded()
                If _displayTemplates.RemoveAll(Function(d) d.Id = id) > 0 Then
                    SaveFile(DisplayFile, _displayTemplates)
                End If
            End SyncLock
        End Sub

        ' ─── Filter sets ──────────────────────────────────────────────

        Public Function GetFilterSets() As List(Of FilterSet)
            SyncLock _lock
                EnsureLoaded()
                Return New List(Of FilterSet)(_filterSets)
            End SyncLock
        End Function

        Public Function GetFilterSet(id As String) As FilterSet
            SyncLock _lock
                EnsureLoaded()
                Return _filterSets.FirstOrDefault(Function(f) f.Id = id)
            End SyncLock
        End Function

        Public Sub UpsertFilterSet(filterSet As FilterSet)
            SyncLock _lock
                EnsureLoaded()
                _filterSets.RemoveAll(Function(f) f.Id = filterSet.Id)
                _filterSets.Add(filterSet)
                SaveFile(FiltersFile, _filterSets)
            End SyncLock
        End Sub

        Public Sub DeleteFilterSet(id As String)
            SyncLock _lock
                EnsureLoaded()
                If _filterSets.RemoveAll(Function(f) f.Id = id) > 0 Then
                    SaveFile(FiltersFile, _filterSets)
                End If
            End SyncLock
        End Sub

    End Class

End Namespace
