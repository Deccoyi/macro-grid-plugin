# Öğretici 3: canlı veri gösterme

Bir widget, canlı veriyi metninde **değişkenler** aracılığıyla gösterir. Her eklenti (ve sunucunun kendisi) değişken yayınlar; siz
bunları widget metninde `{name}` olarak kullanır ve `{name|format}` ile biçimlendirirsiniz. Bu öğretici,
[Öğretici 1](/tr/tutorials/js-hello-world)'deki sayacı (`hellojs.count`), [Öğretici 2](/tr/tutorials/csharp-hello-world)'deki
sayacı (`hellocsharp.count`) ve sunucunun sağladığı değişkenleri kullanır.

## Değişken yayınlama

JavaScript eklentisi `host.variables.set(name, value)` ile yayınlar. Değer bir sayı, dize, boolean veya null olabilir. Ad,
eklenti kimliğinizle başlamalıdır:

<<< @/../examples/hello-js/index.js#variable

C# eklentisi `IVariableProvider` uygular ve `store.Set(name, value)` çağırır:

<<< @/../examples/hello-csharp/src/GreetingCounter.cs#variables{cs}

Mevcut değere eşit bir değer yok sayılır; bu yüzden her yoklamada değişkeni ayarlamak ucuzdur. Değer gerçekten değiştiğinde sunucu
yalnızca onu kullanan widget'ları yeniden çizer ve yalnızca değişen metinleri gönderir (saniyede en fazla yaklaşık on güncelleme).

## Metinde değişken kullanma

Widget metni bir şablondur. Değişken adını süslü parantez içine koyun:

| Metin | Gösterir |
|---|---|
| `Count: {hellojs.count}` | `Count: 3` |
| `CPU {system.cpu\|0}%` | `CPU 42%` |
| `{system.time\|HH:mm}` | `14:05` |
| `Live: {obs.stream.duration}` | `Live: 00:12:31` |

Süslü parantezleri olduğu gibi yazdırmak için ikiye katlayın: iki açma paranteziyle bir `{`, iki kapama paranteziyle bir `}` elde
edersiniz.

Bir aksiyonun metin alanında, eklenti alanda `AllowVariables`'ı ayarlamışsa *değişken ekle* düğmesi görünür. Sunucu,
`{variables}` ifadelerini aksiyon çalışmadan önce çözer (şema tabanlı formu kullanan aksiyonlar için).

## Biçimlendirme

`|` işaretinden sonra bir biçim ekleyin: `{system.cpu|0}`, `{system.time|HH:mm}`.

- **Sayılar** varsayılan olarak `0.##` biçimini kullanır. Ondalık istemiyorsanız `{system.cpu|0}`, bir ondalık için
  `{system.cpu|0.0}` yazın.
- **Tarih ve saatler** varsayılan olarak `HH:mm` kullanır. Standart .NET özel tarih biçimlerini kullanın, örneğin
  `{system.time|HH:mm:ss}`.
- **Süreler** varsayılan olarak `hh:mm:ss` kullanır.
- **Boolean değerler** varsayılan olarak `On` / `Off` görünür (uygulama Türkçe çalışıyorsa `Açık` / `Kapalı`). Kendi sözcüklerinizi
  seçmek için bir biçim verin: `{obs.streaming|ON/OFF}`.
- **Bulunmayan bir değişken** boş dize olarak görünür.

## Yerleşik değişkenler

Sunucu şunları yayınlar: `system.time`, `system.cpu`, `system.ram`, `system.ram.used`, `system.ram.total`, `system.uptime`,
`system.audio.master` ve `system.audio.muted`. OBS eklentisi yaklaşık 45 adet `obs.*` değişkeni ekler. Düzenleyicinin değişken
seçicisi, `host.variables.describe` (JavaScript) veya `IVariableCatalogSource` (C#) ile tanımlananlar dahil, kullanılabilir olanları
listeler.

## Slider ve knob'lar

Bir slider veya knob widget'ı, `valueVariable` olarak bir değişken adı verebilir: o değişkenin değerini gösterir ve kullanıcı
sürüklediğinde `valueChange` olayına bağlı aksiyon, sürüklenen değerle çalışır (`ActionContext.Value` (C#) veya `context.value`
(JavaScript)). Bir slider'ın Windows ses düzeyini böyle kontrol etmesi ve başka yerde değiştirilince onu izlemesi bu sayededir.

## Dinamik kurallar

Bir widget'ın rengi, metni, simgesi veya animasyonu, düzenleyicide oluşturulan kurallarla (bir alanın yanındaki yıldırım düğmesi) bir
değişkene bağlı olabilir: "`hellojs.count` 10'un üzerindeyse yanıp sön". Kurallar düz veridir; koşullar karşılaştırmalardır (`>`,
`>=`, `<`, `<=`, `==`, `!=`, `between`) ve `and`, `or`, `xor` ile `not` ile birleştirilir; ilk eşleşen durum kazanır. Bir ifade dili
yoktur. Dinamik olabilen özellikler: arka plan, ön plan, kenarlık rengi, animasyon, simge ve metin.

## Deneyin

1. Öğretici 1'deki eklentiyi kurun ve üç düğme ekleyin: `Count: {hellojs.count}`, `CPU {system.cpu|0}%` ve `{system.time|HH:mm}`.
2. **Bump the counter** aksiyonunu ilk düğmeye bağlayın ve basın.
3. İlk düğmenin arka planına bir kural ekleyin: 5'in üzerinde kırmızı. Kırmızıya dönene kadar basmaya devam edin.

## Değişkenler için adlandırma kuralları

- JavaScript eklentisi yalnızca `<eklenti kimliği>.` ile başlayan adları ayarlayabilir; `system.cpu` değerinin veya başka bir
  eklentinin değerlerinin üzerine asla yazamaz.
- Kullanıcı verisine bağlı bir ad için (silinebilen veya yeniden adlandırılabilen bir OBS girişi gibi), ad ortadan kalktığında
  `store.Remove(name)` (C#) veya `host.variables.remove(name)` (JavaScript) çağırın; aksi halde seçicide sonsuza dek kalır.
