Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports Microsoft.Extensions.Logging

Namespace Services.Infrastructure
    ''' <summary>
    ''' Manages self-signed certificate generation and loading for HTTPS.
    ''' </summary>
    Public Class CertificateService
        Implements ICertificateService

        Private Const CertPassword As String = "transcription-tools-cert"
        Private Const CertFileName As String = "subtitle-server.pfx"

        Private ReadOnly _logger As ILogger(Of CertificateService)
        Private _certificate As X509Certificate2

        Public Sub New(logger As ILogger(Of CertificateService))
            _logger = logger
        End Sub

        Public ReadOnly Property Certificate As X509Certificate2 Implements ICertificateService.Certificate
            Get
                Return _certificate
            End Get
        End Property

        Public ReadOnly Property CertificatePath As String Implements ICertificateService.CertificatePath
            Get
                Return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EveryTongue",
                    CertFileName)
            End Get
        End Property

        Public Function GetOrCreateCertificate() As X509Certificate2 Implements ICertificateService.GetOrCreateCertificate
            Dim certPath = Me.CertificatePath

            ' Try to load existing cert
            If File.Exists(certPath) Then
                Try
                    Dim cert As New X509Certificate2(certPath, CertPassword, X509KeyStorageFlags.Exportable)
                    If cert.NotAfter > DateTime.Now.AddDays(30) Then
                        _certificate = cert
                        _logger.LogInformation("Loaded existing certificate (expires {Expiry:d})", cert.NotAfter)
                        Return cert
                    End If
                    _logger.LogInformation("Certificate expiring soon, generating new one")
                    cert.Dispose()
                Catch ex As Exception
                    _logger.LogWarning(ex, "Failed to load existing certificate, generating new one")
                End Try
            End If

            ' Generate a new self-signed certificate
            Dim certDir = Path.GetDirectoryName(certPath)
            Directory.CreateDirectory(certDir)

            Using rsa As RSA = RSA.Create(2048)
                Dim req As New CertificateRequest(
                    "CN=Every Tongue Subtitle Server",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1)

                req.CertificateExtensions.Add(
                    New X509BasicConstraintsExtension(True, False, 0, True))
                req.CertificateExtensions.Add(
                    New X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature Or X509KeyUsageFlags.KeyEncipherment, False))
                req.CertificateExtensions.Add(
                    New X509EnhancedKeyUsageExtension(
                        New OidCollection From {New Oid("1.3.6.1.5.5.7.3.1")}, False))

                ' Add SAN with localhost + all local IPs
                Dim san As New SubjectAlternativeNameBuilder()
                san.AddDnsName("localhost")
                Try
                    For Each addr In Dns.GetHostAddresses(Dns.GetHostName())
                        If addr.AddressFamily = AddressFamily.InterNetwork Then
                            san.AddIpAddress(addr)
                        End If
                    Next
                Catch ex As Exception
                    _logger.LogWarning(ex, "Could not enumerate local IPs for certificate SAN")
                End Try
                req.CertificateExtensions.Add(san.Build())

                Dim cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10))
                Dim pfxBytes = cert.Export(X509ContentType.Pfx, CertPassword)
                File.WriteAllBytes(certPath, pfxBytes)

                _certificate = New X509Certificate2(pfxBytes, CertPassword, X509KeyStorageFlags.Exportable)
                _logger.LogInformation("Generated new self-signed certificate (expires {Expiry:d})", _certificate.NotAfter)
                Return _certificate
            End Using
        End Function

        Public Function GetCertificateBytes() As Byte() Implements ICertificateService.GetCertificateBytes
            If _certificate Is Nothing Then Return Nothing
            Return _certificate.Export(X509ContentType.Cert)
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            _certificate?.Dispose()
            _certificate = Nothing
        End Sub
    End Class
End Namespace
