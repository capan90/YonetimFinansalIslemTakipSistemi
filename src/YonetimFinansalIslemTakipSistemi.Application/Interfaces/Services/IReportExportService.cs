using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Rapor dışa aktarma sözleşmesi. Infrastructure katmanı QuestPDF ve ClosedXML ile implemente eder.
/// </summary>
public interface IReportExportService
{
    void ExportToPdf(ReportDto report, string filePath);
    void ExportToExcel(ReportDto report, string filePath);
}
