using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Import;

/// <summary>Önizleme satırının durumu. Error ve kesin mükerrer içe aktarılamaz.</summary>
public enum CargoImportRowStatus
{
    Valid,
    Warning,
    Error,
    Duplicate
}

/// <summary>Satır bazlı doğrulama mesajı — kullanıcıya gösterilecek Türkçe metin.</summary>
public sealed record CargoImportRowMessage(string Column, string Message, bool IsWarning);

/// <summary>Mükerrer tespitinin nedeni — önizlemede kullanıcıya gösterilir.</summary>
public enum DuplicateKind
{
    /// <summary>Aynı takip numarası dosya içinde daha önce geçti (kesin).</summary>
    TrackingNumberInFile,

    /// <summary>Aynı takip numarası veritabanında kayıtlı (kesin).</summary>
    TrackingNumberInDatabase,

    /// <summary>Aynı tarih + firma + kargo firması dosya içinde daha önce geçti (olası).</summary>
    SimilarInFile,

    /// <summary>Aynı tarih + firma + kargo firması veritabanında kayıtlı (olası).</summary>
    SimilarInDatabase
}

/// <summary>
/// Mükerrer nedeni modeli. IsExact=true (takip no eşleşmesi) satır asla içe aktarılamaz;
/// IsExact=false (benzerlik) kullanıcı bilinçli olarak dahil edebilir.
/// </summary>
public sealed class DuplicateReason
{
    public required DuplicateKind Kind { get; init; }

    /// <summary>Kullanıcıya gösterilecek açıklama (örn. "Aynı takip no GDN00042 kaydında mevcut").</summary>
    public required string Description { get; init; }

    /// <summary>Veritabanı eşleşmesinde mevcut kaydın numarası.</summary>
    public string? MatchedShipmentNumber { get; init; }

    /// <summary>Dosya içi eşleşmede ilk geçtiği satır numarası.</summary>
    public int? MatchedRowNumber { get; init; }

    public bool IsExact => Kind is DuplicateKind.TrackingNumberInFile or DuplicateKind.TrackingNumberInDatabase;
}

/// <summary>
/// Analizden geçmiş tek satır: parse edilmiş alanlar + durum + mesajlar.
/// Import handler'ı yalnızca bu DTO'yu bilir — kaynak formatından bağımsızdır
/// (ileride REST/ERP kaynakları da bu DTO'yu üretip aynı import akışını kullanır).
/// </summary>
public sealed class CargoImportRowDto
{
    public required int RowNumber { get; init; }

    // Parse edilmiş / çözümlenmiş alanlar
    public DateTime ShipmentDate { get; set; }
    public Guid?  CompanyDirectoryId { get; set; }
    public string? CompanyName { get; set; }
    public Guid?  CargoCompanyId { get; set; }
    public string? CargoCompanyName { get; set; }
    public CargoShipmentType? ShipmentType { get; set; }
    public CargoShipmentPriority Priority { get; set; } = CargoShipmentPriority.Normal;
    public string? SenderName { get; set; }
    public string? ReceiverName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? VehiclePlate { get; set; }
    public string? Notes { get; set; }

    // Rehberden kopyalanan snapshot alanları (manuel akışla aynı kaynak)
    public string? ReceiverCompanyNameSnapshot { get; set; }
    public string? ReceiverAddressSnapshot { get; set; }
    public string? ReceiverAttentionSnapshot { get; set; }
    public string? ReceiverCitySnapshot { get; set; }
    public string? ReceiverDistrictSnapshot { get; set; }
    public string? ReceiverPhoneSnapshot { get; set; }
    public string? ReceiverEmailSnapshot { get; set; }

    // Durum
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
