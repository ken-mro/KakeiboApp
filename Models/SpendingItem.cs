using Syncfusion.Maui.DataForm;
using System.ComponentModel.DataAnnotations;

namespace KakeiboApp.Models;

public class SpendingItem
{
    public int Id { get; set; }

    [DataFormDateRange(DisplayFormat = "yyyy/MM/dd" ,MinimumDate = "2020/01/01", MaximumDate = "2100/01/01")]
    public DateTime Date { get; set; } = DateTime.Today;
    public Category Category { get; set; }
    public string Name { get; set; } = string.Empty;

    [Range(1, double.MaxValue, ErrorMessage = "金額を入力してください。")]
    public Decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
}
