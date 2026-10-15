using CommunityToolkit.Mvvm.ComponentModel;

namespace KakeiboApp.Models;

public partial class SharedProperty : ObservableObject
{
    private static readonly SharedProperty _instance = new SharedProperty();
    public static SharedProperty Instance => _instance;
    [ObservableProperty]
    DateTime _selectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    /// <summary>
    /// 認証が済んだか(指紋認証がオフなら起動時に済む)。済むまではホーム以外を表示しない。
    /// </summary>
    [ObservableProperty]
    bool _isUnlocked;
}
