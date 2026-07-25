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

### C) Kullanıcı tarafı — doğru kısayolu sabitle

Kullanıcı **çalışan uygulamayı** değil, **sabit yoldaki kısayolu** sabitlemelidir.

`tools/Installer/Create-PinnableShortcut.ps1` masaüstünde, ClickOnce başlatıcısına
(`.appref-ms`) işaret eden ve uygulamayla aynı AUMID'i taşıyan bir kısayol oluşturur.
Kullanıcı bir kez bu kısayola **sağ tık → Görev çubuğuna sabitle** yapar; sonraki tüm
güncellemelerde pin çalışmaya devam eder.

```powershell
# Kurulumdan ve uygulama en az bir kez açıldıktan SONRA, kullanıcı oturumunda:
powershell -ExecutionPolicy Bypass -File .\Create-PinnableShortcut.ps1
```

## Doğrulama (gerçek istemcide, manuel)

1. İstemciye üretim (imzalı) sürümü kur, uygulamayı bir kez aç.
2. `Create-PinnableShortcut.ps1` çalıştır → masaüstü kısayolunu görev çubuğuna sabitle.
3. Sürümü artırıp yeniden yayınla; istemcide **Güncellemeleri Denetle** ile güncelle.
4. **Beklenen:** görev çubuğundaki pin aynı kalır; güncel sürüm bu pin üzerinden açılır,
   ayrı/ikinci bir simge oluşmaz.

> Not: A ve B kod/scriptte doğrulandı (derleme + parse). Görev çubuğu davranışının kendisi
> yalnızca gerçek ClickOnce kurulumu + manuel sabitleme ile teyit edilebilir.
