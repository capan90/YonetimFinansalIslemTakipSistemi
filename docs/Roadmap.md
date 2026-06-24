# Yol Haritası

## Aşama 1 - Proje Kurulumu ✓
- Proje klasör yapısını hazırlama
- README ve teknik dokümantasyon oluşturma
- Git yapısını kurma
- CLAUDE.md oluşturma

## Aşama 2 - Teknik Altyapı ✓
- Solution oluşturma
- Katmanlı proje yapısını kurma
- Temel proje referanslarını bağlama
- Ortak ayar dosyalarını oluşturma

## Aşama 3 - Veritabanı Tasarımı ✓
- Temel tabloları tasarlama
- Kullanıcı yapısını tasarlama
- İşlem tablolarını oluşturma
- Audit log tablosunu tasarlama

## Aşama 4 - Temel Modüller ✓
- Login ekranı
- Ana pencere
- Kullanıcı yönetimi
- Yetki yönetimi

## Aşama 5 - Finansal Modüller ✓
- İşlem giriş ekranı
- İşlem liste ekranı + running balance kolonları
- Filtreleme
- Rapor ekranı

## Aşama 6 - Destek Modülleri ✓
- [x] Dialog sistemi
- [x] Audit log sistemi
- [x] Güncelleme sistemi (ClickOnce)
- [x] Döviz ekranı

---

## Teknik Borç

### Transfer İşlemi — Tek Taraflı Model (V1)

V1'de Transfer kaydı tek taraflı çıkış olarak işlenir (`TransactionTypeExtensions.GetFinancialDirection()`).
Mevcut entity'de kaynak kasa, hedef kasa veya çift yönlü hareket alanı bulunmamaktadır.

Gerçek kasa-arası transfer desteklendiğinde gerekecekler:
- Kaynak kasa / para birimi + hedef kasa / para birimi
- Opsiyonel kur bilgisi
- Eş hareket kaydı (kaynak çıkış + hedef giriş)
- `GetFinancialDirection()` ve rapor handler güncellenmesi

###################################################################

# Production Readiness Çalışmaları (Sprint 1–5)

Bu doküman, sistemin kurumsal kullanıma ve şirket sunucusu ortamına hazırlanması amacıyla gerçekleştirilen Production Readiness çalışmalarını özetlemektedir.

---

# Sprint 1 — Configuration Management

Amaç:

Bağlantı bilgilerinin ve ortam ayarlarının merkezi olarak yönetilmesini sağlamak.

Yapılanlar:

* Hardcoded connection string kullanımları kaldırıldı.
* appsettings.json yapısı oluşturuldu.
* Environment Variable desteği eklendi.
* ConnectionStrings:DefaultConnection yapısı oluşturuldu.
* AppDbContextFactory yeniden düzenlendi.
* EF Core migration komutlarının farklı dizinlerden çalışabilmesi sağlandı.
* Veritabanı Bağlantısını Test Et özelliği eklendi.
* Production ortamı için örnek konfigürasyon dosyası oluşturuldu.
* Production şifrelerinin GitHub'a gönderilmesini engelleyen yapı oluşturuldu.

Kazanımlar:

* Sunucu değişiklikleri kod değişikliği gerektirmeden yapılabilir hale geldi.
* Development ve Production ortamları ayrıştırıldı.
* Merkezi bağlantı yönetimi sağlandı.

---

# Sprint 2 — Logging & Global Exception Handling

Amaç:

Sistemde oluşan hataların izlenebilir ve kayıt altına alınabilir hale getirilmesi.

Yapılanlar:

* Serilog entegrasyonu yapıldı.
* Dosya tabanlı loglama sistemi kuruldu.
* Rolling log dosyaları oluşturuldu.
* Global exception handler mekanizması eklendi.
* DispatcherUnhandledException yönetimi yapıldı.
* AppDomain.UnhandledException yönetimi yapıldı.
* TaskScheduler.UnobservedTaskException yönetimi yapıldı.
* Update sistemi loglamaya dahil edildi.
* Rapor servisleri loglamaya dahil edildi.
* Log Klasörünü Aç özelliği eklendi.

