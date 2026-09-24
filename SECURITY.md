# Security policy

## What to expect from these plugins

- **C# plugins run in-process with the server.** A C# plugin is loaded into the Macro Grid server process with full .NET access: it can
  do anything the server can, including reading and writing your files and starting programs. It sits in its own assembly load context
  so it cannot break other plugins, but that is not a security boundary. Install only C# plugins whose source you trust, including the
  ones in this repository.
- **JavaScript plugins are sandboxed.** They have no .NET access and only the permissions you approve, with time and memory limits per
  call; the sandbox is described in [docs/plugin-authoring.md](docs/plugin-authoring.md#7-javascript-plugins).
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

This is a small project maintained in spare time, so there is no guaranteed response time, but reports are taken seriously.

## Supported versions

Only the latest released version of each plugin (or, before the first release, the `dev` branch) receives fixes.
