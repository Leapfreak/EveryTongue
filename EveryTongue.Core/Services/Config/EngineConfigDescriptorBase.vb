Imports System.Reflection
Imports System.Text.Json
Imports EveryTongue.Services.Infrastructure

Namespace Services.Config

    ''' <summary>
    ''' Generic descriptor base — engines only declare their key and field list;
    ''' (de)serialization, overrides and range validation are driven from the
    ''' declared fields via reflection so every engine behaves identically.
    ''' </summary>
    Public MustInherit Class EngineConfigDescriptorBase(Of T As {Class, IEngineConfigBlock, New})
        Implements IEngineConfigDescriptor

        Private Shared ReadOnly _jsonOptions As New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True,
            .WriteIndented = True
        }

        Public MustOverride ReadOnly Property EngineKey As String Implements IEngineConfigDescriptor.EngineKey
        Public MustOverride ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField) Implements IEngineConfigDescriptor.Fields

        Public ReadOnly Property ConfigType As Type Implements IEngineConfigDescriptor.ConfigType
            Get
                Return GetType(T)
            End Get
        End Property

        Public Function CreateDefault() As IEngineConfigBlock Implements IEngineConfigDescriptor.CreateDefault
            Return New T()
        End Function

        Public Sub ApplyJson(block As IEngineConfigBlock, json As JsonElement) Implements IEngineConfigDescriptor.ApplyJson
            If json.ValueKind <> JsonValueKind.Object Then Return
            ' Path-typed fields are populated from the machine baseline (ApplyMachineBaseline)
            ' BEFORE the template is applied. A template that did not capture a path stores it
            ' as "" — applying that empty value would CLOBBER the baseline (→ "whisper-server
            ' path not configured" / "Model path not configured" at runtime). An empty path in
            ' a template means "inherit the baseline", so skip empty FilePath values here.
            Dim pathFields As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each f In Fields
                If f.FieldType = EngineConfigFieldType.FilePath Then pathFields.Add(f.Key)
            Next
            For Each jsonProp In json.EnumerateObject()
                Dim clrProp = FindProperty(jsonProp.Name)
                If clrProp Is Nothing OrElse Not clrProp.CanWrite Then Continue For
                If pathFields.Contains(jsonProp.Name) AndAlso
                   jsonProp.Value.ValueKind = JsonValueKind.String AndAlso
                   String.IsNullOrEmpty(jsonProp.Value.GetString()) Then
                    Continue For ' empty path → keep the machine baseline
                End If
                Try
                    Dim value = JsonSerializer.Deserialize(jsonProp.Value.GetRawText(), clrProp.PropertyType, _jsonOptions)
                    clrProp.SetValue(block, value)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                        $"[{EngineKey}] Ignoring template field '{jsonProp.Name}': {ex.Message}")
                End Try
            Next
        End Sub

        Public Function Serialize(block As IEngineConfigBlock) As JsonElement Implements IEngineConfigDescriptor.Serialize
            Using doc = JsonDocument.Parse(JsonSerializer.Serialize(block, GetType(T), _jsonOptions))
                Return doc.RootElement.Clone()
            End Using
        End Function

        Public Function ApplyOverrides(block As IEngineConfigBlock, fieldOverrides As IDictionary(Of String, Object)) As List(Of String) Implements IEngineConfigDescriptor.ApplyOverrides
            Dim applied As New List(Of String)
            If fieldOverrides Is Nothing Then Return applied
            For Each kvp In fieldOverrides
                Dim clrProp = FindProperty(kvp.Key)
                If clrProp Is Nothing OrElse Not clrProp.CanWrite Then Continue For
                Try
                    clrProp.SetValue(block, CoerceValue(kvp.Value, clrProp.PropertyType))
                    applied.Add(clrProp.Name)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.CONFIG_ENGINE_VALIDATION,
                        $"[{EngineKey}] Ignoring override '{kvp.Key}': {ex.Message}")
                End Try
            Next
            Return applied
        End Function

        Public Function Validate(block As IEngineConfigBlock) As List(Of String) Implements IEngineConfigDescriptor.Validate
            Dim warnings As New List(Of String)
            For Each field In Fields
                If Not field.Min.HasValue AndAlso Not field.Max.HasValue Then Continue For
                Dim clrProp = FindProperty(field.Key)
                If clrProp Is Nothing Then Continue For
                Dim raw = clrProp.GetValue(block)
                Dim numeric As Double
                If raw Is Nothing OrElse Not Double.TryParse(Convert.ToString(raw, Globalization.CultureInfo.InvariantCulture),
                                                             Globalization.NumberStyles.Any, Globalization.CultureInfo.InvariantCulture, numeric) Then
                    Continue For
                End If
                If field.Min.HasValue AndAlso numeric < field.Min.Value Then
                    warnings.Add($"{field.Key}={raw} below minimum {field.Min.Value}")
                ElseIf field.Max.HasValue AndAlso numeric > field.Max.Value Then
                    warnings.Add($"{field.Key}={raw} above maximum {field.Max.Value}")
                End If
            Next
            Return warnings
        End Function

        Private Function FindProperty(name As String) As PropertyInfo
            Return GetType(T).GetProperty(name, BindingFlags.Public Or BindingFlags.Instance Or BindingFlags.IgnoreCase)
        End Function

        ''' <summary>Convert loose override values (boxed primitives or JsonElement from the web API) to the property type.</summary>
        Private Shared Function CoerceValue(value As Object, targetType As Type) As Object
            If value Is Nothing Then Return Nothing
            If TypeOf value Is JsonElement Then
                Return JsonSerializer.Deserialize(DirectCast(value, JsonElement).GetRawText(), targetType, _jsonOptions)
            End If
            If targetType.IsInstanceOfType(value) Then Return value
            If targetType Is GetType(List(Of String)) AndAlso TypeOf value Is IEnumerable(Of Object) Then
                Return DirectCast(value, IEnumerable(Of Object)).Select(Function(o) Convert.ToString(o)).ToList()
            End If
            Return Convert.ChangeType(value, targetType, Globalization.CultureInfo.InvariantCulture)
        End Function

    End Class

End Namespace
