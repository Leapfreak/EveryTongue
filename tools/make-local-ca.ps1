# Mint a local CA + CA-signed server certificate for LAN HTTPS.
# The CA .cer gets installed ONCE on each broadcast phone; the .pfx replaces
# the app's self-signed certificate. Result: trusted green-padlock HTTPS on
# the LAN, mic prompts work, fully offline.
#   powershell -ExecutionPolicy Bypass -File tools\make-local-ca.ps1 -Ip 10.0.0.73 -OutDir C:\tmp\et-verify
param(
    [string]$Ip = "10.0.0.73",
    [string]$OutDir = "C:\tmp\et-verify"
)
$ErrorActionPreference = "Stop"

$ca = New-SelfSignedCertificate -Type Custom -KeyUsage CertSign, CRLSign, DigitalSignature `
    -KeyLength 2048 -Subject "CN=EveryTongue Local CA" `
    -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5) `
    -TextExtension @("2.5.29.19={critical}{text}CA=true")
Write-Host "CA created: $($ca.Thumbprint)"

$leaf = New-SelfSignedCertificate -Type Custom -Subject "CN=EveryTongue Server" `
    -KeyLength 2048 -CertStoreLocation "Cert:\CurrentUser\My" -Signer $ca `
    -NotAfter (Get-Date).AddYears(3) `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1", "2.5.29.17={text}DNS=localhost&IPAddress=$Ip&IPAddress=127.0.0.1")
Write-Host "Leaf created: $($leaf.Thumbprint)"

$pw = ConvertTo-SecureString "transcription-tools-cert" -AsPlainText -Force
Export-PfxCertificate -Cert $leaf -FilePath (Join-Path $OutDir "config\subtitle-server.pfx") -Password $pw | Out-Null
Export-Certificate -Cert $ca -FilePath (Join-Path $OutDir "everytongue-ca.cer") | Out-Null

# Keys served their purpose; don't leave them in the user's cert store
Remove-Item $ca.PSPath
Remove-Item $leaf.PSPath

Write-Host "pfx -> $OutDir\config\subtitle-server.pfx"
Write-Host "CA  -> $OutDir\everytongue-ca.cer (install this on the phone)"
