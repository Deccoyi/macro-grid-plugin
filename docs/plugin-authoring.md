# Plugin Yazma Rehberi

Bu doküman, `macro-station` server repo'sunda **Aşama 6**'da kurulan gerçek plugin loader'ına göre
yazılmıştır (bkz. `macro-station/src/MacroStation.Core/Plugins/` ve
`macro-station/src/MacroStation.Plugin.Abstractions/`). Genel kurallar için önce
[agent-and-repo-rules.md](../agent-and-repo-rules.md)'ye bakın — bu dosya onun "nasıl" kısmı.

> **Not:** Loader gerçek ve çalışıyor; ilk gerçek plugin ([OBS/](../OBS/)) de yazıldı. Bu doküman ve
> `OBS/`'nin kendisi, yeni bir plugin yazarken referans olarak kullanılabilir.

## 1. Klasör ve yükleme

Server, `%AppData%/MacroStation/plugins/<klasör>/` altındaki her klasörü tarar (editördeki "Eklentiler"
penceresi → "Klasörden Yükle…" bir kaynak klasörü buraya **kopyalar**, taşımaz). Bir klasör şu ikisini
içeriyorsa plugin olarak tanınır:

```
<klasör>/
├── plugin.json       zorunlu manifesto (aşağıda)
└── <entry dosyası>   plugin.json'daki "entry" alanında adı geçen DLL (csharp) ya da script (js)
```

Klasör adı önemli değil; kimlik `plugin.json`'daki `id` alanından gelir. Editördeki "Klasörden Yükle…"
düğmesi seçilen kaynağı `plugins/<id>/` altına kopyalar (aynı id ikinci kez yüklenemez — host reddeder).

**Yükleme:** sunucu açılırken `PluginManager` klasörü tarar; ayrıca editörden kurulan/yeniden yüklenen/kaldırılan
plugin'ler **sunucu yeniden başlatılmadan** devreye girer (bkz. "Hot loading, reload and unload" bölümü).

## 2. `plugin.json` şeması

```json
{
  "id": "obs",
  "name": "OBS Kontrolü",
  "version": "0.1.0",
  "sdkVersion": "^0.3.0",
  "minServerVersion": "0.1.0",
  "entry": "MacroStation.Plugin.Obs.dll",
  "kind": "csharp",
  "permissions": null
}
```

(Gerçek örnek — bkz. [OBS/plugin.json](../OBS/plugin.json).)

| Alan | Zorunlu | Açıklama |
|---|---|---|
| `id` | evet | Benzersiz, değişmez kimlik. Host aynı id'yi ikinci kez yüklemeyi reddeder. |
| `name` | evet | Editördeki "Eklentiler" listesinde gösterilen ad. |
| `version` | evet | Plugin'in kendi semver'i — sunucudan bağımsız (bkz. `macro-station/docs/versioning.md`). |
| `sdkVersion` | evet | Plugin SDK'sına (`MacroStation.Plugin.Abstractions`) karşı npm tarzı caret aralığı, örn. `"^0.1.0"`. Sunucudaki gerçek SDK sürümü bu aralığı karşılamıyorsa plugin **yüklenmez**, "Uyumsuz" olarak listelenir. |
| `minServerVersion` | evet | Gerekli asgari sunucu (Host) sürümü. Sunucu bundan eskiyse plugin **yüklenmez**. |
| `entry` | evet | `kind: "csharp"` için giriş DLL'inin dosya adı (klasörün köküne göre). `kind: "js"` için giriş script (genelde `index.js`). |
| `kind` | evet | `"csharp"` veya `"js"`. |
| `permissions` | hayır | Yalnızca `kind: "js"` için: script'in ihtiyaç duyduğu izinler. Kullanıcı editörde onaylamadan script çalışmaz (bkz. "JavaScript plugins"). |

Sürüm uyumluluk kuralları `macro-station/docs/versioning.md`'deki "Plugin uyumluluk beyanı" bölümüyle
birebir aynı — iki dosya arasında tutarsızlık fark ederseniz `versioning.md` esas alınır, burası ona göre
güncellenir.

## 3. C# plugin'i yazmak

Plugin projeniz `MacroStation.Plugin.Abstractions`'a (NuGet paketlenene kadar proje referansı ya da DLL
referansı olarak) referans verir ve `IPlugin`'i uygular:

