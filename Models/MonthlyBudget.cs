namespace KakeiboApp.Models;

public class MonthlyBudget
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public Category Category { get; set; } = new ();
    public decimal Amount { get; set; }
}
