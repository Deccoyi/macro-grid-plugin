# Macro Grid nedir

Macro Grid, yerel ağınızdaki bir telefonu ya da tableti Windows bilgisayarınız için özelleştirilebilir bir makro deck'e dönüştürür;
kendi tasarladığınız bir donanım makro tuş takımı gibi. Düzenleyicide bir ızgara üzerine düğmeler, toggle'lar, slider'lar ve knob'lar
yerleştirirsiniz. Deck, bilgisayardan canlı değerleri (CPU, RAM, saat, OBS yayın süresi, ...) gösterir; tuşlara basar, metin yazar,
programları açar, sesi değiştirir ve diğer yazılımları eklentiler aracılığıyla kontrol eder.

::: warning Alfa, tamamen yapay zekâ tarafından yazıldı, kullanım riski size aittir
Macro Grid herkese açık alfa aşamasındadır. **Bu site dahil tüm kod, tasarım, dokümantasyon ve görseller yapay zekâ tarafından
(bakımcının yönlendirmesiyle çalışan bir yapay zekâ asistanı) üretilmiştir.** Hiçbir şey bir insan tarafından satır satır incelenmemiş,
güvenlik denetiminden geçirilmemiş ya da herhangi bir amaç için sertifikalandırılmamıştır.
Her şey "olduğu gibi" sunulur, hiçbir tür garanti verilmez. Yazarlar ve katkıda bulunanlar; zarar, veri kaybı, kötüye kullanım veya güvenlik
sorunları dahil hiçbir konuda sorumluluk kabul etmez. **Tüm risk size aittir**: hangi yazılımı kurduğunuz, hangi cihazları eşleştirdiğiniz, hangi eklentileri
çalıştırdığınız ve hangi düğmelere bastığınız. API'ler ve eklenti manifest'leri hâlâ değişebilir; eklenti SDK'sı `0.x` sürümündedir, yani bir minor sürüm eklentileri bozabilir.
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

## İki tür eklenti

| | C# eklentisi | JavaScript eklentisi |
|---|---|---|
| Güven | Tam güven, sunucu işleminin içinde | Korumalı (sandbox); yalnızca küçük bir `host` nesnesi ve onaylanmış izinler |
| Şunun için uygun | Gerçek entegrasyonlar (websocket istemcisi, aygıt sürücüsü) | Küçük betikler (yerel bir HTTP API'yi sorgulamak, değişken yayınlamak, aksiyon eklemek) |
| Derleme gerekir mi | Evet | Hayır |

Yalnızca kaynağına güvendiğiniz C# eklentilerini kurun: sunucunun yapabildiği her şeyi yapabilirler.

## Sonraki adım

- Macro Grid'e yeni misiniz? [Hızlı başlangıç](/tr/getting-started/) sunucuyu kurar ve bir telefonu eşleştirir.
- Eklenti yazmak mı istiyorsunuz? [Eklenti temelleri](/tr/basics/) ile başlayın, ardından [Eğitim 1](/tr/tutorials/js-hello-world) sayfasına geçin.
