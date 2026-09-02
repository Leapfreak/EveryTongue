' ReadinessTest — behavioural checks for SidecarReadiness (idle-based waits) and
' EngineResidencyArbiter (limits / warm spares / re-route / idle event).
' Run: dotnet run --project tools\ReadinessTest -c Release
' Exit 0 = all pass; exit 1 = a check failed (message names it).

Imports System.Threading
Imports EveryTongue.Pipeline
Imports EveryTongue.Services.Infrastructure

Module Program

    Private _failures As Integer = 0

    Sub Check(name As String, cond As Boolean, Optional detail As String = "")
        If cond Then
            Console.WriteLine($"PASS  {name}")
        Else
            _failures += 1
            Console.WriteLine($"FAIL  {name}  {detail}")
        End If
    End Sub

    Function Main() As Integer
        Console.WriteLine("── SidecarReadiness ──")

        ' 1. Ready: probe true on the 3rd poll.
        Dim calls = 0
        Dim r = SidecarReadiness.WaitAsync("t1",
            Function(ct)
                calls += 1
                Return Task.FromResult(calls >= 3)
            End Function,
            Function() True, Function() 0L,
            CancellationToken.None, idleTimeoutSeconds:=5, pollIntervalMs:=50).Result
        Check("ready when probe succeeds", r.Outcome = ReadinessOutcome.Ready, $"got {r.Outcome}")

        ' 2. NoProgress: probe always throws, host silent (activity ms keeps growing).
        Dim t0 = Environment.TickCount64
        r = SidecarReadiness.WaitAsync("t2",
            Function(ct) As Task(Of Boolean)
                Throw New Exception("connection refused")
            End Function,
            Function() True, Function() Environment.TickCount64 - t0,
            CancellationToken.None, idleTimeoutSeconds:=5, pollIntervalMs:=100).Result
        Dim waited = Environment.TickCount64 - t0
        Check("no-progress gives up after idle window", r.Outcome = ReadinessOutcome.NoProgress, $"got {r.Outcome}")
        Check("idle give-up near 5s (not a longer wall clock)", waited >= 4500 AndAlso waited < 9000, $"waited {waited}ms")
        Check("last probe error carried", r.LastProbeError.Contains("refused"), r.LastProbeError)

        ' 3. ProcessExited: dead process fails immediately (no 30s-style wait).
        t0 = Environment.TickCount64
        r = SidecarReadiness.WaitAsync("t3",
            Function(ct) Task.FromResult(False),
            Function() False, Function() 0L,
            CancellationToken.None, idleTimeoutSeconds:=5, pollIntervalMs:=100).Result
        waited = Environment.TickCount64 - t0
        Check("dead process fails fast", r.Outcome = ReadinessOutcome.ProcessExited AndAlso waited < 1500, $"got {r.Outcome} after {waited}ms")

        ' 4. Progress extends the wait past the idle window (the anti-wall-clock core):
        '    host shows activity for 8s (> 5s idle), THEN the probe succeeds.
        t0 = Environment.TickCount64
        r = SidecarReadiness.WaitAsync("t4",
            Function(ct) Task.FromResult(Environment.TickCount64 - t0 > 8000),
            Function() True,
            Function() As Long
                ' Active for the first 8s (activity always fresh), then silent.
                Return If(Environment.TickCount64 - t0 <= 8000, 0L, Environment.TickCount64 - t0 - 8000)
            End Function,
            CancellationToken.None, idleTimeoutSeconds:=5, pollIntervalMs:=100).Result
        waited = Environment.TickCount64 - t0
        Check("visible progress outlives the idle window (no hard cap)",
              r.Outcome = ReadinessOutcome.Ready AndAlso waited > 7500, $"got {r.Outcome} after {waited}ms")

        ' 5. Cancellation exits promptly and is reported as Cancelled.
        Using cts As New CancellationTokenSource(500)
            t0 = Environment.TickCount64
            r = SidecarReadiness.WaitAsync("t5",
                Function(ct) Task.FromResult(False),
                Function() True, Function() 0L,
                cts.Token, idleTimeoutSeconds:=30, pollIntervalMs:=100).Result
            waited = Environment.TickCount64 - t0
            Check("cancel exits promptly", r.Outcome = ReadinessOutcome.Cancelled AndAlso waited < 2500, $"got {r.Outcome} after {waited}ms")
        End Using

        Console.WriteLine("── EngineResidencyArbiter ──")
        Dim arb As New EngineResidencyArbiter() With {
            .LimitProvider = Function(cat) 1
        }
        Dim idleRaised = 0
        AddHandler arb.CategoryIdle, Sub(cat) idleRaised += 1

        ' 6. First load granted; same key shares.
        Dim d = arb.RequestLoad(EngineCategory.Translation, "modelA", "Local:A", "engine-a", "room1")
        Check("first load granted", d.Kind = LoadDecisionKind.Granted, $"got {d.Kind}")
        Dim evictedA = False
        arb.RegisterResident(EngineCategory.Translation, "modelA", "Local:A", "engine-a",
                             evict:=Sub() evictedA = True, ownerId:="room1")
        d = arb.RequestLoad(EngineCategory.Translation, "modelA", "Local:A", "engine-a", "room2")
        Check("same engine shares", d.Kind = LoadDecisionKind.ShareExisting, $"got {d.Kind}")

        ' 7. Different engine at limit with active leases → Denied + re-route names.
        d = arb.RequestLoad(EngineCategory.Translation, "modelB", "Local:B", "engine-b", "room3")
        Check("leased resident denies a different engine", d.Kind = LoadDecisionKind.Denied, $"got {d.Kind}")
        Check("denial names the re-route backend", d.ResidentBackendNames.Contains("Local:A"),
              String.Join(",", d.ResidentBackendNames))
        Check("no eviction happened while leased", Not evictedA)

        ' 8. Release both rooms → CategoryIdle fires once; resident stays as spare.
        arb.ReleaseOwner(EngineCategory.Translation, "room1")
        arb.ReleaseOwner(EngineCategory.Translation, "room2")
        Check("idle event raised once on last lease", idleRaised = 1, $"raised {idleRaised}")
        Check("engine stays resident as spare", arb.IsResident(EngineCategory.Translation, "modelA"))

        ' 9. A different engine now evicts the spare and is granted.
        d = arb.RequestLoad(EngineCategory.Translation, "modelB", "Local:B", "engine-b", "room4")
        Check("spare evicted for a different engine", d.Kind = LoadDecisionKind.Granted AndAlso evictedA, $"got {d.Kind}, evicted={evictedA}")
        Check("evicted spare no longer resident", Not arb.IsResident(EngineCategory.Translation, "modelA"))

        ' 10. Categories are independent: STT slot unaffected by translation residents.
        d = arb.RequestLoad(EngineCategory.Stt, "whisper|modelX", "", "whisper-cpp", "room4")
        Check("stt category independent", d.Kind = LoadDecisionKind.Granted, $"got {d.Kind}")

        Console.WriteLine(If(_failures = 0, "ALL CHECKS PASSED", $"{_failures} CHECK(S) FAILED"))
        Return If(_failures = 0, 0, 1)
    End Function

End Module
