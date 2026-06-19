using YonetimFinansalIslemTakipSistemi.Domain.Common;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Domain.Entities;

public class ExchangeRate : BaseEntity
{
    /// <summary>
    /// Kur tarihi (UTC gece yarısı). Aynı gün + para birimi için yalnızca bir kayıt bulunur.
    /// </summary>
    public DateTime RateDate { get; set; }

    /// <summary>
    /// Para birimi — yalnızca USD ve EUR. TRY handler'da reddedilir.
    /// </summary>
    public CurrencyType CurrencyType { get; set; }

    /// <summary>Döviz alış kuru.</summary>
    public decimal ForexBuying { get; set; }

    /// <summary>Döviz satış kuru.</summary>
    public decimal ForexSelling { get; set; }
}
