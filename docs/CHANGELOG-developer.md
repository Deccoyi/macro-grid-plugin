# Changelog (developer)

Repository-level developer log. Plugins keep their own `CHANGELOG-developer.md` next to `plugin.json`.

## [Unreleased]
### Changed
- Refactor with identical behavior: OBS actions and connection split by concern with shared base classes, plugin csproj boilerplate shared through `build/Plugin.props`, unused code and usings removed, `.editorconfig` added. Details in `docs/refactor-notes.md`.
- `release.yml` publishes releases directly instead of drafts, and now hashes (`<zip>.sha256`) and signs (`<zip>.sig`, ECDSA P-256) every package. The workflow fails if the `PLUGIN_SIGNING_PRIVATE_KEY` repository secret is missing.

### Added
- `docs/engineering-guidelines.md`, and a golden-file test for the OBS action catalog.
- `macrogrid-index.json` at the repository root, committed to `main` by `release.yml` after each release (`scripts/update-plugin-index.ps1`). Format and the signature scheme are documented in `website/reference/source-index.md`. This is what a host or another plugin repository reads to browse and install from this repository without using the GitHub API.
- Optional manifest fields `description`, `author`, `homepage` (additive; set on all three plugins here).
