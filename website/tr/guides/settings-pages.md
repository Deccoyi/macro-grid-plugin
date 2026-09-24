# Ayar sayfaları

Bir eklenti, Eklentiler penceresine bir ayar formu ekleyebilir. Düzenleyici formu bir alan bildirimleri listesinden çizer; eklenti
kendi arayüzünü asla çizmez. Aynı alanlar (C#'ta `SettingField`, JavaScript'te düz nesneler) bir
[aksiyonun](/tr/tutorials/csharp-hello-world#step-3-the-action-and-its-form) formunu da tanımlar.

Ayar sayfası olan bir eklenti, Eklentiler penceresinde bir **dişli düğmesi** alır ve durum çubuğu öğesi sayfayı açar.

## JavaScript

`host.settings.page(fields)` bir sayfa ekler. Değerler eklenti klasöründeki `settings.json` dosyasında saklanır ve
`host.settings.get()` ile okunur. Sayfayı betiğin en üst düzeyinde kaydedin.

<<< @/../examples/hello-js/index.js#settings

## C#

`IPluginSettingsPage` arayüzünü (`Fields`, `Load()`, `Save(values)`) uygulayın ve `host.RegisterSettingsPage` ile kaydedin. `Load`
geçerli değerleri bir `JsonObject` olarak döndürür, `Save` kullanıcının girdiği değerleri alır. Bunlar diskle aranızdaki köprüdür; genellikle
`host.DataDirectory` içindeki bir `settings.json`.

<<< @/../examples/hello-csharp/src/HelloSettingsPage.cs#settings{cs}

## Alan türleri ve seçenekler

`SettingFieldKind` değeri `Text`, `Password`, `Number`, `Slider`, `Bool`, `Select` veya `Segmented` olabilir. Kullanışlı `SettingField` seçenekleri:

| Seçenek | Anlamı |
|---|---|
| `Description`, `Placeholder`, `Default` | Yardım metni, bir ipucu ve başlangıç değeri. |
| `Min`, `Max`, `Step` | `Number` ve `Slider` için sınırlar. |
| `Options` | `Select` ve `Segmented` için sabit bir `SettingOption[]`. |
| `OptionsSource` | `IOptionsSource` tarafından sunulan dinamik bir listenin kimliği. |
| `DependsOn` | Geçerli form değerleri `GetOptionsAsync` yöntemine iletilen anahtarlar; bir değişiklik listeyi yeniden getirir. |
| `AllowVariables` | Bir metin alanında `{var}` ekleme düğmesini gösterir. Ham şablonu siz alırsınız. |
| `VisibleWhen` | Alanı yalnızca başka bir alan bir değere eşitken gösterir, örneğin `"mode=pause"`. |

## Dinamik açılır listeler

`IOptionsSource` arayüzünü de uygulayan bir sayfa (veya aksiyon), `OptionsSource` alanı onu adlandıran bir alan için seçenek sunabilir:

```csharp
Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken);
```

`SettingOption(value, label)` öğeleri içeren bir `OptionsResult` döndürün ya da bir mesaj ve boş bir liste göstermek için `Error` değerini ayarlayın. OBS
eklentisi sahne, ses girişi ve sahne öğesi listelerini bu yolla, OBS'in bildirdiklerinin önbelleğinden sunar (bkz.
[OBS eklentisi](/tr/guides/obs-plugin)).

## Gizli bilgiler

`SettingFieldKind.Password` girdiyi gizler, ancak kaydettiğiniz değer sizin sorumluluğunuzdadır: OBS eklentisi parolasını `settings.json`
dosyasında düz metin olarak saklar. Bunu README'nizde belirtin.
