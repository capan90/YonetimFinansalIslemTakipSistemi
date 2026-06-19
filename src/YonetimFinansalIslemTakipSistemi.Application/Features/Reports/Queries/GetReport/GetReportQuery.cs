namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

public class GetReportQuery
{
    /// <summary>Dahil; null ise açık uç (başlangıçsız aralık).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Dahil; null ise açık uç (bitişsiz aralık).</summary>
    public DateTime? EndDate { get; set; }
}
