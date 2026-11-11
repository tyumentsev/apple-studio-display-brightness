#define AppName "Apple Studio Display Brightness"
#define AppVersion "1.0.0"
#define AppExe "AppleDisplayBrightness.exe"
#define AppUrl "https://github.com/tyumentsev/apple-studio-display-brightness"

[Setup]
AppId={{34D0B06E-146F-4D4C-9D5D-53A5714BF38B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=tyumentsev
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
DefaultDirName={localappdata}\Programs\AppleDisplayBrightness
DefaultGroupName=Apple Studio Display Brightness
DisableProgramGroupPage=yes
AllowNoIcons=yes
PrivilegesRequired=lowest
MinVersion=10.0
SourceDir=..
OutputDir=dist
OutputBaseFilename=AppleDisplayBrightness-{#AppVersion}-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
CloseApplications=yes
RestartApplications=no
VersionInfoVersion=1.0.0.0
VersionInfoProductName={#AppName}
VersionInfoDescription=Apple Studio Display brightness installer
LicenseFile=LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "dist\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "NOTICE.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Яркость Apple Studio Display"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\Яркость Apple Studio Display"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Открыть регулятор яркости"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release)
    and (Release >= 528040);
  if not Result then
    MsgBox('Для работы приложения требуется .NET Framework 4.8.'#13#10 +
      'Установите его с сайта Microsoft и повторите установку.', mbError, MB_OK);
end;
