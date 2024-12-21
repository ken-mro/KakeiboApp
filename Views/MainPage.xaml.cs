using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.ViewModels;
using Syncfusion.Maui.DataSource.Extensions;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage
{
    readonly MainPageViewModel _vm;
    readonly IMonthlyIncomeDataRepository _monthlyIncomeDataRepository;
    readonly IMonthlyFixedCostDataRepository _monthlyFixedCostDataRepository;
    readonly IMonthlyBudgetDataRepository _monthlyBudgetDataRepository;
    public MainPage(MainPageViewModel vm, IMonthlyFixedCostDataRepository monthlyFixedCostDataRepository, IMonthlyIncomeDataRepository monthlyIncomeDataRepository, IMonthlyBudgetDataRepository monthlyBudgetDataRepository)
    {
        InitializeComponent();
        vm.IncomeDataGrid = incomeDataGrid;
        vm.FixedCostDataGrid = fixedCostDataGrid;
        vm.BudgetControlResultsDataGrid = budgetControlResultsDataGrid;
        BindingContext = _vm = vm;
        _monthlyIncomeDataRepository = monthlyIncomeDataRepository;
        _monthlyFixedCostDataRepository = monthlyFixedCostDataRepository;
        _monthlyBudgetDataRepository = monthlyBudgetDataRepository;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (_vm.IsAuthenticated)
        {
            await _vm.InitData();
            return;
        }

        await Task.Delay(100);
        await _vm.AuthenticationRequest();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        datepicker.IsOpen = true;
    }

    private async void incomeDataGrid_CurrentCellEndEditAsync(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
    {
        try
        {

            if (e.OldValue?.ToString()?.Equals(e.NewValue?.ToString()) ?? false)
            {
                return;
            }

            var dataGrid = sender as Syncfusion.Maui.DataGrid.SfDataGrid;
            var propertyName = dataGrid?.Columns[e.RowColumnIndex.ColumnIndex].MappingName;

            if (propertyName is null)
            {
                return;
            }


            var income = dataGrid?.SelectedRow as MonthlyIncome;
            var property = income?.GetType().GetProperty(propertyName);

            if (propertyName.Equals("Amount") && (e.NewValue?.Equals((double)0) ?? false))
            {
                var id = (int?)income?.GetType().GetProperty("Id")?.GetValue(income) ?? 0;

                await _monthlyIncomeDataRepository.DeleteIncomeAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property!.PropertyType);
                property.SetValue(income, convertedValue);
                await _monthlyIncomeDataRepository.UpdateIncomeAsync(income!);
            }

            await _vm.RefreshIncomeDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private async void fixedCostDataGrid_CurrentCellEndEdit(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
    {
        try
        {
            if (e.OldValue?.ToString()?.Equals(e.NewValue?.ToString()) ?? false)
            {
                return;
            }

            var dataGrid = sender as Syncfusion.Maui.DataGrid.SfDataGrid;
            var propertyName = dataGrid?.Columns[e.RowColumnIndex.ColumnIndex].MappingName;

            if (propertyName is null)
            {
                return;
            }

                
            var fixedCost = dataGrid?.SelectedRow as MonthlyFixedCost;
            var property = fixedCost?.GetType().GetProperty(propertyName);

            if (propertyName.Equals("Amount") && (e.NewValue?.Equals((double)0) ?? false))
            {
                var id = (int?)fixedCost?.GetType().GetProperty("Id")?.GetValue(fixedCost) ?? 0;

                await _monthlyFixedCostDataRepository.DeleteFixedCostAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property?.PropertyType);
                property.SetValue(fixedCost, convertedValue);
                await _monthlyFixedCostDataRepository.UpdateFixedCostAsync(fixedCost);
            }

            await _vm.RefreshFixedCostDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private async void budgetControlResultsDataGrid_CurrentCellEndEdit(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
    {
        try
        {
            if (e.OldValue?.ToString()?.Equals(e.NewValue?.ToString()) ?? false)
            {
                return;
            }

            var dataGrid = sender as Syncfusion.Maui.DataGrid.SfDataGrid;
            var propertyName = dataGrid?.Columns[e.RowColumnIndex.ColumnIndex].MappingName;

            if (propertyName is null)
            {
                return;
            }

            var amount = e.NewValue is null ? 0 : Convert.ToDecimal(e.NewValue);
            var budgetControlResult = dataGrid?.SelectedRow as BudgetControlResult;
            var monthlyBudget = budgetControlResult?.MonthlyBudget;
            if (monthlyBudget is null) return;

            if (amount.Equals(0))
            {
                await _monthlyBudgetDataRepository.DeleteBudgetAsync(monthlyBudget.Id);
                await _vm.RefreshBudgetControlResultsDataGrid();
                return;
            }


            monthlyBudget.Amount = amount;

            if (monthlyBudget!.Id.Equals(0))
            {
                await _monthlyBudgetDataRepository.AddBudgetAsync(monthlyBudget);

            }
            else
            {
                await _monthlyBudgetDataRepository.UpdateBudgetAsync(monthlyBudget);
            }

            await _vm.RefreshBudgetControlResultsDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }
}
