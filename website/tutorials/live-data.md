# Tutorial 3: showing live data

A widget shows live data in its text through **variables**. Any plugin (and the server itself) publishes variables; you use them
in a widget's text as `{name}` and format them with `{name|format}`. This tutorial uses the counter from
[Tutorial 1](/tutorials/js-hello-world) (`hellojs.count`), the counter from [Tutorial 2](/tutorials/csharp-hello-world)
(`hellocsharp.count`) and the variables the server provides.

## Publishing a variable

A JavaScript plugin publishes with `host.variables.set(name, value)`. The value is a number, string, boolean or null. The name has
to start with your plugin id:

<<< @/../examples/hello-js/index.js#variable

A C# plugin implements `IVariableProvider` and calls `store.Set(name, value)`:

<<< @/../examples/hello-csharp/src/GreetingCounter.cs#variables{cs}

A value equal to the current one is ignored, so it is cheap to set a variable on every poll. When the value really changes, the
server renders only the widgets that use it and sends only the texts that changed (at most about ten updates per second).

## Using a variable in text

A widget's text is a template. Put the variable name in braces:

| Text | Shows |
|---|---|
| `Count: {hellojs.count}` | `Count: 3` |
| `CPU {system.cpu\|0}%` | `CPU 42%` |
| `{system.time\|HH:mm}` | `14:05` |
| `Live: {obs.stream.duration}` | `Live: 00:12:31` |

To print literal braces, double them: two opening braces give one `{` and two closing braces give one `}`.

In a text field of an action, the *insert variable* button appears when the plugin sets `AllowVariables` on the field. The server
resolves the `{variables}` before the action runs (for actions using the schema-driven form).

## Formatting

Add a format after a `|`: `{system.cpu|0}`, `{system.time|HH:mm}`.

- **Numbers** default to the format `0.##`. Use `{system.cpu|0}` for no decimals or `{system.cpu|0.0}` for one.
- **Dates and times** default to `HH:mm`. Use standard .NET custom date formats, for example `{system.time|HH:mm:ss}`.
- **Durations** default to `hh:mm:ss`.
- **Booleans** render as `On` / `Off` by default (`Açık` / `Kapalı` when the app runs in Turkish). Give a format to choose your own words:
  `{obs.streaming|ON/OFF}`.
- A **missing variable** renders as an empty string.

## Built-in variables

The server publishes `system.time`, `system.cpu`, `system.ram`, `system.ram.used`, `system.ram.total`, `system.uptime`,
`system.audio.master` and `system.audio.muted`. The OBS plugin adds about 45 `obs.*` variables. The editor's variable picker lists
what is available, including variables described with `host.variables.describe` (JavaScript) or `IVariableCatalogSource` (C#).

## Sliders and knobs

A slider or knob widget can name a variable as its `valueVariable`: it shows that variable's value, and when the user drags it
the action bound to `valueChange` runs with the dragged value in `ActionContext.Value` (C#) or `context.value` (JavaScript). That
is how a slider controls the Windows volume and follows it when it is changed elsewhere.

## Dynamic rules

A widget's color, text, icon or animation can depend on a variable through rules made in the editor (the lightning-bolt button
next to a field): "if `hellojs.count` is above 10 blink". Rules are plain data, conditions are comparisons (`>`, `>=`, `<`, `<=`,
`==`, `!=`, `between`) combined with `and`, `or`, `xor` and `not`; the first matching case wins. There is no expression language.
Properties that can be dynamic: background, foreground, border color, animation, icon and text.

## Try it

1. Install the plugin from Tutorial 1 and add three buttons: `Count: {hellojs.count}`, `CPU {system.cpu|0}%` and `{system.time|HH:mm}`.
2. Bind **Bump the counter** to the first one and press it.
3. Add a rule to the first button's background: above 5, red. Keep pressing until it turns red.

## Naming rules for variables

- A JavaScript plugin can only set names that start with `<plugin id>.`; it can never overwrite `system.cpu` or another plugin's values.
- For a name that depends on user data (an OBS input that can be deleted or renamed) call `store.Remove(name)` (C#) or
  `host.variables.remove(name)` (JavaScript) when it disappears, otherwise it stays in the picker forever.
