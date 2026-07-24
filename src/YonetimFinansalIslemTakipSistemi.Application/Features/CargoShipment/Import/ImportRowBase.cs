namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>
/// Tüm içe aktarma dikeylerinin (gönderi, firma rehberi, WhatsApp, ileride finans)
/// paylaştığı satır durumu mantığı: mesajlar, mükerrer nedeni, durum çözümleme ve
/// dahil edilebilirlik kuralları. Alan'a özgü kolonlar türeyen DTO'larda tanımlanır.
/// </summary>
public abstract class ImportRowBase
{
    public required int RowNumber { get; init; }

    public CargoImportRowStatus Status { get; set; } = CargoImportRowStatus.Valid;
    public List<CargoImportRowMessage> Messages { get; } = [];
    public DuplicateReason? DuplicateReason { get; set; }

    /// <summary>Error veya kesin mükerrer satırlar hiçbir koşulda içe aktarılamaz.</summary>
    public bool CanInclude => Status != CargoImportRowStatus.Error
                              && (DuplicateReason is null || !DuplicateReason.IsExact);

    /// <summary>Valid/Warning varsayılan dahil; mükerrerler varsayılan hariç.</summary>
    public bool IncludedByDefault => CanInclude && Status != CargoImportRowStatus.Duplicate;

    public void AddError(string column, string message)
        => Messages.Add(new CargoImportRowMessage(column, message, IsWarning: false));

    public void AddWarning(string column, string message)
        => Messages.Add(new CargoImportRowMessage(column, message, IsWarning: true));

    /// <summary>Mesajlara ve mükerrer durumuna göre nihai durumu belirler (Error > Duplicate > Warning > Valid).</summary>
    public void ResolveStatus()
    {
        if (Messages.Any(m => !m.IsWarning))      Status = CargoImportRowStatus.Error;
        else if (DuplicateReason is not null)     Status = CargoImportRowStatus.Duplicate;
        else if (Messages.Count > 0)              Status = CargoImportRowStatus.Warning;
        else                                      Status = CargoImportRowStatus.Valid;
    }
}
