using System.ComponentModel.DataAnnotations;

namespace KakeiboApp.Models;

public class SpendingItem
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public Category Category { get; set; }
    public string Name { get; set; } = string.Empty;

    [Range(1, double.MaxValue, ErrorMessage = "金額を入力してください。")]
    public Decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
