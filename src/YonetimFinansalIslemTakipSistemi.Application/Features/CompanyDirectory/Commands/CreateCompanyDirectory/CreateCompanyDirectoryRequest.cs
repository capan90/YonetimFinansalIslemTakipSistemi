namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Commands.CreateCompanyDirectory;

public class CreateCompanyDirectoryRequest
{
    public string CompanyName   { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? AttentionTo   { get; set; }
    public string AddressLine   { get; set; } = string.Empty;
    public string? District      { get; set; }
    public string? City          { get; set; }
    public string? PostalCode    { get; set; }
    public string? Phone         { get; set; }
    public string? Email         { get; set; }
    public string? Notes         { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
}
