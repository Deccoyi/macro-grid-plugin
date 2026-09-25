# Contributing to the plugins repository

> **AI-generated software.** All code, design and documentation of this project, including this file, were created by artificial
> intelligence at the maintainer's direction. It is alpha-stage, has not been reviewed line by line by a human or security-audited, and is
> provided "as is", without warranty of any kind. You use it entirely at your own risk (see the [README](README.md) and the
> [MIT license](LICENSE)).

Thanks for helping. This file is the set of rules every plugin in this repository follows. For how to write a plugin, read
[docs/plugin-authoring.md](docs/plugin-authoring.md) first.

By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md). Security problems go through private vulnerability
reporting, see [SECURITY.md](SECURITY.md); for anything else, open an issue.

## This is a hobby project

Macro Grid is maintained in spare time. Issues and pull requests are welcome, but replies and reviews can take a while, and there is no
promise that a request will be accepted or a pull request merged. Please be patient, and don't expect support on a schedule.

## Branching model

- `main` holds released code only. Releases are tagged on `main` (see [Releases and tags](#releases-and-tags)).
- `dev` is the integration branch: open pull requests against `dev`.
- Keep pull requests small and about one thing. CI (build and OBS tests) must pass.

## Building and testing

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Until the plugin SDK is published as a NuGet package, the C#
plugins reference it by path, so clone this repository next to the server repository (`macro-grid/` and `macro-grid-plugin/` in the
same folder). Then:

```powershell
dotnet build OBS\src\MacroGrid.Plugin.Obs.csproj
dotnet test  OBS\tests\MacroGrid.Plugin.Obs.Tests\MacroGrid.Plugin.Obs.Tests.csproj
dotnet build PLCIcons\src\MacroGrid.Plugin.PlcIcons.csproj
```

## Layout

```
macro-grid-plugin/
├── README.md
├── CONTRIBUTING.md
├── SECURITY.md
├── CODE_OF_CONDUCT.md
├── LICENSE                      MIT; each plugin folder also has its own LICENSE and NOTICE.md
├── docs/plugin-authoring.md
├── docs/release.md
├── .github/                     CI, release workflow, issue and pull request templates
└── <PluginName>/
    ├── plugin.json              the manifest
    ├── README.md                what it does, requirements, settings
    ├── CHANGELOG.md             short, public, for non-developers
    ├── CHANGELOG-developer.md   detailed, technical
    └── src/                     the source (a C# project, or the script for a JavaScript plugin)
```

## Independent versions

- Every plugin has its own semantic version (`MAJOR.MINOR.PATCH`) in its `plugin.json`, independent of the server's version and of
  every other plugin's. A plugin's version moves only in that plugin's own changelogs; a server release never changes it.
- A plugin does not depend on another plugin's version. One can be at `3.0.0` while another is at `0.1.0`.
- Bump a MAJOR version only for a breaking change: action types or settings that change meaning, so that a user's saved profile
  would silently stop working.

## Isolation

- A plugin folder never references another plugin's folder: no `ProjectReference`, `file:` dependency, relative import or path into
  a sibling. Deleting one plugin folder must never stop anything else from building.
- Code two plugins both need is copied into each, or published as its own package and referenced as a package. Never shared by path.
- Plugins talk to each other only at run time, through the host's shared interfaces (for example a variable one plugin publishes and
  another reads), never through a compile-time dependency.

## The manifest and compatibility

Every plugin has a `plugin.json` at its root; the fields are described in [docs/plugin-authoring.md](docs/plugin-authoring.md#2-pluginjson).
`sdkVersion` (a caret range against the plugin SDK) and `minServerVersion` are checked when the server loads the plugin, and a plugin
that does not fit is shown as incompatible instead of being loaded. Set them honestly: `sdkVersion` to the SDK you built and tested
against, `minServerVersion` to the oldest server that has what you use.

## C# and JavaScript plugins

- **C#** plugins have full trust. They are loaded into their own assembly load context so they cannot break other plugins, but they
  are not sandboxed.
- **JavaScript** plugins are sandboxed and need approved permissions. Ask only for the permissions you use.
- A plugin folder is one or the other, never both.

## Language, comments and names

- Code, identifiers, comments, documentation, changelogs, log messages and commit messages are written in **English**. Text the user
  sees in the editor (action names, form labels) goes through the plugin's own strings.
- Do not mention third-party product or brand names in code, comments, documentation or commits, except where the product is the
  functional target of the plugin itself (for example the OBS plugin talking to OBS).

## Changelogs

When a change is finished, update the changelogs of the plugin you changed, under `[Unreleased]`:

- `CHANGELOG.md`: one short, plain sentence per change under "New / Changed / Fixed". No code, file or API names, and leave out small
  bug fixes and internal changes (tests, refactors, tooling).
- `CHANGELOG-developer.md`, in [Keep a Changelog](https://keepachangelog.com/) style, **only** for changes to settings, action types or variable names that could break a
  saved profile, the SDK version the plugin is built for, permission changes, migrations, or anything a plugin author or user has to do differently. The rest belongs in
  the commit message and the pull request description. Entries already there stay as they are.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/) in English: `type(scope): description`, with the plugin as the scope, for
example `feat(obs): pause reconnecting while OBS is not running`. Group related changes into one commit and do not mix unrelated ones.

## Releases and tags

A plugin is released by pushing a tag named `plugin-<name>-v<version>` on `main`, for example `plugin-obs-v0.2.0`. The tag version must
match `version` in the plugin's `plugin.json`. The release workflow builds the plugin, hashes and signs the zip, publishes the release,
and updates `macrogrid-index.json` on `main`. Details and the checklist are in [docs/release.md](docs/release.md); the index format is
in [website/reference/source-index.md](website/reference/source-index.md).

## Tests

A plugin with logic should have tests next to it (see `OBS/tests`, which drives the plugin against a fake obs-websocket server). Run
`dotnet test` on the plugin's test project before you open a pull request.

## Issues and labels

Open an issue from the [chooser](https://github.com/Deccoyi/macro-grid-plugin/issues/new/choose): pick a form, or its plain-text twin (the same questions, written as
text you fill in). Questions and ideas start in [Discussions](https://github.com/Deccoyi/macro-grid-plugin/discussions); a maintainer turns one into an issue when there is
something to fix or build. Security problems go to the private form, never to a public issue.

What the labels mean. New issues get `needs-triage` and the area on their own; the maintainer sets the rest.

| Label | Meaning |
|---|---|
| `bug`, `enhancement`, `documentation` | The kind of work. |
| `regression` | It worked in an earlier version. |
| `plugin: obs` | The OBS plugin |
| `plugin: plc-icons` | The PLC Icons plugin |
| `plugin: hello-js` | The JavaScript example plugin |
| `area: sdk` | The plugin SDK and its docs |
| `area: store` | The plugin store website |
| `area: docs-site` | The documentation website |
| `needs-triage` | Not looked at yet (automatic). |
| `needs-info` | We asked a question and wait for the reporter. |
| `confirmed` | Reproduced or accepted by a maintainer. |
| `in progress` | Someone is working on it. |
| `priority: high` | Blocks people: a crash, lost data or a broken install. |
| `good first issue`, `help wanted` | A good place to start, or where help is welcome. |
| `duplicate`, `invalid`, `wontfix` | Closing reasons; the closing comment says why. |
