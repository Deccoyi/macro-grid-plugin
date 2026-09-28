<script setup>
import { data as v } from '../../.vitepress/versions.data'
</script>

# Uyumluluk ve sürümleme

**Macro Grid editörü ve eklenti SDK'sı tek bir sürüm numarasını paylaşır** (editör, bilgisayarınızda çalışan sunucuyu da içerir). Telefon uygulamasının ve her eklentinin kendi sürümü vardır.

| Ne | Sürüm nerede | Şimdi |
|---|---|---|
| Macro Grid editörü ve eklenti SDK'sı (`MacroGrid.Plugin.Abstractions`) | Sunucu deposunun `Directory.Build.props` dosyasındaki `<Version>` | `{{ v.macroGrid ?? 'son sürüme bakın' }}` |
| Her eklenti | Kendi `plugin.json` dosyasındaki `version` | eklentiye göre |
| Telefon uygulaması | Kendi `package.json` dosyası | sürümlerine bakın |

## Editör neyi denetler

Her eklenti `plugin.json` içinde çalıştığı en eski Macro Grid editörü sürümünü bildirir:

```json
{ "minMacroGrid": "1.3.0" }
```

Eklenti, **1.3.0'dan 2.0.0'a kadar (2.0.0 hariç)** her Macro Grid editöründe çalışır. Her zaman üç parça yazın (`1.3.0`; `1.3` kabul edilmez).

**`minMacroGrid`, bir minimum değerdir, birebir eşleşme değil.** `"minMacroGrid": "1.2.1"` yazan bir eklenti, Macro Grid editörü `1.1.1`'de (istediğinden
daha eski) **çalışmaz** — yalnızca `1.2.1` ve aynı MAJOR'un sonraki her sürümünde çalışır.

`minMacroGrid`, Macro Grid editörü 1.2.x'e kadar `macroGrid` adını taşıyordu. Bu ad hâlâ çalışır — yalnızca `minMacroGrid` yoksa okunur — ve
yeniden adlandırmadan sonra en az bir MAJOR boyunca okunabilir kalır. Hâlâ yalnızca eski adı okuyan 1.2.x'te de çalışması gereken bir eklenti
her iki alanı da aynı değerle yazar.

| Eklenti şunu diyor | Editör 1.2.4 | 1.3.0 | 1.9.9 | 2.0.0 |
|---|---|---|---|---|
| `1.0.0` | çalışır | çalışır | çalışır | yeniden derle |
| `1.3.0` | 1.3.0 gerekir | çalışır | çalışır | yeniden derle |

Uymayan bir eklenti Düzenleyici'de sebebiyle birlikte **Uyumsuz** listelenir ("Macro Grid editörü 1.3.0 veya daha yenisi gerekir, bu 1.2.4", ya da başka bir
MAJOR için "yeniden derlenmeli") ve yüklenmez. Keşfet ve Mağaza yalnızca uyan bir sürüm sunar.

`minMacroGrid` değerini derlediğiniz SDK sürümüne ayarlayın; sonradan gelen bir şeyi kullanmıyorsanız daha eskisini yazın: düşük değer eklentinin daha çok
sunucuda çalışmasını sağlar. C# eklentisinde derleme, değerin SDK paketiyle aynı MAJOR'da olduğunu ve ondan yeni olmadığını denetler.

### Eski manifest'ler

Macro Grid 1.0.0 öncesinde manifest'te `sdkVersion` ve `minServerVersion` vardı. Ne `minMacroGrid` ne de `macroGrid` yoksa hâlâ okunurlar:
`sdkVersion` `^0.4.x`, `minMacroGrid: 1.0.0` sayılır (0.4 eklentisinin kullandığı hiçbir şey değişmedi), daha eskisi yeniden derlenmelidir.
Eklentiniz 1.0.0'dan eski sunucularda da yüklenecekse iki eski alanı `minMacroGrid` yanında tutun; Macro Grid 1.0.0 ve sonrası bunları yok sayar.

## Neler uyumsuzluk yaratan (breaking) değişiklik sayılır

Bir MAJOR içinde SDK yalnızca büyür: üyeler eklenir, asla kaldırılmaz ya da değiştirilmez; bu yüzden 1.0.0 için derlenmiş bir eklenti 1.x'te çalışmaya devam eder.

- **Macro Grid / SDK (yeni bir MAJOR):** Bir eklentinin uyguladığı ya da aldığı herkese açık bir arayüz (`IPlugin`, `IPluginHost`, `IActionHandler`,
  `IVariableProvider`, `IVariableStore`, `IDeviceController`, `ActionContext`, ...) uyumsuz biçimde değişir. Her eklentinin yeniden derlenmesi gerekir.
- **Eklenti (kendi MAJOR'u):** Aksiyon türlerini, ayarlarını veya değişken adlarını, kullanıcının kayıtlı profilinin çalışmayı bırakacağı şekilde değiştirir.

## Eklentinizi sürümleme

- `plugin.json` içinde [anlamsal sürümleme](https://semver.org/) (`MAJOR.MINOR.PATCH`) kullanın; Macro Grid'den ve diğer tüm
  eklentilerden bağımsızdır. Bir Macro Grid sürümü bunu asla değiştirmez.
- MAJOR sürümü yalnızca uyumsuzluk yaratan değişikliklerde artırın: anlamı değişen aksiyon türleri veya ayarlar, yani kullanıcının kayıtlı
  profilinin sessizce çalışmayı bırakacağı durumlar. Bir aksiyonun `type` değerini ya da bir değişken adını gelişigüzel yeniden adlandırmayın.
- Bir eklenti başka bir eklentinin sürümüne bağımlı değildir. Eklentiler birbirleriyle yalnızca çalışma zamanında, değişkenler aracılığıyla konuşur.

## SDK paketi

C# eklentileri `MacroGrid.Plugin.Abstractions` NuGet paketine karşı derlenir; paketin sürümü, ait olduğu Macro Grid sürümüdür. Derlemek istediğiniz sürümü
kullanın ve SDK dll'ini çıktınızın dışında tutun (`ExcludeAssets="runtime"`): sunucu ve tüm eklentiler sunucunun kendi kopyasını paylaşır. Bkz.
[Eğitim 2](/tr/tutorials/csharp-hello-world#step-1-create-the-project).
