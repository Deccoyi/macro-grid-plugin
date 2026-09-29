# Neler yapabilirsiniz

Bir JavaScript eklentisinin sekiz aracı vardır: **değişkenler**, **aksiyonlar**, **ayar sayfası**, **durum öğeleri**, **klavye girdisi**, **HTTP**, **zamanlayıcılar** ve **çeviriler**. Her biri küçüktür; eklentiler bunların birleşiminden doğar. Bu sayfa birleşimleri, gereken izinleri ve çalışan taslaklarıyla gösterir. Her çağrının ayrıntısı [JavaScript host API](/tr/reference/js-host-api) sayfasında, form seçenekleri [Alan türleri](/tr/reference/js-field-kinds) sayfasındadır.

| Fikir | Araçlar | İzinler |
|---|---|---|
| Yerel bir uygulamadan/servisten değeri butonda göstermek | HTTP, zamanlayıcı, değişken | `variables`, `http:` |
| Yerel bir HTTP API'sini çağıran buton | aksiyon, HTTP, ayarlar | `actions`, `http:` |
| Başka bir değere tepki vermek (CPU yoğunken uyarı) | değişken, zamanlayıcı, durum | `variables` |
| Bir şeyi kontrol eden slider | aksiyon (`context.value`), HTTP | `actions`, `http:` |
| Kendi mantığınla kısayol gönderen buton | aksiyon, klavye | `actions`, `input` |
| Kendi tasarladığınız sayaç, zamanlayıcı veya saat | zamanlayıcı, değişken | `variables` |
| Durum çubuğunda bağlantı ışığı | HTTP, durum | `http:` |
| Düzenlenebilir hedef listesi olan eklenti | ayarlar `List`, aksiyon | `actions` (+ çağırdığı şey) |

## 1. Yerel bir servisten değer göstermek

Bir HTTP API'sini yoklayıp sonucu yayınlayın. Widget'lar bunu `{home.temp}` olarak gösterir.

```json
{ "id": "home", "name": "Home", "version": "1.0.0", "minMacroGrid": "1.3.0", "kind": "js", "entry": "index.js",
  "permissions": ["variables", "http:localhost:8123"] }
```

```js
host.variables.describe([
  { name: 'home.temp', description: 'Living room temperature', example: '21.5', category: 'Home', type: 'number', unit: '°C' },
])

host.every(10000, async () => {
  try {
    const r = await host.http.getAsync('http://localhost:8123/api/temp')
    host.variables.set('home.temp', JSON.parse(r.body).value)
    host.status('home', 'Online', 'Ok')
  } catch (e) {
    host.status('home', 'Offline', 'Error')
  }
})
```

Bir widget'ın metnine `Temp {home.temp|0.0}°` yazın. `async` sürüm, beklerken diğer zamanlayıcıların ve aksiyonların çalışmasını sürdürür.

## 2. Formlu, API çağıran buton

Kullanıcı aksiyonu bağlarken ışığı ve durumu seçer; adres ayar sayfasında durur.

```json
"permissions": ["actions", "http:localhost:8123"]
```

```js
host.settings.page([{ key: 'base', label: 'Server', kind: 'Text', default: 'http://localhost:8123' }])

host.registerAction({
  type: 'home.light', name: 'Set light', category: 'Home',
  fields: [
    { key: 'light', label: 'Light', kind: 'Text', placeholder: 'kitchen' },
    { key: 'state', label: 'State', kind: 'Segmented', default: 'on',
      options: [{ value: 'on', label: 'On' }, { value: 'off', label: 'Off' }] },
  ],
  async run(context, settings) {
    const base = host.settings.get().base
    await host.http.postAsync(base + '/api/light', { name: settings.light, on: settings.state === 'on' })
  },
})
```

Tam hedef kuralını unutmayın: kullanıcı adresi başka bir ana bilgisayar veya porta değiştirirse eklentinin o `http:` iznine de ihtiyacı olur. Hedefi sabit tutun veya neyi bildirmeleri gerektiğini yazın.

## 3. Diğer değerlere tepki vermek

`variables` her şeyi *okumanıza* izin verir. CPU yoğunken durum çubuğunu kırmızı yapın:

