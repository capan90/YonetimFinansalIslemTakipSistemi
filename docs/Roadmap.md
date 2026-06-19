# Yol Haritası

## Aşama 1 - Proje Kurulumu
- Proje klasör yapısını hazırlama
- README ve teknik dokümantasyon oluşturma
- Git yapısını kurma
- CLAUDE.md oluşturma

## Aşama 2 - Teknik Altyapı
- Solution oluşturma
- Katmanlı proje yapısını kurma
- Temel proje referanslarını bağlama
- Ortak ayar dosyalarını oluşturma

## Aşama 3 - Veritabanı Tasarımı
- Temel tabloları tasarlama
- Kullanıcı yapısını tasarlama
- İşlem tablolarını oluşturma
- Audit log tablosunu tasarlama

## Aşama 4 - Temel Modüller
- Login ekranı
- Ana pencere
- Kullanıcı yönetimi
- Yetki yönetimi

## Aşama 5 - Finansal Modüller
- İşlem giriş ekranı
- İşlem liste ekranı
- Filtreleme
- Rapor ekranı

## Aşama 6 - Destek Modülleri
- Dialog sistemi
- Audit log sistemi
- Güncelleme sistemi
- Döviz ekranı

## Teknik Borç

### Transfer İşlemi — Tek Taraflı Model (V1)

V1'de Transfer işlemi tek taraflı çıkış olarak kaydedilir.
Mevcut `CashTransaction` entity'sinde hedef kasa, banka hesabı veya çift yönlü hareket alanı bulunmamaktadır.

Gerçek kasa-arası transfer modeli desteklendiğinde şunlar gerekecek:
- Kaynak kasa / para birimi
- Hedef kasa / para birimi
- Opsiyonel kur bilgisi (farklı para birimleri arası)
- Eş hareket kaydı (kaynak çıkış + hedef giriş)
- `TransactionTypeExtensions.GetFinancialDirection()` ve rapor handler güncellenmesi