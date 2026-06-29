using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Repositories;
using YonetimFinansalIslemTakipSistemi.Domain.Entities;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.CompanyAttentionContacts.EnsureCompanyAttentionContact;

public class EnsureCompanyAttentionContactHandler
{
    private readonly ICompanyAttentionContactRepository _repository;
    private readonly ICompanyDirectoryRepository        _directoryRepository;

    public EnsureCompanyAttentionContactHandler(
        ICompanyAttentionContactRepository repository,
        ICompanyDirectoryRepository        directoryRepository)
    {
        _repository          = repository;
        _directoryRepository = directoryRepository;
    }

    public async Task HandleAsync(EnsureCompanyAttentionContactRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var contacts = await _repository.GetByCompanyAsync(request.CompanyDirectoryId);

        // Büyük/küçük harf ve boşluk farkı gözetmeksizin var mı kontrol et
        var existing = contacts.FirstOrDefault(c =>
            string.Equals(c.Name.Trim(), name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            // Var olan kaydın son kullanım tarihini güncelle
            existing.LastUsedAt      = DateTime.UtcNow;
            existing.UpdatedAt       = DateTime.UtcNow;
            existing.UpdatedByUserId = request.UserId;
            await _repository.UpdateAsync(existing);
        }
        else
        {
            // Yeni kişi ekle
            var contact = new CompanyAttentionContact
            {
                Id                 = Guid.NewGuid(),
                CompanyDirectoryId = request.CompanyDirectoryId,
                Name               = name,
                LastUsedAt         = DateTime.UtcNow,
                CreatedAt          = DateTime.UtcNow,
                CreatedByUserId    = request.UserId,
            };
            await _repository.AddAsync(contact);
        }

        // Firma kartındaki varsayılan "AttentionTo" son kullanılan kişiyle güncellenir
        var directory = await _directoryRepository.GetByIdWithTrackingAsync(request.CompanyDirectoryId);
        if (directory is not null && !string.Equals(directory.AttentionTo?.Trim(), name, StringComparison.OrdinalIgnoreCase))
        {
            directory.AttentionTo    = name;
            directory.UpdatedAt      = DateTime.UtcNow;
            directory.UpdatedByUserId = request.UserId;
            await _directoryRepository.UpdateAsync(directory);
        }
    }
}
