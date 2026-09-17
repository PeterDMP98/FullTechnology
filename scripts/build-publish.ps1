# build-publish.ps1 — Publica FULLTECHNOLOGY.Presentation self-contained para una plataforma (FASE 13).
#
# Uso:
#   .\scripts\build-publish.ps1               -> win-x64 (Release)
#   .\scripts\build-publish.ps1 -Target linux-x64
#   .\scripts\build-publish.ps1 -Target osx-x64
#   .\scripts\build-publish.ps1 -Target win-x64 -NoRestore
#
# Salida: src\Frontend\FULLTECHNOLOGY.Presentation\bin\Release\net10.0\<Target>\publish\
#   - win-x64 : FULLTECHNOLOGY.Presentation.exe (preparado para el instalador Inno Setup)
#   - linux-x64 / osx-x64 : binario apphost sin extensión (ELF / Mach-O)
#   - Los nativos multiplataforma se incluyen solos por RID:
#     SQLite (libe_sqlite3), SkiaSharp (libSkiaSharp), HarfBuzz, QuestPDF (libqpdf/libQuestPdfSkia).
#
# Nota: en Windows PowerShell 5.1 el atributo [ValidateSet] combinado con params
# extra provoca MetadataError, así que la validación se hace manualmente abajo.

param(
    [string]$Target = 'win-x64'
)

$ErrorActionPreference = 'Stop'

if ($Target -notin @('win-x64', 'linux-x64', 'osx-x64')) {
    throw "plataforma no soportada: $Target (usa win-x64, linux-x64 u osx-x64)."
}

$root = Split-Path -Parent $PSScriptRoot
$csproj = Join-Path $root 'src\Frontend\FULLTECHNOLOGY.Presentation\FULLTECHNOLOGY.Presentation.csproj'
if (-not (Test-Path $csproj)) { throw "No se encontró el proyecto: $csproj" }

Write-Host "[publish] self-contained $Target (Release)..." -ForegroundColor Cyan
$argsList = @('publish', $csproj, '-c', 'Release', '-r', $Target, '--self-contained', 'true', '--nologo')

& dotnet @argsList
if ($LASTEXITCODE -ne 0) { throw "dotnet publish $Target falló con código $LASTEXITCODE." }

$pub = Join-Path $root "src\Frontend\FULLTECHNOLOGY.Presentation\bin\Release\net10.0\$Target\publish"
$size = [math]::Round((Get-ChildItem $pub -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 0)
$files = (Get-ChildItem $pub -File | Measure-Object).Count
Write-Host "[publish] OK: $pub ($files archivos, $size MB)" -ForegroundColor Green