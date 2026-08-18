param(
    [int] $Port = 5080,
    [switch] $ExposeDiagnostics
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:Markstash__Backend__ExposeDiagnostics = $ExposeDiagnostics.ToString().ToLowerInvariant()

dotnet run `
    --project (Join-Path $repoRoot "src\Markstash.Backend\Markstash.Backend.csproj") `
    --no-launch-profile
