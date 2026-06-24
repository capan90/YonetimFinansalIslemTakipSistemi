# Acil Durum SQL Komutları

## Tablo Yapısı Özeti

| Tablo | Amaç |
|-------|------|
| `cash_transactions` | Finansal işlemler |
| `users` | Kullanıcılar |
| `user_permissions` | Kullanıcı yetkileri |
| `audit_logs` | Denetim kayıtları |
| `exchange_rates` | Döviz kurları |
| `user_grid_layouts` | Kullanıcı arayüz ayarları |

## Enum Değerleri

**TransactionType (cash_transactions."TransactionType"):**
- `1` = Giriş (Borç) — bakiyeyi artırır
- `2` = Çıkış (Alacak) — bakiyeyi azaltır

**CurrencyType (cash_transactions."CurrencyType"):**
- `1` = TRY
- `2` = USD
- `3` = EUR

> **İş Kuralı:** Bakiye = Toplam Giriş − Toplam Çıkış
> Giriş artı (+), Çıkış eksi (−).

## Sorgular

---

### 1. Son 50 İşlem

```sql
SELECT
    ct."Id",
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE ct."TransactionType"
        WHEN 1 THEN 'Giriş'
        WHEN 2 THEN 'Çıkış'
    END AS "İşlem Tipi",
    CASE ct."CurrencyType"
        WHEN 1 THEN 'TRY'
        WHEN 2 THEN 'USD'
        WHEN 3 THEN 'EUR'
    END AS "Para Birimi",
    ct."Amount",
    ct."Description",
    u."FullName" AS "Kaydeden",
    ct."CreatedAt" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Kayıt Zamanı"
FROM cash_transactions ct
LEFT JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."IsDeleted" = false
ORDER BY ct."TransactionDate" DESC, ct."CreatedAt" DESC
LIMIT 50;
```

---

### 2. Bugünkü İşlemler

```sql
SELECT
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE ct."TransactionType" WHEN 1 THEN 'Giriş' WHEN 2 THEN 'Çıkış' END AS "Tip",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    ct."Amount",
    ct."Description",
    u."FullName" AS "Kaydeden"
FROM cash_transactions ct
LEFT JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."IsDeleted" = false
  AND (ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul')::date
      = (NOW() AT TIME ZONE 'Europe/Istanbul')::date
ORDER BY ct."TransactionDate" DESC;
```

---

### 3. Tarih Aralığına Göre İşlemler

```sql
-- Başlangıç ve bitiş tarihini değiştirin:
SELECT
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE ct."TransactionType" WHEN 1 THEN 'Giriş' WHEN 2 THEN 'Çıkış' END AS "Tip",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    ct."Amount",
    ct."Description",
    u."FullName" AS "Kaydeden"
FROM cash_transactions ct
LEFT JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."IsDeleted" = false
  AND ct."TransactionDate" >= '2026-06-01 00:00:00+03'
  AND ct."TransactionDate" <  '2026-07-01 00:00:00+03'
ORDER BY ct."TransactionDate";
```

---

### 4. TRY Güncel Bakiye

```sql
SELECT
    SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE -"Amount" END) AS "TRY Bakiye"
FROM cash_transactions
WHERE "IsDeleted" = false
  AND "CurrencyType" = 1;
```

---

### 5. USD Güncel Bakiye

```sql
SELECT
    SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE -"Amount" END) AS "USD Bakiye"
FROM cash_transactions
WHERE "IsDeleted" = false
  AND "CurrencyType" = 2;
```

---

### 6. EUR Güncel Bakiye

```sql
SELECT
    SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE -"Amount" END) AS "EUR Bakiye"
FROM cash_transactions
WHERE "IsDeleted" = false
  AND "CurrencyType" = 3;
```

---

### 7. Tüm Para Birimlerinde Bakiye Özeti

```sql
SELECT
    CASE "CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Para Birimi",
    SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE 0 END)  AS "Toplam Giriş (Borç)",
    SUM(CASE WHEN "TransactionType" = 2 THEN "Amount" ELSE 0 END)  AS "Toplam Çıkış (Alacak)",
    SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE -"Amount" END) AS "Net Bakiye"
FROM cash_transactions
WHERE "IsDeleted" = false
GROUP BY "CurrencyType"
ORDER BY "CurrencyType";
```

---

### 8. Toplam Borç / Alacak (Para Birimine Göre)

```sql
-- Borç = Giriş, Alacak = Çıkış
SELECT
    CASE "CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    ROUND(SUM(CASE WHEN "TransactionType" = 1 THEN "Amount" ELSE 0 END), 2)  AS "Toplam Borç (Giriş)",
    ROUND(SUM(CASE WHEN "TransactionType" = 2 THEN "Amount" ELSE 0 END), 2)  AS "Toplam Alacak (Çıkış)"
FROM cash_transactions
WHERE "IsDeleted" = false
GROUP BY "CurrencyType"
ORDER BY "CurrencyType";
```

---

