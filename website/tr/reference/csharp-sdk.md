# C# SDK arayüzleri

SDK, `MacroGrid.Plugin.Abstractions` NuGet paketidir (ad alanı `MacroGrid.Plugin.Abstractions`, sürüm `1.0.0`, Macro Grid ile aynı numara, çalışma zamanında `PluginSdk.Version` olarak sunulur). Aşağıdaki imzalar,
[sunucu deposundaki](https://github.com/Deccoyi/macro-grid/tree/main/src/MacroGrid.Plugin.Abstractions) SDK kaynağının imzalarıdır. Bir MAJOR içinde SDK yalnızca büyür.

## Giriş noktası

```csharp
public interface IPlugin
{
    void Initialize(IPluginHost host);
}
```

Sunucu, giriş assembly'sinde tam olarak bir uygulama bulur, parametresiz bir yapıcıyla oluşturur ve `Initialize` metodunu bir kez çağırır. Kayıtlar, `Initialize` döndükten sonra uygulanır.

```csharp
public interface IPluginHost
{
    string ServerVersion { get; }
    string SdkVersion { get; }
    string DataDirectory { get; }
    void Log(string message);
    void RegisterAction(IActionHandler handler);
    void RegisterVariableProvider(IVariableProvider provider);
    void RegisterSettingsPage(IPluginSettingsPage page);
    IPluginStatusItem CreateStatusItem(string id);
    void RegisterIconPack(IIconPackSource iconPack);
}
```

| Üye | Amacı |
|---|---|
| `ServerVersion`, `SdkVersion` | Çalışan sunucunun ve SDK'nın sürümleri. |
| `DataDirectory` | Eklentinin kendi kurulum klasörü (`%AppData%\MacroGrid\plugins\<id>\`), yazılabilir. Dosyalarınızı burada tutun. |
| `Log(message)` | Sunucunun eklenti günlüğünde, başında kimliğiniz olan bir satır. Seyrek olaylar için kullanın, yoklama için değil. |
| `RegisterAction` | Bir aksiyon türü ekler. |
| `RegisterVariableProvider` | Arka planda çalışan bir değişken kaynağı ekler. Aynı nesne `IVariableCatalogSource` arayüzünü de uyguluyorsa değişken seçicide de listelenir. |
| `RegisterSettingsPage` | Eklentiler penceresine bir ayar formu ekler. |
| `CreateStatusItem(id)` | Düzenleyicinin durum çubuğunda size ait bir girdi oluşturur. Her mantıksal durum için bir kez çağırın ve yeniden kullanın. |
| `RegisterIconPack` | Düzenleyicinin simge seçicisine simgeler ekler. |

## Aksiyonlar

```csharp
public interface IActionHandler
{
    string Type { get; }          // benzersiz, gelenek olarak "<plugin id>.<name>"
    string DisplayName { get; }
    Task ExecuteAsync(ActionContext context, JsonObject settings, CancellationToken cancellationToken);
}

public sealed record ActionContext(string DeviceId, string PageId, string WidgetId, IDeviceController Device, double? Value = null);
```

`Value` yalnızca bir `valueChange` gönderiminde (slider veya knob sürüklemesinin onaylanması) ayarlanır. Bir widget'ın olayları `press`, `release`,
`longPress`, `doubleTap`, `toggleOn`, `toggleOff` ve `valueChange` değerleridir.

```csharp
public interface IActionDescriptor      // isteğe bağlı, IActionHandler ile aynı sınıfta
{
    string Category { get; }
    string? Description { get; }
    string? Icon { get; }               // bir Lucide simge adı
    IReadOnlyList<SettingField> Fields { get; }
}

public interface IDeviceController      // ActionContext.Device: aksiyonu tetikleyen tek telefon
{
    Task ShowPageAsync(string pageId);
    Task NextPageAsync();
    Task PreviousPageAsync();
    Task BackAsync();
    Task SwitchProfileAsync(string profileId);
}
```

Bir aksiyonun istisnaları sunucu tarafından yakalanır: hata günlüğe yazılır ve iletisi telefonda ve düzenleyicinin durum çubuğunda gösterilir. Bir telefonun aksiyonları birbiri ardına çalışır.

## Formlar

```csharp
public enum SettingFieldKind { Text, Password, Number, Slider, Bool, Select, Segmented }

public sealed record SettingField(string Key, string Label, SettingFieldKind Kind)
{
    public string? Description { get; init; }
    public string? Placeholder { get; init; }
    public JsonNode? Default { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Step { get; init; }
    public SettingOption[]? Options { get; init; }
    public string? OptionsSource { get; init; }
    public string[]? DependsOn { get; init; }
    public bool AllowVariables { get; init; }
    public string? VisibleWhen { get; init; }
}

public sealed record SettingOption(string Value, string Label, string? Group = null, string? Icon = null);
public sealed record OptionsResult(IReadOnlyList<SettingOption> Options, string? Error = null);

public interface IOptionsSource
{
    Task<OptionsResult> GetOptionsAsync(string sourceId, JsonObject currentValues, CancellationToken cancellationToken);
}

public interface IPluginSettingsPage
{
    IReadOnlyList<SettingField> Fields { get; }
    JsonObject Load();
    void Save(JsonObject values);
}
```

Her seçeneğin ne yaptığı için [Ayar sayfaları](/tr/guides/settings-pages) sayfasına bakın.

## Değişkenler ve durum

```csharp
public interface IVariableStore
{
    void Set(string name, object? value);
    object? Get(string name);
    void Remove(string name);
}

public interface IVariableProvider
{
    Task RunAsync(IVariableStore store, CancellationToken cancellationToken);
}

public sealed record VariableInfo(string Name, string Description, string Example, string Category);

public interface IVariableCatalogSource
{
    IEnumerable<VariableInfo> Describe();
}

public enum StatusLevel { Idle, Ok, Busy, Warning, Error }

public interface IPluginStatusItem
{
    void Update(string text, StatusLevel level, string? icon = null, string? tooltip = null);
}
```

`RunAsync`, eklenti yüklü olduğu sürece çalışır ve yalnızca belirteç (token) iptal edildiğinde dönmelidir. Hata fırlatırsa sunucu bunu günlüğe yazar ve sağlayıcıyı 5 saniye sonra yeniden başlatır; normal biçimde dönen bir sağlayıcı yeniden başlatılmaz. Mevcut değere eşit bir değer `Set` tarafından yok sayılır. Bir eklentinin ayarladığı her değişken, eklenti kaldırıldığında silinir. Bkz. [Eğitim 3](/tr/tutorials/live-data).

## Simge paketleri

```csharp
public interface IIconPackSource
{
    string Id { get; }
    string DisplayName { get; }
    IReadOnlyList<string> IconNames { get; }
    string? GetIconSvg(string name);
}
```

Bkz. [Simge paketleri](/tr/guides/icon-packs).

## Sunucunun uyguladığı hizmetler

`IInputService` (`SendKeyCombo(KeyCombo)`, `TypeText(string)`) ve `IAudioService` (`GetMasterVolume`, `SetMasterVolume`, `GetMuted`,
`SetMuted`), sunucunun kendi aksiyonları için SDK assembly'sinin parçasıdır. Bir eklentiye bunlar bugün `IPluginHost` üzerinden verilmez.

## Manifest türleri

`PluginManifest` (`Id`, `Name`, `Version`, `MacroGrid`, ayrıca eski `SdkVersion` ve `MinServerVersion`, `Entry`, `Kind`, `Permissions` alanlarına sahip bir kayıt) ve
`PluginKind` (`Csharp`, `Js`), [`plugin.json`](/tr/reference/manifest) dosyasını yansıtır.

## Serbest bırakma (Disposal)

Eklenti örneğiniz veya kaydettiğiniz herhangi bir şey `IDisposable` ya da `IAsyncDisposable` uyguluyorsa, sunucu kaldırma sırasında, `RunAsync` belirtecinizi iptal edip en fazla 5 saniye bekledikten sonra onu serbest bırakır. Bkz. [yaşam döngüsü](/tr/basics/).
