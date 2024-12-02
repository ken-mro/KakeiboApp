using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Plugin.Maui.Biometric;
using Syncfusion.Maui.Picker;

namespace KakeiboApp.ViewModels;

public partial class MainPageViewModel : BaseViewModel
{
    readonly IBiometric _biometric;
    readonly AppShell _appShell;
    public MainPageViewModel(IBiometric biometric, AppShell appShell)
    {
        _biometric = biometric;
        _appShell = appShell;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAuthButtonVisible))]
    bool _isAuthenticated = false;

    public bool IsAuthButtonVisible => !IsAuthenticated;

    public async Task AuthenticationRequest()
    {
        var result = await _biometric.AuthenticateAsync(new AuthenticationRequest()
        {
            Title = "認証",
            Description = "生体認証を行います",
            NegativeText = "キャンセル",
        }, CancellationToken.None);

        if (result.Status.Equals(BiometricResponseStatus.Success))
        {
            IsAuthenticated = true;
            _appShell.SetTabVisibility(true);
        }
    }

    [ObservableProperty]
    DateTime _selectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand]
    void DateSelectionChanged(DatePickerSelectionChangedEventArgs args)
    {
        SelectedDate = args.NewValue ?? DateTime.Today;
        //Todo: Implement the logic to show the monthly result
    }

    [RelayCommand]
    async Task AuthenticateAsync()
    {
        await AuthenticationRequest();
    }
}