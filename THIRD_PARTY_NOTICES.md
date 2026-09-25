# Third-party notices

This repository's own code and assets are MIT licensed, see [LICENSE](LICENSE) (copyright (c) 2026 Deccoyi).

**What ships:** most plugins contain no third-party code or assets — their build output is only their own code, plus their `LICENSE`
and `NOTICE.md`. The Sound plugin is the exception: it ships NAudio (see below) inside its build output, because shared-mode WASAPI
playback and audio file decoding are not something .NET provides on its own. Every other component below is used to build or test
only, and is not distributed with a plugin. The original license texts are kept in [licenses/](licenses/). This file is an index, not
a replacement for those texts.

| Name | Version | License (SPDX) | Copyright | URL | License text | Used by | Shipped |
|---|---|---|---|---|---|---|---|
| Macro Grid plugin SDK (`MacroGrid.Plugin.Abstractions`) | project reference | MIT | (c) 2026 Deccoyi | https://github.com/Deccoyi/macro-grid | [licenses/macro-grid-plugin-sdk/LICENSE](licenses/macro-grid-plugin-sdk/LICENSE) | all C# plugins (build time) | no |
| NAudio (`NAudio`, `NAudio.Core`, `NAudio.Wasapi`, `NAudio.WinMM`, `NAudio.Midi`, `NAudio.Asio`, `NAudio.WinForms`) | 2.2.1 | MIT | © Mark Heath 2023 | https://github.com/naudio/NAudio | [licenses/naudio/LICENSE.txt](licenses/naudio/LICENSE.txt) | Sound plugin | **yes** |
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
- The Sound plugin's `NAudio.WinForms`, `NAudio.Midi` and `NAudio.Asio` sub-packages come along as NAudio's own dependencies but are
  not used by the plugin's code (it only opens a shared-mode WASAPI output and decodes audio files); they are still shipped because
  NuGet resolves the whole `NAudio` metapackage as one unit, and all of it is the same MIT license.

When you add a dependency, check its license, add the original license text under `licenses/<name>/` and a row here. Do not add a
dependency under a copyleft license (GPL, AGPL and similar) without first checking that it is compatible with this repository's MIT
license.
