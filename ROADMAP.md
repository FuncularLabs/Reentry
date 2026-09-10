# Reentry — Roadmap

Planned work, roughly grouped. Ordering is a loose priority, not a schedule or a
commitment to dates. Items are checked off as they ship (each lands a `CHANGELOG.md`
entry when it does).

**Public story stays the personal local HUD** (MIT, LocalAppData-only, no phone-home).
Anything under *Path to fleet later* is an honest build-toward for IT/MSP GTM — not a
promise that Reentry is a fleet product today. Do not soft-link fleet GTM from the
README until at least one of those items ships in a form a rollout can use.

## v0 — dogfood

- [x] Core inventory / snapshot / boot / tracker with fakes and Linux-runnable tests.
- [x] Unpackaged WinUI HUD + settings + autostart consent.
- [x] **Produce Checklist…** — local markdown/text export of the live HUD (md + sibling PNG, or txt). Dogfood once before Blog 684.
- [ ] **Daily-driver week** on a real Windows box: confirm logon race (Reentry first),
      HUD readability, Approved toggles, ENDSESSION snapshot, unexpected-boot path.
- [ ] Icon / tray polish and a screenshot for the README.
- [ ] Timing stats (last / current / avg for restore, startup, and each row) with a
      local SQLite history under `%LOCALAPPDATA%\Reentry`.
- [ ] Reliable Hung vs Interactive for tray / background apps (Dropbox, Everything, …)
      that stay up without a normal visible HWND.
- [ ] Always-visible scrollbars on the HUD lists.

## Later (personal product)

- [ ] **MSIX / MSI installer** — Start-menu entry, clean uninstall that calls the same
      Cleanup path as `/uninstall`, optional store packaging. The v0 ship vehicle
      stays the unpackaged single-file exe; the installer is also the on-ramp to
      silent / Intune-friendly packaging below.
- [ ] **In-app update** — check GitHub Releases and apply so dogfooders do not
      re-download by hand each alpha.
- [ ] **Explorer-tab detail** — we still will not parse Explorer's private session
      files; if a supported public API appears, show which Explorer windows/tabs
      we *saw* last session versus which are interactive now.
- [ ] ARM64 publish asset alongside win-x64.
- [ ] Optional "don't show HUD on ordinary logon" already exists; add a quiet
      period / auto-dismiss when every row is Interactive.

## Path to fleet later (monetizable, not vapor)

These are the features that make a future IT/MSP offer real. Ship order is rough;
personal HUD keeps shipping in parallel. No paywall in the public build until a
company/seat path exists.

- [ ] **Silent / unattended install** — MSI (and/or MSIX) with quiet switches,
      per-user or per-machine, Intune / SCCM friendly. Builds on the installer item
      above; no admin elevation beyond what the package itself needs.
- [ ] **Policy templates** — declare what restores (or is suppressed) per user /
      machine class (JSON or ADMX-backed). Personal HUD remains the default when no
      policy is present.
- [ ] **Org / multi-machine profile export-import** — portable managed-entry +
      settings bundle a tech can seed onto another box. Still local files; no cloud
      account required for v1 of this.
- [ ] **Restore audit log** — IT-readable local log of boot kind, what was expected,
      what settled / failed / hung, and timings. Exportable (CSV / JSON). Stays on
      the machine unless the admin copies it.
- [ ] **Company mode vs personal HUD** — optional mode that favors quiet restore +
      audit over the always-on-top dogfood HUD. Personal remains the public default.
- [ ] **Seat / entitlement hook** — detect a license / seat later without shipping a
      paywall in the public MIT build now. Hook only; no phone-home requirement for
      personal use.
- [ ] **MDM / GPO ADMX** — policy templates consumable by Intune / GPO for the
      knobs above (autostart, company mode, restore policy path). In scope as the
      distribution surface for policy; out of scope as a hosted MDM service.

## Out of scope (kill / do not roadmap)

- Parsing Outlook / Chrome / Explorer private session files.
- Kernel hooks, injection, or anything that needs admin to "force" other apps up.
- Hosted MDM / cloud control plane / phone-home telemetry as a product dependency.
- Shipping a paywall or breaking personal MIT use before company mode + seat hook exist.
- Claiming "fleet ready" in README / store listing until silent install + at least
  one of policy / audit / company mode actually ships.