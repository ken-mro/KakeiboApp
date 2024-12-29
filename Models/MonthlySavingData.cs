using SQLite;

namespace KakeiboApp.Models;

[Table("MonthlySaving")]
public class MonthlySavingData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    [MaxLength(168)]
    public string Name { get; set; }
    public decimal Amount { get; set; }
    [MaxLength(168)]
    public string Note { get; set; }
}