# Görev Çubuğu Sabitleme (Taskbar Pin) — ClickOnce

## Belirti

Kullanıcı **Yardım → Güncellemeleri Denetle** ile yeni sürümü indirdiğinde, uygulama
"yeni bir uygulama" gibi davranıyor: görev çubuğuna sabitlenen (pinlenen) eski simge
kırılıyor, kullanıcı eskiyi kaldırıp yeniyi tekrar sabitlemek zorunda kalıyor.

## Kök neden

İki ayrı mekanizma bu belirtiyi üretir:

### 1) Versiyonlu exe yolu (asıl sebep)

ClickOnce uygulaması **her sürümde farklı bir klasörden** çalışır:

```
%LocalAppData%\Apps\2.0\<rastgele>\<rastgele>\...UI..<sürüm>\...UI.exe
```

Bu yol her güncellemede değişir. Kullanıcı **çalışan uygulamayı** görev çubuğuna
sabitlerse, pin bu değişken yola bağlanır; güncelleme sonrası yol kaybolunca pin ölür ve
yeni sürüm ayrı bir simge olarak açılır.

### 2) Kimlik çatallanması (imzasız yayın)

ClickOnce kimliği `PublicKeyToken` içerir:

- **İmzalı** paket → sertifikanın token'ı (ör. `7ec061a7563a0fdd`)
- **İmzasız** paket → `0000000000000000`

Bu ikisi **farklı uygulamadır**. Bir sürüm imzalı, diğeri imzasız yayınlanırsa (ya da
farklı `ProviderURL` ile), kullanıcıda mevcut kurulum güncellenmez; **ayrı bir uygulama**
kurulur (Başlat Menüsü'nde `... - 1.appref-ms` kopyası belirir, pin kırılır).

## Çözüm

### A) Uygulama tarafı — sabit AppUserModelID (yapıldı)

`App.OnStartup` içinde, ilk pencereden önce sabit bir **AppUserModelID (AUMID)** bildirilir:

```csharp
private const string AppUserModelId = "ErdemSoftTekstil.YonetimFinansalIslemTakip";
SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
```

Böylece görev çubuğu, pencereyi **değişken exe yoluyla değil sabit kimlikle** gruplar.
Pin artık yol-tabanlı değil kimlik-tabanlı olur → güncellemeler pin'i kırmaz.

> AUMID değeri değişirse pin bağı kopar. Bu değeri **asla değiştirmeyin**; değiştirmek
> zorunda kalırsanız `Create-PinnableShortcut.ps1` içindeki `$AppUserModelId` ile birlikte değiştirin.

### B) Yayın tarafı — Production her zaman imzalı

`Publish-ClickOnce.ps1`, `-Environment Production` ile `-Sign $false` kombinasyonunu artık
**reddeder**. `Publish-Production.ps1` zaten `-Sign $true` verir. Böylece üretim kimliği
(sertifika + ProviderURL) her sürümde aynı kalır → çatallanma olmaz.

**Kural:** Production paketleri daima aynı sertifika ile ve aynı `ProviderURL`
(`\\10.0.0.169\YonetimPublish\...`) ile yayınlanır. Localhost/imzasız test paketleri
kullanıcı makinelerine **asla** kurulmaz.

### C) Kullanıcı tarafı — doğru şeyi sabitle

**Birincil mekanizma A maddesidir (uygulama tarafı AUMID).** Uygulama sabit AUMID
bildirdiği için, kullanıcı **çalışan uygulamayı** görev çubuğuna sabitlediğinde Windows
pin'i sürümlü exe yoluna değil bu sabit kimliğe bağlar; güncelleme sonrası uygulama aynı
AUMID ile açılır ve pin korunur.

Ek olarak `tools/Installer/Create-PinnableShortcut.ps1`, masaüstünde ClickOnce
başlatıcısına (`.appref-ms`) işaret eden **sabit yollu** bir kısayol oluşturur (uygulama
ikonuyla). Bu kısayol her zaman güncel sürümü açar ve sabitlenebilir.

```powershell
# Kurulumdan ve uygulama en az bir kez açıldıktan SONRA, kullanıcı oturumunda:
powershell -ExecutionPolicy Bypass -File .\Create-PinnableShortcut.ps1
```

> **Kısayol AUMID damgası — en iyi çaba:** Script, kısayola uygulamayla aynı AUMID'i
> yazmayı dener ve **geri okuyup doğrular**. Bazı Windows sürümlerinde `.appref-ms` hedefli
> `.lnk` üzerine AUMID kalıcı yazılamaz; script bunu açıkça bildirir ("doğrulanamadı").
> Bu durumda kısayol yine çalışır, ancak gruplama garantisi için birincil mekanizma
> (uygulama tarafı AUMID) geçerlidir — kullanıcı çalışan uygulamayı sabitleyebilir.

## Doğrulama (gerçek istemcide, manuel — bu adım zorunlu)

1. İstemciye üretim (imzalı) sürümü kur, uygulamayı bir kez aç.
2. Görev çubuğuna sabitle: ya **çalışan uygulamayı** (A mekanizması) ya da masaüstü kısayolunu.
3. Sürümü artırıp yeniden yayınla; istemcide **Güncellemeleri Denetle** ile güncelle.
4. **Beklenen:** görev çubuğundaki pin aynı kalır; güncel sürüm bu pin üzerinden açılır,
   ayrı/ikinci bir simge oluşmaz.
5. **Eğer pin hâlâ kırılıyorsa** (uygulama kapalıyken pin'e tıklayınca açılmıyorsa): sonraki
   adım, ana pencereye `PKEY_AppUserModel_RelaunchCommand` ayarlamaktır (dfshim/.appref-ms
   üzerinden yeniden başlatma) — bu, gerçek istemcideki gözleme göre eklenecektir.

> Not: A (uygulama AUMID) ve B (imza kapısı) kod/scriptte doğrulandı; açılışta AUMID
> hatasız atanıyor. Görev çubuğu davranışının kendisi yalnızca gerçek ClickOnce kurulumu +
> manuel sabitleme + güncelleme ile teyit edilebilir. Kısayol AUMID damgası ortama bağlıdır
> (yukarıdaki not).
