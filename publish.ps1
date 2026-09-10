# Publishes Reentry as a framework-dependent Windows app into .\publish\.
# Requires the .NET 10 Desktop Runtime and Windows App SDK 2.4 runtime
# (WindowsAppSDKSelfContained=false; self-contained 2.4 CoreMessagingXP fail-fasts on Win11 25H2).
#
# Default is a folder publish so Reentry.pri / *.xbf sit next to the exe — required for
# ms-appx:/// XAML LoadComponent. Single-file is optional and still needs EnableMsixTooling.
#
# Usage: pwsh ./publish.ps1
#        pwsh ./publish.ps1 -SelfContained
#        pwsh ./publish.ps1 -SingleFile

param(
    [switch]$SelfContained,
    [switch]$SingleFile
)

$ErrorActionPreference = "Stop"
$out = Join-Path $PSScriptRoot "publish"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$args = @(
    "publish", (Join-Path $PSScriptRoot "src/Reentry.App/Reentry.App.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "-o", $out,
    "-p:DebugType=none",
    "-p:DebugSymbols=false",
    "-p:EnableMsixTooling=true"
)

if ($SingleFile) {
    $args += @(
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true"
    )
} else {
    $args += @("-p:PublishSingleFile=false")
}

if ($SelfContained) {
    $args += @("--self-contained", "true")
} else {
    $args += @("--self-contained", "false")
}

$mode = if ($SingleFile) { "single-file" } else { "folder" }
$rt = if ($SelfContained) { "self-contained" } else { "framework-dependent" }
Write-Host "Publishing Reentry ($mode, $rt)..." -ForegroundColor Cyan
& dotnet @args
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }

$pri = Join-Path $out "Reentry.pri"
if (-not $SingleFile -and -not (Test-Path $pri)) {
    throw "Publish output missing Reentry.pri — HUD XAML will fail to load."
}

Write-Host "`nDone -> $out\Reentry.exe" -ForegroundColor Green
if (-not $SingleFile) {
    Write-Host "Dogfood: run $out\Reentry.exe (folder publish; keep .pri next to the exe)." -ForegroundColor Green
}