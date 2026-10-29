# Alan türleri

**Alan**, bir formun tek girdisini tanımlar. Formu düzenleyici çizer; eklentiniz hiçbir zaman arayüz çizmez. Aynı alan nesneleri iki yerde kullanılır:

- `host.settings.page(fields)`: eklentinin ayar penceresi (Eklentiler penceresindeki dişli buton).
- `host.registerAction({...})` içindeki `fields`: aksiyon bir widget olayına bağlanırken kullanıcının doldurduğu form.

```js
{ key: 'volume', label: 'Volume', kind: 'Slider', min: 0, max: 100, step: 5, default: 50 }
```

`key`, değerin geldiği addır (aksiyonda `settings.volume`, ayar sayfasında `host.settings.get().volume`). `label` girdinin yanındaki metindir. `kind` aşağıdaki adlardan biridir.

## Türler

| Tür | Kullanıcı ne görür | Betikteki değer | Kullandığı seçenekler |
|---|---|---|---|
| `Text` | tek satırlık metin kutusu | string | `placeholder`, `default`, `allowVariables` |
| `Password` | gizli metin kutusu | string (JavaScript eklentilerinde düz metin saklanır) | `placeholder` |
| `Number` | sayı kutusu | number | `min`, `max`, `step`, `default` |
| `Slider` | değeri gösteren kaydırıcı | number | `min`, `max`, `step`, `default` |
| `Bool` | onay kutusu veya anahtar | `true` / `false` | `default` |
| `Select` | açılır liste | seçilen seçeneğin `value`'su (string) | `options`, `default` |
| `Segmented` | biri etkin olan bir buton sırası | seçilen seçeneğin `value`'su (string) | `options`, `default` |
| `File` | **Gözat** butonlu yol kutusu (yerel dosya seçici) | yol (string) | `fileFilter` |
| `List` | kullanıcının ekleyip sildiği tekrarlı satırlar | nesne dizisi | `itemFields` |
| `Notice` | salt okunur uyarı metni | yok, hiç kaydedilmez | `label`, `description` |
| `Variable` | değişken seçici (yalnızca widget ayarları) | değişkenin adı (metin) | yok |
| `Color` | renk seçici | `#rrggbb` metni | `default` |
| `Hotkey` | kullanıcının bastığı tuşları yakalayan kutu | `ctrl+shift+s` gibi bir tuş birleşimi metni, yoksa boş | `default` |
| `Duration` | birim seçimli (ms, sn, dk) sayı kutusu | tam milisaniye (sayı) | `min`, `max`, `step`, `default`, hepsi milisaniye |
| `MultiSelect` | onay kutuları listesi | seçilen seçeneklerin `value` dizisi, seçenek sırasıyla | `options`, `default` |

`Hotkey`, `Duration` ve `MultiSelect` bunlara sahip bir Macro Grid sürümü ister (`minMacroGrid` değerini ona ayarlayın). Daha eski sürüm alanın okunamadığını söyler. `plugin.json` içindeki bir widget'ın `settings` bölümünde eski sürüm dosyanın tamamını okuyamaz ("plugin.json could not be parsed"), `minMacroGrid` ne olursa olsun. `MultiSelect` `visibleWhen` koşulunu yönetemez, çünkü o tek bir metinle karşılaştırır.
| `Button` | bir buton | yok | JavaScript'ten kullanılamaz (aşağıya bakın) |

## Seçenekler

| Seçenek | Geçerli olduğu | Anlamı |
|---|---|---|
| `description` | hepsi | Alanın altındaki yardım metni. |
| `placeholder` | metin türleri | Kutu boşken gösterilen ipucu. |
| `default` | değer türleri | Başlangıç değeri. Kullanıcı değiştirene kadar `settings` ve `host.settings.get()` bunu taşır. |
| `min`, `max`, `step` | `Number`, `Slider` | Sınırlar ve artış. |
| `options` | `Select`, `Segmented` | Sabit `{ value, label }` listesi. İsteğe bağlı `group` satırı *Grup › Öğe* olarak girintiler, isteğe bağlı `icon` simge gösterir. |
| `allowVariables` | `Text` | `{var}` ekleme butonunu gösterir. `{...}` hâlâ içinde olan ham metni alırsınız. Değeri `host.variables.get` ile kendiniz okuyun. |
| `visibleWhen` | hepsi | Alanı yalnızca başka bir alanın değeri tutunca gösterir; `"anahtar=değer"` yazılır, örneğin `"mode=pause"`. `List` satırı içinde o satırın kendi değerlerine karşı denetlenir. |
| `fileFilter` | `File` | Zorunlu. `"Audio files (*.wav;*.mp3)\|*.wav;*.mp3"` gibi bir Windows dosya filtresi. |
| `itemFields` | `List` | Zorunlu. Bir satırın alanları; yukarıdaki türlerden herhangi biri kullanılabilir. Bildirmediğiniz satır anahtarları kayıtta korunur. |
| `dependsOn`, `optionsSource`, `command` | dinamik listeler ve butonlar | Bunlar sunucuda kod ister (yalnızca resmî C# eklentileri); JavaScript eklentisinde bir şey yapmazlar. |

## Örnekler

**Birkaç türle bir ayar sayfası**

```js
host.settings.page([
  { kind: 'Notice', key: 'n', label: 'Talks to the hub on your network.' },
  { key: 'host', label: 'Hub address', kind: 'Text', placeholder: '192.168.1.20' },
  { key: 'mode', label: 'Mode', kind: 'Segmented', default: 'auto',
    options: [{ value: 'auto', label: 'Auto' }, { value: 'manual', label: 'Manual' }] },
  { key: 'level', label: 'Level', kind: 'Slider', min: 0, max: 100, step: 5, default: 50,
    visibleWhen: 'mode=manual' },
  { key: 'token', label: 'Token', kind: 'Password' },
  { key: 'log', label: 'Write to file', kind: 'File', fileFilter: 'Text files (*.txt)|*.txt' },
])
```

**Bir liste**

```js
host.settings.page([
  { key: 'lights', label: 'Lights', kind: 'List', itemFields: [
    { key: 'name', label: 'Name', kind: 'Text' },
    { key: 'port', label: 'Port', kind: 'Number', min: 1, max: 65535, default: 8080 },
  ] },
])
const lights = host.settings.get().lights   // [{ name: 'Desk', port: 8080 }, ...]
```

**Değişken kullanabilen bir aksiyon formu**

```js
host.registerAction({
  type: 'myplugin.say', name: 'Say', run(context, settings) { host.log(settings.text) },
  fields: [{ key: 'text', label: 'Text', kind: 'Text', allowVariables: true }],
})
```

## Değerler nerede durur

- **Ayar sayfası:** eklenti klasöründeki `settings.json` içinde. Yalnızca bildirilen anahtarlar tutulur. `host.settings.get()` kayıtlı değerleri varsayılanların üstüne bindirir. Kullanıcı dosyayı da açabilir, okuduğunuzu doğrulayın.
- **Aksiyon formu:** profilin içinde, aksiyonu kullanan butonun yanında; `run`'a `settings` olarak geçer.

Form etiketleri `locales/<language>.json` ile çevrilebilir, bkz. [JavaScript host API](/tr/reference/js-host-api#translations). Ayar sayfalarının genel davranışı [Ayar sayfaları](/tr/guides/settings-pages) bölümündedir.
