# Changelog (developer)

Repository-level developer log. Plugins keep their own `CHANGELOG-developer.md` next to `plugin.json`.

## [Unreleased]
### Changed
- **Macro Grid 1.0.0 (one version for the server and the SDK):** `macroGrid` in `plugin.json` (`MAJOR.MINOR.PATCH`, the oldest Macro Grid the plugin runs on; it runs on every later version of the same MAJOR) replaces `sdkVersion` and `minServerVersion`, which stay as optional legacy fields. The four official plugins keep the old pair next to `macroGrid` so servers before 1.0.0 still load them. `MacroGridSdkVersion` is 1.0.0, and the build (`build/Plugin.props`) fails when `macroGrid` has another MAJOR than the SDK or is newer than it. `scripts/update-plugin-index.ps1` writes `macroGrid` into the index (still `formatVersion` 1, so older servers keep reading it); `release-plugin.ps1` refuses a manifest without it. The Store shows "Macro Grid X.Y.Z or newer". See the server repository's `docs/guides/release.md` and `docs/guides/versioning.md`.
- Refactor with identical behavior: OBS actions and connection split by concern with shared base classes, plugin csproj boilerplate shared through `build/Plugin.props`, unused code and usings removed, `.editorconfig` added. Details in `docs/refactor-notes.md`.
- Official releases are built, hashed (`<zip>.sha256`), signed (`<zip>.sig`, ECDSA P-256) and published from the maintainer's machine with `scripts/release-plugin.ps1`, so the signing key never reaches GitHub. The tag-triggered `release.yml` workflow is gone; its signing-free version is `examples/third-party-release.yml`, a template for other plugin repositories.

### Added
- `docs/engineering-guidelines.md`, and a golden-file test for the OBS action catalog.
- `macrogrid-index.json` at the repository root, committed to `main` by `scripts/release-plugin.ps1` after each release (`scripts/update-plugin-index.ps1`). Format and the signature scheme are documented in `website/reference/source-index.md`. This is what a host or another plugin repository reads to browse and install from this repository without using the GitHub API.
- Optional manifest fields `description`, `author`, `homepage` (additive; set on all three plugins here).
