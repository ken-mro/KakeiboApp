using KakeiboApp.Models;

namespace KakeiboApp.Tests.Models;

public class SavingResultTests
{
    static readonly DateTime October = new(2026, 10, 1);

    [Fact]
    public void WithoutWithdrawal_BalanceIsTheSavedAmount()
    {
        var result = new SavingResult(October, "旅行", 20000);

        Assert.Equal("旅行", result.Name);
        Assert.Equal(20000, result.Amount);
        Assert.Equal(0, result.LivingOffSavingAmount);
        Assert.Equal(20000, result.Balance);
    }

    [Fact]
    public void WithoutWithdrawal_PreparesAnEmptyWithdrawalRowForTheMonth()
    {
        var result = new SavingResult(October, "旅行", 20000);

        Assert.Equal(October, result.LivingOffSaving.Date);
        Assert.Equal("旅行", result.LivingOffSaving.Name);
        Assert.Equal(0, result.LivingOffSaving.Amount);
    }

    [Fact]
    public void WithWithdrawal_StoresItAsAPositiveAmountAndSubtractsItFromBalance()
    {
        var withdrawal = new MonthlySaving { Date = October, Name = "旅行", Amount = -3000 };

        var result = new SavingResult(withdrawal, "旅行", 20000);

        Assert.Equal(3000, result.LivingOffSavingAmount);
        Assert.Equal(17000, result.Balance);
        Assert.Same(withdrawal, result.LivingOffSaving);
    }

    [Fact]
    public void WithWithdrawal_RejectsAPositiveWithdrawalRow()
    {
        var notAWithdrawal = new MonthlySaving { Date = October, Name = "旅行", Amount = 3000 };

        Assert.Throws<ArgumentException>(() => new SavingResult(notAWithdrawal, "旅行", 20000));
    }

    [Fact]
    public void WithWithdrawal_RejectsNull()
    {
        Assert.Throws<ArgumentException>(() => new SavingResult((MonthlySaving)null!, "旅行", 20000));
    }

    [Fact]
    public void SetLivingOffSavingAmount_UpdatesTheWithdrawalRowAsANegativeAmount()
    {
        var result = new SavingResult(October, "旅行", 20000);

        result.SetLivingOffSavingAmount(2000);

        Assert.Equal(2000, result.LivingOffSavingAmount);
        Assert.Equal(-2000, result.LivingOffSaving.Amount);
        Assert.Equal(18000, result.Balance);
    }

    [Fact]
    public void SetLivingOffSavingAmount_IgnoresNegativeAmounts()
    {
        var result = new SavingResult(October, "旅行", 20000);
        result.SetLivingOffSavingAmount(2000);

        result.SetLivingOffSavingAmount(-1);

        Assert.Equal(2000, result.LivingOffSavingAmount);
        Assert.Equal(-2000, result.LivingOffSaving.Amount);
    }

    [Fact]
    public void SetLivingOffSavingAmount_ZeroClearsTheWithdrawal()
    {
        var result = new SavingResult(October, "旅行", 20000);
        result.SetLivingOffSavingAmount(2000);

        result.SetLivingOffSavingAmount(0);

        Assert.Equal(0, result.LivingOffSavingAmount);
        Assert.Equal(20000, result.Balance);
    }
}
