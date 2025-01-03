using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.Data;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Picker;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace KakeiboApp.ViewModels;

public partial class DetailPageViewModel : BaseViewModel
{
	readonly ISpendingItemRepository _spendingItemRepository;
    readonly ICategoryRepository _categoryRepository;

    public DetailPageViewModel(ISpendingItemRepository spendingItemRepository, ICategoryRepository categoryRepository)
	{
        _spendingItemRepository = spendingItemRepository;
        _categoryRepository = categoryRepository;
        _ = Init();
    }

    [ObservableProperty]
    ObservableCollection<Category> _categories = default!;

    public SfDataGrid DataGrid { get; set; } = default!;

    private async Task Init()
    {
        var categories = await _categoryRepository.GetAllAsync();
        Categories = new ObservableCollection<Category>(categories);

        _sourceSpendingItemList = await _spendingItemRepository.GetAllAsync();
        ShowMonthlyResultStartFrom(SharedProperty.Instance.SelectedDate);
    }

    void ShowMonthlyResultStartFrom(DateTime startDate)
    {
        var endMonth = startDate.AddMonths(1);

        var selectedDate = SharedProperty.Instance.SelectedDate;
        var filteredList = _sourceSpendingItemList.Where(i => selectedDate <= i.Date && i.Date < endMonth).ToList();
        SpendingItemList = new(filteredList);
    }


    IEnumerable<SpendingItem> _sourceSpendingItemList = default!;

    [ObservableProperty]
    ObservableCollection<SpendingItem> _spendingItemList = default!;
    

    [RelayCommand]
    void DateSelectionChanged(DatePickerSelectionChangedEventArgs args)
    {
        // Should assign directory to make sure the property is updated.
        var selectedDate = SharedProperty.Instance.SelectedDate = args.NewValue ?? DateTime.Today;
        ShowMonthlyResultStartFrom(selectedDate);
    }

    [RelayCommand]
    public async Task RefreshGridAsync()
    {
        try
        {
            DataGrid.IsBusy = true;
            SaveGroupStates();
            await Init();
            RestoreGroupStates();
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

    private Dictionary<object, bool> _groupStates = new();

    private void SaveGroupStates()
    {
        _groupStates.Clear();

        foreach (Group group in DataGrid?.View?.Groups!)
        {
            var keyValue = group?.Key?.ToString();
            if (keyValue is null) continue;
            _groupStates[keyValue] = group?.IsExpanded ?? false;
        }
    }

    private void RestoreGroupStates()
    {
        foreach (Group group in DataGrid?.View?.Groups!)
        {
            var keyValue = group?.Key?.ToString();
            if (keyValue is null) continue;

            if (_groupStates.TryGetValue(keyValue, out var isExpanded) && group is not null)
            {
                if (isExpanded)
                {
                    DataGrid.ExpandGroup(group);
                }
                else
                {
                    DataGrid.CollapseGroup(group);
                }
            }
        }
    }
}