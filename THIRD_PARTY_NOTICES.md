# Third-party notices

The plugins in this repository use only what ships with .NET plus the following, which is used for building and testing and is not
part of what you install. Each is distributed under its own license; this file is a summary, not a replacement for the license texts.

| Package | License | Used by |
|---|---|---|
| `xunit`, `xunit.runner.visualstudio` | Apache-2.0 | OBS tests only |
| `Microsoft.NET.Test.Sdk` | MIT | OBS tests only |

The plugins build against the Macro Station plugin SDK (`MacroStation.Plugin.Abstractions`) from the
[macro-station](https://github.com/Deccoyi/macro-station) repository, which is MIT licensed. The OBS plugin speaks the obs-websocket v5
protocol; it does not include any OBS code.

When you add a dependency, check its license and add it here. Do not add a dependency under a copyleft license (GPL, AGPL and similar)
without first checking that it is compatible with this repository's MIT license.
