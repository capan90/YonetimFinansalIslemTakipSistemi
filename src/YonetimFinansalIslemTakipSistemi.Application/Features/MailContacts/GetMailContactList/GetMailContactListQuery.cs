namespace YonetimFinansalIslemTakipSistemi.Application.Features.MailContacts.GetMailContactList;

public class GetMailContactListQuery
{
    /// <summary>Ad, e-posta veya firma üzerinden arama (contains, büyük/küçük duyarsız).</summary>
    public string? Search { get; set; }

    /// <summary>true: pasif kayıtlar da listelenir (yönetim ekranı). Seçim listeleri false kullanır.</summary>
    public bool IncludeInactive { get; set; }
}
