namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;

/// <summary>Rapor sonucu — filtreli satırlar ve özet istatistikler.</summary>
public class CargoReportDto
{
    public IReadOnlyList<CargoReportRowDto> Rows           { get; set; } = [];
    public int                              TotalCount     { get; set; }
    public int                              IncomingCount  { get; set; }
    public int                              OutgoingCount  { get; set; }
    public int                              PendingCount   { get; set; }
    public int                              DeliveredCount { get; set; }
    /// <summary>PDF başlığında gösterilecek aktif filtre özeti.</summary>
    public string                           FilterSummary  { get; set; } = "";
    public DateTime?                        DateFrom       { get; set; }
    public DateTime?                        DateTo         { get; set; }
}
