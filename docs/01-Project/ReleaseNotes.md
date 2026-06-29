# Release Notes

En son sürüm üstte. Her sürüm `v1.0.0.xx` formatındadır.

---

## v1.0.0.21 (2026-06-29) — ClickOnce Encoding Fix

**Düzeltildi:**
- `Publish-ClickOnce.ps1`'deki Türkçe karakterler nedeniyle oluşan mojibake (YÃ¶netim...) giderildi.
- Tüm `dotnet-mage` teknik değerleri ASCII-only yapıldı (`$AppName = "Yonetim Finansal Islem Takip Sistemi"`).
- Her publish öncesi publish klasörü temizleniyor — bayat manifest kalma riski ortadan kalktı.

**Eklendi:**
- Publish doğrulama bloğuna ASCII kontrolü ve yasaklı referans (iconFile/AppIcon) kontrolü eklendi.

---

## v1.0.0.20 (2026-06-29) — ClickOnce Icon Hotfix Cleanup

**Düzeltildi:**
- `Publish-ClickOnce.ps1`'den başarısız `-IconFile` XML post-processing kaldırıldı.
- Runtime hatası giderildi: "Dağıtım bildirimi simge dosyasının belirtimini kabul etmez."
- Temiz ve imzalı deployment manifest.

**Not:**
- Masaüstü kısayol ikonu ClickOnce runtime kısıtı nedeniyle özelleştirilemiyor (Won't Fix).

---

## v1.0.0.19 (2026-06-29) — ClickOnce Icon Investigation

**Eklendi:**
- `AppIcon.ico` Application Files klasörüne kopyalanıyor (harmless).

**Sonuç:**
- Desktop shortcut icon üzerinden kaldırıldı; v1.0.0.20'de temizlendi.

---

## v1.0.0.18 (2026-06-29) — Login UX

**Eklendi:**
- Son başarılı giriş kullanıcı adı hatırlanıyor (`user-preferences.json`, `%AppData%`).
- `ILocalUserPreferencesService` — şifre asla kaydedilmez.

**Değişti:**
- Login logo boyutu büyütüldü (180×260).
- Login penceresi yükseltildi (600px).

---

## v1.0.0.17 (2026-06-29) — Login Branding ve Tema

**Eklendi:**
- Login ekranı: logo (LoginIcon.png), "Hoş geldiniz", versiyon metni, telif hakkı.
- ClickOnce ortamında gerçek assembly versiyonu gösterimi.
- `IThemeService`, `ThemeService` — anlık tema değişimi.
- `LightTheme.xaml`, `DarkTheme.xaml`.
- `AppearanceSettingsWindow` (Görünüm Ayarları).

**Not:**
- Koyu tema şimdilik devre dışı (UI hazır, "Yakında" olarak gösterilir).

---

## v1.0.0.16 (2026-06-28) — AES ve SMTP Ayarları

**Eklendi:**
- AES-256 şifreleme ile ayar değerleri güvenli saklanıyor.
- SMTP ayarları DB'ye seed edildi.

---

## v1.0.0.15 (2026-06-27/28) — Sistem Log ve İzleme

**Eklendi:**
- `SystemLog` entity ve DB tablosu.
- `ISystemLogService`: Info / Warning / Error / Critical seviyeleri.
- Kritik hatalarda mail + sistem log; normal hatalarda yalnızca sistem log.
- Sistem Log izleme paneli (yönetici erişimli).

---

## v1.0.0.14 (2026-06-27) — Kargo Stabilizasyon

**Düzeltildi:**
- Mail Preview ekranının UI'yi dondurması (async deadlock) giderildi.
- Kargo edit akışı stabilize edildi.
- DataGrid seçili satır görünürlüğü iyileştirildi.

---

## v1.0.0.13 (2026-06-27) — Sprint 8: Finans UI Polishing

**Değişti:**
- DataGrid stilleri ve renk uyumu.
- Menü yapısı yeniden düzenlendi.
- Form UX iyileştirmeleri.
- Global stiller standardizasyonu.

---

## v1.0.0.12 (2026-06-27) — Sprint 7: Permission-Based Startup

**Eklendi:**
- Kullanıcı yetkisine göre açılış penceresi.
- Uygulama ikonu (`AppIcon.ico`).
- Nakit işlem kopyalama.

---

## v1.0.0.11 (2026-06-26) — Kargo Sprint 5: Dashboard + Rapor + PDF

**Eklendi:**
- Kargo dashboard.
- Kargo rapor ekranı.
- Kargo PDF export.

---

## v1.0.0.10 (2026-06-26) — Kargo Sprint 4: Operasyon Merkezi

**Eklendi:**
- `CargoOperationCenterWindow`: 5 operasyon kartı.
- Kargo listesi arama türü ComboBox.
- Kargo düzenleme ekranı firma kartı ve Ctrl+S.

---

## v1.0.0.9 (2026-06-25/26) — Kargo Sprint 3: Etiket + Bildirim

**Eklendi:**
- Kargo etiketi PDF (kurumsal tasarım, Dikkatine sistemi, QR placeholder).
- Mail bildirim hazırlama (`MailNotificationComposer`).
- WhatsApp bildirim hazırlama.
- `CargoNotificationPreviewWindow`.

---

## v1.0.0.8 (2026-06-25) — Kargo Sprint 2: Operasyonel Akış

**Eklendi:**
- Kargo numarası otomasyonu (G-YYYY-XXXX / C-YYYY-XXXX).
- Takip URL (tıklanabilir hyperlink).
- Bildirim durumu yönetimi.
- Durum geçiş kontrolü (`CargoStatusTransitions`).

---

## v1.0.0.7 (2026-06-24) — Kargo Sprint 1: Temel Modül

**Eklendi:**
- `CompanyDirectory`, `CargoCompany`, `CargoShipment` entity'leri.
- 7 yeni permission (8–14).
- 15 kargo handler'ı.
- Kargo Katip menüsü ve UI ekranları.

---

## v1.0.0.6 (2026-06-23/24) — Finans Revizyonları

**Değişti:**
- TransactionType: 5 tür → 2 tür (Giriş=1, Çıkış=2).
- Borç (yeşil) / Alacak (kırmızı) renk ayrımı.
- Açıklama zorunlu hale getirildi.
- Bakiye barı ana ekrana eklendi.
- Rapor detay satırları + filtreleme.

---

## v1.0.0.5 (2026-06-23) — Production Readiness Tamamlandı

**Eklendi:**
- Config management (appsettings + env var).
- Serilog loglama.
- Backup/restore scriptleri.
- Sistem sağlığı izleme ekranı.
- SMTP hata bildirimleri.

---

## v1.0.0.1 (2026-06-18) — Temel Altyapı

**Eklendi:**
- Domain / Application / Infrastructure / UI katmanları.
- `CreateCashTransaction`, `GetCashTransactions`.
- Login, auth, logout.
- Kullanıcı yönetimi, yetki yönetimi.
- Audit log.
- Dialog sistemi.
- Rapor ekranı (PDF + Excel).
- ClickOnce güncelleme sistemi.
- Exchange rate ekranı.
- Running balance kolonları.
