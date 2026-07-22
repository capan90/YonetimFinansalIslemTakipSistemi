namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

/// <summary>
/// Kullanıcının girdiği metinlerin kayıt öncesi harf dönüşüm tercihi.
/// Dönüşüm tr-TR kültürüyle yapılır (i→İ, ı→I).
/// </summary>
public enum TextCasePreference
{
    /// <summary>Olduğu Gibi — metin girildiği haliyle kaydedilir.</summary>
    Preserve = 0,

    /// <summary>BÜYÜK HARF</summary>
    Uppercase = 1,

    /// <summary>küçük harf</summary>
    Lowercase = 2
}
