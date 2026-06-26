namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;

/// <summary>Grafik çubuğu: etiket, sayı, renk (#HEX).</summary>
public record CargoDashboardChartItem(string Label, int Value, string Color);

/// <summary>Son hareketler listesi için özet satır.</summary>
public class CargoDashboardRecentDto
{
    public Guid     Id                         { get; set; }
    public string?  ShipmentNumber             { get; set; }
    public string   DirectionDisplay           { get; set; } = "";
    public DateTime ShipmentDate               { get; set; }
    public string?  Party                      { get; set; }
    public string?  CargoCompanyName           { get; set; }
    public string   StatusDisplay              { get; set; } = "";
    public string   NotificationStatusDisplay  { get; set; } = "";
    public string   PriorityDisplay            { get; set; } = "";
}

/// <summary>Dashboard özet DTO — kart sayaçları, grafikler, son 10 hareket.</summary>
public class CargoDashboardDto
{
    // ── Özet kartlar ──────────────────────────────────────────────────────
    public int TodayIncoming       { get; set; }
    public int TodayOutgoing       { get; set; }
    public int Pending             { get; set; }
    public int NotificationPending { get; set; }
    public int UrgentPending       { get; set; }
    public int TodayDelivered      { get; set; }

    // ── Grafikler ──────────────────────────────────────────────────────────
    public IReadOnlyList<CargoDashboardChartItem> DirectionChart { get; set; } = [];
    public IReadOnlyList<CargoDashboardChartItem> StatusChart    { get; set; } = [];
    public IReadOnlyList<CargoDashboardChartItem> CompanyChart   { get; set; } = [];

    // ── Son 10 hareket ────────────────────────────────────────────────────
    public IReadOnlyList<CargoDashboardRecentDto> RecentShipments { get; set; } = [];
}
