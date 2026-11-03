param([switch]$Installer)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet run --project tests/Ora.Core.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'A tesztek sikertelenek.' }
    dotnet publish src/Ora/Ora.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -o artifacts/publish
    if ($LASTEXITCODE -ne 0) { throw 'A fordítás sikertelen.' }
    if ($Installer) {
        $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
        $iscc = if ($compiler) { $compiler.Source } else { "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
        if (!(Test-Path $iscc)) { throw 'A telepítő készítéséhez telepítsd az Inno Setup 6-ot, vagy add az ISCC.exe-t a PATH-hoz.' }
        & $iscc installer/Ora.iss
        if ($LASTEXITCODE -ne 0) { throw 'A telepítő készítése sikertelen.' }
    }
} finally { Pop-Location }
