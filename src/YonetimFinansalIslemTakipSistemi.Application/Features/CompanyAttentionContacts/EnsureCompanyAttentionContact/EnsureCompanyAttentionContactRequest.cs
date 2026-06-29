namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.EnsureCompanyAttentionContact;

/// <summary>
/// Firma için dikkatine kişisini ekler (yoksa) ve LastUsedAt'i günceller.
/// </summary>
public record EnsureCompanyAttentionContactRequest(Guid CompanyDirectoryId, string Name, Guid UserId);
