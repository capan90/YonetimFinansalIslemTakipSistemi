using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// PDF → QuestPDF (Community lisans; iç kullanım aracı, ticari ürün değil)
/// Excel → ClosedXML (MIT lisansı)
/// </summary>
public class ReportExportService : IReportExportService
{
    private const string AppTitle = "Yönetim Finansal İşlem Takip Sistemi";

    public void ExportToPdf(ReportDto report, string filePath)
    {
        var dateRange = FormatDateRange(report);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    // Başlık bölümü
                    col.Item().Text(AppTitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Finansal Rapor").FontSize(18).Bold();
                    col.Item().Text(dateRange).FontSize(11).FontColor(Colors.Grey.Darken1);
                    col.Item().Height(1).Background(Colors.Grey.Lighten2);

                    // Para birimi özet kartları
                    col.Item().Text("Para Birimi Özeti").FontSize(12).Bold();
                    col.Item().Row(row =>
                    {
                        foreach (var cs in report.CurrencySummaries)
                        {
                            if (cs != report.CurrencySummaries.First())
                                row.ConstantItem(8);

                            row.RelativeItem()
                                .Border(1).BorderColor(Colors.Grey.Lighten2)
                                .Padding(8)
                                .Column(card =>
                                {
                                    card.Spacing(3);
                                    card.Item().Text(cs.CurrencyDisplay).Bold().FontSize(13);
                                    card.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Toplam Giriş:").FontSize(9);
                                        r.AutoItem().Text(cs.TotalInflow.ToString("N2"))
                                            .FontColor(Colors.Green.Darken2).FontSize(9);
                                    });
                                    card.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Toplam Çıkış:").FontSize(9);
                                        r.AutoItem().Text(cs.TotalOutflow.ToString("N2"))
                                            .FontColor(Colors.Red.Darken2).FontSize(9);
                                    });
                                    card.Item().Height(1).Background(Colors.Grey.Lighten2);
                                    card.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text("Net Bakiye:").Bold().FontSize(9);
                                        r.AutoItem().Text(cs.NetBalance.ToString("N2")).Bold().FontSize(9);
                                    });
                                });
                        }
                    });

                    // İşlem türü tablosu
                    col.Item().Text("İşlem Türü Bazında Toplam").FontSize(12).Bold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);   // İşlem Türü
                            c.RelativeColumn(1.5f); // TL Tutar
                            c.ConstantColumn(38);  // TL Adet
                            c.RelativeColumn(1.5f); // USD Tutar
                            c.ConstantColumn(38);  // USD Adet
                            c.RelativeColumn(1.5f); // EUR Tutar
                            c.ConstantColumn(38);  // EUR Adet
                        });

                        // Tablo başlığı
                        table.Header(h =>
                        {
                            static IContainer Hdr(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3).Padding(5);

                            h.Cell().Element(Hdr).Text("İşlem Türü").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("TL Tutar").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("USD Tutar").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("EUR Tutar").Bold().FontSize(9);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(9);
                        });

                        foreach (var ts in report.TransactionTypeSummaries)
                        {
                            ExtractAmounts(ts, out var tryAmt, out var tryCnt,
                                              out var usdAmt, out var usdCnt,
                                              out var eurAmt, out var eurCnt);

                            static IContainer Row(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5);

                            table.Cell().Element(Row).Text(ts.TypeDisplay).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(tryAmt.ToString("N2")).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(tryCnt.ToString()).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(usdAmt.ToString("N2")).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(usdCnt.ToString()).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(eurAmt.ToString("N2")).FontSize(9);
                            table.Cell().Element(Row).AlignRight().Text(eurCnt.ToString()).FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Sayfa ").FontSize(9);
                    t.CurrentPageNumber().FontSize(9);
                    t.Span(" / ").FontSize(9);
                    t.TotalPages().FontSize(9);
                });
            });
        }).GeneratePdf(filePath);
    }

    public void ExportToExcel(ReportDto report, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws  = wb.Worksheets.Add("Rapor");
        var row = 1;

        // Başlık
        ws.Cell(row, 1).Value = AppTitle;
        ws.Cell(row, 1).Style.Font.Bold     = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;
        ws.Cell(row, 1).Value = "Finansal Rapor — " + FormatDateRange(report);
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
        row += 2;

        // Para birimi özeti — başlık
        ws.Cell(row, 1).Value = "Para Birimi Özeti";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] summaryHeaders = ["Para Birimi", "Toplam Giriş", "Toplam Çıkış", "Net Bakiye", "İşlem Adedi"];
        for (var i = 0; i < summaryHeaders.Length; i++)
        {
            ws.Cell(row, i + 1).Value = summaryHeaders[i];
            ws.Cell(row, i + 1).Style.Font.Bold             = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor  = XLColor.LightGray;
        }
        row++;

        foreach (var cs in report.CurrencySummaries)
        {
            ws.Cell(row, 1).Value = cs.CurrencyDisplay;
            ws.Cell(row, 2).Value = (double)cs.TotalInflow;
            ws.Cell(row, 3).Value = (double)cs.TotalOutflow;
            ws.Cell(row, 4).Value = (double)cs.NetBalance;
            ws.Cell(row, 5).Value = cs.TransactionCount;

            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        row += 2;

        // İşlem türü tablosu — başlık
        ws.Cell(row, 1).Value = "İşlem Türü Bazında Toplam";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] typeHeaders = ["İşlem Türü", "TL Tutar", "TL Adet", "USD Tutar", "USD Adet", "EUR Tutar", "EUR Adet"];
        for (var i = 0; i < typeHeaders.Length; i++)
        {
            ws.Cell(row, i + 1).Value = typeHeaders[i];
            ws.Cell(row, i + 1).Style.Font.Bold            = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        foreach (var ts in report.TransactionTypeSummaries)
        {
            ExtractAmounts(ts, out var tryAmt, out var tryCnt,
                              out var usdAmt, out var usdCnt,
                              out var eurAmt, out var eurCnt);

            ws.Cell(row, 1).Value = ts.TypeDisplay;
            ws.Cell(row, 2).Value = (double)tryAmt;
            ws.Cell(row, 3).Value = tryCnt;
            ws.Cell(row, 4).Value = (double)usdAmt;
            ws.Cell(row, 5).Value = usdCnt;
            ws.Cell(row, 6).Value = (double)eurAmt;
            ws.Cell(row, 7).Value = eurCnt;

            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    // Para birimi tutarlarını bir TransactionTypeSummaryDto'dan çıkarır
    private static void ExtractAmounts(
        TransactionTypeSummaryDto ts,
        out decimal tryAmt, out int tryCnt,
        out decimal usdAmt, out int usdCnt,
        out decimal eurAmt, out int eurCnt)
    {
        tryAmt = usdAmt = eurAmt = 0m;
        tryCnt = usdCnt = eurCnt = 0;

        foreach (var ca in ts.AmountsByCurrency)
        {
            switch (ca.Currency)
            {
                case CurrencyType.TRY: tryAmt = ca.TotalAmount; tryCnt = ca.Count; break;
                case CurrencyType.USD: usdAmt = ca.TotalAmount; usdCnt = ca.Count; break;
                case CurrencyType.EUR: eurAmt = ca.TotalAmount; eurCnt = ca.Count; break;
            }
        }
    }

    private static string FormatDateRange(ReportDto report)
    {
        if (report.StartDate.HasValue && report.EndDate.HasValue)
            return $"{report.StartDate:dd.MM.yyyy} – {report.EndDate:dd.MM.yyyy}";
        if (report.StartDate.HasValue)
            return $"{report.StartDate:dd.MM.yyyy} tarihinden itibaren";
        if (report.EndDate.HasValue)
            return $"{report.EndDate:dd.MM.yyyy} tarihine kadar";
        return "Tüm zamanlar";
    }
}
