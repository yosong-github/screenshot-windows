; Per-user installer for screenshot-windows.
; AppId stays fixed across upgrades. Version is passed by installer/build.ps1 from Directory.Build.props.
; The Run value matches AutostartCommand: name screenshot-windows, command "<exe>" --autostart.

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyAppFileVersion
  #define MyAppFileVersion "1.0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\publish"
#endif

#define MyAppName "screenshot-windows"
#define MyAppExeName "screenshot-windows.exe"

[Setup]
AppId={{6C4E9A21-8B37-4F0D-A5E2-1D7C83B64F90}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppName}
VersionInfoVersion={#MyAppFileVersion}
VersionInfoCompany={#MyAppName}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppFileVersion}
VersionInfoDescription=Screenshots hang from a line along the top of the screen.
DefaultDirName={localappdata}\Programs\screenshot-windows
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputBaseFilename=screenshot-windows-setup-{#MyAppVersion}
OutputDir=..\artifacts
SetupIconFile=..\assets\screenshot-windows.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
CloseApplications=yes
RestartApplications=no
AppMutex=Local\screenshot-windows
UsePreviousTasks=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[CustomMessages]
english.AutostartGroup=Startup
chinesesimplified.AutostartGroup=启动
english.Autostart=Launch screenshot-windows at login
chinesesimplified.Autostart=开机自启

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "{cm:Autostart}"; GroupDescription: "{cm:AutostartGroup}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Keep this command in sync with AutostartCommand.Format.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "screenshot-windows"; ValueData: """{app}\{#MyAppExeName}"" --autostart"; Tasks: autostart; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "screenshot-windows"; Tasks: not autostart; Flags: deletevalue uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'screenshot-windows');
    Exit;
  end;

  if CurUninstallStep <> usPostUninstall then
    Exit;
  { Pictures\screenshot-windows is never deleted. A silent uninstall keeps local data too. }
  if UninstallSilent then
    Exit;

  DataDir := ExpandConstant('{localappdata}\screenshot-windows');
  if not DirExists(DataDir) then
    Exit;

  if MsgBox('Delete settings in %LOCALAPPDATA%\screenshot-windows?' + #13#10 +
            '删除 %LOCALAPPDATA%\screenshot-windows 里的设置？' + #13#10 + #13#10 +
            'Pictures\screenshot-windows is kept either way.' + #13#10 +
            '无论选哪个，Pictures\screenshot-windows 都会保留。',
            mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
    DelTree(DataDir, True, True, True);
end;
