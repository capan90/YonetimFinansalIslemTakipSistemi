namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;

/// <summary>
/// Listeleme ekranı için düz read model.
/// Enum'lar görüntü string'lerine dönüştürülmüş şekilde taşınır; XAML'da converter gerekmez.
/// Balance alanları bu işlem uygulandıktan sonraki kümülatif kasa bakiyelerini gösterir.
/// Borç = Çıkış işlemi tutarı, Alacak = Giriş işlemi tutarı; diğeri 0.
/// </summary>
public class CashTransactionDto
{
    public Guid     Id                     { get; set; }
    public DateTime TransactionDate        { get; set; }
    public string   TransactionTypeDisplay { get; set; } = string.Empty;
    public string   CurrencyTypeDisplay    { get; set; } = string.Empty;
    /// <summary>Giriş işlemlerinde dolu, Çıkış işlemlerinde 0.</summary>
    public decimal  Borc                   { get; set; }
    /// <summary>Çıkış işlemlerinde dolu, Giriş işlemlerinde 0.</summary>
    public decimal  Alacak                 { get; set; }
    public string   Description            { get; set; } = string.Empty;
    public DateTime CreatedAt              { get; set; }

    // Bu işlem sonrası her para biriminin kümülatif bakiyesi.
    // Filtre aktifse bile tarihsel doğruluk korunur; tüm önceki işlemler hesaba katılır.
    public decimal TlBalanceAfter  { get; set; }
    public decimal UsdBalanceAfter { get; set; }
    public decimal EurBalanceAfter { get; set; }
}
