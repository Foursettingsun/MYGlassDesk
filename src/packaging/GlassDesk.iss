[Setup]
AppId=GlassDesk.Independent
AppName=GlassDesk 修改版
AppVersion=1.5.2-mod.20261104
AppVerName=GlassDesk 1.5.2 修改版
AppPublisher=Foursettingsun (modified distribution)
AppPublisherURL=https://github.com/Foursettingsun/MYGlassDesk
AppSupportURL=https://github.com/Foursettingsun/MYGlassDesk/issues
DefaultDirName={autopf}\GlassDesk
DefaultGroupName=GlassDesk
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
OutputDir=output
OutputBaseFilename=GlassDesk-1.5.2-Modified-Setup
SetupIconFile=payload\GlassDesk.ico
UninstallDisplayIcon={app}\GlassDesk.exe
VersionInfoVersion=1.5.2.1104
VersionInfoDescription=GlassDesk 修改版安装程序
LicenseFile=payload\LICENSE.txt
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=*.exe,*.dll
RestartApplications=no
Uninstallable=not IsValidationMode
CreateUninstallRegKey=not IsValidationMode

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; Flags: unchecked; Check: not IsValidationMode

[Files]
#include "payload-files.iss"

[Icons]
Name: "{group}\GlassDesk"; Filename: "{app}\GlassDesk.exe"; Check: not IsValidationMode
Name: "{group}\卸载 GlassDesk"; Filename: "{uninstallexe}"; Check: not IsValidationMode
Name: "{autodesktop}\GlassDesk"; Filename: "{app}\GlassDesk.exe"; Tasks: desktopicon; Check: not IsValidationMode

[Run]
Filename: "{app}\GlassDesk.exe"; Description: "启动 GlassDesk"; Flags: nowait postinstall skipifsilent; Check: not IsValidationMode

[Code]
function IsValidationMode: Boolean;
begin
  Result := ExpandConstant('{param:VALIDATEPACKAGE|0}') = '1';
end;
