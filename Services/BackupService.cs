using System.Text;
using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Services;

/// <summary>How many rows of each kind a backup holds, in <see cref="BackupService.Tables"/> order.</summary>
public record BackupContents(IReadOnlyList<(string Name, int Count)> Counts)
{
    public int Total => Counts.Sum(c => c.Count);

    /// <summary>「収入 12件、貯金 3件、…」 (kinds without rows are left out).</summary>
    public string Summary => Total == 0
        ? "記録は入っていません"
        : string.Join("、", Counts.Where(c => c.Count > 0).Select(c => $"{c.Name} {c.Count}件"));
}

/// <summary>
/// Backs the data up to one SQLite file and restores it. The backup is a copy of the app's database made with
/// VACUUM INTO, so it has the same tables as the app (Models/*Data.cs) and can be opened by any SQLite tool.
/// Restoring replaces the rows of every table inside one transaction instead of swapping the database file,
/// so the repositories' open connections keep working and a failed restore leaves the data as it was.
/// </summary>
public class BackupService(string dataBasePath)
{
    /// <summary>The app's tables and what they are called on screen.</summary>
    public static readonly IReadOnlyList<(string Table, string Name)> Tables =
    [
        ("MonthlyIncome", "収入"),
        ("MonthlySaving", "貯金"),
        ("MonthlyFixedCost", "固定費"),
        ("SpendingItem", "生活費"),
        ("SpecialExpense", "特別出費"),
        ("MonthlyBudget", "予算"),
        ("Category", "種類"),
    ];

    static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

    /// <summary>The file name offered when saving a backup, e.g. kakeibo-backup-20261016-0930.db3.</summary>
    public static string FileName(DateTime now) => $"kakeibo-backup-{now:yyyyMMdd-HHmm}.db3";

    /// <summary>Writes a consistent copy of the data to <paramref name="targetPath"/> (replaced if it exists).</summary>
    public void CreateBackup(string targetPath)
    {
        if (File.Exists(targetPath)) File.Delete(targetPath);
        using var conn = OpenLive();
        conn.Execute($"VACUUM INTO {Quote(targetPath)}");
    }

    /// <summary>Checks that <paramref name="backupPath"/> is a backup of this app and counts what it holds.</summary>
    /// <exception cref="InvalidDataException">The file is not a SQLite database with any of the app's tables.</exception>
    public static BackupContents Inspect(string backupPath)
    {
        if (!IsSqliteFile(backupPath)) throw new InvalidDataException("家計簿のバックアップのファイルではありません。");

        using var conn = new SQLiteConnection(backupPath, SQLiteOpenFlags.ReadOnly);
        var existing = TableNames(conn, "main");
        if (!Tables.Any(t => existing.Contains(t.Table))) throw new InvalidDataException("家計簿のバックアップのファイルではありません。");

        return new BackupContents(Tables
            .Select(t => (t.Name, existing.Contains(t.Table) ? conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM \"{t.Table}\"") : 0))
            .ToList());
    }

    /// <summary>
    /// Replaces all the data with <paramref name="backupPath"/>'s. The current data is first saved to
    /// <paramref name="safetyCopyPath"/>. Columns are matched by name, so a backup from another version of the
    /// app restores whatever columns the two have in common.
    /// </summary>
    public void Restore(string backupPath, string safetyCopyPath)
    {
        Inspect(backupPath);
        CreateBackup(safetyCopyPath);

        using var conn = OpenLive();
        conn.Execute($"ATTACH DATABASE {Quote(backupPath)} AS backup");
        try
        {
            var inBackup = TableNames(conn, "backup");
            conn.RunInTransaction(() =>
            {
                foreach (var (table, _) in Tables)
                {
                    conn.Execute($"DELETE FROM main.\"{table}\"");
                    if (!inBackup.Contains(table)) continue;

                    var columns = Columns(conn, "main", table).Intersect(Columns(conn, "backup", table)).ToList();
                    if (columns.Count == 0) continue;
                    var list = string.Join(", ", columns.Select(c => $"\"{c}\""));
                    conn.Execute($"INSERT INTO main.\"{table}\" ({list}) SELECT {list} FROM backup.\"{table}\"");
                }
            });
        }
        finally
        {
            conn.Execute("DETACH DATABASE backup");
        }
    }

    // The app's database, with every table created so a backup of a new install has them all.
    SQLiteConnection OpenLive()
    {
        var conn = new SQLiteConnection(dataBasePath) { BusyTimeout = TimeSpan.FromSeconds(5) };
        conn.CreateTable<MonthlyIncomeData>();
        conn.CreateTable<MonthlySavingData>();
        conn.CreateTable<MonthlyFixedCostData>();
        conn.CreateTable<SpendingItemData>();
        conn.CreateTable<SpecialExpenseData>();
        conn.CreateTable<MonthlyBudgetData>();
        conn.CreateTable<CategoryData>();
        return conn;
    }

    static bool IsSqliteFile(string path)
    {
        if (!File.Exists(path)) return false;
        var header = new byte[SqliteHeader.Length];
        using var stream = File.OpenRead(path);
        return stream.Read(header, 0, header.Length) == header.Length && header.SequenceEqual(SqliteHeader);
    }

    static HashSet<string> TableNames(SQLiteConnection conn, string schema) =>
        conn.QueryScalars<string>($"SELECT name FROM {schema}.sqlite_master WHERE type = 'table'").ToHashSet();

    static List<string> Columns(SQLiteConnection conn, string schema, string table) =>
        conn.Query<ColumnInfo>($"PRAGMA {schema}.table_info(\"{table}\")").Select(c => c.Name).ToList();

    static string Quote(string path) => "'" + path.Replace("'", "''") + "'";

    class ColumnInfo
    {
        [Column("name")] public string Name { get; set; } = string.Empty;
    }
}
