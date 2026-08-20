# Reentry — Roadmap

Planned work, roughly grouped. Ordering is a loose priority, not a schedule or a
commitment to dates. Items are checked off as they ship (each lands a `CHANGELOG.md`
entry when it does).

## v0 — dogfood

- [x] Core inventory / snapshot / boot / tracker with fakes and Linux-runnable tests.
- [x] Unpackaged WinUI HUD + settings + autostart consent.
- [ ] **Daily-driver week** on a real Windows box: confirm logon race (Reentry first),
      HUD readability, Approved toggles, ENDSESSION snapshot, unexpected-boot path.
- [ ] Icon / tray polish and a screenshot for the README.

## Later

- [ ] **MSIX** (or a real installer) — Start-menu entry, clean uninstall that calls
      the same Cleanup path as `/uninstall`, optional store packaging. The v0
      ship vehicle stays the unpackaged single-file exe.
- [ ] **In-app update** — check GitHub Releases and apply so dogfooders do not
      re-download by hand each alpha.
- [ ] **Explorer-tab detail** — we still will not parse Explorer's private session
      files; if a supported public API appears, show which Explorer windows/tabs
      we *saw* last session versus which are interactive now.
- [ ] ARM64 publish asset alongside win-x64.
- [ ] Optional "don't show HUD on ordinary logon" already exists; add a quiet
      period / auto-dismiss when every row is Interactive.
