# Hata ayıklama ve günlükler

## Sunucu günlüğü

Sunucu her gün için tek bir günlük dosyası yazar: `%AppData%\MacroGrid\logs\server-YYYY-MM-DD.log`. Herhangi bir metin düzenleyiciyle açabilirsiniz.

- `host.log(message)` bu günlüğe, başında eklenti kimliğiniz olan bir satır yazar; örneğin
  `[hellojs] Hello, world! ...`. Günlükte `[<kimliğiniz>]` araması yapın.
- Bunu seyrek olaylar (bağlantı değişiklikleri, hatalar) için kullanın, yoklama (polling) için değil: eklenti başına günlük düzeyi yoktur.
- Sunucu ayrıca eklentilerin yüklenmesini ve kaldırılmasını günlüğe yazar; kaldırılmış bir eklentinin assembly'si hâlâ başvurulmaktaysa uyarı da yazar.

## Eklentiler penceresi

Her eklentinin bir durumu vardır ve adının altında bir ileti gösterilir:

| Durum | Olağan neden |
|---|---|
| **Uyumsuz** (*Incompatible*) | `minMacroGrid` bu Macro Grid'den daha yeni bir sürüm ya da başka bir MAJOR istiyor; adın altındaki mesaj hangisi olduğunu söyler. Bkz. [Uyumluluk](/tr/basics/compatibility). |
| **Onay bekliyor** (*Needs approval*) | Bir JavaScript eklentisinin izinleri henüz onaylanmadı. |
| **İzin verilmedi** (*Not allowed*) | Resmî olarak imzalanmamış ya da dosyaları imzasıyla artık eşleşmeyen bir C# eklentisi. Üçüncü taraf eklentiler JavaScript olmalıdır; resmî anahtar imzalamadıkça C# eklentisi asla yüklenmez. |
| **Hata** (*Error*) | `plugin.json` ayrıştırılamadı; giriş dosyası yok; `id` veya bir aksiyon türü zaten kullanımda; bilinmeyen bir izin dizesi var; `Initialize` veya betiğin ilk çalışması hata verdi; ya da eklenti art arda 5 kez başarısız olduğu için kapatıldı. |

Sorunu giderdikten sonra **Yeniden yükle** yeniden dener. *İzin verilmedi* durumundaki bir eklenti yalnızca kaldırılabilir.

## Çalışma zamanı hataları

- **Bir aksiyon hata fırlatır.** Sunucu hatayı yakalar, günlüğe yazar ve iletiyi widget'a basan telefonda ve düzenleyicinin durum çubuğunda gösterir. Açık iletiler fırlatın. Bir telefonun aksiyonları sırayla çalışır; yavaş bir aksiyon o telefonun sonraki aksiyonunu geciktirir, diğer telefonları değil.
- **Bir JavaScript çağrısı bütçesini aşar** (2 saniye, 32 MB, 2 milyon ifade, özyineleme derinliği 100): çağrı hatayla başarısız olur; eklenti çalışmaya devam eder. HTTP istekleri 5 saniye sonra zaman aşımına uğrar.
- **Bir JavaScript eklentisi art arda 5 kez başarısız olur:** kapatılır, durumu son iletiyle birlikte *Hata* olur. **Yeniden yükle** onu yeniden başlatır.
- **Bir izin eksik.** Çağrı, betiğin yakalayabileceği sıradan bir JavaScript `Error` fırlatır.

## Sık karşılaşılan sorunlar

- **Aksiyon veya değişken görünmüyor.** Değişkenler ve aksiyon türleri `<eklenti kimliği>.` ile başlamalıdır. Durumun *Loaded* olduğunu ve aksiyon türünün benzersiz olduğunu kontrol edin.
- **`host.input` hata veriyor.** Klavye girdisi yalnızca bir düğme basışı işlenirken ve ondan sonra en fazla 5 saniye çalışır; önde bir terminal, sistem aracı ya da
  Macro Grid penceresi varsa reddedilir. Bkz. [İzinler](/tr/reference/permissions#input-izni).
- **JavaScript kayıtları yok sayılıyor.** Aksiyonları, ayar sayfasını ve değişken açıklamalarını bir geri çağrı (callback) içinde değil, betiğin en üst düzeyinde kaydedin.
- **Değiştirdiğiniz betik etkili olmuyor.** Klasörü yeniden kurun (aynı `id` eskisinin yerine geçer) veya **Yeniden yükle**'ye tıklayın.
