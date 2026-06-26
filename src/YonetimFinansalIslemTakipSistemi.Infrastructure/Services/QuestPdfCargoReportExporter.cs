using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// ICargoReportPdfExporter: QuestPDF 2024 ile A4 Yatay kargo raporu PDF'i.
/// Community lisans — iç kullanım aracı, ticari ürün değil.
/// </summary>
public class QuestPdfCargoReportExporter : ICargoReportPdfExporter
{
    static QuestPdfCargoReportExporter()
        => QuestPDF.Settings.License = LicenseType.Community;

    private const string HeaderBg = "#1A3354";
    private const string AltRowBg = "#EEF4FA";
    private const string SubText  = "#64748B";

    public byte[] Export(CargoReportDto report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(12, Unit.Millimetre);
                page.DefaultTextStyle(ts => ts.FontFamily("Arial").FontSize(8));

                // ── Sayfa Başlığı (her sayfada tekrar eder) ──────────────
                page.Header().Column(col =>
                {
                    col.Item()
                        .Background(HeaderBg)
                        .Padding(8)
                        .Row(row =>
                        {
                            row.RelativeItem()
                               .Text("KARGO RAPORU")
                               .FontColor(Colors.White)
                               .Bold()
                               .FontSize(13);

                            row.ConstantItem(280)
                               .AlignRight()
                               .Text(report.FilterSummary)
                               .FontColor(Colors.White)
                               .FontSize(7);
                        });

                    // Özet istatistik satırı
                    col.Item()
                        .Background("#EEF4FA")
                        .Padding(5)
                        .Row(row =>
                        {
                            row.RelativeItem()
                               .Text($"Toplam: {report.TotalCount}")
                               .FontSize(8).Bold();
                            row.RelativeItem()
                               .Text($"Gelen: {report.IncomingCount}")
                               .FontSize(8).Bold();
                            row.RelativeItem()
                               .Text($"Giden: {report.OutgoingCount}")
                               .FontSize(8).Bold();
                            row.RelativeItem()
                               .Text($"Bekleyen: {report.PendingCount}")
                               .FontSize(8).Bold();
                            row.RelativeItem()
                               .Text($"Teslim: {report.DeliveredCount}")
                               .FontSize(8).Bold();
                        });
                });

                // ── İçerik ───────────────────────────────────────────────
                page.Content().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1.8f); // Kargo No
                        cols.RelativeColumn(1.0f); // Yön
                        cols.RelativeColumn(1.6f); // Tarih
                        cols.RelativeColumn(3.2f); // Firma / Karşı Taraf
                        cols.RelativeColumn(2.2f); // Kargo Firması
                        cols.RelativeColumn(2.0f); // Takip No
                        cols.RelativeColumn(1.6f); // Araç Plaka
                        cols.RelativeColumn(1.9f); // Durum
                        cols.RelativeColumn(1.9f); // Bildirim
                        cols.RelativeColumn(1.3f); // Öncelik
                    });

                    // Tablo başlığı — QuestPDF tarafından her sayfada otomatik tekrar eder
                    table.Header(header =>
                    {
                        void H(string text) => header.Cell()
                            .Background(HeaderBg)
                            .Padding(4)
                            .Text(text)
                            .FontColor(Colors.White)
                            .Bold()
                            .FontSize(7.5f);

                        H("Kargo No");
                        H("Yön");
                        H("Tarih");
                        H("Firma / Karşı Taraf");
                        H("Kargo Firması");
                        H("Takip No");
                        H("Araç Plaka");
                        H("Durum");
                        H("Bildirim");
                        H("Öncelik");
                    });

                    // Veri satırları
                    bool alt = false;
                    foreach (var row in report.Rows)
                    {
                        var bg = alt ? AltRowBg : "#FFFFFF";
                        alt = !alt;

                        void D(string? text) => table.Cell()
                            .Background(bg)
                            .BorderBottom(0.3f)
                            .BorderColor("#D1D5DB")
                            .PaddingHorizontal(4)
                            .PaddingVertical(3)
                            .Text(text ?? "—")
                            .FontSize(7.5f);

                        D(row.ShipmentNumber);
                        D(row.DirectionDisplay);
                        D(row.ShipmentDate.ToString("dd.MM.yyyy"));
                        D(row.Party);
                        D(row.CargoCompanyName);
                        D(row.TrackingNumber);
                        D(row.VehiclePlate);
                        D(row.StatusDisplay);
                        D(row.NotificationStatusDisplay);
                        D(row.PriorityDisplay);
                    }

                    if (report.Rows.Count == 0)
                    {
                        table.Cell()
                            .ColumnSpan(10)
                            .Padding(12)
                            .AlignCenter()
                            .Text("Seçili filtrelerle eşleşen kayıt bulunamadı.")
                            .FontColor(SubText)
                            .Italic();
                    }
                });

                // ── Sayfa Altı ─────────────────────────────────────────────
                page.Footer()
                    .AlignRight()
                    .Text(x =>
                    {
                        x.Span("Sayfa ").FontSize(7).FontColor(SubText);
                        x.CurrentPageNumber().FontSize(7).FontColor(SubText);
                        x.Span(" / ").FontSize(7).FontColor(SubText);
                        x.TotalPages().FontSize(7).FontColor(SubText);
                    });
            });
        }).GeneratePdf();
    }
}
