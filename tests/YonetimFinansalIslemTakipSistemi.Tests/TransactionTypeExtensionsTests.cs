using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class TransactionTypeExtensionsTests
{
    [Fact]
    public void Giris_ReturnsInflow()
        => Assert.Equal(FinancialDirection.Inflow, TransactionType.Giris.GetFinancialDirection());

    [Fact]
    public void Cikis_ReturnsOutflow()
        => Assert.Equal(FinancialDirection.Outflow, TransactionType.Cikis.GetFinancialDirection());

    [Theory]
    [InlineData(TransactionType.Giris, FinancialDirection.Inflow)]
    [InlineData(TransactionType.Cikis, FinancialDirection.Outflow)]
    public void AllTypes_CorrectDirection(TransactionType type, FinancialDirection expected)
        => Assert.Equal(expected, type.GetFinancialDirection());

    [Fact]
    public void UnknownValue_ThrowsArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(
               () => ((TransactionType)999).GetFinancialDirection());
}
