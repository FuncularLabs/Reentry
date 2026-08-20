# Publishing to GitHub

Repo: **`github.com/FuncularLabs/Reentry`**.
CI (`.github/workflows/ci.yml`) and a signed release pipeline (`release.yml`) are in the tree.

## Pre-flight
- `LICENSE` — MIT, **© 2026 Funcular Labs**.
- `Directory.Build.props` / csproj — `Version` 0.1.0-alpha1, `Company`/`Authors`/`Copyright` = Funcular Labs.
- `README.md` — public README with Aperture-style badges.
- `.gitignore` — excludes `bin/`, `obj/`, `publish/`.
- No secrets in the tree. Data lives under `%LOCALAPPDATA%\Reentry` (or `REENTRY_DATA_DIR`).

## Do-before-push checklist
- [x] Copyright holder set to **Funcular Labs** (LICENSE + props).
- [x] Version set to **0.1.0-alpha1**.
- [x] CI + signed release workflows added.
- [ ] **Add this repo to the org `AZURE_*` Actions secret scope (Selected repositories).**
      The three secrets (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`) live
      at the FuncularLabs org level and are scoped to **Selected repositories**. A new
      repo does not get them automatically — add **Reentry** on each secret
      (Org → Settings → Secrets and variables → Actions) or the release job's fail-fast
      check stops it. If the secrets are not scoped in, `azuretrustedsigntool` hangs on
      "Submitting digest for signing…" (it silently falls back to absent Azure CLI creds).

## Code signing (Authenticode) — reuse the Funcular Labs Trusted Signing account
Reentry reuses the **same** Azure Trusted Signing setup as Aperture and Markdown Midget —
Trusted Signing has no exportable per-product key; one **certificate profile** carries the
*publisher* identity (Funcular Labs) and signs any number of that publisher's products.

- Account: `func-az-artifact-signing` · Profile: `funcular-labs-public-trust` · Endpoint: `https://cus.codesigning.azure.net/`
- Tool: `azuretrustedsigntool` (dotnet global tool). Auth: the existing service principal via
  `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_CLIENT_SECRET`. That SP already holds the
  *Trusted Signing Certificate Profile Signer* role, so **no RBAC change** is required — the
  only per-app change is `--description "Reentry"` (and `--file`).
- The release workflow publishes a framework-dependent single-file `Reentry.exe`, signs it,
  and attaches `Reentry-vX-win-x64.exe` to a GitHub Release built from `CHANGELOG.md`.

## Cutting a release
1. Ensure `CHANGELOG.md` has a `## [version]` section for the version.
2. Tag and push: `git tag v0.1.0-alpha1 && git push origin v0.1.0-alpha1`.
3. The `Release` workflow builds → tests → publishes → **signs** → creates the (pre)release.
   (`-alpha`/`-beta`/`-rc` tags are marked as prereleases automatically.)
