# OBS Kontrolü

Macro Station için OBS Studio eklentisi. [obs-websocket v5](https://github.com/obsproject/obs-websocket)
üzerinden bağlanır (OBS 28+'da yerleşik olarak gelir, ayrı kurulum gerekmez).

## Gereksinimler

- OBS Studio 28 veya üstü.
- OBS içinde **Araçlar → WebSocket Server Ayarları**'ndan sunucu açık olmalı. Bir şifre belirlediyseniz
  aşağıdaki ayar formuna aynen yazın.

## Kurulum

1. Bu klasörü derleyin: `src/` içinde `dotnet build` (Debug) veya `dotnet build -c Release`.
2. Editördeki **Eklentiler** penceresinden **"Klasörden Yükle…"** ile `src/bin/Debug/net10.0/` (ya da
   `Release/net10.0/`) klasörünü seçin — `plugin.json` derleme çıktısına otomatik kopyalanır, DLL ile
   yan yana durur.
3. Sunucuyu yeniden başlatın (yeni yüklenen bir plugin ancak açılışta taranır).

## Ayarlar

Editördeki **Eklentiler** penceresinde OBS satırının yanındaki dişli ikonuna tıklayın — **Etkin**,
**Sunucu**, **Port**, **Şifre** alanları ve bir **Kaydet** düğmesi açılır (host'un genel schema-driven ayar
penceresi, bkz. `../docs/plugin-authoring.md` §"`SettingField` — şema-tabanlı formlar" — bu eklenti kendi
`ObsSettingsPage : IPluginSettingsPage`'ini kaydeder, editörde artık OBS'e özel kod yok). Kaydettikten
sonra sunucuyu yeniden başlatmanıza gerek yok; ayar sayfası kaydedince bağlantı döngüsüne haber verir,
birkaç saniye içinde yeni ayarlarla yeniden bağlanır. Ayarlar diskte
`%AppData%/MacroStation/plugins/obs/settings.json`'da tutulur, ilk çalıştırmada yoksa şu varsayılanlarla
kendisi oluşturulur:

```json
{ "enabled": false, "host": "127.0.0.1", "port": 4455, "password": "" }
```

## Durum çubuğu

Editörün pencere-geneli durum çubuğunda "OBS · bağlı" / "OBS · bağlanıyor…" / "OBS · şifre hatalı" / "OBS
· Ns sonra tekrar denenecek" gibi bir metin görünür; bağlıyken FPS ve (yayın/kayıt açıksa) süre de eklenir.
Tıklamak ayar penceresini açar.

## Sağladığı değişkenler (~45)

**Bağlantı:** `obs.connected`, `obs.status`, `obs.ws.in`, `obs.ws.out`.
**Sahne/durum:** `obs.scene.current`, `obs.scene.preview`, `obs.studioMode`, `obs.transition.current`,
`obs.profile.current`, `obs.sceneCollection.current`.
**Yayın:** `obs.streaming`, `obs.stream.reconnecting`, `obs.stream.duration`, `obs.stream.timecode`,
`obs.stream.congestion`, `obs.stream.bytes`, `obs.stream.kbps`, `obs.stream.frames.dropped/total/droppedPercent`.
**Kayıt:** `obs.recording`, `obs.record.paused`, `obs.record.duration`, `obs.record.timecode`,
`obs.record.bytes`, `obs.record.kbps`.
**Çıkışlar:** `obs.virtualcam`, `obs.replayBuffer`.
**İstatistik:** `obs.stats.fps`, `obs.stats.cpu`, `obs.stats.memory`, `obs.stats.disk`, `obs.stats.renderTime`,
`obs.stats.render.skipped/total/skippedPercent`, `obs.stats.output.skipped/total/skippedPercent`.
**Dinamik:** her ses girişi için `obs.input.<slug>.muted`/`obs.input.<slug>.volumeDb`; her sahne öğesi için
`obs.item.<sahneSlug>.<kaynakSlug>.visible`. `<slug>`, adın küçük harfe çevrilip `[a-z0-9]` dışındaki her
karakter dizisinin `_` ile değiştirilmiş hali. Giriş/öğe silinince ya da yeniden adlandırılınca ilgili
değişken `IVariableStore.Remove` ile kaldırılır — sonsuza kadar birikmez.

## Sağladığı aksiyonlar (~20)

Hepsi editörün aksiyon seçicisinde "OBS" kategorisinde, kendi şema-tabanlı formlarıyla (sahne/ses
kaynağı/öğe seçiciler OBS'e canlı bağlanmadan, önbellekten doldurulur):

- Sahne: `obs.setScene`, `obs.setPreviewScene`, `obs.studioTransition`, `obs.toggleStudioMode`,
  `obs.setTransition`, `obs.setProfile`.
- Yayın/kayıt: `obs.startStream`/`stopStream`/`toggleStream`, `obs.startRecord`/`stopRecord`/`toggleRecord`,
  `obs.pauseRecord` (duraklat/devam et/aç-kapat).
- Çıkışlar: `obs.virtualCam`, `obs.replayBuffer` (başlat/durdur/aç-kapat), `obs.saveReplay`.
- Ses: `obs.setMute` (mod: sustur/aç), `obs.toggleMute`, `obs.setVolume` (0-100%, slider/knob'un canlı
  sürüklenen değeri varsa onu kullanır), `obs.adjustVolume` (±dB adım).
- Sahne öğesi/metin: `obs.setItemVisibility` (gruplar dahil, göster/gizle/aç-kapat),
  `obs.setText` (metin kaynağının içeriği, `{değişken}` şablonlarını destekler).

Artık var olmayan bir hedefe (silinmiş bir sahne/ses girişi) yönlendirilmiş bir aksiyon çalıştırıldığında
sessizce hiçbir şey yapmaz — açık bir hata fırlatır, sunucu logunda ve (ActionDispatcher üzerinden) hata
olarak görünür.

## Bağlantı katmanının dayanıklılığı

Bağlantı, zaman aşımlı (5s) bir handshake ve isteklerle kurulur; koparsa 2s'den 30s'ye kadar (jitter'lı)
artan bir bekleme ile yeniden dener. Yanlış şifre veya sürüm uyuşmazlığı (obs-websocket kapanış kodu
4009/4010/4012) tespit edilirse sonsuz yeniden denemek yerine **ayarlar değişene kadar durur** — durum
çubuğunda "şifre hatalı" gösterir, log spam yapmaz. OBS'in kendisi kapanırken (`ExitStarted` olayı)
bağlantı TCP'nin zaman aşımına uğramasını beklemeden hemen kapatılır. Durum sorguları (yayın/kayıt/istatistik)
tek bir `RequestBatch` çerçevesinde toplanır — OBS'in "gelen/giden mesaj" sayacı saniyede sabit ~1-2 çerçeve
artar, önceki sürümdeki gibi saniyede 4 ayrı istek göndermez. Sahne/giriş/profil gibi her şey yalnızca
olaylardan (event) güncellenir, hiçbir zaman polling ile sorgulanmaz.

Sunucu OBS ile aynı makinedeyse, 3 başarısız bağlantı denemesinden sonra soket denemeye ara verilir ve
yalnızca `obs64`/`obs32`/`obs` sürecinin var olup olmadığına bakılır (5s'de bir) — OBS kapalıyken boşuna
TCP bağlantı denemesi yapıp CPU/ağ harcamamak için. Süreç görülünce (veya sunucu başka bir makinedeyse
normal backoff sırasında) hemen yeniden bağlanmayı dener.
