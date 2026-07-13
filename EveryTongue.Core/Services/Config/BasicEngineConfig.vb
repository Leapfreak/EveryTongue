Imports EveryTongue.Models

Namespace Services.Config

    ''' <summary>
    ''' Placeholder config block for engines that expose no per-session knobs yet
    ''' (their settings are still app-global). A template bound to such an engine
    ''' records the engine choice only. The moment an engine grows real knobs it
    ''' gets its own block class beside the engine — never fields added here.
    ''' </summary>
    Public Class BasicEngineConfig
        Implements IEngineConfigBlock

        Public Sub ApplyMachineBaseline(cfg As AppConfig) Implements IEngineConfigBlock.ApplyMachineBaseline
        End Sub
    End Class

    ''' <summary>Descriptor for engines with an empty config block (one instance per engine key).</summary>
    Public Class BasicEngineConfigDescriptor
        Inherits EngineConfigDescriptorBase(Of BasicEngineConfig)

        Private Shared ReadOnly _noFields As IReadOnlyList(Of EngineConfigField) = New List(Of EngineConfigField)
        Private ReadOnly _engineKey As String

        Public Sub New(engineKey As String)
            _engineKey = engineKey
        End Sub

        Public Overrides ReadOnly Property EngineKey As String
            Get
                Return _engineKey
            End Get
        End Property

        Public Overrides ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)
            Get
                Return _noFields
            End Get
        End Property
    End Class

End Namespace
