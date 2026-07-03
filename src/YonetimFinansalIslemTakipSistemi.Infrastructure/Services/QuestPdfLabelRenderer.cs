using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text;
using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// ILabelRenderer: QuestPDF ile A4 kurumsal kargo etiketi.
/// Community lisans — iç kullanım aracı, ticari ürün değil.
/// </summary>
public class QuestPdfLabelRenderer : ILabelRenderer
{
    static QuestPdfLabelRenderer()
        => QuestPDF.Settings.License = LicenseType.Community;

    // ── Kurumsal renk paleti ────────────────────────────────────────────
    private const string HeaderText = "#1A3354"; // koyu lacivert
    private const string SectionText = "#1A3354"; // koyu lacivert
    private const string SubText    = "#334155"; // slate-700 (daha koyu okunaklı)
    private const string BorderGrey = "#CBD5E1"; // slate-300
    private const string LightBg    = "#F8FAFC"; // slate-50

    public byte[] Render(CargoLabelModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(15, Unit.Millimetre);
                page.DefaultTextStyle(ts =>
                    ts.FontSize(12).FontFamily("Segoe UI")); // Temel yazı boyutu 12 yapıldı

                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    RenderHeader(col, model);
                    RenderSenderSection(col, model);
                    RenderReceiverSection(col, model);
                    RenderFooter(col, model);
                });
            });
        })
        .GeneratePdf();
    }

    // ── Header: logo (varsa) + başlık ──────────────

    private static void RenderHeader(ColumnDescriptor col, CargoLabelModel model)
    {
        var isErdemsoft = model.SenderCompanyName != null &&
                          (model.SenderCompanyName.Contains("Erdemsoft", StringComparison.OrdinalIgnoreCase) ||
                           model.SenderCompanyName.Contains("Erdem Soft", StringComparison.OrdinalIgnoreCase));

        col.Item()
           .BorderBottom(2).BorderColor(HeaderText)
           .PaddingBottom(10)
           .Row(row =>
           {
               if (isErdemsoft)
               {
                   var logoBytes = TryLoadLogo(model.SenderLogoPath);
                   if (logoBytes != null)
                   {
                       row.ConstantItem(40, Unit.Millimetre)
                          .AlignMiddle()
                          .Image(logoBytes).FitWidth();
                   }
                   else
                   {
                       // Logo bulunamazsa yüksek kontrastlı ve kalın yazı
                       row.ConstantItem(45, Unit.Millimetre)
                          .AlignMiddle()
                          .Column(c =>
                          {
                              c.Item().Text("ERDEMSOFT").FontSize(18).ExtraBold().FontColor(HeaderText);
                              c.Item().Text("TEKSTİL A.Ş.").FontSize(12).Bold().FontColor(HeaderText);
                          });
                   }
                   row.ConstantItem(10, Unit.Millimetre);
               }

               // Başlık (orta)
               row.RelativeItem().AlignMiddle().Column(c =>
               {
                   var titleText = isErdemsoft ? "ERDEMSOFT KARGO ETİKETİ" : "KARGO ETİKETİ";
                   c.Item()
                    .Text(titleText)
                    .FontSize(22).Bold().FontColor(HeaderText);
                   c.Item()
                    .Text($"İç Kargo No: {model.ShipmentNumber ?? "—"}")
                    .FontSize(13).Bold().FontColor(SubText);
               });
           });
    }

    // ── Gönderici: firma bilgileri + QR ──────────────────────

    private static void RenderSenderSection(ColumnDescriptor col, CargoLabelModel model)
    {
        SectionHeader(col, "GÖNDERİCİ");

        col.Item()
           .Border(1).BorderColor(BorderGrey)
           .Background(LightBg)
           .Padding(12)
           .Row(row =>
           {
               // Firma bilgileri (sol) - yazı boyutları büyütüldü
               row.RelativeItem().Column(c =>
               {
                   c.Spacing(3);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyName))
                       c.Item()
                        .Text(model.SenderCompanyName)
                        .FontSize(14).Bold().FontColor(HeaderText); // 12 -> 14 yapıldı

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyAddress))
                       c.Item()
                        .Text(model.SenderCompanyAddress)
                        .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı

                   var senderLoc = BuildLocation(
                       model.SenderCompanyDistrict, model.SenderCompanyCity);
                   if (!string.IsNullOrWhiteSpace(senderLoc))
                       c.Item()
                        .Text(senderLoc)
                        .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyPhone))
                       c.Item()
                        .Text($"Tel: {model.SenderCompanyPhone}")
                        .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı

                   c.Item().PaddingTop(4);

                   if (!string.IsNullOrWhiteSpace(model.Sender))
                       c.Item()
                        .Text($"Yönlendiren: {model.Sender}")
                        .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı

                   c.Item()
                    .Text($"Tarih: {model.CreatedDate:dd.MM.yyyy}")
                    .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı

                   if (!string.IsNullOrWhiteSpace(model.CargoCompany))
                       c.Item()
                        .Text($"Kargo: {model.CargoCompany}")
                        .FontSize(11).FontColor(SubText); // 10 -> 11 yapıldı
               });

               row.ConstantItem(10, Unit.Millimetre);

               // Canlı QR Kod
               byte[] qrBytes = GenerateQrCode(model);
               row.ConstantItem(45, Unit.Millimetre)
                  .AlignMiddle()
                  .Image(qrBytes).FitWidth();
           });
    }

    // ── Alıcı: snapshot verisi ───────────

    private static void RenderReceiverSection(ColumnDescriptor col, CargoLabelModel model)
    {
        SectionHeader(col, "ALICI");

        col.Item()
           .Border(1).BorderColor(BorderGrey)
           .Padding(12)
           .Column(c =>
           {
               c.Spacing(4);

               if (!string.IsNullOrWhiteSpace(model.ReceiverCompany))
                   c.Item()
                    .Text(model.ReceiverCompany.ToUpperInvariant())
                    .FontSize(18).Bold().FontColor(HeaderText); // 16 -> 18 yapıldı

               var attentionLine = AttentionHelper.FormatAttentionLine(model.Attention);
               if (attentionLine != "Muhattap: -")
               {
                   c.Item().PaddingTop(2);
                   c.Item().Text(attentionLine).Bold().FontSize(15).FontColor(SectionText); // 13 -> 15 yapıldı
               }

               if (!string.IsNullOrWhiteSpace(model.Address))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Adres: ").FontColor(SubText).FontSize(13); // 11 -> 13 yapıldı
                       r.RelativeItem().Text(model.Address).FontSize(13); // 11 -> 13 yapıldı
                   });

               var location = BuildLocation(model.District, model.City);
               if (!string.IsNullOrWhiteSpace(location))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("İlçe / İl: ").FontColor(SubText).FontSize(13); // 11 -> 13 yapıldı
                       r.RelativeItem().Text(location).FontSize(13); // 11 -> 13 yapıldı
                   });

               if (!string.IsNullOrWhiteSpace(model.Phone))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Tel: ").FontColor(SubText).FontSize(13); // 11 -> 13 yapıldı
                       r.RelativeItem().Text(model.Phone).FontSize(13); // 11 -> 13 yapıldı
                   });
           });
    }

    // ── Footer: Takip No + Plaka + Vektörel Barkod ────────────────────

    private static void RenderFooter(ColumnDescriptor col, CargoLabelModel model)
    {
        col.Item()
           .Border(1).BorderColor(BorderGrey)
           .Background(LightBg)
           .Padding(12)
           .Column(f =>
           {
               f.Spacing(8);

               // Takip No + Araç Plaka yan yana
               var hasTracking = !string.IsNullOrWhiteSpace(model.TrackingNumber);
               var hasPlate    = !string.IsNullOrWhiteSpace(model.VehiclePlate);

               if (hasTracking || hasPlate)
                   f.Item().Row(r =>
                   {
                       if (hasTracking)
                           r.RelativeItem().Column(c =>
                           {
                               c.Item().Text("TAKİP NO").FontSize(10).FontColor(SubText).Bold();
                               c.Item().Text(model.TrackingNumber!).FontSize(15).Bold().FontColor(HeaderText);
                           });

                       if (hasPlate)
                           r.AutoItem().Column(c =>
                           {
                               c.Item().Text("ARAÇ PLAKA").FontSize(10).FontColor(SubText).Bold();
                               c.Item().Text(model.VehiclePlate!).FontSize(15).Bold().FontColor(HeaderText);
                           });
                   });

               // Vektörel Barkod (Code 128)
               if (!string.IsNullOrWhiteSpace(model.ShipmentNumber))
               {
                   f.Item()
                    .Border(1).BorderColor(BorderGrey)
                    .Background(Colors.White)
                    .Padding(8)
                    .Column(bc =>
                    {
                        bc.Spacing(4);

                        try
                        {
                            var svgContent = GenerateBarcodeSvg(model.ShipmentNumber);
                            bc.Item()
                              .Height(25, Unit.Millimetre)
                              .Svg(svgContent);
                        }
                        catch
                        {
                            // Barkod üretimi başarısız olursa fallback
                            bc.Item()
                              .Height(25, Unit.Millimetre)
                              .AlignCenter()
                              .AlignMiddle()
                              .Text("Barkod Oluşturulamadı")
                              .FontSize(11).Bold().FontColor(Colors.Red.Medium);
                        }

                        bc.Item().AlignCenter()
                                 .Text(model.ShipmentNumber)
                                 .FontSize(16).Bold().FontColor(HeaderText);
                        bc.Item().AlignCenter()
                                 .Text("KARGO BARKODU")
                                 .FontSize(8).FontColor(SubText);
                    });
               }
           });
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item()
           .PaddingTop(8)
           .PaddingBottom(4)
           .BorderBottom(1.5f).BorderColor(HeaderText)
           .Text(title)
           .FontSize(12).Bold().FontColor(HeaderText); // 11 -> 12 yapıldı
    }

    private static string? BuildLocation(string? district, string? city)
    {
        var parts = new[] { district, city?.ToUpperInvariant() }
                        .Where(s => !string.IsNullOrWhiteSpace(s));
        var result = string.Join(" / ", parts);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static byte[]? TryLoadLogo(string? logoPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(logoPath)) return null;
            var fullPath = Path.IsPathRooted(logoPath)
                ? logoPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logoPath);
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] GenerateQrCode(CargoLabelModel model)
    {
        var senderLoc = BuildLocation(model.SenderCompanyDistrict, model.SenderCompanyCity);
        var senderFullAddress = !string.IsNullOrWhiteSpace(senderLoc)
            ? $"{model.SenderCompanyAddress}, {senderLoc}"
            : model.SenderCompanyAddress ?? "—";

        var receiverLoc = BuildLocation(model.District, model.City);
        var receiverFullAddress = !string.IsNullOrWhiteSpace(receiverLoc)
            ? $"{model.Address}, {receiverLoc}"
            : model.Address ?? "—";

        var sb = new StringBuilder();
        sb.AppendLine("GONDEREN");
        sb.AppendLine($"Firma: {model.SenderCompanyName ?? "—"}");
        sb.AppendLine($"Adres: {senderFullAddress}");
        sb.AppendLine($"Yonlendiren: {model.Sender ?? "—"}");
        sb.AppendLine();
        sb.AppendLine("ALICI");
        sb.AppendLine($"Firma: {model.ReceiverCompany ?? "—"}");
        sb.AppendLine($"Adres: {receiverFullAddress}");
        sb.AppendLine($"Dikkatine: {model.Attention ?? "—"}");

        var qrContent = sb.ToString().TrimEnd();

        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    private static string GenerateBarcodeSvg(string value)
    {
        var barcode = Barcoder.Code128.Code128Encoder.Encode(value);
        var width = barcode.Bounds.X;
        var height = 50; // standard aspect ratio height

        var sb = new StringBuilder();
        sb.Append($"<svg viewBox=\"0 0 {width} {height}\" xmlns=\"http://www.w3.org/2000/svg\">");
        sb.Append($"<rect width=\"{width}\" height=\"{height}\" fill=\"white\" />");

        for (int i = 0; i < width; i++)
        {
            if (barcode.At(i, 0))
            {
                sb.Append($"<rect x=\"{i}\" y=\"0\" width=\"1\" height=\"{height}\" fill=\"black\" />");
            }
        }
        sb.Append("</svg>");
        return sb.ToString();
    }
}
