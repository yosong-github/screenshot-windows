# Builds screenshot-windows-setup-<version>.exe from the published single-file exe.
# Version comes from Directory.Build.props. Requires Inno Setup 6 (ISCC.exe).
param(
    [string]$PublishDir = "publish",
    [string]$OutputDir = "artifacts"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$propsPath = Join-Path $root "Directory.Build.props"
$props = Get-Content -Raw -Path $propsPath
if ($props -notmatch '<Version>\s*([^<\s]+)\s*</Version>') {
    throw "Directory.Build.props has no <Version>."
}

$version = $Matches[1]
$parts = @($version.Split('.') | Where-Object { $_ -ne "" })
while ($parts.Count -lt 4) {
    $parts += "0"
}
$fileVersion = ($parts[0..3] -join ".")

$publishFull = [IO.Path]::GetFullPath((Join-Path $root $PublishDir))
$exe = Join-Path $publishFull "screenshot-windows.exe"
if (-not (Test-Path $exe)) {
    throw "Missing $exe. Publish the app before building the installer."
}

$outputFull = [IO.Path]::GetFullPath((Join-Path $root $OutputDir))
New-Item -ItemType Directory -Force -Path $outputFull | Out-Null

$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) {
        $iscc = $cmd.Source
    }
}
if (-not $iscc) {
    throw "ISCC.exe not found. Install Inno Setup 6, or run: choco install innosetup -y"
}

$iss = Join-Path $PSScriptRoot "screenshot-windows.iss"
$publishDefine = $publishFull
if ($publishDefine.Contains(" ")) {
    $publishDefine = '"' + $publishDefine + '"'
}

& $iscc `
    "/DMyAppVersion=$version" `
    "/DMyAppFileVersion=$fileVersion" `
    "/DPublishDir=$publishDefine" `
    "/O$outputFull" `
    "/Fscreenshot-windows-setup-$version" `
    $iss
if ($LASTEXITCODE -ne 0) {
    throw "ISCC exited with $LASTEXITCODE"
}

Write-Host "Wrote $(Join-Path $outputFull "screenshot-windows-setup-$version.exe")"
