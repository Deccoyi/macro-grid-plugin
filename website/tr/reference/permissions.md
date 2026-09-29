# İzinler

İzinler **JavaScript eklentileri** için geçerlidir. Bir betik hiçbir yetkisiz başlar: yalnızca hesaplama yapabilir, zamanlayıcı kurabilir, ayar sayfası gösterebilir, günlüğe yazabilir ve durum çubuğunu güncelleyebilir. Dış dünyaya dokunan her şey (diğer değişkenler, klavye, ağ, aksiyon listesi) kullanıcının onayladığı bir izin ister.

## Dört izin

| İzin | Betiğin yapmasına izin verir | Açtığı çağrılar |
|---|---|---|
| `variables` | sunucudaki **herhangi** bir değişkeni okumak; **kendi** değişkenlerini yayınlamak, değiştirmek ve silmek | `host.variables.set / get / remove / describe` |
| `actions` | düzenleyicinin aksiyon seçicisine kendi aksiyonlarını eklemek | `host.registerAction` |
| `input` | bilgisayarda tuş kombinasyonlarına basmak ve kullanıcı yazmış gibi metin yazmak | `host.input.hotkey / type` |
| `http:<host>:<port>` | tam olarak o ana bilgisayara ve porta HTTP istekleri göndermek | `host.http.get / post / getAsync / postAsync` |

Başka izin adı yoktur. Değer alan tek izin `http:` iznidir ve her hedef için ayrı bir girdi yazılır.

### `variables`

- Okuma kendi değerlerinizle sınırlı değildir: `host.variables.get('system.cpu')`, `get('obs.streaming')` veya başka bir eklentinin değişkeni okunabilir. Bir eklenti böyle başka yerde olanlara tepki verir.
- Yazma yalnızca `<plugin id>.` ile başlayan adlarla yapılır. Bir eklenti `system.*` değerlerini veya başka eklentinin değerlerini asla ezemez.
- Bu izin olmadan bir eklenti canlı veri yayınlayamaz; bu yüzden veri eklentilerinin neredeyse hepsi ister.

### `actions`

- Aksiyon seçicisine girdiler ekler. Kullanıcı bunları bir widget olayına bağlar, sonra betiğin `run` fonksiyonu çalışır.
- Aksiyon türleri `<plugin id>.` ile başlamalıdır (`hellojs.bump`), böylece kayıtlı bir profil aksiyonun kime ait olduğunu her zaman bilir.
- Bu izni istemeyen bir eklenti yine de salt veri kaynağı olarak işe yarar (yalnızca `variables`).

### `input`

