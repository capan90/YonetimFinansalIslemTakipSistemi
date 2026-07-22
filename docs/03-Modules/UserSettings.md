# Kullanıcı Ayarları Modülü

## Genel Bakış

Kullanıcı bazlı, veritabanında kalıcı tercihler. İlk tercih: **Harf Duyarlılığı**.
Yapı, ileride yeni kullanıcı tercihleriyle genişleyecek şekilde ayrı tabloda tutulur
(`user_preferences`, kullanıcı başına tek satır — `UserId` unique).

---

## Harf Duyarlılığı (2026-07-22)

Kullanıcının girdiği anlamlı iş metinlerinin nasıl kaydedileceğini belirler.

| Enum (`TextCasePreference`) | Kullanıcı karşılığı |
|---|---|
| `Preserve = 0` (varsayılan) | Olduğu Gibi |
| `Uppercase = 1` | BÜYÜK HARF |
| `Lowercase = 2` | küçük harf |

### Kurallar

- Dönüşüm **tr-TR** kültürüyle yapılır: `istanbul → İSTANBUL`, `ışık → IŞIK`, `İSTANBUL → istanbul`.
- Tercih değişikliği yalnızca **bundan sonra** oluşturulan/düzenlenen kayıtları etkiler;
  eski kayıtlar geriye dönük değiştirilmez.
- Ayar kaydı `UserPreferenceUpdated` aksiyonuyla audit'e yazılır.

### Mimari

- İş kuralının tek kaynağı Application katmanındaki **`IUserTextNormalizationService`**
  (`UserTextNormalizationService`). WPF TextBox seviyesinde zorlanmaz — ileride Excel
  import veya farklı bir UI gelirse aynı kural korunur.
- Servis, aktif tercihi `IUserContext.TextCasePreference` üzerinden okur. Tercih login'de
  DB'den oturuma yüklenir (`LoginViewModel`), ayar kaydında `IUserSession.SetTextCasePreference`
  ile anında güncellenir — yeniden giriş gerekmez.
- Hangi alanların dönüştürüleceği **handler seviyesinde açıkça** belirtilir; reflection
  ile toplu dönüşüm yapılmaz.

### Kapsam

Dönüştürülür: finans işlem açıklaması; firma rehberi (firma adı, ilgili/dikkatine,
adres, il, ilçe, notlar); kargo kaydı (gönderen/alıcı/teslim eden/teslim alan,
plaka, notlar, dikkatine); kargo firması (ad, notlar); WhatsApp rehberi (ad, firma, açıklama).

Dönüştürülmez: kullanıcı adı, parola, e-posta, telefon, URL/portal/takip linki,
otomatik kargo numarası, takip numarası, posta kodu, sayısal/sistemsel kodlar.

### Ekran ve Erişim

`TextCaseSettingsWindow`: üç seçenekli ComboBox, Kaydet ile DB'ye yazılır ve oturuma
anında uygulanır.

Harf duyarlılığı **kişisel** bir ayardır; `CanAccessSettings` gibi sistem ayarı yetkisi
**gerektirmez**. İki menü girişi aynı pencereyi ve aynı handler altyapısını kullanır:

- **Yardım → Kullanıcı Ayarlarım → Harf Duyarlılığı** — her giriş yapmış kullanıcı erişebilir.
- **Ayarlar → Harf Duyarlılığı** — Ayarlar menüsünü görebilen (CanAccessSettings) kullanıcılar
  için kısayol olarak korunmuştur.
