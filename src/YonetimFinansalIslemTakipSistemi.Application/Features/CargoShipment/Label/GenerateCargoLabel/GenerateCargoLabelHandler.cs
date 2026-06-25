using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Label.GenerateCargoLabel;

/// <summary>
/// Kargo etiketi üretir: CargoShipment → CargoLabelModel → ILabelRenderer → PDF bytes.
/// Gönderici firma bilgisi ICompanyInfoProvider'dan alınır; ileride Company Settings modülüyle değiştirilebilir.
/// Preview için audit yazılmaz; gerçek baskı PrintCargoLabelHandler ile yapılacak (Sprint 3.3+).
/// </summary>
public class GenerateCargoLabelHandler
{
    private readonly ICargoShipmentRepository _repository;
    private readonly ILabelRenderer           _renderer;
    private readonly ICompanyInfoProvider     _companyInfo;
    private readonly IUserContext             _userContext;

    public GenerateCargoLabelHandler(
        ICargoShipmentRepository repository,
        ILabelRenderer renderer,
        ICompanyInfoProvider companyInfo,
        IUserContext userContext)
    {
        _repository  = repository;
        _renderer    = renderer;
        _companyInfo = companyInfo;
        _userContext = userContext;
    }

    public async Task<OperationResult<byte[]>> HandleAsync(GenerateCargoLabelRequest request)
    {
        var viewPerm   = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanViewIncomingCargo
            : PermissionType.CanViewOutgoingCargo;
        var managePerm = request.Direction == CargoShipmentDirection.Incoming
            ? PermissionType.CanManageIncomingCargo
            : PermissionType.CanManageOutgoingCargo;

        if (!_userContext.HasPermission(viewPerm) && !_userContext.HasPermission(managePerm))
            return OperationResult<byte[]>.Fail("Bu işlem için yetkiniz bulunmamaktadır.");

        // WithIncludes: CargoCompany navigasyonu yüklü gelir
        var shipment = await _repository.GetByIdWithIncludesAsync(request.Id);
        if (shipment is null)
            return OperationResult<byte[]>.Fail("Kargo kaydı bulunamadı.");

        // Shipment → model (alıcı: snapshot; kargo firması: navigasyon)
        var model = CargoLabelBuilder.Build(shipment);

        // Gönderici firma bilgisi: ICompanyInfoProvider (AppSettings → ileride Company Settings)
        var company = _companyInfo.Get();
        model.SenderCompanyName     = company.Name;
        model.SenderCompanyAddress  = company.Address;
        model.SenderCompanyDistrict = company.District;
        model.SenderCompanyCity     = company.City;
        model.SenderCompanyPhone    = company.Phone;
        model.SenderLogoPath        = company.LogoPath;

        var pdfBytes = _renderer.Render(model);
        return OperationResult<byte[]>.Ok(pdfBytes);
    }
}
