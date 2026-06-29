# ClickOnce Dağıtım ve Güncelleme Sistemi

## Genel Bakış

V1 dağıtımı ClickOnce + UNC ağ klasörü üzerinden yapılır. Uygulama her çalışan bilgisayarına kurulur; güncellemeler UNC klasöründen otomatik veya manuel olarak alınır.

`System.Deployment.Application` .NET 9'da mevcut değildir. Bu nedenle ClickOnce API'si kullanılmaz; update akışı `version.json` okuma ve `.application` dosyasını shell ile açma üzerine kuruludur.

---

## Klasör Yapısı

```
\\SUNUCU\YonetimPublish\
├── YonetimFinansalIslemTakipSistemi.UI.application   ← Giriş noktası (kurulum)
├── version.json                                       ← Manuel güncelleme kontrolü
└── Application Files\
    └── YonetimFinansalIslemTakipSistemi.UI_1_0_0_21\
        ├── YonetimFinansalIslemTakipSistemi.UI.exe.manifest
        ├── YonetimFinansalIslemTakipSistemi.UI.exe
        └── ... (tüm uygulama dosyaları)
```

`setup.exe` üretilmez (`BootstrapperEnabled = false`). `.application` tek kurulum girişidir.

---

## Publish Süreci

`Publish-ClickOnce.ps1` scripti ile. Visual Studio gerekmez.

### Gereksinim

```powershell
dotnet tool install --global microsoft.dotnet.mage
```

### Temel Akış

```
[0/6] Publish klasörü temizle   → C:\Apps\Yonetim\Publish\* kaldır
[1/6] Build                     → dotnet publish -c Release -o $BuildOutput
[2/6] Launcher ekle             → dotnet-mage -AddLauncher
[3/6] AppFiles hazırla          → BuildOutput → Application Files\
[4/6] Application manifest      → dotnet-mage -New Application
[5/6] Deployment manifest       → dotnet-mage -New Deployment
[6/6] version.json yaz          → PublishDir\version.json
```

### Kritik Parametre: ASCII-Only İsimler

`dotnet-mage`'e Türkçe karakter içeren değer geçildiğinde PowerShell console encoding uyumsuzluğu nedeniyle manifest'e mojibake yazılır. Bu "Uygulama hatalı biçimlendirilmiş" hatasına neden olur.

```powershell
# YANLIŞ — mojibake riski
$AppName = "Yönetim Finansal İşlem Takip Sistemi"

# DOĞRU — ASCII-only zorunlu
$AppName = "Yonetim Finansal Islem Takip Sistemi"
```

### Kullanım

```powershell
# Lokal test
.\Publish-ClickOnce.ps1 -Version "1.0.0.21" -Sign $true

# Üretim (farklı UNC)
$env:YONETIM_UPDATE_PATH = "\\SUNUCU\YonetimPublish\"
.\Publish-ClickOnce.ps1 -Version "1.0.0.21" -Sign $true
```

---

## Version Yönetimi

Her sürümde `csproj`'daki `AssemblyVersion` artırılır:

```xml
<Version>1.0.0.21</Version>
<AssemblyVersion>1.0.0.21</AssemblyVersion>
<FileVersion>1.0.0.21</FileVersion>
```

**Kritik:** Aynı versiyonla tekrar publish yapılırsa ClickOnce güncelleme algılamaz.

---

## ProviderURL

