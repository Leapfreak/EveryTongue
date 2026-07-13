Imports System.Collections.Concurrent
Imports EveryTongue.Models

Namespace Services.Rooms

    ''' <summary>
    ''' Thread-safe store for conference room templates.
    ''' Registered as a singleton in DI. The UI thread calls SyncFromConfig()
    ''' on startup and after Options save; HTTP threads call read methods.
    ''' </summary>
    Public Class TemplateStore

        ''' <summary>Static reference set by KestrelHost after DI build, for UI-thread access.</summary>
        Public Shared Property Instance As TemplateStore

        Private ReadOnly _templates As New ConcurrentDictionary(Of String, ConferenceTemplate)(StringComparer.OrdinalIgnoreCase)

        Public Function GetAll() As List(Of ConferenceTemplate)
            Return _templates.Values.ToList()
        End Function

        Public Function GetById(id As String) As ConferenceTemplate
            Dim t As ConferenceTemplate = Nothing
            If Not String.IsNullOrEmpty(id) Then _templates.TryGetValue(id, t)
            Return t
        End Function

        Public Function ValidateHostingCode(templateId As String, code As String) As Boolean
            Dim t = GetById(templateId)
            If t Is Nothing Then Return False
            Return String.Equals(t.HostingCode, code, StringComparison.Ordinal)
        End Function

        Public Sub Add(template As ConferenceTemplate)
            If template Is Nothing Then Return
            _templates(template.Id) = template
        End Sub

        Public Sub Update(template As ConferenceTemplate)
            If template Is Nothing Then Return
            _templates(template.Id) = template
        End Sub

        Public Sub Remove(id As String)
            If String.IsNullOrEmpty(id) Then Return
            Dim removed As ConferenceTemplate = Nothing
            _templates.TryRemove(id, removed)
        End Sub

        ''' <summary>
        ''' Replaces all templates from the config list. Called on startup
        ''' and after the user saves changes in FormOptions/FormTemplateManager.
        ''' </summary>
        Public Sub SyncFromConfig(templates As List(Of ConferenceTemplate))
            _templates.Clear()
            If templates Is Nothing Then Return
            For Each t In templates
                _templates(t.Id) = t
            Next
        End Sub

    End Class

End Namespace