Kazanımlar:

* Beklenmeyen hatalar kayıt altına alınmaktadır.
* Kullanıcı dostu hata mesajları sunulmaktadır.
* Teknik detaylar log dosyalarında saklanmaktadır.

---

# Sprint 3 — Backup, Recovery & Disaster Planning

Amaç:

Veri kaybı risklerini azaltmak ve felaket senaryolarına hazırlıklı olmak.

Yapılanlar:

* Backup-Database.ps1 oluşturuldu.
* Backup-And-Migrate.ps1 oluşturuldu.
* Restore-Database.ps1 oluşturuldu.
* Backup-Recovery-Guide dokümantasyonu hazırlandı.
* Disaster-Recovery-Plan dokümantasyonu hazırlandı.
* Emergency-SQL-Commands dokümantasyonu hazırlandı.
* Release-Checklist dokümantasyonu hazırlandı.
* Backup dosyalarının GitHub'a gönderilmesi engellendi.

Kazanımlar:

* Migration öncesi güvenli backup süreci oluşturuldu.
* Veri kurtarma prosedürleri tanımlandı.
* Acil durum senaryoları için operasyonel hazırlık sağlandı.

---

# Sprint 4 — System Health Monitoring

Amaç:

Sistemin genel sağlık durumunun merkezi olarak izlenebilmesi.

Yapılanlar:

* Sistem Sağlığı ekranı geliştirildi.
* Veritabanı bağlantı durumu gösterildi.
* Migration durumu gösterildi.
* Log klasörü durumu gösterildi.
* Backup klasörü durumu gösterildi.
* Version.json kontrolü eklendi.
* Publish klasörü bilgisi eklendi.
* Genel sağlık durumu hesaplama mekanizması geliştirildi.
* Yönetici yetkisi ile erişim sağlandı.

Kazanımlar:

* Sistem durumu tek ekrandan takip edilebilir hale geldi.
* Operasyonel sorunlar erken tespit edilebilir hale geldi.
* Sunucu geçişi öncesi gözlemlenebilirlik artırıldı.

---

# Sprint 5 — SMTP Error Notifications

Amaç:

Kritik sistem hatalarının yöneticilere otomatik bildirilmesi.

Yapılanlar:

* SMTP tabanlı bildirim altyapısı geliştirildi.
* SmtpErrorNotificationService oluşturuldu.
* NotificationContext yapısı geliştirildi.
* Mail cooldown mekanizması eklendi.
* SMTP ayarları appsettings üzerinden yönetilebilir hale getirildi.
* Environment Variable desteği eklendi.
* Kritik hata noktaları bildirim sistemine bağlandı.
* Health Check ekranına bildirim durumu bilgileri eklendi.

Kazanımlar:

* Kritik hatalar anlık olarak yöneticilere iletilebilir hale geldi.
* Operasyonel müdahale süreleri azaltıldı.
* Sistem izlenebilirliği önemli ölçüde artırıldı.

---

# Genel Sonuç

Sprint 1–5 çalışmaları sonucunda sistem:

* Merkezi konfigürasyon yönetimine sahiptir.
* Loglama altyapısına sahiptir.
* Global hata yönetimine sahiptir.
* Otomatik hata bildirim altyapısına sahiptir.
* Backup ve restore süreçlerine sahiptir.
* Felaket kurtarma planına sahiptir.
* Sistem sağlık izleme ekranına sahiptir.
* Kurumsal sunucu ortamına taşınabilecek operasyonel olgunluğa ulaşmıştır.

Bu çalışmalar sonrasında sistem, Production Deployment aşamasına geçmeye hazır hale gelmiştir.

