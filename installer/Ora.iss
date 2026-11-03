#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif
[Setup]
AppId={{7D44B508-CBB1-48B6-BE3A-FF83E6C16295}
AppName=Óra
AppVersion=1.0.0
AppMutex=Local\Ora.SingleUser
AppPublisher=Óra
DefaultDirName={localappdata}\Programs\Ora
DefaultGroupName=Óra
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=Ora-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
UninstallDisplayIcon={app}\Ora.exe
SetupIconFile=..\src\Ora\Assets\ora.ico

[Languages]
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"

[Tasks]
Name: "autostart"; Description: "Indítás a Windows bejelentkezésekor"; Flags: checkedonce
Name: "desktopicon"; Description: "Asztali parancsikon létrehozása"; Flags: unchecked

[Files]
Source: "{#PublishDir}\Ora.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\Óra\Óra"; Filename: "{app}\Ora.exe"
Name: "{userprograms}\Óra\Eltávolítás"; Filename: "{uninstallexe}"
Name: "{userdesktop}\Óra"; Filename: "{app}\Ora.exe"; Tasks: desktopicon
Name: "{userstartup}\Óra"; Filename: "{app}\Ora.exe"; Tasks: autostart

[Run]
Filename: "{app}\Ora.exe"; Description: "Óra indítása"; Flags: nowait postinstall skipifsilent
