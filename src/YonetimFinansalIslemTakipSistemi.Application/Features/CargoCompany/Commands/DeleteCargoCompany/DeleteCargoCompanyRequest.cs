namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.DeleteCargoCompany;

public class DeleteCargoCompanyRequest
{
    public Guid Id { get; set; }
    public Guid DeletedByUserId { get; set; }
}
