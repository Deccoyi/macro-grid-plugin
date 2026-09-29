# JavaScript host API

`"kind": "js"` olan bir eklenti, sunucunun içinde yalıtılmış bir ortamda (sandbox) çalışan tek bir betiktir (`entry`, genellikle `index.js`). Her şey, genel ve dondurulmuş `host` nesnesi üzerinden geçer. `require`, `import`, `fetch`, `setTimeout`, dosya erişimi ve .NET'e erişim yoktur. Betik katı modda (strict) ve modern JavaScript ile çalışır; `Promise` ve `async` fonksiyonlar kullanılabilir.

Bu sayfa her fonksiyonu, her seçeneği ve her sınırı listeler. İzin tarafı için bkz. [İzinler](/tr/reference/permissions); bu parçalarla yapılabilecekler için [Neler yapabilirsiniz](/tr/guides/js-recipes).

## Bir bakışta

| Alan | Fonksiyonlar | İzin |
|---|---|---|
| [Günlük](#host-log) | `host.log` | yok |
| [Değişkenler](#host-variables) | `host.variables.set / get / remove / describe` | `variables` |
| [Aksiyonlar](#host-registeraction) | `host.registerAction` | `actions` |
| [Ayar sayfası](#host-settings) | `host.settings.page / get` | yok |
| [Durum çubuğu](#host-status) | `host.status` | yok |
| [Klavye](#host-input) | `host.input.hotkey / type` | `input`, yalnızca buton basışı sırasında |
| [HTTP](#host-http) | `host.http.get / post / getAsync / postAsync` | `http:<host>:<port>` |
| [Zamanlayıcılar](#host-every-host-after-host-cancel) | `host.every / after / cancel` | yok |
| [Verilen izinler](#host-permissions) | `host.permissions` | yok |
| Çeviriler | `plugin.json` yanında `locales/<language>.json` | yok |

İki kural her yerde geçerlidir:

- **Adlar sizindir.** Değişken adları ve aksiyon türleri `<plugin id>.` ile başlamalıdır.
- **En üst düzeyde kaydedin.** Aksiyonlar, ayar sayfası ve değişken açıklamaları betik ilk çalışırken toplanır. Sonradan, bir zamanlayıcıdan veya aksiyondan yapılan kayıtlar yok sayılır. Değişkenler her zaman ayarlanabilir.

## host.log

```js
host.log('connected to ' + url)
```

Sunucu günlüğüne, başında eklenti kimliğiyle bir satır yazar (bkz. [Hata ayıklama](/tr/guides/debugging)). Argüman dizeye çevrilir ve 500 karakterde kesilir.

## host.variables

`variables` gerekir. Değişken, widget'ların `{ad}` olarak gösterdiği ve dinamik kuralların ile koşulların kullanabildiği canlı bir değerdir.

```js
host.variables.set('myplugin.temp', 21.5)   // number, string, boolean or null
host.variables.get('system.cpu')            // herhangi bir değişken, sizin olması gerekmez
host.variables.remove('myplugin.temp')
host.variables.describe([
  { name: 'myplugin.temp', description: 'Room temperature', example: '21.5', category: 'Home',
    type: 'number', unit: '°C' },
  { name: 'myplugin.mode', description: 'Current mode', example: 'eco', category: 'Home',
    type: 'text', values: ['eco', 'comfort', 'away'] },
])
```

| Çağrı | Ne yapar |
|---|---|
| `set(name, value)` | Değişkenlerinizden birini yayınlar veya günceller. `undefined` `null` olur; başka her tür (nesne, dizi) dizeye çevrilir. |
| `get(name)` | `system.*` ve başka eklentilerinkiler dahil, herhangi bir değişkenin güncel değerini okur. Olmayan değişken `null` verir. |
| `remove(name)` | Değişkenlerinizden birini kaldırır. |
| `describe(list)` | Değişkenleri düzenleyicinin değişken seçicisinde listeler; kimse adı tahmin etmek zorunda kalmaz. En üst düzeyde bir kez çağırın. |

**Adlar** `<plugin id>.` ile başlamalı, en çok 120 karakter olmalı ve yalnızca harf, rakam, `.`, `_` ve `-` içermelidir.

**`describe` girdileri:**

| Alan | Anlamı |
|---|---|
| `name` | Eklenti kimliği önekiyle değişken adı. |
| `description` | Seçicide gösterilen tek satır. |
| `example` | Yanında gösterilen örnek değer. |
| `category` | Seçicide altında göründüğü grup. Kendi kategori adınızı serbestçe uydurabilirsiniz. |
| `type` | `'text'` (varsayılan), `'number'`, `'boolean'`, `'duration'` veya `'dateTime'`. Düzenleyici değeri göstermek ve koşulda doğru girdiyi sunmak için kullanır. |
| `unit` | Sayı için: değer girdisinin yanında gösterilen birim, örneğin `'%'`, `'GB'`, `'kbps'`. |
| `values` | Sabit seçenekli metin için: izin verilen değerler; koşullarda liste olarak sunulur. |

**Değer türleri.** Sayılar, dizeler ve boolean'lar olduğu gibi saklanır. Widget metninde sayı biçim alır (`{myplugin.temp|0.0}`), boolean widget kendi sözcüklerini vermedikçe *Açık/Kapalı* yazar (`{myplugin.on|ON/OFF}`); bkz. [Değişkenler ve metin](https://deccoyi.github.io/macro-grid/tr/guide/variables). Açıklamalar, kategoriler ve birimler eklentinin çeviri dosyasından geçer.

Okuyabileceğiniz yerleşik değişkenler arasında `system.time`, `system.cpu`, `system.ram`, `system.ram.used`, `system.ram.total`, `system.uptime`, `system.audio.master` ve `system.audio.muted` vardır; ayrıca başka eklentilerin yayınladıkları (örneğin `obs.*`).

## host.registerAction

`actions` gerekir.

```js
host.registerAction({
  type: 'myplugin.setLight',      // zorunlu, eklenti kimliğiyle başlamalı
  name: 'Set light',              // seçicideki ad (varsayılan: type)
  category: 'Home',               // seçicideki grup (varsayılan: 'Plugins')
  description: 'Turns a light on or off',
  icon: 'lightbulb',              // isteğe bağlı simge adı, seçicide gösterilir
  fields: [ /* aksiyonun kendi formu, bkz. Alan türleri */ ],
  run(context, settings) { /* ... */ },   // zorunlu
})
```

| Özellik | Anlamı |
|---|---|
| `type` | Benzersiz kimlik, profillerde saklanır. Yeniden adlandırmak kayıtlı butonları bozar; sabit tutun. |
| `name`, `category`, `description`, `icon` | Düzenleyicinin aksiyon seçicisinde görünenler. Hepsi eklentinin çeviri dosyasından geçer. |
| `fields` | Aksiyon bağlanırken kullanıcının doldurduğu form. Ayar sayfasıyla aynı biçim. Bkz. [Alan türleri](/tr/reference/js-field-kinds). |
| `run(context, settings)` | Bağlı widget olayı tetiklenince çağrılır. `async` olabilir. |

**`context`**

| Özellik | Anlamı |
|---|---|
| `deviceId` | Tetikleyen telefon veya tarayıcı deck'i. |
| `pageId` | Widget'ın bulunduğu sayfa. |
| `widgetId` | Tetikleyen widget. |
| `value` | Slider veya knob için (*Değer değişti*): kullanıcının sürüklediği değer. Diğer tüm olaylarda `null`. |

**`settings`**, aksiyon formunun her alan `key`'i için bir girdi içeren nesnedir (kullanıcının değiştirmediği değerler alanın `default`'unu taşır).

Aksiyonun hangi olaylara bağlanacağını kullanıcı belirler: basma, bırakma, uzun basma, çift dokunma, aç/kapa, değer değişti. Tek aksiyon hepsine hizmet eder; slider'a tepki vermek için `context.value`'yu okuyun.

Aksiyon, `run` döndüğünde bitmiş sayılır. `async run` içinde `await` ettiğiniz işler bundan sonra da sürer.

## host.settings

```js
host.settings.page([
  { key: 'url',  label: 'Server URL', kind: 'Text', default: 'http://localhost:4455' },
  { key: 'poll', label: 'Poll every (s)', kind: 'Number', default: 5, min: 1, max: 60 },
])
const { url, poll } = host.settings.get()
```

- `page(fields)` Eklentiler penceresine bir ayar formu ekler (dişli buton). En üst düzeyde çağırın. İkinci çağrı birincisinin yerini alır.
- `get()` güncel değerleri nesne olarak verir: `default`'ların üstüne kayıtlı değerler. Sayfa yoksa `{}` döner. Taze değer gerektiğinde yeniden çağırın; kullanıcı eklenti çalışırken değiştirebilir.
- Değerler eklenti klasöründeki `settings.json` içinde saklanır. Yalnızca bildirdiğiniz anahtarlar tutulur ve kaydedilen dosya en çok 64 KB olabilir.
- JavaScript eklentilerinde `Password` alanı düz metin olarak saklanır; README'nizde belirtin.

Her tür ve seçenek için bkz. [Alan türleri](/tr/reference/js-field-kinds).

## host.status

```js
host.status('conn', 'Connected', 'Ok')
```

Düzenleyicinin pencere genelindeki durum çubuğunda bir girdi oluşturur veya günceller. `id` sizindir (eklenti başına). `text` 80 karakterde kesilir. `level` `'Idle'` (varsayılan ve bilinmeyenler için yedek), `'Ok'`, `'Busy'`, `'Warning'` veya `'Error'` olabilir; rengi belirler. Eklenti başına en çok 10 öğe. Eklentinin ayar sayfası varsa öğeye tıklamak onu açar. Metin ve ipucu çeviri dosyasından geçer; içinde değer olan metin şablon olarak çevrilebilir (anahtar `"Retrying in {0}s"`).

## host.input

`input` gerekir ve **yalnızca bir buton basışı işlenirken** çalışır: bir aksiyonun `run`'ı (ve döndürdüğü promise) içinde, en çok 5 saniye. Zamanlayıcıdan veya başlangıçta `Keyboard input is only allowed while handling a button press.` hatası verir. Sınırların tamamı [İzinler](/tr/reference/permissions#input-izni) sayfasındadır.

```js
host.input.hotkey('ctrl+shift+m')
host.input.type('hello')             // çağrı başına en çok 200 karakter, basış başına 200
```

- Basış başına en çok 10 tuş kombinasyonu ve 200 yazılan karakter.
- Windows tuşlu (`win`) kombinasyonlar ile `ctrl+escape`, `ctrl+alt+delete` reddedilir.
- Terminal, betik ana bilgisayarı, sistem aracı, sistem iletişim kutusu veya Macro Grid penceresi öndeyken ya da Macro Grid yönetici olarak çalışırken hiçbir şey gönderilmez.
- Zararlı komut gibi görünen metin `This text is not allowed.` hatası verir ve eklentiyi kapatır.

**Kombinasyon sözdizimi:** `+` ile bağlanmış tuşlar, en çok bir değiştirici olmayan tuş, büyük/küçük harfe duyarsız. Gerçek `+` tuşu `plus` yazılır. Hatalı kombinasyon nedeniyle bir `Error` fırlatır (`Unknown key: 'foo'.`, `More than one key: ...`).

| Grup | Adlar |
|---|---|
| Değiştiriciler | `ctrl` (`control`), `shift`, `alt` (`option`). `win` (`windows`, `meta`, `cmd`) ayrıştırılır ama eklentiler için reddedilir. |
| Harf ve rakamlar | `a`–`z`, `0`–`9`, `num0`–`num9` |
| Fonksiyon tuşları | `f1`–`f24` |
| Gezinme | `up`, `down`, `left`, `right`, `home`, `end`, `pageup` (`pgup`), `pagedown` (`pgdn`), `insert` (`ins`), `delete` (`del`) |
| Düzenleme | `enter` (`return`), `escape` (`esc`), `tab`, `space`, `backspace` (`bksp`) |
| Kilitler ve diğer | `printscreen` (`prtsc`), `pause`, `capslock`, `numlock`, `scrolllock`, `menu` |
| Noktalama | `plus`, `minus`, `comma`, `period`, `semicolon`, `slash`, `backslash`, `quote`, `backquote`, `bracketleft`, `bracketright`, `equal` |
| Sayısal tuş takımı | `numadd`, `numsubtract`, `nummultiply`, `numdivide`, `numdecimal` |
| Medya | `volumeup`, `volumedown`, `volumemute`, `mediaplaypause`, `medianext`, `mediaprev`, `mediastop` |

Tuşlar odaktaki pencereye gider; basış kuralı bu yüzden vardır: kullanıcı az önce bilerek bir butona dokundu.

## host.http

Her URL'nin tam hedefi için `http:<host>:<port>` gerekir.

```js
const r = host.http.get('http://localhost:4455/status', { headers: { Authorization: 'Bearer x' } })
// r = { status: 200, body: '...metin...' }
const data = JSON.parse(r.body)

host.http.post('http://localhost:4455/scene', { name: 'Intro' })   // gövde JSON olarak gider

const r2 = await host.http.getAsync(url)          // promise, engellemez
await host.http.postAsync(url, { on: true }, { headers: {} })
```

| Çağrı | Döner |
|---|---|
| `get(url, { headers })` | `{ status, body }`, engelleyici |
| `post(url, body, { headers })` | `{ status, body }`, engelleyici. `body` `JSON.stringify`'dan geçer ve `Content-Type: application/json` ile gider; yani dize tırnaklı gider. |
| `getAsync(url, { headers })` | `{ status, body }` için bir promise |
| `postAsync(url, body, { headers })` | `{ status, body }` için bir promise |

- `status` HTTP durum kodudur; 404 veya 500 istisna **değildir**. `body` metindir; ayrıştırmayı kendiniz yaparsınız.
- Reddedilen, başarısız olan veya zaman aşımına uğrayan istek `Error` fırlatır (engelleyici) veya promise'i reddeder (async).
- **Engelleyici ve async:** `get`/`post`, yanıt gelene kadar eklentinin iş parçacığını tutar; zamanlayıcıları ve diğer aksiyonları bekler. Diğer her şeyin çalışmaya devam etmesi için `async` aksiyon ve zamanlayıcılarda async çağrıları kullanın. Eklenti başına aynı anda en çok 4 async istek.
- Kurallar (tam ana bilgisayar ve port, yönlendirme yok, 5 sn zaman aşımı, 1 MB, yalnızca GET ve POST) [İzinler](/tr/reference/permissions#http-host-port) sayfasındadır.

## host.every, host.after, host.cancel

```js
const id = host.every(5000, () => { /* tekrarlar */ })
host.after(1000, () => { /* bir kez */ })
host.cancel(id)
```

İzin gerekmez. En kısa aralık 100 ms, eklenti başına en çok 20 zamanlayıcı. Meşgul bir eklentinin zamanlayıcıları birikmez: betik hâlâ meşgulken gelen tik atılır. Geri çağrı, eklentinin diğer her şeyiyle aynı tek iş parçacığında çalışır. Geri çağrılar `async` olabilir.

## host.permissions

Kullanıcının verdiği izin dizelerinin dondurulmuş dizisi, örneğin `['variables', 'http:localhost:4455']`.

## Çeviriler

Her metni tek bir dilde yazın (`plugin.json` içindeki `defaultLanguage`, varsayılan `en`) ve `plugin.json` yanına `locales/<language>.json` ekleyin: metninizi çevirisine eşleyen tek bir nesne.

```json
{ "Set light": "Işığı ayarla", "Home": "Ev", "Retrying in {0}s": "{0} sn içinde yeniden denenecek" }
```

Çevrilenler: aksiyon adları, açıklamaları ve kategorileri, değişken açıklamaları ve kategorileri, ayar formu etiketleri, durum metinleri ve ipuçları. Eksik dosya veya girdi, metnin yazıldığı haline döner.

## Sınırlar ve hatalar

| Sınır | Değer |
|---|---|
| Betiğin başlaması | 10 saniye |
| Betiğe her giriş (ilk çalıştırma, bir aksiyon, bir zamanlayıcı tiki) | 2 saniye, 32 MB, 2 milyon ifade, özyineleme derinliği 100. Aşan çağrı hatayla biter; eklenti çalışmaya devam eder. |
| Zamanlayıcılar | Eklenti başına 20, en az 100 ms |
| Durum öğeleri | Eklenti başına 10 |
| HTTP | İstek başına 5 sn, 1 MB yanıt, 4 async istek aynı anda |
| Günlük satırı | 500 karakter |
| `host.input` | Basış başına 200 yazılan karakter ve 10 tuş kombinasyonu, 5 saniyelik basış penceresi |
| `settings.json` | 64 KB |

- **Aynı anda tek iş.** Betik kendi iş parçacığında, bir seferde bir çağrı çalışır; yavaş bir eklenti sunucuyu veya başka eklentiyi asla engellemez.
- **Üst üste beş hata eklentiyi kapatır.** Durumu son mesajla *Error* olur. **Reload** yeniden başlatır. Tek başarılı çağrı sayacı sıfırlar.
- Betik ilk çalışırken hata fırlatırsa eklenti mesajla birlikte *Error* görünür.

## Henüz mümkün olmayanlar

- Eklenti kendi widget'ını çizemez (bir `plugin-html` widget'ı planlı), simge paketi veya yeni widget türü ekleyemez.
- Dosya, soket, WebSocket, `fetch`, `setTimeout` (yerine `host.after`), modül yok.
- Ayar formunda buton yok (`Button` alanı sunucuda kod ister, bu yüzden yalnızca resmî C# eklentilerinde vardır).
- Dinamik açılır liste yok: `Select` seçenekleri sabittir (dinamik listeler yalnızca resmî C# eklentilerindedir).
- JavaScript'ten bir telefonun hangi sayfayı veya profili gösterdiğini seçmek mümkün değil.