```csharp
using System.Text.Json.Nodes;
using MacroStation.Plugin.Abstractions;

public sealed class AudioExtraPlugin : IPlugin
{
    public void Initialize(IPluginHost host)
    {
        host.RegisterAction(new PingAction());
        // host.RegisterVariableProvider(new MyVariableProvider());
    }
}

public sealed class PingAction : IActionHandler
{
    public string Type => "audioextra.ping";
    public string DisplayName => "Ping (örnek)";

    public Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken ct)
    {
        // context.Device üzerinden built-in aksiyonların kullandığı IDeviceController'a erişilebilir.
        return Task.CompletedTask;
    }
}
```

Notlar:
- `Initialize` içinde kaydettiğiniz her şey (`IActionHandler`, `IVariableProvider`) host tarafında
  toplanıp built-in olanlarla aynı şekilde DI container'a eklenir — çalışma zamanında aksiyon/değişken
  tipleri arasında bir fark yoktur.
- Plugin'inizin kendi ayarlarına (bağlantı bilgisi, API anahtarı, ...) ihtiyacı varsa `host.DataDirectory`
  (plugin'in kendi kurulum klasörü, yazılabilir) altına kendi `settings.json`'ınızı okuyup/yazın — ama
  formu artık **elle yazmanıza gerek yok** (SDK 0.3.0): `IPluginSettingsPage` uygulayan bir sınıfı
  `host.RegisterSettingsPage(page)` ile kaydedin, editör formu `page.Fields`'tan (bkz. aşağıdaki
  `SettingField` bölümü) otomatik çizer, `page.Load()`/`page.Save(values)` diskle sizin aranızdaki köprü
  olur. Kaydedilmiş bir sayfa yoksa host, eski jenerik ham-JSON köprüsüne düşer (`GET`/`PUT
  /api/plugins/{id}/settings`, doğrudan `settings.json`'ı okur/üzerine yazar) — yeni plugin'ler için önerilmez,
  yalnızca geriye dönük uyumluluk içindir. Gerçek örnek: `OBS/src/ObsSettings.cs`'teki `ObsSettingsPage`.

### `SettingField` — şema-tabanlı formlar

Bir `IActionHandler`, `IActionDescriptor`'ı da uygulayıp `Fields` alanında bir `SettingField[]` döndürürse
(action'ın `Category`/`Description`/`Icon`'uyla birlikte), editör o action'ın ayar formunu elle React kodu
yazmadan otomatik çizer (`SchemaForm.tsx`). Aynı `SettingField` tipi bir `IPluginSettingsPage.Fields` için
de kullanılır — ikisi aynı form motorunu paylaşır.

```csharp
public IReadOnlyList<SettingField> Fields =>
[
    new("sceneName", "Sahne", SettingFieldKind.Select) { OptionsSource = "scenes" },
    new("volume", "Ses seviyesi (%)", SettingFieldKind.Slider) { Min = 0, Max = 100, Default = 100 },
    new("text", "Metin", SettingFieldKind.Text) { AllowVariables = true },
];
```

`SettingFieldKind`: `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select`, `Segmented`. `Options` sabit
seçenekler içindir (`SettingOption[]`); `OptionsSource` + `DependsOn` dinamik bir liste içindir — bu
durumda handler'ınız (ya da settings page'iniz) ayrıca `IOptionsSource`'u uygular:

```csharp
public Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken ct)
{
    if (sourceId == "scenes") return Task.FromResult(new OptionsResult([.. myScenes.Select(s => new SettingOption(s, s))]));
    return Task.FromResult(new OptionsResult([], "bilinmeyen kaynak"));
}
```

