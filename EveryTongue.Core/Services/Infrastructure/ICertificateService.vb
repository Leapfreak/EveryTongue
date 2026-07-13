Imports System.Security.Cryptography.X509Certificates

Namespace Services.Infrastructure
    ''' <summary>
    ''' Manages self-signed certificate lifecycle for HTTPS.
    ''' </summary>
    Public Interface ICertificateService
        Inherits IDisposable

        ReadOnly Property Certificate As X509Certificate2
        ReadOnly Property CertificatePath As String

        Function GetOrCreateCertificate() As X509Certificate2
        Function GetCertificateBytes() As Byte()
    End Interface
End Namespace
