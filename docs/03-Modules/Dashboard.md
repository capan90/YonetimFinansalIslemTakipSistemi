# Dashboard

## Finans Dashboard

`AnalysisWindow` veya ana ekrandaki dashboard bölümü.

**Göstergeler:**
- TL / USD / EUR bakiye kartları (filtreden bağımsız, tüm zamanlar)
- İşlem trendi (son 30 gün, giriş/çıkış karşılaştırması)
- Son işlemler listesi
- Toplam Borç / Toplam Alacak

**Handler:** `GetDashboardHandler` — para birimi bazında toplamlar ve son işlemler.

---

## Kargo Dashboard

Kargo modülü içindeki özet ekranı.

**Göstergeler:**
- Toplam gelen / giden sevkiyat sayısı
- Durum dağılımı (Beklemede / Yolda / Teslim / İptal)
- Son sevkiyatlar listesi

---

## Sistem Sağlığı Ekranı

`HealthCheckWindow` (yönetici erişimli):

| Gösterge | Kontrol |
|----------|---------|
| DB Bağlantısı | PostgreSQL ping |
| Migration Durumu | Pending migration var mı |
| Log Klasörü | Erişilebilir mi, boyut |
| Backup Klasörü | Erişilebilir mi, son backup tarihi |
| version.json | Okunabilir mi |
| Publish Klasörü | UNC erişilebilir mi |
| SMTP Durumu | Son bildirim zamanı, cooldown durumu |
| Genel Sağlık | Yukarıdakilerin özet puanı |
