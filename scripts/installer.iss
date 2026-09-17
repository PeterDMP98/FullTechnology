; ============================================================
; FULLTECHNOLOGY v3 — Script de Inno Setup (FASE 13 / ERR-014)
; Instalador .exe para Windows de la versión Avalonia self-contained.
; El publish win-x64 debe generarse antes con scripts\build-publish.ps1.
; Mismo AppId que el instalador WinForms -> actualiza en sitio (no huérfano).
; La base de datos vive en %LOCALAPPDATA%\DecoTechnology y NO se toca al desinstalar.
; FULLTECHNOLOGY es la marca del software; el nombre del negocio se pide en el
; asistente y se guarda en settings.ini (businessName=), igual que en v2/v3.
; ============================================================

[Setup]
AppId={{DECO-TECH-2026-0001}
AppName=FullTechnology
AppVersion=3.0
AppPublisher=FULLTECHNOLOGY
DefaultDirName={autopf}\DecoTechnology
DefaultGroupName=FullTechnology
OutputDir=..\installer
OutputBaseFilename=FullTechnology_Setup_v3.1
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Publicación self-contained completa (Avalonia + .NET runtime + nativos SQLite/Skia).
; Ruta relativa al .iss dentro de scripts/ -> raíz del proyecto (FullTechnology/).
Source: "..\src\Frontend\FULLTECHNOLOGY.Presentation\bin\Release\net10.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Acceso directo en el Menú Inicio
Name: "{group}\FullTechnology"; Filename: "{app}\FULLTECHNOLOGY.Presentation.exe"
; Acceso directo en el Escritorio (solo si el usuario lo eligió)
Name: "{autodesktop}\FullTechnology"; Filename: "{app}\FULLTECHNOLOGY.Presentation.exe"; Tasks: desktopicon

[Run]
; Preguntar si desea ejecutar la aplicación después de instalar
Filename: "{app}\FULLTECHNOLOGY.Presentation.exe"; Description: "Ejecutar FullTechnology ahora"; Flags: nowait postinstall skipifsilent

[Code]
// Página del asistente: nombre del negocio (v3). Si se deja vacío
// se conserva el valor previo (no se toca settings.ini).
var
  BusinessPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  BusinessPage := CreateInputQueryPage(wpSelectTasks,
    'Nombre del negocio',
    '¿Cómo se llama su negocio?',
    'Este nombre aparecerá en la aplicación (menú lateral, encabezados y documentos).' + #13#10 +
    'FULLTECHNOLOGY es la marca del software.');
  BusinessPage.Add('Nombre del negocio:', False);
end;

procedure SaveBusinessName;
var
  Folder, FilePath, Name: string;
  Lines: TStringList;
  I, Idx: Integer;
begin
  Name := Trim(BusinessPage.Values[0]);
  if Name = '' then Exit;
  Folder := ExpandConstant('{localappdata}\DecoTechnology');
  ForceDirectories(Folder);
  FilePath := Folder + '\settings.ini';
  Lines := TStringList.Create;
  try
    if FileExists(FilePath) then
      Lines.LoadFromFile(FilePath);
    Idx := -1;
    for I := 0 to Lines.Count - 1 do
      if Pos('businessName=', Lines[I]) = 1 then
        Idx := I;
    if Idx >= 0 then
      Lines[Idx] := 'businessName=' + Name
    else
      Lines.Add('businessName=' + Name);
    Lines.SaveToFile(FilePath);
  finally
    Lines.Free;
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    SaveBusinessName;
end;

[UninstallDelete]
; NO eliminar la base de datos (está en %LocalAppData%\DecoTechnology)
; El usuario puede eliminarla manualmente si lo desea
Type: filesandordirs; Name: "{app}"