namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;

public class CreateCompanyDirectoryResponse
{
    public Guid Id          { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
