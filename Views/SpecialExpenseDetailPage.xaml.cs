using KakeiboApp.CustomRenderers;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class SpecialExpenseDetailPage : ContentPage
{
    readonly SpecialExpenseDetailPageViewModel _vm;
    readonly ISpecialExpenseDataRepository _specialExpenseDataRepository;
    public SpecialExpenseDetailPage(SpecialExpenseDetailPageViewModel vm, ISpecialExpenseDataRepository specialExpenseDataRepository)
	{
		InitializeComponent();
        _specialExpenseDataRepository = specialExpenseDataRepository;

        var selectionBackground = dataGrid.DefaultStyle.SelectionBackground;
        dataGrid.CellRenderers.Remove("Numeric");
        dataGrid.CellRenderers.Add("Numeric", new CustomNumericCellRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("Text");
        dataGrid.CellRenderers.Add("Text", new CustomTextCellRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("ComboBox");
        dataGrid.CellRenderers.Add("ComboBox", new CustomComboBoxRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("DateTime");
        dataGrid.CellRenderers.Add("DateTime", new CustomDataGridDateCellRenderer(selectionBackground));

        vm.DataGrid = dataGrid;
        BindingContext = _vm = vm;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        await _vm.RefreshGridAsync();
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        datepicker.IsOpen = true;
    }

    private async void dataGrid_CurrentCellEndEdit(object sender, Syncfusion.Maui.DataGrid.DataGridCurrentCellEndEditEventArgs e)
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


            var specialExpense = dataGrid?.SelectedRow as SpecialExpense;
            var property = specialExpense?.GetType().GetProperty(propertyName);

            if (propertyName.Equals("Amount") && (e.NewValue?.Equals((double)0) ?? false))
            {
                var id = (int?)specialExpense?.GetType().GetProperty("Id")?.GetValue(specialExpense) ?? 0;

                await _specialExpenseDataRepository.DeleteAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property?.PropertyType);
                property.SetValue(specialExpense, convertedValue);
                await _specialExpenseDataRepository.UpdateAsync(specialExpense);
            }

            await _vm.RefreshGridAsync();
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