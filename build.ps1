param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'
$resolvedGameRoot = (Resolve-Path -LiteralPath $GameRoot).Path
$projectFile = Join-Path $PSScriptRoot 'src/WanderburgCoopTest.csproj'
$checksFile = Join-Path $PSScriptRoot 'tests/ConnectionChecks.csproj'

& dotnet build $projectFile -c Release "-p:WanderburgGameRoot=$resolvedGameRoot"
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
& dotnet run --project $checksFile -c Release
if ($LASTEXITCODE -ne 0) { throw 'Transport checks failed.' }

$sourceText = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'src/WanderburgCoopPlugin.cs')
$versionMatch = [regex]::Match($sourceText, 'PluginVersion\s*=\s*"([0-9.]+)"')
if (-not $versionMatch.Success) { throw 'Cannot read plugin version.' }
$version = $versionMatch.Groups[1].Value
$artifactRoot = Join-Path $PSScriptRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactRoot -Force | Out-Null
$zipPath = Join-Path $artifactRoot "WanderburgCoop-$version-local-prototype.zip"
$packageFiles = @(
    (Join-Path $PSScriptRoot 'src/bin/Release/net6.0/WanderburgCoopTest.dll'),
    (Join-Path $PSScriptRoot 'README.md'),
    (Join-Path $PSScriptRoot 'README.de.md'),
    (Join-Path $PSScriptRoot 'LICENSE'),
    (Join-Path $PSScriptRoot 'CHANGELOG.md'),
    (Join-Path $PSScriptRoot 'THIRD_PARTY_NOTICES.md')
)
Compress-Archive -LiteralPath $packageFiles -DestinationPath $zipPath -Force
Write-Output "Package ready: $zipPath"
