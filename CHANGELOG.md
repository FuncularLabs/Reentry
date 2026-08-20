# Changelog

All notable changes to Reentry are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/), and releases follow [SemVer](https://semver.org/).

## [Unreleased]

## [0.1.0-alpha1] - 2026-08-20

First scaffold — a Windows startup / session-restore monitor.

### Added
- **`Reentry.Core`** (`net10.0`) — settings and paths (`REENTRY_DATA_DIR`), startup
  inventory (Run / RunOnce / Wow6432Node / Startup folders + StartupApproved overlay),
  last-session snapshot store, boot classifier (User32 1074 / 6008 / Kernel-Power 41),
  tracker state machine (Pending / Starting / Interactive / Failed / Hung / Disabled),
  managed-entry sidecar, autostart registration contract.
- **`Reentry.App`** — unpackaged WinUI 3 HUD (always-on-top), settings, first-run
  consent, tray icon, single-instance, `RegisterApplicationRestart`, ENDSESSION snapshot.
- **Tests** — xUnit fakes covering inventory+Approved merge, snapshot round-trip,
  boot classifier, tracker, managed sort, and settings paths.
- CI + signed release workflows mirroring Aperture (Azure Trusted Signing).

### Notes
- Windows has no public pending-restore list; restore rows are inferred from our
  own last-session snapshot. We do not parse Outlook / Chrome / Explorer session files.
- Framework-dependent build; requires the **.NET 10 Desktop Runtime**.
