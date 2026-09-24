# Security policy

## What to expect from these plugins

A **C# plugin runs inside the Macro Grid server process with full .NET access**: it can do anything the server can, including reading and writing your files and starting programs. Install
only C# plugins whose source you trust, including the ones in this repository. **JavaScript plugins** run in a sandbox with no .NET access and only the permissions you approve, with time and
memory limits per call; the sandbox is described in [docs/plugin-authoring.md](docs/plugin-authoring.md#7-javascript-plugins). The software was written by an AI assistant and has not been
independently audited (see the [README](README.md)).

## Reporting a vulnerability

Please report security problems privately, not in a public issue: use GitHub's private vulnerability reporting (the **Security** tab of the repository, then **Report a vulnerability**). If that is not
available, open an issue that says only that you have a security report and ask for a private channel, without any details of the problem.

Helpful details: which plugin or SDK part is affected, the steps to reproduce, and what a plugin or a network attacker could do. A way for a JavaScript plugin to get past its permissions or its
limits is a vulnerability.

This is a small project maintained in spare time, so there is no guaranteed response time, but reports are taken seriously.

## Supported versions

Only the latest version of each plugin (or, before the first release, the `dev` branch) receives fixes.
