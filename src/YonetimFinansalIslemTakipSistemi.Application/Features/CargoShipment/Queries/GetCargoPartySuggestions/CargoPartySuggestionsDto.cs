namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoPartySuggestions;

/// <summary>
/// Kargo formundaki Gönderen / Alıcı / Teslim Eden / Teslim Alan alanları için
/// geçmiş kayıtlardan türetilmiş öneri listeleri. Her liste son kullanılan önce sıralıdır.
/// </summary>
public class CargoPartySuggestionsDto
{
    public IReadOnlyList<string> SenderNames   { get; init; } = [];
    public IReadOnlyList<string> ReceiverNames { get; init; } = [];
    public IReadOnlyList<string> DeliveredBy   { get; init; } = [];
    public IReadOnlyList<string> ReceivedBy    { get; init; } = [];
}
