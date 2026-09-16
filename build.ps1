param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$CopyToProfile
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$envProps = Join-Path $root "Environment.props"

if (-not (Test-Path $envProps)) {
    Write-Error "Environment.props was not found. Copy Environment.props.example to Environment.props and set VALHEIM_INSTALL, BEPINEX_PATH, and MOD_DEPLOYPATH."
}

[xml]$props = Get-Content $envProps
$propertyGroup = $props.Project.PropertyGroup
$valheimInstall = [string]$propertyGroup.VALHEIM_INSTALL
$bepInExPath = [string]$propertyGroup.BEPINEX_PATH
$deployPath = [string]$propertyGroup.MOD_DEPLOYPATH

function Expand-LocalProperty {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    return $Value.
        Replace('$(VALHEIM_INSTALL)', $valheimInstall).
        Replace('$(BEPINEX_PATH)', $bepInExPath).
        Replace('$(MOD_DEPLOYPATH)', $deployPath)
}

$bepInExPath = Expand-LocalProperty $bepInExPath
$deployPath = Expand-LocalProperty $deployPath

function Require-File {
    param(
        [string]$Label,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path)) {
        Write-Error "${Label} not found:`n$Path"
    }
}

function Require-Directory {
    param(
        [string]$Label,
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path) -or -not (Test-Path $Path -PathType Container)) {
        Write-Error "${Label} not found:`n$Path"
    }
}

Require-Directory "Valheim install directory" $valheimInstall
Require-Directory "BepInEx directory" $bepInExPath
Require-Directory "BepInEx core directory" (Join-Path $bepInExPath "core")

Require-File "Valheim assembly_valheim.dll" (Join-Path $valheimInstall "valheim_Data\Managed\assembly_valheim.dll")
Require-File "Valheim assembly_utils.dll" (Join-Path $valheimInstall "valheim_Data\Managed\assembly_utils.dll")
Require-File "Valheim Newtonsoft.Json.dll" (Join-Path $valheimInstall "valheim_Data\Managed\Newtonsoft.Json.dll")
Require-File "UnityEngine.dll" (Join-Path $valheimInstall "valheim_Data\Managed\UnityEngine.dll")
Require-File "UnityEngine.CoreModule.dll" (Join-Path $valheimInstall "valheim_Data\Managed\UnityEngine.CoreModule.dll")
Require-File "BepInEx.dll" (Join-Path $bepInExPath "core\BepInEx.dll")
Require-File "0Harmony.dll" (Join-Path $bepInExPath "core\0Harmony.dll")

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Error "dotnet was not found on PATH."
}

$sdkList = & dotnet --list-sdks
if (-not $sdkList) {
    Write-Error "No .NET SDK is installed. Install a .NET SDK that can build SDK-style C# projects, then run this script again."
}

& dotnet build (Join-Path $root "SharedDiscovery.sln") -c $Configuration

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if ($CopyToProfile) {
    if ([string]::IsNullOrWhiteSpace($deployPath)) {
        Write-Error "CopyToProfile was requested, but MOD_DEPLOYPATH is empty in Environment.props."
    }

    $pluginDir = $deployPath
    New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
    Copy-Item -Force (Join-Path $root "src\SharedDiscovery\bin\$Configuration\net462\SharedDiscovery.dll") $pluginDir
    Copy-Item -Force (Join-Path $root "src\SharedDiscovery.Core\bin\$Configuration\net462\SharedDiscovery.Core.dll") $pluginDir
    Write-Host "Copied SharedDiscovery to $pluginDir"
}
