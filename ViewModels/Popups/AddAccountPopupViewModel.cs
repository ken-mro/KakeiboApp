using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KakeiboApp.Models;
using CommunityToolkit.Maui.Views;
using KakeiboApp.Repository;
using Syncfusion.Maui.DataForm;
using CommunityToolkit.Maui.Alerts;
using static SQLite.SQLite3;

namespace KakeiboApp.ViewModels.Popups;

public partial class AddAccountPopupViewModel : BaseViewModel
{
    readonly IMonthlyIncomeDataRepository _incomeDataRepository;
    readonly IMonthlyFixedCostDataRepository _fixedCostDataRepository;
    readonly IMonthlySavingDataRepository _savingDataRepository;

    public AddAccountPopupViewModel(IMonthlyIncomeDataRepository incomeDataRepository, IMonthlyFixedCostDataRepository fixedCostDataRepository, IMonthlySavingDataRepository savingDataRepository, object inputObject, string formTitle)
    {
        _incomeDataRepository = incomeDataRepository;
        _fixedCostDataRepository = fixedCostDataRepository;
        _savingDataRepository = savingDataRepository;
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
        if (IsBusy)
            return;
        try
        {
            var dataFormLayout = dataForm as SfDataForm;
            dataFormLayout?.Commit();
            var isValid = dataFormLayout?.Validate() ?? false;
            if (!isValid) return;

            (int result, string name, decimal amount) = await TrySavingInstance();

            if (result != 0)
            {
                var snackBar = Snackbar.Make($"{name} {amount:C} が登録されました。", duration: TimeSpan.FromSeconds(1));
                snackBar?.Show();
                InitializeFormData();
            }
            else
            {
                throw new Exception();
            }

            InitializeFormData();
        }
        catch (Exception)
        {
            await Shell.Current.DisplayAlert("エラー", "登録に失敗しました。もう一度試してください。", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<(int result, string name, decimal amount)> TrySavingInstance()
    {
        if (FormDataObject is MonthlyIncome income)
        {
            var result = await _incomeDataRepository.AddAsync(income);
            if (result != 0)
            {
                return (result, income.Name, income.Amount);
            }
        }
        else if (FormDataObject is MonthlyFixedCost fixedCost)
        {
            var result = await _fixedCostDataRepository.AddAsync(fixedCost);
            if (result != 0)
            {
                return (result, fixedCost.Name, fixedCost.Amount);
            }
        }
        else if (FormDataObject is MonthlySaving saving)
        {
            var result = await _savingDataRepository.AddAsync(saving);
            if (result != 0)
            {
                return (result, saving.Name, saving.Amount);
            }
        }

        return (0, string.Empty, 0);
    }
}
