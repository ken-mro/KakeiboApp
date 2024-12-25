using KakeiboApp.CustomRenderers;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using KakeiboApp.ViewModels;

namespace KakeiboApp.Views;

public partial class DetailPage : ContentPage
{
    readonly ISpendingItemRepository _spendingItemRepository;
    readonly DetailPageViewModel _vm;
    public DetailPage(DetailPageViewModel vm, ISpendingItemRepository spendingItemRepository)
	{
		InitializeComponent();
        _spendingItemRepository = spendingItemRepository;
        
        var selectionBackground = dataGrid.DefaultStyle.SelectionBackground;
        dataGrid.CellRenderers.Remove("Numeric");
        dataGrid.CellRenderers.Add("Numeric", new CustomNumericCellRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("Text");
        dataGrid.CellRenderers.Add("Text", new CustomTextCellRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("ComboBox");
        dataGrid.CellRenderers.Add("ComboBox", new CustomComboBoxRenderer(selectionBackground));
        dataGrid.CellRenderers.Remove("DateTime");
        dataGrid.CellRenderers.Add("DateTime", new   CustomDataGridDateCellRenderer(selectionBackground));

        vm.DataGrid = dataGrid;
        BindingContext = _vm = vm;
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

            var propertyName = dataGrid?.Columns[e.RowColumnIndex.ColumnIndex - 1].MappingName;

            if (propertyName is null)
            {
                return;
            }   


            var spendingItem = dataGrid?.SelectedRow as SpendingItem;
            var property = spendingItem?.GetType().GetProperty(propertyName);

            if (propertyName.Equals("Amount") && (e.NewValue?.Equals((double)0) ?? false))
            {
                var id = (int?)spendingItem?.GetType().GetProperty("Id")?.GetValue(spendingItem) ?? 0;

                await _spendingItemRepository.DeleteAsync(id);
            }
            else
            {
                var convertedValue = Convert.ChangeType(e.NewValue, property!.PropertyType);
                property.SetValue(spendingItem, convertedValue);
                await _spendingItemRepository.UpdateAsync(spendingItem!);
            }

            await _vm.RefreshGridAsync();
        }
        catch (Exception ex)
        {
            // Handle conversion error
            Console.WriteLine($"Error converting value: {ex.Message}");
            await _vm.RefreshGridAsync();
        }
    }
}