<script setup>
import { data as v } from '../../.vitepress/versions.data'
</script>

# Uyumluluk ve sürümleme

Macro Grid'in birbirinden bağımsız sürümlenen üç parçası vardır ve her eklentinin de kendi sürümü bulunur.

| Ne | Sürüm nerede | Şimdi |
|---|---|---|
| Sunucu | Sunucu deposunda `ClientHub.ServerVersion` | `{{ v.server ?? 'son sürüme bakın' }}` |
| Eklenti SDK'sı (`MacroGrid.Plugin.Abstractions`) | `PluginSdk.Version` | `{{ v.sdk ?? 'NuGet sayfasına bakın' }}` |
| Her eklenti | Kendi `plugin.json` dosyasındaki `version` | eklentiye göre |

## Sunucu neyi denetler

Her eklenti `plugin.json` içinde hangi SDK'ya karşı derlendiğini ve hangi sunucuya ihtiyaç duyduğunu bildirir:

- **`sdkVersion`**, `PluginSdk.Version` ile karşılaştırılan npm tarzı bir caret aralığıdır. SDK `0.x` iken `^0.3.0` yalnızca
  `0.3.x` sürümlerini kapsar; `1.0.0` sürümünden itibaren `^1.0.0` her `1.x.y` sürümünü kapsar.
- **`minServerVersion`**, eklentinin kullandığı özelliklere sahip en eski sunucu sürümüdür.

İkisini de karşılamayan bir eklenti Düzenleyici'de **Uyumsuz** olarak listelenir ve yüklenmez. İkisini de dürüstçe belirleyin: `sdkVersion`
derleyip test ettiğiniz SDK olsun, `minServerVersion` ise kullandığınız özelliklere sahip en eski sunucu olsun.

## Neler uyumsuzluk yaratan (breaking) değişiklik sayılır

- **SDK:** Bir eklentinin uyguladığı ya da aldığı herkese açık bir arayüz (`IPlugin`, `IPluginHost`, `IActionHandler`, `IVariableProvider`,
  `IVariableStore`, `IDeviceController`, `ActionContext`, ...) uyumsuz biçimde değişir. O SDK için derlenmiş her eklentinin
  yeniden derlenmesi gerekir. SDK `0.x` iken bir minor artış bunu yapabilir.
- **Eklenti:** Aksiyon türlerini, ayarlarını veya değişken adlarını, kullanıcının kayıtlı profilinin çalışmayı bırakacağı şekilde değiştirir.

## Eklentinizi sürümleme

- `plugin.json` içinde [anlamsal sürümleme](https://semver.org/) (`MAJOR.MINOR.PATCH`) kullanın; sunucudan ve diğer tüm
  eklentilerden bağımsızdır. Bir sunucu sürümü bunu asla değiştirmez.
- MAJOR sürümü yalnızca uyumsuzluk yaratan değişikliklerde artırın: anlamı değişen aksiyon türleri veya ayarlar, yani kullanıcının kayıtlı
  profilinin sessizce çalışmayı bırakacağı durumlar. Bir aksiyonun `type` değerini ya da bir değişken adını gelişigüzel yeniden adlandırmayın.
- Bir eklenti başka bir eklentinin sürümüne bağımlı değildir. Eklentiler birbirleriyle yalnızca çalışma zamanında, değişkenler aracılığıyla konuşur.

## SDK paketi

C# eklentileri `MacroGrid.Plugin.Abstractions` NuGet paketine karşı derlenir. `sdkVersion` ile uyumlu sürümü kullanın ve
SDK dll'ini çıktınızın dışında tutun (`ExcludeAssets="runtime"`): sunucu ve tüm eklentiler sunucunun kendi kopyasını paylaşır. Bkz.
[Eğitim 2](/tr/tutorials/csharp-hello-world#step-1-create-the-project).
