param([Parameter(Mandatory = $true)][string]$ZipPath)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path $repoRoot ('.codex-build\collaudo-pacchetto-' + [Guid]::NewGuid().ToString('N'))
$extractionDirectory = Join-Path $testRoot 'Pacchetto estratto'
$localDataRoot = Join-Path $testRoot 'Profilo utente con spazi'
$desktopDirectory = Join-Path $testRoot 'Desktop di prova'
$startDirectory = Join-Path $testRoot 'Menu Start di prova'
New-Item -ItemType Directory -Path $extractionDirectory -Force | Out-Null
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extractionDirectory
$packageDirectory = @(Get-ChildItem -LiteralPath $extractionDirectory -Directory)
if ($packageDirectory.Count -ne 1) { throw 'Struttura ZIP inattesa.' }
$packageDirectory = $packageDirectory[0].FullName
$installer = Join-Path $packageDirectory 'Installa-SMZ-Test.ps1'

# Il file sentinella rappresenta un database ordinario preesistente.
$ordinaryDirectory = Join-Path $localDataRoot 'SMZ\Conta'
New-Item -ItemType Directory -Path $ordinaryDirectory -Force | Out-Null
$ordinaryDatabase = Join-Path $ordinaryDirectory 'smz-conta.db'
'DATABASE ORDINARIO: NON MODIFICARE' | Set-Content -LiteralPath $ordinaryDatabase -Encoding ASCII
$ordinaryHash = (Get-FileHash -LiteralPath $ordinaryDatabase -Algorithm SHA256).Hash

& $installer -LocalDataRoot $localDataRoot -ShortcutDirectories @($desktopDirectory, $startDirectory) -NoLaunch
$installedDirectory = Join-Path $localDataRoot 'Programs\SMZ-Test-MarioRossi-12345'
$dataDirectory = Join-Path $localDataRoot 'SMZ\Test-MarioRossi-12345'
$databasePath = Join-Path $dataDirectory 'smz-conta.db'
$seedProject = Join-Path $PSScriptRoot 'SMZ.TestPackage\SMZ.TestPackage.csproj'
& dotnet run --project $seedProject -c Release --no-build -- verify $dataDirectory
if ($LASTEXITCODE -ne 0) { throw 'Dati installati diversi dal database pulito previsto.' }

$shell = New-Object -ComObject WScript.Shell
foreach ($shortcutDirectory in @($desktopDirectory, $startDirectory)) {
    $shortcutPath = Join-Path $shortcutDirectory 'SMZ TEST - Mario Rossi 12345.lnk'
    if (-not (Test-Path -LiteralPath $shortcutPath)) { throw 'Collegamento non creato.' }
    $shortcut = $shell.CreateShortcut($shortcutPath)
    if (-not $shortcut.Arguments.Contains((Join-Path $installedDirectory 'Avvia-SMZ-Test.ps1'))) {
        throw 'Il collegamento non avvia il pacchetto installato.'
    }
}

& python -c "import sqlite3,sys; db=sqlite3.connect(sys.argv[1]); db.execute('PRAGMA user_version=12345'); db.commit(); db.close()" $databasePath
if ($LASTEXITCODE -ne 0) { throw 'Preparazione della prova di reinstallazione non riuscita.' }
$modifiedHash = (Get-FileHash -LiteralPath $databasePath -Algorithm SHA256).Hash
& $installer -LocalDataRoot $localDataRoot -ShortcutDirectories @($desktopDirectory, $startDirectory) -NoLaunch
if ((Get-FileHash -LiteralPath $databasePath -Algorithm SHA256).Hash -ne $modifiedHash) {
    throw 'La reinstallazione ha sostituito il database di test gia presente.'
}
if ((Get-FileHash -LiteralPath $ordinaryDatabase -Algorithm SHA256).Hash -ne $ordinaryHash) {
    throw 'Il database ordinario e stato modificato.'
}

$executablePath = Join-Path $installedDirectory 'SMZ.App.exe'
& python -c "import pathlib,sys; data=pathlib.Path(sys.argv[1]).read_bytes(); assert b'SMZ.DataProfile' in data and b'Test-MarioRossi-12345' in data, 'Profilo dati non incluso nel file pubblicato'" $executablePath
if ($LASTEXITCODE -ne 0) { throw 'Profilo dati del pacchetto non verificato.' }

@{
    ZipPath = [IO.Path]::GetFullPath($ZipPath)
    ZipSHA256 = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
    TestRoot = $testRoot
    InstalledDirectory = $installedDirectory
    DataDirectory = $dataDirectory
    Checks = @('Estrazione ZIP', 'Installazione in percorso con spazi', 'Database pulito', 'Collegamenti desktop e Start', 'Reinstallazione conserva dati', 'Database ordinario intatto', 'Profilo test incluso nell''eseguibile')
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $testRoot 'esito-collaudo.json') -Encoding UTF8
Write-Host "OK: pacchetto verificato. Risultati: $testRoot"
