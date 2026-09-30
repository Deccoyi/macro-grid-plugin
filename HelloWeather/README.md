# Hello Weather (widget example)

A plugin whose widget shows data from the internet, in the safe way: **only the plugin talks to the network** (it asks a weather service every
15 minutes and publishes the answer as variables), and the widget, which runs in a box with no network, just draws those variables. Copy this folder
to start a widget of your own that needs online data. The rules for widgets are in [../docs/plugin-authoring.md](../docs/plugin-authoring.md), section "Custom widgets".

License: MIT, see [LICENSE](LICENSE) and [NOTICE.md](NOTICE.md). This plugin was created by AI tools and is provided "as is", without warranty of any kind; the authors accept no responsibility or liability for it, and you use it at your own risk.

Install it from the editor (Plugins, Install from Folder). It asks for the `variables` permission and to send web requests to the weather service (`api.open-meteo.com:443`).
Set the place in the plugin's settings (latitude and longitude), then drag "Weather" from the Toolbox onto a page.
Needs Macro Grid 1.4.0 or newer.
