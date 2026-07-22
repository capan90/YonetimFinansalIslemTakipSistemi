using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.GetWhatsAppContactList;

/// <summary>
/// Ortak WhatsApp rehberini listeler. Rehber kullanıcı bazlı değildir ve
/// ayrı permission gerektirmez — oturum açan kullanıcılar erişebilir.
/// </summary>
public class GetWhatsAppContactListHandler
{
    private readonly IWhatsAppContactRepository _repository;

    public GetWhatsAppContactListHandler(IWhatsAppContactRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<WhatsAppContactDto>> HandleAsync(GetWhatsAppContactListQuery query)
    {
        var contacts = await _repository.GetListAsync(query.Search, query.Company, query.IncludeInactive);

        return contacts.Select(c => new WhatsAppContactDto
        {
            Id          = c.Id,
            FullName    = c.FullName,
            Phone       = c.Phone,
            Company     = c.Company,
            Description = c.Description,
            IsActive    = c.IsActive,
            CreatedAt   = c.CreatedAt
        }).ToList();
    }
}
