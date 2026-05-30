Imports System.IO
Imports System.Linq
Imports System.Security.Cryptography
Imports System.Text.Json

Namespace Services.Infrastructure

    ''' <summary>
    ''' Verifies app file integrity against a checksums.json manifest.
    ''' Only checks files we build and ship — not third-party models.
    ''' </summary>
    Friend Class IntegrityChecker

        Public Enum FileStatus
            Pass
            Fail
            Missing
            NoManifest
        End Enum

        Public Class FileResult
            Public Property RelativePath As String
            Public Property Status As FileStatus
            Public Property ExpectedHash As String
            Public Property ActualHash As String
            Public Property ExpectedSize As Long
            Public Property ActualSize As Long
        End Class

        Public Class CheckResult
            Public Property ManifestFound As Boolean
            Public Property ManifestVersion As String
            Public Property ManifestGenerated As String
            Public Property Files As New List(Of FileResult)

            Public ReadOnly Property PassCount As Integer
                Get
                    Return Files.Where(Function(f) f.Status = FileStatus.Pass).Count()
                End Get
            End Property

            Public ReadOnly Property FailCount As Integer
                Get
                    Return Files.Where(Function(f) f.Status = FileStatus.Fail).Count()
                End Get
            End Property

            Public ReadOnly Property MissingCount As Integer
                Get
                    Return Files.Where(Function(f) f.Status = FileStatus.Missing).Count()
                End Get
            End Property

            Public ReadOnly Property AllPassed As Boolean
                Get
                    Return ManifestFound AndAlso FailCount = 0 AndAlso MissingCount = 0
                End Get
            End Property
        End Class

        Private Shared ReadOnly AppDir As String = AppDomain.CurrentDomain.BaseDirectory

        Public Shared Function Check() As CheckResult
            Dim result As New CheckResult()
            Dim manifestPath = Path.Combine(AppDir, "checksums.json")

            If Not File.Exists(manifestPath) Then
                result.ManifestFound = False
                Return result
            End If

            result.ManifestFound = True

            Dim json As JsonDocument
            Try
                json = JsonDocument.Parse(File.ReadAllText(manifestPath))
            Catch ex As Exception
                result.ManifestFound = False
                Return result
            End Try

            Dim root = json.RootElement

            If root.TryGetProperty("version", Nothing) Then
                result.ManifestVersion = root.GetProperty("version").GetString()
            End If
            If root.TryGetProperty("generated", Nothing) Then
                result.ManifestGenerated = root.GetProperty("generated").GetString()
            End If

            If root.TryGetProperty("files", Nothing) Then
                For Each prop In root.GetProperty("files").EnumerateObject()
                    result.Files.Add(VerifyFile(prop.Name, prop.Value))
                Next
            End If

            json.Dispose()
            Return result
        End Function

        Private Shared Function VerifyFile(relativePath As String, entry As JsonElement) As FileResult
            Dim fr As New FileResult() With {
                .RelativePath = relativePath
            }

            If entry.TryGetProperty("sha256", Nothing) Then
                fr.ExpectedHash = entry.GetProperty("sha256").GetString()
            End If
            If entry.TryGetProperty("size", Nothing) Then
                fr.ExpectedSize = entry.GetProperty("size").GetInt64()
            End If

            Dim fullPath = Path.Combine(AppDir, relativePath.Replace("/", Path.DirectorySeparatorChar))

            If Not File.Exists(fullPath) Then
                fr.Status = FileStatus.Missing
                Return fr
            End If

            Dim info As New FileInfo(fullPath)
            fr.ActualSize = info.Length

            Using stream = File.OpenRead(fullPath)
                Using sha = SHA256.Create()
                    Dim hashBytes = sha.ComputeHash(stream)
                    fr.ActualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower()
                End Using
            End Using

            If String.Equals(fr.ExpectedHash, fr.ActualHash, StringComparison.OrdinalIgnoreCase) Then
                fr.Status = FileStatus.Pass
            Else
                fr.Status = FileStatus.Fail
            End If

            Return fr
        End Function

        Public Shared Function ToReportLines(result As CheckResult) As List(Of String)
            Dim lines As New List(Of String)

            If Not result.ManifestFound Then
                lines.Add("NO MANIFEST — checksums.json not found, cannot verify files")
                Return lines
            End If

            lines.Add($"Manifest version: {result.ManifestVersion}")
            lines.Add($"Generated: {result.ManifestGenerated}")
            lines.Add($"Results: {result.PassCount} pass, {result.FailCount} fail, {result.MissingCount} missing")
            lines.Add("")

            For Each f In result.Files
                Dim icon = If(f.Status = FileStatus.Pass, "PASS",
                           If(f.Status = FileStatus.Fail, "FAIL",
                           If(f.Status = FileStatus.Missing, "MISSING", "?")))
                Dim line = $"  [{icon}] {f.RelativePath}"
                If f.Status = FileStatus.Fail Then
                    line &= $" (expected: {f.ExpectedHash?.Substring(0, 12)}..., actual: {f.ActualHash?.Substring(0, 12)}...)"
                End If
                lines.Add(line)
            Next

            Return lines
        End Function

    End Class

End Namespace
