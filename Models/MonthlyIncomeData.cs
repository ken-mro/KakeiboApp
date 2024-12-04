﻿using SQLite;

namespace KakeiboApp.Models;

[Table("MonthlyIncome")]
public class MonthlyIncomeData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    [MaxLength(168)]
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    [MaxLength(168)]
    public string Note { get; set; } = string.Empty;
}