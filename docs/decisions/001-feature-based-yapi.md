# ADR-001: Application Katmanında Feature-Based Yapı

**Tarih:** 2026-06-18
**Durum:** Kabul Edildi

## Konu

Application katmanı için klasör yapısı seçimi: `Services/` klasörü mü,
feature-based (dikey dilim) yapı mı?

## Karar

Feature-based yapı seçildi:

```
Features/
  CashTransactions/
    Commands/CreateCashTransaction/
    Queries/GetCashTransactions/
Interfaces/Repositories/
Common/
```

## Gerekçe

`Services/` yaklaşımında tüm iş mantığı büyük servis sınıflarına yığılır;
proje büyüdükçe dosyalar şişer ve bağımlılıklar karmaşıklaşır.

Feature-based yapıda her use case kendi klasöründe yaşar. Yeni bir özellik
eklemek mevcut kodu değiştirmek yerine yeni dosya eklemek anlamına gelir.
Bu yapı aynı zamanda ilerleyen aşamalarda CQRS pattern'e geçişi kolaylaştırır.

## Sonuçlar

- Her yeni işlem tipi (Update, Delete, GetList) kendi alt klasörüne açılır.
- Paylaşılan yardımcılar `Common/` altında tutulur.
- `Services/` klasörü bu projede kullanılmaz.
