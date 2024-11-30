namespace KakeiboApp.Models;

public record Category
{
    public string Name { get; set; } = string.Empty;
    public override string ToString()
    {
        return Name;
    }
}
