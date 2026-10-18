param(
    [ValidatePattern('^[A-Za-z0-9][A-Za-z0-9._-]+$')]
    [string]$PackageName = ('SMZ-Test-MarioRossi-12345-' + (Get-Date -Format 'yyyy-MM-dd'))
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$distRoot = Join-Path $repoRoot 'dist'
$finalDirectory = Join-Path $distRoot $PackageName
$zipPath = Join-Path $distRoot ($PackageName + '.zip')
if ((Test-Path -LiteralPath $finalDirectory) -or (Test-Path -LiteralPath $zipPath)) {
    throw 'Pacchetto gia esistente. Specificare un altro -PackageName per conservare la versione precedente.'
}

$stageRoot = Join-Path $repoRoot ('.codex-build\pacchetto-' + [Guid]::NewGuid().ToString('N'))
$packageDirectory = Join-Path $stageRoot $PackageName
$applicationDirectory = Join-Path $packageDirectory 'App'
$seedDirectory = Join-Path $packageDirectory 'Dati iniziali'
$templateDirectory = Join-Path $PSScriptRoot 'test-package'
$seedProject = Join-Path $PSScriptRoot 'SMZ.TestPackage\SMZ.TestPackage.csproj'
$appProject = Join-Path $repoRoot 'SMZ.Conta.App\SMZ.Conta.App.csproj'
New-Item -ItemType Directory -Path $applicationDirectory -Force | Out-Null

& dotnet run --project $seedProject -c Release -- create $seedDirectory
if ($LASTEXITCODE -ne 0) { throw 'Creazione del database iniziale non riuscita.' }

& dotnet publish $appProject -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -p:SmzDataProfile=Test-MarioRossi-12345 `
    -o $applicationDirectory
if ($LASTEXITCODE -ne 0) { throw 'Pubblicazione dell''applicazione non riuscita.' }

foreach ($name in @('INSTALLA-E-AVVIA-SMZ-TEST.cmd', 'Installa-SMZ-Test.ps1', 'LEGGIMI-PRIMA.txt')) {
    Copy-Item -LiteralPath (Join-Path $templateDirectory $name) -Destination (Join-Path $packageDirectory $name)
}
foreach ($name in @('AVVIA-SMZ-TEST.cmd', 'Avvia-SMZ-Test.ps1')) {
    Copy-Item -LiteralPath (Join-Path $templateDirectory $name) -Destination (Join-Path $applicationDirectory $name)
}

$verification = & dotnet run --project $seedProject -c Release --no-build -- verify $seedDirectory
if ($LASTEXITCODE -ne 0) { throw 'Verifica finale del database non riuscita.' }
$verification | Set-Content -LiteralPath (Join-Path $packageDirectory 'VERIFICA-DATABASE.txt') -Encoding UTF8

$files = @(Get-ChildItem -LiteralPath $packageDirectory -File -Recurse | Sort-Object FullName | ForEach-Object {
    @{
        Path = $_.FullName.Substring($packageDirectory.Length + 1)
        SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
})
@{
    Package = $PackageName
    CreatedAt = (Get-Date).ToString('o')
    Configuration = 'Release'
    Runtime = 'win-x64'
    SelfContained = $true
    DataProfile = 'Test-MarioRossi-12345'
    Personale = @{ PerId = 12345; Nome = 'Mario'; Cognome = 'Rossi' }
    Files = $files
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageDirectory 'manifest.json') -Encoding UTF8

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
# Entrambi i percorsi dello spostamento devono restare nel repository.
foreach ($target in @($packageDirectory, $finalDirectory)) {
    if (-not [IO.Path]::GetFullPath($target).StartsWith(($repoRoot + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw "Percorso fuori dal repository: $target"
    }
}
[IO.Directory]::Move($packageDirectory, $finalDirectory)
Compress-Archive -LiteralPath $finalDirectory -DestinationPath $zipPath -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
"$zipHash  $([IO.Path]::GetFileName($zipPath))" |
    Set-Content -LiteralPath ($zipPath + '.sha256.txt') -Encoding ASCII

Write-Host "Pacchetto pronto: $zipPath"
Write-Host "Cartella: $finalDirectory"
Write-Host "SHA256: $zipHash"
