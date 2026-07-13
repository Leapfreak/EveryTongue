Imports System.Threading

Namespace Services.Scheduling

    ''' <summary>
    ''' Snapshot of queue health metrics.
    ''' </summary>
    Public Class QueueMetrics
        Public Property TotalEnqueued As Long
        Public Property TotalCompleted As Long
        Public Property TotalErrors As Long
        Public Property TotalCancelled As Long
        Public Property CurrentDepth As Integer
        Public Property CurrentActive As Integer
        Public Property MaxConcurrency As Integer
        Public Property AvgWaitMs As Double
        Public Property MaxWaitMs As Long
    End Class

    ''' <summary>
    ''' Generic priority work queue with bounded concurrency and starvation prevention.
    ''' Items with lower priority values execute first. Items waiting longer than
    ''' <paramref name="starvationMs"/> get promoted by one priority level per interval,
    ''' preventing low-priority work from waiting indefinitely.
    ''' </summary>
    Public Class PriorityWorkQueue(Of TResult)

        Private Class WorkItem
            Public Property Work As Func(Of CancellationToken, Task(Of TResult))
            Public Property Priority As Integer
            Public Property EnqueuedTicks As Long
            Public Property ReadySignal As TaskCompletionSource(Of Boolean)
            Public Property CtRegistration As CancellationTokenRegistration
        End Class

        Private ReadOnly _pending As New List(Of WorkItem)()
        Private ReadOnly _lock As New Object()
        Private ReadOnly _maxConcurrency As Integer
        Private ReadOnly _starvationMs As Integer
        Private _activeCount As Integer = 0

        ' Metrics (accessed via Interlocked or under _lock)
        Private _totalEnqueued As Long = 0
        Private _totalCompleted As Long = 0
        Private _totalErrors As Long = 0
        Private _totalCancelled As Long = 0
        Private _totalWaitMs As Long = 0
        Private _maxWaitMs As Long = 0

        ''' <summary>
        ''' Creates a new priority work queue.
        ''' </summary>
        ''' <param name="maxConcurrency">Maximum items executing simultaneously.</param>
        ''' <param name="starvationMs">
        ''' After this many milliseconds of waiting, an item's effective priority
        ''' is promoted by one level. Prevents low-priority items from starving.
        ''' Default: 5000ms (5 seconds per level).
        ''' </param>
        Public Sub New(maxConcurrency As Integer, Optional starvationMs As Integer = 5000)
            _maxConcurrency = Math.Max(1, maxConcurrency)
            _starvationMs = Math.Max(500, starvationMs)
        End Sub

        ''' <summary>Returns a snapshot of current queue metrics.</summary>
        Public Function GetMetrics() As QueueMetrics
            SyncLock _lock
                Return New QueueMetrics With {
                    .TotalEnqueued = _totalEnqueued,
                    .TotalCompleted = _totalCompleted,
                    .TotalErrors = _totalErrors,
                    .TotalCancelled = _totalCancelled,
                    .CurrentDepth = _pending.Count,
                    .CurrentActive = _activeCount,
                    .MaxConcurrency = _maxConcurrency,
                    .AvgWaitMs = If(_totalCompleted + _totalErrors > 0,
                        _totalWaitMs / CDbl(_totalCompleted + _totalErrors), 0),
                    .MaxWaitMs = _maxWaitMs
                }
            End SyncLock
        End Function

        ''' <summary>Resets all accumulated metrics counters.</summary>
        Public Sub ResetMetrics()
            SyncLock _lock
                _totalEnqueued = 0
                _totalCompleted = 0
                _totalErrors = 0
                _totalCancelled = 0
                _totalWaitMs = 0
                _maxWaitMs = 0
            End SyncLock
        End Sub

        ''' <summary>
        ''' Enqueues work at the given priority and returns when the work completes.
        ''' If a concurrency slot is free, work starts immediately.
        ''' Otherwise, the caller awaits until a slot opens (in priority order).
        ''' </summary>
        ''' <param name="work">The async work to execute.</param>
        ''' <param name="priority">Priority level (lower = higher priority).</param>
        ''' <param name="ct">Cancellation token. Cancelling removes the item from the queue if still waiting.</param>
        Public Async Function EnqueueAsync(
            work As Func(Of CancellationToken, Task(Of TResult)),
            priority As Integer,
            ct As CancellationToken
        ) As Task(Of TResult)

            ct.ThrowIfCancellationRequested()

            Dim tcs As New TaskCompletionSource(Of Boolean)(
                TaskCreationOptions.RunContinuationsAsynchronously)
            Dim item As New WorkItem With {
                .Work = work,
                .Priority = priority,
                .EnqueuedTicks = Environment.TickCount64,
                .ReadySignal = tcs
            }

            Dim startImmediately = False
            SyncLock _lock
                _totalEnqueued += 1
                If _activeCount < _maxConcurrency Then
                    _activeCount += 1
                    startImmediately = True
                Else
                    ' Register cancellation so we can remove from queue if caller cancels
                    item.CtRegistration = ct.Register(
                        Sub() RemoveAndCancel(item))
                    _pending.Add(item)
                End If
            End SyncLock

            If Not startImmediately Then
                ' Wait for our slot — signalled by StartNext() when a slot frees up
                Await tcs.Task
            End If

            ' Clean up cancellation registration now that we're running
            item.CtRegistration.Dispose()

            ' Track how long this item waited in the queue
            Dim waitMs = Environment.TickCount64 - item.EnqueuedTicks
            Interlocked.Add(_totalWaitMs, waitMs)
            UpdateMaxWait(waitMs)

            ' Execute the work
            Try
                ct.ThrowIfCancellationRequested()
                Dim result = Await work(ct)
                Interlocked.Increment(_totalCompleted)
                Return result
            Catch ex As OperationCanceledException
                Interlocked.Increment(_totalCancelled)
                Throw
            Catch
                Interlocked.Increment(_totalErrors)
                Throw
            Finally
                StartNext()
            End Try
        End Function

        ''' <summary>Remove an item from the pending list and cancel its wait.</summary>
        Private Sub RemoveAndCancel(item As WorkItem)
            SyncLock _lock
                If _pending.Remove(item) Then
                    Interlocked.Increment(_totalCancelled)
                    item.ReadySignal.TrySetCanceled()
                End If
            End SyncLock
        End Sub

        ''' <summary>
        ''' Called when a running item finishes. Picks the next highest-priority
        ''' pending item (with starvation promotion) and signals it to start.
        ''' </summary>
        Private Sub StartNext()
            Dim nextItem As WorkItem = Nothing
            SyncLock _lock
                If _pending.Count > 0 Then
                    ' Sort by effective priority: base priority minus age-based promotion.
                    ' Each _starvationMs interval promotes by 1 level.
                    Dim now = Environment.TickCount64
                    Dim starvMs = _starvationMs
                    _pending.Sort(Function(a, b)
                                      Dim effA = a.Priority - CInt((now - a.EnqueuedTicks) \ starvMs)
                                      Dim effB = b.Priority - CInt((now - b.EnqueuedTicks) \ starvMs)
                                      Dim cmp = effA.CompareTo(effB)
                                      If cmp <> 0 Then Return cmp
                                      ' Tie-break: older items first (FIFO within same effective priority)
                                      Return a.EnqueuedTicks.CompareTo(b.EnqueuedTicks)
                                  End Function)
                    nextItem = _pending(0)
                    _pending.RemoveAt(0)
                    ' Active count stays the same: one slot freed, one starting
                Else
                    _activeCount -= 1
                End If
            End SyncLock

            ' Signal outside the lock to avoid deadlock
            nextItem?.ReadySignal.TrySetResult(True)
        End Sub

        ''' <summary>Thread-safe max update using CAS.</summary>
        Private Sub UpdateMaxWait(waitMs As Long)
            Dim currentMax = Interlocked.Read(_maxWaitMs)
            While waitMs > currentMax
                Dim oldMax = Interlocked.CompareExchange(_maxWaitMs, waitMs, currentMax)
                If oldMax = currentMax Then Exit While
                currentMax = oldMax
            End While
        End Sub
    End Class
End Namespace
