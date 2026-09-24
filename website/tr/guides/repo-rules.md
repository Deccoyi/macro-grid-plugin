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
├── examples/                  eğitim projeleri, CI'da derlenir
└── <PluginName>/
    ├── plugin.json              manifest
    ├── README.md                ne yapar, gereksinimler, ayarlar
    ├── CHANGELOG.md             kısa, herkese açık, geliştirici olmayanlar için
    ├── CHANGELOG-developer.md   ayrıntılı, teknik
    └── src/                     kaynak (bir C# projesi veya JavaScript eklentisi için betik)
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

Her eklentinin kökünde bir `plugin.json` bulunur; alanlar [manifest başvurusunda](/tr/reference/manifest) yer alır. `sdkVersion` ve `minServerVersion`, sunucu eklentiyi yüklerken denetlenir. Bunları dürüstçe ayarlayın.

## C# ve JavaScript eklentileri

- **C#** eklentileri tam güvene sahiptir. Diğer eklentileri bozamasınlar diye kendi assembly yükleme bağlamlarına yüklenirler, ancak yalıtılmış ortamda (sandbox) çalışmazlar.
- **JavaScript** eklentileri yalıtılmış ortamda çalışır ve onaylanmış izinler gerektirir. Yalnızca kullandığınız izinleri isteyin.
- Bir eklenti klasörü ya birdir ya diğeri, asla ikisi birden değil.

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

Mantık içeren bir eklentinin yanında testleri olmalıdır (eklentiyi sahte bir obs-websocket sunucusuna karşı çalıştıran `OBS/tests` örneğine bakın). Çekme isteği açmadan önce eklentinin test projesinde `dotnet test` çalıştırın.
