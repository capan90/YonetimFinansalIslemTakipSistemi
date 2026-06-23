namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

/// <summary>
/// Kullanıcı başına DataGrid kolon düzenini saklar.
/// ScreenKey ile farklı ekranlar için ayrı düzenler tutulabilir.
/// </summary>
public class UserGridLayout
{
    public Guid     Id         { get; set; }
    public Guid     UserId     { get; set; }
    /// <summary>Ekran tanımlayıcısı. Örnek: "CashTransactionList"</summary>
    public string   ScreenKey  { get; set; } = string.Empty;
    /// <summary>JSON formatında kolon durumları (key, görünürlük, sıra, genişlik).</summary>
    public string   LayoutJson { get; set; } = string.Empty;
    public DateTime UpdatedAt  { get; set; }
}
