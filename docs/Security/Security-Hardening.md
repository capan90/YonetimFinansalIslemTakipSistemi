# Güvenlik Sıkılaştırma Rehberi (Production)

Bu belge, **Yönetim Finansal İşlem Takip Sistemi** üretim sunucusu için güvenlik
sıkılaştırma önerilerini içerir. Amaç, çalışan sistemi bozmadan saldırı yüzeyini
azaltmak ve PostgreSQL + ClickOnce publish altyapısını korumaktır.

Denetim için: [`SecurityAudit.ps1`](./SecurityAudit.ps1),
[`HealthCheck.ps1`](./HealthCheck.ps1), [`ServerInfo.ps1`](./ServerInfo.ps1).
İlgili ortam yapılandırması: [../Environment-Configuration.md](../Environment-Configuration.md).

---

## 1. Share Güvenliği (SMB)

Publish/kurulum paylaşımı: `YonetimPublish` → `C:\Apps\Yonetim\Publish`
(istemciler `\\10.0.0.169\YonetimPublish` üzerinden kurulum/güncelleme alır).

- **En az yetki:** İstemcilere yalnızca **Read** yeterlidir. `Everyone`'a **FullAccess** verilmemeli.
- Yayın (publish) yazma yetkisi yalnızca yayın hesabına (ör. `YONETIM\publisher`) tanınmalı.
- SMBv1 kapalı olmalı, SMB imzalama açık olmalı.

```powershell
# Everyone salt-okunur; yazma yalnizca publisher hesabinda
Grant-SmbShareAccess -Name YonetimPublish -AccountName "Everyone" -AccessRight Read -Force
Grant-SmbShareAccess -Name YonetimPublish -AccountName "DOMAIN\publisher" -AccessRight Change -Force
# SMBv1 kapat
Set-SmbServerConfiguration -EnableSMB1Protocol $false -Force
```

## 2. NTFS İzinleri

Share izinleri ile NTFS izinleri birlikte etkilidir; **en kısıtlayıcı olan** geçerlidir.

- `C:\Apps\Yonetim\Publish`: `Users`/`Everyone` → **Read & Execute**; yazma yayın hesabında.
- `C:\Apps\Yonetim\Backups`: yalnızca yönetici + yedek servisi. Yedekler hassas veri içerir → `Everyone` **kaldırılmalı**.
- `C:\Apps\Yonetim\Logs`: uygulama hesabı yazar; `Everyone` yazma **olmamalı** (log tahrifatını önler).
- İzin devralmayı (inheritance) kritik klasörlerde gözden geçirin.

```powershell
icacls "C:\Apps\Yonetim\Backups" /inheritance:r
icacls "C:\Apps\Yonetim\Backups" /grant:r "Administrators:(OI)(CI)F" "SYSTEM:(OI)(CI)F"
icacls "C:\Apps\Yonetim\Backups" /remove "Everyone"
```

## 3. Firewall

- PostgreSQL **5432** yalnızca güvenilen subnet'e/istemci IP aralığına açılmalı; `RemoteAddress=Any` **olmamalı**.
- Sunucu internete açıksa 5432 dışarıya **kapatılmalı** (yalnızca LAN).
- Gereksiz gelen kuralları devre dışı bırakın.

```powershell
New-NetFirewallRule -DisplayName "PostgreSQL 5432 (LAN)" -Direction Inbound `
  -Protocol TCP -LocalPort 5432 -Action Allow -RemoteAddress 10.0.0.0/24
