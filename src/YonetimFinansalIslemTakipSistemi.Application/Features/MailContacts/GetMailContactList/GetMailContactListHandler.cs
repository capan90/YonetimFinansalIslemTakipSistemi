using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.GetMailContactList;

/// <summary>
/// Ortak mail rehberini listeler. Rehber kullanıcı bazlı değildir ve ayrı
/// permission gerektirmez — oturum açan kullanıcılar erişebilir.
/// </summary>
public class GetMailContactListHandler
{
    private readonly IMailContactRepository _repository;

    public GetMailContactListHandler(IMailContactRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<MailContactDto>> HandleAsync(GetMailContactListQuery query)
    {
        var contacts = await _repository.GetListAsync(query.Search, query.IncludeInactive);
        return contacts.Select(ToDto).ToList();
    }

    /// <summary>Mail ekranı açılışında CC'ye otomatik eklenecek kayıtlar.</summary>
    public async Task<IReadOnlyList<MailContactDto>> GetDefaultCcAsync()
    {
        var contacts = await _repository.GetDefaultCcAsync();
        return contacts.Select(ToDto).ToList();
    }

    internal static MailContactDto ToDto(MailContact c) => new()
    {
        Id          = c.Id,
        FullName    = c.FullName,
        Email       = c.Email,
        Company     = c.Company,
        Description = c.Description,
        IsDefaultCc = c.IsDefaultCc,
        LastUsedAt  = c.LastUsedAt,
        IsActive    = c.IsActive,
        CreatedAt   = c.CreatedAt
    };
}
