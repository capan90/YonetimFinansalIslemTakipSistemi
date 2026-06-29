# ADR-001: Clean Architecture ve Feature-Based Yapı

**Tarih:** 2026-06-18  
**Durum:** Kabul Edildi

---

## Karar

Clean Architecture (katmanlı mimari) ve Application katmanında feature-based (dikey dilim) yapı kullanılır.

```
Domain → Application → Infrastructure → UI
```

Application katmanı:
```
Features/
  CashTransactions/Commands/CreateCashTransaction/
  CashTransactions/Queries/GetCashTransactions/
  Cargo/Commands/CreateCargoShipment/
  ...
Interfaces/Repositories/
Common/
```

---

## Bağlam

Proje başlangıçta basit bir CRUD uygulaması olarak planlandı. Ancak:
- Birden fazla modül (finans, kargo, kullanıcı, log, güncelleme) hızla eklendi.
- Her modülün yetki, audit, validasyon ve DB katmanı vardır.
- Geliştirici (Claude Code + insan) katmanlar arasında net sınır istedi.
- Test edilebilirlik önemliydi.

---

## Alternatifler

### A: Service-Based (Services klasörü)

```
Application/Services/
  CashTransactionService.cs   ← 500+ satır olur
  UserService.cs
  CargoService.cs
```

**Sorun:** Büyüdükçe "God class" oluşur. Bağımlılıklar karmaşıklaşır. Yeni özellik = mevcut devasa sınıfı değiştirmek.

### B: CQRS + MediatR

Her use case `IRequest<T>` ve `IRequestHandler<T>` implementasyonu.

**Sorun:** MediatR bağımlılığı ekler. Bu ölçekteki proje için fazla kompleks.

### C: Feature-Based (Seçilen)

Her use case kendi klasöründe; harici kütüphane yok.

---

## Neden Seçildi

- Yeni özellik = mevcut kodu değiştirmeden yeni dosya eklemek.
- Her handler bağımsız test edilebilir.
- Klasör adı use case'i açıklar.
- İleride MediatR'a geçiş kolaylaşır (interface değişmez, MediatR adapte edilir).
- Harici MVVM veya mediator paketi gerektirmez.

---

## Artılar

- Tek sorumluluk: her handler bir şey yapar.
- Net sınırlar: UI'da SQL yok, Domain'de EF yok.
- Bağımsız test edilebilirlik.
- Claude Code'un bağlamı küçük tutulur — her özellik izole.

---

## Eksiler

- Her use case için birden fazla dosya gerekir (Command + Handler).
- Küçük projeler için fazla yapısal olabilir.

---

## Riskleri

- Handler'lar arası ortak mantığın `Common/` yerine handler'lara kopyalanma riski → kod tekrarı.
  - **Önlem:** Paylaşılan mantık `Common/` veya Domain extension metodlarına taşınır.

---

## Sonuç

Bu yapı V1'de başarıyla uygulandı. 15+ handler oluşturuldu ve hiçbir "God class" sorunu yaşanmadı. Kargo modülü (15 handler, 3 entity) clean architecture sayesinde mevcut koda dokunmadan eklenebildi.
