# Using the SDK package

C# plugins compile against `MacroGrid.Plugin.Abstractions`. There are two ways to get it; the plugin projects in this
repository support both.

## Default: NuGet package

Nothing to set up. `dotnet build` restores `MacroGrid.Plugin.Abstractions` (version in `MacroGridSdkVersion`,
`Directory.Build.props`) from nuget.org, so a clean clone of this repo builds on its own. `nuget.config` lists nuget.org only.

In your own plugin project:

```xml
<PackageReference Include="MacroGrid.Plugin.Abstractions" Version="1.0.0"
                  PrivateAssets="all" ExcludeAssets="runtime" />
```

`ExcludeAssets="runtime"` keeps the SDK dll out of the plugin's output. The server and all plugins share the server's copy
of that assembly; shipping your own makes type checks like `is IActionHandler` fail. Test projects are the exception: they
need the dll at runtime, so they reference the package without `ExcludeAssets`.

## Working on both repos: local SDK

If you have the server repo checked out next to this one (`..\macro-grid`) and are changing the SDK and a plugin together,
build against the sibling source instead of the package:

```powershell
dotnet build OBS/src -p:UseLocalSdk=true
dotnet test OBS/tests/MacroGrid.Plugin.Obs.Tests -p:UseLocalSdk=true
```

`UseLocalSdk` defaults to `false`. To make it stick on your machine, pass it from an environment variable
(`$env:UseLocalSdk = "true"`), and do not commit a changed default.

## Testing an unpublished SDK build

Pack the SDK into a folder and add that folder as a source:

```powershell
dotnet pack ..\macro-grid\src\MacroGrid.Plugin.Abstractions -c Release -o C:\local-feed
dotnet build OBS/src -p:RestoreSources=C:\local-feed
```

Alternatively uncomment the `local-sdk` line in `nuget.config` (do not commit that).

## Versions

Package version = the Macro Grid version = `PluginSdk.Version` in the server repo (one number for both). A plugin's `plugin.json` says
`"macroGrid": "1.0.0"`: the oldest Macro Grid it runs on. The build checks that it has the same MAJOR as `MacroGridSdkVersion` and is not
newer than it; the minor and patch may be lower, which lets the plugin run on more servers. When you start to use something added in a newer
MINOR, bump `MacroGridSdkVersion` in `Directory.Build.props` and raise `macroGrid` in that plugin's `plugin.json` to match. After a MAJOR
release of Macro Grid, every plugin is rebuilt against it.
