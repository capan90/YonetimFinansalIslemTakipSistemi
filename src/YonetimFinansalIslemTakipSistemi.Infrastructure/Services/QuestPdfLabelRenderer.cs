using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// ILabelRenderer: QuestPDF ile A6 (105×148 mm) kurumsal kargo etiketi.
/// Community lisans — iç kullanım aracı, ticari ürün değil.
/// </summary>
public class QuestPdfLabelRenderer : ILabelRenderer
{
    static QuestPdfLabelRenderer()
        => QuestPDF.Settings.License = LicenseType.Community;

    // ── Kurumsal renk paleti ────────────────────────────────────────────
    private const string HeaderBg   = "#1A3354"; // koyu lacivert
    private const string SectionBg  = "#2E5984"; // orta mavi
    private const string FooterBg   = "#F0F4F8"; // açık gri-mavi
    private const string SubText    = "#64748B"; // slate-500
    private const string BorderGrey = "#CBD5E1"; // slate-300
    private const string AccentBlue = "#B0C4DE"; // header alt metin

    public byte[] Render(CargoLabelModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(105, 148, Unit.Millimetre);
                page.Margin(5, Unit.Millimetre);
                page.DefaultTextStyle(ts =>
                    ts.FontSize(8.5f).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    col.Spacing(0);
                    RenderHeader(col, model);
                    RenderSenderSection(col, model);
                    RenderReceiverSection(col, model);
                    RenderFooter(col, model);
                });
            });
        })
        .GeneratePdf();
    }

    // ── Header: logo / initials + başlık + priority badge ──────────────

    private static void RenderHeader(ColumnDescriptor col, CargoLabelModel model)
    {
        col.Item()
           .Background(HeaderBg)
           .Padding(5)
           .Row(row =>
           {
               // Logo (sol) — yüklenemezse initials placeholder
               var logoBytes = TryLoadLogo(model.SenderLogoPath);
               if (logoBytes != null)
               {
                   row.ConstantItem(22, Unit.Millimetre)
                      .AlignMiddle()
                      .Image(logoBytes).FitWidth();
               }
               else
               {
                   row.ConstantItem(22, Unit.Millimetre)
                      .AlignMiddle()
                      .Border(1).BorderColor(Colors.White)
                      .Padding(3)
                      .AlignCenter()
                      .Text(GetInitials(model.SenderCompanyName))
                      .FontSize(11).Bold().FontColor(Colors.White);
               }

               row.ConstantItem(5);

               // Başlık (orta)
               row.RelativeItem().AlignMiddle().Column(c =>
               {
                   c.Item()
                    .Text("KARGO ETİKETİ")
                    .FontSize(10).Bold().FontColor(Colors.White);
                   c.Item()
                    .Text($"No: {model.ShipmentNumber ?? "—"}")
                    .FontSize(8).FontColor(AccentBlue);
               });

               // Öncelik badge (sağ) — korundu
               row.AutoItem()
                  .AlignMiddle()
                  .Background(PriorityBadgeColor(model.Priority))
                  .Padding(4)
                  .Text(model.Priority.ToUpperInvariant())
                  .FontSize(7.5f).Bold().FontColor(Colors.White);
           });
    }

    // ── Gönderici: firma bilgileri + QR placeholder ──────────────────────

    private static void RenderSenderSection(ColumnDescriptor col, CargoLabelModel model)
    {
        SectionHeader(col, "GÖNDERİCİ");

        col.Item()
           .BorderBottom(1).BorderColor(BorderGrey)
           .PaddingHorizontal(5).PaddingVertical(4)
           .Row(row =>
           {
               // Firma bilgileri (sol)
               row.RelativeItem().Column(c =>
               {
                   c.Spacing(1.5f);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyName))
                       c.Item()
                        .Text(model.SenderCompanyName)
                        .FontSize(9).Bold();

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyAddress))
                       c.Item()
                        .Text(model.SenderCompanyAddress)
                        .FontSize(8);

                   var senderLoc = BuildLocation(
                       model.SenderCompanyDistrict, model.SenderCompanyCity);
                   if (!string.IsNullOrWhiteSpace(senderLoc))
                       c.Item()
                        .Text(senderLoc)
                        .FontSize(8).FontColor(SubText);

                   if (!string.IsNullOrWhiteSpace(model.SenderCompanyPhone))
                       c.Item()
                        .Text($"Tel: {model.SenderCompanyPhone}")
                        .FontSize(8).FontColor(SubText);

                   // Ayırıcı
                   c.Item().PaddingTop(2);

                   if (!string.IsNullOrWhiteSpace(model.Sender))
                       c.Item()
                        .Text($"Yönlendiren: {model.Sender}")
                        .FontSize(7.5f).FontColor(SubText);

                   c.Item()
                    .Text($"Tarih: {model.CreatedDate:dd.MM.yyyy}")
                    .FontSize(7.5f).FontColor(SubText);

                   if (!string.IsNullOrWhiteSpace(model.CargoCompany))
                       c.Item()
                        .Text($"Kargo: {model.CargoCompany}")
                        .FontSize(7.5f).FontColor(SubText);
               });

               row.ConstantItem(4);

               // QR Placeholder (sağ) — ileride TrackingUrl'den gerçek QR üretilecek
               row.ConstantItem(20, Unit.Millimetre)
                  .AlignTop()
                  .Border(1).BorderColor(BorderGrey)
                  .Background("#F8FAFC")
                  .Height(20, Unit.Millimetre)
                  .Padding(2)
                  .Column(qr =>
                  {
                      qr.Spacing(2);
                      qr.Item().AlignCenter()
                               .Text("QR")
                               .FontSize(11).Bold().FontColor(SubText);
                      qr.Item().AlignCenter()
                               .Text("(Yakında)")
                               .FontSize(5.5f).FontColor(SubText);
                  });
           });
    }

    // ── Alıcı: snapshot verisi, Dikkatine belirgin, adres wrap ───────────

    private static void RenderReceiverSection(ColumnDescriptor col, CargoLabelModel model)
    {
        SectionHeader(col, "ALICI");

        col.Item()
           .BorderBottom(1).BorderColor(BorderGrey)
           .PaddingHorizontal(5).PaddingVertical(4)
           .Column(c =>
           {
               c.Spacing(2);

               // Firma adı: büyük + kalın
               if (!string.IsNullOrWhiteSpace(model.ReceiverCompany))
                   c.Item()
                    .Text(model.ReceiverCompany.ToUpperInvariant())
                    .FontSize(10.5f).Bold();

               // Dikkatine: BOLD label, normal font value — görsel ayrım
               if (!string.IsNullOrWhiteSpace(model.Attention))
                   c.Item().Row(r =>
                   {
                       r.AutoItem()
                        .Text("Dikkatine: ")
                        .Bold().FontSize(8.5f);
                       r.RelativeItem()
                        .Text(model.Attention)
                        .FontSize(8.5f);
                   });

               // Adres — RelativeItem ile otomatik satır kaydırma
               if (!string.IsNullOrWhiteSpace(model.Address))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Adres: ").FontColor(SubText);
                       r.RelativeItem().Text(model.Address);
                   });

               var location = BuildLocation(model.District, model.City);
               if (!string.IsNullOrWhiteSpace(location))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("İlçe/Şehir: ").FontColor(SubText);
                       r.RelativeItem().Text(location);
                   });

               if (!string.IsNullOrWhiteSpace(model.Phone))
                   c.Item().Row(r =>
                   {
                       r.AutoItem().Text("Tel: ").FontColor(SubText);
                       r.RelativeItem().Text(model.Phone);
                   });
           });
    }

    // ── Footer: Takip No + Araç Plaka + Barkod alanı ────────────────────

    private static void RenderFooter(ColumnDescriptor col, CargoLabelModel model)
    {
        col.Item()
           .Background(FooterBg)
           .PaddingHorizontal(5).PaddingVertical(4)
           .Column(f =>
           {
               f.Spacing(3);

               // Takip No + Araç Plaka yan yana
               var hasTracking = !string.IsNullOrWhiteSpace(model.TrackingNumber);
               var hasPlate    = !string.IsNullOrWhiteSpace(model.VehiclePlate);

               if (hasTracking || hasPlate)
                   f.Item().Row(r =>
                   {
                       if (hasTracking)
                           r.RelativeItem().Column(c =>
                           {
                               c.Item().Text("Takip No").FontSize(6.5f).FontColor(SubText);
                               c.Item().Text(model.TrackingNumber!).FontSize(8.5f).Bold();
                           });

                       if (hasPlate)
                           r.AutoItem().Column(c =>
                           {
                               c.Item().Text("Araç Plaka").FontSize(6.5f).FontColor(SubText);
                               c.Item().Text(model.VehiclePlate!).FontSize(9).Bold();
                           });
                   });

               // Barkod alanı — ShipmentNumber büyük monospace gösterimi
               if (!string.IsNullOrWhiteSpace(model.ShipmentNumber))
                   f.Item()
                    .Border(1).BorderColor(BorderGrey)
                    .Background(Colors.White)
                    .PaddingHorizontal(5).PaddingVertical(3)
                    .Column(bc =>
                    {
                        bc.Item().AlignCenter()
                                 .Text(model.ShipmentNumber)
                                 .FontSize(13).Bold();
                        bc.Item().AlignCenter()
                                 .Text("B A R K O D")
                                 .FontSize(6).FontColor(SubText);
                    });
           });
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item()
           .Background(SectionBg)
           .PaddingHorizontal(5).PaddingVertical(2)
           .Text(title)
           .FontSize(7).Bold().FontColor(Colors.White);
    }

    private static string? BuildLocation(string? district, string? city)
    {
        var parts = new[] { district, city?.ToUpperInvariant() }
                        .Where(s => !string.IsNullOrWhiteSpace(s));
        var result = string.Join(" / ", parts);
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private static string GetInitials(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName)) return "?";
        return new string(
            companyName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Take(2)
                       .Select(w => w[0])
                       .ToArray())
            .ToUpperInvariant();
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
            // Logo yüklenemezse sessizce initials placeholder'a düşer
            return null;
        }
    }

    private static string PriorityBadgeColor(string priority) => priority switch
    {
        "Orta"     => "#2563EB",  // blue-600
        "Acil"     => "#EA580C",  // orange-600
        "Çok Acil" => "#DC2626",  // red-600
        _          => "#6B7280"   // grey-500 (Normal)
    };
}
