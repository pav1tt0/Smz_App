param(
    [string]$LocalDataRoot = [Environment]::GetFolderPath('LocalApplicationData'),
    [string]$SourceDatabase,
    [switch]$UseExistingDatabase,
    [string[]]$ShortcutDirectories = @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs')),
    [switch]$NoLaunch,
    [switch]$SkipShortcuts
)

$ErrorActionPreference = 'Stop'
if (-not [Environment]::Is64BitOperatingSystem) { throw 'Questo pacchetto richiede Windows a 64 bit.' }
if ([string]::IsNullOrWhiteSpace($LocalDataRoot)) { throw 'Cartella utente Windows non disponibile.' }
$LocalDataRoot = [IO.Path]::GetFullPath($LocalDataRoot)
$packageRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$installDirectory = Join-Path $LocalDataRoot 'Programs\SMZ'
$ordinaryDirectory = Join-Path $LocalDataRoot 'SMZ\Conta'
$settingsPath = Join-Path $installDirectory 'installazione.json'

# Verify the files that will be installed, including both launchers.
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.PackageKind -ne 'Operational' -or $manifest.DatabaseIncluded -ne $false -or $manifest.Configuration -ne 'Release') {
    throw 'Il pacchetto non e una distribuzione operativa SMZ senza dati iniziali.'
}
$verified = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $manifest.Files) {
    if ([IO.Path]::IsPathRooted($entry.Path) -or $entry.Path -match '(^|[\\/])\.\.?([\\/]|$)' -or $entry.Path.Contains(':')) {
        throw 'Percorso file del pacchetto non valido.'
    }
    $filePath = [IO.Path]::GetFullPath((Join-Path $packageRoot $entry.Path))
    if (-not $filePath.StartsWith(($packageRoot + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase) -or -not $verified.Add($entry.Path)) {
        throw 'Percorso duplicato o esterno al pacchetto.'
    }
    if ((Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "File danneggiato: $($entry.Path). Estrarre nuovamente lo ZIP originale."
    }
}
foreach ($required in @('App\SMZ.App.exe', 'App\Avvia-SMZ.ps1', 'App\AVVIA-SMZ.cmd', 'LEGGIMI-PRIMA.txt')) {
    if (-not $verified.Contains($required)) { throw "File obbligatorio non verificato: $required" }
}
if (Get-Process -Name 'SMZ.App' -ErrorAction SilentlyContinue) {
    throw 'Chiudere tutte le copie di SMZ prima di installare o aggiornare il programma.'
}

$existingDirectory = $ordinaryDirectory
if (Test-Path -LiteralPath $settingsPath) {
    $previous = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($previous.PackageKind -ne 'Operational' -or -not [IO.Path]::IsPathRooted($previous.DataDirectory)) {
        throw 'Configurazione precedente non valida. Conservare i dati e verificare installazione.json.'
    }
    $existingDirectory = [IO.Path]::GetFullPath($previous.DataDirectory)
}
$existingDatabase = Join-Path $existingDirectory 'smz-conta.db'
if ($UseExistingDatabase -and -not [string]::IsNullOrWhiteSpace($SourceDatabase)) {
    throw 'Specificare il database sorgente oppure il riutilizzo di quello esistente.'
}
if ([string]::IsNullOrWhiteSpace($SourceDatabase)) {
    if ($UseExistingDatabase) {
        if (-not (Test-Path -LiteralPath $existingDatabase -PathType Leaf)) { throw 'Database esistente non trovato.' }
        $SourceDatabase = $existingDatabase
    } else {
        Add-Type -AssemblyName PresentationFramework
        if (Test-Path -LiteralPath $existingDatabase -PathType Leaf) {
            $answer = [System.Windows.MessageBox]::Show(
                "E stato trovato il database gia in uso:`n$existingDatabase`n`nSI: usa questo database.`nNO: scegli un altro file .db o backup .smzbak.`nANNULLA: esci dall'installazione.",
                'SMZ - scelta dei dati reali', 'YesNoCancel', 'Question')
            if ($answer -eq 'Cancel') { return }
            if ($answer -eq 'Yes') { $SourceDatabase = $existingDatabase }
        }
        if ([string]::IsNullOrWhiteSpace($SourceDatabase)) {
            $dialog = New-Object Microsoft.Win32.OpenFileDialog
            $dialog.Title = 'Seleziona il database reale o il backup SMZ salvato su questo PC'
            $dialog.Filter = 'Database o backup SMZ|*.db;*.sqlite;*.sqlite3;*.smzbak|Tutti i file|*.*'
            $dialog.CheckFileExists = $true
            if ($dialog.ShowDialog() -ne $true) { return }
            $SourceDatabase = $dialog.FileName
        }
    }
}
$SourceDatabase = [IO.Path]::GetFullPath($SourceDatabase)
if (-not (Test-Path -LiteralPath $SourceDatabase -PathType Leaf)) { throw 'Database o backup selezionato non trovato.' }

if ($SourceDatabase.Equals($existingDatabase, [StringComparison]::OrdinalIgnoreCase)) {
    $dataDirectory = $existingDirectory
} elseif (-not (Test-Path -LiteralPath $ordinaryDirectory) -or @(Get-ChildItem -LiteralPath $ordinaryDirectory -Force).Count -eq 0) {
    $dataDirectory = $ordinaryDirectory
} else {
    # Selecting another source never replaces the database already on this PC.
    $dataDirectory = Join-Path $LocalDataRoot ('SMZ\Operativo-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))
}

New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
$reportPath = Join-Path $installDirectory ('preparazione-dati-' + [Guid]::NewGuid().ToString('N') + '.json')
foreach ($argument in @($SourceDatabase, $dataDirectory, $reportPath)) {
    if ($argument.Contains('"')) { throw 'Percorso non valido.' }
}
$processInfo = New-Object Diagnostics.ProcessStartInfo
$processInfo.FileName = Join-Path $packageRoot 'App\SMZ.App.exe'
$processInfo.UseShellExecute = $false
$processInfo.CreateNoWindow = $true
$processInfo.WindowStyle = 'Hidden'
$processInfo.Arguments = '--prepare-operational-database "' + $SourceDatabase + '" "' + $dataDirectory + '" "' + $reportPath + '"'
$process = [Diagnostics.Process]::Start($processInfo)
try {
    $process.WaitForExit()
    if (-not (Test-Path -LiteralPath $reportPath)) { throw 'Verifica del database non completata. Nessun dato e stato sostituito.' }
    $result = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or -not $result.Success) { throw "Database non utilizzabile: $($result.Error)" }
} finally {
    $process.Dispose()
}

foreach ($name in @('SMZ.App.exe', 'Avvia-SMZ.ps1', 'AVVIA-SMZ.cmd')) {
    Copy-Item -LiteralPath (Join-Path $packageRoot ('App\' + $name)) -Destination (Join-Path $installDirectory $name) -Force
}
Copy-Item -LiteralPath (Join-Path $packageRoot 'LEGGIMI-PRIMA.txt') -Destination (Join-Path $installDirectory 'LEGGIMI-PRIMA.txt') -Force
@{
    PackageKind = 'Operational'; DataDirectory = $dataDirectory; DatabasePath = $result.DatabasePath
    SafetyBackupPath = $result.SafetyBackupPath; Package = $manifest.Package; InstalledAt = (Get-Date).ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding UTF8

if (-not $SkipShortcuts) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($directory in $ShortcutDirectories) {
        if ([string]::IsNullOrWhiteSpace($directory)) { continue }
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
        $shortcut = $shell.CreateShortcut((Join-Path $directory 'SMZ.lnk'))
        $shortcut.TargetPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $shortcut.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + (Join-Path $installDirectory 'Avvia-SMZ.ps1') + '"'
        $shortcut.WorkingDirectory = $installDirectory
        $shortcut.IconLocation = (Join-Path $installDirectory 'SMZ.App.exe') + ',0'
        $shortcut.Description = 'SMZ - database operativo selezionato durante l''installazione'
        $shortcut.WindowStyle = 7
        $shortcut.Save()
    }
}
Write-Host 'Installazione SMZ completata.'
Write-Host "Database in uso: $($result.DatabasePath)"
Write-Host "Copia di sicurezza: $($result.SafetyBackupPath)"
Write-Host "Personale: $($result.PersonaleCount) - Servizi: $($result.ServiziCount)"
if ($result.HasUsers) {
    Write-Host 'Accedere con PerID e password gia presenti nel database.'
} else {
    Write-Host 'Al primo avvio creare l''amministratore con un PerID presente nel database reale.'
}
if (-not $NoLaunch) { & (Join-Path $installDirectory 'Avvia-SMZ.ps1') }