```js
host.every(2000, () => {
  const cpu = host.variables.get('system.cpu')
  if (cpu === null) return
  host.status('cpu', 'CPU ' + Math.round(cpu) + '%', cpu > 90 ? 'Error' : cpu > 70 ? 'Warning' : 'Ok')
  host.variables.set('watch.hot', cpu > 90)      // bir widget artık dinamik kuralla kendini renklendirebilir
})
```

Okuma başka eklentilerin değişkenlerinde de çalışır; bir eklenti birkaç kaynağı (`obs.streaming` ve `system.cpu`) tek bir türetilmiş değişkende birleştirebilir.

## 4. Bir şeyi kontrol eden slider

Slider veya knob, *Değer değişti* olayında aksiyonu tetikler ve değeri `context.value` içinde geçirir.

```js
host.registerAction({
  type: 'home.dim', name: 'Dim light', category: 'Home',
  run(context) {
    if (context.value === null) return          // slider'a değil butona bağlanmış
    host.http.post('http://localhost:8123/api/dim', { level: Math.round(context.value) })
  },
})
```

## 5. Kendi mantığınla kısayol

Kısayolu gönderip göndermeyeceğini yalnızca eklenti bilir. Durumu hatırlayıp iki tuş arasında geçiş yapın:

```json
"permissions": ["actions", "input"]
```

```js
let muted = false
host.registerAction({
  type: 'keys.talk', name: 'Push to talk', category: 'Keyboard',
  run() {
    muted = !muted
    host.input.hotkey(muted ? 'ctrl+shift+m' : 'ctrl+shift+u')
    host.status('talk', muted ? 'Muted' : 'Live', muted ? 'Warning' : 'Ok')
  },
})
```

`host.input.type` bütün bir metni yazar; metnin canlı değerler içerebilmesi için bir form alanıyla (`allowVariables: true`) birleştirin.

## 6. Kendi tasarladığınız zamanlayıcı

`variables` dışında izin gerekmez:

```js
let left = 0, id = null
host.registerAction({
  type: 'tools.countdown', name: 'Start countdown', category: 'Tools',
  fields: [{ key: 'seconds', label: 'Seconds', kind: 'Number', default: 60, min: 1, max: 3600 }],
  run(context, settings) {
    if (id !== null) host.cancel(id)
    left = settings.seconds
    id = host.every(1000, () => {
      host.variables.set('tools.left', left)
      if (--left < 0) { host.cancel(id); id = null }
    })
  },
})
```

## 7. Hedef listesi

`List` alanı kullanıcıya istediği kadar satır ekletir; eklenti de bunları ada göre kullanabilir:

```js
host.settings.page([{ key: 'hosts', label: 'Hosts', kind: 'List', itemFields: [
  { key: 'name', label: 'Name', kind: 'Text' }, { key: 'url', label: 'URL', kind: 'Text' } ] }])

host.every(15000, async () => {
  for (const h of host.settings.get().hosts ?? []) {
    try { await host.http.getAsync(h.url); host.variables.set('ping.' + h.name, true) }
    catch { host.variables.set('ping.' + h.name, false) }
  }
})
```

Yalnızca onaylı `host:port` hedefleri çalışır; bu kalıp, kullanıcının hedeflerinin eklenti yazarınca bilindiği durumlara uyar (herhangi bir adresi yoklayan eklentide kullanıcının her birini onaylaması gerekir).

## Tasarım ipuçları

- **Eklenti başına tek iş.** Küçük eklentiyi onaylamak, her şeyi isteyen büyük eklentiyi onaylamaktan kolaydır.
- **Kararlı adlar.** Yeniden adlandırılan aksiyon `type`'ı veya değişken kayıtlı butonları bozar; bu eklentinizin MAJOR sürüm artışıdır.
- **Ucuz zamanlayıcılar.** İhtiyacınızdan sık yoklamayın. Yavaş bir servis eklentiyi durdurmasın diye `getAsync`'i tercih edin.
- **Hatayı karşılayın.** HTTP çevresinde `try/catch` kullanın, durum düzeyini `Error` yapın ve zamanlayıcıyı sürdürün. Üst üste beş yakalanmamış hata eklentiyi kapatır.
- **Ne gönderdiğinizi söyleyin.** `input` ve internet hedefleri için eklentinin ne yapacağını README'nizde belirtin.

Sonraki: [Hata ayıklama](/tr/guides/debugging), ardından [Yayımlama](/tr/guides/publishing).
