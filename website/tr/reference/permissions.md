# İzinler

İzinler **JavaScript eklentileri** için geçerlidir; üçüncü taraflar yalnızca bu türü yazabilir. (C# eklentileri resmî ve imzalıdır; sunucu başkasını yüklemez ve izin kullanmazlar.) Bir betik hiçbir yetkisiz başlar: yalnızca hesaplama yapabilir, zamanlayıcı kurabilir, ayar sayfası gösterebilir, günlüğe yazabilir ve durum çubuğunu güncelleyebilir. Dış dünyaya dokunan her şey (diğer değişkenler, klavye, ağ, aksiyon listesi) kullanıcının onayladığı bir izin ister.

## Dört izin

| İzin | Betiğin yapmasına izin verir | Açtığı çağrılar |
|---|---|---|
| `variables` | sunucudaki **herhangi** bir değişkeni okumak; **kendi** değişkenlerini yayınlamak, değiştirmek ve silmek | `host.variables.set / get / remove / describe` |
| `actions` | düzenleyicinin aksiyon seçicisine kendi aksiyonlarını eklemek | `host.registerAction` |
| `input` | bilgisayarda tuş kombinasyonlarına basmak ve metin yazmak; yalnızca bir buton basışı işlenirken ([sınırlar](#input-izni)) | `host.input.hotkey / type` |
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

::: danger `input` vermeden önce eklentiye güvenin
`input` izinli bir eklenti bilgisayarınızda tuşlara basabilir ve yazı yazabilir. Macro Grid sınırlar koyar (aşağıya bakın), ama bunlar
kötüye kullanımı yalnızca zorlaştırır; tamamen engelleyemez. Üçüncü taraf eklentileri **yalnızca güvendiğiniz kaynaklardan** kurun,
onaylamadan önce izinleri okuyun ve tetikte olun.
:::

- `host.input.hotkey('ctrl+shift+m')` bir tuş kombinasyonu gönderir, `host.input.type('metin')` klavyenin başındaymış gibi metin yazar.
- En güçlü yerel izindir; bu yüzden sunucu onu sıkı sınırlar: yalnızca kullanıcının kendi buton basışı işlenirken, küçük miktarlarda ve hiçbir zaman bir terminale veya sistem aracına çalışır. Bkz. [`input` izni](#input-izni).
- Yalnızca eklentinin asıl amacı girdi göndermekse isteyin ve ne yapacağını README'nizde belirtin.
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
| `input` | Butonlarından birine bastığınızda, klavyenizin başındaymış gibi bilgisayarınızda tuşlara basabilir ve yazı yazabilir. Yalnızca güvendiğiniz eklentilere izin verin. |
| `http:<host>:<port>` | *hedef* adresine web istekleri göndermek: **bu bilgisayarda**, **yerel ağınızda** veya **internette (ağınızın dışına veri gönderebilir)** |

HTTP satırı, hedefin nerede olduğuna göre etiketlenir: `localhost`, `127.x.x.x` ve `::1` *bu bilgisayar*; `10.x`, `172.16-31.x`, `192.168.x`, `169.254.x`, tek sözcüklü adlar (`nas`) ve `.local`, `.lan`, `.home.arpa` veya `.internal` ile biten adlar *yerel ağ*; geri kalan her şey *internettir*. Kullanıcılar internet hedeflerine daha dikkatli bakar; mümkün olan en dar hedefi isteyin.

## Onay

1. Kullanıcı onaylayana kadar eklentinin durumu *Needs approval* (onay bekliyor) kalır. Betiğin hiçbir kısmı ondan önce çalışmaz.
2. Onay **tam o küme** içindir. Bir izin (veya yeni bir `http:` hedefi) ekleyen güncelleme yeniden onay bekler.
3. Eklenti kaldırılınca onay unutulur. Yeniden kurmak yeniden sorar.
4. Onaylar, kurulumlar ve kaldırmalar günlük dosyalarına yazılır.

## `input` izni

::: danger `input` vermeden önce eklentiye güvenin
`input` izinli bir eklenti bilgisayarınızda tuşlara basabilir ve yazı yazabilir. Macro Grid sınırlar koyar (aşağıya bakın), ama bunlar
kötüye kullanımı yalnızca zorlaştırır; tamamen engelleyemez. Üçüncü taraf eklentileri **yalnızca güvendiğiniz kaynaklardan** kurun,
onaylamadan önce izinleri okuyun ve tetikte olun.
:::

`input`, eklentinin klavyenin başındaymış gibi tuşlara basmasına ve yazı yazmasına izin verir. Asıl karar kullanıcının onayıdır. Bunun üstüne sunucu, JavaScript eklentileri için şu sınırları uygular (kullanıcının kendi yapılandırdığı yerleşik kısayol ve metin yazma aksiyonları değişmez):

- **Yalnızca gerçek bir basış sırasında.** `host.input.*`, bir cihazda dokunuşla başlamış aksiyonun içinde ve yalnızca o aksiyon ile döndürdüğü promise bitene kadar ya da 5 saniye geçene kadar (hangisi önce olursa) çalışır. Zamanlayıcıdan, başlangıçta veya basış sırasında başlatılmamış bir web isteğinden `Keyboard input is only allowed while handling a button press.` hatası verir. `async` bir aksiyon önce bir web isteğini `await` edip sonra yazabilir; 5 saniye içinde kalmak şartıyla.
- **Küçük miktarlar.** Basış başına en çok 200 yazılan karakter ve 10 tuş kombinasyonu; tek bir `host.input.type` çağrısı en çok 200 karakter alır. Fazlası hata verir.
- **Reddedilen kombinasyonlar.** Windows tuşlu her şey (Başlat, Çalıştır, güç kullanıcısı menüsü, ayarlar) ve Görev Yöneticisi'ni veya güvenlik ekranını açanlar (`ctrl+escape`, `ctrl+alt+delete`). Kullanıcı bunları yine yerleşik kısayol aksiyonuyla kullanabilir.
- **Reddedilen hedef pencereler.** Komut istemi, kabuk veya terminal, betik ana bilgisayarı, kayıt defteri düzenleyicisi, sistem yönetim konsolu, Görev Yöneticisi, Başlat menüsü, Çalıştır gibi sistem iletişim kutusu ya da bir Macro Grid penceresi öndeyken (bir eklenti kendi onayına asla tıklayamamalı), öndeki pencere tanınamıyorsa veya Macro Grid yönetici olarak çalışıyorsa hiçbir şey gönderilmez.
- **Zararlı metin kara listesi.** Bir basışta yazılan metin normalleştirilir (küçük harf, boşluklar tekleştirilir, kaçış karakterleri ve tırnaklar kaldırılır) ve kabuk veya betik ana bilgisayarı başlatma, bir şey indirip çalıştırma, kodlanmış komutlar, kayıt defterini, servisleri, zamanlanmış görevleri, kullanıcıları veya güvenlik duvarını değiştirme ve sürücü silme veya biçimlendirme gibi şeyler için denetlenir. Eşleşmede çağrı `This text is not allowed.` hatası verir, eklenti kapatılır ve günlüğe kuralın adını (metni asla) belirten bir `Security:` satırı yazılır. Bu zayıf kuraldır: aşılabilir ve meşru metni reddedebilir. Gizli kötüye kullanımı gerçekten durduran basış kuralı ve pencere kuralıdır.
- **Görünür kullanım.** Klavyeyi kullanan her basış sayılır ve eklentinin Eklentiler penceresindeki satırında günlük kullanım sayısı görünür.

**Bunun çözmediği:** `input` izinli onaylı bir eklenti, butonuna bastığınızda sıradan bir programa yine de 200 karaktere kadar yazabilir. `input` iznini yalnızca güvendiğiniz eklentilere verin.

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
- **Basış dışında veya reddedilen klavye:** `Keyboard input is only allowed while handling a button press.`, `Keyboard input is refused while a terminal, script host or system tool is in front.` ve benzerleri.
- **Hatalı adlar** (`Variable names must start with 'myplugin.'`) izinler ne olursa olsun aynı şekilde hata fırlatır.
- Üst üste beş hata eklentiyi kapatır; bkz. [JavaScript host API](/tr/reference/js-host-api).

## İzin seçimi

| Eklenti | İstenecek |
|---|---|
| hesaplanan veya çekilen bir değeri gösteriyor | `variables` (+ çekiyorsa `http:...`) |
| kendi kodunda bir şey yapan buton aksiyonu ekliyor | `actions` |
| sistem değerlerine tepki veriyor (`system.cpu` yükselince durumu kırmızı yapmak gibi) | `variables` |
| kullanıcı butona basınca kısayol veya metin gönderiyor | `input` + `actions` (girdi yalnızca aksiyon içinde çalışır) |
| HTTP API'si olan yerel bir uygulamayla konuşuyor | `http:localhost:<port>` |

Yalnızca kullandığınız şeyi isteyin. Hiçbir şey istemeyen eklenti "özel bir izin istemiyor" olarak görünür.

```json
"permissions": ["variables", "actions", "http:localhost:4455"]
```

::: tip
`host.permissions`, kullanıcının verdiklerinin dondurulmuş bir dizisidir. Eklenti bunu kontrol edip, örneğin isteğe bağlı bir özelliği hata vermek yerine gizleyebilir.
:::
