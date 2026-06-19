namespace YonetimFinansalIslemTakipSistemi.Application.Features.CashTransactions.Queries.GetCashTransactions;

/// <summary>
/// Listeleme ekranı için düz read model.
/// Enum'lar görüntü string'lerine dönüştürülmüş şekilde taşınır; XAML'da converter gerekmez.
/// Balance alanları bu işlem uygulandıktan sonraki kümülatif kasa bakiyelerini gösterir.
/// </summary>
public class CashTransactionDto
{
    public Guid     Id                     { get; set; }
    public DateTime TransactionDate        { get; set; }
    public string   TransactionTypeDisplay { get; set; } = string.Empty;
    public string   CurrencyTypeDisplay    { get; set; } = string.Empty;
    public decimal  Amount                 { get; set; }
    public string   Description            { get; set; } = string.Empty;
    public DateTime CreatedAt              { get; set; }

    // Bu işlem sonrası her para biriminin kümülatif bakiyesi.
    // Filtre aktifse bile tarihsel doğruluk korunur; tüm önceki işlemler hesaba katılır.
    public decimal TlBalanceAfter  { get; set; }
    public decimal UsdBalanceAfter { get; set; }
    public decimal EurBalanceAfter { get; set; }
}
