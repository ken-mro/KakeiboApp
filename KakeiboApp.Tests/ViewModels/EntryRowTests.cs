using KakeiboApp.ViewModels.Entries;

namespace KakeiboApp.Tests.ViewModels;

public class EntryRowTests
{
    static EntryRow Spending(string name, string note, string category = "食費") =>
        new() { Kind = EntryKind.Spending, Name = name, Note = note, Category = category, Amount = 980, Source = new object() };

    [Fact]
    public void CleanNote_KeepsTheLinesAsTyped()
    {
        Assert.Equal("牛乳\n  卵\n\nパン", EntryRow.CleanNote("牛乳\r\n  卵\r\n\r\nパン"));
    }

    [Fact]
    public void CleanNote_DropsBlankLinesAroundIt_AndSpacesAtLineEnds()
    {
        Assert.Equal("牛乳\n卵", EntryRow.CleanNote("\n\n牛乳  \n卵\n \n"));
        Assert.Equal(string.Empty, EntryRow.CleanNote(" \n\n "));
        Assert.Equal(string.Empty, EntryRow.CleanNote(null));
    }

    [Fact]
    public void Memo_IsShownOnItsOwnLines_NotJoinedToTheCategory()
    {
        var row = Spending("スーパー", "牛乳\n卵");

        Assert.Equal("食費", row.Subtitle);
        Assert.Equal("牛乳\n卵", row.Memo);
        Assert.True(row.HasMemo);
    }

    [Fact]
    public void Subtitle_IsEmpty_WhenTheCategoryIsAlreadyTheTitle()
    {
        var row = Spending(string.Empty, "牛乳");

        Assert.Equal("食費", row.Title);
        Assert.False(row.HasSubtitle);
        Assert.Equal("牛乳", row.Memo);
    }

    [Fact]
    public void SpecialExpense_ShowsWhereItWasPaidFrom_AndTheMemoApart()
    {
        var row = new EntryRow { Kind = EntryKind.SpecialExpense, Name = "旅行", FromWhere = "旅行の貯金", Note = "宿\n電車", Source = new object() };

        Assert.Equal("旅行の貯金から", row.Subtitle);
        Assert.Equal("宿\n電車", row.Memo);
    }

    [Fact]
    public void Description_ReadsTheMemoOnOneLine()
    {
        var row = Spending("スーパー", "牛乳\n卵");

        Assert.Equal("スーパー、食費、¥980、メモ 牛乳 卵", row.Description);
    }
}
