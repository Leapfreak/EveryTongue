Namespace Services.Config

    ''' <summary>UI rendering hint for a single engine-config field.</summary>
    Public Enum EngineConfigFieldType
        Text
        [Integer]
        [Decimal]
        Toggle
        Choice
        FilePath
        DirectoryPath
        StringList
    End Enum

    ''' <summary>
    ''' Self-description of one knob an engine exposes. The Options/Template UI
    ''' renders from these — adding an engine adds zero UI code.
    ''' </summary>
    Public Class EngineConfigField
        ''' <summary>Property name on the engine's config class (and JSON key).</summary>
        Public Property Key As String = ""
        ''' <summary>Localization key for the field label (resolved via LanguagePackService when rendered).</summary>
        Public Property LabelKey As String = ""
        Public Property FieldType As EngineConfigFieldType = EngineConfigFieldType.Text
        Public Property Min As Double? = Nothing
        Public Property Max As Double? = Nothing
        ''' <summary>Allowed values for Choice fields.</summary>
        Public Property Choices As List(Of String)
        ''' <summary>UI hint: tuck behind an "Advanced" expander.</summary>
        Public Property Advanced As Boolean = False
    End Class

End Namespace
