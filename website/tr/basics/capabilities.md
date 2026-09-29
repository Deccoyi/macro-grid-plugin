# SDK neler yapabilir

Bu sayfa eklenti geliştiricileri için bir haritadır: bir eklenti neler yapabilir ve yapamaz, her iş için hangi fonksiyon çağrılır, sürümler nasıl işler ve
sonra nereye bakılır. Her satır ayrıntıların olduğu sayfaya bağlanır.

## Bir tür seçin

Bakımcı dışındaki herkesin eklentileri [JavaScript eklentileridir](/tr/tutorials/js-hello-world): sandbox içinde, sizin bildirdiğiniz ve kullanıcının
onayladığı izinlerle çalışır. C# eklentileri yalnızca resmî, imzalı eklentiler olarak vardır; sunucu başka hiçbirini yüklemez.

Klasör yapısı, kurulum adımları ve yaşam döngüsü için [Eklenti temelleri](/tr/basics/) sayfasına bakın.

## Bir JavaScript eklentisi neler ekleyebilir

| Yetenek | JavaScript |
|---|---|
| Bir widget olayının çalıştırdığı **aksiyonlar** (basma, bırakma, uzun basma, çift dokunma, aç/kapa, değer değişimi) | `host.registerAction({...})` |
| Widget'ların metinde gösterdiği ve koşullarda kullandığı **canlı değişkenler** | `host.variables.set / get / remove` |
| **Değişken kataloğu** (seçici için açıklama, tür, birim, izinli değerler) | `host.variables.describe([...])` |
| Düzenleyicinin alan bildirimlerinden çizdiği **ayar sayfası** | `host.settings.page(fields)` |
| **Durum çubuğu öğesi** | `host.status(id, text, level)` |
| **Tuş basmak, metin yazmak** (yalnızca bir düğme basışı işlenirken ve sınırlarla) | `host.input.hotkey / type` (`input` izni) |
| **HTTP istekleri** | `host.http.get / post / getAsync / postAsync` (`http:<host>:<port>` izni) |
| **Zamanlayıcılar** | `host.every / after / cancel` |
| **Çeviriler** | `plugin.json` yanında `locales/<dil>.json` |

Aksiyonlar ayrıca kendilerini tetikleyen cihazı, sayfayı ve widget'ı alır.

Bazı şeyler sunucunun içinde çalışan kod gerektirdiği için **yalnızca resmî C# eklentilerine** açıktır: simge paketleri, dinamik açılır liste seçenekleri, ayar
formunda düğmeler, basma-bırakma eşleşmesi ve korunan sırlar. Bir JavaScript eklentisi bunları kullanamaz.

## Bir eklenti neler yapamaz

- Yeni bir widget türü eklemek veya kendi widget'ını çizmek. Eklenti aksiyon, değişken, form, durum öğesi ve simge ekler; her şeyi düzenleyici çizer.
- Windows dışında çalışmak (sunucu yalnızca Windows'ta çalışır).
- `require`, `fetch`, dosya, .NET kullanmak veya 20'den fazla zamanlayıcı açmak. Bkz. [JavaScript host API](/tr/reference/js-host-api).

## İhtiyaç duyacağınız fonksiyonlar, sırasıyla

1. **Eklentiyi tanımlayın:** `id`, `name`, `version`, `minMacroGrid`, `entry`, `kind` içeren [`plugin.json`](/tr/reference/manifest).
2. **Başlatın:** betik her yüklemede baştan sona bir kez çalışır. Tüm kayıtları burada yapın.
3. **İşi yapın:** bir widget olayı tetiklenince aksiyonun `run(context, settings)` metodu çalışır. `context` hangi cihaz, sayfa ve widget'ın tetiklediğini söyler.
4. **Veri yayımlayın:** `host.variables.set(name, value)`, her an (örneğin bir zamanlayıcıdan). Adlar ve aksiyon türleri `<plugin id>.` ile başlamalıdır.
5. **Kullanıcının ayarlamasına izin verin:** aksiyonlar ve ayar sayfaları için alan bildirimleri ([Ayar sayfaları](/tr/guides/settings-pages)).
6. **Temiz kapatın:** yapacak bir şey yok; eklenti kaldırılırken zamanlayıcılar iptal edilir ve değişkenler silinir ([yaşam döngüsü](/tr/basics/#yasam-dongusu)).

Tam liste [JavaScript host API](/tr/reference/js-host-api) sayfasındadır.

## Sürümleme nasıl işler

Birbirinden bağımsız üç sürüm numarası vardır, hepsi [anlamsal sürümdür](https://semver.org/):

| Ne | Nerede | Kim değiştirir |
|---|---|---|
| Macro Grid düzenleyicisi **ve** eklenti SDK'sı | tek numara; sunucu deposundaki `<Version>` | Macro Grid sürümü |
| Sizin eklentiniz | `plugin.json` içindeki `version` | siz |
| İhtiyaç duyduğunuz en düşük Macro Grid | `plugin.json` içindeki `minMacroGrid` | siz |

- **Bir MAJOR içinde SDK yalnızca büyür.** Üyeler eklenir, asla kaldırılmaz veya değiştirilmez; `1.0.0` için derlenmiş bir eklenti her `1.x` üzerinde çalışır.
  Yeni bir MAJOR eklentileri bozabilir ve her eklenti yeniden derlenmelidir.
- **`minMacroGrid` bir alt sınırdır.** `"minMacroGrid": "1.2.0"`, 1.2.0'dan 2.0.0'a kadar (2.0.0 hariç) çalışır. Eklentinin mümkün olduğunca çok düzenleyicide çalışması için
  kullandığınız her şeyi içeren en eski sürümü yazın.
- **Kendi MAJOR'unuzu** yalnızca kayıtlı bir profil sessizce çalışmaz hale geliyorsa artırın: adı değişen bir aksiyon `type`'ı, değişken adı veya anlamı değişen bir ayar.
- Uyumsuz bir eklenti nedeni belirtilerek *Incompatible* olarak listelenir ve yüklenmez.

Ayrıntılar ve uyumluluk tablosu: [Uyumluluk ve sürümleme](/tr/basics/compatibility).

## Sonra nereye

1. [Başlarken](/tr/getting-started/), ardından [Öğretici 1: JavaScript](/tr/tutorials/js-hello-world).
2. Değişkenler için [Öğretici 2: canlı veri](/tr/tutorials/live-data).
3. Formlar, açılır listeler, listeler, dosya seçiciler ve sırlar için [Ayar sayfaları](/tr/guides/settings-pages).
4. [Hata ayıklama ve günlükler](/tr/guides/debugging), sonra [Eklentinizi yayımlama](/tr/guides/publishing).
5. Okumak için resmî eklentiler: [OBS](/tr/guides/obs-plugin) (değişkenler, dinamik seçenekler, bağlantı durumu) ve [PLC Icons](/tr/guides/icon-packs).
