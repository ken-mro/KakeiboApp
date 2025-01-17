using System;
using System.ComponentModel.DataAnnotations;

namespace KakeiboApp.Models;

public class SpecialExpense
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [Range(1, double.MaxValue, ErrorMessage = "‹àŠz‚ğ“ü—Í‚µ‚Ä‚­‚¾‚³‚¢B")]
    public decimal Amount { get; set; }
    public DateTime? CollectionDate { get; set; } = null;
    public string FromWhere { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
