; Every Tongue - Inno Setup Script
; Installer bundles the app + runtime deps. Large tools/models are downloaded via Download Manager.

#define MyAppName "Every Tongue"
#define MyAppVersion GetEnv("APP_VERSION")
#if MyAppVersion == ""
#define MyAppVersion "0.0.0"
#endif
#define MyAppPublisher "Leapfreak"
#define MyAppExeName "EveryTongue.exe"
#define MyAppURL "https://github.com/Leapfreak/EveryTongue"

; Source directory for published app files
#define AppPublishDir "EveryTongue\bin\Publish"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=EveryTongue_Setup_{#MyAppVersion}
LicenseFile=LICENSE
SetupIconFile=EveryTongue\Resources\AppIcon.ico
WizardImageFile=EveryTongue\Assets\installer_wizard.bmp
WizardSmallImageFile=EveryTongue\Assets\installer_small.bmp
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
DisableDirPage=no
UsePreviousAppDir=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; --- License and notices ---
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "THIRD_PARTY_NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion

; --- Every Tongue application ---
Source: "{#AppPublishDir}\EveryTongue.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\EveryTongue.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\EveryTongue.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\EveryTongue.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\checksums.json"; DestDir: "{app}"; Flags: ignoreversion

; --- NuGet dependency DLLs ---
Source: "{#AppPublishDir}\Microsoft.Web.WebView2.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\Microsoft.Web.WebView2.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\Microsoft.Web.WebView2.Wpf.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\Microsoft.Data.Sqlite.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.Asio.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.Core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.Midi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.Wasapi.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.WinForms.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\NAudio.WinMM.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\QRCoder.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\SQLitePCLRaw.batteries_v2.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\SQLitePCLRaw.core.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\SQLitePCLRaw.provider.e_sqlite3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\e_sqlite3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\USFMToolsSharp.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\System.CodeDom.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\System.Management.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppPublishDir}\WebView2Loader.dll"; DestDir: "{app}"; Flags: ignoreversion

; --- Native runtimes (Windows only — skip Linux/macOS/WASM) ---
Source: "{#AppPublishDir}\runtimes\win-arm64\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-arm64\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-x64\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-x64\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-x86\native\WebView2Loader.dll"; DestDir: "{app}\runtimes\win-x86\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-arm\native\e_sqlite3.dll"; DestDir: "{app}\runtimes\win-arm\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-arm64\native\e_sqlite3.dll"; DestDir: "{app}\runtimes\win-arm64\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-x64\native\e_sqlite3.dll"; DestDir: "{app}\runtimes\win-x64\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win-x86\native\e_sqlite3.dll"; DestDir: "{app}\runtimes\win-x86\native"; Flags: ignoreversion
Source: "{#AppPublishDir}\runtimes\win\lib\net8.0\System.Management.dll"; DestDir: "{app}\runtimes\win\lib\net8.0"; Flags: ignoreversion

; --- Web client (served by Kestrel to phones) ---
Source: "{#AppPublishDir}\wwwroot\*"; DestDir: "{app}\wwwroot"; Flags: ignoreversion recursesubdirs

; --- Help files ---
Source: "{#AppPublishDir}\Help\*"; DestDir: "{app}\Help"; Flags: ignoreversion recursesubdirs

; --- Locale packs (JSON) ---
Source: "{#AppPublishDir}\locales\*"; DestDir: "{app}\locales"; Flags: ignoreversion

; --- Python servers (scripts only — Python runtime downloaded by Download Manager) ---
Source: "{#AppPublishDir}\translate-server\*"; DestDir: "{app}\translate-server"; Flags: ignoreversion
Source: "{#AppPublishDir}\live-server\*"; DestDir: "{app}\live-server"; Flags: ignoreversion recursesubdirs
Source: "{#AppPublishDir}\mms-tts-server\*"; DestDir: "{app}\mms-tts-server"; Flags: ignoreversion

; NOTE: The following are NOT bundled — they are downloaded at runtime via the Download Manager:
;   - python-embed/          (Python 3.12 embedded + pip packages)
;   - whisper-server.exe     (Vulkan build from GitHub releases)
;   - whisper-server-cuda.exe (CUDA build from GitHub releases)
;   - ggml-large-v3-turbo.bin (GGML whisper model from HuggingFace)
;   - ggml-silero-v6.2.0.bin (Silero VAD model from HuggingFace)
;   - yt-dlp.exe             (from GitHub releases)
;   - ffmpeg.exe / ffprobe.exe (from GitHub releases)
;   - SubtitleEdit/           (from GitHub releases)
;   - nllb-model/             (NLLB 1.3B from HuggingFace)
;   - nllb-3.3b-model/        (NLLB 3.3B from HuggingFace)
;   - tts-models/piper/       (Piper TTS + voice models)
;   - Bibles/                  (downloaded from eBible.org)

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function CheckDotNetRuntime: Boolean;
var
  TmpFile: String;
  ResultCode: Integer;
  Output: AnsiString;
begin
  Result := False;
  TmpFile := ExpandConstant('{tmp}\dotnet_check.txt');

  if Exec('cmd.exe', '/c dotnet --list-runtimes > "' + TmpFile + '" 2>&1', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if LoadStringFromFile(TmpFile, Output) then
    begin
      if Pos('Microsoft.WindowsDesktop.App 8.', String(Output)) > 0 then
        Result := True;
    end;
  end;

  DeleteFile(TmpFile);
end;

function InitializeSetup: Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if not CheckDotNetRuntime then
  begin
    if MsgBox(
      '.NET 8 Desktop Runtime is required but was not found.' + #13#10 + #13#10 +
      'Would you like to download it now?' + #13#10 + #13#10 +
      'Click Yes to open the download page, then run the installer.' + #13#10 +
      'Click No to continue setup anyway (the app will not run without it).',
      mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
  end;
end;
