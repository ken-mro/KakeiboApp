using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;

namespace KakeiboApp.ViewModels.Popups;

public partial class AddSpecialExpensePopupViewModel : BaseViewModel
{
    readonly ISpecialExpenseDataRepository _specialExpenseDataRepository;
    public AddSpecialExpensePopupViewModel(ISpecialExpenseDataRepository specialExpenseDataRepository, SpecialExpense inputObject)
    {
        _specialExpenseDataRepository = specialExpenseDataRepository;
        FormDataObject = inputObject;
        _defaultDate = inputObject.Date;
    }

    private DateTime _defaultDate = DateTime.Now;

    public SfDataForm DataForm = default!;

    [ObservableProperty]
    SpecialExpense _formDataObject;

    private void InitializeFormData()
    {
        var monthlyObject = DataForm.DataObject;

        monthlyObject?.GetType().GetProperty("Date")?.SetValue(monthlyObject, _defaultDate);
        DataForm.UpdateEditor("Date");
        monthlyObject?.GetType().GetProperty("Name")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("Name");
        decimal defaultAmount = 0;
        monthlyObject?.GetType().GetProperty("Amount")?.SetValue(monthlyObject, defaultAmount);
        DataForm.UpdateEditor("Amount");
        monthlyObject?.GetType().GetProperty("FromWhere")?.SetValue(monthlyObject, string.Empty);
        DataForm.UpdateEditor("FromWhere");
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

        if (FormDataObject is SpecialExpense specialExpense)
        {
            await _specialExpenseDataRepository.AddAsync(specialExpense);
        }

        InitializeFormData();
    }
}
