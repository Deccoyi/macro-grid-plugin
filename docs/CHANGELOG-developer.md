# Changelog (developer)

Repository-level developer log. Plugins keep their own `CHANGELOG-developer.md` next to `plugin.json`.

## [Unreleased]
### Changed
- Refactor with identical behavior: OBS actions and connection split by concern with shared base classes, plugin csproj boilerplate shared through `build/Plugin.props`, unused code and usings removed, `.editorconfig` added. Details in `docs/refactor-notes.md`.
- Official releases are built, hashed (`<zip>.sha256`), signed (`<zip>.sig`, ECDSA P-256) and published from the maintainer's machine with `scripts/release-plugin.ps1`, so the signing key never reaches GitHub. The tag-triggered `release.yml` workflow is gone; its signing-free version is `examples/third-party-release.yml`, a template for other plugin repositories.

### Added
- `docs/engineering-guidelines.md`, and a golden-file test for the OBS action catalog.
- `macrogrid-index.json` at the repository root, committed to `main` by `scripts/release-plugin.ps1` after each release (`scripts/update-plugin-index.ps1`). Format and the signature scheme are documented in `website/reference/source-index.md`. This is what a host or another plugin repository reads to browse and install from this repository without using the GitHub API.
- Optional manifest fields `description`, `author`, `homepage` (additive; set on all three plugins here).
