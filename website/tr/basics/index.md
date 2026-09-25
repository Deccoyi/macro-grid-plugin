# Eklenti temelleri

Bir eklenti, Macro Grid sunucusuna aksiyonlar (bir widget'ın yapabileceği şeyler), değişkenler (bir widget'ın gösterebileceği canlı değerler) ve küçük
ekstralar (bir ayar sayfası, durum çubuğu öğeleri, simge paketleri) ekler.

## Türler

| | C# eklentisi | JavaScript eklentisi |
|---|---|---|
| `plugin.json` içindeki `kind` | `"csharp"` | `"js"` |
| Güven | Tam güven. Sunucu işleminin içinde, tam .NET erişimiyle çalışır; yalnızca diğer eklentileri bozamayacak şekilde yalıtılmıştır. | Korumalı (sandbox). .NET erişimi yoktur; yalnızca küçük bir `host` nesnesi ve kullanıcının onayladığı izinler vardır. |
| Şunun için uygun | Gerçek entegrasyonlar (websocket istemcisi, aygıt sürücüsü) | Küçük betikler (yerel bir HTTP API'yi sorgulamak, değişken yayınlamak, aksiyon eklemek) |
| Derleme gerekir mi | Evet (bir DLL) | Hayır (tek bir betik) |

::: danger Yalnızca güvendiğiniz C# eklentilerini kurun
Sunucunun yapabildiği her şeyi yapabilirler. Bir eklenti klasörü ya birdir ya diğeri, asla ikisi birden değildir.
:::

Çalışan örnekler: [OBS](/tr/guides/obs-plugin) (tam bir C# entegrasyonu), [PLC Icons](/tr/guides/icon-packs) (bir C# simge paketi)
ve [HelloJs](/tr/tutorials/js-hello-world) (küçük bir JavaScript eklentisi).

## Klasör yapısı

Sunucu eklentileri `%AppData%\MacroGrid\plugins\<folder>\` altında arar. İçinde `plugin.json` bulunan klasör bir eklentidir.
Klasör adı önemli değildir; kimliği manifest'teki `id` belirler.

Bir JavaScript eklentisi:

```
hello-js/
├── plugin.json
└── index.js
```

Kurulmuş hâliyle bir C# eklentisi (derleme çıktı klasörü; derleme `plugin.json` dosyasını DLL'in yanına kopyalar):

```
HelloCSharp/
├── plugin.json
└── HelloCSharp.dll
```

Kaynak depoda her eklenti ayrıca `plugin.json` dosyasının yanında bir README, bir `LICENSE` ve iki değişiklik günlüğü tutar; bkz.
[Depo kuralları](/tr/guides/repo-rules).

## Eklenti kurma

1. Düzenleyicide **Eklentiler, Eklentileri Yönet…** yolunu açın.
2. **Klasörden Yükle…** seçeneğini seçin ve `plugin.json` içeren bir klasör belirtin (C# eklentisi için `src\bin\Debug\net10.0\`
   gibi derleme çıktı klasörü).
3. Klasör `plugins\<id>\` altına kopyalanır ve yeniden başlatmaya gerek kalmadan hemen yüklenir.

`id` değeri zaten kurulu olan bir klasörü kurmak, o eklentinin yerini alır. Eklentinin kendi klasörüne yazdığı dosyalar,
örneğin `settings.json`, korunur. Aynı pencereden bir eklenti yeniden yüklenebilir ve kaldırılabilir.

## Eklentinin durumu

Eklentiler penceresi, içinde `plugin.json` bulunan her klasörü listeler:

- **Yüklü**: çalışıyor, aksiyonları ve değişkenleri kullanılabilir.
- **Uyumsuz**: `macroGrid` bu Macro Grid'den daha yeni bir sürüm ya da başka bir MAJOR istiyor (mesaj hangisi olduğunu söyler).
- **Onay bekliyor**: bildirdiği izinler kullanıcı tarafından henüz onaylanmamış bir JavaScript eklentisi. Onaylanana kadar çalışmaz.
- **Hata**: `plugin.json` ayrıştırılamadı, giriş dosyası eksik, kimlik kurulu başka bir eklenti tarafından kullanılıyor, bir
  aksiyon türü zaten kayıtlı, bir izin bilinmiyor, `Initialize` (ya da betiğin ilk çalışması) başarısız oldu veya bir JavaScript
  eklentisi art arda hata verdiği için kapatıldı. Mesaj adın altında gösterilir. **Yeniden yükle** tekrar dener.

## Yaşam döngüsü

Eklentiler sunucu başlarken yüklenir; sunucu yeniden başlatılmadan istenen zamanda kurulabilir, yeniden yüklenebilir ve
kaldırılabilir. Bunun kodunuz için anlamı:

- **`Initialize` tek bir sunucu oturumunda birçok kez çalışabilir** (kurulum, yeniden yükleme, bir eklentiyi yeni sürümle değiştirme); her seferinde
  yeni bir assembly yükleme bağlamında yeni bir örnek üzerinde çalışır. Durumu nesnelerinizde tutun, hayatta kalması gereken `static` alanlarda değil.
- **Başlattığınız her şeyi durdurun.** Kaldırma sırasında sunucu, `IVariableProvider.RunAsync` yöntemine verilen token'ı iptal eder ve
  en fazla 5 saniye bekler; ardından eklenti örneğiniz ile kaydettiğiniz her aksiyon, sağlayıcı, ayar sayfası ve simge paketi
  üzerinde, `IDisposable` / `IAsyncDisposable` uyguluyorlarsa `Dispose` / `DisposeAsync` çağırır. Soketleri kapatın, zamanlayıcıları ve iş parçacıklarını
  orada durdurun. Çalışmaya devam eden her şey, sunucu yeniden başlatılana kadar assembly'nizi bellekte tutar (bir uyarı kaydedilir; eklentinin
  kaydı her durumda silinir).
- **Değişkenleriniz kaldırma sırasında sizin yerinize silinir**, durum öğeleriniz ve kayıtlarınız da bırakılır.
- **Assembly'leriniz bellekten yüklenir**, bu yüzden dosyalarınız asla kilitlenmez ve eklenti çalışırken değiştirilebilir. Bedeli:
  bir eklentinin içinde `Assembly.Location` boştur. Kendi dosyalarınızı bulmak için `IPluginHost.DataDirectory` kullanın.
- **Aksiyon türleri benzersiz olmalıdır.** Sizinkilerden biri (sunucu veya başka bir eklenti tarafından) zaten kayıtlıysa eklentinin tamamı
  *Hata* durumuyla yüklenemez ve hiçbir kaydı kalmaz.
- Her eklentinin kendi assembly yükleme bağlamı vardır; böylece iki eklenti aynı kütüphanenin farklı sürümlerini kullanabilir. Paylaşılan tek
  assembly `MacroGrid.Plugin.Abstractions` olur.

## Aksiyonlar, widget'lar ve değişkenler tek resimde

Bir widget'ın olayları (basma, bırakma, uzun basma, çift dokunma, toggle açık/kapalı, değer değişimi) her biri bir aksiyon listesini sırayla çalıştırır.
Bir aksiyonun ayarları, formunun değerleridir. Değişkenler ters yönde akar: sağlayıcılar değer yayınlar, widget'lar bunları
metinde gösterir veya koşullu kurallarda (renk, metin, simge, animasyon) kullanır.

## Mevcut SDK'nın sınırları

- Bir eklenti widget türü ekleyemez ya da kendi widget'ını çizemez (bir `plugin-html` widget'ı planlanıyor).
- Sunucu yalnızca Windows'ta çalışır, dolayısıyla eklentiler fiilen yalnızca Windows içindir.
- JavaScript eklentileri için henüz `async`/`await` host API'si yok.
