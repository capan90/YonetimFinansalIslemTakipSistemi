using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoPartySuggestions;

/// <summary>
/// Öneriler yön bazlıdır: gelen kargoda gönderen/teslim alan, giden kargoda
/// alıcı/teslim eden isimleri farklı kümelerdir; karıştırmak listeyi kirletir.
/// </summary>
public record GetCargoPartySuggestionsQuery(CargoShipmentDirection Direction);
