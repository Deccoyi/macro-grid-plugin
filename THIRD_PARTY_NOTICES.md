# Third-party notices

This repository's own code and assets are MIT licensed, see [LICENSE](LICENSE) (copyright (c) 2026 Deccoyi).

**What ships:** none of the plugins contains third-party code or assets. Each plugin's build output contains only its own code, plus its
`LICENSE` and `NOTICE.md`. The components below are used to build or test only, and are not distributed with a plugin. The original
license texts are kept in [licenses/](licenses/). This file is an index, not a replacement for those texts.

| Name | Version | License (SPDX) | Copyright | URL | License text | Used by | Shipped |
|---|---|---|---|---|---|---|---|
| Macro Station plugin SDK (`MacroStation.Plugin.Abstractions`) | project reference | MIT | (c) 2026 Deccoyi | https://github.com/Deccoyi/macro-station | [licenses/macro-station-plugin-sdk/LICENSE](licenses/macro-station-plugin-sdk/LICENSE) | all C# plugins (build time) | no |
| xunit (`xunit`, `xunit.core`, `xunit.assert`, `xunit.abstractions`, `xunit.analyzers`) | 2.9.3 (abstractions 2.0.3, analyzers 1.18.0) | Apache-2.0 | (c) .NET Foundation and Contributors | https://github.com/xunit/xunit | [licenses/xunit/LICENSE.txt](licenses/xunit/LICENSE.txt) | OBS tests | no |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 | (c) .NET Foundation and Contributors | https://github.com/xunit/visualstudio.xunit | [licenses/xunit/LICENSE.txt](licenses/xunit/LICENSE.txt) | OBS tests | no |
| Microsoft.NET.Test.Sdk, Microsoft.TestPlatform.TestHost, Microsoft.TestPlatform.ObjectModel, Microsoft.CodeCoverage | 17.14.1 | MIT | (c) Microsoft Corporation | https://github.com/microsoft/vstest | [licenses/microsoft-test-platform/LICENSE.txt](licenses/microsoft-test-platform/LICENSE.txt) | OBS tests | no |
| Newtonsoft.Json (transitive, via the test host) | 13.0.3 | MIT | (c) 2007 James Newton-King | https://www.newtonsoft.com/json | [licenses/newtonsoft-json/LICENSE.md](licenses/newtonsoft-json/LICENSE.md) | OBS tests | no |

Notes:

- Versions are the ones resolved in `OBS/tests` at the time of writing; the `xunit.*` and `Microsoft.*` packages carry no license file in
  their NuGet package, so the texts in `licenses/` come from the upstream repositories. The Microsoft.CodeCoverage and
  Microsoft.TestPlatform.TestHost packages also include their own `ThirdPartyNotices.txt` for code bundled inside them.
- **PLC Icons:** all 29 icons are original AI-generated artwork under this repository's MIT license. They were compared with the Lucide
  icon set (ISC, versions 0.460.0 and 1.47.0) and no icon shares path data with it, so Lucide is not a component of this repository.
  If an icon is later added that is taken or derived from Lucide, add Lucide's ISC license (and, for icons derived from Feather, the
  Feather MIT text that Lucide's license file contains) to `licenses/lucide/` and list it here.
- The OBS plugin speaks the obs-websocket v5 protocol to a running OBS Studio. It contains no OBS or obs-websocket code.
- HelloJs and the plugin code of OBS and PLCIcons use only what ships with .NET or the plugin host.

When you add a dependency, check its license, add the original license text under `licenses/<name>/` and a row here. Do not add a
dependency under a copyleft license (GPL, AGPL and similar) without first checking that it is compatible with this repository's MIT
license.
