# Enterprise Installer (Sprint 14.7)

Canlı kullanıcının **tek adımda** (Kurulum.bat çalıştırarak) uygulamayı kurabilmesi
için hazırlanmış kurumsal kurulum deneyimi. Mevcut **ClickOnce publish** altyapısını
kullanır; onu değiştirmez.

| | |
|---|---|
| **Kullanıcı çalıştırır** | `Kurulum.bat` |
| **Kurulum motoru** | `Install-Yonetim.ps1` (PowerShell 5.1) |
| **Kaynak (repo)** | [`tools/Installer/`](../../tools/Installer/) |
| **Kurulum share (dağıtım)** | `\\10.0.0.169\YonetimKurulum` |
| **ClickOnce publish share** | `\\10.0.0.169\YonetimPublish` |
| **Manifest** | `YonetimFinansalIslemTakipSistemi.UI.application` |
| **Log** | `%LOCALAPPDATA%\Yonetim\InstallerLogs\install-<tarih>.log` |
| **Desteklenen OS** | Windows 10 / 11 |

## Kurulum akışı

Kullanıcı `\\10.0.0.169\YonetimKurulum\Kurulum.bat` dosyasını çalıştırır. `Kurulum.bat`,
yanındaki `Install-Yonetim.ps1`'i (PowerShell) başlatır ve şu adımları sırayla yürütür:

```
  ✔ Ortam uygun (Windows 11)
  ✔ .NET Desktop Runtime 9.x bulundu
  ✔ Sunucu bağlantısı tamam
  ✔ Kurulum paketi doğrulandı
  ✔ Kurulum başlatıldı (ekrandaki adımları onaylayın)
  ✔ Kurulum tamamlandı.
```

1. **Ortam kontrolü** — Windows 10/11 mi? (`Win32_OperatingSystem`; major < 10 → durur).
2. **.NET Desktop Runtime kontrolü** — **hem** `Microsoft.NETCore.App 9.x` **hem**
   `Microsoft.WindowsDesktop.App 9.x` **(x64)** var mı? Eksikse **otomatik kurulur** (aşağıda).
3. **Sunucu bağlantısı** — `\\10.0.0.169\YonetimPublish` erişilebilir mi?
4. **Kurulum paketi + güncelleme** — ClickOnce `.application` manifesti **ve** `version.json`
   sunucuda var mı? (ikisi de yoksa → durur; sadece `version.json` yoksa → uyarı, devam).
5. **Kurulum** — runtime kesin doğrulandıktan **sonra** manifest başlatılır
   (`Start-Process <manifest>`), ClickOnce kurulum akışı devreye girer; uygulama kurulur ve başlar.
6. **Tamamlandı** — başarı mesajı; istenirse (`-CreateDesktopShortcut`) masaüstü kısayolu.

## Runtime kontrolü

WPF uygulaması için **iki** bileşen gereklidir ve **x64** olmalıdır:
`Microsoft.NETCore.App 9.x` + `Microsoft.WindowsDesktop.App 9.x`.

