namespace YonetimFinansalIslemTakipSistemi.Application.Common;

/// <summary>
/// Veri katmanında tespit edilen, kullanıcıya doğrudan gösterilebilir Türkçe mesaj
/// taşıyan hata (ör. eksik migration). Message alanı UI'da aynen gösterilebilir;
/// teknik ayrıntı InnerException'da taşınır ve System Log'a yazılır.
/// </summary>
public class DataStoreException : Exception
{
    public DataStoreException(string userMessage, Exception innerException)
        : base(userMessage, innerException) { }
}