### 9. Yalnızca Giriş (Borç) Kayıtları

```sql
SELECT
    "TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE "CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    "Amount",
    "Description"
FROM cash_transactions
WHERE "IsDeleted" = false
  AND "TransactionType" = 1
ORDER BY "TransactionDate" DESC;
```

---

### 10. Yalnızca Çıkış (Alacak) Kayıtları

```sql
SELECT
    "TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE "CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    "Amount",
    "Description"
FROM cash_transactions
WHERE "IsDeleted" = false
  AND "TransactionType" = 2
ORDER BY "TransactionDate" DESC;
```

---

### 11. Soft-Delete Edilmiş Kayıtlar

```sql
SELECT
    ct."Id",
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "İşlem Tarihi",
    CASE ct."TransactionType" WHEN 1 THEN 'Giriş' WHEN 2 THEN 'Çıkış' END AS "Tip",
    ct."Amount",
    ct."Description",
    ct."DeletedAt" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Silinme Zamanı",
    u."FullName" AS "Silen Kullanıcı"
FROM cash_transactions ct
LEFT JOIN users u ON ct."DeletedByUserId" = u."Id"
WHERE ct."IsDeleted" = true
ORDER BY ct."DeletedAt" DESC
LIMIT 50;
```

---

### 12. Tüm Kullanıcılar

```sql
SELECT
    u."Id",
    u."FullName"        AS "Ad Soyad",
    u."UserName"        AS "Kullanıcı Adı",
    u."IsActive"        AS "Aktif",
    u."IsDeleted"       AS "Silindi",
    u."CreatedAt" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Oluşturma"
FROM users u
ORDER BY u."FullName";
```

---

### 13. Aktif Kullanıcılar ve Yetkileri

```sql
SELECT
    u."FullName"    AS "Ad Soyad",
    u."UserName"    AS "Kullanıcı Adı",
    up.permission   AS "Yetki Kodu"
FROM users u
LEFT JOIN user_permissions up ON up.user_id = u."Id"
WHERE u."IsActive" = true
  AND u."IsDeleted" = false
ORDER BY u."FullName", up.permission;
```

---

### 14. Audit Log — Son 50 Kayıt

```sql
SELECT
    al."Timestamp" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Zaman",
    al."UserName"     AS "Kullanıcı",
    al."Action"       AS "Eylem Kodu",
    al."EntityType"   AS "Varlık Tipi",
    al."EntityId"     AS "Kayıt ID",
    al."ComputerName" AS "Bilgisayar",
    al."OldValues"    AS "Eski Değer",
    al."NewValues"    AS "Yeni Değer"
FROM audit_logs al
ORDER BY al."Timestamp" DESC
LIMIT 50;
```

---

### 15. Belirli Kullanıcının Audit Kayıtları

```sql
-- 'KULLANICI_ADI' yerine kullanıcı adını yazın:
SELECT
    al."Timestamp" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Zaman",
    al."Action"       AS "Eylem",
    al."EntityType"   AS "Varlık",
    al."EntityId"     AS "ID",
    al."ComputerName" AS "Bilgisayar",
    al."OldValues",
    al."NewValues"
FROM audit_logs al
WHERE al."UserName" = 'KULLANICI_ADI'
ORDER BY al."Timestamp" DESC
LIMIT 100;
```

---

### 16. Açıklama ile İşlem Arama

```sql
-- 'arama_terimi' yerine aranacak metni yazın (büyük/küçük harf duyarsız):
SELECT
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE ct."TransactionType" WHEN 1 THEN 'Giriş' WHEN 2 THEN 'Çıkış' END AS "Tip",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    ct."Amount",
    ct."Description"
FROM cash_transactions ct
WHERE ct."IsDeleted" = false
  AND ct."Description" ILIKE '%arama_terimi%'
ORDER BY ct."TransactionDate" DESC;
```

---

### 17. Günlük Özet (Son 30 Gün)

```sql
SELECT
    DATE(ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul') AS "Gun",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    COUNT(*)                                                                          AS "İşlem Sayısı",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Giriş",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 2 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Çıkış",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE -ct."Amount" END), 2) AS "Net"
FROM cash_transactions ct
WHERE ct."IsDeleted" = false
  AND ct."TransactionDate" >= NOW() - INTERVAL '30 days'
GROUP BY 1, 2
ORDER BY 1 DESC, 2;
```

---

### 18. Aylık Özet (Mevcut Yıl)

```sql
SELECT
    TO_CHAR(ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul', 'YYYY-MM') AS "Ay",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    COUNT(*) AS "İşlem Sayısı",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Giriş",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 2 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Çıkış",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE -ct."Amount" END), 2) AS "Net"
FROM cash_transactions ct
WHERE ct."IsDeleted" = false
  AND EXTRACT(YEAR FROM ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul')
      = EXTRACT(YEAR FROM NOW() AT TIME ZONE 'Europe/Istanbul')
GROUP BY 1, 2
ORDER BY 1, 2;
```

---

