param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $repoRoot "SMZ.Conta.App\SMZ.Conta.App.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Progetto non trovato: $projectPath"
}

$arguments = @(
    "run",
    "--project", $projectPath,
    "-c", $Configuration
)

if ($NoBuild) {
    $arguments += "--no-build"
}

& dotnet @arguments

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
