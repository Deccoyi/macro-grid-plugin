# Changelog (developer) — PLC Icons

This file tracks the version of this plugin only (independent of the main program — see `../agent-and-repo-rules.md`, item 1). The short, public changelog is [CHANGELOG.md](CHANGELOG.md).

## [0.1.1] - 2026-09-23
### Added
- Two new icons: `open-branch`, `close-branch` (branch split/merge).
### Fixed
- The `p` icon now uses `currentColor` for `stroke`/`fill` instead of `#000` — it was inconsistent with all the other icons, so tinting in the editor's icon picker did not work on it.

## [0.1.0] - 2026-09-23
### Added
- First release: 27 ladder-logic icons (coil, timers, comparison and arithmetic blocks), added to the editor's icon picker as a "PLC Icons" category through `IIconPackSource`.
