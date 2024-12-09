using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using CommunityToolkit.Maui.Views;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;

namespace KakeiboApp.ViewModels.Popups;

public partial class AddAccountPopupViewModel : BaseViewModel
{
    readonly IMonthlyIncomeDataRepository _incomeDataRepository;
    readonly IMonthlyFixedCostDataRepository _fixedCostDataRepository;

    public AddAccountPopupViewModel(IMonthlyIncomeDataRepository incomeDataRepository, IMonthlyFixedCostDataRepository fixedCostDataRepository, object inputObject, string formTitle)
    {
        _incomeDataRepository = incomeDataRepository;
        _fixedCostDataRepository = fixedCostDataRepository;
        FormDataObject = inputObject;
        FormTitle = formTitle;
    }

    public Popup popup = default!;

    [ObservableProperty]
    object _formDataObject;

    [ObservableProperty]
    string _formTitle = string.Empty;

    [RelayCommand]
    async Task RegisterAsync(object dataForm)
    {
        var dataFormLayout = dataForm as SfDataForm;
        var isValid = dataFormLayout?.Validate() ?? false;
        if (!isValid) return;

        int id = 0;
        if (FormDataObject is MonthlyIncome income)
        {
            id = await _incomeDataRepository.AddIncomeAsync(income);
        }
        else if (FormDataObject is MonthlyFixedCost fixedCost)
        {
            id = await _fixedCostDataRepository.AddFixedCostAsync(fixedCost);
        }

        var idProperty = FormDataObject.GetType().GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(FormDataObject, id);
        }

        await popup.CloseAsync();
    }
}
