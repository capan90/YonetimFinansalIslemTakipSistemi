using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Application.Features.Analysis.Queries.GetDashboard;

public class GetDashboardQuery
{
    public DateTime?        StartDate           { get; set; }
    public DateTime?        EndDate             { get; set; }
    public CurrencyType?    CurrencyType        { get; set; }
    public TransactionType? TransactionType     { get; set; }
    public string?          DescriptionContains { get; set; }
}
