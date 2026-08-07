namespace YonetimFinansalIslemTakipSistemi.UI.Common.Shell;

/// <summary>
/// Kabukta sekme olarak açılabilen ekranların benzersiz kimliği.
///
/// Enum kullanılıyor çünkü sekme kimliği bir DEĞER, serbest metin değil:
/// aynı ekranın ikinci kez açılmasını engellemek için karşılaştırılabilir
/// olmalı ve yazım hatası derleme zamanında yakalanmalı.
///
/// Modal kalan ekranlar (Login, diyaloglar, Excel sihirbazları, Rapor
/// Önizleme, Sistem Log Detayı, ayar pencereleri) burada YOKTUR — onlar
/// sekme değil, gerçek kesinti gerektiren pencerelerdir.
/// </summary>
public enum ScreenKey
{
    // ── Finans ────────────────────────────────────────────────────────────
    CashTransactions = 1,
    Analysis,
    Reports,
    ExchangeRates,

    // ── Kargo ─────────────────────────────────────────────────────────────
    CargoDashboard,
    IncomingCargo,
    OutgoingCargo,

    /// <summary>
    /// PARAMETRELİ ekran: seçili bir kargo gönderisi üzerinde çalışır.
    /// Navigasyon rayında görünmez; kargo listesindeki "Operasyon" butonundan
    /// açılır. Farklı kargolar ayrı sekmelerde açılabilir.
    /// </summary>
    CargoOperationCenter,

    CompanyDirectory,
    CargoCompanies,
    WhatsAppContacts,
    MailContacts,

    // ── Yönetim / sistem ──────────────────────────────────────────────────
    Users,
    Permissions,
    AuditLog,
    SystemLogs,
    SystemHealth,
}
