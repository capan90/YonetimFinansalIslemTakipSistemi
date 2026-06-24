# Yayın Kontrol Listesi

Her yeni sürümü yayınlamadan önce bu listeyi sırayla uygulayın.

---

## 1. Build

- [ ] `dotnet build` hatasız tamamlanıyor
- [ ] Uyarı sayısı önceki sürüme göre artmadı
- [ ] NuGet paketleri güncel ve güvenlik açığı yok

```powershell
dotnet build src\YonetimFinansalIslemTakipSistemi.sln -c Release
```

---

## 2. Test

- [ ] `dotnet test` tüm testler geçiyor

```powershell
dotnet test src\YonetimFinansalIslemTakipSistemi.sln
```

- [ ] Yeni özellik veya değişiklik için test yazıldı (uygulanabiliyorsa)

---

## 3. Sürüm Numarası Artırma

- [ ] `YonetimFinansalIslemTakipSistemi.UI.csproj` içinde `<Version>` güncellendi
- [ ] Sürüm formatı: `MAJOR.MINOR.PATCH.BUILD` (örnek: `1.2.0.0`)

```xml
<Version>1.2.0.0</Version>
<AssemblyVersion>1.2.0.0</AssemblyVersion>
<FileVersion>1.2.0.0</FileVersion>
```

- [ ] Sürüm numarası `git tag` ile etiketlendi (yayın sonrası)

---

## 4. Backup

- [ ] Üretim veritabanı backup'ı alındı

```powershell
# Üretim sunucusundan veya bağlantı bilgileriyle:
$env:YONETIM_DB_CONNECTION = "Host=prod-server;..."
.\scripts\Backup-Database.ps1 -BackupDirectory "D:\Backups\pre-release"
```

- [ ] Backup dosyası güvenli bir konuma kopyalandı (ağ sürücüsü veya harici depolama)
- [ ] Backup boyutu makul görünüyor (sıfır byte değil)

---

## 5. Migration

Migration yoksa bu bölümü atlayın.

- [ ] Migration var mı? (`git diff main --name-only | grep Migrations`)
- [ ] Migration `Backup-And-Migrate.ps1` ile uygulandı

```powershell
$env:YONETIM_DB_CONNECTION = "Host=prod-server;..."
.\scripts\Backup-And-Migrate.ps1 -BackupDirectory "D:\Backups\pre-migration"
```

- [ ] Migration sonrası uygulama sağlıklı başlıyor
- [ ] Kritik tablolarda satır sayısı beklendiği gibi

---

## 6. ClickOnce Yayını

- [ ] Visual Studio'da `Release` yapılandırması seçili
- [ ] `Yayımla` sihirbazı ile publish edildi
- [ ] Publish klasörü sunucuya kopyalandı / yüklemesi tamamlandı
- [ ] `publish.htm` veya `setup.exe` erişilebilir durumda

> ClickOnce publish sadece Visual Studio üzerinden yapılır.
> Ayrıntılar için `docs/update-flow.md` dosyasına bakın.

---

## 7. Smoke Test

Yayın sonrası hızlı doğrulama:

- [ ] Uygulama başlıyor ve giriş ekranı açılıyor
- [ ] Admin hesabıyla giriş yapılabiliyor
- [ ] İşlem listesi yükleniyor (boş olsa da hata yok)
- [ ] Yeni işlem ekleniyor ve kaydediliyor
- [ ] Bakiye ekranı hatasız gösteriliyor
- [ ] Rapor oluşturuluyor (PDF veya Excel)
- [ ] Güncelleme kontrolü çalışıyor ("Güncelleme yok" veya yeni sürüm bulunuyor)
- [ ] Log dosyası oluşturuldu: `<kurulum-dizini>\logs\`

---

## 8. Güncelleme Testi

- [ ] Önceki sürümü yüklü bir makinede güncelleme yapıldı
- [ ] Güncelleme başlatma ekranı göründü
- [ ] Güncelleme sonrası yeni sürüm numarası doğrulandı (Hakkında menüsü veya başlık)

---

## 9. Rollback Planı

Yayın sonrası sorun çıkarsa:

| Durum | Eylem |
|-------|-------|
| Migration sorunlu | `.\scripts\Restore-Database.ps1` ile geri dön |
| Uygulama başlamıyor | Log kontrol et → önceki ClickOnce paketini sunucuya geri yükle |
| Veri kaybı | Pre-release backup'tan restore et |

- [ ] Rollback için backup'ın erişilebilir olduğu doğrulandı
- [ ] Sorun çözüme kavuşana kadar kullanıcılar bilgilendirildi

---

## Yayın Özeti (doldurulacak)

| Alan | Değer |
|------|-------|
| Sürüm | |
| Yayın tarihi | |
| Migration uygulandı mı | |
| Backup konumu | |
| Onaylayan | |
| Notlar | |
