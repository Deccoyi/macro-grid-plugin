# Macro Station Plugins — Agent ve Repo Kuralları

Bu repo (`macro-station-plugins`), Macro Station'ın **server** (`macro-station`) ve **client** (`macro-station-client`) repolarından ayrı, üçüncü bağımsız repo. Server/editör tarafındaki genel ürün planı için `macro-station/docs/plan.md` ve `agent-notes.md`'ye bakılabilir (Plugin sistemi bölümü) — ama bu repo kendi kurallarıyla, kendi başına yaşar.

> **Not (2026-09-23):** `macro-station` repo'sunda gerçek plugin *yükleyicisi* kuruldu (`IPlugin`/`IPluginHost`, `AssemblyLoadContext` izolasyonu, `%AppData%/MacroStation/plugins/` klasör taraması, editörde "Klasörden Yükle…" akışı — bkz. `MacroStation.Core/Plugins/` ve [docs/plugin-authoring.md](docs/plugin-authoring.md)). Aynı gün, bu repoda **ilk gerçek plugin de yazıldı: [OBS/](OBS/)** — `_backup/obs-plugin-reference/`'daki taslak temel alınarak obs-websocket v5 bağlantısı, sahne/yayın/kayıt/ses aksiyonları ve `obs.*` değişkenleri. Bu, plugin'e özel ayar ihtiyacının (host/port/şifre) `IPluginHost`'a `DataDirectory`/`Log` eklenmesini gerektirdiğini ortaya çıkardı — Plugin SDK bu yüzden `0.1.0`'dan `0.2.0`'a çıktı (bkz. `OBS/README.md` ve `docs/plugin-authoring.md`). Soundboard/IconPacks/WebView-chat gibi diğer plugin'ler hâlâ yazılmadı.

## 1. Bağımsız versiyonlama

- Her plugin **kendi sürüm numarasını** taşır (semver: `MAJOR.MINOR.PATCH`), ana programın (`macro-station`) sürümünden bağımsız.
- Bir plugin'in sürümü yalnızca o plugin'in kendi `CHANGELOG.md`'sinde ilerler. Ana programın sürüm bump'ı bir plugin'in sürümünü otomatik değiştirmez, tetiklemez.
- Aynı şekilde iki farklı plugin birbirinin sürümüne bağlı/duyarlı değildir — biri 3.0.0'dayken diğeri 0.1.0'da olabilir, aralarında bir uyumluluk ilişkisi yoktur (bkz. madde 2).
- Bir plugin'in sürüm bump'ı (özellikle MAJOR) da — tıpkı ana programda olduğu gibi — sessizce yapılmaz, kullanıcıya sorulur.

## 2. Her plugin kendi klasöründe, izole

- Repo kökünde her plugin kendi klasöründe yaşar: `macro-station-plugins/<PluginAdi>/`.
- Bir plugin klasörü **başka bir plugin klasörüne asla import/reference/dosya yolu referansı içermez.** `../DigerPlugin/...` gibi bir yol, bir plugin'in diğerinin kaynak kodunu/paketini `ProjectReference`/`file:`/relative import ile kullanması — hepsi yasak.
- İki plugin'in ortak kod ihtiyacı olursa (örn. ortak bir yardımcı fonksiyon), bu kodu birbirine referans vererek paylaşmazlar; ya (a) her biri kendi kopyasını taşır (küçük kod için, `packages/renderer`'ın server/client repo'ları arasında kasıtlı senkronize tutulmaması gibi — bkz. `macro-station/docs/agent-notes.md`), ya da (b) gerçekten paylaşılması gereken büyük bir şeyse, ayrı, bağımsız yayınlanan bir paket olarak (NuGet/npm) dışarıdan referans alınır — asla repo-içi dosya yoluyla değil.
- Bir plugin'in klasörü silinip taşınabilir/kopyalanabilir olmalı, başka hiçbir şeyi bozmadan. Bu, izolasyonun pratik testi: "bu plugin klasörünü sil, geri kalan her şey hâlâ derlenir mi?" sorusunun cevabı her zaman evet olmalı.
- Plugin'ler arası iletişim gerekiyorsa (örn. biri diğerinin sağladığı bir değişkeni okumak istiyorsa) bu, ana programın `IVariableStore`/`IPluginHost` gibi ortak host arayüzleri **üzerinden**, çalışma zamanında olur — derleme zamanı bir bağımlılık değil.

## 3. Manifesto ve ana program uyumluluğu

