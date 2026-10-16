using KakeiboApp.Models;
using KakeiboApp.Services;
using SQLite;

namespace KakeiboApp.Tests.Services;

public sealed class BackupServiceTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "kakeibo-backup-tests", Guid.NewGuid().ToString("N"));
    readonly string _live;
    readonly BackupService _service;

    public BackupServiceTests()
    {
        Directory.CreateDirectory(_dir);
        _live = Path.Combine(_dir, "KakeiboApp.db3");
        _service = new BackupService(_live);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    string PathOf(string name) => Path.Combine(_dir, name);

    SQLiteConnection Live()
    {
        var conn = new SQLiteConnection(_live);
        conn.CreateTable<MonthlyIncomeData>();
        conn.CreateTable<SpendingItemData>();
        conn.CreateTable<CategoryData>();
        return conn;
    }

    void Seed()
    {
        using var conn = Live();
        conn.Insert(new MonthlyIncomeData { Date = new DateTime(2026, 10, 1), Name = "給料", Amount = 300000 });
        conn.Insert(new SpendingItemData { Date = new DateTime(2026, 10, 3), Category = "食費", Name = "スーパー", Amount = 2480 });
        conn.Insert(new SpendingItemData { Date = new DateTime(2026, 10, 4), Category = "食費", Name = "ランチ", Amount = 980 });
        conn.Insert(new CategoryData { Name = "食費" });
    }

    [Fact]
    public void Backup_HoldsEveryRow()
    {
        Seed();
        var backup = PathOf("backup.db3");

        _service.CreateBackup(backup);
        var contents = BackupService.Inspect(backup);

        Assert.Equal(4, contents.Total);
        Assert.Equal("収入 1件、生活費 2件、種類 1件", contents.Summary);
    }

    [Fact]
    public void Restore_PutsTheBackupBack_AndKeepsTheReplacedDataAside()
    {
        Seed();
        var backup = PathOf("backup.db3");
        _service.CreateBackup(backup);
        using (var conn = Live())
        {
            conn.Insert(new SpendingItemData { Date = new DateTime(2026, 10, 5), Category = "娯楽", Name = "映画", Amount = 2000 });
            conn.Execute("DELETE FROM MonthlyIncome");
        }

        var safety = PathOf("before-restore.db3");
        _service.Restore(backup, safety);

        using (var conn = Live())
        {
            Assert.Equal(["ランチ", "スーパー"], conn.Table<SpendingItemData>().OrderByDescending(x => x.Date).ToList().Select(x => x.Name));
            Assert.Equal(300000, conn.Table<MonthlyIncomeData>().Single().Amount);
        }
        // The data that was replaced: 3 spending items and no income.
        Assert.Equal("生活費 3件、種類 1件", BackupService.Inspect(safety).Summary);
    }

    [Fact]
    public void Restore_OfAnEmptyBackup_EmptiesTheData()
    {
        _service.CreateBackup(PathOf("empty.db3"));
        Seed();

        _service.Restore(PathOf("empty.db3"), PathOf("before-restore.db3"));

        Assert.Equal(0, BackupService.Inspect(_live).Total);
    }

    [Fact]
    public void Restore_KeepsTheIdsSoRowsStayDistinct()
    {
        Seed();
        _service.CreateBackup(PathOf("backup.db3"));
        _service.Restore(PathOf("backup.db3"), PathOf("before-restore.db3"));

        using var conn = Live();
        conn.Insert(new SpendingItemData { Date = new DateTime(2026, 10, 6), Category = "食費", Name = "パン", Amount = 300 });
        Assert.Equal([1, 2, 3], conn.Table<SpendingItemData>().ToList().Select(x => x.Id).Order());
    }

    [Fact]
    public void Inspect_RejectsAFileThatIsNotADatabase()
    {
        var path = PathOf("note.txt");
        File.WriteAllText(path, "これはメモです");

        Assert.Throws<InvalidDataException>(() => BackupService.Inspect(path));
    }

    [Fact]
    public void Inspect_RejectsADatabaseOfAnotherApp()
    {
        var path = PathOf("other.db3");
        using (var conn = new SQLiteConnection(path)) conn.Execute("CREATE TABLE Song (Title TEXT)");

        Assert.Throws<InvalidDataException>(() => BackupService.Inspect(path));
    }

    [Fact]
    public void Restore_OfABadFile_LeavesTheDataAsItWas()
    {
        Seed();
        var path = PathOf("note.txt");
        File.WriteAllText(path, "これはメモです");

        Assert.Throws<InvalidDataException>(() => _service.Restore(path, PathOf("before-restore.db3")));
        Assert.Equal(4, BackupService.Inspect(_live).Total);
    }

    [Fact]
    public void Backup_CarriesTheSettings()
    {
        Seed();
        var backup = PathOf("backup.db3");

        _service.CreateBackup(backup, new BackupSettings(IsFingerprintEnabled: true, CarriesOver: false));
        var contents = BackupService.Inspect(backup);

        Assert.Equal(new BackupSettings(true, false), contents.Settings);
        Assert.Equal("指紋認証 オン、繰り越し オフ", contents.Settings!.Summary);
        // The settings table is not counted as records.
        Assert.Equal(4, contents.Total);
    }

    [Fact]
    public void Backup_WithoutSettings_HasNone()
    {
        Seed();
        _service.CreateBackup(PathOf("backup.db3"));

        Assert.Null(BackupService.Inspect(PathOf("backup.db3")).Settings);
    }

    [Fact]
    public void Restore_KeepsTheReplacedSettingsAside_AndDoesNotAddTheSettingsTableToTheData()
    {
        Seed();
        _service.CreateBackup(PathOf("backup.db3"), new BackupSettings(false, true));

        _service.Restore(PathOf("backup.db3"), PathOf("before-restore.db3"), new BackupSettings(true, false));

        Assert.Equal(new BackupSettings(true, false), BackupService.Inspect(PathOf("before-restore.db3")).Settings);
        Assert.Null(BackupService.Inspect(_live).Settings);
    }

    [Fact]
    public void FileName_HasTheDateAndTime() =>
        Assert.Equal("kakeibo-backup-20261016-0930.db3", BackupService.FileName(new DateTime(2026, 10, 16, 9, 30, 0)));
}
