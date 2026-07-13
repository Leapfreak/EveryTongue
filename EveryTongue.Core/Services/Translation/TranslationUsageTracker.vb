Imports System.IO
Imports System.Text.Json
Imports System.Threading
Imports EveryTongue.Services.Infrastructure

Namespace Services.Translation
    ''' <summary>
    ''' Tracks characters submitted to CLOUD translation backends, keyed by
    ''' backend key (e.g. "deepl") + calendar month ("2026-06") — characters are
    ''' what vendors bill. Also keeps in-memory rolling latency stats per backend.
    '''
    ''' Persistence: %AppData%\EveryTongue\translation-usage.json with debounced
    ''' writes (dirty flag + 30s timer flush + explicit Flush() on shutdown) so
    ''' the translate path never blocks on disk I/O.
    '''
    ''' Budgets: when a month's count crosses the configured budget for a backend,
    ''' ONE warning per backend per month is logged (TRANS_BUDGET_EXCEEDED).
    ''' Translation is NEVER blocked — mid-service cutoff is unacceptable.
    ''' </summary>
    Public NotInheritable Class TranslationUsageTracker

        Private Sub New()
        End Sub

        Private Class LatencyStats
            Public Property Count As Long
            Public Property TotalMs As Long
        End Class

        Private Shared ReadOnly _lock As New Object()
        ' backend key → (month "yyyy-MM" → chars submitted)
        Private Shared _usage As New Dictionary(Of String, Dictionary(Of String, Long))(StringComparer.OrdinalIgnoreCase)
        ' "backendKey|yyyy-MM" entries that already produced a budget warning
        Private Shared _warned As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        ' backend key → in-memory latency stats (not persisted)
        Private Shared ReadOnly _latency As New Dictionary(Of String, LatencyStats)(StringComparer.OrdinalIgnoreCase)

        Private Shared _loaded As Boolean = False
        Private Shared _dirty As Boolean = False
        Private Shared _flushTimer As Timer

        ''' <summary>
        ''' Resolves the monthly character budget for a backend key (0/absent = no
        ''' budget). Wired to AppConfig.GetTranslationCharBudget by ServerController
        ''' so budget changes in Options apply live.
        ''' </summary>
        Public Shared Property BudgetProvider As Func(Of String, Long)

        Private Shared ReadOnly Property StorePath As String
            Get
                Return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EveryTongue", "translation-usage.json")
            End Get
        End Property

        Private Shared Function CurrentMonth() As String
            Return DateTime.Now.ToString("yyyy-MM", Globalization.CultureInfo.InvariantCulture)
        End Function

        ''' <summary>
        ''' Record characters submitted to a cloud backend (source length × target
        ''' count — one vendor-billed request per target language). Cheap and
        ''' non-blocking: disk writes happen on the flush timer, never here.
        ''' </summary>
        Public Shared Sub RecordUsage(backendKey As String, chars As Long)
            If String.IsNullOrEmpty(backendKey) OrElse chars <= 0 Then Return
            Dim warnTotal As Long = -1
            Dim warnBudget As Long = 0
            Dim month = CurrentMonth()

            SyncLock _lock
                EnsureLoaded()
                Dim months As Dictionary(Of String, Long) = Nothing
                If Not _usage.TryGetValue(backendKey, months) Then
                    months = New Dictionary(Of String, Long)(StringComparer.Ordinal)
                    _usage(backendKey) = months
                End If
                Dim total As Long = 0
                months.TryGetValue(month, total)
                total += chars
                months(month) = total
                _dirty = True
                EnsureTimer()

                ' Budget check — warn once per backend per month, never block.
                Dim budget = GetBudget(backendKey)
                If budget > 0 AndAlso total >= budget Then
                    Dim warnKey = backendKey & "|" & month
                    If _warned.Add(warnKey) Then
                        warnTotal = total
                        warnBudget = budget
                    End If
                End If
            End SyncLock

            If warnTotal >= 0 Then
                AppLogger.Log(LogEvents.TRANS_BUDGET_EXCEEDED,
                    $"backend={backendKey} month={month}: {warnTotal:N0} characters submitted, budget {warnBudget:N0} exceeded — translation continues (warning only)")
            End If
        End Sub

        ''' <summary>Record the latency of one cloud translation HTTP call (in-memory only).</summary>
        Public Shared Sub RecordLatency(backendKey As String, elapsedMs As Long)
            If String.IsNullOrEmpty(backendKey) OrElse elapsedMs < 0 Then Return
            SyncLock _lock
                Dim stats As LatencyStats = Nothing
                If Not _latency.TryGetValue(backendKey, stats) Then
                    stats = New LatencyStats()
                    _latency(backendKey) = stats
                End If
                stats.Count += 1
                stats.TotalMs += elapsedMs
            End SyncLock
        End Sub

        ''' <summary>Characters submitted to a backend in the current calendar month.</summary>
        Public Shared Function GetMonthUsage(backendKey As String) As Long
            If String.IsNullOrEmpty(backendKey) Then Return 0
            SyncLock _lock
                EnsureLoaded()
                Dim months As Dictionary(Of String, Long) = Nothing
                Dim total As Long = 0
                If _usage.TryGetValue(backendKey, months) Then
                    months.TryGetValue(CurrentMonth(), total)
                End If
                Return total
            End SyncLock
        End Function

        ''' <summary>Average cloud-call latency for a backend this session (0 = no calls yet).</summary>
        Public Shared Function GetAverageLatencyMs(backendKey As String) As Long
            If String.IsNullOrEmpty(backendKey) Then Return 0
            SyncLock _lock
                Dim stats As LatencyStats = Nothing
                If _latency.TryGetValue(backendKey, stats) AndAlso stats.Count > 0 Then
                    Return CLng(stats.TotalMs / stats.Count)
                End If
                Return 0
            End SyncLock
        End Function

        ''' <summary>Write pending usage to disk if dirty. Called by the 30s timer and on shutdown.</summary>
        Public Shared Sub Flush()
            Dim json As String = Nothing
            SyncLock _lock
                If Not _dirty OrElse Not _loaded Then Return
                _dirty = False
                Try
                    Dim doc As New Dictionary(Of String, Object) From {
                        {"usage", _usage},
                        {"warned", _warned.ToList()}
                    }
                    json = JsonSerializer.Serialize(doc, New JsonSerializerOptions With {.WriteIndented = True})
                Catch ex As Exception
                    AppLogger.Log(LogEvents.TRANS_ERROR, $"TranslationUsageTracker serialize failed: {ex.Message}")
                    Return
                End Try
            End SyncLock

            ' Disk I/O outside the lock — the translate path never waits on it.
            Try
                Directory.CreateDirectory(Path.GetDirectoryName(StorePath))
                File.WriteAllText(StorePath, json)
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"TranslationUsageTracker flush failed: {ex.Message}")
            End Try
        End Sub

        Private Shared Function GetBudget(backendKey As String) As Long
            Try
                Dim provider = BudgetProvider
                If provider IsNot Nothing Then Return provider(backendKey)
            Catch
            End Try
            Return 0
        End Function

        Private Shared Sub EnsureTimer()
            ' Inside _lock. Flush at most every 30s while dirty.
            If _flushTimer Is Nothing Then
                _flushTimer = New Timer(Sub() Flush(), Nothing,
                                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30))
            End If
        End Sub

        Private Shared Sub EnsureLoaded()
            ' Inside _lock.
            If _loaded Then Return
            _loaded = True
            Try
                If Not File.Exists(StorePath) Then Return
                Using doc = JsonDocument.Parse(File.ReadAllText(StorePath))
                    Dim usageEl As JsonElement
                    If doc.RootElement.TryGetProperty("usage", usageEl) AndAlso
                       usageEl.ValueKind = JsonValueKind.Object Then
                        For Each backendProp In usageEl.EnumerateObject()
                            If backendProp.Value.ValueKind <> JsonValueKind.Object Then Continue For
                            Dim months As New Dictionary(Of String, Long)(StringComparer.Ordinal)
                            For Each monthProp In backendProp.Value.EnumerateObject()
                                Dim chars As Long
                                If monthProp.Value.TryGetInt64(chars) Then months(monthProp.Name) = chars
                            Next
                            _usage(backendProp.Name) = months
                        Next
                    End If
                    Dim warnedEl As JsonElement
                    If doc.RootElement.TryGetProperty("warned", warnedEl) AndAlso
                       warnedEl.ValueKind = JsonValueKind.Array Then
                        For Each item In warnedEl.EnumerateArray()
                            If item.ValueKind = JsonValueKind.String Then _warned.Add(item.GetString())
                        Next
                    End If
                End Using
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"TranslationUsageTracker load failed: {ex.Message}")
            End Try
        End Sub
    End Class
End Namespace
