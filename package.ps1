param(
    [string]$OutputDirectory = "dist"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$output = Join-Path $root $OutputDirectory
$temp = Join-Path $root "package-temp"
$version = "0.1.0"
$zipPath = Join-Path $output "SharedDiscovery-$version.zip"

& (Join-Path $root "build.ps1") -Configuration Release

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (Test-Path $temp) {
    Remove-Item -Recurse -Force $temp
}

New-Item -ItemType Directory -Force -Path (Join-Path $temp "plugins") | Out-Null
New-Item -ItemType Directory -Force -Path $output | Out-Null

Copy-Item -Force (Join-Path $root "Package\manifest.json") $temp
Copy-Item -Force (Join-Path $root "Package\README.md") $temp
Copy-Item -Force (Join-Path $root "Package\CHANGELOG.md") $temp
Copy-Item -Force (Join-Path $root "Package\icon.png") $temp
Copy-Item -Force (Join-Path $root "src\SharedDiscovery\bin\Release\net462\SharedDiscovery.dll") (Join-Path $temp "plugins")
Copy-Item -Force (Join-Path $root "src\SharedDiscovery.Core\bin\Release\net462\SharedDiscovery.Core.dll") (Join-Path $temp "plugins")

if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $temp "*") -DestinationPath $zipPath
Write-Host "Created $zipPath"
