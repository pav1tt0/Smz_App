$ErrorActionPreference = 'Stop'
try {
    $settings = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'installazione-test.json') -Raw | ConvertFrom-Json
    $dataDirectory = [IO.Path]::GetFullPath($settings.DataDirectory)
    if ($settings.DataProfile -ne 'Test-MarioRossi-12345' -or (Split-Path -Leaf $dataDirectory) -ne 'Test-MarioRossi-12345') {
        throw 'Configurazione del database di test non valida. Ripetere l''installazione.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $dataDirectory 'smz-conta.db') -PathType Leaf)) {
        throw 'Database di test non trovato. Ripetere l''installazione dal pacchetto ZIP.'
    }
    $previousDatabaseOverride = $env:SMZ_CONTA_APPDATA
    try {
        $env:SMZ_CONTA_APPDATA = $dataDirectory
        Start-Process -FilePath (Join-Path $PSScriptRoot 'SMZ.App.exe') -WorkingDirectory $PSScriptRoot
    } finally {
        $env:SMZ_CONTA_APPDATA = $previousDatabaseOverride
    }
} catch {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show($_.Exception.Message, 'Avvio SMZ TEST') | Out-Null
    exit 1
}
