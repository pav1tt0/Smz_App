$ErrorActionPreference = 'Stop'
try {
    $settings = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'installazione.json') -Raw | ConvertFrom-Json
    if ($settings.PackageKind -ne 'Operational' -or -not [IO.Path]::IsPathRooted($settings.DataDirectory)) {
        throw 'Configurazione SMZ non valida. Ripetere l''installazione.'
    }
    $dataDirectory = [IO.Path]::GetFullPath($settings.DataDirectory)
    if (-not (Test-Path -LiteralPath (Join-Path $dataDirectory 'smz-conta.db') -PathType Leaf)) {
        throw 'Il database reale configurato non e disponibile. Ripetere l''installazione e selezionare il database corretto.'
    }
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = Join-Path $PSScriptRoot 'SMZ.App.exe'
    $start.WorkingDirectory = $PSScriptRoot
    $start.UseShellExecute = $false
    $start.EnvironmentVariables['SMZ_CONTA_APPDATA'] = $dataDirectory
    [Diagnostics.Process]::Start($start) | Out-Null
} catch {
    Add-Type -AssemblyName PresentationFramework
    [System.Windows.MessageBox]::Show($_.Exception.Message, 'Avvio SMZ') | Out-Null
    exit 1
}
