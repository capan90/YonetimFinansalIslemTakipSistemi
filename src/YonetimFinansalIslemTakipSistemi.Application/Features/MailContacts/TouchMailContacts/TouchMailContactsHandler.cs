using YonetimFinansalIslemTakipSistemi.Application.Common;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.TouchMailContacts;

/// <summary>
/// Başarılı bir gönderimden sonra kullanılan adreslerin "son kullanım" bilgisini tazeler;
/// rehber listesi böylece sık kullanılan adresleri üstte gösterir.
/// Yetki istemez: kullanıcı zaten mail göndermeye yetkili ve içerik değişmiyor,
/// yalnızca sıralama metadatası güncelleniyor. Audit yazılmaz — gönderimin kendisi
/// CargoMailPrepared olarak zaten denetleniyor, her adres için ayrı kayıt gürültü olur.
/// </summary>
public class TouchMailContactsHandler
{
    private readonly IMailContactRepository _repository;

    public TouchMailContactsHandler(IMailContactRepository repository)
        => _repository = repository;

    public Task HandleAsync(IEnumerable<string> usedEmails)
    {
        var normalized = usedEmails
            .Select(EmailAddressHelper.Normalize)
            .Where(e => e is not null)
            .Select(e => e!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Count == 0
            ? Task.CompletedTask
            : _repository.TouchLastUsedAsync(normalized, DateTime.UtcNow);
    }
}
