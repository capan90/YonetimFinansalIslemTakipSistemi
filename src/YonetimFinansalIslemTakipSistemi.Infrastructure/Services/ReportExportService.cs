using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// PDF → QuestPDF (Community lisans; iç kullanım aracı, ticari ürün değil)
/// Excel → ClosedXML (MIT lisansı)
/// Borç/Alacak mantığı: Giriş → Borç kolonuna, Çıkış → Alacak kolonuna yazılır.
/// NOT: QuestPDF'de sayfa alt toplamı (running footer) karmaşık olduğundan bu sürümde
///      genel toplam rapor sonunda verilmekte, sayfa alt toplamı eklenmemektedir.
/// </summary>
public class ReportExportService : IReportExportService
{
    private const string AppTitle = "Yönetim Finansal İşlem Takip Sistemi";

    public void ExportToPdf(ReportDto report, string filePath)
    {
        var dateRange = FormatDateRange(report);
        var filterNote = BuildFilterNote(report);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    // Başlık
                    col.Item().Text(AppTitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().Text("Finansal Rapor").FontSize(18).Bold();
                    col.Item().Text(dateRange).FontSize(11).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(filterNote))
                        col.Item().Text(filterNote).FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
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
                            c.RelativeColumn(2);    // İşlem Türü
                            c.RelativeColumn(1.4f); // TL Borç
                            c.RelativeColumn(1.4f); // TL Alacak
                            c.ConstantColumn(32);   // TL Adet
                            c.RelativeColumn(1.4f); // USD Borç
                            c.RelativeColumn(1.4f); // USD Alacak
                            c.ConstantColumn(32);   // USD Adet
                            c.RelativeColumn(1.4f); // EUR Borç
                            c.RelativeColumn(1.4f); // EUR Alacak
                            c.ConstantColumn(32);   // EUR Adet
                        });

                        table.Header(h =>
                        {
                            static IContainer Hdr(IContainer c) =>
                                c.Background(Colors.Grey.Lighten3).Padding(4);

                            h.Cell().Element(Hdr).Text("İşlem Türü").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("TL Borç").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("TL Alacak").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("USD Borç").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("USD Alacak").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("EUR Borç").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("EUR Alacak").Bold().FontSize(8);
                            h.Cell().Element(Hdr).AlignRight().Text("Adet").Bold().FontSize(8);
                        });

                        foreach (var ts in report.TransactionTypeSummaries)
                        {
                            ExtractAmounts(ts, out var tryBorc, out var tryAlacak, out var tryCnt,
                                              out var usdBorc, out var usdAlacak, out var usdCnt,
                                              out var eurBorc, out var eurAlacak, out var eurCnt);

                            static IContainer Row(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4);

                            table.Cell().Element(Row).Text(ts.TypeDisplay).FontSize(8);
                            table.Cell().Element(Row).AlignRight().Text(tryBorc.ToString("N2")).FontSize(8).FontColor(Colors.Green.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(tryAlacak.ToString("N2")).FontSize(8).FontColor(Colors.Red.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(tryCnt.ToString()).FontSize(8);
                            table.Cell().Element(Row).AlignRight().Text(usdBorc.ToString("N2")).FontSize(8).FontColor(Colors.Green.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(usdAlacak.ToString("N2")).FontSize(8).FontColor(Colors.Red.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(usdCnt.ToString()).FontSize(8);
                            table.Cell().Element(Row).AlignRight().Text(eurBorc.ToString("N2")).FontSize(8).FontColor(Colors.Green.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(eurAlacak.ToString("N2")).FontSize(8).FontColor(Colors.Red.Darken2);
                            table.Cell().Element(Row).AlignRight().Text(eurCnt.ToString()).FontSize(8);
                        }
                    });

                    // İşlem detay tablosu — yalnızca istendiğinde
                    if (report.TransactionDetails is { Count: > 0 })
                    {
                        col.Item().Text("İşlem Detayları").FontSize(12).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(60);   // Tarih
                                c.RelativeColumn(3);    // Açıklama
                                c.ConstantColumn(45);   // Tür
                                c.ConstantColumn(36);   // Para Bir.
                                c.RelativeColumn(1.4f); // Borç
                                c.RelativeColumn(1.4f); // Alacak
                                c.RelativeColumn(1.4f); // Bakiye
                            });

                            table.Header(h =>
                            {
                                static IContainer Hdr(IContainer c) =>
                                    c.Background(Colors.Grey.Lighten3).Padding(3);

                                h.Cell().Element(Hdr).Text("Tarih").Bold().FontSize(7);
                                h.Cell().Element(Hdr).Text("Açıklama").Bold().FontSize(7);
                                h.Cell().Element(Hdr).Text("Tür").Bold().FontSize(7);
                                h.Cell().Element(Hdr).Text("Para Bir.").Bold().FontSize(7);
                                h.Cell().Element(Hdr).AlignRight().Text("Borç").Bold().FontSize(7);
                                h.Cell().Element(Hdr).AlignRight().Text("Alacak").Bold().FontSize(7);
                                h.Cell().Element(Hdr).AlignRight().Text("Bakiye").Bold().FontSize(7);
                            });

                            foreach (var d in report.TransactionDetails)
                            {
                                static IContainer Row(IContainer c) =>
                                    c.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(3);

                                table.Cell().Element(Row).Text(d.TransactionDate.ToString("dd.MM.yy")).FontSize(7);
                                table.Cell().Element(Row).Text(d.Description).FontSize(7);
                                table.Cell().Element(Row).Text(d.TypeDisplay).FontSize(7);
                                table.Cell().Element(Row).Text(d.CurrencyDisplay).FontSize(7);
                                table.Cell().Element(Row).AlignRight().Text(d.Borc.ToString("N2")).FontSize(7).FontColor(Colors.Green.Darken2);
                                table.Cell().Element(Row).AlignRight().Text(d.Alacak.ToString("N2")).FontSize(7).FontColor(Colors.Red.Darken2);
                                table.Cell().Element(Row).AlignRight().Text(d.Balance.ToString("N2")).FontSize(7);
                            }
                        });
                    }

                    // Genel toplamlar
                    col.Item().Height(1).Background(Colors.Grey.Lighten2);
                    col.Item().Text("Genel Toplam").FontSize(11).Bold();
                    foreach (var cs in report.CurrencySummaries)
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(50).Text(cs.CurrencyDisplay).Bold().FontSize(9);
                            r.ConstantItem(100).AlignRight()
                                .Text($"Giriş: {cs.TotalInflow:N2}").FontSize(9).FontColor(Colors.Green.Darken2);
                            r.ConstantItem(100).AlignRight()
                                .Text($"Çıkış: {cs.TotalOutflow:N2}").FontSize(9).FontColor(Colors.Red.Darken2);
                            r.ConstantItem(100).AlignRight()
                                .Text($"Net: {cs.NetBalance:N2}").Bold().FontSize(9);
                        });
                    }
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
        row++;

        var filterNote = BuildFilterNote(report);
        if (!string.IsNullOrEmpty(filterNote))
        {
            ws.Cell(row, 1).Value = filterNote;
            ws.Cell(row, 1).Style.Font.Italic    = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
            row++;
        }
        row++;

        // Para birimi özeti
        ws.Cell(row, 1).Value = "Para Birimi Özeti";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] summaryHeaders = ["Para Birimi", "Toplam Giriş", "Toplam Çıkış", "Net Bakiye", "İşlem Adedi"];
        for (var i = 0; i < summaryHeaders.Length; i++)
        {
            ws.Cell(row, i + 1).Value = summaryHeaders[i];
            ws.Cell(row, i + 1).Style.Font.Bold            = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
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

        // İşlem türü tablosu
        ws.Cell(row, 1).Value = "İşlem Türü Bazında Toplam";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] typeHeaders = [
            "İşlem Türü",
            "TL Borç", "TL Alacak", "TL Adet",
            "USD Borç", "USD Alacak", "USD Adet",
            "EUR Borç", "EUR Alacak", "EUR Adet"
        ];
        for (var i = 0; i < typeHeaders.Length; i++)
        {
            ws.Cell(row, i + 1).Value = typeHeaders[i];
            ws.Cell(row, i + 1).Style.Font.Bold            = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }
        row++;

        foreach (var ts in report.TransactionTypeSummaries)
        {
            ExtractAmounts(ts,
                out var tryBorc, out var tryAlacak, out var tryCnt,
                out var usdBorc, out var usdAlacak, out var usdCnt,
                out var eurBorc, out var eurAlacak, out var eurCnt);

            ws.Cell(row, 1).Value  = ts.TypeDisplay;
            ws.Cell(row, 2).Value  = (double)tryBorc;
            ws.Cell(row, 3).Value  = (double)tryAlacak;
            ws.Cell(row, 4).Value  = tryCnt;
            ws.Cell(row, 5).Value  = (double)usdBorc;
            ws.Cell(row, 6).Value  = (double)usdAlacak;
            ws.Cell(row, 7).Value  = usdCnt;
            ws.Cell(row, 8).Value  = (double)eurBorc;
            ws.Cell(row, 9).Value  = (double)eurAlacak;
            ws.Cell(row, 10).Value = eurCnt;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        row += 2;

        // İşlem detay tablosu — yalnızca istendiğinde
        if (report.TransactionDetails is { Count: > 0 })
        {
            ws.Cell(row, 1).Value = "İşlem Detayları";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            string[] detailHeaders = ["Tarih", "Açıklama", "Tür", "Para Bir.", "Borç", "Alacak", "Bakiye"];
            for (var i = 0; i < detailHeaders.Length; i++)
            {
                ws.Cell(row, i + 1).Value = detailHeaders[i];
                ws.Cell(row, i + 1).Style.Font.Bold            = true;
                ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }
            row++;

            foreach (var d in report.TransactionDetails)
            {
                ws.Cell(row, 1).Value = d.TransactionDate.ToString("dd.MM.yyyy");
                ws.Cell(row, 2).Value = d.Description;
                ws.Cell(row, 3).Value = d.TypeDisplay;
                ws.Cell(row, 4).Value = d.CurrencyDisplay;
                ws.Cell(row, 5).Value = (double)d.Borc;
                ws.Cell(row, 6).Value = (double)d.Alacak;
                ws.Cell(row, 7).Value = (double)d.Balance;
                ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
            row += 2;
        }

        // Genel toplamlar
        ws.Cell(row, 1).Value = "Genel Toplam";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        string[] totalHeaders = ["Para Birimi", "Toplam Giriş", "Toplam Çıkış", "Net Bakiye"];
        for (var i = 0; i < totalHeaders.Length; i++)
        {
            ws.Cell(row, i + 1).Value = totalHeaders[i];
            ws.Cell(row, i + 1).Style.Font.Bold            = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.LightSteelBlue;
        }
        row++;

        foreach (var cs in report.CurrencySummaries)
        {
            ws.Cell(row, 1).Value = cs.CurrencyDisplay;
            ws.Cell(row, 2).Value = (double)cs.TotalInflow;
            ws.Cell(row, 3).Value = (double)cs.TotalOutflow;
            ws.Cell(row, 4).Value = (double)cs.NetBalance;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    // İşlem türü bazında Borç ve Alacak tutarlarını ayır
    private static void ExtractAmounts(
        TransactionTypeSummaryDto ts,
        out decimal tryBorc,  out decimal tryAlacak, out int tryCnt,
        out decimal usdBorc,  out decimal usdAlacak, out int usdCnt,
        out decimal eurBorc,  out decimal eurAlacak, out int eurCnt)
    {
        tryBorc = tryAlacak = usdBorc = usdAlacak = eurBorc = eurAlacak = 0m;
        tryCnt  = usdCnt  = eurCnt  = 0;

        var isInflow = ts.TransactionType.GetFinancialDirection() == FinancialDirection.Inflow;

        foreach (var ca in ts.AmountsByCurrency)
        {
            switch (ca.Currency)
            {
                case CurrencyType.TRY:
                    if (isInflow) tryBorc = ca.TotalAmount; else tryAlacak = ca.TotalAmount;
                    tryCnt = ca.Count;
                    break;
                case CurrencyType.USD:
                    if (isInflow) usdBorc = ca.TotalAmount; else usdAlacak = ca.TotalAmount;
                    usdCnt = ca.Count;
                    break;
                case CurrencyType.EUR:
                    if (isInflow) eurBorc = ca.TotalAmount; else eurAlacak = ca.TotalAmount;
                    eurCnt = ca.Count;
                    break;
            }
        }
    }

    private static string BuildFilterNote(ReportDto report)
    {
        var parts = new List<string>();
        if (report.FilterTransactionType.HasValue)
            parts.Add($"Tür: {(report.FilterTransactionType == TransactionType.Giris ? "Giriş" : "Çıkış")}");
        if (report.FilterCurrencyType.HasValue)
            parts.Add($"Para Birimi: {report.FilterCurrencyType}");
        if (!string.IsNullOrWhiteSpace(report.FilterDescription))
            parts.Add($"Açıklama: \"{report.FilterDescription}\"");
        return parts.Count == 0 ? string.Empty : "Filtre: " + string.Join(" | ", parts);
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
