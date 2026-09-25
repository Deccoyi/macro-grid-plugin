# Project rules

Read `CONTRIBUTING.md` first: it covers plugin versioning, isolation, the manifest and the SDK compatibility rules.

## Language
- Everything in the project is written in **English**: code, identifiers (variables, functions, types, files), comments, documentation, changelogs, log messages and commit messages. Do not use Turkish in any of them.
- User-facing UI text is the only exception, and only through the plugin's own settings/label strings that the editor displays.
- Existing Turkish text is migrated gradually: when you come across Turkish in code, comments, identifiers or docs while working or reviewing, translate it on the way (only the part you are already touching). Do not start a dedicated bulk-translation pass or a translation-only agent.
- The conversation with the user is in Turkish. That does not affect anything written into the project.

## Changelogs
- Each plugin keeps two changelogs next to its `plugin.json`: `CHANGELOG.md` (short, public, for non-developers) and `CHANGELOG-developer.md` (only what developers need and git history cannot carry). A plugin's version moves only in its own changelogs.
- `CHANGELOG.md` uses short, simple sentences, one line per change ("New / Changed / Fixed"). No code, file or API names. Leave out small bug fixes and stability or internal improvements (tests, refactors, tooling).
- `CHANGELOG-developer.md` records only: changes to settings, action types or variable names that could break a saved profile, the SDK version the plugin is built for, permission changes (JavaScript plugins), migrations, and anything a plugin author or user has to do differently. Everything else (how it was built, refactors, internal details, small fixes) goes in the commit message and the pull request description, not in this file. Entries already there stay as they are.
- When a change is finished, update `CHANGELOG.md` under `[Unreleased]`, and `CHANGELOG-developer.md` only if the change is one of the kinds above. See the `commit-all` skill.

## Versions, releases and signing
- Any version, release, tag or signing work: read the central guide first, `docs/guides/release.md` in the server repository (`macro-grid`, https://github.com/Deccoyi/macro-grid/blob/main/docs/guides/release.md). It has the tag table, the order of a release and the signing keys. `docs/release.md` here only has the plugin release script steps; do not copy the shared content into it.
- A plugin's `version` is its own; `macroGrid` in `plugin.json` is the oldest Macro Grid it runs on (three parts, same MAJOR as the SDK it is built against, never newer than it).
- A plugin release (`scripts/release-plugin.ps1 -Publish`) signs with a key that stays on the maintainer's PC and publishes outward: ask the owner before each one, and never print or copy the key.

## Commits
- Conventional Commits (`type(scope): description`), always in English.

## Names
- Never mention third-party product or brand names in code, comments, docs or commits, except where they are the functional integration target itself (for example the OBS plugin talking to OBS). Describe patterns generically.