```

> **Denetim notu:** PostgreSQL `0.0.0.0` üzerinde dinlese bile firewall `RemoteAddress`
> güvenli bir subnet'e kısıtlıysa `SecurityAudit.ps1` bunu **FAIL değil**, kabul edilebilir
> **WARNING** olarak raporlar. Asıl kritik olan portun dinlenmesi değil, firewall kısıtıdır.

## 4. PostgreSQL

- `pg_hba.conf`: uzak bağlantılar için `scram-sha-256`; `trust` **kullanılmamalı**.
- `postgresql.conf` `listen_addresses`: yalnızca gerekli arayüz (tüm arayüz `*` yerine sabit IP).
- Uygulama kullanıcısı (`yonetim_app`) **en az yetkili** olmalı; `SUPERUSER` verilmemeli.
- Güçlü parola; parola yalnızca env var / gitignore'lu `appsettings.Production.json` içinde tutulur (repoya girmez).
- Düzenli minor sürüm güncellemeleri uygulanmalı.

## 5. Backup

- Otomatik günlük yedek: `scripts\Backup-Database.ps1` bir **zamanlanmış görevle** çalıştırılmalı.
- Şifre komut satırına yazılmaz; `PGPASSWORD`/`YONETIM_DB_CONNECTION` env var kullanılır (script bunu uygular).
- Yedekler ayrı bir diske/konuma kopyalanmalı (3-2-1 kuralı); erişim kısıtlı olmalı.
- Restore tatbikatı periyodik yapılmalı (bkz. [../Disaster-Recovery-Plan.md](../Disaster-Recovery-Plan.md)).
- `HealthCheck.ps1` son yedeğin yaşını denetler (varsayılan eşik 24 saat).

## 6. Publish (ClickOnce)

- Yayın **imzalı** olmalı (`Publish-ClickOnce.ps1 -Sign $true`); imzasız yayın istemciye dağıtılmaz.
- `ProviderURL` sabit ve doğru UNC olmalı; manifest doğrulaması publish akışında yapılır.
- Publish klasörüne yazma yalnızca yayın hesabında; istemciler salt-okunur.
- Production yayını `Publish-Production.ps1` ile alınır: ortam `Production` gömülür,
  `appsettings.Production.json` pakete dahil edilir, `appsettings.Development.json` **hariç** tutulur.

## 7. Sertifika

- ClickOnce imzalama sertifikası `Cert:\CurrentUser\My` altında, parmak izi
  `0136460438B6DED7F20498C00F7D3AB4C1E1B203`.
- Özel anahtar **dışa aktarılamaz** olarak korunmalı, yedeği güvenli kasada tutulmalı.
- Süre dolmadan **en az 30 gün önce** yenilenmeli (`SecurityAudit.ps1` uyarır).
- Sertifika yenilenirse `Publish-ClickOnce.ps1` içindeki `$CertThumb` güncellenmeli.

**Denetim modu (önemli):** Sertifika yalnızca **publish/imzalama makinesinde** zorunludur.
`SecurityAudit.ps1` bu nedenle iki modda çalışır:

- **Server modu** (varsayılan): sertifika yoksa **WARNING** — DB/uygulama sunucusunda
  imzalama sertifikası bulunmaması normaldir.
- **PublishMachine modu** (`-PublishMachine`): sertifika yok/süresi dolmuşsa **FAIL** —
  bu makineden imzalı yayın alınamayacağı için kritiktir.

```powershell
# Publish makinesinde imzalama sertifikasini zorunlu denetle:
.\docs\Security\SecurityAudit.ps1 -PublishMachine
```

## 8. Environment Variables

Secret değerler dosyaya değil **ortam değişkenlerine** yazılır (bkz. [../Environment-Configuration.md](../Environment-Configuration.md)):

| Değişken | Amaç | Not |
|----------|------|-----|
| `YONETIM_ENVIRONMENT` | Aktif ortam | Üretim sunucusunda `Production` |
| `YONETIM_DB_CONNECTION` | DB bağlantı override | Opsiyonel; en yüksek öncelik |
| `YONETIM_SMTP_PASSWORD` | SMTP şifresi | appsettings'e **yazılmaz** |
| `YONETIM_SMTP_USERNAME` | SMTP kullanıcısı | Opsiyonel override |
| `YONETIM_UPDATE_PATH` | Publish UNC yolu | Publish sırasında |

- Secret'lar **Machine** kapsamında (tüm oturumlarda geçerli) ayarlanmalı, script'lere gömülmemeli.
- Değerler loglanmaz/raporlanmaz (`ServerInfo.ps1` bunları maskeler).

```powershell
[System.Environment]::SetEnvironmentVariable('YONETIM_ENVIRONMENT','Production','Machine')
[System.Environment]::SetEnvironmentVariable('YONETIM_SMTP_PASSWORD','<sifre>','Machine')
```

---

## Denetim Sıklığı

| Görev | Sıklık | Araç |
|-------|--------|------|
| Health check | Günlük | `HealthCheck.ps1` |
| Güvenlik denetimi | Haftalık / yayın öncesi | `SecurityAudit.ps1` |
| Sunucu envanteri | Aylık / değişiklik sonrası | `ServerInfo.ps1` |
| Sertifika kontrolü | Aylık | `SecurityAudit.ps1` |
| Restore tatbikatı | Çeyrek dönem | `scripts\Restore-Database.ps1` |
