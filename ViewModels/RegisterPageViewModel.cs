using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using System.Collections.ObjectModel;

namespace KakeiboApp.ViewModels;

public partial class RegisterPageViewModel : BaseViewModel
{
    readonly ICategoryRepository _categoryRepository;
    readonly ISpendingItemRepository _spendingItemRepository;
    public RegisterPageViewModel(ICategoryRepository categoryRepository, ISpendingItemRepository spendingItemRepository)
    {
        _categoryRepository = categoryRepository;
        _spendingItemRepository = spendingItemRepository;
        _ = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        Categories = new ObservableCollection<Category>(categories);
        SelectedCategory = Categories.FirstOrDefault()!;
    }

    [ObservableProperty]
    ObservableCollection<Category> _categories = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistable))]
    Category _selectedCategory = default!;

    [ObservableProperty]
    DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistable))]
    string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRegistable))]
    decimal? _amount = default!;

    [ObservableProperty]
    string _note = string.Empty;

    public bool IsRegistable => SelectedCategory is not null 
                                && Amount > 0;

    public void ResetValues()
    {
        SelectedDate = DateTime.Today;
        SelectedCategory = Categories.FirstOrDefault()!;
        Name = string.Empty;
        Amount = default!;
        Note = string.Empty;
    }

    [RelayCommand]
    public async Task RegisterItemAsync()
    {
        if (IsBusy)
            return;
        try
        {
            IsBusy = true;
            var item = new SpendingItem
            {
                Date = SelectedDate,
                Category = SelectedCategory,
                Name = Name,
                Amount = Amount ?? 0,
                Note = Note,
            };

            await _spendingItemRepository.AddAsync(item);
            await Shell.Current.DisplayAlert("ê¨å˜", "ìoò^ÇµÇ‹ÇµÇΩÅB", "OK");
            ResetValues();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("ÉGÉâÅ[", "ìoò^Ç…é∏îsÇµÇ‹ÇµÇΩÅBÇ‡Ç§àÍìxééÇµÇƒÇ≠ÇæÇ≥Ç¢ÅB", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}