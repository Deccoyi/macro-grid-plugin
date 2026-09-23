# Changelog — OBS Kontrolü

Bu dosya yalnızca bu plugin'in sürümünü takip eder (ana programdan bağımsız — bkz. `../agent-and-repo-rules.md` madde 1).

## [0.1.0] - 2026-09-23
### Added
- İlk sürüm: obs-websocket v5 bağlantısı (Hello/Identify handshake, SHA256 tabanlı kimlik doğrulama,
  otomatik yeniden bağlanma).
- Değişkenler: `obs.connected`, `obs.streaming`, `obs.stream.duration`, `obs.recording`,
  `obs.record.duration`, `obs.scene.current`, `obs.stats.fps`, `obs.stats.cpu`.
- Aksiyonlar: sahne değiştirme, yayın/kayıt başlat-durdur-aç/kapat, giriş sesi kapat/aç/seviye ayarla.
- Ayarlar `%AppData%/MacroStation/plugins/obs/settings.json`'da tutulur (editörde henüz ayar ekranı yok).
