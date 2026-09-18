# build-installer.ps1 — FASE 13 / ERR-014: genera el instalador Windows de la versión Avalonia.
# 1) Publica la app win-x64 self-contained (Release).
# 2) Compila scripts\installer.iss con Inno Setup.
# Salida: installer\FullTechnology_Setup_v3.5.exe
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host " FULLTECHNOLOGY v3.5 (Avalonia) - Instalador" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# --- Paso 1: Verificar que dotnet esté disponible ---
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        winget install --id Microsoft.DotNet.SDK.10 --exact --source winget --accept-source-agreements --accept-package-agreements
    } else {
        throw "Instala .NET 10 SDK desde Microsoft y vuelve a ejecutar este archivo."
    }
}

# --- Paso 2: Verificar que Inno Setup esté disponible ---
$ISCC = "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $ISCC)) {
    $ISCC = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (-not (Test-Path $ISCC)) {
        $ISCC = "C:\Program Files\Inno Setup 6\ISCC.exe"
    }
}
if (-not (Test-Path $ISCC)) {
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        winget install --id JRSoftware.InnoSetup --source winget --accept-source-agreements --accept-package-agreements
        $ISCC = "C:\Users\$env:USERNAME\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
    } else {
        throw "Instala Inno Setup 6 desde https://jrsoftware.org/isinfo.php y vuelve a ejecutar."
    }
}

# --- Paso 3: Publicar la app Avalonia win-x64 (self-contained, Release) ---
Write-Host "[1/2] Publicando la aplicación Avalonia (win-x64 self-contained)..." -ForegroundColor Yellow
& (Join-Path $PSScriptRoot 'build-publish.ps1') -Target win-x64
if ($LASTEXITCODE -ne 0) { throw "Error al publicar la aplicación." }

# --- Paso 4: Generar el instalador ---
Write-Host ""
Write-Host "[2/2] Generando el instalador con Inno Setup..." -ForegroundColor Yellow
& "$ISCC" (Join-Path $PSScriptRoot 'installer.iss')
if ($LASTEXITCODE -ne 0) { throw "Error al generar el instalador." }

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host " LISTO - Instalador generado correctamente" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Ubicación: installer\FullTechnology_Setup_v3.5.exe" -ForegroundColor Green
Write-Host ""
Write-Host "Para distribuir: envía el archivo FullTechnology_Setup_v3.5.exe" -ForegroundColor Cyan
Write-Host "El usuario final solo debe ejecutarlo y seguir los pasos." -ForegroundColor Cyan