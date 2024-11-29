using SQLite;

namespace KakeiboApp.Models;

[Table("Category")]
public class CategoryData
{
    [PrimaryKey, MaxLength(168)]
    public string Name { get; set; } = string.Empty;
}
