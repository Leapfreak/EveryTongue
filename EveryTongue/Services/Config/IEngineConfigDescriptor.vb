Imports System.Text.Json
Imports EveryTongue.Models

Namespace Services.Config

    ''' <summary>
    ''' Implemented by every engine's config block. The only thing shared code may
    ''' ask a block to do is fill machine-level baseline values (paths, ports,
    ''' machine defaults) from the app-global config — the block itself decides
    ''' which fields that means. No engine reads another engine's block.
    ''' </summary>
    Public Interface IEngineConfigBlock
        Sub ApplyMachineBaseline(cfg As AppConfig)
    End Interface

    ''' <summary>
    ''' Self-description of an engine's configuration: its fields, defaults and
    ''' (de)serialization. Registered alongside the engine in its backend registry.
    ''' Shared code drives templates and UI entirely through this interface —
    ''' it never knows a concrete engine's config type.
    ''' </summary>
    Public Interface IEngineConfigDescriptor
        ''' <summary>Engine key this descriptor belongs to (e.g. "speechmatics").</summary>
        ReadOnly Property EngineKey As String
        ''' <summary>CLR type of the engine's config block.</summary>
        ReadOnly Property ConfigType As Type
        ''' <summary>Template-editable knobs, in display order.</summary>
        ReadOnly Property Fields As IReadOnlyList(Of EngineConfigField)

        ''' <summary>New block with the engine's own defaults.</summary>
        Function CreateDefault() As IEngineConfigBlock
        ''' <summary>Apply the fields present in a stored template JSON block onto an existing block (absent fields untouched).</summary>
        Sub ApplyJson(block As IEngineConfigBlock, json As JsonElement)
        ''' <summary>Serialize a block for template storage.</summary>
        Function Serialize(block As IEngineConfigBlock) As JsonElement
        ''' <summary>Apply loose-typed per-field overrides (session/web). Unknown keys are ignored. Returns the keys actually applied.</summary>
        Function ApplyOverrides(block As IEngineConfigBlock, fieldOverrides As IDictionary(Of String, Object)) As List(Of String)
        ''' <summary>Range-check declared fields. Returns warnings (empty = valid).</summary>
        Function Validate(block As IEngineConfigBlock) As List(Of String)
    End Interface

End Namespace
