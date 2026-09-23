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
└── <entry dosyası>   plugin.json'daki "entry" alanında adı geçen DLL (csharp) — js henüz çalıştırılmıyor
```

Klasör adı önemli değil; kimlik `plugin.json`'daki `id` alanından gelir. Editördeki "Klasörden Yükle…"
düğmesi seçilen kaynağı `plugins/<id>/` altına kopyalar (aynı id ikinci kez yüklenemez — host reddeder).

**Yükleme, sunucu her açıldığında bir kez olur** (`PluginLoader.LoadAll`, DI container kurulmadan önce
çalışır — bkz. `ServerApp.cs`). Yeni kopyalanan bir plugin'in devreye girmesi için **sunucunun yeniden
başlatılması gerekir**; editör bunu kopyalama sonrası açıkça söyler.

## 2. `plugin.json` şeması

```json
{
  "id": "obs",
  "name": "OBS Kontrolü",
  "version": "0.1.0",
  "sdkVersion": "^0.2.0",
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
| `entry` | evet | `kind: "csharp"` için giriş DLL'inin dosya adı (klasörün köküne göre). `kind: "js"` için giriş script — ama JS runtime henüz yok (madde 5). |
| `kind` | evet | `"csharp"` veya `"js"`. |
| `permissions` | hayır | Yalnızca `kind: "js"` için — henüz uygulanmıyor. |

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
  (plugin'in kendi kurulum klasörü, yazılabilir) altına kendi JSON dosyanızı okuyup/yazın — host'un
  plugin'lere özel bir ayar UI'ı/deposu henüz yok (madde 5). Gerçek örnek: `OBS/src/ObsSettings.cs`.
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
- **Incompatible** — `sdkVersion`/`minServerVersion` uyuşmuyor, ya da `kind: "js"` (JS runtime henüz yok).
- **Error** — `plugin.json` ayrıştırılamadı, `entry` dosyası bulunamadı, id çakışması, ya da `Initialize`
  sırasında exception.

## 5. Kapsam dışı (henüz yok)

- **JS/Jint plugin çalıştırma:** manifesto `kind: "js"` olarak tanınıyor ve listede görünüyor, ama
  sandbox'lı Jint runtime'ı henüz yazılmadı (Aşama 7 — `macro-station/docs/plan.md`).
- **Plugin keşif/katalog sayfası:** şu an tek yol editördeki "Klasörden Yükle…" ile elle seçmek; bir
  online/merkezi katalog yok.
- **Sıcak yükleme:** yeni kopyalanan bir plugin için sunucu yeniden başlatılmadan devreye girmiyor
  (`PluginLoadContext` collectible olsa da, host tarafında henüz bir "reload" akışı yok).
- **Widget türü / `plugin-html` köprüsü kaydı:** `IPluginHost` şu an yalnızca `IActionHandler` ve
  `IVariableProvider` kaydını destekliyor; `macro-station/docs/plan.md`'deki plugin'e özel widget türü
  ve sandbox'lı `plugin-html` iframe köprüsü henüz eklenmedi.
