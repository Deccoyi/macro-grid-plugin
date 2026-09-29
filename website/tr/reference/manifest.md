# Manifest (plugin.json)

Her eklenti klasörünün kökünde bir `plugin.json` bulunur. Aşağıdaki, JavaScript örneğinin dosyasıdır:

<<< @/../examples/hello-js/plugin.json

## Alanlar

| Alan | Zorunlu | Anlamı |
|---|---|---|
| `id` | evet | Benzersiz, sabit kimlik. Klasör adında, aksiyon türlerinde ve değişken adlarında, ayrıca onaylar için kullanılır. Sunucu aynı kimlikli ikinci bir eklentiyi reddeder. |
| `name` | evet | Eklentiler penceresinde görünen ad. |
| `version` | evet | Eklentinin kendi anlamsal sürümü, sunucununkinden bağımsız. |
| `minMacroGrid` | evet | Eklentinin çalıştığı en eski Macro Grid, `1.3.0` gibi `MAJOR.MINOR.PATCH` biçiminde. **Bu bir minimum değerdir, birebir eşleşme değil:** eklenti, o sürümden bir sonraki MAJOR'a kadar (o hariç) her Macro Grid'de çalışır — daha eski bir sunucuda (örn. `1.2.1` yazıp sunucu `1.1.1` ise) **çalışmaz**. Macro Grid ve eklenti SDK'sı tek sürümü paylaşır; derlediğiniz SDK sürümünü, daha yenisini kullanmıyorsanız daha eskisini yazın. Uymayan bir sunucu eklentiyi *Incompatible* (uyumsuz) listeler ve yüklemez. Macro Grid 1.2.x'e kadar adı `macroGrid` idi; o ad hâlâ çalışır — yalnızca `minMacroGrid` yoksa okunur — ve yeniden adlandırmadan sonra en az bir MAJOR boyunca okunabilir kalır. |
| `macroGrid` | hayır | Eskisi: `minMacroGrid`'in önceki adı, aynı anlam ve biçimde. Yalnızca `minMacroGrid` yoksa okunur. Yalnızca eski adı okuyan Macro Grid 1.2.x'e kadar da çalışması gereken bir eklenti her iki alanı da aynı değerle yazar. |
| `sdkVersion` | hayır | Macro Grid 1.0.0 öncesinden kalma alan. Yalnızca ne `minMacroGrid` ne de `macroGrid` varsa okunur: `^0.4.x`, `minMacroGrid: 1.0.0` sayılır, daha eski aralıklar uyumsuzdur. Eklenti 1.0.0'dan eski sunucularda da yüklenecekse `minMacroGrid` yanında tutun. |
| `minServerVersion` | hayır | `sdkVersion` gibi eski alan. Macro Grid 1.0.0 ve sonrası bunu yok sayar. |
| `entry` | evet | JavaScript: betik (genellikle `index.js`). Resmî bir C# eklentisi için: giriş DLL'sinin dosya adı. |
| `kind` | evet | Resmî olanlar dışındaki her eklenti için `"js"`. `"csharp"` yalnızca resmî eklentilerce kullanılır ve sunucu böyle bir eklentiyi yalnızca resmî imzayı (`signature.json` ve `signature.sig`) taşıyorsa yükler; bunsuz `"csharp"` diyen eklenti *İzin verilmedi* olarak gösterilir. |
| `defaultLanguage` | hayır | Eklentinin kendi metinlerinin yazıldığı dil, örneğin `"en"` (varsayılan). Çeviriler `plugin.json` yanındaki `locales/<language>.json` dosyasından gelir; eksik bir dil veya metin, yazıldığı haline döner. |
| `permissions` | hayır | Yalnızca JavaScript: betiğin ihtiyaç duyduğu izinler (bkz. [İzinler](/tr/reference/permissions)). |
| `description` | hayır | Keşfet ve Mağaza'da gösterilen tek satırlık özet. Eklentiseldir; eski sunucular yok sayar. |
| `author` | hayır | Eklentinin yazarı, `description` yanında gösterilir. Eklentiseldir. |
| `homepage` | hayır | Eklentinin sayfasına veya kaynağına bir bağlantı olarak gösterilir. Eklentiseldir. |

## JSON şeması

Manifest, SDK'daki `PluginManifest` kaydına karşılık gelir (dosyada özellik adları camelCase'dir). Bir düzenleyicide kullanabileceğiniz şema:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["id", "name", "version", "minMacroGrid", "entry", "kind"],
  "properties": {
    "id": { "type": "string" },
    "name": { "type": "string" },
    "version": { "type": "string" },
    "minMacroGrid": { "type": "string", "pattern": "^\\d+\\.\\d+\\.\\d+$" },
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

## Örnek

Bir JavaScript eklentisi:

<<< @/../examples/hello-js/plugin.json

Neyin uyumsuz değişiklik sayıldığı için [Uyumluluk](/tr/basics/compatibility) sayfasına bakın.
