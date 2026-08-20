# Publishes Reentry as a framework-dependent Windows app into .\publish\.
# Requires the .NET 10 Desktop Runtime on the target machine.
# Requires the Windows App SDK 2.4 runtime (WindowsAppSDKSelfContained=false; the self-contained 2.4 CoreMessagingXP payload fail-fasts on Windows 11 25H2).
#
# Usage: pwsh ./publish.ps1                 (framework-dependent .NET, small)
#        pwsh ./publish.ps1 -SelfContained  (bundles the .NET runtime, large but portable)

param(
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$out = Join-Path $PSScriptRoot "publish"

$args = @(
    "publish", (Join-Path $PSScriptRoot "src/Reentry.App/Reentry.App.csproj"),
    "-c", "Release",
    "-r", "win-x64",
    "-o", $out,
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:DebugType=none",
    "-p:DebugSymbols=false"
)

if ($SelfContained) {
    $args += @("--self-contained", "true")
} else {
    $args += @("--self-contained", "false")
}

Write-Host "Publishing Reentry ($([bool]$SelfContained ? 'self-contained' : 'framework-dependent'))..." -ForegroundColor Cyan
& dotnet @args

Write-Host "`nDone -> $out\Reentry.exe" -ForegroundColor Green
