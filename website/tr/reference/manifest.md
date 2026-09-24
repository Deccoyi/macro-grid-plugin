# Manifest (plugin.json)

Her eklenti klasörünün kökünde bir `plugin.json` bulunur. Aşağıdaki, OBS eklentisinin dosyasıdır:

<<< @/../OBS/plugin.json

## Alanlar

| Alan | Zorunlu | Anlamı |
|---|---|---|
| `id` | evet | Benzersiz, sabit kimlik. Klasör adında, aksiyon türlerinde ve değişken adlarında, ayrıca onaylar için kullanılır. Sunucu aynı kimlikli ikinci bir eklentiyi reddeder. |
| `name` | evet | Eklentiler penceresinde görünen ad. |
| `version` | evet | Eklentinin kendi anlamsal sürümü, sunucununkinden bağımsız. |
| `sdkVersion` | evet | Eklentinin derlendiği eklenti SDK aralığı; `^0.3.0` gibi bir caret aralığı. SDK `0.x` iken `^0.3.0` yalnızca `0.3.x` ile eşleşir. Sunucunun SDK'sı bunu karşılamıyorsa eklenti *Incompatible* (uyumsuz) olarak listelenir ve yüklenmez. |
| `minServerVersion` | evet | Eklentinin ihtiyaç duyduğu en eski sunucu sürümü. Daha eski bir sunucu eklentiyi *Incompatible* olarak listeler. |
| `entry` | evet | C#: giriş DLL'sinin dosya adı. JavaScript: betik (genellikle `index.js`). |
| `kind` | evet | `"csharp"` veya `"js"`. |
| `defaultLanguage` | hayır | Eklentinin kendi metinlerinin yazıldığı dil, örneğin `"en"` (varsayılan). Çeviriler `plugin.json` yanındaki `locales/<language>.json` dosyasından gelir; eksik bir dil veya metin, yazıldığı haline döner. |
| `permissions` | hayır | Yalnızca JavaScript: betiğin ihtiyaç duyduğu izinler (bkz. [İzinler](/tr/reference/permissions)). C# eklentilerinde yok sayılır. |

## JSON şeması

Manifest, SDK'daki `PluginManifest` kaydına karşılık gelir (dosyada özellik adları camelCase'dir). Bir düzenleyicide kullanabileceğiniz şema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "sdkVersion", "minServerVersion", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "sdkVersion": { "type": "string" },
    "minServerVersion": { "type": "string" },
    "entry": { "type": "string" },
    "kind": { "enum": ["csharp", "js"] },
    "defaultLanguage": { "type": "string" },
    "permissions": { "type": ["array", "null"], "items": { "type": "string" } }
  },
  "additionalProperties": true
}
```

Sunucu bu şemayı okumaz; kolaylık için sunulmuştur ve SDK'nın `PluginManifest` türünden türetilmiştir.

## Örnekler

Bir JavaScript eklentisi:

<<< @/../examples/hello-js/plugin.json

Bir C# eklentisi:

<<< @/../examples/hello-csharp/plugin.json

SDK sürümü, `MacroGrid.Plugin.Abstractions` içindeki `PluginSdk.Version` değeridir. Neyin uyumsuz değişiklik sayıldığı için [Uyumluluk](/tr/basics/compatibility) sayfasına bakın.