`$env:YONETIM_UPDATE_PATH` set edilmişse o UNC kullanılır; set edilmemişse `\\localhost\YonetimPublish\` varsayılanı.

```powershell
$ProviderUrl = "${UncBase}YonetimFinansalIslemTakipSistemi.UI.application"
```

---

## version.json

Manuel güncelleme kontrolü için kullanılır.

```json
{ "version": "1.0.0.21" }
```

`IUpdateService.CheckForUpdateAsync()`:
1. UNC'deki `version.json` okunur.
2. `Version.Parse()` ile `Assembly.GetExecutingAssembly().GetName().Version` karşılaştırılır.
3. Yeni sürüm varsa kullanıcıya onay dialogs gösterilir.
4. Onay alınırsa `.application` shell ile açılır + `Application.Shutdown()`.

---

## Startup Güncelleme

`pubxml`'deki `<UpdateMode>Foreground</UpdateMode>` ayarı ile yönetilir. Uygulama açılmadan önce ClickOnce launcher UNC klasöründeki manifest ile karşılaştırır. Kod yazılmaz.

---

## İmzalama

V1: Self-signed sertifika.

```powershell
# Geliştirici makinesinde bir kez
$cert = New-SelfSignedCertificate -Subject "CN=YonetimApp" `
    -CertStoreLocation "Cert:\CurrentUser\My" -Type CodeSigningCert -NotAfter (Get-Date).AddYears(5)
```

```powershell
# Her istemci makinede admin olarak bir kez
Import-Certificate -FilePath "YonetimApp.cer" -CertStoreLocation "Cert:\LocalMachine\Root"
```

`CertThumb` publish scriptinde sabitlenmiştir. `YonetimApp.pfx` git'e dahil edilmez.

---

## SMB Share Kurulumu

```powershell
# Admin PowerShell'de bir kez
New-SmbShare -Name "YonetimPublish" -Path "C:\Apps\Yonetim\Publish" -FullAccess "Everyone"
```

---

## dotnet publish /p:PublishProfile=ClickOnce Neden Çalışmıyor?

.NET 9 CLI'de ClickOnce publish için `GenerateLauncher` MSBuild görevi `Engine\Launcher.exe` şablon dosyasına ihtiyaç duyar. Bu dosya Visual Studio kurulumunun bir parçasıdır; yalnızca .NET SDK kuruluysa eksiktir.

`Publish-ClickOnce.ps1` bu görevi `microsoft.dotnet.mage` ile bypass eder.

---

## IUpdateService

| Üye | Açıklama |
|-----|----------|
| `IsClickOnceDeployment` | `AppContext.BaseDirectory` → `%LOCALAPPDATA%\Apps\` kontrolü |
| `CheckForUpdateAsync()` | version.json okur, karşılaştırır |
| `LaunchInstaller()` | .application'ı shell ile açar |

ClickOnce ortam tespiti:
```csharp
AppContext.BaseDirectory.StartsWith(
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Apps"),
    StringComparison.OrdinalIgnoreCase)
```

---

## Release Süreci

1. EF migration varsa: `dotnet ef database update`
2. `csproj`'da `AssemblyVersion` artır
3. `.\Publish-ClickOnce.ps1 -Version "x.x.x.x" -Sign $true`
4. Smoke test: bir istemcide uygulamayı kapat ve yeniden aç

Bkz. [`docs/04-Development/GitFlow.md`](../04-Development/GitFlow.md)

---

## Bilinen Kısıtlar

### Masaüstü Kısayol İkonu — Won't Fix

ClickOnce masaüstü kısayolunun özel ikon göstermesi mümkün değildir.

| Nerede | Durum |
|--------|-------|
| EXE ikonu | ✅ Çalışıyor |
| Taskbar ikonu | ✅ Çalışıyor |
| Pencere (window) ikonu | ✅ Çalışıyor |
| Masaüstü kısayol ikonu | ❌ Won't Fix |

**Araştırma sonuçları:**
- `dotnet-mage -New Deployment -IconFile` → "can only be used with Application type" hatası
- `dotnet-mage -New Application -IconFile` → manifest'e mutlak yol yazılıyor → "hatalı biçimlendirilmiş"
- Deployment manifest XML post-processing (asmv2:iconFile) → runtime hatası: "Dağıtım bildirimi simge dosyasının belirtimini kabul etmez."

Tüm yaklaşımlar denendi; ClickOnce runtime bu özelliği bu sürümde desteklemiyor.

Bkz. [`docs/04-Development/LessonsLearned.md`](../04-Development/LessonsLearned.md)
