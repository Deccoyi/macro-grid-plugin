# Eklenti temelleri

Bir eklenti, Macro Grid sunucusuna aksiyonlar (bir widget'ın yapabileceği şeyler), değişkenler (bir widget'ın gösterebileceği canlı değerler) ve küçük
ekstralar (bir ayar sayfası, durum çubuğu öğeleri, simge paketleri) ekler.

## Türler

| | JavaScript eklentisi | C# eklentisi (yalnızca resmî eklentiler) |
|---|---|---|
| `plugin.json` içindeki `kind` | `"js"` | `"csharp"` |
| Kim dağıtabilir | Herkes | Yalnızca [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) deposundaki resmî eklentiler |
| Güven | Korumalı (sandbox). .NET erişimi yoktur; yalnızca küçük bir `host` nesnesi ve kullanıcının onayladığı izinler vardır. | Tam güven, sunucu işleminin içinde. Yalnızca geçerli bir resmî imzayla yüklenir; imza her seferinde denetlenir. |
| Şunun için uygun | Küçük betikler (yerel bir HTTP API'yi sorgulamak, değişken yayınlamak, aksiyon eklemek) | Gerçek entegrasyonlar (websocket istemcisi, aygıt sürücüsü) |
| Derleme gerekir mi | Hayır (tek bir betik) | Evet ve bakımcı imzalar |

::: warning Üçüncü taraf eklentiler JavaScript'tir
Sunucu, resmî olarak imzalanmamış bir C# eklentisini kaynağı ne olursa olsun (Keşfet, eklenmiş bir kaynak, yapıştırılmış bir bağlantı ya da bir klasör)
kurmayı ve yüklemeyi reddeder. Bir eklenti klasörü ya birdir ya diğeri, asla ikisi birden değildir.
:::

Çalışan örnekler: [HelloJs](/tr/tutorials/js-hello-world) (küçük bir JavaScript eklentisi) ve okumak için resmî [OBS](/tr/guides/obs-plugin)
eklentisi (bir C# entegrasyonu).

## Klasör yapısı

Sunucu eklentileri `%AppData%\MacroGrid\plugins\<folder>\` altında arar. İçinde `plugin.json` bulunan klasör bir eklentidir.
Klasör adı önemli değildir; kimliği manifest'teki `id` belirler.

Bir JavaScript eklentisi:

```
hello-js/
├── plugin.json
└── index.js
```

Kaynak depoda her eklenti ayrıca `plugin.json` dosyasının yanında bir README, bir `LICENSE` ve iki değişiklik günlüğü tutar; bkz.
[Depo kuralları](/tr/guides/repo-rules).

## Eklenti kurma

1. Düzenleyicide **Eklentiler, Eklentileri Yönet…** yolunu açın.
2. **Klasörden Yükle…** seçeneğini seçin ve `plugin.json` içeren bir klasör belirtin.
3. Klasör `plugins\<id>\` altına kopyalanır ve yeniden başlatmaya gerek kalmadan hemen yüklenir.

`id` değeri zaten kurulu olan bir klasörü kurmak, o eklentinin yerini alır. Eklentinin kendi klasörüne yazdığı dosyalar,
örneğin `settings.json`, korunur. Aynı pencereden bir eklenti yeniden yüklenebilir ve kaldırılabilir.

## Eklentinin durumu

Eklentiler penceresi, içinde `plugin.json` bulunan her klasörü listeler:

- **Yüklü**: çalışıyor, aksiyonları ve değişkenleri kullanılabilir.
- **Uyumsuz**: `minMacroGrid` bu Macro Grid'den daha yeni bir sürüm ya da başka bir MAJOR istiyor (mesaj hangisi olduğunu söyler).
- **Onay bekliyor**: bildirdiği izinler kullanıcı tarafından henüz onaylanmamış bir JavaScript eklentisi. Onaylanana kadar çalışmaz.
- **İzin verilmedi** (*Not allowed*): resmî olarak imzalanmamış ya da dosyaları imzasıyla artık eşleşmeyen bir C# eklentisi. Hiç çalışmaz; mesaj nedenini söyler ve yalnızca kaldırabilirsiniz.
- **Hata**: `plugin.json` ayrıştırılamadı, giriş dosyası eksik, kimlik kurulu başka bir eklenti tarafından kullanılıyor, bir
  aksiyon türü zaten kayıtlı, bir izin bilinmiyor, `Initialize` (ya da betiğin ilk çalışması) başarısız oldu veya bir JavaScript
  eklentisi art arda hata verdiği için kapatıldı. Mesaj adın altında gösterilir. **Yeniden yükle** tekrar dener.

## Yaşam döngüsü

Eklentiler sunucu başlarken yüklenir; sunucu yeniden başlatılmadan istenen zamanda kurulabilir, yeniden yüklenebilir ve
kaldırılabilir. Bir JavaScript eklentisi için bunun anlamı:

- **Betik her yüklemede bir kez çalışır** (başlangıç, kurulum, yeniden yükleme, bir eklentiyi yeni sürümle değiştirme). Aksiyonları, ayar sayfasını
  ve değişken açıklamalarını betiğin en üst düzeyinde kaydedin.
- **Zamanlayıcılar ve istekler kaldırmada durur.** Betiğin `host.every` veya `host.after` ile başlattığı her şey eklenti kaldırılırken iptal edilir;
  değişkenleri ve durum öğeleri sizin yerinize silinir.
- **Aksiyon türleri benzersiz olmalıdır.** Sizinkilerden biri (sunucu veya başka bir eklenti tarafından) zaten kayıtlıysa eklentinin tamamı
  *Hata* durumuyla yüklenemez ve hiçbir kaydı kalmaz.
- **Dosyalar.** Bir JavaScript eklentisinin kendi dosya erişimi yoktur; ayar sayfasının değerleri onun için klasöründeki `settings.json` dosyasında saklanır (en fazla 64 KB).

## Aksiyonlar, widget'lar ve değişkenler tek resimde

Bir widget'ın olayları (basma, bırakma, uzun basma, çift dokunma, toggle açık/kapalı, değer değişimi) her biri bir aksiyon listesini sırayla çalıştırır.
Bir aksiyonun ayarları, formunun değerleridir. Değişkenler ters yönde akar: sağlayıcılar değer yayınlar, widget'lar bunları
metinde gösterir veya koşullu kurallarda (renk, metin, simge, animasyon) kullanır.

## Mevcut SDK'nın sınırları

- Bir eklenti widget türü ekleyemez ya da kendi widget'ını çizemez (bir `plugin-html` widget'ı planlanıyor).
- Sunucu yalnızca Windows'ta çalışır, dolayısıyla eklentiler fiilen yalnızca Windows içindir.