`currentValues`, alanın `DependsOn` listesindeki anahtarların formdaki o anki değerleridir (ör. bir sahne
öğesi dropdown'u seçili sahneye göre değişir). `OptionsResult.Error` set edilirse editör onu kullanıcıya
gösterir (ör. "OBS'e bağlı değil") — dropdown boş kalır ama sessizce değil. Gerçek örnek: `OBS/src/ObsActions.cs`'teki
`ObsOptionSources` ve her action'ın `GetOptionsAsync`'i.

### Durum çubuğu

`host.CreateStatusItem("connection")` ile aldığınız `IPluginStatusItem`'ı `Update(text, level, icon?,
tooltip?)` ile güncelleyin (`StatusLevel`: `Idle`/`Ok`/`Busy`/`Warning`/`Error`) — editörün pencere-geneli
durum çubuğunda (sağ taraf) görünür, tıklanınca plugin'inizin ayar penceresini açar. Bağlantı durumu olan
her plugin (OBS gibi) bunu kullanmalı; her state değişiminde bir kez çağırmak yeterli, yüksek frekansta
çağırmayın.

### Kaybolan değişkenler: `IVariableStore.Remove`

Bir OBS input'u silinir/yeniden adlandırılırsa, o input için ürettiğiniz `obs.input.<slug>.muted` gibi
dinamik bir değişken sonsuza kadar `VariableStore`'da kalmamalı — `store.Remove(name)` ile açıkça silin
(bir `Set` gibi `Changed` event'i tetikler, ama değeri ve varlığını kaldırır). Adı sabit olmayan (kullanıcı
verisine göre üretilen) her değişken için geçerli.

### İkon paketleri

Bir plugin editörün ikon seçicisine kendi ikonlarını ekleyebilir: `IIconPackSource`'u (`Id`, `DisplayName`,
`IconNames`, `GetIconSvg(name)`) uygulayıp `host.RegisterIconPack(pack)` ile kaydedin. Seçicide paket ayrı bir
kategori olarak görünür. SVG'lerde `stroke="currentColor"` kullanın, editör seçilen rengi kök `<svg>`'ye
uygular. İkonları DLL'e `EmbeddedResource` olarak gömmek en basiti. Gerçek örnek: [PLCIcons/](../PLCIcons/).
- Loglamak için `host.Log(string)` kullanın (host'un dosya loguna plugin id'nizle etiketlenerek yazılır) —
  yoğun/sık tekrarlayan durumlar için değil, bağlantı durumu/hata gibi seyrek olaylar için.
- Bir sağlayıcı hem `IVariableProvider` hem `IVariableCatalogSource` uyguluyorsa (bkz. server repo'daki
  `SystemAudioProvider` örneği), `RegisterVariableProvider` ikisini de otomatik yakalar — ayrıca
  kaydetmenize gerek yok.
- Assembly'niz kendi izole `AssemblyLoadContext`'inde yüklenir (agent-and-repo-rules.md madde 4) — CLR'a
  tam erişiminiz var (sandbox değil), ama başka bir plugin'in bağımlılık sürümüyle çakışmazsınız.
  `MacroStation.Plugin.Abstractions` tek istisna: host ile plugin **aynı kopyayı** paylaşır (aksi halde
  `IPlugin`/`IActionHandler` gibi arayüz kontrolleri tip kimliği uyuşmazlığından sessizce başarısız
  olurdu) — bu paketi plugin'inizin çıktı klasörüne kendiniz kopyalamayın/farklı bir sürümünü taşımayın.
- Plugin `Initialize` içinde exception fırlatırsa host bunu yakalar, plugin'i "Hata" durumunda listeler,
  sunucuyu düşürmez.

## 4. Editörde görünüm

`GET /api/plugins` her klasörü şu durumlardan biriyle döner (editördeki "Eklentiler" → "Yüklü Eklentiler"
sekmesinde renkli bir nokta + `detail` metniyle gösterilir):

- **Loaded** — yüklendi, aksiyonları/değişkenleri devrede.
- **Incompatible** — `sdkVersion`/`minServerVersion` uyuşmuyor, (JS plugin'lerde: bilinmeyen izin ya da script hatası `Error`'dır).
- **NeedsApproval** — JS plugin'in istediği izinler henüz onaylanmadı; onaylanana kadar çalışmaz.
- **Error** — `plugin.json` ayrıştırılamadı, `entry` dosyası bulunamadı, id çakışması, ya da `Initialize`
  sırasında exception.

## JavaScript plugins

A plugin with `"kind": "js"` is one script (`entry`, usually `index.js`) that runs in a sandbox inside the server. Use it
for small integrations (poll a local HTTP API, publish a variable, add an action) without building a DLL. A complete
example is [../HelloJs/](../HelloJs/).

### Permissions

Declare what the script needs in `plugin.json`; the user approves the list in the editor's Plugins window before the
script runs (status `NeedsApproval`). An update that asks for more than was approved waits for approval again.

| Permission | Lets the script |
|---|---|
| `variables` | read any variable and publish its own |
| `actions` | register actions |
| `input` | press key combinations and type text on the PC |
| `http:<host>:<port>` | send HTTP requests to exactly that host and port (one entry per target, e.g. `http:localhost:4455`) |

Timers, settings pages, status items and logging need no permission. An unknown permission string makes the plugin an
`Error`. A call without its permission throws an ordinary JavaScript `Error` that the script can catch.

### The `host` object

Everything goes through the global, read-only `host`. There is no `require`, no `fetch`, no file access and no access to .NET.

```js
host.log(message)

host.variables.set(name, value)      // value: number, string, boolean or null
host.variables.get(name)
host.variables.remove(name)
host.variables.describe([{ name, description, example, category }])   // lists them in the editor's variable picker

