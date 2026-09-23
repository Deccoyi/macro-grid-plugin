# Changelog — OBS Kontrolü

Bu dosya yalnızca bu plugin'in sürümünü takip eder (ana programdan bağımsız — bkz. `../agent-and-repo-rules.md` madde 1).

## [0.2.0] - 2026-09-23
### Added
- Bağlantı ayarları artık kendi `ObsSettingsPage : IPluginSettingsPage`'i ile schema-driven formda
  (Plugin SDK 0.3.0); editörde OBS'e özel kod kalmadı.
- `ObsState` önbelleği: sahneler, ses girişleri, gruplar dahil sahne öğeleri, profiller, geçişler —
  dropdown'lar OBS'e canlı bağlanmadan buradan doldurulur.
- Değişken kataloğu 8'den ~45'e çıktı (yayın/kayıt istatistikleri, stüdyo modu, sanal kamera/tekrar
  arabelleği, dinamik giriş/öğe değişkenleri; bkz. `README.md`).
- Yeni aksiyonlar: `obs.setPreviewScene`, `obs.studioTransition`, `obs.toggleStudioMode`,
  `obs.setTransition`, `obs.setProfile`, `obs.pauseRecord`, `obs.virtualCam`, `obs.replayBuffer`,
  `obs.saveReplay`, `obs.adjustVolume`, `obs.setItemVisibility`, `obs.setText`. Var olan aksiyonların
  hepsi artık `IActionDescriptor`+`IOptionsSource` ile schema-driven form kullanıyor.
- Artık silinmiş bir hedefe (kaldırılmış sahne/ses girişi) yönlendirilmiş bir aksiyon sessizce no-op
  olmak yerine açık hata veriyor.
- Durum sorguları tek bir `RequestBatch` çerçevesinde (1s yayın/kayıt açıkken, yoksa 5s); sahne/giriş/
  profil/vb. yalnızca olaylardan güncelleniyor, hiç polling yok.
- Editörün pencere-geneli durum çubuğunda canlı bağlantı durumu.
- Sunucu bu makinedeyse, 3 başarısız bağlantı denemesinden sonra `obs64`/`obs32`/`obs` süreci aranıyor;
  süreç yoksa denemeler durup 5s'de bir sessizce kontrol ediliyor, durum çubuğunda "OBS · çalışmıyor"
  gösteriliyor. OBS açılınca hemen (backoff beklemeden) yeniden bağlanıyor. `ExitStarted` olayı gelirse
  süreç kontrolü 3 başarısızlığı beklemeden yapılıyor. Sunucu başka bir makinedeyse normal backoff
  devam ediyor. Amaç: OBS kapalıyken boşuna soket denemesi yapıp kaynak tüketmemek.

### Fixed
- `ObsEventSubscription.Inputs` yanlış bit değerindeydi (aslında Transitions'tı) — `InputMuteStateChanged`
  gibi olaylar hiç gelmiyordu.
- Handshake ve her istek artık zaman aşımlı (5s); donmuş bir OBS veya yarı-açık bir TCP bağlantısı artık
  sonsuza kadar "bağlı" görünmüyor.
- Kapatılmış bir bağlantıya kayıtlı bir istek artık anında başarısız oluyor (önceden sonsuza kadar bekliyordu).
- Yanlış şifre (kapanış kodu 4009/4010/4012) artık sonsuz yeniden denemiyor ve log'u doldurmuyor — ayarlar
  değişene kadar bekliyor, durum çubuğunda "şifre hatalı" gösteriyor.
- Diğer bağlantı kopması nedenleri 2s'den 30s'ye (jitter'lı) artan backoff ile deneniyor.
- Aynı durum tekrar tekrar loglanmıyor, yalnızca değiştiğinde.
- Her mesaj için yeni arabellek ayırmak yerine tek bir arabellek yeniden kullanılıyor.
- Beklenmedik JSON tipleri artık receive loop'unu düşürmüyor (güvenli okuma yardımcıları).
- OBS kapanırken (`ExitStarted`) bağlantı TCP zaman aşımını beklemeden hemen kapatılıyor.
- Bağlantı koptuğunda yalnızca üç boolean değil, her `obs.*` değişkeni sıfırlanıyor/kaldırılıyor.

## [0.1.0] - 2026-09-23
### Added
- İlk sürüm: obs-websocket v5 bağlantısı (Hello/Identify handshake, SHA256 tabanlı kimlik doğrulama,
  otomatik yeniden bağlanma).
- Değişkenler: `obs.connected`, `obs.streaming`, `obs.stream.duration`, `obs.recording`,
  `obs.record.duration`, `obs.scene.current`, `obs.stats.fps`, `obs.stats.cpu`.
- Aksiyonlar: sahne değiştirme, yayın/kayıt başlat-durdur-aç/kapat, giriş sesi kapat/aç/seviye ayarla.
- Ayarlar `%AppData%/MacroStation/plugins/obs/settings.json`'da tutulur (editörde henüz ayar ekranı yok).
