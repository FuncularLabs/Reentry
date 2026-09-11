# Changelog

All notable changes to Reentry are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/), and releases follow [SemVer](https://semver.org/).

## [Unreleased]

## [0.1.0-alpha1] - 2026-09-10

First public dogfood build of Reentry — a Windows startup / session-restore monitor.

### Added
- **HUD** — always-on-top WinUI surface: inferred last-session restore rows, startup inventory, progress bar, colored status chips, gear to Settings, tray menu.
- **Produce Checklist…** — tray / HUD / Settings export of a local markdown (plus sibling HUD PNG) or plain-text diagnostic. Default filename `reentry-checklist-YYYYMMDD-HHMM.md`. Nothing leaves the machine.
- **Demo screenshot list** — `/demo` or tray toggle fills Contoso-style plus familiar hung suspects (Outlook, Teams, dwm, rasman, Dropbox, …) for redaction-free captures.
- **Start with Windows** — first-run consent, per-user logon registration, Settings Save/Cancel back to the HUD.
- **Core** — settings under `%LocalAppData%\Reentry`, startup inventory (Run / RunOnce / Wow6432Node / Startup folders + StartupApproved), last-session snapshot, boot classifier (User32 1074 / 6008 / Kernel-Power 41), tracker state machine, managed-entry sidecar.
- Roadmap note: personal HUD first; *Path to fleet later* (silent install, policy, audit, company mode) stays aspirational.

### Changed
- Source chip for Application Restart and Recovery reads **Last session** (not `Arr`).
- List rows leave a right gutter so always-on Win11 scrollbars do not cover status chips.
- Folder `publish.ps1` keeps `Reentry.pri` beside the exe (required for WinUI `ms-appx` load).

### Fixed
- Interactive launch always shows the HUD (Settings-only left a headless process).
- Tray icon loads from `reentry.ico` / BitmapImage (System.Drawing.Icon path failed silently).
- Single-instance second launch activates the HUD on the UI thread.
- Caption `AppWindow.SetIcon`; in-place list sync (no 1 Hz Clear pulse).
- WASDK 2.4 on Windows 11 25H2: use installed runtime (`WindowsAppSDKSelfContained=false`).

### Notes
- Windows has no public pending-restore list; restore rows are inferred from our own last-session snapshot. We do not parse Outlook / Chrome / Explorer session files.
- Framework-dependent build; requires the **.NET 10 Desktop Runtime** and **Windows App SDK 2.4**.
- Downloads are Authenticode-signed via Azure Trusted Signing. **Windows SmartScreen may still warn** on a brand-new release until reputation accrues — use More info → Run anyway when you trust the Funcular Labs signature.