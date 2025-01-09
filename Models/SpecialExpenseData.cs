using SQLite;

namespace KakeiboApp.Models;

[Table("SpecialExpense")]
public class SpecialExpenseData
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    [MaxLength(168)]
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public DateTime? CollectionDate { get; set; } = null;
    public string FromWhere { get; set; } = string.Empty;
    [MaxLength(168)]
    public string Note { get; set; } = string.Empty;
}
