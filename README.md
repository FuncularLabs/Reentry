# Reentry

[![Latest release](https://img.shields.io/github/v/release/FuncularLabs/Reentry?include_prereleases&sort=semver&color=blue&label=latest)](https://github.com/FuncularLabs/Reentry/releases)
[![Release date](https://img.shields.io/github/release-date-pre/FuncularLabs/Reentry?color=informational)](https://github.com/FuncularLabs/Reentry/releases)
[![Downloads](https://img.shields.io/github/downloads/FuncularLabs/Reentry/total?color=success)](https://github.com/FuncularLabs/Reentry/releases)
[![CI](https://github.com/FuncularLabs/Reentry/actions/workflows/ci.yml/badge.svg)](https://github.com/FuncularLabs/Reentry/actions/workflows/ci.yml)
[![Release](https://github.com/FuncularLabs/Reentry/actions/workflows/release.yml/badge.svg)](https://github.com/FuncularLabs/Reentry/actions/workflows/release.yml)

A Windows startup / session-restore monitor. Reentry starts first among user apps at
logon and shows pending, starting, interactive, failed, and hung app initializations
with elapsed time. Reentry-configured startup entries sort first and carry a
**Reentry-managed** badge.

> Built by [Funcular Labs](https://github.com/FuncularLabs).

---

## Honesty

Windows has **no public pending-restore list**. After a reboot, Explorer, Outlook,
Chrome, and friends reopen windows from private session files that we do not parse.

Reentry infers restore work from a **last-session snapshot we take ourselves**
(ARR-registered processes we can see, visible windows, plus the current startup
inventory) and matches that to live processes and windows. That is an inference,
not an official Windows API.

---

## What it does

- Always-on-top HUD after `/autostart` or an unexpected shutdown.
- Banner for boot kind: expected (User32 1074), unexpected (6008 / Kernel-Power 41), or ordinary.
- **Session restore (inferred)** and **Startup apps** sections.
- Settings: list inventory, enable/disable user-scope items via StartupApproved
  (the Run value is **not** deleted), add/remove a Reentry-owned user Run entry.
- First-run consent: *Start with Windows so we can show restore progress after a reboot.*
- Single-instance. Registers itself with `RegisterApplicationRestart`.
- `WM_QUERYENDSESSION` / `WM_ENDSESSION` write `last-session.json`.
- `/uninstall` (or `/cleanup`) deletes the Run value, StartupApproved value, and logon task.

Reentry never kills a hung app.

---

## Build & run

Requires **Windows** and the **.NET 10 SDK**. You cannot compile the WinUI head on Linux;
`Reentry.Core` (and its tests) target `net10.0` and are fakeable.

```powershell
dotnet run --project src/Reentry.App
dotnet test tests/Reentry.Core.Tests/Reentry.Core.Tests.csproj
```

Distributable single-file exe (needs the .NET 10 Desktop Runtime on the target):

```powershell
pwsh ./publish.ps1                 # framework-dependent (small)
pwsh ./publish.ps1 -SelfContained  # bundles the .NET runtime (portable, larger)
```

→ `publish/Reentry.exe`

The app is unpackaged (`WindowsPackageType=None`) and uses the installed Windows App SDK 2.4 runtime (`WindowsAppSDKSelfContained=false`). The self-contained 2.4 CoreMessagingXP payload fail-fasts on Windows 11 25H2.

---

## Data & privacy

Everything is local. Settings, the last-session snapshot, and the managed-entry
sidecar live in `%LOCALAPPDATA%\Reentry` (`settings.json`, `last-session.json`,
`managed-entries.json`). Delete that folder to reset. Set `REENTRY_DATA_DIR` to
relocate it. Nothing is uploaded; nothing phones home.

---

## Architecture

- **`Reentry.Core`** — paths, settings, inventory, StartupApproved merge, snapshot
  store, boot classifier, tracker state machine, managed-entry map. No UI, no Win32.
  `net10.0`, unit-tested with fakes.
- **`Reentry.App`** — WinUI 3 unpackaged HUD, settings, tray, CsWin32 / Task
  Scheduler implementations of the Core interfaces.
- **`Reentry.Core.Tests`** — xUnit. Not in `Reentry.slnx` (Aperture pattern);
  CI runs `dotnet test` on the csproj directly.

---

## Status

v0 scaffold (`0.1.0-alpha1`) — dogfood the HUD and inventory on a real Windows box.

## License

[MIT](LICENSE) © 2026 Funcular Labs.
