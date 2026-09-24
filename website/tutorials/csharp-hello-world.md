# Tutorial 2: C# hello world

In this tutorial you build a small C# plugin from an empty folder: an **action** with a settings form, a **settings page**, a
**variable** and a **status bar item**. You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and a running Macro
Grid server.

The finished project is [`examples/hello-csharp`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/examples/hello-csharp),
and CI builds it, so the code below cannot rot. Every code block is taken from that folder.

::: tip Remember: a C# plugin has full trust
It runs inside the server process with full .NET access. Never install a C# plugin whose source you do not trust.
:::

## Step 1: create the project

```powershell
mkdir HelloCSharp
cd HelloCSharp
mkdir src
cd src
dotnet new classlib -n HelloCSharp -f net10.0 -o .
del Class1.cs
dotnet add package MacroGrid.Plugin.Abstractions --version 0.3.1
```

`MacroGrid.Plugin.Abstractions` is the plugin SDK: the interfaces your plugin implements. Open `HelloCSharp.csproj` and change the
reference that `dotnet add package` created so that it looks like this:

<<< @/../examples/hello-csharp/src/HelloCSharp.csproj#sdk{xml}

- **`ExcludeAssets="runtime"`** (with `PrivateAssets="all"`) keeps a copy of the SDK out of your output. The server and every
  plugin must share the server's copy of `MacroGrid.Plugin.Abstractions`; with two copies, a check such as `is IActionHandler`
  fails because the same type from two copies is two different types. Never ship your own copy.
- The `Condition` attributes and the second reference belong to the repository convention (see below); in your own plugin a single
  `PackageReference` is enough.

Also copy `plugin.json` into the build output, so that the output folder is directly installable:

```xml
<ItemGroup>
  <None Include="..\plugin.json" Link="plugin.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

The complete project file of the example, which follows the same `UseLocalSdk` / `MacroGridSdkVersion` convention as the plugins in
this repository (the SDK comes from NuGet by default; `dotnet build -p:UseLocalSdk=true` builds against a checkout of the server
repository next to this one):

<<< @/../examples/hello-csharp/src/HelloCSharp.csproj{xml}

::: warning Package availability
While the SDK package has not been published to nuget.org, pack it yourself and add the folder as a source:
`dotnet pack <server repo>\src\MacroGrid.Plugin.Abstractions -c Release -o C:\local-feed`, then
`dotnet build -p:RestoreSources=C:\local-feed`.
:::

Now create the manifest `HelloCSharp/plugin.json` (one level above `src`):

<<< @/../examples/hello-csharp/plugin.json

`entry` is the file name of your DLL (the `AssemblyName` plus `.dll`). `permissions` is only used by JavaScript plugins.

## Step 2: the entry point

The server finds exactly one class implementing `IPlugin` in the entry assembly, creates it with a parameterless constructor and
calls `Initialize` once. Everything you register there is applied when `Initialize` returns. Create `HelloPlugin.cs`:

<<< @/../examples/hello-csharp/src/HelloPlugin.cs#plugin{cs}

`IPluginHost` is what you register with. Here it creates a **status bar item** (`CreateStatusItem`), and registers a variable
provider, a settings page and an action. If `Initialize` throws, the server catches it, shows the plugin as *Error* and keeps running.

## Step 3: the action and its form

An action is an `IActionHandler` (`Type`, `DisplayName`, `ExecuteAsync`). If it also implements `IActionDescriptor`, the editor
lists it under a `Category` and draws its settings form from `Fields`, a list of `SettingField`. No UI code is needed. Create
`GreetAction.cs`:

<<< @/../examples/hello-csharp/src/GreetAction.cs#action{cs}

- `Type` must be unique. By convention it is `<plugin id>.<name>`. If it is already registered the whole plugin fails to load.
- `SettingField(key, label, kind)` declares one field. `SettingFieldKind` is `Text`, `Password`, `Number`, `Slider`, `Bool`,
  `Select` or `Segmented`. `AllowVariables = true` lets the user insert `{variables}` in a text field; you receive the raw
  template.
- `ExecuteAsync` receives an `ActionContext` (`DeviceId`, `PageId`, `WidgetId`, `Device`, and `Value` for a slider or knob) and
  the values of the form as a `JsonObject`. Throw an exception with a clear message on failure: the server logs it and shows it on
  the phone and in the editor's status bar.
- `host.Log` writes a line to the server's log, prefixed with your plugin id (see [Debugging and logs](/guides/debugging)).
- `status.Update(text, level)` changes the status bar item. Update it when the state changes, not at a high rate.

## Step 4: the settings page

A settings page is an `IPluginSettingsPage` (`Fields`, `Load()`, `Save(values)`). The editor draws the form; `Load` and `Save` are
your bridge to disk. Create `HelloSettingsPage.cs`:

<<< @/../examples/hello-csharp/src/HelloSettingsPage.cs#settings{cs}

`host.DataDirectory` is your own install folder (`%AppData%\MacroGrid\plugins\hellocsharp\`), writable without admin rights. A
plugin with a settings page gets a gear button in the Plugins window and its status item opens the page. More in
[Settings pages](/guides/settings-pages).

## Step 5: a variable

A variable provider publishes live values that widgets can show. Create `GreetingCounter.cs`:

<<< @/../examples/hello-csharp/src/GreetingCounter.cs#variables{cs}

`RunAsync` runs for as long as the plugin is loaded and should return only when the token is cancelled. `store.Set(name, value)`
publishes a value (a value equal to the current one is ignored). Implementing `IVariableCatalogSource` on the same object lists
the variable in the editor's variable picker. When the plugin is unloaded, the server removes every variable it set.

## Step 6: build

```powershell
dotnet build -c Release
```

The output folder `bin\Release\net10.0\` contains `HelloCSharp.dll` and `plugin.json`, and **no** `MacroGrid.Plugin.Abstractions.dll`.
If the SDK dll is there, check `ExcludeAssets="runtime"`.

## Step 7: install and try

1. In the editor open **Plugins, Manage Plugins...**, choose **Install from Folder...** and pick `bin\Release\net10.0\`.
2. The plugin loads immediately. A gear button appears: set **Name**, **Greeting** and **Loud**.
3. Add a button with the text `Greetings: {hellocsharp.count}` and bind the **Greet** action (category *Hello*) to its press event.
4. Press it. The counter goes up, the status bar shows `Hello: <n> greetings`, and the server log gets a line like
   `[hellocsharp] Hello, world! (pressed on device <id>)`.

## Step 8: package it

A C# plugin is distributed as its build output folder. Zip the contents of `bin\Release\net10.0\` (without the `.pdb`, if you
like) together with your `LICENSE`. Users unzip it into `%AppData%\MacroGrid\plugins\<id>\` or point **Install from Folder...**
at it. See [Publishing your plugin](/guides/publishing).

## What to try next

- Show and format your variable in widget text: [Tutorial 3](/tutorials/live-data).
- Read how a real integration is organised: [the OBS plugin](/guides/obs-plugin).
- Look up every interface: [C# SDK reference](/reference/csharp-sdk).
