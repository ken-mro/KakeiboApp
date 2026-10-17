using KakeiboApp.Services;

namespace KakeiboApp.Tests.Services;

public class TitleBreakTests
{
    [Theory]
    [InlineData("2026年の特別出費", "2026年の", "特別出費")]
    [InlineData("10月の生活費", "10月の", "生活費")]
    [InlineData("貯金の内訳・切り崩し", "貯金の", "内訳・切り崩し")]
    public void Split_BreaksAfterTheFirstNo(string title, string lead, string rest) =>
        Assert.Equal((lead, rest), TitleBreak.Split(title));

    [Theory]
    [InlineData("生活費をさがす")]
    [InlineData("ことの")]
    public void Split_WithoutABreak_KeepsTheTitleWhole(string title) =>
        Assert.Equal((title, string.Empty), TitleBreak.Split(title));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Split_OfNothing_IsEmpty(string? title) =>
        Assert.Equal((string.Empty, string.Empty), TitleBreak.Split(title));
}
