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

Bu eklentinin editörde henüz bir ayar ekranı yok (host'un plugin'lere yönelik genel bir ayar arayüzü/
depolaması henüz yok — bkz. `../docs/plugin-authoring.md` §5). Bunun yerine, eklenti ilk çalıştığında
kendi klasörüne (`%AppData%/MacroStation/plugins/obs/settings.json`) şu varsayılanlarla bir dosya yazar:

```json
{
  "enabled": false,
  "host": "127.0.0.1",
  "port": 4455,
  "password": ""
}
```

`enabled: true` yapıp OBS'in WebSocket şifresini (varsa) `password` alanına yazın, kaydedin — sunucuyu
yeniden başlatmanıza gerek yok, bağlantı döngüsü ayarları her yeniden bağlanma denemesinde diskten
tekrar okur (en geç birkaç saniye içinde bağlanır).

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
