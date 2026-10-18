param(
    [string]$LocalDataRoot = [Environment]::GetFolderPath('LocalApplicationData'),
    [string[]]$ShortcutDirectories = @([Environment]::GetFolderPath('Desktop'), [Environment]::GetFolderPath('Programs')),
    [switch]$NoLaunch,
    [switch]$SkipShortcuts
)

$ErrorActionPreference = 'Stop'
$profileName = 'Test-MarioRossi-12345'
if ([string]::IsNullOrWhiteSpace($LocalDataRoot)) { throw 'Cartella utente Windows non disponibile.' }
$LocalDataRoot = [IO.Path]::GetFullPath($LocalDataRoot)
$installDirectory = Join-Path $LocalDataRoot 'Programs\SMZ-Test-MarioRossi-12345'
$dataDirectory = Join-Path $LocalDataRoot "SMZ\$profileName"
$databasePath = Join-Path $dataDirectory 'smz-conta.db'
$sourceApplication = Join-Path $PSScriptRoot 'App'
$seedPath = Join-Path $PSScriptRoot 'Dati iniziali\smz-conta.db'
$executablePath = Join-Path $installDirectory 'SMZ.App.exe'

if (-not [Environment]::Is64BitOperatingSystem) { throw 'Questo pacchetto richiede Windows a 64 bit.' }
foreach ($required in @('App\SMZ.App.exe', 'App\Avvia-SMZ-Test.ps1', 'App\AVVIA-SMZ-TEST.cmd', 'Dati iniziali\smz-conta.db', 'LEGGIMI-PRIMA.txt', 'manifest.json')) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $required) -PathType Leaf)) {
        throw "File mancante: $required. Estrarre l'intero ZIP prima di avviare l'installazione."
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.DataProfile -ne $profileName) { throw 'Profilo del pacchetto non riconosciuto.' }
foreach ($entry in $manifest.Files) {
    $sourcePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot $entry.Path))
    if (-not $sourcePath.StartsWith(([IO.Path]::GetFullPath($PSScriptRoot) + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Percorso file del pacchetto non valido.'
    }
    if ((Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash -ne $entry.SHA256) {
        throw "File danneggiato: $($entry.Path). Estrarre nuovamente il pacchetto originale."
    }
}

$runningApplication = Get-Process -Name 'SMZ.App' -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $executablePath }
if ($runningApplication) { throw 'Chiudere SMZ TEST prima di ripetere l''installazione.' }

New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $sourceApplication -File | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $installDirectory $_.Name) -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LEGGIMI-PRIMA.txt') -Destination (Join-Path $installDirectory 'LEGGIMI-PRIMA.txt') -Force

if (-not (Test-Path -LiteralPath $databasePath)) {
    [IO.File]::Copy($seedPath, $databasePath, $false)
    Write-Host 'Database iniziale installato: solo Mario Rossi, PerID 12345.'
} else {
    Write-Host 'Database di questa prova gia presente: i dati inseriti durante il test sono stati conservati.'
}

@{ DataProfile = $profileName; DataDirectory = $dataDirectory } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $installDirectory 'installazione-test.json') -Encoding UTF8

if (-not $SkipShortcuts) {
    $shell = New-Object -ComObject WScript.Shell
    foreach ($shortcutDirectory in $ShortcutDirectories) {
        if ([string]::IsNullOrWhiteSpace($shortcutDirectory)) { continue }
        New-Item -ItemType Directory -Path $shortcutDirectory -Force | Out-Null
        $shortcut = $shell.CreateShortcut((Join-Path $shortcutDirectory 'SMZ TEST - Mario Rossi 12345.lnk'))
        $shortcut.TargetPath = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
        $shortcut.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "' + (Join-Path $installDirectory 'Avvia-SMZ-Test.ps1') + '"'
        $shortcut.WorkingDirectory = $installDirectory
        $shortcut.IconLocation = "$executablePath,0"
        $shortcut.Description = 'SMZ TEST - database separato, Mario Rossi PerID 12345'
        $shortcut.WindowStyle = 7
        $shortcut.Save()
    }
}

Write-Host "Applicazione: $installDirectory"
Write-Host "Dati di test: $dataDirectory"
Write-Host 'Primo accesso: PerID 12345, scegliere e confermare una password di almeno 10 caratteri.'
if (-not $NoLaunch) { & (Join-Path $installDirectory 'Avvia-SMZ-Test.ps1') }
