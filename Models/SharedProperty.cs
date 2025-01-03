using CommunityToolkit.Mvvm.ComponentModel;

namespace KakeiboApp.Models;

public partial class SharedProperty : ObservableObject
{
    private static readonly SharedProperty _instance = new SharedProperty();
    public static SharedProperty Instance => _instance;
    [ObservableProperty]
    DateTime _selectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
}
