# Plugin source index

The host installs plugins from GitHub without ever calling the GitHub API (no rate limit): it reads a fixed
`raw.githubusercontent.com` URL for metadata and downloads release assets from a fixed `releases/download` URL. This
page is the format for both, and applies to the official repository and to any third-party repository people add as a
source.

## Multi-plugin repository: `macrogrid-index.json`

A repository that hosts more than one plugin (like this one) keeps an index file at its root, on `main`:

```
https://raw.githubusercontent.com/<owner>/<repo>/HEAD/macrogrid-index.json
```

```json
{
  "formatVersion": 1,
  "name": "Example Plugins",
  "author": "someone",
  "plugins": [
    {
      "id": "obs",
      "name": "OBS",
      "description": "One line shown in Discover.",
      "author": "someone",
      "homepage": "https://github.com/<owner>/<repo>/tree/main/OBS",
      "kind": "csharp",
      "versions": [
        {
          "version": "0.2.0",
          "sdkVersion": "^0.3.0",
          "minServerVersion": "0.2.0",
          "url": "https://github.com/<owner>/<repo>/releases/download/plugin-obs-v0.2.0/obs-0.2.0.zip",
          "sha256": "<hex>",
          "size": 123456,
          "permissions": [],
          "signature": "<base64, official source only>"
        }
      ]
    }
  ]
}
```

| Field | Meaning |
|---|---|
| `formatVersion` | Always `1` today. A host that does not understand a future version ignores that source rather than crashing. |
| `plugins[].id` / `.name` / `.description` / `.author` / `.homepage` | Shown in Discover before anything is downloaded. |
| `plugins[].kind` | `"csharp"` or `"js"`, matching `plugin.json`. |
| `versions[].sdkVersion` / `.minServerVersion` | Copied from the released `plugin.json` so the host can grey out an incompatible version without downloading it. |
| `versions[].url` | Must be `https://github.com/<same owner>/<same repo>/releases/download/...` — **an index can only point at its own repository's releases.** A host refuses any other host or repository. |
| `versions[].sha256` / `.size` | Required. Checked against the downloaded file before it is unzipped. |
| `versions[].permissions` | JavaScript only; empty array otherwise. Must match the zip's `plugin.json` exactly. |
| `versions[].signature` | Only meaningful for the official source (see below); other sources are third-party even when this is present. |

The host also checks, after download: the zip's own `plugin.json` (`id`, `version`, `sdkVersion`, `minServerVersion`, `kind`,
`permissions`) must equal the index entry exactly, or the install is refused.

## Single-plugin repository (a pasted link)

A repository with one plugin at its root has no index; the host reads `plugin.json` directly:

```
https://raw.githubusercontent.com/<owner>/<repo>/HEAD/plugin.json
```

That gives `id`, `version`, `sdkVersion`, `minServerVersion`, `kind` and `permissions` — enough to show compatibility before
downloading anything. The package and its hash come from fixed URLs derived from that `version`:

```
https://github.com/<owner>/<repo>/releases/download/v<version>/<id>-<version>.zip
https://github.com/<owner>/<repo>/releases/download/v<version>/<id>-<version>.zip.sha256
```

The zip's own `plugin.json` must equal the one read from `main`. If the repository has `macrogrid-index.json` instead of a root
`plugin.json`, the host offers to add it as a source (the multi-plugin flow above) rather than installing it directly.

## Official-source signing

The official repository ([`Deccoyi/macro-grid-plugin`](https://github.com/Deccoyi/macro-grid-plugin)) additionally signs every
release with a dedicated ECDSA P-256 key that never leaves the maintainer's machine:

- The release script (`scripts/release-plugin.ps1`) reads the zip's bytes, signs them with `ECDsa.SignData(bytes, HashAlgorithmName.SHA256)` (not the hex
  `sha256` string — the raw file bytes), and base64-encodes the result as `<zip>.sig`, an IEEE P1363 (`r`&#8203;`||`&#8203;`s`,
  64 bytes) signature.
- `macrogrid-index.json` carries the same base64 value in that version's `signature` field.
- The host embeds the matching public key and verifies with `ECDsa.VerifyData(bytes, signature, HashAlgorithmName.SHA256)`
  before installing anything from the official source. A package that fails verification is refused, not just warned about.
- A signature on a *non*-official source's entry does not make it official — only packages fetched through the built-in official
  source URL are verified and trusted that way. Everything else is always shown as third-party.

This is why the official releases are built and signed locally and the private key is never stored on GitHub: a compromised
GitHub account cannot produce a package the host accepts as official.

## What CI does for you

If your plugin is released with this repository's `scripts/release-plugin.ps1`, or you copy `examples/third-party-release.yml` into your own multi-plugin
repository (see [Publishing your plugin](/guides/publishing)), you never write `sdkVersion`, `minServerVersion`, `sha256`,
`size` or the download `url` into the index by hand: the release step fills them in from the build and from `plugin.json` after
each release and commits `macrogrid-index.json` back to `main`. You only keep `plugin.json` and the changelogs honest.
