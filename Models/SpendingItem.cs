namespace KakeiboApp.Models;

public record SpendingItem
{
    public int Id { get; init; }
    public required DateTime Date { get; init; }
    public required Category Category { get; init; }
    public required string Name { get; init; } = string.Empty;
    public required Decimal Amount { get; init; }
    public string Note { get; init; } = string.Empty;
}
