using KakeiboApp.CustomRenderers;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.ViewModels;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.DataSource.Extensions;

namespace KakeiboApp.Views;

public partial class MainPage : ContentPage
{
    readonly MainPageViewModel _vm;
    readonly IMonthlyIncomeDataRepository _monthlyIncomeDataRepository;
    readonly IMonthlySavingDataRepository _monthlySavingDataRepository;
    readonly IMonthlyFixedCostDataRepository _monthlyFixedCostDataRepository;
    readonly IMonthlyBudgetDataRepository _monthlyBudgetDataRepository;
    public MainPage(MainPageViewModel vm, IMonthlyFixedCostDataRepository monthlyFixedCostDataRepository, IMonthlySavingDataRepository monthlySavingDataRepository, IMonthlyIncomeDataRepository monthlyIncomeDataRepository, IMonthlyBudgetDataRepository monthlyBudgetDataRepository)
    {
        InitializeComponent();
        InitDataGrid(incomeDataGrid);
        InitDataGrid(savingDataGrid);
        InitDataGrid(fixedCostDataGrid);
        InitDataGrid(budgetControlResultsDataGrid);
        InitDataGrid(savingsUntilPreviousMonthDataGrid);

        vm.IncomeDataGrid = incomeDataGrid;
        vm.SavingDataGrid = savingDataGrid;
        vm.FixedCostDataGrid = fixedCostDataGrid;
        vm.BudgetControlResultsDataGrid = budgetControlResultsDataGrid;
        vm.SavingsUntilPreviousMonthDataGrid = savingsUntilPreviousMonthDataGrid;
        BindingContext = _vm = vm;
        _monthlyIncomeDataRepository = monthlyIncomeDataRepository;
        _monthlySavingDataRepository = monthlySavingDataRepository;
        _monthlyFixedCostDataRepository = monthlyFixedCostDataRepository;
        _monthlyBudgetDataRepository = monthlyBudgetDataRepository;
    }

    private void InitDataGrid(SfDataGrid dataGrid)
    {
        var selectionBackground = dataGrid.DefaultStyle.SelectionBackground;
        dataGrid.CellRenderers.Remove("Numeric");
        dataGrid.CellRenderers.Add("Numeric", new CustomNumericCellRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("Text");
        dataGrid.CellRenderers.Add("Text", new CustomTextCellRenderer(selectionBackground));
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

                await _monthlyIncomeDataRepository.DeleteAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property!.PropertyType);
                property.SetValue(income, convertedValue);
                await _monthlyIncomeDataRepository.UpdateAsync(income!);
            }

            await _vm.RefreshIncomeDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private async void savingDataGrid_CurrentCellEndEditAsync(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
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


            var saving = dataGrid?.SelectedRow as MonthlySaving;
            var property = saving?.GetType().GetProperty(propertyName);

            if (propertyName.Equals("Amount") && (e.NewValue?.Equals((double)0) ?? false))
            {
                var id = (int?)saving?.GetType().GetProperty("Id")?.GetValue(saving) ?? 0;

                await _monthlySavingDataRepository.DeleteAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property!.PropertyType);
                property.SetValue(saving, convertedValue);
                await _monthlySavingDataRepository.UpdateAsync(saving!);
            }

            await _vm.RefreshSavingDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private async void fixedCostDataGrid_CurrentCellEndEditAsync(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
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

                await _monthlyFixedCostDataRepository.DeleteAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property?.PropertyType);
                property.SetValue(fixedCost, convertedValue);
                await _monthlyFixedCostDataRepository.UpdateAsync(fixedCost);
            }

            await _vm.RefreshFixedCostDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private async void budgetControlResultsDataGrid_CurrentCellEndEditAsync(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
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
                await _monthlyBudgetDataRepository.DeleteAsync(monthlyBudget.Id);
                await _vm.RefreshBudgetControlResultsDataGrid();
                return;
            }


            monthlyBudget.Amount = amount;

            if (monthlyBudget!.Id.Equals(0))
            {
                await _monthlyBudgetDataRepository.AddAsync(monthlyBudget);

            }
            else
            {
                await _monthlyBudgetDataRepository.UpdateAsync(monthlyBudget);
            }

            await _vm.RefreshBudgetControlResultsDataGrid();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
        }
    }

    private void dataGrid_QueryRowHeight(object sender, Syncfusion.Maui.DataGrid.DataGridQueryRowHeightEventArgs e)
    {
        e.Height = e.GetIntrinsicRowHeight(e.RowIndex);
        e.Handled = true;
    }
}
