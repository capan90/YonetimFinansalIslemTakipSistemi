using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsHandler
{
    private readonly IAuditLogRepository _repository;
    private readonly IUserContext        _userContext;

    public GetAuditLogsHandler(IAuditLogRepository repository, IUserContext userContext)
    {
        _repository  = repository;
        _userContext = userContext;
    }

    public async Task<OperationResult<List<AuditLogDto>>> HandleAsync(GetAuditLogsQuery query)
    {
        if (!_userContext.HasPermission(PermissionType.CanViewAuditLog))
            return OperationResult<List<AuditLogDto>>.Fail(
                "Bu işlem için yetkiniz bulunmamaktadır.");

        var logs = await _repository.GetFilteredAsync(
            query.UserId, query.DateFrom, query.DateTo, query.Action);

        var dtos = logs.Select(l => new AuditLogDto
        {
            Id            = l.Id,
            UserName      = l.UserName,
            ActionDisplay = MapAction(l.Action),
            EntityType    = l.EntityType,
            EntityId      = l.EntityId,
            OldValues     = l.OldValues,
            NewValues     = l.NewValues,
            ComputerName  = l.ComputerName,
            Timestamp     = l.Timestamp
        }).ToList();

        return OperationResult<List<AuditLogDto>>.Ok(dtos);
    }

    private static string MapAction(AuditAction action) => action switch
    {
        AuditAction.TransactionCreated => "İşlem Oluşturuldu",
        AuditAction.TransactionUpdated => "İşlem Güncellendi",
        AuditAction.TransactionDeleted => "İşlem Silindi",
        AuditAction.UserCreated        => "Kullanıcı Oluşturuldu",
        AuditAction.UserUpdated        => "Kullanıcı Güncellendi",
        AuditAction.UserDeleted        => "Kullanıcı Silindi",
        AuditAction.UserLoggedIn       => "Giriş Yapıldı",
        AuditAction.PermissionUpdated  => "Yetki Güncellendi",
        AuditAction.ExchangeRateCreated => "Döviz Kuru Eklendi",
        AuditAction.ExchangeRateUpdated => "Döviz Kuru Güncellendi",
        // Kargo Katip modülü
        AuditAction.CompanyDirectoryCreated => "Firma Rehberi Kaydı Oluşturuldu",
        AuditAction.CompanyDirectoryUpdated => "Firma Rehberi Kaydı Güncellendi",
        AuditAction.CompanyDirectoryDeleted => "Firma Rehberi Kaydı Silindi",
        AuditAction.CargoCompanyCreated     => "Kargo Firması Oluşturuldu",
        AuditAction.CargoCompanyUpdated     => "Kargo Firması Güncellendi",
        AuditAction.CargoCompanyDeleted     => "Kargo Firması Silindi",
        AuditAction.CargoShipmentCreated    => "Kargo Kaydı Oluşturuldu",
        AuditAction.CargoShipmentUpdated    => "Kargo Kaydı Güncellendi",
        AuditAction.CargoShipmentDeleted    => "Kargo Kaydı Silindi",
        AuditAction.CargoLabelPrinted       => "Kargo Etiketi Yazdırıldı",
        AuditAction.CargoWhatsAppPrepared   => "WhatsApp Mesajı Hazırlandı",
        AuditAction.CargoMailPrepared       => "Mail Mesajı Hazırlandı",
        _                                   => action.ToString()
    };
}
