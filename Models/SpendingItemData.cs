using SQLite;

namespace KakeiboApp.Models;

[Table("SpendingItem")]
public record SpendingItemData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; init; }
    public DateTime Date { get; init; }
    [MaxLength(168)]
    public string Category { get; init; } = string.Empty;
    [MaxLength(168)]
    public string Name { get; init; } = string.Empty;
    public Decimal Amount { get; init; }
    public string Note { get; init; } = string.Empty;
}
