Imports System.Threading
Imports Amazon
Imports Amazon.Runtime
Imports Amazon.Translate
Imports Amazon.Translate.Model
Imports EveryTongue.Services.Infrastructure
Imports EveryTongue.Services.Interfaces
Imports EveryTongue.Services.Models

Namespace Services.Translation

    ''' <summary>
    ''' Amazon Translate backend (AWSSDK.Translate). Credentials come from the
    ''' standard per-engine key field using the composite format
    ''' "accessKeyId:secretAccessKey" (split on the FIRST colon — AWS secret keys
    ''' can contain further symbols but access key IDs never contain ':').
    ''' The per-engine endpoint field holds the AWS REGION name (e.g. "us-east-1"),
    ''' not a URL. The SDK client is disposed and recreated whenever the
    ''' credentials or region change.
    ''' </summary>
    Public Class AmazonTranslateBackend
        Inherits CloudTranslationBackend

        Private ReadOnly _clientLock As New Object()
        Private _client As AmazonTranslateClient
        Private _accessKeyId As String = ""
        Private _secretAccessKey As String = ""
        Private _region As String = ""
        Private _credentialsValid As Boolean = False

        Public Overrides ReadOnly Property Name As String
            Get
                Return "Amazon"
            End Get
        End Property

        ''' <summary>Available only when the composite credential parsed cleanly.</summary>
        Public Overrides ReadOnly Property IsAvailable As Boolean
            Get
                Return _credentialsValid
            End Get
        End Property

        ''' <summary>
        ''' Parse the composite "accessKeyId:secretAccessKey" credential. A
        ''' malformed value (non-empty but missing the ':' separator or either
        ''' half) marks the backend unavailable and logs ONE error.
        ''' </summary>
        Public Overrides Sub Configure(apiKey As String)
            MyBase.Configure(apiKey)
            Dim composite = If(apiKey, "").Trim()
            Dim newAccessKey = ""
            Dim newSecretKey = ""
            Dim newValid = False

            If composite.Length > 0 Then
                Dim colon = composite.IndexOf(":"c)
                If colon > 0 AndAlso colon < composite.Length - 1 Then
                    newAccessKey = composite.Substring(0, colon).Trim()
                    newSecretKey = composite.Substring(colon + 1).Trim()
                    newValid = newAccessKey.Length > 0 AndAlso newSecretKey.Length > 0
                End If
                If Not newValid Then
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        "AmazonTranslateBackend.Configure: malformed credential — expected ""accessKeyId:secretAccessKey"" (split on first ':'). Backend disabled until fixed.")
                End If
            End If

            SyncLock _clientLock
                If newAccessKey <> _accessKeyId OrElse newSecretKey <> _secretAccessKey OrElse newValid <> _credentialsValid Then
                    _accessKeyId = newAccessKey
                    _secretAccessKey = newSecretKey
                    _credentialsValid = newValid
                    DisposeClientLocked()
                End If
            End SyncLock
        End Sub

        ''' <summary>The "endpoint" for Amazon Translate is an AWS region name.</summary>
        Public Overrides Sub ConfigureEndpoint(url As String)
            MyBase.ConfigureEndpoint(url)
            Dim newRegion = If(url, "").Trim()
            SyncLock _clientLock
                If Not newRegion.Equals(_region, StringComparison.OrdinalIgnoreCase) Then
                    _region = newRegion
                    DisposeClientLocked()
                End If
            End SyncLock
        End Sub

        Private Sub DisposeClientLocked()
            If _client IsNot Nothing Then
                Try : _client.Dispose() : Catch : End Try
                _client = Nothing
            End If
        End Sub

        ''' <summary>Get (or lazily create) the SDK client for the current credentials + region.</summary>
        Private Function GetClient() As AmazonTranslateClient
            SyncLock _clientLock
                If _client Is Nothing AndAlso _credentialsValid AndAlso _region.Length > 0 Then
                    Try
                        _client = New AmazonTranslateClient(
                            New BasicAWSCredentials(_accessKeyId, _secretAccessKey),
                            RegionEndpoint.GetBySystemName(_region))
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.TRANS_ERROR,
                            $"AmazonTranslateBackend: failed to create client for region '{_region}': {ex.Message}")
                    End Try
                End If
                Return _client
            End SyncLock
        End Function

        ''' <summary>
        ''' FLORES → Amazon Translate code via the shared vendor-ISO mapping.
        ''' Amazon accepts ISO 639-1 plus regional variants (e.g. "zh-TW"), which
        ''' the google column of the language table already carries. "" when unmapped.
        ''' </summary>
        Private Shared Function ToAmazonCode(floresCode As String) As String
            Return FloresToVendorIso(floresCode)
        End Function

        Public Overrides Async Function TranslateAsync(text As String,
                                                        sourceLang As String,
                                                        targetLangs As IReadOnlyList(Of String),
                                                        ct As CancellationToken,
                                                        Optional noCache As Boolean = False,
                                                        Optional filters As TranslationFilterPaths = Nothing
        ) As Task(Of Dictionary(Of String, String))
            If Not IsAvailable Then Return New Dictionary(Of String, String)()

            Dim client = GetClient()
            If client Is Nothing Then Return New Dictionary(Of String, String)()

            Dim amazonSource = ToAmazonCode(sourceLang)
            If String.IsNullOrEmpty(amazonSource) Then
                AppLogger.Log(LogEvents.TRANS_ERROR,
                    $"AmazonTranslateBackend: no language mapping for source '{sourceLang}' — skipping request")
                Return New Dictionary(Of String, String)()
            End If

            Dim results As New Dictionary(Of String, String)()
            Dim gate As New SemaphoreSlim(4)
            Dim tasks As New List(Of Task)()

            For Each targetLang In targetLangs
                Dim tl = targetLang  ' capture for closure
                Dim amazonTarget = ToAmazonCode(tl)
                If String.IsNullOrEmpty(amazonTarget) Then
                    AppLogger.Log(LogEvents.TRANS_ERROR,
                        $"AmazonTranslateBackend: no language mapping for target '{tl}' — skipping target")
                    Continue For
                End If

                tasks.Add(Task.Run(Async Function()
                    Await gate.WaitAsync(ct)
                    Try
                        Dim request As New TranslateTextRequest With {
                            .Text = text,
                            .SourceLanguageCode = amazonSource,
                            .TargetLanguageCode = amazonTarget
                        }
                        Dim response = Await client.TranslateTextAsync(request, ct)
                        If response?.TranslatedText IsNot Nothing Then
                            SyncLock results
                                results(tl) = response.TranslatedText
                            End SyncLock
                            Interlocked.Add(CharactersUsed, text.Length)
                        End If
                    Catch ex As OperationCanceledException
                    Catch ex As Exception
                        AppLogger.Log(LogEvents.TRANS_ERROR,
                            $"AmazonTranslateBackend.TranslateAsync: target={tl} - {ex.Message}")
                    Finally
                        gate.Release()
                    End Try
                End Function))
            Next

            Await Task.WhenAll(tasks)
            Return results
        End Function

        Public Overrides Function GetSupportedLanguagesAsync(ct As CancellationToken
        ) As Task(Of IReadOnlyList(Of LanguageInfo))
            Return Task.FromResult(DirectCast(New List(Of LanguageInfo)(), IReadOnlyList(Of LanguageInfo)))
        End Function

        Public Overrides Async Function CheckHealthAsync(ct As CancellationToken) As Task(Of Boolean)
            If Not IsAvailable Then Return False
            Dim client = GetClient()
            If client Is Nothing Then Return False
            Try
                Dim response = Await client.ListLanguagesAsync(New ListLanguagesRequest(), ct)
                Return response IsNot Nothing
            Catch ex As Exception
                AppLogger.Log(LogEvents.TRANS_ERROR, $"AmazonTranslateBackend.CheckHealthAsync: {ex.Message}")
                Return False
            End Try
        End Function
    End Class

End Namespace
