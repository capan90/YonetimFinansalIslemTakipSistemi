using YonetimFinansalIslemTakipSistemi.Domain.Enums;
using YonetimFinansalIslemTakipSistemi.Domain.Extensions;

namespace YonetimFinansalIslemTakipSistemi.Tests;

public class TransactionTypeExtensionsTests
{
    [Fact]
    public void Tahsilat_ReturnsInflow()
        => Assert.Equal(FinancialDirection.Inflow, TransactionType.Tahsilat.GetFinancialDirection());

    [Fact]
    public void Odeme_ReturnsOutflow()
        => Assert.Equal(FinancialDirection.Outflow, TransactionType.Odeme.GetFinancialDirection());

    [Fact]
    public void Avans_ReturnsOutflow()
        => Assert.Equal(FinancialDirection.Outflow, TransactionType.Avans.GetFinancialDirection());

    [Fact]
    public void OzelHarcama_ReturnsOutflow()
        => Assert.Equal(FinancialDirection.Outflow, TransactionType.OzelHarcama.GetFinancialDirection());

    /// <summary>
    /// V1: Transfer tek taraflı çıkış olarak modellenir.
    /// Kasalar arası gerçek çift yönlü hareket desteklendiğinde bu test güncellenecek.
    /// Bkz. docs/roadmap.md — Transfer teknik borcu.
    /// </summary>
    [Fact]
    public void Transfer_ReturnsOutflow_V1SingleSidedRule()
        => Assert.Equal(FinancialDirection.Outflow, TransactionType.Transfer.GetFinancialDirection());

    [Theory]
    [InlineData(TransactionType.Tahsilat,    FinancialDirection.Inflow)]
    [InlineData(TransactionType.Odeme,       FinancialDirection.Outflow)]
    [InlineData(TransactionType.Avans,       FinancialDirection.Outflow)]
    [InlineData(TransactionType.OzelHarcama, FinancialDirection.Outflow)]
    [InlineData(TransactionType.Transfer,    FinancialDirection.Outflow)]
    public void AllTypes_CorrectDirection(TransactionType type, FinancialDirection expected)
        => Assert.Equal(expected, type.GetFinancialDirection());

    [Fact]
    public void UnknownValue_ThrowsArgumentOutOfRangeException()
        => Assert.Throws<ArgumentOutOfRangeException>(
               () => ((TransactionType)999).GetFinancialDirection());
}
