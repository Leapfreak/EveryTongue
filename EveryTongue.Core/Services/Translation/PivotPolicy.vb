Imports System.IO
Imports System.Text.Json
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Models
Imports EveryTongue.Models

Namespace Services.Translation

    ''' <summary>
    ''' English-pivot routing policy: decides, per (source, target, backend), whether
    ''' to translate direct or source→English→target. The SAME instance answers both
    ''' the orchestrator's routing and the /api/translation/routing explain endpoint,
    ''' so what the UI displays is definitionally what the orchestrator does.
    '''
    ''' Decision order in Auto mode: measured/seed entry in
    ''' wwwroot/data/translation-direct-pairs.json (engine-scoped entries win only for
    ''' their engines) → the engine's EnglishCentric bias from
    ''' TranslationBackendRegistry. Pairs involving the pivot language never pivot.
    ''' </summary>
    Public Class PivotPolicy

        Private Class DirectPairEntry
            Public Property A As String
            Public Property B As String
            Public Property Reason As String
            Public Property Source As String
            ''' <summary>Registry engine KEYS this entry is limited to; empty = all engines.</summary>
            Public Property Engines As New List(Of String)
        End Class

        Private ReadOnly _pairs As New List(Of DirectPairEntry)
        Private _mode As TranslationPivotMode ' mutable: live-applied from /api/settings
        Private ReadOnly _pivotLanguage As String

        Public ReadOnly Property Mode As TranslationPivotMode
            Get
                Return _mode
            End Get
        End Property

        Public ReadOnly Property PivotLanguage As String
            Get
                Return _pivotLanguage
            End Get
        End Property

        Public ReadOnly Property PairCount As Integer
            Get
                Return _pairs.Count
            End Get
        End Property

        Public Sub New(options As Server.ServerOptions)
            _mode = If(options?.TranslationPivotMode, TranslationPivotMode.Auto)
            _pivotLanguage = If(String.IsNullOrWhiteSpace(options?.TranslationPivotLanguage),
                                "eng_Latn", options.TranslationPivotLanguage)
            LoadPairsFile()

            ' Startup summary — the one line that tells a Lite console or the desktop
            ' log what the effective policy is (mode, via, pair counts).
            Dim seedCount = _pairs.Where(Function(p) Not "measured".Equals(If(p.Source, ""), StringComparison.OrdinalIgnoreCase)).Count()
            AppLogger.Log(LogEvents.TRANS_PIVOT,
                $"Pivot policy: mode={_mode}, via {_pivotLanguage}, {_pairs.Count} direct pairs loaded " &
                $"({seedCount} seed / {_pairs.Count - seedCount} measured); " &
                "unlisted non-English pairs " &
                If(_mode = TranslationPivotMode.Off, "translate DIRECT (pivot off)",
                If(_mode = TranslationPivotMode.Always, "always PIVOT",
                   "pivot when the engine is English-centric")))
        End Sub

        ''' <summary>
        ''' Live-apply a mode change (web settings page) — takes effect on the next
        ''' translation, no server restart. An enum write is atomic; in-flight
        ''' decisions simply use whichever mode they read.
        ''' </summary>
        Public Sub UpdateMode(mode As TranslationPivotMode)
            If mode = _mode Then Return
            _mode = mode
            AppLogger.Log(LogEvents.TRANS_PIVOT, $"Pivot policy mode changed → {mode} (live-applied)")
        End Sub

        Private Sub LoadPairsFile()
            ' Shipped seed file (wwwroot, replaced on every release) + a LOCAL
            ' overlay in the config directory. Measured entries written by the
            ' benchmark land in the overlay so they survive publishes and Lite
            ' container updates. Overlay entries are appended (both lists apply).
            Dim shipped = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "wwwroot", "data", "translation-direct-pairs.json")
            If File.Exists(shipped) Then
                LoadPairsFrom(shipped)
            Else
                AppLogger.Log(LogEvents.TRANS_PIVOT,
                    $"translation-direct-pairs.json not found ({shipped}) — Auto mode will pivot every non-English pair on English-centric engines")
            End If

            Try
                Dim overlay = Path.Combine(EveryTongue.Models.ConfigManager.ConfigDirectory,
                                           "translation-direct-pairs.local.json")
                If File.Exists(overlay) Then LoadPairsFrom(overlay)
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"Failed to load translation-direct-pairs.local.json overlay: {ex.Message}")
            End Try
        End Sub

        Private Sub LoadPairsFrom(jsonPath As String)
            Try
                Using doc = JsonDocument.Parse(File.ReadAllText(jsonPath))
                    Dim pairsEl As JsonElement = Nothing
                    If Not doc.RootElement.TryGetProperty("pairs", pairsEl) Then Return
                    For Each el In pairsEl.EnumerateArray()
                        Dim entry As New DirectPairEntry With {
                            .A = GetStr(el, "a"),
                            .B = GetStr(el, "b"),
                            .Reason = GetStr(el, "reason"),
                            .Source = GetStr(el, "source")
                        }
                        Dim enginesEl As JsonElement = Nothing
                        If el.TryGetProperty("engines", enginesEl) AndAlso enginesEl.ValueKind = JsonValueKind.Array Then
                            For Each e In enginesEl.EnumerateArray()
                                entry.Engines.Add(e.GetString())
                            Next
                        End If
                        If Not String.IsNullOrEmpty(entry.A) AndAlso Not String.IsNullOrEmpty(entry.B) Then
                            _pairs.Add(entry)
                        End If
                    Next
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"Failed to parse {Path.GetFileName(jsonPath)}: {ex.Message} — its direct-pairs entries are skipped")
            End Try
        End Sub

        Private Shared Function GetStr(el As JsonElement, name As String) As String
            Dim v As JsonElement = Nothing
            If el.TryGetProperty(name, v) AndAlso v.ValueKind = JsonValueKind.String Then Return v.GetString()
            Return ""
        End Function

        ''' <summary>
        ''' Decide routing for one target. backendName is the ORCHESTRATOR backend
        ''' name (e.g. "Local", "Google") — the group key the orchestrator routes by.
        ''' </summary>
        Public Function Decide(sourceLang As String, targetLang As String, backendName As String) As PivotDecision
            If _mode = TranslationPivotMode.Off Then
                Return Direct("pivot mode is Off")
            End If
            If String.IsNullOrWhiteSpace(sourceLang) OrElse String.IsNullOrWhiteSpace(targetLang) OrElse
               sourceLang.Equals(targetLang, StringComparison.OrdinalIgnoreCase) Then
                Return Direct("no distinct source/target pair")
            End If
            If sourceLang.Equals(_pivotLanguage, StringComparison.OrdinalIgnoreCase) OrElse
               targetLang.Equals(_pivotLanguage, StringComparison.OrdinalIgnoreCase) Then
                Return Direct($"pair already includes {_pivotLanguage}")
            End If
            If _mode = TranslationPivotMode.Always Then
                Return Pivot("pivot mode is Always")
            End If

            ' Auto: direct-pairs file first (engine-scoped entries win only for their engines)
            Dim engineKey = If(TranslationBackendRegistry.FindByBackendName(backendName)?.Key, "")
            Dim entry = _pairs.FirstOrDefault(
                Function(p) PairMatches(p, sourceLang, targetLang) AndAlso EngineMatches(p, engineKey))
            If entry IsNot Nothing Then
                Return Direct($"direct pair ({If(entry.Source, "seed")}): {entry.Reason}")
            End If

            ' Then the engine's bias. Unknown backends (e.g. dynamically registered
            ' per-room NLLB sidecars) are treated as English-centric — conservative.
            Dim regEntry = TranslationBackendRegistry.FindByBackendName(backendName)
            Dim englishCentric = If(regEntry Is Nothing, True, regEntry.EnglishCentric)
            If englishCentric Then
                Return Pivot("engine is English-centric and the pair is not on the direct-pairs list")
            End If
            Return Direct("engine handles direct pairs well")
        End Function

        Private Shared Function PairMatches(p As DirectPairEntry, source As String, target As String) As Boolean
            ' Entries are symmetric: one line covers a→b and b→a.
            Return (p.A.Equals(source, StringComparison.OrdinalIgnoreCase) AndAlso p.B.Equals(target, StringComparison.OrdinalIgnoreCase)) OrElse
                   (p.A.Equals(target, StringComparison.OrdinalIgnoreCase) AndAlso p.B.Equals(source, StringComparison.OrdinalIgnoreCase))
        End Function

        Private Shared Function EngineMatches(p As DirectPairEntry, engineKey As String) As Boolean
            If p.Engines Is Nothing OrElse p.Engines.Count = 0 Then Return True
            Return p.Engines.Any(Function(e) e.Equals(engineKey, StringComparison.OrdinalIgnoreCase))
        End Function

        Private Function Direct(reason As String) As PivotDecision
            Return New PivotDecision With {.ShouldPivot = False, .Via = "", .Reason = reason}
        End Function

        Private Function Pivot(reason As String) As PivotDecision
            Return New PivotDecision With {.ShouldPivot = True, .Via = _pivotLanguage, .Reason = reason}
        End Function

    End Class

End Namespace
