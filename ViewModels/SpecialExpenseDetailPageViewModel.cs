using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Picker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KakeiboApp.ViewModels;

public partial class SpecialExpenseDetailPageViewModel : BaseViewModel
{
    readonly ISpecialExpenseDataRepository _specialExpenseDataRepository;
    public SpecialExpenseDetailPageViewModel(ISpecialExpenseDataRepository specialExpenseDataRepository)
    {
        _specialExpenseDataRepository = specialExpenseDataRepository;

        _ = Init();
    }

    public SfDataGrid DataGrid { get; set; } = default!;

    IEnumerable<SpecialExpense> _sourceSpecialExpenses = default!;

    [ObservableProperty]
    ObservableCollection<SpecialExpense> _specialExpenses = default!;

    private async Task Init()
    {
        _sourceSpecialExpenses = await _specialExpenseDataRepository.GetAllAsync();
        ShowYearlyResultStartFrom();
    }

    [RelayCommand]
    void DateSelectionChanged(DatePickerSelectionChangedEventArgs args)
    {
        // Should assign directory to make sure the property is updated.
        var selectedDate = SharedProperty.Instance.SelectedDate = args.NewValue ?? DateTime.Today;
        ShowYearlyResultStartFrom();
    }

    private void ShowYearlyResultStartFrom()
    {
        var selectedDate = SharedProperty.Instance.SelectedDate;
        var filteredList = _sourceSpecialExpenses.Where(i => i.Date.Year.Equals(selectedDate.Year)).ToList();
        SpecialExpenses = new(filteredList);
    }

    [RelayCommand]
    public async Task RefreshGridAsync()
    {
        try
        {
            DataGrid.IsBusy = true;
            await Init();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            DataGrid.IsBusy = false;
        }
    }
}
