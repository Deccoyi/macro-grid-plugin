# Eklentinizi denetleyin, deneyin ve paketleyin

Eklenti aracı, JavaScript eklentisi yazanlar için küçük bir komut satırı programıdır. Macro Grid sürüm sayfasında ayrı bir indirme olarak bulunur (`MacroGrid-PluginTool-<sürüm>-win-x64.zip`) ve uygulamayla aynı kuralları ve aynı betik çalışma ortamını kullanır; yani aracın kabul ettiği bir eklentiyi uygulama da kabul eder.

| Komut | Ne yapar |
|---|---|
| `macrogrid-plugin new <id>` | Kimliğin adıyla bir klasörde eklenti başlatır: bir manifest, küçük çalışan bir betik ve bir README. |
| `macrogrid-plugin validate <klasör>` | Klasörü hiçbir şey çalıştırmadan denetler. Her bulgu için bir `error:` veya `warning:` satırı yazar, son satır `OK: <id> <sürüm>` veya `FAILED: <n> error(s)` olur. |
| `macrogrid-plugin run <klasör> --action <tür>` | Eklentiyi geçici bir veri klasöründe başlatır, eylemi bir kez çalıştırır ve izinleri, eylemleri, ayarları, durum öğelerini ve değişkenleri listeler. `--deny <izin>` eklentiyi o izin olmadan başlatır, `--seconds <n>` zamanlayıcıların çalışmasına izin verir. |
| `macrogrid-plugin pack <klasör>` | Klasörü denetler, sonra `<id>-<sürüm>.zip` ve bir `.sha256` dosyası yazar. Eklentinin kaydettiği veriler (`settings.json`, `storage.json`) ve gizli dosyalar pakete girmez. Hiçbir şey imzalamaz. |

Çıkış kodu 0 tamam, 1 eklentide hata var, 2 komut satırı anlaşılmadı demektir.

`run` komutunu kendi eklentiniz için kullanın. Başkasının eklentisini denemek için uygulamaya kurun; orada eklenti izinlerinin onayını ister. `run` ile başlatılan bir eklenti Macro Grid'in kendi bağlantı noktalarına hiçbir zaman ulaşamaz.

`pack` ile hazırladığınız paketi açtıktan sonra Eklentiler penceresindeki **Klasörden Yükle…** ile kurabilir veya [Eklentinizi yayımlama](/tr/guides/publishing) bölümündeki gibi yayımlayabilirsiniz.
