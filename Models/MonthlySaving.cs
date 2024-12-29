using System;
using System.ComponentModel.DataAnnotations;

namespace KakeiboApp.Models;

public class MonthlySaving
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Name { get; set; }
    [Range(1, double.MaxValue, ErrorMessage = "���z����͂��Ă��������B")]
    public decimal Amount { get; set; }
    public string Note { get; set; }
}
