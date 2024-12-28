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

    public SfDataForm DataForm = default!;

    [ObservableProperty]
    object _formDataObject;

    [ObservableProperty]
    string _formTitle = string.Empty;

    private void InitializeFormData()
    {
        var monthlyObject = DataForm.DataObject;

        monthlyObject?.GetType().GetProperty("Name")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Name");
        decimal defaultAmount = 0;
        monthlyObject?.GetType().GetProperty("Amount")?.SetValue(monthlyObject, defaultAmount);
        DataForm.UpdateEditor("Amount");
        monthlyObject?.GetType().GetProperty("Note")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Note");
    }

    [RelayCommand]
    async Task RegisterAsync(object dataForm)
    {
        var dataFormLayout = dataForm as SfDataForm;
        dataFormLayout?.Commit();
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

        InitializeFormData();
    }
}
