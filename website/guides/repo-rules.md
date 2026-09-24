# Repository rules

These are the rules every plugin in the [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) repository follows.
They are a good template for your own plugin repository too. The authoritative copy is
[CONTRIBUTING.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/CONTRIBUTING.md); it also covers the branching model and how
to open a pull request.

## Layout

```
macro-grid-plugin/
├── README.md
├── CONTRIBUTING.md
├── website/                   this documentation site
├── examples/                  the tutorial projects, built in CI
└── <PluginName>/
    ├── plugin.json              the manifest
    ├── README.md                what it does, requirements, settings
    ├── CHANGELOG.md             short, public, for non-developers
    ├── CHANGELOG-developer.md   detailed, technical
    └── src/                     the source (a C# project, or the script for a JavaScript plugin)
```

## Independent versions

- Every plugin has its own semantic version in its `plugin.json`, independent of the server's version and of every other plugin's. A
  plugin's version moves only in that plugin's own changelogs; a server release never changes it.
- A plugin does not depend on another plugin's version.
- Bump a MAJOR version only for a breaking change: action types or settings that change meaning, so that a user's saved profile would
  silently stop working.

## Isolation

- A plugin folder never references another plugin's folder: no `ProjectReference`, `file:` dependency, relative import or path into a
  sibling. Deleting one plugin folder must never stop anything else from building.
- Code two plugins both need is copied into each, or published as its own package and referenced as a package. Never shared by path.
- Plugins talk to each other only at run time, through the host's shared interfaces (for example a variable one plugin publishes and
  another reads), never through a compile-time dependency.

## The manifest and compatibility

Every plugin has a `plugin.json` at its root; the fields are in the [manifest reference](/reference/manifest). `sdkVersion` and
`minServerVersion` are checked when the server loads the plugin. Set them honestly.

## C# and JavaScript plugins

- **C#** plugins have full trust. They are loaded into their own assembly load context so they cannot break other plugins, but they are
  not sandboxed.
- **JavaScript** plugins are sandboxed and need approved permissions. Ask only for the permissions you use.
- A plugin folder is one or the other, never both.

## Language, comments and names

- Code, identifiers, comments, documentation, changelogs, log messages and commit messages are written in **English**. Text the user sees
  in the editor (action names, form labels) goes through the plugin's own strings.
- Do not mention third-party product or brand names in code, comments, documentation or commits, except where the product is the
  functional target of the plugin itself (for example the OBS plugin talking to OBS).

## Changelogs

Update both changelogs of the plugin you changed, under `[Unreleased]`, when a change is finished:

- `CHANGELOG-developer.md`: detailed and technical, in [Keep a Changelog](https://keepachangelog.com/) style (Added / Changed / Fixed).
- `CHANGELOG.md`: one short, plain sentence per change under "New / Changed / Fixed". No code, file or API names, and leave out small bug
  fixes and internal changes.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/) in English: `type(scope): description`, with the plugin as the scope, for
example `feat(obs): pause reconnecting while OBS is not running`.

## Tests

A plugin with logic should have tests next to it (see `OBS/tests`, which drives the plugin against a fake obs-websocket server). Run
`dotnet test` on the plugin's test project before you open a pull request.
