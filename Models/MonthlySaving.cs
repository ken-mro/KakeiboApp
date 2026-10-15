using System;
using System.ComponentModel.DataAnnotations;

namespace KakeiboApp.Models;

public class MonthlySaving
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    [Range(1, double.MaxValue, ErrorMessage = "金額を入力してください。")]
    public decimal Amount { get; set; }
    public string Note { get; set; }
}
