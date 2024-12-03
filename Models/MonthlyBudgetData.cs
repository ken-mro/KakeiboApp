using SQLite;

namespace KakeiboApp.Models;

[Table("MonthlyBudget")]
public class MonthlyBudgetData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    [MaxLength(168)]
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
