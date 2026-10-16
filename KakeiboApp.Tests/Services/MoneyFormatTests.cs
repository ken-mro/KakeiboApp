using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class MoneyFormatTests
{
    [Theory]
    [InlineData(0, "¥0")]
    [InlineData(90000, "¥90,000")]
    [InlineData(1234567, "¥1,234,567")]
    [InlineData(-5000, "−¥5,000")]
    [InlineData(5294.12, "¥5,294")]
    [InlineData(5294.5, "¥5,295")]
    public void Yen_FormatsWholeYenWithSeparators(decimal amount, string expected)
    {
        Assert.Equal(expected, MoneyFormat.Yen(amount));
    }

    [Theory]
    [InlineData(0, "¥0")]
    [InlineData(50000, "−¥50,000")]
    public void Deduction_PrefixesAMinus(decimal amount, string expected)
    {
        Assert.Equal(expected, MoneyFormat.Deduction(amount));
    }

    [Theory]
    [InlineData("1280", 1280)]
    [InlineData("1,280", 1280)]
    [InlineData("１２８０", 1280)]
    [InlineData("¥1,280", 1280)]
    [InlineData("1280円", 1280)]
    [InlineData(" 7 ", 7)]
    public void TryParseAmount_AcceptsCommonWaysOfWritingYen(string text, decimal expected)
    {
        Assert.True(MoneyFormat.TryParseAmount(text, out var amount));
        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-100")]
    [InlineData("12.5")]
    [InlineData("abc")]
    [InlineData("1234567890123")]
    public void TryParseAmount_RejectsAnythingButAPositiveWholeAmount(string? text)
    {
        Assert.False(MoneyFormat.TryParseAmount(text, out _));
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1280, "1,280")]
    public void ForInput_LeavesZeroEmpty(decimal amount, string expected)
    {
        Assert.Equal(expected, MoneyFormat.ForInput(amount));
    }
}
