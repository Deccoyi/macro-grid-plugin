# Macro Grid nedir

Macro Grid, yerel ağınızdaki bir telefonu ya da tableti Windows bilgisayarınız için özelleştirilebilir bir makro deck'e dönüştürür;
kendi tasarladığınız bir donanım makro tuş takımı gibi. Düzenleyicide bir ızgara üzerine düğmeler, toggle'lar, slider'lar ve knob'lar
yerleştirirsiniz. Deck, bilgisayardan canlı değerleri (CPU, RAM, saat, OBS yayın süresi, ...) gösterir; tuşlara basar, metin yazar,
programları açar, sesi değiştirir ve diğer yazılımları eklentiler aracılığıyla kontrol eder.

::: warning Beta, tamamen yapay zekâ tarafından yazıldı, kullanım riski size aittir
Macro Grid herkese açık beta aşamasındadır. **Bu site dahil tüm kod, tasarım, dokümantasyon ve görseller yapay zekâ tarafından
(bakımcının yönlendirmesiyle çalışan bir yapay zekâ asistanı) üretilmiştir.** Hiçbir şey bir insan tarafından satır satır incelenmemiş,
güvenlik denetiminden geçirilmemiş ya da herhangi bir amaç için sertifikalandırılmamıştır.
Her şey "olduğu gibi" sunulur, hiçbir tür garanti verilmez. Yazarlar ve katkıda bulunanlar; zarar, veri kaybı, kötüye kullanım veya güvenlik
sorunları dahil hiçbir konuda sorumluluk kabul etmez. **Tüm risk size aittir**: hangi yazılımı kurduğunuz, hangi cihazları eşleştirdiğiniz, hangi eklentileri
çalıştırdığınız ve hangi düğmelere bastığınız. API'ler ve eklenti manifest'leri hâlâ değişebilir; eklenti SDK'sı anlamsal sürümlemeyi izler ([Uyumluluk](/tr/basics/compatibility)): bir minor sürüm yalnızca ekleme yapar, yeni bir major sürüm eklentileri bozabilir.
:::

## Mimari

Üç parça vardır; üç ayrı depoda, birbirinden bağımsız sürümlenir.

| Parça | Depo | Nedir |
|---|---|---|
| Sunucu ve Düzenleyici | [macro-grid](https://github.com/Deccoyi/macro-grid) | Bir Windows sistem tepsisi uygulamasıdır. Profilleri saklar, deck'lerle WebSocket (9820 portu) üzerinden konuşur, aksiyonları bilgisayarda çalıştırır ve Düzenleyici'yi kendi penceresinde barındırır. Eklenti SDK'sı da içinde yer alır. |
| İstemci | [macro-grid-client](https://github.com/Deccoyi/macro-grid-client) | Deck'i çizen ve dokunuşları bildiren Android uygulamasıdır. Herhangi bir tarayıcı da `http://<PC address>:9820/deck/` adresinde deck olarak çalışabilir. |
| Eklentiler | [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) | Resmî eklentiler ve bu dokümantasyon. |

```
┌───────────── Windows PC ───────────────────────────────┐
│  Macro Grid server (tray app)                          │
│   ├─ port 9820: /ws  WebSocket for phones and decks    │
│   │             /api editor API (this computer only)   │
│   ├─ the editor (a WebView2 window)                    │
│   ├─ core: profiles, actions, variables, plugins       │
│   └─ Windows: key input, audio, system metrics         │
└───────────────▲────────────────────────────────────────┘
                │ WebSocket, JSON, local network only
     phone / tablet app  ·  any browser at /deck/
```

**Eklentiler sunucunun içinde çalışır.** Dört tür şey ekleyebilirler:

- **Aksiyonlar**: bir widget'a basıldığında yapılabilecek şeyler (bir OBS sahnesine geçmek, bir sayacı artırmak).
- **Değişkenler**: bir widget'ın metninde gösterebileceği (`{obs.stream.duration}`) ya da rengini veya simgesini değiştiren kurallarda kullanabileceği canlı değerler.
- **Ayar sayfaları, durum çubuğu öğeleri**: Eklentiler penceresinde bir form ve Düzenleyici'nin durum çubuğunda renkli bir öğe.
- **Simge paketleri**: Düzenleyici'nin simge seçicisi için simge kümeleri.

Bir eklenti henüz kendi widget'ını çizemez ya da yeni bir widget türü ekleyemez.

## Hangi eklentiyi kim yazabilir

| | Resmî eklenti (C#) | Başkalarının eklentisi (JavaScript) |
|---|---|---|
| Güven | Tam güven, sunucu işleminin içinde. Bakımcı tarafından imzalanır ve sunucu imzayı her yüklemede denetler. | Korumalı (sandbox); yalnızca küçük bir `host` nesnesi ve onaylanmış izinler |
| Şunun için uygun | Bu depodaki eklentiler | Küçük betikler (yerel bir HTTP API'yi sorgulamak, değişken yayınlamak, aksiyon eklemek) |
| Derleme gerekir mi | Evet | Hayır |

**Üçüncü taraf eklentiler JavaScript'tir.** Bir C# eklentisi bilgisayara tam erişimle çalışır ve Macro Grid onu sınırlayamaz; bu yüzden sunucu, yalnızca
resmî, imzalı eklentilerden biri olan C# eklentisini yükler. Bir JavaScript eklentisi istediği izinleri gösterir ve yalnızca siz onayladıktan sonra çalışır.
Macro Grid başka yazarların eklentilerini incelemez: yalnızca güvendiklerinizi kurun.

## Sonraki adım

- Macro Grid'e yeni misiniz? [Hızlı başlangıç](/tr/getting-started/) sunucuyu kurar ve bir telefonu eşleştirir.
- Eklenti yazmak mı istiyorsunuz? [Eklenti temelleri](/tr/basics/) ile başlayın, ardından [Eğitim 1](/tr/tutorials/js-hello-world) sayfasına geçin.
