using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Picker;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KakeiboApp.ViewModels;

public partial class DetailPageViewModel : BaseViewModel
{
	readonly ISpendingItemRepository _spendingItemRepository;
    public DetailPageViewModel(ISpendingItemRepository spendingItemRepository)
	{
        _spendingItemRepository = spendingItemRepository;
        _ = Init();
    }

    public SfDataGrid DataGrid { get; set; } = default!;

    private async Task Init()
    {
        _sourceSpendingItemList = await _spendingItemRepository.GetAllAsync();
        ShowMonthlyResultStartFrom(SelectedDate);
    }

    void ShowMonthlyResultStartFrom(DateTime startDate)
    {
        var endMonth = startDate.AddMonths(1);

        var filteredList = _sourceSpendingItemList.Where(i => SelectedDate <= i.Date && i.Date < endMonth).ToList();
        SpendingItemList = new(filteredList);
    }


    [ObservableProperty]
    DateTime _selectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    IEnumerable<SpendingItem> _sourceSpendingItemList = default!;

    [ObservableProperty]
    ObservableCollection<SpendingItem> _spendingItemList = default!;
    

    [RelayCommand]
    void DateSelectionChanged(DatePickerSelectionChangedEventArgs args)
    {
        SelectedDate = args.NewValue ?? DateTime.Today;
        ShowMonthlyResultStartFrom(SelectedDate);
    }

    [RelayCommand]
    async Task RefreshGridAsync()
    {
        try
        {
            DataGrid.IsBusy = true;
            await Init();
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            DataGrid.IsBusy = false;
        }
    }
}