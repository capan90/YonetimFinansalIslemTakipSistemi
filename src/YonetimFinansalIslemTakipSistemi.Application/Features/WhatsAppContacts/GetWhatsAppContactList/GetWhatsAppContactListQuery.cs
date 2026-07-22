namespace YonetimFinansalIslemTakipSistemi.Application.Features.WhatsAppContacts.GetWhatsAppContactList;

public class GetWhatsAppContactListQuery
{
    /// <summary>Ad, telefon veya firma üzerinden arama (contains, büyük/küçük duyarsız).</summary>
    public string? Search { get; set; }

    /// <summary>Firma filtresi (tam eşleşme).</summary>
    public string? Company { get; set; }

    /// <summary>true: pasif kayıtlar da listelenir (yönetim ekranı). Seçim listeleri false kullanır.</summary>
    public bool IncludeInactive { get; set; }
}
