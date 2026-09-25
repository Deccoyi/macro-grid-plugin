# Eklentinizi yayımlama

Eklentiler düz klasörler olarak dağıtılır. Yayımlamak, insanlara düzenleyiciden kurabilecekleri bir klasör (genellikle bir zip) vermek demektir.

## Kontrol listesi

1. **Manifest.** `id` benzersiz ve sabittir, `version` [anlamsal sürümlemeyi](/tr/basics/compatibility#eklentinizi-sürümleme) izler, `sdkVersion` derleyip test ettiğiniz SDK'dır, `minServerVersion` kullandığınız özelliklere sahip en eski sunucudur.
2. **İzinler** (JavaScript). Yalnızca kullandığınız izinleri isteyin; bkz. [İzinler](/tr/reference/permissions). Daha fazla izin isteyen bir güncelleme, kullanıcının yeniden onayını bekler.
3. **Bir README**: eklentinin ne yaptığını, gereksinimlerini, ayarlarını ve verileri nerede sakladığını anlatır. Bir sırrı düz metin olarak saklıyorsa bunu belirtin.
4. **Bir lisans.** Bir `LICENSE` dosyası ekleyin; üçüncü taraf kod veya varlık içeriyorsa bir `NOTICE.md` de ekleyin. Her varlığın (simge, yazı tipi) nereden geldiğini ve yeniden dağıtma hakkınız olduğunu belirtin.
5. **Bir değişiklik günlüğü.** Bu depodaki eklentilerde olduğu gibi, kısa bir herkese açık ve ayrıntılı bir teknik günlük tutun.
6. **Temiz çıktı** (C#). Klasör DLL'nizi, kendi bağımlılıklarınızı ve `plugin.json` dosyasını içerir; `MacroGrid.Plugin.Abstractions.dll` dosyasını **içermez**.
7. **Temiz bir durumdan test edin**: eklentiyi kaldırın, dağıtacağınız klasörden kurun ve kendi README'nizi izleyin.

## Neler dağıtılır

- **JavaScript eklentisi:** `plugin.json`, betik ve `LICENSE` dosyalarını içeren klasör.
- **C# eklentisi:** zaten `plugin.json` içeren derleme çıktısı klasörü (`bin\Release\net10.0\`) ve `LICENSE` dosyanız.

Klasörün içeriğini zipleyin. Kullanıcılar ya `%AppData%\MacroGrid\plugins\<id>\` içine açar ya da açılmış klasör üzerinde **Eklentiler, Eklentileri Yönet…, Klasörden Yükle…** yolunu kullanır.

## Bu depodaki sürümler

Resmî depoya bir eklenti katkısı yaparsanız, her eklenti `main` üzerindeki bir etiketten (tag) kendi başına yayımlanır:

```
plugin-<name>-v<version>
```

örneğin `plugin-obs-v0.2.0`. Sürüm, eklentinin `plugin.json` dosyasındaki `version` ile aynı olmalıdır. Bir sürüm iş akışı (release workflow) o eklentiyi derler, çıktıyı lisans dosyalarıyla zipler, zip'i özetler (hash) ve imzalar, sürümü (taslak değil) yayımlar, ve eklentinin yeni sürümünü `main` üzerindeki `macrogrid-index.json` dosyasına işler — bu dosyanın biçimi ve imzanın neyi kapsadığı için bkz. [Kaynak dizini](/tr/reference/source-index). Katkı kuralları [Depo kuralları](/tr/guides/repo-rules) sayfasındadır.

## Kendi kaynağınızı çalıştırma

Sunucu, kullanıcının eklediği herhangi bir genel GitHub deposundan da eklenti kurabilir, yalnızca bu depo değil — ama açık bir üçüncü taraf uyarısıyla, çünkü yalnızca bu deponun sürümleri resmî anahtarla imzalanır. İki biçim desteklenir:

- **Çok eklentili bir depo**, Keşfet sekmesinde kaynak olarak eklenir: deponuzun kökünde, `main` üzerinde, yalnızca kendi sürümlerinize işaret eden bir `macrogrid-index.json` tutun. Bu deponun `examples/third-party-release.yml` ve `scripts/update-plugin-index.ps1` dosyalarını başlangıç noktası olarak kopyalayın ve imzalama adımını çıkarın (resmî bir anahtarınız yok, ve kendiniz eklediğiniz bir imzaya zaten güvenilmez).
- **Tek eklentili bir depo**, URL'si yapıştırılarak kurulur: kökte, her zaman en son sürümle eşleşen bir `plugin.json` tutun; `v<version>` etiketiyle, bir `<id>-<version>.zip` ve bir `<id>-<version>.zip.sha256` varlığıyla.

Kökte `plugin.json` yerine `macrogrid-index.json` bulunması, sunucuya "bu çok eklentili bir depo" olduğunu söyler — tam şema için bkz. [Kaynak dizini](/tr/reference/source-index).

## Eklentinizi Mağaza'ya ekleme

[Mağaza](/tr/store/), bu deponun eklentilerini listeler ve kendini depodan ve GitHub sürümlerinden oluşturur; bu yüzden bir eklentiyi listelemek üç adımdır (katkı için önce [Depo kuralları](/tr/guides/repo-rules) sayfasını izleyin):

1. **Klasörü ekleyin**: depo kökünde (örneğin `MyPlugin/`) `plugin.json`, bir `README.md` (ilk bölümü "Ne yapar" olur, ilk paragrafı kart metni olur), bir `CHANGELOG.md` ve yukarıda anlatıldığı gibi lisans dosyaları bulunsun.
2. **`website/store/catalog.json` dosyasına bir satır ekleyin**: `{ "id": "my-plugin", "dir": "MyPlugin", "category": "Integrations", "icon": "code" }`.
   `id`, `plugin.json` içindeki `id` ile aynı olmalıdır. `icon`, `website/public/store/icons/` içindeki bir SVG'nin adıdır; yoksa kart bir harf gösterir. En başa sıralamak için `"featured": true` ekleyin.
3. **Bir sürüm etiketleyin**: `plugin-<id>-vX.Y.Z` (örneğin `plugin-my-plugin-v0.1.0`), [Sürümler](#bu-depodaki-surumler) bölümünde anlatıldığı gibi. Sürüm yayımlandığında Mağaza, zip dosyasını indirme düğmesi olarak gösterir. Site, bir sürüm yayımlandığında, düzenlendiğinde veya silindiğinde yeniden oluşturulur.

Taslak sürümler asla gösterilmez. Bir eklentinin yayımlanmış sürümü olana kadar kartı GitHub Releases sayfasına bağlanır.

## Kullanıcılar için güvenlik notları

Kullanıcılarınıza eklentinizin neler yapabildiğini söyleyin. C# eklentisi tam güvene sahiptir; bu yüzden kullanıcılar yalnızca kaynağına güveniyorlarsa kurmalıdır. JavaScript eklentisi, çalışmadan önce izin listesini gösterir.