host.registerAction({ type, name, category, description, icon, fields, run(context, settings) {} })
// context: { deviceId, pageId, widgetId, value }   settings: the values of the action's fields
// fields: the same shape as the C# SettingField: { key, label, kind: 'Text'|'Number'|'Bool'|'Select'|..., default, min, max, options }

host.settings.page(fields)           // adds a settings page to the plugin (stored in settings.json)
host.settings.get()                  // the current values as an object
host.status(id, text, level)         // a status bar item; level: 'Idle' | 'Ok' | 'Busy' | 'Warning' | 'Error'

host.input.hotkey('ctrl+shift+m')    // needs 'input'
host.input.type('hello')             // needs 'input'

host.http.get(url, { headers })      // needs http:<host>:<port>; returns { status, body } (body is text), synchronous
host.http.post(url, body, { headers })   // body is sent as JSON

const id = host.every(ms, fn)        // repeat, shortest 100 ms
host.after(ms, fn)                   // once
host.cancel(id)
host.permissions                     // what was granted
```

Rules the host enforces:

- **Names are yours.** Variable names and action types must start with `<plugin id>.`, so a plugin can never overwrite
  `system.cpu` or another plugin's values.
- **Register at the top level.** Actions, the settings page and variable descriptions must be registered while the script
  first runs; registrations made later from a callback are ignored. Variables can be set at any time.
- **Time and memory are limited per call** (start-up, each action, each timer tick): 2 seconds, 32 MB, 2 million statements,
  recursion depth 100. A call that goes over fails with an error; the plugin keeps running. HTTP requests time out after 5 s
  and responses are capped at 1 MB, redirects are not followed.
- **One thing at a time.** The script runs on its own thread, one call at a time, so a slow plugin never blocks the server
  or another plugin, but it also means `host.http` blocks the plugin's own other callbacks while it waits. At most 20
  timers; ticks that pile up while the script is busy are dropped.
- **A plugin that fails 5 times in a row is switched off** (status `Error` with the last message); Reload starts it again.
- Uninstalling a plugin unloads it, removes its variables and forgets its approval.

There is no `async`/`await` host API yet and no bridge for a plugin to draw its own widget (`plugin-html`); both are planned.

## Hot loading, reload and unload

A plugin is installed, reloaded and removed while the server runs (Plugins window in the editor); no restart is needed.
What that means for plugin code:

- **`Initialize` may run more than once per server run** (install, reload, replacing a plugin with a new version), each
  time in a fresh instance and a fresh load context. Keep state in the plugin instance, not in `static` fields that must
  survive.
- **Stop everything you started.** On unload the host cancels the `CancellationToken` passed to your `IVariableProvider.RunAsync`
  and waits a few seconds, then calls `Dispose` / `DisposeAsync` on the plugin instance and on every action, provider,
  settings page and icon pack you registered, if they implement `IDisposable` / `IAsyncDisposable`. Close sockets and stop
  timers and threads there. Anything still running keeps the plugin's assembly in memory until the server restarts.
- **Variables you set are removed on unload** automatically; you do not have to call `Remove` for them.
- **Your assemblies are loaded from memory,** so your files are never locked and can be replaced while the plugin runs, but
  `Assembly.Location` is empty inside a plugin. Use `IPluginHost.DataDirectory` to find your own files.
- **Action types must be unique.** If one of your action types is already registered (by the host or another plugin), the
  whole plugin fails to load with an `Error` entry and nothing of it stays registered.
- Installing a folder whose `plugin.json` has an id that is already installed replaces that plugin; files the plugin wrote
  into its own folder (such as `settings.json`) are kept.

## 5. Kapsam dışı (henüz yok)

- **JS plugin `async` API'si ve `plugin-html` widget köprüsü:** JS plugin'ler çalışıyor (bkz. "JavaScript plugins") ama
  script'ler senkron çalışır ve kendi widget'larını çizemez.
- **Plugin keşif/katalog sayfası:** şu an tek yol editördeki "Klasörden Yükle…" ile elle seçmek; bir
  online/merkezi katalog yok.
- **Widget türü / `plugin-html` köprüsü kaydı:** `IPluginHost` aksiyon, değişken sağlayıcı, ayar sayfası,
  durum öğesi ve ikon paketi kaydını destekliyor ama widget türü kaydı yok; `macro-station/docs/plan.md`'deki plugin'e özel widget türü
  ve sandbox'lı `plugin-html` iframe köprüsü henüz eklenmedi.
