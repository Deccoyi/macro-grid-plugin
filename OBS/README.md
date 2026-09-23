# OBS Kontrolü

Macro Station için OBS Studio eklentisi. [obs-websocket v5](https://github.com/obsproject/obs-websocket)
üzerinden bağlanır (OBS 28+'da yerleşik olarak gelir, ayrı kurulum gerekmez).

## Gereksinimler

- OBS Studio 28 veya üstü.
- OBS içinde **Araçlar → WebSocket Server Ayarları**'ndan sunucu açık olmalı. Bir şifre belirlediyseniz
  aşağıdaki `settings.json`'a aynen yazın.

## Kurulum

1. Bu klasörü derleyin: `src/` içinde `dotnet build` (Debug) veya `dotnet build -c Release`.
2. Editördeki **Eklentiler** penceresinden **"Klasörden Yükle…"** ile `src/bin/Debug/net10.0/` (ya da
   `Release/net10.0/`) klasörünü seçin — `plugin.json` derleme çıktısına otomatik kopyalanır, DLL ile
   yan yana durur.
3. Sunucuyu yeniden başlatın (yeni yüklenen bir plugin ancak açılışta taranır).

## Ayarlar

Editördeki **Eklentiler** penceresinde OBS satırının yanındaki dişli ikonuna tıklayın — **Etkin**,
**Sunucu**, **Port**, **Şifre** alanları ve bir **Kaydet** düğmesi açılır. Kaydettikten sonra sunucuyu
yeniden başlatmanıza gerek yok; bağlantı döngüsü ayarları her yeniden bağlanma denemesinde diskten tekrar
okur (en geç birkaç saniye içinde bağlanır).

Bu form, host'un plugin'lere özel bir ayar şeması/UI'ı olmadığı için (bkz. `../docs/plugin-authoring.md`
§"Ayarlar") host'taki genel `/api/plugins/{id}/settings` (GET/PUT, ham JSON) uç noktası üzerinden bu
eklentinin kendi `%AppData%/MacroStation/plugins/obs/settings.json`'ını okuyup yazıyor — editördeki form
OBS'e özel, ama taşıma katmanı jenerik. Eklenti ilk çalıştığında bu dosyayı yoksa şu varsayılanlarla
kendisi oluşturur:

```json
{
  "enabled": false,
  "host": "127.0.0.1",
  "port": 4455,
  "password": ""
}
```

## Sağladığı değişkenler

`obs.connected`, `obs.streaming`, `obs.stream.duration`, `obs.recording`, `obs.record.duration`,
`obs.scene.current`, `obs.stats.fps`, `obs.stats.cpu`.

## Sağladığı aksiyonlar

`obs.setScene` (ayar: `sceneName`), `obs.startStream`/`obs.stopStream`/`obs.toggleStream`,
`obs.startRecord`/`obs.stopRecord`/`obs.toggleRecord`, `obs.setMute` (ayar: `inputName`, `muted`),
`obs.toggleMute` (ayar: `inputName`), `obs.setVolume` (ayar: `inputName`, `volume` — 0..1 doğrusal çarpan,
OBS'in kendi `inputVolumeMul`'ıyla birebir, dB değil).

Not: editörde bu aksiyonlar için henüz özel bir ayar formu (sahne/giriş adı seçici) yok — widget'ın
aksiyon ayarları JSON'u elle düzenlenmeli, ya da editöre plugin-özel ayar paneli desteği eklenene kadar
bu haliyle kullanılmalı.
