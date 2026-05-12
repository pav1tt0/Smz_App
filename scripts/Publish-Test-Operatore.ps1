param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [switch]$SkipZip
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "SMZ.Conta.App\SMZ.Conta.App.csproj"
$distRoot = Join-Path $repoRoot "dist"
$instructionsPath = Join-Path $repoRoot "ISTRUZIONI_TEST_OPERATORE_SMZ.md"
$backupProcedurePath = Join-Path $repoRoot "PROCEDURA_BACKUP_E_CAMBIO_PC_SMZ.md"
$packageName = "SMZ.Conta.App-$Runtime-test"
$publishDir = Join-Path $distRoot $packageName
$zipPath = Join-Path $distRoot "$packageName.zip"

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

if (Test-Path $instructionsPath) {
    Copy-Item -LiteralPath $instructionsPath -Destination (Join-Path $publishDir "LEGGIMI_TEST_OPERATORE_SMZ.md") -Force
}

if (Test-Path $backupProcedurePath) {
    Copy-Item -LiteralPath $backupProcedurePath -Destination (Join-Path $publishDir "PROCEDURA_BACKUP_E_CAMBIO_PC_SMZ.md") -Force
}

if (-not $SkipZip) {
    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
}

Write-Host "Pubblicazione completata:"
Write-Host "Cartella: $publishDir"

if (-not $SkipZip) {
    Write-Host "Archivio: $zipPath"
}
