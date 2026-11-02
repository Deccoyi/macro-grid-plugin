# Changelog (developer) — WeatherPlugin

This file follows the [Keep a Changelog](https://keepachangelog.com/) format. Only what a plugin author, integrator or maintainer
needs and git history cannot carry — everything else is in commit messages and pull request descriptions. The short, public
changelog is [CHANGELOG.md](CHANGELOG.md).

## [Unreleased]

## [0.1.0] - 2026-11-02
### Added
- First release. JavaScript plugin, id `weather`, built for SDK 1.4.0 (`minMacroGrid` `1.4.0`). Variables `weather.temperature`, `weather.code` and `weather.wind`; one widget `weather` (setting `unit`).
- Permissions: `variables` and `http:api.open-meteo.com:443`.
- The folder was named `HelloWeather` (id `helloweather`, variables `helloweather.*`) before this release; that example was never released, so nothing is migrated.
