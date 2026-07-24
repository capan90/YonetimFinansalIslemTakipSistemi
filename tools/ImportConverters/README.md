# Import Dönüştürücüleri (Bir Kerelik Taşıma Araçları)

Mevcut rehber dosyalarını, uygulamadaki **Excel'den İçe Aktar** sihirbazlarının
okuduğu xlsx şablonlarına çevirir. Ana çözümün (sln) parçası değildir; uygulama
bu araçlara bağımlı olmaz. `dotnet run` ile klasöründen çalıştırılır.

| Araç | Kaynak | Çıktı (Masaüstü) |
|---|---|---|
| OdsRehberConverter | `REHBER 1.ods` (Firma_Tlf + Şirket_Tlf sayfaları) | `rehber-firmalar.xlsx` + `rehber-whatsapp-kisiler.xlsx` |
| XlsSehirlerArasiConverter | `REHBER 2 ŞEHİRLER ARASI.xls` | `rehber-firmalar-sehirlerarasi.xlsx` |

Kullanım (kaynak dosya yolları Program.cs başında sabittir, gerekirse güncelleyin):

```powershell
dotnet run --project tools\ImportConverters\OdsRehberConverter
dotnet run --project tools\ImportConverters\XlsSehirlerArasiConverter
```

Dönüştürme kuralları:
- **ODS/Firma_Tlf:** ad + telefon + FAX→Not. **ODS/Şirket_Tlf:** ad soyad + cep + kod→Açıklama.
- **XLS/Şehirler Arası:** iki adres kolonu = iki AYRI rehber kaydı (firma başına adres
  kaydı; kargo gönderisinde doğru adres seçilebilsin). Tel No1 → 1. kayıt,
  Tel No2 → 2. kayıt; telefonlar `0 + bölge kodu + numara` biçiminde birleştirilir.
- Dahili_No sayfası kapsam dışıdır (sistemde karşılığı henüz yok — planlı iş).
