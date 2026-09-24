# İzinler

İzinler **yalnızca JavaScript eklentileri** için geçerlidir. C# eklentisi tam güvene sahiptir ve `permissions` alanı yok sayılır.

Betiğin ihtiyaç duyduklarını `plugin.json` içindeki `permissions` dizisinde bildirin. Kullanıcı listeyi Eklentiler penceresinde görür ve betik çalışmadan önce onaylaması gerekir (durum *Needs approval*, yani onay bekliyor). Onay tam o küme içindir: daha fazla izin isteyen bir güncelleme yeniden onay bekler. Bir eklenti kaldırıldığında onayı da unutulur.

| İzin | Betiğin yapmasına izin verir |
|---|---|
| `variables` | herhangi bir değişkeni okumak ve kendi değişkenini yayınlamak |
| `actions` | aksiyon kaydetmek |
| `input` | bilgisayarda tuş kombinasyonlarına basmak ve metin yazmak |
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

C# eklentileri için karşılığı güvendir: eklenti sunucu işleminin içinde çalışır ve sunucunun yapabildiği her şeyi yapabilir. Bkz.
[Eklenti temelleri](/tr/basics/).
