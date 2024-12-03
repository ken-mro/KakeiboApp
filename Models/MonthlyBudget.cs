namespace KakeiboApp.Models;

public class MonthlyBudget
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
