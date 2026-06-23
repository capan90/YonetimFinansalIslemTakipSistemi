namespace YonetimFinansalIslemTakipSistemi.Application.Features.Analysis.Queries.GetDashboard;

public class DashboardDto
{
    // Üst özet kartları — tüm para birimleri üzerinden birleşik toplamlar
    public decimal TotalAlacak { get; init; }
    public decimal TotalBorc   { get; init; }
    public decimal NetFark     { get; init; }
    public int     TotalCount  { get; init; }
    // En büyük tek işlem tutarları (para birimi fark etmeksizin tutar büyüklüğüne göre)
    public decimal MaxGiris    { get; init; }
    public decimal MaxCikis    { get; init; }

    // Para birimi bazlı kartlar — para birimi filtresi Tümü ise üç kart, belirli ise tek kart
    public List<DashboardCurrencyCardDto> CurrencyCards { get; init; } = new();

    // Günlük trend — tarih bazında gruplama (grafik kütüphanesi sonraki sprintte eklenecek)
    public List<DailyTrendDto> DailyTrend { get; init; } = new();

    // Son işlemler — filtreye uyan en güncel 20 kayıt (tarih DESC)
    public List<RecentTransactionDto> RecentTransactions { get; init; } = new();
}

public record DashboardCurrencyCardDto(
    string  CurrencyDisplay,
    decimal TotalAlacak,
    decimal TotalBorc,
    decimal NetFark,
    int     Count);

/// <summary>
/// Günlük özet satırı. Grafik kütüphanesi ekleneceğinde bu DTO'yu doğrudan serisi olarak kullanın.
/// NOT: Bu sprintte yalnızca DataGrid ile gösterilmektedir; grafik entegrasyonu sonraki sprintte yapılacak.
/// </summary>
public record DailyTrendDto(
    string  DateDisplay,  // dd.MM.yyyy
    decimal TotalAlacak,
    decimal TotalBorc,
    decimal NetFark);

public record RecentTransactionDto(
    string  DateDisplay,
    string  Description,
    string  TypeDisplay,
    string  CurrencyDisplay,
    decimal Borc,
    decimal Alacak);
