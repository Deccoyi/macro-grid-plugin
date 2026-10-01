# Eklenti kaynak dizini

Sunucu, GitHub API'sini hiç kullanmadan (hız sınırı yok) GitHub üzerinden eklenti kurar: metadata için sabit bir
`raw.githubusercontent.com` URL'sini okur, sürüm varlıklarını (release asset) sabit bir `releases/download` URL'sinden indirir. Bu
sayfa ikisinin de biçimidir; hem resmî depo hem de birinin kaynak olarak eklediği herhangi bir üçüncü taraf deposu için geçerlidir.

## Çok eklentili depo: `macrogrid-index.json`

Birden fazla eklenti barındıran bir depo (bu depo gibi), kökünde, `main` üzerinde bir dizin dosyası tutar:

```
https://raw.githubusercontent.com/<owner>/<repo>/HEAD/macrogrid-index.json
```

```json
{
  "formatVersion": 1,
  "name": "Örnek Eklentiler",
  "author": "biri",
  "plugins": [
    {
      "id": "obs",
      "name": "OBS",
      "description": "Keşfet'te gösterilen tek satır.",
      "author": "biri",
      "homepage": "https://github.com/<owner>/<repo>/tree/main/WebSocketBridgeForOBS",
      "kind": "csharp",
      "versions": [
        {
          "version": "0.2.0",
          "minMacroGrid": "1.0.0",
          "url": "https://github.com/<owner>/<repo>/releases/download/plugin-obs-v0.2.0/obs-0.2.0.zip",
          "sha256": "<hex>",
          "size": 123456,
          "permissions": [],
          "signature": "<base64, yalnızca resmî kaynak>"
        }
      ]
    }
  ]
}
```

| Alan | Anlamı |
|---|---|
| `formatVersion` | Bugün her zaman `1`. Gelecekteki bir sürümü anlamayan bir sunucu çökmek yerine o kaynağı yok sayar. |
| `plugins[].id` / `.name` / `.description` / `.author` / `.homepage` | Herhangi bir şey indirilmeden önce Keşfet'te gösterilir. |
| `plugins[].kind` | `"csharp"` veya `"js"`, `plugin.json` ile eşleşir. Yalnızca resmî kaynak `"csharp"` eklentileri listeleyebilir; sunucu başka bir kaynağın `"csharp"` girdisini gizler ve reddeder. Üçüncü taraf kaynaklar `"js"` eklentileri listeler. |
| `versions[].minMacroGrid` | Yayımlanan `plugin.json`'dan kopyalanır; böylece sunucu, indirmeden önce uyumsuz bir sürümü soluklaştırabilir. `plugin.json`'da hâlâ varsa `macroGrid` (alanın önceki adı, yalnızca `minMacroGrid` yoksa okunur), `sdkVersion` ve `minServerVersion` (1.0.0 öncesi sunucular için eski alanlar) de kopyalanır. |
| `versions[].url` | `https://github.com/<aynı owner>/<aynı repo>/releases/download/...` olmalıdır — **bir dizin yalnızca kendi deposunun sürümlerine işaret edebilir.** Sunucu başka bir host veya depoyu reddeder. |
| `versions[].sha256` / `.size` | Zorunlu. İndirilen dosya, zip'i açmadan önce buna göre denetlenir. |
| `versions[].permissions` | Yalnızca JavaScript; aksi halde boş dizi. Zip'in `plugin.json`'uyla tam eşleşmelidir. |
| `versions[].signature` | Yalnızca resmî kaynak için anlamlıdır (aşağıya bakın); bu alan olsa bile diğer kaynaklar üçüncü taraf sayılır. |

Sunucu, indirmeden sonra ayrıca şunu denetler: zip'in kendi `plugin.json`'u (`id`, `version`, `minMacroGrid`, varsa eski alanlar,
`kind`, `permissions`) dizin girdisiyle tam eşleşmelidir, yoksa kurulum reddedilir.

## Tek eklentili depo (yapıştırılan bir bağlantı)

Kökünde tek bir eklenti bulunan bir depoda dizin yoktur; sunucu `plugin.json`'u doğrudan okur:

```
https://raw.githubusercontent.com/<owner>/<repo>/HEAD/plugin.json
```

