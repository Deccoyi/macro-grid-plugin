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
// fields: örn. { key, label, kind: 'Text' | 'Number' | 'Bool' | 'Select' | ..., default, min, max, options }

host.settings.page(fields)           // bir ayar sayfası ekler (eklenti klasöründeki settings.json içinde saklanır, en fazla 64 KB)
host.settings.get()                  // güncel değerler, bir nesne olarak
host.status(id, text, level)         // durum çubuğu öğesi; level: 'Idle' | 'Ok' | 'Busy' | 'Warning' | 'Error'

host.input.hotkey('ctrl+shift+m')    // 'input' gerekir; yalnızca bir düğme basışı işlenirken, aşağıya bakın
host.input.type('hello')             // 'input' gerekir; en fazla 200 karakter

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
| `host.input.*` | `input` ve yalnızca bir düğme basışı işlenirken |
| `host.http.*` | tam o hedef için `http:<host>:<port>` |
| `host.log`, `host.settings.*`, `host.status`, zamanlayıcılar | hiçbiri |

Bkz. [İzinler](/tr/reference/permissions).

## Sunucunun uyguladığı kurallar

- **Adlar sizindir.** Değişken adları ve aksiyon türleri `<plugin id>.` ile başlamalıdır; böylece bir eklenti `system.cpu` değişkenini veya başka bir eklentinin değerlerini asla ezemez.
- **En üst düzeyde kaydedin.** Aksiyonlar, ayar sayfası ve değişken açıklamaları betik ilk çalışırken kaydedilmelidir; sonradan bir geri çağrıdan (callback) yapılan kayıtlar yok sayılır. Değişkenler her an ayarlanabilir.
- **Süre ve bellek çağrı başına sınırlıdır** (başlangıç, her aksiyon, her zamanlayıcı tıkı): 2 saniye, 32 MB, 2 milyon ifade ve 100 özyineleme derinliği. Sınırı aşan çağrı hatayla başarısız olur; eklenti çalışmaya devam eder. HTTP istekleri 5 saniye sonra zaman aşımına uğrar, yanıtlar 1 MB ile sınırlıdır ve yönlendirmeler izlenmez.
- **Aynı anda tek iş.** Betik kendi iş parçacığında, bir seferde tek çağrı olarak çalışır; bu yüzden yavaş bir eklenti sunucuyu veya başka bir eklentiyi asla engellemez. `host.http`, beklerken eklentinin kendi diğer geri çağrılarını engeller. Eklenti başına en fazla 20 zamanlayıcı vardır; betik meşgulken biriken tıklar atılır.
- **Art arda 5 kez başarısız olan eklenti kapatılır** (durum, son iletiyle birlikte *Error*). Yeniden yükleme onu tekrar başlatır.

Bir eklentinin kendi widget'ını çizmesinin yolu yoktur (bir `plugin-html` widget'ı planlanmaktadır).

## Klavye girdisi

`host.input.hotkey` ve `host.input.type` yalnızca bir cihaza dokunuşla başlayan bir aksiyonun (ve promise'inin) içinde, en fazla 5 saniye, basış başına en fazla 200 yazılan karakter
ve 10 tuş kombinasyonuyla çalışır; Windows tuşuyla asla, önde bir terminal, sistem aracı ya da Macro Grid penceresi varken asla çalışmaz; zararlı bir komuta benzeyen metin eklentiyi
kapatır. Nedenler ve ayrıntılar [İzinler](/tr/reference/permissions#input-izni) sayfasındadır.

## Alan tanımları

`fields` (bir aksiyon veya ayar sayfası için) düz nesnelerdir; bkz. [Ayar sayfaları](/tr/guides/settings-pages#alan-turleri-ve-secenekler).
`kind`, `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select`, `Segmented`, `File`, `List`, `Button`, `Notice` değerlerinden biridir. `Button` sunucuda kod gerektirir, bu yüzden yalnızca resmî C# eklentilerinde işe yarar.
