# Ayar sayfaları

Bir eklenti, Eklentiler penceresine bir ayar formu ekleyebilir. Düzenleyici formu bir alan bildirimleri listesinden çizer; eklenti
kendi arayüzünü asla çizmez. Aynı alanlar (düz nesneler) bir [aksiyonun](/tr/tutorials/js-hello-world) formunu da tanımlar.

Ayar sayfası olan bir eklenti, Eklentiler penceresinde bir **dişli düğmesi** alır ve durum çubuğu öğesi sayfayı açar.

## Sayfa bildirme

`host.settings.page(fields)` bir sayfa ekler. Değerler eklenti klasöründeki `settings.json` dosyasında saklanır (en fazla 64 KB; daha büyük bir kayıt bir mesajla reddedilir) ve
`host.settings.get()` ile okunur. Sayfayı betiğin en üst düzeyinde kaydedin.

<<< @/../examples/hello-js/index.js#settings

## Alan türleri ve seçenekler

`kind` değeri `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select`, `Segmented`, `File`, `List`, `Button` veya `Notice` olabilir. `File` Gözat düğmeli bir yol kutusudur, `List` tekrarlanan satırlardır, `Button` eklentide bir komut çalıştırır (yalnızca resmî C# eklentileri), `Notice` salt okunur uyarı metnidir (`Button` ve `Notice` değer olarak kaydedilmez). Kullanışlı alan seçenekleri:

| Seçenek | Anlamı |
|---|---|
| `Description`, `Placeholder`, `Default` | Yardım metni, bir ipucu ve başlangıç değeri. |
| `Min`, `Max`, `Step` | `Number` ve `Slider` için sınırlar. |
| `Options` | `Select` ve `Segmented` için sabit bir `SettingOption[]`. |
| `OptionsSource` | `IOptionsSource` tarafından sunulan dinamik bir listenin kimliği. |
| `DependsOn` | Geçerli form değerleri `GetOptionsAsync` yöntemine iletilen anahtarlar; bir değişiklik listeyi yeniden getirir. |
| `AllowVariables` | Bir metin alanında `{var}` ekleme düğmesini gösterir. Ham şablonu siz alırsınız. |
| `VisibleWhen` | Alanı yalnızca başka bir alan bir değere eşitken gösterir, örneğin `"mode=pause"`. `List` satırı içinde satırın kendi değerlerine bakılır. |
| `FileFilter` | `File` için zorunlu: yerel dosya seçiciye olduğu gibi verilen bir WinForms dosya filtresi, örneğin `"Audio files (*.wav;*.mp3)\|*.wav;*.mp3"`. |
| `ItemFields` | `List` için zorunlu: bir satırın alanları. Değer, nesnelerden oluşan bir JSON dizisidir; şema dışı anahtarlar kayıtta korunur. |
| `Command` | `Button` için zorunlu: ayar sayfanızla aynı sınıftaki `ISettingsCommandHandler.RunCommandAsync(command, values, token)` metoduna geçirilen komut kimliği. Dönen metin kısa bir ileti olarak gösterilir. |

## Yalnızca resmî C# eklentilerinde

Bazı şeyler sunucunun içinde kod gerektirir ve bu yüzden JavaScript eklentilerine açık değildir: eklentide bir komut çalıştıran `Button` alanı, canlı bir kaynaktan
dolan açılır listeler (`OptionsSource`) ve korunan sırlar (`Password` alanı yazarken gizlenir, ama bir JavaScript eklentisi değeri `settings.json` içinde düz metin olarak saklar;
bunu README'nizde belirtin).
