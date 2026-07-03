using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.IO;
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
    private const string SubText    = "#475569"; // slate-600
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
                    ts.FontSize(11).FontFamily("Segoe UI"));

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
                       // Logo bulunamazsa şık bir tipografik logo göstergesi
                       row.ConstantItem(40, Unit.Millimetre)
                          .AlignMiddle()
                          .Column(c =>
                          {
                              c.Item().Text("ERDEMSOFT").FontSize(16).Bold().FontColor(HeaderText);
                              c.Item().Text("TEKSTİL").FontSize(10).FontColor(SubText);
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
                    .FontSize(20).Bold().FontColor(HeaderText);
                   c.Item()
                    .Text($"İç Kargo No: {model.ShipmentNumber ?? "—"}")
                    .FontSize(12).FontColor(SubText);
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
               // Firma bilgileri (sol)
               row.RelativeItem().Column(c =>
               {
                   c.Spacing(3);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyName))
                       c.Item()
                        .Text(model.SenderCompanyName)
                        .FontSize(12).Bold().FontColor(HeaderText);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyAddress))
                       c.Item()
                        .Text(model.SenderCompanyAddress)
                        .FontSize(10).FontColor(SubText);

                   var senderLoc = BuildLocation(
                       model.SenderCompanyDistrict, model.SenderCompanyCity);
                   if (!string.IsNullOrWhiteSpace(senderLoc))
                       c.Item()
                        .Text(senderLoc)
                        .FontSize(10).FontColor(SubText);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyPhone))
                       c.Item()
                        .Text($"Tel: {model.SenderCompanyPhone}")
                        .FontSize(10).FontColor(SubText);

                   c.Item().PaddingTop(4);

                   if (!string.IsNullOrWhiteSpace(model.Sender))
                       c.Item()
                        .Text($"Yönlendiren: {model.Sender}")
                        .FontSize(10).FontColor(SubText);

                   c.Item()
                    .Text($"Tarih: {model.CreatedDate:dd.MM.yyyy}")
                    .FontSize(10).FontColor(SubText);

                   if (!string.IsNullOrWhiteSpace(model.CargoCompany))
                       c.Item()
                        .Text($"Kargo: {model.CargoCompany}")
                        .FontSize(10).FontColor(SubText);
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
                    .FontSize(16).Bold().FontColor(HeaderText);

               var attentionLine = AttentionHelper.FormatAttentionLine(model.Attention);
               if (attentionLine != "Muhattap: -")
               {
                   c.Item().PaddingTop(2);
                   c.Item().Text(attentionLine).Bold().FontSize(13).FontColor(SectionText);
               }

               if (!string.IsNullOrWhiteSpace(model.Address))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Adres: ").FontColor(SubText).FontSize(11);
                       r.RelativeItem().Text(model.Address).FontSize(11);
                   });

               var location = BuildLocation(model.District, model.City);
               if (!string.IsNullOrWhiteSpace(location))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("İlçe / İl: ").FontColor(SubText).FontSize(11);
                       r.RelativeItem().Text(location).FontSize(11);
                   });

               if (!string.IsNullOrWhiteSpace(model.Phone))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Tel: ").FontColor(SubText).FontSize(11);
                       r.RelativeItem().Text(model.Phone).FontSize(11);
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
                               c.Item().Text("TAKİP NO").FontSize(9).FontColor(SubText).Bold();
                               c.Item().Text(model.TrackingNumber!).FontSize(14).Bold().FontColor(HeaderText);
                           });

                       if (hasPlate)
                           r.AutoItem().Column(c =>
                           {
                               c.Item().Text("ARAÇ PLAKA").FontSize(9).FontColor(SubText).Bold();
                               c.Item().Text(model.VehiclePlate!).FontSize(14).Bold().FontColor(HeaderText);
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

                        // SkiaSharp Vektör Çizim Alanı
                        bc.Item()
                          .Height(30, Unit.Millimetre)
                          .Canvas((object canvasObj, QuestPDF.Infrastructure.Size size) =>
                          {
                              SKCanvas canvas = (SKCanvas)canvasObj;
                              try
                              {
                                  var barcode = Barcoder.Code128.Code128Encoder.Encode(model.ShipmentNumber);
                                  var numModules = barcode.Bounds.X;
                                  var moduleWidth = size.Width / numModules;

                                  using var paint = new SKPaint
                                  {
                                      Color = SKColors.Black,
                                      Style = SKPaintStyle.Fill
                                  };

                                  for (int i = 0; i < numModules; i++)
                                  {
                                      if (barcode.At(i, 0))
                                      {
                                          var x = i * moduleWidth;
                                          canvas.DrawRect(x, 0, moduleWidth, size.Height, paint);
                                      }
                                  }
                              }
                              catch
                              {
                                  // Barkod kodlama hatası durumunda fallback olarak hata çizgisi çizilir
                                  using var paint = new SKPaint
                                  {
                                      Color = SKColors.Red,
                                      Style = SKPaintStyle.Stroke,
                                      StrokeWidth = 2
                                  };
                                  canvas.DrawLine(0, 0, size.Width, size.Height, paint);
                                  canvas.DrawLine(0, size.Height, size.Width, 0, paint);
                              }
                          });

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
           .FontSize(11).Bold().FontColor(HeaderText);
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
        string qrContent;
        if (!string.IsNullOrWhiteSpace(model.TrackingUrl))
        {
            qrContent = model.TrackingUrl;
        }
        else
        {
            qrContent = $"Kargo No: {model.ShipmentNumber}\n" +
                        $"Gönderici: {model.SenderCompanyName}\n" +
                        $"Alıcı: {model.ReceiverCompany}\n" +
                        $"İlgili Kişi: {model.Attention}\n" +
                        $"Adres: {model.Address}, {model.District} / {model.City}\n" +
                        $"Tel: {model.Phone}\n" +
                        $"Kargo Firması: {model.CargoCompany}";
        }

        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrContent, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}
