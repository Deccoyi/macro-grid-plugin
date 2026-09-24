# Hızlı başlangıç

Bu sayfa sizi sıfırdan telefonunuzda çalışan bir düğmeye götürür. Kullanıcı dokümantasyonunun tamamı
[sunucu deposundadır](https://github.com/Deccoyi/macro-grid); burası eklenti yazmadan önce ihtiyaç duyacağınız kısa yoldur.

## Gereksinimler

- Sunucu için Windows 10 veya 11.
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (güncel Windows'un parçasıdır).
- Android uygulaması yüklü, **aynı yerel ağdaki** bir telefon veya tablet ya da herhangi bir tarayıcı.

## 1. Sunucuyu kurun

Proje alfa aşamasında olduğu için henüz yayımlanmış sürüm yok. İlk sürüme kadar sunucuyu kaynaktan derlersiniz
([.NET 10 SDK](https://dotnet.microsoft.com/download) ve [Node.js](https://nodejs.org/) 20 veya üzeri gerekir):

```powershell
cd editor;    npm install; npm run build; cd ..
cd webclient; npm install; npm run build; cd ..
Copy-Item editor\dist\*    src\MacroGrid.Host\wwwroot\editor -Recurse -Force
New-Item -ItemType Directory -Force src\MacroGrid.Host\wwwroot\deck | Out-Null
Copy-Item webclient\dist\* src\MacroGrid.Host\wwwroot\deck -Recurse -Force
dotnet run --project src/MacroGrid.Host
```

Bir sürüm yayımlandığında sunucu bir Windows yükleyicisi (`MacroGrid-Setup-<version>.exe`) olarak gelir; `Program Files\Macro Grid` altına kurulur,
özel ağlar için TCP 9820 portunu açar ve kaldırıldığında verilerinizi `%AppData%\MacroGrid` içinde bırakır.
Sunucu deposunun [Sürümler sayfasına](https://github.com/Deccoyi/macro-grid/releases) bakın.

Sunucu bir **sistem tepsisi simgesi** olarak görünür; menüsü Düzenleyici'yi açar.

::: warning Yalnızca güvendiğiniz ağlarda kullanın
Trafik şifrelenmez ve sunucu 9820 portunda tüm arayüzleri dinler. Güvendiğiniz bir ev veya ofis ağında kullanın,
portu asla internete yönlendirmeyin ve yalnızca güvendiğiniz cihazları eşleştirin: eşleşmiş bir cihaz bilgisayarınızda tuşlara basabilir, metin yazabilir
ve programlar başlatabilir.
:::

## 2. Telefonunuzu eşleştirin

1. Düzenleyicide **Eşleştirme** penceresini açın. Beş dakika geçerli, altı haneli bir **PIN** ve bir **QR kodu** gösterir.
2. Telefonda uygulamayı açın ve QR kodunu tarayın ya da bilgisayarın adresini ve PIN'i girin.
3. Cihaz eşleşir ve profili gösterir. Bundan sonra PIN yerine bir belirteç (token) kullanır. Cihazların eşleşmesini Düzenleyici'den kaldırabilirsiniz.

QR kodu `macrogrid://pair?host=<ip>&port=9820&pin=<pin>` bilgisini içerir. Bir tarayıcı da deck olarak çalışabilir:
`http://<PC address>:9820/deck/` adresini açın ve PIN ile eşleştirin.

## 3. İlk profiliniz ve düğmeniz

1. Düzenleyicide bir **profil** oluşturun (bir profilin bir ya da birkaç sayfası vardır; sayfa bir ızgaradır).
2. Izgaraya bir **düğme** widget'ı sürükleyin ve ona bir metin verin, örneğin `CPU {system.cpu|0}%`.
3. Düğmenin **basma** olayına bir aksiyon bağlayın, örneğin yerleşik `core.hotkey` aksiyonunu `ctrl+shift+s` ile.
4. Kaydedin. Bağlı cihazlar hemen güncellenir; telefonda düğmeye basmak aksiyonu bilgisayarda çalıştırır.

Aynı olaya bağlanan birden çok aksiyon art arda çalışır; bu da bir makro oluşturur.

## 4. Eklentiler

Eklenti kurmak için Düzenleyici'de **Eklentiler, Eklentileri Yönet…** yolunu açın. Eklentiler, aksiyon seçiciye yeni aksiyonlar ve metin alanlarındaki
`{...}` seçiciye yeni değişkenler ekler. Bu depodaki eklentiler (OBS kontrolü, PLC simgeleri, bir JavaScript hello
world) [Kılavuzlar](/tr/guides/obs-plugin) bölümünde anlatılır ve [kendinizinkini yazabilirsiniz](/tr/tutorials/js-hello-world).
