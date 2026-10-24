using KakeiboApp.Services;
using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Tests.Services;

public class EntrySearchTests
{
    static EntryRow Spending(string name = "", string note = "", string? category = "食費", decimal amount = 980) => new()
    {
        Kind = EntryKind.Spending, Name = name, Note = note, Category = category, Amount = amount, Source = new object(),
    };

    static EntryRow Special(string name = "", string fromWhere = "", decimal amount = 30000) => new()
    {
        Kind = EntryKind.SpecialExpense, Name = name, FromWhere = fromWhere, Amount = amount, Source = new object(),
    };

    static bool Matches(EntryRow row, string query) => EntrySearch.Matches(row, EntrySearch.Terms(query));

    [Theory]
    [InlineData("スーパー")]
    [InlineData("ーパ")]
    [InlineData("すーぱー")]
    [InlineData("ｽｰﾊﾟｰ")]
    public void Name_MatchesPartsIgnoringKanaTypeAndWidth(string query) =>
        Assert.True(Matches(Spending(name: "スーパーで買い物"), query));

    [Theory]
    [InlineData("coffee")]
    [InlineData("ＣＯＦＦＥＥ")]
    public void Note_MatchesIgnoringCaseAndWidth(string query) =>
        Assert.True(Matches(Spending(note: "Coffee beans"), query));

    [Fact]
    public void Category_Matches() => Assert.True(Matches(Spending(category: "日用品"), "日用"));

    [Fact]
    public void FromWhere_Matches() => Assert.True(Matches(Special(name: "旅行", fromWhere: "旅行の貯金"), "貯金"));

    [Theory]
    [InlineData("980")]
    [InlineData("¥980")]
    [InlineData("９８０円")]
    public void Amount_MatchesTheSameAmount(string query) => Assert.True(Matches(Spending(amount: 980), query));

    [Fact]
    public void Amount_DoesNotMatchPartOfAnAmount() => Assert.False(Matches(Spending(amount: 1980), "980"));

    [Fact]
    public void Amount_WithCommaMatches() => Assert.True(Matches(Special(amount: 30000), "30,000"));

    [Fact]
    public void Terms_AreAllRequired()
    {
        var row = Spending(name: "ランチ", category: "食費", amount: 1200);
        Assert.True(Matches(row, "ランチ 1200"));
        Assert.True(Matches(row, "食費　ランチ")); // 全角スペースでも区切る
        Assert.False(Matches(row, "ランチ 娯楽"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyQuery_MatchesNothing(string? query) =>
        Assert.False(EntrySearch.Matches(Spending(name: "ランチ"), EntrySearch.Terms(query)));

    [Fact]
    public void NoMatch_ReturnsFalse() => Assert.False(Matches(Spending(name: "ランチ", note: "同僚と"), "映画"));

    static readonly DateTime Today = new(2026, 10, 25);

    [Theory]
    [InlineData(EntrySearch.Period.ThisMonth, "2026-10-01", "2026-10-31")]
    [InlineData(EntrySearch.Period.LastMonth, "2026-09-01", "2026-09-30")]
    [InlineData(EntrySearch.Period.ThisYear, "2026-01-01", "2026-12-31")]
    public void Range_OfAPreset_IsAsOfToday(EntrySearch.Period period, string from, string to) =>
        Assert.Equal((DateTime.Parse(from), DateTime.Parse(to)), EntrySearch.Range(period, Today, default, default));

    [Fact]
    public void Range_InJanuary_LastMonthIsDecemberOfTheYearBefore() =>
        Assert.Equal((new DateTime(2025, 12, 1), new DateTime(2025, 12, 31)),
            EntrySearch.Range(EntrySearch.Period.LastMonth, new DateTime(2026, 1, 10), default, default));

    [Fact]
    public void Range_Custom_PutsTheDatesInOrderWithoutTheirTime() =>
        Assert.Equal((new DateTime(2026, 3, 1), new DateTime(2026, 4, 30)),
            EntrySearch.Range(EntrySearch.Period.Custom, Today, new DateTime(2026, 4, 30, 9, 0, 0), new DateTime(2026, 3, 1)));

    [Fact]
    public void Range_All_IsNone() => Assert.Null(EntrySearch.Range(EntrySearch.Period.All, Today, default, default));

    [Fact]
    public void InRange_IncludesBothEnds()
    {
        var range = (new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));
        Assert.True(EntrySearch.InRange(new DateTime(2026, 10, 31, 23, 0, 0), range));
        Assert.True(EntrySearch.InRange(new DateTime(2026, 10, 1), range));
        Assert.False(EntrySearch.InRange(new DateTime(2026, 11, 1), range));
        Assert.True(EntrySearch.InRange(new DateTime(2000, 1, 1), null));
    }
}
