using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoPartySuggestions;

/// <summary>
/// Gönderi/teslim isim önerilerini geçmiş kargo kayıtlarından türetir.
/// Ayrı bir rehber tablosu tutulmaz: liste kullanıldıkça kendini besler,
/// kullanıcı ayrıca bakım yapmak zorunda kalmaz.
/// </summary>
public class GetCargoPartySuggestionsHandler
{
    private readonly ICargoShipmentRepository _repository;

    /// <summary>Öneri havuzu: taranan en son kayıt sayısı.</summary>
    private const int ScanRecordLimit = 500;

    /// <summary>Bir alan için açılır listede gösterilecek en fazla öneri.</summary>
    private const int MaxSuggestionsPerField = 30;

    public GetCargoPartySuggestionsHandler(ICargoShipmentRepository repository)
        => _repository = repository;

    public async Task<CargoPartySuggestionsDto> HandleAsync(GetCargoPartySuggestionsQuery query)
    {
        var rows = await _repository.GetPartyNameHistoryAsync(query.Direction, ScanRecordLimit);

        // rows zaten CreatedAt DESC sıralı → ilk görülen değer en güncel kullanımdır
        return new CargoPartySuggestionsDto
        {
            SenderNames   = Distinct(rows.Select(r => r.SenderName)),
            ReceiverNames = Distinct(rows.Select(r => r.ReceiverName)),
            DeliveredBy   = Distinct(rows.Select(r => r.DeliveredBy)),
            ReceivedBy    = Distinct(rows.Select(r => r.ReceivedBy)),
        };
    }

    /// <summary>
    /// Boşları eler, baştaki/sondaki boşlukları kırpar ve büyük/küçük harf farkını
    /// yok sayarak tekilleştirir; sıralama korunur (en son kullanılan üstte kalır).
    /// Karşılaştırma Türkçe farkındadır: "YILMAZ" ile "Yılmaz" aynı isim sayılır,
    /// OrdinalIgnoreCase bunları ayrı görüp listeyi ikizlerle doldururdu.
    /// </summary>
    private static IReadOnlyList<string> Distinct(IEnumerable<string?> values) =>
        values
            .Select(v => v?.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => v!)
            .Distinct(TextNormalizer.TurkishIgnoreCase)
            .Take(MaxSuggestionsPerField)
            .ToList();
}