Bu, `id`, `version`, `minMacroGrid`, `kind` ve `permissions` alanlarını verir — herhangi bir şey indirilmeden
önce uyumluluğu göstermeye yeter. Paket ve özeti (hash), bu `version`'dan türetilen sabit URL'lerden gelir:

```
https://github.com/<owner>/<repo>/releases/download/v<version>/<id>-<version>.zip
https://github.com/<owner>/<repo>/releases/download/v<version>/<id>-<version>.zip.sha256
```

Zip'in kendi `plugin.json`'u, `main`'den okunanla eşleşmelidir. Depoda kök `plugin.json` yerine `macrogrid-index.json` varsa,
sunucu bunu doğrudan kurmak yerine kaynak olarak eklemeyi önerir (yukarıdaki çok eklentili akış).

## Resmî kaynak imzalama

Resmî depo ([`Deccoyi/macro-grid-plugin`](https://github.com/Deccoyi/macro-grid-plugin)) ayrıca her sürümü, yayımlayanın
makinesinden hiç çıkmayan özel bir ECDSA P-256 anahtarıyla imzalar:

- Sürüm betiği (`scripts/release-plugin.ps1`), zip'in baytlarını okur, `ECDsa.SignData(bytes, HashAlgorithmName.SHA256)` ile imzalar (onaltılık `sha256`
  metnini değil — ham dosya baytlarını) ve sonucu base64 ile `<zip>.sig` olarak kodlar; bu bir IEEE P1363 (`r`&#8203;`||`&#8203;`s`,
  64 bayt) imzasıdır.
- `macrogrid-index.json`, o sürümün `signature` alanında aynı base64 değerini taşır.
- Sunucu, eşleşen genel anahtarı gömülü tutar ve herhangi bir şey kurmadan önce `ECDsa.VerifyData(bytes, signature,
  HashAlgorithmName.SHA256)` ile doğrular. Doğrulaması başarısız olan bir paket yalnızca uyarılmaz, reddedilir.
- Resmî olmayan bir kaynağın girdisindeki bir imza, onu resmî yapmaz — yalnızca gömülü resmî kaynak URL'si üzerinden alınan
  paketler bu şekilde doğrulanır ve güvenilir. Geri kalan her şey her zaman üçüncü taraf olarak gösterilir.

Bu yüzden resmî sürümler yerelde derlenip imzalanır ve özel anahtar hiçbir zaman GitHub'da tutulmaz: ele geçirilmiş bir GitHub
hesabı, sunucunun resmî olarak kabul edeceği bir paket üretemez.

## Resmî katalog dosyaları

Uygulama resmî kataloğu `website/public/catalog/` klasöründeki iki imzalı dosyadan okur:

- `index.signed.json`: `macrogrid-index.json` ile aynı liste.
- `revoked.signed.json`: güvenlik listesi, `{ "formatVersion": 1, "plugins": [ { "id": "...", "versions": ["1.0.0"], "reason": "..." } ] }`. `versions` yazılmazsa eklentinin her sürümü kapsanır.
  Listedeki resmî eklenti sürümü uygulamada nedeniyle birlikte kapatılır.

Her dosya `{ "payload": "<base64>", "signature": "<base64>" }` biçimindedir. İmza, paketlerle aynı anahtarla, çözülmüş payload baytları üzerinde ECDSA P-256 / SHA-256'dır ve payload okunmadan önce denetlenir.
Payload'ın başında `kind`, her imzalamada artan `sequence` ve `issuedAt` bulunur; uygulama kabul ettiğinden düşük bir sıra numarasını reddeder. Dosya alınamazsa hiçbir şey değişmez:
**bir eklenti yalnızca doğrulanmış bir listeyle kapatılır, bir indirme başarısız oldu diye asla.**

## CI sizin için ne yapar

Eklentiniz bu deponun `scripts/release-plugin.ps1` betiğiyle yayımlanıyorsa, ya da `examples/third-party-release.yml` dosyasını kendi çok eklentili deponuza kopyaladıysanız (bkz.
[Eklentinizi yayımlama](/tr/guides/publishing)), `minMacroGrid`, `sha256`, `size` veya indirme `url`'sini
dizine hiçbir zaman elle yazmazsınız: sürüm adımı bunları derlemeden ve her sürümden sonra `plugin.json`'dan doldurur ve
`macrogrid-index.json`'u `main`'e geri işler. Yalnızca `plugin.json`'u ve değişiklik günlüklerini doğru tutmanız yeterlidir.
