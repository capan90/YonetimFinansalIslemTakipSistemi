namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoDashboard;

public class GetCargoDashboardQuery
{
    /// <summary>Gelen/Giden grafik için tarih aralığı başlangıcı (varsayılan: son 30 gün).</summary>
    public DateTime ChartDateFrom { get; set; }
    /// <summary>Gelen/Giden grafik için tarih aralığı bitişi (varsayılan: bugün).</summary>
    public DateTime ChartDateTo   { get; set; }
}
