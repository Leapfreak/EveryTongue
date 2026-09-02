Imports System.Net
Imports System.Net.Sockets
Imports System.Threading.Tasks
Imports EveryTongue.Models
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure

Namespace Services.Translation

    ''' <summary>
    ''' Pool of additional offline translation sidecars — one process per distinct
    ''' (model path, compute_type) signature — so different rooms can run DIFFERENT
    ''' offline models concurrently. The global-default model is NOT managed here
    ''' (it stays the "Local" sidecar owned by FormMain, serving desktop/fallback/
    ''' default rooms). Rooms whose engine matches the default reuse "Local"; rooms
    ''' on a different model get a pooled sidecar named "Local:&lt;sig&gt;".
    '''
    ''' Reference-counted by owner id (room id): the last room to release a model
    ''' stops its sidecar and frees the VRAM. Same model + precision ⇒ one shared
    ''' sidecar (no second load). Thread-safe.
    ''' </summary>
    Public Class TranslationSidecarPool

        Private Class PooledSidecar
            Public Sig As String
            Public Name As String
            Public Svc As TranslationService
            Public Refs As Integer
        End Class

        Private ReadOnly _orchestrator As TranslationOrchestrator
        Private ReadOnly _config As AppConfig
        Private ReadOnly _lock As New Object()
        Private ReadOnly _bySig As New Dictionary(Of String, PooledSidecar)(StringComparer.OrdinalIgnoreCase)
        Private ReadOnly _byOwner As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) ' ownerId → sig

        Public Sub New(orchestrator As TranslationOrchestrator, config As AppConfig)
            _orchestrator = orchestrator
            _config = config
        End Sub

        ''' <summary>Signature for a model directory + compute type (the dedup key).</summary>
        Public Shared Function Signature(modelPath As String, computeType As String) As String
            Return AppConfig.ResolvePath(modelPath) & "|" & If(String.IsNullOrEmpty(computeType), "auto", computeType)
        End Function

        ''' <summary>Orchestrator backend name for a pooled (non-default) offline model.</summary>
        Public Shared Function NameFor(modelPath As String, computeType As String) As String
            Return "Local:" & Signature(modelPath, computeType)
        End Function

        ''' <summary>
        ''' Ensure a sidecar exists for this model+compute and bump its refcount for ownerId.
        ''' Returns the orchestrator backend name to use as backendOverride. Releases any
        ''' previous sidecar this owner held (so switching a room's engine is clean).
        ''' </summary>
        Public Function Acquire(ownerId As String, engineKey As String, modelPath As String, modelType As String, computeType As String) As String
            Dim sig = Signature(modelPath, computeType)
            Dim name = "Local:" & sig
            Dim arbiter = EngineResidencyArbiter.Instance

            ' Arbitration happens OUTSIDE the pool lock (an eviction may re-enter
            ' this pool via EvictSidecar). The decision also records the lease.
            Dim decision As LoadDecision
            SyncLock _lock
                Dim priorSig As String = Nothing
                If _byOwner.TryGetValue(ownerId, priorSig) AndAlso
                   priorSig.Equals(sig, StringComparison.OrdinalIgnoreCase) Then Return name
            End SyncLock
            decision = arbiter.RequestLoad(EngineCategory.Translation, sig, name, engineKey, ownerId)

            If decision.Kind = LoadDecisionKind.Denied Then
                ' At limit, every resident leased (ENGINE_CONCURRENCY_PLAN row 4):
                ' translation RE-ROUTES to a leased resident — the room keeps
                ' translating, the operator is told which engine substituted.
                Dim reroute = If(decision.ResidentBackendNames.FirstOrDefault(), "Local")
                AppLogger.Log(LogEvents.ENGINE_RESIDENCY_CONFLICT,
                    $"Translation at MaxConcurrentTranslationEngines with all engines in use — room '{ownerId}' wanted '{engineKey}' but is re-routed to '{reroute}' ({String.Join(", ", decision.ResidentDisplays)}). Raise the limit in Options → Advanced to run more local models at once.")
                Return reroute
            End If

            SyncLock _lock
                Dim priorSig As String = Nothing
                If _byOwner.TryGetValue(ownerId, priorSig) AndAlso
                   Not priorSig.Equals(sig, StringComparison.OrdinalIgnoreCase) Then
                    ReleaseInternal(ownerId)   ' owner switched models — drop the old lease
                End If

                Dim e As PooledSidecar = Nothing
                If Not _bySig.TryGetValue(sig, e) Then
                    Dim port = FreePort()
                    Dim svc As New TranslationService()
                    Dim nm = name
                    AddHandler svc.StatusChanged, Sub(s, m) AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, $"[pool {nm}] {m}")
                    svc.Start(port, modelPath, If(_config.TranslationDevice, "cuda"),
                              _config.TranslationGlossaryPath, If(modelType, "nllb"), computeType)
                    _orchestrator.RegisterBackend(New SidecarTranslationBackend(svc, name, engineKey))
                    e = New PooledSidecar With {.Sig = sig, .Name = name, .Svc = svc, .Refs = 0}
                    _bySig(sig) = e
                    Dim capturedSig = sig
                    arbiter.RegisterResident(EngineCategory.Translation, sig, name, engineKey,
                                             evict:=Sub() EvictSidecar(capturedSig), ownerId:=ownerId)
                    AppLogger.Log(LogEvents.TRANS_SERVER_STARTING,
                        $"Translation pool: started '{engineKey}' on port {port} as backend '{name}'")
                End If
                e.Refs += 1
                _byOwner(ownerId) = sig
                Return name
            End SyncLock
        End Function

        ''' <summary>Release the owner's lease. The sidecar STAYS resident as a warm
        ''' spare (ENGINE_CONCURRENCY_PLAN: instant reuse; the arbiter evicts it only
        ''' when a different engine needs the slot).</summary>
        Public Sub Release(ownerId As String)
            SyncLock _lock
                ReleaseInternal(ownerId)
            End SyncLock
            EngineResidencyArbiter.Instance.ReleaseOwner(EngineCategory.Translation, ownerId)
        End Sub

        Private Sub ReleaseInternal(ownerId As String)   ' caller holds _lock
            Dim sig As String = Nothing
            If Not _byOwner.TryGetValue(ownerId, sig) Then Return
            _byOwner.Remove(ownerId)
            Dim e As PooledSidecar = Nothing
            If Not _bySig.TryGetValue(sig, e) Then Return
            e.Refs -= 1
            If e.Refs > 0 Then Return
            ' Refs 0 = warm spare: process and orchestrator registration stay —
            ' the next room on this model starts instantly instead of repaying
            ' the ~30s load. The arbiter owns eviction from here.
            AppLogger.Log(LogEvents.ENGINE_RESIDENCY,
                $"Translation pool: '{e.Name}' now a warm spare (no rooms using it — stays loaded, evictable)")
        End Sub

        ''' <summary>Arbiter evict action: actually stop a spare sidecar and free VRAM.</summary>
        Private Sub EvictSidecar(sig As String)
            Dim svc As TranslationService = Nothing
            Dim nm As String = Nothing
            SyncLock _lock
                Dim e As PooledSidecar = Nothing
                If Not _bySig.TryGetValue(sig, e) Then Return
                _bySig.Remove(sig)
                svc = e.Svc
                nm = e.Name
            End SyncLock
            _orchestrator.UnregisterBackend(nm)
            Try : svc.Stop() : Catch : End Try
            AppLogger.Log(LogEvents.TRANS_SERVER_STARTING, $"Translation pool: stopped backend '{nm}' (evicted by residency arbiter)")
        End Sub

        ''' <summary>Stop every pooled sidecar (app shutdown).</summary>
        Public Sub StopAll()
            Dim entries As List(Of PooledSidecar)
            SyncLock _lock
                entries = _bySig.Values.ToList()
                _bySig.Clear()
                _byOwner.Clear()
            End SyncLock
            For Each e In entries
                EngineResidencyArbiter.Instance.DropResident(EngineCategory.Translation, e.Sig)
                _orchestrator.UnregisterBackend(e.Name)
                Try : e.Svc.Stop() : Catch : End Try
            Next
        End Sub

        Private Shared Function FreePort() As Integer
            Dim l As New TcpListener(IPAddress.Loopback, 0)
            l.Start()
            Try
                Return CType(l.LocalEndpoint, IPEndPoint).Port
            Finally
                l.Stop()
            End Try
        End Function

    End Class

End Namespace
