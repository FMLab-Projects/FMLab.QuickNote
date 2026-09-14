; Script Inno Setup para o instalador Windows do FMLab.QuickNote.
;
; Espera o binário self-contained/single-file já publicado em dist/win-x64
; (via publish.ps1 ou pelo workflow de release). Compilar com ISCC:
;
;   ISCC installer\windows\FMLab.QuickNote.iss /DMyAppVersion="0.1.0-beta"
;
; MyAppVersion pode ser omitido para builds locais (usa um valor default).

#define MyAppName "Quick Note"
#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif
#define MyAppPublisher "FMLab"
#define MyAppURL "https://github.com/fagnerm/FMLab.QuickNote"
#define MyAppExeName "FMLab.QuickNote.App.exe"
#define MySourceDir "..\..\dist\win-x64"
#define MyOutputDir "..\..\dist-installer"

[Setup]
AppId={{9F3A4B8E-2C6A-4C2E-9B77-2E7B7A4A6C21}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir={#MyOutputDir}
OutputBaseFilename=FMLab.QuickNote-{#MyAppVersion}-win-x64-Setup
SetupIconFile=..\..\assets\icons\icon.ico
LicenseFile=..\..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes
; Não exige admin: instala em %LOCALAPPDATA%\Programs por padrão, mas
; permite elevar para Program Files via diálogo (constantes "auto*" acima).
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
