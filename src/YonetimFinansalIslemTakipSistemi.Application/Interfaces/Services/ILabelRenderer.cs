using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

/// <summary>
/// Kargo etiketi render arayüzü.
/// İlk implementasyon: QuestPdfLabelRenderer (Infrastructure).
/// İleride farklı render motorları (ZPL, HTML, vb.) eklenebilir.
/// </summary>
public interface ILabelRenderer
{
    /// <summary>CargoLabelModel'den PDF içeriğini byte[] olarak üretir.</summary>
    byte[] Render(CargoLabelModel model);
}
