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
- A plugin's `version` is its own; `minMacroGrid` in `plugin.json` is the oldest Macro Grid it runs on (three parts, same MAJOR as the SDK it is built against, never newer than it). Called `macroGrid` up to Macro Grid 1.2.x; that name is still read as a fallback.
- A plugin release (`scripts/release-plugin.ps1 -Publish`) signs with a key that stays on the maintainer's PC and publishes outward: ask the owner before each one, and never print or copy the key.

## Commits
- Conventional Commits (`type(scope): description`), always in English.

## Names
- Never mention third-party product or brand names in code, comments, docs or commits, except where they are the functional integration target itself (for example the OBS plugin talking to OBS). Describe patterns generically.

## Local notes: macro-grid-library
Working notes are NOT in the public repos. They live in the private repo `macro-grid-library` (sibling folder of `macro-grid`, `macro-grid-client`, `macro-grid-plugin`) and are linked into each repo with directory junctions:
- `macro-grid/docs/agents/` (with `hidden/` and `proposals/`), `docs/done/`, `docs/releases/` -> `macro-grid-library/macro-grid/{agents,done,releases}/`
- `macro-grid-client/docs/agents/` -> `macro-grid-library/macro-grid-client/agents/`
- `macro-grid-plugin/docs/agents/`, `docs/done/` -> `macro-grid-library/macro-grid-plugin/{agents,done}/`

Rules:
- These paths are git-ignored. Never `git add -f` them, never link to them from public docs, never copy their content into public files.
- Session handoffs, refactor logs, unapproved proposals, finished plans and maintainer-only docs (triage answers, internal plans, mockups) go there. Public docs (`docs/design/`, `plans/`, `guides/`, `ui/`, `roadmap.md`, changelogs) stay in the public repos.
- Edit through the junction path (`docs/agents/...`); the change lands in `macro-grid-library`. Commit and push it THERE (`cd ../macro-grid-library`), not in the public repo. The user syncs two PCs through that repo.
- If a `docs/agents` path is missing or is a real folder, run `macro-grid-library/scripts/link.ps1` (`powershell -ExecutionPolicy Bypass -File ...`); move files out of a real folder first.
- Trap: if a file under a junction path is ever git-tracked, a rebase/checkout deletes it from `macro-grid-library` too. These paths are untracked now; before checking out an old commit or a branch that still tracks them, remove the junction (`rmdir docsgents`, this does not delete files). In `macro-grid-library`, run `git status` before `git add -A` and stop if it shows mass deletions; recover with `git checkout <old-commit> -- <path>`. Always `git pull` there before editing.
