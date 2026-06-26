; ===========================================================================
;  Noor — Windows installer (Inno Setup)
;
;  Build:
;    1. dotnet publish Noor/Noor.csproj -c Release -f net10.0-windows10.0.19041.0 -r win-x64 --self-contained -o publish/win
;    2. iscc packaging\windows\noor.iss
;
;  Invoke from the repository root.
; ===========================================================================
#define NoorAppName      "Noor"
#define NoorAppExeName   "Noor.exe"
#define NoorVersion      GetEnv("NOOR_VERSION")
#if NoorVersion == ""
  #define NoorVersion    "1.0.0"
#endif
#define NoorPublisher    "AzimMahmud"
#define NoorURL          "https://github.com/AzimMahmud/Noor"
#define NoorExe          "Noor.exe"

[Setup]
AppId={{8F3C2A1E-7B6D-4E9F-A2C5-1B7E9D3F4A60}
AppName={#NoorAppName}
AppVersion={#NoorVersion}
AppVerName={#NoorAppName} {#NoorVersion}
AppPublisher={#NoorPublisher}
AppPublisherURL={#NoorURL}
AppSupportURL={#NoorURL}/issues
AppUpdatesURL={#NoorURL}/releases
AppContact={#NoorURL}
AppComments=Islamic prayer time companion
AppCopyright=Copyright (c) 2024 {#NoorPublisher}
VersionInfoVersion={#NoorVersion}
VersionInfoProductVersion={#NoorVersion}

; Default install location (per-machine)
DefaultDirName={autopf}\{#NoorAppName}
DefaultGroupName={#NoorAppName}
DisableProgramGroupPage=yes
DisableDirPage=no

; Output
OutputBaseFilename=NoorSetup-{#NoorVersion}-win-x64
OutputDir=..\..\dist
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Look & feel
SetupIconFile=..\icons\noor.ico
UninstallDisplayIcon={app}\{#NoorExe}
UninstallDisplayName={#NoorAppName}
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog

; Misc
CloseApplications=force
RestartApplications=no
LicenseFile=..\..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start Noor when Windows starts"; GroupDescription: "Other:"

[Files]
; The published self-contained output (built before running iscc).
Source: "..\..\publish\win\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#NoorAppName}"; Filename: "{app}\{#NoorExe}"; IconFilename: "{app}\{#NoorExe}"
Name: "{group}\Uninstall {#NoorAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#NoorAppName}"; Filename: "{app}\{#NoorExe}"; Tasks: desktopicon; IconFilename: "{app}\{#NoorExe}"
Name: "{autostartup}\{#NoorAppName}"; Filename: "{app}\{#NoorExe}"; Tasks: startup; IconFilename: "{app}\{#NoorExe}"

[Run]
Filename: "{app}\{#NoorExe}"; Description: "{cm:LaunchProgram,{#NoorAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\Noor"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
