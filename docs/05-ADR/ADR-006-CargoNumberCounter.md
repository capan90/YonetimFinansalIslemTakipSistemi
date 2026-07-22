# ADR-006 — Kargo Numarası İçin Transaction-Safe Sayaç Tablosu

**Tarih:** 2026-07-22
**Durum:** Kabul edildi

## Bağlam

Gelen/giden kargolar birbirinden bağımsız, mükerrer olamayan otomatik sıra numarası
almalıdır (Gelen: `GLN00001`, Giden: `GDN00001`). Önceki yöntem, kayıt tablosundaki
mevcut maksimum numaraya bakıp +1 üretiyordu (`G-YYYY-NNNN`). Bu yaklaşımın sorunları:

1. **Yarış durumu:** İki kullanıcı aynı anda kayıt açtığında ikisi de aynı max değeri
   okuyup aynı numarayı üretebilir.
2. **Silinen numaraların geri dönmesi:** Numara geçmişi kayıt tablosuna bağlı olduğu
   için tablo temizlenirse/taşınırsa numara geçmişi kaybolur.

## Karar

Kayıt tablosundan bağımsız bir **`cargo_number_counters`** tablosu tutulur
(yön başına tek satır: `Direction`, `LastValue`). Numara üretimi:

```sql
INSERT INTO cargo_number_counters ("Direction", "LastValue") VALUES (@d, 1)
ON CONFLICT ("Direction") DO UPDATE SET "LastValue" = ... + 1
RETURNING "LastValue"
```

- Bu ifade **atomiktir**: PostgreSQL satır kilidi eşzamanlı çağrıları sıraya sokar;
  mükerrer numara üretimi imkânsızdır.
- Sayaç artışı ve kargo insert'i **aynı transaction** içinde yapılır
  (`CargoShipmentRepository.AddWithAutoNumberAsync`). Insert başarısız olursa rollback
  sayacı da geri alır — numara boşa gitmez.
- Defansif katman: `ShipmentNumber` üzerindeki unique index ihlalinde sayaç, mevcut en
  büyük numaraya senkronlanıp işlem yeniden denenir (elle taşınan veri senaryosu).

## Silme ve Kontrollü Geri Alma (Revizyon — 2026-07-22)

İş kuralı: **aradaki silinmiş numaralar asla yeniden kullanılmaz**; yalnızca yönün
**en son üretilen numarasına** sahip kayıt silinirse (ve daha yüksek numaralı başka
kayıt yoksa) numara sonraki kayıtta yeniden kullanılabilir.

Uygulama (`CargoShipmentRepository.SoftDeleteWithNumberReclaimAsync`) — soft delete ve
sayaç geri alma **aynı transaction** içindedir:

```sql
UPDATE cargo_number_counters c
SET "LastValue" = c."LastValue" - 1
WHERE c."Direction" = @d
  AND c."LastValue" = @seq            -- yalnızca silinen kayıt SON numaraysa
  AND NOT EXISTS (                     -- ve daha yüksek numaralı kayıt yoksa (soft delete dahil)
      SELECT 1 FROM cargo_shipments s
      WHERE s."Id" <> @id
        AND s."ShipmentNumber" ~ ('^' || @prefix || '[0-9]+$')
        AND substring(s."ShipmentNumber" from 4)::bigint > @seq)
```

- Koşullu UPDATE, sayaç satırını kilitler → eşzamanlı create'ler (aynı satırdaki
  `ON CONFLICT ... RETURNING`) delete commit'ine kadar bekler; duplicate imkânsızdır.
- Geri alma gerçekleşirse silinen kaydın `ShipmentNumber` alanı NULL'a çekilir
  (partial unique index numarayı serbest bırakır); **numara bilgisi silme audit
  kaydında korunur** ve geri alma System Log'a Info seviyesinde yazılır.
- UPDATE 0 satır etkilerse (aradaki numara, sayaç ileride, eski format) yalnızca
  soft delete yapılır — sayaç ve numara dokunulmadan kalır.
- Transaction rollback olursa ne sayaç ne kayıt değişir.
- Eski `G/C-YYYY-NNNN` numaralar `TryParseSequence` tarafından tanınmaz → sayaca
  hiçbir koşulda dokunulmaz, numara entity üzerinde korunur.

## Alternatifler

- **PostgreSQL SEQUENCE:** Atomiktir ancak transaction rollback'inde değer geri
  alınmaz (numara boşa gider) ve "son kullanılan numarayı mevcut veriye eşitleme"
  gibi düzeltmeler daha zahmetlidir. Sayaç tablosu aynı garantiyi verirken rollback'te
  numara kaybını da önler.
- **Max+1 (eski yöntem):** Yarış durumuna açık; reddedildi.

## Sonuçlar

- Numara alanı kullanıcıdan tamamen kaldırıldı (request'lerde yok, UI salt okunur);
  create audit'inde yer alır, silme audit'inde korunur.
- Silinen SON numara kontrollü biçimde geri kullanılır (yukarıdaki bölüm);
  aradaki silinmiş numaralar asla geri dönmez.
- Eski `G/C-YYYY-NNNN` formatındaki mevcut numaralara dokunulmadı — audit kayıtları ve
  basılı etiketler bu numaralara referans verir. Yeni kayıtlar `GLN/GDN` formatıyla devam eder.
- Migration yalnızca NULL numaraları deterministik doldurur ve sayaçları mevcut
  maksimuma eşitler.
