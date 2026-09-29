# Simge paketleri: PLC Icons

::: warning Simge paketleri yalnızca resmî eklentilerle gelir
Simge paketi sunucunun içinde çalışan koddur; bu yüzden yalnızca bir C# eklentisi olabilir ve sunucu C# eklentilerini yalnızca resmî ve imzalı olduklarında yükler.
JavaScript eklentileri simge paketi ekleyemez. Bu sayfa, resmî PLC Icons eklentisinin nasıl yapıldığını okumak için gösterir.
:::

Simge paketi, işe yarar en basit C# eklentisidir: bağlantı yok, ayar yok, aksiyon yok. **PLC Icons** eklentisi
([`PLCIcons/`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/PLCIcons), id `plc-icons`) Düzenleyici'nin simge seçicisine kendi kategorisi olarak
29 merdiven mantığı (PLC) simgesi ekler. Bu sayfa onu adım adım anlatır.

## Paket nedir

`IIconPackSource` arayüzünü uygulayın ve `Initialize` içinde `host.RegisterIconPack` ile kaydedin:

| Üye | Anlamı |
|---|---|
| `Id` | Paketin benzersiz kimliği. |
| `DisplayName` | Simge seçicideki kategori adı. |
| `IconNames` | Simgelerin adları, listelenmesini istediğiniz sırayla. |
| `GetIconSvg(name)` | Bir simgenin SVG metni, yoksa `null`. |

## Eklentinin tamamı

<<< @/../PLCIcons/src/PlcIconsPlugin.cs

- `PlcIconsPlugin` `IPlugin` giriş noktasıdır: tek satır, `host.RegisterIconPack(new PlcIconPack())`.
- İsimler sıralanır, böylece seçicinin sıralaması dosya sistemine bağlı olmaz.
- SVG'ler assembly'ye gömülü kaynaklardan okunur, dolayısıyla çalışma zamanında ayrı dosya yoktur.

Görünen ad İngilizce yazılmıştır (eklentinin varsayılan dili); Türkçe ad "PLC İkonları" `locales/tr.json` dosyasından gelir.

## Proje

Simgeler `src/icons/` içinde durur ve `.csproj` içindeki tek bir satırla gömülür:

```xml
<EmbeddedResource Include="icons\*.svg" />
```

Kaynak adı `<RootNamespace>.icons.<file name>` biçimindedir (`MacroGrid.Plugin.PlcIcons.icons.coil.svg`); yukarıdaki
`GetManifestResourceStream` de bunu ister. `plugin.json`, her C# eklentisinde olduğu gibi çıktıya kopyalanır:

<<< @/../PLCIcons/plugin.json

## Bir simge

Her simge `stroke="currentColor"` ile çizen tek renkli bir SVG'dir. Düzenleyici simgeyi kök `<svg>` öğesinde `color` ayarlayarak renklendirir,
bu yüzden renkleri asla sabit yazmayın. Bu, `coil.svg` dosyasıdır:

<<< @/../PLCIcons/src/icons/coil.svg{xml}

## Simge ekleme

1. `src/icons/` içine yeni bir `.svg` koyun. Tek renkli tutun ve her çizgi ve dolgu için `currentColor` kullanın.
2. Dosya adını (uzantısız) `Names` dizisine ekleyin.
3. Eklentiyi derleyin ve yeniden kurun (veya yeniden yükleyin).
