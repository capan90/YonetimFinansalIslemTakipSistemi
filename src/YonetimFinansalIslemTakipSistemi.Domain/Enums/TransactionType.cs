namespace YonetimFinansalIslemTakipSistemi.Domain.Enums;

/// <summary>
/// Sistem içinde desteklenen finansal işlem tiplerini tanımlar.
/// </summary>
public enum TransactionType
{
    Tahsilat = 1,
    Odeme = 2,
    Avans = 3,
    OzelHarcama = 4,
    Transfer = 5
}
