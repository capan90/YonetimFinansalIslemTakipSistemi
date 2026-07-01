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
2. **.NET Desktop Runtime kontrolü** — `dotnet --list-runtimes` içinde
   `Microsoft.WindowsDesktop.App 9.x` var mı? (uygulama `net9.0-windows`).
3. **Sunucu bağlantısı** — `\\10.0.0.169\YonetimPublish` erişilebilir mi?
4. **Kurulum paketi** — ClickOnce `.application` manifesti sunucuda var mı?
5. **Kurulum** — manifest başlatılır (`Start-Process <manifest>`), ClickOnce kurulum/güncelleme
   akışı devreye girer; uygulama kurulur ve otomatik başlar.
6. **Tamamlandı** — başarı mesajı; istenirse (`-CreateDesktopShortcut`) masaüstü kısayolu.

## Runtime kontrolü

- **Varsa:** sürüm gösterilir, devam edilir.
- **Yoksa** iki yol:
  1. **Yerel installer:** Kurulum klasöründe `windowsdesktop-runtime-*.exe` varsa (ya da
     `-RuntimeInstallerPath` verilirse) **sessiz** kurulur (`/install /quiet /norestart`,
     yönetici izni ister), sonra yeniden doğrulanır.
  2. **Yerel installer yoksa:** resmi indirme sayfası açılır ve kullanıcı yönlendirilir:
     `https://dotnet.microsoft.com/download/dotnet/9.0` → **Desktop Runtime (x64)**.
     Kurup kurulumu tekrar başlatması istenir.

> **Öneri:** Ağı kısıtlı ortamlarda `windowsdesktop-runtime-9.x.x-win-x64.exe` dosyasını
> Kurulum share'ine (`Install-Yonetim.ps1` ile aynı klasöre) koyun; installer onu otomatik bulur.

## Log dosyaları

- Konum: `%LOCALAPPDATA%\Yonetim\InstallerLogs\install-<yyyy-MM-dd_HH-mm-ss>.log`
- Her adım, uyarı ve hata teknik ayrıntısıyla loglanır.
- **Kullanıcı ekranda teknik hata görmez**; sadece açıklayıcı mesaj + log yolu görür.
  Destek için kullanıcıdan bu log dosyası istenir.

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
