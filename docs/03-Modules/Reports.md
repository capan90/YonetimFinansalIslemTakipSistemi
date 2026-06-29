# Raporlama

## Finans Raporu

`ReportWindow` — `CanViewReports` izni gerektirir.

### Filtreler

| Filtre | Açıklama |
|--------|----------|
| Başlangıç tarihi | Rapor dönem başı |
| Bitiş tarihi | Rapor dönem sonu (dahil) |
| İşlem tipi | Giriş / Çıkış / Tümü |
| Para birimi | TRY / USD / EUR |
| Açıklama | Metin içerir (ILIKE) |
| İşlem detayları | Detay satırları göster/gizle |

### Özet Kartlar

Her para birimi için:
- Toplam Giriş (Borç)
- Toplam Çıkış (Alacak)
- Net Bakiye

### Detay Satırları

Her işlem satırı + kümülatif bakiye gösterimi. `GetFilteredForReportDetailAsync` ile DB'den çekilir.

### Handler Detayları

`GetReportHandler`:
- UTC yarı-açık aralık: `>= startDate AND < endDate.AddDays(1)`
- GROUP BY PostgreSQL'de; kayıtlar belleğe alınmaz
- Detaylar için kümülatif bakiye handler'da ASC sırayla hesaplanır

---

## Export

### PDF Export

`ReportExportService` + `QuestPDF 2024.3.5`:
- Özet tablo (para birimi bazında toplamlar)
- Detay tablosu (işlem satırları, kümülatif bakiye)
- Genel toplamlar paneli

`ReportPreviewWindow`: önizleme ekranı, yazdır butonu.

### Excel Export

`ClosedXML`:
- Özet sheet
- Detay sheet (filtre uygulanmış işlemler)

---

## Kargo Raporu

- Tarih filtreli kargo sevkiyat raporu
- Gelen/giden ayrımı
- Durum özeti
- PDF export

---

## xUnit Test Kapsamı

`TransactionTypeExtensions.GetFinancialDirection()` yön kuralları için 11 birim testi.

```powershell
dotnet test src\YonetimFinansalIslemTakipSistemi.sln
```
