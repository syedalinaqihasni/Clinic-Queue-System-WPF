#define MyAppName "Clinic Queue System"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Clinic Queue System"
#define MyAppExeName "ClinicQueueSystem.exe"
[Setup]
AppId={{B2E4D8C2-5F11-4D75-9D71-4F8E0B4B1001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ClinicQueueSystem
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=ClinicQueueSystem-Setup-1.0.1
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
[Files]
Source: "..\bin\Release\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
[Icons]
Name: "{group}\Clinic Queue System"; Filename: "{app}\ClinicQueueSystem.exe"
Name: "{commondesktop}\Clinic Queue System"; Filename: "{app}\ClinicQueueSystem.exe"
