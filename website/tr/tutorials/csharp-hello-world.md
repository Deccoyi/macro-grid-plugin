# Öğretici 2: C# ile merhaba dünya

Bu öğreticide boş bir klasörden küçük bir C# eklentisi geliştiriyorsunuz: ayar formu olan bir **aksiyon**, bir **ayar sayfası**, bir
**değişken** ve bir **durum çubuğu öğesi**. [.NET 10 SDK](https://dotnet.microsoft.com/download)'ya ve çalışan bir Macro Grid
sunucusuna ihtiyacınız var.

Bitmiş proje [`examples/hello-csharp`](https://github.com/Deccoyi/macro-grid-plugin/tree/main/examples/hello-csharp) klasöründedir
ve CI onu derler; yani aşağıdaki kod eskiyip bozulamaz. Her kod bloğu bu klasörden alınmıştır.

::: tip İpucu: C# eklentisine tam güvenilir
Sunucu sürecinin içinde, tam .NET erişimiyle çalışır. Kaynağına güvenmediğiniz bir C# eklentisini asla kurmayın.
:::

## Adım 1: projeyi oluşturun

```powershell
mkdir HelloCSharp
cd HelloCSharp
mkdir src
cd src
dotnet new classlib -n HelloCSharp -f net10.0 -o .
del Class1.cs
dotnet add package MacroGrid.Plugin.Abstractions --version 0.3.1
```

`MacroGrid.Plugin.Abstractions` eklenti SDK'sıdır: eklentinizin uyguladığı arayüzler. `HelloCSharp.csproj` dosyasını açın ve
`dotnet add package`'in oluşturduğu referansı şu hale getirin:

<<< @/../examples/hello-csharp/src/HelloCSharp.csproj#sdk{xml}

- **`ExcludeAssets="runtime"`** (`PrivateAssets="all"` ile birlikte) SDK'nın bir kopyasının çıktınıza girmesini engeller. Sunucu ve
  her eklenti, `MacroGrid.Plugin.Abstractions` için sunucunun kopyasını paylaşmalıdır; iki kopya olursa `is IActionHandler` gibi
  bir denetim başarısız olur, çünkü aynı tür iki ayrı kopyadan gelince iki farklı tür sayılır. Kendi kopyanızı asla göndermeyin.
- `Condition` öznitelikleri ve ikinci referans depo kuralına aittir (aşağıya bakın); kendi eklentinizde tek bir `PackageReference`
  yeterlidir.

Ayrıca `plugin.json` dosyasını derleme çıktısına kopyalayın; böylece çıktı klasörü doğrudan kurulabilir olur:

```xml
<ItemGroup>
  <None Include="..\plugin.json" Link="plugin.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

Örneğin tam proje dosyası, bu depodaki eklentilerle aynı `UseLocalSdk` / `MacroGridSdkVersion` kuralını izler (SDK varsayılan olarak
NuGet'ten gelir; `dotnet build -p:UseLocalSdk=true`, bunun yanındaki sunucu deposunun bir kopyasına karşı derler):

<<< @/../examples/hello-csharp/src/HelloCSharp.csproj{xml}

::: warning Uyarı: paket erişilebilirliği
SDK paketi nuget.org'a yayımlanmadığı sürece, paketi kendiniz oluşturup klasörü bir kaynak olarak eklemelisiniz:
`dotnet pack <server repo>\src\MacroGrid.Plugin.Abstractions -c Release -o C:\local-feed`, ardından
`dotnet build -p:RestoreSources=C:\local-feed`.
:::

Şimdi `HelloCSharp/plugin.json` manifestini oluşturun (`src`'nin bir üst klasörü):

<<< @/../examples/hello-csharp/plugin.json

`entry`, DLL'inizin dosya adıdır (`AssemblyName` artı `.dll`). `permissions` yalnızca JavaScript eklentileri için kullanılır.

## Adım 2: giriş noktası

Sunucu, giriş derlemesinde `IPlugin` uygulayan tam olarak bir sınıf bulur, onu parametresiz yapıcıyla oluşturur ve `Initialize`'ı
bir kez çağırır. Orada kaydettiğiniz her şey `Initialize` döndüğünde uygulanır. `HelloPlugin.cs` dosyasını oluşturun:

<<< @/../examples/hello-csharp/src/HelloPlugin.cs#plugin{cs}

`IPluginHost`, kayıt yaptığınız arayüzdür. Burada bir **durum çubuğu öğesi** oluşturur (`CreateStatusItem`), bir değişken sağlayıcı,
bir ayar sayfası ve bir aksiyon kaydeder. `Initialize` hata fırlatırsa sunucu bunu yakalar, eklentiyi *Hata* olarak gösterir ve
çalışmaya devam eder.

## Adım 3: aksiyon ve formu

Aksiyon, bir `IActionHandler`'dır (`Type`, `DisplayName`, `ExecuteAsync`). `IActionDescriptor`'ı da uygularsa düzenleyici onu bir
`Category` altında listeler ve ayar formunu `Fields`'tan, yani bir `SettingField` listesinden çizer. Arayüz kodu gerekmez.
`GreetAction.cs` dosyasını oluşturun:

<<< @/../examples/hello-csharp/src/GreetAction.cs#action{cs}

- `Type` benzersiz olmalıdır. Kural olarak `<eklenti kimliği>.<ad>` biçimindedir. Zaten kayıtlıysa eklentinin tamamı yüklenemez.
- `SettingField(key, label, kind)` tek bir alanı tanımlar. `SettingFieldKind` şunlardan biridir: `Text`, `Password`, `Number`,
  `Slider`, `Bool`, `Select` veya `Segmented`. `AllowVariables = true`, kullanıcının bir metin alanına `{variables}` eklemesini
  sağlar; siz ham şablonu alırsınız.
- `ExecuteAsync`, bir `ActionContext` (`DeviceId`, `PageId`, `WidgetId`, `Device`, slider veya knob için `Value`) ve formun
  değerlerini `JsonObject` olarak alır. Hata durumunda açık bir iletiyle istisna fırlatın: sunucu bunu günlüğe yazar ve telefonda
  ile düzenleyicinin durum çubuğunda gösterir.
- `host.Log`, sunucunun günlüğüne eklenti kimliğinizle başlayan bir satır yazar (bkz. [Hata ayıklama ve günlükler](/tr/guides/debugging)).
- `status.Update(text, level)` durum çubuğu öğesini değiştirir. Durum değiştiğinde güncelleyin, yüksek sıklıkta değil.

## Adım 4: ayar sayfası

Ayar sayfası bir `IPluginSettingsPage`'dir (`Fields`, `Load()`, `Save(values)`). Formu düzenleyici çizer; `Load` ve `Save` sizin
diske uzanan köprünüzdür. `HelloSettingsPage.cs` dosyasını oluşturun:

<<< @/../examples/hello-csharp/src/HelloSettingsPage.cs#settings{cs}

`host.DataDirectory` kendi kurulum klasörünüzdür (`%AppData%\MacroGrid\plugins\hellocsharp\`); yönetici hakkı olmadan yazılabilir.
Ayar sayfası olan bir eklenti, Eklentiler penceresinde bir dişli düğmesi alır ve durum öğesi sayfayı açar. Ayrıntılar için
[Ayar sayfaları](/tr/guides/settings-pages).

## Adım 5: bir değişken

Değişken sağlayıcı, widget'ların gösterebileceği canlı değerler yayınlar. `GreetingCounter.cs` dosyasını oluşturun:

<<< @/../examples/hello-csharp/src/GreetingCounter.cs#variables{cs}

`RunAsync`, eklenti yüklü olduğu sürece çalışır ve yalnızca belirteç (token) iptal edildiğinde dönmelidir. `store.Set(name, value)`
bir değer yayınlar (mevcut değere eşit bir değer yok sayılır). Aynı nesnede `IVariableCatalogSource` uygulamak, değişkeni
düzenleyicinin değişken seçicisinde listeler. Eklenti kaldırıldığında sunucu, onun ayarladığı tüm değişkenleri siler.

## Adım 6: derleyin

```powershell
dotnet build -c Release
```

`bin\Release\net10.0\` çıktı klasöründe `HelloCSharp.dll` ve `plugin.json` bulunur; `MacroGrid.Plugin.Abstractions.dll` **bulunmaz**.
SDK dll'i oradaysa `ExcludeAssets="runtime"` ayarını kontrol edin.

## Adım 7: kurun ve deneyin

1. Düzenleyicide **Eklentiler, Eklentileri Yönet…** yolunu açın, **Klasörden Yükle…** seçeneğini seçin ve `bin\Release\net10.0\`
   klasörünü gösterin.
2. Eklenti hemen yüklenir. Bir dişli düğmesi belirir: **Name**, **Greeting** ve **Loud** alanlarını ayarlayın.
3. `Greetings: {hellocsharp.count}` metinli bir düğme ekleyin ve **Greet** aksiyonunu (kategori *Hello*) basma olayına bağlayın.
4. Düğmeye basın. Sayaç artar, durum çubuğunda `Hello: <n> greetings` görünür ve sunucu günlüğüne
   `[hellocsharp] Hello, world! (pressed on device <id>)` gibi bir satır düşer.

## Adım 8: paketleyin

C# eklentisi, derleme çıktı klasörü olarak dağıtılır. `bin\Release\net10.0\` içeriğini (isterseniz `.pdb` olmadan) `LICENSE`
dosyanızla birlikte zipleyin. Kullanıcılar bunu `%AppData%\MacroGrid\plugins\<id>\` altına açar veya **Klasörden Yükle…** ile
gösterir. Bkz. [Eklentinizi yayımlama](/tr/guides/publishing).

## Sırada ne deneyebilirsiniz

- Değişkeninizi widget metninde gösterin ve biçimlendirin: [Öğretici 3](/tr/tutorials/live-data).
- Gerçek bir entegrasyonun nasıl düzenlendiğini okuyun: [OBS eklentisi](/tr/guides/obs-plugin).
- Tüm arayüzlere bakın: [C# SDK başvurusu](/tr/reference/csharp-sdk).
