param(
    [string]$OutputDirectory = "dist"
)

$ErrorActionPreference = "Stop"
$version = "0.1.0"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$output = Join-Path $root $OutputDirectory
$staging = Join-Path $output ".staging"
$zipPath = Join-Path $output "SharedDiscovery-$version.zip"

function Require-File {
    param([string]$Path, [string]$Label)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label not found: $Path"
    }
}

function Get-PngDimensions {
    param([string]$Path)

    $stream = [System.IO.File]::OpenRead($Path)
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        $signature = $reader.ReadBytes(8)
        $expected = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
        if (($signature -join ",") -ne ($expected -join ",")) {
            throw "Icon is not a PNG file: $Path"
        }

        $length = $reader.ReadUInt32()
        $chunk = [Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
        if ($chunk -ne "IHDR" -or $length -lt 8) {
            throw "PNG does not contain a valid IHDR chunk: $Path"
        }

        $widthBytes = $reader.ReadBytes(4)
        $heightBytes = $reader.ReadBytes(4)
        $width = [BitConverter]::ToUInt32([byte[]]($widthBytes[3], $widthBytes[2], $widthBytes[1], $widthBytes[0]), 0)
        $height = [BitConverter]::ToUInt32([byte[]]($heightBytes[3], $heightBytes[2], $heightBytes[1], $heightBytes[0]), 0)
        return @{ Width = $width; Height = $height }
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Assert-ZipContents {
    param([string]$Path, [string[]]$Expected)

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $actual = @(
    	$archive.Entries |
    	ForEach-Object { $_.FullName.Replace('\', '/').TrimEnd('/') } |
    	Sort-Object
	)

	$expectedSorted = @(
    	$Expected |
    	ForEach-Object { $_.Replace('\', '/').TrimEnd('/') } |
    	Sort-Object
	)
        if (($actual -join "`n") -ne ($expectedSorted -join "`n")) {
            throw "ZIP contents do not match the expected root layout.`nActual:`n$($actual -join "`n")`nExpected:`n$($expectedSorted -join "`n")"
        }
    }
    finally {
        $archive.Dispose()
    }
}

if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $staging "plugins") | Out-Null
New-Item -ItemType Directory -Force -Path $output | Out-Null

& dotnet build (Join-Path $root "SharedDiscovery.sln") -c Release
if ($LASTEXITCODE -ne 0) {
    throw "Release build failed with exit code $LASTEXITCODE."
}

$manifestPath = Join-Path $root "Thunderstore\manifest.json"
$iconPath = Join-Path $root "Thunderstore\icon.png"
$readmePath = Join-Path $root "README.md"
$changelogPath = Join-Path $root "Thunderstore\CHANGELOG.md"
$pluginPath = Join-Path $root "src\SharedDiscovery\bin\Release\net462\SharedDiscovery.dll"
$corePath = Join-Path $root "src\SharedDiscovery.Core\bin\Release\net462\SharedDiscovery.Core.dll"

Require-File $manifestPath "manifest.json"
Require-File $iconPath "icon.png"
Require-File $readmePath "README.md"
Require-File $changelogPath "CHANGELOG.md"
Require-File $pluginPath "SharedDiscovery.dll"
Require-File $corePath "SharedDiscovery.Core.dll"

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.name -ne "SharedDiscovery" -or $manifest.version_number -ne $version) {
    throw "Manifest name/version mismatch."
}
if ($manifest.dependencies -notcontains "denikson-BepInExPack_Valheim-5.4.2350") {
    throw "Required BepInEx dependency is missing from manifest."
}

$dimensions = Get-PngDimensions $iconPath
if ($dimensions.Width -ne 256 -or $dimensions.Height -ne 256) {
    throw "icon.png must be 256x256, got $($dimensions.Width)x$($dimensions.Height)."
}
if ((Get-Item -LiteralPath $pluginPath).Length -eq 0 -or (Get-Item -LiteralPath $corePath).Length -eq 0) {
    throw "A package DLL has zero length."
}

Copy-Item -LiteralPath $manifestPath -Destination $staging
Copy-Item -LiteralPath $iconPath -Destination $staging
Copy-Item -LiteralPath $readmePath -Destination $staging
Copy-Item -LiteralPath $changelogPath -Destination $staging
Copy-Item -LiteralPath $pluginPath -Destination (Join-Path $staging "plugins")
Copy-Item -LiteralPath $corePath -Destination (Join-Path $staging "plugins")

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath
$expected = @(
    "manifest.json",
    "icon.png",
    "README.md",
    "CHANGELOG.md",
    "plugins/SharedDiscovery.dll",
    "plugins/SharedDiscovery.Core.dll"
)
Assert-ZipContents $zipPath $expected

$pluginHash = (Get-FileHash -LiteralPath $pluginPath -Algorithm SHA256).Hash
$coreHash = (Get-FileHash -LiteralPath $corePath -Algorithm SHA256).Hash
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Remove-Item -LiteralPath $staging -Recurse -Force

Write-Host "Created package: $zipPath"
Write-Host "Contents:"
$expected | ForEach-Object { Write-Host "  $_" }
Write-Host "SHA256 SharedDiscovery.dll: $pluginHash"
Write-Host "SHA256 SharedDiscovery.Core.dll: $coreHash"
Write-Host "SHA256 package: $zipHash"
