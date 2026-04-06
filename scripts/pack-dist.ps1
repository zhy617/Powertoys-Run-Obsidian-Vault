# Pack x64 and ARM64 Release outputs into dist/ as ObsidianVaults-v{version}-win-{rid}.zip
# Syncs plugin.json "Version" when you pass -Version (otherwise reads version from plugin.json).
# Usage: .\scripts\pack-dist.ps1 -Version 1.0.0
#        .\scripts\pack-dist.ps1 -Version v2.3.4 -Build
#        .\scripts\pack-dist.ps1 -Build   # uses Version from plugin.json
param(
    [Parameter(Position = 0)]
    [AllowEmptyString()]
    [string] $Version,

    [switch] $Build
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$PluginRoot = Join-Path $RepoRoot 'src\Community.PowerToys.Run.Plugin.ObsidianVaults'
$PluginJsonPath = Join-Path $PluginRoot 'plugin.json'
$DistDir = Join-Path $RepoRoot 'dist'

function Set-PluginJsonVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $NewVersion
    )
    if ($NewVersion -match '["\r\n]') {
        throw 'Version must not contain quotes or line breaks.'
    }
    $content = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $pattern = '("Version"\s*:\s*")[^"]*(")'
    if ($content -notmatch $pattern) {
        throw "Could not find Version field in: $Path"
    }
    $updated = [regex]::Replace($content, $pattern, {
            param($m)
            return $m.Groups[1].Value + $NewVersion + $m.Groups[2].Value
        })
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText((Resolve-Path -LiteralPath $Path), $updated, $utf8NoBom)
}

$syncPluginJsonToRelease = $false
if ([string]::IsNullOrWhiteSpace($Version)) {
    $plugin = Get-Content -LiteralPath $PluginJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
    $Version = $plugin.Version.ToString().Trim().TrimStart('v')
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw 'Version in plugin.json is empty.'
    }
}
else {
    $Version = $Version.Trim().TrimStart('v')
    if ([string]::IsNullOrWhiteSpace($Version)) {
        throw 'Version must not be empty.'
    }
    Set-PluginJsonVersion -Path $PluginJsonPath -NewVersion $Version
    if (-not $Build) {
        $syncPluginJsonToRelease = $true
    }
}

if ($Build) {
    Push-Location $PluginRoot
    try {
        dotnet build -c Release -p:Platform=x64
        dotnet build -c Release -p:Platform=ARM64
    }
    finally {
        Pop-Location
    }
}

if (Test-Path -LiteralPath $DistDir) {
    Remove-Item -LiteralPath $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

$x64Release = Join-Path $PluginRoot 'bin\x64\Release'
$arm64Release = Join-Path $PluginRoot 'bin\ARM64\Release'

$pairs = @(
    [pscustomobject]@{ ReleaseDir = $x64Release; Rid = 'win-x64' }
    [pscustomobject]@{ ReleaseDir = $arm64Release; Rid = 'win-arm64' }
)

foreach ($p in $pairs) {
    $releaseDir = $p.ReleaseDir
    if (-not (Test-Path -LiteralPath $releaseDir)) {
        throw "Release output not found (build Release first): $releaseDir"
    }
    $items = @(Get-ChildItem -LiteralPath $releaseDir -Force)
    if ($items.Count -eq 0) {
        throw "Release folder is empty: $releaseDir"
    }
    if ($syncPluginJsonToRelease) {
        Copy-Item -LiteralPath $PluginJsonPath -Destination $releaseDir -Force
    }

    $zipName = "ObsidianVaults-v$Version-$($p.Rid).zip"
    $zipPath = Join-Path $DistDir $zipName
    $glob = Join-Path $releaseDir '*'
    Compress-Archive -Path $glob -DestinationPath $zipPath -Force
    Write-Host "Created: $zipPath"
}

Write-Host "Done. Output: $DistDir"
