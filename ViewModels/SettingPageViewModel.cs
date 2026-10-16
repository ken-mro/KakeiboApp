using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Repository;
using KakeiboApp.Services;

namespace KakeiboApp.ViewModels;

/// <summary>
/// A choice of the テーマ setting.
/// </summary>
public partial class ThemeOption(string label, AppTheme theme) : ObservableObject
{
    public string Label { get; } = label;
    public AppTheme Theme { get; } = theme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    bool _isSelected;

    public string Description => IsSelected ? $"{Label}、選択中" : Label;
}

public partial class SettingPageViewModel : BaseViewModel
{
    private readonly SettingPreferences _settingPreferences;
    private readonly GuideService _guide;
    private readonly BackupService _backup;

    public SettingPageViewModel(SettingPreferences settingPreferences, GuideService guide, BackupService backup)
    {
        _settingPreferences = settingPreferences;
        _guide = guide;
        _backup = backup;
        Themes =
        [
            new ThemeOption("システム", AppTheme.Unspecified),
            new ThemeOption("ライト", AppTheme.Light),
            new ThemeOption("ダーク", AppTheme.Dark),
        ];
        MarkSelectedTheme();
    }

    public IReadOnlyList<ThemeOption> Themes { get; }

    public bool IsFingerprintEnabled
    {
        get => _settingPreferences.IsFingerprintEnabled;
        set
        {
            _settingPreferences.IsFingerprintEnabled = value;
            OnPropertyChanged();
        }
    }

    public bool RecordsTotalRemainingToThisMonth
    {
        get => _settingPreferences.RecordsTotalRemainingToThisMonth;
        set
        {
            _settingPreferences.RecordsTotalRemainingToThisMonth = value;
            OnPropertyChanged();
        }
    }

    public string VersionText => $"バージョン {GuideService.CurrentVersion}";

    [RelayCommand]
    void SelectTheme(ThemeOption option)
    {
        // Stored as the AppTheme name, as before ("Unspecified", "Light", "Dark").
        _settingPreferences.Theme = option.Theme.ToString();
        MarkSelectedTheme();
    }

    void MarkSelectedTheme()
    {
        foreach (var option in Themes)
        {
            option.IsSelected = option.Theme.ToString() == _settingPreferences.Theme;
        }
    }

    [RelayCommand]
    Task OpenGuideAsync() => _guide.ShowAsync();

    public IReadOnlyList<GuideTopic> GuideTopics => GuideTour.Topics;

    [RelayCommand]
    Task OpenTopicAsync(GuideTopic topic) => _guide.ShowTopicAsync(topic);

    // The current data, kept inside the app just before a restore replaces it.
    static string SafetyCopyPath => Path.Combine(FileSystem.AppDataDirectory, "before-restore.db3");

    /// <summary>バックアップを作成: copies the data to one file and lets the user choose where to save it.</summary>
    [RelayCommand]
    async Task CreateBackupAsync()
    {
        var fileName = BackupService.FileName(DateTime.Now);
        var snapshot = Path.Combine(FileSystem.CacheDirectory, fileName);
        try
        {
            await Task.Run(() => _backup.CreateBackup(snapshot));
            FileSaverResult result;
            await using (var stream = File.OpenRead(snapshot))
            {
                result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
            }

            if (result.IsSuccessful)
            {
                await Shell.Current.DisplayAlertAsync("バックアップを保存しました",
                    $"{result.FilePath}\n\nこのファイルを「バックアップから復元」で選ぶと、今の記録に戻せます。", "OK");
            }
            else if (result.Exception is not null && !IsCancel(result.Exception))
            {
                System.Diagnostics.Debug.WriteLine(result.Exception);
                await Shell.Current.DisplayAlertAsync("保存できませんでした", "保存する場所を変えて、もう一度ためしてください。", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("バックアップを作成できませんでした", "時間をおいて、もう一度ためしてください。", "OK");
        }
        finally
        {
            TryDelete(snapshot);
        }
    }

    /// <summary>バックアップから復元: shows what the chosen file holds and, once confirmed, replaces the data with it.</summary>
    [RelayCommand]
    async Task RestoreBackupAsync()
    {
        FileResult? picked;
        try
        {
            picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "バックアップのファイルを選んでください" });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return;
        }
        if (picked is null) return;

        // The picked file may be a content URI on Android; copy it so SQLite can open it.
        var copy = Path.Combine(FileSystem.CacheDirectory, "restore.db3");
        try
        {
            await using (var source = await picked.OpenReadAsync())
            await using (var target = File.Create(copy))
            {
                await source.CopyToAsync(target);
            }

            BackupContents contents;
            try
            {
                contents = await Task.Run(() => BackupService.Inspect(copy));
            }
            catch (Exception ex) when (ex is InvalidDataException or SQLite.SQLiteException)
            {
                await Shell.Current.DisplayAlertAsync("このファイルは使えません",
                    "家計簿のバックアップのファイル（「バックアップを作成」で保存したファイル）を選んでください。", "OK");
                return;
            }

            var confirmed = await Shell.Current.DisplayAlertAsync("このバックアップで置き換えますか？",
                $"{picked.FileName}\n{contents.Summary}\n\n今の記録はすべて、このバックアップの内容に置き換わります。", "置き換える", "やめる");
            if (!confirmed) return;

            await Task.Run(() => _backup.Restore(copy, SafetyCopyPath));
            await Shell.Current.DisplayAlertAsync("復元しました", "バックアップの記録に戻しました。", "OK");
            await Shell.Current.GoToAsync("//MainPage", false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("復元できませんでした", "記録は変わっていません。時間をおいて、もう一度ためしてください。", "OK");
        }
        finally
        {
            TryDelete(copy);
        }
    }

    static bool IsCancel(Exception ex) =>
        ex is OperationCanceledException || ex.Message.Contains("cancel", StringComparison.OrdinalIgnoreCase);

    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
