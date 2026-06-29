# Finans Modülü

## Genel Bakış

Şirket içi nakit hareketlerinin giriş, takip, analiz ve raporlanması.

---

## İşlem Tipleri

V1'de iki tip işlem vardır (önceki 5 tip Sprint 7 revizyonunda sadeleştirildi):

| TransactionType | Anlamı | Muhasebe | Renk |
|----------------|--------|---------|------|
| Giriş = 1 | Kasaya giriş | Borç | Yeşil |
| Çıkış = 2 | Kasadan çıkış | Alacak | Kırmızı |

**Para birimleri:** TRY, USD, EUR (ayrı bakiyeler)

**Açıklama:** Zorunlu. Boş bırakılamaz.

---

## İşlem CRUD

### Oluşturma
Handler: `CreateCashTransactionHandler`

Akış:
1. `CanCreateTransaction` izin kontrolü
2. Amount > 0 validasyonu
3. Description boş kontrolü
4. Entity oluşturma ve persist
5. `TransactionCreated` audit log

### Güncelleme
Handler: `UpdateCashTransactionHandler`

- `CanEditTransaction` izin kontrolü
- Yalnızca aynı gün veya yönetici editable (iş kuralı)
- OldValues / NewValues diff ile audit log

### Silme
Handler: `DeleteCashTransactionHandler`

- `CanDeleteTransaction` izin kontrolü
- Soft delete (`IsDeleted = true`, `DeletedAt`, `DeletedByUserId`)
- `TransactionDeleted` audit log

### Kopyalama
Mevcut işlemin tüm alanları yeni forma kopyalanır; kullanıcı tutarı ve tarihi düzenler.

---

## İşlem Listesi

`CashTransactionListViewModel` + `MainWindow` DataGrid.

**Filtreler:**
- Başlangıç / Bitiş tarihi (DatePicker)
- İşlem tipi (Giriş / Çıkış / Tümü)
- Para birimi (TRY / USD / EUR)
- Açıklama metin arama (ILIKE)
- Tutar operatörü (>=, <=, =)

**Filtre davranışı:** DB seviyesinde uygulanır (`GetFilteredAsync`). Performans açısından kayıtlar belleğe alınmadan filtrelenir.

---

## Running Balance (Kümülatif Bakiye)

Filtreden bağımsız olarak tarihsel doğru bakiye gösterilir.

**Mekanizma:**
1. `GetAllForBalanceAsync`: tüm aktif kayıtlar ASC sırayla çekilir.
2. Handler: her kayıt için per-currency kümülatif bakiye hesabı.
3. Filtre in-memory uygulanır; tarih filtresi altında bile bakiye gerçek tarihsel değeri yansıtır.

**DataGrid kolonları:**
- TL Bakiye
- USD Bakiye
- EUR Bakiye

Para birimi filtresine göre ilgili bakiye kolonları gösterilir/gizlenir.

---

## Bakiye Barı

`MainWindow` üstünde TL / USD / EUR bakiye barı bulunur. Filtreden bağımsız, tüm zamanlar kümülatif bakiyeyi gösterir. `GetCurrentBalancesHandler` ile her işlem sonrası otomatik güncellenir.

---

## Grid Layout Kaydetme

Kullanıcı kolonu gizlediğinde veya genişlettiğinde `user_grid_layouts` tablosuna kaydedilir. "Varsayılan Tasarıma Dön" ile silinir.

---

## Finans Dashboard

`AnalysisWindow` (veya Dashboard bölümü):
- TL / USD / EUR bakiye kartları
- İşlem trend grafiği (son 30 gün)
- Son işlemler listesi
- Borç/Alacak toplamları

---

## Rapor Ekranı

`ReportWindow`:

**Filtreler:**
- Tarih aralığı
- İşlem tipi
- Para birimi
- Açıklama içeriği
- İşlem detayları göster/gizle

**Çıktılar:**
- Özet kartlar (TL / USD / EUR toplamları)
- İşlem türü tablosu (Giriş / Çıkış / Net)
- Detay satırları (kümülatif bakiye ile)

**Export:**
- PDF export (QuestPDF 2024.3.5)
- Excel export (ClosedXML)
- `ReportPreviewWindow`: önizleme + yazdır

**Handler:** `GetReportHandler`
- `CanViewReports` izin kontrolü
- UTC yarı-açık aralık: `>= startDate AND < endDate.AddDays(1)`
- GROUP BY PostgreSQL'de; kayıtlar belleğe alınmaz

---

## Döviz Ekranı

`ExchangeRateWindow`:
- USD / EUR alış-satış kuru manuel girişi
- `CanManageExchangeRates` izin kontrolü
- En son kur tarihi gösterimi

Not: TCMB otomatik entegrasyonu V2 planında.

---

## Muhasebe Kuralı

```
Bakiye = Toplam Giriş (Borç) - Toplam Çıkış (Alacak)
Giriş → Bakiyeyi artırır
Çıkış → Bakiyeyi azaltır
```

`TransactionTypeExtensions.GetFinancialDirection()` merkezi yön belirleme metodudur.

---

## İş Kuralı Notları

- Açıklama zorunludur — handler seviyesinde doğrulama.
- Tutar sıfır veya negatif olamaz.
- Silinen kayıtlar bakiye hesabına dahil edilmez (`IsDeleted = false` filtresi).
- Silinmiş kayıtlar acil durumda SQL ile geri alınabilir (bkz. `docs/Emergency-SQL-Commands.md`).
