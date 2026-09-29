# Depo kuralları

Bunlar, [macro-grid-plugin](https://github.com/Deccoyi/macro-grid-plugin) deposundaki her eklentinin izlediği kurallardır.
Kendi eklenti deponuz için de iyi bir şablondur. Esas kopya
[CONTRIBUTING.md](https://github.com/Deccoyi/macro-grid-plugin/blob/main/CONTRIBUTING.md) dosyasıdır; dallanma modelini ve çekme isteğinin (pull request) nasıl açılacağını da orada bulursunuz.

## Yapı

```
macro-grid-plugin/
├── README.md
├── CONTRIBUTING.md
├── website/                   bu dokümantasyon sitesi
├── examples/                  eğitim projesi, CI'da denetlenir
└── <PluginName>/
    ├── plugin.json              manifest
    ├── README.md                ne yapar, gereksinimler, ayarlar
    ├── CHANGELOG.md             kısa, herkese açık, geliştirici olmayanlar için
    ├── CHANGELOG-developer.md   ayrıntılı, teknik
    └── src/                     kaynak (resmî bir eklenti için bir C# projesi, ya da JavaScript eklentisi için betik)
```

## Bağımsız sürümler

- Her eklentinin `plugin.json` içinde, sunucunun ve diğer eklentilerin sürümünden bağımsız kendi anlamsal sürümü vardır. Bir eklentinin sürümü yalnızca o eklentinin kendi değişiklik günlüklerinde ilerler; bir sunucu sürümü onu asla değiştirmez.
- Bir eklenti, başka bir eklentinin sürümüne bağlı değildir.
- MAJOR sürümü yalnızca geriye dönük uyumsuz bir değişiklikte artırın: anlamı değişen aksiyon türleri veya ayarlar, yani kullanıcının kayıtlı profilinin sessizce çalışmaz hale geleceği durumlar.

## Yalıtım

- Bir eklenti klasörü asla başka bir eklentinin klasörüne başvurmaz: kardeş klasöre `ProjectReference`, `file:` bağımlılığı, göreli içe aktarma veya yol yoktur. Bir eklenti klasörünü silmek, başka hiçbir şeyin derlenmesini asla durdurmamalıdır.
- İki eklentinin de ihtiyaç duyduğu kod her birine kopyalanır veya kendi paketi olarak yayımlanıp paket olarak başvurulur. Yol ile asla paylaşılmaz.
- Eklentiler yalnızca çalışma zamanında, ana bilgisayarın (host) paylaşılan arayüzleri üzerinden konuşur (örneğin bir eklentinin yayımladığı ve diğerinin okuduğu bir değişken); derleme zamanı bağımlılığı asla olmaz.

## Manifest ve uyumluluk

Her eklentinin kökünde bir `plugin.json` bulunur; alanlar [manifest başvurusunda](/tr/reference/manifest) yer alır. `minMacroGrid` (eklentinin çalıştığı en eski Macro Grid), sunucu eklentiyi yüklerken denetlenir. Bunu dürüstçe ayarlayın.

## C# ve JavaScript eklentileri

- **JavaScript** eklentileri yalıtılmış ortamda çalışır ve onaylanmış izinler gerektirir. Yalnızca kullandığınız izinleri isteyin. Başka yazarların eklentileri
  JavaScript'tir ve yazarın kendi deposuna aittir.
- **C#** eklentileri yalnızca bu depodaki resmî eklentiler için kullanılır. Tam güvene sahiptir; bu yüzden bakımcı derler ve imzalar, sunucu da bir C# eklentisini
  yalnızca bu imzayla yükler. Diğer eklentileri bozamasınlar diye kendi assembly yükleme bağlamlarına yüklenirler, ancak yalıtılmış ortamda (sandbox) çalışmazlar.
- Bir eklenti klasörü ya birdir ya diğeri, asla ikisi birden değil.
- Bu depo proje dışından gelen çekme isteklerini genel olarak kabul etmez, çünkü resmî eklentiler insanların bilgisayarlarında çalışır. Bunun yerine bir issue açın;
  orada konuşulduktan sonra küçük bir düzeltme kabul edilebilir.

## Dil, yorumlar ve adlar

- Kod, tanımlayıcılar, yorumlar, dokümantasyon, değişiklik günlükleri, günlük iletileri ve commit iletileri **İngilizce** yazılır. Kullanıcının düzenleyicide gördüğü metinler (aksiyon adları, form etiketleri) eklentinin kendi dizeleri üzerinden geçer.
- Ürün, eklentinin işlevsel hedefi olmadıkça (örneğin OBS ile konuşan OBS eklentisi) kodda, yorumlarda, dokümantasyonda veya commit'lerde üçüncü taraf ürün veya marka adlarından söz etmeyin.

## Değişiklik günlükleri

Bir değişiklik tamamlandığında, değiştirdiğiniz eklentinin her iki değişiklik günlüğünü de `[Unreleased]` altında güncelleyin:

- `CHANGELOG-developer.md`: ayrıntılı ve teknik, [Keep a Changelog](https://keepachangelog.com/) tarzında (Added / Changed / Fixed).
- `CHANGELOG.md`: "New / Changed / Fixed" altında her değişiklik için kısa, sade bir cümle. Kod, dosya veya API adı yok; küçük hata düzeltmelerini ve iç değişiklikleri dışarıda bırakın.

## Commit'ler

İngilizce [Conventional Commits](https://www.conventionalcommits.org/): `type(scope): description`; kapsam olarak eklentiyi kullanın, örneğin `feat(obs): pause reconnecting while OBS is not running`.

## Testler

Mantık içeren bir eklentinin yanında testleri olmalıdır (eklentiyi sahte bir obs-websocket sunucusuna karşı çalıştıran `WebSocketBridgeForOBS/tests` örneğine bakın). Eklentiyi değiştirmeden önce test projesinde `dotnet test` çalıştırın.
