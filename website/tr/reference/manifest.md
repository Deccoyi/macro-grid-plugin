# Manifest (plugin.json)

Her eklenti klasörünün kökünde bir `plugin.json` bulunur. Aşağıdaki, OBS eklentisinin dosyasıdır:

<<< @/../OBS/plugin.json

## Alanlar

| Alan | Zorunlu | Anlamı |
|---|---|---|
| `id` | evet | Benzersiz, sabit kimlik. Klasör adında, aksiyon türlerinde ve değişken adlarında, ayrıca onaylar için kullanılır. Sunucu aynı kimlikli ikinci bir eklentiyi reddeder. |
| `name` | evet | Eklentiler penceresinde görünen ad. |
| `version` | evet | Eklentinin kendi anlamsal sürümü, sunucununkinden bağımsız. |
| `macroGrid` | evet | Eklentinin çalıştığı en eski Macro Grid, `1.3.0` gibi `MAJOR.MINOR.PATCH` biçiminde. Eklenti, o sürümden bir sonraki MAJOR'a kadar (o hariç) her Macro Grid'de çalışır. Macro Grid ve eklenti SDK'sı tek sürümü paylaşır; derlediğiniz SDK sürümünü, daha yenisini kullanmıyorsanız daha eskisini yazın. Uymayan bir sunucu eklentiyi *Incompatible* (uyumsuz) listeler ve yüklemez. |
| `sdkVersion` | hayır | Macro Grid 1.0.0 öncesinden kalma alan. Yalnızca `macroGrid` yoksa okunur: `^0.4.x`, `macroGrid: 1.0.0` sayılır, daha eski aralıklar uyumsuzdur. Eklenti 1.0.0'dan eski sunucularda da yüklenecekse `macroGrid` yanında tutun. |
| `minServerVersion` | hayır | `sdkVersion` gibi eski alan. Macro Grid 1.0.0 ve sonrası bunu yok sayar. |
| `entry` | evet | C#: giriş DLL'sinin dosya adı. JavaScript: betik (genellikle `index.js`). |
| `kind` | evet | `"csharp"` veya `"js"`. |
| `defaultLanguage` | hayır | Eklentinin kendi metinlerinin yazıldığı dil, örneğin `"en"` (varsayılan). Çeviriler `plugin.json` yanındaki `locales/<language>.json` dosyasından gelir; eksik bir dil veya metin, yazıldığı haline döner. |
| `permissions` | hayır | Yalnızca JavaScript: betiğin ihtiyaç duyduğu izinler (bkz. [İzinler](/tr/reference/permissions)). C# eklentilerinde yok sayılır. |
| `description` | hayır | Keşfet ve Mağaza'da gösterilen tek satırlık özet. Eklentiseldir; eski sunucular yok sayar. |
| `author` | hayır | Eklentinin yazarı, `description` yanında gösterilir. Eklentiseldir. |
| `homepage` | hayır | Eklentinin sayfasına veya kaynağına bir bağlantı olarak gösterilir. Eklentiseldir. |

## JSON şeması

Manifest, SDK'daki `PluginManifest` kaydına karşılık gelir (dosyada özellik adları camelCase'dir). Bir düzenleyicide kullanabileceğiniz şema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "macroGrid", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "macroGrid": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" },
    "sdkVersion": { "type": "string" },
    "minServerVersion": { "type": "string" },
    "entry": { "type": "string" },
    "kind": { "enum": ["csharp", "js"] },
    "defaultLanguage": { "type": "string" },
    "permissions": { "type": ["array", "null"], "items": { "type": "string" } },
    "description": { "type": "string" },
    "author": { "type": "string" },
    "homepage": { "type": "string" }
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
