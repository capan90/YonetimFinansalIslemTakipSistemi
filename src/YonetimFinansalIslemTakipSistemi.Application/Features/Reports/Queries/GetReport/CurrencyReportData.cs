using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Reports.Queries.GetReport;

/// <summary>
/// Repository'nin döndürdüğü ham aggregate satırı.
/// Infrastructure veya EF Core bağımlılığı içermez.
/// Handler bu satırları FinancialDirection kuralına göre DTO'ya dönüştürür.
/// </summary>
public record CurrencyReportData(
    CurrencyType    Currency,
    TransactionType TransactionType,
    decimal         TotalAmount,
    int             Count);
