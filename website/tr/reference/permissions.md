# İzinler

İzinler, üçüncü tarafların yazabildiği tek tür olan **JavaScript eklentileri** için geçerlidir. (C# eklentileri resmî ve imzalıdır; sunucu başkasını yüklemez ve
onlar izin kullanmaz.)

Betiğin ihtiyaç duyduklarını `plugin.json` içindeki `permissions` dizisinde bildirin. Kullanıcı listeyi Eklentiler penceresinde görür ve betik çalışmadan önce onaylaması gerekir (durum *Needs approval*, yani onay bekliyor). Onay tam o küme içindir: daha fazla izin isteyen bir güncelleme yeniden onay bekler. Bir eklenti kaldırıldığında onayı da unutulur.

| İzin | Betiğin yapmasına izin verir |
|---|---|
| `variables` | herhangi bir değişkeni okumak ve kendi değişkenini yayınlamak |
| `actions` | aksiyon kaydetmek |
| `input` | bir düğme basışı işlenirken bilgisayarda tuş kombinasyonlarına basmak ve metin yazmak (aşağıya bakın) |
| `http:<host>:<port>` | tam olarak o ana bilgisayara ve porta HTTP istekleri göndermek (her hedef için bir girdi, örneğin `http:localhost:4455`) |

Zamanlayıcılar, ayar sayfaları, durum öğeleri ve günlük kaydı izin gerektirmez.

- Bilinmeyen bir izin dizesi eklentiyi *Error* (hata) durumuna sokar.
- İzni olmayan bir çağrı, betiğin yakalayabileceği sıradan bir JavaScript `Error` fırlatır.
- HTTP kesindir: yalnızca onaylanmış `host:port` çiftleri, yönlendirmeler izlenmez (bir yönlendirme onaylanan ana bilgisayarın dışına çıkabilir), istekler 5 saniye sonra zaman aşımına uğrar ve yanıtlar 1 MB ile sınırlıdır.
- Değişken adları ve aksiyon türleri, izinler ne olursa olsun `<plugin id>.` ile başlamalıdır.

Yalnızca kullandığınız izinleri isteyin. JavaScript örneğinden bir manifest parçası:

```json
"permissions": ["variables", "actions"]
```

## `input` izni

`input`, bir eklentinin sanki klavyenin başındaymış gibi bilgisayarda tuşlara basmasını ve yazı yazmasını sağlar. Asıl karar kişinin onayıdır; bu yüzden Eklentiler penceresi şunu söyler:
"Butonlarından birine bastığında, sanki klavyenin başındaymışsın gibi bilgisayarında tuşlara basabilir ve yazı yazabilir. Yalnızca güvendiğin eklentilere izin ver."
Onayın üstüne sunucu, JavaScript eklentileri için şu sınırları uygular (kişinin kendisinin ayarladığı yerleşik kısayol ve metin yazma aksiyonları değişmez):

- **Yalnızca gerçek bir basış işlenirken.** `host.input.*` yalnızca bir cihaza dokunuşla başlayan bir aksiyonun içinde ve yalnızca o aksiyon ile döndürdüğü promise bitene ya da
  5 saniye geçene kadar (hangisi önceyse) çalışır. Bir zamanlayıcıdan, başlangıçta ya da bir basış sırasında başlatılmamış bir web isteğinden çağrılırsa
  `Keyboard input is only allowed while handling a button press.` hatası fırlatır.
- **Neden 5 saniye.** Bir aksiyon çoğu zaman yazmadan önce bir web isteğini bekler; bu yüzden pencerenin bir isteği aşacak kadar uzun olması gerekir (isteğin kendisi 5 saniyede
  zaman aşımına uğrar). Aynı zamanda bilerek kısadır: bir basış saklanıp sonra yeniden kullanılamaz, yani kimse bir düğmeye basmıyorken bir eklenti yazı yazamaz.
- **Küçük miktarlar.** Bir basışta en fazla 200 yazılan karakter ve 10 tuş kombinasyonu; tek bir `host.input.type` çağrısı en fazla 200 karakter alır. Fazlası hata fırlatır.
- **Reddedilen tuş kombinasyonları.** Windows tuşlu her şey (Başlat, Çalıştır, güç kullanıcısı menüsü, ayarlar) ile Görev Yöneticisi'ni ya da güvenlik ekranını açanlar. Kişi bunları
  yerleşik kısayol aksiyonuyla yine kullanabilir.
- **Reddedilen hedef pencereler.** Önde bir komut istemi, kabuk veya terminal, bir betik ana bilgisayarı, kayıt defteri düzenleyicisi, bir sistem yönetim konsolu, Görev Yöneticisi,
  Başlat menüsü, Çalıştır gibi bir sistem iletişim kutusu ya da bir Macro Grid penceresi varsa (bir eklenti kendi onayına asla tıklamamalıdır), öndeki pencere tanınamıyorsa
  ya da Macro Grid yönetici olarak çalışıyorsa hiçbir şey gönderilmez.
- **Zararlı metnin engelleme listesi.** Bir basış sırasında yazılan metin normalleştirilir (küçük harf, boşluklar daraltılır, kaçış karakterleri ve tırnaklar kaldırılır) ve bir kabuk ya da betik ana bilgisayarı başlatma,
  bir şey indirip çalıştırma, kodlanmış komutlar, kayıt defterini, hizmetleri, zamanlanmış görevleri, kullanıcıları veya güvenlik duvarını değiştirme, sürücü silme ya da biçimlendirme gibi şeyler için denetlenir.
  Eşleşmede çağrı reddedilir, eklenti kapatılır ve günlüğe kuralın adını (metni asla değil) yazan bir `Security:` satırı düşer. Bu zayıf kuraldır: dolanılabilir (yazım hileleri, başka bir programa yazma)
  ve meşru metni de reddedebilir. Kaba ve kopyalanmış saldırıları yakalar; gizli kötüye kullanımı gerçekten durduran, basış kuralı ve pencere kuralıdır.
- **Görünür kullanım.** Klavyeyi kullanan her basış sayılır ve eklentinin Eklentiler penceresindeki satırında "Bugün klavyeyi N kez kullandı" görünür.

**Bunun çözmediği:** `input` izni olan onaylı bir eklenti, siz düğmesine bastığınızda sıradan bir programa yine de 200 karaktere kadar yazabilir. `input` iznini yalnızca güvendiğiniz eklentilere verin.
