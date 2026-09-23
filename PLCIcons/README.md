# PLC İkonları

Macro Station için statik bir ikon paketi eklentisi. Bağlantı, ayar ya da aksiyon içermez —
`IPluginHost.RegisterIconPack` üzerinden 27 adet PLC/ladder-logic ikonunu (bobin, zamanlayıcılar,
karşılaştırma ve aritmetik blokları) editörün ikon seçiciye ekler.

## Kurulum

1. Bu klasörü derleyin: `src/` içinde `dotnet build` (Debug) veya `dotnet build -c Release`.
2. Editördeki **Eklentiler** penceresinden **"Klasörden Yükle…"** ile `src/bin/Debug/net10.0/` (ya da
   `Release/net10.0/`) klasörünü seçin — `plugin.json` derleme çıktısına otomatik kopyalanır, DLL ile
   yan yana durur.
3. Sunucuyu yeniden başlatın (yeni yüklenen bir plugin ancak açılışta taranır).

## Sağladığı ikonlar

İkonlar `src/icons/*.svg` altında, DLL içine gömülü olarak taşınır (ayrı dosya okuma yok). İkon seçicide
**"PLC İkonları"** kategorisi olarak görünür:

`add`, `calculate`, `close-branch`, `coil`, `convert`, `divide`, `empty-block`, `equal`, `f-trig`,
`greater`, `greater-equal`, `lesser`, `lesser-equal`, `move`, `multiply`, `n`, `nc`, `no`,
`not-equal`, `open-branch`, `p`, `r-trig`, `reset-coil`, `set-coil`, `subtract`, `timer-convert`,
`tof-timer`, `ton-timer`, `tp-timer`.

Her SVG `stroke="currentColor"` kullanır; editör, ikonu seçildiği rengi taşıyan bir `data:` URI'ye
çevirirken kök `<svg>` etiketine `color="…"` ekleyerek bunu boyar (ayrı bir CSS bağlamı olmadığından
`currentColor` aksi halde siyaha düşerdi).

## Yeni ikon eklemek

`src/icons/` altına yeni bir `.svg` koyup `PlcIconPack.Names` dizisine dosya adını (uzantısız) eklemek
yeterli — `.csproj`'daki `<EmbeddedResource Include="icons\*.svg" />` glob'u otomatik yakalar.
