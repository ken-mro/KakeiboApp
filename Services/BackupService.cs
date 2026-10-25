using System.Text;
using KakeiboApp.Models;
using SQLite;

namespace KakeiboApp.Services;

/// <summary>The settings a backup carries along with the data: they change how the app opens and what it adds up.</summary>
public record BackupSettings(bool IsFingerprintEnabled, bool CarriesOver)
{
    /// <summary>「指紋認証 オン、繰り越し オフ」 (「Face ID オン」 on an iPhone)</summary>
    public string Summary => $"{Biometrics.Name} {OnOff(IsFingerprintEnabled)}、繰り越し {OnOff(CarriesOver)}";

    static string OnOff(bool value) => value ? "オン" : "オフ";
}

/// <summary>
/// How many rows of each kind a backup holds, in <see cref="BackupService.Tables"/> order, and its settings
/// (null for a backup made without them).
/// </summary>
public record BackupContents(IReadOnlyList<(string Name, int Count)> Counts, BackupSettings? Settings = null)
{
    public int Total => Counts.Sum(c => c.Count);

    /// <summary>「収入 12件、貯金 3件、…」 (kinds without rows are left out).</summary>
    public string Summary => Total == 0
        ? "記録は入っていません"
        : string.Join("、", Counts.Where(c => c.Count > 0).Select(c => $"{c.Name} {c.Count}件"));
}

/// <summary>
/// Backs the data up to one SQLite file and restores it. The backup is a copy of the app's database made with
/// VACUUM INTO, so it has the same tables as the app (Models/*Data.cs) and can be opened by any SQLite tool;
/// the settings in <see cref="BackupSettings"/> go in one more table, AppSetting, that only backups have.
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

    const string SettingsTable = "AppSetting";
    const string FingerprintKey = "IsFingerprintEnabled";
    const string CarryOverKey = "RecordsTotalRemainingToThisMonth";

    static readonly byte[] SqliteHeader = Encoding.ASCII.GetBytes("SQLite format 3\0");

    /// <summary>The file name offered when saving a backup, e.g. kakeibo-backup-20261016-0930.db3.</summary>
    public static string FileName(DateTime now) => $"kakeibo-backup-{now:yyyyMMdd-HHmm}.db3";

    /// <summary>Writes a consistent copy of the data, and the settings, to <paramref name="targetPath"/> (replaced if it exists).</summary>
    public void CreateBackup(string targetPath, BackupSettings? settings = null)
    {
        if (File.Exists(targetPath)) File.Delete(targetPath);
        using (var conn = OpenLive())
        {
            conn.Execute($"VACUUM INTO {Quote(targetPath)}");
        }
        if (settings is null) return;

        using var backup = new SQLiteConnection(targetPath);
        backup.Execute($"CREATE TABLE \"{SettingsTable}\" (\"Key\" TEXT PRIMARY KEY, \"Value\" TEXT)");
        backup.Execute($"INSERT INTO \"{SettingsTable}\" VALUES (?, ?), (?, ?)",
            FingerprintKey, settings.IsFingerprintEnabled ? "1" : "0", CarryOverKey, settings.CarriesOver ? "1" : "0");
    }

    /// <summary>Checks that <paramref name="backupPath"/> is a backup of this app and counts what it holds.</summary>
    /// <exception cref="InvalidDataException">The file is not a SQLite database with any of the app's tables.</exception>
    public static BackupContents Inspect(string backupPath)
    {
        if (!IsSqliteFile(backupPath)) throw new InvalidDataException("家計簿のバックアップのファイルではありません。");

        using var conn = new SQLiteConnection(backupPath, SQLiteOpenFlags.ReadOnly);
        var existing = TableNames(conn, "main");
        if (!Tables.Any(t => existing.Contains(t.Table))) throw new InvalidDataException("家計簿のバックアップのファイルではありません。");

        var counts = Tables
            .Select(t => (t.Name, existing.Contains(t.Table) ? conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM \"{t.Table}\"") : 0))
            .ToList();
        return new BackupContents(counts, existing.Contains(SettingsTable) ? ReadSettings(conn) : null);
    }

    static BackupSettings ReadSettings(SQLiteConnection conn)
    {
        bool Get(string key) => conn.ExecuteScalar<string>($"SELECT \"Value\" FROM \"{SettingsTable}\" WHERE \"Key\" = ?", key) == "1";
        return new BackupSettings(Get(FingerprintKey), Get(CarryOverKey));
    }

    /// <summary>
    /// Replaces all the data with <paramref name="backupPath"/>'s. The current data and <paramref name="currentSettings"/>
    /// are first saved to <paramref name="safetyCopyPath"/>. Columns are matched by name, so a backup from another
    /// version of the app restores whatever columns the two have in common. The caller applies the backup's
    /// settings (<see cref="BackupContents.Settings"/>), which are not in the app's database.
    /// </summary>
    public void Restore(string backupPath, string safetyCopyPath, BackupSettings? currentSettings = null)
    {
        Inspect(backupPath);
        CreateBackup(safetyCopyPath, currentSettings);

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
