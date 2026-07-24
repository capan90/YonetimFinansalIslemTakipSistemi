namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyDirectory.Queries.GetCompanyDirectoryList;

public class CompanyDirectoryDto
{
    public Guid Id            { get; set; }
    public string CompanyName  { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? AttentionTo   { get; set; }
    public string AddressLine  { get; set; } = string.Empty;
    public string? District     { get; set; }
    public string? City         { get; set; }
    public string? PostalCode   { get; set; }
    public string? Phone        { get; set; }
    public string? Email        { get; set; }
    public string? Notes        { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Seçim listeleri için ayırt edici etiket. Aynı firma birden çok adres/telefonla
    /// ayrı kayıt olarak bulunabildiğinden yalnızca ad yeterli değildir —
    /// ilçe/il ve telefon eklenerek kayıtlar birbirinden ayrılır.
    /// </summary>
    public string DisplayLabel
    {
        get
        {
            var location = string.Join("/",
                new[] { District, City }.Where(s => !string.IsNullOrWhiteSpace(s)));

            var parts = new List<string> { CompanyName };
            if (location.Length > 0) parts.Add(location);
            if (!string.IsNullOrWhiteSpace(Phone)) parts.Add(Phone!);
            return string.Join(" — ", parts);
        }
    }
}
