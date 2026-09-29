# Changelog (developer) — PLC Icons

This file tracks the version of this plugin only (independent of the main program — see the "Independent versions" section of `../CONTRIBUTING.md`). The short, public changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]

## [0.1.4] - 2026-09-29
### Changed
- `plugin.json` declares `"minMacroGrid": "1.0.0"` (the field's new name; built against SDK 1.0.0). The old `sdkVersion` / `minServerVersion` stay so servers before 1.0.0 still load it.
- Package format: the release zip now also carries `signature.json` (`id`, `version`, `kind` and the SHA-256 of every file) and `signature.sig` in its root, written by `scripts/release-plugin.ps1`. Macro Grid versions that enforce official-only C# plugins check them every time the plugin loads and refuse a plugin whose files do not match; older versions ignore the two files. No manifest, setting, action or variable changed.

## [0.1.3] - 2026-09-25
### Changed
- `sdkVersion` is now `^0.4.0` (`Directory.Build.props`'s `MacroGridSdkVersion` moved to 0.4.0 for the whole repository) and `minServerVersion` is `0.3.2`, the first server that ships SDK 0.4.0: an older server no longer loads this plugin. The SDK bump brings nothing this plugin uses yet.
- Manifest gained the optional `description`, `author` and `homepage` fields that the plugin catalog (Discover) shows.

## [0.1.2] - 2026-09-24
### Added
- **Plugin languages:** the pack name is English (`defaultLanguage: "en"`) and `locales/tr.json` translates it to Turkish; the file is copied into the build output.
- `NOTICE.md` and `LICENSE` in the plugin folder, copied into the build output. `NOTICE.md` and the README record the provenance of the icons: all 29 are original AI-generated artwork under the repository's MIT license; none shares path data with the Lucide set (checked against 0.460.0 and 1.47.0). Documentation and packaging only, no behavior change, no version bump.

## [0.1.1] - 2026-09-23
### Added
- Two new icons: `open-branch`, `close-branch` (branch split/merge).
### Fixed
- The `p` icon now uses `currentColor` for `stroke`/`fill` instead of `#000` — it was inconsistent with all the other icons, so tinting in the editor's icon picker did not work on it.

## [0.1.0] - 2026-09-23
### Added
- First release: 27 ladder-logic icons (coil, timers, comparison and arithmetic blocks), added to the editor's icon picker as a "PLC Icons" category through `IIconPackSource`.
