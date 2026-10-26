using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Repository;
using KakeiboApp.Services;
using Plugin.Maui.Biometric;

namespace KakeiboApp.ViewModels;

/// <summary>
/// A chip of a setting with a few choices, one of them selected.
/// </summary>
public partial class ChoiceOption(string label) : ObservableObject
{
    public string Label { get; } = label;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    bool _isSelected;

    public string Description => IsSelected ? $"{Label}、選択中" : Label;
}

/// <summary>
/// A choice of the テーマ setting.
/// </summary>
public class ThemeOption(string label, AppTheme theme) : ChoiceOption(label)
{
    public AppTheme Theme { get; } = theme;
}

/// <summary>
/// A choice of the 「＋ つける」の位置 setting.
/// </summary>
public class SideOption(string label, bool onLeft) : ChoiceOption(label)
{
    public bool OnLeft { get; } = onLeft;
}

public partial class SettingPageViewModel : BaseViewModel
{
    private readonly SettingPreferences _settingPreferences;
    private readonly GuideService _guide;
    private readonly BackupService _backup;
    private readonly IBiometric _biometric;

    public SettingPageViewModel(SettingPreferences settingPreferences, GuideService guide, BackupService backup, IBiometric biometric)
    {
        _settingPreferences = settingPreferences;
        _guide = guide;
        _backup = backup;
        _biometric = biometric;
        Themes =
        [
            new ThemeOption("システム", AppTheme.Unspecified),
            new ThemeOption("ライト", AppTheme.Light),
            new ThemeOption("ダーク", AppTheme.Dark),
        ];
        MarkSelectedTheme();
        Sides =
        [
            new SideOption("右", onLeft: false),
            new SideOption("左", onLeft: true),
        ];
        MarkSelectedSide();
    }

    public IReadOnlyList<ThemeOption> Themes { get; }

    public IReadOnlyList<SideOption> Sides { get; }

    [RelayCommand]
    void SelectSide(SideOption option)
    {
        _settingPreferences.QuickAddOnLeft = option.OnLeft;
        Views.Controls.QuickAddButton.OnLeft = option.OnLeft;
        MarkSelectedSide();
    }

    void MarkSelectedSide()
    {
        foreach (var option in Sides)
        {
            option.IsSelected = option.OnLeft == _settingPreferences.QuickAddOnLeft;
        }
    }

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

    [RelayCommand]
    Task OpenDetailedGuideAsync() => _guide.ShowDetailedAsync();

    [RelayCommand]
    Task OpenTermsAsync() => OpenLinkAsync(AppLinks.Terms);

    [RelayCommand]
    Task OpenPrivacyAsync() => OpenLinkAsync(AppLinks.Privacy);

    [RelayCommand]
    Task OpenContactAsync() => OpenLinkAsync(AppLinks.Contact);

    [RelayCommand]
    Task OpenLicensesAsync() => Shell.Current.GoToAsync(Views.LicensesPage.Route);

    /// <summary>Opens a page in the browser inside the app (Safari or Chrome's), so 設定 is right there to go back to.</summary>
    static async Task OpenLinkAsync(string url)
    {
        try
        {
            await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await Shell.Current.DisplayAlertAsync("ページをひらけませんでした", url, "OK");
        }
    }

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
            var settings = CurrentSettings;
            await Task.Run(() => _backup.CreateBackup(snapshot, settings));
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
                $"{picked.FileName}\n{contents.Summary}" + (contents.Settings is { } s ? $"\n設定：{s.Summary}" : string.Empty)
                + "\n\n今の記録はすべて、このバックアップの内容に置き換わります。", "置き換える", "やめる");
            if (!confirmed) return;

            var current = CurrentSettings;
            await Task.Run(() => _backup.Restore(copy, SafetyCopyPath, current));
            var note = contents.Settings is { } restored ? await ApplySettingsAsync(restored) : string.Empty;
            await Shell.Current.DisplayAlertAsync("復元しました", "バックアップの記録に戻しました。" + note, "OK");
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

    BackupSettings CurrentSettings => new(_settingPreferences.IsFingerprintEnabled, _settingPreferences.RecordsTotalRemainingToThisMonth);

    /// <summary>
    /// Applies a backup's settings. 指紋認証 is turned on only when this device can authenticate with a fingerprint;
    /// otherwise the app would ask for one at every launch and stay locked. Returns a note for the user, or "".
    /// </summary>
    async Task<string> ApplySettingsAsync(BackupSettings settings)
    {
        RecordsTotalRemainingToThisMonth = settings.CarriesOver;
        if (!settings.IsFingerprintEnabled)
        {
            IsFingerprintEnabled = false;
            return string.Empty;
        }

        var canUse = false;
        try
        {
            canUse = await _biometric.GetAuthenticationStatusAsync() == BiometricHwStatus.Success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
        if (canUse)
        {
            IsFingerprintEnabled = true;
            return string.Empty;
        }
        return $"\n\nこの端末では{Biometrics.Name}が使えないため、{Biometrics.Name}はオフのままにしました。{Biometrics.Means}を登録したあと、設定でオンにできます。";
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
