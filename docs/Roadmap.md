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
- İşlem liste ekranı
- Filtreleme
- Rapor ekranı

## Aşama 6 - Destek Modülleri (kısmen)
- [x] Dialog sistemi
- [x] Audit log sistemi
- [x] Güncelleme sistemi (ClickOnce)
- [ ] Döviz ekranı

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
