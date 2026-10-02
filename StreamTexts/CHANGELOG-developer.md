# Changelog (developer) — Stream Texts

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]
### Added
- First version. Built against SDK 1.3.1 (`minMacroGrid` 1.0.0). It publishes no variables and registers no actions; it only reads variables and writes files. Settings: `outputFolder`, `intervalMs`, `writeFiles`, `overlayEnabled`, `overlayPort` and the `texts` list (`name`, `template`).
- The optional browser source (`overlayEnabled`, off by default) opens a plain `HttpListener` on `127.0.0.1:<overlayPort>` (default 9830), outside the host's own ports. It serves `/t/<name>`, `/t/<name>.txt` and `/t/<name>/events` (server-sent events), GET only, for the configured names only. If the port is taken the plugin shows a warning status and retries after the next settings change.
