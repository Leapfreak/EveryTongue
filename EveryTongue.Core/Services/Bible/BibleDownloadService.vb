Imports System.IO
Imports System.IO.Compression
Imports System.Net.Http
Imports EveryTongue.Services.Infrastructure

Namespace Services.Bible

    ''' <summary>Parsed row from the eBible.org translations.csv catalog.</summary>
    Public Class BibleCatalogEntry
        Public Property TranslationId As String
        Public Property LanguageCode As String
        Public Property LanguageName As String
        Public Property LanguageNameEnglish As String
        Public Property Title As String
        Public Property Description As String
        Public Property Copyright As String
        Public Property OTBooks As Integer
        Public Property NTBooks As Integer
    End Class

    ''' <summary>
    ''' eBible.org catalog fetch + Bible download/convert orchestration — shared by
    ''' the desktop Download Manager and the web settings page (a headless Lite
    ''' deployment has no desktop, so the browser is its only install path).
    ''' Only freely-redistributable, downloadable translations are listed;
    ''' copyrighted Bibles stay manual-copy into the Bibles folder.
    ''' </summary>
    Public Module BibleDownloadService

        Private ReadOnly _http As New HttpClient()

        Public Const CatalogUrl = "https://ebible.org/Scriptures/translations.csv"
        Public Const UsfmUrlTemplate = "https://ebible.org/Scriptures/{0}_usfm.zip"

        ''' <summary>
        ''' The catalog, via a 24-hour CSV cache in the Bibles folder (same cache
        ''' file the desktop Download Manager uses, so the two paths share it).
        ''' </summary>
        Public Async Function GetCatalogAsync(biblesDir As String,
                                              Optional forceRefresh As Boolean = False) As Task(Of List(Of BibleCatalogEntry))
            Dim cacheFile = Path.Combine(biblesDir, "translations.csv")
            Dim csvText As String = Nothing
            If Not forceRefresh Then
                Try
                    If File.Exists(cacheFile) AndAlso
                       (DateTime.Now - File.GetLastWriteTime(cacheFile)).TotalHours < 24 Then
                        csvText = File.ReadAllText(cacheFile)
                    End If
                Catch
                    ' Cache unreadable (deleted mid-check, locked) — fetch fresh below.
                End Try
            End If
            If csvText Is Nothing Then
                csvText = Await _http.GetStringAsync(CatalogUrl)
                Try
                    If Not Directory.Exists(biblesDir) Then Directory.CreateDirectory(biblesDir)
                    File.WriteAllText(cacheFile, csvText)
                Catch ex As Exception
                    ' Best-effort cache — a read-only Bibles dir must not fail the fetch.
                    AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"Catalog cache write failed: {ex.Message}")
                End Try
            End If
            Return ParseCatalogCsv(csvText)
        End Function

        ''' <summary>The cached catalog only — no network. Empty list when no cache exists.</summary>
        Public Function LoadCachedCatalog(biblesDir As String) As List(Of BibleCatalogEntry)
            Dim cacheFile = Path.Combine(biblesDir, "translations.csv")
            If Not File.Exists(cacheFile) Then Return New List(Of BibleCatalogEntry)
            Try
                Return ParseCatalogCsv(File.ReadAllText(cacheFile))
            Catch ex As Exception
                AppLogger.Log(LogEvents.DL_CHECK_RESULT, $"LoadCachedCatalog: {ex.Message}")
                Return New List(Of BibleCatalogEntry)
            End Try
        End Function

        ''' <summary>Installed translation IDs = filenames (without extension) of every Bible DB under the folder.</summary>
        Public Function GetInstalledIds(biblesDir As String) As HashSet(Of String)
            Dim installed As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If Not Directory.Exists(biblesDir) Then Return installed
            ' Same enumeration BibleService.ScanTranslations uses: pattern-scoped
            ' (no *.* full-tree walk) and case-insensitive (BCI.SQLite3 on Linux).
            Dim scanOpts As New EnumerationOptions With {
                .RecurseSubdirectories = True,
                .MatchCasing = MatchCasing.CaseInsensitive
            }
            For Each pattern In {"*.sqlite3", "*.sqlite", "*.db"}
                For Each f In Directory.GetFiles(biblesDir, pattern, scanOpts)
                    installed.Add(Path.GetFileNameWithoutExtension(f))
                Next
            Next
            Return installed
        End Function

        ''' <summary>
        ''' Download the USFM zip for one catalog entry, convert it to
        ''' &lt;biblesDir&gt;/&lt;languageCode&gt;/&lt;translationId&gt;.sqlite3 and verify it.
        ''' Reports coarse stages ("downloading" / "converting" / "verifying") for UI.
        ''' Returns the integrity-check issues (empty = clean); throws on hard failure.
        ''' </summary>
        Public Async Function DownloadAndConvertAsync(entry As BibleCatalogEntry, biblesDir As String,
                                                      Optional reportStage As Action(Of String) = Nothing) As Task(Of List(Of String))
            reportStage?.Invoke("downloading")
            Dim url = String.Format(UsfmUrlTemplate, entry.TranslationId)
            Dim zipBytes = Await _http.GetByteArrayAsync(url)

            Dim tempDir = Path.Combine(Path.GetTempPath(), "EveryTongue_USFM_" & entry.TranslationId)
            If Directory.Exists(tempDir) Then Directory.Delete(tempDir, True)
            Directory.CreateDirectory(tempDir)
            Try
                Dim zipPath = Path.Combine(tempDir, "usfm.zip")
                File.WriteAllBytes(zipPath, zipBytes)
                ZipFile.ExtractToDirectory(zipPath, tempDir)
                File.Delete(zipPath)

                reportStage?.Invoke("converting")
                Dim langDir = Path.Combine(biblesDir, entry.LanguageCode)
                If Not Directory.Exists(langDir) Then Directory.CreateDirectory(langDir)
                Dim dbPath = Path.Combine(langDir, entry.TranslationId & ".sqlite3")
                ' Microsoft.Data.Sqlite pools connections — a previous read (or a
                ' failed earlier convert) can hold the file open and block the
                ' overwrite on Windows. Flush pools before a retry writes over it.
                If File.Exists(dbPath) Then Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()
                Await Task.Run(Sub() UsfmConverter.Convert(tempDir, dbPath, entry.Title, entry.LanguageCode, entry.Copyright))

                If Not File.Exists(dbPath) Then
                    Throw New InvalidOperationException($"Conversion produced no database for {entry.TranslationId}")
                End If

                reportStage?.Invoke("verifying")
                Return Await Task.Run(Function() UsfmConverter.VerifyDatabase(dbPath))
            Finally
                Try
                    Directory.Delete(tempDir, True)
                Catch ex As Exception
                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"DownloadAndConvert (cleanup tempDir): {ex.Message}")
                End Try
            End Try
        End Function

        ' ── CSV parsing ────────────────────────────────────────────────────────

        Public Function ParseCatalogCsv(csvText As String) As List(Of BibleCatalogEntry)
            Dim entries As New List(Of BibleCatalogEntry)
            Dim lines = csvText.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If lines.Length < 2 Then Return entries

            ' Parse header to find column indices
            Dim headers = ParseCsvLine(lines(0))
            Dim colMap As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For i = 0 To headers.Length - 1
                colMap(headers(i).Trim()) = i
            Next

            For i = 1 To lines.Length - 1
                Try
                    Dim cols = ParseCsvLine(lines(i))
                    Dim redistributable = GetCol(cols, colMap, "Redistributable")
                    If Not redistributable.Equals("True", StringComparison.OrdinalIgnoreCase) Then Continue For
                    Dim downloadable = GetCol(cols, colMap, "downloadable")
                    If downloadable.Equals("False", StringComparison.OrdinalIgnoreCase) Then Continue For

                    Dim entry As New BibleCatalogEntry With {
                        .TranslationId = GetCol(cols, colMap, "translationId"),
                        .LanguageCode = GetCol(cols, colMap, "languageCode"),
                        .LanguageName = GetCol(cols, colMap, "languageName"),
                        .LanguageNameEnglish = GetCol(cols, colMap, "languageNameInEnglish"),
                        .Title = GetCol(cols, colMap, "title"),
                        .Description = GetCol(cols, colMap, "description"),
                        .Copyright = GetCol(cols, colMap, "Copyright")
                    }
                    Integer.TryParse(GetCol(cols, colMap, "OTbooks"), entry.OTBooks)
                    Integer.TryParse(GetCol(cols, colMap, "NTbooks"), entry.NTBooks)

                    If Not String.IsNullOrEmpty(entry.TranslationId) Then
                        entries.Add(entry)
                    End If
                Catch ex As Exception
                    AppLogger.Log(LogEvents.DL_DOWNLOAD_ERROR, $"ParseCatalogCsv (row {i}): {ex.Message}")
                End Try
            Next

            Return entries
        End Function

        Private Function GetCol(cols As String(), colMap As Dictionary(Of String, Integer), name As String) As String
            Dim idx As Integer
            If colMap.TryGetValue(name, idx) AndAlso idx < cols.Length Then Return cols(idx).Trim()
            Return ""
        End Function

        ''' <summary>Simple CSV line parser that handles quoted fields.</summary>
        Private Function ParseCsvLine(line As String) As String()
            Dim fields As New List(Of String)
            Dim current As New Text.StringBuilder()
            Dim inQuotes = False
            For Each ch In line
                If ch = """"c Then
                    inQuotes = Not inQuotes
                ElseIf ch = ","c AndAlso Not inQuotes Then
                    fields.Add(current.ToString())
                    current.Clear()
                Else
                    current.Append(ch)
                End If
            Next
            fields.Add(current.ToString())
            Return fields.ToArray()
        End Function

    End Module
End Namespace
