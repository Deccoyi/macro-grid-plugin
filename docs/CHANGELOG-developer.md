# Changelog (developer)

Repository-level developer log. Plugins keep their own `CHANGELOG-developer.md` next to `plugin.json`.

## [Unreleased]
### Changed
- Refactor with identical behavior: OBS actions and connection split by concern with shared base classes, plugin csproj boilerplate shared through `build/Plugin.props`, unused code and usings removed, `.editorconfig` added. Details in `docs/refactor-notes.md`.

### Added
- `docs/engineering-guidelines.md`, and a golden-file test for the OBS action catalog.
