# JavaScript host API

`"kind": "js"` olan bir eklenti, sunucunun içinde yalıtılmış bir ortamda (sandbox) çalışan tek bir betiktir (`entry`, genellikle `index.js`). Her şey, genel ve salt okunur `host` nesnesi üzerinden geçer. `require`, `fetch`, dosya erişimi ve .NET'e erişim yoktur.

```js
host.log(message)

host.variables.set(name, value)      // number, string, boolean or null
host.variables.get(name)
host.variables.remove(name)
host.variables.describe([{ name, description, example, category }])   // düzenleyicinin değişken seçicisinde listelenir

host.registerAction({ type, name, category, description, icon, fields, run(context, settings) {} })
// context: { deviceId, pageId, widgetId, value }; settings: aksiyonun alanlarının değerleri
// fields: C# SettingField ile aynı biçim, örn. { key, label, kind: 'Text' | 'Number' | 'Bool' | 'Select' | ..., default, min, max, options }

host.settings.page(fields)           // bir ayar sayfası ekler (eklenti klasöründeki settings.json içinde saklanır)
host.settings.get()                  // güncel değerler, bir nesne olarak
host.status(id, text, level)         // durum çubuğu öğesi; level: 'Idle' | 'Ok' | 'Busy' | 'Warning' | 'Error'

host.input.hotkey('ctrl+shift+m')    // 'input' gerekir
host.input.type('hello')             // 'input' gerekir

host.http.get(url, { headers })          // http:<host>:<port> gerekir; { status, body } döner (body metindir), eşzamanlıdır
host.http.post(url, body, { headers })   // body JSON olarak gönderilir

const id = host.every(ms, fn)        // tekrarlar; en kısa aralık 100 ms
host.after(ms, fn)                   // bir kez
host.cancel(id)
host.permissions                     // verilen izinler
```

`variables`, `settings`, `registerAction`, `status` ve `every` kullanan eksiksiz bir betik
[Eğitim 1](/tr/tutorials/js-hello-world) sayfasındadır.

## Hangi izin ne yapar

| Çağrı | Gerektirdiği izin |
|---|---|
| `host.variables.*` | `variables` |
| `host.registerAction` | `actions` |
| `host.input.*` | `input` |
| `host.http.*` | tam o hedef için `http:<host>:<port>` |
| `host.log`, `host.settings.*`, `host.status`, zamanlayıcılar | hiçbiri |

Bkz. [İzinler](/tr/reference/permissions).

## Sunucunun uyguladığı kurallar

- **Adlar sizindir.** Değişken adları ve aksiyon türleri `<plugin id>.` ile başlamalıdır; böylece bir eklenti `system.cpu` değişkenini veya başka bir eklentinin değerlerini asla ezemez.
- **En üst düzeyde kaydedin.** Aksiyonlar, ayar sayfası ve değişken açıklamaları betik ilk çalışırken kaydedilmelidir; sonradan bir geri çağrıdan (callback) yapılan kayıtlar yok sayılır. Değişkenler her an ayarlanabilir.
- **Süre ve bellek çağrı başına sınırlıdır** (başlangıç, her aksiyon, her zamanlayıcı tıkı): 2 saniye, 32 MB, 2 milyon ifade ve 100 özyineleme derinliği. Sınırı aşan çağrı hatayla başarısız olur; eklenti çalışmaya devam eder. HTTP istekleri 5 saniye sonra zaman aşımına uğrar, yanıtlar 1 MB ile sınırlıdır ve yönlendirmeler izlenmez.
- **Aynı anda tek iş.** Betik kendi iş parçacığında, bir seferde tek çağrı olarak çalışır; bu yüzden yavaş bir eklenti sunucuyu veya başka bir eklentiyi asla engellemez. `host.http`, beklerken eklentinin kendi diğer geri çağrılarını engeller. Eklenti başına en fazla 20 zamanlayıcı vardır; betik meşgulken biriken tıklar atılır.
- **Art arda 5 kez başarısız olan eklenti kapatılır** (durum, son iletiyle birlikte *Error*). Yeniden yükleme onu tekrar başlatır.

Henüz `async`/`await` host API'si yoktur ve bir eklentinin kendi widget'ını çizmesinin yolu yoktur (bir `plugin-html` widget'ı planlanmaktadır).

## Alan tanımları

`fields` (bir aksiyon veya ayar sayfası için) C# `SettingField` ile aynı biçimi kullanır; bkz.
[Ayar sayfaları](/tr/guides/settings-pages). `kind`, `Text`, `Password`, `Number`, `Slider`, `Bool`,
`Select`, `Segmented` değerlerinden biridir.
