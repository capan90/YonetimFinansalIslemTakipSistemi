# Dokümantasyon Rehberi

Bu klasör, **Yönetim Finansal İşlem Takip Sistemi** projesinin teknik ve operasyonel dokümantasyonunu içerir.

---

## Yapı

```
docs/
├── README.md                   ← Bu dosya — navigasyon haritası
├── 01-Project/                 ← Proje özeti, yol haritası, sprint geçmişi
├── 02-Architecture/            ← Teknik mimari, veritabanı, sistem bileşenleri
├── 03-Modules/                 ← Modül bazlı detaylar
├── 04-Development/             ← Geliştirici rehberi, standartlar, sorun giderme
└── 05-ADR/                     ← Mimari kararlar (Architecture Decision Records)
```

---

## Yeni Geliştirici Başlangıç Sırası

1. `CLAUDE.md` — Proje kuralları ve AI talimatları
2. `docs/01-Project/ProjectOverview.md` — Projenin amacı ve genel yapısı
3. `docs/02-Architecture/CleanArchitecture.md` — Katmanlı mimari anlayışı
4. `docs/02-Architecture/Database.md` — Veritabanı yapısı ve tablolar
5. İlgili modül dokümanı (`docs/03-Modules/`)

---

## Claude Code Okuma Sırası

Claude Code bir göreve başlamadan önce şu sırayı izlemeli:

```
CLAUDE.md
docs/README.md
docs/01-Project/ProjectOverview.md
docs/02-Architecture/CleanArchitecture.md
<ilgili modül veya mimari dosyası>
```

**Kargo görevi geldiğinde:** `docs/03-Modules/Cargo.md`  
**Finans görevi geldiğinde:** `docs/03-Modules/Finance.md`  
**Deploy/publish görevi geldiğinde:** `docs/02-Architecture/ClickOnce.md`  
**Hata ayıklama:** `docs/04-Development/Troubleshooting.md`  
**Mimari karar gündeme geldiğinde:** `docs/05-ADR/`

---

## Dosyalar Arası İlişki

| Kaynak | Referans |
|--------|----------|
| `ClickOnce.md` | `04-Development/GitFlow.md` (release süreci) |
| `Database.md` | `03-Modules/Finance.md`, `03-Modules/Cargo.md` |
| `Authentication.md` | `Authorization.md` |
| `SystemLogs.md` | `02-Architecture/ClickOnce.md` (sürüm log) |
| `LessonsLearned.md` | Tüm `02-Architecture/` dosyaları (bağlam için) |
| `ADR-002-ClickOnce.md` | `ClickOnce.md` |

---

## Eski Dokümanlar

Aşağıdaki dosyalar korunmuştur — yeni yapıya referans verilmiş ancak içerik taşınmıştır:

| Eski Dosya | Yeni Konum |
|------------|------------|
| `docs/Architecture.md` | `docs/02-Architecture/CleanArchitecture.md` |
| `docs/Database.md` | `docs/02-Architecture/Database.md` |
| `docs/Audit-log.md` | `docs/02-Architecture/SystemLogs.md` |
| `docs/update-flow.md` | `docs/02-Architecture/ClickOnce.md` |
| `docs/Dialog-system.md` | `docs/02-Architecture/CleanArchitecture.md` (Dialog bölümü) |
| `docs/Roadmap.md` | `docs/01-Project/Roadmap.md` |
| `docs/session-summary.md` | `docs/01-Project/SprintHistory.md` |
| `docs/progress.md` | `docs/01-Project/SprintHistory.md` |
| `docs/Backup-Recovery-Guide.md` | `docs/04-Development/Troubleshooting.md` |
| `docs/Disaster-Recovery-Plan.md` | `docs/04-Development/Troubleshooting.md` |
| `docs/Emergency-SQL-Commands.md` | `docs/02-Architecture/Database.md` |
| `docs/Release-Checklist.md` | `docs/04-Development/GitFlow.md` |
| `docs/decisions/001-feature-based-yapi.md` | `docs/05-ADR/ADR-001-CleanArchitecture.md` |
