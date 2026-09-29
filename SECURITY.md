# Security policy

## What to expect from these plugins

- **Only official, signed C# plugins run.** A C# plugin runs in-process with the server with full .NET access, so Macro Grid loads one only
  when it carries a valid signature of the official plugin key, checked every time it loads; the signature covers every file of the plugin. The
  official plugins in this repository are built and signed by the maintainer, and the key stays on the maintainer's PC. They still run with full
  trust: install only the ones you want.
- **Plugins by other authors are JavaScript.** They run in a sandbox with no .NET access and only the permissions you approve, with time and memory
  limits per call; the sandbox is described in [docs/plugin-authoring.md](docs/plugin-authoring.md#7-javascript-plugins). Macro Grid does not
  review them, and shows them as third-party. A JavaScript plugin can only send web requests to the exact `http:<host>:<port>` addresses it declares,
  and the permission shown before you approve it says whether each one is on this computer, your local network or the internet. The `input`
  permission (pressing keys and typing) still needs your approval and works only while you press one of the plugin's buttons, with small limits and
  never into a terminal or a system tool; an approved plugin with it can still type up to 200 characters into an ordinary program when you press its
  button, so allow it only for plugins you trust. Report a problem in another author's plugin to that author.
- **Local network only.** Macro Grid is designed for a trusted local network (your PC and your phone). It is not meant to be exposed to
  the internet, and neither are the plugins that talk to it. Plugins that connect to other software (for example the OBS plugin
  connecting to OBS Studio) do so with the settings and passwords you enter; keep those services on your local machine or network too.
- **No warranty, no liability.** This software was created entirely by AI tools, is beta-stage and has not been independently audited or security-reviewed (see the [README](README.md)). It is provided "as is", without warranty of any kind, and the authors and contributors accept no responsibility or liability for it, including for security problems and their consequences (see the [MIT license](LICENSE)). You use it entirely at your own risk. Security reports are welcome, but they create no obligation to fix and are not a promise of support or of a response time.

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

This is a hobby project, so nothing here is a promise. If a reported vulnerability gets fixed, the fix may be described in a GitHub
security advisory on this repository and under "Security" in the plugin's changelog.

## Supported versions

There is no support period and no promise of fixes: these plugins are a hobby project maintained in spare time. If a fix is made, it only
goes into a new version of that plugin; older versions are not updated. Before the first release, fixes land on the `dev` branch.

## What a release contains

Each C# plugin release has a software bill of materials (SBOM) attached, `<id>-<version>.cdx.json` (CycloneDX), listing the packages inside
the zip (the plugin SDK is not in the zip; the server supplies it). JavaScript plugins contain no packages. A release is refused when one of
those packages has a known vulnerability, and every pull request runs the same check.
