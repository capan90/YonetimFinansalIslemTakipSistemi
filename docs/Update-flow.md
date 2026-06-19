# Güncelleme Akışı

## Genel Bakış

V1: ClickOnce + UNC ağ klasörü dağıtımı.

Startup güncelleme kontrolü → ClickOnce Foreground manifest (sıfır kod).
Manuel güncelleme kontrolü → UNC'deki `version.json` okunur, `IUpdateService` üzerinden yönetilir.

`System.Deployment.Application` .NET 9'da mevcut değildir ve kullanılmaz.

---

## Klasör Yapısı

```
\\SUNUCU\uygulamalar\yonetim\
├── YonetimFinansalIslemTakipSistemi.application   ← ClickOnce giriş noktası
├── setup.exe
├── version.json                                    ← Manuel kontrol için
└── Application Files\
    └── YonetimFinansalIslemTakipSistemi_1_0_0_x\
```

---

## Startup Güncelleme Kontrolü

Kod yazılmaz.

`pubxml` içindeki `<UpdateMode>Foreground</UpdateMode>` ayarı ClickOnce launcher'a uygulamanın
açılmadan önce UNC klasöründeki manifest ile kendi manifestini karşılaştırmasını söyler.
Yeni sürüm varsa ClickOnce kendi dialogunu gösterir; `<UpdateRequired>false</UpdateRequired>`
olduğundan kullanıcı güncellemeyi erteleyebilir.

---

## Manuel Güncelleme Kontrolü

`Yardım → Güncellemeleri Denetle` menü öğesi.

Akış:

```
ClickOnce kurulumu değilse (debug/doğrudan exe)
  → ShowInfo: "Yalnızca kurulu sürümde kullanılabilir."

ClickOnce kurulumuysa
  → File.ReadAllTextAsync(\\SUNUCU\...\version.json)
  → Version.Parse → Assembly.Version ile karşılaştır
    Güncel          → ShowInfo
    Erişilemiyor    → ShowWarning
    Güncelleme var  → ShowConfirmation (güncellemek ister misiniz?)
                        → ShowConfirmation (uygulama kapatılacak, devam?)
                            → Process.Start(.application) + Application.Shutdown()
```

### ClickOnce Ortam Tespiti

```csharp
AppContext.BaseDirectory.StartsWith(
    Path.Combine(Environment.SpecialFolder.LocalApplicationData, "Apps"),
    StringComparison.OrdinalIgnoreCase)
```

ClickOnce uygulamayı `%LOCALAPPDATA%\Apps\2.0\...` altına kurar.
Debug ve doğrudan exe başlatmasında `BaseDirectory` bu konumun dışındadır.

`AppDomain.SetupInformation.ActivationArguments` .NET Framework'e özgüdür; .NET 9'da mevcut değildir.

---

## Sertifika

V1: Self-signed sertifika.

```powershell
# Geliştirici makinesinde bir kez
$cert = New-SelfSignedCertificate -Subject "CN=YonetimApp" `
    -CertStoreLocation "Cert:\CurrentUser\My" -Type CodeSigningCert -NotAfter (Get-Date).AddYears(5)
Export-PfxCertificate -Cert $cert -FilePath "YonetimApp.pfx" -Password (Read-Host -AsSecureString)
Export-Certificate -Cert $cert -FilePath "YonetimApp.cer"
```

```powershell
# Her istemci makinede admin olarak bir kez
Import-Certificate -FilePath "YonetimApp.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

`ManifestCertificateThumbprint` pubxml'e girilir. `YonetimApp.pfx` git'e dahil edilmez.

---

## Yayın Sırası (Her Release)

```
1. Yeni EF migration varsa:
   dotnet ef database update --project src/Infrastructure --startup-project src/Infrastructure

2. csproj <AssemblyVersion> artır (örn. 1.0.0.0 → 1.0.0.1)
   pubxml  <PublishVersion>  ile senkron tut

3. .\Publish-ClickOnce.ps1 -Version "1.0.0.1"
   → dotnet publish (flat output) + dotnet-mage (manifest) + version.json UNC'ye yazılır

4. Smoke test: bir istemcide uygulamayı kapat ve yeniden aç
```

### Kritik Kural

Migration **publish öncesi** uygulanır. Uygulama startup'ta migration çalıştırmaz.
DevDataSeeder migration runner değildir.

### `dotnet publish /p:PublishProfile=ClickOnce` Neden Çalışmıyor?

.NET 9 CLI'de ClickOnce publish için `GenerateLauncher` görevi `Engine\Launcher.exe` şablon
dosyasına ihtiyaç duyar. Bu dosya Visual Studio kurulumunda bulunur; yalnızca .NET SDK kuruluysa
eksiktir. `Publish-ClickOnce.ps1` bu görevi `microsoft.dotnet.mage` aracıyla bypass eder.

---

## IUpdateService

`UI/Abstractions/IUpdateService.cs`

| Üye | Açıklama |
|-----|----------|
| `IsClickOnceDeployment` | `AppContext.BaseDirectory` → `%LOCALAPPDATA%\Apps\` kontrolü |
| `CheckForUpdateAsync()` | version.json okur, karşılaştırır |
| `LaunchInstaller()` | .application'ı shell ile açar |

Implementasyon: `UI/Services/UpdateService.cs`

UNC sabitleri (`VersionJsonPath`, `DeploymentFilePath`): gerçek sunucu adı netleşince güncellenir.

---

## Smoke Test

| # | Senaryo | Beklenen |
|---|---------|----------|
| 1 | Debug'da "Güncellemeleri Denetle" | Info: "Yalnızca kurulu sürümde kullanılabilir." |
| 2 | Kurulu sürüm, güncel | Info: "Uygulamanız güncel. Mevcut sürüm: v1.0.0.0" |
| 3 | Kurulu sürüm, güncelleme var, her iki onayda Hayır | Hiçbir şey olmaz |
| 4 | Kurulu sürüm, güncelleme var, her iki onayda Evet | .application açılır, uygulama kapanır |
| 5 | UNC erişilemiyor | Warning: "Güncelleme sunucusuna erişilemiyor." |
| 6 | Startup, yeni sürüm | ClickOnce dialog → Güncelle → yeni sürüm yüklenir |
| 7 | Startup, güncel | Dialog çıkmaz, uygulama normal açılır |
| 8 | Startup, ağ yok | UpdateRequired=false → ClickOnce atlar, uygulama açılır |