**Tespit iki kaynaktan** yapılır (yalnızca `dotnet --list-runtimes`'a güvenilmez):
1. `dotnet --list-runtimes` çıktısı (x86 yolları **hariç**),
2. dosya sistemi: `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\9.*` ve
   `...\Microsoft.WindowsDesktop.App\9.*`.

**Eksikse otomatik kurulum** (kullanıcı elle sayfa açmak zorunda kalmaz — bu **son** seçenektir):
1. `-RuntimeInstallerPath` parametresi verildiyse o kullanılır.
2. Kurulum klasöründe `windowsdesktop-runtime-9*-win-x64.exe` varsa o kullanılır.
3. İkisi de yoksa resmi **aka.ms doğrudan** bağlantısından indirilir:
   `https://aka.ms/dotnet/9.0/windowsdesktop-runtime-win-x64.exe`.
4. **Sessiz kurulur:** `installer.exe /install /quiet /norestart` (yönetici izni istenir).
5. Kurulumdan **sonra runtime'lar tekrar doğrulanır**.
6. Hâlâ eksikse **son çare** olarak resmi indirme sayfası açılıp kullanıcı yönlendirilir ve
   açıklayıcı hata verilir.

Bu sıralama sayesinde ClickOnce başlatılmadan runtime **kesin** doğrulanır; uygulama
**"You must install or update .NET"** hatası vermez.

> **Öneri:** Ağı kısıtlı/internetsiz istemcilerde `windowsdesktop-runtime-9.x.x-win-x64.exe`
> dosyasını Kurulum share'ine (`Install-Yonetim.ps1` ile aynı klasöre) koyun; installer
> indirmeye çalışmadan onu kullanır.

## Güncelleme yolu (version.json) — uygulama tarafı

Manuel **"Güncellemeleri Denetle"** akışı `version.json`'ı ClickOnce ProviderURL ile **aynı**
UNC'den okumalıdır. Yol önceliği (uygulama içinde `DeploymentSettings`):

1. `YONETIM_UPDATE_PATH` ortam değişkeni,
2. `appsettings.json` → `Deployment:UpdatePath`,
3. üretim varsayılanı `\\10.0.0.169\YonetimPublish\`.

> **Bugfix (14.7):** Eski varsayılan `\\localhost\YonetimPublish\` idi; production istemcisinde
> `YONETIM_UPDATE_PATH` set olmadığından manuel güncelleme kontrolü **"sunucuya erişilemedi"**
> veriyordu. Varsayılan artık ClickOnce ProviderURL ile aynı (`\\10.0.0.169\YonetimPublish\`)
> ve `appsettings.json` `Deployment:UpdatePath` ile yönetilebilir. Yerel test için
> `appsettings.Development.json` `\\localhost\YonetimPublish\` kullanır.

## Log dosyaları

- Konum: `%LOCALAPPDATA%\Yonetim\InstallerLogs\install-<yyyy-MM-dd_HH-mm-ss>.log`
- **Kullanıcı ekranda teknik hata görmez**; sadece açıklayıcı mesaj + log yolu görür.
  Destek için kullanıcıdan bu log dosyası istenir.
- Loga yazılanlar (tanılama için):
  - Windows sürümü (Caption + version + mimari)
  - `dotnet --list-runtimes` tam çıktısı
  - `Microsoft.NETCore.App 9.x (x64)` bulundu mu?
  - `Microsoft.WindowsDesktop.App 9.x (x64)` bulundu mu?
  - Runtime installer kaynağı (parametre / yerel klasör / aka.ms indirme)
  - Runtime kurulum çıkış kodu
  - Publish manifest erişim sonucu (yol + true/false)
  - `version.json` erişim sonucu (yol + true/false)

## Hata senaryoları (kullanıcıya gösterilen mesaj)

| Durum | Kullanıcı mesajı (özet) |
|-------|--------------------------|
| Windows 10'dan eski | "Bu uygulama Windows 10 veya Windows 11 gerektirir." |
| Runtime yok, yerel installer yok | "Gerekli .NET bileşeni eksik. Açılan sayfadan kurup tekrar başlatın." |
| Runtime kurulumu başarısız | "Gerekli bileşen kurulamadı. Yönetici izniyle tekrar deneyin." |
| Share erişilemez | "Kurulum sunucusuna ulaşılamadı. Şirket ağınıza bağlı olun ve tekrar deneyin." |
| Manifest yok | "Kurulum paketi sunucuda bulunamadı. BT ekibine başvurun." |
| ClickOnce başlatılamadı | "Uygulama kurulumu başlatılamadı. Tekrar deneyin veya BT'ye başvurun." |

Tüm hatalarda çıkış kodu **1**; başarıda **0**. `Kurulum.bat` başarısızlıkta pencereyi
açık tutar (`pause`).

## Ağ paylaşımı

- **Kurulum share** (`\\10.0.0.169\YonetimKurulum`): `Kurulum.bat`, `Install-Yonetim.ps1`
  ve (opsiyonel) runtime installer burada bulunur. Kullanıcıya **Read** yeterlidir.
- **Publish share** (`\\10.0.0.169\YonetimPublish`): ClickOnce manifest ve `Application Files`
  burada; `Publish-Production.ps1` buraya yayın yapar.
- Her iki share de kullanıcı ağından erişilebilir olmalıdır.

> **Bugfix (14.7-2):** `Kurulum.bat`, kurulum motoruna sunucu yolunu **açıkça**
> `-ShareRoot "\\10.0.0.169\YonetimPublish"` olarak geçirir (asla `pushd`/`%~dp0`/`%CD%`
> üzerinden türetilmez). `Install-Yonetim.ps1` ShareRoot'u doğrular; boş, `\`, `\\` veya
> yalnızca kök/sürücü ise **"Kurulum sunucusu yolu geçersiz."** hatası verir ve loga
> `ShareRoot=...` satırını net yazar. Bu, sahada görülen `ShareRoot=\` hatasını giderir.

## Güncelleme mantığı

- Kurulum ClickOnce kullandığından **güncelleme otomatiktir**: kullanıcı uygulamayı her
  açtığında ClickOnce, publish share'deki manifesti kontrol eder ve yeni sürüm varsa
  onay isteyerek günceller (bkz. [../02-Architecture/ClickOnce.md](../02-Architecture/ClickOnce.md),
  [../update-flow.md](../update-flow.md)).
- `Kurulum.bat` yalnızca **ilk kurulum** (ve gerekli ön koşullar) içindir; sonraki
  güncellemeler için tekrar çalıştırmaya gerek yoktur.
- Yeni sürüm `Publish-Production.ps1` ile yayınlanır; installer tarafında değişiklik gerekmez.

## Kullanıcı destek adımları

1. `\\10.0.0.169\YonetimKurulum\Kurulum.bat` çift tıklanır.
2. Ekrandaki adımlar (✔) izlenir; ClickOnce onay ekranı gelirse onaylanır.
3. Uygulama Başlat menüsünde **"Yonetim Finansal Islem Takip Sistemi"** olarak görünür.
4. Sorun olursa: `%LOCALAPPDATA%\Yonetim\InstallerLogs` altındaki **son log** BT'ye iletilir.
5. Sık karşılaşılanlar:
   - "Sunucuya ulaşılamadı" → VPN/ağ bağlantısını kontrol edin.
   - ".NET eksik" → açılan sayfadan Desktop Runtime (x64) kurun, tekrar çalıştırın.
   - ClickOnce güncelleme uyarısı → normaldir; onaylayın.

## Sunucuda yapılacak işlemler (BT)

```powershell
# 1) Kurulum share'i olustur (bir kez, yonetici):
New-Item -ItemType Directory -Force "C:\Apps\Yonetim\Kurulum\Installer" | Out-Null
New-SmbShare -Name "YonetimKurulum" -Path "C:\Apps\Yonetim\Kurulum\Installer" -ReadAccess "Everyone"

# 2) Installer dosyalarini kopyala:
Copy-Item ".\tools\Installer\Kurulum.bat"        "C:\Apps\Yonetim\Kurulum\Installer\" -Force
Copy-Item ".\tools\Installer\Install-Yonetim.ps1" "C:\Apps\Yonetim\Kurulum\Installer\" -Force
# (Opsiyonel) cevrimdisi runtime:
# Copy-Item ".\windowsdesktop-runtime-9.x.x-win-x64.exe" "C:\Apps\Yonetim\Kurulum\Installer\" -Force

# 3) Uygulamayi yayinla (mevcut akis):
.\Publish-Production.ps1 -Version "1.0.0.x"
```

> Installer, publish share (`YonetimPublish`) ve manifest adını `Publish-ClickOnce.ps1` ile
> aynı değerlerle bekler; ayrı bir yapılandırma gerekmez. Farklı bir sunucu/ad için
> `Install-Yonetim.ps1` parametreleri (`-ShareRoot`, `-ManifestName`) kullanılabilir.
