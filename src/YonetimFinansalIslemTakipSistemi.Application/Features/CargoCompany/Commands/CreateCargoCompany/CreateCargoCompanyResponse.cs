namespace YonetimFinansalIslemTakipSistemi.Application.Features.CargoCompany.Commands.CreateCargoCompany;

public class CreateCargoCompanyResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
