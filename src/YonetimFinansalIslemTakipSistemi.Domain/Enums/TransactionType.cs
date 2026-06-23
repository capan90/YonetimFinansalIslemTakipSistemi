namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

/// <summary>
/// Sistem içinde desteklenen finansal işlem tiplerini tanımlar.
/// Giriş = Alacak (nakit girişi), Çıkış = Borç (nakit çıkışı).
/// </summary>
public enum TransactionType
{
    Giris = 1,
    Cikis = 2
}
