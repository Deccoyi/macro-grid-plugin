# Gerçek dünyadan bir eklenti: OBS

[OBS eklentisi](https://github.com/Deccoyi/macro-grid-plugin/tree/main/OBS) (id `obs`), OBS 28 ve üzerinde yerleşik gelen obs-websocket v5
üzerinden [OBS Studio](https://obsproject.com/)'yu kontrol eder. Depodaki en büyük eklentidir ve SDK'nın sunduğu neredeyse her şeyi
kullanır; bu yüzden eğitimlerden sonra okunacak en iyi örnektir.

## Neler sunar

- *OBS* kategorisinde **22 aksiyon**: sahneler, stüdyo modu, geçişler, yayın, kayıt, tekrar arabelleği, sanal kamera,
  ses sessize alma ve seviyesi, sahne öğesi görünürlüğü ve metin kaynakları.
- `obs.` önekli **yaklaşık 45 canlı değişken**, örneğin `obs.streaming`, `obs.stream.duration`, `obs.scene.current`,
  `obs.stats.fps`; gelip giden öğeler için de `obs.input.<slug>.muted` ve `obs.item.<scene>.<source>.visible`.
- **Bir ayar sayfası** (Enabled, Server, Port, Password) ve bağlantı durumunu gösteren **bir durum çubuğu öğesi**.

WebSocket sunucusu açık, OBS 28 veya üzeri gerektirir (OBS'te **Tools, WebSocket Server Settings**). Eklentinin README dosyası her değişkeni ve
aksiyonu listeler.

## Giriş noktası

<<< @/../OBS/src/ObsPlugin.cs

`ObsConnection` adlı tek bir nesne bağlantıyı yönetir. Değişken sağlayıcı olarak kaydedilir, ayar sayfasına ve
her aksiyona verilir.

| Dosya | Rol |
|---|---|
| `ObsPlugin.cs` | `IPlugin`: bağlantıyı oluşturur ve her şeyi kaydeder (yukarıda). |
| `ObsConnection.cs` | `IVariableProvider` ve `IVariableCatalogSource`. Eklentinin ömrü boyunca çalışır: bağlanır, değişkenleri güncel tutar, durum öğesini günceller ve üstel geri çekilmeyle (üst sınırlı) yeniden bağlanır. |
| `ObsClient.cs`, `ObsAuth.cs`, `ObsJson.cs` | Küçük bir obs-websocket v5 istemcisi: el sıkışma, kimlik doğrulama, istekler ve olaylar. |
| `ObsState.cs` | OBS'in bildirdiklerinin (sahneler, girişler, sahne öğeleri) önbelleği. |
| `ObsActions.cs` | Aksiyonlar. Her biri `IActionHandler`, `IActionDescriptor` ve açılır listeler için `IOptionsSource` uygular. |
| `ObsSettings.cs` | `ObsSettings` (`DataDirectory` içinde `settings.json` olarak saklanır) ve `ObsSettingsPage : IPluginSettingsPage`. |

## Kopyalamaya değer kalıplar

- **Uzun soluklu bir sağlayıcı.** `ObsConnection.RunAsync`, token'ı iptal edilene kadar çalışır. Durum öğesini
  `host.CreateStatusItem("connection")` ile oluşturur, değişkenleri kendisine verilen `IVariableStore` üzerinden yayınlar ve başarısız bir
  bağlantının yöntemi bitirmesine asla izin vermez: üst sınırlı üstel geri çekilmeyle yeniden dener ve bir ayar değişikliği onu erken uyandırır.
- **Dinamik değişkenler kaldırılır.** Bir OBS girişi veya sahne öğesi silindiğinde ya da yeniden adlandırıldığında eklenti, değişkenini
  `store.Remove(name)` ile kaldırır; böylece seçicide görünmeyi bırakır.
- **Önbellekten açılır listeler.** Aksiyonlar `IOptionsSource` uygular; sahne, giriş ve öğe listeleri OBS'in bildirdiklerinin önbelleğinden gelir,
  dolayısıyla canlı bir gidiş dönüşü beklemezler.
- **Açık hatalar.** Artık var olmayan bir şeyi hedefleyen bir aksiyon (OBS'te silinmiş bir sahne) açık bir
  mesajla başarısız olur. Sunucu bunu telefonda ve Düzenleyici'nin durum çubuğunda gösterir, böylece eskimiş bir düğme asla sessiz kalmaz.
- **Ayarlar hemen uygulanır.** Ayar sayfasının `Save` yöntemi `settings.json` dosyasını yazar ve bağlantıyı uyarır; böylece düzeltilmiş bir sunucu,
  port veya parola kısa süre içinde yeniden bağlanır.
- **OBS olmadan testler.** `OBS/tests`, eklentiyi sahte bir obs-websocket sunucusuna (`FakeObsServer.cs`) karşı çalıştırır. `OBS/tests/MacroGrid.Plugin.Obs.Tests`
  üzerinde `dotnet test` çalıştırın.

Eklentinin kullanıcıya görünen metinleri (aksiyon adları, form etiketleri, durum metni) İngilizce yazılmıştır ve `OBS/locales/tr.json`
aracılığıyla Türkçeye çevrilir; kod, yorumlar ve dokümanlar İngilizcedir. Bu, depodaki her eklenti için geçerli kuraldır.

## Bir düzende kullanmak

1. Eklentiyi kurun ([Eklenti temelleri](/tr/basics/#eklenti-kurma)), dişli düğmesinden ayarlarını açın ve **Enabled** seçeneğini açın
   (eklenti devre dışı başlar).
2. Metni `{obs.scene.current}` olan bir düğme ekleyin ve sahne ayarlama aksiyonunu ona bağlayın.
3. `Live {obs.stream.duration}` etiketi ekleyin ve yayın aksiyonlarını bir toggle'a bağlayın.