### 19. Belirli Ay Özeti

```sql
-- Yıl ve ay numarasını değiştirin:
SELECT
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Para Birimi",
    COUNT(*) AS "İşlem Sayısı",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Giriş (Borç)",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 2 THEN ct."Amount" ELSE 0 END), 2)    AS "Toplam Çıkış (Alacak)",
    ROUND(SUM(CASE WHEN ct."TransactionType" = 1 THEN ct."Amount" ELSE -ct."Amount" END), 2) AS "Net Bakiye"
FROM cash_transactions ct
WHERE ct."IsDeleted" = false
  AND EXTRACT(YEAR  FROM ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul') = 2026
  AND EXTRACT(MONTH FROM ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul') = 6
GROUP BY "CurrencyType"
ORDER BY "CurrencyType";
```

---

### 20. Döviz Kurları (En Son)

```sql
SELECT
    er.rate_date AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Kur Tarihi",
    CASE er.currency_type WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    er.forex_buying  AS "Döviz Alış",
    er.forex_selling AS "Döviz Satış"
FROM exchange_rates er
WHERE er.is_deleted = false
ORDER BY er.rate_date DESC, er.currency_type
LIMIT 20;
```

---

### 21. Kullanıcı Başına İşlem Sayısı

```sql
SELECT
    u."FullName"   AS "Kullanıcı",
    COUNT(ct."Id") AS "İşlem Sayısı",
    ROUND(SUM(ct."Amount"), 2) AS "Toplam Tutar (Karışık Döviz)"
FROM cash_transactions ct
JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."IsDeleted" = false
GROUP BY u."Id", u."FullName"
ORDER BY COUNT(ct."Id") DESC;
```

---

### 22. Belirli İşlem ID ile Kayıt Sorgulama

```sql
-- '<kayit-uuid>' yerine işlem ID'sini yazın:
SELECT
    ct.*,
    u."FullName" AS "Kaydeden Kullanıcı"
FROM cash_transactions ct
LEFT JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."Id" = '<kayit-uuid>';
```

---

### 23. Son Hafta Audit Aktivitesi

```sql
SELECT
    DATE(al."Timestamp" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul') AS "Gün",
    al."UserName",
    al."Action",
    COUNT(*) AS "Eylem Sayısı"
FROM audit_logs al
WHERE al."Timestamp" >= NOW() - INTERVAL '7 days'
GROUP BY 1, 2, 3
ORDER BY 1 DESC, 4 DESC;
```

---

### 24. Büyük Tutarlı İşlemler (Eşik Belirlenerek)

```sql
-- 10000 TL veya üzeri işlemler:
SELECT
    ct."TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul' AS "Tarih",
    CASE ct."TransactionType" WHEN 1 THEN 'Giriş' WHEN 2 THEN 'Çıkış' END AS "Tip",
    CASE ct."CurrencyType" WHEN 1 THEN 'TRY' WHEN 2 THEN 'USD' WHEN 3 THEN 'EUR' END AS "Döviz",
    ct."Amount",
    ct."Description",
    u."FullName" AS "Kaydeden"
FROM cash_transactions ct
LEFT JOIN users u ON ct."CreatedByUserId" = u."Id"
WHERE ct."IsDeleted" = false
  AND ct."CurrencyType" = 1        -- TRY
  AND ct."Amount" >= 10000
ORDER BY ct."Amount" DESC;
```

---

### 25. Log Tablosu Hakkında Not

> Uygulama hata logları bir veritabanı tablosuna değil, **dosyaya** yazılır.
> Log klasörü: `<kurulum-dizini>\logs\app-YYYYMMDD.log`
>
> Log dosyasını incelemek için:
> - Windows: Not Defteri, VS Code veya PowerShell `Get-Content ... -Tail 100`
> - Hata satırları `[ERR]` veya `[FTL]` ile başlar.
>
> Gelecek sprint'te log yönetimi için ayrı bir altyapı değerlendirilebilir.

---

## Hızlı Referans — Sık Kullanılan Sorgular

```sql
-- Anlık TRY bakiye
SELECT SUM(CASE WHEN "TransactionType"=1 THEN "Amount" ELSE -"Amount" END) AS "TRY"
FROM cash_transactions WHERE "IsDeleted"=false AND "CurrencyType"=1;

-- Bugünkü işlem sayısı
SELECT COUNT(*) FROM cash_transactions
WHERE "IsDeleted"=false
AND DATE("TransactionDate" AT TIME ZONE 'UTC' AT TIME ZONE 'Europe/Istanbul')
    = DATE(NOW() AT TIME ZONE 'Europe/Istanbul');

-- En son 10 audit kaydı
SELECT "Timestamp", "UserName", "Action", "EntityType" FROM audit_logs
ORDER BY "Timestamp" DESC LIMIT 10;

-- Tüm aktif kullanıcılar
SELECT "FullName", "UserName" FROM users
WHERE "IsActive"=true AND "IsDeleted"=false ORDER BY "FullName";
```
