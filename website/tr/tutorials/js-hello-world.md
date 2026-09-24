# Öğretici 1: JavaScript ile merhaba dünya

Bu öğreticide en küçük işe yarar JavaScript eklentisini yazıyorsunuz: bir **sayaç değişkeni**, bir **ayar sayfası**, sayacı artıran
bir **aksiyon** ve bir **durum çubuğu öğesi**. Tek ihtiyacınız bir metin düzenleyici ve çalışan bir Macro Grid sunucusu; derlenecek
bir şey yok.

Bitmiş eklenti depoda [`examples/hello-js`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/examples/hello-js) altında duruyor
([`HelloJs/`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/HelloJs) klasörünün bir kopyası, CI ikisini birebir aynı tutar).
Aşağıdaki her kod bloğu bu klasörden alınmıştır.

## Adım 1: klasör ve manifest

`hello-js` adında bir klasör oluşturun ve içine `plugin.json` dosyasını koyun:

<<< @/../examples/hello-js/plugin.json

- `id` benzersiz ve sabit olmalıdır. Değişken adları ve aksiyon türleri `<id>.` ile başlamalıdır (burada `hellojs.`).
- `kind` değeri `js`, `entry` ise çalışacak betiktir.
- `permissions` betiğin neye ihtiyaç duyduğunu listeler. `variables` değişken yayınlamasını, `actions` aksiyon kaydetmesini sağlar.
  Kullanıcı, betik çalışmadan önce tam olarak bu listeyi onaylamak zorundadır.
- `sdkVersion` ve `minServerVersion`, eklentinin hangi SDK ve sunucu sürümüne ihtiyaç duyduğunu söyler (bkz. [Uyumluluk](/tr/basics/compatibility)).

## Adım 2: bir değişken

Yanına `index.js` dosyasını oluşturun. Betik bir korumalı alanda (sandbox) çalışır ve sunucuya yalnızca genel `host` nesnesi
üzerinden ulaşır. Bir sayaç değişkeniyle başlayın:

<<< @/../examples/hello-js/index.js#variable

`host.variables.describe`, değişkeni düzenleyicinin değişken seçicisinde listeler; böylece kullanıcılar adı tahmin etmek zorunda
kalmaz. `host.variables.set` ise ilk değeri yayınlar. Artık her widget bunu metninde `Count: {hellojs.count}` şeklinde gösterebilir.

## Adım 3: bir ayar sayfası

<<< @/../examples/hello-js/index.js#settings

`host.settings.page`, bir alan listesinden Eklentiler penceresine bir ayar formu ekler. Değerler eklentinin klasöründeki
`settings.json` dosyasında saklanır ve `host.settings.get()` ile geri okunur.

## Adım 4: bir aksiyon

<<< @/../examples/hello-js/index.js#action

`host.registerAction`, düzenleyicinin aksiyon seçicisine bir aksiyon ekler. `fields` aksiyonun kendi formunu tanımlar (burada
`Times` adlı bir sayı alanı); `run`, aksiyon tetiklendiğinde bu formun değerleriyle (`settings`) çağrılır. Burada sayaca
`step * times` ekler ve yeni değeri yayınlar.

::: tip İpucu: en üst düzeyde kaydedin
Aksiyonlar, ayar sayfası ve değişken açıklamaları betik ilk çalışırken kaydedilmelidir. Daha sonra, bir geri çağrımın (callback)
içinden yapılan kayıtlar yok sayılır. Değişkenler ise her zaman ayarlanabilir.
:::

## Adım 5: bir durum öğesi

<<< @/../examples/hello-js/index.js#status

`host.every(ms, fn)` bir işlevi tekrar tekrar çalıştırır (en kısa aralık 100 ms) ve `host.status(id, text, level)` düzenleyicinin
durum çubuğunda size ait bir öğeyi günceller. Zamanlayıcılar için izin gerekmez.

Betiğin tamamı:

<<< @/../examples/hello-js/index.js

## Adım 6: kurun

1. Düzenleyicide **Eklentiler, Eklentileri Yönet…** yolunu açın ve **Klasörden Yükle…** seçeneğini seçin.
2. `hello-js` klasörünü seçin. Klasör `%AppData%\MacroGrid\plugins\hellojs\` altına kopyalanır.
3. Eklenti **Onay bekliyor** olarak görünür. İzin listesini okuyup onaylayın. Eklenti başlar.

## Adım 7: kullanın

1. Bir sayfaya düğme ekleyin. Metnini `Count: {hellojs.count}` yapın (değişken, `{...}` seçicisinde de *Hello* kategorisinde bulunur).
2. Düğmenin **basınca** olayına bir aksiyon bağlayın: **Bump the counter** aksiyonunu (kategori *Hello*) seçin ve *Times* değerini girin.
3. Kaydedin, deck'i açın ve düğmeye basın. Metindeki sayı artar.
4. Eklentiler penceresinde eklentinin dişli düğmesini açın ve **Step** değerini değiştirin: artık her basış `step * times` ekler.
5. Durum çubuğunun sağına bakın: `Count <n>` beş saniyede bir güncellenir.

## Adım 8: değiştirin ve yeniden yükleyin

Betiği düzenleyin ve sunucuyu yeniden başlatmadan tekrar deneyin. İki yol var:

- Kaynak klasörünüzü değiştirip yeniden **Klasörden Yükle…** deyin. Aynı `id`'ye sahip klasör, kurulu eklentinin yerini alır ve
  eklentinin kendi `settings.json` dosyası korunur.
- Ya da `%AppData%\MacroGrid\plugins\hellojs\index.js` içindeki kurulu kopyayı düzenleyin ve Eklentiler penceresinde
  **Yeniden yükle**'ye tıklayın.

Betik ilk çalışırken hata fırlatırsa eklenti, adının altında hata iletisiyle birlikte **Hata** olarak görünür; düzeltip yeniden
yükleyin. Bkz. [Hata ayıklama ve günlükler](/tr/guides/debugging).

## Sırada ne deneyebilirsiniz

- [JavaScript host API başvurusunu](/tr/reference/js-host-api) okuyun; örneğin yerel bir hizmeti yoklamak için `host.http.get`
  (bir `http:<host>:<port>` izni gerekir).
- Değişkeninizi widget metninde biçimlendirmek için [Öğretici 3](/tr/tutorials/live-data) ile devam edin.
- Aynı fikri C# ile yazın: [Öğretici 2](/tr/tutorials/csharp-hello-world).
