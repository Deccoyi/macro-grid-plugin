# SDK neler yapabilir

Bu sayfa eklenti geliştiricileri için bir haritadır: bir eklenti neler yapabilir ve yapamaz, her iş için hangi fonksiyon çağrılır, sürümler nasıl işler ve
sonra nereye bakılır. Her satır ayrıntıların olduğu sayfaya bağlanır.

## Bir tür seçin

| Yapmak istediğiniz | Kullanın |
|---|---|
| Yerel bir HTTP API'yi yoklamak, bir değer yayımlamak, küçük bir aksiyon eklemek; derleme yok | bir [JavaScript eklentisi](/tr/tutorials/js-hello-world) (sandbox, bildirdiğiniz izinlerle) |
| Açık bir soket tutmak, bir cihazla konuşmak, herhangi bir .NET kütüphanesini kullanmak | bir [C# eklentisi](/tr/tutorials/csharp-hello-world) (tam yetki, derleme gerekir) |

Klasör yapısı, kurulum adımları ve yaşam döngüsü için [Eklenti temelleri](/tr/basics/) sayfasına bakın.

## Bir eklenti neler ekleyebilir

| Yetenek | C# | JavaScript |
|---|---|---|
| Bir widget olayının çalıştırdığı **aksiyonlar** (basma, bırakma, uzun basma, çift dokunma, aç/kapa, değer değişimi) | `host.RegisterAction(IActionHandler)` | `host.registerAction({...})` |
| Widget'ların metinde gösterdiği ve koşullarda kullandığı **canlı değişkenler** | `host.RegisterVariableProvider(IVariableProvider)` | `host.variables.set / get / remove` |
| **Değişken kataloğu** (seçici için açıklama, tür, birim, izinli değerler) | `IVariableCatalogSource.Describe()` | `host.variables.describe([...])` |
| Düzenleyicinin alan bildirimlerinden çizdiği **ayar sayfası** | `host.RegisterSettingsPage(IPluginSettingsPage)` | `host.settings.page(fields)` |
| **Durum çubuğu öğesi** | `host.CreateStatusItem(id).Update(...)` | `host.status(id, text, level)` |
| Simge seçicisi için **simge paketi** | `host.RegisterIconPack(IIconPackSource)` | yok |
| **Dinamik açılır liste seçenekleri** | `IOptionsSource` | yok |
| **Basma ve bırakma eşleşmesi** ("basılıyken çal") | `IReleaseAwareAction` | yok |
| **Ayar formunda düğmeler** (bağlantıyı dene, önizleme) | `ISettingsCommandHandler` | yok |
| **Korunan sırlar** (ayar dosyanızda parola) | `host.Secrets.Protect / Unprotect` | yok |
| **Tuş basmak, metin yazmak** | bugün eklentilere verilmiyor | `host.input.hotkey / type` (`input` izni) |
| **HTTP istekleri** | kendi `HttpClient`'ınız | `host.http.get / post` (`http:<host>:<port>` izni) |
| **Zamanlayıcılar** | kendiniz | `host.every / after / cancel` |
| **Çeviriler** | `plugin.json` yanında `locales/<dil>.json` | aynı |

Aksiyonlar ayrıca bir `IDeviceController` alır; böylece bir aksiyon, kendisini tetikleyen telefonda sayfa veya profil değiştirebilir.

## Bir eklenti neler yapamaz

- Yeni bir widget türü eklemek veya kendi widget'ını çizmek. Eklenti aksiyon, değişken, form, durum öğesi ve simge ekler; her şeyi düzenleyici çizer.
- Windows dışında çalışmak (sunucu yalnızca Windows'ta çalışır).
- (JavaScript) `require`, `fetch`, dosya, .NET, `async`/`await` kullanmak veya 20'den fazla zamanlayıcı açmak. Bkz. [JavaScript host API](/tr/reference/js-host-api).

## İhtiyaç duyacağınız fonksiyonlar, sırasıyla

1. **Eklentiyi tanımlayın:** `id`, `name`, `version`, `macroGrid`, `entry`, `kind` içeren [`plugin.json`](/tr/reference/manifest).
2. **Başlatın:** C# `IPlugin.Initialize(IPluginHost host)` her yüklemede bir kez çağrılır; bir JavaScript betiği baştan sona bir kez çalışır. Tüm kayıtları burada yapın.
3. **İşi yapın:** bir widget olayı tetiklenince aksiyonun `ExecuteAsync(context, settings, token)` (C#) veya `run(context, settings)` (JS) metodu çalışır.
   `context` hangi cihaz, sayfa ve widget'ın tetiklediğini söyler.
4. **Veri yayımlayın:** eklenti yüklü olduğu sürece çalışan ve token iptal edilince dönen `IVariableProvider.RunAsync(store, token)` içinden
   `IVariableStore.Set(name, value)`. Adlar ve aksiyon türleri `<plugin id>.` ile başlamalıdır.
5. **Kullanıcının ayarlamasına izin verin:** aksiyonlar ve ayar sayfaları için `SettingField` bildirimleri ([Ayar sayfaları](/tr/guides/settings-pages)).
6. **Temiz kapatın:** işi token ile iptal edin, `IDisposable` / `IAsyncDisposable` uygulayın ([yaşam döngüsü](/tr/basics/#yasam-dongusu)).

Tam imzalar [C# SDK başvurusunda](/tr/reference/csharp-sdk) ve [JavaScript host API](/tr/reference/js-host-api) sayfasındadır.

## Sürümleme nasıl işler

Birbirinden bağımsız üç sürüm numarası vardır, hepsi [anlamsal sürümdür](https://semver.org/):

| Ne | Nerede | Kim değiştirir |
|---|---|---|
| Macro Grid düzenleyicisi **ve** eklenti SDK'sı | tek numara; sunucu deposundaki `<Version>` (NuGet paketi `MacroGrid.Plugin.Abstractions`) | Macro Grid sürümü |
| Sizin eklentiniz | `plugin.json` içindeki `version` | siz |
| İhtiyaç duyduğunuz en düşük Macro Grid | `plugin.json` içindeki `macroGrid` | siz |

- **Bir MAJOR içinde SDK yalnızca büyür.** Üyeler eklenir, asla kaldırılmaz veya değiştirilmez; `1.0.0` için derlenmiş bir eklenti her `1.x` üzerinde çalışır.
  Yeni bir MAJOR eklentileri bozabilir ve her eklenti yeniden derlenmelidir.
- **`macroGrid` bir alt sınırdır.** `"macroGrid": "1.2.0"`, 1.2.0'dan 2.0.0'a kadar (2.0.0 hariç) çalışır. Eklentinin mümkün olduğunca çok düzenleyicide çalışması için
  kullandığınız her şeyi içeren en eski sürümü yazın.
- **Kendi MAJOR'unuzu** yalnızca kayıtlı bir profil sessizce çalışmaz hale geliyorsa artırın: adı değişen bir aksiyon `type`'ı, değişken adı veya anlamı değişen bir ayar.
- Uyumsuz bir eklenti nedeni belirtilerek *Incompatible* olarak listelenir ve yüklenmez.

Ayrıntılar ve uyumluluk tablosu: [Uyumluluk ve sürümleme](/tr/basics/compatibility).

## Sonra nereye

1. [Başlarken](/tr/getting-started/), ardından [Öğretici 1: JavaScript](/tr/tutorials/js-hello-world) veya [Öğretici 2: C#](/tr/tutorials/csharp-hello-world).
2. Değişkenler için [Öğretici 3: canlı veri](/tr/tutorials/live-data).
3. Formlar, açılır listeler, listeler, dosya seçiciler ve sırlar için [Ayar sayfaları](/tr/guides/settings-pages).
4. [Hata ayıklama ve günlükler](/tr/guides/debugging), sonra [Eklentinizi yayımlama](/tr/guides/publishing).
5. Gerçek örnekler: [OBS](/tr/guides/obs-plugin) (C#, değişkenler, dinamik seçenekler, bağlantı durumu) ve [PLC Icons](/tr/guides/icon-packs).
