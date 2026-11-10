param([Parameter(Mandatory = $true)][string]$ZipPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$root = Join-Path $repoRoot ('.codex-build\collaudo-operativo-' + [Guid]::NewGuid().ToString('N'))
$extracted = Join-Path $root 'Pacchetto con spazi'
New-Item -ItemType Directory -Path $extracted -Force | Out-Null
Expand-Archive -LiteralPath $ZipPath -DestinationPath $extracted
$packageDirectories = @(Get-ChildItem -LiteralPath $extracted -Directory)
if ($packageDirectories.Count -ne 1) { throw 'Struttura del pacchetto non valida.' }
$package = $packageDirectories[0].FullName
if (@(Get-ChildItem -LiteralPath $package -Recurse -File | Where-Object { $_.Extension -in @('.db', '.sqlite', '.sqlite3', '.smzbak') }).Count -ne 0) {
    throw 'Il pacchetto contiene un database.'
}
$project = Join-Path $PSScriptRoot 'SMZ.DeploymentCheck\SMZ.DeploymentCheck.csproj'
$fixtureDirectory = Join-Path $root 'Sorgente di collaudo'
& dotnet run --project $project -c Release -- create $fixtureDirectory
if ($LASTEXITCODE -ne 0) { throw 'Fixture di collaudo non creata.' }
$fixture = Get-Content -LiteralPath (Join-Path $fixtureDirectory 'fixture.json') -Raw | ConvertFrom-Json
$probe = Join-Path $PSScriptRoot 'SMZ.DeploymentCheck\bin\Release\net8.0-windows\SMZ.DeploymentCheck.dll'
function Get-DataFingerprint([string]$Path) {
    $value = & dotnet $probe fingerprint $Path
    if ($LASTEXITCODE -ne 0 -or $value -notmatch '^[A-F0-9]{64}$') { throw 'Lettura dati di collaudo non riuscita.' }
    return $value
}
function Assert-Condition([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
$expected = Get-DataFingerprint $fixture.DatabasePath
$sourceHash = (Get-FileHash -LiteralPath $fixture.DatabasePath -Algorithm SHA256).Hash
$installer = Join-Path $package 'Installa-SMZ.ps1'
$previousPc = Join-Path $root 'PC gia usato senza installatore'
$previousData = Join-Path $previousPc 'SMZ\Conta'
New-Item -ItemType Directory -Path $previousData -Force | Out-Null
$previousDatabase = Join-Path $previousData 'smz-conta.db'
Copy-Item -LiteralPath $fixture.DatabasePath -Destination $previousDatabase
$previousHash = (Get-FileHash -LiteralPath $previousDatabase -Algorithm SHA256).Hash
& $installer -LocalDataRoot $previousPc -UseExistingDatabase -NoLaunch -SkipShortcuts
Assert-Condition ((Get-FileHash -LiteralPath $previousDatabase -Algorithm SHA256).Hash -eq $previousHash) 'Prima installazione ha modificato il database gia presente.'
$profile = Join-Path $root 'PC con dati reali simulati'
$desktop = Join-Path $root 'Desktop di collaudo'
$start = Join-Path $root 'Menu Start di collaudo'
& $installer -LocalDataRoot $profile -SourceDatabase $fixture.DatabasePath -ShortcutDirectories @($desktop, $start) -NoLaunch
$installed = Join-Path $profile 'Programs\SMZ'
$settings = Get-Content -LiteralPath (Join-Path $installed 'installazione.json') -Raw | ConvertFrom-Json
Assert-Condition ((Get-DataFingerprint $settings.DatabasePath) -eq $expected) 'Installazione ha modificato righe, account o cataloghi.'
Assert-Condition ((Get-FileHash -LiteralPath $fixture.DatabasePath -Algorithm SHA256).Hash -eq $sourceHash) 'Sorgente modificata.'
Assert-Condition (Test-Path -LiteralPath $settings.SafetyBackupPath) 'Backup precedente al primo avvio assente.'
$reportFile = Get-ChildItem -LiteralPath $installed -Filter 'preparazione-dati-*.json' | Select-Object -First 1
$report = Get-Content -LiteralPath $reportFile.FullName -Raw | ConvertFrom-Json
Assert-Condition ($report.HasUsers -and $report.PersonaleCount -eq 1 -and $report.ServiziCount -eq 1) 'Account, personale o servizi di collaudo mancanti.'
Assert-Condition ($report.RequiresInstalledDatabase -eq $true) 'Il pacchetto puo avviarsi senza database selezionato.'
$shell = New-Object -ComObject WScript.Shell
foreach ($directory in @($desktop, $start)) {
    $shortcut = $shell.CreateShortcut((Join-Path $directory 'SMZ.lnk'))
    Assert-Condition ($shortcut.Arguments.Contains((Join-Path $installed 'Avvia-SMZ.ps1'))) 'Collegamento non corretto.'
}
$originalInstalledPath = $settings.DatabasePath
$originalHash = (Get-FileHash -LiteralPath $originalInstalledPath -Algorithm SHA256).Hash
& $installer -LocalDataRoot $profile -UseExistingDatabase -NoLaunch -SkipShortcuts
$reinstalled = Get-Content -LiteralPath (Join-Path $installed 'installazione.json') -Raw | ConvertFrom-Json
Assert-Condition ($reinstalled.DatabasePath -eq $originalInstalledPath) 'Reinstallazione ha cambiato database.'
Assert-Condition ((Get-FileHash -LiteralPath $originalInstalledPath -Algorithm SHA256).Hash -eq $originalHash) 'Reinstallazione ha modificato il database.'

# Choosing another source uses a separate profile and preserves the earlier database.
& $installer -LocalDataRoot $profile -SourceDatabase $fixture.DatabasePath -NoLaunch -SkipShortcuts
$selected = Get-Content -LiteralPath (Join-Path $installed 'installazione.json') -Raw | ConvertFrom-Json
Assert-Condition ($selected.DatabasePath -ne $originalInstalledPath) 'Selezione di un altro file ha sovrascritto il database precedente.'
Assert-Condition ((Get-DataFingerprint $selected.DatabasePath) -eq $expected) 'Nuovo database selezionato non corrispondente.'
Assert-Condition ((Get-FileHash -LiteralPath $originalInstalledPath -Algorithm SHA256).Hash -eq $originalHash) 'Database precedente modificato.'

$backupProfile = Join-Path $root 'PC da backup'
& $installer -LocalDataRoot $backupProfile -SourceDatabase $fixture.BackupPath -NoLaunch -SkipShortcuts
$fromBackup = Get-Content -LiteralPath (Join-Path $backupProfile 'Programs\SMZ\installazione.json') -Raw | ConvertFrom-Json
Assert-Condition ((Get-DataFingerprint $fromBackup.DatabasePath) -eq $expected) 'Dati del backup non conservati.'
Assert-Condition ((Get-Content -LiteralPath (Join-Path $fromBackup.DataDirectory 'Export\documento-collaudo.txt') -Raw) -eq 'Export da preservare') 'Export perso.'

$invalid = Join-Path $root 'Database errato.db'
'NON E SQLITE' | Set-Content -LiteralPath $invalid
$invalidProfile = Join-Path $root 'Installazione rifiutata'
$failed = $false
try { & $installer -LocalDataRoot $invalidProfile -SourceDatabase $invalid -NoLaunch -SkipShortcuts } catch { $failed = $true }
Assert-Condition $failed 'Database errato accettato.'
Assert-Condition (-not (Test-Path -LiteralPath (Join-Path $invalidProfile 'SMZ\Conta\smz-conta.db'))) 'Database vuoto creato dopo il rifiuto.'

$readme = Join-Path $package 'LEGGIMI-PRIMA.txt'
$readmeBytes = [IO.File]::ReadAllBytes($readme)
try {
    'PACCHETTO ALTERATO' | Set-Content -LiteralPath $readme
    $failed = $false
    try { & $installer -LocalDataRoot $profile -UseExistingDatabase -NoLaunch -SkipShortcuts } catch { $failed = $true }
    Assert-Condition $failed 'Manifest alterato accettato.'
} finally { [IO.File]::WriteAllBytes($readme, $readmeBytes) }
Assert-Condition ((Get-FileHash -LiteralPath $originalInstalledPath -Algorithm SHA256).Hash -eq $originalHash) 'Dati modificati dalla prova negativa.'

@{
    ZipPath = [IO.Path]::GetFullPath($ZipPath); ZipSHA256 = (Get-FileHash -LiteralPath $ZipPath -Algorithm SHA256).Hash
    TestRoot = $root; InstalledDirectory = $installed
    Checks = @('Nessun database distribuito', 'Database gia presente prima dell''installazione', 'Percorsi con spazi', 'Importazione DB', 'Account e cataloghi conservati', 'Sorgente intatta', 'Backup prima dell''avvio', 'Collegamenti desktop e Start', 'Reinstallazione non modifica DB', 'Altro file non sovrascrive DB precedente', 'Importazione smzbak con Export', 'Database non valido rifiutato', 'Pacchetto alterato rifiutato', 'Database selezionato obbligatorio')
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'esito-collaudo.json') -Encoding UTF8
Write-Host "OK: distribuzione operativa verificata. Risultati: $root"
