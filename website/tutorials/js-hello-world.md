# Tutorial 1: JavaScript hello world

In this tutorial you build the smallest useful JavaScript plugin: a **counter variable**, a **settings page**, one **action**
that bumps the counter, and a **status bar item**. You need only a text editor and a running Macro Grid server; there is nothing
to build or compile.

The finished plugin lives in the repository as [`examples/hello-js`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/examples/hello-js)
(a copy of [`HelloJs/`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/HelloJs), kept identical by CI). Every code block
below is taken from that folder.

## Step 1: the folder and the manifest

Create a folder `hello-js` with a file `plugin.json`:

<<< @/../examples/hello-js/plugin.json

- `id` must be unique and stable. Variable names and action types must start with `<id>.` (here `hellojs.`).
- `kind` is `js`, and `entry` is the script to run.
- `permissions` lists what the script needs. `variables` lets it publish variables, `actions` lets it register actions. The user
  has to approve exactly this list before the script runs.
- `macroGrid` says the oldest Macro Grid this plugin runs on (see [Compatibility](/basics/compatibility)).

## Step 2: a variable

Create `index.js` next to it. The script runs in a sandbox and reaches the server only through the global `host` object. Start
with a counter variable:

<<< @/../examples/hello-js/index.js#variable

`host.variables.describe` lists the variable in the editor's variable picker so users do not have to guess names, and
`host.variables.set` publishes its first value. Any widget can now show it in its text as `Count: {hellojs.count}`.

## Step 3: a settings page

<<< @/../examples/hello-js/index.js#settings

`host.settings.page` adds a settings form to the Plugins window from a list of fields. The values are stored in a
`settings.json` in the plugin's folder and read back with `host.settings.get()`.

## Step 4: an action

<<< @/../examples/hello-js/index.js#action

`host.registerAction` adds an action to the editor's action picker. `fields` describes the action's own form (here a `Times`
number); `run` is called when the action fires, with the values of that form as `settings`. Here it adds
`step * times` to the counter and publishes the new value.

::: tip Register at the top level
Actions, the settings page and variable descriptions must be registered while the script first runs. Registrations made later,
from a callback, are ignored. Variables can be set at any time.
:::

## Step 5: a status item

<<< @/../examples/hello-js/index.js#status

`host.every(ms, fn)` repeats a function (the shortest interval is 100 ms) and `host.status(id, text, level)` updates an entry
you own in the editor's status bar. Timers need no permission.

The whole script:

<<< @/../examples/hello-js/index.js

## Step 6: install it

1. In the editor open **Plugins, Manage Plugins...** and choose **Install from Folder...**
2. Pick the `hello-js` folder. It is copied to `%AppData%\MacroGrid\plugins\hellojs\`.
3. The plugin shows as **Needs approval**. Read the permission list and approve it. The plugin starts.

## Step 7: use it

1. Add a button to a page. Set its text to `Count: {hellojs.count}` (the variable is also in the `{...}` picker, category
   *Hello*).
2. Bind an action to its **press** event: pick **Bump the counter** (category *Hello*) and set *Times*.
3. Save, open the deck and press the button. The text counts up.
4. Open the plugin's gear button in the Plugins window and change **Step**: each press now adds `step * times`.
5. Look at the right of the status bar: `Count <n>` updates every five seconds.

## Step 8: change it and reload

Edit the script and try again without restarting the server. There are two ways:

- Change your source folder and use **Install from Folder...** again. A folder with the same `id` replaces the installed plugin,
  and the plugin's own `settings.json` is kept.
- Or edit the installed copy in `%AppData%\MacroGrid\plugins\hellojs\index.js` and click **Reload** in the Plugins window.

If the script throws while it first runs, the plugin shows as **Error** with the message under its name; fix it and reload.
See [Debugging and logs](/guides/debugging).

## What to try next

- Read the [JavaScript host API reference](/reference/js-host-api), for example `host.http.get` to poll a local service (needs an
  `http:<host>:<port>` permission).
- Continue with [Tutorial 3](/tutorials/live-data) to format your variable in widget text.
- Write the same idea in C#: [Tutorial 2](/tutorials/csharp-hello-world).