- Her plugin'in kendi `plugin.json` manifestosu olur (plugin klasörünün kökünde). Zorunlu alanlar:
  - `id`: benzersiz, değişmez kimlik (örn. `"obs"`, `"audio"`, `"soundboard"`). Ana programda ve diğer plugin'lerle çakışmaması kullanıcının/geliştiricinin sorumluluğunda; ana program aynı `id`'yi ikinci kez yüklemeyi reddeder.
  - `name`: kullanıcıya gösterilen ad (editördeki "Eklentiler" listesinde).
  - `version`: plugin'in kendi sürümü (madde 1).
  - `sdkVersion`: Plugin SDK'sına (`MacroStation.Plugin.Abstractions`) karşı npm tarzı caret aralığı (örn. `"^0.1.0"`).
  - `minServerVersion`: bu plugin'in ihtiyaç duyduğu asgari ana program (macro-station) sürümü. Ana program, bir plugin'i yüklerken kendi sürümünü/SDK sürümünü bu iki alanla karşılaştırır; uyuşmuyorsa plugin'i **yüklemez** ve editörde açık bir uyarı gösterir ("Uyumsuz: SDK X istiyor, sunucudaki SDK Y") — sessizce yüklenip daha sonra tuhaf hatalar vermez.
  - `entry`: giriş derlemesi/dosyası (C# plugin için DLL adı, JS plugin için giriş script'i).
  - `kind`: `"csharp"` | `"js"` (madde 4).
- Manifesto ayrıca (varsa) JS plugin'ler için `permissions` listesini taşır (`macro-station/docs/plan.md`'deki JS plugin izin modeliyle birebir aynı: `"variables"`, `"actions"`, `"http:host:port"`, `"input"` gibi).
- Manifesto şeması değişirse (yeni zorunlu alan eklenirse) bu, ana programın kendi sürümünde bir MINOR/MAJOR değişiklik olarak ele alınır ve eski manifestoların hâlâ okunabilir kalması (ya da açıkça reddedilmesi, sessizce yanlış yorumlanmaması) hedeflenir.
- Tam şema, alan alan açıklamayla ve örnek bir plugin ile birlikte: [docs/plugin-authoring.md](docs/plugin-authoring.md).

## 4. C# plugin'leri vs JS plugin'leri (ayrım netleştirilmeli)

`macro-station/docs/plan.md`'deki "Plugin sistemi" bölümüne göre iki ayrı güven seviyesi var — bu repo'daki her plugin klasörü hangi kategoride olduğunu manifestosunda (`kind`) açıkça belirtir:

- **C# plugin'leri (tam yetkili, güvenilir):** Kendi `AssemblyLoadContext`'inde yüklenir (izolasyon, ana programı çökertmemesi için — ama sandbox değil, CLR'a tam erişimi var). `IPlugin.Initialize(IPluginHost)` üzerinden aksiyon tipi (`IActionHandler`), değişken sağlayıcı (`IVariableProvider`/`IVariableCatalogSource`), widget türü kaydı yapabilir. Bu repodaki klasör yapısı: `.csproj`, kaynak kod, derlenmiş `plugin.json` + DLL çıktısı.
- **JS plugin'leri (Jint, sandbox'lı):** CLR erişimi yok (`AllowClr` kapalı), yalnızca host'un enjekte ettiği izinli API nesneleri. Kaynak limitleri zorunlu (timeout, bellek, statement sayısı, recursion). Bu repodaki klasör yapısı: `plugin.json` + `.js`/`.ts` kaynak (derlenmiş bir native ikili değil).
- Bir plugin klasörü ikisini karıştırmaz — ya tamamen C# ya tamamen JS.

## 5. Klasör içi standart yapı (öneri, plugin loader'ı yazılırken netleşecek)

```
macro-station-plugins/
├── agent-and-repo-rules.md      (bu dosya)
├── <PluginAdi>/
│   ├── plugin.json               zorunlu manifesto (madde 3)
│   ├── CHANGELOG.md               bu plugin'e özel, ana programınkinden bağımsız
│   ├── README.md                  ne yaptığı, kurulum/bağımlılık notları (örn. OBS plugin'i için "OBS WebSocket sunucusu açık olmalı")
│   └── src/                       kaynak kod (C# .csproj veya JS/TS dosyaları)
```

## 6. Ne zaman uygulanacak

Server tarafındaki gerçek `IPlugin`/`IPluginHost`/`AssemblyLoadContext` yükleyicisi kuruldu, ilk gerçek plugin de ([OBS/](OBS/)) yazıldı (2026-09-23) — bkz. [docs/plugin-authoring.md](docs/plugin-authoring.md). Soundboard, IconPacks, WebView/chat gibi diğer plugin'lerin ne zaman yazılacağı hâlâ ayrı, açık bir kullanıcı kararı.
