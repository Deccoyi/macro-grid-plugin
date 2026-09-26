# Security policy

## What to expect from these plugins

- **C# plugins run in-process with the server.** A C# plugin is loaded into the Macro Grid server process with full .NET access: it can
  do anything the server can, including reading and writing your files and starting programs. It sits in its own assembly load context
  so it cannot break other plugins, but that is not a security boundary. Install only C# plugins whose source you trust, including the
  ones in this repository.
- **JavaScript plugins are sandboxed.** They have no .NET access and only the permissions you approve, with time and memory limits per
  call; the sandbox is described in [docs/plugin-authoring.md](docs/plugin-authoring.md#7-javascript-plugins).
- **Plugins by other authors are not reviewed by this project.** Macro Grid can install plugins from other repositories and shows them as
  third-party. Such a C# plugin can connect to the internet and send data, and Macro Grid cannot limit or check that. A JavaScript plugin can
  only send web requests to the exact `http:<host>:<port>` addresses it declares, and the permission shown before you approve it says whether
  each one is on this computer, your local network or the internet. Report a problem in another author's plugin to that author.
- **Local network only.** Macro Grid is designed for a trusted local network (your PC and your phone). It is not meant to be exposed to
  the internet, and neither are the plugins that talk to it. Plugins that connect to other software (for example the OBS plugin
  connecting to OBS Studio) do so with the settings and passwords you enter; keep those services on your local machine or network too.
- **No warranty, no liability.** This software was created entirely by AI tools, is alpha-stage and has not been independently audited or security-reviewed (see the [README](README.md)). It is provided "as is", without warranty of any kind, and the authors and contributors accept no responsibility or liability for it, including for security problems and their consequences (see the [MIT license](LICENSE)). You use it entirely at your own risk. Security reports are welcome, but they create no obligation to fix and are not a promise of support or of a response time.

## Reporting a vulnerability

Please report security problems privately, not in a public issue: use GitHub's private vulnerability reporting (the **Security** tab of
the repository, then **Report a vulnerability**).

Helpful details: which plugin or SDK part is affected, the steps to reproduce, and what a plugin or a network attacker could do. A way
for a JavaScript plugin to get past its permissions or its limits is a vulnerability.

For anything that is not a security report, open a normal issue.

## This is a hobby project

Macro Grid is a hobby project maintained in spare time, not a full-time job or a commercial product. Security reports are read and the
maintainer will try to fix real problems, but there is no guaranteed response time, no guaranteed fix, no support schedule and no bug
bounty. Fixes land when there is time for them. If that is not acceptable for how you use the software, do not rely on it.

## How fixes are announced

When a reported vulnerability is fixed, the fix is described in a GitHub security advisory on this repository and under "Security" in the
plugin's changelog.

## Supported versions

Only the latest released version of each plugin (or, before the first release, the `dev` branch) receives fixes. A version stops receiving
fixes as soon as a newer one is released; there is no longer support period.

## What a release contains

Each C# plugin release has a software bill of materials (SBOM) attached, `<id>-<version>.cdx.json` (CycloneDX), listing the packages inside
the zip (the plugin SDK is not in the zip; the server supplies it). JavaScript plugins contain no packages. A release is refused when one of
those packages has a known vulnerability, and every pull request runs the same check.
