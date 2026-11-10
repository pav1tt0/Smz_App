param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]+$')]
    [string]$PackageName = ('SMZ-Installazione-Dati-Reali-win-x64-' + (Get-Date -Format 'yyyy-MM-dd'))
)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$distRoot = Join-Path $repoRoot 'dist'
$destination = Join-Path $distRoot $PackageName
$zip = Join-Path $distRoot ($PackageName + '.zip')
if ((Test-Path -LiteralPath $destination) -or (Test-Path -LiteralPath $zip)) { throw 'Pacchetto gia esistente: scegliere un altro nome.' }
$stage = Join-Path $repoRoot ('.codex-build\operativo-' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $stage $PackageName
$application = Join-Path $package 'App'
New-Item -ItemType Directory -Path $application -Force | Out-Null
& dotnet publish (Join-Path $repoRoot 'SMZ.Conta.App\SMZ.Conta.App.csproj') -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false `
    -p:SmzDataProfile=Conta -p:SmzRequireInstalledDatabase=true -o $application --ignore-failed-sources
if ($LASTEXITCODE -ne 0) { throw 'Pubblicazione non riuscita.' }
$templates = Join-Path $PSScriptRoot 'operational-package'
foreach ($name in @('Installa-SMZ.ps1', 'INSTALLA-E-AVVIA-SMZ.cmd', 'LEGGIMI-PRIMA.txt')) {
    Copy-Item -LiteralPath (Join-Path $templates $name) -Destination (Join-Path $package $name)
}
foreach ($name in @('Avvia-SMZ.ps1', 'AVVIA-SMZ.cmd')) {
    Copy-Item -LiteralPath (Join-Path $templates $name) -Destination (Join-Path $application $name)
}
Copy-Item -LiteralPath (Join-Path $repoRoot 'MANUALE_UTENTE_SMZ.md') -Destination $package
if (@(Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Extension -in @('.db', '.sqlite', '.sqlite3', '.smzbak') }).Count -ne 0) {
    throw 'Un database e stato incluso nel pacchetto operativo.'
}
$files = @(Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName | ForEach-Object {
    @{ Path = $_.FullName.Substring($package.Length + 1); SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
@{
    Package = $PackageName; PackageKind = 'Operational'; Configuration = 'Release'; Runtime = 'win-x64'
    SelfContained = $true; DatabaseIncluded = $false; RequiresSelectedDatabase = $true
    CreatedAt = (Get-Date).ToString('o'); Files = $files
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $package 'manifest.json') -Encoding UTF8
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
foreach ($target in @($package, $destination)) {
    if (-not [IO.Path]::GetFullPath($target).StartsWith(($repoRoot + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Percorso del pacchetto fuori dal repository.'
    }
}
[IO.Directory]::Move($package, $destination)
Compress-Archive -LiteralPath $destination -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
"$hash  $([IO.Path]::GetFileName($zip))" | Set-Content -LiteralPath ($zip + '.sha256.txt') -Encoding ASCII
Write-Host "Pacchetto pronto: $zip"
Write-Host "SHA256: $hash"
