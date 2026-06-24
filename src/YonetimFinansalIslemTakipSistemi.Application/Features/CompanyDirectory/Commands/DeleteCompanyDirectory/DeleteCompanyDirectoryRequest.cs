namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.DeleteCompanyDirectory;

public class DeleteCompanyDirectoryRequest
{
    public Guid Id { get; set; }
    public Guid DeletedByUserId { get; set; }
}