- `host.input.hotkey('ctrl+shift+m')` bir tuş kombinasyonu gönderir, `host.input.type('metin')` metin yazar (çağrı başına en çok 2000 karakter).
- Bu en güçlü yerel izindir: odaktaki pencere tuşları alır. Yalnızca eklentinin asıl amacı girdi göndermekse isteyin ve ne zaman göndereceğini README'nizde belirtin.
- Kabul edilen tuş adları [`host.input`](/tr/reference/js-host-api#host-input) altında listelidir.

### `http:<host>:<port>`

Değer, tam hedeftir; örneğin:

```json
"permissions": ["http:localhost:4455", "http:192.168.1.20:8080", "http:api.example.com:443"]
```

Kuralların hepsini sunucu uygular:

| Kural | Ayrıntı |
|---|---|
| Tam eşleşme | İstek adresinin ana bilgisayarı ve portu onaylı bir girdiyle aynı olmalıdır. Joker karakter, alt alan adı eşleşmesi veya yol kuralı yoktur. |
| Port her zaman yazılır | `http://localhost/x` 80 portunu kullanır, yani `http:localhost:80` gerekir. Portsuz `https://` 443 kullanır, yani `http:<host>:443` gerekir. İzin adı `https://` adresleri için de `http:` ile başlar. |
| Büyük/küçük harf | İzin dizeleri büyük/küçük harfe duyarsızdır. |
| Geçerli biçim | `http:` + harf, rakam, `.` veya `-` + `:` + 1 ila 5 rakam. Başka her şey bilinmeyen izindir ve eklenti *Error* olur. IPv6 adresleri kabul edilmez. |
| Şemalar | Yalnızca `http://` ve `https://`. |
| Yöntemler | `GET` ve `POST`. POST gövdesi her zaman JSON olarak gönderilir. |
| Yönlendirmeler | İzlenmez. Bir yönlendirme isteği kimsenin onaylamadığı bir ana bilgisayara götürebilir. Son adrese kendiniz istek atın. |
| Zaman aşımı | İstek başına 5 saniye. |
| Yanıt boyutu | En çok 1 MB metin. Daha büyük yanıt hata verir. |
| Eşzamanlılık | Async çağrılar: eklenti başına en çok 4 istek aynı anda. |

## Kullanıcı ne görür

Eklenti çalışmadan önce (ve bir klasörden kurulmadan önce) Eklentiler penceresi izinleri sade bir dille listeler:

| İzin | Gösterilen |
|---|---|
| `variables` | Değişkenleri okumak ve kendi değişkenlerini yayınlamak |
| `actions` | Kendi aksiyonlarını eklemek |
| `input` | Bu bilgisayarda tuşlara basmak ve yazı yazmak |
| `http:<host>:<port>` | *hedef* adresine web istekleri göndermek: **bu bilgisayarda**, **yerel ağınızda** veya **internette (ağınızın dışına veri gönderebilir)** |

HTTP satırı, hedefin nerede olduğuna göre etiketlenir: `localhost`, `127.x.x.x` ve `::1` *bu bilgisayar*; `10.x`, `172.16-31.x`, `192.168.x`, `169.254.x`, tek sözcüklü adlar (`nas`) ve `.local`, `.lan`, `.home.arpa` veya `.internal` ile biten adlar *yerel ağ*; geri kalan her şey *internettir*. Kullanıcılar internet hedeflerine daha dikkatli bakar; mümkün olan en dar hedefi isteyin.

## Onay

1. Kullanıcı onaylayana kadar eklentinin durumu *Needs approval* (onay bekliyor) kalır. Betiğin hiçbir kısmı ondan önce çalışmaz.
2. Onay **tam o küme** içindir. Bir izin (veya yeni bir `http:` hedefi) ekleyen güncelleme yeniden onay bekler.
3. Eklenti kaldırılınca onay unutulur. Yeniden kurmak yeniden sorar.
4. Onaylar, kurulumlar ve kaldırmalar günlük dosyalarına yazılır.

## İzin gerektirmeyenler

| Çağrı | Not |
|---|---|
| `host.log` | Sunucu günlüğüne yazılır, satır başına 500 karakter. |
| `host.settings.page / get` | Eklenti klasöründeki `settings.json` içinde saklanan ayar formu. |
| `host.status` | Eklenti başına en çok 10 durum çubuğu öğesi. |
| `host.every / after / cancel` | En çok 20 zamanlayıcı, en az 100 ms. |
| `host.permissions` | Verilen izinlerin listesi; betik buna göre davranabilir. |

## Hatalar

- `plugin.json` içinde **bilinmeyen izin dizesi**: eklenti *Error* olur ve başlamaz.
- **İzni olmayan çağrı**, betiğin `try/catch` ile yakalayabileceği sıradan bir JavaScript `Error` fırlatır. Mesajlar şöyledir: `This plugin has not been granted the 'input' permission.` veya `This plugin has not been granted 'http:localhost:4455'.`
- **Hatalı adlar** (`Variable names must start with 'myplugin.'`) izinler ne olursa olsun aynı şekilde hata fırlatır.
- Üst üste beş hata eklentiyi kapatır; bkz. [JavaScript host API](/tr/reference/js-host-api).

## İzin seçimi

| Eklenti | İstenecek |
|---|---|
| hesaplanan veya çekilen bir değeri gösteriyor | `variables` (+ çekiyorsa `http:...`) |
| kendi kodunda bir şey yapan buton aksiyonu ekliyor | `actions` |
| sistem değerlerine tepki veriyor (`system.cpu` yükselince durumu kırmızı yapmak gibi) | `variables` |
| bir şey olunca kısayol gönderiyor | `input` (+ buton tetikliyorsa `actions`) |
| HTTP API'si olan yerel bir uygulamayla konuşuyor | `http:localhost:<port>` |

Yalnızca kullandığınız şeyi isteyin. Hiçbir şey istemeyen eklenti "özel bir izin istemiyor" olarak görünür.

```json
"permissions": ["variables", "actions", "http:localhost:4455"]
```

::: tip
`host.permissions`, kullanıcının verdiklerinin dondurulmuş bir dizisidir. Eklenti bunu kontrol edip, örneğin isteğe bağlı bir özelliği hata vermek yerine gizleyebilir.
:::

C# eklentilerinde izin kapısı yoktur ve yalnızca resmi depoya özeldir; bu sayfa onları kapsamaz.
