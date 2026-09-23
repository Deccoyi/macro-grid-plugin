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
- **Widget türü / `plugin-html` köprüsü kaydı:** `IPluginHost` aksiyon, değişken sağlayıcı, ayar sayfası,
  durum öğesi ve ikon paketi kaydını destekliyor ama widget türü kaydı yok; `macro-station/docs/plan.md`'deki plugin'e özel widget türü
  ve sandbox'lı `plugin-html` iframe köprüsü henüz eklenmedi.
